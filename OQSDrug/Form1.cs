using Microsoft.VisualBasic;
using Microsoft.VisualBasic.Logging;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using static OQSDrug.CommonFunctions;
using static System.Net.WebRequestMethods;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;
using File = System.IO.File;


namespace OQSDrug
{
    public partial class Form1 : Form
    {
        // Global
        public string DataReadMode = "Read", DynaReadMode = "Read";  //Share Deny None 
        
        public long tempId = 0;
        public bool autoRSB = false, forceIdLink = false, autoTKK = false, autoSR = false;
        
        public string RSBdrive = string.Empty;

        string DynaTable = "T_資格確認結果表示";
        DataTable dynaTable = new DataTable();

        DataTable reqResultsTable = new DataTable();
        //

        // 最新の特定健診結果を保存しておく
        private static Dictionary<long, string> TKKdate = new Dictionary<long, string>();

        byte okSettings = 0;

        //private Timer timer;
        private bool isTimerRunning = false; // タイマーの状態フラグ
        private bool isOQSRunnnig = false;   //取得開始しているか
        private bool isFormVisible = true;  //最小化

        private bool skipReload = false; //reqResult更新をスキップする 1回だけ
        public bool forceSkipReload = false; //設定されてる間更新Off

        private System.Threading.Timer backgroundTimer; //非同期タイマー

        private FileSystemWatcher fileWatcher;
        string idFile = ""; //RSB連携
        int idStyle = 0;
        bool idChageCalled = false;
        int fileReadDelayms = 500;

        // PGDump
        private System.Threading.Timer _dumpTimer;

        // バックアップ、ログファイル //CommonFunctionsに移行した
        //private static readonly string PersonalFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        //private static readonly string OQSFolder = Path.Combine(PersonalFolder, "OQSDrug");
        //private static readonly string LogFile = Path.Combine(OQSFolder, "OQSDrug.log");


        // 動的に追加するラベル
        //private Label[] statusLabels;
        //private Label[] statusTexts;


        public FormTKK formTKKInstance = null;
        public FormDI formDIInstance = null;
        public FormSR formSRInstance = null;
        
        // アイコンの配列を用意 (例: 3つのアイコン)
        private System.Windows.Forms.Timer animationTimer;
        private int currentFrame;
        private Icon[] icons;

        public Form1()
        {
            InitializeComponent();

            // これがないと UiSync が null のままになる
            CommonFunctions.UiSync = SynchronizationContext.Current; // WindowsFormsSynchronizationContext
            // UIログ表示のコールバック登録
            CommonFunctions.UiLogCallback = AddLogAsyncToUi;
                        
            //InitializeTimer();
        }

        private async Task RunTimerLogicAsync()
        {
            DateTime startTime = DateTime.Now;
            AddLogAsync("タイマーイベント開始");

            // Status check
            okSettings = await UpdateStatus();
            Invoke(new Action(() => this.StartStop.Enabled = (okSettings == 0b111)));

            //AutoStartStop
            if (Properties.Settings.Default.AutoStart)
            {
                Invoke(new Action(() => StartStop.Checked = (okSettings == 0b111)));
            }

            //PGDump timer
            if (Properties.Settings.Default.AutoPGDump && _dumpTimer == null)
            {
                StartDumpTimer();
            }
            else if (!Properties.Settings.Default.AutoPGDump && _dumpTimer != null)
            {
                StopDumpTimer();
            }


            checkAccessProcess();

            //取得作業
            if (isOQSRunnnig)
            {
                if (okSettings != 0b111)
                {   // Running->NG->Stop
                    Invoke(new Action(() => StartStop.Checked = false));
                }
                else
                {
                    await UpdateClientAsync();

                    // Datadynaのデータ取得
                    dynaTable.Clear();
                    dynaTable = await LoadDataFromDatabaseAsync(Properties.Settings.Default.Datadyna);

                    if (dynaTable != null)
                    {
                        // 薬剤PDF
                        if (Properties.Settings.Default.DrugFileCategory % 2 == 0) //xml
                        {
                            MakeReq(Properties.Settings.Default.DrugFileCategory + 10, dynaTable);
                        }
                        else
                        {
                            MakeReq((Properties.Settings.Default.DrugFileCategory % 10) + 1 + 10, dynaTable); //xml
                            MakeReq((Properties.Settings.Default.DrugFileCategory % 10) + 10, dynaTable);  //pdf
                        }

                        // 健診PDF
                        
                        if (Properties.Settings.Default.KensinFileCategory == 1)
                        {
                            MakeReq(102, dynaTable);
                            MakeReq(101, dynaTable); //固定間隔
                        }
                        else // xmlのみ、Or 健診日によってPDF日付を変える場合
                        {
                            MakeReq(102, dynaTable); // xmlを先行、取り込み後健診実施日を確定、TKKdateに設定, ProcessResAsyncで再度MakeReq
                        }
                    }
                    await reloadDataAsync();

                    // Resフォルダの処理
                    bool processCompleted = false;
                    bool isRemainRes = true;

                    // 5秒ごとにProcessResAsyncを呼び出し
                    while ((!processCompleted || isRemainRes) && isOQSRunnnig)
                    {
                        await Task.Delay(5000);

                        if (!isTimerRunning || !isOQSRunnnig) break;

                        processCompleted = await ProcessResAsync();
                        if (processCompleted) AddLogAsync("すべてのresファイルを処理しました");

                        isRemainRes = await RemainResTask();

                        if (isRemainRes && (DateTime.Now - startTime).TotalSeconds > (Properties.Settings.Default.TimerInterval - 5))
                        {
                            processCompleted = true;
                            isRemainRes = false;
                            AddLogAsync("時間内に処理が終了しませんでしたので、タイマー処理を中止します");
                        }

                        await reloadDataAsync();
                    }
                }
            }
            else if ((okSettings & 0b001) == 1)  //OQSDrugData OK
            {
                //取得停止中はreloadのみ
                await reloadDataAsync();
            }
            AddLogAsync($"タイマーイベント終了");
        }

        public void StartTimer()
        {
            backgroundTimer = new System.Threading.Timer(async _ =>
            {
                if (!isTimerRunning)
                {
                    isTimerRunning = true;
                    await RunTimerLogicAsync();
                    isTimerRunning = false;
                }
            }, null, 0, Properties.Settings.Default.TimerInterval * 1000);
        }

        public void StopTimer()
        {
            backgroundTimer?.Dispose();
            backgroundTimer = null;
        }

        // フォームが閉じられるときにタイマーを停止
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            //if (timer != null)
            //{
            //    timer.Stop();
            //    timer.Dispose();
            //}
            if (backgroundTimer != null)
            {
                backgroundTimer.Dispose();
            }

        }

        // データベースの内容を読み込み、DataGridViewに表示
        private async Task reloadDataAsync(bool skipSql = false)
        {
            if (forceSkipReload || skipReload)
            {
                skipReload = false;
                return;
            }
            else if (!await CommonFunctions.WaitForDbUnlock(1000))
            {
                AddLogAsync("データベースがロックされていたためreloadDataAsyncをスキップししました");
            }
            else
            {
                string sql = @"
                    SELECT CategoryName, PtID, PtName, result, reqDate, reqFile, resDate, resFile, category, ID 
                    FROM reqResults 
                    WHERE reqDate > @dateThreshold 
                    ORDER BY reqDate DESC";

                sql = CommonFunctions.ConvertSqlForOleDb(sql);
                
                try
                {
                    using (DataTable dt = new DataTable())
                    {
                        if (!skipSql)
                        {
                            using (IDbConnection connection = CommonFunctions.GetDbConnection(true))
                            {
                                await ((DbConnection)connection).OpenAsync();

                                using (IDbCommand command = connection.CreateCommand())
                                {
                                    command.CommandText = sql;

                                    CommonFunctions.AddDbParameter(command, "@dateThreshold", DateTime.UtcNow.AddDays(-700)); // UTC推奨

                                    CommonFunctions.DataDbLock = true;

                                    using (var reader = await ((DbCommand)command).ExecuteReaderAsync())
                                    {
                                        dt.Load(reader);
                                    }

                                    CommonFunctions.DataDbLock = false;
                                }
                            }

                            reqResultsTable = dt;
                            AddLogAsync("reqResultsテーブルを読み込みました");
                        }

                        if (isFormVisible)
                        {
                            dataGridView1.Invoke(new Action(() =>
                            {
                                dataGridView1.DataSource = reqResultsTable;
                                ConfigureDataGridView(dataGridView1);
                            }));
                            AddLogAsync("DataGridViewを更新しました");
                        }
                    }
                }
                catch (Exception ex)
                {
                    AddLogAsync($"reloadDataAsyncエラー: {ex.Message}");
                }
                finally
                {
                    CommonFunctions.DataDbLock = false;
                }
            }
        }


        private async void toolStripButtonSettings_Click(object sender, EventArgs e)
        {
            //動作中の場合は停止する
            if (isOQSRunnnig)
            {
                Invoke(new Action(() => MessageBox.Show("一旦タイマー動作を停止します")));
                
                StartStop.Checked = false;
                
                StopTimer();
            }

            forceSkipReload = true;

            //Form2を開く
            Form2 form2 = new Form2(this);
            form2.ShowDialog(this);

            //Form2閉じたあと

            await initializeForm();

            forceSkipReload = false;
            StartTimer();

        }

        private async void StartStop_CheckedChanged(object sender, EventArgs e)
        {
            if (StartStop.Checked)
            {
                if (await IsAccessAllowedAsync())
                {

                    //開始
                    StartStop.Text = "停止";
                    StartStop.Image = Properties.Resources.Stop;
                    animationTimer?.Start();

                    //StartTimer();
                    isOQSRunnnig = true;

                    AddLogAsync($"タイマー処理を開始します。間隔は{Properties.Settings.Default.TimerInterval}秒です");

                }
                else
                {
                    StartStop.Checked = false ;

                    MessageBox.Show("他のPCで取込操作を行っているようです。2箇所以上で取込を行うとデータ競合が起こりますので、取込は1箇所でお願いします。\n" +
                        "薬歴や健診のビュワーとして使うときは、開始ボタンは押さずに利用してください。");

                    animationTimer?.Stop();
                    notifyIcon1.Icon = Properties.Resources.BlueDrug1;
                }
                //初回実行
                //await Task.Run(() => Timer_Tick(timer, EventArgs.Empty));
            }
            else
            {
                StartStop.Text = "開始";
                StartStop.Image = Properties.Resources.Go;
                //timer.Stop();
                animationTimer?.Stop();
                notifyIcon1.Icon = Properties.Resources.BlueDrug1;

                //StopTimer();
                isOQSRunnnig = false;

                await DeleteClientAsync();

                AddLogAsync("タイマー処理を終了します");
            }
        }

        public async void MakeReq(int category, DataTable dynaTable, long targetId = 0) //Category: 11:薬剤pdf, 12:薬剤xml、13:薬剤診療pdf、14：薬剤診療xml、101：健診pdf、102：健診xml
        {
            int Span = Properties.Settings.Default.YZspan;
            int YZinterval = Properties.Settings.Default.YZinterval, KSinterval = Properties.Settings.Default.KSinterval, Interval;
            string dynaPath = Properties.Settings.Default.Datadyna;
            string douiFlag = "", douiDate = "";
            DateTime checkDate;
            bool doReq = true;
            int fileCategory = 0; //1:PDF, 2:xml
            string OQSpath = Properties.Settings.Default.OQSFolder;
            string CategoryName = "";

            string startDate = DateTime.Now.AddMonths(-Span).ToString("yyyyMM");
            string endDate = DateTime.Now.ToString("yyyyMM");

            //string reqDateSQL = "SELECT TOP 1 reqDate, ID FROM reqResults " +
                       //"WHERE PtID = ? AND category = ? ORDER BY ID DESC";
            string reqDateSQL = (Properties.Settings.Default.DBtype == "pg")
                ? @"SELECT reqDate, id FROM reqResults WHERE ptid= @ptId AND category = @category ORDER BY id DESC LIMIT 1"
                : @"SELECT TOP 1 reqDate, ID FROM reqResults WHERE PtID = ? AND category = ? ORDER BY ID DESC";

            // レコードの更新クエリ
            string updateRecordSQL = @"
                UPDATE reqResults
                SET reqFile = @reqFile, reqDate = @reqDate, resFile = NULL, resDate = NULL, result = NULL
                WHERE id = @ID";

            string insertRecordSQL = @"
                INSERT INTO reqResults (category, ptid, ptname, reqfile, reqdate, categoryname)
                VALUES (@category, @ptId, @ptName, @reqFile, @reqDate, @categoryName)";

            reqDateSQL = CommonFunctions.ConvertSqlForOleDb(reqDateSQL);
            updateRecordSQL = CommonFunctions.ConvertSqlForOleDb(updateRecordSQL);
            insertRecordSQL = CommonFunctions.ConvertSqlForOleDb(insertRecordSQL);

            switch (category)
            {
                case 11:
                    douiFlag = "薬剤情報閲覧同意フラグ";
                    douiDate = "薬剤情報閲覧有効期限";
                    Interval = YZinterval;  // Replace with appropriate interval
                    fileCategory = 1; //1:PDF, 2:xml
                    CategoryName = "薬剤PDF";
                    AddLogAsync("PDF薬剤情報取得用reqファイルを作成します");
                    break;
                case 101:
                    douiFlag = "特定検診情報閲覧同意フラグ";
                    douiDate = "特定検診情報閲覧有効期限";
                    Interval = KSinterval;
                    fileCategory = 1;
                    CategoryName = "健診PDF";
                    AddLogAsync("特定検診PDF情報取得用reqファイルを作成します");
                    break;
                case 102:
                    douiFlag = "特定検診情報閲覧同意フラグ";
                    douiDate = "特定検診情報閲覧有効期限";
                    Interval = KSinterval;
                    fileCategory = 2;
                    CategoryName = "健診xml";
                    AddLogAsync("特定検診xml情報取得用reqファイルを作成します");
                    break;
                case 12:
                    douiFlag = "薬剤情報閲覧同意フラグ";
                    douiDate = "薬剤情報閲覧有効期限";
                    Interval = YZinterval;  // Replace with appropriate interval
                    fileCategory = 2;
                    CategoryName = "薬剤xml";
                    AddLogAsync("xml薬剤情報取得用reqファイルを作成します");
                    break;
                case 13:
                    douiFlag = "薬剤情報閲覧同意フラグ";
                    douiDate = "薬剤情報閲覧有効期限";
                    Interval = YZinterval;  // Replace with appropriate interval
                    fileCategory = 1;
                    CategoryName = "薬剤診療PDF";
                    AddLogAsync("PDF薬剤診療情報取得用reqファイルを作成します");
                    break;
                case 14:
                    douiFlag = "薬剤情報閲覧同意フラグ";
                    douiDate = "薬剤情報閲覧有効期限";
                    Interval = YZinterval;  // Replace with appropriate interval
                    fileCategory = 2;
                    CategoryName = "薬剤診療xml";
                    AddLogAsync("xml薬剤診療情報取得用reqファイルを作成します");
                    break;
                default:
                    AddLogAsync("Invalid category");
                    return;
            }

            checkDate = fileCategory == 1 ? DateTime.Now.AddMonths(-Interval) : DateTime.Now.AddHours(-6); // xmlの場合は取得間隔6時間
            if (category == 102) checkDate = DateTime.Now.AddDays(-1); //特定健診xmlは1日1回

            if (targetId > 0) checkDate = DateTime.Now; //強制受信

            try
            {
                DataRow[] DouiRows = dynaTable.Select($"{douiFlag} = '1'");
                foreach (DataRow DouiRow in DouiRows)
                {
                    long ptId = DouiRow["カルテ番号"] == DBNull.Value ? 0 : Convert.ToInt64(DouiRow["カルテ番号"]);
                    string ptName = DouiRow["氏名"].ToString();

                    if (ptId == 0)
                    {
                        ptId = Name2ID(ptName, DouiRow["生年月日西暦"].ToString(), dynaTable);
                        if (ptId == 0 && targetId > 0) ptId = targetId;
                    }

                    if (targetId == 0 || ptId == targetId)
                    {
                        if (!IsDateStringAfterNow(DouiRow[douiDate].ToString()))
                        {
                            AddLogAsync($"{ptId}:{ptName}さんの同意有効期限が切れているのでスキップします");
                        }
                        else if (ptId == 0)
                        {
                            AddLogAsync($"{ptName}さんのカルテ番号の取得ができなかったため処理をスキップします");
                        }
                        else
                        {
                            if (!await CommonFunctions.WaitForDbUnlock(1000))
                            {
                                AddLogAsync("データベースがロックされています。Makereq処理をスキップします");
                            }
                            else
                            {
                                CommonFunctions.DataDbLock = true;
                                try
                                {
                                    using (IDbConnection connData = CommonFunctions.GetDbConnection(false))
                                    {
                                        connData.Open();  // PostgreSQLの場合は OpenAsync を使ってもOK
                                        string reqXml = "";
                                        object resultId = null;

                                        using (IDbCommand cmd = connData.CreateCommand())
                                        {
                                            cmd.CommandText = reqDateSQL;
                                            CommonFunctions.AddDbParameter(cmd, "@ptId", ptId);
                                            CommonFunctions.AddDbParameter(cmd, "@category", category);

                                            using (IDataReader reqReader = cmd.ExecuteReader())
                                            {
                                                doReq = false;

                                                if (reqReader.Read())
                                                {
                                                    DateTime reqDate = Convert.ToDateTime(reqReader["reqDate"]);

                                                    if (reqDate < checkDate)
                                                    {
                                                        doReq = true;
                                                        resultId = reqReader["ID"];
                                                    }
                                                }
                                                else
                                                {
                                                    doReq = true;
                                                }
                                            }

                                            CommonFunctions.DataDbLock = false;

                                            if (doReq)
                                            {
                                                var ptData = new
                                                {
                                                    Id = ptId,
                                                    Name = ptName,
                                                    InsurerNumber = DouiRow["保険者番号"].ToString(),
                                                    InsuranceCardSymbol = DouiRow["被保険者証記号"].ToString(),
                                                    InsuredPersonIdentificationNumber = DouiRow["被保険者証番号"].ToString(),
                                                    BranchNumber = DouiRow["被保険者証枝番"].ToString()
                                                };

                                                reqXml = await Task.Run(() => GenerateXML(ptData, OQSpath, startDate, endDate, Properties.Settings.Default.MCode, category));

                                                if (!string.IsNullOrEmpty(reqXml))
                                                {
                                                    if (resultId != null)
                                                    {
                                                        using (IDbCommand updateCmd = connData.CreateCommand())
                                                        {
                                                            updateCmd.CommandText = updateRecordSQL;
                                                            CommonFunctions.AddDbParameter(updateCmd, "@reqFile", reqXml);
                                                            CommonFunctions.AddDbParameter(updateCmd, "@reqDate", DateTime.Now);
                                                            CommonFunctions.AddDbParameter(updateCmd, "@ID", resultId);
                                                            updateCmd.ExecuteNonQuery();
                                                        }
                                                    }
                                                    else
                                                    {
                                                        using (IDbCommand insertCmd = connData.CreateCommand())
                                                        {
                                                            insertCmd.CommandText = insertRecordSQL;
                                                            CommonFunctions.AddDbParameter(insertCmd, "@category", category);
                                                            CommonFunctions.AddDbParameter(insertCmd, "@ptId", ptId);
                                                            CommonFunctions.AddDbParameter(insertCmd, "@ptName", ptName);
                                                            CommonFunctions.AddDbParameter(insertCmd, "@reqFile", reqXml);
                                                            CommonFunctions.AddDbParameter(insertCmd, "@reqDate", DateTime.Now);
                                                            CommonFunctions.AddDbParameter(insertCmd, "@categoryName", CategoryName);
                                                            insertCmd.ExecuteNonQuery();
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    AddLogAsync($"Makereqでエラー：{ex}");
                                }
                                finally
                                {
                                    CommonFunctions.DataDbLock = false;
                                }
                            }
                        }
                    }
                }
                AddLogAsync("MakeReq処理が終了しました");
            }
            catch (Exception ex)
            {
                AddLogAsync($"Error occurred in MakeReq: {ex.Message}");
            }
        }

        private async Task<DataTable> LoadDataFromDatabaseAsync(string dynaPath)
        {
            string connectionString = $"Provider={CommonFunctions.DBProvider};Data Source={dynaPath};Mode={DynaReadMode};Persist Security Info=False;";
            string query = $"SELECT * FROM [{DynaTable}]"; // テーブル名をエスケープ

            DataTable dataTable = new DataTable();

            try
            {
                using (OleDbConnection connection = new OleDbConnection(connectionString))
                {
                    await connection.OpenAsync(); // 非同期で接続を開く

                    using (OleDbCommand command = new OleDbCommand(query, connection))
                    {
                        using (OleDbDataReader reader = (OleDbDataReader)await command.ExecuteReaderAsync()) // 非同期でデータを取得
                        {
                            dataTable.Load(reader); // DataReader のデータを DataTable にロード
                        }
                    }
                }

                AddLogAsync($"ダイナミクスの {DynaTable} の取り込みが完了しました");
            }
            catch (Exception ex)
            {
                AddLogAsync($"ダイナミクスの読み込みエラー: {ex.Message}\n{ex.StackTrace}");
                return null;
            }

            return dataTable;
        }

        public async Task<string> CheckDatabaseAsync(string dbPath, string tableName)
        {
            if (string.IsNullOrEmpty(dbPath))
            {
                return "エラー: データベースパスが指定されていません。";
            }
            if (string.IsNullOrEmpty(tableName))
            {
                return "エラー: テーブル名が指定されていません。";
            }

            string connectionString = $"Provider={CommonFunctions.DBProvider};Data Source={dbPath};Mode={DataReadMode};";

            try
            {
                using (var connection = new OleDbConnection(connectionString))
                {
                    await connection.OpenAsync(); // 非同期で接続を開く

                    // GetSchema を使ってテーブルが存在するか確認
                    DataTable schemaTable = connection.GetSchema("Tables", new string[] { null, null, tableName, null });

                    if (schemaTable.Rows.Count > 0)
                    {
                        return "OK"; // テーブルが存在
                    }
                    else
                    {
                        return $"エラー: テーブル '{tableName}' が存在しません。";
                    }
                }
            }
            catch (Exception ex)
            {
                return $"エラー: {ex.Message}\n{ex.StackTrace}"; // 例外の詳細情報を含める
            }
        }


        // ディレクトリチェックの非同期メソッド
        private async Task<string> CheckDirectoryExistsAsync(string directoryPath)
        {
            return await Task.Run(() =>
            {
                return Directory.Exists(directoryPath) ? "OK" : "NG";
            });
        }

        //RSBaseの妥当性チェック：PDF保存先が確保されているか
        private async Task<string> CheckRSBaseSetting()
        {
            int Yz = Properties.Settings.Default.DrugFileCategory;
            int Ks = Properties.Settings.Default.KensinFileCategory;
            string returnString = "";

            //検査登録のみの場合、gazouフォルダの有無
            if(Yz % 2 != 0 || Ks > 0) //PDF
            {
                if(Yz > 10 || Ks == 4) //SideShow PDF
                {
                    returnString = (await CheckDirectoryExistsAsync(Properties.Settings.Default.RSBServerFolder)) == "OK" ? "" : "Server " ;
                }

                if (Yz < 10 || Ks < 4) //検査登録
                {
                    returnString += (await CheckDirectoryExistsAsync(Properties.Settings.Default.RSBgazouFolder)) == "OK" ? "" : "Gazou";
                }
            }
            
            return (returnString == "") ? "OK" : $"NG:{returnString}";
        }

        private async Task<byte> UpdateStatus() //GazouF|OQSF|dyna|Data 
        {
            byte resultCode = 0;

            //DynaTable
            DynaTable = (Properties.Settings.Default.Datadyna.IndexOf("datadyna.mdb", StringComparison.OrdinalIgnoreCase) >= 0) ? "T_資格確認結果表示" : "WKO資格確認結果表示";

            //設定初期値の確認
            if (Properties.Settings.Default.TimerInterval <= 0)
            {
                Properties.Settings.Default.TimerInterval = 30;
            }

            Properties.Settings.Default.Save();

            // 非同期チェックタスクを作成
            var tasks = new List<Task<string>>();

            if (Properties.Settings.Default.DBtype == "mdb")
            {
                tasks.Add(CheckDatabaseAsync(Properties.Settings.Default.OQSDrugData, "reqResults"));
                tasks.Add(CheckDatabaseAsync(Properties.Settings.Default.Datadyna, DynaTable));
                tasks.Add(CheckDirectoryExistsAsync(Properties.Settings.Default.OQSFolder));
                //tasks.Add(CheckRSBaseSetting());
            }
            else // PG
            {
                tasks.Add(CommonFunctions.CheckPGStatusAsync(CommonFunctions.PGdatabaseName));
                tasks.Add(CheckDatabaseAsync(Properties.Settings.Default.Datadyna, DynaTable));   // ダイナは引き続きAccessならこのまま
                tasks.Add(CheckDirectoryExistsAsync(Properties.Settings.Default.OQSFolder));
                //tasks.Add(CheckRSBaseSetting());
            }

            // 各タスクのインデックスと結果を保持するための辞書
            var taskIndexMap = tasks
                    .Select((task, index) => new { task, index })
                    .ToDictionary(x => x.task, x => x.index);

            while (taskIndexMap.Any())
            {
                try
                {
                    // 完了したタスクを取得
                    var completedTask = await Task.WhenAny(taskIndexMap.Keys);

                    // タスクの結果を取得
                    string result = await completedTask;

                    // 対応するインデックスを取得
                    int index = taskIndexMap[completedTask];
                    taskIndexMap.Remove(completedTask);

                    // UI を更新（インデックスに基づいて更新）
                    Invoke((Action)(() =>
                    {
                        if (result == "OK")
                        {
                            // アイコンを緑チェックに
                            switch (index)
                            {
                                case 0: pictureBoxDB.Image = Properties.Resources.Apply; break;
                                case 1: pictureBoxDynamics.Image = Properties.Resources.Apply; break;
                                case 2: pictureBoxOQSFolder.Image = Properties.Resources.Apply; break;
                            }

                            // OKの場合、対応するビットを1にする
                            resultCode |= (byte)(1 << index);
                        }
                        else
                        {
                            // アイコンを赤バツに
                            switch (index)
                            {
                                case 0: pictureBoxDB.Image = Properties.Resources.Error; break;
                                case 1: pictureBoxDynamics.Image = Properties.Resources.Error; break;
                                case 2: pictureBoxOQSFolder.Image = Properties.Resources.Error; break;
                            }

                        }
                    }));

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                    resultCode = 0;
                }
            }

            return resultCode;
        }

        

        private void InitializeDBProvider()
        {
            try
            {
                AddLogAsync("登録されているOLE DBプロバイダを確認します...");

                // プロバイダの優先順序
                string[] preferredProviders =
                {
                    "Microsoft.Jet.OLEDB.4.0"
                    //"Microsoft.ACE.OLEDB.12.0",
                    //"Microsoft.ACE.OLEDB.15.0",
                    //"Microsoft.ACE.OLEDB.16.0"
                };
                toolStripComboBoxDBProviders.Items.Clear();

                // OleDbEnumeratorを使用して登録されているプロバイダを取得
                var enumerator = new OleDbEnumerator();
                var dataTable = enumerator.GetElements();

                // 登録されているプロバイダの一覧を取得
                var availableProviders = new List<string>();
                foreach (System.Data.DataRow row in dataTable.Rows)
                {
                    string providerName = row["SOURCES_NAME"].ToString();
                    availableProviders.Add(providerName);
                }

                // プロバイダの優先順序に従ってチェック
                foreach (string provider in preferredProviders)
                {
                    if (availableProviders.Contains(provider))
                    {
                        AddLogAsync($"DBプロバイダ: {provider}が見つかりました");
                        toolStripComboBoxDBProviders.Items.Add(provider);
                        if (CommonFunctions.DBProvider.Length == 0)
                        {
                            CommonFunctions.DBProvider = provider;
                            AddLogAsync($"使用するプロバイダ: {CommonFunctions.DBProvider}");
                            toolStripComboBoxDBProviders.SelectedIndex = toolStripComboBoxDBProviders.Items.IndexOf(provider);
                        }
                    }
                }

                // 適切なプロバイダが見つからなかった場合
                if (CommonFunctions.DBProvider.Length == 0)
                {
                    AddLogAsync("適切なOLE DBプロバイダが見つかりませんでした。");
                }
            }
            catch (Exception ex)
            {
                AddLogAsync($"エラー: {ex.Message}");
            }
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            // 前バージョンからのUpgradeを実行していないときは、Upgradeを実施する
            if (Properties.Settings.Default.IsUpgrade == false)
            {
                // Upgradeを実行する
                Properties.Settings.Default.Upgrade();

                // 「Upgradeを実行した」という情報を設定する
                Properties.Settings.Default.IsUpgrade = true;

                // 現行バージョンの設定を保存する
                Properties.Settings.Default.Save();
            }

            if (Properties.Settings.Default.MinimumStart)
            {
                toolStripButtonToTaskTray_Click(sender, EventArgs.Empty);
            }

            PrepareLogFiles();

            InitAnimationTimer();

            await initializeForm();
            
            // Timer start
            StartTimer();
        }

        private async Task initializeForm()
        {
            // 失敗メッセージをためる
            var initErrors = new List<string>();
            bool needSettingsDialog = false;

            // ========= UIを先に（Invoke不要：UIスレッド上想定） =========
            OnUI(() =>
            {
                LoadViewerSettings();

                listViewLog.Columns.Clear();
                listViewLog.Columns.Add("TimeStamp", 100); // 列1: タイムスタンプ
                listViewLog.Columns.Add("Log",-2);            // 列2: メッセージ
                SetupYZKSindicator();
                SetConnectionStatus();
            });

            // いったん描画を進める（DoEventsは使わない）
            await Task.Yield();

            // ========= ここから非UIの初期処理（落ちないようにガード） =========
            // ローカル関数：タイムアウト付きで実行し、成功/失敗をboolで返す
            async Task<bool> TryRunAsync(Func<Task> op, int timeoutMs, string name)
            {
                try
                {
                    var t = op();
                    var done = await Task.WhenAny(t, Task.Delay(timeoutMs)).ConfigureAwait(false);
                    if (done != t)
                    {
                        await AddLogAsync($"[{name}] タイムアウト（{timeoutMs}ms）").ConfigureAwait(false);
                        return false;
                    }
                    await t.ConfigureAwait(false);
                    return true;
                }
                catch (Exception ex)
                {
                    await AddLogAsync($"[{name}] 例外: {ex.GetType().Name} / {ex.Message}").ConfigureAwait(false);
                    return false;
                }
            }

            // DBプロバイダ・接続文字列（例外は握って継続）
            if (!await TryRunAsync(() => Task.Run(() =>
            {
                InitializeDBProvider();
                loadConnectionString();
            }), 3000, "InitializeDBProvider/loadConnectionString"))
            {
                initErrors.Add("DBプロバイダの初期化または接続文字列の読み込みに失敗しました。");
                needSettingsDialog = true;
            }

            // スキーマ/バージョンチェック（重い・ネットワーク要素あり：タイムアウト短めに）
            //bool dbOk = await TryRunAsync(() => CheckDBVersionAsync(CommonFunctions.DBversion), 8000, "CheckDBVersionAsync");
            bool dbOk = await CheckDBVersionAsync(CommonFunctions.DBversion);

            // 設定やステータス更新（DBに触るならdbOkで分岐）
            if (dbOk)
            {
                await TryRunAsync(async () =>
                {
                    okSettings = await UpdateStatus().ConfigureAwait(false);
                    await setStatus().ConfigureAwait(false);
                }, 5000, "UpdateStatus/setStatus");
            }
            else
            {
                await AddLogAsync("[Init] DB到達不可のため一部機能はオフラインで起動します。").ConfigureAwait(false);
                // DB前提UIをここで無効化するなら↓
                // DisableDbOnlyButtons();
                initErrors.Add("OQSDrug_data に接続できませんでした。データベースの設定を確認してください。");
                needSettingsDialog = true;
            }

            // ユーザー設定（UI）
            OnUI(() =>
            {
                autoRSB = Properties.Settings.Default.autoRSB;
                autoTKK = Properties.Settings.Default.autoTKK;
                autoSR = Properties.Settings.Default.autoSR;

                checkBoxAutoview.CheckedChanged -= checkBoxAutoview_CheckedChanged;
                checkBoxAutoTKK.CheckedChanged -= checkBoxAutoview_CheckedChanged;
                checkBoxAutoSR.CheckedChanged -= checkBoxAutoview_CheckedChanged;

                checkBoxAutoview.Checked = autoRSB;
                checkBoxAutoTKK.Checked = autoTKK;
                checkBoxAutoSR.Checked = autoSR;

                checkBoxAutoview.CheckedChanged += checkBoxAutoview_CheckedChanged;
                checkBoxAutoTKK.CheckedChanged += checkBoxAutoview_CheckedChanged;
                checkBoxAutoSR.CheckedChanged += checkBoxAutoview_CheckedChanged;

                checkBoxAutoview_CheckedChanged(this, EventArgs.Empty); // 初回実行してFileWatcher起動
                checkBoxAutoStart.Checked = Properties.Settings.Default.AutoStart;

                InitNotifyIcon();
            });

                        
            // ===== Tables（AI関連はPGのみ）: 失敗しても落とさない =====
            if (Properties.Settings.Default.DBtype == "pg" && dbOk)
            {
                // EnsureAiResultsTableAsync は既存そのまま呼ぶ（接続を渡さない）
                await TryRunAsync(() => EnsureAiResultsTableAsync(), 8000, "EnsureAiResultsTableAsync");
                await TryRunAsync(() => EnsurePromptTplTableAsync(), 8000, "EnsurePromptTplTableAsync");
                await TryRunAsync(() => InsertSampleTemplateIfEmptyAsync(), 8000, "InsertSampleTemplateIfEmptyAsync");
            }

            // ここまでで初期化と各種並列処理は一通り完了

            if (needSettingsDialog)
            {
                // UI スレッドに戻してメッセージ＋設定画面を出す
                OnUI(() =>
                {
                    if (initErrors.Count > 0)
                    {
                        var msg = string.Join(Environment.NewLine, initErrors);
                        MessageBox.Show(this,
                            msg,
                            "初期化エラー",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }

                    // 設定画面を開く（クリックと同等の処理）
                    toolStripButtonSettings.PerformClick();
                    // もしくは toolStripButtonSettings_Click(this, EventArgs.Empty);
                });
            }


        // ====== 重い並列タスク（KORO取込 / RSB読込 / Ollamaモデル） ======
        var tasks = new List<Task>();

            if ((okSettings & (0b001)) == 1) // OQSDrugData OK
            {
                await AddLogAsync("特定健診基準値データを読み込みます").ConfigureAwait(false);
                // ここはDB不要の想定：タイムアウト付きにしておく
                var tkkTask = Task.Run(async () =>
                {
                    CommonFunctions.TKKreferenceDict = await CommonFunctions.LoadTKKReference().ConfigureAwait(false);
                    await AddLogAsync($"{CommonFunctions.TKKreferenceDict.Count}件のデータを読み込みました").ConfigureAwait(false);
                });
                tasks.Add(tkkTask);

                // KORO2SQL（重い場合あり）
                tasks.Add(LoadKoro2SQL());
            }

            // RSB 読み込み
            var rsbTask = Task.Run(async () =>
            {
                RSBdrive = await GetRSBdrive().ConfigureAwait(false);
                if (!string.IsNullOrEmpty(RSBdrive))
                {
                    var path = System.IO.Path.Combine(RSBdrive, @"Users\rsn\public_html\drug_RSB.dat");
                    await LoadRSBDIAsync(path).ConfigureAwait(false);
                }
            });
            tasks.Add(rsbTask);

            
            // Ollama モデル一覧
            var ollamaTask = Task.Run(async () =>
            {
                if (Properties.Settings.Default.LLMserver.Length > 4 && Properties.Settings.Default.LLMport > 1)
                {
                    string ollamaUrl = $"http://{Properties.Settings.Default.LLMserver.Trim()}:{Properties.Settings.Default.LLMport}";
                    await CommonFunctions.GetOllamaModelsAsync(ollamaUrl).ConfigureAwait(false);
                }
            });
            tasks.Add(ollamaTask);

            // 並列の待機（失敗しても落とさずログだけ出す）
            try
            {
                // 並列全体にも上限をかけたい場合は Task.WhenAny + Delay を追加する
                await Task.WhenAll(tasks).ConfigureAwait(false);
                await AddLogAsync("KORO取込・RSB読込：並列完了").ConfigureAwait(false);
            }
            catch
            {
                foreach (var t in tasks)
                {
                    if (t.IsFaulted && t.Exception != null)
                    {
                        var ae = t.Exception.Flatten();
                        foreach (var ex in ae.InnerExceptions)
                            await AddLogAsync($"並列処理エラー: {ex.Message}\n{ex.StackTrace}").ConfigureAwait(false);
                    }
                }
                // ここで throw しない：アプリ継続
            }
        }

        private void OnUI(Action ui)
        {
            if (this.IsDisposed) return;
            if (this.InvokeRequired) this.BeginInvoke(ui);
            else ui();
        }

        private void checkAccessProcess()
        {
            // "msaccess" という名前のプロセスがあるかをチェック
            Process[] processes = Process.GetProcessesByName("msaccess");

            if (processes.Length > 0 && DynaTable == "T_資格確認結果表示")
            {
                this.Invoke(new Action(() =>
                {
                    if (StartStop.Checked)
                    {
                        if (checkBoxAutoStart.Checked)
                        {
                            checkBoxAutoStart.Checked = false;
                        }

                        StartStop.Checked = false;
                        AddLogAsync("MSaccess.exeを検知しましたので、動作を停止します");

                        MessageBox.Show("このPCでMSAccessが実行されており、ダイナミクスのリンク先がdatadyna.mdbになっていまので動作を停止しました。\nこの設定はダイナミクスのデータ障害が起こる可能性が高いので、別PCで実行するかクライアントダイナと連携するように設定を変更してください");
                    }
                }));
            }
        }

        private async Task setStatus()
        {
            if (Properties.Settings.Default.DBtype == "mdb")
            {
                if ((okSettings & (0b001)) == 1) //OQSDrugData OK
                {
                    //テーブルフィールドのアップデート
                    bool fieldCheck = await AddFieldIfNotExists(Properties.Settings.Default.OQSDrugData, "drug_history", "Source", "INTEGER")
                                    && await AddFieldIfNotExists(Properties.Settings.Default.OQSDrugData, "drug_history", "Revised", "YESNO")
                                    && await AddFieldIfNotExists(Properties.Settings.Default.OQSDrugData, "reqResults", "CategoryName", "TEXT(12) NULL");
                    if (!fieldCheck)
                    {
                        Invoke(new Action(() => MessageBox.Show("OQSDrugDataのアップデートでエラーが発生しました。OQSDrug_data.mdbにアクセスできるかを調べて再起動してください")));
                    }

                    // TKK_history table:
                    string TKKtable = "TKK_history";
                    if (await CheckDatabaseAsync(Properties.Settings.Default.OQSDrugData, TKKtable) != "OK")
                    {
                        await AddLogAsync($"{TKKtable}がないので、作成します");
                        string sql = $@"CREATE TABLE {TKKtable} (
                                        ID AUTOINCREMENT PRIMARY KEY,
                                        EffectiveTime TEXT(8),
                                        ItemCode TEXT(32),
                                        ItemName TEXT(128),
                                        DataType TEXT(4),
                                        DataValue TEXT(64),
                                        Unit TEXT(32),
                                        Oid TEXT(32) NULL,
                                        DataValueName TEXT(64) NULL,
                                        PtIDmain LONG,
                                        PtName TEXT(64) NULL,
                                        PtKana TEXT(64) NULL,
                                        Sex INTEGER
                                    )";
                        if (await CreateTableAsync(Properties.Settings.Default.OQSDrugData, sql))
                        {
                            await AddLogAsync($"{TKKtable}を作成しました");
                        }
                        else
                        {
                            await AddLogAsync($"{TKKtable}の作成に失敗しました");
                        }
                    }

                    // TKK_reference table:
                    string TKKreference = "TKK_reference";

                    if (await CheckDatabaseAsync(Properties.Settings.Default.OQSDrugData, TKKreference) != "OK")
                    {
                        await AddLogAsync($"{TKKreference}がないので、作成します");
                        string sql = $@"CREATE TABLE {TKKreference} (
                                        ID AUTOINCREMENT PRIMARY KEY,
                                        ItemCode TEXT(32),
                                        ItemName TEXT(128),
                                        Sex INTEGER,
                                        CompairType TEXT(8) NULL,
                                        Limit1 TEXT(16) NULL,
                                        Limit2 TEXT(16) NULL,
                                        IncludeValue TEXT(16) NULL
                                    )";
                        if (await CreateTableAsync(Properties.Settings.Default.OQSDrugData, sql))
                        {
                            await AddLogAsync($"{TKKreference}を作成しました。続いて初期値を入力します");
                            if (await setInitialReferenceAsync(Properties.Settings.Default.OQSDrugData))
                            {
                                await AddLogAsync("特定健診基準初期値を設定しました");

                            }
                        }
                        else
                        {
                            await AddLogAsync($"{TKKreference}の作成に失敗しました");
                        }
                    }

                    // 排他処理用テーブル
                    string exclusiveTable = "connectedClient";
                    if (await CheckDatabaseAsync(Properties.Settings.Default.OQSDrugData, exclusiveTable) != "OK")
                    {
                        await AddLogAsync($"{exclusiveTable}がないので、作成します");
                        string sql = $@"
                            CREATE TABLE {exclusiveTable} (
                            ID AUTOINCREMENT PRIMARY KEY,
                            clientName TEXT(32),
                            lastUpdated DATETIME
                                    )";
                        if (await CreateTableAsync(Properties.Settings.Default.OQSDrugData, sql))
                        {
                            await AddLogAsync($"{exclusiveTable}を作成しました");
                        }
                        else
                        {
                            await AddLogAsync($"{exclusiveTable}の作成に失敗しました");
                        }
                    }

                    // Sinryo table:
                    string SinryoTable = "sinryo_history";

                    if (await CheckDatabaseAsync(Properties.Settings.Default.OQSDrugData, SinryoTable) != "OK")
                    {
                        await AddLogAsync($"{SinryoTable}がないので、作成します");
                        string sql = $@"CREATE TABLE {SinryoTable} (
                                        id AUTOINCREMENT PRIMARY KEY,
                                        PtID LONG,
                                        PtIDmain LONG,
                                        PtName TEXT(255),
                                        PtKana TEXT(255),
                                        Birth TEXT(10),
                                        Sex INT,
                                        MeTrDiHCd TEXT(12) NULL,
                                        MeTrDiHNm TEXT(255) NULL,
                                        MeTrMonth TEXT(10) NULL,
                                        DiDate TEXT(10) NULL,
                                        SinInfN TEXT(255) NULL,
                                        SinInfCd TEXT(12) NULL,
                                        MeTrIdCl TEXT(12) NULL,
                                        Qua1 SINGLE,
                                        Times LONG,
                                        Unit TEXT(50) NULL,
                                        ReceiveDate TEXT(10) NULL
                                    )";
                        if (await CreateTableAsync(Properties.Settings.Default.OQSDrugData, sql))
                        {
                            await AddLogAsync($"{SinryoTable}を作成しました");
                        }
                        else
                        {
                            await AddLogAsync($"{SinryoTable}の作成に失敗しました");
                        }
                    }
                    await reloadDataAsync();
                }
            }
            else //PostgreSQL
            {
                //AddLogAsync($"{TKKreference}を作成しました。続いて初期値を入力します");
                //if (await setInitialReferenceAsync(Properties.Settings.Default.OQSDrugData))
                //{
                //    AddLogAsync("特定健診基準初期値を設定しました");

                //}
                if ((okSettings & (0b001)) == 1) await reloadDataAsync();
            }

            Invoke(new Action(() => this.StartStop.Enabled = (okSettings == 0b111))); 


        }

        private async Task<bool> setInitialReferenceAsync(string databasePath)
        {
            string connectionString = $"Provider={CommonFunctions.DBProvider};Data Source={databasePath};";
            bool result = false;

            // 初期データ
            var initialData = new[]
            {
            new { ItemCode = "9A755000000000001", ItemName = "収縮期血圧(その他)", CompairType = "<", Limit1 = "130", Limit2 = "140", IncludeValue = "" , Sex= 0},
            new { ItemCode = "9A751000000000001", ItemName = "収縮期血圧(1回目)", CompairType = "<", Limit1 = "130", Limit2 = "140", IncludeValue = "" , Sex= 0},
            new { ItemCode = "9A752000000000001", ItemName = "収縮期血圧(2回目)", CompairType = "<" , Limit1 = "130", Limit2 = "140", IncludeValue = "" , Sex= 0},
            new { ItemCode = "9A765000000000001", ItemName = "拡張期血圧(その他)", CompairType = "<" , Limit1 = "85", Limit2 = "90", IncludeValue = "" , Sex= 0},
            new { ItemCode = "9A761000000000001", ItemName = "拡張期血圧(1回目)", CompairType = "<" , Limit1 = "85", Limit2 = "90", IncludeValue = "" , Sex= 0},
            new { ItemCode = "9A762000000000001", ItemName = "拡張期血圧(2回目)", CompairType = "<", Limit1 = "85", Limit2 = "90", IncludeValue = "" , Sex= 0},
            new { ItemCode = "3F015000002327101", ItemName = "中性脂肪（トリグリセリド）", CompairType = "<", Limit1 = "150", Limit2 = "300", IncludeValue = "" , Sex= 0},
            new { ItemCode = "3F070000002327101", ItemName = "HDLコレステロール", CompairType = ">", Limit1 = "39", Limit2 = "34", IncludeValue = "" , Sex= 0},
            new { ItemCode = "3F077000002327101", ItemName = "LDLコレステロール", CompairType = "<", Limit1 = "120", Limit2 = "140", IncludeValue = "" , Sex= 0},
            new { ItemCode = "3B035000002327201", ItemName = "GOT(AST)", CompairType = "<", Limit1 = "31", Limit2 = "51", IncludeValue = "" , Sex= 0},
            new { ItemCode = "3B045000002327201", ItemName = "GPT(ALT)", CompairType = "<", Limit1 = "31", Limit2 = "51", IncludeValue = "" , Sex= 0},
            new { ItemCode = "3B090000002327101", ItemName = "γ-GT(γ-GTP)", CompairType = "<", Limit1 = "51", Limit2 = "101", IncludeValue = "" , Sex= 0},
            //new { ItemCode = "3C015000002327101", ItemName = "血清クレアチニン", CompairType = "", Limit1 = "", Limit2 = "", IncludeValue = "" , Sex= 0},
            new { ItemCode = "8A065000002391901", ItemName = "eGFR", CompairType = ">", Limit1 = "60", Limit2 = "45", IncludeValue = "" , Sex= 0},
            new { ItemCode = "3D010000001927201", ItemName = "空腹時血糖", CompairType = "<", Limit1 = "100", Limit2 = "126", IncludeValue = "" , Sex= 0},
            new { ItemCode = "3D046000001920402", ItemName = "HbA1c（ＮＧＳＰ値）", CompairType = "<", Limit1 = "5.6", Limit2 = "6.5", IncludeValue = "" , Sex= 0},
            new { ItemCode = "1A020000000190111", ItemName = "尿糖", CompairType = "=", Limit1 = "+-", Limit2 = "+", IncludeValue = "(-)" , Sex= 0},
            new { ItemCode = "1A010000000190111", ItemName = "尿蛋白", CompairType = "=", Limit1 = "+-", Limit2 = "+", IncludeValue = "(-)" , Sex= 0},
            //new { ItemCode = "2A040000001930102", ItemName = "ヘマトクリット値", CompairType = "", Limit1 = "", Limit2 = "", IncludeValue = "" , Sex= 0},
            new { ItemCode = "2A030000001930101", ItemName = "血色素量(ヘモグロビン値)", CompairType = ">", Limit1 = "12.0", Limit2 = "13.0", IncludeValue = "" , Sex= 1},
            new { ItemCode = "2A030000001930101", ItemName = "血色素量(ヘモグロビン値)", CompairType = ">", Limit1 = "11.0", Limit2 = "12.0", IncludeValue = "" , Sex= 2}
            //new { ItemCode = "2A020000001930101", ItemName = "赤血球数", CompairType = "", Limit1 = "", Limit2 = "", IncludeValue = "" , Sex= 0},
            //new { ItemCode = "9A110160700000011", ItemName = "心電図(所見の有無)", CompairType = "", Limit1 = "", Limit2 = "", IncludeValue = "" , Sex= 0},
            //new { ItemCode = "9A110160800000049", ItemName = "心電図所見", CompairType = "", Limit1 = "", Limit2 = "", IncludeValue = "" , Sex= 0}
        };

            try
            {
                using (var connection = new OleDbConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string insertQuery = "INSERT INTO TKK_reference (ItemCode, ItemName, CompairType, Limit1, Limit2, IncludeValue, Sex) VALUES (?, ?, ?, ?, ?, ?, ?);";

                    foreach (var data in initialData)
                    {
                        using (var command = new OleDbCommand(insertQuery, connection))
                        {
                            command.Parameters.AddWithValue("@ItemCode", data.ItemCode);
                            command.Parameters.AddWithValue("@ItemName", data.ItemName);
                            command.Parameters.AddWithValue("@DataType", data.CompairType);
                            command.Parameters.AddWithValue("@Limit1", data.Limit1);
                            command.Parameters.AddWithValue("@Limit2", data.Limit2);
                            command.Parameters.AddWithValue("@IncludeValue", data.IncludeValue);
                            command.Parameters.AddWithValue("@Sex", data.Sex);

                            await command.ExecuteNonQueryAsync();
                        }
                    }
                    result = true;
                }
            }
            catch (Exception ex)
            {
                result = false;
                AddLogAsync($"setInitialReferenceAsyncでエラー{ex.Message}");
            }
            return result;
        }

        private async Task<bool> CreateTableAsync(string databasePath, string createTableQuery)
        {
            string connectionString = $"Provider={CommonFunctions.DBProvider};Data Source={databasePath};";
            bool result = false;

            using (var connection = new OleDbConnection(connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    using (var command = new OleDbCommand(createTableQuery, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                    result = true;
                }
                catch (Exception ex)
                {
                    result = false;
                    AddLogAsync($"CreateTableAsyncでエラー{ex.Message}");
                }
            }
            return result;
        }


        // フィールドで使う（フォームクラス内）
        private const int MaxLogItems = 1000;
        private readonly System.Diagnostics.Stopwatch _logScrollSw = System.Diagnostics.Stopwatch.StartNew();
        private int _pendingAdds = 0;

        private void AddLogAsyncToUi(string timestamp, string message)
        {
            // ❶ UIスレッド以外 → 必ずどこかに BeginInvoke でポストする
            if (listViewLog.InvokeRequired)
            {
                // コントロールのハンドルが未作成なら「フォーム」に投げる
                if (!listViewLog.IsHandleCreated)
                {
                    // フォームは this。Form.Load 以降なら大抵ハンドルあり
                    this.BeginInvoke(new Action(() => AddLogAsyncToUi(timestamp, message)));
                }
                else
                {
                    listViewLog.BeginInvoke(new Action(() => AddLogAsyncToUi(timestamp, message)));
                }
                return;
            }

            // ❷ ここからUIスレッド。ハンドル未作成の可能性に備え、作られていなければ後で再試行
            if (!listViewLog.IsHandleCreated)
            {
                // Handle ができたら再度流す
                void handler(object s, EventArgs e)
                {
                    listViewLog.HandleCreated -= handler;
                    AddLogAsyncToUi(timestamp, message);
                }
                listViewLog.HandleCreated += handler;
                return;
            }

            // ❸ 追加 & 描画負荷を抑制
            listViewLog.BeginUpdate();
            try
            {
                // 念のため表示モード
                if (listViewLog.View != View.Details) listViewLog.View = View.Details;

                var item = new ListViewItem(timestamp);
                item.SubItems.Add(message);
                listViewLog.Items.Add(item);
                _pendingAdds++;

                // 上限超過はまとめて削除
                if (listViewLog.Items.Count > MaxLogItems)
                {
                    int overflow = listViewLog.Items.Count - MaxLogItems;
                    for (int i = 0; i < overflow; i++)
                        listViewLog.Items.RemoveAt(0);
                }

                // スクロールは200msに1回だけ
                if (_logScrollSw.ElapsedMilliseconds >= 200 && listViewLog.Items.Count > 0)
                {
                    if (_pendingAdds > 0)
                    {
                        listViewLog.EnsureVisible(listViewLog.Items.Count - 1);
                        _pendingAdds = 0;
                    }
                    _logScrollSw.Restart();
                }
            }
            finally
            {
                listViewLog.EndUpdate();
            }
        }


        

        /// <summary>
        /// ログファイルを準備（古いログをリネーム）
        /// </summary>
        private  void PrepareLogFiles()
        {
            // ログフォルダが存在しない場合は作成
            if (!Directory.Exists(OQSFolder))
            {
                Directory.CreateDirectory(OQSFolder);
            }
                        
        }

        private bool IsDateStringAfterNow(string dateString)
        {
            try
            {
                // 日付文字列を変換
                var targetDate = new DateTime(
                    int.Parse(dateString.Substring(0, 4)),   // 年
                    int.Parse(dateString.Substring(4, 2)),   // 月
                    int.Parse(dateString.Substring(6, 2)),   // 日
                    int.Parse(dateString.Substring(8, 2)),   // 時
                    int.Parse(dateString.Substring(10, 2)),  // 分
                    int.Parse(dateString.Substring(12, 2))   // 秒
                );

                // 現在時刻と比較
                return targetDate > DateTime.Now;
            }
            catch (Exception)
            {
                // 不正な文字列が渡された場合の処理
                AddLogAsync($"Invalid date string: {dateString}");
                return false;
            }
        }
                
        private void SetConnectionStatus()
        {
            Color activeMDB = Color.LightCoral;
            Color activePG = Color.SkyBlue;
            Color activeDyna = Color.LightCoral;
            Color activeOQS = Color.NavajoWhite;
            Color activeText = SystemColors.ControlText;

            Color inactive = SystemColors.Control;

            if (Properties.Settings.Default.DBtype == "pg")
            {
                buttonPG.BackColor = activePG;
                buttonMDB.BackColor = inactive;
            }
            else
            {
                buttonPG.BackColor = inactive;
                buttonMDB.BackColor = activeMDB;
            }

            buttonDynamics.BackColor = activeDyna;
            buttonOQSFolder.BackColor = activeOQS;

            pictureBoxDB.Image        = Properties.Resources.Hourglass;
            pictureBoxDynamics.Image  = Properties.Resources.Hourglass;
            pictureBoxOQSFolder.Image = Properties.Resources.Hourglass;

        }

       
        private void buttonExit_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("終了しますか？", "終了", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                if (animationTimer.Enabled)
                {
                    animationTimer.Stop();
                    animationTimer.Dispose();
                }

                // NotifyIconの解放
                if (notifyIcon1 != null)
                {
                    notifyIcon1.BalloonTipClicked -= NotifyIcon_BalloonTipClicked; // イベント解除
                    notifyIcon1.Dispose();
                }

                Application.Exit();
            }
        }

        private void toolStripComboBoxDBProviders_SelectedIndexChanged(object sender, EventArgs e)
        {
            CommonFunctions.DBProvider = toolStripComboBoxDBProviders.Text;

            AddLogAsync($"データベースプロバイダーを{CommonFunctions.DBProvider}に変更しました");
        }

        private void SetupYZKSindicator() //xmlは常に取得
        {
            Color activeYZ = Color.LightGreen;
            Color activeKS = Color.LightCoral;
            Color acticePDF = Color.Violet;
            Color activeXML = Color.Orange;
            Color activeText = SystemColors.ControlText;

            buttonYZXML.BackColor = activeXML;
            buttonYZXML.ForeColor = activeText;

            buttonYZ.BackColor = activeYZ;
            buttonYZ.ForeColor = activeText;
            buttonSR.BackColor = SystemColors.Control;
            buttonSR.ForeColor = SystemColors.ControlText;

            buttonSR.BackColor = activeYZ;
            buttonSR.ForeColor = activeText;
            
            //健診
            buttonKS.BackColor = activeKS;
            buttonKS.BackColor = activeKS;
            buttonKSXML.BackColor = activeXML;
            
        }

        private string GenerateXML(dynamic ptData, string folderPath, string startDate, string endDate, string medicalInstitutionCode, int category)
        {
            int fileCategory;
            try
            {
                // ArbitraryFileIdentifier を生成
                string currentDate = DateTime.Now.ToString("yyMMdd");
                string currentTime = DateTime.Now.ToString("HHmm");
                string arbitraryFileIdentifier = ptData.Id.ToString();

                // FileSymbol を決定
                string fileSymbol;
                if (category > 100) //健診
                {
                    fileSymbol = "TKK";
                }
                else
                {
                    fileSymbol = "YZK";
                }
                fileCategory = category % 10;

                // ファイル名を生成
                string fileName = $"{fileSymbol}siquc01req_{fileCategory:00}{currentDate}{currentTime}{ptData.Id}.xml";

                // XMLファイルのフルパス
                string filePath = Path.Combine(folderPath, "req", fileName);

                // フォルダが存在しない場合は作成
                string directoryPath = Path.Combine(folderPath, "req");
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // XmlWriterSettingsの設定
                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    Encoding = Encoding.GetEncoding("Shift_JIS"),
                    OmitXmlDeclaration = true // ヘッダーをカスタムで出力するため省略
                };

                // XMLコンテンツを構築
                using (XmlWriter writer = XmlWriter.Create(filePath, settings))
                // using (XmlWriter writer = XmlWriter.Create(filePath, new XmlWriterSettings { Indent = true, Encoding = System.Text.Encoding.GetEncoding("Shift_JIS") }))
                {
                    writer.WriteStartDocument();
                    // 手動でカスタムヘッダーを記述
                    writer.WriteRaw("<?xml version=\"1.0\" encoding=\"Shift_JIS\" standalone=\"no\"?>\n");
                    writer.WriteStartElement("XmlMsg");

                    // MessageHeader セクション
                    writer.WriteStartElement("MessageHeader");
                    writer.WriteElementString("MedicalInstitutionCode", medicalInstitutionCode);
                    writer.WriteElementString("InsurerNumber", ptData.InsurerNumber);
                    writer.WriteElementString("InsuranceCardSymbol", ptData.InsuranceCardSymbol);
                    writer.WriteElementString("InsuredPersonIdentificationNumber", ptData.InsuredPersonIdentificationNumber);
                    writer.WriteElementString("BranchNumber", ptData.BranchNumber);
                    writer.WriteElementString("ArbitraryFileIdentifier", arbitraryFileIdentifier);
                    writer.WriteEndElement(); // MessageHeader

                    // MessageBody セクション
                    writer.WriteStartElement("MessageBody");
                    if (category < 100) //薬剤
                    {
                        writer.WriteElementString("StartDate", startDate);
                        writer.WriteElementString("EndDate", endDate);
                    }
                    writer.WriteElementString("FileCategory", fileCategory.ToString());
                    writer.WriteElementString("PrDiInfClassification", "1");
                    writer.WriteElementString("MedicalTreatmentFlag", "1");
                    writer.WriteEndElement(); // MessageBody

                    writer.WriteEndElement(); // XmlMsg
                    writer.WriteEndDocument();
                }

                // 成功時にファイルパスを返す
                return filePath;
            }
            catch (Exception ex)
            {
                // エラー時は空文字列を返す
                AddLogAsync($"Error: {ex.Message}");
                return string.Empty;
            }
        }

        private async Task<bool> ProcessResAsync()
        {
            bool AllDataProcessed = true;

            try
            {
                string resFolder = Path.Combine(Properties.Settings.Default.OQSFolder, "res");
                string gazouFolder = Properties.Settings.Default.RSBgazouFolder;
                string[] RSBname = { "", Properties.Settings.Default.YZname, Properties.Settings.Default.KSname };

                if (!Directory.Exists(resFolder))
                {
                    AddLogAsync($"エラー: resフォルダが見つかりません。{resFolder}");
                    return true;
                }

                AddLogAsync("resフォルダの処理を開始します");
                string[] fileList = Directory.GetFiles(resFolder);

                if (!await CommonFunctions.WaitForDbUnlock(1000))
                {
                    AddLogAsync("データベースがロックされています。ProcessResAsyncをスキップします");
                }
                else
                {
                    using (var connection = CommonFunctions.GetDbConnection(ReadOnly: false))
                    {
                        await ((DbConnection)connection).OpenAsync();

                        var command = connection.CreateCommand();
                        command.CommandText = "SELECT * FROM reqResults WHERE resFile IS NULL";

                        var records = new List<Dictionary<string, object>>();

                        using (var reader = await ((DbCommand)command).ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                //var record = new Dictionary<string, object>();
                                var record = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    record[reader.GetName(i)] = reader.GetValue(i);
                                }
                                records.Add(record);
                            }
                        }

                        bool RSBreloadFlag = false;
                        bool RSBXMLreloadFlag = false;

                        foreach (var record in records)
                        {
                            bool isProcessed = false;

                            long PtID = Convert.ToInt64(record["PtID"]);
                            int Category = record["category"] != DBNull.Value ? Convert.ToInt32(record["category"]) : 0;
                            string PtName = Convert.ToString(record["PtName"]) ?? "";
                            //string PtName = (string)record["PtName"];
                            object resultId = record["ID"];
                            string reqFilePath = Convert.ToString(record["reqFile"]) ?? ""; 
                            //string reqFilePath = (string)record["reqFile"];

                            string reqfileName = Path.GetFileName(reqFilePath);
                            string resFileName = reqfileName.Replace("req", "res");
                            string resFilePath = Path.Combine(resFolder, resFileName);
                            string resBaseFileName = Path.GetFileNameWithoutExtension(resFilePath);

                            foreach (string file in fileList)
                            {
                                if (Path.GetFileNameWithoutExtension(file) == resBaseFileName)
                                {
                                    string extension = Path.GetExtension(file).ToLower();
                                    string messageContent = "";

                                    switch (extension)
                                    {
                                        case ".xml":
                                            AddLogAsync($"{PtID}:{PtName}resフォルダにxmlファイルが見つかりました: {resFileName}");
                                            XmlDocument xmlDoc = new XmlDocument();
                                            try
                                            {
                                                xmlDoc.Load(file);
                                                var resultCodeNode = xmlDoc.SelectSingleNode("//ResultCode");

                                                if (resultCodeNode != null && resultCodeNode.InnerText == "1")
                                                {
                                                    if (resFileName.StartsWith("YZK"))
                                                    {
                                                        messageContent = await ProcessDrugInfoAsync2(PtID, xmlDoc);

                                                        //LLM自動問い合わせ
                                                        if (Properties.Settings.Default.DBtype == "pg" &&  messageContent.StartsWith("成功：") && Properties.Settings.Default.AIauto)
                                                        {
                                                            await CommonFunctions.AutoLLMAsync(
                                                                ptId: PtID / 10,
                                                                timeoutMsOverride: Properties.Settings.Default.LLMtimeout * 1000,
                                                                minDaysBetween: 1,                       // 直近1日以内に同タイトルがあれば再取得しない
                                                                ct: CancellationToken.None);
                                                            await AddLogAsync("LLM自動問い合わせを終了します");
                                                        }
                                                    }
                                                    else if (resFileName.StartsWith("TKK"))
                                                    {
                                                        messageContent = await ProcessTKKAsync(PtID, xmlDoc, connection);

                                                        if (Properties.Settings.Default.KensinFileCategory > 0 && TKKdate.TryGetValue(PtID, out string lastTKKdate))
                                                        {
                                                            string lastReceived = await getLastReceivedDate(connection, PtID, 101);
                                                            if (lastTKKdate.CompareTo(lastReceived) > 0)
                                                            {
                                                                AddLogAsync("新しい健診結果が見つかりましたのでPDFを要求します");
                                                                MakeReq(101, dynaTable, PtID);
                                                                AllDataProcessed = false;
                                                            }
                                                        }
                                                    }

                                                    if (Properties.Settings.Default.KeepXml && Properties.Settings.Default.RSBXml)
                                                    {
                                                        RSBXMLreloadFlag = true;
                                                    }
                                                }
                                                else
                                                {
                                                    var xmlNode = xmlDoc.SelectSingleNode("//MessageContents");
                                                    messageContent = xmlNode != null ? xmlNode.InnerText : "<MessageContents> タグが見つかりません";
                                                }
                                            }
                                            catch
                                            {
                                                messageContent = "XMLファイルの読み込みに失敗しました";
                                            }

                                            if (!Properties.Settings.Default.KeepXml)
                                            {
                                                File.Delete(file);
                                            }
                                            break;

                                        case ".pdf":
                                            int ReadCategory = (int)Math.Floor(Math.Log10(Math.Abs(Category)));
                                            string rsbDate = DateTime.Now.ToString("yyyy_MM_dd");

                                            if (Properties.Settings.Default.KensinFileCategory == 3 && TKKdate.TryGetValue(PtID, out string value))
                                            {
                                                rsbDate = $"{value.Substring(0, 4)}_{value.Substring(4, 2)}_{value.Substring(6, 2)}";
                                            }

                                            if ((ReadCategory == 1 && Properties.Settings.Default.DrugFileCategory < 10) ||
                                                (ReadCategory == 2 && Properties.Settings.Default.KensinFileCategory < 4))
                                            {
                                                string targetFileName = $"{PtID / 10}~01~{rsbDate}~{RSBname[ReadCategory]}~RSB.pdf";
                                                string rsbFilePath = Path.Combine(gazouFolder, targetFileName);

                                                File.Move(file, rsbFilePath);
                                                resFilePath = rsbFilePath;
                                                messageContent = "成功";
                                                RSBreloadFlag = true;
                                            }
                                            else
                                            {
                                                string RSBcategory = (ReadCategory == 1) ? "薬歴data" : "健診data";
                                                string mynumberFoler = Path.Combine(Properties.Settings.Default.RSBServerFolder, "myNumber");

                                                await MoveFileToPatientFolder(mynumberFoler, (int)(PtID / 10), file, rsbDate, RSBcategory);
                                            }
                                            break;
                                    }

                                    var update = connection.CreateCommand();
                                    update.CommandText = "UPDATE reqResults SET resFile = @resFile, resDate = @resDate, result = @result WHERE ID = @ID";
                                    CommonFunctions.AddDbParameter(update, "@resFile", resFilePath ?? "");
                                   
                                    if(Properties.Settings.Default.DBtype == "pg")
                                    {
                                        var nowUns = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                                        CommonFunctions.AddDbParameter(update, "@resDate", nowUns);
                                    }
                                    else
                                    {
                                        CommonFunctions.AddDbParameter(update, "@resDate", DateTime.Now);
                                        
                                    }
                                    CommonFunctions.AddDbParameter(update, "@result", messageContent ?? "");
                                    CommonFunctions.AddDbParameter(update, "@ID", resultId);

                                    CommonFunctions.DataDbLock = true;
                                    await ((DbCommand)update).ExecuteNonQueryAsync();
                                    CommonFunctions.DataDbLock = false;

                                    isProcessed = true;
                                }
                            }

                            if (!isProcessed)
                            {
                                AddLogAsync($"{resBaseFileName}が未着です");
                                AllDataProcessed = false;
                            }
                        }

                        if (Properties.Settings.Default.RSBReload && RSBreloadFlag)
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = Properties.Settings.Default.RSBUrl,
                                UseShellExecute = true
                            });
                        }

                        if (RSBXMLreloadFlag)
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = Properties.Settings.Default.RSBXmlURL,
                                UseShellExecute = true
                            });
                        }
                    }
                }

                AddLogAsync("ProcessResAsyncを終了します");
                return AllDataProcessed;
            }
            catch (Exception ex)
            {
                AddLogAsync($"ProcessResAsync処理中にエラーが発生しました: {ex.Message}");
                CommonFunctions.DataDbLock = false;
                return false;
            }
        }

        private async Task MoveFileToPatientFolder(string baseDir, long ptIDmain, string sourceFilePath, string rsbDate, string rsbCategory)
        {
            try
            {
                // PtIDmain の 1の位を取得
                int lastDigit = (int)(ptIDmain % 10);

                // サブフォルダのパス
                string subFolder = Path.Combine(baseDir, lastDigit.ToString());
                string patientFolder = Path.Combine(subFolder, ptIDmain.ToString());

                // フォルダを非同期で作成（存在しない場合のみ）
                if (!Directory.Exists(baseDir))
                {
                    Directory.CreateDirectory(baseDir);
                }
                if (!Directory.Exists(subFolder))
                {
                    Directory.CreateDirectory(subFolder);
                }
                if (!Directory.Exists(patientFolder))
                {
                    Directory.CreateDirectory(patientFolder);
                }

                // ファイル名のベース部分
                string fileBaseName = $"{rsbDate}_{ptIDmain}_";
                string fileExtension = ".pdf";

                // XX の部分を決定（50 から開始し、存在しないファイル名を探す）
                int fileIndex = 50;
                string destinationFilePath;
                do
                {
                    string fileName = $"{fileBaseName}{fileIndex}_{rsbCategory}{fileExtension}";
                    destinationFilePath = Path.Combine(patientFolder, fileName);
                    fileIndex++;
                } while (File.Exists(destinationFilePath));

                // ファイルを非同期で移動
                await Task.Run(() => File.Move(sourceFilePath, destinationFilePath));

            }
            catch (Exception ex)
            {
                AddLogAsync($"CopyFiletoPatientFolderでエラー: {ex.Message}");
            }
        }

        private async Task<string> getLastReceivedDate(IDbConnection connection, long ptId, int category)
        {
            string sql;
            if (connection is NpgsqlConnection)
            {
                // PostgreSQL: LIMIT 1
                sql = @"SELECT resDate FROM reqResults WHERE (PtID / 10) = @PtIDMain AND category = @Category AND result LIKE '成功%' ORDER BY ID DESC LIMIT 1;";
            }
            else
            {
                // Access: TOP 1
                sql = @"SELECT TOP 1 resDate FROM reqResults WHERE (PtID \ 10) = ? AND category = ? AND result LIKE '成功%' ORDER BY ID DESC;";
            }

            string returnDate = "00000000";

            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;

                    if (connection is NpgsqlConnection)
                    {
                        CommonFunctions.AddDbParameter(command, "@PtIDMain", (long)(ptId / 10));
                        CommonFunctions.AddDbParameter(command, "@Category", category);
                    }
                    else
                    {
                        CommonFunctions.AddDbParameter(command, "?", (long)(ptId / 10));
                        CommonFunctions.AddDbParameter(command, "?", category);
                    }

                    var result = await ((DbCommand)command).ExecuteScalarAsync();

                    if (result != null && result is DateTime resDate)
                    {
                        returnDate = resDate.ToString("yyyyMMdd");
                    }
                }

                return returnDate;
            }
            catch (Exception ex)
            {
                AddLogAsync($"getLastReceivedDateでエラーが発生しました: {ex.Message}");
                return returnDate;
            }
        }



        private async Task<string> ProcessTKKAsync(long ptID, XmlDocument xmlDoc, IDbConnection dbConnection)
        {
            AddLogAsync($"{ptID}の特定健診xmlを処理します");

            string lastTKKdate = "00000000";
            long ptIDMain = ptID / 10;
            int recordCount = 0;

            try
            {
                XmlNode headerNode = xmlDoc.SelectSingleNode("/XmlMsg/MessageHeader/QualificationsInfo");
                if (headerNode == null)
                {
                    return "エラー：xmlヘッダー情報の取得に失敗しました";
                }

                string ptName = GetNodeValue(headerNode, "Name");
                string ptKana = GetNodeValue(headerNode, "KanasName");
                string ptBirth = GetNodeValue(headerNode, "Birthday");
                int sex = (int)NzConvert(GetNodeValue(headerNode, "AdministrativeGenderCode"));

                string checkSql = "SELECT COUNT(*) FROM TKK_history WHERE PtIDmain = @PtIDmain AND EffectiveTime = @EffectiveTime";
                string insertSql = @"
                    INSERT INTO TKK_history (
                        PtIDmain, PtName, PtKana, Sex, EffectiveTime, ItemCode, ItemName,
                        DataType, DataValue, Unit, Oid, DataValueName
                    ) VALUES (
                        @PtIDmain, @PtName, @PtKana, @Sex, @EffectiveTime, @ItemCode, @ItemName,
                        @DataType, @DataValue, @Unit, @Oid, @DataValueName
                    )";

                XmlNodeList TKKresults = xmlDoc.SelectNodes("//SpecificHealthCheckupInfo");

                foreach (XmlNode TKKresult in TKKresults)
                {
                    string effectiveTime = GetNodeValue(TKKresult, "EffectiveTime");
                    if (effectiveTime.CompareTo(lastTKKdate) > 0) lastTKKdate = effectiveTime;

                    // 既登録確認
                    using (var checkCommand = dbConnection.CreateCommand())
                    {
                        checkCommand.CommandText = checkSql;
                        CommonFunctions.AddDbParameter(checkCommand, "@PtIDmain", ptIDMain);
                        CommonFunctions.AddDbParameter(checkCommand, "@EffectiveTime", effectiveTime);

                        int TKKcount = Convert.ToInt32(await ((DbCommand)checkCommand).ExecuteScalarAsync());
                        if (TKKcount > 0) continue;
                    }

                    var checkupInfos = TKKresult.SelectNodes("HealthCheckupResultAndQuestionInfos/HealthCheckupResultAndQuestionInfo");

                    foreach (XmlNode info in checkupInfos)
                    {
                        string itemCode = GetNodeValue(info, "ItemCode");
                        string dataValue = GetNodeValue(info, "DataValue");
                        dataValue = dataValue.Length > 64 ? dataValue.Substring(0, 64) : dataValue;
                        string itemName = GetNodeValue(info, "ItemName");
                        string dataType = GetNodeValue(info, "DataType");
                        string unit = GetNodeValue(info, "Unit");
                        string oid = GetNodeValue(info, "Oid");
                        string dataValueName = GetNodeValue(info, "DataValueName");
                        dataValueName = dataValueName.Length > 64 ? dataValueName.Substring(0, 64) : dataValueName;

                        using (var insertCommand = dbConnection.CreateCommand())
                        {
                            insertCommand.CommandText = insertSql;
                            CommonFunctions.AddDbParameter(insertCommand, "@PtIDmain", ptIDMain);
                            CommonFunctions.AddDbParameter(insertCommand, "@PtName", ptName);
                            CommonFunctions.AddDbParameter(insertCommand, "@PtKana", ptKana);
                            CommonFunctions.AddDbParameter(insertCommand, "@Sex", sex);
                            CommonFunctions.AddDbParameter(insertCommand, "@EffectiveTime", effectiveTime);
                            CommonFunctions.AddDbParameter(insertCommand, "@ItemCode", itemCode);
                            CommonFunctions.AddDbParameter(insertCommand, "@ItemName", itemName);
                            CommonFunctions.AddDbParameter(insertCommand, "@DataType", dataType);
                            CommonFunctions.AddDbParameter(insertCommand, "@DataValue", dataValue);
                            CommonFunctions.AddDbParameter(insertCommand, "@Unit", unit);
                            CommonFunctions.AddDbParameter(insertCommand, "@Oid", oid);
                            CommonFunctions.AddDbParameter(insertCommand, "@DataValueName", dataValueName);

                            CommonFunctions.DataDbLock = true;
                            await ((DbCommand)insertCommand).ExecuteNonQueryAsync();
                            CommonFunctions.DataDbLock = false;
                        }
                        recordCount++;
                    }
                }

                if (recordCount > 0)
                {
                    string message = $"{ptName}さんの特定健診{recordCount}件取得";
                    ShowNotification($"{ptIDMain}", message);
                    AddLogAsync(message);
                }

                TKKdate[ptID] = lastTKKdate;

                return $"成功：#{lastTKKdate}# xml健診から{recordCount}件のレコードを読み込みました";
            }
            catch (Exception ex)
            {
                return "エラー：" + ex.Message;
            }
        }

             private async Task<string> ProcessDrugInfoAsync2(long ptID, XmlDocument xmlDoc)
        {
            // --- マッピング（元コード準拠） ---
            var elementMappings = new Dictionary<string, List<string>>
                {
                    { "MonthInf", new List<string> { "MeTrMonthInf", "CzDiMonthInf", "ShPrMonthInf" } },
                    { "Org", new List<string> { "DiOrg", "CzDiOrg", "ShPrOrg" } },
                    { "DiHCd", new List<string> { "MeTrDiHCd", "CzMeTrDiHCd", "ShMeTrDiHCd" } },
                    { "HCd", new List<string> { "PrlsHCd", "CzPrlsHCd", "ShPrHCd" } },
                    { "DiHNm", new List<string> { "MeTrDiHNm", "CzMeTrDiHNm", "ShMeTrDiHNm" } },
                    { "Month", new List<string> { "MeTrMonth", "CzDiMonth", "ShPrMonth" } },
                    { "HNm", new List<string> { "PrlsHNm", "CzPrlsHNm", "ShPrHNm" } },
                    { "IsOrg", new List<string> { "PrIsOrg", "CzPrIsOrg", "ShPrIsOrg" } },
                    { "Cl", new List<string> { "InOut", "CzPrCl", "ShPrCl" } },
                    { "DateInf", new List<string> { "DiDateInfs/DiDateInf", "CzDiDateInfs/CzDiDateInf", "ShPrDateInfs/ShPrDateInf" } },
                    { "DiDate", new List<string> { "DiDate", "CzDiDate", "ShPrDate" } },
                    { "PrDate", new List<string> { "PrDate", "CzPrDate", "ShPrDate" } },
                    { "DrugInf", new List<string> { "DrugInfs/DrugInf", "CzDrugInfs/CzDrugInf", "ShDrugInfs/ShDrugInf" } },
                    { "DrugC", new List<string> { "DrugC", "CzDrugC", "ShDrugC" } },
                    { "Qua1", new List<string> { "Qua1", "CzQua1", "ShQua1" } },
                    { "UsageN", new List<string> { "UsageN", "CzUsageN", "ShUsageN" } },
                    { "SpInst", new List<string> { "SpInst", "ShSpInst", "CzSpInst" } },
                    { "Times", new List<string> { "Times", "CzTimes", "ShTimes" } },
                    { "IngreN", new List<string> { "IngreN", "CzIngreN", "ShIngreN" } },
                    { "UsageCl", new List<string> { "MeTrIdCl", "CzUsageCl", "ShUsageCl" } },
                    { "Unit", new List<string> { "Unit", "CzUnit", "ShUnit" } },
                    { "DrugN", new List<string> { "DrugN", "CzDrugN", "ShDrugN" } }
                };

            // 処理するルート
            string[] rootNodes = {
                "/XmlMsg/MessageBody/MeTrMonthInf",
                "/XmlMsg/MessageBody/ShPrInf/ShPrMonthInf",
                "/XmlMsg/MessageBody/CzDiInf/CzDiMonthInf"
            };

            try
            {
                using (IDbConnection db = CommonFunctions.GetDbConnection(false))
                {
                    await ((DbConnection)db).OpenAsync();

                    long ptIDMain = ptID / 10;
                    string receiveDate = DateTime.Now.ToString("yyyyMMdd");

                    XmlNode headerNode = xmlDoc.SelectSingleNode("/XmlMsg/MessageHeader/QuaInf");
                    if (headerNode == null) return "エラー：xmlヘッダー情報の取得に失敗しました";

                    string ptName = GetNodeValue(headerNode, "Name");
                    string ptKana = GetNodeValue(headerNode, "KanaName");
                    string ptBirth = GetNodeValue(headerNode, "Birth");
                    int ptSex = (int)NzConvert(GetNodeValue(headerNode, "AdmGendCode"));

                    int insertedCount = 0;
                    int sinryoCount = 0;
                    int revisedCount = 0;

                    // SQL（?変換は共通で実施）
                    string insertSql = CommonFunctions.ConvertSqlForOleDb(@"
                        INSERT INTO drug_history (
                            Source, PtID, PtIDmain, ReceiveDate, PtName, PtKana, Birth, diOrg, MeTrDiHCd,
                            prlsHCd, MeTrDiHNm, MeTrMonth, prlsHNm, prIsOrg, InOut, DiDate, PrDate,
                            DrugC, Qua1, UsageN, Times, IngreN, MeTrIdCl, Unit, DrugN, Revised
                        ) VALUES (
                            @Source, @PtID, @PtIDmain, @ReceiveDate, @PtName, @PtKana, @Birth, @diOrg, @MeTrDiHCd,
                            @prlsHCd, @MeTrDiHNm, @MeTrMonth, @prlsHNm, @prIsOrg, @InOut, @DiDate, @PrDate,
                            @DrugC, @Qua1, @UsageN, @Times, @IngreN, @MeTrIdCl, @Unit, @DrugN, @Revised
                        )");

                    string selectSql = CommonFunctions.ConvertSqlForOleDb(@"
                        SELECT ID, Source
                          FROM drug_history
                         WHERE PtIDmain = @PtIDmain
                           AND (MeTrDiHCd = @MIcode OR PrlsHCd = @MIcode2)
                           AND DiDate = @DiDate");

                    string updateSql = CommonFunctions.ConvertSqlForOleDb(
                        "UPDATE drug_history SET Revised = True WHERE ID = @ID");

                    // ===== DB種別分岐：PGはCOPYで極速、OleDb/その他はPrepared＋TX =====
                    bool isPg = db.GetType().FullName != null && db.GetType().FullName.IndexOf("Npgsql", StringComparison.OrdinalIgnoreCase) >= 0;

                    using (IDbTransaction tx = db.BeginTransaction())
                    {
                        if (isPg)
                        {
                            // --- PostgreSQL: まずメモリに挿入候補を貯め、既存チェック→COPY一括 ---
                            // DataTable をバッファとして利用（外部クラス不要）
                            DataTable dt = new DataTable();
                            dt.Columns.Add("Source", typeof(int));
                            dt.Columns.Add("PtID", typeof(long));
                            dt.Columns.Add("PtIDmain", typeof(long));
                            dt.Columns.Add("ReceiveDate", typeof(string));
                            dt.Columns.Add("PtName", typeof(string));
                            dt.Columns.Add("PtKana", typeof(string));
                            dt.Columns.Add("Birth", typeof(string));
                            dt.Columns.Add("diOrg", typeof(int));
                            dt.Columns.Add("MeTrDiHCd", typeof(string));
                            dt.Columns.Add("prlsHCd", typeof(string));
                            dt.Columns.Add("MeTrDiHNm", typeof(string));
                            dt.Columns.Add("MeTrMonth", typeof(string));
                            dt.Columns.Add("prlsHNm", typeof(string));
                            dt.Columns.Add("prIsOrg", typeof(int));
                            dt.Columns.Add("InOut", typeof(int));
                            dt.Columns.Add("DiDate", typeof(string));
                            dt.Columns.Add("PrDate", typeof(string));
                            dt.Columns.Add("DrugC", typeof(string));
                            dt.Columns.Add("Qua1", typeof(float));
                            dt.Columns.Add("UsageN", typeof(string));
                            dt.Columns.Add("Times", typeof(int));
                            dt.Columns.Add("IngreN", typeof(string));
                            dt.Columns.Add("MeTrIdCl", typeof(int));
                            dt.Columns.Add("Unit", typeof(string));
                            dt.Columns.Add("DrugN", typeof(string));
                            dt.Columns.Add("Revised", typeof(bool));

                            // 既存チェック用 Prepared
                            using (IDbCommand checkCmd = db.CreateCommand())
                            using (IDbCommand updCmd = db.CreateCommand())
                            {
                                checkCmd.Transaction = tx;
                                updCmd.Transaction = tx;
                                checkCmd.CommandText = selectSql;
                                updCmd.CommandText = updateSql;

                                foreach (string rootPath in rootNodes)
                                {
                                    XmlNodeList monthInfList = xmlDoc.SelectNodes(rootPath);
                                    if (monthInfList == null || monthInfList.Count == 0) continue;

                                    for (int iM = 0; iM < monthInfList.Count; iM++)
                                    {
                                        XmlNode monthInfNode = monthInfList[iM];
                                        int Source = (monthInfNode.Name == "MeTrMonthInf") ? 1 :
                                                     (monthInfNode.Name == "CzDiMonthInf") ? 2 :
                                                     (monthInfNode.Name == "ShPrMonthInf") ? 3 : 0;

                                        int diOrg = (int)NzConvert(GetNodeValue(monthInfNode, GetMatchingNodeName(monthInfNode, elementMappings, "Org")), -1);
                                        string meTrDiHCd = GetNodeValue(monthInfNode, GetMatchingNodeName(monthInfNode, elementMappings, "DiHCd"));
                                        string prlsHCd = GetNodeValue(monthInfNode, GetMatchingNodeName(monthInfNode, elementMappings, "HCd"));
                                        string meTrDiHNm = GetNodeValue(monthInfNode, GetMatchingNodeName(monthInfNode, elementMappings, "DiHNm"));
                                        string meTrMonth = GetNodeValue(monthInfNode, GetMatchingNodeName(monthInfNode, elementMappings, "Month"));
                                        string prlsHNm = GetNodeValue(monthInfNode, GetMatchingNodeName(monthInfNode, elementMappings, "HNm"));
                                        int prIsOrg = (int)NzConvert(GetNodeValue(monthInfNode, GetMatchingNodeName(monthInfNode, elementMappings, "IsOrg")), -1);
                                        int inOut = (int)NzConvert(GetNodeValue(monthInfNode, GetMatchingNodeName(monthInfNode, elementMappings, "Cl")), -1);

                                        string MIcode = GetMedicalInstitutionCode(meTrDiHCd, prlsHCd);
                                        if (prIsOrg == -1) prIsOrg = (MIcode == Properties.Settings.Default.MCode) ? 1 : 2;

                                        XmlNodeList dateInfList = monthInfNode.SelectNodes(GetMatchingNodeName(monthInfNode, elementMappings, "DateInf"));
                                        for (int iD = 0; iD < dateInfList.Count; iD++)
                                        {
                                            XmlNode dateInfNode = dateInfList[iD];
                                            string diDate = GetNodeValue(dateInfNode, GetMatchingNodeName(dateInfNode, elementMappings, "DiDate"));
                                            string prDate = GetNodeValue(dateInfNode, GetMatchingNodeName(dateInfNode, elementMappings, "PrDate"));

                                            // 既存チェック
                                            checkCmd.Parameters.Clear();
                                            CommonFunctions.AddDbParameter(checkCmd, "@PtIDmain", ptIDMain);
                                            CommonFunctions.AddDbParameter(checkCmd, "@MIcode", MIcode);
                                            CommonFunctions.AddDbParameter(checkCmd, "@MIcode2", MIcode);
                                            CommonFunctions.AddDbParameter(checkCmd, "@DiDate", diDate);

                                            bool doRead = true;
                                            List<int> idsToRev = new List<int>();

                                            using (DbDataReader rd = await ((DbCommand)checkCmd).ExecuteReaderAsync())
                                            {
                                                while (await rd.ReadAsync())
                                                {
                                                    int rid = rd.GetInt32(0);
                                                    int rsrc = rd.IsDBNull(1) ? 9 : rd.GetInt32(1);
                                                    if (rsrc > Source) idsToRev.Add(rid);
                                                    else doRead = false;
                                                }
                                            }
                                            for (int ii = 0; ii < idsToRev.Count; ii++)
                                            {
                                                updCmd.Parameters.Clear();
                                                CommonFunctions.AddDbParameter(updCmd, "@ID", idsToRev[ii]);
                                                await ((DbCommand)updCmd).ExecuteNonQueryAsync();
                                            }

                                            if (doRead)
                                            {
                                                XmlNodeList drugInfList = dateInfNode.SelectNodes(GetMatchingNodeName(dateInfNode, elementMappings, "DrugInf"));
                                                for (int iR = 0; iR < drugInfList.Count; iR++)
                                                {
                                                    XmlNode drugInfNode = drugInfList[iR];

                                                    string usage = GetNodeValue(drugInfNode, GetMatchingNodeName(drugInfNode, elementMappings, "UsageN"));
                                                    string spinst = GetNodeValue(drugInfNode, GetMatchingNodeName(drugInfNode, elementMappings, "SpInst"));
                                                    if (!string.IsNullOrEmpty(spinst)) usage = string.IsNullOrEmpty(usage) ? spinst : usage + "/" + spinst;

                                                    DataRow row = dt.NewRow();
                                                    row["Source"] = Source;
                                                    row["PtID"] = ptID;
                                                    row["PtIDmain"] = ptIDMain;
                                                    row["ReceiveDate"] = receiveDate;
                                                    row["PtName"] = ptName;
                                                    row["PtKana"] = ptKana;
                                                    row["Birth"] = ptBirth;
                                                    row["diOrg"] = diOrg;
                                                    row["MeTrDiHCd"] = meTrDiHCd;
                                                    row["prlsHCd"] = prlsHCd;
                                                    row["MeTrDiHNm"] = meTrDiHNm;
                                                    row["MeTrMonth"] = meTrMonth;
                                                    row["prlsHNm"] = prlsHNm;
                                                    row["prIsOrg"] = prIsOrg;
                                                    row["InOut"] = inOut;
                                                    row["DiDate"] = diDate;
                                                    row["PrDate"] = prDate;
                                                    row["DrugC"] = GetNodeValue(drugInfNode, GetMatchingNodeName(drugInfNode, elementMappings, "DrugC"));
                                                    row["Qua1"] = NzConvert(GetNodeValue(drugInfNode, GetMatchingNodeName(drugInfNode, elementMappings, "Qua1")));
                                                    row["UsageN"] = usage;
                                                    row["Times"] = (int)NzConvert(GetNodeValue(drugInfNode, GetMatchingNodeName(drugInfNode, elementMappings, "Times")));
                                                    row["IngreN"] = GetNodeValue(drugInfNode, GetMatchingNodeName(drugInfNode, elementMappings, "IngreN"));
                                                    row["MeTrIdCl"] = (int)NzConvert(GetNodeValue(drugInfNode, GetMatchingNodeName(drugInfNode, elementMappings, "UsageCl")));
                                                    row["Unit"] = GetNodeValue(drugInfNode, GetMatchingNodeName(drugInfNode, elementMappings, "Unit"));
                                                    row["DrugN"] = GetNodeValue(drugInfNode, GetMatchingNodeName(drugInfNode, elementMappings, "DrugN"));
                                                    row["Revised"] = false;
                                                    dt.Rows.Add(row);

                                                    insertedCount++;
                                                }
                                            }

                                            // 診療情報
                                            XmlNode meTrInfsNode = dateInfNode.SelectSingleNode("MeTrInfs");
                                            if (meTrInfsNode != null)
                                            {
                                                var ptData = new
                                                {
                                                    Id = ptID,
                                                    Idmain = ptIDMain,
                                                    Name = ptName,
                                                    Kana = ptKana,
                                                    Birth = ptBirth,
                                                    Sex = ptSex,
                                                    MeTrDiHCd = meTrDiHCd,
                                                    MeTrDiHNm = meTrDiHNm,
                                                    MeTrMonth = meTrMonth,
                                                    DiDate = diDate
                                                };
                                                sinryoCount += await ProcessSinryoInfoAsync(db, tx, meTrInfsNode, ptData);
                                            }
                                        }
                                    }
                                }
                            }

                            // COPY一括投入（Npgsql 専用／TEXT CSV）
                            var npg = (Npgsql.NpgsqlConnection)db;
                            // tx は BeginTransaction 済み想定（渡す必要はありません）

                            string copySql = @"
                                COPY drug_history (
                                    Source, PtID, PtIDmain, ReceiveDate, PtName, PtKana, Birth, diOrg, MeTrDiHCd,
                                    prlsHCd, MeTrDiHNm, MeTrMonth, prlsHNm, prIsOrg, InOut, DiDate, PrDate,
                                    DrugC, Qua1, UsageN, Times, IngreN, MeTrIdCl, Unit, DrugN, Revised
                                ) FROM STDIN WITH (FORMAT csv, NULL '\N')";
                            // HEADER なし

                            using (var writer = npg.BeginTextImport(copySql))
                            {
                                var sb = new System.Text.StringBuilder(1024);

                                // CSVエスケープ関数（値→CSVセル文字列、NULLは \N）
                                string Csv(object v)
                                {
                                    if (v == null || v == DBNull.Value) return @"\N"; // PGのNULL表現
                                    string s;

                                    // 数値やboolは InvariantCulture で文字列化（CSVなので型は気にしない）
                                    if (v is IFormattable fmt)
                                        s = fmt.ToString(null, System.Globalization.CultureInfo.InvariantCulture);
                                    else
                                        s = v.ToString();

                                    // NULL文字列を明示的に入れたいわけではないので空文字は空のままでOK
                                    // CSVルール：ダブルクォートで囲み、内部の " は "" にエスケープ
                                    bool needQuote = s.IndexOfAny(new[] { '"', ',', '\n', '\r' }) >= 0;
                                    if (needQuote)
                                        return "\"" + s.Replace("\"", "\"\"") + "\"";
                                    return s;
                                }

                                for (int i = 0; i < dt.Rows.Count; i++)
                                {
                                    var r = dt.Rows[i];
                                    sb.Clear();

                                    // 列順は COPY と厳密一致させること
                                    sb.Append(Csv(r["Source"])).Append(',');
                                    sb.Append(Csv(r["PtID"])).Append(',');
                                    sb.Append(Csv(r["PtIDmain"])).Append(',');
                                    sb.Append(Csv(r["ReceiveDate"])).Append(',');
                                    sb.Append(Csv(r["PtName"])).Append(',');
                                    sb.Append(Csv(r["PtKana"])).Append(',');
                                    sb.Append(Csv(r["Birth"])).Append(',');
                                    sb.Append(Csv(r["diOrg"])).Append(',');
                                    sb.Append(Csv(r["MeTrDiHCd"])).Append(',');
                                    sb.Append(Csv(r["prlsHCd"])).Append(',');
                                    sb.Append(Csv(r["MeTrDiHNm"])).Append(',');
                                    sb.Append(Csv(r["MeTrMonth"])).Append(',');
                                    sb.Append(Csv(r["prlsHNm"])).Append(',');
                                    sb.Append(Csv(r["prIsOrg"])).Append(',');
                                    sb.Append(Csv(r["InOut"])).Append(',');
                                    sb.Append(Csv(r["DiDate"])).Append(',');
                                    sb.Append(Csv(r["PrDate"])).Append(',');
                                    sb.Append(Csv(r["DrugC"])).Append(',');
                                    sb.Append(Csv(r["Qua1"])).Append(',');
                                    sb.Append(Csv(r["UsageN"])).Append(',');
                                    sb.Append(Csv(r["Times"])).Append(',');
                                    sb.Append(Csv(r["IngreN"])).Append(',');
                                    sb.Append(Csv(r["MeTrIdCl"])).Append(',');
                                    sb.Append(Csv(r["Unit"])).Append(',');
                                    sb.Append(Csv(r["DrugN"])).Append(',');
                                    sb.Append(Csv(r["Revised"])).Append('\n');

                                    writer.Write(sb.ToString());
                                }
                                // TextImportは Complete 不要（Writeで流し切り）
                            }


                            tx.Commit();
                        }
                        else
                        {
                            // --- OleDb / その他: Prepared + TX + パラメータ差し替え ---
                            using (IDbCommand insertCmd = db.CreateCommand())
                            using (IDbCommand checkCmd = db.CreateCommand())
                            using (IDbCommand updateCmd = db.CreateCommand())
                            {
                                insertCmd.Transaction = tx;
                                checkCmd.Transaction = tx;
                                updateCmd.Transaction = tx;

                                insertCmd.CommandText = insertSql;
                                checkCmd.CommandText = selectSql;
                                updateCmd.CommandText = updateSql;

                                foreach (string rootPath in rootNodes)
                                {
                                    XmlNodeList monthInfList = xmlDoc.SelectNodes(rootPath);
                                    if (monthInfList == null || monthInfList.Count == 0) continue;

                                    for (int iM = 0; iM < monthInfList.Count; iM++)
                                    {
                                        XmlNode monthInfNode = monthInfList[iM];
                                        int Source = (monthInfNode.Name == "MeTrMonthInf") ? 1 :
                                                     (monthInfNode.Name == "CzDiMonthInf") ? 2 :
                                                     (monthInfNode.Name == "ShPrMonthInf") ? 3 : 0;

                                        int diOrg = (int)NzConvert(GetNodeValue(monthInfNode, GetMatchingNodeName(monthInfNode, elementMappings, "Org")), -1);
                                        string meTrDiHCd = GetNodeValue(monthInfNode, GetMatchingNodeName(monthInfNode, elementMappings, "DiHCd"));
                                        string prlsHCd = GetNodeValue(monthInfNode, GetMatchingNodeName(monthInfNode, elementMappings, "HCd"));
                                        string meTrDiHNm = GetNodeValue(monthInfNode, GetMatchingNodeName(monthInfNode, elementMappings, "DiHNm"));
                                        string meTrMonth = GetNodeValue(monthInfNode, GetMatchingNodeName(monthInfNode, elementMappings, "Month"));
                                        string prlsHNm = GetNodeValue(monthInfNode, GetMatchingNodeName(monthInfNode, elementMappings, "HNm"));
                                        int prIsOrg = (int)NzConvert(GetNodeValue(monthInfNode, GetMatchingNodeName(monthInfNode, elementMappings, "IsOrg")), -1);
                                        int inOut = (int)NzConvert(GetNodeValue(monthInfNode, GetMatchingNodeName(monthInfNode, elementMappings, "Cl")), -1);

                                        string MIcode = GetMedicalInstitutionCode(meTrDiHCd, prlsHCd);
                                        if (prIsOrg == -1) prIsOrg = (MIcode == Properties.Settings.Default.MCode) ? 1 : 2;

                                        XmlNodeList dateInfList = monthInfNode.SelectNodes(GetMatchingNodeName(monthInfNode, elementMappings, "DateInf"));
                                        for (int iD = 0; iD < dateInfList.Count; iD++)
                                        {
                                            XmlNode dateInfNode = dateInfList[iD];
                                            string diDate = GetNodeValue(dateInfNode, GetMatchingNodeName(dateInfNode, elementMappings, "DiDate"));
                                            string prDate = GetNodeValue(dateInfNode, GetMatchingNodeName(dateInfNode, elementMappings, "PrDate"));

                                            // 既存チェック
                                            checkCmd.Parameters.Clear();
                                            CommonFunctions.AddDbParameter(checkCmd, "@PtIDmain", ptIDMain);
                                            CommonFunctions.AddDbParameter(checkCmd, "@MIcode", MIcode);
                                            CommonFunctions.AddDbParameter(checkCmd, "@MIcode2", MIcode);
                                            CommonFunctions.AddDbParameter(checkCmd, "@DiDate", diDate);

                                            bool doRead = true;
                                            List<int> idsToRev = new List<int>();

                                            using (DbDataReader rd = await ((DbCommand)checkCmd).ExecuteReaderAsync())
                                            {
                                                while (await rd.ReadAsync())
                                                {
                                                    int rid = rd.GetInt32(0);
                                                    int rsrc = rd.IsDBNull(1) ? 9 : rd.GetInt32(1);
                                                    if (rsrc > Source) idsToRev.Add(rid);
                                                    else doRead = false;
                                                }
                                            }
                                            for (int ii = 0; ii < idsToRev.Count; ii++)
                                            {
                                                updateCmd.Parameters.Clear();
                                                CommonFunctions.AddDbParameter(updateCmd, "@ID", idsToRev[ii]);
                                                await ((DbCommand)updateCmd).ExecuteNonQueryAsync();
                                            }

                                            if (doRead)
                                            {
                                                XmlNodeList drugInfList = dateInfNode.SelectNodes(GetMatchingNodeName(dateInfNode, elementMappings, "DrugInf"));
                                                for (int iR = 0; iR < drugInfList.Count; iR++)
                                                {
                                                    XmlNode drugInfNode = drugInfList[iR];

                                                    string usage = GetNodeValue(drugInfNode, GetMatchingNodeName(drugInfNode, elementMappings, "UsageN"));
                                                    string spinst = GetNodeValue(drugInfNode, GetMatchingNodeName(drugInfNode, elementMappings, "SpInst"));
                                                    if (!string.IsNullOrEmpty(spinst)) usage = string.IsNullOrEmpty(usage) ? spinst : usage + "/" + spinst;

                                                    insertCmd.Parameters.Clear();
                                                    CommonFunctions.AddDbParameter(insertCmd, "@Source", Source);
                                                    CommonFunctions.AddDbParameter(insertCmd, "@PtID", ptID);
                                                    CommonFunctions.AddDbParameter(insertCmd, "@PtIDmain", ptIDMain);
                                                    CommonFunctions.AddDbParameter(insertCmd, "@ReceiveDate", receiveDate);
                                                    CommonFunctions.AddDbParameter(insertCmd, "@PtName", ptName);
                                                    CommonFunctions.AddDbParameter(insertCmd, "@PtKana", ptKana);
                                                    CommonFunctions.AddDbParameter(insertCmd, "@Birth", ptBirth);
                                                    CommonFunctions.AddDbParameter(insertCmd, "@diOrg", diOrg);
                                                    CommonFunctions.AddDbParameter(insertCmd, "@MeTrDiHCd", meTrDiHCd);
                                                    CommonFunctions.AddDbParameter(insertCmd, "@prlsHCd", prlsHCd);
                                                    CommonFunctions.AddDbParameter(insertCmd, "@MeTrDiHNm", meTrDiHNm);
                                                    CommonFunctions.AddDbParameter(insertCmd, "@MeTrMonth", meTrMonth);
                                                    CommonFunctions.AddDbParameter(insertCmd, "@prlsHNm", prlsHNm);
                                                    CommonFunctions.AddDbParameter(insertCmd, "@prIsOrg", prIsOrg);
                                                    CommonFunctions.AddDbParameter(insertCmd, "@InOut", inOut);
                                                    CommonFunctions.AddDbParameter(insertCmd, "@DiDate", diDate);
                                                    CommonFunctions.AddDbParameter(insertCmd, "@PrDate", prDate);
                                                    CommonFunctions.AddDbParameter(insertCmd, "@DrugC", GetNodeValue(drugInfNode, GetMatchingNodeName(drugInfNode, elementMappings, "DrugC")));
                                                    CommonFunctions.AddDbParameter(insertCmd, "@Qua1", NzConvert(GetNodeValue(drugInfNode, GetMatchingNodeName(drugInfNode, elementMappings, "Qua1"))));
                                                    CommonFunctions.AddDbParameter(insertCmd, "@UsageN", usage);
                                                    CommonFunctions.AddDbParameter(insertCmd, "@Times", (int)NzConvert(GetNodeValue(drugInfNode, GetMatchingNodeName(drugInfNode, elementMappings, "Times"))));
                                                    CommonFunctions.AddDbParameter(insertCmd, "@IngreN", GetNodeValue(drugInfNode, GetMatchingNodeName(drugInfNode, elementMappings, "IngreN")));
                                                    CommonFunctions.AddDbParameter(insertCmd, "@MeTrIdCl", (int)NzConvert(GetNodeValue(drugInfNode, GetMatchingNodeName(drugInfNode, elementMappings, "UsageCl"))));
                                                    CommonFunctions.AddDbParameter(insertCmd, "@Unit", GetNodeValue(drugInfNode, GetMatchingNodeName(drugInfNode, elementMappings, "Unit")));
                                                    CommonFunctions.AddDbParameter(insertCmd, "@DrugN", GetNodeValue(drugInfNode, GetMatchingNodeName(drugInfNode, elementMappings, "DrugN")));
                                                    CommonFunctions.AddDbParameter(insertCmd, "@Revised", false);

                                                    await ((DbCommand)insertCmd).ExecuteNonQueryAsync();
                                                    insertedCount++;
                                                }
                                            }

                                            // 診療情報
                                            XmlNode meTrInfsNode = dateInfNode.SelectSingleNode("MeTrInfs");
                                            if (meTrInfsNode != null)
                                            {
                                                var ptData = new
                                                {
                                                    Id = ptID,
                                                    Idmain = ptIDMain,
                                                    Name = ptName,
                                                    Kana = ptKana,
                                                    Birth = ptBirth,
                                                    Sex = ptSex,
                                                    MeTrDiHCd = meTrDiHCd,
                                                    MeTrDiHNm = meTrDiHNm,
                                                    MeTrMonth = meTrMonth,
                                                    DiDate = diDate
                                                };
                                                sinryoCount += await ProcessSinryoInfoAsync(db, tx, meTrInfsNode, ptData);
                                            }
                                        }
                                    }
                                }
                                tx.Commit();
                            }
                        }


                        // Revised処理
                        revisedCount = await SetRevisedBySourceAsync(db, tx, ptIDMain);
                    }


                    if (insertedCount > 0) ShowNotification(ptIDMain.ToString(), ptName + "さんの薬歴" + insertedCount + "件取得");
                    return "成功：xml薬歴から" + insertedCount + "件,診療情報" + sinryoCount + "件,重複Revised"+ revisedCount +"件のレコードを読み込みました";
                }
            }
            catch (Exception ex)
            {
                return "エラー：" + ex.Message;
            }
        }

        // 既存の補助：GetNodeValue / GetMatchingNodeName / NzConvert / GetMedicalInstitutionCode / ProcessSinryoInfoAsync などはそのまま利用


        private async Task<int> ProcessSinryoInfoAsync(IDbConnection conn, IDbTransaction tx, XmlNode meTrInfsNode, dynamic ptData)
        {
            int count = 0;

            // 1回だけ変換
            string insertSql = CommonFunctions.ConvertSqlForOleDb(@"
                                INSERT INTO sinryo_history (
                                    PtID, PtIDmain, PtName, PtKana, Birth, Sex, MeTrDiHCd,
                                    MeTrDiHNm, MeTrMonth, DiDate, SinInfN, SinInfCd, MeTrIdCl, Qua1, Times, Unit, ReceiveDate
                                ) VALUES (
                                    @PtID, @PtIDmain, @PtName, @PtKana, @Birth, @Sex, @MeTrDiHCd,
                                    @MeTrDiHNm, @MeTrMonth, @DiDate, @SinInfN, @SinInfCd, @MeTrIdCl, @Qua1, @Times, @Unit, @ReceiveDate
                                )");

            string checkSql = CommonFunctions.ConvertSqlForOleDb(@"
                                SELECT COUNT(*) FROM sinryo_history WHERE PtIDmain = @PtIDmain AND DiDate = @DiDate");

            try
            {
                // null ガード（念のため）
                var meTrList = meTrInfsNode?.SelectNodes("MeTrInf");
                if (meTrList == null || meTrList.Count == 0) return 0;

                // 同日既存チェック（TX内なので Transaction を必ず付与）
                using (IDbCommand checkCommand = conn.CreateCommand())
                {
                    checkCommand.Transaction = tx;
                    checkCommand.CommandText = checkSql;
                    CommonFunctions.AddDbParameter(checkCommand, "@PtIDmain", ptData.Idmain);
                    CommonFunctions.AddDbParameter(checkCommand, "@DiDate", ptData.DiDate);

                    int existed = Convert.ToInt32(await ((DbCommand)checkCommand).ExecuteScalarAsync());
                    if (existed > 0) return 0;
                }

                var receiveDate = DateTime.Now.ToString("yyyyMMdd");

                using (IDbCommand insertCommand = conn.CreateCommand())
                {
                    insertCommand.Transaction = tx;
                    insertCommand.CommandText = insertSql; // ← 二重Convertやめる

                    foreach (XmlNode meTrInf in meTrList)
                    {
                        insertCommand.Parameters.Clear();

                        CommonFunctions.AddDbParameter(insertCommand, "@PtID", ptData.Id);
                        CommonFunctions.AddDbParameter(insertCommand, "@PtIDmain", ptData.Idmain);
                        CommonFunctions.AddDbParameter(insertCommand, "@PtName", ptData.Name);
                        CommonFunctions.AddDbParameter(insertCommand, "@PtKana", ptData.Kana);
                        CommonFunctions.AddDbParameter(insertCommand, "@Birth", ptData.Birth);
                        CommonFunctions.AddDbParameter(insertCommand, "@Sex", ptData.Sex);
                        CommonFunctions.AddDbParameter(insertCommand, "@MeTrDiHCd", ptData.MeTrDiHCd);
                        CommonFunctions.AddDbParameter(insertCommand, "@MeTrDiHNm", ptData.MeTrDiHNm);
                        CommonFunctions.AddDbParameter(insertCommand, "@MeTrMonth", ptData.MeTrMonth);
                        CommonFunctions.AddDbParameter(insertCommand, "@DiDate", ptData.DiDate);

                        CommonFunctions.AddDbParameter(insertCommand, "@SinInfN", GetNodeValue(meTrInf, "SinInfN"));
                        CommonFunctions.AddDbParameter(insertCommand, "@SinInfCd", GetNodeValue(meTrInf, "SinInfCd"));
                        CommonFunctions.AddDbParameter(insertCommand, "@MeTrIdCl", (int)NzConvert(GetNodeValue(meTrInf, "MeTrIdCl")));
                        CommonFunctions.AddDbParameter(insertCommand, "@Qua1", NzConvert(GetNodeValue(meTrInf, "Qua1")));
                        CommonFunctions.AddDbParameter(insertCommand, "@Times", (int)NzConvert(GetNodeValue(meTrInf, "Times")));
                        CommonFunctions.AddDbParameter(insertCommand, "@Unit", GetNodeValue(meTrInf, "Unit"));
                        CommonFunctions.AddDbParameter(insertCommand, "@ReceiveDate", receiveDate);

                        await ((DbCommand)insertCommand).ExecuteNonQueryAsync();
                        count++;
                    }
                }

                return count;
            }
            catch (Exception ex)
            {
                await AddLogAsync($"ProcessSinryoInfoAsync エラー: {ex.Message}");
                return count;
            }
        }

        /// <summary>
        /// (PtIDmain, MIcode(prlsHCd優先→meTrDiHCd), DiDate) 単位で、
        /// Source が異なるものが共存するグループに限り、
        /// 最小Source以外を Revised=TRUE にする。
        /// PG/Access 両対応。一括更新で 1レコードずつは評価しません。
        /// </summary>
        /// <param name="conn">Open済み接続</param>
        /// <param name="tx">任意: 同一トランザクション内で実行したい場合に指定</param>
        /// <param name="ptIDMain">絞り込み（null なら全体）</param>
        /// <returns>Revised=TRUE に更新された件数</returns>
        private async Task<int> SetRevisedBySourceAsync(
            IDbConnection conn,
            IDbTransaction tx = null,
            long? ptIDMain = null)
        {
            bool isPg = conn.GetType().FullName != null &&
                        conn.GetType().FullName.IndexOf("Npgsql", StringComparison.OrdinalIgnoreCase) >= 0;

            if (isPg)
            {
                // PostgreSQL: CTEでベース集合を作り、最小Source以外を更新
                string sql = @"
                    WITH base AS (
                      SELECT
                          id,
                          ptidmain,
                          didate,
                          CASE
                            WHEN prlshcd  IS NOT NULL AND length(prlshcd)=10  AND substring(prlshcd ,3,1) IN ('1','3') THEN prlshcd
                            WHEN metrdihcd IS NOT NULL AND length(metrdihcd)=10 AND substring(metrdihcd,3,1) IN ('1','3') THEN metrdihcd
                            ELSE ''
                          END AS micode,
                          COALESCE(source, 9) AS src
                      FROM drug_history
                      WHERE (@Pt IS NULL OR ptidmain = CAST(@Pt AS bigint))
                    ),
                    mins AS (
                      SELECT ptidmain, didate, micode, MIN(src) AS min_src
                      FROM base
                      GROUP BY ptidmain, didate, micode
                    )
                    UPDATE drug_history d
                    SET revised = TRUE
                    FROM base b
                    JOIN mins m
                      ON m.ptidmain = b.ptidmain
                     AND m.didate   = b.didate
                     AND m.micode   = b.micode
                    WHERE d.id = b.id
                      AND b.src > m.min_src;";

                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = sql;
                    CommonFunctions.AddDbParameter(cmd, "@Pt", (object)ptIDMain ?? DBNull.Value);
                    return await ((DbCommand)cmd).ExecuteNonQueryAsync();
                }
            }
            else
            {
                // Access(OleDb): 相関副問合せで “最小Source以外” を更新
                string miExprT = @"
                    IIf(NOT IsNull(t.prlsHCd) AND Len(t.prlsHCd)=10 AND (Mid(t.prlsHCd,3,1)='1' OR Mid(t.prlsHCd,3,1)='3'),
                        t.prlsHCd,
                        IIf(NOT IsNull(t.meTrDiHCd) AND Len(t.meTrDiHCd)=10 AND (Mid(t.meTrDiHCd,3,1)='1' OR Mid(t.meTrDiHCd,3,1)='3'),
                            t.meTrDiHCd,
                            ''
                        )
                    )";
                string miExprU = @"
                    IIf(NOT IsNull(u.prlsHCd) AND Len(u.prlsHCd)=10 AND (Mid(u.prlsHCd,3,1)='1' OR Mid(u.prlsHCd,3,1)='3'),
                        u.prlsHCd,
                        IIf(NOT IsNull(u.meTrDiHCd) AND Len(u.meTrDiHCd)=10 AND (Mid(u.meTrDiHCd,3,1)='1' OR Mid(u.meTrDiHCd,3,1)='3'),
                            u.meTrDiHCd,
                            ''
                        )
                    )";

                var where = new System.Text.StringBuilder(" WHERE 1=1 ");
                if (ptIDMain.HasValue) where.Append(" AND t.PtIDmain = @Pt ");

                string sqlTemplate = $@"
                    UPDATE drug_history AS t
                       SET Revised = TRUE
                    {where}
                      AND EXISTS (
                            SELECT 1
                              FROM drug_history AS u
                             WHERE u.PtIDmain = t.PtIDmain
                               AND u.DiDate   = t.DiDate
                               AND {miExprU}  = {miExprT}
                               AND IIf(IsNull(u.Source),9,u.Source) < IIf(IsNull(t.Source),9,t.Source)
                        )";

                string sql = CommonFunctions.ConvertSqlForOleDb(sqlTemplate);

                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = sql;
                    if (ptIDMain.HasValue)
                        CommonFunctions.AddDbParameter(cmd, "@Pt", ptIDMain.Value);

                    return await ((DbCommand)cmd).ExecuteNonQueryAsync();
                }
            }
        }



        private string GetMedicalInstitutionCode(string meTrDiHCd, string prlsHCd)
        {
            // コードが10桁かつ左から3番目が1または3の場合に有効とする 医科歯科
            bool IsValidCode(string code) =>
                !string.IsNullOrEmpty(code) &&
                code.Length == 10 &&
                (code[2] == '1' || code[2] == '3');

            // prlsHCdが条件に該当する場合は優先的に返す
            if (IsValidCode(prlsHCd))
            {
                return prlsHCd;
            }

            // prlsHCdが該当しない場合、meTrDiHCdを確認
            if (IsValidCode(meTrDiHCd))
            {
                return meTrDiHCd;
            }

            // どちらも該当しない場合は空文字を返す
            return string.Empty;
        }

        private string GetMatchingNodeName(XmlNode node, Dictionary<string, List<string>> elementMappings, string key)
        {
            if (!elementMappings.ContainsKey(key))
            {
                return key; // マッピングがなければキーをそのまま返す
            }

            foreach (var possibleName in elementMappings[key])
            {
                if (node.SelectSingleNode(possibleName) != null || node.SelectNodes(possibleName).Count > 0)
                {
                    return possibleName; // ノードが存在する最初のマッピング名を返す
                }
            }

            return key; // マッチしなかった場合、デフォルトのキーを返す
        }


        private string GetNodeValue(XmlNode node, string xpath)
        {
            XmlNode selectedNode = node.SelectSingleNode(xpath);
            return selectedNode?.InnerText ?? string.Empty;
        }

        private float NzConvert(string value, float defaultValue = 0f)
        {
            return float.TryParse(value, out float result) ? result : defaultValue;
        }

        public async Task<bool> AddFieldIfNotExists(string databasePath, string tableName, string fieldName, string fieldFormat)
        {
            // 接続文字列の設定
            string connectionString = $"Provider={CommonFunctions.DBProvider};Data Source={databasePath};";

            try
            {
                using (var connection = new OleDbConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // テーブルのスキーマ情報を取得
                    DataTable schemaTable = connection.GetSchema("Columns", new string[] { null, null, tableName, null });

                    // フィールドの存在チェック
                    foreach (DataRow row in schemaTable.Rows)
                    {
                        if (row["COLUMN_NAME"].ToString().Equals(fieldName, StringComparison.OrdinalIgnoreCase))
                        {
                            AddLogAsync($"Field '{fieldName}' already exists in table '{tableName}'.");
                            return true;
                        }
                    }

                    // フィールドが存在しない場合は追加
                    string alterTableQuery = $"ALTER TABLE [{tableName}] ADD COLUMN [{fieldName}] {fieldFormat};";

                    using (var command = new OleDbCommand(alterTableQuery, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                        AddLogAsync($"Field '{fieldName}' has been added to table '{tableName}'.");

                        if (fieldName == "Source")
                        {
                            // SQLクエリ：SourceがNULLの場合に一括で1に設定
                            string updateSql = $"UPDATE {tableName} SET Source = 1 WHERE Source IS NULL";

                            using (OleDbCommand updateCommand = new OleDbCommand(updateSql, connection))
                            {
                                // クエリを実行して、更新を反映
                                int rowsAffected = await updateCommand.ExecuteNonQueryAsync();
                                AddLogAsync("Sourceフィールドの初期値を設定しました");
                            }
                        }
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                AddLogAsync($"An error occurred: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> RemainResTask()
        {
            try
            {
                using (IDbConnection connection = CommonFunctions.GetDbConnection())
                {
                    await ((DbConnection)connection).OpenAsync();

                    // 1. reqDate が1日以上前で resFile と result が NULL のレコードを削除
                    string deleteSql = @"
                        DELETE FROM reqResults
                        WHERE reqDate < @cutoffDate AND resFile IS NULL AND result IS NULL";
                    deleteSql = CommonFunctions.ConvertSqlForOleDb(deleteSql);

                    using (IDbCommand deleteCommand = connection.CreateCommand())
                    {
                        deleteCommand.CommandText = deleteSql;
                        CommonFunctions.AddDbParameter(deleteCommand, "@cutoffDate", DateTime.Now.AddDays(-1));

                        int updatedRows = await ((DbCommand)deleteCommand).ExecuteNonQueryAsync();
                        if (updatedRows > 0)
                        {
                            AddLogAsync($"タイムアウトデータ {updatedRows} 件削除しました");
                        }
                    }

                    // 2. resFile と result が NULL のレコードを検索
                    string checkSql = @"
                        SELECT COUNT(*) FROM reqResults
                        WHERE resFile IS NULL AND result IS NULL";
                    checkSql = CommonFunctions.ConvertSqlForOleDb(checkSql);

                    using (IDbCommand checkCommand = connection.CreateCommand())
                    {
                        checkCommand.CommandText = checkSql;
                        object result = await ((DbCommand)checkCommand).ExecuteScalarAsync();
                        return Convert.ToInt32(result) > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                AddLogAsync($"RemainResTask でエラー: {ex.Message}");
                return false;
            }
        }


        private long Name2ID(string ptName, string strBirth, DataTable dataTable)
        {
            long maxPtID = 0;

            var rows = dataTable.Select($"氏名 = '{ptName}' AND 生年月日西暦 = '{strBirth}' AND カルテ番号 IS NOT NULL");
            if (rows.Length == 0)
            {
                return 0;
            }

            // 条件に合う行の中で最大のカルテ番号を取得
            maxPtID = rows
                .Where(row => row["カルテ番号"] != DBNull.Value)  // DBNull.Value を除外
                .Max(row => Convert.ToInt64(row["カルテ番号"]));

            return maxPtID;
        }

        private async void checkBoxAutoview_CheckedChanged(object sender, EventArgs e) //RSB連動遷移
        {
            autoRSB = checkBoxAutoview.Checked;
            autoTKK = checkBoxAutoTKK.Checked;
            autoSR = checkBoxAutoSR.Checked;

            Properties.Settings.Default.autoRSB = autoRSB;
            Properties.Settings.Default.autoTKK = autoTKK;
            Properties.Settings.Default.autoSR = autoSR;

            Properties.Settings.Default.Save();

            if (autoRSB || autoTKK || autoSR)
            {
                InitializeFileWatcher();

                //初回読み込み
                if (idStyle < 3 && File.Exists(idFile))
                {
                    await ReadIdAsync(idFile,idStyle);
                } 
                else if(idStyle == 3)
                {
                    string latestIdFile = KeepLatestFile(idFile);
                    if (latestIdFile.Length > 0)
                    {
                        await ReadIdAsync(latestIdFile, idStyle);
                    }
                }
                
            } else
            {
                stopFileWatcher();
            }
        }

        private void stopFileWatcher()
        {
            if (fileWatcher != null)
            {
                fileWatcher.EnableRaisingEvents = false;
                fileWatcher.Dispose();
            }
            AddLogAsync("RSB連携を終了しました");
        }

        private void InitializeFileWatcher()
        {
            switch (Properties.Settings.Default.RSBID)
            {
                case 0:
                    idFile = @"C:\RSB_TEMP\ID.dat";
                    idStyle = 1;
                    break;
                case 1:
                    idFile = @"C:\RSB_TEMP\temp_rs.txt";
                    idStyle = 1;
                    break;
                case 2:
                    idFile = @"C:\common\thept.txt";
                    idStyle = 2;
                    break;
                case 3:
                    idFile = @"C:\DynaID";
                    idStyle = 3;
                    break;
                case 4:
                    idFile = @"D:\DynaID";
                    idStyle = 3;
                    break;
            }

            // FileSystemWatcherを作成し、監視対象のディレクトリとファイルを指定
            //fileWatcher = new FileSystemWatcher();
            //fileWatcher.Path = Path.GetDirectoryName(idFile);  // ファイルがあるディレクトリ
            //fileWatcher.Filter = Path.GetFileName(idFile);     // ファイル名でフィルタリング
            //fileWatcher.NotifyFilter = NotifyFilters.LastWrite; // 最終書き込み変更を監視

            string idPath = (idStyle < 3) ? Path.GetDirectoryName(idFile) : idFile;
            if (!Directory.Exists(idPath))
            {
                try
                {
                    Directory.CreateDirectory(idPath);
                    AddLogAsync($"{idPath}が見つからなかったので作成しました");
                }
                catch(Exception ex)
                {
                    MessageBox.Show($"RSBase ID連携フォルダ{idPath}が存在しません。ID連携の設定を確認してください。{ex.Message}", "ID連携エラー");
                    return;
                }
            }

            try
            {
                if (idStyle == 3) //ダイナ他社連携
                {
                    fileWatcher = new FileSystemWatcher
                    {
                        Path = idFile,
                        Filter = "dyna*.txt", // 拡張子がTXTのすべてのファイル
                        NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite, // ファイル作成・更新を検知
                        EnableRaisingEvents = true             // 監視を有効化
                    };
                }
                else
                {
                    fileWatcher = new FileSystemWatcher
                    {
                        Path = idPath,   // ディレクトリを監視
                        Filter = Path.GetFileName(idFile),     // ファイル名でフィルタリング
                        NotifyFilter = NotifyFilters.LastWrite, // | NotifyFilters.CreationTime, // 必要な通知フィルタを設定
                        EnableRaisingEvents = true             // 監視を有効化
                    };
                }

                // 監視イベントハンドラを設定
                fileWatcher.Changed += FileWatcher_Changed;
                //fileWatcher.Created += FileWatcher_Changed;

                AddLogAsync("RSB連携を開始しました");
            }
            catch (Exception ex)
            {
                AddLogAsync($"FileWatcherの初期化に失敗しました{ex.ToString()}");
            }
        }

        // ファイルが変更されたときに呼ばれるイベントハンドラ
        private async void FileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (!idChageCalled)
            {
                idChageCalled = true; //二重起動を避ける

                await Task.Delay(fileReadDelayms); // 読み込み遅延

                if ((idStyle < 3 && e.FullPath == idFile) || (idStyle == 3 && e.FullPath.StartsWith(idFile, StringComparison.OrdinalIgnoreCase)))
                {
                    // ファイル内容の読み取り
                    await ReadIdAsync(e.FullPath, idStyle);

                    await reloadDataAsync();
                }
                idChageCalled = false;
            }
        }

        private async Task ReadIdAsync(string filePath, int style)
        {
            try
            {
                // ファイルの内容を非同期で読み取る
                string fileContent = await Task.Run(() =>
                {
                    // FileStreamを使用して共有アクセスを許可
                    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(stream))
                    {
                        return reader.ReadLine();
                    }
                });

                if (style == 3)
                {
                    //ダイナ
                    fileContent = fileContent.Split(',')[0];
                }
                else
                {
                    //thept.txtは内容が違う
                    if (style == 2)
                    {
                        fileContent = fileContent.Split(',')[1];
                    }
                }
                AddLogAsync($"ダイナ/RSB連携ファイルの変更を検知。内容：{fileContent}");

                // 数値に変換を試みる
                if (long.TryParse(fileContent, out long idValue))
                {
                    // 数値に変換できた場合
                    tempId = idValue;

                    if(autoRSB)  await OpenDrugHistory(tempId,false);

                    if(autoTKK)  await OpenTKKHistory(tempId,false);

                    if(autoSR)   await OpenSinryoHistory(tempId,false);
                   
                }
                else
                {
                    AddLogAsync("RSB連携ファイルの内容が数値に変換できませんでした。");
                }

                //ダイナの場合は削除する
                if (style == 3)
                {
                    File.Delete(filePath);
                    AddLogAsync($"{filePath}を削除しました");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"FileWatcherエラー: {ex.Message}");
            }
        }

        private string KeepLatestFile(string folderName) //最新のファイルのみ残してあとはすべて削除する
        {
            try
            {
                // 指定フォルダの全ファイルを取得
                var files = new DirectoryInfo(folderName).GetFiles();

                if (files.Length == 0)
                {
                    Console.WriteLine("フォルダ内にファイルがありません。");
                    return string.Empty;
                }

                // 更新日時が最新のファイルを取得
                var newestFile = files.OrderByDescending(f => f.LastWriteTime).First();

                Console.WriteLine($"残すファイル: {newestFile.FullName}");

                // 最新のファイル以外を削除
                foreach (var file in files)
                {
                    if (file.FullName != newestFile.FullName)
                    {
                        try
                        {
                            file.Delete();
                            Console.WriteLine($"削除: {file.Name}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"削除失敗: {file.Name}, エラー: {ex.Message}");
                        }
                    }
                }

                // 最新のファイルのフルパスを返す
                return newestFile.FullName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"エラー: {ex.Message}");
                return string.Empty;
            }
        }

        public async Task OpenDrugHistory(long ptId, bool messagePopup = false, bool alwaysShow = false)
        {
            if (alwaysShow || await existHistory(ptId, "drug_history"))
            {
                tempId = ptId;
                // UIスレッドで操作
                Invoke((Action)(() =>
                {
                    if (formDIInstance == null || formDIInstance.IsDisposed)
                    {
                        formDIInstance = new FormDI(this);

                        // 前回の位置とサイズを復元
                        if (Properties.Settings.Default.ViewerBounds != Rectangle.Empty)
                        {
                            formDIInstance.StartPosition = FormStartPosition.Manual;
                            formDIInstance.Bounds = Properties.Settings.Default.ViewerBounds;

                            // マージンと境界線を設定
                            formDIInstance.Padding = new Padding(0);
                            formDIInstance.Margin = new Padding(0);
                            //form3Instance.FormBorderStyle = FormBorderStyle.None;
                        }

                        // TopMost状態を設定
                        formDIInstance.TopMost = Properties.Settings.Default.ViewerTopmost;

                        // Form3が閉じるときに位置、サイズ、TopMost状態を保存
                        formDIInstance.FormClosing += (s, args) =>
                        {
                            SaveViewerSettings(formDIInstance, "ViewerBounds");
                        };

                        formDIInstance.Show(this);
                    }
                    else
                    {
                        // Form3が開いている場合、LoadDataIntoComboBoxes()を実行
                        Task.Run(async () =>
                            await formDIInstance.LoadDataIntoComboBoxes()
                        );
                        // すでに開いている場合はアクティブにする
                        formDIInstance.Activate();

                    }
                }));
                AddLogAsync($"{ptId}の薬歴を開きます");
            }
            else
            {
                //薬歴なしの場合はViewerを閉じる
                if (formDIInstance != null && !formDIInstance.IsDisposed)
                {
                    // UI スレッドで操作する必要があるため Invoke を使用
                    formDIInstance.Invoke((Action)(() =>
                    {
                        formDIInstance.Close(); // Form3 を閉じる
                        formDIInstance = null;
                    }));
                    AddLogAsync($"{ptId}は薬歴がないので薬歴ビュワーを閉じます");
                }
                if (messagePopup)
                {
                    MessageBox.Show($"{ptId}の薬歴はありません");
                }
            }
        }

        public async Task OpenTKKHistory(long ptId, bool messagePopup = false, bool alwaysShow = false)
        {
            if (alwaysShow || await existHistory(ptId, "TKK_history"))
            {
                tempId = ptId;
                // UIスレッドで操作
                Invoke((Action)(() =>
                {
                    // FormTKKがすでに開いているか確認
                    if (formTKKInstance == null || formTKKInstance.IsDisposed)
                    {
                        formTKKInstance = new FormTKK(this);

                        // 前回の位置とサイズを復元
                        if (Properties.Settings.Default.TKKBounds != Rectangle.Empty)
                        {
                            formTKKInstance.StartPosition = FormStartPosition.Manual;
                            formTKKInstance.Bounds = Properties.Settings.Default.TKKBounds;

                            // マージンと境界線を設定
                            formTKKInstance.Padding = new Padding(0);
                            formTKKInstance.Margin = new Padding(0);
                            //form3Instance.FormBorderStyle = FormBorderStyle.None;
                        }

                        // TopMost状態を設定
                        formTKKInstance.TopMost = Properties.Settings.Default.ViewerTopmost;

                        // Form3が閉じるときに位置、サイズ、TopMost状態を保存
                        formTKKInstance.FormClosing += (s, args) =>
                        {
                            SaveViewerSettings(formTKKInstance, "TKKBounds");
                        };

                        formTKKInstance.Show(this);
                    }
                    else
                    {
                        // FormTKKが開いている場合、LoadDataIntoComboBoxes()を実行
                        Task.Run(async () =>
                            await formTKKInstance.LoadToolStripComboBox()
                        );
                        // すでに開いている場合はアクティブにする
                        formTKKInstance.Activate();

                    }
                }));
                AddLogAsync($"{ptId}の健診結果を開きます");
            }
            else
            {
                //健診歴なしの場合はViewerを閉じる
                if (formTKKInstance != null && !formTKKInstance.IsDisposed)
                {
                    // UI スレッドで操作する必要があるため Invoke を使用
                    formTKKInstance.Invoke((Action)(() =>
                    {
                        formTKKInstance.Close(); // Form3 を閉じる
                        formTKKInstance = null;
                    }));
                    AddLogAsync($"{ptId}は健診歴がないので健診ビュワーを閉じます");
                }
                if (messagePopup)
                {
                    MessageBox.Show($"{ptId}の健診歴はありません");
                }
            }
        }

        public async Task OpenSinryoHistory(long ptId, bool messagePopup = false, bool alwaysShow = false)
        {
            if (alwaysShow || await existHistory(ptId, "sinryo_history"))
            {
                tempId = ptId;
                // UIスレッドで操作
                Invoke((Action)(() =>
                {
                    // FormSRがすでに開いているか確認
                    if (formSRInstance == null || formSRInstance.IsDisposed)
                    {
                        formSRInstance = new FormSR(this);

                        // 前回の位置とサイズを復元
                        if (Properties.Settings.Default.SRBounds != Rectangle.Empty)
                        {
                            formSRInstance.StartPosition = FormStartPosition.Manual;
                            formSRInstance.Bounds = Properties.Settings.Default.SRBounds;

                            // マージンと境界線を設定
                            formSRInstance.Padding = new Padding(0);
                            formSRInstance.Margin = new Padding(0);
                            //form3Instance.FormBorderStyle = FormBorderStyle.None;
                        }

                        // TopMost状態を設定
                        formSRInstance.TopMost = Properties.Settings.Default.ViewerTopmost;

                        // Form3が閉じるときに位置、サイズ、TopMost状態を保存
                        formSRInstance.FormClosing += (s, args) =>
                        {
                            SaveViewerSettings(formSRInstance, "SRBounds");
                        };

                        formSRInstance.Show(this);
                    }
                    else
                    {
                        // FormTKKが開いている場合、LoadDataIntoComboBoxes()を実行
                        Task.Run(async () =>
                            await formSRInstance.LoadDataIntoComboBoxes()
                        );
                        // すでに開いている場合はアクティブにする
                        formSRInstance.Activate();

                    }
                }));
                AddLogAsync($"{ptId}の診療情報を開きます");
            }
            else
            {
                //なしの場合はViewerを閉じる
                if (formSRInstance != null && !formSRInstance.IsDisposed)
                {
                    // UI スレッドで操作する必要があるため Invoke を使用
                    formSRInstance.Invoke((Action)(() =>
                    {
                        formSRInstance.Close(); // 閉じる
                        formSRInstance = null;
                    }));
                    AddLogAsync($"{ptId}は診療情報履歴がないのでビュワーを閉じます");
                }
                if (messagePopup)
                {
                    MessageBox.Show($"{ptId}の診療情報履歴はありません");
                }
            }
        }
                
        private async Task<bool> existHistory(long PtIDmain, string tableName)
        {
            try
            {
                if (await CommonFunctions.WaitForDbUnlock(2000))
                {
                    using (IDbConnection connection = CommonFunctions.GetDbConnection(true))  // ReadOnly = true
                    {
                        await ((DbConnection)connection).OpenAsync();

                        string sql = $"SELECT COUNT(*) FROM {tableName} WHERE PtIDmain = @PtIDmain;";
                        sql = CommonFunctions.ConvertSqlForOleDb(sql);

                        using (IDbCommand command = connection.CreateCommand())
                        {
                            command.CommandText = sql;
                            CommonFunctions.AddDbParameter(command, "@PtIDmain", PtIDmain);

                            CommonFunctions.DataDbLock = true;

                            object result = await ((DbCommand)command).ExecuteScalarAsync();
                            int count = Convert.ToInt32(result);

                            return count > 0;
                        }
                    }
                }
                else
                {
                    AddLogAsync("existHistoryでデータベースロックタイムアウト");
                    return false;
                }
            }
            catch (Exception ex)
            {
                AddLogAsync($"existHistoryでエラー: {ex.Message}");
                return false;
            }
            finally
            {
                CommonFunctions.DataDbLock = false;
            }
        }


        private void toolStripButtonVersion_Click(object sender, EventArgs e)
        {
            FormVersion formVersion = new FormVersion();
            formVersion.ShowDialog(this);
        }

        private void LoadViewerSettings()
        {
            // デフォルトの ViewerBounds を Form1 の現在位置 + オフセットで設定
            if (Properties.Settings.Default.ViewerBounds == Rectangle.Empty)
            {
                int offsetX = 100; // X方向のオフセット
                int offsetY = 100; // Y方向のオフセット

                // Form1 の現在位置を基準に初期位置を設定
                Properties.Settings.Default.ViewerBounds = new Rectangle(
                    this.Location.X + offsetX,
                    this.Location.Y + offsetY,
                    1280,  // デフォルトの幅
                    680   // デフォルトの高さ
                );
            }

          
            // 設定を保存
            Properties.Settings.Default.Save();
        }
                
        private void ConfigureDataGridView(DataGridView dataGridView)
        {
            if (dataGridView.InvokeRequired)
            {
                dataGridView.Invoke((MethodInvoker)(() => ConfigureDataGridView(dataGridView)));
                return;
            }

            // レコードセレクタを非表示にする
            dataGridView.RowHeadersVisible = false;

            // カラム幅を自動調整する
            dataGridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);

            // 行の高さを変更できないようにする
            dataGridView.AllowUserToResizeRows = false;

            // 特定のカラムの幅を固定にする
            if (dataGridView.Columns.Count > 0)
            {
                dataGridView.Columns[0].Width = 80;
                dataGridView.Columns[1].Width = 50;
                dataGridView.Columns[2].Width = 100;
                dataGridView.Columns[3].Width = 250;
                dataGridView.Columns[4].Width = 100;
                dataGridView.Columns[5].Width = 200;
                dataGridView.Columns[6].Width = 100;
                dataGridView.Columns[7].Width = 200;
            }

            // ソート機能を無効にする
            //dataGridView.AllowUserToOrderColumns = false;
            //// 各列のソートモードを無効にする
            //foreach (DataGridViewColumn column in dataGridView.Columns)
            //{
            //    column.SortMode = DataGridViewColumnSortMode.NotSortable;
            //}

            dataGridView.CellBorderStyle = DataGridViewCellBorderStyle.Raised;

            dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect; //行全体選択
            dataGridView.MultiSelect = false; // 複数行選択を無効にする

        }


        private void dataGridView1_CellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {
            // 右クリックかどうかを確認
            if (e.Button == MouseButtons.Right)
            {
                // クリックされた行を選択状態にする
                dataGridView1.Rows[e.RowIndex].Selected = true;

                // コンテキストメニューを表示（必要なら設定）
                ContextMenu contextMenu = new ContextMenu();
                contextMenu.MenuItems.Add(new MenuItem("再取得", async (s, args) => await DeleteRow(e.RowIndex)));

                contextMenu.Show(dataGridView1, dataGridView1.PointToClient(Cursor.Position));
            }
        }

        private async Task DeleteRow(int rowIndex)
        {
            if (rowIndex >= 0 && rowIndex < dataGridView1.Rows.Count)
            {
                if(MessageBox.Show("この取得履歴を削除し再取得しますか？\n 削除すると再取得間隔がリセットされますが、取得済データは消えません","再取得の確認",MessageBoxButtons.OKCancel) == DialogResult.OK)
                {
                    // IDフィールドの値を取得 
                    object idValue = dataGridView1.Rows[rowIndex].Cells["ID"].Value;
                    if (idValue != null)
                    {
                        // 親データ削除
                        await DeleteReqResultsRecord(idValue);

                        await reloadDataAsync();
                    }
                }
            }
        }

        private async Task DeleteReqResultsRecord(object idValue)
        {
            try
            {
                using (IDbConnection connection = CommonFunctions.GetDbConnection())
                {
                    await ((DbConnection)connection).OpenAsync();

                    string query = "DELETE FROM reqResults WHERE ID = @ID";
                    query = CommonFunctions.ConvertSqlForOleDb(query);

                    using (IDbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = query;
                        CommonFunctions.AddDbParameter(command, "@ID", idValue);

                        int deletedRows = await ((DbCommand)command).ExecuteNonQueryAsync();
                        AddLogAsync($"reqResultsから{deletedRows}件のレコードを削除しました");
                    }
                }
            }
            catch (Exception ex)
            {
                AddLogAsync($"DeleteReqResultsRecordでエラー：{ex.Message}");
            }
        }


        private void InitAnimationTimer()
        {
            // アイコンの配列を用意
            icons = new Icon[]
            {
                Properties.Resources.BlueDrug1,
                Properties.Resources.BlueDrug2,
                Properties.Resources.BlueDrug3,
                Properties.Resources.BlueDrug4
            };

            // タイマーを初期化
            animationTimer = new System.Windows.Forms.Timer
            {
                Interval = 200 // 200msごとに切り替え
            };
            animationTimer.Tick += AnimationTimer_Tick;
        }

        private void InitNotifyIcon()
        {
            // コンテキストメニューを設定
            var contextMenu = new ContextMenuStrip();
            var startStopMenuItem = new ToolStripMenuItem();

            // 状態に応じたメニュー項目を更新
            UpdateStartStopMenuItem(startStopMenuItem);
            startStopMenuItem.Enabled = (okSettings == 0b111);

            // メニューに動的な項目を追加
            startStopMenuItem.Click += (s, e) =>
            {
                // チェックボックスの状態を切り替え
                StartStop.Checked = !StartStop.Checked;

                // メニューを更新
                UpdateStartStopMenuItem(startStopMenuItem);

                StartStop_CheckedChanged(s, EventArgs.Empty); // 開始処理
                
            };

            contextMenu.Items.Add("メイン表示", Properties.Resources.BlueDrug, ShowForm); 
            contextMenu.Items.Add(startStopMenuItem); // 動的な項目を追加
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("薬歴", Properties.Resources.Text_preview,toolStripButtonDI_Click);
            contextMenu.Items.Add("診療情報", Properties.Resources.Equipment, toolStripButtonSinryo_Click);
            contextMenu.Items.Add("健診", Properties.Resources.Heart, toolStripButtonTKK_Click);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("PMDA薬情",Properties.Resources.PMDA, toolStripButtonPMDA_Click);
            contextMenu.Items.Add("終了", Properties.Resources.Exit, ExitApplication);

            notifyIcon1.ContextMenuStrip = contextMenu;

            // バルーン通知の表示イベント
            notifyIcon1.BalloonTipClicked += NotifyIcon_BalloonTipClicked;

            // チェックボックスの状態変更時にもメニューを更新
            StartStop.CheckedChanged += (s, e) => UpdateStartStopMenuItem(startStopMenuItem);

        }

        private void UpdateStartStopMenuItem(ToolStripMenuItem menuItem)
        {
            if (StartStop.Checked)
            {
                menuItem.Text = "停止";
                menuItem.Image = Properties.Resources.Stop;
                //animationTimer?.Start();

                menuItem.Enabled = (okSettings == 0b111);
            }
            else
            {
                menuItem.Text = "開始";
                menuItem.Image = Properties.Resources.Go;
                //animationTimer?.Stop();
                //notifyIcon1.Icon = Properties.Resources.drug1;
                
                menuItem.Enabled = (okSettings == 0b111);
            }
        }

        // イベントが発生した場合にバルーン通知を表示
        public void ShowNotification(string title, string message)
        {
            try
            {
                if (this.InvokeRequired) // this はフォーム
                {
                    this.Invoke(new Action(() => ShowNotification(title, message)));
                }
                else
                {
                    notifyIcon1.BalloonTipTitle = title;
                    notifyIcon1.BalloonTipText = message;
                    notifyIcon1.BalloonTipIcon = ToolTipIcon.Info;
                    notifyIcon1.ShowBalloonTip(5000); // 5秒間表示
                }
            }
            catch (Exception ex)
            {
                AddLogAsync($"エラー：ShowNotification：{ex.Message}");
            }
        }

        // バルーン通知をクリックしたときの処理
        private void NotifyIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            //buttonViewer_Click(sender, EventArgs.Empty);
            toolStripButtonDI_Click(sender, EventArgs.Empty);
        }

        // フォームを表示する
        private async void ShowForm(object sender, EventArgs e)
        {
            isFormVisible = true;

            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            this.Show();
            this.Activate();                          // フォームをアクティブ化

            await reloadDataAsync(true);
        }

        // アプリケーションを終了する
        private void ExitApplication(object sender, EventArgs e)
        {
            buttonExit_Click(toolStripButtonExit, EventArgs.Empty);
        }

        
        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized || !this.Visible)
            {
                ShowForm(notifyIcon1, EventArgs.Empty);
            }
            else
            {
                // 表示されている場合はタスクトレイに最小化
                toolStripButtonToTaskTray_Click(sender, EventArgs.Empty);
            }
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            // アイコンを切り替える
            notifyIcon1.Icon = icons[currentFrame];
            currentFrame = (currentFrame + 1) % icons.Length; // フレームを更新
        }

        private async void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            await DeleteClientAsync();

            // クリーンアップ
            //timer?.Stop();
            //timer?.Dispose();
            backgroundTimer?.Dispose();

            notifyIcon1?.Dispose();
            animationTimer?.Stop();
            animationTimer?.Dispose();

            if (_dumpTimer != null) StopDumpTimer();

            BackupSettings();

            if (Properties.Settings.Default.DBtype == "mdb")
            {
                BackupGenerations(Properties.Settings.Default.OQSDrugData, OQSFolder, 7);
            }
            //base.OnFormClosing(e);
        }

        private void toolStripButtonToTaskTray_Click(object sender, EventArgs e)
        {
            // フォームを非表示にし、タスクバーから削除
            this.Hide();
            this.ShowInTaskbar = false;

            isFormVisible = false;
        }
               
        private async void buttonReload_Click(object sender, EventArgs e)
        {
            skipReload = false;
            await Task.Run(async ()=>  await reloadDataAsync());
        }

        private void listViewLog_SizeChanged(object sender, EventArgs e)
        {
            // 残りの幅を "Log" 列に割り当て
            listViewLog.Columns[1].Width = -2;
        }

        private async void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                var cellValue = dataGridView1.Rows[e.RowIndex].Cells["PtID"].Value;

                if (cellValue != null && long.TryParse(cellValue.ToString(), out long ptId))
                {
                    tempId = ptId / 10;
                    AddLogAsync($"{tempId}のダブルクリックを検知しました");

                    forceIdLink = true;

                    var categoryValue = dataGridView1.Rows[e.RowIndex].Cells["category"].Value;
                    if (categoryValue != null && int.TryParse(categoryValue.ToString(), out int category))
                    {
                        if(category >= 100)
                        {
                            await OpenTKKHistory(tempId, true);
                        } else if(category >= 10)
                        {
                            await OpenDrugHistory(tempId, true);
                        }
                    }

                    //forceIdLink = false; //Form3側でクリアする
                }
                else
                {
                    MessageBox.Show("患者IDデータがありません。");
                }
            }
        }

        private async Task<string> GetRSBdrive()
        {
            // C: から F: ドライブまで
            for (char driveLetter = 'C'; driveLetter <= 'F'; driveLetter++)
            {
                string drivePath = $@"{driveLetter}:\";
                string fullPath = drivePath + @"Users\rsn\public_html\drug_RSB.dat";
               
                // 非同期タスクとしてファイルの存在を確認
                bool exists = await Task.Run(() => File.Exists(fullPath));

                if (exists)
                {
                    AddLogAsync($"RSBaseが{drivePath}ドライブに見つかりました。薬歴情報をバッファに読み込んでいます...");
                    return drivePath; // 見つかったドライブを返す
                }
            }

            AddLogAsync("RSBaseが見つかりませんでした。");
            return null; // 見つからなかった場合
        }

        private void checkBoxAutoStart_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.AutoStart = checkBoxAutoStart.Checked;
            Properties.Settings.Default.Save(); 

        }

        private async Task LoadRSBDIAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                AddLogAsync($"指定されたRSBase薬情ファイルが見つかりませんでした。{filePath}");
                return;
            }

            // ファイルを非同期で読み込み
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new StreamReader(stream, Encoding.GetEncoding("EUC-JP")))
            {
                string line;
                int count = 0;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    // カンマ区切りで分割し、指定カラム(0, 3, 8)のみ取得
                    var columns = line.Split(',');
                    if (columns.Length > 7) // 必要なカラム数が存在するか確認
                    {
                        CommonFunctions.RSBDI.Add(new string[] { columns[0], columns[3], columns[8], columns[5], columns[1] }); // 0:商品名、1:一般名、2:コード、3：先発、4：薬価
                        count++;
                    }
                }
                AddLogAsync($"RSBase薬情インデックス{count}件を読み込みました。");
                AddLogAsync("薬歴の右クリックでRSBase薬情表示が可能になります。");
            }
        }

        // sgml_rawdata ∩ drug_code_map を対象に SGMLDI を構築
        private async Task LoadSGMLDIAsync()
        {
            try
            {
                await CommonFunctions.AddLogAsync("SGML薬情インデックス（sgml_rawdata×drug_code_map）の読み込みを開始します…");

                // yj_codeごとに集約した drug_code_map を中間CTEで作成
                const string sql = @"
                    WITH s1 AS (
                      SELECT DISTINCT ON (s.yj_code)
                        s.yj_code,
                        s.package_insert_no,
                        s.brand_name_ja,
                        s.generic_name_ja
                      FROM public.sgml_rawdata s
                      WHERE s.yj_code IS NOT NULL AND s.yj_code <> ''
                      ORDER BY s.yj_code, s.package_insert_no DESC
                    ),
                    d1 AS (
                      -- YJ（drug_code_map）側。is_generic を必ず取得
                      SELECT DISTINCT ON (d.yj_code)
                        d.yj_code,
                        d.is_generic,   -- TRUE=後発, FALSE=先発候補
                        d.price,
                        d.updated_at
                      FROM public.drug_code_map d
                      WHERE d.yj_code IS NOT NULL AND d.yj_code <> ''
                      ORDER BY d.yj_code, d.updated_at DESC NULLS LAST
                    ),
                    m1 AS (
                      -- MEDIS 側。is_generic と original_brand を取得
                      SELECT DISTINCT ON (m.yj_code)
                        m.yj_code,
                        m.is_generic,       -- TRUE=後発, FALSE=先発/準先発, NULL=不明
                        m.original_brand,   -- '先発品' / '準先発品' / 空白
                        m.updated_at
                      FROM public.drug_medis_generic m
                      WHERE m.yj_code IS NOT NULL AND m.yj_code <> ''
                      ORDER BY m.yj_code, m.updated_at DESC NULLS LAST
                    )
                    SELECT
                      s1.yj_code,
                      s1.brand_name_ja,
                      s1.generic_name_ja,
                      CASE
                        -- 1) YJで後発なら即「空白」（後発）
                        WHEN d1.is_generic = TRUE THEN ''
                        -- 2) YJで先発候補（=FALSE）
                        WHEN d1.is_generic = FALSE THEN
                          CASE
                            -- 2-1) MEDIS未収載 → 新薬想定 → 「先発」
                            WHEN m1.yj_code IS NULL THEN '先発'
                            -- 2-2) MEDISで先発/準先発（=is_generic=FALSE かつ original_brandあり） → そのまま継承
                            WHEN m1.is_generic = FALSE AND COALESCE(m1.original_brand, '') <> '' THEN m1.original_brand
                            -- 2-3) MEDISはあるが後発/不明（is_generic=TRUE or NULL、あるいは original_brand 空） → 後発扱いで空白
                            ELSE ''
                          END
                        -- 想定外は後発扱い
                        ELSE ''
                      END AS originator_flag,
                      d1.price,
                      s1.package_insert_no
                    FROM s1
                    JOIN d1 ON d1.yj_code = s1.yj_code
                    LEFT JOIN m1 ON m1.yj_code = s1.yj_code;
                    ";



                var result = new List<string[]>();

                using (var conn = CommonFunctions.GetDbConnection(true))
                {
                    if (conn is DbConnection dbc) await dbc.OpenAsync(); else conn.Open();

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = sql;

                        using (var r = cmd.ExecuteReader())
                        {
                            int addCount = 0;
                            while (r.Read())
                            {
                                var yj = SafeStr(r, 0);
                                var brand = SafeStr(r, 1);
                                var generic = SafeStr(r, 2);
                                var originator = SafeStr(r, 3);
                                var packageNo = SafeStr(r, 5);

                                string priceStr = "";
                                if (!r.IsDBNull(4))
                                {
                                    // priceはnumeric。文字列化（表示都合で小数不要ならToString("0")などに）
                                    var priceVal = Convert.ToDecimal(r.GetValue(4));
                                    priceStr = priceVal.ToString("0.##"); // 必要なら "0.##" などで整形
                                }

                                // SGMLDI: [0:商品名, 1:一般名, 2:yj_code, 3:先発, 4:薬価]
                                result.Add(new string[]
                                {
                                    brand,
                                    generic,
                                    yj,
                                    originator,
                                    priceStr,
                                    packageNo
                                });
                                addCount++;
                            }
                            await CommonFunctions.AddLogAsync($"SGML薬情インデックス {addCount}件を読み込みました（両表に存在するyj_codeのみ）。");
                        }
                    }
                }

                CommonFunctions.SGMLDI.Clear();
                CommonFunctions.SGMLDI.AddRange(result);

                CommonFunctions._readySGML = true;

                await CommonFunctions.AddLogAsync("薬歴右クリックのSGMLベース薬情表示が利用可能になりました。");
            }
            catch (Exception ex)
            {
                await CommonFunctions.AddLogAsync($"SGML薬情インデックスの読み込みエラー: {ex.Message}");
                CommonFunctions._readySGML = false;
            }
        }

        private static string SafeStr(IDataRecord r, int ordinal)
        {
            if (ordinal < 0 || ordinal >= r.FieldCount) return "";
            var v = r.GetValue(ordinal);
            return (v == null || v is DBNull) ? "" : v.ToString().Trim();
        }

        private void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            Color YZcolor = Color.FromArgb(230, 255, 230);
            Color TKcolor = Color.FromArgb(255, 230, 230);

            // "category"列のインデックスを取得
            int categoryIndex = dataGridView1.Columns["category"].Index;

            // "category"列かどうかを確認
            if (e.ColumnIndex == categoryIndex)
            {
                // 現在の行のcategory列の値を取得
                if (int.TryParse(dataGridView1.Rows[e.RowIndex].Cells[categoryIndex].Value?.ToString(), out int categoryValue))
                {
                    // 行全体の背景色を変更
                    if (categoryValue >= 10 && categoryValue <= 99) // 2桁の場合
                    {
                        dataGridView1.Rows[e.RowIndex].DefaultCellStyle.BackColor = YZcolor;
                    }
                    else if (categoryValue >= 100 && categoryValue <= 999) // 3桁の場合
                    {
                        dataGridView1.Rows[e.RowIndex].DefaultCellStyle.BackColor = TKcolor;
                    }
                    else
                    {
                        // デフォルトの色に戻す場合
                        dataGridView1.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.White;
                    }
                }
            }
        }

        public async void toolStripButtonTKK_Click(object sender, EventArgs e)
        {
            string strPtIDmain = null;

            toolStripVersion.Invoke(new Action(() =>
                strPtIDmain = toolStripTextBoxPtIDmain.Text
            ));

            if (long.TryParse(strPtIDmain, out long idValue))
            {
                // 数値に変換できた場合
                tempId = idValue;
                forceIdLink = true;
            }
            
            await OpenTKKHistory(tempId, true, true);
        }

        private void BackupSettings()
        {
            try
            {
                // 完全なファイルパスを生成
                string defaultPath = Path.Combine(OQSFolder, $"OQSDrug_{Environment.MachineName}.config");

                //Daily backup
                if (File.Exists(defaultPath))
                {
                    BackupGenerations(defaultPath, OQSFolder, 7);
                }

                // 設定を保存
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
                config.SaveAs(defaultPath, ConfigurationSaveMode.Full);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"設定値のエクスポート時にエラーが発生しました：{ex.Message}");
            }
        }

        private void BackupGenerations(string sourceFile, string backupFolder, int generationNumber)
        {
            if (!File.Exists(sourceFile))
            {
                AddLogAsync($"バックアップ元のファイルが見つかりません: {sourceFile}");
                return;
            }

            // バックアップフォルダの作成
            if (!Directory.Exists(backupFolder))
            {
                Directory.CreateDirectory(backupFolder);
            }

            // バックアップファイル名（"元のファイル名_YYYYMMDD.拡張子"）
            string fileName = Path.GetFileNameWithoutExtension(sourceFile);
            string extension = Path.GetExtension(sourceFile);
            string today = DateTime.Now.ToString("yyyyMMdd");
            string backupFilePath = Path.Combine(backupFolder, $"{fileName}_{today}{extension}");

            // 既に今日のバックアップが存在する場合はスキップ
            if (File.Exists(backupFilePath))
            {
                AddLogAsync($"本日のバックアップは既に存在するのでバックアップ処理をスキップします: {backupFilePath}");
            }
            else
            {
                try
                {
                    // **同期的にファイルをコピー**
                    File.Copy(sourceFile, backupFilePath, true);

                    AddLogAsync($"バックアップ完了: {backupFilePath}");
                }
                catch (Exception ex)
                {
                    AddLogAsync($"バックアップエラー: {ex.Message}");
                    return;
                }
            }

            try
            {
                // バックアップファイルの一覧を取得し、日付降順にソート
                var backupFiles = Directory.GetFiles(backupFolder, $"{fileName}_*{extension}")
                                           .OrderByDescending(f => f)
                                           .ToList();

                // 指定世代数を超えたファイルを削除
                if (backupFiles.Count > generationNumber)
                {
                    foreach (var oldFile in backupFiles.Skip(generationNumber))
                    {
                        File.Delete(oldFile);
                        AddLogAsync($"古いバックアップを削除: {oldFile}");
                    }
                }
            }
            catch (Exception ex)
            {
                AddLogAsync($"古いバックアップ削除エラー: {ex.Message}");
            }
        }

        // 排他 チェック関数
        public async Task<bool> IsAccessAllowedAsync()
        {
            string localMachineName = Environment.MachineName;
            string query = @"
                SELECT clientName, lastUpdated 
                FROM connectedClient 
                WHERE clientName <> @clientName 
                  AND lastUpdated > @lastUpdated";

            try
            {
                using (var connection = CommonFunctions.GetDbConnection(ReadOnly: true))
                {
                    await ((DbConnection)connection).OpenAsync();

                    string convertedSql = CommonFunctions.ConvertSqlForOleDb(query);

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = convertedSql;

                        DateTime threshold = DateTime.UtcNow.AddMinutes(-3);  // UTC推奨

                        CommonFunctions.AddDbParameter(command, "@clientName", localMachineName);
                        CommonFunctions.AddDbParameter(command, "@lastUpdated", threshold);

                        using (var reader = await ((DbCommand)command).ExecuteReaderAsync())
                        {
                            return !reader.HasRows; // 他に使用中クライアントがなければ true
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error:IsAccessAllowedAsync {ex.Message}");
                return false;
            }
        }


        public async Task UpdateClientAsync()
        {
            string localMachineName = Environment.MachineName;

            try
            {
                using (IDbConnection connection = CommonFunctions.GetDbConnection(false))
                {
                    await ((DbConnection)connection).OpenAsync();

                    string updateSql = @"
                UPDATE connectedClient 
                SET lastUpdated = @lastUpdated 
                WHERE clientName = @clientName";

                    string insertSql = @"
                INSERT INTO connectedClient (clientName, lastUpdated) 
                VALUES (@clientName, @lastUpdated)";

                    updateSql = CommonFunctions.ConvertSqlForOleDb(updateSql);
                    insertSql = CommonFunctions.ConvertSqlForOleDb(insertSql);

                    using (IDbCommand updateCommand = connection.CreateCommand())
                    {
                        updateCommand.CommandText = updateSql;
                        CommonFunctions.AddDbParameter(updateCommand, "@lastUpdated", DateTime.UtcNow);
                        CommonFunctions.AddDbParameter(updateCommand, "@clientName", localMachineName);

                        int affectedRows = await ((DbCommand)updateCommand).ExecuteNonQueryAsync();

                        if (affectedRows == 0)
                        {
                            using (IDbCommand insertCommand = connection.CreateCommand())
                            {
                                insertCommand.CommandText = insertSql;
                                CommonFunctions.AddDbParameter(insertCommand, "@clientName", localMachineName);
                                CommonFunctions.AddDbParameter(insertCommand, "@lastUpdated", DateTime.UtcNow);

                                await ((DbCommand)insertCommand).ExecuteNonQueryAsync();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"UpdateClientAsyncでエラー: {ex.Message}");
            }
        }



        private async void toolStripButtonDI_Click(object sender, EventArgs e)
        {
            string strPtIDmain = null;

            toolStripVersion.Invoke(new Action(() =>
                strPtIDmain = toolStripTextBoxPtIDmain.Text
            ));

            if (long.TryParse(strPtIDmain, out long idValue))
            {
                // 数値に変換できた場合
                tempId = idValue;
                forceIdLink = true;
            }
            
            await OpenDrugHistory(tempId, true, true);
        }

        public async Task DeleteClientAsync()
        {
            string localMachineName = Environment.MachineName;

            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2))) // 2秒タイムアウト
            {
                try
                {
                    using (IDbConnection connection = CommonFunctions.GetDbConnection(false))
                    {
                        var openTask = ((DbConnection)connection).OpenAsync(cts.Token);
                        if (await Task.WhenAny(openTask, Task.Delay(TimeSpan.FromSeconds(2))) != openTask)
                        {
                            throw new TimeoutException("データベース接続がタイムアウトしました。");
                        }

                        string sql = "DELETE FROM connectedClient WHERE clientName = @clientName";
                        sql = CommonFunctions.ConvertSqlForOleDb(sql);

                        using (IDbCommand command = connection.CreateCommand())
                        {
                            command.CommandText = sql;
                            CommonFunctions.AddDbParameter(command, "@clientName", localMachineName);

                            var executeTask = ((DbCommand)command).ExecuteNonQueryAsync(cts.Token);
                            if (await Task.WhenAny(executeTask, Task.Delay(TimeSpan.FromSeconds(2))) != executeTask)
                            {
                                ((DbCommand)command).Cancel(); // タイムアウト時にキャンセル
                                throw new TimeoutException("DELETE クエリがタイムアウトしました。");
                            }
                        }
                    }
                }
                catch (TimeoutException ex)
                {
                    MessageBox.Show($"処理がタイムアウトしました: {ex.Message}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"DeleteClientAsync でエラー: {ex.Message}");
                }
            }
        }


        private async void toolStripButtonDebug_Click(object sender, EventArgs e)
        {
            if (long.TryParse(toolStripTextBoxDebug.Text, out long ptId))
            {
                OpenFileDialog op = new OpenFileDialog();
                op.Title = "xmlファイルの読込";
                op.FileName = "*.xml";
                //op.InitialDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                op.Filter = "xmlファイル(*.xml)|*.xml|すべてのファイル(*.*)|*.*";
                op.FilterIndex = 1;
                op.RestoreDirectory = true;
                op.CheckFileExists = false;
                op.CheckPathExists = true;

                if (op.ShowDialog(this) == DialogResult.OK)
                {
                    string settingsFilePath = op.FileName;

                    var xmlDoc = new XmlDocument();

                    xmlDoc.Load(settingsFilePath);

                    MessageBox.Show(await ProcessDrugInfoAsync2(ptId, xmlDoc));
                }

            }
            else
            {
                MessageBox.Show("ID(枝番付)を入力してから実行してください");
            }

            
        }

        private void toolStripVersion_DoubleClick(object sender, EventArgs e)
        {
            toolStripComboBoxDBProviders.Visible = !toolStripComboBoxDBProviders.Visible;
            toolStripButtonDebug.Visible = !toolStripButtonDebug.Visible;
            toolStripSeparatorDebug1.Visible = !toolStripSeparatorDebug1.Visible;
            toolStripSeparatorDebug2.Visible = !toolStripSeparatorDebug2.Visible;
            toolStripTextBoxDebug.Visible = !toolStripTextBoxDebug.Visible;
            toolStripComboBoxConnectionMode.Visible = !toolStripComboBoxConnectionMode.Visible;

        }

        

        private void dataGridView1_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            e.ToolTipText = "行選択⇢右クリックで再取得メニュー表示\r\nダブルクリックで薬歴/健診歴を表示します\r\n";
        }

        private void dataGridView1_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            skipReload = true;
        }

        private void toolStripButtonLog_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo(LogFile) { UseShellExecute = true });
        }

        private void toolStripTextBoxPtIDmain_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                toolStripButtonDI_Click(sender, e);
            }
        }

        //private void textBoxPtIDmain_KeyDown(object sender, KeyEventArgs e)
        //{
        //    if (e.KeyCode == Keys.Enter)
        //    {
        //        buttonPtIDSearch_Click(sender, EventArgs.Empty);
        //    }
        //}

        public async void toolStripButtonSinryo_Click(object sender, EventArgs e)
        {
            string strPtIDmain = null;

            toolStripVersion.Invoke(new Action(() =>
                strPtIDmain = toolStripTextBoxPtIDmain.Text
            ));

            if (long.TryParse(strPtIDmain, out long idValue))
            {
                // 数値に変換できた場合
                tempId = idValue;
                forceIdLink = true;
            }
            
            await OpenSinryoHistory(tempId, true, true);
        }

        public async Task LoadKoroDataAsync()
        {
            try
            {
                string koroPath = Path.Combine(Path.GetDirectoryName(Properties.Settings.Default.OQSDrugData), "KOROdata.mdb");

                if (!File.Exists(koroPath))
                {
                    AddLogAsync("エラー: KOROdata.mdb が見つかりません。");
                    return;
                }

                AddLogAsync("KOROdataが見つかりましたので薬品名コードを読み込みます");

                string connectionKoroData = $"Provider={CommonFunctions.DBProvider};Data Source={koroPath};Mode={DynaReadMode};";
                string sql = "SELECT 医薬品コード AS ReceptCode, 薬価基準コード AS MedisCode " +
                             " FROM TG医薬品マスター " +
                             " WHERE (((薬価基準コード) IS NOT NULL));";
                int count = 0;
                var tempDictionary = new Dictionary<string, string>();

                using (OleDbConnection connection = new OleDbConnection(connectionKoroData))
                {
                    await connection.OpenAsync();

                    using (OleDbCommand command = new OleDbCommand(sql, connection))
                    using (OleDbDataReader reader = (OleDbDataReader)await command.ExecuteReaderAsync())
                    {
                        // データを一時Dictionaryに格納（エラー時にデータを消さないようにする）
                        while (await reader.ReadAsync())
                        {
                            string receptCode = reader["ReceptCode"].ToString();
                            string medisCode = reader["MedisCode"].ToString();

                            if (!tempDictionary.ContainsKey(receptCode))
                            {
                                tempDictionary.Add(receptCode, medisCode);
                                count++;
                            }
                        }
                    }
                }

                // 読み込み成功したらクリア＆更新
                CommonFunctions.ReceptToMedisCodeMap.Clear();
                foreach (var pair in tempDictionary)
                {
                    CommonFunctions.ReceptToMedisCodeMap.Add(pair.Key, pair.Value);
                }

                AddLogAsync($"KOROdataから{count}件のコードを読み込みました");
            }
            catch (Exception ex)
            {
                AddLogAsync($"エラー: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public async Task LoadKoro2SQL()
        {
            try
            {
                // === 0) KOROdata.mdb パス確認 ===
                string koroPath = Path.Combine(
                    Path.GetDirectoryName(Properties.Settings.Default.OQSDrugData),
                    "KOROdata.mdb"
                );

                if (!File.Exists(koroPath))
                {
                    //KOROを読めないときは、SGMLDIの読み込みだけ行う
                    await AddLogAsync("KOROdata.mdb が見つかりませんでした");

                    if (Properties.Settings.Default.DBtype == "pg")
                    {
                        using (var conn = CommonFunctions.GetDbConnection())
                        {
                            await OpenAsync(conn);
                            await RefreshDictionaryFromDbAsync(conn);
                            await AddLogAsync("drug_code_mapをDBから更新しました。");
                        }
                        //SGML DIのロード
                        await LoadSGMLDIAsync();
                        await AddLogAsync("SGML薬剤情報インデックスをDBから更新しました。");
                    }
                }
                else if (Properties.Settings.Default.DBtype == "pg")
                {
                    // === 1) KORO 側の最新更新日を取得 ===
                    DateTime? koroVersion = await GetKoroLatestVersionAsync(koroPath);
                    if (koroVersion == null)
                    {
                        await AddLogAsync("エラー: KOROdataの更新日が取得できません。");
                        return;
                    }
                    await AddLogAsync($"KOROdata 更新日: {koroVersion:yyyy-MM-dd HH:mm:ss}");

                    // === 2) ターゲットDBオープン＆テーブル準備 ===
                    using (var conn = CommonFunctions.GetDbConnection())
                    {
                        await OpenAsync(conn);
                        await EnsureTablesAsync(conn); // なければ作成（Accessは失敗しても無視）

                        // === 3) 既存バージョンと比較 ===
                        DateTime? currentVersion = await GetCurrentVersionAsync(conn, 1);
                        DateTime? currentMedisVersion = await GetCurrentVersionAsync(conn, 2);
                        DateTime? currentContraVer = await GetCurrentVersionAsync(conn, 3);

                        if (currentVersion != null && currentVersion >= koroVersion && currentMedisVersion != null && currentMedisVersion >= koroVersion && currentContraVer != null && currentContraVer >= koroVersion)
                        {
                            await AddLogAsync("KOROdataは最新版をロード済みのため、drug_code_mapテーブルの更新をスキップします。");
                        }
                        else
                        {
                            await AddLogAsync("KOROdataが更新されているため、drug_code_mapテーブルを再構築します…");
                                                       
                            // === 5) 高速ルートで全入れ替え（DBごとに最速を使用）===
                            if (conn is Npgsql.NpgsqlConnection pg)
                            {
                                await BulkLoadPostgresFromKoroAsync(pg, koroPath, koroVersion.Value);
                                await BulkLoadPostgresFromMedisAsync(pg, koroPath, koroVersion.Value);
                                await BulkLoadPostgresFromContraindicationAsync(pg, koroPath, koroVersion.Value);

                                // 大量挿入後に補助インデックス再作成（既存の RebuildIndexesAsync をそのまま呼んでOK）
                                await RebuildIndexesAsync(pg);
                            }
                            else if (conn is System.Data.OleDb.OleDbConnection acc)
                            {
                                await BulkLoadAccessFromKoroAsync(acc, koroPath, koroVersion.Value);
                            }
                            else
                            {
                                await AddLogAsync("未対応プロバイダのため逐次INSERTで処理します。");
                                //await FallbackInsertLoopAsync(conn, map); // ※保険（任意）
                            }

                            await AddLogAsync("drug_code_mapテーブルを更新しました。");
                        }
                        // === 6) 最後に Dictionary を DB から更新 ===
                        await RefreshDictionaryFromDbAsync(conn);
                        await AddLogAsync("drug_code_mapをDBから読み込みました。");
                        //SGML DIのロード
                        await LoadSGMLDIAsync();
                        await AddLogAsync("SGML薬剤情報インデックスをDBから読み込みました。");
                    }
                }
                else // Accessの場合はDictionaryのみロード
                {
                    CommonFunctions.ReceptToMedisCodeMap = await ReadKoroMapAsync(koroPath);
                    await AddLogAsync($"KOROdataから {CommonFunctions.ReceptToMedisCodeMap.Count} 件のマッピングを取得。");
                }
            }
            catch (Exception ex)
            {
                await AddLogAsync($"エラー: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private async Task BulkLoadPostgresFromKoroAsync(Npgsql.NpgsqlConnection pgConn, string koroPath, DateTime koroVersion)
        {
            var nowUns = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

            using (var tx = pgConn.BeginTransaction())
            {
                // 速度チューニング
                using (var set = pgConn.CreateCommand())
                {
                    set.Transaction = tx;
                    set.CommandText = "SET LOCAL synchronous_commit TO OFF";
                    set.ExecuteNonQuery();
                }

                // ★ DROP → CREATE
                using (var ddl = pgConn.CreateCommand())
                {
                    ddl.Transaction = tx;
                    ddl.CommandText = @"
                        DROP TABLE IF EXISTS public.drug_code_map;

                        CREATE TABLE public.drug_code_map
                        (
                            drugc       character varying(64)  NOT NULL,
                            yj_code     character varying(32)  NOT NULL,
                            yj7         character varying(7),
                            drugn       text,
                            is_generic  boolean,
                            price       numeric,
                            updated_at  timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
                            CONSTRAINT drug_code_map_pkey PRIMARY KEY (drugc)
                        );

                        ALTER TABLE public.drug_code_map OWNER TO postgres;

                        CREATE INDEX IF NOT EXISTS idx_drug_code_map_yj
                            ON public.drug_code_map USING btree (yj_code ASC NULLS LAST);

                        CREATE INDEX IF NOT EXISTS idx_drug_code_map_yj7
                            ON public.drug_code_map USING btree (yj7 ASC NULLS LAST);";
                                            ddl.ExecuteNonQuery();
                }

                // KORO (Access) から読み出し
                string koroConnStr = $"Provider={CommonFunctions.DBProvider};Data Source={koroPath};Mode=Read;Jet OLEDB:Database Locking Mode=1;";
                using (var koro = new System.Data.OleDb.OleDbConnection(koroConnStr))
                {
                    await koro.OpenAsync();

                    const string sql = @"
                        SELECT 
                            医薬品コード   AS ReceptCode,
                            薬価基準コード AS MedisCode,
                            漢字名称       AS drugn,
                            後発品         AS IsGenericByte,
                            金額           AS Price
                        FROM TG医薬品マスター
                        WHERE 薬価基準コード IS NOT NULL";

                    using (var kcmd = new System.Data.OleDb.OleDbCommand(sql, koro))
                    using (var r = await kcmd.ExecuteReaderAsync())
                    using (var writer = pgConn.BeginBinaryImport(@"
                        COPY public.drug_code_map 
                        (drugc, yj_code, yj7, drugn, is_generic, price, updated_at) 
                        FROM STDIN (FORMAT BINARY)"))
                    {
                        CommonFunctions.ReceptToMedisCodeMap.Clear();
                        int i = 0;

                        while (await r.ReadAsync())
                        {
                            string recept = r["ReceptCode"]?.ToString() ?? "";
                            string yj = r["MedisCode"]?.ToString() ?? "";
                            string yj7 = (yj.Length >= 7) ? yj.Substring(0, 7) : yj;
                            string drugn = r["drugn"]?.ToString() ?? "";

                            // 後発品列（Byte）: 0=先発, 1=後発 と仮定
                            bool? isGeneric = null;
                            if (r["IsGenericByte"] != DBNull.Value)
                            {
                                byte b = Convert.ToByte(r["IsGenericByte"]);
                                isGeneric = (b != 0); // 1=後発, 0=先発
                            }

                            decimal? price = null;
                            if (r["Price"] != DBNull.Value)
                            {
                                double d = Convert.ToDouble(r["Price"]);
                                price = Convert.ToDecimal(d);
                            }

                            writer.StartRow();
                            writer.Write(recept, NpgsqlTypes.NpgsqlDbType.Varchar);
                            writer.Write(yj, NpgsqlTypes.NpgsqlDbType.Varchar);
                            writer.Write(yj7, NpgsqlTypes.NpgsqlDbType.Varchar);
                            writer.Write(drugn, NpgsqlTypes.NpgsqlDbType.Text); 
                            writer.Write(isGeneric.HasValue ? (object)isGeneric.Value : DBNull.Value, NpgsqlTypes.NpgsqlDbType.Boolean);
                            writer.Write(price.HasValue ? (object)price.Value : DBNull.Value, NpgsqlTypes.NpgsqlDbType.Numeric);
                            writer.Write(nowUns, NpgsqlTypes.NpgsqlDbType.Timestamp);

                            if (!CommonFunctions.ReceptToMedisCodeMap.ContainsKey(recept))
                                CommonFunctions.ReceptToMedisCodeMap[recept] = yj;

                            if (++i % 5000 == 0)
                                await AddLogAsync($"…{i} 件COPY中 (drug_code_map)");
                        }

                        writer.Complete();
                    }
                }

                // バージョン更新
                using (var delVer = pgConn.CreateCommand())
                {
                    delVer.Transaction = tx;
                    delVer.CommandText = "DELETE FROM drug_code_map_version WHERE id = 1";
                    await delVer.ExecuteNonQueryAsync();
                }
                using (var insVer = pgConn.CreateCommand())
                {
                    insVer.Transaction = tx;
                    insVer.CommandText = "INSERT INTO drug_code_map_version (id, source_version) VALUES (1, @v)";
                    insVer.Parameters.Add(new Npgsql.NpgsqlParameter("@v", koroVersion));
                    await insVer.ExecuteNonQueryAsync();
                }

                tx.Commit();
            }
        }


        private async Task BulkLoadAccessFromKoroAsync(System.Data.OleDb.OleDbConnection accConn, string koroPath, DateTime koroVersion)
        {
            using (var tx = accConn.BeginTransaction())
            using (var cmd = accConn.CreateCommand())
            {
                cmd.Transaction = tx;

                // 旧データ削除
                cmd.CommandText = "DELETE FROM drug_code_map";
                await cmd.ExecuteNonQueryAsync();

                // KORO側から一括コピー（IN 'path' がキモ）
                string escPath = koroPath.Replace("'", "''"); // パスのエスケープ
                cmd.CommandText = $@"
            INSERT INTO drug_code_map (drugc, yj_code, yj7, drugn, updated_at)
            SELECT
                医薬品コード,
                薬価基準コード,
                Left(薬価基準コード, 7) AS yj7,
                NULL AS drugn,
                Now() AS updated_at
            FROM TG医薬品マスター IN '{escPath}'
            WHERE 薬価基準コード IS NOT NULL";
                await cmd.ExecuteNonQueryAsync();

                // バージョン更新（DELETE→INSERT）
                cmd.CommandText = "DELETE FROM drug_code_map_version WHERE id = 1";
                await cmd.ExecuteNonQueryAsync();

                cmd.CommandText = "INSERT INTO drug_code_map_version (id, source_version) VALUES (1, ?)";
                var p = cmd.CreateParameter();
                p.Value = koroVersion; cmd.Parameters.Clear(); cmd.Parameters.Add(p);
                await cmd.ExecuteNonQueryAsync();

                tx.Commit();
            }

            // 大量挿入後のインデックス再作成（Access構文）
            await RebuildIndexesAsync(accConn);

            // DictionaryはDBから一括再読込が速い
            await RefreshDictionaryFromDbAsync(accConn);
        }

        private async Task BulkLoadPostgresFromMedisAsync(NpgsqlConnection pgConn, string koroPath, DateTime medisVersion)
        {
            // Access接続（Koroと同じプロバイダ指定）
            string accConnStr = $"Provider={CommonFunctions.DBProvider};Data Source={koroPath};Mode=Read;";

            // Postgres: 同期Tx
            using (var tx = pgConn.BeginTransaction())
            {
                using (var set = pgConn.CreateCommand())
                {
                    set.Transaction = tx;
                    set.CommandText = "SET LOCAL synchronous_commit TO OFF";
                    set.ExecuteNonQuery();
                }

                using (var ddl = pgConn.CreateCommand())
                {
                    ddl.Transaction = tx;
                    ddl.CommandText = @"
                        DROP TABLE IF EXISTS public.drug_medis_generic;

                        CREATE TABLE public.drug_medis_generic
                        (
                            yakka_code      text PRIMARY KEY,
                            yj_code         text,
                            generic_name    text,
                            brand_name      text,
                            unit            text,
                            company_name    text,
                            is_generic      boolean,
                            original_brand  text,
                            no_origin_generic boolean,
                            min_price       numeric,
                            updated_at      timestamp without time zone DEFAULT now()
                        );

                        CREATE INDEX IF NOT EXISTS idx_medis_yj_code      ON public.drug_medis_generic (yj_code);
                        CREATE INDEX IF NOT EXISTS idx_medis_brand_name   ON public.drug_medis_generic (brand_name);
                        CREATE INDEX IF NOT EXISTS idx_medis_generic_name ON public.drug_medis_generic (generic_name);
                    ";
                    ddl.ExecuteNonQuery();
                }

                using (var acc = new OleDbConnection(accConnStr))
                {
                    await acc.OpenAsync();

                    const string sql = @"
                        SELECT
                            [薬価基準コード]            AS yakka,
                            [一般名]                    AS generic_name,
                            [販売名称]                  AS brand_name,
                            [規格単位]                  AS unit,
                            [販売会社]                  AS company_name,
                            [後発品]                    AS ge_col,
                            [先発品]                    AS origin_brand,
                            [先発品のない後発医薬品]    AS no_origin,
                            [最低薬価]                  AS min_price
                        FROM [T_MEDIS一般名]
                        WHERE [薬価基準コード] IS NOT NULL
                    ";

                    using (var cmd = new OleDbCommand(sql, acc))
                    using (var r = await cmd.ExecuteReaderAsync())
                    using (var writer = pgConn.BeginBinaryImport(@"
                        COPY public.drug_medis_generic
                        (yakka_code, yj_code, generic_name, brand_name, unit, company_name,
                         is_generic, original_brand, no_origin_generic, min_price, updated_at)
                        FROM STDIN (FORMAT BINARY)"))
                    {
                        var nowUns = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
                        int i = 0;
                        var seen = new HashSet<string>();   // 🔹 重複チェック用

                        while (await r.ReadAsync())
                        {
                            string yakka = r["yakka"]?.ToString()?.Trim();
                            if (string.IsNullOrEmpty(yakka)) continue;

                            // 🔹 既に出た薬価コードならスキップ
                            if (!seen.Add(yakka))
                                continue;

                            string yjCode = yakka;
                            string gname = r["generic_name"]?.ToString();
                            string bname = r["brand_name"]?.ToString();
                            string unit = r["unit"]?.ToString();
                            string company = r["company_name"]?.ToString();
                            string geCol = r["ge_col"]?.ToString();
                            string origin = r["origin_brand"]?.ToString();
                            string noOrigin = r["no_origin"]?.ToString();
                            string minPriceS = r["min_price"]?.ToString();

                            bool isGeneric = string.IsNullOrEmpty(origin) && !origin.Contains("先発");
                            bool noOriginGeneric = !string.IsNullOrWhiteSpace(noOrigin);

                            decimal? minPrice = null;
                            if (decimal.TryParse(minPriceS, out var dec)) minPrice = dec;

                            writer.StartRow();
                            writer.Write(yakka, NpgsqlDbType.Varchar);
                            writer.Write(yjCode, NpgsqlDbType.Varchar);
                            writer.Write(gname ?? "", NpgsqlDbType.Text);
                            writer.Write(bname ?? "", NpgsqlDbType.Text);
                            writer.Write(unit ?? "", NpgsqlDbType.Text);
                            writer.Write(company ?? "", NpgsqlDbType.Text);
                            writer.Write(isGeneric, NpgsqlDbType.Boolean);
                            writer.Write(origin ?? "", NpgsqlDbType.Text);
                            writer.Write(noOriginGeneric, NpgsqlDbType.Boolean);
                            if (minPrice.HasValue)
                                writer.Write(minPrice.Value, NpgsqlDbType.Numeric);
                            else
                                writer.Write(DBNull.Value, NpgsqlDbType.Numeric);
                            writer.Write(nowUns, NpgsqlDbType.Timestamp);

                            if (++i % 5000 == 0)
                                await AddLogAsync($"…MEDIS {i} 件COPY中");
                        }

                        writer.Complete();
                    }
                }

                // ★ バージョン更新（id=2）
                using (var delVer = pgConn.CreateCommand())
                {
                    delVer.Transaction = tx;
                    delVer.CommandText = "DELETE FROM drug_code_map_version WHERE id = 2";
                    await delVer.ExecuteNonQueryAsync();
                }
                using (var insVer = pgConn.CreateCommand())
                {
                    insVer.Transaction = tx;
                    insVer.CommandText = "INSERT INTO drug_code_map_version (id, source_version) VALUES (2, @v)";
                    insVer.Parameters.Add(new NpgsqlParameter("@v", medisVersion));
                    await insVer.ExecuteNonQueryAsync();
                }

                // 4) Commit
                tx.Commit();
            }

        }

        private async Task BulkLoadPostgresFromContraindicationAsync(Npgsql.NpgsqlConnection pgConn, string koroPath, DateTime koroVersion)
        {
            var nowUns = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

            using (var tx = pgConn.BeginTransaction())
            {
                // トランザクション中は同期処理で高速化
                using (var set = pgConn.CreateCommand())
                {
                    set.Transaction = tx;
                    set.CommandText = "SET LOCAL synchronous_commit TO OFF";
                    set.ExecuteNonQuery();
                }

                // 古いテーブルを消して再作成
                using (var drop = pgConn.CreateCommand())
                {
                    drop.Transaction = tx;
                    drop.CommandText = @"
                        DROP TABLE IF EXISTS drug_contraindication;
                        CREATE TABLE drug_contraindication (
                            self_code       VARCHAR,
                            self_name       TEXT,
                            self_generic    TEXT,
                            target_code     VARCHAR,
                            target_name     TEXT,
                            target_generic  TEXT,
                            symptom_action  TEXT,
                            mechanism       TEXT,
                            updated_at      TIMESTAMP WITHOUT TIME ZONE
                        );";
                    drop.ExecuteNonQuery();
                }

                // Access (MDB) 側に接続
                string koroConnStr = $"Provider={CommonFunctions.DBProvider};Data Source={koroPath};Mode=Read;";
                using (var koro = new System.Data.OleDb.OleDbConnection(koroConnStr))
                {
                    await koro.OpenAsync();

                    const string sql = @"
                        SELECT
                            自薬剤コード,
                            自薬剤名,
                            自薬剤一般名,
                            対象薬剤コード,
                            対象薬剤名,
                            対象薬剤一般名,
                            [症状・処置],
                            [症状処置機序]
                        FROM T_厚生禁忌";

                    using (var kcmd = new System.Data.OleDb.OleDbCommand(sql, koro))
                    using (var r = await kcmd.ExecuteReaderAsync())
                    using (var writer = pgConn.BeginBinaryImport(@"
                        COPY drug_contraindication
                        (self_code, self_name, self_generic, target_code, target_name, target_generic, symptom_action, mechanism, updated_at)
                        FROM STDIN (FORMAT BINARY)
                    "))
                    {
                        int i = 0;
                        while (await r.ReadAsync())
                        {
                            writer.StartRow();
                            writer.Write(r["自薬剤コード"]?.ToString() ?? "", NpgsqlTypes.NpgsqlDbType.Varchar);
                            writer.Write(r["自薬剤名"]?.ToString() ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                            writer.Write(r["自薬剤一般名"]?.ToString() ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                            writer.Write(r["対象薬剤コード"]?.ToString() ?? "", NpgsqlTypes.NpgsqlDbType.Varchar);
                            writer.Write(r["対象薬剤名"]?.ToString() ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                            writer.Write(r["対象薬剤一般名"]?.ToString() ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                            writer.Write(r["症状・処置"]?.ToString() ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                            writer.Write(r["症状処置機序"]?.ToString() ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                            writer.Write(nowUns, NpgsqlTypes.NpgsqlDbType.Timestamp);

                            if (++i % 5000 == 0)
                                await AddLogAsync($"…{i} 件COPY中 (T_厚生禁忌)");
                        }

                        writer.Complete();
                    }
                }

                // バージョン管理に登録（id=3を禁忌テーブル用とする）
                using (var delVer = pgConn.CreateCommand())
                {
                    delVer.Transaction = tx;
                    delVer.CommandText = "DELETE FROM drug_code_map_version WHERE id = 3";
                    await delVer.ExecuteNonQueryAsync();
                }
                using (var insVer = pgConn.CreateCommand())
                {
                    insVer.Transaction = tx;
                    insVer.CommandText = "INSERT INTO drug_code_map_version (id, source_version) VALUES (3, @v)";
                    insVer.Parameters.Add(new Npgsql.NpgsqlParameter("@v", koroVersion));
                    await insVer.ExecuteNonQueryAsync();
                }

                tx.Commit();
            }

            await AddLogAsync("T_厚生禁忌 → drug_contraindication を更新しました。");
        }

        private async Task RebuildIndexesAsync(IDbConnection conn)
        {
            if (conn is Npgsql.NpgsqlConnection)
            {
                // ------------------------------
                // PostgreSQL
                // ------------------------------

                // drug_code_map
                try { await ExecNonQueryAsync(conn, null, "DROP INDEX IF EXISTS idx_drug_code_map_yj"); } catch { }
                try { await ExecNonQueryAsync(conn, null, "DROP INDEX IF EXISTS idx_drug_code_map_yj7"); } catch { }
                try { await ExecNonQueryAsync(conn, null, "CREATE INDEX IF NOT EXISTS idx_drug_code_map_yj  ON public.drug_code_map(yj_code)"); } catch { }
                try { await ExecNonQueryAsync(conn, null, "CREATE INDEX IF NOT EXISTS idx_drug_code_map_yj7 ON public.drug_code_map(yj7)"); } catch { }

                // drug_medis_generic
                try { await ExecNonQueryAsync(conn, null, "DROP INDEX IF EXISTS idx_medis_yj_code"); } catch { }
                try { await ExecNonQueryAsync(conn, null, "DROP INDEX IF EXISTS idx_medis_brand_name"); } catch { }
                try { await ExecNonQueryAsync(conn, null, "DROP INDEX IF EXISTS idx_medis_generic_name"); } catch { }
                try { await ExecNonQueryAsync(conn, null, "CREATE INDEX IF NOT EXISTS idx_medis_yj_code       ON public.drug_medis_generic(yj_code)"); } catch { }
                try { await ExecNonQueryAsync(conn, null, "CREATE INDEX IF NOT EXISTS idx_medis_brand_name    ON public.drug_medis_generic(brand_name)"); } catch { }
                try { await ExecNonQueryAsync(conn, null, "CREATE INDEX IF NOT EXISTS idx_medis_generic_name  ON public.drug_medis_generic(generic_name)"); } catch { }

                // drug_contraindication
                try { await ExecNonQueryAsync(conn, null, "DROP INDEX IF EXISTS idx_contra_self_code"); } catch { }
                try { await ExecNonQueryAsync(conn, null, "DROP INDEX IF EXISTS idx_contra_target_code"); } catch { }
                try { await ExecNonQueryAsync(conn, null, "DROP INDEX IF EXISTS idx_contra_pair"); } catch { }
                try { await ExecNonQueryAsync(conn, null, "CREATE INDEX IF NOT EXISTS idx_contra_self_code   ON public.drug_contraindication(self_code)"); } catch { }
                try { await ExecNonQueryAsync(conn, null, "CREATE INDEX IF NOT EXISTS idx_contra_target_code ON public.drug_contraindication(target_code)"); } catch { }
                try { await ExecNonQueryAsync(conn, null, "CREATE INDEX IF NOT EXISTS idx_contra_pair        ON public.drug_contraindication(self_code, target_code)"); } catch { }

                // 統計更新（クエリ計画の最適化）
                try { await ExecNonQueryAsync(conn, null, "ANALYZE public.drug_code_map"); } catch { }
                try { await ExecNonQueryAsync(conn, null, "ANALYZE public.drug_medis_generic"); } catch { }
                try { await ExecNonQueryAsync(conn, null, "ANALYZE public.drug_contraindication"); } catch { }
            }
            else if (conn is System.Data.OleDb.OleDbConnection)
            {
                // ------------------------------
                // Access（運用上は参照用が多い想定）
                // ------------------------------

                // drug_code_map
                try { await ExecNonQueryAsync(conn, null, "DROP INDEX idx_drug_code_map_yj ON drug_code_map"); } catch { }
                try { await ExecNonQueryAsync(conn, null, "DROP INDEX idx_drug_code_map_yj7 ON drug_code_map"); } catch { }
                try { await ExecNonQueryAsync(conn, null, "CREATE INDEX idx_drug_code_map_yj  ON drug_code_map(yj_code)"); } catch { }
                try { await ExecNonQueryAsync(conn, null, "CREATE INDEX idx_drug_code_map_yj7 ON drug_code_map(yj7)"); } catch { }

                // drug_medis_generic
                try { await ExecNonQueryAsync(conn, null, "DROP INDEX idx_medis_yj_code ON drug_medis_generic"); } catch { }
                try { await ExecNonQueryAsync(conn, null, "DROP INDEX idx_medis_brand_name ON drug_medis_generic"); } catch { }
                try { await ExecNonQueryAsync(conn, null, "DROP INDEX idx_medis_generic_name ON drug_medis_generic"); } catch { }
                try { await ExecNonQueryAsync(conn, null, "CREATE INDEX idx_medis_yj_code      ON drug_medis_generic(yj_code)"); } catch { }
                try { await ExecNonQueryAsync(conn, null, "CREATE INDEX idx_medis_brand_name   ON drug_medis_generic(brand_name)"); } catch { }
                try { await ExecNonQueryAsync(conn, null, "CREATE INDEX idx_medis_generic_name ON drug_medis_generic(generic_name)"); } catch { }

                // drug_contraindication
                try { await ExecNonQueryAsync(conn, null, "DROP INDEX idx_contra_self_code ON drug_contraindication"); } catch { }
                try { await ExecNonQueryAsync(conn, null, "DROP INDEX idx_contra_target_code ON drug_contraindication"); } catch { }
                try { await ExecNonQueryAsync(conn, null, "DROP INDEX idx_contra_pair ON drug_contraindication"); } catch { }
                try { await ExecNonQueryAsync(conn, null, "CREATE INDEX idx_contra_self_code   ON drug_contraindication(self_code)"); } catch { }
                try { await ExecNonQueryAsync(conn, null, "CREATE INDEX idx_contra_target_code ON drug_contraindication(target_code)"); } catch { }
                try { await ExecNonQueryAsync(conn, null, "CREATE INDEX idx_contra_pair        ON drug_contraindication(self_code, target_code)"); } catch { }
            }
        }

        // ===== 下位ユーティリティ =====

        // KORO: 最新の更新日（先頭行）を取得
        private async Task<DateTime?> GetKoroLatestVersionAsync(string koroPath)
        {
            string connStr = $"Provider={CommonFunctions.DBProvider};Data Source={koroPath};Mode=Read;";
            const string sql = "SELECT TOP 1 更新日 AS Ver FROM T_更新日 ORDER BY 更新日 DESC";

            using (var cn = new OleDbConnection(connStr))
            {
                await cn.OpenAsync();
                using (var cmd = new OleDbCommand(sql, cn))
                using (var r = await cmd.ExecuteReaderAsync())
                {
                    if (await r.ReadAsync())
                    {
                        if (DateTime.TryParse(r["Ver"]?.ToString(), out var dt))
                            return dt;
                    }
                }
            }
            return null;
        }

        // KORO: マッピング全件（医薬品コード→薬価基準コード）
        private async Task<Dictionary<string, string>> ReadKoroMapAsync(string koroPath)
        {
            var map = new Dictionary<string, string>();
            string connStr = $"Provider={CommonFunctions.DBProvider};Data Source={koroPath};Mode=Read;";
            const string sql = @"
                SELECT 医薬品コード AS ReceptCode, 薬価基準コード AS MedisCode
                FROM TG医薬品マスター
                WHERE 薬価基準コード IS NOT NULL
            ";

            using (var cn = new OleDbConnection(connStr))
            {
                await cn.OpenAsync();
                using (var cmd = new OleDbCommand(sql, cn))
                using (var r = await cmd.ExecuteReaderAsync())
                {
                    while (await r.ReadAsync())
                    {
                        string rc = r["ReceptCode"]?.ToString();
                        string yj = r["MedisCode"]?.ToString();
                        if (!string.IsNullOrEmpty(rc) && !map.ContainsKey(rc))
                            map[rc] = yj ?? "";
                    }
                }
            }
            return map;
        }

        // ターゲットDB：必要テーブル・インデックスを用意
        private async Task EnsureTablesAsync(IDbConnection conn)
        {
            // drug_code_map
            try
            {
                await ExecNonQueryAsync(conn, null,
                    "CREATE TABLE IF NOT EXISTS drug_code_map (" +
                    "drugc VARCHAR(64) PRIMARY KEY, " +
                    "yj_code VARCHAR(32) NOT NULL, " +
                    "yj7 VARCHAR(7), " +
                    "drugn TEXT, " +
                    "updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP)"
                );
            }
            catch { /* AccessではIF NOT EXISTS不可のため無視 */ }

            // yj_code / yj7 インデックス
            try { await ExecNonQueryAsync(conn, null, "CREATE INDEX IF NOT EXISTS idx_drug_code_map_yj ON drug_code_map(yj_code)"); } catch { }
            try { await ExecNonQueryAsync(conn, null, "CREATE INDEX IF NOT EXISTS idx_drug_code_map_yj7 ON drug_code_map(yj7)"); } catch { }

            // Access では TEXT 型や DATETIME 型で作られる想定。既存環境に合わせてください。

            // drug_code_map_version
            try
            {
                await ExecNonQueryAsync(conn, null,
                    "CREATE TABLE IF NOT EXISTS drug_code_map_version (" +
                    "id INTEGER PRIMARY KEY, " +
                    "source_version TIMESTAMP NOT NULL)"
                );
            }
            catch { }
        }

        // 既存バージョン取得
        private async Task<DateTime?> GetCurrentVersionAsync(IDbConnection conn, int koroTableId = 1)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT source_version FROM drug_code_map_version WHERE id = @id";
                CommonFunctions.AddDbParameter(cmd, "@id", koroTableId);
                var obj = await ExecuteScalarAsync(cmd);
                if (obj != null && DateTime.TryParse(obj.ToString(), out var dt)) return dt;
                return null;
            }
        }

        // DB→Dictionary 更新（drugc→yj_code を全読込）
        private async Task RefreshDictionaryFromDbAsync(IDbConnection conn)
        {
            string sql = "SELECT drugc, yj_code FROM drug_code_map";
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = CommonFunctions.ConvertSqlForOleDb(sql);
                using (var r = await ExecuteReaderAsync(cmd))
                {
                    var temp = new Dictionary<string, string>();
                    while (await r.ReadAsync())
                    {
                        string rc = r["drugc"]?.ToString();
                        string yj = r["yj_code"]?.ToString();
                        if (!string.IsNullOrEmpty(rc) && !temp.ContainsKey(rc))
                            temp[rc] = yj ?? "";
                    }
                    // 置換（エラー時の巻き込みを避けるため一時Dictを使用）
                    CommonFunctions.ReceptToMedisCodeMap.Clear();
                    foreach (var kv in temp) CommonFunctions.ReceptToMedisCodeMap[kv.Key] = kv.Value;
                }
            }
        }

        // --- 共通 I/O ラッパ（IDb* を非同期で安全に使うための薄いヘルパ） ---

        private static async Task OpenAsync(IDbConnection conn)
        {
            if (conn is System.Data.Common.DbConnection dbc) await dbc.OpenAsync();
            else conn.Open();
        }

        private static async Task ExecNonQueryAsync(IDbConnection conn, IDbTransaction tx, string sql)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = CommonFunctions.ConvertSqlForOleDb(sql);
                await ExecuteNonQueryAsync(cmd);
            }
        }

        private void toolStripButtonPMDA_Click(object sender, EventArgs e)
        {
            if(Properties.Settings.Default.DBtype != "pg")
            {
                MessageBox.Show("PostgreSQLバージョンでのみ利用可能です");
                return;

            }
            else if (!CommonFunctions._readySGML)
            {
                MessageBox.Show("添付文書データがまだロードされてないので起動できません。少し時間をおいてから再度試してみてください。");
                return;
            }

            var dlg = new FormSGML_DI(null);
            // 前回の位置とサイズを復元
            if (Properties.Settings.Default.PMDABounds != Rectangle.Empty)
            {
                dlg.StartPosition = FormStartPosition.Manual;
                dlg.Bounds = Properties.Settings.Default.PMDABounds;

                // マージンと境界線を設定
                dlg.Padding = new Padding(0);
                dlg.Margin = new Padding(0);
                //form3Instance.FormBorderStyle = FormBorderStyle.None;
            }

            // TopMost状態を設定
            //formSGML_DI.TopMost = Properties.Settings.Default.ViewerTopmost;

            // Form3が閉じるときに位置、サイズ、TopMost状態を保存
            dlg.FormClosing += (s, args) =>
            {
                SaveViewerSettings(dlg, "PMDABounds");
            };
            dlg.Show(this);
        }

        private static async Task<int> ExecuteNonQueryAsync(IDbCommand cmd)
        {
            if (cmd is System.Data.Common.DbCommand dbc) return await dbc.ExecuteNonQueryAsync();
            return cmd.ExecuteNonQuery();
        }

        private static async Task<object> ExecuteScalarAsync(IDbCommand cmd)
        {
            if (cmd is System.Data.Common.DbCommand dbc) return await dbc.ExecuteScalarAsync();
            return cmd.ExecuteScalar();
        }

        private static async Task<System.Data.Common.DbDataReader> ExecuteReaderAsync(IDbCommand cmd)
        {
            if (cmd is System.Data.Common.DbCommand dbc) return await dbc.ExecuteReaderAsync();
            // OleDbCommand も DbCommand 継承なので通常ここは通りません
            throw new NotSupportedException("ExecuteReaderAsync is only supported for DbCommand.");
        }


        private void loadConnectionString()
        {
            CommonFunctions.connectionOQSdata     = $"Provider={CommonFunctions.DBProvider};Data Source={Properties.Settings.Default.OQSDrugData};";
            CommonFunctions.connectionReadOQSdata = $"Provider={CommonFunctions.DBProvider};Data Source={Properties.Settings.Default.OQSDrugData};Mode={DataReadMode};";
        }

        // PGDump
        private void StartDumpTimer(TimeSpan? checkEvery = null)
        {
            // 起動直後に一度チェック、その後30分ごとに確認
            _dumpTimer = new System.Threading.Timer(async _ =>
            {
                try
                {
                    if ((okSettings & 0b1) != 0)
                    {
                        // UIを直接触らないログ関数（UIへ出すなら Invoke 必須）
                        Action<string> log = AddLogSafe;

                        await CommonFunctions.TryRunScheduledDumpAsync(
                            force: false,
                            log: log,
                            ct: CancellationToken.None);
                    }
                }
                catch (Exception ex)
                {
                    AddLogSafe("[Backup] タイマー例外: " + ex.Message);
                }
            }, null, TimeSpan.Zero, TimeSpan.FromMinutes(30));
        }

        private void StopDumpTimer()
        {
            if (_dumpTimer != null)
            {
                _dumpTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _dumpTimer.Dispose();
                _dumpTimer = null;
            }
        }

        private void AddLogSafe(string msg)
        {
            if (IsDisposed) return;

            if (InvokeRequired)
            {
                // UIスレッドにマーシャリングしてから async 実行
                BeginInvoke(new Action(async () =>
                {
                    try
                    {
                        await AddLogAsync(msg);   // UIスレッド上でawait（UI更新も安全）
                    }
                    catch (Exception ex)
                    {
                        // ここで握りつぶす／ログに落とす
                        System.Diagnostics.Debug.WriteLine("[AddLogSafe] " + ex);
                    }
                }));
            }
            else
            {
                // 既にUIスレッド。awaitしないなら例外を拾う
                var _ = AddLogAsync(msg).ContinueWith(t =>
                {
                    System.Diagnostics.Debug.WriteLine("[AddLogSafe] " + t.Exception);
                }, TaskContinuationOptions.OnlyOnFaulted);
            }
        }

    }
}


