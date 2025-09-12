using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OQSDrug
{
    public partial class FormTKK : Form
    {
        private const int SnapDistance = 16; // 吸着の距離（ピクセル）
        private int SnapCompPixel = 8;  //余白補正

        private Form1 _parentForm;
        private string provider;

        private Color[] ColumnColors = { Color.Gainsboro, Color.White, Color.WhiteSmoke };
        private Color WarningColor = Color.LightYellow;
        private Color AlertColor = Color.LightCoral;

        private List<(long PtID, string PtName)> ptData = new List<(long PtID, string PtName)>();

       
        public FormTKK(Form1 parentForm)
        {
            InitializeComponent();

            _parentForm = parentForm;
            provider = CommonFunctions.DBProvider;
        }

        public class PtItem
        {
            public long PtID { get; set; }
            public string DisplayText { get; set; }

            public override string ToString()
            {
                return DisplayText; // ToolStripComboBox に表示されるテキスト
            }
        }


        public async Task LoadToolStripComboBox()
        {
            if (await CommonFunctions.WaitForDbUnlock(2000))
            {
                ptData = new List<(long PtID, string PtName)>();

                string query = @"SELECT PtIDmain, PtName, Max(id) AS Maxid
                      FROM TKK_history
                      GROUP BY PtIDmain, PtName
                      ORDER BY Max(id) DESC;";
                
                using (IDbConnection connection = CommonFunctions.GetDbConnection())
                {
                    try
                    {
                        // OpenAsync対応
                        if (connection is DbConnection dbConnection)
                        {
                            await dbConnection.OpenAsync();
                        }
                        else
                        {
                            connection.Open();
                        }

                        CommonFunctions.DataDbLock = true;

                        using (IDbCommand command = connection.CreateCommand())
                        {
                            command.CommandText = query;

                            using (IDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    long ptID = reader["PtIDmain"] == DBNull.Value ? 0 : Convert.ToInt64(reader["PtIDmain"]);
                                    string ptName = reader["PtName"].ToString();

                                    string displayName = $"{ptID.ToString().PadLeft(6, ' ')} : {ptName}";
                                    ptData.Add((ptID, displayName));
                                }
                            }
                        }

                        CommonFunctions.DataDbLock = false;

                        Invoke(new Action(() =>
                        {
                            toolStripComboBoxPt.SelectedIndexChanged -= toolStripComboBoxPt_SelectedIndexChanged;

                            toolStripComboBoxPt.Items.Clear();
                            toolStripComboBoxPt.SelectedIndex = -1;

                            foreach (var item in ptData)
                            {
                                toolStripComboBoxPt.Items.Add(new PtItem { PtID = item.PtID, DisplayText = item.PtName });
                            }

                            toolStripComboBoxPt.SelectedIndexChanged += toolStripComboBoxPt_SelectedIndexChanged;

                            if (_parentForm.autoTKK || _parentForm.forceIdLink)
                            {
                                _parentForm.forceIdLink = false;

                                int index = ptData.FindIndex(p => p.PtID == _parentForm.tempId);
                                toolStripComboBoxPt.SelectedIndex = index >= 0 ? index : -1;
                            }
                        }));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"データの取得中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        CommonFunctions.DataDbLock = false;
                    }
                }
            }
            else
            {
                MessageBox.Show("データベースがロックされており、LoadDataIntoComboBoxに失敗しました。もう一度やり直してみてください。");
            }
        }


        private async void FormTKK_Load(object sender, EventArgs e)
        {
            InitializeContextMenu();

            await LoadToolStripComboBox();
        }

        private async void toolStripComboBoxPt_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (toolStripComboBoxPt.SelectedItem is PtItem selectedPt)
                {
                    // 選択された PtID を取得
                    long ptID = selectedPt.PtID;

                    // DataGridView に表示するデータを取得
                    await ShowTKKData(ptID);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"患者ID選択時にエラーが発生しました:{ex.Message}");
            }
        }

        private async Task ShowTKKData(long ptID)
        {
            if (!await CommonFunctions.WaitForDbUnlock(2000))
            {
                MessageBox.Show("データベースがロックされており、ShowTKKData に失敗しました。もう一度やり直してください。");
                return;
            }

            CommonFunctions.DataDbLock = true;

            // PG用のSQLをベースに統一（全て小文字）
            string query = @"
                SELECT 
                    LEFT(itemcode, 4) AS itemcode4,
                    sex,
                    itemname,
                    unit,
                    effectivetime,
                    datavaluename
                FROM tkk_history
                WHERE ptidmain = @ptid
                ORDER BY LEFT(itemcode, 4), effectivetime DESC";

            query = CommonFunctions.ConvertSqlForOleDb(query);
            
            try
            {
                using (IDbConnection connection = CommonFunctions.GetDbConnection())
                {
                    if (connection is DbConnection dbConn)
                        await dbConn.OpenAsync();
                    else
                        connection.Open();

                    using (IDbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = query;
                        CommonFunctions.AddDbParameter(command, "ptid", ptID);

                        using (IDataReader reader = command.ExecuteReader())
                        {
                            var raw = new DataTable();
                            raw.Load(reader);
                            CommonFunctions.DataDbLock = false;

                            // pivot化
                            var pivot = CommonFunctions.ConvertPivotDataTable(
                                source: raw,
                                rowFields: new[] { "itemcode4", "sex", "itemname", "unit" },
                                columnField: "effectivetime",
                                dataField: "datavaluename",
                                aggregate: values =>
                                {
                                    foreach (var v in values)
                                        if (v != null && v != DBNull.Value && v.ToString() != "")
                                            return v;
                                    return DBNull.Value;
                                }
                            );

                            dataGridViewTKK.Invoke(new Action(() =>
                            {
                                dataGridViewTKK.DataSource = pivot;
                                ConfigureDataGridView(dataGridViewTKK);
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
                CommonFunctions.DataDbLock = false;
            }
        }


        private void toolStripButtonClose_Click(object sender, EventArgs e)
        {
            if(this.WindowState == FormWindowState.Maximized || this.WindowState == FormWindowState.Minimized) this.WindowState = FormWindowState.Normal;

            this.Close();
        }

        private void ConfigureDataGridView(DataGridView dataGridView)
        {
            try
            {
                // レコードセレクタを非表示にする
                dataGridView.RowHeadersVisible = false;

                // 行サイズの変更を無効化する
                dataGridView.AllowUserToResizeRows = false;

                // カラム幅を自動調整する
                dataGridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);

                dataGridView.EditMode = DataGridViewEditMode.EditProgrammatically;
                dataGridView.SelectionMode = DataGridViewSelectionMode.CellSelect;

                // 列タイトルを日本語に変更
                if (dataGridView.Columns.Contains("itemname"))
                {
                    dataGridView.Columns["itemname"].HeaderText = "項目名";
                }
                if (dataGridView.Columns.Contains("unit"))
                {
                    dataGridView.Columns["unit"].HeaderText = "単位";
                }

                dataGridView.Columns["itemcode4"].Visible = false;
                dataGridView.Columns["sex"].Visible = false;

                // 特定のカラムの幅を固定にする
                if (dataGridView.Columns.Count > 0)
                {
                    dataGridView.Columns[2].Width = 150;  //3番目のカラム

                }
                for (int i = 4; i < dataGridView.Columns.Count; i++)
                {
                    dataGridView.Columns[i].Width = 100; //結果カラム
                }

                // ソート機能を無効にする
                dataGridView.AllowUserToOrderColumns = false;
                // 各列のソートモードを無効にする
                foreach (DataGridViewColumn column in dataGridView.Columns)
                {
                    column.SortMode = DataGridViewColumnSortMode.NotSortable;
                }

                //// 特定の列の背景色を変える
                for (int i = 4; i < dataGridView.Columns.Count; i++)
                {
                    dataGridView.Columns[i].DefaultCellStyle.BackColor = ColumnColors[i % 2 + 1];
                }
                dataGridView.Columns[2].DefaultCellStyle.BackColor = ColumnColors[0];
                dataGridView.Columns[3].DefaultCellStyle.BackColor = ColumnColors[0];

                // 縦方向の罫線を非表示にする
                dataGridView.CellBorderStyle = DataGridViewCellBorderStyle.Raised;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void FormTKK_LocationChanged(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                CommonFunctions.SnapToScreenEdges(this, SnapDistance, SnapCompPixel);
            }
        }

        private void FormTKK_MaximumSizeChanged(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                CommonFunctions.SnapToScreenEdges(this, SnapDistance, SnapCompPixel);
            }
        }

        private void dataGridViewTKK_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            // 非表示列（ItemCode）を利用する
            string itemCode = dataGridViewTKK.Rows[e.RowIndex].Cells["itemcode4"].Value.ToString();
            if (itemCode.Length > 3)
            {
                itemCode = itemCode.Substring(0, 4);
                int sex = int.TryParse(dataGridViewTKK.Rows[e.RowIndex].Cells["Sex"].Value?.ToString(), out int parsedValue) ? parsedValue : 1;

                string itemCodeWithSex = $"{sex}_{itemCode}";

                // 基準値が存在しない場合は何もしない
                if (e.Value == null || string.IsNullOrWhiteSpace(e.Value.ToString()) || !CommonFunctions.TKKreferenceDict.ContainsKey(itemCodeWithSex)) return;

                //カラムが3以上
                if (e.ColumnIndex < 4) return;

                var reference = CommonFunctions.TKKreferenceDict[itemCodeWithSex];
                string compairType = reference.CompairType;
                string limit1 = reference.Limit1;
                string limit2 = reference.Limit2;

                try
                {
                    //「含む」
                    if (compairType == "=")
                    {
                        if (limit1.Length > 0 && e.Value.ToString().Contains(limit1))
                        {
                            e.CellStyle.BackColor = WarningColor;
                        }
                        else if (limit2.Length > 0 && e.Value.ToString().Contains(limit2))
                        {
                            e.CellStyle.BackColor = AlertColor;
                        }
                        
                    }
                    else
                    {
                        // セルの値を取得
                        if (double.TryParse(e.Value?.ToString(), out double cellValue))
                        {
                            // 条件に応じた色設定
                            if (compairType == "<")
                            {
                                if (double.TryParse(limit2, out double dlimit2) && cellValue > dlimit2)
                                    e.CellStyle.BackColor = AlertColor;
                                else if (double.TryParse(limit1, out double dlimit1) && cellValue > dlimit1)
                                    e.CellStyle.BackColor = WarningColor;
                            }
                            else if (compairType == ">")
                            {
                                if (double.TryParse(limit2, out double dlimit2) && cellValue < dlimit2)
                                    e.CellStyle.BackColor = AlertColor;
                                else if (double.TryParse(limit1, out double dlimit1) && cellValue < dlimit1)
                                    e.CellStyle.BackColor = WarningColor;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"セルの条件設定でエラーが発生しました。{ex.Message}");
                }
            }
        }

        private void InitializeContextMenu()
        {
            // ContextMenuStripの作成
            ContextMenuStrip contextMenuStrip = new ContextMenuStrip();

            ToolStripMenuItem copyWithNameMenuItem = new ToolStripMenuItem("検査名付コピー");
            copyWithNameMenuItem.Tag = "CopyWithName"; // 引数として識別するためのタグを設定
            copyWithNameMenuItem.Image = Properties.Resources.Copy;
            copyWithNameMenuItem.Click += CopyMenuItem_Click;

           
            // メニューアイテムを追加
            contextMenuStrip.Items.Add(copyWithNameMenuItem);
            
            // DataGridViewに右クリックメニューを設定
            dataGridViewTKK.ContextMenuStrip = contextMenuStrip;
        }

        private async void CopyMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (sender is ToolStripMenuItem menuItem)
                {
                    string clipboardText = "";

                    //// Tagから動作を判別
                    //string mode = menuItem.Tag as string;

                    if (dataGridViewTKK.SelectedCells.Count > 0)
                    {
                        string dateString = "";

                        // 選択されたセルを上から順に並び替え
                        var sortedCells = dataGridViewTKK.SelectedCells
                            .Cast<DataGridViewCell>()
                            .Where(c => c.ColumnIndex >= 4)
                            .OrderBy(c => c.ColumnIndex)
                            .ThenBy(c => c.RowIndex)
                            .ToList();

                        // 選択セルを処理
                        foreach (var cell in sortedCells)
                        {
                            // 1. 左上のセルの列名を取得
                            string columnName = dataGridViewTKK.Columns[cell.ColumnIndex].HeaderText;
                            if (columnName != dateString)
                            {
                                dateString = columnName;
                                clipboardText += $"<{dateString}>" + Environment.NewLine;
                            }

                            // 2. ItemName列の値を取得
                            clipboardText += dataGridViewTKK.Rows[cell.RowIndex].Cells["itemname"].Value?.ToString() + ":";

                            // 3. セルの値を取得
                            clipboardText += cell.Value?.ToString() + Environment.NewLine;
                        }
                    }


                    // リトライを使ってクリップボードにコピー
                    bool success = (clipboardText.Length > 0) ? await CommonFunctions.RetryClipboardSetTextAsync(clipboardText) : false;

                    if (!success)
                    {
                        MessageBox.Show("クリップボードへのコピーに失敗しました。もう一度トライしてみてください。");
                    }
                }

            } 
            catch(Exception ex)
            {
                MessageBox.Show($"コピー操作時にエラーが発生しました。もう一度試してみてください。{ex.Message}");
            }
        }
    }
}
