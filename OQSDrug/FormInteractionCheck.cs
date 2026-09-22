using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace OQSDrug
{
    internal sealed class FormInteractionCheck : Form
    {
        private readonly Label status = new Label { Dock = DockStyle.Bottom, Height = 68, Padding = new Padding(8) };
        private readonly TabControl tabs = new TabControl { Dock = DockStyle.Fill };
        private readonly DataGridView medications = Grid(), koro = Grid(), text = Grid();
        private readonly ToolStripComboBox months = new ToolStripComboBox { DropDownStyle = ComboBoxStyle.DropDownList, AutoSize = false, Width = 65 };
        private readonly ToolStripButton reload = new ToolStripButton("再取得・チェック");
        internal event EventHandler CheckRequested;
        internal event EventHandler ResultStateChanged;
        internal int BadgeIndex { get; private set; }
        internal string BadgeText { get; private set; } = "未チェック";
        internal int Months => months.SelectedIndex == 0 ? 3 : months.SelectedIndex == 2 ? 12 : 6;
        internal void SetMonths(int value) { months.SelectedIndex = value == 3 ? 0 : value == 12 ? 2 : 1; }
        internal FormInteractionCheck()
        {
            Text = "相互作用チェック"; Size = new Size(1100, 650); MinimumSize = new Size(780, 420);
            StartPosition = FormStartPosition.CenterParent;
            months.Items.AddRange(new object[] { "3か月", "6か月", "12か月" }); months.SelectedIndex = 1;
            var bar = new ToolStrip();
            bar.Items.Add(new ToolStripLabel("他院処方の対象期間")); bar.Items.Add(months); bar.Items.Add(reload);
            reload.Click += (s, e) => CheckRequested?.Invoke(this, EventArgs.Empty);
            months.SelectedIndexChanged += (s, e) => { SetState("期間変更・再取得してチェックしてください", true); };
            AddTab("カルテ入力一覧", medications); AddTab("KOROコード照合", koro); AddTab("文字列照合（候補）", text);
            medications.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            Controls.Add(tabs); Controls.Add(status); Controls.Add(bar);
            SetState("再取得して現在の受診を確認してください", true);
        }
        private static DataGridView Grid() => new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
            AllowUserToDeleteRows = false, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
            DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True } };
        private void AddTab(string title, Control control) { var page = new TabPage(title); page.Controls.Add(control); tabs.TabPages.Add(page); }
        internal void SetBusy(bool busy) { reload.Enabled = !busy; months.Enabled = !busy; }
        internal void SetState(string message, bool clearResults)
        {
            string value = message + "\r\n過去の他院処方との照合です。現在の服用は未確認。該当なしは安全性の保証ではありません。";
            if (status.Text != value) status.Text = value;
            if (clearResults)
            {
                koro.DataSource = null; text.DataSource = null;
                SetBadge(0, message == "チェック中" ? "チェック中" : "未チェック");
            }
        }
        private void SetBadge(int index, string label)
        {
            BadgeIndex = index; BadgeText = label;
            status.BackColor = index == 1 ? Color.Honeydew : index == 2 ? Color.MistyRose : SystemColors.Control;
            ResultStateChanged?.Invoke(this, EventArgs.Empty);
        }
        internal void ClearSnapshot() { medications.DataSource = null; }
        internal void ShowSnapshot(ChartMedicationSnapshot snapshot)
        {
            var table = new DataTable();
            foreach (var name in new[] { "薬コード", "薬名", "レセ電コード", "YJコード", "扱い" }) table.Columns.Add(name);
            foreach (var m in snapshot.Medications.Where(m => m.IsDrug))
                table.Rows.Add(m.InternalCode, m.Name, m.ReceptCode, m.YjCode,
                    string.IsNullOrEmpty(m.ReceptCode) ? "コード未取得・名称照合のみ" : "照合対象");
            medications.DataSource = table;
            foreach (DataGridViewColumn column in medications.Columns)
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            medications.Columns["扱い"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            medications.Columns["扱い"].Width = 110;
            medications.Columns["薬名"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            medications.Columns["薬名"].MinimumWidth = 100;
        }
        internal void ShowResult(ChartMedicationSnapshot snapshot, MedicationInteractionResult result, bool selectResult)
        {
            ShowSnapshot(snapshot); koro.DataSource = HitTable(result.Koro); text.DataSource = HitTable(result.Text);
            SetState("チェック完了: KORO " + result.Koro.Count + "件 / 文字列 " + result.Text.Count + "件。 " + result.Status, false);
            int count = result.Koro.Count + result.Text.Count;
            SetBadge(count > 0 ? 2 : 1, count > 0 ? "要確認 " + count + "件" : "該当なし");
            if (selectResult) tabs.SelectedIndex = result.Koro.Count > 0 ? 1 : result.Text.Count > 0 ? 2 : 0;
        }
        private static DataTable HitTable(IEnumerable<MedicationInteractionHit> hits)
        {
            var table = new DataTable();
            foreach (var name in new[] { "区分", "今回の薬", "他院薬", "最終処方日", "医療機関", "根拠", "記載・対応", "機序" }) table.Columns.Add(name);
            foreach (var h in hits.OrderBy(h => h.Section.Contains("禁忌") ? 0 : 1))
                table.Rows.Add(h.Section, h.Current, h.History, h.Latest, h.Institution, h.Evidence, h.Text, h.Mechanism);
            return table;
        }
    }
}
