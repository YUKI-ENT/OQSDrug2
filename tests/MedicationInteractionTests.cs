using System;
using System.Collections;
using System.Data;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

public static class MedicationInteractionTests
{
    static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
    static void Set(object obj, string field, object value) { obj.GetType().GetField(field).SetValue(obj, value); }
    public static void Run(Assembly assembly, string output)
    {
        var snapshotType = assembly.GetType("OQSDrug.ChartMedicationSnapshot");
        var medType = assembly.GetType("OQSDrug.ChartMedication");
        Func<string, string, object> med = (name, code) =>
        {
            var m = Activator.CreateInstance(medType); Set(m,"Name",name); Set(m,"InternalCode",code); Set(m,"Quantity","1"); return m;
        };
        var snapshot = Activator.CreateInstance(snapshotType);
        Set(snapshot,"ChartId",123451L); Set(snapshot,"Visit","100"); Set(snapshot,"VisitDate",DateTime.Today);
        var list = (IList)snapshotType.GetField("Medications").GetValue(snapshot);
        list.Add(med("テスト薬", "1"));
        var stateType = assembly.GetType("OQSDrug.MedicationConfirmationState");
        var state = Activator.CreateInstance(stateType);
        Func<bool> observe = () => (bool)stateType.GetMethod("Observe").Invoke(state,new[] { snapshot });
        Require(!observe() && !observe(), "Unconfirmed list triggered");
        var fee = med("処方箋料（その他）", "100001"); list.Add(fee);
        Require(!observe() && observe(), "Confirmation must stabilize twice");
        stateType.GetMethod("Complete").Invoke(state,new[] { snapshot });
        Require(!observe(), "Unchanged list retriggered");
        list.Add(med("追加薬", "2")); Require(!observe() && !observe(), "Edit with old fee retriggered");
        list.Remove(fee); Require(!observe(), "Fee removal triggered"); list.Add(fee);
        Require(!observe() && observe(), "Reconfirmation failed");
        Set(snapshot,"Visit","101"); Require(!observe() && observe(), "New visit failed");

        var reader = assembly.GetType("OQSDrug.DynamicsComReader");
        var readApplication = reader.GetMethod("ReadMedicationApplication",BindingFlags.Static|BindingFlags.NonPublic);
        var fakeApp = new InteractionFakeAccess();
        var captured = readApplication.Invoke(null,new object[] {fakeApp});
        var capturedMeds = (IList)snapshotType.GetField("Medications").GetValue(captured);
        Require(capturedMeds.Count==2 && (string)medType.GetField("ReceptCode").GetValue(capturedMeds[0])=="620000001",
            "COM snapshot or master mapping failed");
        Require(fakeApp.Patient.Sub.LastClone.Closed && fakeApp.Database.AllClosed,"COM clone/query cleanup failed");
        Require((DateTime)snapshotType.GetField("VisitDate").GetValue(captured)==DateTime.Today,"Visit date lost");
        fakeApp.Patient.Sub.Dirty=true;
        try { readApplication.Invoke(null,new object[] {fakeApp}); throw new Exception("Dirty snapshot accepted"); }
        catch (TargetInvocationException ex) { Require(ex.InnerException.Message.Contains("保存待ち"),"Dirty state not recognized"); }
        fakeApp.Patient.Sub.Dirty=false;
        fakeApp.Database.SwitchPatient=true;
        try { readApplication.Invoke(null,new object[] {fakeApp}); throw new Exception("Changed patient accepted"); }
        catch (TargetInvocationException ex) { Require(ex.InnerException.Message.Contains("変更"),"Patient change not detected"); }
        Require(fakeApp.Patient.Sub.LastClone.Closed && fakeApp.Database.AllClosed,"Failure cleanup leaked a snapshot");
        var parse = reader.GetMethod("ParseMedicationRows",BindingFlags.Static|BindingFlags.NonPublic);
        var table = new DataTable(); for (int i=1;i<=6;i++) table.Columns.Add("式"+i);
        table.Rows.Add("100","123451","1","2","テスト薬","1");
        table.Rows.Add("999","999990","1","2","別受診","1");
        Require(((IList)parse.Invoke(null,new object[] { table,123451L,"100" })).Count==1,"Visit filtering/alias columns failed");
        table.Rows.Add("100","123452","1","2","別枝番","1");
        try { parse.Invoke(null,new object[] { table,123451L,"100" }); throw new Exception("Foreign branch accepted"); }
        catch (TargetInvocationException ex) { Require(ex.InnerException is InvalidOperationException,"Unexpected mismatch error"); }

        var match = assembly.GetType("OQSDrug.MedicationInteractionCheck").GetMethod("MatchName",BindingFlags.Static|BindingFlags.NonPublic);
        var koro = assembly.GetType("OQSDrug.MedicationInteractionCheck").GetMethod("MatchKoro",BindingFlags.Static|BindingFlags.NonPublic);
        Require((bool)koro.Invoke(null,new object[] {"1","2","1","2"}),"KORO forward failed");
        Require((bool)koro.Invoke(null,new object[] {"1","2","2","1"}),"KORO reverse failed");
        Require(!(bool)koro.Invoke(null,new object[] {"","2","","2"}),"Missing codes falsely matched");
        Require(!(bool)koro.Invoke(null,new object[] {"1","2","1","3"}),"Unrelated codes matched");
        var drug = med("クラリス錠２００ｍｇ", "3"); Set(drug,"GenericName","クラリスロマイシン");
        Require(match.Invoke(null,new object[] { "CYP3A阻害剤（クラリスロマイシン、イトラコナゾール等）", drug })!=null,"Generic-name list match failed");
        Require(match.Invoke(null,new object[] { "CYP3A阻害剤", drug })==null,"Class heading matched");
        Require(match.Invoke(null,new object[] { "剤", drug })==null,"Short fragment matched");
        Require(match.Invoke(null,new object[] { "クラリスロマイシンを除く", drug })==null,"Exclusion matched");

        using (var form = (Form)Activator.CreateInstance(assembly.GetType("OQSDrug.FormInteractionCheck"),true))
        {
            form.GetType().GetMethod("ShowSnapshot",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(form,new[] {snapshot});
            form.ShowInTaskbar = false; form.StartPosition = FormStartPosition.Manual; form.Location = new Point(-2000,-2000);
            form.Show(); form.PerformLayout(); Application.DoEvents();
            using (var bmp = new Bitmap(form.Width, form.Height)) { form.DrawToBitmap(bmp,new Rectangle(Point.Empty,form.Size)); bmp.Save(Path.Combine(output,"interaction-form.png")); }
        }
        Console.WriteLine("PASS: confirmation stability, re-edit/reconfirmation, visit reset, aliases, branch isolation, generic matching, form render");
        CheckEmbeddedTab(assembly, output, snapshot);
    }

    private static void CheckEmbeddedTab(Assembly assembly, string output, object snapshot)
    {
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var mainType = assembly.GetType("OQSDrug.Form1");
        var viewerType = assembly.GetType("OQSDrug.FormDI");
        var settingsType = assembly.GetType("OQSDrug.Properties.Settings");
        var settings = settingsType.GetProperty("Default").GetValue(null);
        var comSetting = settingsType.GetProperty("DynamicsUseCom");
        var enabledSetting = settingsType.GetProperty("InteractionCheckEnabled");
        var oldCom = comSetting.GetValue(settings); var oldEnabled = enabledSetting.GetValue(settings);
        var common = assembly.GetType("OQSDrug.CommonFunctions");
        var log = common.GetField("UiLogCallback"); var sync = common.GetField("UiSync");
        var oldLog = log.GetValue(null); var oldSync = sync.GetValue(null);
        try
        {
            comSetting.SetValue(settings, true); enabledSetting.SetValue(settings, true);
            using (var main = (Form)Activator.CreateInstance(mainType))
            using (var viewer = (Form)Activator.CreateInstance(viewerType, new object[] { main }))
            using (var combo = new ToolStripComboBox())
            using (var host = new Form { Size = new Size(1250, 650), ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual, Location = new Point(-2000, -2000) })
            {
                // A selection without the production DB-loading event handler.
                var patientType = assembly.GetType("OQSDrug.FormTKK+PtItem");
                if (patientType == null)
                    foreach (var type in assembly.GetTypes()) if (type.Name == "PtItem") { patientType = type; break; }
                var patient = Activator.CreateInstance(patientType);
                patientType.GetProperty("PtID").SetValue(patient, 12345L);
                patientType.GetProperty("DisplayText").SetValue(patient, "12345 : テスト患者");
                combo.Items.Add(patient); combo.SelectedIndex = 0;
                viewerType.GetField("toolStripComboBoxPt", flags).SetValue(viewer, combo);
                var tabs = (TabControl)viewerType.GetField("tabControl1", flags).GetValue(viewer);
                host.Controls.Add(tabs);
                var update = viewerType.GetMethod("UpdateInteractionCheck", flags);
                var resultType = assembly.GetType("OQSDrug.MedicationInteractionResult");
                var result = Activator.CreateInstance(resultType);
                Action<int, long, object> render = (revision, chart, value) => update.Invoke(viewer,
                    new object[] { true, revision, chart, snapshot, value, "チェック完了", false, 6 });
                render(1, 12345, result);
                var page = (TabPage)viewerType.GetField("interactionCheckPage", flags).GetValue(viewer);
                tabs.SelectedTab = page;
                Require(page.ImageIndex == 1 && page.Text.Contains("該当なし"), "No-hit result must be green");
                var panel = (Form)viewerType.GetField("interactionCheckView", flags).GetValue(viewer);
                Require(!panel.TopLevel && panel.Parent == page, "Result opened outside the history tab");
                var grid = (DataGridView)panel.GetType().GetField("medications", flags).GetValue(panel);
                var oldSource = grid.DataSource;
                render(1, 12345, result);
                Require(ReferenceEquals(oldSource, grid.DataSource), "Unchanged view was rebound");
                host.Show(); Application.DoEvents();
                using (var bitmap = new Bitmap(host.Width, host.Height))
                { host.DrawToBitmap(bitmap, new Rectangle(Point.Empty, host.Size)); bitmap.Save(Path.Combine(output, "interaction-tab-green.png")); }
                var hit = Activator.CreateInstance(assembly.GetType("OQSDrug.MedicationInteractionHit"));
                Set(hit, "Section", "併用禁忌"); Set(hit, "Current", "今回薬"); Set(hit, "History", "他院薬");
                ((IList)resultType.GetField("Koro").GetValue(result)).Add(hit);
                render(2, 12345, result);
                Require(page.ImageIndex == 2 && page.Text.Contains("要確認"), "Hit must be red");
                Application.DoEvents();
                using (var bitmap = new Bitmap(host.Width, host.Height))
                { host.DrawToBitmap(bitmap, new Rectangle(Point.Empty, host.Size)); bitmap.Save(Path.Combine(output, "interaction-tab-red.png")); }
                render(2, 99999, result);
                Require(page.ImageIndex == 0 && grid.DataSource == null, "Other patient's result remained visible");
                render(3, 12345, null);
                Require(page.ImageIndex == 0, "Missing result must not be green");
                update.Invoke(viewer, new object[] { false, 4, 12345L, null, null, "", false, 6 });
                Require(viewerType.GetField("interactionCheckPage", flags).GetValue(viewer) == null, "Disabled tab remained");
                mainType.GetField("interactionComplete", flags).SetValue(main, true);
                mainType.GetField("interactionSnapshot", flags).SetValue(main, snapshot);
                mainType.GetField("interactionResult", flags).SetValue(main, result);
                mainType.GetMethod("InvalidateInteractionHistory", flags).Invoke(main, new object[] { 12345L });
                Require((bool)mainType.GetField("interactionComplete", flags).GetValue(main)
                    && mainType.GetField("interactionResult", flags).GetValue(main) == null,
                    "History update must mark results stale without restarting automatic reads");
                using (var timer = new Timer())
                {
                    mainType.GetField("comPatientTimer", flags).SetValue(main, timer);
                    var poll = (System.Threading.Tasks.Task)mainType.GetMethod("PollInteractionAsync", flags).Invoke(main, new object[] { false });
                    Require(poll.IsCompleted && !poll.IsFaulted, "Completed check did not stop before COM dispatch");
                    mainType.GetField("comPatientTimer", flags).SetValue(main, null);
                }
                mainType.GetMethod("ResetInteraction", flags).Invoke(main, null);
                Require(!(bool)mainType.GetField("interactionComplete", flags).GetValue(main), "Patient reset did not rearm checks");
            }
        }
        finally
        {
            comSetting.SetValue(settings, oldCom); enabledSetting.SetValue(settings, oldEnabled);
            log.SetValue(null, oldLog); sync.SetValue(null, oldSync);
        }
        Console.WriteLine("PASS: embedded tab, green/red/unknown badges, no unchanged rebinding, patient isolation, option off, completion stop/reset");
    }
}

public sealed class InteractionFakeAccess
{
    public FakePatientProject CurrentProject { get; } = new FakePatientProject(new FakePatientForms());
    public InteractionFakePatient Patient { get; } = new InteractionFakePatient();
    public InteractionObjectMap Forms { get; }
    public InteractionFakeDb Database { get; }
    public InteractionFakeAccess()
    {
        Forms = new InteractionObjectMap(); Forms.Values["患者マスター"] = Patient;
        Database = new InteractionFakeDb(Patient);
    }
    public InteractionFakeDb CurrentDb() => Database;
}
public sealed class InteractionObjectMap
{
    public System.Collections.Generic.Dictionary<string,object> Values = new System.Collections.Generic.Dictionary<string,object>();
    public object this[string name] => Values[name];
}
public sealed class InteractionValue
{
    private readonly Func<object> get;
    public InteractionValue(Func<object> get) { this.get = get; }
    public object Value => get();
}
public sealed class InteractionFakePatient
{
    public bool NewRecord => false;
    public long Chart = 123451;
    public InteractionObjectMap Controls { get; } = new InteractionObjectMap();
    public InteractionFakeSubform Sub { get; } = new InteractionFakeSubform();
    public InteractionFakePatient()
    {
        Controls.Values["カルテ番号"] = new InteractionValue(()=>Chart);
        Controls.Values["氏名"] = new InteractionValue(()=>"テスト患者");
        Controls.Values["受診投薬サブフォーム"] = new InteractionSubControl(Sub);
    }
}
public sealed class InteractionSubControl
{
    public InteractionSubControl(InteractionFakeSubform form) { Form=form; }
    public InteractionFakeSubform Form { get; }
}
public sealed class InteractionFakeSubform
{
    public bool Dirty { get; set; }
    public InteractionObjectMap Controls { get; } = new InteractionObjectMap();
    public InteractionRecordset LastClone;
    public InteractionFakeSubform() { Controls.Values["受診コード"]=new InteractionValue(()=>"100"); }
    public InteractionRecordset RecordsetClone
    {
        get
        {
            var t = new DataTable(); for(int i=1;i<=6;i++) t.Columns.Add("式"+i,typeof(object));
            t.Rows.Add("100",123451,1,1,"テスト薬",1);
            t.Rows.Add("100",123451,2,100001,"処方箋料",1);
            return LastClone = new InteractionRecordset(t);
        }
    }
}
public sealed class InteractionFakeDb
{
    private readonly InteractionFakePatient patient;
    public bool SwitchPatient;
    private readonly System.Collections.Generic.List<InteractionRecordset> sets = new System.Collections.Generic.List<InteractionRecordset>();
    public bool AllClosed { get { foreach(var s in sets) if(!s.Closed) return false; return true; } }
    public InteractionFakeDb(InteractionFakePatient patient) { this.patient=patient; }
    public InteractionRecordset OpenRecordset(string sql, int kind, int options)
    {
        if(kind!=4 || options!=4) throw new Exception("Expected read-only query");
        var t = new DataTable();
        if(sql.Contains("FROM [受診]"))
        {
            if(!sql.Contains("123451") || !sql.Contains("'100'")) throw new Exception("Query not scoped to patient/visit");
            t.Columns.Add("受診日",typeof(object)); t.Rows.Add(DateTime.Today);
        }
        else if(sql.Contains("FROM [薬マスター]"))
        {
            if(!sql.Contains("IN (1)")) throw new Exception("Non-drug fee entered master lookup");
            t.Columns.Add("薬コード",typeof(object)); t.Columns.Add("厚生省コード",typeof(object)); t.Rows.Add(1,"620000001");
        }
        else throw new Exception("Unexpected SQL");
        if(SwitchPatient) patient.Chart=999990;
        var result=new InteractionRecordset(t); sets.Add(result); return result;
    }
}
public sealed class InteractionRecordset
{
    private readonly DataTable table;
    private int position;
    public bool Closed;
    public InteractionRecordset(DataTable table) { this.table=table; Fields=new InteractionFields(table); }
    public InteractionFields Fields { get; }
    public bool BOF => table.Rows.Count==0;
    public bool EOF => position>=table.Rows.Count;
    public void MoveFirst() { position=0; }
    public Array GetRows(int requested)
    {
        int count=Math.Min(requested,table.Rows.Count-position);
        var result=Array.CreateInstance(typeof(object),new[]{table.Columns.Count,count},new[]{1,2});
        for(int r=0;r<count;r++) for(int c=0;c<table.Columns.Count;c++) result.SetValue(table.Rows[position+r][c],c+1,r+2);
        position+=count; return result;
    }
    public void Close() { Closed=true; }
}
public sealed class InteractionFields
{
    private readonly DataTable table;
    public InteractionFields(DataTable table) { this.table=table; }
    public int Count => table.Columns.Count;
    public InteractionField this[int i] => new InteractionField(table.Columns[i].ColumnName);
}
public sealed class InteractionField
{
    public InteractionField(string name) { Name=name; }
    public string Name { get; }
}
