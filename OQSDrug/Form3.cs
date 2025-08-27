using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;
using Microsoft.VisualBasic;
using System.IO;
using System.Text.RegularExpressions;

namespace OQSDrug
{
    public partial class Form3 : Form
    {
        private const int SnapDistance = 16; // 吸着の距離（ピクセル）
        private int SnapCompPixel = 8;  //余白補正

        private Form1 _parentForm;
        private string provider;

        private Color[] RowColors = {Color.WhiteSmoke, Color.White};

        public List<string[]> RSBDI = new List<string[]>();

        private int ShowSpan = Properties.Settings.Default.ViewerSpan;
        private FormSearch formSearch = null;

        public Form3(Form1 parentForm)
        {
            InitializeComponent();

            _parentForm = parentForm;
            provider = _parentForm.DBProvider;

            RSBDI = _parentForm.RSBDI;
        }

        public async Task LoadDataIntoComboBoxes()
        {
            if (await _parentForm.WaitForDbUnlock(2000))
            {
                _parentForm.DataDbLock = true;

                // クエリ文字列
                string query = @"SELECT PtIDmain, PtName, Max(id) AS Maxid
                            FROM drug_history
                            GROUP BY PtIDmain, PtName
                            ORDER BY Max(id) DESC;";

                string connectionOQSData = $"Provider={provider};Data Source={Properties.Settings.Default.OQSDrugData};";
                // データテーブルを作成
                DataTable dataTable = new DataTable();
                dataTable.Columns.Add("PtID", typeof(long));
                dataTable.Columns.Add("DisplayName", typeof(string));

                using (OleDbConnection connection = new OleDbConnection(connectionOQSData))
                {
                    try
                    {
                        // 接続を開く
                        await connection.OpenAsync();

                        // コマンドを作成
                        using (OleDbCommand command = new OleDbCommand(query, connection))
                        {
                            // データを取得
                            using (OleDbDataReader reader = (OleDbDataReader)await command.ExecuteReaderAsync())
                            {
                                while (reader.Read())
                                {
                                    long ptID = reader["PtIDmain"] == DBNull.Value ? 0 : Convert.ToInt64((reader["PtIDmain"]));
                                    string ptName = reader["PtName"].ToString();

                                    // PtIDとPtNameを結合した表示名を作成
                                    string displayName = $"{ptID.ToString().PadLeft(6, ' ')} : {ptName}";

                                    // DataTableに行を追加
                                    dataTable.Rows.Add(ptID, displayName);
                                }
                            }
                        }

                        _parentForm.DataDbLock = false;

                        comboBoxPtID.Invoke(new Action(() =>
                        {
                            
                            // ComboBoxにデータをバインド
                            comboBoxPtID.SelectedIndexChanged -= comboBoxPtID_SelectedIndexChanged; // イベントを一時解除 selectedvalueのnull対策

                            // ComboBoxのデータソースを一旦クリア
                            comboBoxPtID.DataSource = null;

                            comboBoxPtID.DataSource = dataTable;
                            comboBoxPtID.ValueMember = "PtID";
                            comboBoxPtID.DisplayMember = "DisplayName";

                            comboBoxPtID.SelectedIndex = -1;
                            //comboBoxPtID.SelectedValue = null;

                            comboBoxPtID.SelectedIndexChanged += comboBoxPtID_SelectedIndexChanged; // イベントを再登録

                            // RSB 連動か ダブルクリック起動か
                            if (_parentForm.autoRSB || _parentForm.forceIdLink)
                            {
                                _parentForm.forceIdLink = false;

                                // PtID が _parentForm.tempId に一致する行を検索
                                DataRow[] rows = dataTable.Select($"PtID = {_parentForm.tempId}");

                                // 一致するデータが見つかれば、選択する
                                if (rows.Length > 0)
                                {
                                    // 最初の一致したレコードの PtID を SelectedValue に設定
                                    comboBoxPtID.SelectedValue = rows[0]["PtID"];
                                }
                                else
                                {
                                    comboBoxPtID.SelectedIndex = -1;
                                }
                            }
                        }));

                    }
                    catch (Exception ex)
                    {
                        // エラー処理
                        MessageBox.Show($"データの取得中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        _parentForm.DataDbLock = false;
                    }
                }
            }
            else
            {
                MessageBox.Show("データベースがロックされており、LoadDataIntoComboBoxに失敗しました。もう一度やり直してみてください。");
            }
            
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private async void Form3_Load(object sender, EventArgs e)
        {
            SetRadioButtonState(ShowSpan);
            SetRadioButtonEvent();

            InitializeContextMenu();

            await LoadDataIntoComboBoxes();

            checkBoxSum.Checked = Properties.Settings.Default.Sum;

            checkBoxOmitMyOrg.Checked = Properties.Settings.Default.OmitMyOrg;


        }

        private async Task ShowDrugData(long PtID)
        {
            if (await _parentForm.WaitForDbUnlock(2000))
            {
                _parentForm.DataDbLock = true;

                // 動的クエリをC#コード内で作成
                string query = @"
                            TRANSFORM Sum(drug_history.Times) AS Times
                            SELECT 
                                IIf(Len([PrlsHNm])=0, [MeTrDiHNm], [PrlsHNm]) AS Hospital, 
                                drug_history.DrugN, 
                                drug_history.IngreN,
                                drug_history.Qua1, 
                                drug_history.Unit
                            FROM 
                                drug_history
                            WHERE 
                                Revised = false AND %SPANFILTER%  %MYORGFILTER%
                                drug_history.PtIDmain = ? 
                            GROUP BY 
                                IIf(Len([PrlsHNm])=0, [MeTrDiHNm], [PrlsHNm]),
                                drug_history.IngreN,
                                drug_history.DrugN, 
                                drug_history.Qua1, 
                                drug_history.Unit
                            ORDER BY 
                                %FIELD% DESC
                            PIVOT
                                %FIELD% ;";

                string pivot = (Properties.Settings.Default.Sum) ? "drug_history.MeTrMonth" : "drug_history.DiDate";

                //long viewSpan = GetComboBoxSelectedValue(comboBoxSpan);
                string startDate = DateTime.Now.AddMonths(0 - ShowSpan).ToString("yyyyMM");
                string spanfilter = (ShowSpan == 0) ? "" : $" MeTrMonth >= '{startDate}' AND ";
                string myorgfilter = (Properties.Settings.Default.OmitMyOrg) ? "drug_history.PrIsOrg <> 1 AND " : "";

                query = query.Replace("%FIELD%", pivot);
                query = query.Replace("%SPANFILTER%", spanfilter);
                query = query.Replace("%MYORGFILTER%", myorgfilter);

                // 固定列の定義
                var fixedColumns = new List<string> { "Hospital", "DrugN", "Qua1", "Unit" };

                string connectionOQSData = $"Provider={provider};Data Source={Properties.Settings.Default.OQSDrugData};";

                try
                {
                    using (OleDbConnection connection = new OleDbConnection(connectionOQSData))
                    {
                        // 接続を開く
                        await connection.OpenAsync();

                        // データ取得
                        using (var command = new OleDbCommand(query, connection))
                        {

                            command.Parameters.AddWithValue("?", PtID);

                            using (var reader = await command.ExecuteReaderAsync())
                            using (var dataTable = new DataTable())
                            {
                                dataTable.Load(reader);

                                _parentForm.DataDbLock = false;

                                // 手動で加工する
                                Color[] RowColorSetting = new Color[100];
                                int colorIndex = 0, i = 0;

                                string previousHospital = null; // 前回のHospital値
                                foreach (DataRow row in dataTable.Rows)
                                {
                                    // Hospital列を加工
                                    if (previousHospital == null || previousHospital != row["Hospital"].ToString())
                                    { //新しい病院
                                        previousHospital = row["Hospital"].ToString();
                                        colorIndex++;
                                        if (colorIndex > 1) colorIndex = 0;
                                    }
                                    else //同じ病院のレコード
                                    {
                                        row["Hospital"] = ""; // 同じHospitalが続く場合は空白
                                    }
                                    RowColorSetting[i] = RowColors[colorIndex];
                                    i++;
                                }

                                // DataGridViewにバインド
                                // 非同期関数内でのUIスレッド操作
                                dataGridViewDH.Invoke(new Action(() =>
                                {
                                    // DataGridViewを初期化
                                    InitializeDataGridView(dataGridViewDH);

                                    // DataGridViewにデータをバインド
                                    dataGridViewDH.DataSource = dataTable;

                                    // DataGridViewの外観や動作を設定
                                    ConfigureDataGridView(dataGridViewDH, RowColorSetting);
                                }));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"エラーが発生しました: {ex.Message}");
                }
                finally
                {
                    _parentForm.DataDbLock = false;
                }
            }
            else
            {
                MessageBox.Show("データベースがロックされており、ShowDrugDataに失敗しました。もう一度やり直してみてください。");
            }
        }

        private async void comboBoxPtID_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                // nullチェック
                if (comboBoxPtID.SelectedValue == null)
                {
                    InitializeDataGridView(dataGridViewDH);
                }
                else
                {
                    // PtIDmainの値を取得
                    string strPtID = comboBoxPtID.SelectedValue.ToString();
                    //MessageBox.Show(strPtID);
                    // 数字としてパース可能かチェック
                    if (long.TryParse(strPtID, out long PtID))
                    {
                        // sender が RadioButton かどうかを判定
                        if (!(sender is System.Windows.Forms.RadioButton radioButton) && !(sender is System.Windows.Forms.CheckBox))
                        {
                            //表示期間をリセットする
                            ShowSpan = Properties.Settings.Default.ViewerSpan;
                            RemoveRadioButtonEvent();
                            SetRadioButtonState(ShowSpan);
                            SetRadioButtonEvent();
                        }
                        await ShowDrugData(PtID);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"患者ID選択時にエラーが発生しました:{ex.Message}");
            }

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

        }

        private void ConfigureDataGridView(DataGridView dataGridView, Color[] rowColors)
        {
            // レコードセレクタを非表示にする
            dataGridView.RowHeadersVisible = false;

            // カラム幅を自動調整する
            dataGridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);

            dataGridView.EditMode = DataGridViewEditMode.EditProgrammatically;
            dataGridView.SelectionMode = DataGridViewSelectionMode.CellSelect;

            // 特定のカラムの幅を固定にする（例: 1番目と3番目のカラム）
            if (dataGridView.Columns.Count > 0)
            {
                dataGridView.Columns[0].Width = 150;  // 1番目のカラム
                
            }
            dataGridView.Columns["IngreN"].Visible = false; //一般名は非表示

            // ソート機能を無効にする
            dataGridView.AllowUserToOrderColumns = false;
            // 各列のソートモードを無効にする
            foreach (DataGridViewColumn column in dataGridView.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            // 特定の行の背景色を変える（例: 2番目の行の背景色を変更）
            for(int i = 0; i < dataGridView.Rows.Count; i++)
            {
                dataGridView.Rows[i].DefaultCellStyle.BackColor = rowColors[i];
            }

            // 縦方向の罫線を非表示にする
            dataGridView.CellBorderStyle = DataGridViewCellBorderStyle.Raised;



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
            if (_parentForm.RSBdrive != null)
            {
                contextMenuStrip.Items.Add(searchMedicineMenuItem);
            }
            // DataGridViewに右クリックメニューを設定
            dataGridViewDH.ContextMenuStrip = contextMenuStrip;
        }

        private async void CopyMenuItem_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem menuItem)
            {
                // Tagから動作を判別
                string mode = menuItem.Tag as string;

                if (dataGridViewDH.SelectedCells.Count > 0)
                {
                    // 選択されたセルを行・列のインデックスでソート
                    var sortedCells = dataGridViewDH.SelectedCells
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
            if (dataGridViewDH.SelectedCells.Count > 0)
            {
                try
                {
                    List<string[]> results = new List<string[]>();

                    if (RSBDI.Count == 0)
                    {
                        RSBDI = _parentForm.RSBDI;
                    }

                    // 選択されたセルの中で最後のセルを取得
                    DataGridViewCell lastSelectedCell = dataGridViewDH.SelectedCells[dataGridViewDH.SelectedCells.Count - 1];

                    string drugName = lastSelectedCell.Value?.ToString();
                    string IngreN = dataGridViewDH.Rows[lastSelectedCell.RowIndex].Cells["IngreN"].Value?.ToString();

                    if (drugName.Length > 0)
                    {
                        List<Tuple<string[], double>> topResults = await FuzzySearchAsync(drugName, IngreN, RSBDI, 0.2);

                        if (topResults.Count > 0)
                        {
                            // formSearchを開く、すでに開いていれば表示変更する
                            if(formSearch == null || formSearch.IsDisposed)
                            {
                                //formSearch = new FormSearch(this);
                                formSearch.Show(this);
                            }

                            formSearch.SetDrugLists(topResults);
                            //formSearch.SetDrugName(topResults[0].Item1[0]);
                        }
                        else
                        {
                            MessageBox.Show("RSB薬情に該当薬剤が見つかりませんでした。");
                        }
                        
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("薬情表示時にエラーが発生しました。" + ex.Message);
                }
            }            
        }

        // リストRSBDIのカラム1,2を対象にあいまい検索を行い、上位10件を返すメソッド
        public async Task<List<Tuple<string[], double>>> FuzzySearchAsync(string drugName, string ingreN, List<string[]> DI, double cutoffThreshold = 0.4, double bonusForOriginator = 0.4, double penaltyForMissingIngreN = 0.5)
        {
            //double bonusForOriginator = 0.4;        // "先発" の場合のボーナス
            //double penaltyForMissingIngreN = 0.5;   // 一般名が含まれない場合のペナルティ
            double weightColumn1 = 0.3;             // 1列目のスコアに対する重み
            double weightColumn2 = 0.5;             // 2列目のスコアに対する重み

            // 正規表現で「」や【】に囲まれた部分を削除
            string processedDrugName = RemoveCampany(drugName);

            //数字アルファベットは除去しておく
            //string drugNameNoDigit = RemoveDigits(drugName);

            if (ingreN == null || ingreN == "") ingreN = processedDrugName;

            // 各レコードに対してN-gram類似度を計算（非同期処理）
            var tasks = DI.Select(record => Task.Run(() =>
            {
                string column1 = record[0]; // 1列目
                string column2 = record[1]; // 2列目
                string column4 = record[3]; // 4列目（"先発" の確認に使用）

                double similarityColumn1 = CalculateNGramSimilarity(processedDrugName, column1);
                double similarityColumn2 = CalculateNGramSimilarity(ingreN, column2);

                // 編集距離を考慮したスコア
                double editDistanceScore = 1.0 - (double)CalculateLevenshteinDistance(processedDrugName, column1)
                                           / Math.Max(processedDrugName.Length, column1.Length);

                // 最終的なスコア（加重平均）
                double similarity = weightColumn1 * Math.Max(similarityColumn1, editDistanceScore) +
                                    weightColumn2 * similarityColumn2;

                bool exact = false;
                // 完全一致の特別スコア
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

                // "先発" のボーナス
                if (similarity > cutoffThreshold && column4 == "先発")
                {
                    similarity += bonusForOriginator;
                }

                // 一般名が含まれない場合のペナルティ
                if (!exact && !column2.Contains(ingreN) && !ingreN.Contains(column2))
                {
                    similarity -= penaltyForMissingIngreN;
                }

                // 類似度の下限を 0 に制限
                similarity = Math.Max(0, similarity);

                return new Tuple<string[], double>(record, similarity);  // レコードと類似度のタプルを返す
            }));

            // 全タスクの完了を待つ
            var results = await Task.WhenAll(tasks);

            // 類似度の降順で並び替え、カットオフ値以上のものだけフィルタリング
            var filteredResults = results
                .Where(r => r.Item2 >= cutoffThreshold)  // 類似度がカットオフ値以上のものを選択
                .OrderByDescending(r => r.Item2)  // 類似度の降順で並べ替え
                .Take(20)  // 上位10件を取得
                .ToList();

            return filteredResults;
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

        private void checkBoxSum_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.Sum = checkBoxSum.Checked;
            Properties.Settings.Default.Save();

            comboBoxPtID_SelectedIndexChanged(sender, EventArgs.Empty);
        }
                
        private void Form3_LocationChanged(object sender, EventArgs e)
        {
            CommonFunctions.SnapToScreenEdges(this, SnapDistance, SnapCompPixel);
        }

        private void Form3_SizeChanged(object sender, EventArgs e)
        {
            CommonFunctions.SnapToScreenEdges(this, SnapDistance, SnapCompPixel);
        }

        private void radioButton_CheckedChanged(object sender, EventArgs e)
        {
            System.Windows.Forms.RadioButton selectedButton = sender as System.Windows.Forms.RadioButton;

            if (selectedButton != null && selectedButton.Checked)
            {
                if (int.TryParse(selectedButton.Tag.ToString(), out int tagValue))
                {
                    ShowSpan = tagValue; // Tag から値を取得
                }
            }
        
            comboBoxPtID_SelectedIndexChanged(sender, e);
        }

        // 変数値に基づいてラジオボタンの状態を設定
        private void SetRadioButtonState(int value)
        {
            // フォーム内のすべての RadioButton を走査
            foreach (Control control in this.Controls)
            {
                if (control is System.Windows.Forms.RadioButton radioButton && radioButton.Tag != null)
                {
                    // Tag の値が指定された値と一致する場合のみ Checked を true にする
                    if (int.TryParse(radioButton.Tag.ToString(), out int tagValue))
                    {
                        radioButton.Checked = tagValue == value;
                    }
                }
            }
        }

        private void SetRadioButtonEvent()
        {
            //radioButton1M.CheckedChanged += radioButton_CheckedChanged;
            radioButton3M.CheckedChanged += radioButton_CheckedChanged;
            radioButton6M.CheckedChanged += radioButton_CheckedChanged;
            radioButton12M.CheckedChanged += radioButton_CheckedChanged;
            radioButtonAll.CheckedChanged += radioButton_CheckedChanged;
        }

        private void RemoveRadioButtonEvent()
        {
            //radioButton1M.CheckedChanged -= radioButton_CheckedChanged;
            radioButton3M.CheckedChanged -= radioButton_CheckedChanged;
            radioButton6M.CheckedChanged -= radioButton_CheckedChanged;
            radioButton12M.CheckedChanged -= radioButton_CheckedChanged;
            radioButtonAll.CheckedChanged -= radioButton_CheckedChanged;
        }

        private void checkBoxOmitMyOrg_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.OmitMyOrg = checkBoxOmitMyOrg.Checked;
            Properties.Settings.Default.Save();

            comboBoxPtID_SelectedIndexChanged(sender, e);
        }
    }
}
