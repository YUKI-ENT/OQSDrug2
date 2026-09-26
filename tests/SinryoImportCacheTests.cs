using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Xml;
using OQSDrug;

// The runner compiles the actual ProcessSinryoInfoAsync method from Form1.cs.
internal partial class SinryoImportTestHost
{
    private string GetNodeValue(XmlNode node, string path) { return node.SelectSingleNode(path)?.InnerText ?? ""; }
    private float NzConvert(string value) { float result; return float.TryParse(value, out result) ? result : 0; }
    private Task AddLogAsync(string message) { return Task.CompletedTask; }
}

internal static class CommonFunctions
{
    internal static string ConvertSqlForOleDb(string sql) { return sql; }
    internal static void AddDbParameter(IDbCommand cmd, string name, object value)
    {
        var parameter = cmd.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        cmd.Parameters.Add(parameter);
    }
}

internal sealed class ImportTestConnection : DbConnection
{
    internal int Checks, Inserts, Attempts, FailAttempt;
    internal int Existing;
    public override string ConnectionString { get; set; }
    public override string Database { get { return "test"; } }
    public override string DataSource { get { return "test"; } }
    public override string ServerVersion { get { return "test"; } }
    public override ConnectionState State { get { return ConnectionState.Open; } }
    public override void ChangeDatabase(string name) { }
    public override void Open() { }
    public override void Close() { }
    protected override DbTransaction BeginDbTransaction(IsolationLevel level) { throw new NotSupportedException(); }
    protected override DbCommand CreateDbCommand() { return new Command(this); }

    private sealed class Command : DbCommand
    {
        private readonly ImportTestConnection owner;
        private readonly SqlCommand parameters = new SqlCommand();
        internal Command(ImportTestConnection owner) { this.owner = owner; }
        public override string CommandText { get; set; }
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection DbConnection { get; set; }
        protected override DbTransaction DbTransaction { get; set; }
        protected override DbParameterCollection DbParameterCollection { get { return parameters.Parameters; } }
        protected override DbParameter CreateDbParameter() { return new SqlParameter(); }
        public override void Cancel() { }
        public override void Prepare() { }
        public override object ExecuteScalar() { owner.Checks++; return owner.Existing; }
        public override int ExecuteNonQuery()
        {
            owner.Attempts++;
            if (owner.Attempts == owner.FailAttempt) throw new InvalidOperationException("Injected insert failure");
            owner.Inserts++;
            return 1;
        }
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) { throw new NotSupportedException(); }
    }
}

internal static class SinryoImportCacheTests
{
    private static void Assert(bool value, string message) { if (!value) throw new Exception(message); }
    public static int Main()
    {
        try { Run().GetAwaiter().GetResult(); Console.WriteLine("PASS: actual diagnosis import cache, detail groups, failure visibility and uncached fallback"); return 0; }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static async Task Run()
    {
        var xml = new XmlDocument();
        xml.LoadXml("<MeTrInfs><MeTrInf><SinInfCd>A</SinInfCd></MeTrInf><MeTrInf><SinInfCd>B</SinInfCd></MeTrInf></MeTrInfs>");
        var patient = new { Id = 10L, Idmain = 1L, Name = "test", Kana = "test", Birth = "20000101", Sex = 1,
            MeTrDiHCd = "0010000001", MeTrDiHNm = "test", MeTrMonth = "202609", DiDate = "20260920" };
        var host = new SinryoImportTestHost();
        using (var timing = new ResImportTiming("test", _ => { }))
        {
            var db = new ImportTestConnection();
            var dates = new HashSet<string>(StringComparer.Ordinal) { patient.DiDate };
            Assert(await host.ProcessSinryoInfoAsync(db, null, xml.DocumentElement, patient, timing, dates) == 0 &&
                db.Checks == 0 && db.Inserts == 0, "Existing date must skip without SQL");
            dates.Clear();
            Assert(await host.ProcessSinryoInfoAsync(db, null, xml.DocumentElement, patient, timing, dates) == 2 &&
                dates.Contains(patient.DiDate) && db.Checks == 0, "Same-day detail group must be inserted in full");
            Assert(await host.ProcessSinryoInfoAsync(db, null, xml.DocumentElement, patient, timing, dates) == 0 &&
                db.Inserts == 2, "Next group must see the newly inserted date");

            dates.Clear();
            db = new ImportTestConnection { FailAttempt = 2 };
            Assert(await host.ProcessSinryoInfoAsync(db, null, xml.DocumentElement, patient, timing, dates) == 1 &&
                dates.Contains(patient.DiDate), "Partial successful insertion must remain visible");
            dates.Clear();
            db = new ImportTestConnection { FailAttempt = 1 };
            Assert(await host.ProcessSinryoInfoAsync(db, null, xml.DocumentElement, patient, timing, dates) == 0 &&
                !dates.Contains(patient.DiDate), "Failed first insertion must not mark date as registered");

            db = new ImportTestConnection { Existing = 1 };
            Assert(await host.ProcessSinryoInfoAsync(db, null, xml.DocumentElement, patient, timing) == 0 &&
                db.Checks == 1 && db.Inserts == 0, "Uncached Access path must retain its SQL check");
        }
    }
}
