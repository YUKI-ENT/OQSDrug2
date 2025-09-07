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
                    labelServer.Text = $"Server {Properties.Settings.Default.PGaddress} Connected";
                    labelDB.Text = CommonFunctions.PGdatabaseName + ":Ready";

                    buttonMigrate.Enabled = true;
                    buttonCreate.Enabled = true;
                    buttonImportSGML.Enabled = true;    
                    buttonDump.Enabled = true;

                    dbExists = true;
                }
                else if (result == "No database")
                {

                    textBoxStatus.Text = $"⚠️ サーバー接続成功、しかしデータベース {CommonFunctions.PGdatabaseName} が存在しません。";
                    labelServer.ForeColor = Color.White;
                    labelServer.BackColor = Color.LightGreen;
                    labelDB.ForeColor = Color.Gray;
                    labelDB.BackColor = Color.LightGray;

                    labelServer.Text = $"Server {Properties.Settings.Default.PGaddress} Connected";
                    labelDB.Text = CommonFunctions.PGdatabaseName + " not found";

                    buttonMigrate.Enabled = true;
                    buttonCreate.Enabled = true;
                    buttonImportSGML.Enabled = false;
                    buttonDump.Enabled = false;

                    dbExists = false;
                }
                else
                {

                    textBoxStatus.Text = "❌ サーバー接続失敗:\r\n";
                    labelServer.ForeColor = Color.Gray;
                    labelServer.BackColor = Color.LightGray;
                    labelDB.ForeColor = Color.Gray;
                    labelDB.BackColor = Color.LightGray;

                    labelServer.Text = $"Server {Properties.Settings.Default.PGaddress} not found";
                    labelDB.Text = CommonFunctions.PGdatabaseName + " not found";

                    buttonMigrate.Enabled = false;
                    buttonCreate.Enabled = false;
                    buttonImportSGML.Enabled = false;
                    buttonDump.Enabled = false;

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
            textBoxPGDumpFolder.Text = Properties.Settings.Default.PGDumpFolder;

            checkBoxScheduleDump.Checked = Properties.Settings.Default.AutoPGDump;
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

                    MessageBox.Show($"PostgreSQLにデータベース{PGdatabaseName}を作成しました");
                }catch (Exception ex)
                {
                    MessageBox.Show(ex.Message );
                }
                finally
                {
                    _isRunning = false;
                    buttonCreate.Text = "新規作成";
                }

                await CheckPostgreSQLConnectionAsync();
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
            if (this.IsDisposed || textBoxStatus.IsDisposed) return;

            if (textBoxStatus.InvokeRequired)
            {
                textBoxStatus.BeginInvoke((Action)(() =>
                {
                    textBoxStatus.AppendText(message + Environment.NewLine);
                    // 必要なら最終行までスクロール
                    textBoxStatus.SelectionStart = textBoxStatus.TextLength;
                    textBoxStatus.ScrollToCaret();
                }));
            }
            else
            {
                textBoxStatus.AppendText(message + Environment.NewLine);
                textBoxStatus.SelectionStart = textBoxStatus.TextLength;
                textBoxStatus.ScrollToCaret();
            }
        }

        private async void buttonImportSGML_Click(object sender, EventArgs e)
        {
            await RestoreFromBackupAsync();
        }

        // ▼ メイン：ダイアログでファイル選択→pg_restore実行
        private async Task RestoreFromBackupAsync()
        {
            AddLog("データのインポートを開始します");

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
                        string orgCaption = buttonImportSGML.Text;

                        buttonImportSGML.Text = "インポート中";
                        buttonImportSGML.Enabled = false;
                        
                        bool ok = await RunPgRestoreAsync(
                            backupPath: backupPath,
                            host: host, port: port, user: user, database: db, password: password,
                            cancel: cts.Token).ConfigureAwait(true); 

                        string result = ok ? "[Restore] 完了" : "[Restore] 失敗"; 
                        AddLog(result);

                        buttonImportSGML.Text = orgCaption;
                        buttonImportSGML.Enabled = true;

                        MessageBox.Show(result);    

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
            CancellationToken cancel,
            IEnumerable<string> tables = null,   // null/空: 全復元, 指定あり: 部分復元
            string schema = null,                // 例: "public"（任意）
            bool clean = true,                   // 既存を落として作り直す
            bool noOwner = true,                 // 所有者/権限は復元しない
            int jobs = 2                         // 並列度
)
        {
            try
            {
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                string pgRestorePath = Path.Combine(exeDir, "pgsql", "bin", "pg_restore.exe");
                if (!File.Exists(pgRestorePath))
                {
                    AddLog($"[Restore] pg_restore が見つかりません: {pgRestorePath}");
                    return false;
                }
                if (!File.Exists(backupPath))
                {
                    AddLog($"[Restore] バックアップが見つかりません: {backupPath}");
                    return false;
                }

                var sb = new StringBuilder();
                sb.AppendFormat("-h \"{0}\" -p {1} -U \"{2}\" -d \"{3}\" ", host, port, user, database);
                if (clean) sb.Append("--clean --if-exists ");
                if (noOwner) sb.Append("--no-owner --no-privileges ");
                if (jobs > 1) sb.AppendFormat("--jobs={0} ", jobs);
                sb.Append("--verbose ");

                if (!string.IsNullOrWhiteSpace(schema))
                    sb.AppendFormat("-n \"{0}\" ", schema);

                if (tables != null)
                {
                    foreach (var t in tables.Where(s => !string.IsNullOrWhiteSpace(s)))
                        sb.AppendFormat("-t \"{0}\" ", t);   // 例: public.ai_prompt_tpl
                }

                sb.AppendFormat("\"{0}\"", backupPath);

                var psi = new ProcessStartInfo
                {
                    FileName = pgRestorePath,
                    Arguments = sb.ToString(),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                if (!string.IsNullOrEmpty(password))
                    psi.EnvironmentVariables["PGPASSWORD"] = password;

                AddLog($"[Restore] 実行: {psi.FileName} {psi.Arguments}");
                return await RunProcessWithLogsAsync(psi, cancel);
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

        // 共通のプロセス実行（ログ取りつつ終了コード判定）
        private async Task<bool> RunProcessWithLogsAsync(ProcessStartInfo psi, CancellationToken cancel)
        {
            using (var proc = new Process { StartInfo = psi, EnableRaisingEvents = true })
            {
                var tcsExit = new TaskCompletionSource<int>();
                proc.OutputDataReceived += (_, e) => { if (!string.IsNullOrEmpty(e.Data)) AddLog(e.Data); };
                proc.ErrorDataReceived += (_, e) => { if (!string.IsNullOrEmpty(e.Data)) AddLog(e.Data); };
                proc.Exited += (_, __) => { try { tcsExit.TrySetResult(proc.ExitCode); } catch { } };

                if (!proc.Start()) { AddLog("[Restore] プロセス起動に失敗"); return false; }
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                using (cancel.Register(() => { try { if (!proc.HasExited) proc.Kill(); } catch { } }))
                {
                    int exit = await tcsExit.Task;
                    AddLog($"[Restore] 終了コード: {exit}");
                    return exit == 0;
                }
            }
        }

        
        private async void buttonDump_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("PostgreSQLデータのバックアップを作成しますか？", "確認", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            if (!Directory.Exists(textBoxPGDumpFolder.Text))
            {
                MessageBox.Show("バックアップ先フォルダを指定してから実行してください");
                return;
            }

            buttonDump.Enabled = false;
            var oldText = buttonDump.Text;
            buttonDump.Text = "バックアップ中…";
            Cursor = Cursors.WaitCursor;

            try
            {
                // 時刻プレフィックス付けたいならこの行を使う：
                // Action<string> uiLog = m => AddLog($"[{DateTime.Now:HH:mm:ss}] {m}");
                // その場合は log: uiLog を渡す。

                AddLog("[Backup] 手動バックアップ開始…");
                var result = await CommonFunctions.TryRunScheduledDumpAsync(
                    force: true,
                    log: AddLog,                     // ← そのまま渡してOK
                    ct: System.Threading.CancellationToken.None
                );

                switch (result)
                {
                    case BackupOutcome.Success:
                        AddLog("[Backup] 手動バックアップ: 完了");
                        break;
                    case BackupOutcome.Skipped:
                        AddLog("[Backup] 手動バックアップ: スキップ（間隔未経過 or 実行中）");
                        break;
                    default:
                        AddLog("[Backup] 手動バックアップ: 失敗");
                        break;
                }
            }
            catch (Exception ex)
            {
                AddLog("[Backup] 例外: " + ex.Message);
            }
            finally
            {
                buttonDump.Enabled = true;
                buttonDump.Text = oldText;
                Cursor = Cursors.Default;
            }
        }

        private void buttonPGDumpFolderSelect_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "バックアップの保存先フォルダを選択してください";
                fbd.ShowNewFolderButton = true;

                if (fbd.ShowDialog(this) != DialogResult.OK) return;

                string folder = fbd.SelectedPath;
                if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                {
                    MessageBox.Show($"[Backup] フォルダが無効です: {folder}");
                }
                else
                {
                    textBoxPGDumpFolder.Text = folder;
                    Properties.Settings.Default.PGDumpFolder = folder;
                    Properties.Settings.Default.Save();
                }
            }
        }

        private void checkBoxScheduleDump_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.AutoPGDump = checkBoxScheduleDump.Checked;
            Properties.Settings.Default.Save();
        }

        private async void buttonListContent_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "バックアップファイルを選択";
                dlg.Filter = "PG Dump (*.backup;*.dump;*.tar)|*.backup;*.dump;*.tar|すべてのファイル (*.*)|*.*";
                dlg.CheckFileExists = true;
                dlg.Multiselect = false;

                // 既定フォルダ：textBoxPGDumpFolder.Text → だめならドキュメント
                string initial = (textBoxPGDumpFolder != null) ? textBoxPGDumpFolder.Text : null;
                if (string.IsNullOrWhiteSpace(initial) || !Directory.Exists(initial))
                    initial = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                dlg.InitialDirectory = initial;

                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                string backupPath = dlg.FileName;
                AddLog($"[Inspect] 対象: {backupPath}");

                // 必要なら設定からパスワードを復号
                string password = CommonFunctions.decodePassword(Properties.Settings.Default.PGpass);

                try
                {
                    // テーブル「データが入っているもの」だけ見たいなら dataOnly: true
                    // （全部見たいなら両方 false、定義も含めたテーブル系だけ見たいなら tablesOnly: true）
                    var lines = await ListDumpContents(backupPath, password, tablesOnly: false, dataOnly: true);

                    if (lines == null || lines.Length == 0)
                    {
                        AddLog("[Inspect] 一致するエントリがありません。");
                        return;
                    }

                    foreach (var line in lines)
                        AddLog(line);
                }
                catch (Exception ex)
                {
                    AddLog("[Inspect] エラー: " + ex.Message);
                }
            }
        }
    }
}

