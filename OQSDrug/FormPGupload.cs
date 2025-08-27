using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using static OQSDrug.CommonFunctions;

namespace OQSDrug
{
    public partial class FormPGupload : Form
    {
        string pgConnStr = $"Host={Properties.Settings.Default.PGaddress};" +
                               $"Port={Properties.Settings.Default.PGport};" +
                               $"Username={Properties.Settings.Default.PGuser};" +
                               $"Password={decodePassword(Properties.Settings.Default.PGpass)};" +
                               $"Database={CommonFunctions.PGdatabaseName}";

        string adminConnStr = $"Host={Properties.Settings.Default.PGaddress};" +
                              $"Port={Properties.Settings.Default.PGport};" +
                              $"Username={Properties.Settings.Default.PGuser};" +
                              $"Password={decodePassword(Properties.Settings.Default.PGpass)};" +
                              "Database=postgres";
        bool dbExists = false;
        // フォームのフィールドにキャンセルトークンを保持
        private CancellationTokenSource _cts;
        private bool _isRunning = false;

        public FormPGupload()
        {
            InitializeComponent();
            this.Load += async (s, e) => await CheckPostgreSQLConnectionAsync();
        }

        private async Task CheckPostgreSQLConnectionAsync()
        {
            try
            {
                string result = await CommonFunctions.CheckPGStatusAsync(CommonFunctions.PGdatabaseName);

                if (result == "OK")
                {

                    textBoxStatus.Text = "✅ PostgreSQLに接続成功。データベース存在確認済み。";
                    labelServer.ForeColor = Color.White;
                    labelServer.BackColor = Color.LightGreen;
                    labelDB.ForeColor = Color.White;
                    labelDB.BackColor = Color.LightGreen;
                    labelServer.Text = "PosegreSQL server Connected";
                    labelDB.Text = CommonFunctions.PGdatabaseName + ":Ready";

                    buttonMigrate.Enabled = true;
                    buttonCreate.Enabled = true;

                    dbExists = true;
                }
                else if (result == "No database")
                {

                    textBoxStatus.Text = $"⚠️ サーバー接続成功、しかしデータベース {CommonFunctions.PGdatabaseName} が存在しません。";
                    labelServer.ForeColor = Color.White;
                    labelServer.BackColor = Color.LightGreen;
                    labelDB.ForeColor = Color.Gray;
                    labelDB.BackColor = Color.LightGray;

                    labelServer.Text = "PosegreSQL server Connected";
                    labelDB.Text = CommonFunctions.PGdatabaseName + " not found";

                    buttonMigrate.Enabled = true;
                    buttonCreate.Enabled = true;
                    dbExists = false;
                }
                else
                {

                    textBoxStatus.Text = "❌ サーバー接続失敗:\r\n";
                    labelServer.ForeColor = Color.Gray;
                    labelServer.BackColor = Color.LightGray;
                    labelDB.ForeColor = Color.Gray;
                    labelDB.BackColor = Color.LightGray;

                    labelServer.Text = "PosegreSQL server not found";
                    labelDB.Text = CommonFunctions.PGdatabaseName + " not found";

                    buttonMigrate.Enabled = false;
                    buttonCreate.Enabled = false;
                    dbExists = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async void buttonMigrate_Click(object sender, EventArgs e)
        {
            if (!_isRunning)
            {
                if (MessageBox.Show(
                    "データ移行を開始します。\n既存のテーブルは削除され、新規作成されます。\nよろしいですか？",
                    "データ移行開始確認",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    return;
                }

                _isRunning = true;
                buttonMigrate.Text = "中止";
                _cts = new CancellationTokenSource();

                // mdb設定
                string connectionMDB = $"Provider={CommonFunctions.DBProvider};Data Source={textBoxMDB.Text};Mode=Read;";

                try
                {
                    // まずテーブル作成
                    await CreateOrRecreatePgDatabaseAndTablesAsync(_cts.Token);

                    // 次にデータ移行
                    await MigrateDataAsync(_cts.Token, connectionMDB);
                }
                finally
                {
                    _isRunning = false;
                    buttonMigrate.Text = "データ移行開始";
                }
            }
            else
            {
                if (MessageBox.Show(
                    "データ移行処理を中止しますか？",
                    "処理中止確認",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    _cts.Cancel();
                }
            }
        }

        // データベース作成 & テーブル作成
        public async Task CreateOrRecreatePgDatabaseAndTablesAsync(CancellationToken ct)
        {
            try
            {
                AddLog("PostgreSQLデータベース作成処理開始");

                if (!dbExists)
                {
                    AddLog("データベースが存在しません。新規作成します。");
                    await CreatePgDatabaseAsync(CommonFunctions.PGdatabaseName, ct);
                }
                else
                {
                    AddLog("データベースは既に存在します。");
                }

                string sqlFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "02_create_oqsdrug_data.sql");
                if (!File.Exists(sqlFilePath))
                {
                    AddLog("SQLファイルが見つかりません: " + sqlFilePath);
                    return;
                }

                string sql = await ReadFileTextAsync(sqlFilePath, ct);

                ct.ThrowIfCancellationRequested();

                AddLog("既存テーブルを削除し、テーブルを作成し直します");

                ct.ThrowIfCancellationRequested();
                await ExecuteSqlAsync(sql, ct);

                AddLog("PostgreSQLデータベース作成処理完了");
            }
            catch (OperationCanceledException)
            {
                AddLog("処理がキャンセルされました。");
            }
            catch (Exception ex)
            {
                AddLog("データベース作成処理中にエラー: " + ex.Message);
            }
        }

        private async Task<string> ReadFileTextAsync(string path, CancellationToken ct)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
            using (var sr = new StreamReader(fs))
            {
                var sb = new StringBuilder();
                char[] buffer = new char[4096];
                int read;
                while ((read = await sr.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    ct.ThrowIfCancellationRequested();
                    sb.Append(buffer, 0, read);
                }
                return sb.ToString();
            }
        }

        // ★ 移行処理（進捗付き）
        private async Task MigrateDataAsync(CancellationToken ct, string connection)
        {
            using (var mdbConn = new OleDbConnection(connection))
            using (var pgConn = new NpgsqlConnection(pgConnStr))
            {
                await mdbConn.OpenAsync();
                await pgConn.OpenAsync();

                DataTable tables = mdbConn.GetSchema("Tables");

                foreach (DataRow row in tables.Rows)
                {
                    string tableName = row["TABLE_NAME"].ToString();
                    if (tableName.StartsWith("MSys")) continue;

                    string tableNameLower = tableName.ToLower();

                    try
                    {
                        AddLog($"[テーブル: {tableName}] 移行開始...");

                        // --- レコード数取得 ---
                        int totalRecords;
                        using (var countCmd = new OleDbCommand($"SELECT COUNT(*) FROM [{tableName}]", mdbConn))
                        {
                            totalRecords = (int)countCmd.ExecuteScalar();
                        }

                        // PostgreSQL 側のカラムリスト取得
                        var pgColumns = new List<string>();
                        using (var pgColCmd = new NpgsqlCommand(
                            "SELECT column_name FROM information_schema.columns WHERE table_schema='public' AND table_name=@tbl",
                            pgConn))
                        {
                            pgColCmd.Parameters.AddWithValue("tbl", tableNameLower);
                            using (var pgReader = await pgColCmd.ExecuteReaderAsync())
                            {
                                while (await pgReader.ReadAsync())
                                {
                                    pgColumns.Add(pgReader.GetString(0).ToLower());
                                }
                            }
                        }

                        // settings テーブルでカラム不一致ならスキップ
                        if (tableNameLower == "settings")
                        {
                            var mdbColumns = new List<string>();
                            using (var schemaCmd = new OleDbCommand($"SELECT * FROM [{tableName}] WHERE 1=0", mdbConn))
                            using (var reader = await schemaCmd.ExecuteReaderAsync())
                            {
                                var schema = reader.GetSchemaTable();
                                foreach (DataRow colRow in schema.Rows)
                                {
                                    mdbColumns.Add(colRow["ColumnName"].ToString().ToLower());
                                }
                            }

                            // ID カラムは除外して比較
                            var pgCompareCols = pgColumns.Where(c => c != "id").OrderBy(c => c).ToList();
                            var mdbCompareCols = mdbColumns.Where(c => c != "id").OrderBy(c => c).ToList();

                            if (!pgCompareCols.SequenceEqual(mdbCompareCols))
                            {
                                AddLog($"[SKIP] settings テーブルのカラム構成が異なるためスキップします。");
                                continue;
                            }
                        }

                        // --- データ読み込み ---
                        using (var mdbCmd = new OleDbCommand($"SELECT * FROM [{tableName}]", mdbConn))
                        using (var reader = await mdbCmd.ExecuteReaderAsync())
                        {
                            var insertColumns = new List<string>();
                            var insertParamIndexes = new List<int>();

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                string name = reader.GetName(i).ToLower();
                                if (name.Equals("id", StringComparison.OrdinalIgnoreCase)) continue;
                                if (!pgColumns.Contains(name)) continue; // PG に存在しないカラムはスキップ

                                insertColumns.Add($"\"{name}\"");
                                insertParamIndexes.Add(i);
                            }

                            if (insertColumns.Count == 0)
                            {
                                AddLog($"[WARN] {tableNameLower} に一致するカラムがないためスキップします。");
                                continue;
                            }

                            int batchSize = 100; // まとめる件数
                            int recordCount = 0;

                            while (true)
                            {
                                if (ct.IsCancellationRequested) throw new OperationCanceledException();

                                var sb = new StringBuilder();
                                var parameters = new List<NpgsqlParameter>();
                                int batchCount = 0;

                                sb.Append($"INSERT INTO \"{tableNameLower}\" ({string.Join(", ", insertColumns)}) VALUES ");

                                while (batchCount < batchSize && await reader.ReadAsync())
                                {
                                    var valuePlaceholders = new List<string>();

                                    for (int pIndex = 0; pIndex < insertParamIndexes.Count; pIndex++)
                                    {
                                        int colIndex = insertParamIndexes[pIndex];
                                        string paramName = $"@p{recordCount}_{pIndex}";
                                        valuePlaceholders.Add(paramName);

                                        object value = reader.IsDBNull(colIndex) ? DBNull.Value : reader.GetValue(colIndex);
                                        parameters.Add(new NpgsqlParameter(paramName, value));
                                    }

                                    sb.Append("(" + string.Join(", ", valuePlaceholders) + ")");
                                    batchCount++;
                                    recordCount++;

                                    if (batchCount < batchSize && recordCount < totalRecords)
                                        sb.Append(", ");
                                }

                                if (batchCount > 0)
                                {
                                    using (var insertCmd = new NpgsqlCommand(sb.ToString(), pgConn))
                                    {
                                        insertCmd.Parameters.AddRange(parameters.ToArray());
                                        await insertCmd.ExecuteNonQueryAsync();
                                    }

                                    AddLog($"[{tableNameLower}]{recordCount}/{totalRecords} 件移行済み");
                                }

                                if (recordCount >= totalRecords) break;
                            }
                        }

                        AddLog($"[OK] {tableNameLower} 完了");
                    }
                    catch (OperationCanceledException)
                    {
                        AddLog($"[CANCELLED] {tableNameLower} の移行が中止されました。");
                        break;
                    }
                    catch (Exception ex)
                    {
                        AddLog($"[ERROR] {tableNameLower} の移行中にエラー: {ex.Message}");
                        // 続行して次のテーブルへ
                    }
                }

                // --- 移行後に DBversion を設定 ---
                try
                {
                    // PostgreSQLなので isPg = true
                    await CommonFunctions.SetDbVersionAsync(pgConn, true, CommonFunctions.DBversion);
                    AddLog($"DBversion を {CommonFunctions.DBversion} に設定しました。");
                }
                catch (Exception ex)
                {
                    AddLog($"DBversion 設定時にエラー: {ex.Message}");
                }

                AddLog("全テーブルの移行が完了しました");
            }
        }



        private void FormPGupload_Load(object sender, EventArgs e)
        {
            textBoxMDB.Text = Properties.Settings.Default.OQSDrugData;
        }

        private void buttonSelectMDB_Click(object sender, EventArgs e)
        {
            // ファイル選択ダイアログの設定
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "MDB files (*.mdb)|*.mdb|All files (*.*)|*.*";
            openFileDialog.Title = "OQSDrug_data.mdbを選択してください";

            // ダイアログを表示して結果を確認
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // 選択したファイルパスをテキストボックスに設定
                textBoxMDB.Text = openFileDialog.FileName;
            }
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            this.Close();   
        }

        

        private async void buttonCreate_Click(object sender, EventArgs e)
        {
            if (!_isRunning)
            {
                if (MessageBox.Show(
                    "既存のテーブルは削除され、新規に作成されます。\n続行しますか？",
                    "データベース初期化確認",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    return;
                }

                _isRunning = true;
                buttonCreate.Text = "中止";
                _cts = new CancellationTokenSource();

                try
                {
                    await CreateOrRecreatePgDatabaseAndTablesAsync(_cts.Token);
                }
                finally
                {
                    _isRunning = false;
                    buttonCreate.Text = "新規作成";
                }
            }
            else
            {
                _cts.Cancel();
            }
        }

        private async Task CreatePgDatabaseAsync(string dbName, CancellationToken ct)
        {
            using (var conn = new NpgsqlConnection(adminConnStr))
            {
                await conn.OpenAsync(ct);
                using (var cmd = new NpgsqlCommand($"CREATE DATABASE \"{dbName}\"", conn))
                {
                    await cmd.ExecuteNonQueryAsync(ct);
                }
            }
        }

        private async Task ExecuteSqlAsync(string sql, CancellationToken ct)
        {
            using (var conn = new NpgsqlConnection(pgConnStr))
            {
                await conn.OpenAsync(ct);
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    await cmd.ExecuteNonQueryAsync(ct);
                }
            }
        }

        private void AddLog(string message)
        {
            textBoxStatus.AppendText(message + "\r\n");
            textBoxStatus.Refresh();
        }

        private async void buttonImportSGML_Click(object sender, EventArgs e)
        {
            await RestoreFromBackupAsync();
        }

        // ▼ メイン：ダイアログでファイル選択→pg_restore実行
        private async Task RestoreFromBackupAsync()
        {
            AddLog("薬剤情報データのインポートを開始します");

            try
            {
                using (var ofd = new OpenFileDialog())
                {
                    ofd.Title = "pg_dump カスタム形式（.backup）を選択";
                    ofd.Filter = "PostgreSQL backup (*.backup;*.dump;*.custom)|*.backup;*.dump;*.custom|All files (*.*)|*.*";
                    ofd.Multiselect = false;

                    if (ofd.ShowDialog(this) != DialogResult.OK) return;

                    string backupPath = ofd.FileName;
                    if (!File.Exists(backupPath))
                    {
                        AddLog($"[Restore] ファイルが見つかりません: {backupPath}");
                        return;
                    }

                    // 接続情報（必要なら設定から取得）
                    string host = Properties.Settings.Default.PGaddress;
                    int port = Properties.Settings.Default.PGport;
                    string user = Properties.Settings.Default.PGuser;
                    string db = CommonFunctions.PGdatabaseName;
                    string password = decodePassword(Properties.Settings.Default.PGpass); // ← 保管場所は任意（空でも実行は可）

                    // 実行（キャンセルしたい場合はフォームに CancellationTokenSource を持たせて渡す）
                    using (var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10))) // 10分上限例
                    {
                        bool ok = await RunPgRestoreAsync(
                            backupPath: backupPath,
                            host: host, port: port, user: user, database: db, password: password,
                            cancel: cts.Token);

                        AddLog(ok ? "[Restore] 完了" : "[Restore] 失敗");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                AddLog(ex.ToString());
            }
        }

        // ▼ 実体：pg_restore を起動して出力をログへ反映
        private async Task<bool> RunPgRestoreAsync(
            string backupPath,
            string host, int port, string user, string database, string password,
            CancellationToken cancel)
        {
            try
            {
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                string pgRestorePath = Path.Combine(exeDir, "pqsgl", "bin", "pg_restore.exe"); // ← 配置パスに合わせて修正
                if (!File.Exists(pgRestorePath))
                {
                    AddLog($"[Restore] pg_restore が見つかりません: {pgRestorePath}");
                    return false;
                }

                // 対象テーブル（バックアップに既に絞ってあっても -t を付けると安全）
                string[] tables = { "sgml_rawdata", "sgml_interaction", "ai_prompt_tpl" };

                // できるだけ“今のDB/権限”に合わせる設定を推奨
                // --clean --if-exists : 既存オブジェクトを落としてから作成
                // --no-owner --no-privileges : 所有者/権限は復元しない（環境差異での失敗を避ける）
                // --jobs=2 : 並列化（CPU/IOに合わせて調整）
                var sb = new StringBuilder();
                sb.AppendFormat("-h \"{0}\" -p {1} -U \"{2}\" -d \"{3}\" ", host, port, user, database);
                sb.Append("--clean --if-exists --no-owner --no-privileges --verbose --jobs=2 ");
                foreach (var t in tables)
                {
                    sb.AppendFormat("-t \"{0}\" ", t);
                }
                sb.AppendFormat("\"{0}\"", backupPath); // 最後にバックアップファイル

                var psi = new ProcessStartInfo
                {
                    FileName = pgRestorePath,
                    Arguments = sb.ToString(),
                    UseShellExecute = false,            // 必須：リダイレクトのため
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(pgRestorePath) ?? exeDir
                };

                // パスワードの渡し方：PGPASSWORD が最も手軽
                if (!string.IsNullOrEmpty(password))
                {
                    // .NET 4.8 では EnvironmentVariables コレクションを使用
                    psi.EnvironmentVariables["PGPASSWORD"] = password;
                }

                AddLog($"[Restore] 実行開始: {psi.FileName} {psi.Arguments}");

                using (var proc = new Process { StartInfo = psi, EnableRaisingEvents = true })
                {
                    // 出力取り込み（デッドロック回避のためイベントで読む）
                    var tcsExit = new TaskCompletionSource<int>();

                    proc.OutputDataReceived += async (_, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                            AddLog($"[pg_restore][out] {e.Data}");
                    };
                    proc.ErrorDataReceived += async (_, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                            AddLog($"[pg_restore][err] {e.Data}");
                    };
                    proc.Exited += (_, __) =>
                    {
                        try { tcsExit.TrySetResult(proc.ExitCode); } catch { }
                    };

                    if (!proc.Start())
                    {
                        AddLog("[Restore] pg_restore 起動に失敗しました");
                        return false;
                    }

                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();

                    // キャンセル監視
                    using (cancel.Register(() =>
                    {
                        try
                        {
                            if (!proc.HasExited)
                            {
                                proc.Kill();
                            }
                        }
                        catch { }
                    }))
                    {
                        // WaitForExitAsync が .NET 4.8 にないため、Exited の TCS を await
                        int exitCode = await tcsExit.Task;
                        AddLog($"[Restore] pg_restore 終了コード: {exitCode}");
                        return exitCode == 0;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                AddLog("[Restore] キャンセルされました");
                return false;
            }
            catch (Exception ex)
            {
                AddLog($"[Restore] 例外: {ex.GetType().Name} / {ex.Message}");
                return false;
            }
        }

        private async Task BackupDatabaseAsync()
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "バックアップの保存先フォルダを選択してください";
                fbd.ShowNewFolderButton = true;

                if (fbd.ShowDialog(this) != DialogResult.OK) return;

                string folder = fbd.SelectedPath;
                if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                {
                    AddLog($"[Backup] フォルダが無効です: {folder}");
                    return;
                }

                // 既定ファイル名 OQSDrugData_yymmdd.backup
                string baseName = $"OQSDrugData_{DateTime.Now:yyMMdd}.backup";
                string outFile = Path.Combine(folder, baseName);

                // 既に同名がある場合は時刻を付与
                if (File.Exists(outFile))
                {
                    outFile = Path.Combine(folder, $"OQSDrugData_{DateTime.Now:yyMMdd_HHmmss}.backup");
                }

                // 接続情報（必要に応じてSettingsから取得）
                string host = Properties.Settings.Default.PGaddress;
                int port = Properties.Settings.Default.PGport;
                string user = Properties.Settings.Default.PGuser;
                string db = CommonFunctions.PGdatabaseName;
                string password = decodePassword(Properties.Settings.Default.PGpass); // 任意の保管場所

                using (var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10)))
                {
                    bool ok = await RunPgDumpAsync(
                        outFile: outFile,
                        host: host, port: port, user: user, database: db, password: password,
                        cancel: cts.Token);

                    AddLog(ok
                        ? $"[Backup] 完了: {outFile}"
                        : $"[Backup] 失敗: {outFile}");
                }
            }
        }

        // pg_dump 実行（-Fc, -Z 9、sgml_テーブル除外）
        private async Task<bool> RunPgDumpAsync(
            string outFile,
            string host, int port, string user, string database, string password,
            CancellationToken cancel)
        {
            try
            {
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                string pgDumpPath = Path.Combine(exeDir, "pqsgl", "bin", "pg_dump.exe"); // 同梱パスに合わせて調整
                if (!File.Exists(pgDumpPath))
                {
                    AddLog($"[Backup] pg_dump が見つかりません: {pgDumpPath}");
                    return false;
                }

                // -F c（= -Fc）: カスタム形式 / -Z 9: 最高圧縮
                // -T で sgml_ を除外（スキーマ付き/なし両方指定しておくと安心）
                var sb = new StringBuilder();
                sb.AppendFormat("-h \"{0}\" -p {1} -U \"{2}\" -d \"{3}\" ", host, port, user, database);
                sb.Append("-F c -Z 9 --no-owner --no-privileges ");  // 所有者/権限は環境依存を避ける
                sb.Append("-T \"sgml_*\" -T \"public.sgml_*\" ");   // sgml_で始まるテーブル除外
                sb.AppendFormat("-f \"{0}\"", outFile);

                var psi = new ProcessStartInfo
                {
                    FileName = pgDumpPath,
                    Arguments = sb.ToString(),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(pgDumpPath) ?? exeDir
                };

                if (!string.IsNullOrEmpty(password))
                {
                    psi.EnvironmentVariables["PGPASSWORD"] = password;
                }

                AddLog($"[Backup] 実行: {psi.FileName} {psi.Arguments}");

                using (var proc = new Process { StartInfo = psi, EnableRaisingEvents = true })
                {
                    var tcsExit = new TaskCompletionSource<int>();

                    proc.OutputDataReceived += async (_, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                            AddLog($"[pg_dump][out] {e.Data}");
                    };
                    proc.ErrorDataReceived += async (_, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                            AddLog($"[pg_dump][err] {e.Data}");
                    };
                    proc.Exited += (_, __) =>
                    {
                        try { tcsExit.TrySetResult(proc.ExitCode); } catch { }
                    };

                    if (!proc.Start())
                    {
                        AddLog("[Backup] pg_dump の起動に失敗しました");
                        return false;
                    }

                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();

                    using (cancel.Register(() =>
                    {
                        try { if (!proc.HasExited) proc.Kill(); } catch { }
                    }))
                    {
                        int exit = await tcsExit.Task;
                        AddLog($"[Backup] pg_dump 終了コード: {exit}");
                        return exit == 0;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                AddLog("[Backup] キャンセルされました");
                return false;
            }
            catch (Exception ex)
            {
                AddLog($"[Backup] 例外: {ex.GetType().Name} / {ex.Message}");
                return false;
            }
        }

        private async void buttonDump_Click(object sender, EventArgs e)
        {
            await BackupDatabaseAsync(); 
        }
    }
}

