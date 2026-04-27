using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OQSDrug
{
    internal partial class FormBulkExecutionStatus : Form
    {
        private readonly BindingList<BulkExecutionResultRow> rows = new BindingList<BulkExecutionResultRow>();
        private readonly HashSet<BulkQualificationKind> runningKinds = new HashSet<BulkQualificationKind>();
        private readonly Dictionary<BulkQualificationKind, Label[]> progressStepLabels = new Dictionary<BulkQualificationKind, Label[]>();
        private readonly Dictionary<BulkQualificationKind, int> activeProgressSteps = new Dictionary<BulkQualificationKind, int>();
        private readonly Dictionary<BulkQualificationKind, int> completedProgressSteps = new Dictionary<BulkQualificationKind, int>();
        private readonly Timer progressBlinkTimer = new Timer();
        private bool progressBlinkOn = true;
        private bool bulkExecutionAvailable = true;
        private string bulkExecutionDisabledReason = string.Empty;
        private bool detailLogExpanded = false;

        private Func<Task> runHoumonOnceAsync;
        private Func<Task> toggleHoumonAutoAsync;
        private Func<Task> cancelHoumonAsync;
        private Func<bool> isHoumonAutoEnabled;

        private Func<Task> runOnlineOnceAsync;
        private Func<Task> toggleOnlineAutoAsync;
        private Func<Task> cancelOnlineAsync;
        private Func<bool> isOnlineAutoEnabled;

        private Func<Task> runMedicalOnceAsync;
        private Func<Task> toggleMedicalAutoAsync;
        private Func<Task> cancelMedicalAsync;
        private Func<bool> isMedicalAutoEnabled;
        private Func<Task> refreshLatestAsync;
        private Func<IReadOnlyList<ImportedQualificationRecord>, Task<QualificationSendSummary>> sendSelectedAsync;

        public FormBulkExecutionStatus()
        {
            InitializeComponent();
            components?.Add(progressBlinkTimer);
            InitializeCardStepIndicators();
            ResetRangeControlsFromSettings();
            InitializeGrid();
            ApplyVisualStyle();
            InitializeCardProgress();
            ApplyDetailLogExpandedState(false);
            progressBlinkTimer.Interval = 500;
            progressBlinkTimer.Tick += progressBlinkTimer_Tick;
            UpdateActionState();
        }

        public void SetBulkExecutionAvailability(bool available, string reason)
        {
            bulkExecutionAvailable = available;
            bulkExecutionDisabledReason = reason ?? string.Empty;
            UpdateExecutionWarning();
            UpdateActionState();
        }

        internal event EventHandler<ImportedQualificationRecordEventArgs> ResultDetailRequested;

        public QualificationImportSession CurrentSession { get; private set; }

        public BulkAutoExecutionOptions GetExecutionOptions(BulkQualificationKind kind)
        {
            BulkAutoExecutionOptions options = BulkAutoExecutionOptions.FromSettings(kind);

            switch (kind)
            {
                case BulkQualificationKind.Houmon:
                    options.UseConsentDates = true;
                    options.DateFromOverride = dateTimePickerHoumonFrom.Value.Date;
                    options.DateToOverride = dateTimePickerHoumonTo.Value.Date;
                    break;
                case BulkQualificationKind.Online:
                    options.UseConsentDates = checkBoxOnlineUseConsent.Checked;
                    options.DateFromOverride = dateTimePickerOnlineFrom.Value.Date;
                    options.DateToOverride = dateTimePickerOnlineTo.Value.Date;
                    break;
                case BulkQualificationKind.MedicalAid:
                    options.MedicalTreatmentMonthOverride = new DateTime(
                        (int)numericUpDownMedicalYear.Value,
                        (int)numericUpDownMedicalMonth.Value,
                        1);
                    break;
            }

            return options;
        }

        public void ConfigureActions(
            Func<Task> runHoumonOnceAsync,
            Func<Task> toggleHoumonAutoAsync,
            Func<Task> cancelHoumonAsync,
            Func<bool> isHoumonAutoEnabled,
            Func<Task> runOnlineOnceAsync,
            Func<Task> toggleOnlineAutoAsync,
            Func<Task> cancelOnlineAsync,
            Func<bool> isOnlineAutoEnabled,
            Func<Task> runMedicalOnceAsync,
            Func<Task> toggleMedicalAutoAsync,
            Func<Task> cancelMedicalAsync,
            Func<bool> isMedicalAutoEnabled,
            Func<Task> refreshLatestAsync,
            Func<IReadOnlyList<ImportedQualificationRecord>, Task<QualificationSendSummary>> sendSelectedAsync)
        {
            this.runHoumonOnceAsync = runHoumonOnceAsync;
            this.toggleHoumonAutoAsync = toggleHoumonAutoAsync;
            this.cancelHoumonAsync = cancelHoumonAsync;
            this.isHoumonAutoEnabled = isHoumonAutoEnabled;

            this.runOnlineOnceAsync = runOnlineOnceAsync;
            this.toggleOnlineAutoAsync = toggleOnlineAutoAsync;
            this.cancelOnlineAsync = cancelOnlineAsync;
            this.isOnlineAutoEnabled = isOnlineAutoEnabled;

            this.runMedicalOnceAsync = runMedicalOnceAsync;
            this.toggleMedicalAutoAsync = toggleMedicalAutoAsync;
            this.cancelMedicalAsync = cancelMedicalAsync;
            this.isMedicalAutoEnabled = isMedicalAutoEnabled;
            this.refreshLatestAsync = refreshLatestAsync;
            this.sendSelectedAsync = sendSelectedAsync;

            UpdateActionState();
        }

        public void InitializeForProcess(string processName)
        {
            if (!string.IsNullOrWhiteSpace(processName))
            {
                labelProcessName.Text = processName;
                Text = processName + " ステータス";
            }

            labelStatus.Text = "待機中";
            labelDetail.Text = "Bulk 実行の開始待ちです。";
            labelReceptionNumber.Text = "受付番号: -";
            labelResultCount.Text = "取得件数: 0";
            SetStatusTone(Color.FromArgb(239, 246, 255), Color.FromArgb(30, 64, 175));
            UpdateActionState();
        }

        public void ResetDisplay(string processName)
        {
            CurrentSession = null;
            listBoxLog.Items.Clear();
            InitializeForProcess(processName);
            SetActionButtonsEnabled(true);
        }

        public void ApplyWindowMode(BulkStatusWindowMode mode)
        {
            switch (mode)
            {
                case BulkStatusWindowMode.Minimized:
                    WindowState = FormWindowState.Minimized;
                    break;
                case BulkStatusWindowMode.Normal:
                    if (WindowState == FormWindowState.Minimized)
                    {
                        WindowState = FormWindowState.Normal;
                    }
                    break;
            }
        }

        public void AppendProgress(BulkExecutionProgressInfo info)
        {
            if (info == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(info.StatusText)
                && info.StatusText.IndexOf("一時フォルダ削除", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(info.ProcessName))
            {
                labelProcessName.Text = info.ProcessName;
                Text = info.ProcessName + " ステータス";
            }

            labelStatus.Text = string.IsNullOrWhiteSpace(info.StatusText) ? "処理中" : info.StatusText;
            labelDetail.Text = string.IsNullOrWhiteSpace(info.DetailText) ? "詳細情報はありません。" : info.DetailText;
            labelReceptionNumber.Text = string.IsNullOrWhiteSpace(info.ReceptionNumber)
                ? "受付番号: -"
                : "受付番号: " + info.ReceptionNumber;

            if (info.ImportedCount >= 0)
            {
                labelResultCount.Text = "取得件数: " + info.ImportedCount.ToString();
            }

            if (info.HasError)
            {
                SetStatusTone(Color.FromArgb(254, 242, 242), Color.FromArgb(185, 28, 28));
            }
            else if (info.IsCompleted)
            {
                SetStatusTone(Color.FromArgb(240, 253, 244), Color.FromArgb(21, 128, 61));
            }
            else
            {
                SetStatusTone(Color.FromArgb(255, 251, 235), Color.FromArgb(180, 83, 9));
            }

            UpdateCardProgress(info);

            string logLine = $"{info.Timestamp:HH:mm:ss} {info.StatusText}";
            if (!string.IsNullOrWhiteSpace(info.DetailText))
            {
                logLine += " - " + info.DetailText;
            }

            listBoxLog.Items.Add(logLine);
            listBoxLog.TopIndex = Math.Max(0, listBoxLog.Items.Count - 1);
        }

        public void SetResults(QualificationImportSession session)
        {
            CurrentSession = session;
            rows.Clear();

            if (session != null)
            {
                foreach (ImportedQualificationRecord record in session.Records.OrderByDescending(r => r.ImportedAt))
                {
                    rows.Add(new BulkExecutionResultRow(record));
                }

                labelResultCount.Text = "取得件数: " + session.Records.Count.ToString();
            }
            else
            {
                labelResultCount.Text = "取得件数: 0";
            }

            SetActionButtonsEnabled(true);
        }

        private void ApplyVisualStyle()
        {
            BackColor = Color.FromArgb(245, 247, 250);
            panelHeader.BackColor = Color.FromArgb(15, 23, 42);
            panelActionSection.BackColor = Color.FromArgb(245, 247, 250);
            panelExecutionWarning.BackColor = Color.FromArgb(254, 243, 199);
            labelExecutionWarning.ForeColor = Color.FromArgb(146, 64, 14);
            panelStatusCard.BackColor = Color.FromArgb(239, 246, 255);
            panelHoumonCard.BackColor = Color.White;
            panelOnlineCard.BackColor = Color.White;
            panelMedicalCard.BackColor = Color.White;
            if (splitContainerTop != null)
            {
                splitContainerTop.Panel1Collapsed = true;
                splitContainerTop.IsSplitterFixed = true;
            }
            if (splitContainerMain != null)
            {
                splitContainerMain.Panel1Collapsed = true;
                splitContainerMain.IsSplitterFixed = true;
            }

            listBoxLog.BackColor = Color.FromArgb(248, 250, 252);
            listBoxLog.BorderStyle = BorderStyle.None;
            dgvResults.BackgroundColor = Color.White;
            dgvResults.BorderStyle = BorderStyle.None;
            dgvResults.GridColor = Color.FromArgb(226, 232, 240);

            ApplyButtonStyle(buttonRunHoumonOnce, Color.FromArgb(37, 99, 235), Color.White);
            ApplyButtonStyle(buttonToggleHoumonAuto, Color.FromArgb(14, 116, 144), Color.White);
            ApplyButtonStyle(buttonCancelHoumon, Color.FromArgb(220, 38, 38), Color.White);

            ApplyButtonStyle(buttonRunOnlineOnce, Color.FromArgb(2, 132, 199), Color.White);
            ApplyButtonStyle(buttonToggleOnlineAuto, Color.FromArgb(8, 145, 178), Color.White);
            ApplyButtonStyle(buttonCancelOnline, Color.FromArgb(220, 38, 38), Color.White);

            ApplyButtonStyle(buttonRunMedicalOnce, Color.FromArgb(22, 163, 74), Color.White);
            ApplyButtonStyle(buttonToggleMedicalAuto, Color.FromArgb(21, 128, 61), Color.White);
            ApplyButtonStyle(buttonCancelMedical, Color.FromArgb(220, 38, 38), Color.White);

            ApplyButtonStyle(buttonCheckAll, Color.FromArgb(226, 232, 240), Color.FromArgb(15, 23, 42));
            ApplyButtonStyle(buttonClearChecks, Color.FromArgb(226, 232, 240), Color.FromArgb(15, 23, 42));
            ApplyButtonStyle(buttonSendSelected, Color.FromArgb(249, 115, 22), Color.White);
            ApplyButtonStyle(buttonRefreshResults, Color.FromArgb(37, 99, 235), Color.White);
            ApplyButtonStyle(buttonToggleLog, Color.FromArgb(226, 232, 240), Color.FromArgb(15, 23, 42));
            ApplyButtonStyle(buttonClose, Color.FromArgb(226, 232, 240), Color.FromArgb(15, 23, 42));
            UpdateExecutionWarning();
        }

        private void UpdateExecutionWarning()
        {
            if (panelExecutionWarning == null || labelExecutionWarning == null)
            {
                return;
            }

            bool showWarning = !bulkExecutionAvailable && !string.IsNullOrWhiteSpace(bulkExecutionDisabledReason);
            labelExecutionWarning.Text = bulkExecutionDisabledReason;
            panelExecutionWarning.Visible = showWarning;
        }

        private void ResetRangeControlsFromSettings()
        {
            DateTime today = DateTime.Today;

            BulkAutoExecutionOptions houmon = BulkAutoExecutionOptions.FromSettings(BulkQualificationKind.Houmon);
            dateTimePickerHoumonFrom.Value = houmon.GetDateFrom(today);
            dateTimePickerHoumonTo.Value = houmon.GetDateTo(today);

            BulkAutoExecutionOptions online = BulkAutoExecutionOptions.FromSettings(BulkQualificationKind.Online);
            checkBoxOnlineUseConsent.Checked = online.UseConsentDates;
            dateTimePickerOnlineFrom.Value = online.GetDateFrom(today);
            dateTimePickerOnlineTo.Value = online.GetDateTo(today);
            UpdateOnlineRangeCaption();

            BulkAutoExecutionOptions medical = BulkAutoExecutionOptions.FromSettings(BulkQualificationKind.MedicalAid);
            DateTime medicalMonth = medical.GetMedicalTreatmentMonth(today);
            numericUpDownMedicalYear.Value = ClampNumericValue(medicalMonth.Year, numericUpDownMedicalYear.Minimum, numericUpDownMedicalYear.Maximum);
            numericUpDownMedicalMonth.Value = medicalMonth.Month;
        }

        private static decimal ClampNumericValue(decimal value, decimal minimum, decimal maximum)
        {
            if (value < minimum) return minimum;
            if (value > maximum) return maximum;
            return value;
        }

        private void UpdateOnlineRangeCaption()
        {
            checkBoxOnlineUseConsent.Text = checkBoxOnlineUseConsent.Checked ? "同意日で取得" : "受診日で取得";
        }

        private void checkBoxOnlineUseConsent_CheckedChanged(object sender, EventArgs e)
        {
            UpdateOnlineRangeCaption();
        }

        private void InitializeGrid()
        {
            dgvResults.AutoGenerateColumns = false;
            dgvResults.Columns.Clear();
            dgvResults.Columns.Add(new DataGridViewCheckBoxColumn
            {
                DataPropertyName = nameof(BulkExecutionResultRow.Send),
                HeaderText = "送信",
                Width = 48
            });
            dgvResults.Columns.Add(CreateTextColumn(nameof(BulkExecutionResultRow.ImportedAt), "取得日時", 135));
            dgvResults.Columns.Add(CreateTextColumn(nameof(BulkExecutionResultRow.Kind), "種別", 90));
            dgvResults.Columns.Add(CreateTextColumn(nameof(BulkExecutionResultRow.Sequence), "No", 50));
            dgvResults.Columns.Add(CreateTextColumn(nameof(BulkExecutionResultRow.Name), "氏名", 120));
            dgvResults.Columns.Add(CreateTextColumn(nameof(BulkExecutionResultRow.BirthDate), "生年月日", 90));
            dgvResults.Columns.Add(CreateTextColumn(nameof(BulkExecutionResultRow.Insurance), "保険情報", 220));
            dgvResults.Columns.Add(CreateTextColumn(nameof(BulkExecutionResultRow.Validity), "資格", 90));
            dgvResults.Columns.Add(CreateTextColumn(nameof(BulkExecutionResultRow.MatchStatus), "照合", 90));
            dgvResults.Columns.Add(CreateTextColumn(nameof(BulkExecutionResultRow.KarteNo), "カルテ", 80));
            dgvResults.Columns.Add(CreateTextColumn(nameof(BulkExecutionResultRow.QueryNumber), "照会番号", 120));
            dgvResults.Columns.Add(CreateTextColumn(nameof(BulkExecutionResultRow.SendStatus), "送信結果", 140));
            dgvResults.DataSource = rows;
            dgvResults.ReadOnly = false;

            dgvResults.EnableHeadersVisualStyles = false;
            dgvResults.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(226, 232, 240);
            dgvResults.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(15, 23, 42);
            dgvResults.ColumnHeadersDefaultCellStyle.Font = new Font("Meiryo UI", 9F, FontStyle.Bold);
            dgvResults.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            dgvResults.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            dgvResults.RowsDefaultCellStyle.BackColor = Color.White;
            dgvResults.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
        }

        private void SetStatusTone(Color backColor, Color textColor)
        {
            panelStatusCard.BackColor = backColor;
            labelStatus.ForeColor = textColor;
            labelDetail.ForeColor = textColor;
        }

        private void InitializeCardProgress()
        {
            ResetStepProgress(BulkQualificationKind.Houmon);
            ResetStepProgress(BulkQualificationKind.Online);
            ResetStepProgress(BulkQualificationKind.MedicalAid);
            SetCardProgress(BulkQualificationKind.Houmon, "待機中", "実行待ちです。", false, false);
            SetCardProgress(BulkQualificationKind.Online, "待機中", "実行待ちです。", false, false);
            SetCardProgress(BulkQualificationKind.MedicalAid, "待機中", "実行待ちです。", false, false);
        }

        private void ResetStepProgress(BulkQualificationKind kind)
        {
            activeProgressSteps[kind] = -1;
            completedProgressSteps[kind] = -1;
            ApplyStepProgressVisuals(kind, false);
        }

        private void InitializeCardStepIndicators()
        {
            AddStepIndicators(BulkQualificationKind.Houmon, panelHoumonCard, labelHoumonProgressDetail);
            AddStepIndicators(BulkQualificationKind.Online, panelOnlineCard, labelOnlineProgressDetail);
            AddStepIndicators(BulkQualificationKind.MedicalAid, panelMedicalCard, labelMedicalProgressDetail);
        }

        private void AddStepIndicators(BulkQualificationKind kind, Panel card, Label detailLabel)
        {
            string[] captions = { "処理番号", "資格情報", "完了" };
            Label[] labels = new Label[captions.Length];
            for (int i = 0; i < captions.Length; i++)
            {
                Label label = new Label
                {
                    AutoSize = false,
                    Font = new Font("Meiryo UI", 8.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(148, 163, 184),
                    Location = new Point(18 + (i * 112), 123),
                    Name = kind.ToString() + "ProgressStep" + i.ToString(),
                    Size = new Size(i == 2 ? 82 : 104, 18),
                    Text = (i == 0 ? "" : "→ ") + captions[i],
                    TextAlign = ContentAlignment.MiddleLeft
                };
                labels[i] = label;
                card.Controls.Add(label);
                label.BringToFront();
            }

            detailLabel.Location = new Point(18, 145);
            detailLabel.Size = new Size(350, 22);
            progressStepLabels[kind] = labels;
            activeProgressSteps[kind] = -1;
            completedProgressSteps[kind] = -1;
        }

        private void UpdateCardProgress(BulkExecutionProgressInfo info)
        {
            BulkQualificationKind? kind = info.Kind ?? InferKindFromProcessName(info.ProcessName);
            if (!kind.HasValue)
            {
                return;
            }

            string status = string.IsNullOrWhiteSpace(info.StatusText) ? "処理中" : info.StatusText;
            string detail = string.IsNullOrWhiteSpace(info.DetailText) ? string.Empty : info.DetailText;
            if (!string.IsNullOrWhiteSpace(info.ReceptionNumber))
            {
                detail = string.IsNullOrWhiteSpace(detail)
                    ? "受付番号: " + info.ReceptionNumber
                    : detail + " / 受付番号: " + info.ReceptionNumber;
            }

            SetCardProgress(kind.Value, status, detail, info.IsCompleted, info.HasError);
            UpdateStepProgress(kind.Value, status, info.IsCompleted, info.HasError);
        }

        private static BulkQualificationKind? InferKindFromProcessName(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
            {
                return null;
            }

            if (processName.Contains("訪問"))
            {
                return BulkQualificationKind.Houmon;
            }

            if (processName.Contains("オンライン"))
            {
                return BulkQualificationKind.Online;
            }

            if (processName.Contains("医療扶助"))
            {
                return BulkQualificationKind.MedicalAid;
            }

            return null;
        }

        private void SetCardProgress(BulkQualificationKind kind, string status, string detail, bool completed, bool error)
        {
            Label statusLabel;
            Label detailLabel;
            Panel card;
            switch (kind)
            {
                case BulkQualificationKind.Houmon:
                    statusLabel = labelHoumonProgress;
                    detailLabel = labelHoumonProgressDetail;
                    card = panelHoumonCard;
                    break;
                case BulkQualificationKind.Online:
                    statusLabel = labelOnlineProgress;
                    detailLabel = labelOnlineProgressDetail;
                    card = panelOnlineCard;
                    break;
                default:
                    statusLabel = labelMedicalProgress;
                    detailLabel = labelMedicalProgressDetail;
                    card = panelMedicalCard;
                    break;
            }

            Color textColor = error
                ? Color.FromArgb(185, 28, 28)
                : completed
                    ? Color.FromArgb(21, 128, 61)
                    : Color.FromArgb(180, 83, 9);

            statusLabel.Text = status;
            statusLabel.ForeColor = textColor;
            detailLabel.Text = string.IsNullOrWhiteSpace(detail) ? " " : detail;
            detailLabel.ForeColor = Color.FromArgb(71, 85, 105);
            card.BackColor = error
                ? Color.FromArgb(254, 242, 242)
                : completed
                    ? Color.FromArgb(240, 253, 244)
                    : Color.White;
        }

        private void UpdateStepProgress(BulkQualificationKind kind, string status, bool completed, bool error)
        {
            if (error)
            {
                activeProgressSteps[kind] = -1;
                ApplyStepProgressVisuals(kind, true);
                RefreshBlinkTimerState();
                return;
            }

            if (completed || (!string.IsNullOrWhiteSpace(status) && status.Contains("取得完了")))
            {
                completedProgressSteps[kind] = 2;
                activeProgressSteps[kind] = -1;
                ApplyStepProgressVisuals(kind, false);
                RefreshBlinkTimerState();
                return;
            }

            int activeStep = GetStepIndex(status);
            if (activeStep >= 0)
            {
                completedProgressSteps[kind] = activeStep - 1;
                activeProgressSteps[kind] = activeStep;
            }
            else
            {
                completedProgressSteps[kind] = -1;
                activeProgressSteps[kind] = -1;
            }

            ApplyStepProgressVisuals(kind, false);
            RefreshBlinkTimerState();
        }

        private static int GetStepIndex(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return -1;
            }

            if (status.Contains("処理番号") || status.Contains("受付番号") || status.Contains("要求"))
            {
                return 0;
            }

            if (status.Contains("資格情報")
                || status.Contains("結果")
                || status.Contains("解析")
                || status.Contains("照合")
                || status.Contains("保存"))
            {
                return 1;
            }

            if (status.Contains("完了") || status.Contains("成功"))
            {
                return 2;
            }

            return -1;
        }

        private void ApplyStepProgressVisuals(BulkQualificationKind kind, bool error)
        {
            Label[] labels;
            if (!progressStepLabels.TryGetValue(kind, out labels))
            {
                return;
            }

            int activeStep = activeProgressSteps.ContainsKey(kind) ? activeProgressSteps[kind] : -1;
            int completedStep = completedProgressSteps.ContainsKey(kind) ? completedProgressSteps[kind] : -1;

            for (int i = 0; i < labels.Length; i++)
            {
                if (error)
                {
                    labels[i].ForeColor = Color.FromArgb(185, 28, 28);
                }
                else if (i <= completedStep)
                {
                    labels[i].ForeColor = Color.FromArgb(21, 128, 61);
                }
                else if (i == activeStep)
                {
                    labels[i].ForeColor = progressBlinkOn
                        ? Color.FromArgb(217, 119, 6)
                        : Color.FromArgb(203, 213, 225);
                }
                else
                {
                    labels[i].ForeColor = Color.FromArgb(148, 163, 184);
                }
            }
        }

        private void RefreshBlinkTimerState()
        {
            bool hasActive = activeProgressSteps.Values.Any(step => step >= 0);
            if (hasActive && !progressBlinkTimer.Enabled)
            {
                progressBlinkOn = true;
                progressBlinkTimer.Start();
            }
            else if (!hasActive && progressBlinkTimer.Enabled)
            {
                progressBlinkTimer.Stop();
                progressBlinkOn = true;
            }
        }

        private void progressBlinkTimer_Tick(object sender, EventArgs e)
        {
            progressBlinkOn = !progressBlinkOn;
            foreach (BulkQualificationKind kind in progressStepLabels.Keys.ToList())
            {
                ApplyStepProgressVisuals(kind, false);
            }
        }

        private void UpdateActionState()
        {
            ApplyCardState(
                buttonRunHoumonOnce,
                buttonToggleHoumonAuto,
                labelHoumonMode,
                runHoumonOnceAsync,
                toggleHoumonAutoAsync,
                isHoumonAutoEnabled?.Invoke() ?? false,
                Color.FromArgb(14, 116, 144));

            ApplyCardState(
                buttonRunOnlineOnce,
                buttonToggleOnlineAuto,
                labelOnlineMode,
                runOnlineOnceAsync,
                toggleOnlineAutoAsync,
                isOnlineAutoEnabled?.Invoke() ?? false,
                Color.FromArgb(8, 145, 178));

            ApplyCardState(
                buttonRunMedicalOnce,
                buttonToggleMedicalAuto,
                labelMedicalMode,
                runMedicalOnceAsync,
                toggleMedicalAutoAsync,
                isMedicalAutoEnabled?.Invoke() ?? false,
                Color.FromArgb(21, 128, 61));

            SetActionButtonsEnabled(true);
        }

        private static void ApplyCardState(
            Button runButton,
            Button toggleButton,
            Label modeLabel,
            Func<Task> runAction,
            Func<Task> toggleAction,
            bool autoEnabled,
            Color activeColor)
        {
            runButton.Enabled = runAction != null;
            toggleButton.Enabled = toggleAction != null;

            toggleButton.Text = autoEnabled ? "自動実行を停止" : "自動実行を開始";
            ApplyButtonStyle(
                toggleButton,
                autoEnabled ? Color.FromArgb(220, 38, 38) : activeColor,
                Color.White);

            modeLabel.Text = autoEnabled ? "自動実行: 有効" : "自動実行: 停止中";
            modeLabel.ForeColor = autoEnabled ? Color.FromArgb(21, 128, 61) : Color.FromArgb(100, 116, 139);
        }

        private void SetKindActionButtons(BulkQualificationKind kind, bool enabled)
        {
            switch (kind)
            {
                case BulkQualificationKind.Houmon:
                    buttonRunHoumonOnce.Enabled = CanRunKindAction(enabled, BulkQualificationKind.Houmon, runHoumonOnceAsync);
                    buttonToggleHoumonAuto.Enabled = CanToggleKindAction(enabled, BulkQualificationKind.Houmon, toggleHoumonAutoAsync, isHoumonAutoEnabled);
                    buttonCancelHoumon.Enabled = CanCancelKindAction(BulkQualificationKind.Houmon, cancelHoumonAsync);
                    RefreshButtonVisualStates(buttonRunHoumonOnce, buttonToggleHoumonAuto, buttonCancelHoumon);
                    break;
                case BulkQualificationKind.Online:
                    buttonRunOnlineOnce.Enabled = CanRunKindAction(enabled, BulkQualificationKind.Online, runOnlineOnceAsync);
                    buttonToggleOnlineAuto.Enabled = CanToggleKindAction(enabled, BulkQualificationKind.Online, toggleOnlineAutoAsync, isOnlineAutoEnabled);
                    buttonCancelOnline.Enabled = CanCancelKindAction(BulkQualificationKind.Online, cancelOnlineAsync);
                    RefreshButtonVisualStates(buttonRunOnlineOnce, buttonToggleOnlineAuto, buttonCancelOnline);
                    break;
                default:
                    buttonRunMedicalOnce.Enabled = CanRunKindAction(enabled, BulkQualificationKind.MedicalAid, runMedicalOnceAsync);
                    buttonToggleMedicalAuto.Enabled = CanToggleKindAction(enabled, BulkQualificationKind.MedicalAid, toggleMedicalAutoAsync, isMedicalAutoEnabled);
                    buttonCancelMedical.Enabled = CanCancelKindAction(BulkQualificationKind.MedicalAid, cancelMedicalAsync);
                    RefreshButtonVisualStates(buttonRunMedicalOnce, buttonToggleMedicalAuto, buttonCancelMedical);
                    break;
            }
        }

        private static void ApplyButtonStyle(Button button, Color backColor, Color foreColor)
        {
            button.Tag = new ButtonPalette(backColor, foreColor);
            button.UseVisualStyleBackColor = false;
            button.BackColor = backColor;
            button.ForeColor = foreColor;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Cursor = Cursors.Hand;
            RefreshButtonVisualState(button);
        }

        private static void RefreshButtonVisualState(Button button)
        {
            ButtonPalette palette = button.Tag as ButtonPalette;
            if (palette == null)
            {
                return;
            }

            if (button.Enabled)
            {
                button.BackColor = palette.BackColor;
                button.ForeColor = palette.ForeColor;
                button.Cursor = Cursors.Hand;
            }
            else
            {
                button.BackColor = Color.FromArgb(226, 232, 240);
                button.ForeColor = Color.FromArgb(148, 163, 184);
                button.Cursor = Cursors.Default;
            }
        }

        private static void RefreshButtonVisualStates(params Button[] buttons)
        {
            foreach (Button button in buttons)
            {
                RefreshButtonVisualState(button);
            }
        }

        private sealed class ButtonPalette
        {
            public ButtonPalette(Color backColor, Color foreColor)
            {
                BackColor = backColor;
                ForeColor = foreColor;
            }

            public Color BackColor { get; }
            public Color ForeColor { get; }
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

        private async void buttonRunHoumonOnce_Click(object sender, EventArgs e)
        {
            await ExecuteBulkActionAsync(BulkQualificationKind.Houmon, runHoumonOnceAsync);
        }

        private async void buttonToggleHoumonAuto_Click(object sender, EventArgs e)
        {
            await ExecuteBulkActionAsync(BulkQualificationKind.Houmon, toggleHoumonAutoAsync, true);
        }

        private async void buttonCancelHoumon_Click(object sender, EventArgs e)
        {
            await ExecuteCancelActionAsync(BulkQualificationKind.Houmon, cancelHoumonAsync);
        }

        private async void buttonRunOnlineOnce_Click(object sender, EventArgs e)
        {
            await ExecuteBulkActionAsync(BulkQualificationKind.Online, runOnlineOnceAsync);
        }

        private async void buttonToggleOnlineAuto_Click(object sender, EventArgs e)
        {
            await ExecuteBulkActionAsync(BulkQualificationKind.Online, toggleOnlineAutoAsync, true);
        }

        private async void buttonCancelOnline_Click(object sender, EventArgs e)
        {
            await ExecuteCancelActionAsync(BulkQualificationKind.Online, cancelOnlineAsync);
        }

        private async void buttonRunMedicalOnce_Click(object sender, EventArgs e)
        {
            await ExecuteBulkActionAsync(BulkQualificationKind.MedicalAid, runMedicalOnceAsync);
        }

        private async void buttonToggleMedicalAuto_Click(object sender, EventArgs e)
        {
            await ExecuteBulkActionAsync(BulkQualificationKind.MedicalAid, toggleMedicalAutoAsync, true);
        }

        private async void buttonCancelMedical_Click(object sender, EventArgs e)
        {
            await ExecuteCancelActionAsync(BulkQualificationKind.MedicalAid, cancelMedicalAsync);
        }

        private async Task ExecuteBulkActionAsync(BulkQualificationKind kind, Func<Task> action, bool refreshStateAfter = false)
        {
            if (action == null || runningKinds.Contains(kind))
            {
                return;
            }

            runningKinds.Add(kind);
            SetKindActionButtons(kind, false);
            try
            {
                await action().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                Exception actual = UnwrapActionException(ex);
                AppendProgress(new BulkExecutionProgressInfo
                {
                    Timestamp = DateTime.Now,
                    Kind = kind,
                    ProcessName = BulkQualificationService.GetDisplayName(kind),
                    StatusText = "エラー",
                    DetailText = actual.Message,
                    HasError = true
                });

                MessageBox.Show(
                    this,
                    actual.Message,
                    "BulkTool",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            finally
            {
                runningKinds.Remove(kind);
                SetKindActionButtons(kind, true);
                if (refreshStateAfter)
                {
                    UpdateActionState();
                }
                else
                {
                    SetActionButtonsEnabled(true);
                }
            }
        }

        private async Task ExecuteActionAsync(Func<Task> action, bool refreshStateAfter = false)
        {
            if (action == null)
            {
                return;
            }

            SetActionButtonsEnabled(false);
            try
            {
                await action().ConfigureAwait(true);
            }
            finally
            {
                SetActionButtonsEnabled(true);
                if (refreshStateAfter)
                {
                    UpdateActionState();
                }
            }
        }

        private async Task ExecuteCancelActionAsync(BulkQualificationKind kind, Func<Task> action)
        {
            if (action == null || !runningKinds.Contains(kind))
            {
                return;
            }

            Button cancelButton = GetCancelButton(kind);
            if (cancelButton != null)
            {
                cancelButton.Enabled = false;
                RefreshButtonVisualState(cancelButton);
            }

            AppendProgress(new BulkExecutionProgressInfo
            {
                Timestamp = DateTime.Now,
                Kind = kind,
                ProcessName = BulkQualificationService.GetDisplayName(kind),
                StatusText = "中止要求中",
                DetailText = "実行中の処理へ中止を要求しました。"
            });

            try
            {
                await action().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                Exception actual = UnwrapActionException(ex);
                AppendProgress(new BulkExecutionProgressInfo
                {
                    Timestamp = DateTime.Now,
                    Kind = kind,
                    ProcessName = BulkQualificationService.GetDisplayName(kind),
                    StatusText = "中止エラー",
                    DetailText = actual.Message,
                    HasError = true
                });
            }
        }

        private void ToggleActionButtons(bool enabled)
        {
            SetActionButtonsEnabled(enabled);
        }

        private void SetActionButtonsEnabled(bool enabled)
        {
            buttonRunHoumonOnce.Enabled = CanRunKindAction(enabled, BulkQualificationKind.Houmon, runHoumonOnceAsync);
            buttonToggleHoumonAuto.Enabled = CanToggleKindAction(enabled, BulkQualificationKind.Houmon, toggleHoumonAutoAsync, isHoumonAutoEnabled);
            buttonCancelHoumon.Enabled = CanCancelKindAction(BulkQualificationKind.Houmon, cancelHoumonAsync);

            buttonRunOnlineOnce.Enabled = CanRunKindAction(enabled, BulkQualificationKind.Online, runOnlineOnceAsync);
            buttonToggleOnlineAuto.Enabled = CanToggleKindAction(enabled, BulkQualificationKind.Online, toggleOnlineAutoAsync, isOnlineAutoEnabled);
            buttonCancelOnline.Enabled = CanCancelKindAction(BulkQualificationKind.Online, cancelOnlineAsync);

            buttonRunMedicalOnce.Enabled = CanRunKindAction(enabled, BulkQualificationKind.MedicalAid, runMedicalOnceAsync);
            buttonToggleMedicalAuto.Enabled = CanToggleKindAction(enabled, BulkQualificationKind.MedicalAid, toggleMedicalAutoAsync, isMedicalAutoEnabled);
            buttonCancelMedical.Enabled = CanCancelKindAction(BulkQualificationKind.MedicalAid, cancelMedicalAsync);

            buttonCheckAll.Enabled = enabled && rows.Count > 0;
            buttonClearChecks.Enabled = enabled && rows.Count > 0;
            buttonSendSelected.Enabled = enabled && sendSelectedAsync != null && rows.Any(r => r.Send && !r.Record.IsSent);
            buttonRefreshResults.Enabled = enabled && refreshLatestAsync != null;

            RefreshButtonVisualStates(
                buttonRunHoumonOnce,
                buttonToggleHoumonAuto,
                buttonCancelHoumon,
                buttonRunOnlineOnce,
                buttonToggleOnlineAuto,
                buttonCancelOnline,
                buttonRunMedicalOnce,
                buttonToggleMedicalAuto,
                buttonCancelMedical,
                buttonCheckAll,
                buttonClearChecks,
                buttonSendSelected,
                buttonRefreshResults,
                buttonToggleLog);
        }

        private bool CanRunKindAction(bool enabled, BulkQualificationKind kind, Func<Task> action)
        {
            return enabled
                && bulkExecutionAvailable
                && action != null
                && !runningKinds.Contains(kind);
        }

        private bool CanToggleKindAction(bool enabled, BulkQualificationKind kind, Func<Task> action, Func<bool> isAutoEnabled)
        {
            bool autoEnabled = isAutoEnabled?.Invoke() ?? false;
            return enabled
                && action != null
                && !runningKinds.Contains(kind)
                && (bulkExecutionAvailable || autoEnabled);
        }

        private bool CanCancelKindAction(BulkQualificationKind kind, Func<Task> action)
        {
            return action != null && runningKinds.Contains(kind);
        }

        private Button GetCancelButton(BulkQualificationKind kind)
        {
            switch (kind)
            {
                case BulkQualificationKind.Houmon:
                    return buttonCancelHoumon;
                case BulkQualificationKind.Online:
                    return buttonCancelOnline;
                default:
                    return buttonCancelMedical;
            }
        }

        private static Exception UnwrapActionException(Exception ex)
        {
            if (ex is TargetInvocationException targetInvocationException && targetInvocationException.InnerException != null)
            {
                return targetInvocationException.InnerException;
            }

            return ex;
        }

        private void ApplyDetailLogExpandedState(bool expanded)
        {
            detailLogExpanded = expanded;
            if (splitContainerMain != null)
            {
                splitContainerMain.Panel1Collapsed = !expanded;
                if (expanded && splitContainerMain.SplitterDistance < 110)
                {
                    splitContainerMain.SplitterDistance = 122;
                }
            }

            if (buttonToggleLog != null)
            {
                buttonToggleLog.Text = expanded ? "ログ非表示" : "ログ表示";
                RefreshButtonVisualState(buttonToggleLog);
            }
        }

        private void buttonToggleLog_Click(object sender, EventArgs e)
        {
            ApplyDetailLogExpandedState(!detailLogExpanded);
        }

        private void buttonCheckAll_Click(object sender, EventArgs e)
        {
            foreach (BulkExecutionResultRow row in rows)
            {
                if (!row.Record.IsSent)
                {
                    row.Send = true;
                }
            }

            dgvResults.Refresh();
            ToggleActionButtons(true);
        }

        private void buttonClearChecks_Click(object sender, EventArgs e)
        {
            foreach (BulkExecutionResultRow row in rows)
            {
                row.Send = false;
            }

            dgvResults.Refresh();
            ToggleActionButtons(true);
        }

        private async void buttonSendSelected_Click(object sender, EventArgs e)
        {
            if (sendSelectedAsync == null)
            {
                return;
            }

            List<BulkExecutionResultRow> selectedRows = rows.Where(r => r.Send && !r.Record.IsSent).ToList();
            if (selectedRows.Count == 0)
            {
                MessageBox.Show(this, "送信対象のチェックがありません。", "BulkTool", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ToggleActionButtons(false);
            try
            {
                QualificationSendSummary summary = await sendSelectedAsync(selectedRows.Select(r => r.Record).ToList()).ConfigureAwait(true);
                foreach (BulkExecutionResultRow row in selectedRows)
                {
                    row.Send = false;
                    row.RefreshFromRecord();
                }

                dgvResults.Refresh();
                AppendProgress(new BulkExecutionProgressInfo
                {
                    Timestamp = DateTime.Now,
                    ProcessName = labelProcessName.Text,
                    StatusText = "face出力完了",
                    DetailText = $"出力成功 {summary.SentCount}件 / 失敗 {summary.FailedCount}件",
                    ImportedCount = CurrentSession == null ? 0 : CurrentSession.Records.Count,
                    IsCompleted = summary.FailedCount == 0
                });

                MessageBox.Show(
                    this,
                    $"face出力成功: {summary.SentCount}\r\nface出力失敗: {summary.FailedCount}",
                    "BulkTool",
                    MessageBoxButtons.OK,
                    summary.FailedCount == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            finally
            {
                ToggleActionButtons(true);
            }
        }

        private void dgvResults_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dgvResults.IsCurrentCellDirty)
            {
                dgvResults.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void dgvResults_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                ToggleActionButtons(true);
            }
        }

        private async void buttonRefreshResults_Click(object sender, EventArgs e)
        {
            await ExecuteActionAsync(refreshLatestAsync);
        }

        private void dgvResults_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            BulkExecutionResultRow row = dgvResults.Rows[e.RowIndex].DataBoundItem as BulkExecutionResultRow;
            if (row?.Record == null)
            {
                return;
            }

            ResultDetailRequested?.Invoke(this, new ImportedQualificationRecordEventArgs(row.Record));
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            Hide();
        }

        internal sealed class ImportedQualificationRecordEventArgs : EventArgs
        {
            public ImportedQualificationRecordEventArgs(ImportedQualificationRecord record)
            {
                Record = record;
            }

            public ImportedQualificationRecord Record { get; }
        }

        private sealed class BulkExecutionResultRow : INotifyPropertyChanged
        {
            private readonly ImportedQualificationRecord record;
            private bool send;
            private string matchStatus;
            private string sendStatus;

            public BulkExecutionResultRow(ImportedQualificationRecord record)
            {
                this.record = record;
                matchStatus = record == null ? string.Empty : record.MatchStatus;
                sendStatus = record == null ? string.Empty : record.LastSendMessage;
            }

            public ImportedQualificationRecord Record => record;
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
            public int Sequence => record.Sequence;
            public string ImportedAt => record.FormatImportedAt();
            public string Kind => ImportedQualificationRecord.GetKindDisplayName(record.Kind);
            public string Name => record.Name;
            public string BirthDate => record.BirthDate;
            public string Insurance => $"{record.InsurerNumber} / {record.Symbol}-{record.Number}-{record.BranchNumber}".Trim();
            public string Validity => record.QualificationValidity;
            public string MatchStatus
            {
                get { return matchStatus; }
                private set
                {
                    matchStatus = value;
                    OnPropertyChanged(nameof(MatchStatus));
                }
            }
            public string KarteNo => record.MatchedPatientId > 0 ? record.MatchedPatientId.ToString() : string.Empty;
            public string QueryNumber => record.QueryNumber;
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
                MatchStatus = record.MatchStatus;
                SendStatus = record.LastSendMessage;
                OnPropertyChanged(nameof(KarteNo));
            }

            public event PropertyChangedEventHandler PropertyChanged;

            private void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
