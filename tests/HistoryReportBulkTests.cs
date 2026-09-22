using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

public static class HistoryReportBulkTests
{
    static Assembly app;
    const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
    static object Get(object o, string name) { return o.GetType().GetField(name, Flags).GetValue(o); }
    static void Set(object o, string name, object value) { o.GetType().GetProperty(name, Flags).SetValue(o,value); }
    static object Call(object o, string name, params object[] args) { return o.GetType().GetMethod(name,Flags).Invoke(o,args); }
    static object New(string name, params object[] args) { return Activator.CreateInstance(app.GetType("OQSDrug."+name),BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,null,args,null); }
    [STAThread] public static void Main(string[] args)
    {
        try {
        app=Assembly.LoadFrom(args[0]); Directory.CreateDirectory(args[1]);
        Console.WriteLine("Reports"); TestReport(args[1]); Console.WriteLine("Integrated"); TestIntegratedViews(args[1]); Console.WriteLine("Bulk"); TestBulk(); Console.WriteLine("Face"); TestFace(args[1]);
        Console.WriteLine("PASS: history grouping/pagination/render, bulk selection/manual resend, consent tags and automatic duplicate suppression");
        } catch(Exception ex) { Console.WriteLine(ex.GetType().Name + ": " + ex.Message); Console.WriteLine(ex.StackTrace); if(ex.InnerException != null) Console.WriteLine(ex.InnerException.GetType().Name + ": " + ex.InnerException.Message); Environment.ExitCode=1; }
    }
    static DataTable Drugs()
    {
        var t=new DataTable();
        foreach(string key in new[]{"id","didate","prdate","metrmonth","metrdihcd","metrdihnm","prlshcd","prlshnm","inout","source","prisorg","diorg","drugn","ingren","usagen","qua1","unit","times","metridcl"}) t.Columns.Add(key);
        for(int i=0;i<55;i++)
        {
            var r=t.NewRow(); r["id"]=(i+1).ToString();r["didate"]=i<50?"20260922":"20260810";r["prdate"]=r["didate"];r["metrmonth"]="202609";
            r["metrdihcd"]="A";r["metrdihnm"]="テスト薬局";r["prlshcd"]="B";r["prlshnm"]="テスト診療所";r["inout"]="2";r["source"]="1";
            r["drugn"]="印字テスト錠「架空製薬」";r["ingren"]="架空の一般名";r["usagen"]="１日３回 朝昼夕食後";r["qua1"]="3";r["unit"]="錠";r["times"]="14";r["metridcl"]="21";t.Rows.Add(r);
        }
        return t;
    }
    static void TestReport(string output)
    {
        var table=Drugs();
        var type=app.GetType("OQSDrug.HistoryReport");
        var report=type.GetMethod("Drugs",Flags).Invoke(null,new object[]{table,"000001 : 印字確認用の架空患者","全期間・自施設を含む"});
        var rows=(IList)Get(report,"Rows");
        Require(rows.Count==57,"Source rows lost or wrong grouping");
        Require(rows.Cast<object>().Count(r=>(bool)Get(r,"Heading"))==2,"Dates mixed");
        Require(((string[])Get(rows[1],"Cells"))[0].Contains("用法"),"Usage lost");
        Require(((string[])Get(report,"Columns")).SequenceEqual(new[]{"薬剤名／一般名／用法","数量・日数等"}),"Unexpected category column");
        Require(rows.Cast<object>().Where(r=>!(bool)Get(r,"Heading")).All(r=>((string[])Get(r,"Cells")).Length==2),"Category still included in report rows");
        var other=table.NewRow();other.ItemArray=table.Rows[0].ItemArray.Clone() as object[];other["id"]="100";other["prlshcd"]="C";table.Rows.Add(other);
        var separate=type.GetMethod("Drugs",Flags).Invoke(null,new object[]{table,"架空患者","全期間"});
        Require(((IList)Get(separate,"Rows")).Cast<object>().Count(r=>(bool)Get(r,"Heading"))==3,"Different institutions merged");
        using(var renderer=(IDisposable)New("HistoryReportRenderer",report))
        using(var bitmap=new Bitmap(827,1169))
        using(var g=Graphics.FromImage(bitmap))
        {
            g.PageUnit=GraphicsUnit.Pixel;bitmap.SetResolution(100,100);
            bool more;int page=0;
            do { g.Clear(Color.White);more=(bool)Call(renderer,"DrawNext",g,new Rectangle(40,40,747,1089));if(page<2)bitmap.Save(Path.Combine(output,"history-page-"+page+".png"));page++;Require(page<20,"Pagination did not terminate"); } while(more);
            Require(page>1,"Expected multiple pages");
            Require(((IList)Get(renderer,"pages")).Cast<IList>().SelectMany(p=>p.Cast<object>()).Any(r=>!(bool)Get(r,"LastLine")),"Wrapped rows have no continuation metadata");
            Call(renderer,"Reset");g.Clear(Color.White);Call(renderer,"DrawNext",g,new Rectangle(40,40,747,1089));
            var pages=(IList)Get(renderer,"pages");
            Require(pages.Count==page,"Preview/print reset changes page count");
        }
        using(var document=new PrintDocument())
        using(var renderer=(IDisposable)New("HistoryReportRenderer",report))
        {
            var preview=new PreviewPrintController();document.PrintController=preview;
            document.DefaultPageSettings.PaperSize=new PaperSize("A4",827,1169);
            document.DefaultPageSettings.Margins=new Margins(40,40,40,40);
            document.BeginPrint+=(s,e)=>Call(renderer,"Reset");
            document.PrintPage+=(s,e)=>e.HasMorePages=(bool)Call(renderer,"DrawNext",e.Graphics,e.MarginBounds);
            document.Print(); // PreviewPrintController renders in memory; no printer output.
            var pages=preview.GetPreviewPageInfo();Require(pages.Length>1,"PrintDocument preview did not paginate");
            using(var bmp=new Bitmap(827,1169))using(var g=Graphics.FromImage(bmp))
            {g.Clear(Color.White);g.DrawImage(pages[0].Image,new Rectangle(0,0,827,1169));bmp.Save(Path.Combine(output,"printer-preview.png"));}
            foreach(var p in pages)p.Image.Dispose();
        }
        using(var panel=(Control)New("HistoryReportView"))
        using(var form=new Form{Width=1000,Height=650,ShowInTaskbar=false,Location=new Point(-2000,-2000),StartPosition=FormStartPosition.Manual})
        {
            form.Controls.Add(panel);Call(panel,"SetReport",report);form.Show();Application.DoEvents();
            Require((bool)GetProperty(panel,"CanPrint"),"Print disabled with data");
            using(var bmp=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bmp,new Rectangle(Point.Empty,form.Size));bmp.Save(Path.Combine(output,"history-screen.png"));}
            Call(panel,"ClearReport","読み込み中");Require(!(bool)GetProperty(panel,"CanPrint"),"Stale patient printable");
        }
        string heading=((string[])Get(rows[0],"Cells"))[0];
        Require(heading.IndexOf("処方元") < heading.IndexOf("処方日") && heading.IndexOf("処方日") < heading.IndexOf("調剤薬局") && heading.IndexOf("調剤薬局") < heading.IndexOf("情報源"),"Heading order wrong");
        Require(((string)Get(report,"Patient")).Contains("ID：000001") && ((string)Get(report,"Patient")).Contains("架空患者"),"Patient metadata missing");
        Require((string)Get(report,"Scope")=="出力期間：2026/08/10 ～ 2026/09/22","Output range incorrect");
        Require((string)Get(report,"Note")=="","Unwanted disclaimer");
        TestPivot(output);
    }
    static void TestPivot(string output)
    {
        foreach(bool drug in new[]{true,false})
        using(var grid=new DataGridView { AllowUserToAddRows=false })
        {
            string[] fixedNames=drug?new[]{"hospital","drugn","dose"}:new[]{"itemname","unit"};
            foreach(string name in fixedNames)grid.Columns.Add(name,name=="hospital"?"処方元":name=="drugn"?"薬剤名":name=="dose"?"用法":name=="itemname"?"項目名":"単位");
            for(int i=0;i<13;i++)grid.Columns.Add("date"+i,"2026/09/"+(22-i));
            object[] cells=new object[grid.Columns.Count];cells[0]=drug?"テスト診療所":"長い健診項目名";cells[1]=drug?"架空薬剤名":"cm";
            if(drug)cells[2]="1日3回食後";
            for(int i=fixedNames.Length;i<cells.Length;i++)cells[i]="値"+(i-fixedNames.Length);
            grid.Rows.Add(cells);
            if (drug)
            {
                var repeated=(object[])cells.Clone(); repeated[0]=""; repeated[2]="1日3回 朝昼夕食後、服用方法についての長い説明をここに記載する折り返し確認用の用法文です。さらに続く説明です。";
                grid.Rows.Add(repeated);
            }
            var reports=(IList)app.GetType("OQSDrug.HistoryReport").GetMethod("Pivot",Flags).Invoke(null,new object[]{drug?"処方歴":"健診結果",fixedNames.Length,drug?6:4,new[]{grid}});
            app.GetType("OQSDrug.HistoryReport").GetMethod("WithMetadata",Flags).Invoke(null,new object[]{reports,"患者名：印字用架空患者　ID：000001","出力期間：2026/09/10 ～ 2026/09/22"});
            Require(reports.Cast<object>().All(r=>((string)Get(r,"Patient")).Contains("000001") && ((string)Get(r,"Scope")).Contains("2026/09/10")),"Metadata missing from column bands");
            Require(reports.Count==(drug?3:4),"Horizontal bands incorrect");
            int dateColumns=0;
            foreach(var report in reports)dateColumns+=((string[])Get(report,"Columns")).Length-fixedNames.Length;
            Require(dateColumns==13,"Dates lost/duplicated");
            if(drug)
            {
                Require(((string[])Get(((IList)Get(reports[0],"Rows"))[1],"Cells"))[0]=="","Repeated institution filled down");
                Require(((float[])Get(reports[0],"Widths"))[2] >= .28f,"Dosage column too narrow");
            }
            var captured=reports.Cast<object>().SelectMany(r=>((string[])Get(((IList)Get(r,"Rows"))[0],"Cells")).Skip(fixedNames.Length)).ToArray();
            Require(captured.SequenceEqual(Enumerable.Range(0,13).Select(i=>"値"+i)),"Pivot values reordered or lost");
            using(var renderer=(IDisposable)New("HistoryReportPrintJob",reports))
            using(var bitmap=new Bitmap(drug?1169:827,drug?827:1169))using(var g=Graphics.FromImage(bitmap))
            {
                bitmap.SetResolution(100,100);int page=0;bool more;
                do {g.Clear(Color.White);more=(bool)Call(renderer,"DrawNext",g,new Rectangle(40,40,bitmap.Width-80,bitmap.Height-80));
                    bitmap.Save(Path.Combine(output,(drug?"pivot-drug-":"pivot-health-")+page+".png"));page++;Require(page<20,"Pivot pagination loop");}while(more);
                Require(page==reports.Count,"Band pages missing");
            }
            // Long results still flow over vertical pages without truncation.
            grid.Rows[0].Cells[fixedNames.Length].Value=new string('あ',8000);
            reports=(IList)app.GetType("OQSDrug.HistoryReport").GetMethod("Pivot",Flags).Invoke(null,new object[]{"長文",fixedNames.Length,4,new[]{grid}});
            using(var renderer=(IDisposable)New("HistoryReportRenderer",reports[0]))using(var bitmap=new Bitmap(827,1169))using(var g=Graphics.FromImage(bitmap))
            {
                bitmap.SetResolution(100,100);int page=0;bool more;
                do{more=(bool)Call(renderer,"DrawNext",g,new Rectangle(40,40,747,1089));page++;Require(page<100,"Long row loop");}while(more);
                Require(((IList)Get(renderer,"columnHeaders")).Count==1,"Date headings wrapped");
                int count=0;foreach(IList p in (IList)Get(renderer,"pages"))foreach(object r in p)count+=((string[])Get(r,"Cells"))[fixedNames.Length].Count(c=>c=='あ');
                Require(count==8000,"Long pivot value truncated");
            }
        }
    }
    static void TestIntegratedViews(string output)
    {
        using(var main=(Form)New("Form1"))using(var drug=(Form)New("FormDI",main))using(var checkup=(Form)New("FormTKK",main))
        {
            var tabs=(TabControl)Get(drug,"tabControl1");Require(tabs.TabPages[1].Text=="処方歴原文","Prescription tab misplaced");
            Require(((ToolStripButton)Get(drug,"toolStripButtonSGMLDI")).Available,"SGML button hidden");
            Require(!checkup.Controls.OfType<TabControl>().Any(),"Unexpected checkup tab");
            Require(((Control)Get(checkup,"dataGridViewTKK")).Parent==checkup,"Original health view not preserved");
            Require(!((ToolStripButton)Get(checkup,"checkupPrint")).Enabled,"Print active before loading");
            Require(!((ToolStripButton)Get(drug,"pivotPrint")).Enabled,"Drug print active before loading");
            Call(checkup,"toolStripComboBoxPt_SelectedIndexChanged",null,EventArgs.Empty);
            Require(!((ToolStripButton)Get(checkup,"checkupPrint")).Enabled,"Blank patient printable");
            var report=app.GetType("OQSDrug.HistoryReport").GetMethod("Drugs",Flags).Invoke(null,new object[]{Drugs(),"架空患者","全期間"});
            Call(Get(drug,"prescriptionReport"),"SetReport",report);
            Require(!((Control)Get(Get(drug,"prescriptionReport"),"info")).Visible,"Metadata panel remains");
            Require(!((Control)Get(drug,"prescriptionReport")).Controls.OfType<ToolStrip>().Any(),"Tab-local print toolbar remains");
            var print=(ToolStripButton)Get(drug,"pivotPrint");
            Require(print.DisplayStyle==ToolStripItemDisplayStyle.Image && print.Image!=null,"Print icon missing");
            var color=(ToolStripButton)Get(drug,"toolStripButtonClass");
            Require(color.DisplayStyle==ToolStripItemDisplayStyle.Image && color.Image!=null && color.CheckOnClick,"Color icon toggle broken");
            tabs.SelectedIndex=1;Call(drug,"UpdateHistoryPrintState");Require(print.Enabled,"Original print disabled");
            tabs.SelectedIndex=2;Call(drug,"UpdateHistoryPrintState");Require(!print.Enabled,"Unprintable tab enabled");
            tabs.SelectedIndex=0;Call(drug,"UpdateHistoryPrintState");Require(!print.Enabled,"Unloaded pivot printable");
            drug.GetType().GetField("pivotPrintReady",Flags).SetValue(drug,true);
            Call(drug,"UpdateHistoryPrintState");Require(print.Enabled,"Loaded pivot print disabled");
            using(var host=new Form{Width=1000,Height=650,ShowInTaskbar=false,Location=new Point(-2000,-2000),StartPosition=FormStartPosition.Manual})
            {
                host.Controls.Add(tabs);host.Controls.Add((Control)Get(drug,"toolStrip1"));tabs.SelectedIndex=1;host.Show();Application.DoEvents();
                using(var bmp=new Bitmap(host.Width,host.Height)){host.DrawToBitmap(bmp,new Rectangle(Point.Empty,host.Size));bmp.Save(Path.Combine(output,"prescription-tab.png"));}
            }
        }
    }
    static Func<IReadOnlyList<T>,Task<TResult>> Sender<T,TResult>() {return records=>Task.FromResult(Activator.CreateInstance<TResult>());}
    static void TestBulk()
    {
        using(var form=(Form)New("FormBulkExecutionStatus"))
        {
            form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-2000,-2000);
            var record=New("ImportedQualificationRecord");Set(record,"IsSent",true);Set(record,"IsDuplicate",true);
            var rowType=form.GetType().GetNestedType("BulkExecutionResultRow",BindingFlags.NonPublic);
            var row=Activator.CreateInstance(rowType,Flags,null,new[]{record},null);
            var list=(IList)Get(form,"rows");list.Add(row);
            var sender=typeof(HistoryReportBulkTests).GetMethod("Sender",Flags).MakeGenericMethod(record.GetType(),app.GetType("OQSDrug.QualificationSendSummary")).Invoke(null,null);
            form.GetType().GetField("sendSelectedAsync",Flags).SetValue(form,sender);
            form.Show();Application.DoEvents();
            var grid=(DataGridView)Get(form,"dgvResults");Require(!grid.Columns[0].ReadOnly,"Selection column readonly");
            grid.Rows[0].Cells[0].Value=true;grid.EndEdit();Application.DoEvents();
            Require((bool)rowType.GetProperty("Send").GetValue(row),"Grid selection not committed");
            Require(((Button)Get(form,"buttonSendSelected")).Enabled,"Manual resend not enabled");
            Call(form,"buttonClearChecks_Click",null,EventArgs.Empty);
            Require(!((Button)Get(form,"buttonSendSelected")).Enabled,"Cleared send enabled");
            Call(form,"buttonCheckAll_Click",null,EventArgs.Empty);
            Require((bool)grid.Rows[0].Cells[0].Value,"Check-all did not include sent/duplicate row");
            form.GetType().GetField("sendingSelected",Flags).SetValue(form,true);
            Set(row,"Send",false);Set(row,"Send",true);
            Require(!((Button)Get(form,"buttonSendSelected")).Enabled,"Selection enabled concurrent send");
        }
    }
    static void TestFace(string output)
    {
        var record=New("ImportedQualificationRecord");
        var values=(IDictionary)GetProperty(record,"BulkToolValues");
        values["MIC"]="0000000000";values["PICF"]="0";values["PICT"]="20260922090000";values["PIAT"]="20260923235959";
        values["SHCICF"]="1";values["SHCICT"]="20260922090000";values["SHCIAT"]="20260923235959";
        values["DICF"]="1";values["DIAT"]="20260923235959";values["OICF"]="0";values["OIAT"]="20260923235959";
        var temp=Path.Combine(output,"face-test-"+Guid.NewGuid().ToString("N"));
        var exporter=New("QualificationFaceExporter",temp,null);
        var xml=new System.Text.StringBuilder();
        using(var writer=XmlWriter.Create(xml,new XmlWriterSettings{OmitXmlDeclaration=true}))Call(exporter,"WriteQualificationResult",writer,record);
        var doc=new XmlDocument();doc.LoadXml(xml.ToString());
        Require(doc.SelectSingleNode("//PharmacistsInfoConsFlg").InnerText=="0","Refusal changed/lost");
        Require(doc.SelectSingleNode("//SpecificHealthCheckupsInfoConsFlg").InnerText=="1","Health consent lost");
        foreach(var prefix in new[]{"SpecificHealthCheckups","Pharmacists","Diagnosis","Operation"})
            Require(doc.SelectSingleNode("//"+prefix+"InfoAvailableTime").InnerText=="20260923235959","Availability tag wrong");
        Require(!xml.ToString().Contains("Pharmaceutical")&&!xml.ToString().Contains("AcquisitionTime"),"Legacy incorrect tags remain");
        var blank=New("ImportedQualificationRecord");xml.Clear();
        using(var writer=XmlWriter.Create(xml,new XmlWriterSettings{OmitXmlDeclaration=true}))Call(exporter,"WriteQualificationResult",writer,blank);
        Require(!xml.ToString().Contains("PharmacistsInfoConsFlg"),"Missing consent fabricated");
        Set(record,"IsSent",true);Set(record,"IsDuplicate",true);
        var records=Array.CreateInstance(record.GetType(),1);records.SetValue(record,0);
        ((Task)Call(exporter,"ExportAsync",records,false)).GetAwaiter().GetResult();
        Require(Directory.GetFiles(temp,"*.xml",SearchOption.AllDirectories).Length==0,"Automatic duplicate resent");
        ((Task)Call(exporter,"ExportAsync",records,true)).GetAwaiter().GetResult();
        Require(Directory.GetFiles(temp,"*.xml",SearchOption.AllDirectories).Length==1,"Explicit resend skipped");
        Require(!(bool)GetProperty(record,"IsDuplicate"),"Successful resend still shown as skipped");
        ((Task)Call(exporter,"ExportAsync",records,true)).GetAwaiter().GetResult();
        Require(Directory.GetFiles(temp,"*.xml",SearchOption.AllDirectories).Length==2,"Manual resend overwrote previous file");
    }
    static object GetProperty(object o,string name){return o.GetType().GetProperty(name,Flags).GetValue(o);}
}
