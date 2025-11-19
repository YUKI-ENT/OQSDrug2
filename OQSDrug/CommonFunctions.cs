using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;


namespace OQSDrug
{
    public sealed class MedRankRow
    {
        public string DrugC { get; set; } = "";   // レセプトコード（そのまま）
        public string DrugN { get; set; } = "";   // 薬剤名（コード空欄時のため常に保持）
        public long PtID { get; set; }
        public double ChronicScore { get; set; }  // 0〜1
        public double AcuteScore { get; set; }    // 0〜1
        public string LatestDate { get; set; } = "";  // "yyyyMMdd" 形式の最新投薬日
        public int DaysSinceLast { get; set; }        // 直近性（小さいほど最近）
        public string RankLabel { get; set; } = "";   // ①〜④相当のラベル
        public string Note { get; set; } = "";        // 短い説明（中断リスクなど）
    }
    public enum BackupOutcome
    {
        Success,
        Skipped,
        Failed
    }
    public static class CommonFunctions
    {
        // グローバル変数の定義
        public static string PGdatabaseName = "OQSDrug_data";

        public static string DBversion = "2";

        public static string DBProvider = "";
        public static string connectionOQSdata = "";
        public static string connectionReadOQSdata = "";
        public static bool DataDbLock = false;

        // 成分判定に使うプレフィックス長（必要なら設定化）
        private const int YJ_ING_PREFIX_LEN = 7;

        public static bool _readySGML = false;
        
        public static List<string[]> RSBDI = new List<string[]>();
        public static List<string[]> SGMLDI = new List<string[]>();

        // Korodata Dictionary
        public static Dictionary<string, string> ReceptToMedisCodeMap = new Dictionary<string, string>();

        // 基準値を格納する辞書
        public static Dictionary<string, TKKReference> TKKreferenceDict = new Dictionary<string, TKKReference>();

        // 個人フォルダ
        public static readonly string PersonalFolder =
            Environment.GetFolderPath(Environment.SpecialFolder.Personal);

        // OQSフォルダ
        public static readonly string OQSFolder =
            Path.Combine(PersonalFolder, "OQSDrug");

        // ログファイルパス
        public static readonly string LogFile =
            Path.Combine(OQSFolder, $"OQSDrug_{Environment.MachineName}_{DateTime.Now:yyyyMMdd}.log");

        // UIログ出力用コールバック（timestamp, message）
        public static SynchronizationContext UiSync;   // フィールドを追加しておく
                                                       // フォームのLoadで： CommonFunctions.UiSync = SynchronizationContext.Current;

        public static Action<string, string> UiLogCallback; // (timestamp, message)

        // ollama models
        public static List<ModelInfo> ollamaModelList = new List<ModelInfo>();

        //PGDump
        public static DateTime? lastDumped = null;                   // 最後に成功した時刻（ローカル時刻）
        public static readonly TimeSpan DumpInterval = TimeSpan.FromHours(3);
        public static readonly TimeSpan Retention = TimeSpan.FromDays(7);
        private static readonly SemaphoreSlim _dumpGate = new SemaphoreSlim(1, 1); // 重複実行防止


        // 基準値を格納するクラス
        public class TKKReference
        {
            public string ItemCode { get; set; }
            public string ItemName { get; set; }
            public string CompairType { get; set; }
            public string Limit1 { get; set; }
            public string Limit2 { get; set; }
            public int? Sex { get; set; }
        }


        public static void SnapToScreenEdges(Form form, int snapDistance, int snapCompPixel)
        {
            Rectangle workingArea = Screen.FromControl(form).WorkingArea;

            int newX = form.Left;
            int newY = form.Top;

            if (Math.Abs(form.Left - workingArea.Left) <= snapDistance)
            {
                newX = workingArea.Left - snapCompPixel;
            }
            else if (Math.Abs(form.Right - workingArea.Right) <= snapDistance)
            {
                newX = workingArea.Right - form.Width + snapCompPixel;
            }

            if (Math.Abs(form.Top - workingArea.Top) <= snapDistance)
            {
                newY = workingArea.Top;
            }
            else if (Math.Abs(form.Bottom - workingArea.Bottom) <= snapDistance)
            {
                newY = workingArea.Bottom - form.Height + snapCompPixel;
            }

            form.Location = new Point(newX, newY);
        }


        public static async Task<bool> RetryClipboardSetTextAsync(string text)
        {
            const int maxRetries = 10;
            const int baseDelayMs = 50;  // 50,100,150...
            const int timeoutMs = 2000;  // 1試行あたりのタイムアウト

            if (text == null) text = string.Empty;

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                var tcs = new TaskCompletionSource<bool>();

                var th = new Thread(() =>
                {
                    try
                    {
                        // 遅延レンダリング由来の固まりを避けるため、形式を明示＋copy=true
                        var data = new DataObject();
                        data.SetData(DataFormats.UnicodeText, true, text);
                        Clipboard.SetDataObject(data, /*copy:*/ true);
                        tcs.TrySetResult(true);
                    }
                    catch (System.Runtime.InteropServices.ExternalException)
                    {
                        // 「他プロセスが使用中」は false で返して上位でリトライ
                        tcs.TrySetResult(false);
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                });

                th.IsBackground = true;
                th.SetApartmentState(ApartmentState.STA);
                th.Start();

                // タイムアウト監視
                var completed = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs));
                if (completed == tcs.Task)
                {
                    // 4.8 互換：成功判定
                    if (tcs.Task.IsCompleted && !tcs.Task.IsFaulted && !tcs.Task.IsCanceled && tcs.Task.Result)
                        return true; // 成功

                    // Result==false（使用中）→下でリトライ
                }
                // タイムアウト or 使用中 → 少し待って再試行（指数バックオフ）
                await Task.Delay(baseDelayMs * (attempt + 1));
            }

            return false;
        }


        public static async Task<bool> WaitForDbUnlock(int maxWaitms)
        {
            int interval = 10;
            int retry = maxWaitms / interval;
            for (int i = 0; i < retry; i++)
            {
                if (DataDbLock)
                {
                    await Task.Delay(interval);
                }
                else
                {
                    return true;
                }
            }
            return false;
        }

        // 基準値を取得して辞書に格納
        public static async Task<Dictionary<string, TKKReference>> LoadTKKReference()
        {
            var dict = new Dictionary<string, TKKReference>();

            try
            {
                using (OleDbConnection connection = new OleDbConnection(connectionReadOQSdata))
                {
                    await connection.OpenAsync();
                    string query = "SELECT ItemCode, ItemName, CompairType, Limit1, Limit2, Sex FROM TKK_reference";
                    using (var command = new OleDbCommand(query, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            string itemCode = reader["ItemCode"].ToString();
                            if (itemCode.Length > 3)
                            {
                                itemCode = itemCode.Substring(0, 4);
                                string itemName = reader["ItemName"].ToString();
                                string compairType = reader["CompairType"].ToString();
                                string limit1 = reader["Limit1"].ToString();
                                string limit2 = reader["Limit2"].ToString();
                                int? sex = reader["Sex"] == DBNull.Value ? 0 : (int?)Convert.ToInt32(reader["Sex"]);

                                string itemCodeWithSex;

                                if (sex == 0) // 男女共通
                                {
                                    itemCodeWithSex = $"1_{itemCode}";
                                    dict[itemCodeWithSex] = new TKKReference
                                    {
                                        ItemCode = itemCode,
                                        ItemName = itemName,
                                        CompairType = compairType,
                                        Limit1 = limit1,
                                        Limit2 = limit2,
                                        Sex = sex
                                    };
                                    itemCodeWithSex = $"2_{itemCode}";
                                    dict[itemCodeWithSex] = new TKKReference
                                    {
                                        ItemCode = itemCode,
                                        ItemName = itemName,
                                        CompairType = compairType,
                                        Limit1 = limit1,
                                        Limit2 = limit2,
                                        Sex = sex
                                    };
                                }
                                else
                                {
                                    itemCodeWithSex = $"{sex}_{itemCode}";
                                    dict[itemCodeWithSex] = new TKKReference
                                    {
                                        ItemCode = itemCode,
                                        ItemName = itemName,
                                        CompairType = compairType,
                                        Limit1 = limit1,
                                        Limit2 = limit2,
                                        Sex = sex
                                    };
                                }
                            }
                        }
                    }
                }
                return dict;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return new Dictionary<string, TKKReference>();
            }
        }

        /// <summary>
        /// UIとファイルにログを追加
        /// </summary>
        // 改訂版 AddLogAsync
        public static async Task AddLogAsync(string message, bool fileOnly = false)
        {
            var now = DateTime.Now; // 表示はローカルでOK（UTCにしたいなら DateTime.UtcNow）
            string timestamp = now.ToString("yy-MM-dd HH:mm:ss");
            string fullMessage = $"{timestamp} {message}";

            // UI表示（非同期ポスト：呼び出し元をブロックしない）
            if (!fileOnly && UiLogCallback != null)
            {
                var sync = UiSync;
                if (sync != null)
                {
                    sync.Post(_ =>
                    {
                        try { UiLogCallback(timestamp, message); }
                        catch { /* UI側で安全に握りつぶす */ }
                    }, null);
                }
                else
                {
                    // 同期コンテキストがない場合は最悪ベストエフォート
                    // UI以外の処理にしておく（UIスレッドでなければ直接触らない）
                }
            }

            // ファイル保存（本物の非同期I/O）
            await SaveLogToFileAsync(fullMessage).ConfigureAwait(false);
        }

        /// <summary>ログファイルに非同期追記</summary>
        private static async Task SaveLogToFileAsync(string logEntry)
        {
            try
            {
                if (!Directory.Exists(OQSFolder))
                {
                    Directory.CreateDirectory(OQSFolder);
                }

                // useAsync:true で FileStream を開く
                using (var fs = new FileStream(
                    LogFile,
                    FileMode.Append,
                    FileAccess.Write,
                    FileShare.ReadWrite,
                    bufferSize: 4096,
                    useAsync: true))
                using (var writer = new StreamWriter(fs))
                {
                    await writer.WriteLineAsync(logEntry).ConfigureAwait(false);
                    await writer.FlushAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                // ファイルI/OはUIをブロックしないようにConsoleへ
                System.Diagnostics.Debug.WriteLine($"[Log] {ex.Message}");
            }
        }

        // Access/PG 併用対策関数

        public static IDbConnection GetDbConnection(bool ReadOnly = true)
        {
            if (Properties.Settings.Default.DBtype == "pg")
            {
                string connStr = $"Host={Properties.Settings.Default.PGaddress};" +
                                 $"Port={Properties.Settings.Default.PGport};" +
                                 $"Username={Properties.Settings.Default.PGuser};" +
                                 $"Password={decodePassword(Properties.Settings.Default.PGpass)};" +
                                 $"Database={PGdatabaseName}";
                return new NpgsqlConnection(connStr);
            }
            else
            {
                if (ReadOnly)
                {
                    return new OleDbConnection(connectionReadOQSdata);
                }
                else
                {
                    return new OleDbConnection(connectionOQSdata);
                }
            }
        }


        public static DataTable PivotDataTable(
            DataTable sourceTable,
            string[] rowFields,
            string columnField,
            string valueField,
            Func<IEnumerable<object>, object> aggregateFunc = null)
        {
            DataTable pivotTable = new DataTable();

            // 結合したRowKeyを作成
            var rowKeys = sourceTable.AsEnumerable()
                .Select(r => string.Join("|", rowFields.Select(f => r[f].ToString())))
                .Distinct()
                .ToList();

            var columnKeys = sourceTable.AsEnumerable()
                .Select(r => r[columnField].ToString())
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            // 列構成の作成
            foreach (var f in rowFields)
            {
                pivotTable.Columns.Add(f);
            }
            foreach (var col in columnKeys)
            {
                pivotTable.Columns.Add(col);
            }

            // データの格納
            foreach (var key in rowKeys)
            {
                var newRow = pivotTable.NewRow();
                var keyParts = key.Split('|');

                for (int i = 0; i < rowFields.Length; i++)
                {
                    newRow[rowFields[i]] = keyParts[i];
                }

                var matches = sourceTable.AsEnumerable()
                    .Where(r => string.Join("|", rowFields.Select(f => r[f].ToString())) == key);

                foreach (var col in columnKeys)
                {
                    var values = matches
                        .Where(r => r[columnField].ToString() == col)
                        .Select(r => r[valueField]);

                    newRow[col] = aggregateFunc != null ? aggregateFunc(values) : values.FirstOrDefault();
                }

                pivotTable.Rows.Add(newRow);
            }

            return pivotTable;
        }


        public static void AddDbParameter(IDbCommand cmd, string name, object value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name; // OleDbは位置指定だが、名前はそのままでOK

            if (value is DateTime dt)
            {
                if (Properties.Settings.Default.DBtype == "pg")
                {
                    // PostgreSQL: 必ず UTC で渡す（timestamptz の Kind=Utc 必須）
                    if (dt.Kind == DateTimeKind.Unspecified)
                    {
                        // 未指定ならローカル想定→UTCへ
                        dt = DateTime.SpecifyKind(dt, DateTimeKind.Local);
                    }
                    dt = dt.ToUniversalTime();
                    p.DbType = DbType.DateTime;
                    p.Value = dt;
                }
                else
                {
                    // Access(OleDb): ローカル時刻の「文字列」で渡すのが安全
                    // ※ DateTimeのままだと「データ型が一致しません」になりがち
                    var local = (dt.Kind == DateTimeKind.Utc) ? dt.ToLocalTime() : dt;
                    p.DbType = DbType.String;
                    p.Value = local.ToString("yyyy-MM-dd HH:mm:ss");
                }
            }
            else
            {
                // DateTime以外はそのまま
                p.Value = value ?? DBNull.Value;
            }

            cmd.Parameters.Add(p);
        }


        public static string ConvertSqlForOleDb(string sql)
        {
            if (Properties.Settings.Default.DBtype == "pg") return sql;
            if (string.IsNullOrEmpty(sql)) return sql;

            // @param を ? に置換（@の後は英字 or _ で開始、その後は英数字 or _）
            return Regex.Replace(sql, @"@([A-Za-z_][A-Za-z0-9_]*)", "?");
        }

        public static async Task<string> CheckPGStatusAsync(
             string databaseName,
             string tableName = "",
             int timeoutMs = 5000) // タイムアウト(ms)
        {
            // Npgsqlの接続/コマンドタイムアウトは秒単位
            int timeoutSec = Math.Max(1, timeoutMs / 1000);

            string baseConnStr =
                $"Host={Properties.Settings.Default.PGaddress};" +
                $"Port={Properties.Settings.Default.PGport};" +
                $"Username={Properties.Settings.Default.PGuser};" +
                $"Password={decodePassword(Properties.Settings.Default.PGpass)};" +
                $"Timeout={timeoutSec};" +            // 接続タイムアウト(秒)
                $"Command Timeout={timeoutSec};";     // コマンドタイムアウト(秒)

            string fullConnStr = baseConnStr + $"Database={databaseName};";

            try
            {
                using (var cts = new CancellationTokenSource(timeoutMs))
                using (var conn = new Npgsql.NpgsqlConnection(fullConnStr))
                {
                    // --- 接続（タイムアウトあり） ---
                    var openTask = conn.OpenAsync(cts.Token);
                    if (await Task.WhenAny(openTask, Task.Delay(timeoutMs, cts.Token)) != openTask)
                        return "No server"; // タイムアウト＝接続NG

                    // 例外（認証失敗など）があればここで吐かせる
                    await openTask;

                    if (!string.IsNullOrEmpty(tableName))
                    {
                        const string sql = @"
                    SELECT 1
                    FROM information_schema.tables
                    WHERE table_schema = 'public'
                      AND (table_name = @tname_lower OR table_name = @tname_exact)
                    LIMIT 1;";

                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = sql;
                            CommonFunctions.AddDbParameter(cmd, "@tname_lower", tableName.ToLower());
                            CommonFunctions.AddDbParameter(cmd, "@tname_exact", tableName);

                            // --- クエリ（タイムアウトあり） ---
                            var execTask = ((DbCommand)cmd).ExecuteScalarAsync(cts.Token);
                            if (await Task.WhenAny(execTask, Task.Delay(timeoutMs, cts.Token)) != execTask)
                                return "No server"; // タイムアウト＝接続NG

                            var exists = await execTask;
                            return (exists != null)
                                ? "OK"
                                : $"エラー: テーブル '{tableName}' が存在しません。";
                        }
                    }

                    return "OK";
                }
            }
            // 指定DBが存在しない（3D000）
            catch (Npgsql.PostgresException pgex) when (pgex.SqlState == "3D000")
            {
                // 接続自体は生きているので "No database"
                return "No database";
            }
            catch (TaskCanceledException)
            {
                // 任意の待機/実行タイムアウトをまとめて接続NG扱い
                return "No server";
            }
            catch
            {
                // DNS失敗、ネットワーク不通、認証/SSL失敗 なども接続NG扱い
                return "No server";
            }
        }

        //Upgrade Backend DB 
        public static async Task<bool> CheckDBVersionAsync(string databaseVersion)
        {
            try
            {
                using (var conn = GetDbConnection(false))
                {
                    await ((DbConnection)conn).OpenAsync();
                    bool isPg = (Properties.Settings.Default.DBtype == "pg");

                    bool tableExists = false;
                    bool hasSettingValueCol = false;

                    // --- テーブル・列確認 ---
                    if (isPg)
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "SELECT 1 FROM information_schema.tables WHERE table_schema='public' AND table_name='settings'";
                            tableExists = (await ((DbCommand)cmd).ExecuteScalarAsync()) != null;
                        }

                        if (tableExists)
                        {
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = "SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='settings' AND column_name='setting_value'";
                                hasSettingValueCol = (await ((DbCommand)cmd).ExecuteScalarAsync()) != null;
                            }
                        }
                    }
                    else
                    {
                        var ole = (OleDbConnection)conn;
                        var tables = ole.GetSchema("Tables", new string[] { null, null, "settings", "TABLE" });
                        tableExists = tables.Rows.Count > 0;

                        if (tableExists)
                        {
                            var cols = ole.GetSchema("Columns", new string[] { null, null, "settings", null });
                            hasSettingValueCol = cols.AsEnumerable().Any(r => string.Equals(r["COLUMN_NAME"]?.ToString(), "setting_value", StringComparison.OrdinalIgnoreCase));
                        }
                    }

                    // --- 再作成が必要な場合 ---
                    if (!tableExists || !hasSettingValueCol)
                    {
                        await DropAndCreateSettingsTableAsync(conn, isPg);
                        AddLogAsync("[CheckDBVersionAsync] settingテーブルを再作成しました");
                    }

                    // --- version の取得 ---
                    string currentVersion = await GetCurrentDbVersionAsync(conn, isPg);

                    if (string.IsNullOrEmpty(currentVersion))
                    {
                        // バージョンキーが無ければ初期設定
                        await SetDbVersionAsync(conn, isPg, databaseVersion);
                        AddLogAsync($"[CheckDBVersionAsync] 初期バージョンを {databaseVersion} に設定しました。");
                    }
                    else
                    {
                        // バージョン比較
                        if (Convert.ToUInt16(currentVersion) < Convert.ToUInt16(databaseVersion))
                        {
                            AddLogAsync($"[CheckDBVersionAsync] バージョンアップ検出: {currentVersion} → {databaseVersion}");
                            await UpgradeDatabaseAsync(conn, isPg, currentVersion, databaseVersion);
                            await SetDbVersionAsync(conn, isPg, databaseVersion);
                            AddLogAsync($"[CheckDBVersionAsync] バージョンを {databaseVersion} に更新しました。");
                        }
                        else
                        {
                            AddLogAsync($"[CheckDBVersionAsync] バージョンは最新 ({currentVersion}) です。");
                        }
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                AddLogAsync($"[CheckDBVersionAsync] エラー: {ex.Message}");
                return false;
            }

        }

        public static async Task DropAndCreateSettingsTableAsync(IDbConnection conn, bool isPg)
        {
            if (isPg)
            {
                using (var drop = conn.CreateCommand())
                {
                    drop.CommandText = "DROP TABLE IF EXISTS public.settings CASCADE;";
                    await ((DbCommand)drop).ExecuteNonQueryAsync();
                }
                using (var create = conn.CreateCommand())
                {
                    create.CommandText = @"CREATE TABLE public.settings (
                id SERIAL PRIMARY KEY,
                key VARCHAR(255) NOT NULL UNIQUE,
                setting_value VARCHAR(255)
            );";
                    await ((DbCommand)create).ExecuteNonQueryAsync();
                }
            }
            else
            {
                using (var drop = conn.CreateCommand())
                {
                    drop.CommandText = "DROP TABLE settings";
                    try { await ((DbCommand)drop).ExecuteNonQueryAsync(); } catch { }
                }
                using (var create = conn.CreateCommand())
                {
                    create.CommandText = @"CREATE TABLE settings (
                id AUTOINCREMENT PRIMARY KEY,
                [key] TEXT(255) NOT NULL,
                setting_value TEXT(255)
            )";
                    await ((DbCommand)create).ExecuteNonQueryAsync();
                }
                using (var uniq = conn.CreateCommand())
                {
                    uniq.CommandText = "CREATE UNIQUE INDEX settings_key_uq ON settings([key])";
                    await ((DbCommand)uniq).ExecuteNonQueryAsync();
                }
            }
        }

        public static async Task<string> GetCurrentDbVersionAsync(IDbConnection conn, bool isPg)
        {
            try
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT setting_value FROM settings WHERE key=@k";
                    cmd.CommandText = ConvertSqlForOleDb(cmd.CommandText);
                    AddDbParameter(cmd, "@k", "version");
                    var val = await ((DbCommand)cmd).ExecuteScalarAsync();
                    return val?.ToString();
                }
            }
            catch
            {
                return null;
            }
        }

        public static async Task SetDbVersionAsync(IDbConnection conn, bool isPg, string version)
        {
            using (var cmd = conn.CreateCommand())
            {
                if (isPg)
                {
                    cmd.CommandText = @"INSERT INTO settings (key, setting_value) VALUES (@k, @v)
                ON CONFLICT (key) DO UPDATE SET setting_value = EXCLUDED.setting_value";
                }
                else
                {
                    cmd.CommandText = "UPDATE settings SET setting_value=@v WHERE [key]=@k";
                    cmd.CommandText = ConvertSqlForOleDb(cmd.CommandText);
                }
                AddDbParameter(cmd, "@k", "version");
                AddDbParameter(cmd, "@v", version);
                int affected = await ((DbCommand)cmd).ExecuteNonQueryAsync();

                if (!isPg && affected == 0)
                {
                    using (var ins = conn.CreateCommand())
                    {
                        ins.CommandText = "INSERT INTO settings ([key], setting_value) VALUES (@k, @v)";
                        ins.CommandText = ConvertSqlForOleDb(ins.CommandText);
                        AddDbParameter(ins, "@k", "version");
                        AddDbParameter(ins, "@v", version);
                        await ((DbCommand)ins).ExecuteNonQueryAsync();
                    }
                }
            }
        }

       
        public static async Task UpgradeDatabaseAsync(IDbConnection conn, bool isPg, string oldVersion, string newVersion)
        {
            // ここにバージョンアップ処理（例: ALTER TABLE 追加等）
            AddLogAsync($"[DB] バージョンアップ処理実行: {oldVersion} → {newVersion}");
            await Task.CompletedTask;
        }

        /// <summary>
        /// 任意のDataTableをクロス集計（Pivot）する汎用関数
        /// </summary>
        /// <param name="source">元のDataTable</param>
        /// <param name="rowFields">行方向に残す列名の配列</param>
        /// <param name="columnField">列方向に展開する列名（値が列見出しになる）</param>
        /// <param name="dataField">集計対象の列名（値がセルに入る）</param>
        /// <param name="aggregate">集計関数（nullの場合はSum）</param>
        /// <returns>ピボット変換後のDataTable</returns>
        public static DataTable ConvertPivotDataTable(
            DataTable source,
            string[] rowFields,
            string columnField,
            string dataField,
            Func<IEnumerable<object>, object> aggregate = null)
        {
            if (aggregate == null)
            {
                // デフォルトはSum
                aggregate = values =>
                {
                    double sum = 0;
                    foreach (var v in values)
                    {
                        if (v != DBNull.Value && double.TryParse(v.ToString(), out double d))
                            sum += d;
                    }
                    return sum;
                };
            }

            DataTable pivotTable = new DataTable();

            // 行フィールド列を追加
            foreach (var field in rowFields)
                pivotTable.Columns.Add(field, source.Columns[field].DataType);

            // 列見出しのユニーク値取得（降順ソート）
            var columnValues = source.AsEnumerable()
                .Select(r => r[columnField]?.ToString())
                .Where(v => !string.IsNullOrEmpty(v))
                .Distinct()
                .OrderByDescending(v =>
                {
                    // 数値として解釈できる場合は数値降順
                    if (long.TryParse(v, out long num))
                        return num;
                    // 日付として解釈できる場合は日付降順
                    if (DateTime.TryParse(v, out DateTime dt))
                        return dt.Ticks;
                    // それ以外は文字列降順
                    return long.MinValue;
                })
                .ToList();

            // 列見出し列を追加
            foreach (var colValue in columnValues)
            {
                string newColName = TrimLeadingTwo(colValue);
                pivotTable.Columns.Add(newColName, typeof(object));
            }

            // グループ化
            var groups = source.AsEnumerable()
                .GroupBy(r => string.Join("|", rowFields.Select(f => r[f]?.ToString())));

            foreach (var group in groups)
            {
                var newRow = pivotTable.NewRow();

                var firstRow = group.First();
                foreach (var field in rowFields)
                    newRow[field] = firstRow[field];

                foreach (var colValue in columnValues)
                {
                    var values = group
                        .Where(r => r[columnField]?.ToString() == colValue)
                        .Select(r => r[dataField]);

                    // 同じ変換で列名を参照する
                    string newColName = TrimLeadingTwo(colValue);
                    newRow[newColName] = values.Any() ? aggregate(values) : DBNull.Value;
                }

                pivotTable.Rows.Add(newRow);
            }

            return pivotTable;
        }

        // 左2桁を削除するヘルパー
        private static string TrimLeadingTwo(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            if (s.Length > 2) return s.Substring(2);
            return s;
        }

        // ====== ここからスコア化ロジック（CommonFunctions 内に完結）======

        // 正規化上限（調整用）
        private const double GAP_CV_MAX = 1.2;
        private const double MONTH_CV_MAX = 1.5;

        // “最近”の区間（調整OK）
        private const int RECENT_6W = 45; // 6週間程度
        private const int RECENT_3M = 90; // 3ヶ月

        private enum RouteMajor { Unknown, Oral, Injection, ExternalOrDental }

        private sealed class RouteWeight
        {
            public double ChronicW;
            public double AcuteW;
            public RouteWeight(double c, double a) { ChronicW = c; AcuteW = a; }
        }

        // 経路別の重み
        private static readonly Dictionary<RouteMajor, RouteWeight> RouteWeights =
            new Dictionary<RouteMajor, RouteWeight>
            {
                { RouteMajor.Oral,             new RouteWeight(1.00, 1.00) },
                { RouteMajor.ExternalOrDental, new RouteWeight(0.95, 0.95) }, // 貼付/外用など
                { RouteMajor.Injection,        new RouteWeight(0.60, 1.10) }, // 注射は急性寄りを強調
                { RouteMajor.Unknown,          new RouteWeight(1.00, 1.00) },
            };

        private sealed class DrugHistoryRow
        {
            public DateTime DiDate;
            public string DrugC;
            public string DrugN;
        }

        // ラベル＆注記生成
        private static void MakeLabelAndNote(double chronic, double acute, int daysSinceLast, double coverage,
                                             out string label, out string note)
        {
            if (daysSinceLast <= RECENT_6W && chronic >= 0.55)
            {
                label = "継続中（慢性）";
                note = string.Format("直近{0}日以内・月カバレッジ{1:P0}", daysSinceLast, coverage);
                return;
            }
            if (chronic >= 0.55 && daysSinceLast > RECENT_3M)
            {
                label = "継続傾向だが3ヶ月空白";
                note = string.Format("最後の投薬から{0}日・月カバレッジ{1:P0}", daysSinceLast, coverage);
                return;
            }
            if (acute >= 0.60 && daysSinceLast <= RECENT_3M)
            {
                label = "急性（最近）";
                note = string.Format("直近{0}日以内に短期投薬傾向", daysSinceLast);
                return;
            }
            if (acute >= 0.60 && daysSinceLast > RECENT_3M)
            {
                label = "急性（過去）";
                note = string.Format("最後の投薬から{0}日", daysSinceLast);
                return;
            }
            if (chronic >= 0.45 && daysSinceLast <= RECENT_3M)
            {
                label = "準継続（要確認）";
                note = string.Format("直近{0}日以内・月カバレッジ{1:P0}", daysSinceLast, coverage);
                return;
            }
            label = "散発";
            note = string.Format("最後の投薬から{0}日", daysSinceLast);
        }

        /// <summary>
        /// 入力：患者ID、対象期間（月）、自院除外PrIsOrg値(例:1を除外)。
        /// 出力：薬剤ごとの Chronic/Acute スコア（0-1）＋ラベル等。
        /// 依存：GetDbConnection(), ConvertSqlForOleDb(), AddDbParameter(), ReceptToMedisCodeMap
        /// </summary>
        public static async Task<List<MedRankRow>> GetMedRanksAsync(long ptId, int months, int? excludePrIsOrgValue)
        {
            var refDate = DateTime.Today.Date;
            var tmpDate = refDate.AddMonths(-months);
            var startDate = new DateTime(tmpDate.Year, tmpDate.Month, 1);
            string startDateStr = startDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

            // 1) drug_history 抽出
            var rows = new List<DrugHistoryRow>();
            using (IDbConnection conn = GetDbConnection())
            {
                if (conn is DbConnection dbConn) await dbConn.OpenAsync();
                else conn.Open();

                using (IDbCommand cmd = conn.CreateCommand())
                {
                    string sql = @"
                        SELECT didate, drugc, drugn, prisorg
                        FROM drug_history
                        WHERE ptidmain = @PtID
                          AND didate >= @StartDate
                    ";
                    if (excludePrIsOrgValue.HasValue)
                        sql += "  AND prisorg <> @ExPrIsOrg \n";

                    sql += "ORDER BY drugc, drugn, didate;";
                    cmd.CommandText = ConvertSqlForOleDb(sql);

                    AddDbParameter(cmd, "@PtID", ptId);
                    AddDbParameter(cmd, "@StartDate", startDateStr);
                    if (excludePrIsOrgValue.HasValue)
                        AddDbParameter(cmd, "@ExPrIsOrg", excludePrIsOrgValue.Value);

                    if (cmd is DbCommand dbc)
                    {
                        using (DbDataReader r = await dbc.ExecuteReaderAsync())
                        {
                            while (await r.ReadAsync())
                            {
                                DateTime di = DateTime.ParseExact(r["didate"].ToString(), "yyyyMMdd", CultureInfo.InvariantCulture);
                                rows.Add(new DrugHistoryRow
                                {
                                    DiDate = di,
                                    DrugC = (r["drugc"] == DBNull.Value ? "" : r["drugc"].ToString().Trim()),
                                    DrugN = (r["drugn"] == DBNull.Value ? "" : r["drugn"].ToString().Trim())
                                });
                            }
                        }
                    }
                    else
                    {
                        using (IDataReader r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                DateTime di = DateTime.ParseExact(r["didate"].ToString(), "yyyyMMdd", CultureInfo.InvariantCulture);
                                rows.Add(new DrugHistoryRow
                                {
                                    DiDate = di,
                                    DrugC = (r["drugc"] == DBNull.Value ? "" : r["drugc"].ToString().Trim()),
                                    DrugN = (r["drugn"] == DBNull.Value ? "" : r["drugn"].ToString().Trim())
                                });
                            }
                        }
                    }
                }
            }

            // 2) グループ化（コード空欄は名称で補助）
            var groups = rows.GroupBy(x => string.IsNullOrEmpty(x.DrugC) ? ("#NOCODE:" + x.DrugN) : x.DrugC);
            var results = new List<MedRankRow>();

            foreach (var g in groups)
            {
                var entries = g.OrderBy(x => x.DiDate).ToList();
                if (entries.Count == 0) continue;

                string drugc = entries[entries.Count - 1].DrugC ?? "";
                string drugn = string.IsNullOrWhiteSpace(entries[entries.Count - 1].DrugN) ? "(名称未設定)" : entries[entries.Count - 1].DrugN;

                // 投与日（重複除去）
                var dates = entries.Select(e => e.DiDate.Date).Distinct().OrderBy(d => d).ToList();
                int fills = dates.Count;
                int daysSinceLast = (int)(refDate - dates[dates.Count - 1]).TotalDays;

                // 規則性メトリクス
                double? gapCv = ComputeGapCv(dates);                     // 調剤間隔CV
                double? monthCv = ComputeMonthCv(dates);                   // 月別ヒストグラムCV
                double coverage = ComputeCoverageMonths(dates, startDate, refDate); // 期間内に「投薬のあった月」の比率

                // 3) 剤型（経路大区分）
                RouteMajor route = RouteMajor.Unknown;
                if (!string.IsNullOrEmpty(drugc) &&
                    ReceptToMedisCodeMap != null &&
                    ReceptToMedisCodeMap.TryGetValue(drugc, out string medis))
                {
                    route = ParseRouteFromMedisCode(medis);
                }

                // 4) スコア算出＋剤型重み
                ComputeScores(daysSinceLast, fills, gapCv, monthCv, coverage, out double chronic, out double acute);

                if (!RouteWeights.TryGetValue(route, out var weight))
                    weight = RouteWeights[RouteMajor.Unknown];

                chronic = Clamp01(chronic * weight.ChronicW);
                acute = Clamp01(acute * weight.AcuteW);

                // 表示用
                string latestStr = dates[dates.Count - 1].ToString("yyyyMMdd", CultureInfo.InvariantCulture);
                MakeLabelAndNote(chronic, acute, daysSinceLast, coverage, out string label, out string note);

                results.Add(new MedRankRow
                {
                    DrugC = drugc,
                    DrugN = drugn,
                    PtID = (int)ptId,
                    ChronicScore = Math.Round(chronic, 4),
                    AcuteScore = Math.Round(acute, 4),
                    LatestDate = latestStr,
                    DaysSinceLast = daysSinceLast,
                    RankLabel = label,
                    Note = note
                });
            }

            return results
                .OrderByDescending(x => x.ChronicScore)
                .ThenByDescending(x => x.AcuteScore)
                .ThenBy(x => x.DrugC) // 安定化
                .ToList();
        }

        // ---- 内部ヘルパ ----

        private static RouteMajor ParseRouteFromMedisCode(string medisCodeRaw)
        {
            if (string.IsNullOrWhiteSpace(medisCodeRaw)) return RouteMajor.Unknown;

            // 英数字のみ抽出
            var list = new List<char>(medisCodeRaw.Length);
            foreach (var ch in medisCodeRaw.Trim())
                if (char.IsLetterOrDigit(ch)) list.Add(ch);
            var s = new string(list.ToArray());

            if (s.Length < 5) return RouteMajor.Unknown;
            char c = s[4];

            if (c == '0' || c == '1' || c == '2' || c == '3') return RouteMajor.Oral;
            if (c == '4' || c == '5' || c == '6') return RouteMajor.Injection;
            if (c == '7' || c == '8' || c == '9') return RouteMajor.ExternalOrDental;

            return RouteMajor.Unknown;
        }

        private static void ComputeScores(int daysSinceLast, int fills, double? gapCv, double? monthCv, double coverage,
                                          out double chronic, out double acute)
        {
            // 規則性（低いほど良い）→ “良さ”に変換
            double regGap = 1.0 - Math.Min(1.0, (gapCv.HasValue ? (gapCv.Value / GAP_CV_MAX) : 1.0));
            double regMon = 1.0 - Math.Min(1.0, (monthCv.HasValue ? (monthCv.Value / MONTH_CV_MAX) : 1.0));
            double regularity = 0.65 * regGap + 0.35 * regMon;

            // 直近性（近いほど高い）
            double recency = 1.0 / (1.0 + (double)Math.Max(0, daysSinceLast) / 30.0); // 30日で≈0.5

            // フィル回数（多い=慢性寄り）
            double fillsScore = Math.Min(1.0, (double)fills / 3.0);

            // 慢性：規則性＋カバレッジ＋直近
            chronic = 0.5 * regularity + 0.35 * coverage + 0.15 * recency;

            // 急性：直近＋不規則＋低カバレッジ
            acute = 0.60 * recency + 0.25 * (1.0 - regularity) + 0.15 * (1.0 - coverage);

            // フィルが多いと急性はやや抑制
            if (fillsScore >= 0.67) acute *= 0.85;
        }

        private static double? ComputeGapCv(List<DateTime> dates)
        {
            if (dates == null || dates.Count < 3) return null;
            var gaps = new List<int>();
            for (int i = 1; i < dates.Count; i++)
                gaps.Add((int)(dates[i] - dates[i - 1]).TotalDays);

            double mu = gaps.Average();
            if (mu <= 0) return null;

            double sum = 0.0;
            for (int i = 0; i < gaps.Count; i++)
            {
                double d = gaps[i] - mu;
                sum += d * d;
            }
            double sd = Math.Sqrt(sum / gaps.Count);
            return sd / mu;
        }

        private static double? ComputeMonthCv(List<DateTime> dates)
        {
            if (dates == null || dates.Count == 0) return null;

            var countsByMonth = dates
                .GroupBy(d => new DateTime(d.Year, d.Month, 1))
                .Select(g => g.Count())
                .ToList();

            if (countsByMonth.Count <= 1) return null;

            double mu = countsByMonth.Average();
            if (mu <= 0) return null;

            double sum = 0.0;
            for (int i = 0; i < countsByMonth.Count; i++)
            {
                double d = countsByMonth[i] - mu;
                sum += d * d;
            }
            double sd = Math.Sqrt(sum / countsByMonth.Count);
            return sd / mu;
        }

        private static double ComputeCoverageMonths(List<DateTime> dates, DateTime start, DateTime end)
        {
            if (dates == null || dates.Count == 0) return 0.0;

            var monthsWithFill = dates
                .Select(d => new DateTime(d.Year, d.Month, 1))
                .Distinct()
                .Count();

            int totalMonths = ((end.Year - start.Year) * 12 + (end.Month - start.Month)) + 1;
            if (totalMonths <= 0) totalMonths = 1;

            double ratio = (double)monthsWithFill / (double)totalMonths;
            if (ratio < 0.0) ratio = 0.0;
            if (ratio > 1.0) ratio = 1.0;
            return ratio;
        }

        private static double Clamp01(double v)
            => v < 0 ? 0 : (v > 1 ? 1 : v);

        // yj_code から成分キーを作る（英数字のみ、先頭N桁）
        public static string GetIngredientKey(string yj)
        {
            if (string.IsNullOrWhiteSpace(yj)) return "";
            var s = new string(yj.Where(char.IsLetterOrDigit).ToArray());
            if (s.Length == 0) return "";
            int n = Math.Min(YJ_ING_PREFIX_LEN, s.Length);
            return s.Substring(0, n);
        }

        //重複畳み込み関数（成分×agent×区分で1行に集約）
        public static DataTable CollapseDuplicatesByIngredientAgentCategory(DataTable src)
        {
            if (src == null) return null;

            // 必要列の存在チェック
            var ingCol = src.Columns.Contains("_ingKey") ? "_ingKey"
                      : src.Columns.Contains("yj_code") ? "yj_code" : null;
            if (ingCol == null || !src.Columns.Contains("相互作用薬") || !src.Columns.Contains("内容"))
                return src; // 足りなければ何もしない

            var dst = src.Clone(); // スキーマはそのまま

            var groups = src.AsEnumerable().GroupBy(r => new
            {
                IngKey = r[ingCol] == DBNull.Value ? "" : r[ingCol].ToString(),
                Agent = r["相互作用薬"] == DBNull.Value ? "" : r["相互作用薬"].ToString()
            });

            foreach (var g in groups)
            {
                // 代表行：内容が最長 → 同長なら _type に「禁忌」を含む方を優先
                var rep = g.OrderByDescending(r =>
                {
                    var s = r["内容"] == DBNull.Value ? "" : r["内容"].ToString();
                    return s.Length;
                })
                           .ThenBy(r =>
                           {
                               if (!src.Columns.Contains("_type")) return 1; // 無いなら後順位
                               var t = r["_type"] == DBNull.Value ? "" : r["_type"].ToString();
                               return t.Contains("禁忌") ? 0 : 1; // 禁忌を優先
                           })
                           .First();

                var nr = dst.NewRow();
                // 代表行の全列コピー
                foreach (DataColumn c in src.Columns)
                    nr[c.ColumnName] = rep[c.ColumnName];

                // キー列はグループキーで上書き（代表行側が空や表記ブレでも安定化）
                nr[ingCol] = g.Key.IngKey;
                nr["相互作用薬"] = g.Key.Agent;

                // _prio がある場合はグループ内の最小を採用（任意）
                if (dst.Columns.Contains("_prio"))
                    nr["_prio"] = g.Min(r => r.Field<int?>("_prio") ?? int.MaxValue);

                dst.Rows.Add(nr);
            }

            return dst;
        }


        /// <summary>
        /// LLM向けプロンプトを生成。
        /// attachKnowledge=false: sgmlを使わず、薬名＋スコアの簡易JSONのみ
        /// attachKnowledge=true : sgml_rawdataの薬効・効能も付与
        /// 依存：GetMedRanksAsync（既存）
        /// </summary>
        public static async Task<string> MakeLLMPrompt(long ptId, DataTable dtTemplate)
        {
            if (dtTemplate == null || dtTemplate.Rows.Count == 0)
                throw new ArgumentException("テンプレートがありません（dtTemplate が空）");

            var row = dtTemplate.Rows[0];

            // 1) テンプレ本文（ペイロード手前）
            string promptHeader = row.Table.Columns.Contains("prompt") && row["prompt"] != DBNull.Value
                ? row["prompt"].ToString()
                : "以下のJSONを基に要約してください。";

            // 2) options_json を読む（フラット）
            var ser = new JavaScriptSerializer();
            var optsJson = row.Table.Columns.Contains("options_json") && row["options_json"] != DBNull.Value
                ? row["options_json"].ToString()
                : "{}";

            var opts = SafeDeserializeDict(optsJson);

            // オプション値
            int months = GetOpt(opts, "months", 6);
            bool excludeMyOrg = GetOpt(opts, "excludeMyOrg", false);
            double chronicThreshold = GetOpt(opts, "chronicThreshold", 0.6);
            double acuteThreshold = GetOpt(opts, "acuteThreshold", 0.6);

            int maxMeds = GetOpt(opts, "maxMeds", 15);
            bool includeThera = GetOpt(opts, "includeThera", true);
            bool includeIndi = GetOpt(opts, "includeIndications", true);
            int maxIndi = GetOpt(opts, "maxIndications", 6);
            int indiMaxChars = GetOpt(opts, "indicationMaxChars", 60);
            bool includeDrugC = GetOpt(opts, "includeDrugC", false);

            // 3) ランク取得
            int? exVal = excludeMyOrg ? 1 : (int?)null;
            var ranks = await GetMedRanksAsync(ptId, months, exVal);

            // 4) 抽出（閾値 + 上位）
            const int topChronic = 8;
            const int topAcute = 8;

            var chronicTop = ranks.OrderByDescending(x => x.ChronicScore).ThenByDescending(x => x.AcuteScore).Take(topChronic);
            var acuteTop = ranks.OrderByDescending(x => x.AcuteScore).ThenByDescending(x => x.ChronicScore).Take(topAcute);
            var chronicThr = ranks.Where(x => x.ChronicScore >= chronicThreshold);
            var acuteThr = ranks.Where(x => x.AcuteScore >= acuteThreshold);

            var selected = chronicTop.Concat(acuteTop).Concat(chronicThr).Concat(acuteThr)
                                     .GroupBy(x => x.DrugC ?? "")
                                     .Select(g => g.First())
                                     .OrderByDescending(x => x.ChronicScore)
                                     .ThenByDescending(x => x.AcuteScore)
                                     .Take(maxMeds) // ← ここで件数制限
                                     .ToList();

            // 5) まずは常にリンクから薬効/効能を取る（シンプル化）
            var drugcList = selected.Select(s => s.DrugC ?? "")
                                    .Where(s => !string.IsNullOrWhiteSpace(s))
                                    .Distinct()
                                    .ToList();
            // 取得自体は常に行い、出力時に含める/含めないを切替
            var infoMap = await FetchTheraAndIndicationsByDrugCAsync(drugcList, maxIndicationsPerDrug: Math.Max(1, maxIndi));

            // 6) ペイロードを Dictionary で動的構築（条件でキーを入れたり外したりできる）
            var meds = new List<Dictionary<string, object>>();
            foreach (var s in selected)
            {
                var med = new Dictionary<string, object>
                {
                    ["drugn"] = s.DrugN ?? "",
                    ["latest"] = s.LatestDate,
                    ["chronic"] = s.ChronicScore,
                    ["acute"] = s.AcuteScore,
                    ["days_since_last"] = s.DaysSinceLast,
                    ["rank_label"] = s.RankLabel
                };

                if (includeDrugC && !string.IsNullOrWhiteSpace(s.DrugC))
                    med["drugc"] = s.DrugC;

                if (infoMap.TryGetValue(s.DrugC ?? "", out var info) && info != null)
                {
                    if (includeThera && !string.IsNullOrWhiteSpace(info.TherapeuticClassJa))
                        med["therapeutic_class"] = info.TherapeuticClassJa;

                    if (includeIndi && info.Indications != null && info.Indications.Count > 0)
                    {
                        // 文字数丸め＆件数制限（最大 maxIndi・1項目 indiMaxChars まで）
                        var trimmed = info.Indications
                                          .Where(x => !string.IsNullOrWhiteSpace(x))
                                          .Select(x => TrimTo(x.Trim(), indiMaxChars))
                                          .Distinct()
                                          .Take(maxIndi)
                                          .ToList();
                        if (trimmed.Count > 0)
                            med["indications"] = trimmed;
                    }
                }

                meds.Add(med);
            }

            var payload = new Dictionary<string, object>
            {
                ["patient_id"] = ptId,
                ["months"] = months,
                ["medications"] = meds
            };

            string payloadJson = ser.Serialize(payload);

            // 7) 出力：テンプレ本文 + JSON ペイロード
            var prompt =
        $@"{promptHeader}
--- JSON PAYLOAD START ---
{payloadJson}
--- JSON PAYLOAD END ---";

            return NormalizeJaWhitespace(prompt);
        }

        // ========= ヘルパ =========

        private static Dictionary<string, object> SafeDeserializeDict(string json)
        {
            var ser = new JavaScriptSerializer();
            try
            {
                return ser.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
            }
            catch { return new Dictionary<string, object>(); }
        }

        private static T GetOpt<T>(Dictionary<string, object> dict, string key, T def)
        {
            if (dict == null || !dict.ContainsKey(key) || dict[key] == null) return def;
            try
            {
                object v = dict[key];
                if (v is T t) return t;
                var ty = typeof(T);
                if (ty == typeof(int)) return (T)(object)Convert.ToInt32(v);
                if (ty == typeof(double)) return (T)(object)Convert.ToDouble(v);
                if (ty == typeof(bool)) return (T)(object)Convert.ToBoolean(v);
                if (ty == typeof(string)) return (T)(object)Convert.ToString(v);
                return (T)Convert.ChangeType(v, ty);
            }
            catch { return def; }
        }

        private static string TrimTo(string s, int maxChars)
        {
            if (string.IsNullOrEmpty(s)) return s;
            if (maxChars <= 0) return "";
            return s.Length <= maxChars ? s : s.Substring(0, maxChars) + "…";
        }

        // --- 内部ヘルパ: sgml_rawdata 付帯情報 ---
        public class LiteInfo
        {
            // 基本情報
            public string TherapeuticClassJa { get; set; }
            public List<string> Indications { get; set; }

            // 将来拡張: 相互作用関連
            public List<string> Contraindications { get; set; }    // 禁忌
            public List<string> Precautions { get; set; }          // 注意
            public List<string> Interactions { get; set; }         // その他の相互作用

            // 将来拡張: 副作用なども入れる余地
            public List<string> AdverseEvents { get; set; }

            public LiteInfo()
            {
                Indications = new List<string>();
                Contraindications = new List<string>();
                Precautions = new List<string>();
                Interactions = new List<string>();
                AdverseEvents = new List<string>();
            }
        }


        /// <summary>
        /// drugc のリストをもとに sgml_rawdata から薬効分類と効能を取得して返す
        /// key = drugc, value = LiteInfo(薬効分類 + 効能リスト)
        /// </summary>
        public static async Task<Dictionary<string, LiteInfo>> FetchTheraAndIndicationsByDrugCAsync(
        List<string> drugcList, int maxIndicationsPerDrug = 12)
        {
            var result = new Dictionary<string, LiteInfo>();
            if (drugcList == null || drugcList.Count == 0) return result;

            using (var conn = (NpgsqlConnection)GetDbConnection())
            {
                await conn.OpenAsync();

                string sql = @"
                    WITH input AS (
                      SELECT unnest(@drugcs::text[]) AS drugc
                    ),
                    map AS (
                      SELECT i.drugc, dcm.yj_code, left(dcm.yj_code, 7) AS yj7
                      FROM input i
                      LEFT JOIN drug_code_map dcm ON dcm.drugc = i.drugc
                    ),
                    candidates AS (
                      SELECT
                        m.drugc,
                        sr.yj_code AS sr_yj_code,
                        m.yj_code  AS map_yj_code,
                        sr.therapeutic_class_ja,
                        sr.indications_json,
                        sr.start_marketing,
                        sr.updated_at
                      FROM map m
                      JOIN sgml_rawdata sr
                        ON left(sr.yj_code,7) = m.yj7
                    ),
                    ranked AS (
                      SELECT *,
                        ROW_NUMBER() OVER (
                          PARTITION BY drugc
                          ORDER BY
                            (start_marketing IS NULL), start_marketing ASC,
                            (CASE WHEN sr_yj_code = map_yj_code THEN 0 ELSE 1 END),
                            updated_at DESC
                        ) AS rn
                      FROM candidates
                    )
                    SELECT drugc, therapeutic_class_ja, indications_json
                    FROM ranked
                    WHERE rn = 1;
                    ";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.Add(new NpgsqlParameter<string[]>("drugcs", NpgsqlDbType.Array | NpgsqlDbType.Text)
                    {
                        TypedValue = drugcList.Distinct().ToArray()
                    });

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string drugc = reader.GetString(reader.GetOrdinal("drugc"));

                            var info = new LiteInfo
                            {
                                TherapeuticClassJa = reader.IsDBNull(reader.GetOrdinal("therapeutic_class_ja"))
                                    ? string.Empty
                                    : reader.GetString(reader.GetOrdinal("therapeutic_class_ja")).Trim(),
                                Indications = new List<string>()
                            };

                            string rawJson = reader.IsDBNull(reader.GetOrdinal("indications_json"))
                                ? "[]"
                                : reader.GetString(reader.GetOrdinal("indications_json"));

                            info.Indications = ParseJsonStringArray(rawJson, maxIndicationsPerDrug);
                            result[drugc] = info;
                        }
                    }
                }
            }

            return result;
        }

        private static string NormalizeJaWhitespace(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            // 全角スペース→半角、連続空白/改行/タブを1個に、前後trim
            s = s.Replace('\u3000', ' ');
            s = Regex.Replace(s, @"\s+", " ");
            return s.Trim();
        }

        private static List<string> ParseJsonStringArray(string rawJson, int max = 10)
        {
            var list = new List<string>();
            try
            {
                var ser = new JavaScriptSerializer();
                var root = ser.DeserializeObject(rawJson);

                var stack = new Stack<object>();
                stack.Push(root);

                while (stack.Count > 0)
                {
                    var cur = stack.Pop();
                    if (cur is Dictionary<string, object> dict)
                    {
                        // よく使われるキー
                        foreach (var k in new[] { "text", "title", "label", "value" })
                        {
                            if (dict.ContainsKey(k) && dict[k] is string s && !string.IsNullOrWhiteSpace(s))
                                list.Add(Normalize(s));
                        }
                        if (dict.ContainsKey("children") && dict["children"] is object[] arr)
                            foreach (var c in arr) stack.Push(c);
                    }
                    else if (cur is object[] arr)
                    {
                        foreach (var c in arr) stack.Push(c);
                    }
                    else if (cur is string s2)
                    {
                        if (!string.IsNullOrWhiteSpace(s2)) list.Add(Normalize(s2));
                    }
                }
            }
            catch
            {
                // JSONでなければテキスト分割
                list.AddRange(Regex.Split(rawJson ?? "", @"[、。\n\r;；]+")
                                   .Select(Normalize)
                                   .Where(x => x.Length > 0));
            }

            return list.Distinct().Take(max).ToList();
        }
        private static string Normalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var noHtml = Regex.Replace(s, "<.*?>", "");
            var half = noHtml.Replace('\u3000', ' ');
            return Regex.Replace(half, @"\s{2,}", " ").Trim();
        }

        // ---- 5) LLM呼び出し（Ollamaなどの汎用HTTP）----
        // serverUrl: 例 "http://localhost:11434/api/generate" (Ollama)
        public static async Task<string> CallLlmAsync(
            string serverUrl,
            string modelName,
            string prompt,
            int timeoutMs = 120000,
            CancellationToken ct = default(CancellationToken),
            Action<string> onStatus = null)   // ← 進捗やデバッグ文字列をUIに出すなら使う
        {
            // URLの補正（よくある間違い対策）
            if (string.IsNullOrWhiteSpace(serverUrl))
                throw new ArgumentException("serverUrl が空です。例: http://localhost:11434/api/generate");

            // 末尾が /api/generate でなければ補う（http://host:11434 → http://host:11434/api/generate）
            if (!serverUrl.EndsWith("/api/generate", StringComparison.OrdinalIgnoreCase))
            {
                serverUrl = serverUrl.TrimEnd('/');
                if (!serverUrl.EndsWith("/api"))
                    serverUrl += "/api";
                serverUrl += "/generate";
            }

            onStatus?.Invoke($"[CallLlmAsync] URL={serverUrl}");

            var payload = new Dictionary<string, object>
            {
                { "model",  modelName ?? "" },
                { "prompt", prompt    ?? "" },
                { "stream", false }
            };
            var json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(payload);
            onStatus?.Invoke($"[CallLlmAsync] payload {json.Length} chars");

            try
            {
                using (var http = new HttpClient() { Timeout = TimeSpan.FromMilliseconds(timeoutMs) })
                using (var req = new StringContent(json, Encoding.UTF8, "application/json"))
                using (var resp = await http.PostAsync(serverUrl, req, ct))
                {
                    string body = await resp.Content.ReadAsStringAsync();

                    // 非200系は詳細を投げる（本文は長いので先頭だけ）
                    if (!resp.IsSuccessStatusCode)
                    {
                        var head = body == null ? "" : (body.Length > 500 ? body.Substring(0, 500) + "..." : body);
                        var msg =
                            $"LLM HTTP {(int)resp.StatusCode} {resp.StatusCode} (POST {serverUrl})\n" +
                            $"Request-Model: {modelName}\n" +
                            $"Request-Len: {json.Length}\n" +
                            $"Response-Head: {head}";
                        onStatus?.Invoke("[CallLlmAsync] ERROR " + msg.Replace("\n", " | "));
                        throw new HttpRequestException(msg);
                    }

                    // Ollama {"response":"...","done":true,...}
                    try
                    {
                        var dict = new System.Web.Script.Serialization.JavaScriptSerializer()
                                   .Deserialize<Dictionary<string, object>>(body);
                        if (dict != null && dict.TryGetValue("response", out var o) && o is string res)
                        {
                            onStatus?.Invoke($"[CallLlmAsync] OK response {res.Length} chars");
                            return res.Trim();
                        }
                    }
                    catch (Exception jx)
                    {
                        // JSONパースに失敗 → 生の本文を返す（後段で扱うため）
                        onStatus?.Invoke("[CallLlmAsync] WARN JSON parse failed: " + jx.Message);
                    }

                    onStatus?.Invoke($"[CallLlmAsync] OK (raw) {body?.Length ?? 0} chars");
                    return body;
                }
            }
            catch (TaskCanceledException tex)
            {
                // Timeout or user-cancel
                var msg = tex.CancellationToken.IsCancellationRequested
                    ? "LLM リクエストがキャンセルされました。"
                    : $"LLM タイムアウト（{timeoutMs}ms）: {tex.Message}";
                onStatus?.Invoke("[CallLlmAsync] " + msg);
                throw new TimeoutException(msg, tex);
            }
            catch (HttpRequestException hex)
            {
                // ネットワーク/HTTPレイヤーの例外
                onStatus?.Invoke("[CallLlmAsync] HttpRequestException: " + hex.Message);
                throw;
            }
            catch (Exception ex)
            {
                // それ以外
                onStatus?.Invoke("[CallLlmAsync] Exception: " + ex.Message);
                throw;
            }
        }


        // ── 1回分の問い合わせ（DB更新まで一括） ──
        public static async Task<long> RunLlmOnceAndPersistAsync(long ptId, string prompt, string tplName ,string modelName, int? timeoutMsOverride = null)
        {
            // 設定からURL組み立て
            var server = $"http://{Properties.Settings.Default.LLMserver}";  // 例: "http://localhost"
            var port = Properties.Settings.Default.LLMport;    // 例: 11434
            //var model = Properties.Settings.Default.LLMmodel;   // 例: "gemma3:4b"
            var tout = timeoutMsOverride ?? Properties.Settings.Default.LLMtimeout * 1000; // ms

            // Ollama想定（/api/generate）。他APIなら適宜差し替え。
            string serverUrl = $"{server}:{port}/api/generate";

            
            // 1) pending 挿入（長さ/トークンも記録）
            long rid = await InsertAiResultPendingAsync(ptId, prompt, serverUrl,tplName, modelName);

            try
            {
                // 2) 送信
                //var res = await CallLlmAsync(serverUrl, modelName, prompt, tout);
                string res = await CallLlmQueuedAsync(
                                serverUrl, modelName, prompt,
                                timeoutMs: 120000,
                                ct: CancellationToken.None
                                );

                // 3) 成功更新（長さ/トークンも記録）
                await UpdateAiResultSuccessAsync(rid, res);
            }
            catch (Exception ex)
            {
                await UpdateAiResultErrorAsync(rid, ex.Message);
                throw; // UI側でトースト/MessageBoxしたければ投げ直す
            }

            return rid;
        }

        // ========== LLM キュー管理部 ==========
        private sealed class LlmJob
        {
            public long Id;
            public string ServerUrl;
            public string ModelName;
            public string Prompt;
            public int TimeoutMs;
            public CancellationToken Ct;
            public Action<string> OnStatus;
            public TaskCompletionSource<string> Tcs;
        }

        private static readonly ConcurrentQueue<LlmJob> _llmQueue = new ConcurrentQueue<LlmJob>();
        private static readonly SemaphoreSlim _llmSignal = new SemaphoreSlim(0, int.MaxValue);
        private static int _llmRunnerStarted = 0;
        private static long _llmIdSeq = 0;

        /// <summary>
        /// 現在のキュー待ち件数を返す
        /// </summary>
        public static int CurrentQueueCount => _llmQueue.Count;

        /// <summary>
        /// 直列キュー越しに LLM を呼び出します（FIFO・同時実行数1）。
        /// 戻り値・例外の投げ方は CallLlmAsync と同じ。
        /// </summary>
        public static Task<string> CallLlmQueuedAsync(
            string serverUrl,
            string modelName,
            string prompt,
            int timeoutMs = 120000,
            CancellationToken ct = default(CancellationToken),
            Action<string> onStatus = null)
        {
            // 事前キャンセルは即キャンセル返し
            if (ct.IsCancellationRequested)
                return Task.FromCanceled<string>(ct);

            var job = new LlmJob
            {
                Id = Interlocked.Increment(ref _llmIdSeq),
                ServerUrl = serverUrl,
                ModelName = modelName,
                Prompt = prompt,
                TimeoutMs = timeoutMs,
                Ct = ct,
                OnStatus = onStatus,
                Tcs = new TaskCompletionSource<string>()
            };

            _llmQueue.Enqueue(job);
            _llmSignal.Release();

            // ランナー起動（最初の一回だけ）
            if (Interlocked.CompareExchange(ref _llmRunnerStarted, 1, 0) == 0)
            {
                Task.Run(LlmProcessLoop);
            }

            return job.Tcs.Task;
        }

        /// <summary>
        /// キューのジョブを順次処理するループ
        /// </summary>
        private static async Task LlmProcessLoop()
        {
            while (true)
            {
                try
                {
                    await _llmSignal.WaitAsync().ConfigureAwait(false);

                    while (_llmQueue.TryDequeue(out var job))
                    {
                        if (job.Ct.IsCancellationRequested)
                        {
                            job.Tcs.TrySetCanceled(job.Ct);
                            continue;
                        }

                        Action<string> status = null;
                        if (job.OnStatus != null)
                        {
                            status = s =>
                            {
                                try { job.OnStatus($"[Queue#{job.Id}] {s}"); }
                                catch { /* UI 側で握りつぶす */ }
                            };
                        }

                        try
                        {
                            status?.Invoke("dequeued → processing...");
                            // 既存の CallLlmAsync を利用
                            var res = await CallLlmAsync(
                                job.ServerUrl, job.ModelName, job.Prompt,
                                job.TimeoutMs, job.Ct, status
                            ).ConfigureAwait(false);

                            job.Tcs.TrySetResult(res);
                            status?.Invoke($"done (len={res?.Length ?? 0})");
                        }
                        catch (OperationCanceledException oce)
                        {
                            job.Tcs.TrySetCanceled(job.Ct.IsCancellationRequested ? job.Ct : new CancellationToken(true));
                            status?.Invoke($"canceled: {oce.Message}");
                        }
                        catch (Exception ex)
                        {
                            job.Tcs.TrySetException(ex);
                            status?.Invoke($"error: {ex.Message}");
                        }
                    }
                }
                catch
                {
                    // ランナーが落ちないよう握り潰す（必要ならログ）
                }
            }
        }


        /// <summary>
        /// 自動 LLM 問い合わせ（auto_fetch=TRUE のテンプレート全件対象）。
        /// 同一タイトルの ai_results が <paramref name="minDaysBetween"/> 日以内に存在する場合はスキップします。
        /// 1件も実行しなければ null、実行した場合は最後の rid を返します。
        /// </summary>
        public static async Task<long?> AutoLLMAsync(
            long ptId,
            int? timeoutMsOverride = null,
            int minDaysBetween = 1,               // 例: 1なら「同じタイトルが1日以内にあれば再取得しない」
            CancellationToken ct = default)
        {
            try
            {
                await AddLogAsync($"[AutoFetch] start ptId={ptId}, minDaysBetween={minDaysBetween}");

                // 1) auto_fetch テンプレート全件を取得
                DataTable dtTpl;
                using (var conn = (NpgsqlConnection)GetDbConnection(true))
                {
                    await conn.OpenAsync();

                    const string sqlAuto = @"
                    SELECT id, tpl_name, model_name, auto_fetch, prompt, prompt_len, updated_at, options_json
                      FROM public.ai_prompt_tpl
                     WHERE auto_fetch = TRUE
                     ORDER BY id;";

                    using (var cmd = new NpgsqlCommand(sqlAuto, conn))
                    using (var r = await cmd.ExecuteReaderAsync(ct))
                    {
                        dtTpl = new DataTable();
                        dtTpl.Load(r);
                    }
                }

                if (dtTpl.Rows.Count == 0)
                {
                    await AddLogAsync("[AutoFetch] auto_fetch=TRUE のテンプレートが見つかりません。処理を中止します。");
                    return null;
                }

                long? lastRid = null;
                int executed = 0, skipped = 0;

                // 2) 各テンプレートを順に処理
                foreach (DataRow row in dtTpl.Rows)
                {
                    ct.ThrowIfCancellationRequested();

                    string tplName = row["tpl_name"]?.ToString() ?? "(no title)";
                    string modelName = row["model_name"]?.ToString() ?? "";

                    // 2-1) 直近 minDaysBetween 日以内に同タイトルの結果があればスキップ
                    if (minDaysBetween > 0)
                    {
                        bool existsRecent = await ExistsRecentResultAsync(ptId, tplName, minDaysBetween, ct);
                        if (existsRecent)
                        {
                            skipped++;
                            await AddLogAsync($"[AutoFetch] skip '{tplName}'（{minDaysBetween}日以内に既存あり）");
                            continue;
                        }
                    }

                    await AddLogAsync($"[AutoFetch] template='{tplName}' model='{modelName}' → prompt生成");

                    // 2-2) MakeLLMPrompt は 1行の DataTable を想定しているので複製して渡す
                    var dtOne = row.Table.Clone();
                    dtOne.ImportRow(row);

                    string prompt = await MakeLLMPrompt(ptId, dtOne);
                    if (string.IsNullOrWhiteSpace(prompt))
                    {
                        await AddLogAsync($"[AutoFetch] 生成プロンプトが空でした（tpl='{tplName}'）。スキップ");
                        skipped++;
                        continue;
                    }

                    // 2-3) 実行（内部は直列キューで順次処理）
                    long rid = await RunLlmOnceAndPersistAsync(ptId, prompt, tplName, modelName, timeoutMsOverride);
                    lastRid = rid;
                    executed++;

                    await AddLogAsync($"[AutoFetch] done tpl='{tplName}' rid={rid}");
                }

                await AddLogAsync($"[AutoFetch] 完了 executed={executed}, skipped={skipped}");
                return lastRid;
            }
            catch (OperationCanceledException)
            {
                await AddLogAsync("[AutoFetch] キャンセルされました。");
                return null;
            }
            catch (Exception ex)
            {
                await AddLogAsync("[AutoFetch] ERROR " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// ai_results に、同じ ptId・同じ title のレコードが
        /// 直近 days 日以内に存在するかを判定します。
        /// </summary>
        private static async Task<bool> ExistsRecentResultAsync(
            long ptId, string title, int days, CancellationToken ct)
        {
            using (var conn = (NpgsqlConnection)GetDbConnection(true))
            {
                await conn.OpenAsync(ct);

                const string sql = @"
                SELECT 1
                  FROM public.ai_results
                 WHERE ptidmain = @pt
                   AND title    = @title
                   AND COALESCE(res_at, req_at) >= (NOW() - make_interval(days => @days))
                 LIMIT 1;";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@pt", ptId);
                    cmd.Parameters.AddWithValue("@title", title ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@days", days);

                    var obj = await cmd.ExecuteScalarAsync(ct);
                    return obj != null; // 1行でもあれば true
                }
            }
        }



        // ── 最新結果の取得（UI表示用の簡易ヘルパ） ──
        public static async Task<DataTable> LoadLatestAiResultToUIAsync(long ptId, int months)
        {
            var dt = new DataTable();

            try
            {
                using (var conn = (Npgsql.NpgsqlConnection)GetDbConnection())
                {
                    await conn.OpenAsync();

                    var sql = @"
                SELECT id, ptidmain, prompt, res, model_name, res_at, res_len_chars, status, error_msg, title
                  FROM ai_results
                 WHERE ptidmain = @ptid
                   AND res_at  >= @fromDate
                 ORDER BY id DESC;";

                    using (var cmd = new Npgsql.NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@ptid", ptId);

                        // monthsヶ月前を計算 (UTC基準で保存されている想定)
                        var fromDate = DateTime.UtcNow.AddMonths(-months);
                        cmd.Parameters.AddWithValue("@fromDate", fromDate);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            dt.Load(reader);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // エラー時に例外情報を格納して返す（UI表示用）
                dt = new DataTable();
                dt.Columns.Add("error", typeof(string));
                dt.Rows.Add(ex.Message);
            }

            return dt;
        }

        //
        // LLM記録用
        //

        public static async Task EnsureAiResultsTableAsync(int openTimeoutMs = 8000, int commandTimeoutSec = 30)
        {
            const string createSql = @"
                CREATE TABLE IF NOT EXISTS public.ai_results (
                  id                 BIGSERIAL PRIMARY KEY,
                  title              VARCHAR(255),
                  ptidmain           BIGINT      NOT NULL,
                  prompt             TEXT        NOT NULL,
                  server_url         TEXT        NOT NULL,
                  model_name         TEXT,
                  req_at             TIMESTAMPTZ NOT NULL DEFAULT now(),
                  prompt_len_chars   INTEGER,
                  prompt_tokens_est  INTEGER,
                  res                TEXT,
                  res_at             TIMESTAMPTZ,
                  res_len_chars      INTEGER,
                  res_tokens_est     INTEGER,
                  status             TEXT        NOT NULL DEFAULT 'pending',
                  error_msg          TEXT
                );
                CREATE INDEX IF NOT EXISTS idx_ai_results_ptidmain ON public.ai_results(ptidmain);
                CREATE INDEX IF NOT EXISTS idx_ai_results_req_at   ON public.ai_results(req_at);
                CREATE INDEX IF NOT EXISTS idx_ai_results_status   ON public.ai_results(status);
                ";

            string[] alterSqls =
            {
                "ALTER TABLE public.ai_results ADD COLUMN IF NOT EXISTS title             VARCHAR(255);",
                "ALTER TABLE public.ai_results ADD COLUMN IF NOT EXISTS prompt_len_chars  INTEGER;",
                "ALTER TABLE public.ai_results ADD COLUMN IF NOT EXISTS prompt_tokens_est INTEGER;",
                "ALTER TABLE public.ai_results ADD COLUMN IF NOT EXISTS res_len_chars     INTEGER;",
                "ALTER TABLE public.ai_results ADD COLUMN IF NOT EXISTS res_tokens_est    INTEGER;"
            };

            try
            {
                using (var conn = (NpgsqlConnection)CommonFunctions.GetDbConnection(true))
                {
                    // 明示タイムアウト付きで接続
                    using (var cts = new CancellationTokenSource(openTimeoutMs))
                    {
                        try
                        {
                            await conn.OpenAsync(cts.Token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            await AddLogAsync($"[EnsureAiResultsTableAsync] 接続タイムアウト（{openTimeoutMs}ms）").ConfigureAwait(false);
                            return; // ここで終了（落とさない）
                        }
                        catch (Exception ex)
                        {
                            await AddLogAsync($"[EnsureAiResultsTableAsync] 接続失敗: {ex.GetType().Name} / {ex.Message}").ConfigureAwait(false);
                            return;
                        }
                    }

                    // CREATE TABLE & INDEX
                    try
                    {
                        using (var cmd = new NpgsqlCommand(createSql, conn))
                        {
                            cmd.CommandTimeout = commandTimeoutSec;
                            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        await AddLogAsync($"[EnsureAiResultsTableAsync] CREATE/INDEX 失敗: {ex.GetType().Name} / {ex.Message}").ConfigureAwait(false);
                        // スキーマ未整備のままでもアプリ継続したいので return
                        return;
                    }

                    // ALTER TABLE は個別実行（失敗しても続行）
                    foreach (var sql in alterSqls)
                    {
                        try
                        {
                            using (var cmd = new NpgsqlCommand(sql, conn))
                            {
                                cmd.CommandTimeout = commandTimeoutSec;
                                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                            }
                        }
                        catch (Exception ex)
                        {
                            await AddLogAsync($"[EnsureAiResultsTableAsync][ALTER skipped] {ex.GetType().Name}: {ex.Message}").ConfigureAwait(false);
                            // 続行
                        }
                    }
                }

                await AddLogAsync("[EnsureAiResultsTableAsync] スキーマ確認OK").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // ここに来るのは稀（上で握れていない想定外の例外）
                await AddLogAsync($"[EnsureAiResultsTableAsync][Unhandled] {ex.GetType().Name}: {ex.Message}").ConfigureAwait(false);
                // throw しない：アプリ継続
            }
        }


        // ── 概算トークン数（ざっくり：全角半角混在でも平均4文字=1token目安） ──
        private static int EstimateTokens(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            var len = text.Length;
            // 句読点や余分空白を軽く削ると安定（任意）
            return Math.Max(1, (int)Math.Ceiling(len / 4.0));
        }

        // ── req: pendingレコード作成（文字数/トークン数も保存） ──
        public static async Task<long> InsertAiResultPendingAsync(long ptId, string prompt, string serverUrl, string tplName, string modelName)
        {
            const string sql = @"
            INSERT INTO public.ai_results
              (ptidmain, prompt, server_url, model_name, status, prompt_len_chars, prompt_tokens_est, title)
            VALUES
              (@p1, @p2, @p3, @p4, 'pending', @len, @tok, @title)
            RETURNING id;";

            int len = string.IsNullOrEmpty(prompt) ? 0 : prompt.Length;
            int tok = EstimateTokens(prompt ?? "");

            using (IDbConnection conn = GetDbConnection(false))
            {
                if (conn is DbConnection dbc) await dbc.OpenAsync(); else conn.Open();
                using (var cmd = new NpgsqlCommand(sql, (NpgsqlConnection)conn))
                {
                    cmd.Parameters.AddWithValue("p1", ptId);
                    cmd.Parameters.AddWithValue("p2", prompt ?? "");
                    cmd.Parameters.AddWithValue("p3", serverUrl ?? "");
                    cmd.Parameters.AddWithValue("p4", modelName ?? "");
                    cmd.Parameters.AddWithValue("len", len);
                    cmd.Parameters.AddWithValue("tok", tok);
                    cmd.Parameters.AddWithValue("title", tplName);

                    var obj = await cmd.ExecuteScalarAsync();
                    return Convert.ToInt64(obj);
                }
            }
        }

        // ── res: 成功更新（本文＋長さ＋トークン） ──
        public static async Task UpdateAiResultSuccessAsync(long id, string responseText)
        {
            const string sql = @"
            UPDATE public.ai_results
               SET res = @r,
                   res_at = now(),
                   res_len_chars = @len,
                   res_tokens_est = @tok,
                   status='success',
                   error_msg = NULL
             WHERE id = @id;";

            int len = string.IsNullOrEmpty(responseText) ? 0 : responseText.Length;
            int tok = EstimateTokens(responseText ?? "");

            using (IDbConnection conn = GetDbConnection(false))
            {
                if (conn is DbConnection dbc) await dbc.OpenAsync(); else conn.Open();
                using (var cmd = new NpgsqlCommand(sql, (NpgsqlConnection)conn))
                {
                    cmd.Parameters.AddWithValue("id", id);
                    cmd.Parameters.AddWithValue("r", (object)(responseText ?? "") ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("len", len);
                    cmd.Parameters.AddWithValue("tok", tok);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public static async Task UpdateAiResultErrorAsync(long id, string errorMessage)
        {
            const string sql = @"
            UPDATE public.ai_results
               SET status='error', error_msg = @e, res_at = now()
             WHERE id = @id;";

            using (IDbConnection conn = GetDbConnection(false))
            {
                if (conn is DbConnection dbc) await dbc.OpenAsync(); else conn.Open();
                using (var cmd = new NpgsqlCommand(sql, (NpgsqlConnection)conn))
                {
                    cmd.Parameters.AddWithValue("id", id);
                    cmd.Parameters.AddWithValue("e", errorMessage ?? "");
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        //
        // ollamaのモデルリストを取得
        //
        public class ModelInfo
        {
            public string Name { get; set; }
            public string Digest { get; set; }
            public long Size { get; set; }
            public DateTime ModifiedAt { get; set; }
        }

        public static async Task<List<ModelInfo>> GetOllamaModelsAsync(string serverUrl)
        {
            string url = $"{serverUrl.TrimEnd('/')}/api/tags";
            try
            {

                using (var http = new HttpClient())
                {
                    var resp = await http.GetStringAsync(url);

                    var ser = new JavaScriptSerializer();
                    var rootObj = ser.DeserializeObject(resp);  // 戻り値は object

                    var modelList = new List<ModelInfo>();

                    if (rootObj is Dictionary<string, object> root && root.ContainsKey("models"))
                    {
                        if (root["models"] is object[] arr)
                        {
                            foreach (var item in arr)
                            {
                                if (item is Dictionary<string, object> dict)
                                {
                                    var info = new ModelInfo
                                    {
                                        Name = dict.ContainsKey("name") ? dict["name"].ToString() : "",
                                        Digest = dict.ContainsKey("digest") ? dict["digest"].ToString() : "",
                                        Size = dict.ContainsKey("size") ? Convert.ToInt64(dict["size"]) : 0,
                                        ModifiedAt = dict.ContainsKey("modified_at")
                                            ? DateTime.Parse(dict["modified_at"].ToString())
                                            : DateTime.MinValue
                                    };
                                    modelList.Add(info);
                                }
                            }
                        }
                    }
                    ollamaModelList = modelList;

                    return modelList;
                }
            }
            catch (Exception ex)
            {
                await AddLogAsync($"ollamaモデルの取得に失敗しました。{ex.Message}｝");
                return null;
            }
        }

        //コンボボックスにセットし、既定値を選択する
        public static async Task SetModelsToComboBox(ComboBox cb, List<ModelInfo> modelList, string selectedModel = "")
        {
            cb.DataSource = null;
            // バインド（表示＝Name、値＝Name でOK。Digestを使うなら ValueMember="Digest" に）
            cb.DisplayMember = "Name";
            cb.ValueMember = "Name";
            cb.DataSource = modelList;

            // 既存設定があれば選択
            try
            {
                if (!string.IsNullOrWhiteSpace(selectedModel))
                {
                    int idx = modelList.FindIndex(m => string.Equals(m.Name, selectedModel, StringComparison.OrdinalIgnoreCase));
                    if (idx >= 0) cb.SelectedIndex = idx;
                }
            }
            catch (Exception ex)
            {
                await AddLogAsync("ollamaリスト一覧の設定に失敗しました" + ex.Message);
            }
        }

        /// <summary>
        /// 指定IDのテンプレートを取得して DataTable（1行 or 0行）で返す。
        /// </summary>
        public static async Task<DataTable> GetPromptTemplateByIdAsync(long templateId)
        {
            var dt = new DataTable();

            const string sql = @"
                SELECT id,
                       tpl_name,
                       model_name,
                       auto_fetch,
                       prompt,
                       prompt_len,
                       payload_type,
                       updated_at,
                       options_json
                  FROM public.ai_prompt_tpl
                 WHERE id = @id
                 LIMIT 1;";
            try
            {
                using (var conn = (NpgsqlConnection)CommonFunctions.GetDbConnection(true))
                {
                    await conn.OpenAsync();

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", templateId);

                        using (var r = await cmd.ExecuteReaderAsync())
                        {
                            dt.Load(r); // 0行でもスキーマは入る
                        }
                    }
                }
                return dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"テンプレート取得でエラー：{ex.Message}");
                return null;
            }
            
        }


        // ai_prompt_tpl
        /// <summary>
        /// ai_prompt_tpl を作成（なければ）
        /// </summary>
        public static async Task EnsurePromptTplTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS public.ai_prompt_tpl (
                    id           BIGSERIAL PRIMARY KEY,
                    tpl_name     TEXT        NOT NULL,
                    model_name   TEXT,
                    auto_fetch   BOOLEAN     NOT NULL DEFAULT FALSE,
                    prompt       TEXT,
                    prompt_len   INTEGER,
                    payload_type INTEGER     NOT NULL DEFAULT 0,
                    updated_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    options_json JSONB       NOT NULL DEFAULT '{}'::jsonb
                );
                -- 代表インデックス
                CREATE INDEX IF NOT EXISTS idx_ai_prompt_tpl_name ON public.ai_prompt_tpl (tpl_name);
            ";

            using (var conn = (NpgsqlConnection)CommonFunctions.GetDbConnection(true))
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// ai_prompt_tpl が空ならサンプル1件を挿入して id を返す。既にレコードがあれば null。
        /// </summary>
        public static async Task<long?> InsertSampleTemplateIfEmptyAsync()
        {
            const string countSql = @"SELECT COUNT(*) FROM public.ai_prompt_tpl;";
            const string insertSql = @"
                INSERT INTO public.ai_prompt_tpl
                  (tpl_name, model_name, auto_fetch, prompt, prompt_len, updated_at, options_json)
                VALUES
                  (@tpl_name, @model_name, @auto_fetch, @prompt, @prompt_len, NOW(), CAST(@options_json AS jsonb))
                RETURNING id;";

            // サンプルの初期プロンプト
            string samplePrompt =
        @"以下のJSONを基に患者の病態を1段落で要約してください。

【要件】
- 出力は日本語の文章のみ（JSON/箇条書きは禁止）。
- 慢性：慢性疾患（chronic高 or rank_labelが継続）は薬効ごとにまとめて疾患名を類推し、「◯◯などの薬剤を処方されており、◯◯病(病名)で継続治療中と思われる」と出力。
- 中断：慢性疾患でかつ（days_since_last>=60）は「◯はxx日以上中断している」。
- 急性（acute高 or days_since_last<=30）は「◯が臨時処方」
- 急性で14日以内に投薬があるときはあり→◯が◯日前に投与されており重複に注意」。
- 疾患名は短縮（例：狭心症/心筋梗塞→虚血性心疾患、胃潰瘍/十二指腸潰瘍→消化性潰瘍）。
- 疾患は最大2件、根拠薬は代表1–2剤のみ。1段落3–5文以内。

【入力】";

            string tplName = "病名病態推論データ付";
            string modelName = Properties.Settings.Default.LLMmodel ?? "";
            bool autoFetch = false; // 必要なら true
            int promptLen = samplePrompt.Length;

            // サンプル options_json（将来UIで編集する想定）
            var sampleOptions = new
            {
                excludeMyOrg = true,
                months = 6,
                attachKnowledge = true,
                chronicThreshold = 0.6,
                acuteThreshold = 0.6
            };
            var ser = new System.Web.Script.Serialization.JavaScriptSerializer();
            string optionsJson = ser.Serialize(sampleOptions);

            try
            {
                using (var conn = (NpgsqlConnection)CommonFunctions.GetDbConnection(true))
                {
                    await conn.OpenAsync();

                    // 件数確認
                    using (var cmdCnt = new NpgsqlCommand(countSql, conn))
                    {
                        var count = Convert.ToInt64(await cmdCnt.ExecuteScalarAsync());
                        if (count > 0) return null;
                    }

                    // 空ならサンプルを1件挿入
                    using (var cmdIns = new NpgsqlCommand(insertSql, conn))
                    {
                        cmdIns.Parameters.AddWithValue("@tpl_name", tplName);
                        cmdIns.Parameters.AddWithValue("@model_name", (object)modelName ?? DBNull.Value);
                        cmdIns.Parameters.AddWithValue("@auto_fetch", autoFetch);
                        cmdIns.Parameters.AddWithValue("@prompt", samplePrompt);
                        cmdIns.Parameters.AddWithValue("@prompt_len", promptLen);
                        cmdIns.Parameters.AddWithValue("@options_json", optionsJson);

                        var idObj = await cmdIns.ExecuteScalarAsync();
                        long newId = Convert.ToInt64(idObj);
                        return newId;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"テンプレートのサンプルレコード追加でエラーが発生しました。{ex.Message}");
                return null;
            }
        }

        // 保存用（string → byte[] → DPAPI）
        public static string encodePassword(string plain)
        {
            var bytes = Encoding.UTF8.GetBytes(plain);
            var enc = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            var b64 = Convert.ToBase64String(enc);

            return b64;
        }

        // 復号用
        public static string decodePassword(string b64)
        {
            if (string.IsNullOrEmpty(b64)) return "";

            try
            {
                var enc = Convert.FromBase64String(b64);
                var dec = ProtectedData.Unprotect(enc, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(dec);
            }
            catch
            {
                return "";
            }
        }

        public static void SetInteractionView(DataGridView dgv)
        {
            if (dgv == null || dgv.DataSource == null) return;

            // 基本設定
            dgv.AutoGenerateColumns = true;
            dgv.ReadOnly = true;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.AllowUserToResizeRows = false;
            dgv.AllowUserToResizeColumns = true;
            dgv.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dgv.MultiSelect = false;
            dgv.RowHeadersVisible = false;

            // 列検索ヘルパ
            DataGridViewColumn FindCol(string key)
                => dgv.Columns.Cast<DataGridViewColumn>()
                    .FirstOrDefault(c =>
                        string.Equals(c.Name, key, StringComparison.Ordinal) ||
                        string.Equals(c.DataPropertyName, key, StringComparison.Ordinal) ||
                        string.Equals(c.HeaderText, key, StringComparison.Ordinal));

            // 表示名（必要な列だけ）
            var headers = new Dictionary<string, string>
            {
                { "didate", "処方日" },                  // DIフォーム側だけに出したければ Visible を制御
                { "drugn",  "薬剤名" },
                { "drugc",  "院内コード" },
                { "yj7",    "成分7桁" },

                { "section_type",         "区分" },      // 併用禁忌 / 併用注意 など
                { "partner_name_ja",      "相互作用相手" },
                { "symptoms_measures_ja", "説明" },
                { "mechanism_ja",         "機序" },
            };
            int idx = 0;
            foreach (var kv in headers)
            {
                if (dgv.Columns.Contains(kv.Key))
                {
                    dgv.Columns[kv.Key].HeaderText = kv.Value;
                    dgv.Columns[kv.Key].DisplayIndex = idx++;
                }
            }

            // 非表示列
            string[] hiddenCols = { "ptidmain", "didate", "drugc", "yj7", "yj_code", "has_interaction" };
            foreach (string name in hiddenCols)
            {
                var col = FindCol(name);
                if (col != null) col.Visible = false;
            }

            // 幅指定（固定）
            SetW("drugn", 150);
            SetW("partner_name_ja", 150);
            SetW("section_type", 70);
            SetW("symptoms_measures_ja", 160);

            // 「説明」は残り幅
            {
                var c = FindCol("mechanism_ja");
                if (c != null)
                {
                    c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    //c.MinimumWidth = 200; // ここで c が null でも落ちない
                }
            }

            // すべての列をソート可能に
            foreach (DataGridViewColumn col in dgv.Columns)
                col.SortMode = DataGridViewColumnSortMode.Automatic;

            // 幅固定ヘルパ
            void SetW(string key, int w)
            {
                var c = FindCol(key);
                if (c == null) return;
                c.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                c.Width = w;
            }
        }


        public static void SetInteractionColors(DataGridView dgv)
        {
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.Cells["section_type"]?.Value == null) continue;

                string type = row.Cells["section_type"].Value.ToString();

                if (type.Contains("禁忌"))
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 235, 238);
                    row.DefaultCellStyle.ForeColor = Color.Maroon;
                    //row.DefaultCellStyle.SelectionBackColor = row.DefaultCellStyle.BackColor;
                    //row.DefaultCellStyle.SelectionForeColor = row.DefaultCellStyle.ForeColor;
                }
                else if (type.Contains("注意"))
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 255, 224); // 淡い黄（light yellow）
                    row.DefaultCellStyle.ForeColor = Color.Black;
                    //row.DefaultCellStyle.SelectionBackColor = row.DefaultCellStyle.BackColor;
                    //row.DefaultCellStyle.SelectionForeColor = row.DefaultCellStyle.ForeColor;
                }
            }
        }

        // リストRSBDIのカラム1,2を対象にあいまい検索を行い、上位10件を返すメソッド
        public static async Task<List<Tuple<string[], double>>> FuzzySearchAsync(string drugName, string ingreN, string YJcode, List<string[]> DI, double cutoffThreshold = 0.4, double bonusForOriginator = 0.4, double penaltyForMissingIngreN = 0.5)
        {
            //double bonusForOriginator = 0.4;        // "先発" の場合のボーナス
            //double penaltyForMissingIngreN = 0.5;   // 一般名が含まれない場合のペナルティ
            double weightColumn1 = 0.3;             // 1列目のスコアに対する重み
            double weightColumn2 = 0.5;             // 2列目のスコアに対する重み

            // 正規表現で「」や【】に囲まれた部分を削除
            string processedDrugName = RemoveCampany(drugName);

            //数字アルファベットは除去しておく
            //string drugNameNoDigit = RemoveDigits(drugName);

            if (string.IsNullOrEmpty(ingreN)) ingreN = processedDrugName;

            string yjPrefix = (!string.IsNullOrEmpty(YJcode) && YJcode.Length >= 9)
                    ? YJcode.Substring(0, 9)
                    : (YJcode ?? string.Empty);

            var exactMatchList = new List<Tuple<string[], double>>();
            var prefixMatchList = new List<Tuple<string[], double>>();
            var fuzzyMatchTasks = new List<Task<Tuple<string[], double>>>();

            foreach (var record in DI)
            {
                string column1 = record.Length > 0 ? (record[0] ?? "") : "";  // 1列目（薬品名）
                string column2 = record.Length > 1 ? (record[1] ?? "") : "";  // 2列目（成分名）
                string column3 = record.Length > 2 ? (record[2] ?? "") : "";  // 3列目（YJコード）
                string column4 = record.Length > 3 ? (record[3] ?? "") : "";  // 4列目（"先発" の確認）
                //string column5 = record[4];  // 5列目（薬価文字列）

                if (!string.IsNullOrEmpty(YJcode))
                {
                    // YJコード完全一致
                    if (column3 == YJcode)
                    {
                        exactMatchList.Add(new Tuple<string[], double>(record, 1.0));
                        continue;
                    }
                    // YJコード上位9桁一致
                    if (column3.Length >= 9 && column3.Substring(0, 9) == yjPrefix)
                    {
                        prefixMatchList.Add(new Tuple<string[], double>(record, 0.9));
                        continue;
                    }
                }

                // あいまい検索の処理を非同期タスクで並列実行
                fuzzyMatchTasks.Add(Task.Run(() =>
                {
                    double similarityColumn1 = CalculateNGramSimilarity(processedDrugName, column1);
                    double similarityColumn2 = CalculateNGramSimilarity(ingreN, column2);

                    double editDistanceScore = 1.0 - (double)CalculateLevenshteinDistance(processedDrugName, column1)
                                               / Math.Max(processedDrugName.Length, column1.Length);

                    double similarity = weightColumn1 * Math.Max(similarityColumn1, editDistanceScore) +
                                        weightColumn2 * similarityColumn2;

                    bool exact = false;
                    if (drugName == column1)
                    {
                        similarity = 1.0;
                        exact = true;
                    }
                    else if (ingreN == column2)
                    {
                        similarity = 0.9;
                        exact = true;
                    }
                    //部分一致
                    else if (column1.Length > 0 && ( processedDrugName.Contains(column1) || column1.Contains(processedDrugName)))
                    {
                        similarity = 0.8;
                    }
                    else if (column2.Length > 0 && ( ingreN.Contains(column2) || column2.Contains(ingreN)))
                    {
                        similarity = 0.7;
                    }

                    if (similarity > cutoffThreshold && column4.Contains("先発"))
                    {
                        similarity += bonusForOriginator;
                    }

                    if (!exact && !column2.Contains(ingreN) && !ingreN.Contains(column2))
                    {
                        similarity -= penaltyForMissingIngreN;
                    }

                    similarity = Math.Max(0, similarity);

                    return new Tuple<string[], double>(record, similarity);
                }));
            }

            // あいまい検索のタスク完了を待つ
            var fuzzyResults = await Task.WhenAll(fuzzyMatchTasks);

            // 類似度でフィルタリング
            var filteredFuzzyResults = fuzzyResults
                .Where(r => r.Item2 >= cutoffThreshold)
                .OrderByDescending(r => r.Item2)
                .ToList();

            // 結果を統合（完全一致 → 9桁一致 → あいまい検索）
            var finalResults = exactMatchList.Concat(prefixMatchList).Concat(filteredFuzzyResults).Take(20).ToList();


            return finalResults;
        }

        public static string RemoveCampany(string drugName)
        {
            if (string.IsNullOrWhiteSpace(drugName)) return drugName;

            string processedDrugName = drugName.Trim();

            // 全体が（）で囲まれている場合は中身だけ残す
            if (Regex.IsMatch(processedDrugName, @"^（[^（）]+）$"))
            {
                processedDrugName = Regex.Replace(processedDrugName, @"^（(.+)）$", "$1");
            }
            else
            {
                // 部分的な「」「【】」「（）を削除」
                string pattern = @"[「【（][^」】）]*[」】）]";
                processedDrugName = Regex.Replace(processedDrugName, pattern, "");
            }

            // 末尾の「等」を削除
            processedDrugName = Regex.Replace(processedDrugName, @"等$", "");

            // 前後の空白をトリム
            processedDrugName = processedDrugName.Trim();

            return processedDrugName;
        }

        private static HashSet<string> GenerateNGrams(string input, int n)
        {
            var ngrams = new HashSet<string>();
            if (input.Length < n) return ngrams;

            for (int i = 0; i <= input.Length - n; i++)
            {
                ngrams.Add(input.Substring(i, n));
            }

            return ngrams;
        }

        private static double CalculateNGramSimilarity(string source, string target, int n = 2)
        {
            if(target.Length == 0) return 0;

            var sourceNGrams = GenerateNGrams(source, n);
            var targetNGrams = GenerateNGrams(target, n);

            if (sourceNGrams.Count == 0 || targetNGrams.Count == 0) return 0.0;

            int intersectionCount = sourceNGrams.Intersect(targetNGrams).Count();
            int unionCount = sourceNGrams.Union(targetNGrams).Count();

            return (double)intersectionCount / unionCount;
        }

        private static int CalculateLevenshteinDistance(string source, string target)
        {
            int[,] dp = new int[source.Length + 1, target.Length + 1];

            for (int i = 0; i <= source.Length; i++) dp[i, 0] = i;
            for (int j = 0; j <= target.Length; j++) dp[0, j] = j;

            for (int i = 1; i <= source.Length; i++)
            {
                for (int j = 1; j <= target.Length; j++)
                {
                    int cost = source[i - 1] == target[j - 1] ? 0 : 1;
                    dp[i, j] = Math.Min(Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1), dp[i - 1, j - 1] + cost);
                }
            }

            return dp[source.Length, target.Length];
        }

        public static void SaveViewerSettings(Form form, string key)
        {
            if (form.WindowState != FormWindowState.Normal || form.WindowState == FormWindowState.Minimized) form.WindowState = FormWindowState.Normal;

            // 現在の位置とサイズを保存
            if (Properties.Settings.Default.Properties[key] != null) // キーが存在するか確認
            {
                Properties.Settings.Default[key] = form.Bounds;
                Properties.Settings.Default.Save();
            }
        }

        /// <summary>
        /// スケジュール条件に従いバックアップ（メインフォームが閉じていてもOK）
        /// </summary>
        public static async Task<BackupOutcome> TryRunScheduledDumpAsync(
            bool force = false,
            Action<string> log = null,
            CancellationToken ct = default(CancellationToken))
        {
            string folder = Properties.Settings.Default.PGDumpFolder;
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                SafeLog(log, "[Backup] フォルダが無効: " + folder);
                return BackupOutcome.Failed;
            }

            // 7日超のバックアップを削除（ローカル時刻でOK）
            CleanupOldBackups(folder, log);

            // 二重起動防止
            if (!_dumpGate.Wait(0))
            {
                SafeLog(log, "[Backup] 別のバックアップ実行中のためスキップ");
                return BackupOutcome.Skipped;
            }

            try
            {
                // 最新ファイル時刻と lastDumped のうち遅い方を基準に判定
                DateTime now = DateTime.Now;
                DateTime? latestFile = GetLatestBackupTime(folder); // LastWriteTime(ローカル)
                DateTime? baseline = latestFile;
                if (lastDumped.HasValue && (!baseline.HasValue || lastDumped.Value > baseline.Value))
                    baseline = lastDumped.Value;

                if (!force && baseline.HasValue && (now - baseline.Value) < DumpInterval)
                {
                    SafeLog(log, string.Format("[Backup] スキップ: 最新 {0:yyyy-MM-dd HH:mm:ss} / 経過 {1}分 (< {2}分)",
                        baseline.Value, (int)(now - baseline.Value).TotalMinutes, (int)DumpInterval.TotalMinutes));
                    return BackupOutcome.Skipped;
                }

                // 出力ファイル名
                string outFile = Path.Combine(folder, $"{PGdatabaseName}_{now:yyMMdd_HHmmss}.backup");

                // 接続情報（Settings から取得）
                string host = Properties.Settings.Default.PGaddress;
                int port = Properties.Settings.Default.PGport;
                string user = Properties.Settings.Default.PGuser;
                string db = CommonFunctions.PGdatabaseName;
                string password = CommonFunctions.decodePassword(Properties.Settings.Default.PGpass);

                // タイムアウト（必要あれば呼び出し側で LinkedToken にする）
                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(ct))
                {
                    cts.CancelAfter(TimeSpan.FromMinutes(10));

                    bool ok = await RunPgDumpAsync(outFile, host, port, user, db, password, cts.Token)
                                  .ConfigureAwait(false);
                    if (ok)
                    {
                        lastDumped = DateTime.Now; // ローカル時刻で保持
                        SafeLog(log, "[Backup] 完了: " + outFile);
                        return BackupOutcome.Success;
                    }
                    else
                    {
                        SafeLog(log, "[Backup] 失敗: " + outFile);
                        return BackupOutcome.Failed;
                    }
                }
            }
            catch (Exception ex)
            {
                SafeLog(log, "[Backup] 例外: " + ex.Message);
                return BackupOutcome.Failed;
            }
            finally
            {
                _dumpGate.Release();
            }
        }

        /// <summary>pg_dump 実行（.NET 4.8 対応、UI非依存）</summary>
        public static async Task<bool> RunPgDumpAsync(
            string outFile, string host, int port, string user, string database, string password, CancellationToken cancel)
        {
            // 追加で除外したいテーブル
            string[] extraExcludes = {
                "drug_code_map",
                "drug_code_map_version",
                "drug_contraindication",
                "drug_medis_generic"
            };
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            string pgDumpPath = Path.Combine(exeDir, "pgsql", "bin", "pg_dump.exe");
            if (!File.Exists(pgDumpPath))
            {
                await AddLogAsync($"[Backup] pg_dump が見つかりません: {pgDumpPath}");
                return false;
            }

            var sb = new StringBuilder();
            sb.AppendFormat("-h {0} -p {1} -U {2} ", EscapeArg(host), port, EscapeArg(user));
            sb.Append("-F c -Z 9 --no-owner --no-privileges ");

            // 既存: sgml_* を除外（スキーマ付き/なし両方）
            sb.Append("-T \"sgml_*\" -T \"public.sgml_*\" ");

            // 追加: 個別テーブルの除外（スキーマ付き/なし両方）
            foreach (var name in extraExcludes)
            {
                sb.AppendFormat("-T \"{0}\" -T \"public.{0}\" ", name);
            }

            // 出力先とデータベース名
            sb.AppendFormat("-f \"{0}\" {1}", outFile, EscapeArg(database));

            var psi = new ProcessStartInfo
            {
                FileName = pgDumpPath, // PATH未設定なら絶対パス指定に変更
                Arguments = sb.ToString(),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            // パスワードは環境変数で渡す（コマンドラインに残さない）
            if (!string.IsNullOrEmpty(password))
                psi.EnvironmentVariables["PGPASSWORD"] = password;

            using (var proc = new Process { StartInfo = psi })
            {
                proc.Start();
                // .NET 4.8 では WaitForExitAsync が無いので Task.Run で待機
                await Task.Run(new Action(proc.WaitForExit), cancel);
                return proc.ExitCode == 0;
            }
        }


        private static void CleanupOldBackups(string folder, Action<string> log)
        {
            try
            {
                foreach (var file in Directory.GetFiles(folder, $"{PGdatabaseName}_*.backup"))
                {
                    var age = DateTime.Now - File.GetLastWriteTime(file);
                    if (age > Retention)
                    {
                        try
                        {
                            File.Delete(file);
                            SafeLog(log, "[Backup] 旧ファイル削除: " + Path.GetFileName(file) + " (" + age.Days + "日経過)");
                        }
                        catch (Exception ex)
                        {
                            SafeLog(log, "[Backup] 削除失敗: " + file + " : " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SafeLog(log, "[Backup] クリーンアップ失敗: " + ex.Message);
            }
        }

        private static DateTime? GetLatestBackupTime(string folder)
        {
            var files = Directory.GetFiles(folder, "OQSDrugData_*.backup");
            if (files.Length == 0) return null;
            return files.Select(f => File.GetLastWriteTime(f)).Max();
        }

        private static string EscapeArg(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }

        private static void SafeLog(Action<string> log, string msg)
        {
            if (log != null) log(msg);
        }


        /// <summary>
        /// .backup(-Fc/-Ft/-Fd)の内容一覧を取得。tablesOnly/dataOnlyで簡易フィルタ可能。
        /// </summary>
        public static async Task<string[]> ListDumpContents(
             string backupPath,
             string password,
             bool tablesOnly = false,
             bool dataOnly = false)
        {
            if (string.IsNullOrWhiteSpace(backupPath) ||
                (!File.Exists(backupPath) && !Directory.Exists(backupPath)))
                throw new FileNotFoundException("バックアップファイル/ディレクトリが見つかりません。", backupPath);

            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            string pgRestorePath = Path.Combine(exeDir, "pgsql", "bin", "pg_restore.exe");
            if (!File.Exists(pgRestorePath))
            {
                await AddLogAsync($"[Restore] pg_restore が見つかりません: {pgRestorePath}");
                return null;
            }

            var psi = new ProcessStartInfo
            {
                FileName = pgRestorePath,
                Arguments = $"-l \"{backupPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            if (!string.IsNullOrEmpty(password))
                psi.EnvironmentVariables["PGPASSWORD"] = password;

            using (var p = new Process { StartInfo = psi, EnableRaisingEvents = false })
            {
                p.Start();

                // 出力を非同期で読み取り
                var stdOutTask = p.StandardOutput.ReadToEndAsync();
                var stdErrTask = p.StandardError.ReadToEndAsync();

                // .NET 4.8 では WaitForExitAsync が無いのでスレッド上で待機
                await Task.Run(new Action(p.WaitForExit));

                var stdout = await stdOutTask.ConfigureAwait(false);
                var stderr = await stdErrTask.ConfigureAwait(false);

                if (p.ExitCode != 0)
                    throw new InvalidOperationException("pg_restore -l 失敗: " + stderr);

                var lines = stdout.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                if (dataOnly)
                    return lines.Where(l => l.Contains("TABLE DATA")).ToArray();

                if (tablesOnly)
                    return lines.Where(l => l.Contains(" TABLE ") || l.Contains("TABLE DATA")).ToArray();

                return lines; // 全オブジェクト
            }
        }
    }
        
}


