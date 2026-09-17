using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

// Standalone smoke test. Does not start the main application or save user settings.
public static class DynamicsComSmokeTests
{
    static void Require(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }

    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            string appPath = Path.GetFullPath(args[0]);
            AppDomain.CurrentDomain.AssemblyResolve += (sender, e) =>
            {
                string path = Path.Combine(Path.GetDirectoryName(appPath), new AssemblyName(e.Name).Name + ".dll");
                return File.Exists(path) ? Assembly.LoadFrom(path) : null;
            };
            var assembly = Assembly.LoadFrom(appPath);
            CheckSettingsReentry(assembly);
            MedicationInteractionTests.Run(assembly, args[1]);
            var reader = assembly.GetType("OQSDrug.DynamicsComReader", true);
            var readPatient = reader.GetMethod("ReadCurrentPatientApplication", BindingFlags.NonPublic | BindingFlags.Static);
            var patientApp = new FakePatientAccess();
            Func<long?> currentPatient = () => (long?)readPatient.Invoke(null, new object[] { patientApp });
            patientApp.Forms.Patient.Controls.Carte.Value = "123451";
            Require(currentPatient() == 123451 && currentPatient().Value / 10 == 12345, "Chart ID / branch conversion failed");
            patientApp.Forms.Patient.Controls.Carte.Value = 123459;
            Require(currentPatient() == 123459, "Branch-only transition was lost");
            patientApp.Forms.Patient.Controls.Carte.Value = DBNull.Value;
            Require(currentPatient() == 0, "Empty patient value was not distinguished from closed form");
            patientApp.Forms.Patient.Controls.Carte.Value = "invalid";
            Require(currentPatient() == 0, "Invalid ID accepted");
            patientApp.Forms.Exists = false;
            Require(currentPatient() == null, "Closed form not detected");
            patientApp.Forms.Exists = true;
            patientApp.Forms.Defined = false;
            ExpectFailure(() => currentPatient());
            patientApp.Forms.Defined = true;
            patientApp.Forms.Patient.NewRecord = true;
            Require(currentPatient() == 0, "New record was not ignored");
            patientApp.Forms.Patient.NewRecord = false;
            patientApp.Forms.Busy = true;
            ExpectFailure(() => currentPatient());
            patientApp.Forms.Busy = false;
            patientApp.Forms.Patient.Controls.Carte.FailRead = true;
            try { currentPatient(); throw new Exception("Expected property failure"); }
            catch (TargetInvocationException ex)
            {
                Require(ex.InnerException.HResult == unchecked((int)0x800A88D2)
                    && ex.InnerException.Message.Contains("Value"), "Property failure lost stage or HRESULT");
            }
            patientApp.Forms.Patient.Controls.Carte.FailRead = false;
            patientApp.Forms.Patient.Controls.Carte.Value = 456780;
            Require(currentPatient() == 456780, "Read did not recover after COM failure");
            Console.WriteLine("PASS: current chart control, branch IDs, empty/invalid values, closed form, busy failure");
            var read = reader.GetMethod("ReadApplication", BindingFlags.NonPublic | BindingFlags.Static);
            var recordset = new FakeRecordset(600);
            var app = new FakeAccess(recordset);
            using (var table = (DataTable)read.Invoke(null, new object[] { app, "SELECT * FROM [WKO資格確認結果表示]" }))
            {
                Require(table.Rows.Count == 600, "Batch transfer lost rows");
                Require((string)table.Rows[599][0] == "000599", "Text identifiers/leading zeros changed");
                Require(table.Rows[0][1] == DBNull.Value, "NULL was not preserved");
                Require((decimal)table.Rows[599][1] == 599.25m, "Currency precision changed");
                Require((DateTime)table.Rows[599][2] == new DateTime(2026, 9, 15), "Date type changed");
                Require((bool)table.Rows[599][3], "Boolean type changed");
                Require(recordset.ReadCalls == 3, "Expected batched reads");
                Require(recordset.Closed, "Snapshot was not closed");
            }
            Console.WriteLine("PASS: DAO batches, nonzero array bounds, types, NULL, snapshot cleanup");

            recordset = new FakeRecordset(0);
            using (var empty = (DataTable)read.Invoke(null, new object[] { new FakeAccess(recordset), "SELECT * FROM [WKO資格確認結果表示] WHERE 1=0" }))
                Require(empty.Rows.Count == 0 && empty.Columns.Count == 4, "Empty table lost schema");
            Console.WriteLine("PASS: successful zero-row read preserves schema");

            recordset = new FakeRecordset(1) { FailRead = true };
            ExpectFailure(() => read.Invoke(null, new object[] { new FakeAccess(recordset), "SELECT * FROM [WKO資格確認結果表示]" }));
            Require(recordset.Closed, "Failed snapshot leaked");
            app = new FakeAccess(new FakeRecordset(1));
            app.Forms.Exists = false;
            ExpectFailure(() => read.Invoke(null, new object[] { app, "SELECT * FROM [WKO資格確認結果表示]" }));
            Require(!app.Database.Opened, "Read occurred without patient form");
            app = new FakeAccess(new FakeRecordset(1));
            app.Database.TableDefs.Exists = false;
            ExpectFailure(() => read.Invoke(null, new object[] { app, "SELECT * FROM [WKO資格確認結果表示]" }));
            Require(!app.Database.Opened, "Read occurred without qualification table");
            Console.WriteLine("PASS: missing form/table and interrupted reads fail without returning partial data");

            Application.EnableVisualStyles();
            using (var form = (Form)Activator.CreateInstance(assembly.GetType("OQSDrug.Form2"), new object[] { null }))
            {
                var type = form.GetType();
                Func<string, Control> control = name => (Control)type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
                var mdb = (RadioButton)control("radioButtonDynamicsMdb");
                var com = (RadioButton)control("radioButtonDynamicsCom");
                var path = (TextBox)control("textBoxDatadyna");
                var browse = control("buttonDatadyna");
                path.Text = @"C:\example\client.mdb";
                Require(mdb.Checked && path.Enabled && browse.Enabled, "MDB default changed");
                var link = (ComboBox)control("comboBoxRSBID");
                link.SelectedIndex = 4;
                com.Checked = true;
                Require(!mdb.Checked && !path.Enabled && !browse.Enabled, "COM did not disable path controls");
                Require(!link.Enabled && (string)link.SelectedItem == "COM連携", "COM patient link was not forced");
                var interactionCheck = (CheckBox)control("checkBoxInteractionCheck");
                var viewerPage = control("tabPageViewer");
                Render(viewerPage, Path.Combine(args[1], "interaction-settings-com.png"));
                Render(control("tabPageMain"), Path.Combine(args[1], "dynamics-com.png"));
                mdb.Checked = true;
                Require(!com.Checked && path.Enabled && browse.Enabled, "MDB did not re-enable path controls");
                Require(link.Enabled && !link.Items.Contains("COM連携"), "File patient link was not restored");
                Require(link.SelectedIndex == 4, "Previous file patient link was lost");
                Render(viewerPage, Path.Combine(args[1], "interaction-settings-mdb.png"));
                Require(path.Text == @"C:\example\client.mdb", "Switching mode erased MDB path");
                Render(control("tabPageMain"), Path.Combine(args[1], "dynamics-mdb.png"));
            }
            Console.WriteLine("PASS: settings radio buttons, path enable/disable, retained path, rendered both modes");

            if (Process.GetProcessesByName("msaccess").Length == 0)
            {
                var check = reader.GetMethod("CheckAvailabilityAsync");
                for (int i = 0; i < 2; i++)
                {
                    var task = (Task<string>)check.Invoke(null, null);
                    Require(task.Wait(10000), "COM availability check stalled without Access");
                    Require(task.Result.StartsWith("電カルCOM接続待ち:"), "Missing Access did not return waiting state");
                }
                var settingsType = assembly.GetType("OQSDrug.Properties.Settings", true);
                var settings = settingsType.GetProperty("Default").GetValue(null);
                var setting = settingsType.GetProperty("DynamicsUseCom");
                setting.SetValue(settings, true); // in-memory only; never Save().
                var source = assembly.GetType("OQSDrug.DynamicsDataSource", true);
                var open = (Task)source.GetMethod("OpenReaderAsync").Invoke(null,
                    new object[] { "invalid path; must not be used", "SELECT * FROM [WKO資格確認結果表示]", false });
                try { open.GetAwaiter().GetResult(); throw new Exception("Unexpected successful COM read"); }
                catch (System.Runtime.InteropServices.COMException) { }
                setting.SetValue(settings, false);
                Require(Process.GetProcessesByName("msaccess").Length == 0, "Access was unexpectedly launched");
                Console.WriteLine("PASS: STA dispatch, repeated absent-Access checks, no MDB fallback/no Access launch");
            }
            else Console.WriteLine("SKIP: absent-Access check (an Access process is running)");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    static void CheckSettingsReentry(Assembly assembly)
    {
        var type = assembly.GetType("OQSDrug.Form1");
        var common = assembly.GetType("OQSDrug.CommonFunctions");
        var log = common.GetField("UiLogCallback");
        var sync = common.GetField("UiSync");
        var oldLog = log.GetValue(null);
        var oldSync = sync.GetValue(null);
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        try
        {
            using (var form = (Form)Activator.CreateInstance(type))
            using (var dialog = (Form)Activator.CreateInstance(assembly.GetType("OQSDrug.Form2"), new object[] {form}))
            {
                try
                {
                    type.GetField("settingsFlowActive", flags).SetValue(form, true);
                    type.GetField("settingsDialogOpen", flags).SetValue(form, true);
                    type.GetField("settingsForm", flags).SetValue(form, dialog);
                    type.GetField("initializationRevision", flags).SetValue(form, 10);
                    type.GetMethod("toolStripButtonSettings_Click", flags).Invoke(form, new object[] {null, EventArgs.Empty});
                    Require(ReferenceEquals(type.GetField("settingsForm", flags).GetValue(form), dialog), "Settings reentry replaced the existing dialog");
                    var init = (Task)type.GetMethod("initializeForm", flags).Invoke(form, new object[] {true});
                    Require(init.IsCompleted, "Initialization did not stop while settings was open");
                    Require((int)type.GetField("initializationRevision", flags).GetValue(form)==10, "Reentry invalidated the active settings flow");
                    var timer = (Task)type.GetMethod("RunTimerLogicAsync", flags).Invoke(form, null);
                    Require(timer.IsCompleted, "Timer started DB work while settings was open");
                    var status = (Task<byte>)type.GetMethod("UpdateStatus", flags).Invoke(form, null);
                    Require(status.IsCompleted && status.Result==0, "Status tried to connect while settings was open");
                    type.GetMethod("StartTimer").Invoke(form, null);
                    Require(type.GetField("backgroundTimer", flags).GetValue(form)==null, "Timer restarted during settings");
                    type.GetField("settingsFlowActive", flags).SetValue(form, false);
                    type.GetField("settingsDialogOpen", flags).SetValue(form, false);
                    type.GetMethod("StartTimer").Invoke(form, null);
                    Require(type.GetField("backgroundTimer", flags).GetValue(form)==null, "Timer started before initialization completed");
                    // The previous dialog closed, but its initialization is still awaiting IO.
                    type.GetField("settingsForm", flags).SetValue(form, null);
                    type.GetField("settingsFlowActive", flags).SetValue(form, true);
                    var begin = type.GetMethod("TryBeginSettingsDialog", flags);
                    var finish = type.GetMethod("FinishSettingsFlow", flags);
                    int first = (int)begin.Invoke(form, null);
                    Require(first > 0, "Pending post-settings initialization blocked reopening");
                    Require((int)begin.Invoke(form, null)==0, "An open dialog allowed a second dialog");
                    type.GetField("settingsDialogOpen", flags).SetValue(form, false);
                    int second = (int)begin.Invoke(form, null);
                    Require(second > first, "Second settings session did not supersede the first");
                    finish.Invoke(form, new object[] {first});
                    Require((bool)type.GetField("settingsDialogOpen", flags).GetValue(form)
                        && (bool)type.GetField("settingsFlowActive", flags).GetValue(form), "Old completion unlocked the newer dialog");
                    finish.Invoke(form, new object[] {second});
                    Require(!(bool)type.GetField("settingsFlowActive", flags).GetValue(form), "Current session did not unlock");
                }
                finally
                {
                    ((IDisposable)type.GetField("resImportWorker", flags).GetValue(form)).Dispose();
                }
            }
        }
        finally { log.SetValue(null,oldLog); sync.SetValue(null,oldSync); }
        Console.WriteLine("PASS: settings reentry, initialization/status/DB timer suppression, uninitialized timer guard");
    }

    static void Render(Control control, string path)
    {
        // Render a detached panel so the settings form's Load handler (which saves settings) never runs.
        var children = new Control[control.Controls.Count];
        control.Controls.CopyTo(children, 0);
        using (var panel = new Panel { Size = control.Size, BackColor = Color.White })
        using (var bitmap = new Bitmap(control.Width, control.Height))
        {
            try
            {
                panel.Controls.AddRange(children);
                panel.DrawToBitmap(bitmap, new Rectangle(Point.Empty, panel.Size));
                bitmap.Save(path);
            }
            finally { control.Controls.AddRange(children); }
        }
    }

    static void ExpectFailure(Action action)
    {
        try { action(); }
        catch (TargetInvocationException) { return; }
        throw new Exception("Expected source failure");
    }
}

// Public types allow the production late-bound DAO code to exercise a fake Access object model.
public sealed class FakeAccess
{
    public FakeNamedObjects Forms { get; } = new FakeNamedObjects("患者マスター");
    public FakeDatabase Database { get; }
    public FakeAccess(FakeRecordset recordset) { Database = new FakeDatabase(recordset); }
    public FakeDatabase CurrentDb() => Database;
}
public sealed class FakeNamedObjects
{
    readonly string name;
    public bool Exists = true;
    public FakeNamedObjects(string name) { this.name = name; }
    public object this[string key] => Exists && key == name ? new object() : throw new InvalidOperationException("Missing " + key);
}
public sealed class FakeDatabase
{
    readonly FakeRecordset recordset;
    public bool Opened;
    public FakeNamedObjects TableDefs { get; } = new FakeNamedObjects("WKO資格確認結果表示");
    public FakeDatabase(FakeRecordset recordset) { this.recordset = recordset; }
    public FakeRecordset OpenRecordset(string sql, int type, int options)
    {
        if (type != 4 || options != 4) throw new Exception("Expected read-only snapshot");
        Opened = true;
        return recordset;
    }
}
public sealed class FakeFields
{
    readonly FakeField[] fields = { new FakeField("コード", 10), new FakeField("金額", 5), new FakeField("日時", 8), new FakeField("同意", 1) };
    public int Count => fields.Length;
    public FakeField this[int index] => fields[index];
}
public sealed class FakeField
{
    public string Name { get; }
    public int Type { get; }
    public FakeField(string name, int type) { Name = name; Type = type; }
}
public sealed class FakeRecordset
{
    readonly int total;
    int position;
    public bool Closed, FailRead;
    public int ReadCalls;
    public FakeFields Fields { get; } = new FakeFields();
    public FakeRecordset(int rows) { total = rows; }
    public bool EOF => position >= total;
    public Array GetRows(int requested)
    {
        if (FailRead) throw new InvalidOperationException("Simulated disconnect");
        int count = Math.Min(requested, total - position);
        var result = Array.CreateInstance(typeof(object), new[] { 4, count }, new[] { 1, 2 });
        for (int i = 0; i < count; i++, position++)
        {
            result.SetValue(position.ToString("D6"), 1, i + 2);
            result.SetValue(position == 0 ? null : (object)(position + .25m), 2, i + 2);
            result.SetValue(new DateTime(2026, 9, 15), 3, i + 2);
            result.SetValue(true, 4, i + 2);
        }
        ReadCalls++;
        return result;
    }
    public void Close() { Closed = true; }
}

public sealed class FakePatientAccess
{
    public FakePatientForms Forms { get; } = new FakePatientForms();
    public FakePatientProject CurrentProject => new FakePatientProject(Forms);
}
public sealed class FakePatientProject
{
    public FakePatientProject(FakePatientForms forms) { AllForms = new FakePatientMetadataCollection(forms); }
    public FakePatientMetadataCollection AllForms { get; }
}
public sealed class FakePatientMetadataCollection
{
    private readonly FakePatientForms forms;
    public FakePatientMetadataCollection(FakePatientForms forms) { this.forms = forms; }
    public int Count => throw new Exception("Polling must not enumerate AllForms");
    public FakePatientMetadata this[int index] => throw new Exception("Polling must look up the form by name");
    public FakePatientMetadata this[string name]
    {
        get
        {
            if (forms.Busy) throw new System.Runtime.InteropServices.COMException("Busy");
            if (name != "患者マスター" || !forms.Defined) throw new System.Runtime.InteropServices.COMException("Missing form definition");
            return new FakePatientMetadata(forms.Exists);
        }
    }
}
public sealed class FakePatientMetadata
{
    public FakePatientMetadata(bool loaded) { IsLoaded = loaded; }
    public string Name => throw new Exception("Polling must not read form names");
    public bool IsLoaded { get; }
}
public sealed class FakePatientForms
{
    public bool Exists = true, Defined = true, Busy;
    public FakePatientForm Patient { get; } = new FakePatientForm();
    public int Count => Busy ? throw new System.Runtime.InteropServices.COMException("Busy") : (Exists ? 1 : 0);
    public FakePatientForm this[int index] => Patient;
    public FakePatientForm this[string name] => name == "患者マスター" && Exists ? Patient : throw new Exception("Missing form");
}
public sealed class FakePatientForm
{
    public string Name => throw new System.Runtime.InteropServices.COMException("Live form Name fails", unchecked((int)0x800A88D2));
    public bool NewRecord { get; set; }
    public FakePatientControls Controls { get; } = new FakePatientControls();
}
public sealed class FakePatientControls
{
    public FakePatientControl Carte { get; } = new FakePatientControl();
    public FakePatientControl this[string name] => name == "カルテ番号" ? Carte : throw new Exception("Unexpected control");
}
public sealed class FakePatientControl
{
    private object value;
    public bool FailRead;
    public object Value
    {
        get => FailRead ? throw new System.Runtime.InteropServices.COMException("Value fails", unchecked((int)0x800A88D2)) : value;
        set => this.value = value;
    }
}
