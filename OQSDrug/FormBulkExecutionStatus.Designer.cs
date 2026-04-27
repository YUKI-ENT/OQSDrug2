namespace OQSDrug
{
    partial class FormBulkExecutionStatus
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Panel panelHeader;
        private System.Windows.Forms.Label labelHeaderTitle;
        private System.Windows.Forms.Label labelHeaderDescription;
        private System.Windows.Forms.Panel panelActionSection;
        private System.Windows.Forms.Panel panelExecutionWarning;
        private System.Windows.Forms.Label labelExecutionWarning;
        private System.Windows.Forms.Panel panelHoumonCard;
        private System.Windows.Forms.Label labelHoumonTitle;
        private System.Windows.Forms.Label labelHoumonMode;
        private System.Windows.Forms.Label labelHoumonProgress;
        private System.Windows.Forms.Label labelHoumonProgressDetail;
        private System.Windows.Forms.Label labelHoumonRangeCaption;
        private System.Windows.Forms.Label labelHoumonRangeSeparator;
        private System.Windows.Forms.DateTimePicker dateTimePickerHoumonFrom;
        private System.Windows.Forms.DateTimePicker dateTimePickerHoumonTo;
        private System.Windows.Forms.Button buttonRunHoumonOnce;
        private System.Windows.Forms.Button buttonToggleHoumonAuto;
        private System.Windows.Forms.Button buttonCancelHoumon;
        private System.Windows.Forms.Panel panelOnlineCard;
        private System.Windows.Forms.Label labelOnlineTitle;
        private System.Windows.Forms.Label labelOnlineMode;
        private System.Windows.Forms.Label labelOnlineProgress;
        private System.Windows.Forms.Label labelOnlineProgressDetail;
        private System.Windows.Forms.CheckBox checkBoxOnlineUseConsent;
        private System.Windows.Forms.Label labelOnlineRangeCaption;
        private System.Windows.Forms.Label labelOnlineRangeSeparator;
        private System.Windows.Forms.DateTimePicker dateTimePickerOnlineFrom;
        private System.Windows.Forms.DateTimePicker dateTimePickerOnlineTo;
        private System.Windows.Forms.Button buttonRunOnlineOnce;
        private System.Windows.Forms.Button buttonToggleOnlineAuto;
        private System.Windows.Forms.Button buttonCancelOnline;
        private System.Windows.Forms.Panel panelMedicalCard;
        private System.Windows.Forms.Label labelMedicalTitle;
        private System.Windows.Forms.Label labelMedicalMode;
        private System.Windows.Forms.Label labelMedicalProgress;
        private System.Windows.Forms.Label labelMedicalProgressDetail;
        private System.Windows.Forms.Label labelMedicalMonthCaption;
        private System.Windows.Forms.NumericUpDown numericUpDownMedicalYear;
        private MonthNumericUpDown numericUpDownMedicalMonth;
        private System.Windows.Forms.Label labelMedicalYearSuffix;
        private System.Windows.Forms.Label labelMedicalMonthSuffix;
        private System.Windows.Forms.Button buttonRunMedicalOnce;
        private System.Windows.Forms.Button buttonToggleMedicalAuto;
        private System.Windows.Forms.Button buttonCancelMedical;
        private System.Windows.Forms.Panel panelStatusCard;
        private System.Windows.Forms.Label labelCaption;
        private System.Windows.Forms.Label labelProcessName;
        private System.Windows.Forms.Label labelStatusCaption;
        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.Label labelDetailCaption;
        private System.Windows.Forms.Label labelDetail;
        private System.Windows.Forms.Label labelReceptionNumber;
        private System.Windows.Forms.Label labelResultCount;
        private System.Windows.Forms.Button buttonCheckAll;
        private System.Windows.Forms.Button buttonClearChecks;
        private System.Windows.Forms.Button buttonSendSelected;
        private System.Windows.Forms.Button buttonRefreshResults;
        private System.Windows.Forms.Button buttonToggleLog;
        private System.Windows.Forms.Button buttonClose;
        private System.Windows.Forms.SplitContainer splitContainerMain;
        private System.Windows.Forms.SplitContainer splitContainerTop;
        private System.Windows.Forms.ListBox listBoxLog;
        private System.Windows.Forms.DataGridView dgvResults;
        private System.Windows.Forms.Label labelLogCaption;
        private System.Windows.Forms.Label labelResultCaption;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormBulkExecutionStatus));
            this.panelHeader = new System.Windows.Forms.Panel();
            this.buttonClose = new System.Windows.Forms.Button();
            this.labelHeaderDescription = new System.Windows.Forms.Label();
            this.labelHeaderTitle = new System.Windows.Forms.Label();
            this.panelActionSection = new System.Windows.Forms.Panel();
            this.panelExecutionWarning = new System.Windows.Forms.Panel();
            this.labelExecutionWarning = new System.Windows.Forms.Label();
            this.panelMedicalCard = new System.Windows.Forms.Panel();
            this.labelMedicalMonthSuffix = new System.Windows.Forms.Label();
            this.labelMedicalYearSuffix = new System.Windows.Forms.Label();
            this.numericUpDownMedicalMonth = new OQSDrug.MonthNumericUpDown();
            this.numericUpDownMedicalYear = new System.Windows.Forms.NumericUpDown();
            this.labelMedicalMonthCaption = new System.Windows.Forms.Label();
            this.buttonToggleMedicalAuto = new System.Windows.Forms.Button();
            this.buttonRunMedicalOnce = new System.Windows.Forms.Button();
            this.buttonCancelMedical = new System.Windows.Forms.Button();
            this.labelMedicalMode = new System.Windows.Forms.Label();
            this.labelMedicalProgress = new System.Windows.Forms.Label();
            this.labelMedicalProgressDetail = new System.Windows.Forms.Label();
            this.labelMedicalTitle = new System.Windows.Forms.Label();
            this.panelOnlineCard = new System.Windows.Forms.Panel();
            this.dateTimePickerOnlineTo = new System.Windows.Forms.DateTimePicker();
            this.dateTimePickerOnlineFrom = new System.Windows.Forms.DateTimePicker();
            this.labelOnlineRangeSeparator = new System.Windows.Forms.Label();
            this.labelOnlineRangeCaption = new System.Windows.Forms.Label();
            this.checkBoxOnlineUseConsent = new System.Windows.Forms.CheckBox();
            this.buttonToggleOnlineAuto = new System.Windows.Forms.Button();
            this.buttonRunOnlineOnce = new System.Windows.Forms.Button();
            this.buttonCancelOnline = new System.Windows.Forms.Button();
            this.labelOnlineMode = new System.Windows.Forms.Label();
            this.labelOnlineProgress = new System.Windows.Forms.Label();
            this.labelOnlineProgressDetail = new System.Windows.Forms.Label();
            this.labelOnlineTitle = new System.Windows.Forms.Label();
            this.panelHoumonCard = new System.Windows.Forms.Panel();
            this.dateTimePickerHoumonTo = new System.Windows.Forms.DateTimePicker();
            this.dateTimePickerHoumonFrom = new System.Windows.Forms.DateTimePicker();
            this.labelHoumonRangeSeparator = new System.Windows.Forms.Label();
            this.labelHoumonRangeCaption = new System.Windows.Forms.Label();
            this.buttonToggleHoumonAuto = new System.Windows.Forms.Button();
            this.buttonRunHoumonOnce = new System.Windows.Forms.Button();
            this.buttonCancelHoumon = new System.Windows.Forms.Button();
            this.labelHoumonMode = new System.Windows.Forms.Label();
            this.labelHoumonProgress = new System.Windows.Forms.Label();
            this.labelHoumonProgressDetail = new System.Windows.Forms.Label();
            this.labelHoumonTitle = new System.Windows.Forms.Label();
            this.splitContainerMain = new System.Windows.Forms.SplitContainer();
            this.splitContainerTop = new System.Windows.Forms.SplitContainer();
            this.panelStatusCard = new System.Windows.Forms.Panel();
            this.labelResultCount = new System.Windows.Forms.Label();
            this.labelReceptionNumber = new System.Windows.Forms.Label();
            this.labelDetail = new System.Windows.Forms.Label();
            this.labelDetailCaption = new System.Windows.Forms.Label();
            this.labelStatus = new System.Windows.Forms.Label();
            this.labelStatusCaption = new System.Windows.Forms.Label();
            this.labelProcessName = new System.Windows.Forms.Label();
            this.labelCaption = new System.Windows.Forms.Label();
            this.listBoxLog = new System.Windows.Forms.ListBox();
            this.labelLogCaption = new System.Windows.Forms.Label();
            this.dgvResults = new System.Windows.Forms.DataGridView();
            this.buttonSendSelected = new System.Windows.Forms.Button();
            this.buttonRefreshResults = new System.Windows.Forms.Button();
            this.buttonToggleLog = new System.Windows.Forms.Button();
            this.labelResultCaption = new System.Windows.Forms.Label();
            this.buttonClearChecks = new System.Windows.Forms.Button();
            this.buttonCheckAll = new System.Windows.Forms.Button();
            this.panelHeader.SuspendLayout();
            this.panelActionSection.SuspendLayout();
            this.panelExecutionWarning.SuspendLayout();
            this.panelMedicalCard.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownMedicalMonth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownMedicalYear)).BeginInit();
            this.panelOnlineCard.SuspendLayout();
            this.panelHoumonCard.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).BeginInit();
            this.splitContainerMain.Panel1.SuspendLayout();
            this.splitContainerMain.Panel2.SuspendLayout();
            this.splitContainerMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerTop)).BeginInit();
            this.splitContainerTop.Panel1.SuspendLayout();
            this.splitContainerTop.Panel2.SuspendLayout();
            this.splitContainerTop.SuspendLayout();
            this.panelStatusCard.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvResults)).BeginInit();
            this.SuspendLayout();
            // 
            // panelHeader
            // 
            this.panelHeader.Controls.Add(this.buttonClose);
            this.panelHeader.Controls.Add(this.labelHeaderDescription);
            this.panelHeader.Controls.Add(this.labelHeaderTitle);
            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Location = new System.Drawing.Point(0, 0);
            this.panelHeader.Name = "panelHeader";
            this.panelHeader.Padding = new System.Windows.Forms.Padding(18, 16, 18, 14);
            this.panelHeader.Size = new System.Drawing.Size(1224, 58);
            this.panelHeader.TabIndex = 0;
            // 
            // buttonClose
            // 
            this.buttonClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonClose.Location = new System.Drawing.Point(1126, 18);
            this.buttonClose.Name = "buttonClose";
            this.buttonClose.Size = new System.Drawing.Size(78, 30);
            this.buttonClose.TabIndex = 9;
            this.buttonClose.Text = "閉じる";
            this.buttonClose.UseVisualStyleBackColor = true;
            this.buttonClose.Click += new System.EventHandler(this.buttonClose_Click);
            // 
            // labelHeaderDescription
            // 
            this.labelHeaderDescription.AutoSize = true;
            this.labelHeaderDescription.Font = new System.Drawing.Font("Meiryo UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelHeaderDescription.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(219)))), ((int)(((byte)(254)))));
            this.labelHeaderDescription.Location = new System.Drawing.Point(221, 25);
            this.labelHeaderDescription.Name = "labelHeaderDescription";
            this.labelHeaderDescription.Size = new System.Drawing.Size(265, 15);
            this.labelHeaderDescription.TabIndex = 1;
            this.labelHeaderDescription.Text = "訪問診療・オンライン診療・医療扶助の一括資格取得";
            // 
            // labelHeaderTitle
            // 
            this.labelHeaderTitle.AutoSize = true;
            this.labelHeaderTitle.Font = new System.Drawing.Font("Meiryo UI", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelHeaderTitle.ForeColor = System.Drawing.Color.White;
            this.labelHeaderTitle.Location = new System.Drawing.Point(12, 13);
            this.labelHeaderTitle.Name = "labelHeaderTitle";
            this.labelHeaderTitle.Size = new System.Drawing.Size(177, 30);
            this.labelHeaderTitle.TabIndex = 0;
            this.labelHeaderTitle.Text = "Bulk Console";
            // 
            // panelActionSection
            // 
            this.panelActionSection.Controls.Add(this.panelExecutionWarning);
            this.panelActionSection.Controls.Add(this.panelMedicalCard);
            this.panelActionSection.Controls.Add(this.panelOnlineCard);
            this.panelActionSection.Controls.Add(this.panelHoumonCard);
            this.panelActionSection.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelActionSection.Location = new System.Drawing.Point(0, 58);
            this.panelActionSection.Name = "panelActionSection";
            this.panelActionSection.Padding = new System.Windows.Forms.Padding(16, 14, 16, 10);
            this.panelActionSection.Size = new System.Drawing.Size(1224, 286);
            this.panelActionSection.TabIndex = 1;
            // 
            // panelExecutionWarning
            // 
            this.panelExecutionWarning.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelExecutionWarning.Controls.Add(this.labelExecutionWarning);
            this.panelExecutionWarning.Location = new System.Drawing.Point(16, 246);
            this.panelExecutionWarning.Name = "panelExecutionWarning";
            this.panelExecutionWarning.Padding = new System.Windows.Forms.Padding(12, 7, 12, 7);
            this.panelExecutionWarning.Size = new System.Drawing.Size(1188, 30);
            this.panelExecutionWarning.TabIndex = 3;
            this.panelExecutionWarning.Visible = false;
            // 
            // labelExecutionWarning
            // 
            this.labelExecutionWarning.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelExecutionWarning.Font = new System.Drawing.Font("Meiryo UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelExecutionWarning.Location = new System.Drawing.Point(12, 7);
            this.labelExecutionWarning.Name = "labelExecutionWarning";
            this.labelExecutionWarning.Size = new System.Drawing.Size(1164, 16);
            this.labelExecutionWarning.TabIndex = 0;
            this.labelExecutionWarning.Text = "設定 → 取込設定 → OQSフォルダの場所 の設定が必要です。";
            this.labelExecutionWarning.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panelMedicalCard
            // 
            this.panelMedicalCard.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelMedicalCard.Controls.Add(this.labelMedicalMonthSuffix);
            this.panelMedicalCard.Controls.Add(this.labelMedicalYearSuffix);
            this.panelMedicalCard.Controls.Add(this.numericUpDownMedicalMonth);
            this.panelMedicalCard.Controls.Add(this.numericUpDownMedicalYear);
            this.panelMedicalCard.Controls.Add(this.labelMedicalMonthCaption);
            this.panelMedicalCard.Controls.Add(this.buttonToggleMedicalAuto);
            this.panelMedicalCard.Controls.Add(this.buttonRunMedicalOnce);
            this.panelMedicalCard.Controls.Add(this.buttonCancelMedical);
            this.panelMedicalCard.Controls.Add(this.labelMedicalMode);
            this.panelMedicalCard.Controls.Add(this.labelMedicalProgress);
            this.panelMedicalCard.Controls.Add(this.labelMedicalProgressDetail);
            this.panelMedicalCard.Controls.Add(this.labelMedicalTitle);
            this.panelMedicalCard.Location = new System.Drawing.Point(815, 14);
            this.panelMedicalCard.Name = "panelMedicalCard";
            this.panelMedicalCard.Size = new System.Drawing.Size(389, 224);
            this.panelMedicalCard.TabIndex = 2;
            // 
            // labelMedicalMonthSuffix
            // 
            this.labelMedicalMonthSuffix.AutoSize = true;
            this.labelMedicalMonthSuffix.Font = new System.Drawing.Font("Meiryo UI", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelMedicalMonthSuffix.Location = new System.Drawing.Point(176, 74);
            this.labelMedicalMonthSuffix.Name = "labelMedicalMonthSuffix";
            this.labelMedicalMonthSuffix.Size = new System.Drawing.Size(19, 15);
            this.labelMedicalMonthSuffix.TabIndex = 4;
            this.labelMedicalMonthSuffix.Text = "月";
            // 
            // labelMedicalYearSuffix
            // 
            this.labelMedicalYearSuffix.AutoSize = true;
            this.labelMedicalYearSuffix.Font = new System.Drawing.Font("Meiryo UI", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelMedicalYearSuffix.Location = new System.Drawing.Point(91, 74);
            this.labelMedicalYearSuffix.Name = "labelMedicalYearSuffix";
            this.labelMedicalYearSuffix.Size = new System.Drawing.Size(19, 15);
            this.labelMedicalYearSuffix.TabIndex = 2;
            this.labelMedicalYearSuffix.Text = "年";
            // 
            // numericUpDownMedicalMonth
            // 
            this.numericUpDownMedicalMonth.Font = new System.Drawing.Font("Meiryo UI", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.numericUpDownMedicalMonth.Location = new System.Drawing.Point(116, 70);
            this.numericUpDownMedicalMonth.Maximum = new decimal(new int[] {
            12,
            0,
            0,
            0});
            this.numericUpDownMedicalMonth.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownMedicalMonth.Name = "numericUpDownMedicalMonth";
            this.numericUpDownMedicalMonth.Size = new System.Drawing.Size(54, 22);
            this.numericUpDownMedicalMonth.TabIndex = 3;
            this.numericUpDownMedicalMonth.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numericUpDownMedicalMonth.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // numericUpDownMedicalYear
            // 
            this.numericUpDownMedicalYear.Font = new System.Drawing.Font("Meiryo UI", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.numericUpDownMedicalYear.Location = new System.Drawing.Point(20, 70);
            this.numericUpDownMedicalYear.Maximum = new decimal(new int[] {
            2100,
            0,
            0,
            0});
            this.numericUpDownMedicalYear.Minimum = new decimal(new int[] {
            1900,
            0,
            0,
            0});
            this.numericUpDownMedicalYear.Name = "numericUpDownMedicalYear";
            this.numericUpDownMedicalYear.Size = new System.Drawing.Size(65, 22);
            this.numericUpDownMedicalYear.TabIndex = 1;
            this.numericUpDownMedicalYear.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numericUpDownMedicalYear.Value = new decimal(new int[] {
            2026,
            0,
            0,
            0});
            // 
            // labelMedicalMonthCaption
            // 
            this.labelMedicalMonthCaption.AutoSize = true;
            this.labelMedicalMonthCaption.Font = new System.Drawing.Font("Meiryo UI", 8.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelMedicalMonthCaption.Location = new System.Drawing.Point(18, 48);
            this.labelMedicalMonthCaption.Name = "labelMedicalMonthCaption";
            this.labelMedicalMonthCaption.Size = new System.Drawing.Size(55, 15);
            this.labelMedicalMonthCaption.TabIndex = 1;
            this.labelMedicalMonthCaption.Text = "診療年月";
            // 
            // buttonToggleMedicalAuto
            // 
            this.buttonToggleMedicalAuto.Location = new System.Drawing.Point(137, 180);
            this.buttonToggleMedicalAuto.Name = "buttonToggleMedicalAuto";
            this.buttonToggleMedicalAuto.Size = new System.Drawing.Size(123, 32);
            this.buttonToggleMedicalAuto.TabIndex = 5;
            this.buttonToggleMedicalAuto.Text = "自動実行を開始";
            this.buttonToggleMedicalAuto.UseVisualStyleBackColor = true;
            this.buttonToggleMedicalAuto.Click += new System.EventHandler(this.buttonToggleMedicalAuto_Click);
            // 
            // buttonRunMedicalOnce
            // 
            this.buttonRunMedicalOnce.Location = new System.Drawing.Point(20, 180);
            this.buttonRunMedicalOnce.Name = "buttonRunMedicalOnce";
            this.buttonRunMedicalOnce.Size = new System.Drawing.Size(111, 32);
            this.buttonRunMedicalOnce.TabIndex = 4;
            this.buttonRunMedicalOnce.Text = "1回実行";
            this.buttonRunMedicalOnce.UseVisualStyleBackColor = true;
            this.buttonRunMedicalOnce.Click += new System.EventHandler(this.buttonRunMedicalOnce_Click);
            // 
            // buttonCancelMedical
            // 
            this.buttonCancelMedical.Location = new System.Drawing.Point(266, 180);
            this.buttonCancelMedical.Name = "buttonCancelMedical";
            this.buttonCancelMedical.Size = new System.Drawing.Size(94, 32);
            this.buttonCancelMedical.TabIndex = 8;
            this.buttonCancelMedical.Text = "中止";
            this.buttonCancelMedical.UseVisualStyleBackColor = true;
            this.buttonCancelMedical.Click += new System.EventHandler(this.buttonCancelMedical_Click);
            // 
            // labelMedicalMode
            // 
            this.labelMedicalMode.AutoSize = true;
            this.labelMedicalMode.Font = new System.Drawing.Font("Meiryo UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelMedicalMode.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
            this.labelMedicalMode.Location = new System.Drawing.Point(260, 18);
            this.labelMedicalMode.Name = "labelMedicalMode";
            this.labelMedicalMode.Size = new System.Drawing.Size(100, 15);
            this.labelMedicalMode.TabIndex = 3;
            this.labelMedicalMode.Text = "自動実行: 停止中";
            // 
            // labelMedicalProgress
            // 
            this.labelMedicalProgress.AutoSize = true;
            this.labelMedicalProgress.Font = new System.Drawing.Font("Meiryo UI", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelMedicalProgress.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(83)))), ((int)(((byte)(9)))));
            this.labelMedicalProgress.Location = new System.Drawing.Point(18, 104);
            this.labelMedicalProgress.Name = "labelMedicalProgress";
            this.labelMedicalProgress.Size = new System.Drawing.Size(47, 17);
            this.labelMedicalProgress.TabIndex = 6;
            this.labelMedicalProgress.Text = "待機中";
            // 
            // labelMedicalProgressDetail
            // 
            this.labelMedicalProgressDetail.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelMedicalProgressDetail.Font = new System.Drawing.Font("Meiryo UI", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelMedicalProgressDetail.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(71)))), ((int)(((byte)(85)))), ((int)(((byte)(105)))));
            this.labelMedicalProgressDetail.Location = new System.Drawing.Point(18, 124);
            this.labelMedicalProgressDetail.Name = "labelMedicalProgressDetail";
            this.labelMedicalProgressDetail.Size = new System.Drawing.Size(350, 18);
            this.labelMedicalProgressDetail.TabIndex = 7;
            this.labelMedicalProgressDetail.Text = "実行待ちです。";
            // 
            // labelMedicalTitle
            // 
            this.labelMedicalTitle.AutoSize = true;
            this.labelMedicalTitle.Font = new System.Drawing.Font("Meiryo UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelMedicalTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(23)))), ((int)(((byte)(42)))));
            this.labelMedicalTitle.Location = new System.Drawing.Point(16, 15);
            this.labelMedicalTitle.Name = "labelMedicalTitle";
            this.labelMedicalTitle.Size = new System.Drawing.Size(73, 20);
            this.labelMedicalTitle.TabIndex = 0;
            this.labelMedicalTitle.Text = "医療扶助";
            // 
            // panelOnlineCard
            // 
            this.panelOnlineCard.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelOnlineCard.Controls.Add(this.dateTimePickerOnlineTo);
            this.panelOnlineCard.Controls.Add(this.dateTimePickerOnlineFrom);
            this.panelOnlineCard.Controls.Add(this.labelOnlineRangeSeparator);
            this.panelOnlineCard.Controls.Add(this.labelOnlineRangeCaption);
            this.panelOnlineCard.Controls.Add(this.checkBoxOnlineUseConsent);
            this.panelOnlineCard.Controls.Add(this.buttonToggleOnlineAuto);
            this.panelOnlineCard.Controls.Add(this.buttonRunOnlineOnce);
            this.panelOnlineCard.Controls.Add(this.buttonCancelOnline);
            this.panelOnlineCard.Controls.Add(this.labelOnlineMode);
            this.panelOnlineCard.Controls.Add(this.labelOnlineProgress);
            this.panelOnlineCard.Controls.Add(this.labelOnlineProgressDetail);
            this.panelOnlineCard.Controls.Add(this.labelOnlineTitle);
            this.panelOnlineCard.Location = new System.Drawing.Point(416, 14);
            this.panelOnlineCard.Name = "panelOnlineCard";
            this.panelOnlineCard.Size = new System.Drawing.Size(389, 224);
            this.panelOnlineCard.TabIndex = 1;
            // 
            // dateTimePickerOnlineTo
            // 
            this.dateTimePickerOnlineTo.CustomFormat = "yyyy/MM/dd";
            this.dateTimePickerOnlineTo.Font = new System.Drawing.Font("Meiryo UI", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.dateTimePickerOnlineTo.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dateTimePickerOnlineTo.Location = new System.Drawing.Point(167, 70);
            this.dateTimePickerOnlineTo.Name = "dateTimePickerOnlineTo";
            this.dateTimePickerOnlineTo.Size = new System.Drawing.Size(112, 22);
            this.dateTimePickerOnlineTo.TabIndex = 4;
            // 
            // dateTimePickerOnlineFrom
            // 
            this.dateTimePickerOnlineFrom.CustomFormat = "yyyy/MM/dd";
            this.dateTimePickerOnlineFrom.Font = new System.Drawing.Font("Meiryo UI", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.dateTimePickerOnlineFrom.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dateTimePickerOnlineFrom.Location = new System.Drawing.Point(20, 70);
            this.dateTimePickerOnlineFrom.Name = "dateTimePickerOnlineFrom";
            this.dateTimePickerOnlineFrom.Size = new System.Drawing.Size(112, 22);
            this.dateTimePickerOnlineFrom.TabIndex = 3;
            // 
            // labelOnlineRangeSeparator
            // 
            this.labelOnlineRangeSeparator.AutoSize = true;
            this.labelOnlineRangeSeparator.Font = new System.Drawing.Font("Meiryo UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelOnlineRangeSeparator.Location = new System.Drawing.Point(140, 74);
            this.labelOnlineRangeSeparator.Name = "labelOnlineRangeSeparator";
            this.labelOnlineRangeSeparator.Size = new System.Drawing.Size(19, 15);
            this.labelOnlineRangeSeparator.TabIndex = 8;
            this.labelOnlineRangeSeparator.Text = "～";
            // 
            // labelOnlineRangeCaption
            // 
            this.labelOnlineRangeCaption.AutoSize = true;
            this.labelOnlineRangeCaption.Font = new System.Drawing.Font("Meiryo UI", 8.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelOnlineRangeCaption.Location = new System.Drawing.Point(18, 48);
            this.labelOnlineRangeCaption.Name = "labelOnlineRangeCaption";
            this.labelOnlineRangeCaption.Size = new System.Drawing.Size(43, 15);
            this.labelOnlineRangeCaption.TabIndex = 2;
            this.labelOnlineRangeCaption.Text = "取得日";
            // 
            // checkBoxOnlineUseConsent
            // 
            this.checkBoxOnlineUseConsent.AutoSize = true;
            this.checkBoxOnlineUseConsent.Font = new System.Drawing.Font("Meiryo UI", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.checkBoxOnlineUseConsent.Location = new System.Drawing.Point(67, 47);
            this.checkBoxOnlineUseConsent.Name = "checkBoxOnlineUseConsent";
            this.checkBoxOnlineUseConsent.Size = new System.Drawing.Size(96, 19);
            this.checkBoxOnlineUseConsent.TabIndex = 1;
            this.checkBoxOnlineUseConsent.Text = "同意日で取得";
            this.checkBoxOnlineUseConsent.UseVisualStyleBackColor = true;
            this.checkBoxOnlineUseConsent.CheckedChanged += new System.EventHandler(this.checkBoxOnlineUseConsent_CheckedChanged);
            // 
            // buttonToggleOnlineAuto
            // 
            this.buttonToggleOnlineAuto.Location = new System.Drawing.Point(143, 180);
            this.buttonToggleOnlineAuto.Name = "buttonToggleOnlineAuto";
            this.buttonToggleOnlineAuto.Size = new System.Drawing.Size(123, 32);
            this.buttonToggleOnlineAuto.TabIndex = 6;
            this.buttonToggleOnlineAuto.Text = "自動実行を開始";
            this.buttonToggleOnlineAuto.UseVisualStyleBackColor = true;
            this.buttonToggleOnlineAuto.Click += new System.EventHandler(this.buttonToggleOnlineAuto_Click);
            // 
            // buttonRunOnlineOnce
            // 
            this.buttonRunOnlineOnce.Location = new System.Drawing.Point(20, 180);
            this.buttonRunOnlineOnce.Name = "buttonRunOnlineOnce";
            this.buttonRunOnlineOnce.Size = new System.Drawing.Size(111, 32);
            this.buttonRunOnlineOnce.TabIndex = 5;
            this.buttonRunOnlineOnce.Text = "1回実行";
            this.buttonRunOnlineOnce.UseVisualStyleBackColor = true;
            this.buttonRunOnlineOnce.Click += new System.EventHandler(this.buttonRunOnlineOnce_Click);
            // 
            // buttonCancelOnline
            // 
            this.buttonCancelOnline.Location = new System.Drawing.Point(272, 180);
            this.buttonCancelOnline.Name = "buttonCancelOnline";
            this.buttonCancelOnline.Size = new System.Drawing.Size(94, 32);
            this.buttonCancelOnline.TabIndex = 12;
            this.buttonCancelOnline.Text = "中止";
            this.buttonCancelOnline.UseVisualStyleBackColor = true;
            this.buttonCancelOnline.Click += new System.EventHandler(this.buttonCancelOnline_Click);
            // 
            // labelOnlineMode
            // 
            this.labelOnlineMode.AutoSize = true;
            this.labelOnlineMode.Font = new System.Drawing.Font("Meiryo UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelOnlineMode.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
            this.labelOnlineMode.Location = new System.Drawing.Point(260, 18);
            this.labelOnlineMode.Name = "labelOnlineMode";
            this.labelOnlineMode.Size = new System.Drawing.Size(100, 15);
            this.labelOnlineMode.TabIndex = 9;
            this.labelOnlineMode.Text = "自動実行: 停止中";
            // 
            // labelOnlineProgress
            // 
            this.labelOnlineProgress.AutoSize = true;
            this.labelOnlineProgress.Font = new System.Drawing.Font("Meiryo UI", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelOnlineProgress.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(83)))), ((int)(((byte)(9)))));
            this.labelOnlineProgress.Location = new System.Drawing.Point(18, 104);
            this.labelOnlineProgress.Name = "labelOnlineProgress";
            this.labelOnlineProgress.Size = new System.Drawing.Size(47, 17);
            this.labelOnlineProgress.TabIndex = 10;
            this.labelOnlineProgress.Text = "待機中";
            // 
            // labelOnlineProgressDetail
            // 
            this.labelOnlineProgressDetail.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelOnlineProgressDetail.Font = new System.Drawing.Font("Meiryo UI", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelOnlineProgressDetail.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(71)))), ((int)(((byte)(85)))), ((int)(((byte)(105)))));
            this.labelOnlineProgressDetail.Location = new System.Drawing.Point(18, 124);
            this.labelOnlineProgressDetail.Name = "labelOnlineProgressDetail";
            this.labelOnlineProgressDetail.Size = new System.Drawing.Size(350, 18);
            this.labelOnlineProgressDetail.TabIndex = 11;
            this.labelOnlineProgressDetail.Text = "実行待ちです。";
            // 
            // labelOnlineTitle
            // 
            this.labelOnlineTitle.AutoSize = true;
            this.labelOnlineTitle.Font = new System.Drawing.Font("Meiryo UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelOnlineTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(23)))), ((int)(((byte)(42)))));
            this.labelOnlineTitle.Location = new System.Drawing.Point(16, 15);
            this.labelOnlineTitle.Name = "labelOnlineTitle";
            this.labelOnlineTitle.Size = new System.Drawing.Size(104, 20);
            this.labelOnlineTitle.TabIndex = 0;
            this.labelOnlineTitle.Text = "オンライン診療";
            // 
            // panelHoumonCard
            // 
            this.panelHoumonCard.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelHoumonCard.Controls.Add(this.dateTimePickerHoumonTo);
            this.panelHoumonCard.Controls.Add(this.dateTimePickerHoumonFrom);
            this.panelHoumonCard.Controls.Add(this.labelHoumonRangeSeparator);
            this.panelHoumonCard.Controls.Add(this.labelHoumonRangeCaption);
            this.panelHoumonCard.Controls.Add(this.buttonToggleHoumonAuto);
            this.panelHoumonCard.Controls.Add(this.buttonRunHoumonOnce);
            this.panelHoumonCard.Controls.Add(this.buttonCancelHoumon);
            this.panelHoumonCard.Controls.Add(this.labelHoumonMode);
            this.panelHoumonCard.Controls.Add(this.labelHoumonProgress);
            this.panelHoumonCard.Controls.Add(this.labelHoumonProgressDetail);
            this.panelHoumonCard.Controls.Add(this.labelHoumonTitle);
            this.panelHoumonCard.Location = new System.Drawing.Point(16, 14);
            this.panelHoumonCard.Name = "panelHoumonCard";
            this.panelHoumonCard.Size = new System.Drawing.Size(389, 224);
            this.panelHoumonCard.TabIndex = 0;
            // 
            // dateTimePickerHoumonTo
            // 
            this.dateTimePickerHoumonTo.CustomFormat = "yyyy/MM/dd";
            this.dateTimePickerHoumonTo.Font = new System.Drawing.Font("Meiryo UI", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.dateTimePickerHoumonTo.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dateTimePickerHoumonTo.Location = new System.Drawing.Point(167, 70);
            this.dateTimePickerHoumonTo.Name = "dateTimePickerHoumonTo";
            this.dateTimePickerHoumonTo.Size = new System.Drawing.Size(112, 22);
            this.dateTimePickerHoumonTo.TabIndex = 2;
            // 
            // dateTimePickerHoumonFrom
            // 
            this.dateTimePickerHoumonFrom.CustomFormat = "yyyy/MM/dd";
            this.dateTimePickerHoumonFrom.Font = new System.Drawing.Font("Meiryo UI", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.dateTimePickerHoumonFrom.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dateTimePickerHoumonFrom.Location = new System.Drawing.Point(20, 70);
            this.dateTimePickerHoumonFrom.Name = "dateTimePickerHoumonFrom";
            this.dateTimePickerHoumonFrom.Size = new System.Drawing.Size(112, 22);
            this.dateTimePickerHoumonFrom.TabIndex = 1;
            // 
            // labelHoumonRangeSeparator
            // 
            this.labelHoumonRangeSeparator.AutoSize = true;
            this.labelHoumonRangeSeparator.Font = new System.Drawing.Font("Meiryo UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelHoumonRangeSeparator.Location = new System.Drawing.Point(140, 74);
            this.labelHoumonRangeSeparator.Name = "labelHoumonRangeSeparator";
            this.labelHoumonRangeSeparator.Size = new System.Drawing.Size(19, 15);
            this.labelHoumonRangeSeparator.TabIndex = 7;
            this.labelHoumonRangeSeparator.Text = "～";
            // 
            // labelHoumonRangeCaption
            // 
            this.labelHoumonRangeCaption.AutoSize = true;
            this.labelHoumonRangeCaption.Font = new System.Drawing.Font("Meiryo UI", 8.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelHoumonRangeCaption.Location = new System.Drawing.Point(20, 48);
            this.labelHoumonRangeCaption.Name = "labelHoumonRangeCaption";
            this.labelHoumonRangeCaption.Size = new System.Drawing.Size(43, 15);
            this.labelHoumonRangeCaption.TabIndex = 6;
            this.labelHoumonRangeCaption.Text = "同意日";
            // 
            // buttonToggleHoumonAuto
            // 
            this.buttonToggleHoumonAuto.Location = new System.Drawing.Point(137, 180);
            this.buttonToggleHoumonAuto.Name = "buttonToggleHoumonAuto";
            this.buttonToggleHoumonAuto.Size = new System.Drawing.Size(123, 32);
            this.buttonToggleHoumonAuto.TabIndex = 4;
            this.buttonToggleHoumonAuto.Text = "自動実行を開始";
            this.buttonToggleHoumonAuto.UseVisualStyleBackColor = true;
            this.buttonToggleHoumonAuto.Click += new System.EventHandler(this.buttonToggleHoumonAuto_Click);
            // 
            // buttonRunHoumonOnce
            // 
            this.buttonRunHoumonOnce.Location = new System.Drawing.Point(20, 180);
            this.buttonRunHoumonOnce.Name = "buttonRunHoumonOnce";
            this.buttonRunHoumonOnce.Size = new System.Drawing.Size(111, 32);
            this.buttonRunHoumonOnce.TabIndex = 3;
            this.buttonRunHoumonOnce.Text = "1回実行";
            this.buttonRunHoumonOnce.UseVisualStyleBackColor = true;
            this.buttonRunHoumonOnce.Click += new System.EventHandler(this.buttonRunHoumonOnce_Click);
            // 
            // buttonCancelHoumon
            // 
            this.buttonCancelHoumon.Location = new System.Drawing.Point(266, 180);
            this.buttonCancelHoumon.Name = "buttonCancelHoumon";
            this.buttonCancelHoumon.Size = new System.Drawing.Size(94, 32);
            this.buttonCancelHoumon.TabIndex = 10;
            this.buttonCancelHoumon.Text = "中止";
            this.buttonCancelHoumon.UseVisualStyleBackColor = true;
            this.buttonCancelHoumon.Click += new System.EventHandler(this.buttonCancelHoumon_Click);
            // 
            // labelHoumonMode
            // 
            this.labelHoumonMode.AutoSize = true;
            this.labelHoumonMode.Font = new System.Drawing.Font("Meiryo UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelHoumonMode.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
            this.labelHoumonMode.Location = new System.Drawing.Point(260, 18);
            this.labelHoumonMode.Name = "labelHoumonMode";
            this.labelHoumonMode.Size = new System.Drawing.Size(100, 15);
            this.labelHoumonMode.TabIndex = 2;
            this.labelHoumonMode.Text = "自動実行: 停止中";
            // 
            // labelHoumonProgress
            // 
            this.labelHoumonProgress.AutoSize = true;
            this.labelHoumonProgress.Font = new System.Drawing.Font("Meiryo UI", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelHoumonProgress.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(83)))), ((int)(((byte)(9)))));
            this.labelHoumonProgress.Location = new System.Drawing.Point(18, 104);
            this.labelHoumonProgress.Name = "labelHoumonProgress";
            this.labelHoumonProgress.Size = new System.Drawing.Size(47, 17);
            this.labelHoumonProgress.TabIndex = 8;
            this.labelHoumonProgress.Text = "待機中";
            // 
            // labelHoumonProgressDetail
            // 
            this.labelHoumonProgressDetail.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelHoumonProgressDetail.Font = new System.Drawing.Font("Meiryo UI", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelHoumonProgressDetail.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(71)))), ((int)(((byte)(85)))), ((int)(((byte)(105)))));
            this.labelHoumonProgressDetail.Location = new System.Drawing.Point(18, 124);
            this.labelHoumonProgressDetail.Name = "labelHoumonProgressDetail";
            this.labelHoumonProgressDetail.Size = new System.Drawing.Size(350, 18);
            this.labelHoumonProgressDetail.TabIndex = 9;
            this.labelHoumonProgressDetail.Text = "実行待ちです。";
            // 
            // labelHoumonTitle
            // 
            this.labelHoumonTitle.AutoSize = true;
            this.labelHoumonTitle.Font = new System.Drawing.Font("Meiryo UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelHoumonTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(23)))), ((int)(((byte)(42)))));
            this.labelHoumonTitle.Location = new System.Drawing.Point(16, 15);
            this.labelHoumonTitle.Name = "labelHoumonTitle";
            this.labelHoumonTitle.Size = new System.Drawing.Size(73, 20);
            this.labelHoumonTitle.TabIndex = 0;
            this.labelHoumonTitle.Text = "訪問診療";
            // 
            // splitContainerMain
            // 
            this.splitContainerMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerMain.Location = new System.Drawing.Point(0, 344);
            this.splitContainerMain.Name = "splitContainerMain";
            this.splitContainerMain.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainerMain.Panel1
            // 
            this.splitContainerMain.Panel1.Controls.Add(this.splitContainerTop);
            this.splitContainerMain.Panel1.Padding = new System.Windows.Forms.Padding(16, 12, 16, 12);
            // 
            // splitContainerMain.Panel2
            // 
            this.splitContainerMain.Panel2.Controls.Add(this.dgvResults);
            this.splitContainerMain.Panel2.Controls.Add(this.buttonSendSelected);
            this.splitContainerMain.Panel2.Controls.Add(this.buttonRefreshResults);
            this.splitContainerMain.Panel2.Controls.Add(this.buttonToggleLog);
            this.splitContainerMain.Panel2.Controls.Add(this.labelResultCaption);
            this.splitContainerMain.Panel2.Controls.Add(this.buttonClearChecks);
            this.splitContainerMain.Panel2.Controls.Add(this.buttonCheckAll);
            this.splitContainerMain.Panel2.Padding = new System.Windows.Forms.Padding(16, 10, 16, 16);
            this.splitContainerMain.Size = new System.Drawing.Size(1224, 445);
            this.splitContainerMain.SplitterDistance = 122;
            this.splitContainerMain.TabIndex = 3;
            // 
            // splitContainerTop
            // 
            this.splitContainerTop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerTop.Location = new System.Drawing.Point(16, 12);
            this.splitContainerTop.Name = "splitContainerTop";
            // 
            // splitContainerTop.Panel1
            // 
            this.splitContainerTop.Panel1.Controls.Add(this.panelStatusCard);
            this.splitContainerTop.Panel1MinSize = 420;
            // 
            // splitContainerTop.Panel2
            // 
            this.splitContainerTop.Panel2.Controls.Add(this.listBoxLog);
            this.splitContainerTop.Panel2.Controls.Add(this.labelLogCaption);
            this.splitContainerTop.Panel2.Padding = new System.Windows.Forms.Padding(16, 12, 16, 12);
            this.splitContainerTop.Panel2MinSize = 320;
            this.splitContainerTop.Size = new System.Drawing.Size(1192, 98);
            this.splitContainerTop.SplitterDistance = 642;
            this.splitContainerTop.TabIndex = 0;
            // 
            // panelStatusCard
            // 
            this.panelStatusCard.Controls.Add(this.labelResultCount);
            this.panelStatusCard.Controls.Add(this.labelReceptionNumber);
            this.panelStatusCard.Controls.Add(this.labelDetail);
            this.panelStatusCard.Controls.Add(this.labelDetailCaption);
            this.panelStatusCard.Controls.Add(this.labelStatus);
            this.panelStatusCard.Controls.Add(this.labelStatusCaption);
            this.panelStatusCard.Controls.Add(this.labelProcessName);
            this.panelStatusCard.Controls.Add(this.labelCaption);
            this.panelStatusCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelStatusCard.Location = new System.Drawing.Point(0, 0);
            this.panelStatusCard.Name = "panelStatusCard";
            this.panelStatusCard.Padding = new System.Windows.Forms.Padding(18, 14, 18, 12);
            this.panelStatusCard.Size = new System.Drawing.Size(642, 98);
            this.panelStatusCard.TabIndex = 2;
            // 
            // labelResultCount
            // 
            this.labelResultCount.AutoSize = true;
            this.labelResultCount.Font = new System.Drawing.Font("Meiryo UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelResultCount.Location = new System.Drawing.Point(21, 96);
            this.labelResultCount.Name = "labelResultCount";
            this.labelResultCount.Size = new System.Drawing.Size(72, 15);
            this.labelResultCount.TabIndex = 7;
            this.labelResultCount.Text = "取得件数: 0";
            // 
            // labelReceptionNumber
            // 
            this.labelReceptionNumber.AutoSize = true;
            this.labelReceptionNumber.Font = new System.Drawing.Font("Meiryo UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelReceptionNumber.Location = new System.Drawing.Point(190, 96);
            this.labelReceptionNumber.Name = "labelReceptionNumber";
            this.labelReceptionNumber.Size = new System.Drawing.Size(70, 15);
            this.labelReceptionNumber.TabIndex = 6;
            this.labelReceptionNumber.Text = "受付番号: -";
            // 
            // labelDetail
            // 
            this.labelDetail.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelDetail.Font = new System.Drawing.Font("Meiryo UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelDetail.Location = new System.Drawing.Point(103, 71);
            this.labelDetail.Name = "labelDetail";
            this.labelDetail.Size = new System.Drawing.Size(519, 40);
            this.labelDetail.TabIndex = 5;
            this.labelDetail.Text = "Bulk 実行の開始待ちです。";
            // 
            // labelDetailCaption
            // 
            this.labelDetailCaption.AutoSize = true;
            this.labelDetailCaption.Font = new System.Drawing.Font("Meiryo UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelDetailCaption.Location = new System.Drawing.Point(21, 73);
            this.labelDetailCaption.Name = "labelDetailCaption";
            this.labelDetailCaption.Size = new System.Drawing.Size(60, 15);
            this.labelDetailCaption.TabIndex = 4;
            this.labelDetailCaption.Text = "詳細情報:";
            // 
            // labelStatus
            // 
            this.labelStatus.AutoSize = true;
            this.labelStatus.Font = new System.Drawing.Font("Meiryo UI", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelStatus.Location = new System.Drawing.Point(103, 39);
            this.labelStatus.Name = "labelStatus";
            this.labelStatus.Size = new System.Drawing.Size(67, 24);
            this.labelStatus.TabIndex = 3;
            this.labelStatus.Text = "待機中";
            // 
            // labelStatusCaption
            // 
            this.labelStatusCaption.AutoSize = true;
            this.labelStatusCaption.Font = new System.Drawing.Font("Meiryo UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelStatusCaption.Location = new System.Drawing.Point(21, 45);
            this.labelStatusCaption.Name = "labelStatusCaption";
            this.labelStatusCaption.Size = new System.Drawing.Size(60, 15);
            this.labelStatusCaption.TabIndex = 2;
            this.labelStatusCaption.Text = "現在状態:";
            // 
            // labelProcessName
            // 
            this.labelProcessName.AutoSize = true;
            this.labelProcessName.Font = new System.Drawing.Font("Meiryo UI", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelProcessName.Location = new System.Drawing.Point(103, 18);
            this.labelProcessName.Name = "labelProcessName";
            this.labelProcessName.Size = new System.Drawing.Size(71, 18);
            this.labelProcessName.TabIndex = 1;
            this.labelProcessName.Text = "BulkTool";
            // 
            // labelCaption
            // 
            this.labelCaption.AutoSize = true;
            this.labelCaption.Font = new System.Drawing.Font("Meiryo UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelCaption.Location = new System.Drawing.Point(21, 20);
            this.labelCaption.Name = "labelCaption";
            this.labelCaption.Size = new System.Drawing.Size(60, 15);
            this.labelCaption.TabIndex = 0;
            this.labelCaption.Text = "処理対象:";
            // 
            // listBoxLog
            // 
            this.listBoxLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listBoxLog.FormattingEnabled = true;
            this.listBoxLog.HorizontalScrollbar = true;
            this.listBoxLog.ItemHeight = 12;
            this.listBoxLog.Location = new System.Drawing.Point(73, 15);
            this.listBoxLog.Name = "listBoxLog";
            this.listBoxLog.Size = new System.Drawing.Size(451, 76);
            this.listBoxLog.TabIndex = 0;
            // 
            // labelLogCaption
            // 
            this.labelLogCaption.AutoSize = true;
            this.labelLogCaption.Font = new System.Drawing.Font("Meiryo UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelLogCaption.Location = new System.Drawing.Point(19, 15);
            this.labelLogCaption.Name = "labelLogCaption";
            this.labelLogCaption.Size = new System.Drawing.Size(55, 17);
            this.labelLogCaption.TabIndex = 1;
            this.labelLogCaption.Text = "進捗ログ";
            // 
            // dgvResults
            // 
            this.dgvResults.AllowUserToAddRows = false;
            this.dgvResults.AllowUserToDeleteRows = false;
            this.dgvResults.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvResults.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvResults.Location = new System.Drawing.Point(22, 37);
            this.dgvResults.MultiSelect = false;
            this.dgvResults.Name = "dgvResults";
            this.dgvResults.ReadOnly = true;
            this.dgvResults.RowHeadersVisible = false;
            this.dgvResults.RowTemplate.Height = 21;
            this.dgvResults.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvResults.Size = new System.Drawing.Size(1186, 266);
            this.dgvResults.TabIndex = 0;
            this.dgvResults.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvResults_CellDoubleClick);
            this.dgvResults.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvResults_CellValueChanged);
            this.dgvResults.CurrentCellDirtyStateChanged += new System.EventHandler(this.dgvResults_CurrentCellDirtyStateChanged);
            // 
            // buttonSendSelected
            // 
            this.buttonSendSelected.Location = new System.Drawing.Point(433, 7);
            this.buttonSendSelected.Name = "buttonSendSelected";
            this.buttonSendSelected.Size = new System.Drawing.Size(124, 30);
            this.buttonSendSelected.TabIndex = 11;
            this.buttonSendSelected.Text = "選択レコードを送信";
            this.buttonSendSelected.UseVisualStyleBackColor = true;
            this.buttonSendSelected.Click += new System.EventHandler(this.buttonSendSelected_Click);
            // 
            // buttonRefreshResults
            // 
            this.buttonRefreshResults.Location = new System.Drawing.Point(111, 7);
            this.buttonRefreshResults.Name = "buttonRefreshResults";
            this.buttonRefreshResults.Size = new System.Drawing.Size(37, 30);
            this.buttonRefreshResults.TabIndex = 12;
            this.buttonRefreshResults.Text = "更新";
            this.buttonRefreshResults.UseVisualStyleBackColor = true;
            this.buttonRefreshResults.Click += new System.EventHandler(this.buttonRefreshResults_Click);
            // 
            // buttonToggleLog
            // 
            this.buttonToggleLog.Location = new System.Drawing.Point(154, 7);
            this.buttonToggleLog.Name = "buttonToggleLog";
            this.buttonToggleLog.Size = new System.Drawing.Size(89, 30);
            this.buttonToggleLog.TabIndex = 13;
            this.buttonToggleLog.Text = "ログ表示";
            this.buttonToggleLog.UseVisualStyleBackColor = true;
            this.buttonToggleLog.Click += new System.EventHandler(this.buttonToggleLog_Click);
            // 
            // labelResultCaption
            // 
            this.labelResultCaption.AutoSize = true;
            this.labelResultCaption.Font = new System.Drawing.Font("Meiryo UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelResultCaption.Location = new System.Drawing.Point(19, 12);
            this.labelResultCaption.Name = "labelResultCaption";
            this.labelResultCaption.Size = new System.Drawing.Size(86, 17);
            this.labelResultCaption.TabIndex = 1;
            this.labelResultCaption.Text = "取得資格一覧";
            // 
            // buttonClearChecks
            // 
            this.buttonClearChecks.Location = new System.Drawing.Point(341, 7);
            this.buttonClearChecks.Name = "buttonClearChecks";
            this.buttonClearChecks.Size = new System.Drawing.Size(86, 30);
            this.buttonClearChecks.TabIndex = 10;
            this.buttonClearChecks.Text = "選択解除";
            this.buttonClearChecks.UseVisualStyleBackColor = true;
            this.buttonClearChecks.Click += new System.EventHandler(this.buttonClearChecks_Click);
            // 
            // buttonCheckAll
            // 
            this.buttonCheckAll.Location = new System.Drawing.Point(249, 7);
            this.buttonCheckAll.Name = "buttonCheckAll";
            this.buttonCheckAll.Size = new System.Drawing.Size(86, 30);
            this.buttonCheckAll.TabIndex = 9;
            this.buttonCheckAll.Text = "全件チェック";
            this.buttonCheckAll.UseVisualStyleBackColor = true;
            this.buttonCheckAll.Click += new System.EventHandler(this.buttonCheckAll_Click);
            // 
            // FormBulkExecutionStatus
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1224, 789);
            this.Controls.Add(this.splitContainerMain);
            this.Controls.Add(this.panelActionSection);
            this.Controls.Add(this.panelHeader);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(1120, 760);
            this.Name = "FormBulkExecutionStatus";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Bulk ステータス";
            this.panelHeader.ResumeLayout(false);
            this.panelHeader.PerformLayout();
            this.panelActionSection.ResumeLayout(false);
            this.panelExecutionWarning.ResumeLayout(false);
            this.panelMedicalCard.ResumeLayout(false);
            this.panelMedicalCard.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownMedicalMonth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownMedicalYear)).EndInit();
            this.panelOnlineCard.ResumeLayout(false);
            this.panelOnlineCard.PerformLayout();
            this.panelHoumonCard.ResumeLayout(false);
            this.panelHoumonCard.PerformLayout();
            this.splitContainerMain.Panel1.ResumeLayout(false);
            this.splitContainerMain.Panel2.ResumeLayout(false);
            this.splitContainerMain.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).EndInit();
            this.splitContainerMain.ResumeLayout(false);
            this.splitContainerTop.Panel1.ResumeLayout(false);
            this.splitContainerTop.Panel2.ResumeLayout(false);
            this.splitContainerTop.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerTop)).EndInit();
            this.splitContainerTop.ResumeLayout(false);
            this.panelStatusCard.ResumeLayout(false);
            this.panelStatusCard.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvResults)).EndInit();
            this.ResumeLayout(false);

        }
    }
}
