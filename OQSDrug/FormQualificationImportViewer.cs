using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OQSDrug
{
    internal partial class FormQualificationImportViewer : Form
    {
        private readonly QualificationImportSession session;
        private readonly Func<IReadOnlyList<ImportedQualificationRecord>, Task<QualificationSendSummary>> sendAsync;
        private readonly BindingList<QualificationImportRow> rows = new BindingList<QualificationImportRow>();

        public FormQualificationImportViewer(
            QualificationImportSession session,
            Func<IReadOnlyList<ImportedQualificationRecord>, Task<QualificationSendSummary>> sendAsync)
        {
            InitializeComponent();
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.sendAsync = sendAsync ?? throw new ArgumentNullException(nameof(sendAsync));
            InitializeGrid();
            ApplyVisualStyle();
        }

        private void FormQualificationImportViewer_Load(object sender, EventArgs e)
        {
            labelSummary.Text = $"{ImportedQualificationRecord.GetKindDisplayName(session.Kind)} 取込結果: {session.Records.Count}件";

            rows.Clear();
            foreach (ImportedQualificationRecord record in session.Records)
            {
                rows.Add(new QualificationImportRow(record));
            }

            dgvQualifications.DataSource = rows;
            UpdateSelectionCount();
            if (rows.Count > 0)
            {
                dgvQualifications.Rows[0].Selected = true;
                UpdateDetail();
            }
        }

        private void InitializeGrid()
        {
            dgvQualifications.AutoGenerateColumns = false;
            dgvQualifications.Columns.Clear();
            dgvQualifications.Columns.Add(new DataGridViewCheckBoxColumn
            {
                DataPropertyName = nameof(QualificationImportRow.Send),
                HeaderText = "送信",
                Width = 48
            });
            dgvQualifications.Columns.Add(CreateTextColumn(nameof(QualificationImportRow.Name), "氏名", 120));
            dgvQualifications.Columns.Add(CreateTextColumn(nameof(QualificationImportRow.BirthDate), "生年月日", 90));
            dgvQualifications.Columns.Add(CreateTextColumn(nameof(QualificationImportRow.Insurance), "保険情報", 220));
            dgvQualifications.Columns.Add(CreateTextColumn(nameof(QualificationImportRow.Validity), "資格", 90));
            dgvQualifications.Columns.Add(CreateTextColumn(nameof(QualificationImportRow.MatchStatus), "照合", 90));
            dgvQualifications.Columns.Add(CreateTextColumn(nameof(QualificationImportRow.PatientIdDisplay), "カルテ", 80));
            dgvQualifications.Columns.Add(CreateTextColumn(nameof(QualificationImportRow.SendStatus), "送信結果", 150));

            dgvQualifications.DataSource = rows;
            dgvQualifications.ReadOnly = false;
            dgvQualifications.EnableHeadersVisualStyles = false;
            dgvQualifications.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(226, 232, 240);
            dgvQualifications.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(15, 23, 42);
            dgvQualifications.ColumnHeadersDefaultCellStyle.Font = new Font("Meiryo UI", 9F, FontStyle.Bold);
            dgvQualifications.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            dgvQualifications.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            dgvQualifications.RowsDefaultCellStyle.BackColor = Color.White;
            dgvQualifications.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgvQualifications.BackgroundColor = Color.White;
            dgvQualifications.BorderStyle = BorderStyle.None;
            dgvQualifications.GridColor = Color.FromArgb(226, 232, 240);
        }

        private void ApplyVisualStyle()
        {
            Font = new Font("Meiryo UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 128);
            BackColor = Color.FromArgb(245, 247, 250);
            panelTop.BackColor = Color.FromArgb(15, 23, 42);
            splitContainerMain.BackColor = Color.FromArgb(245, 247, 250);
            splitContainerMain.Panel1.BackColor = Color.White;
            splitContainerMain.Panel2.BackColor = Color.FromArgb(248, 250, 252);

            labelSummary.ForeColor = Color.White;
            labelSummary.Font = new Font("Meiryo UI", 10F, FontStyle.Bold, GraphicsUnit.Point, 128);
            labelSelectedCount.ForeColor = Color.FromArgb(226, 232, 240);

            textBoxDetail.BackColor = Color.FromArgb(248, 250, 252);
            textBoxDetail.BorderStyle = BorderStyle.None;
            textBoxDetail.Font = new Font("Meiryo UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 128);

            ApplyButtonStyle(buttonCheckAll, Color.FromArgb(226, 232, 240), Color.FromArgb(15, 23, 42));
            ApplyButtonStyle(buttonClearChecks, Color.FromArgb(226, 232, 240), Color.FromArgb(15, 23, 42));
            ApplyButtonStyle(buttonSendSelected, Color.FromArgb(249, 115, 22), Color.White);
            ApplyButtonStyle(buttonClose, Color.FromArgb(226, 232, 240), Color.FromArgb(15, 23, 42));
        }

        private static void ApplyButtonStyle(Button button, Color backColor, Color foreColor)
        {
            button.BackColor = backColor;
            button.ForeColor = foreColor;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Cursor = Cursors.Hand;
        }

        private static DataGridViewTextBoxColumn CreateTextColumn(string propertyName, string header, int width)
        {
            return new DataGridViewTextBoxColumn
            {
                DataPropertyName = propertyName,
                HeaderText = header,
                Width = width,
                ReadOnly = true
            };
        }

        private void buttonCheckAll_Click(object sender, EventArgs e)
        {
            foreach (QualificationImportRow row in rows)
            {
                if (!row.Record.IsSent)
                {
                    row.Send = true;
                }
            }

            dgvQualifications.Refresh();
            UpdateSelectionCount();
        }

        private void buttonClearChecks_Click(object sender, EventArgs e)
        {
            foreach (QualificationImportRow row in rows)
            {
                row.Send = false;
            }

            dgvQualifications.Refresh();
            UpdateSelectionCount();
        }

        private async void buttonSendSelected_Click(object sender, EventArgs e)
        {
            List<QualificationImportRow> selectedRows = rows.Where(r => r.Send && !r.Record.IsSent).ToList();
            if (selectedRows.Count == 0)
            {
                MessageBox.Show(this, "送信対象にチェックを入れてください。", "資格情報送信", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                ToggleButtons(false);
                QualificationSendSummary summary = await sendAsync(selectedRows.Select(r => r.Record).ToList());

                foreach (QualificationImportRow row in selectedRows)
                {
                    row.Send = false;
                    row.RefreshFromRecord();
                }

                dgvQualifications.Refresh();
                UpdateSelectionCount();
                UpdateDetail();

                MessageBox.Show(
                    this,
                    $"送信成功: {summary.SentCount}\r\n送信失敗: {summary.FailedCount}",
                    "資格情報送信",
                    MessageBoxButtons.OK,
                    summary.FailedCount == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            finally
            {
                ToggleButtons(true);
            }
        }

        private void ToggleButtons(bool enabled)
        {
            buttonCheckAll.Enabled = enabled;
            buttonClearChecks.Enabled = enabled;
            buttonSendSelected.Enabled = enabled;
            buttonClose.Enabled = enabled;
        }

        private void dgvQualifications_SelectionChanged(object sender, EventArgs e)
        {
            UpdateDetail();
        }

        private void dgvQualifications_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dgvQualifications.IsCurrentCellDirty)
            {
                dgvQualifications.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void dgvQualifications_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                UpdateSelectionCount();
            }
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void UpdateSelectionCount()
        {
            int selectedCount = rows.Count(r => r.Send && !r.Record.IsSent);
            labelSelectedCount.Text = $"選択: {selectedCount}件";
        }

        private void UpdateDetail()
        {
            if (dgvQualifications.CurrentRow == null)
            {
                textBoxDetail.Text = string.Empty;
                return;
            }

            var row = dgvQualifications.CurrentRow.DataBoundItem as QualificationImportRow;
            textBoxDetail.Text = row == null ? string.Empty : row.Record.DetailText();
        }

        private sealed class QualificationImportRow : INotifyPropertyChanged
        {
            private bool send;
            private string matchStatus;
            private string sendStatus;

            public QualificationImportRow(ImportedQualificationRecord record)
            {
                Record = record;
                matchStatus = record.MatchStatus;
                sendStatus = record.LastSendMessage;
            }

            public ImportedQualificationRecord Record { get; }
            public bool Send
            {
                get { return send; }
                set
                {
                    if (send == value)
                    {
                        return;
                    }

                    send = value;
                    OnPropertyChanged(nameof(Send));
                }
            }

            public string Name => Record.Name;
            public string BirthDate => Record.BirthDate;
            public string Insurance => $"{Record.InsurerNumber} / {Record.Symbol}-{Record.Number}-{Record.BranchNumber}".Trim();
            public string Validity => Record.QualificationValidity;
            public string MatchStatus
            {
                get { return matchStatus; }
                private set
                {
                    matchStatus = value;
                    OnPropertyChanged(nameof(MatchStatus));
                }
            }

            public string PatientIdDisplay => Record.MatchedPatientId > 0 ? Record.MatchedPatientId.ToString() : string.Empty;
            public string SendStatus
            {
                get { return sendStatus; }
                private set
                {
                    sendStatus = value;
                    OnPropertyChanged(nameof(SendStatus));
                }
            }

            public void RefreshFromRecord()
            {
                MatchStatus = Record.MatchStatus;
                SendStatus = Record.LastSendMessage;
                OnPropertyChanged(nameof(PatientIdDisplay));
            }

            public event PropertyChangedEventHandler PropertyChanged;

            private void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
