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
            var reader = assembly.GetType("OQSDrug.DynamicsComReader", true);
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
                com.Checked = true;
                Require(!mdb.Checked && !path.Enabled && !browse.Enabled, "COM did not disable path controls");
                Render(control("tabPageMain"), Path.Combine(args[1], "dynamics-com.png"));
                mdb.Checked = true;
                Require(!com.Checked && path.Enabled && browse.Enabled, "MDB did not re-enable path controls");
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
