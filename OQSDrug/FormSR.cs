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
using static OQSDrug.FormTKK;

namespace OQSDrug
{
    public partial class FormSR : Form
    {
        private const int SnapDistance = 16; // 吸着の距離（ピクセル）
        private int SnapCompPixel = 8;  //余白補正

        private Form1 _parentForm;

        private Color[] RowColors = { Color.WhiteSmoke, Color.White };

        private List<(long PtID, string PtName)> ptData = new List<(long PtID, string PtName)>();

        private DataTable SinryoData = new DataTable();

        private int ShowSpan = Properties.Settings.Default.ViewerSpan;

        public FormSR(Form1 parentForm)
        {
            InitializeComponent();

            _parentForm = parentForm;
            toolStrip1.Renderer = new CustomToolStripRenderer(); // カスタム描画を適用
        }

        public async Task LoadDataIntoComboBoxes()
        {
            if (!await CommonFunctions.WaitForDbUnlock(2000))
            {
                MessageBox.Show("データベースがロックされており、LoadDataIntoComboBoxes に失敗しました。もう一度やり直してください。");
                return;
            }

            ptData = new List<(long PtID, string DisplayText)>();

            string sql = @"
                SELECT PtIDmain, PtName, Max(id) AS Maxid
                FROM sinryo_history
                GROUP BY PtIDmain, PtName
                ORDER BY Max(id) DESC;";

            // MDBの場合は?に変換
            sql = CommonFunctions.ConvertSqlForOleDb(sql);

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
                            while (await reader.ReadAsync())
                            {
                                long ptID = reader["PtIDmain"] == DBNull.Value ? 0 : Convert.ToInt64(reader["PtIDmain"]);
                                string ptName = reader["PtName"]?.ToString() ?? "";

                                string displayName = $"{ptID.ToString().PadLeft(6, ' ')} : {ptName}";
                                ptData.Add((ptID, displayName));
                            }
                        }
                    }

                    toolStrip1.Invoke(new Action(() =>
                    {
                        toolStripComboBoxPtID.SelectedIndexChanged -= toolStripComboBoxPtID_SelectedIndexChanged;

                        toolStripComboBoxPtID.Items.Clear();
                        toolStripComboBoxPtID.SelectedIndex = -1;

                        foreach (var item in ptData)
                        {
                            toolStripComboBoxPtID.Items.Add(new PtItem
                            {
                                PtID = item.PtID,
                                DisplayText = item.PtName // ← タプルの第2要素を使用
                            });
                        }

                        toolStripComboBoxPtID.SelectedIndexChanged += toolStripComboBoxPtID_SelectedIndexChanged;

                        // RSB 連動 or ダブルクリック起動
                        if (_parentForm.autoRSB || _parentForm.forceIdLink)
                        {
                            _parentForm.forceIdLink = false;

                            int index = ptData.FindIndex(p => p.PtID == _parentForm.tempId);
                            toolStripComboBoxPtID.SelectedIndex = index >= 0 ? index : -1;
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


        private void toolStripButtonClose_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Maximized || this.WindowState == FormWindowState.Minimized) this.WindowState = FormWindowState.Normal;

            this.Close();
        }

        private async void toolStripComboBoxPtID_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (toolStripComboBoxPtID.SelectedItem is PtItem selectedPt)
                {
                    // 選択された PtID を取得
                    long ptID = selectedPt.PtID;

                    // DataGridView に表示するデータを取得
                    await ShowSRData(ptID);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"患者ID選択時にエラーが発生しました:{ex.Message}");
            }
        }

        private async void FormSR_Load(object sender, EventArgs e)
        {
            toolStripButtonSum.Checked = Properties.Settings.Default.Sum;

            await LoadDataIntoComboBoxes();

            InitializeContextMenu();
        }

        private async Task ShowSRData(long PtID)
        {
            if (!await CommonFunctions.WaitForDbUnlock(2000))
            {
                MessageBox.Show("データベースがロックされており、ShowSRDataに失敗しました。もう一度やり直してみてください。");
                return;
            }

            // pivotField を Sumボタンの状態で切り替え
            string pivotField = toolStripButtonSum.Checked ? "metrmonth" : "didate";

            // 取得する縦持ちSQL
            string query = @"
                SELECT
                    metrdihnm AS hospital,
                    sininfn,
                    qua1,
                    metrmonth,
                    didate,
                    times
                FROM sinryo_history
                WHERE ptidmain = @ptidmain
                ORDER BY " + pivotField + " DESC";

            // OleDb 用に変換（Accessなら ? に変換）
            query = CommonFunctions.ConvertSqlForOleDb(query);

            try
            {
                using (IDbConnection connection = CommonFunctions.GetDbConnection(true))
                {
                    await ((DbConnection)connection).OpenAsync();

                    using (IDbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = query;

                        // パラメータ追加
                        CommonFunctions.AddDbParameter(command, "@ptidmain", PtID);

                        CommonFunctions.DataDbLock = true;

                        DataTable rawTable = new DataTable();
                        using (var reader = await ((DbCommand)command).ExecuteReaderAsync())
                        {
                            rawTable.Load(reader);
                        }

                        CommonFunctions.DataDbLock = false;

                        // ピボット変換
                        var pivotTable = CommonFunctions.ConvertPivotDataTable(
                            rawTable,
                            new[] { "hospital", "sininfn", "qua1" },
                            pivotField,
                            "times"
                        );
                                               
                        // Row Color
                        if (!pivotTable.Columns.Contains("Color"))
                        {
                            pivotTable.Columns.Add("Color", typeof(Color));
                        }

                        string previousHospital = null;
                        int colorIndex = 0;
                        foreach (DataRow row in pivotTable.Rows)
                        {
                            if (previousHospital == null || row["hospital"]?.ToString() != previousHospital)
                            {
                                previousHospital = row["hospital"]?.ToString();
                                colorIndex = (colorIndex + 1) % RowColors.Length;
                            }
                            else
                            {
                                row["hospital"] = "";
                            }
                            row["Color"] = RowColors[colorIndex];
                        }

                        SinryoData = pivotTable;
                    }
                }

                // UI反映
                Invoke(new Action(() =>
                {
                    InitializeDataGridView(dataGridViewSinryo);
                    dataGridViewSinryo.DataSource = SinryoData;
                    ConfigureDataGridView(dataGridViewSinryo);
                }));
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

            for (int i = 0; i < dataGridView.Columns.Count; i++)
            {
                if (i > 2)
                {
                    dataGridView.Columns[i].Width = 60;
                }
                else
                {
                    dataGridView.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                }
            }
            //dataGridView.Columns["DrugC"].Visible = true;
            dataGridView.Columns["Color"].Visible = false;
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

            // メニューアイテムを追加
            contextMenuStrip.Items.Add(copyFullMenuItem);
            contextMenuStrip.Items.Add(copyHalfMenuItem);

            dataGridViewSinryo.ContextMenuStrip = contextMenuStrip;
        }

        private async void CopyMenuItem_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem menuItem)
            {
                // Tagから動作を判別
                string mode = menuItem.Tag as string;

                if (dataGridViewSinryo.SelectedCells.Count > 0)
                {
                    // 選択されたセルを行・列のインデックスでソート
                    var sortedCells = dataGridViewSinryo.SelectedCells
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

        private void FormSR_LocationChanged(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                CommonFunctions.SnapToScreenEdges(this, SnapDistance, SnapCompPixel);
            }
        }

        private void toolStripButtonSum_CheckedChanged(object sender, EventArgs e)
        {
            toolStripComboBoxPtID_SelectedIndexChanged(sender, e);
        }
    }
}
