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
