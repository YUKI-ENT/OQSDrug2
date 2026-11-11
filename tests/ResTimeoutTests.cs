using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

internal partial class TimeoutTestHost
{
    internal readonly List<string> Logs = new List<string>();
    private Task AddLogAsync(string message) { Logs.Add(message); return Task.CompletedTask; }
}

internal static class CommonFunctions
{
    internal static bool DataDbLock, OleDb;
    internal static string ConvertSqlForOleDb(string sql)
    { return OleDb ? Regex.Replace(sql, @"@([A-Za-z_][A-Za-z0-9_]*)", "?") : sql; }
    internal static void AddDbParameter(IDbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter(); p.ParameterName = name; p.Value = value; cmd.Parameters.Add(p);
    }
}

internal sealed class TimeoutTestConnection : DbConnection
{
    internal int Attempts, Affected = 1;
    internal bool Fail;
    internal string Sql;
    internal readonly List<object> Values = new List<object>();
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
        private readonly TimeoutTestConnection owner;
        private readonly SqlCommand parameters = new SqlCommand();
        internal Command(TimeoutTestConnection owner) { this.owner = owner; }
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
        public override object ExecuteScalar() { throw new NotSupportedException(); }
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) { throw new NotSupportedException(); }
        public override int ExecuteNonQuery()
        {
            owner.Attempts++;
            owner.Sql = CommandText;
            owner.Values.Clear();
            foreach (SqlParameter p in parameters.Parameters) owner.Values.Add(p.Value);
            if (owner.Fail) throw new InvalidOperationException("Injected DB failure");
            return owner.Affected;
        }
    }
}

internal static class ResTimeoutTests
{
    private static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
    public static int Main()
    {
        string folder = Path.Combine(Path.GetTempPath(), "OQSDrug-timeout-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try { Run(folder).GetAwaiter().GetResult(); Console.WriteLine("PASS: timeout boundary, old/missing dates, arrival recheck, SQL guards, both DB parameter styles, failure retry"); return 0; }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { Directory.Delete(folder, true); }
    }
    private static async Task Run(string folder)
    {
        var now = new DateTime(2026, 11, 11, 11, 40, 11);
        var record = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        { ["ID"] = 42, ["reqFile"] = "req/test.xml", ["reqDate"] = now.AddHours(-24).AddTicks(1) };
        var host = new TimeoutTestHost();
        var db = new TimeoutTestConnection();
        Func<Task<bool>> expire = () => host.ExpireMissingResponseAsync(db, record, folder, "test", now);
        Assert(!await expire() && db.Attempts == 0, "Must wait the full 24 hours");
        record["reqDate"] = now.AddHours(1);
        Assert(!await expire(), "Future request must not expire");
        record["reqDate"] = DBNull.Value;
        Assert(!await expire(), "NULL date must not expire");
        record["reqDate"] = "invalid";
        Assert(!await expire(), "Invalid date must not expire");
        record.Remove("reqDate");
        Assert(!await expire(), "Missing date must not expire");
        record["reqDate"] = now.AddHours(-24);
        foreach (bool oleDb in new[] { false, true })
        {
            CommonFunctions.OleDb = oleDb;
            Assert(await expire(), "Exactly 24 hours must expire");
            Assert(db.Sql.Contains("resFile = ''") && db.Sql.Contains("resFile IS NULL"), "Must end pending status and preserve history");
            Assert(db.Sql.Contains(oleDb ? "ID = ? AND resFile IS NULL AND reqFile = ? AND reqDate = ?" :
                "ID = @ID AND resFile IS NULL AND reqFile = @reqFile AND reqDate = @reqDate"), "Concurrent replacement must be guarded");
            Assert(db.Values.Count == 5 && (DateTime)db.Values[0] == now &&
                (string)db.Values[1] == "未着タイムアウト（要求後24時間経過）" &&
                (int)db.Values[2] == 42 && (string)db.Values[3] == "req/test.xml" &&
                (DateTime)db.Values[4] == now.AddHours(-24), "Parameter order must support OleDb");
            Assert(!CommonFunctions.DataDbLock, "DB lock must release on success");
        }
        record["reqDate"] = now.AddDays(-30);
        Assert(await expire(), "Pre-existing old requests must expire");
        int attempts = db.Attempts;
        foreach (string ext in new[] { ".xml", ".pdf", ".XML" })
        {
            string file = Path.Combine(folder, "test" + ext);
            File.WriteAllText(file, "arrived after scan snapshot");
            Assert(!await expire() && db.Attempts == attempts && File.Exists(file), "Arrived response must be preserved for import");
            File.Delete(file);
        }
        db.Affected = 0;
        int logs = host.Logs.Count;
        Assert(!await expire() && host.Logs.Count == logs, "Concurrent update must not report a timeout");
        db.Fail = true;
        try { await expire(); throw new Exception("Expected DB failure"); }
        catch (InvalidOperationException) { }
        Assert(!CommonFunctions.DataDbLock && host.Logs.Count == logs, "Failure must release lock without confirming timeout");
        db.Fail = false; db.Affected = 1;
        Assert(await expire(), "Failed timeout must be retryable");
    }
}
