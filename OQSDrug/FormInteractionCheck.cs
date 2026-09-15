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
        private readonly Label patient = new Label { Dock = DockStyle.Top, Height = 34, TextAlign = ContentAlignment.MiddleLeft };
        private readonly Label status = new Label { Dock = DockStyle.Bottom, Height = 68, Padding = new Padding(8) };
        private readonly TabControl tabs = new TabControl { Dock = DockStyle.Fill };
        private readonly DataGridView medications = Grid(), koro = Grid(), text = Grid();
        private readonly ToolStripComboBox months = new ToolStripComboBox { DropDownStyle = ComboBoxStyle.DropDownList, AutoSize = false, Width = 65 };
        private readonly ToolStripButton reload = new ToolStripButton("再取得・チェック");
        internal event EventHandler CheckRequested;
        internal int Months => months.SelectedIndex == 0 ? 3 : 6;
        internal FormInteractionCheck()
        {
            Text = "相互作用チェック"; Size = new Size(1100, 650); MinimumSize = new Size(780, 420);
            StartPosition = FormStartPosition.CenterParent;
            months.Items.AddRange(new object[] { "3か月", "6か月" }); months.SelectedIndex = 1;
            var bar = new ToolStrip();
            bar.Items.Add(new ToolStripLabel("他院処方の対象期間")); bar.Items.Add(months); bar.Items.Add(reload);
            reload.Click += (s, e) => CheckRequested?.Invoke(this, EventArgs.Empty);
            months.SelectedIndexChanged += (s, e) => { SetState("期間変更・再取得してチェックしてください", true); };
            AddTab("カルテ入力一覧", medications); AddTab("KOROコード照合", koro); AddTab("文字列照合（候補）", text);
            Controls.Add(tabs); Controls.Add(status); Controls.Add(patient); Controls.Add(bar);
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
            status.Text = message + "\r\n過去の他院処方との照合です。現在の服用は未確認。該当なしは安全性の保証ではありません。";
            if (clearResults) { koro.DataSource = null; text.DataSource = null; }
        }
        internal void ShowSnapshot(ChartMedicationSnapshot snapshot)
        {
            patient.Text = "患者番号 " + snapshot.ChartId / 10 + "（枝番 " + snapshot.ChartId % 10 + "） / 受診 " + snapshot.Visit
                + " / " + snapshot.VisitDate.ToString("yyyy/MM/dd") + " / " + snapshot.PatientName;
            var table = new DataTable();
            foreach (var name in new[] { "順番", "薬コード", "薬名", "数量", "レセ電コード", "YJコード", "扱い" }) table.Columns.Add(name);
            foreach (var m in snapshot.Medications)
                table.Rows.Add(m.Order, m.InternalCode, m.Name, m.Quantity, m.ReceptCode, m.YjCode,
                    m.IsConfirmation ? "確定検知用・対象外" : string.IsNullOrEmpty(m.ReceptCode) ? "コード未取得・名称照合のみ" : "照合対象");
            medications.DataSource = table;
        }
        internal void ShowResult(ChartMedicationSnapshot snapshot, MedicationInteractionResult result, bool selectResult)
        {
            ShowSnapshot(snapshot); koro.DataSource = HitTable(result.Koro); text.DataSource = HitTable(result.Text);
            SetState("チェック完了: KORO " + result.Koro.Count + "件 / 文字列 " + result.Text.Count + "件。 " + result.Status, false);
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
