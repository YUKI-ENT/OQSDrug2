using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Data.OleDb;
using System.Data.Common;
using System.Collections.Generic;

namespace OQSDrug
{
    public partial class FormDynaViewer : Form
    {
        private Dictionary<string, TextBox> detailBoxes = new Dictionary<string, TextBox>();
        private DataTable currentTable;
        // map of desired detail field key -> actual DataTable column name present in currentTable
        private Dictionary<string, string> columnMap = new Dictionary<string, string>(StringComparer.Ordinal);

        // fields to display on the details pane (Japanese labels / keys as present in DataTable)
        // Include many variants/aliases so environments with slightly different schemas still show important values
        private readonly string[] detailFields = new[] {
            "処理実行日時","任意のファイル識別子","氏名","氏名カナ","カルテ番号","性別コード","生年月日","住所","郵便番号",
            "被保険者証記号","被保険者証番号","被保険者証枝番","保険者番号","資格有効性","資格取得年月日","資格喪失年月日",
            "被保険者証有効開始年月日","被保険者証有効終了年月日","特定疾病療養受療証自己負担限度額",
            // 公費 / 高齢者 / 限度額 関連（複数の命名バリエーションを含める）
            "公費負担者番号１","公費受給者番号１","公費有効開始年月日１","公費有効終了年月日１",
            "公費負担者番号2","公費受給者番号2","公費有効開始年月日2","公費有効終了年月日2",
            "公費負担者番号２","公費受給者番号２","公費有効開始年月日２","公費有効終了年月日２",
            "高齢受給者証交付年月日","高齢受給者証有効開始年月日","高齢受給者証有効終了年月日",
            //"限度額適用区分","限度額適用認定証交付年月日","限度額適用認定証有効開始年月日","限度額適用認定証有効終了年月日",
            "一部負担金割合","被保険者証一部負担金割合",
            // explicit additional 限度額-related keys requested
            "限度額適用認定証適用区分","限度額適用認定証区分","限度額適用認定証交付年月日","限度額適用認定証有効開始年月日","限度額適用認定証有効終了年月日",
            // display-only generated fields
            "証区分表示","適用区分表示",
            // 照会区分表示は表示と行の色分けに重要
            "照会区分表示","照会区分"
            ,"本人家族の別","未就学区分"
        };

        public FormDynaViewer()
        {
            InitializeComponent();
            BuildDetailControls();
            ApplyStyles();
            // color rows after binding
            this.dgvList.DataBindingComplete += DgvList_DataBindingComplete;
            this.Shown += FormDynaViewer_Shown;
            // prevent user from changing row height
            try
            {
                dgvList.AllowUserToResizeRows = false;
                dgvList.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
                dgvList.RowTemplate.Height = dgvList.RowTemplate.Height; // keep existing template height
            }
            catch { }
        }

        private void BuildDetailControls()
        {
            var y = 8;
            int labelW = 160;
            int boxW = 360;

            // make right panel scrollable and visually distinct
            try
            {
                rightPanel.AutoScroll = true;
                rightPanel.BackColor = Color.WhiteSmoke;
                rightPanel.Padding = new Padding(8);
            }
            catch { }

            foreach (var key in detailFields)
            {
                var lbl = new Label { Text = key + ":", Location = new Point(8, y + 4), Size = new Size(labelW, 20) };
                lbl.Font = new Font("Meiryo UI", 9F, FontStyle.Bold);
                lbl.ForeColor = Color.FromArgb(50, 50, 50);
                rightPanel.Controls.Add(lbl);

                var tb = new TextBox { Location = new Point(8 + labelW + 8, y), Size = new Size(boxW, 22), ReadOnly = true };
                // make detail boxes easier to read: single-line read-only boxes (avoid wrapping/scroll)
                tb.Font = new Font("Meiryo UI", 9F);
                tb.BackColor = Color.White;
                tb.BorderStyle = BorderStyle.FixedSingle;
                tb.Multiline = false; // keep single line to avoid wrapping/scroll in narrow panel
                tb.ScrollBars = ScrollBars.None;
                tb.Height = 22;
                rightPanel.Controls.Add(tb);
                detailBoxes[key] = tb;

                y += 30;
            }

            // Add a refresh button
            var btn = new Button { Text = "更新", Location = new Point(8, y + 8), Size = new Size(80, 28) };
            btn.Click += async (s, e) => await LoadDataAsync();
            rightPanel.Controls.Add(btn);
        }

        private void ApplyStyles()
        {
            try
            {
                // DataGridView styling for better readability
                dgvList.EnableHeadersVisualStyles = false;
                dgvList.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(45, 45, 48);
                dgvList.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                dgvList.ColumnHeadersDefaultCellStyle.Font = new Font("Meiryo UI", 9F, FontStyle.Bold);
                dgvList.RowHeadersVisible = false;
                dgvList.GridColor = Color.FromArgb(220, 220, 220);
                dgvList.BackgroundColor = Color.White;
                dgvList.DefaultCellStyle.Font = new Font("Meiryo UI", 9F);
                dgvList.DefaultCellStyle.SelectionBackColor = Color.FromArgb(173, 216, 230);
                dgvList.DefaultCellStyle.SelectionForeColor = Color.Black;
                // avoid cell text wrapping which can make rows expand unexpectedly
                dgvList.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
                dgvList.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 250, 250);
                dgvList.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                dgvList.AllowUserToResizeColumns = true;

                // ensure row height accommodates font to avoid clipping
                try
                {
                    int desired = dgvList.DefaultCellStyle.Font.Height + 10; // padding
                    if (desired < 20) desired = 20;
                    dgvList.RowTemplate.Height = desired;
                    dgvList.RowTemplate.MinimumHeight = desired;
                    dgvList.ColumnHeadersHeight = dgvList.ColumnHeadersDefaultCellStyle.Font.Height + 14;
                }
                catch { }

                // make columns more compact by default
                dgvList.DefaultCellStyle.Padding = new Padding(4);

                // Right panel header style: add a subtle title if not present
                var title = new Label { Text = "詳細情報", Location = new Point(8, 8), AutoSize = true, Font = new Font("Meiryo UI", 11F, FontStyle.Bold), ForeColor = Color.FromArgb(60, 60, 60) };
                rightPanel.Controls.Add(title);

                // reposition existing controls down if necessary
                foreach (Control c in rightPanel.Controls)
                {
                    if (c == title) continue;
                    c.Top += 28;
                }

                // highlight selected row better
                dgvList.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgvList.MultiSelect = false;
                dgvList.AllowUserToAddRows = false;
                // respond to size/column changes to keep date column as flexible center
                try
                {
                    this.Resize += (s, e) => AdjustGridColumns();
                    dgvList.SizeChanged += (s, e) => AdjustGridColumns();
                    dgvList.ColumnWidthChanged += (s, e) => AdjustGridColumns();
                    rightPanel.SizeChanged += (s, e) => AdjustGridColumns();
                }
                catch { }
            }
            catch { }
        }

        // Adjust column widths so that ID/Name/Muk are fixed and 処理実行日時 expands/contracts to fill remaining space
        private void AdjustGridColumns()
        {
            try
            {
                if (dgvList.Columns == null || dgvList.Columns.Count == 0) return;

                int idWidth = 0, nameWidth = 0, mukWidth = 0, other = 0;
                if (dgvList.Columns.Contains("カルテ番号")) idWidth = dgvList.Columns["カルテ番号"].Width;
                if (dgvList.Columns.Contains("氏名")) nameWidth = dgvList.Columns["氏名"].Width;
                if (dgvList.Columns.Contains("照会区分表示") && dgvList.Columns["照会区分表示"].Visible) mukWidth = dgvList.Columns["照会区分表示"].Width;

                other = idWidth + nameWidth + mukWidth;

                int available = dgvList.ClientSize.Width - other;
                // subtract a small margin for grid lines / scroll
                int margin = SystemInformation.VerticalScrollBarWidth + 8;
                available -= margin;
                if (available < 160) available = 160;

                if (dgvList.Columns.Contains("処理実行日時"))
                {
                    var col = dgvList.Columns["処理実行日時"];
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    col.Width = Math.Max(col.MinimumWidth > 0 ? col.MinimumWidth : 160, available);
                }
            }
            catch { }
        }

        private async void FormDynaViewer_Shown(object sender, EventArgs e)
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                DataTable dt = null;

                if (Properties.Settings.Default.DBtype == "pg")
                {
                    // Try to read from public.dyna_sikaku if exists
                    using (var conn = CommonFunctions.GetDbConnection(false))
                    {
                        await ((DbConnection)conn).OpenAsync();
                        using (var cmd = conn.CreateCommand())
                        {
                            // select limited rows, order by original timestamp column so sorting is correct
                            cmd.CommandText = "SELECT * FROM public.dyna_sikaku ORDER BY \"処理実行日時\" DESC LIMIT 1000";
                            try
                            {
                                using (var reader = await ((DbCommand)cmd).ExecuteReaderAsync())
                                {
                                    dt = new DataTable();
                                    dt.Load(reader);
                                    AddLogSafe($"[DynaViewer] PG dyna_sikaku loaded rows={dt.Rows.Count} cols={dt.Columns.Count}");
                                }
                            }
                            catch (Exception ex)
                            {
                                // Try fallback without ORDER BY if ORDER BY caused an error
                                AddLogSafe("dyna_sikaku ORDER BY failed: " + ex.Message + " → fallback to simple SELECT");
                                try
                                {
                                    cmd.CommandText = "SELECT * FROM public.dyna_sikaku LIMIT 1000";
                                    using (var reader = await ((DbCommand)cmd).ExecuteReaderAsync())
                                    {
                                        dt = new DataTable();
                                        dt.Load(reader);
                                        AddLogSafe($"[DynaViewer] PG dyna_sikaku (fallback) loaded rows={dt.Rows.Count} cols={dt.Columns.Count}");
                                    }
                                }
                                catch (Exception ex2)
                                {
                                    AddLogSafe("dyna_sikaku fallback failed: " + ex2.Message);
                                    dt = new DataTable();
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Access: read Datadyna table
                    string dynaFile = Properties.Settings.Default.Datadyna;
                    string DynaTable = await ResolveDynaTableAsync(dynaFile);
                    string connStr = $"Provider={CommonFunctions.DBProvider};Data Source={dynaFile};Mode=Read;Persist Security Info=False;";
                    string sql = $"SELECT * FROM [{DynaTable}]";

                    dt = new DataTable();
                    try
                    {
                        using (var conn = new OleDbConnection(connStr))
                        {
                            await conn.OpenAsync();
                            using (var cmd = new OleDbCommand(sql, conn))
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                dt.Load(reader);
                                AddLogSafe($"[DynaViewer] Access {DynaTable} loaded rows={dt.Rows.Count} cols={dt.Columns.Count}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        AddLogSafe("ダイナ読み込みに失敗: " + ex.Message);
                        dt = new DataTable();
                    }
                }

                currentTable = dt;

                // build columnMap to resolve variants/aliases of column names to actual columns present in dt
                try
                {
                    columnMap.Clear();
                    var cols = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
                    string Normalize(string s)
                    {
                        if (s == null) return string.Empty;
                        var sb = new System.Text.StringBuilder();
                        foreach (var ch in s.ToLowerInvariant())
                        {
                            if (!char.IsWhiteSpace(ch) && !char.IsPunctuation(ch) && !char.IsControl(ch)) sb.Append(ch);
                        }
                        return sb.ToString();
                    }

                    var normCols = cols.ToDictionary(c => c, c => Normalize(c), StringComparer.Ordinal);
                    foreach (var key in detailFields)
                    {
                        // try exact, contains, or normalized match
                        string found = null;
                        // exact
                        found = cols.FirstOrDefault(c => string.Equals(c, key, StringComparison.Ordinal));
                        if (found == null)
                            found = cols.FirstOrDefault(c => c.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0);
                        if (found == null)
                        {
                            var nk = Normalize(key);
                            found = normCols.FirstOrDefault(kv => kv.Value.Contains(nk)).Key;
                        }
                        if (found != null && !columnMap.ContainsKey(key)) columnMap[key] = found;
                    }
                }
                catch { }

                // Prepare a view with key columns for list
                DataTable listTable = new DataTable();
                // Decide columns to show: カルテ番号, 氏名, 処理実行日時 or fallback to first 3 columns
                string colCarte = dt.Columns.Contains("カルテ番号") ? "カルテ番号" : (dt.Columns.Count > 0 ? dt.Columns[0].ColumnName : null);
                string colName = dt.Columns.Contains("氏名") ? "氏名" : (dt.Columns.Count > 1 ? dt.Columns[1].ColumnName : null);
                string colDate = dt.Columns.Contains("処理実行日時") ? "処理実行日時" : (dt.Columns.Count > 2 ? dt.Columns[2].ColumnName : null);

                listTable.Columns.Add("カルテ番号", typeof(string));
                listTable.Columns.Add("氏名", typeof(string));
                // raw value to preserve original カルテ番号 for matching (hidden)
                listTable.Columns.Add("カルテ番号_raw", typeof(string));
                // 処理実行日時は表示用文字列（Postgres側で ORDER BY しているので表示は文字列でOK）
                listTable.Columns.Add("処理実行日時", typeof(string));
                // hidden column to carry 照会区分表示 or similar for coloring
                listTable.Columns.Add("照会区分表示", typeof(string));
                // mapping back to original currentTable row index to reliably find the source row
                listTable.Columns.Add("source_row_index", typeof(int));

                if (dt.Rows.Count > 0)
                {
                    for (int ri = 0; ri < dt.Rows.Count; ri++)
                    {
                        var r = dt.Rows[ri];
                        var v1 = colCarte != null && dt.Columns.Contains(colCarte) && r[colCarte] != DBNull.Value ? r[colCarte].ToString() : "";
                        // format カルテ番号 as 主番号-枝番 (last digit is branch)
                        string formattedCarte = v1;
                        if (!string.IsNullOrEmpty(v1))
                        {
                            var digits = new string(v1.Where(char.IsDigit).ToArray());
                            if (digits.Length >= 2)
                            {
                                var main = digits.Substring(0, digits.Length - 1);
                                var branch = digits.Substring(digits.Length - 1, 1);
                                formattedCarte = main + "-" + branch;
                            }
                            else if (digits.Length == 1)
                            {
                                formattedCarte = "0-" + digits;
                            }
                        }
                        var v2 = colName != null && dt.Columns.Contains(colName) && r[colName] != DBNull.Value ? r[colName].ToString() : "";
                        // determine display string for 処理実行日時 when possible
                        string displayDate = "";
                        string rawDateStr = colDate != null && dt.Columns.Contains(colDate) && r[colDate] != DBNull.Value ? r[colDate].ToString() : "";
                        if (!string.IsNullOrEmpty(rawDateStr))
                        {
                            DateTime parsed;
                            if (DateTime.TryParse(rawDateStr, System.Globalization.CultureInfo.CurrentCulture, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out parsed)
                                || DateTime.TryParse(rawDateStr, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out parsed))
                            {
                                displayDate = parsed.ToString("yyyy/MM/dd HH:mm:ss");
                            }
                            else
                            {
                                displayDate = rawDateStr; // fallback to original representation
                            }
                        }
                        // try to obtain 照会 value from known column names if present
                        string muk = "";
                        string[] candNames = new[]{"照会区分表示","照会区分"};
                        foreach(var cn in candNames)
                        {
                            if (dt.Columns.Contains(cn) && r[cn] != DBNull.Value)
                            {
                                muk = r[cn].ToString();
                                break;
                            }
                        }
                        // fallback: try any column that contains 照会 or 照会区分 in its name (trim/normalize)
                        if (string.IsNullOrEmpty(muk))
                        {
                            foreach (DataColumn dc in dt.Columns)
                            {
                                var nm = dc.ColumnName ?? "";
                                if (nm.IndexOf("照会", StringComparison.OrdinalIgnoreCase) >= 0 || nm.IndexOf("照会区分", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    if (r[dc] != DBNull.Value)
                                    {
                                        muk = r[dc].ToString();
                                        break;
                                    }
                                }
                            }
                        }
                        // columns: カルテ番号, 氏名, カルテ番号_raw, 処理実行日時 (string), 照会区分表示, source_row_index
                        listTable.Rows.Add(formattedCarte, v2, v1, displayDate, muk, ri);
                    }
                }

                // Debug: report 照会区分表示 content stats
                try
                {
                    if (listTable.Columns.Contains("照会区分表示"))
                    {
                        var nonEmpty = listTable.AsEnumerable().Where(r => r["照会区分表示"] != DBNull.Value && !string.IsNullOrWhiteSpace(r["照会区分表示"].ToString())).ToList();
                        AddLogSafe($"[DynaViewer] 照会区分表示 non-empty rows={nonEmpty.Count} / total={listTable.Rows.Count}");
                        if (nonEmpty.Count > 0)
                        {
                            var sample = nonEmpty.Take(5).Select(r => r["照会区分表示"].ToString()).Distinct();
                            AddLogSafe("[DynaViewer] 照会区分表示 samples: " + string.Join(" | ", sample));
                        }
                    }
                }
                catch { }

                // Bind directly
                dgvList.DataSource = listTable;

                // Debug: log column names after binding so we can detect missing/misnamed columns
                try
                {
                    AddLogSafe($"[DynaViewer] dgvList.Columns.Count={dgvList.Columns.Count}");
                    for (int i = 0; i < dgvList.Columns.Count; i++)
                    {
                        var c = dgvList.Columns[i];
                        AddLogSafe($"[DynaViewer] Col[{i}] Name='{c.Name}' Header='{c.HeaderText}' Visible={c.Visible}");
                    }
                }
                catch { }

                try { /* display already formatted */ } catch { }
                try { dgvList.Sort(dgvList.Columns["処理実行日時"], System.ComponentModel.ListSortDirection.Descending); } catch { }

                // Hide raw カルテ番号 and source_row_index columns used for matching; ensure 照会区分表示 is visible
                try { if (dgvList.Columns.Contains("カルテ番号_raw")) dgvList.Columns["カルテ番号_raw"].Visible = false; } catch { }
                try { if (dgvList.Columns.Contains("source_row_index")) dgvList.Columns["source_row_index"].Visible = false; } catch { }
                try { if (dgvList.Columns.Contains("照会区分表示")) { dgvList.Columns["照会区分表示"].Visible = true; dgvList.Columns["照会区分表示"].DisplayIndex = 3; dgvList.Columns["照会区分表示"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells; } } catch { }

                // Ensure 処理実行日時 is visible and placed as 3rd column (index 2)
                try
                {
                    if (dgvList.Columns.Contains("処理実行日時"))
                    {
                        var col = dgvList.Columns["処理実行日時"];
                        col.HeaderText = "取得日時";
                        col.Visible = true;
                        col.DisplayIndex = 2;
                        col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    }
                }
                catch { }

                // Set headers for カルテ番号 and 氏名 and ensure they are left of the list
                try { if (dgvList.Columns.Contains("カルテ番号")) { dgvList.Columns["カルテ番号"].HeaderText = "ID"; dgvList.Columns["カルテ番号"].DisplayIndex = 0; dgvList.Columns["カルテ番号"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells; } } catch { }
                try { if (dgvList.Columns.Contains("氏名")) { dgvList.Columns["氏名"].HeaderText = "氏名"; dgvList.Columns["氏名"].DisplayIndex = 1; dgvList.Columns["氏名"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; dgvList.Columns["氏名"].MinimumWidth = 120; } } catch { }
                // default sort by 処理実行日時 descending
                // If reading from PostgreSQL we rely on DB ORDER BY (client-side string sort can break ordering),
                // otherwise fall back to sorting the displayed column.
                try
                {
                    if (Properties.Settings.Default.DBtype != "pg")
                    {
                        if (dgvList.Columns.Contains("処理実行日時"))
                        {
                            dgvList.Sort(dgvList.Columns["処理実行日時"], System.ComponentModel.ListSortDirection.Descending);
                        }
                        else if (dgvList.Columns.Count > 2)
                        {
                            dgvList.Sort(dgvList.Columns[2], System.ComponentModel.ListSortDirection.Descending);
                        }
                    }
                }
                catch { }
                // 照会区分表示 は表示する（行色分けに使う）。タイトルは「照会区分」にする
                try { if (dgvList.Columns.Contains("照会区分表示")) { dgvList.Columns["照会区分表示"].Visible = true; dgvList.Columns["照会区分表示"].DisplayIndex = 3; dgvList.Columns["照会区分表示"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells; dgvList.Columns["照会区分表示"].HeaderText = "照会区分"; } } catch { }

                // 列幅調整：その他は固定幅、"処理実行日時" を残り幅で伸ばす
                try
                {
                    if (dgvList.Columns.Contains("カルテ番号"))
                    {
                        var c = dgvList.Columns["カルテ番号"];
                        c.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        c.Width = 80;
                        c.MinimumWidth = 80;
                        c.DisplayIndex = 0;
                    }
                }
                catch { }
                try
                {
                    if (dgvList.Columns.Contains("氏名"))
                    {
                        var c = dgvList.Columns["氏名"];
                        c.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        c.Width = 110;
                        c.MinimumWidth = 80;
                        c.DisplayIndex = 1;
                    }
                }
                catch { }
                try
                {
                    if (dgvList.Columns.Contains("処理実行日時"))
                    {
                        var c = dgvList.Columns["処理実行日時"];
                        // make the date column fill remaining space
                        c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                        c.MinimumWidth = 160;
                        c.DisplayIndex = 2;
                    }
                }
                catch { }
                try
                {
                    if (dgvList.Columns.Contains("照会区分表示"))
                    {
                        var c = dgvList.Columns["照会区分表示"];
                        c.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        c.Width = 70;
                        c.MinimumWidth = 50;
                        c.DisplayIndex = 3;
                        c.Visible = true;
                    }
                }
                catch { }

                // adjust selection
                if (dgvList.Rows.Count > 0) dgvList.Rows[0].Selected = true;
            }
            catch (Exception ex)
            {
                AddLogSafe("LoadDataAsyncでエラー: " + ex.Message);
            }
        }

        private async Task<string> ResolveDynaTableAsync(string dbPath)
        {
            const string wkoTable = "WKO資格確認結果表示";
            const string tTable = "T_資格確認結果表示";

            string connStr = $"Provider={CommonFunctions.DBProvider};Data Source={dbPath};Mode=Read;";

            using (var conn = new OleDbConnection(connStr))
            {
                await conn.OpenAsync();

                DataTable wkoSchema = conn.GetSchema("Tables", new string[] { null, null, wkoTable, null });
                if (wkoSchema.Rows.Count > 0) return wkoTable;

                DataTable tSchema = conn.GetSchema("Tables", new string[] { null, null, tTable, null });
                if (tSchema.Rows.Count > 0) return tTable;
            }

            return tTable;
        }

        private void DgvList_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            try
            {
                // Robust coloring: if any visible cell in the row contains 'マイナ', mark the row
                for (int i = 0; i < dgvList.Rows.Count; i++)
                {
                    bool isMina = false;
                    for (int j = 0; j < dgvList.Rows[i].Cells.Count; j++)
                    {
                        try
                        {
                            var cell = dgvList.Rows[i].Cells[j];
                            if (cell == null || cell.Value == null) continue;
                            var s = cell.Value.ToString();
                            if (string.IsNullOrWhiteSpace(s)) continue;
                            if (s.IndexOf("マイナ", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                isMina = true;
                                break;
                            }
                        }
                        catch { }
                    }

                    if (isMina)
                        dgvList.Rows[i].DefaultCellStyle.BackColor = Color.FromArgb(230, 255, 255);
                    else
                        dgvList.Rows[i].DefaultCellStyle.BackColor = Color.White;
                }
            }
            catch { }
        }

        private void TxtSearch_KeyUp(object sender, KeyEventArgs e)
        {
            ApplyFilter(txtSearch.Text);
        }

        private void ApplyFilter(string text)
        {
            if (dgvList.DataSource is DataTable dt)
            {
                var dv = dt.DefaultView;
                if (string.IsNullOrWhiteSpace(text)) dv.RowFilter = string.Empty;
                else
                {
                    // filter on カルテ番号 or 氏名
                    text = text.Replace("'", "''");
                    dv.RowFilter = $"[カルテ番号] LIKE '%{text}%' OR [氏名] LIKE '%{text}%'";
                }
            }
        }

        private void DgvList_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                if (dgvList.SelectedRows.Count == 0) return;
                var row = dgvList.SelectedRows[0];
                string carte = null;
                // prefer raw value if present (hidden) for exact matching
                try
                {
                    if (dgvList.Columns.Contains("カルテ番号_raw"))
                        carte = row.Cells["カルテ番号_raw"].Value?.ToString();
                }
                catch { }
                if (string.IsNullOrEmpty(carte))
                    carte = row.Cells[0].Value?.ToString();
                var name = row.Cells[1].Value?.ToString();
                var date = row.Cells[2].Value?.ToString();
                // try to get source row index if present
                int? srcIdx = null;
                try
                {
                    if (dgvList.Columns.Contains("source_row_index"))
                    {
                        var v = row.Cells["source_row_index"].Value;
                        if (v != null && v != DBNull.Value)
                        {
                            int t; if (int.TryParse(v.ToString(), out t)) srcIdx = t;
                        }
                    }
                }
                catch { }

                // Find the matching row in currentTable
                if (currentTable == null) return;

                DataRow found = null;
                // If we have a source_row_index mapping use it first
                if (srcIdx.HasValue && srcIdx.Value >= 0 && srcIdx.Value < currentTable.Rows.Count)
                {
                    found = currentTable.Rows[srcIdx.Value];
                }
                else
                {
                    // Prefer matching by カルテ番号 + 氏名
                    if (currentTable.Columns.Contains("カルテ番号") && currentTable.Columns.Contains("氏名"))
                    {
                        var rows = currentTable.Select($"Convert([カルテ番号], 'System.String') = '{carte.Replace("'","''")}' AND [氏名] = '{name.Replace("'","''")}'");
                        if (rows.Length > 0) found = rows[0];
                    }

                    // fallback: try matching by 氏名 + 処理実行日時
                    if (found == null && currentTable.Columns.Contains("氏名") && currentTable.Columns.Contains("処理実行日時"))
                    {
                        var rows = currentTable.Select($"[氏名] = '{name.Replace("'","''")}' AND [処理実行日時] = '{date.Replace("'","''")}'");
                        if (rows.Length > 0) found = rows[0];
                    }
                }

                // final fallback: first row
                if (found == null && currentTable.Rows.Count > 0) found = currentTable.Rows[0];

                if (found != null)
                {
                    foreach (var k in detailFields)
                    {
                        if (!detailBoxes.ContainsKey(k)) continue;

                        // Generated display fields for 証区分 / 適用区分
                        if (k == "証区分表示")
                        {
                            string origCol = null;
                            try { if (columnMap != null && columnMap.TryGetValue("限度額適用認定証区分", out var m)) origCol = m; } catch { }
                            if (string.IsNullOrEmpty(origCol)) origCol = "限度額適用認定証区分";
                            string raw = null;
                            if (currentTable.Columns.Contains(origCol) && found[origCol] != DBNull.Value) raw = found[origCol].ToString();
                            detailBoxes[k].Text = CommonFunctions.ConvertCertificateKindToDisplay(raw);
                            continue;
                        }

                        // Special display for 本人家族の別: 1 -> 本人, 2 -> 家族, otherwise show raw value (or empty)
                        if (k == "本人家族の別")
                        {
                            string colName = null;
                            try { if (columnMap != null && columnMap.TryGetValue(k, out var m)) colName = m; } catch { }
                            if (string.IsNullOrEmpty(colName)) colName = k;

                            string outv = "";
                            if (currentTable.Columns.Contains(colName) && found[colName] != DBNull.Value)
                            {
                                var raw = found[colName];
                                string s = raw?.ToString();
                                if (s == "1") outv = "本人";
                                else if (s == "2") outv = "家族";
                                else outv = s;
                            }
                            detailBoxes[k].Text = outv;
                            continue;
                        }

                        // Special display for 未就学区分: 1 -> 未就学, otherwise show raw value (or empty)
                        if (k == "未就学区分")
                        {
                            string colName = null;
                            try { if (columnMap != null && columnMap.TryGetValue(k, out var m)) colName = m; } catch { }
                            if (string.IsNullOrEmpty(colName)) colName = k;

                            string outv = "";
                            if (currentTable.Columns.Contains(colName) && found[colName] != DBNull.Value)
                            {
                                var raw = found[colName];
                                string s = raw?.ToString();
                                if (s == "1") outv = "未就学";
                                else outv = s;
                            }
                            detailBoxes[k].Text = outv;
                            continue;
                        }

                        if (k == "適用区分表示")
                        {
                            string origCol = null;
                            try { if (columnMap != null && columnMap.TryGetValue("限度額適用認定証適用区分", out var m)) origCol = m; } catch { }
                            if (string.IsNullOrEmpty(origCol)) origCol = "限度額適用認定証適用区分";
                            string raw = null;
                            if (currentTable.Columns.Contains(origCol) && found[origCol] != DBNull.Value) raw = found[origCol].ToString();
                            detailBoxes[k].Text = CommonFunctions.ConvertApplicationKindToDisplay(raw);
                            continue;
                        }

                        string actualCol = null;
                        try { if (columnMap != null && columnMap.TryGetValue(k, out var m)) actualCol = m; } catch { }
                        if (string.IsNullOrEmpty(actualCol)) actualCol = k;

                        if (currentTable.Columns.Contains(actualCol) && found[actualCol] != DBNull.Value)
                        {
                            detailBoxes[k].Text = found[actualCol].ToString();
                        }
                        else
                        {
                            detailBoxes[k].Text = "";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AddLogSafe("DgvList_SelectionChangedでエラー: " + ex.Message);
            }
        }

        // simple helper to log to main UI if available
        private void AddLogSafe(string msg)
        {
            try
            {
                if (this.InvokeRequired)
                {
                    this.BeginInvoke(new Action(() => AddLogSafe(msg)));
                    return;
                }

                // Use shared logging helper which updates UI (via UiLogCallback) and file
                try
                {
                    _ = CommonFunctions.AddLogAsync(msg);
                }
                catch
                {
                    // fallback to console when logging fails
                    Console.WriteLine(msg);
                }
            }
            catch { }
        }
    }
}
