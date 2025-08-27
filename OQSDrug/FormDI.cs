using Microsoft.VisualBasic;
using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using static OQSDrug.CommonFunctions;
using static OQSDrug.FormTKK;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;

namespace OQSDrug
{
    public partial class FormDI : Form
    {
        private const int SnapDistance = 16; // 吸着の距離（ピクセル）
        private int SnapCompPixel = 8;  //余白補正

        private Form1 _parentForm;
        public string provider;

        private Color[] RowColors = { Color.WhiteSmoke, Color.White };
        private int[] fixedColumnWidth = {120, 268, 60 };

        private List<(long PtID, string PtName)> ptData = new List<(long PtID, string PtName)>();

        private DataTable DrugHistoryData = new DataTable();
        
        private int ShowSpan = Properties.Settings.Default.ViewerSpan;
        private FormSearch formSearch = null;

        // 同期中フラグ（必要最小限）
        private bool _syncingSelect = false;

        // 薬歴用
        private BindingSource bsHistory;

        // 相互作用
        private DataTable dtInteractions;
        private BindingSource bsInteractions;

        // キャッシュ（同じ yj_code で無駄に再読込しない用）
        private long? _lastPtIdForInteraction = null;

        // 優先表示しきい値（必要なら調整）
        private const double CHRONIC_PRIORITY_MIN = 0.55;
        private const int CHRONIC_PRIORITY_DAYS = 90;

        private const double ACUTE_PRIORITY_MIN = 0.60;
        private const int ACUTE_PRIORITY_DAYS = 30;

        public FormDI(Form1 parentForm)
        {
            InitializeComponent();

            _parentForm = parentForm;
            provider = CommonFunctions.DBProvider;
                        
            toolStrip1.Renderer = new CustomToolStripRenderer(); // カスタム描画を適用
        }

        public async Task LoadDataIntoComboBoxes()
        {
            if (!await CommonFunctions.WaitForDbUnlock(2000))
            {
                MessageBox.Show("データベースがロックされており、LoadDataIntoComboBoxes に失敗しました。もう一度やり直してみてください。");
                return;
            }

            string sql = @"
                select ptidmain, ptname, maxid
                from (
                    select ptidmain, ptname, max(id) as maxid
                    from drug_history
                    group by ptidmain, ptname
                ) as sub
                order by maxid desc;";

            sql = CommonFunctions.ConvertSqlForOleDb(sql);

            var localList = new List<(long PtID, string PtName)>();

            using (IDbConnection connection = CommonFunctions.GetDbConnection(true))
            {
                try
                {
                    await ((DbConnection)connection).OpenAsync();

                    CommonFunctions.DataDbLock = true;

                    using (IDbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = sql;

                        using (var reader = await ((DbCommand)command).ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                long ptIDMain = reader["ptidmain"] != DBNull.Value ? Convert.ToInt64(reader["ptidmain"]) : 0;
                                string ptName = reader["ptname"] != DBNull.Value ? Convert.ToString(reader["ptname"]) : "";

                                // 表示用の文字列
                                string displayName = $"{ptIDMain.ToString().PadLeft(6, ' ')} : {ptName}";

                                localList.Add((ptIDMain, displayName));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("データの取得中にエラーが発生しました: " + ex.Message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                finally
                {
                    CommonFunctions.DataDbLock = false;
                }
            }

            // UI 更新
            try
            {
                ptData = localList;

                toolStrip1.Invoke(new Action(() =>
                {
                    toolStripComboBoxPt.SelectedIndexChanged -= toolStripComboBoxPt_SelectedIndexChanged;

                    toolStripComboBoxPt.Items.Clear();
                    toolStripComboBoxPt.SelectedIndex = -1;

                    foreach (var item in ptData)
                    {
                        toolStripComboBoxPt.Items.Add(new PtItem
                        {
                            PtID = item.PtID,
                            DisplayText = item.PtName // ← 修正: DisplayNameではなくPtName
                        });
                    }

                    toolStripComboBoxPt.SelectedIndexChanged += toolStripComboBoxPt_SelectedIndexChanged;

                    if (_parentForm.autoRSB || _parentForm.forceIdLink)
                    {
                        _parentForm.forceIdLink = false;

                        int index = ptData.FindIndex(p => p.PtID == _parentForm.tempId);
                        toolStripComboBoxPt.SelectedIndex = (index >= 0) ? index : -1;
                    }
                }));
            }
            catch (Exception ex)
            {
                MessageBox.Show("コンボボックスの更新中にエラーが発生しました: " + ex.Message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void FormDI_Load(object sender, EventArgs e)
        {
            SetSpanButtonState(ShowSpan);
            SettoolStripButtonSpanEvent();

            toolStripButtonClass.Checked = Properties.Settings.Default.DrugClass;
            toolStripButtonClass.Enabled = (CommonFunctions.ReceptToMedisCodeMap.Count > 0);

            // Context menu
            InitializeContextMenu();

            // 履歴
            bsHistory = new BindingSource { DataSource = DrugHistoryData };
            dataGridViewDH.DataSource = bsHistory;

            // 相互作用
            dtInteractions = new DataTable();
            bsInteractions = new BindingSource { DataSource = dtInteractions };

            dataGridViewInteraction.AutoGenerateColumns = false;
            dataGridViewInteraction.DataSource = bsInteractions;
            
            
            // タブ：mdbモードでは 相互作用機能はサポートしない
            if (Properties.Settings.Default.DBtype != "pg")
            {
                tabControl1.Appearance = TabAppearance.Buttons;
                tabControl1.SizeMode = TabSizeMode.Fixed;
                tabControl1.ItemSize = new Size(0, 1);          // 高さほぼゼロ
                tabControl1.Padding = new Point(0, 0);

                tabControl1.SelectedIndexChanged -= TabControl1_SelectedIndexChanged;
            }
            else
            {
                tabControl1.Appearance = TabAppearance.Normal;
                tabControl1.SizeMode = TabSizeMode.Normal;
                tabControl1.ItemSize = new Size(54, 22); // いったんリセット気味
                tabControl1.Padding = new Point(6, 3);
                // タブ切替で相互作用を更新
                tabControl1.SelectedIndexChanged += TabControl1_SelectedIndexChanged;
                InitializeInteractionContextMenu();
            }
           
            await LoadDataIntoComboBoxes();

            toolStripButtonSum.Checked = Properties.Settings.Default.Sum;

            toolStripButtonOmitMyOrg.Checked = Properties.Settings.Default.OmitMyOrg;

            // マウスホイールスクロールを補完
            dataGridViewFixed.MouseWheel += DataGridViewFixed_MouseWheel;

            //外部スクロールバー設定
            spaceLeft.Height = hScrollBar1.Height;

            // スクロールバー初期設定＆イベント配線
            WireExternalScrollbars();

            // 初期の範囲計算
            RecalcScrollbars();

        }

        private async void toolStripComboBoxPt_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (toolStripComboBoxPt.SelectedItem is PtItem selectedPt)
                {
                    // 選択された PtID を取得
                    long ptID = selectedPt.PtID;

                    //tempIDを設定
                    _parentForm.tempId = ptID;

                    // sender が toolstripButton かどうかを判定
                    if (!(sender is ToolStripButton stripButton))
                    {
                        //コンボボックスからの場合表示期間をリセットする
                        ShowSpan = Properties.Settings.Default.ViewerSpan;
                        RemovetoolStripButtonSpanEvent();
                        SetSpanButtonState(ShowSpan);
                        SettoolStripButtonSpanEvent();

                        //tabリセット
                        tabControl1.SelectedIndex = 0;
                    } 
                    else if (tabControl1.SelectedTab == tabPageInteraction) // 相互作用表示時に期間や自施設除外を操作
                    {
                        TabControl1_SelectedIndexChanged(sender, EventArgs.Empty);
                    }


                    // DataGridView に表示するデータを取得
                    await ShowDrugData(ptID);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"患者ID選択時にエラーが発生しました:{ex.Message}");
            }
        }

        private async Task ShowDrugData(long PtID)
        {
            if (!await CommonFunctions.WaitForDbUnlock(2000))
            {
                MessageBox.Show("データベースがロックされており、ShowDrugDataに失敗しました。もう一度やり直してみてください。");
                return;
            }

            // Sum 設定によって pivotField を切り替える
            string pivotField = Properties.Settings.Default.Sum ? "metrmonth" : "didate";

            string startDate = DateTime.Now.AddMonths(0 - ShowSpan).ToString("yyyyMM");
            bool spanOff = (ShowSpan == 0);
            bool omitOff = !Properties.Settings.Default.OmitMyOrg;

            // 元のTRANSFORMを使わずに縦持ちで取得
            string query = $@"
                SELECT
                    CASE WHEN COALESCE(prlshnm, '') = '' THEN metrdihnm ELSE prlshnm END AS hospital,
                    drugn,
                    drugc,
                    ingren,
                    (CAST(qua1 AS TEXT) || unit || CASE WHEN COALESCE(usagen,'') = '' THEN '' ELSE '/' || usagen END) AS dose,
                    metrmonth,
                    didate,
                    times
                FROM drug_history
                WHERE
                    revised IS NOT TRUE
                    AND (@spanOff = 1 OR metrmonth >= @startMonth)
                    AND (@omitOff = 1 OR prisorg <> 1)
                    AND ptidmain = @ptidmain
                ORDER BY {pivotField} DESC;
            ";

            if (Properties.Settings.Default.DBtype == "mdb")
            {
                query = $@"
                   SELECT
                        IIf(IsNull(prlshnm) OR prlshnm = '', metrdihnm, prlshnm) AS hospital,
                        drugn,
                        drugc,
                        ingren,
                        (CStr(qua1) & unit & IIf(IsNull(usagen) OR usagen = '', '', '/' & usagen)) AS dose,
                        metrmonth,
                        didate,
                        times
                    FROM drug_history
                    WHERE
                        (revised = FALSE OR revised IS NULL)
                        AND (? = 1 OR metrmonth >= ?)
                        AND (? = 1 OR prisorg <> 1)
                        AND ptidmain = ?
                    ORDER BY {pivotField} DESC;";
            }

            try
            {
                using (IDbConnection connection = CommonFunctions.GetDbConnection(true))
                {
                    await ((DbConnection)connection).OpenAsync();

                    using (IDbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = query;

                        // パラメータ追加（共通関数利用）
                        CommonFunctions.AddDbParameter(command, "@spanOff", spanOff ? 1 : 0);
                        CommonFunctions.AddDbParameter(command, "@startMonth", startDate);
                        CommonFunctions.AddDbParameter(command, "@omitOff", omitOff ? 1 : 0);
                        CommonFunctions.AddDbParameter(command, "@ptidmain", PtID);

                        CommonFunctions.DataDbLock = true;

                        DataTable rawTable = new DataTable();
                        using (var reader = await ((DbCommand)command).ExecuteReaderAsync())
                        {
                            rawTable.Load(reader);
                        }

                        CommonFunctions.DataDbLock = false;

                        // ↓ここでC#側でPivot化（metrmonth列を列見出しに変換）
                        DataTable pivoted = CommonFunctions.ConvertPivotDataTable(rawTable,
                            rowFields: new[] { "hospital", "drugn", "dose", "drugc", "ingren" },
                            columnField: pivotField,
                            dataField: "times");

                        // 付加列追加
                        if (!pivoted.Columns.Contains("medisCode"))
                            pivoted.Columns.Add("medisCode", typeof(string));

                        if (!pivoted.Columns.Contains("Color"))
                            pivoted.Columns.Add("Color", typeof(Color));

                        // 色付け・medisCode設定
                        string previousHospital = null;
                        int colorIndex = 0;

                        foreach (DataRow row in pivoted.Rows)
                        {
                            // 病院名の表示制御
                            if (previousHospital == null ||
                                (row["hospital"] != DBNull.Value && previousHospital != row["hospital"].ToString()))
                            {
                                previousHospital = row["hospital"].ToString();
                                colorIndex = (colorIndex + 1) % RowColors.Length;
                            }
                            else
                            {
                                row["hospital"] = "";
                            }

                            // medisCodeマッピング
                            string receptCode = row["drugc"]?.ToString();
                            if (CommonFunctions.ReceptToMedisCodeMap.TryGetValue(receptCode, out string medisCode))
                                row["medisCode"] = medisCode;

                            // 色
                            if (Properties.Settings.Default.DrugClass && row["medisCode"] != DBNull.Value)
                                row["Color"] = getDrugClassColor(row["medisCode"].ToString());
                            else
                                row["Color"] = RowColors[colorIndex];
                        }

                        // --- DataTable を保持 & BindingSource 経由でバインド ---
                        DrugHistoryData = pivoted;

                        if (bsHistory == null)
                            bsHistory = new BindingSource();
                        bsHistory.DataSource = DrugHistoryData;

                        

                        // DataGridViewへ反映
                        Invoke(new Action(() =>
                        {
                            InitializeDataGridView(dataGridViewFixed);
                            InitializeDataGridView(dataGridViewDH);
                            dataGridViewFixed.DataSource = bsHistory;
                            dataGridViewDH.DataSource = bsHistory;
                            ConfigureDataGridView(dataGridViewFixed);
                            ConfigureDataGridView(dataGridViewDH);
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"エラーが発生しました: {ex.Message}");
                CommonFunctions.DataDbLock = false;
            }
        }


        private DataTable SortDataTableByLastDate(DataTable dt)
        {
            var sortedRows = dt.AsEnumerable()
                .GroupBy(row => row["Hospital"].ToString()) // Hospitalごとにグループ化
                .OrderByDescending(g => g.Max(row => row.Field<String>("LastDate"))) // 各Hospitalの最大LastDateで降順ソート
                .SelectMany(g => g.OrderByDescending(row => row.Field<String>("LastDate"))  // 各Hospital内でLastDate降順
                              .ThenBy(row => row.Field<string>("MedisCode"))) // MedisCode 昇順
                .CopyToDataTable(); // DataTable に変換

            return sortedRows;
        }

        private Color getDrugClassColor(string yjCode)
        {
            string digit1 = yjCode.Substring(0, 1);
            Color color;

            switch (digit1)
            {
                case "1": color = Color.FromArgb(208, 232, 242); break; // 淡い青
                case "2": color = Color.FromArgb(214, 245, 214); break; // 淡い緑
                case "3": color = Color.FromArgb(255, 228, 181); break; // 淡いオレンジ
                case "4": color = Color.FromArgb(255, 218, 218); break; // 淡いピンク
                case "5": color = Color.FromArgb(245, 230, 196); break; // 淡い黄土色
                case "6": color = Color.FromArgb(227, 215, 255); break; // 淡い紫
                case "7": color = Color.FromArgb(234, 234, 234); break; // 淡いグレー
                case "8": color = Color.FromArgb(255, 202, 202); break; // 淡い赤
                default: color = Color.White; break; // その他は白
            }
            return color;
        }

        private void InitializeDataGridView(DataGridView dataGridView)
        {
            dataGridView.DataSource = null;
            dataGridView.Rows.Clear();
            dataGridView.Columns.Clear();
            dataGridView.Refresh();

            // レコードセレクタを非表示にする
            dataGridView.RowHeadersVisible = false;

            // カラム幅を自動調整する
            dataGridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);

            // 行の高さを変更できないようにする
            dataGridView.AllowUserToResizeRows = false;

            dataGridView.AllowUserToAddRows = false;

            
        }

        private void ConfigureDataGridView(DataGridView dataGridView)
        {
            //共通
            // レコードセレクタを非表示にする
            dataGridView.RowHeadersVisible = false;

            // ソート機能を無効にする
            //dataGridView.AllowUserToOrderColumns = false;
            //各列のソートモードを無効にする
            foreach (DataGridViewColumn column in dataGridView.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            dataGridView.EditMode = DataGridViewEditMode.EditProgrammatically;
            dataGridView.SelectionMode = DataGridViewSelectionMode.CellSelect;
            
            // 縦方向の罫線を非表示にする
            dataGridView.CellBorderStyle = DataGridViewCellBorderStyle.Raised;
            // 特定の行の背景色を変える（例: 2番目の行の背景色を変更）
            for (int i = 0; i < dataGridView.Rows.Count; i++)
            {
                dataGridView.Rows[i].DefaultCellStyle.BackColor = dataGridView.Rows[i].Cells["Color"].Value as Color? ?? Color.Empty;
            }

            // 表示名（必要な列だけ）
            var headers = new Dictionary<string, string>
            {
                { "hospital", "処方元" },
                { "drugn",  "薬剤名" },
                { "dose",  "用法" },
            };
            foreach (var kv in headers)
                if (dataGridView.Columns.Contains(kv.Key))
                    dataGridView.Columns[kv.Key].HeaderText = kv.Value;
            //個別
            if (dataGridView.Name.Contains("Fixed"))
            {
                //dataGridView.ScrollBars = ScrollBars.Vertical; // スクロールバーを無効化
                                                               // Fixedはスクロールバーを非表示にする
                dataGridView.ScrollBars = ScrollBars.Horizontal;

                for (int i = 0; i < dataGridView.Columns.Count; i++)
                {
                    if (i == 0)
                    {
                        dataGridView.Columns[i].Width = fixedColumnWidth[i];
                        dataGridView.Columns[i].DefaultCellStyle.Font = new Font("Meiryo UI", 8);
                    }
                    else if (i == 1) dataGridView.Columns[i].Width = fixedColumnWidth[i];
                    else if (i == 2)
                    {
                        dataGridView.Columns[i].Frozen = true;
                        dataGridView.Columns[i].Width = fixedColumnWidth[i];
                    } 
                    else if (i > 2) dataGridView.Columns[i].Visible = false;
                }
            }
            else //dataGridViewDH
            {
                dataGridView.Left = dataGridViewFixed.Right;
                dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

                for (int i = 0; i < dataGridView.Columns.Count; i++)
                {
                    if (i < 5)
                    {
                        dataGridView.Columns[i].Visible = false;
                    }
                    else
                    {
                        dataGridView.Columns[i].Width = 60;
                    }
                }
                //dataGridView.Columns["DrugC"].Visible = true;
                dataGridView.Columns["Color"].Visible = false;
                dataGridView.Columns["MedisCode"].Visible = false;

                //AdjustFixedHeight();
            }
           
        }

        private void toolStripButtonClose_Click(object sender, EventArgs e)
        {
            if(this.WindowState == FormWindowState.Maximized || this.WindowState == FormWindowState.Minimized) this.WindowState = FormWindowState.Normal;

            this.Close();
        }

        // 変数値に基づいてラジオボタンの状態を設定
        private void SetSpanButtonState(int value)
        {
            foreach (ToolStripItem item in toolStrip1.Items)
            {
                // 名前が "toolStripButtonSpan" で始まり、ToolStripButtonである場合
                if (item is ToolStripButton button && item.Name.StartsWith("toolStripButtonSpan"))
                {
                    // Tag の値が指定された値と一致する場合のみ Checked を true にする
                    if (int.TryParse(button.Tag.ToString(), out int tagValue))
                    {
                        button.Checked = (tagValue == value);
                    }
                }
            }
        }

        private void toolStripButtonSpan_CheckedChanged(object sender, EventArgs e)
        {
            ToolStripButton button = sender as ToolStripButton;

            if (button != null)
            {
                if (button.Checked)
                {
                    if (int.TryParse(button.Tag.ToString(), out int tagValue))
                    {
                        ShowSpan = tagValue; // Tag から値を取得

                        //他のボタンをOffにする
                        SetSpanButtonState(ShowSpan);

                        toolStripComboBoxPt_SelectedIndexChanged(sender, e); //再描画
                    }
                }
                else
                {
                    //オンのボタンをもう一度押してオフにしてしまった場合、もう一度オンにする
                    RemovetoolStripButtonSpanEvent();

                    SetSpanButtonState(ShowSpan); //ShowSpanは変えない

                    SettoolStripButtonSpanEvent();
                }
            }
        }

        private void SettoolStripButtonSpanEvent()
        {
            //toolStripButtonSpan1M.CheckedChanged += toolStripButtonSpan_CheckedChanged;
            toolStripButtonSpan3.CheckedChanged += toolStripButtonSpan_CheckedChanged;
            toolStripButtonSpan6.CheckedChanged += toolStripButtonSpan_CheckedChanged;
            toolStripButtonSpan12.CheckedChanged += toolStripButtonSpan_CheckedChanged;
            toolStripButtonSpanAll.CheckedChanged += toolStripButtonSpan_CheckedChanged;
        }

        private void RemovetoolStripButtonSpanEvent()
        {
            //toolStripButtonSpan1M.CheckedChanged -= toolStripButtonSpan_CheckedChanged;
            toolStripButtonSpan3.CheckedChanged -= toolStripButtonSpan_CheckedChanged;
            toolStripButtonSpan6.CheckedChanged -= toolStripButtonSpan_CheckedChanged;
            toolStripButtonSpan12.CheckedChanged -= toolStripButtonSpan_CheckedChanged;
            toolStripButtonSpanAll.CheckedChanged -= toolStripButtonSpan_CheckedChanged;
        }

        private void InitializeContextMenu()
        {
            // ContextMenuStripの作成
            ContextMenuStrip contextMenuStrip = new ContextMenuStrip();

            // 「全角でコピー」メニューアイテムを作成
            ToolStripMenuItem copyFullMenuItem = new ToolStripMenuItem("表示のままコピー");
            copyFullMenuItem.Tag = "full"; // 引数として識別するためのタグを設定
            copyFullMenuItem.Image = Properties.Resources.Zen;
            copyFullMenuItem.Click += CopyMenuItem_Click;

            // 「半角でコピー」メニューアイテムを作成
            ToolStripMenuItem copyHalfMenuItem = new ToolStripMenuItem("半角でコピー");
            copyHalfMenuItem.Tag = "half"; // 引数として識別するためのタグを設定
            copyHalfMenuItem.Image = Properties.Resources.Han;
            copyHalfMenuItem.Click += CopyMenuItem_Click;

            // 「薬情検索」メニューアイテムを作成（初期では追加しない）
            ToolStripMenuItem searchMedicineMenuItem = new ToolStripMenuItem("薬情検索");
            searchMedicineMenuItem.Image = Properties.Resources.Find;
            searchMedicineMenuItem.Click += SearchMedicineMenuItem_Click;

            // メニューアイテムを追加
            contextMenuStrip.Items.Add(copyFullMenuItem);
            contextMenuStrip.Items.Add(copyHalfMenuItem);
            if (!string.IsNullOrEmpty(_parentForm.RSBdrive))
            {
                contextMenuStrip.Items.Add(searchMedicineMenuItem);
            }
            // DataGridViewに右クリックメニューを設定
            dataGridViewFixed.ContextMenuStrip = contextMenuStrip;
        }

        private async void CopyMenuItem_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem menuItem)
            {
                // Tagから動作を判別
                string mode = menuItem.Tag as string;

                if (dataGridViewFixed.SelectedCells.Count > 0)
                {
                    // 選択されたセルを行・列のインデックスでソート
                    var sortedCells = dataGridViewFixed.SelectedCells
                        .Cast<DataGridViewCell>()
                        .OrderBy(cell => cell.RowIndex)
                        .ThenBy(cell => cell.ColumnIndex)
                        .ToList();

                    List<string> cellValues = new List<string>();
                    foreach (DataGridViewCell selectedCell in sortedCells)
                    {
                        cellValues.Add(selectedCell.Value?.ToString() ?? string.Empty);
                    }

                    string clipboardText = string.Join(",", cellValues);
                    clipboardText = RemoveCampany(clipboardText);

                    if (mode == "half")
                    {
                        // 半角変換
                        clipboardText = Strings.StrConv(clipboardText, VbStrConv.Narrow, 0x0411);
                    }

                    // リトライを使ってクリップボードにコピー
                    bool success = (clipboardText.Length > 0) ? await CommonFunctions.RetryClipboardSetTextAsync(clipboardText) : false;

                    if (!success)
                    {
                        MessageBox.Show("クリップボードへのコピーに失敗しました。もう一度トライしてみてください。");
                    }
                }
            }
        }

        // 「薬情検索」メニューのクリック時処理
        private async void SearchMedicineMenuItem_Click(object sender, EventArgs e)
        {
            var dgv = GetInvokedGrid(sender);
            if (dgv == null)
            {
                MessageBox.Show("検索対象のグリッドが取得できませんでした。");
                return;
            }
            if (dgv.SelectedCells.Count == 0 && dgv.CurrentCell == null)
            {
                MessageBox.Show("セルを選択してください。");
                return;
            }

            var cell = (dgv.SelectedCells.Count > 0)
                ? dgv.SelectedCells[dgv.SelectedCells.Count - 1]
                : dgv.CurrentCell;
            int row = cell.RowIndex;

            // ★ ここで分岐：Interactionグリッドならセル文字列だけで検索
            bool isInteraction = (dgv == dataGridViewInteraction) ||
                                 (dgv.Name != null && dgv.Name.Equals("dataGridViewInteraction", StringComparison.OrdinalIgnoreCase));

            string drugName, ingreN, yjCode;

            if (isInteraction)
            {
                drugName = (cell.Value == null) ? "" : cell.Value.ToString().Trim();
                if (string.IsNullOrEmpty(drugName) && dgv.Columns.Contains("drugn"))
                    drugName = Convert.ToString(dgv.Rows[row].Cells["drugn"].Value) ?? "";

                ingreN = "";
                yjCode = "";
            }
            else
            {
                // ← 従来どおり dataGridViewFixed 用の取り方
                drugName = Convert.ToString(cell.Value) ?? "";
                if (dgv.Columns.Contains("IngreN"))
                    ingreN = Convert.ToString(dgv.Rows[row].Cells["IngreN"].Value) ?? "";
                else if (dgv.Columns.Contains("ingren"))
                    ingreN = Convert.ToString(dgv.Rows[row].Cells["ingren"].Value) ?? "";
                else ingreN = "";

                if (dgv.Columns.Contains("MedisCode"))
                    yjCode = Convert.ToString(dgv.Rows[row].Cells["MedisCode"].Value) ?? "";
                else if (dgv.Columns.Contains("yj_code"))
                    yjCode = Convert.ToString(dgv.Rows[row].Cells["yj_code"].Value) ?? "";
                else yjCode = "";

                if (string.IsNullOrWhiteSpace(drugName) && !string.IsNullOrWhiteSpace(ingreN))
                    drugName = ingreN;
            }

            if (string.IsNullOrWhiteSpace(drugName))
            { MessageBox.Show("検索キーワードが取得できませんでした。"); return; }

            try
            {
                // ★ Interaction の場合も同じFuzzySearchを使い、不要パラメータは空で渡す
                var topResults = await FuzzySearchAsync(drugName, ingreN, yjCode, CommonFunctions.RSBDI, 0.2);
                if (topResults != null && topResults.Count > 0)
                {
                    if (formSearch == null || formSearch.IsDisposed)
                    { formSearch = new FormSearch(this); formSearch.Show(this); }
                    formSearch.SetDrugLists(topResults);
                }
                else
                {
                    MessageBox.Show("RSB薬情に該当薬剤が見つかりませんでした。");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("薬情表示時にエラーが発生しました。" + ex.Message);
            }
        }

        private DataGridView GetInvokedGrid(object sender)
        {
            // 右クリックメニューから来た場合は SourceControl を優先
            var cms = (sender as ToolStripItem)?.Owner as ContextMenuStrip;
            if (cms != null && cms.SourceControl is DataGridView g1) return g1;

            // フォールバック：フォーカス中の既知グリッド
            if (dataGridViewInteraction != null && dataGridViewInteraction.Focused) return dataGridViewInteraction;
            if (dataGridViewFixed != null && dataGridViewFixed.Focused) return dataGridViewFixed;

            // それでもダメなら null
            return null;
        }

        private string FirstExistingColumn(DataGridView dgv, params string[] candidates)
        {
            foreach (var name in candidates)
            {
                // 列の Name / DataPropertyName の両方を大小無視で照合
                foreach (DataGridViewColumn c in dgv.Columns)
                {
                    if (string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(c.DataPropertyName, name, StringComparison.OrdinalIgnoreCase))
                        return c.Name; // 実列名を返す
                }
            }
            return null;
        }

        private string GetCellString(DataGridView dgv, int rowIndex, string colName)
        {
            if (string.IsNullOrEmpty(colName)) return null;
            if (!dgv.Columns.Contains(colName)) return null;
            var v = dgv.Rows[rowIndex].Cells[colName].Value;
            return v == null ? null : v.ToString();
        }
                      
        // リストRSBDIのカラム1,2を対象にあいまい検索を行い、上位10件を返すメソッド
        public async Task<List<Tuple<string[], double>>> FuzzySearchAsync(string drugName, string ingreN, string YJcode, List<string[]> DI, double cutoffThreshold = 0.4, double bonusForOriginator = 0.4, double penaltyForMissingIngreN = 0.5)
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

            string yjPrefix = YJcode.Length >= 9 ? YJcode.Substring(0, 9) : YJcode;

            var exactMatchList = new List<Tuple<string[], double>>();
            var prefixMatchList = new List<Tuple<string[], double>>();
            var fuzzyMatchTasks = new List<Task<Tuple<string[], double>>>();

            foreach (var record in DI)
            {
                string column1 = record[0];  // 1列目（薬品名）
                string column2 = record[1];  // 2列目（成分名）
                string column3 = record[2];  // 3列目（YJコード）
                string column4 = record[3];  // 4列目（"先発" の確認）

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
                    else if (ingreN == column1)
                    {
                        similarity = 0.9;
                        exact = true;
                    }

                    if (similarity > cutoffThreshold && column4 == "先発")
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

        private string RemoveCampany(string drugName)
        {
            // 正規表現で「」や【】に囲まれた部分を削除
            string pattern = @"[「【（][^」】）]*[」】）]";
            string processedDrugName = Regex.Replace(drugName, pattern, "");

            return processedDrugName;
        }

        private HashSet<string> GenerateNGrams(string input, int n)
        {
            var ngrams = new HashSet<string>();
            if (input.Length < n) return ngrams;

            for (int i = 0; i <= input.Length - n; i++)
            {
                ngrams.Add(input.Substring(i, n));
            }

            return ngrams;
        }

        private double CalculateNGramSimilarity(string source, string target, int n = 2)
        {
            var sourceNGrams = GenerateNGrams(source, n);
            var targetNGrams = GenerateNGrams(target, n);

            if (sourceNGrams.Count == 0 || targetNGrams.Count == 0) return 0.0;

            int intersectionCount = sourceNGrams.Intersect(targetNGrams).Count();
            int unionCount = sourceNGrams.Union(targetNGrams).Count();

            return (double)intersectionCount / unionCount;
        }

        public int CalculateLevenshteinDistance(string source, string target)
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

        private void toolStripButtonSum_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.Sum = toolStripButtonSum.Checked;
            Properties.Settings.Default.Save();

            toolStripComboBoxPt_SelectedIndexChanged(sender, EventArgs.Empty);
        }

        private void toolStripButtonOmitMyOrg_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.OmitMyOrg = toolStripButtonOmitMyOrg.Checked;
            Properties.Settings.Default.Save();

            toolStripComboBoxPt_SelectedIndexChanged(sender, EventArgs.Empty);
        }

        private void FormDI_LocationChanged(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                CommonFunctions.SnapToScreenEdges(this, SnapDistance, SnapCompPixel);
            }
        }

        private void FormDI_SizeChanged(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                CommonFunctions.SnapToScreenEdges(this, SnapDistance, SnapCompPixel);
            }
        }

        // Fixed上でマウスホイールを回したら、DHをスクロール
        private void DataGridViewFixed_MouseWheel(object sender, MouseEventArgs e)
        {
            // WheelDeltaが正なら上スクロール、負なら下スクロール
            int lines = e.Delta > 0 ? -1 : 1;
            int newIndex = Math.Max(0, Math.Min(dataGridViewDH.RowCount - 1, dataGridViewDH.FirstDisplayedScrollingRowIndex + lines));

            // DHのスクロールを動かす
            dataGridViewDH.FirstDisplayedScrollingRowIndex = newIndex;
        }

        private void toolStripButtonClass_CheckStateChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.DrugClass = toolStripButtonClass.Checked;
            Properties.Settings.Default.Save();

            toolStripComboBoxPt_SelectedIndexChanged(sender, EventArgs.Empty);
        }

        private async void toolStripButtonSinryo_Click(object sender, EventArgs e)
        {
            _parentForm.forceIdLink = true;
            await _parentForm.OpenSinryoHistory(_parentForm.tempId, true, false);
        }

        private void dataGridViewFixed_ColumnWidthChanged(object sender, DataGridViewColumnEventArgs e)
        {
            dataGridViewFixed.ColumnWidthChanged -= dataGridViewFixed_ColumnWidthChanged;

            int colIndex = e.Column.Index;
            if (colIndex < 2) // 最後の列ではない場合
            {
                var currentCol = dataGridViewFixed.Columns[colIndex];
                var nextCol = dataGridViewFixed.Columns[colIndex + 1];
                
                int totalWidth = fixedColumnWidth[colIndex] + fixedColumnWidth[colIndex + 1];

                nextCol.Width = totalWidth - currentCol.Width;

            }
            else
            {
                dataGridViewFixed.Columns[colIndex].Width = fixedColumnWidth[colIndex];
            }
            dataGridViewFixed.ColumnWidthChanged += dataGridViewFixed_ColumnWidthChanged;
        }

        private void dataGridViewFixed_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (string.IsNullOrEmpty(_parentForm.RSBdrive))
            {
                MessageBox.Show("RSBaseが検出できなかったので薬情検索はできません");
            }
            else
            {
                // e.ColumnIndex が有効な値であることを確認
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                {
                    // DataGridViewの列名を取得
                    string columnName = dataGridViewFixed.Columns[e.ColumnIndex].Name;

                    // 列名が "DrugN" だった場合に関数を実行
                    if (columnName.Equals("DrugN", StringComparison.OrdinalIgnoreCase))
                    {
                        SearchMedicineMenuItem_Click(sender, e);
                    }
                }
            }
        }

        private async void toolStripButtonTKK_Click(object sender, EventArgs e)
        {
            _parentForm.forceIdLink = true;
            await _parentForm.OpenTKKHistory(_parentForm.tempId, true, false);
        }
              
        private void WireExternalScrollbars()
        {
            // マウスホイールは常に外部Vバーを回す
            dataGridViewFixed.MouseWheel += OnGridMouseWheelToVScroll;
            dataGridViewDH.MouseWheel += OnGridMouseWheelToVScroll;

            // 選択行の同期（任意だが入れておくと操作感が良い）
            //dataGridViewFixed.SelectionChanged += OnSelectionSync;
            //dataGridViewDH.SelectionChanged += OnSelectionSync;

            // 外部スクロールバーの操作
            vScrollBar1.ValueChanged += (s, e) => SyncVerticalFromVScroll();
            hScrollBar1.ValueChanged += (s, e) => SyncHorizontalFromHScroll();

            // レイアウト変更時は再計算
            this.Resize += (s, e) => RecalcScrollbars();
            dataGridViewDH.SizeChanged += (s, e) => RecalcScrollbars();
            dataGridViewDH.DataBindingComplete += (s, e) => RecalcScrollbars();
            dataGridViewDH.RowsAdded += (s, e) => RecalcScrollbars();
            dataGridViewDH.RowsRemoved += (s, e) => RecalcScrollbars();
            dataGridViewDH.ColumnAdded += (s, e) => RecalcScrollbars();
            dataGridViewDH.ColumnRemoved += (s, e) => RecalcScrollbars();
            dataGridViewDH.ColumnWidthChanged += (s, e) => RecalcScrollbars();
        }

        private void OnGridMouseWheelToVScroll(object sender, MouseEventArgs e)
        {
            // 1ノッチ＝SmallChange
            int deltaSteps = Math.Sign(e.Delta) * -1; // 上回転で-1, 下回転で+1（環境によって逆なら符号を反転）
            int newValue = vScrollBar1.Value + deltaSteps * vScrollBar1.SmallChange;

            // 値をクランプ
            newValue = Math.Max(vScrollBar1.Minimum, Math.Min(newValue, Math.Max(vScrollBar1.Minimum, vScrollBar1.Maximum - vScrollBar1.LargeChange + 1)));

            if (newValue != vScrollBar1.Value)
            {
                vScrollBar1.Value = newValue;
                SyncVerticalFromVScroll();
            }
        }

        private void SyncVerticalFromVScroll()
        {
            if (dataGridViewDH.RowCount == 0) return;

            int idx = Math.Max(0, Math.Min(vScrollBar1.Value, MaxFirstScrollableRowIndex()));

            try { dataGridViewDH.FirstDisplayedScrollingRowIndex = idx; } catch { }
            try { dataGridViewFixed.FirstDisplayedScrollingRowIndex = idx; } catch { }
        }

        private void RecalcScrollbars()
        {
            // --- 垂直方向（共通） ---
            int page = VisibleRowCount(dataGridViewDH);
            int maxFirst = MaxFirstScrollableRowIndex();

            vScrollBar1.Minimum = 0;
            vScrollBar1.SmallChange = 1;
            vScrollBar1.LargeChange = Math.Max(1, page);
            vScrollBar1.Maximum = (dataGridViewDH.RowCount == 0) ? 0 : maxFirst + page - 1;

            // Vの現在値を有効範囲へ
            int vMaxValue = Math.Max(vScrollBar1.Minimum, vScrollBar1.Maximum - vScrollBar1.LargeChange + 1);
            vScrollBar1.Value = Math.Max(vScrollBar1.Minimum, Math.Min(vScrollBar1.Value, vMaxValue));
            SyncVerticalFromVScroll();

            // --- 水平方向（右のみ） ---
            int contentWidth = SumVisibleColumnWidths(dataGridViewDH);
            // 右パネルは Dock:Right の vScrollBar1 が占有するぶん可視幅が減る
            int clientWidth = Math.Max(0, dataGridViewDH.ClientSize.Width - vScrollBar1.Width);
            int maxH = Math.Max(0, contentWidth - clientWidth);

            hScrollBar1.Minimum = 0;
            hScrollBar1.SmallChange = 16;                   // 好みで
            hScrollBar1.LargeChange = Math.Max(32, clientWidth);
            hScrollBar1.Maximum = (maxH == 0) ? 0 : maxH + hScrollBar1.LargeChange - 1;
            hScrollBar1.Value = Math.Max(hScrollBar1.Minimum, Math.Min(hScrollBar1.Value, Math.Max(hScrollBar1.Minimum, maxH)));
            SyncHorizontalFromHScroll();

            // 左下スペーサは常に右Hバー高さに合わせる（高さ差による“1行ズレ”を防止）
            spaceLeft.Height = hScrollBar1.Height;
        }

        private int SumVisibleColumnWidths(DataGridView dgv)
            => dgv.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).Sum(c => c.Width);

        private int VisibleRowCount(DataGridView dgv)
        {
            if (dgv.RowCount == 0) return 1;
            int rowH = dgv.RowTemplate.Height; // 固定高さ前提
            int avail = dgv.ClientSize.Height - dgv.ColumnHeadersHeight - hScrollBar1.Height; // 右側は下にHバーがいる前提
            if (avail <= 0) return 1;
            return Math.Max(1, avail / rowH);
        }

        private int MaxFirstScrollableRowIndex()
        {
            int total = dataGridViewDH.RowCount;
            int page = VisibleRowCount(dataGridViewDH);
            return Math.Max(0, total - page);
        }


        private void SyncHorizontalFromHScroll()
        {
            dataGridViewDH.HorizontalScrollingOffset = hScrollBar1.Value;
        }

        private async void TabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab == tabPageInteraction)
            {
                await ShowInteractionAsync(_parentForm.tempId, ShowSpan, Properties.Settings.Default.OmitMyOrg);
            }
            else if(tabControl1.SelectedTab == tabPageAIDisease)
            {
                await LoadPromptTemplatesAsync(comboBoxLLMtemplates);

                await ShowLLMResult(_parentForm.tempId);

                //comboBoxLLMtemplates.SelectedIndex = 0;  // LoadPromptTemplatesAsyncの最後で呼ばれる イベント発火期待
            }
        }

        // ============================
        // 相互作用の更新ロジック
        // ============================
        public async Task ShowInteractionAsync(long ptId, int months, bool excludeMyOrg = true)
        {
            try
            {
                var from = DateTime.Today.AddMonths(-months);
                DateTime fstDay = new DateTime(from.Year, from.Month, 1);
                string startDate8 = fstDay.ToString("yyyyMMdd");

                using(IDbConnection conn = CommonFunctions.GetDbConnection(false))
                {
                    var npg = conn as NpgsqlConnection;
                    if (npg == null) throw new InvalidOperationException("PostgreSQL 接続が必要です。");

                    await npg.OpenAsync();

                    string sql = @"
                        WITH dh_fil AS (
                            SELECT dh.id, dh.ptidmain, dh.drugc, dh.drugn, dh.didate
                            FROM drug_history dh
                            WHERE dh.ptidmain = @ptId
                              AND dh.didate   >= @startDate8
                              AND dh.didate   ~ '^\d{8}$'
                              " + (excludeMyOrg ? "AND dh.prisorg <> 1" : "") + @"
                            ),
                            dcm_latest AS (
                                SELECT DISTINCT ON (drugc)
                                       drugc, yj_code, yj7, updated_at
                                FROM drug_code_map
                                ORDER BY drugc, updated_at DESC NULLS LAST, yj_code DESC
                            ),
                            dh_join AS (
                                SELECT f.*, m.yj_code, m.yj7
                                FROM dh_fil f
                                JOIN dcm_latest m ON m.drugc = f.drugc
                            ),
                            latest_per_yj7 AS (
                                SELECT *
                                FROM (
                                    SELECT dj.*,
                                           ROW_NUMBER() OVER (
                                               PARTITION BY dj.ptidmain, dj.yj7
                                               ORDER BY dj.didate DESC, dj.id DESC
                                           ) AS rn
                                    FROM dh_join dj
                                ) s
                                WHERE s.rn = 1
                            ),
                            -- sgml_interaction から yj7 単位の代表行を抽出（区分はここで日本語化）
                            di_longest AS (
                                SELECT
                                    di_yj7,
                                    partner_name_ja,
                                    partner_group_ja,
                                    section_type,              -- ← ここは既に日本語（禁忌/注意）
                                    symptoms_measures_ja
                                FROM (
                                    SELECT
                                        LEFT(si.yj_code, 7) AS di_yj7,
                                        si.partner_name_ja,
                                        si.partner_group_ja,
                                        CASE
                                          WHEN si.section_type = 'contraindicated' THEN '禁忌'
                                          WHEN si.section_type = 'precaution'      THEN '注意'
                                          ELSE '（その他）'
                                        END AS section_type,
                                        si.symptoms_measures_ja,
                                        si.mechanism_ja,
                                        si.id,
                                        ROW_NUMBER() OVER (
                                            PARTITION BY LEFT(si.yj_code, 7),
                                                         COALESCE(si.partner_name_ja, ''),
                                                         COALESCE(si.partner_group_ja, ''),
                                                         si.section_type
                                            ORDER BY (LENGTH(COALESCE(si.symptoms_measures_ja,'')) +
                                                      LENGTH(COALESCE(si.mechanism_ja,''))) DESC,
                                                     si.id DESC
                                        ) AS rn
                                    FROM public.sgml_interaction si
                                    WHERE si.section_type IN ('contraindicated','precaution')
                                ) x
                                WHERE x.rn = 1
                            )
                            SELECT
                                l.didate,
                                l.ptidmain,
                                l.drugc,
                                l.drugn,
                                l.yj_code,
                                l.yj7,
                                COALESCE(di.partner_name_ja, '(相互作用データなし)') AS partner_name_ja,
                                COALESCE(di.partner_group_ja, '')                    AS partner_group_ja,
                                COALESCE(di.section_type, '')                         AS section_type,            -- ← 日本語（禁忌/注意）
                                COALESCE(di.symptoms_measures_ja, '')                 AS symptoms_measures_ja,
                                (di.partner_name_ja IS NOT NULL)                      AS has_interaction
                            FROM latest_per_yj7 l
                            LEFT JOIN di_longest di
                                   ON di.di_yj7 = l.yj7
                            ORDER BY
                                has_interaction DESC,
                                CASE
                                  WHEN di.section_type = '禁忌' THEN 0
                                  WHEN di.section_type = '注意' THEN 1
                                  ELSE 2
                                END,
                                l.didate DESC,
                                l.drugc,
                                di.partner_name_ja NULLS LAST;";


                    using (var cmd = new NpgsqlCommand(sql, npg))
                    {
                        cmd.Parameters.AddWithValue("ptId", ptId);
                        cmd.Parameters.AddWithValue("startDate8", startDate8);

                        using (var r = await cmd.ExecuteReaderAsync())
                        {
                            var dt = new DataTable();
                            dt.Load(r);

                            dataGridViewInteraction.DataSource = dt.DefaultView;
                        }
                    }
                }

                SetInteractionView(dataGridViewInteraction);
                SetInteractionColors(dataGridViewInteraction);
            }
            catch (Exception ex)
            {
                MessageBox.Show("ShowInteractionAsync エラー: " + ex.Message + "\n" + ex.StackTrace);
            }
        }

        private void SetInteractionView(DataGridView dgv)
        {
            if (dgv.DataSource == null) return;

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
            //dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnMode.None;

            // 表示名（必要な列だけ）
            var headers = new Dictionary<string, string>
            {
                { "didate", "処方日" },
                { "drugn",  "薬剤名" },
                { "drugc",  "院内コード" },
                { "yj7",    "成分7桁" },
                { "partner_name_ja",  "相互作用相手" },
                { "partner_group_ja", "カテゴリ" },
                { "section_type", "区分" },
                { "symptoms_measures_ja", "説明" },
            };
            foreach (var kv in headers)
                if (dgv.Columns.Contains(kv.Key))
                    dgv.Columns[kv.Key].HeaderText = kv.Value;

            // 非表示列（あなたの指定を踏襲）
            string[] hiddenCols = { "ptidmain", "didate", "drugc", "yj7", "yj_code", "has_interaction" };
            foreach (string name in hiddenCols)
                if (dgv.Columns.Contains(name))
                    dgv.Columns[name].Visible = false;

            // 幅指定（固定）
            SetW("drugn", 150);
            SetW("partner_name_ja", 150);
            SetW("partner_group_ja", 150);
            SetW("section_type", 80);

            // 最後の列「説明」は残り幅をすべて使う
            if (dgv.Columns.Contains("symptoms_measures_ja"))
            {
                var c = dgv.Columns["symptoms_measures_ja"];
                c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                c.MinimumWidth = 200; // お好みで
            }

            // すべての列をソート可能に
            foreach (DataGridViewColumn col in dgv.Columns)
                col.SortMode = DataGridViewColumnSortMode.Automatic;

            // 固定幅ヘルパ
            void SetW(string name, int w)
            {
                if (!dgv.Columns.Contains(name)) return;
                var c = dgv.Columns[name];
                c.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                c.Width = w;
            }
        }

        private void SetInteractionColors(DataGridView dgv)
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

        private void dataGridViewInteraction_Sorted(object sender, EventArgs e)
        {
            SetInteractionColors(dataGridViewInteraction);
        }

        private void dataGridViewInteraction_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                var dgv = (DataGridView)sender;
                dgv.ClearSelection();
                dgv.CurrentCell = dgv.Rows[e.RowIndex].Cells[e.ColumnIndex];
                dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Selected = true;
            }
        }

        private void InitializeInteractionContextMenu()
        {
            var cms = new ContextMenuStrip();

            var copyFullMenuItem = new ToolStripMenuItem("表示のままコピー")
            {
                Tag = "full",
                Image = Properties.Resources.Zen
            };
            copyFullMenuItem.Click += CopyMenuItem_Click;

            var copyHalfMenuItem = new ToolStripMenuItem("半角でコピー")
            {
                Tag = "half",
                Image = Properties.Resources.Han
            };
            copyHalfMenuItem.Click += CopyMenuItem_Click;

            cms.Items.Add(copyFullMenuItem);
            cms.Items.Add(copyHalfMenuItem);

            // 薬情検索（RSBドライブが有効なときだけ）
            if (!string.IsNullOrEmpty(_parentForm.RSBdrive))
            {
                var searchItem = new ToolStripMenuItem("薬情検索")
                {
                    Image = Properties.Resources.Find
                };
                searchItem.Click += SearchMedicineMenuItem_Click;
                cms.Items.Add(searchItem);
            }

            dataGridViewInteraction.ContextMenuStrip = cms;
        }

        private void dataGridViewInteraction_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (string.IsNullOrEmpty(_parentForm.RSBdrive))
            {
                MessageBox.Show("RSBaseが検出できなかったので薬情検索はできません");
            }
            else
            {
                // e.ColumnIndex が有効な値であることを確認
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                {
                    // DataGridViewの列名を取得
                    string columnName =dataGridViewInteraction.Columns[e.ColumnIndex].Name;

                    // 列名が "DrugN" だった場合に関数を実行
                    if (columnName.Contains("partner_name_ja") || columnName.Contains("drugn"))
                    {
                        SearchMedicineMenuItem_Click(sender, e);
                    }
                }
            }
        }

        private async void buttonDiseaseRemakePrompt_Click(object sender, EventArgs e)
        {
            long ptID = _parentForm.tempId;

            string propmt = "";

            //テンプレート読み込み
            var tplSelectedId = GetSelectedTemplateId();
            if (tplSelectedId == null)
            {
                MessageBox.Show("テンプレートを選択してから実行してください");
                propmt = "(テンプレートが選択されていません)";
                textBoxDiseasePrompt.Invoke(new Action(() =>
                {
                    textBoxDiseasePrompt.Text = propmt;
                }));
                return;
            }
            else
            {
                long tplId = (long) tplSelectedId;
                DataTable dtTemplate = await GetPromptTemplateByIdAsync(tplId);

                propmt = await CommonFunctions.MakeLLMPrompt(ptID, dtTemplate);

            }

            textBoxDiseasePrompt.Invoke(new Action(() =>
            {
                textBoxDiseasePrompt.Text = propmt;
            }));
        }

        private async void buttonDiseaseQuery_Click(object sender, EventArgs e)
        {
            // ステータス表示用ヘルパ
            Action<string> setStatus = msg =>
            {
                if (labelStatus.InvokeRequired)
                    labelStatus.Invoke(new Action(() => labelStatus.Text = msg));
                else
                    labelStatus.Text = msg;
            };

            try
            {
                long ptId = _parentForm.tempId;

                // プロンプト
                string prompt = textBoxDiseasePrompt.Text;
                if (string.IsNullOrWhiteSpace(prompt))
                {
                    MessageBox.Show("プロンプトが空です。");
                    return;
                }

                setStatus("送信中...");

                //template ID取得
                string modelName = comboBoxModel.Text;

                string tplName = comboBoxLLMtemplates.Text;
                                
                var rid = await CommonFunctions.RunLlmOnceAndPersistAsync(ptId, prompt, tplName, modelName);

                // 表示
                // 直近結果をUIへ
                await ShowLLMResult(ptId, true);
            }
            catch (Exception ex)
            {
                setStatus("エラー");
                MessageBox.Show(ex.Message, "LLM送信エラー");
            }
        }

        private async Task ShowLLMResult(long ptId, bool onlyRes = false)
        {
            try
            {
                // 直近 months ヶ月分を取得
                DataTable dt = await CommonFunctions.LoadLatestAiResultToUIAsync(ptId, 1);

                // 表示列を用意：[title](model):yyyy/MM/dd HH:mm
                if (dt != null)
                {
                    if (!dt.Columns.Contains("display_name"))
                        dt.Columns.Add("display_name", typeof(string));

                    bool hasTitle = dt.Columns.Contains("title");
                    bool hasModel = dt.Columns.Contains("model_name");
                    bool hasResAt = dt.Columns.Contains("res_at");

                    foreach (DataRow r in dt.Rows)
                    {
                        string title = hasTitle && r["title"] != DBNull.Value ? Convert.ToString(r["title"]) : "";
                        string model = hasModel && r["model_name"] != DBNull.Value ? Convert.ToString(r["model_name"]) : "";
                        string when = "-";

                        if (hasResAt && r["res_at"] != DBNull.Value)
                        {
                            var dtUtc = DateTime.SpecifyKind(Convert.ToDateTime(r["res_at"]), DateTimeKind.Utc);
                            var dtLocal = dtUtc.ToLocalTime();
                            when = dtLocal.ToString("yyyy/MM/dd HH:mm");
                        }

                        if (string.IsNullOrWhiteSpace(title)) title = "(no title)";
                        if (string.IsNullOrWhiteSpace(model)) model = "-";

                        r["display_name"] = $"[{title}]({model}):{when}";
                    }
                }

                // UIへ反映（コンボだけ）
                Action bind = () =>
                {
                    comboBoxAIresults.DataSource = null; // 先に外す
                    comboBoxAIresults.DisplayMember = "display_name";
                    comboBoxAIresults.ValueMember = "id";
                    comboBoxAIresults.DropDownStyle = ComboBoxStyle.DropDownList;
                    comboBoxAIresults.DataSource = dt;

                    if (comboBoxAIresults.Items.Count > 0)
                    {
                        // 先頭を選択 → SelectedIndexChanged が発火してハンドラ側でテキスト更新
                        comboBoxAIresults.SelectedIndex = 0;
                    }
                    else
                    {
                        // レコードなし時は表示クリアだけ
                        textBoxDiseasePrompt.Clear();
                        textBoxDiseaseResponse.Clear();
                        labelStatus.Text = "[AI履歴なし]";
                    }
                };

                if (InvokeRequired) BeginInvoke(bind); else bind();
            }
            catch (Exception ex)
            {
                Action uiErr = () => labelStatus.Text = ex.Message;
                if (InvokeRequired) BeginInvoke(uiErr); else uiErr();
            }
        }

       
        private void checkBoxLLMLocal_CheckedChanged(object sender, EventArgs e)
        {
            //buttonDiseaseQuery_Click(sender, e);
        }

        private async void buttonPromptTpl_Click(object sender, EventArgs e)
        {
            //FormLLMsettingを開く
            using (var f = new FormLLMsetteing())
            {
                // 親フォームを所有者にしてモーダル表示
                f.StartPosition = FormStartPosition.CenterParent;
                f.ShowDialog(this);

                // OKボタンなどを置いて DialogResult.OK を返すようにしていればここで判定もできる
                // if (f.DialogResult == DialogResult.OK) { … }

                await LoadPromptTemplatesAsync(comboBoxLLMtemplates);
            }
        }

        private async Task LoadPromptTemplatesAsync(System.Windows.Forms.ComboBox combo)
        {
            var dt = new DataTable();

            const string sql = @"
                SELECT
                    id,
                    tpl_name,
                    model_name,
                    -- 表示用: tpl_name (model_name) / model_nameがNULLや空なら tpl_nameのみ
                    CASE
                        WHEN COALESCE(NULLIF(TRIM(model_name), ''), '') = ''
                             THEN tpl_name
                        ELSE tpl_name || ' (' || model_name || ')'
                    END AS display_name
                FROM public.ai_prompt_tpl
                ORDER BY id ASC;";

            using (var conn = (NpgsqlConnection)CommonFunctions.GetDbConnection(true))
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(sql, conn))
                using (var r = await cmd.ExecuteReaderAsync())
                {
                    dt.Load(r);
                }
            }

            combo.DisplayMember = "display_name"; // 画面に出す文字列
            combo.ValueMember = "id";           // 選択値として返す列
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.DataSource = dt;

            // 既定選択（必要なら設定値などで拾う）
            if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        }

        //テンプレートコンボボックスの値取得
        private long? GetSelectedTemplateId()
        {
            if (comboBoxLLMtemplates.SelectedValue == null) return null;
            long id;
            return long.TryParse(comboBoxLLMtemplates.SelectedValue.ToString(), out id) ? (long?)id : null;
        }

        private async void comboBoxLLMtemplates_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                // comboBoxModelにテンプレートのモデルを設定
                // 1) 選択IDの取得を安全に
                object selVal = comboBoxLLMtemplates.SelectedValue;
                if (selVal == null || selVal == DBNull.Value) return;

                long tplSelId;
                if (!long.TryParse(selVal.ToString(), out tplSelId)) return;

                // この時点の選択を保持（await後のレース対策）
                string expectedSel = selVal.ToString();

                // 2) テンプレ取得（DB）
                DataTable dtTemplate = await GetPromptTemplateByIdAsync(tplSelId);
                if (dtTemplate == null || dtTemplate.Rows.Count == 0)
                {
                    // 取得できなかった場合はモデル一覧だけ既定値で表示 or クリア
                    var fallbackModel = Properties.Settings.Default.LLMmodel ?? string.Empty;
                    SetModelsToComboBox(comboBoxModel, ollamaModelList ?? new List<ModelInfo>(), fallbackModel);
                    return;
                }

                // 3) await 中に別選択になっていないか確認（レース防止）
                var currentSel = comboBoxLLMtemplates.SelectedValue;
                if (currentSel == null || !string.Equals(currentSel.ToString(), expectedSel, StringComparison.Ordinal))
                {
                    // 選択が変わっていたら適用しない
                    return;
                }

                // 4) model_name を安全に取り出し（DBNull/列欠落に対応）
                var row = dtTemplate.Rows[0];
                string defaultModel =
                    row.Table.Columns.Contains("model_name") && row["model_name"] != DBNull.Value
                    ? row["model_name"].ToString()
                    : (Properties.Settings.Default.LLMmodel ?? string.Empty);

                // 5) モデル一覧をバインド（null安全）
                SetModelsToComboBox(comboBoxModel, ollamaModelList ?? new List<ModelInfo>(), defaultModel);
            }
            catch (Exception ex)
            {
                // ログだけに残したい場合
                try { await CommonFunctions.AddLogAsync($"[LLMtpl] 選択変更エラー: {ex.Message}", fileOnly: true); } catch { }

                // ユーザーへも通知したい場合はコメントアウト外す
                // MessageBox.Show(this, "テンプレートの読み込みに失敗しました。\n" + ex.Message, "エラー",
                //     MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void comboBoxAIresults_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (!(comboBoxAIresults.SelectedItem is DataRowView drv))
                    return;

                var row = drv.Row;
                var t = row.Table;

                string res = t.Columns.Contains("res") && row["res"] != DBNull.Value ? Convert.ToString(row["res"]) : "";
                string prompt = t.Columns.Contains("prompt") && row["prompt"] != DBNull.Value ? Convert.ToString(row["prompt"]) : "";
                string status = t.Columns.Contains("status") && row["status"] != DBNull.Value ? Convert.ToString(row["status"]) : "";
                string model = t.Columns.Contains("model_name") && row["model_name"] != DBNull.Value ? Convert.ToString(row["model_name"]) : "";
                string resAtStr = "";
                int resLength = 0;

                if (t.Columns.Contains("res_len_chars") && row["res_len_chars"] != DBNull.Value)
                    int.TryParse(row["res_len_chars"].ToString(), out resLength);

                if (t.Columns.Contains("res_at") && row["res_at"] != DBNull.Value)
                {
                    var dtUtc = DateTime.SpecifyKind(Convert.ToDateTime(row["res_at"]), DateTimeKind.Utc);
                    var dtLocal = dtUtc.ToLocalTime();
                    resAtStr = dtLocal.ToString("yyyy/MM/dd HH:mm");
                }

                textBoxDiseaseResponse.Text = res;
                textBoxDiseasePrompt.Text = prompt;
                labelStatus.Text = $"[{status}] {model} {resLength}文字 更新日：{resAtStr}";
            }
            catch (Exception ex)
            {
                labelStatus.Text = ex.Message;
            }
        }
    }
}
