using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace OQSDrug
{
    internal sealed class HistoryReportRow
    {
        public string[] Cells;
        public bool Heading;
        public bool FirstLine = true, LastLine = true;
        public HistoryReportRow(bool heading, params string[] cells) { Heading = heading; Cells = cells; }
    }

    internal sealed class HistoryReport
    {
        public string Title, Patient, Scope, Note;
        public string[] Columns;
        public float[] Widths;
        public List<HistoryReportRow> Rows = new List<HistoryReportRow>();
        internal static string Value(DataRow r, string key) => r.Table.Columns.Contains(key) && !r.IsNull(key) ? Convert.ToString(r[key]).Trim() : "";
        private static string Date(string value)
        {
            DateTime date;
            return DateTime.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
                ? date.ToString("yyyy/MM/dd") : value;
        }
        private static string SourceLabel(string source)
        {
            switch (source) { case "1": return "レセプト"; case "2": return "調剤情報"; case "3": return "処方情報"; default: return "情報源不明"; }
        }
        internal static string PatientCaption(string display)
        {
            display = (display ?? "").Trim();
            int separator = display.IndexOf(':');
            return separator >= 0 ? "患者名：" + display.Substring(separator + 1).Trim() + "　ID：" + display.Substring(0, separator).Trim()
                : "患者名：" + display;
        }
        internal static string OutputPeriod(DataTable data, string dateColumn, string fallbackColumn)
        {
            var dates = new List<string>();
            bool unknown = false;
            foreach (DataRow row in data.Rows)
            {
                string text = Value(row, dateColumn);
                if (text.Length == 0 && fallbackColumn != null) text = Value(row, fallbackColumn);
                DateTime date;
                if (DateTime.TryParseExact(text, new[] { "yyyyMMdd", "yyyy/MM/dd", "yyyy-MM-dd" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                    dates.Add(date.ToString("yyyy/MM/dd"));
                else if (DateTime.TryParseExact(text, new[] { "yyyyMM", "yyyy/MM" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                    dates.Add(date.ToString("yyyy/MM"));
                else unknown = true;
            }
            dates.Sort(StringComparer.Ordinal);
            return "出力期間：" + (dates.Count == 0 ? "日付不明" : dates[0] + " ～ " + dates[dates.Count - 1])
                + (unknown && dates.Count > 0 ? "（日付不明の記録を含む）" : "");
        }
        internal static List<HistoryReport> WithMetadata(List<HistoryReport> reports, string patient, string period)
        {
            foreach (var report in reports) { report.Patient = patient; report.Scope = period; }
            return reports;
        }
        internal static HistoryReport Drugs(DataTable data, string patient, string scope)
        {
            var report = new HistoryReport { Title = "処方歴原文", Patient = PatientCaption(patient), Scope = OutputPeriod(data, "didate", "metrmonth"), Note = "",
                Columns = new[] { "薬剤名／一般名／用法", "数量・日数等" }, Widths = new[] { .78f, .22f } };
            // Preserve every database row; do not sum quantities or remove duplicate-looking drugs.
            var groups = data.AsEnumerable().OrderByDescending(r => Value(r, "didate"), StringComparer.Ordinal)
                .ThenByDescending(r => Value(r, "prdate"), StringComparer.Ordinal)
                .ThenBy(r => r.Table.Columns.Contains("id") ? Convert.ToInt64(r["id"]) : 0)
                .GroupBy(r => new { Date = Value(r,"didate"), Prescription = Value(r,"prdate"), Month = Value(r,"metrmonth"),
                    InstitutionCode = Value(r,"metrdihcd"), Institution = Value(r,"metrdihnm"),
                    PrescriberCode = Value(r,"prlshcd"), Prescriber = Value(r,"prlshnm"),
                    InOut = Value(r,"inout"), Source = Value(r,"source"), Own = Value(r,"prisorg"), Origin = Value(r,"diorg") });
            foreach (var group in groups)
            {
                string date = group.Key.Date.Length > 0 ? Date(group.Key.Date) : group.Key.Month + "（日付不明）";
                string prescriber = group.Key.Prescriber.Length > 0 ? group.Key.Prescriber : group.Key.Institution;
                string pharmacy = group.Key.Prescriber.Length > 0 && group.Key.Prescriber != group.Key.Institution ? group.Key.Institution : "";
                report.Rows.Add(new HistoryReportRow(true, date + "　処方元：" + prescriber
                    + (group.Key.Prescription.Length > 0 ? "　処方日：" + Date(group.Key.Prescription) : "")
                    + (pharmacy.Length > 0 ? "　調剤薬局：" + pharmacy : "")
                    + "　情報源：" + SourceLabel(group.Key.Source)));
                foreach (DataRow row in group)
                {
                    string name = Value(row,"drugn");
                    if (Value(row,"ingren").Length > 0) name += "\n（" + Value(row,"ingren") + "）";
                    if (Value(row,"usagen").Length > 0) name += "\n用法：" + Value(row,"usagen");
                    report.Rows.Add(new HistoryReportRow(false, name,
                        Value(row,"qua1") + Value(row,"unit") + "\n日数／回数：" + Value(row,"times")));
                }
            }
            return report;
        }
        internal static List<HistoryReport> Pivot(string title, int fixedCount, int datesPerPage, params DataGridView[] grids)
        {
            var columns = grids.SelectMany(g => g.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible)
                .OrderBy(c => c.DisplayIndex)).ToList();
            var fixedColumns = columns.Take(fixedCount).ToList();
            var dates = columns.Skip(fixedCount).ToList();
            var reports = new List<HistoryReport>();
            if (grids.Length == 0 || grids[0].Rows.Count == 0 || columns.Count == 0) return reports;
            int bands = Math.Max(1, (dates.Count + datesPerPage - 1) / datesPerPage);
            for (int band = 0; band < bands; band++)
            {
                var selected = fixedColumns.Concat(dates.Skip(band * datesPerPage).Take(datesPerPage)).ToList();
                float fixedWidth = fixedColumns.Sum(c => (float)Math.Max(40, c.Width));
                var widths = selected.Select((c,i) => i < fixedCount
                    ? .55f * Math.Max(40, c.Width) / fixedWidth
                    : .45f / Math.Max(1, selected.Count - fixedCount)).ToArray();
                // Reserve more space for dosage instructions in the prescription pivot.
                if (fixedCount == 3 && fixedColumns.Any(c => c.Name == "dose" || c.DataPropertyName == "dose"))
                {
                    widths = selected.Select((c,i) => i < fixedCount
                        ? (c.Name == "hospital" || c.DataPropertyName == "hospital" ? .16f
                           : c.Name == "dose" || c.DataPropertyName == "dose" ? .29f : .25f)
                        : .30f / Math.Max(1, selected.Count - fixedCount)).ToArray();
                }
                float total = widths.Sum();
                var report = new HistoryReport { Title = title + (bands > 1 ? "（列 " + (band + 1) + "/" + bands + "）" : ""),
                    Patient = "", Scope = "", Note = "", Columns = selected.Select(c => c.HeaderText).ToArray(),
                    Widths = widths.Select(w => w / total).ToArray() };
                string institution = "";
                foreach (DataGridViewRow row in grids[0].Rows)
                {
                    if (row.IsNewRow || !row.Visible) continue;
                    var cells = selected.Select(c => Convert.ToString(c.DataGridView.Rows[row.Index].Cells[c.Index].FormattedValue)).ToArray();
                    int hospital = selected.FindIndex(c => c.DataPropertyName == "hospital" || c.Name == "hospital");
                    if (hospital >= 0)
                    {
                        string displayed = cells[hospital];
                        if (!string.IsNullOrWhiteSpace(displayed))
                        {
                            if (displayed == institution) cells[hospital] = "";
                            institution = displayed;
                        }
                        // Keep the grid's blank cells blank; never fill down repeated institutions.
                    }
                    report.Rows.Add(new HistoryReportRow(false, cells));
                }
                reports.Add(report);
            }
            return reports;
        }
    }

    internal sealed class HistoryReportView : UserControl
    {
        private readonly DataGridView grid = new DataGridView();
        internal event EventHandler ReportChanged;
        internal bool CanPrint => report != null && report.Rows.Count > 0;
        private readonly Label info = new Label { Dock = DockStyle.Top, Height = 66, Padding = new Padding(6), AutoEllipsis = true };
        private HistoryReport report;
        internal HistoryReportView()
        {
            Dock = DockStyle.Fill;
            Font = new Font("Meiryo UI", 9);
            grid.Dock = DockStyle.Fill; grid.ReadOnly = true; grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false; grid.RowHeadersVisible = false;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            grid.BackgroundColor = Color.White; grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            Controls.Add(grid); Controls.Add(info);
        }
        internal void ClearReport(string message = "データを読み込んでください。")
        {
            report = null; grid.Rows.Clear(); info.Text = message; info.Visible = true;
            ReportChanged?.Invoke(this, EventArgs.Empty);
        }
        internal void SetReport(HistoryReport value)
        {
            report = value; grid.Rows.Clear(); grid.Columns.Clear();
            for (int i = 0; i < value.Columns.Length; i++)
                grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = value.Columns[i], FillWeight = value.Widths[i] * 100,
                    SortMode = DataGridViewColumnSortMode.NotSortable });
            foreach (var row in value.Rows)
            {
                object[] cells = row.Heading ? new object[] { row.Cells[0], "" } : row.Cells.Cast<object>().ToArray();
                int index = grid.Rows.Add(cells);
                if (row.Heading) grid.Rows[index].DefaultCellStyle.BackColor = Color.FromArgb(229, 236, 242);
            }
            info.Text = value.Rows.Count == 0 ? "該当データはありません。" : "";
            info.Visible = value.Rows.Count == 0;
            ReportChanged?.Invoke(this, EventArgs.Empty);
        }
        internal void ShowPreview()
        {
            if (report == null || report.Rows.Count == 0) return;
            // The modal preview uses this immutable snapshot even if the underlying patient changes.
            ShowPreview(this, new List<HistoryReport> { report }, false);
        }
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr handle);

        private static Icon CreatePrintIcon()
        {
            var handle = Properties.Resources.Print.GetHicon();
            try
            {
                using (var icon = Icon.FromHandle(handle))
                    return (Icon)icon.Clone();
            }
            finally { DestroyIcon(handle); }
        }

        internal static void ShowPreview(IWin32Window owner, List<HistoryReport> reports, bool landscape)
        {
            if (reports.Count == 0 || !reports.Any(r => r.Rows.Count > 0)) return;
            var snapshot = reports[0];
            try
            {
                using (var document = new PrintDocument())
                using (var renderer = new HistoryReportPrintJob(reports))
                using (var icon = CreatePrintIcon())
                using (var window = new Form { Icon = icon, Text = snapshot.Title + " - 印刷プレビュー", Width = 1000, Height = 800, StartPosition = FormStartPosition.CenterParent })
                using (var control = new PrintPreviewControl { Dock = DockStyle.Fill, AutoZoom = true })
                {
                    document.DocumentName = snapshot.Title;
                    document.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);
                    document.DefaultPageSettings.Landscape = landscape;
                    document.DefaultPageSettings.Margins = new Margins(40, 40, 40, 40);
                    document.BeginPrint += (s, e) => renderer.Reset();
                    document.PrintPage += (s, e) => e.HasMorePages = renderer.DrawNext(e.Graphics, e.MarginBounds);
                    var bar = new ToolStrip();
                    var print = new ToolStripButton("プリンターを選んで印刷");
                    var previous = new ToolStripButton("前ページ"); var next = new ToolStripButton("次ページ");
                    previous.Click += (s,e) => control.StartPage = Math.Max(0, control.StartPage - 1);
                    next.Click += (s,e) => { if (control.StartPage + 1 < renderer.PageCount) control.StartPage++; };
                    print.Click += (s,e) =>
                    {
                        using (var dialog = new PrintDialog { Document = document, UseEXDialog = true, AllowSomePages = false })
                            if (dialog.ShowDialog(window) == DialogResult.OK)
                            {
                                try { document.Print(); }
                                catch (Exception ex) { MessageBox.Show(window, "印刷できませんでした：" + ex.Message); }
                            }
                    };
                    bar.Items.AddRange(new ToolStripItem[] { print, previous, next });
                    control.Document = document; window.Controls.Add(control); window.Controls.Add(bar);
                    window.ShowDialog(owner);
                    control.Document = null;
                }
            }
            catch (Exception ex) { MessageBox.Show(owner, "プレビューを表示できませんでした：" + ex.Message); }
        }
    }

    internal sealed class HistoryReportPrintJob : IDisposable
    {
        private readonly List<HistoryReportRenderer> renderers;
        private int section;
        internal HistoryReportPrintJob(List<HistoryReport> reports) { renderers = reports.Select(r => new HistoryReportRenderer(r)).ToList(); }
        internal int PageCount => renderers.Sum(r => r.PageCount);
        internal void Reset() { section = 0; foreach (var r in renderers) r.Reset(); }
        internal bool DrawNext(Graphics g, Rectangle bounds)
        {
            if (!renderers[section].DrawNext(g, bounds)) section++;
            return section < renderers.Count;
        }
        public void Dispose() { foreach (var r in renderers) r.Dispose(); }
    }

    // Layout uses measured, wrapped lines and paginates even when one cell exceeds a page.
    internal sealed class HistoryReportRenderer : IDisposable
    {
        private readonly HistoryReport report;
        private readonly Font body = new Font("Meiryo UI", 9);
        private readonly Font title = new Font("Meiryo UI", 14, FontStyle.Bold);
        private readonly StringFormat format = new StringFormat(StringFormat.GenericTypographic);
        private List<List<HistoryReportRow>> pages;
        private List<string> header;
        private List<HistoryReportRow> columnHeaders;
        private int page;
        private float lineHeight, top;
        public int PageCount => pages == null ? 0 : pages.Count;
        internal HistoryReportRenderer(HistoryReport report) { this.report = report; }
        internal void Reset() { page = 0; pages = null; }
        private List<string> Wrap(Graphics g, string text, float width)
        {
            var result = new List<string>();
            foreach (string paragraph in (text ?? "").Replace("\r", "").Split('\n'))
            {
                string line = "";
                var chars = StringInfo.GetTextElementEnumerator(paragraph);
                while (chars.MoveNext())
                {
                    string c = chars.GetTextElement();
                    if (line.Length > 0 && g.MeasureString(line + c, body, int.MaxValue, format).Width > width)
                    { result.Add(line); line = ""; }
                    line += c;
                }
                result.Add(line);
            }
            return result;
        }
        private void Layout(Graphics g, Rectangle bounds)
        {
            lineHeight = (float)Math.Ceiling(body.GetHeight(g)) + 7;
            string description = (report.Patient + "　" + report.Scope + "\n" + report.Note).Trim();
            header = description.Length == 0 ? new List<string>() : Wrap(g, description, bounds.Width - 8);
            // Preserve displayed date labels verbatim and keep every column heading on one line.
            columnHeaders = new List<HistoryReportRow> { new HistoryReportRow(false, report.Columns) };
            top = bounds.Top + 32 + (header.Count + columnHeaders.Count) * lineHeight;
            int capacity = (int)((bounds.Bottom - 28 - top) / lineHeight);
            if (capacity < 4) throw new InvalidOperationException("印刷領域が小さすぎます。用紙サイズと余白を確認してください。");
            pages = new List<List<HistoryReportRow>> { new List<HistoryReportRow>() };
            string group = "";
            foreach (var row in report.Rows)
            {
                var wrapped = row.Heading ? new[] { Wrap(g, row.Cells[0], bounds.Width - 8) }
                    : row.Cells.Select((c,i) => Wrap(g, c, bounds.Width * report.Widths[i] - 8)).ToArray();
                int lines = wrapped.Max(x => x.Count);
                var current = pages[pages.Count - 1];
                int needed = row.Heading ? Math.Min(capacity, lines + 1) : lines;
                if (current.Count > 0 && current.Count + needed > capacity)
                {
                    current = new List<HistoryReportRow>(); pages.Add(current);
                    if (!row.Heading && group.Length > 0)
                    {
                        foreach (string continuation in Wrap(g, group + "（続き）", bounds.Width - 8).Take(capacity - 2))
                            current.Add(new HistoryReportRow(true, continuation));
                    }
                }
                if (row.Heading) group = row.Cells[0];
                for (int i = 0; i < lines; i++)
                {
                    if (current.Count >= capacity) { current = new List<HistoryReportRow>(); pages.Add(current); }
                    current.Add(new HistoryReportRow(row.Heading, wrapped.Select(w => i < w.Count ? w[i] : "").ToArray())
                    { FirstLine = i == 0, LastLine = i == lines - 1 });
                }
            }
        }
        internal bool DrawNext(Graphics g, Rectangle bounds)
        {
            if (pages == null) Layout(g, bounds);
            g.DrawString(report.Title, title, Brushes.Black, bounds.Left, bounds.Top);
            float y = bounds.Top + 32;
            foreach (string line in header) { g.DrawString(line, body, Brushes.Black, bounds.Left + 4, y, format); y += lineHeight; }
            foreach (var heading in columnHeaders) { DrawLine(g, heading, bounds.Left, y, bounds.Width, true, true); y += lineHeight; }
            y = top;
            g.DrawLine(Pens.LightGray, bounds.Left, y, bounds.Right, y);
            foreach (var line in pages[page]) { DrawLine(g, line, bounds.Left, y, bounds.Width, line.Heading); y += lineHeight; }
            g.DrawLine(Pens.LightGray, bounds.Left, y, bounds.Right, y);
            g.DrawString("作成日：" + DateTime.Today.ToString("yyyy/MM/dd") + "　" + (page + 1) + " / " + pages.Count + " ページ",
                body, Brushes.Black, bounds.Left, bounds.Bottom - 20, format);
            page++;
            return page < pages.Count;
        }
        private void DrawLine(Graphics g, HistoryReportRow row, float x, float y, float width, bool shaded, bool fitHeading = false)
        {
            if (shaded) g.FillRectangle(Brushes.Gainsboro, x, y, width, lineHeight);
            for (int i = 0; i < row.Cells.Length; i++)
            {
                float cellWidth = row.Heading ? width : width * report.Widths[i];
                g.DrawLine(Pens.LightGray, x, y, x, y + lineHeight);
                g.DrawLine(Pens.LightGray, x + cellWidth, y, x + cellWidth, y + lineHeight);
                if (row.FirstLine) g.DrawLine(Pens.LightGray, x, y, x + cellWidth, y);
                if (row.LastLine) g.DrawLine(Pens.LightGray, x, y + lineHeight, x + cellWidth, y + lineHeight);
                if (fitHeading)
                {
                    float measured = g.MeasureString(row.Cells[i], body, int.MaxValue, format).Width;
                    float scale = Math.Min(1f, Math.Max(1f, cellWidth - 8) / Math.Max(1f, measured));
                    using (var headingFont = new Font(body.FontFamily, body.Size * scale, body.Style))
                        g.DrawString(row.Cells[i], headingFont, Brushes.Black, x + 4, y + 2, format);
                }
                else g.DrawString(row.Cells[i], body, Brushes.Black, x + 4, y + 2, format);
                x += cellWidth;
            }
        }
        public void Dispose() { body.Dispose(); title.Dispose(); format.Dispose(); }
    }
}
