using System;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Threading.Tasks;

namespace OQSDrug
{
    // Only the electronic-chart source is switched here. OQSDrugData/PG writes are unchanged.
    internal sealed class DynamicsDataSource : IDisposable
    {
        private OleDbConnection connection;
        private OleDbCommand command;
        private DataTable table;
        public DbDataReader Reader { get; private set; }

        public static async Task<DynamicsDataSource> OpenReaderAsync(string path, string sql, bool rowLevelLocking = false)
        {
            var source = new DynamicsDataSource();
            try
            {
                if (Properties.Settings.Default.DynamicsUseCom)
                {
                    source.table = await DynamicsComReader.ReadAsync(sql).ConfigureAwait(false);
                    source.Reader = source.table.CreateDataReader();
                }
                else
                {
                    var builder = new OleDbConnectionStringBuilder
                    {
                        Provider = CommonFunctions.DBProvider,
                        DataSource = path
                    };
                    builder["Mode"] = "Read";
                    if (rowLevelLocking) builder["Jet OLEDB:Database Locking Mode"] = 1;
                    source.connection = new OleDbConnection(builder.ConnectionString);
                    await source.connection.OpenAsync().ConfigureAwait(false);
                    source.command = new OleDbCommand(sql, source.connection);
                    source.Reader = await source.command.ExecuteReaderAsync().ConfigureAwait(false);
                }
                return source;
            }
            catch
            {
                source.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            Reader?.Dispose();
            command?.Dispose();
            connection?.Dispose();
            table?.Dispose();
        }
    }
}
