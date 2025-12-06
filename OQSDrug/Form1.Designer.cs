namespace OQSDrug
{
    partial class Form1
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージド リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.listViewLog = new System.Windows.Forms.ListView();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.checkBoxAutoview = new System.Windows.Forms.CheckBox();
            this.StartStop = new System.Windows.Forms.CheckBox();
            this.checkBoxAutoStart = new System.Windows.Forms.CheckBox();
            this.checkBoxAutoTKK = new System.Windows.Forms.CheckBox();
            this.checkBoxAutoSR = new System.Windows.Forms.CheckBox();
            this.buttonReload = new System.Windows.Forms.Button();
            this.buttonYZ = new System.Windows.Forms.Button();
            this.buttonSR = new System.Windows.Forms.Button();
            this.buttonYZXML = new System.Windows.Forms.Button();
            this.buttonKS = new System.Windows.Forms.Button();
            this.buttonKSXML = new System.Windows.Forms.Button();
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.toolStripVersion = new OQSDrug.ClickThroughToolStrip();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.toolStripTextBoxPtIDmain = new System.Windows.Forms.ToolStripTextBox();
            this.toolStripButtonViewer = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonExit = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonToTaskTray = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonVersion = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonLog = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonSettings = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonTKK = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonSinryo = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparatorDebug2 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonPMDA = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparatorDebug1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripComboBoxDBProviders = new System.Windows.Forms.ToolStripComboBox();
            this.toolStripComboBoxConnectionMode = new System.Windows.Forms.ToolStripComboBox();
            this.toolStripTextBoxDebug = new System.Windows.Forms.ToolStripTextBox();
            this.toolStripButtonDebug = new System.Windows.Forms.ToolStripButton();
            this.buttonMDB = new System.Windows.Forms.Button();
            this.buttonPG = new System.Windows.Forms.Button();
            this.buttonDynamics = new System.Windows.Forms.Button();
            this.buttonOQSFolder = new System.Windows.Forms.Button();
            this.pictureBoxDB = new System.Windows.Forms.PictureBox();
            this.pictureBoxDynamics = new System.Windows.Forms.PictureBox();
            this.pictureBoxOQSFolder = new System.Windows.Forms.PictureBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.toolStripVersion.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxDB)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxDynamics)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxOQSFolder)).BeginInit();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(12, 273);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.RowTemplate.Height = 21;
            this.dataGridView1.Size = new System.Drawing.Size(949, 432);
            this.dataGridView1.TabIndex = 2;
            this.dataGridView1.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellDoubleClick);
            this.dataGridView1.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.dataGridView1_CellFormatting);
            this.dataGridView1.CellMouseDown += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridView1_CellMouseDown);
            this.dataGridView1.CellMouseUp += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridView1_CellMouseUp);
            this.dataGridView1.CellToolTipTextNeeded += new System.Windows.Forms.DataGridViewCellToolTipTextNeededEventHandler(this.dataGridView1_CellToolTipTextNeeded);
            // 
            // listViewLog
            // 
            this.listViewLog.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listViewLog.FullRowSelect = true;
            this.listViewLog.HideSelection = false;
            this.listViewLog.Location = new System.Drawing.Point(422, 91);
            this.listViewLog.Name = "listViewLog";
            this.listViewLog.Size = new System.Drawing.Size(539, 176);
            this.listViewLog.TabIndex = 8;
            this.toolTip1.SetToolTip(this.listViewLog, "ログを表示します");
            this.listViewLog.UseCompatibleStateImageBehavior = false;
            this.listViewLog.View = System.Windows.Forms.View.Details;
            this.listViewLog.SizeChanged += new System.EventHandler(this.listViewLog_SizeChanged);
            // 
            // toolTip1
            // 
            this.toolTip1.AutoPopDelay = 10100;
            this.toolTip1.InitialDelay = 300;
            this.toolTip1.ReshowDelay = 82;
            // 
            // checkBoxAutoview
            // 
            this.checkBoxAutoview.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxAutoview.Font = new System.Drawing.Font("Meiryo UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.checkBoxAutoview.Image = ((System.Drawing.Image)(resources.GetObject("checkBoxAutoview.Image")));
            this.checkBoxAutoview.Location = new System.Drawing.Point(6, 16);
            this.checkBoxAutoview.Name = "checkBoxAutoview";
            this.checkBoxAutoview.Size = new System.Drawing.Size(84, 33);
            this.checkBoxAutoview.TabIndex = 27;
            this.checkBoxAutoview.Text = "薬歴";
            this.checkBoxAutoview.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.toolTip1.SetToolTip(this.checkBoxAutoview, "RSBase/ダイナと連動して薬歴が存在すれば自動で表示します");
            this.checkBoxAutoview.UseVisualStyleBackColor = true;
            this.checkBoxAutoview.CheckedChanged += new System.EventHandler(this.checkBoxAutoview_CheckedChanged);
            // 
            // StartStop
            // 
            this.StartStop.Appearance = System.Windows.Forms.Appearance.Button;
            this.StartStop.CheckAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.StartStop.Enabled = false;
            this.StartStop.Font = new System.Drawing.Font("Meiryo UI", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.StartStop.Image = ((System.Drawing.Image)(resources.GetObject("StartStop.Image")));
            this.StartStop.Location = new System.Drawing.Point(13, 30);
            this.StartStop.Name = "StartStop";
            this.StartStop.Size = new System.Drawing.Size(280, 45);
            this.StartStop.TabIndex = 6;
            this.StartStop.Text = "開始";
            this.StartStop.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.StartStop.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.toolTip1.SetToolTip(this.StartStop, "情報取得処理を開始/終了します");
            this.StartStop.UseVisualStyleBackColor = true;
            this.StartStop.CheckedChanged += new System.EventHandler(this.StartStop_CheckedChanged);
            // 
            // checkBoxAutoStart
            // 
            this.checkBoxAutoStart.Font = new System.Drawing.Font("Meiryo UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.checkBoxAutoStart.Image = ((System.Drawing.Image)(resources.GetObject("checkBoxAutoStart.Image")));
            this.checkBoxAutoStart.Location = new System.Drawing.Point(299, 40);
            this.checkBoxAutoStart.Name = "checkBoxAutoStart";
            this.checkBoxAutoStart.Size = new System.Drawing.Size(113, 45);
            this.checkBoxAutoStart.TabIndex = 29;
            this.checkBoxAutoStart.Text = "自動開始";
            this.checkBoxAutoStart.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.checkBoxAutoStart.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.toolTip1.SetToolTip(this.checkBoxAutoStart, "下の接続がOKになると自動で動作開始/停止します");
            this.checkBoxAutoStart.UseVisualStyleBackColor = true;
            this.checkBoxAutoStart.CheckedChanged += new System.EventHandler(this.checkBoxAutoStart_CheckedChanged);
            // 
            // checkBoxAutoTKK
            // 
            this.checkBoxAutoTKK.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxAutoTKK.Font = new System.Drawing.Font("Meiryo UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.checkBoxAutoTKK.Image = ((System.Drawing.Image)(resources.GetObject("checkBoxAutoTKK.Image")));
            this.checkBoxAutoTKK.Location = new System.Drawing.Point(96, 16);
            this.checkBoxAutoTKK.Name = "checkBoxAutoTKK";
            this.checkBoxAutoTKK.Size = new System.Drawing.Size(84, 33);
            this.checkBoxAutoTKK.TabIndex = 28;
            this.checkBoxAutoTKK.Text = "健診";
            this.checkBoxAutoTKK.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.toolTip1.SetToolTip(this.checkBoxAutoTKK, "特定健診履歴を連動表示します");
            this.checkBoxAutoTKK.UseVisualStyleBackColor = true;
            this.checkBoxAutoTKK.CheckedChanged += new System.EventHandler(this.checkBoxAutoview_CheckedChanged);
            // 
            // checkBoxAutoSR
            // 
            this.checkBoxAutoSR.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxAutoSR.Font = new System.Drawing.Font("Meiryo UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.checkBoxAutoSR.Image = ((System.Drawing.Image)(resources.GetObject("checkBoxAutoSR.Image")));
            this.checkBoxAutoSR.Location = new System.Drawing.Point(186, 16);
            this.checkBoxAutoSR.Name = "checkBoxAutoSR";
            this.checkBoxAutoSR.Size = new System.Drawing.Size(84, 33);
            this.checkBoxAutoSR.TabIndex = 29;
            this.checkBoxAutoSR.Text = "診療";
            this.checkBoxAutoSR.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.toolTip1.SetToolTip(this.checkBoxAutoSR, "診療手術情報を連動表示します");
            this.checkBoxAutoSR.UseVisualStyleBackColor = true;
            this.checkBoxAutoSR.CheckedChanged += new System.EventHandler(this.checkBoxAutoview_CheckedChanged);
            // 
            // buttonReload
            // 
            this.buttonReload.Font = new System.Drawing.Font("Meiryo UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.buttonReload.Image = ((System.Drawing.Image)(resources.GetObject("buttonReload.Image")));
            this.buttonReload.Location = new System.Drawing.Point(96, 240);
            this.buttonReload.Name = "buttonReload";
            this.buttonReload.Size = new System.Drawing.Size(32, 27);
            this.buttonReload.TabIndex = 28;
            this.toolTip1.SetToolTip(this.buttonReload, "取得結果を更新します");
            this.buttonReload.UseVisualStyleBackColor = true;
            this.buttonReload.Click += new System.EventHandler(this.buttonReload_Click);
            // 
            // buttonYZ
            // 
            this.buttonYZ.Enabled = false;
            this.buttonYZ.Location = new System.Drawing.Point(8, 18);
            this.buttonYZ.Name = "buttonYZ";
            this.buttonYZ.Size = new System.Drawing.Size(140, 23);
            this.buttonYZ.TabIndex = 19;
            this.buttonYZ.Text = "薬剤情報";
            this.buttonYZ.UseVisualStyleBackColor = true;
            // 
            // buttonSR
            // 
            this.buttonSR.Enabled = false;
            this.buttonSR.Location = new System.Drawing.Point(148, 18);
            this.buttonSR.Name = "buttonSR";
            this.buttonSR.Size = new System.Drawing.Size(140, 23);
            this.buttonSR.TabIndex = 20;
            this.buttonSR.Text = "診療情報";
            this.buttonSR.UseVisualStyleBackColor = true;
            // 
            // buttonYZXML
            // 
            this.buttonYZXML.Enabled = false;
            this.buttonYZXML.Location = new System.Drawing.Point(294, 18);
            this.buttonYZXML.Name = "buttonYZXML";
            this.buttonYZXML.Size = new System.Drawing.Size(37, 23);
            this.buttonYZXML.TabIndex = 22;
            this.buttonYZXML.Text = "XML";
            this.buttonYZXML.UseVisualStyleBackColor = true;
            // 
            // buttonKS
            // 
            this.buttonKS.Enabled = false;
            this.buttonKS.Location = new System.Drawing.Point(8, 39);
            this.buttonKS.Name = "buttonKS";
            this.buttonKS.Size = new System.Drawing.Size(280, 23);
            this.buttonKS.TabIndex = 23;
            this.buttonKS.Text = "健診情報";
            this.buttonKS.UseVisualStyleBackColor = true;
            // 
            // buttonKSXML
            // 
            this.buttonKSXML.Enabled = false;
            this.buttonKSXML.Location = new System.Drawing.Point(294, 39);
            this.buttonKSXML.Name = "buttonKSXML";
            this.buttonKSXML.Size = new System.Drawing.Size(37, 23);
            this.buttonKSXML.TabIndex = 25;
            this.buttonKSXML.Text = "XML";
            this.buttonKSXML.UseVisualStyleBackColor = true;
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = "OQSDrug\r\n右クリックでメニュー表示\r\nダブルクリックで表示非表示切替";
            this.notifyIcon1.Visible = true;
            this.notifyIcon1.DoubleClick += new System.EventHandler(this.notifyIcon1_DoubleClick);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.checkBoxAutoSR);
            this.groupBox1.Controls.Add(this.checkBoxAutoTKK);
            this.groupBox1.Controls.Add(this.checkBoxAutoview);
            this.groupBox1.Font = new System.Drawing.Font("Meiryo UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.groupBox1.Location = new System.Drawing.Point(422, 30);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(282, 55);
            this.groupBox1.TabIndex = 30;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "ID連携";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Meiryo UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.label1.Location = new System.Drawing.Point(17, 243);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(73, 20);
            this.label1.TabIndex = 31;
            this.label1.Text = "取込結果";
            // 
            // toolStripVersion
            // 
            this.toolStripVersion.Font = new System.Drawing.Font("游ゴシック", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.toolStripVersion.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripLabel1,
            this.toolStripTextBoxPtIDmain,
            this.toolStripButtonViewer,
            this.toolStripButtonExit,
            this.toolStripButtonToTaskTray,
            this.toolStripSeparator1,
            this.toolStripButtonVersion,
            this.toolStripButtonLog,
            this.toolStripSeparator2,
            this.toolStripButtonSettings,
            this.toolStripButtonTKK,
            this.toolStripButtonSinryo,
            this.toolStripSeparatorDebug2,
            this.toolStripButtonPMDA,
            this.toolStripSeparatorDebug1,
            this.toolStripComboBoxDBProviders,
            this.toolStripComboBoxConnectionMode,
            this.toolStripTextBoxDebug,
            this.toolStripButtonDebug});
            this.toolStripVersion.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow;
            this.toolStripVersion.Location = new System.Drawing.Point(0, 0);
            this.toolStripVersion.Name = "toolStripVersion";
            this.toolStripVersion.Size = new System.Drawing.Size(973, 27);
            this.toolStripVersion.Stretch = true;
            this.toolStripVersion.TabIndex = 3;
            this.toolStripVersion.Text = "toolStrip1";
            this.toolStripVersion.DoubleClick += new System.EventHandler(this.toolStripVersion_DoubleClick);
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(24, 24);
            this.toolStripLabel1.Text = "ID";
            this.toolStripLabel1.ToolTipText = "枝番なしIDを入力し右のいずれかのボタンを押すと\r\n薬歴、健診歴等のウインドウが開きます\r\n空欄だと患者選択無しでウインドウが開きます\r\n";
            // 
            // toolStripTextBoxPtIDmain
            // 
            this.toolStripTextBoxPtIDmain.Font = new System.Drawing.Font("Yu Gothic UI", 9F);
            this.toolStripTextBoxPtIDmain.Name = "toolStripTextBoxPtIDmain";
            this.toolStripTextBoxPtIDmain.Size = new System.Drawing.Size(70, 27);
            this.toolStripTextBoxPtIDmain.ToolTipText = "枝番なしIDを入力し右のいずれかのボタンを押すと\r\n薬歴、健診歴等のウインドウが開きます\r\n空欄だと患者選択無しでウインドウが開きます";
            this.toolStripTextBoxPtIDmain.KeyDown += new System.Windows.Forms.KeyEventHandler(this.toolStripTextBoxPtIDmain_KeyDown);
            // 
            // toolStripButtonViewer
            // 
            this.toolStripButtonViewer.Font = new System.Drawing.Font("游ゴシック", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.toolStripButtonViewer.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonViewer.Image")));
            this.toolStripButtonViewer.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonViewer.Name = "toolStripButtonViewer";
            this.toolStripButtonViewer.Size = new System.Drawing.Size(59, 24);
            this.toolStripButtonViewer.Text = "薬歴";
            this.toolStripButtonViewer.ToolTipText = "薬歴を表示します";
            this.toolStripButtonViewer.Click += new System.EventHandler(this.toolStripButtonDI_Click);
            // 
            // toolStripButtonExit
            // 
            this.toolStripButtonExit.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripButtonExit.Font = new System.Drawing.Font("游ゴシック", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.toolStripButtonExit.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonExit.Image")));
            this.toolStripButtonExit.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonExit.Name = "toolStripButtonExit";
            this.toolStripButtonExit.Size = new System.Drawing.Size(59, 24);
            this.toolStripButtonExit.Text = "終了";
            this.toolStripButtonExit.Click += new System.EventHandler(this.buttonExit_Click);
            // 
            // toolStripButtonToTaskTray
            // 
            this.toolStripButtonToTaskTray.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripButtonToTaskTray.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonToTaskTray.Image")));
            this.toolStripButtonToTaskTray.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonToTaskTray.Name = "toolStripButtonToTaskTray";
            this.toolStripButtonToTaskTray.Size = new System.Drawing.Size(23, 24);
            this.toolStripButtonToTaskTray.ToolTipText = "タスクトレイに最小化します";
            this.toolStripButtonToTaskTray.Click += new System.EventHandler(this.toolStripButtonToTaskTray_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 27);
            // 
            // toolStripButtonVersion
            // 
            this.toolStripButtonVersion.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripButtonVersion.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonVersion.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonVersion.Image")));
            this.toolStripButtonVersion.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonVersion.Name = "toolStripButtonVersion";
            this.toolStripButtonVersion.Size = new System.Drawing.Size(23, 24);
            this.toolStripButtonVersion.Text = "Version";
            this.toolStripButtonVersion.Click += new System.EventHandler(this.toolStripButtonVersion_Click);
            // 
            // toolStripButtonLog
            // 
            this.toolStripButtonLog.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripButtonLog.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonLog.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonLog.Image")));
            this.toolStripButtonLog.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonLog.Name = "toolStripButtonLog";
            this.toolStripButtonLog.Size = new System.Drawing.Size(23, 24);
            this.toolStripButtonLog.Text = "Log";
            this.toolStripButtonLog.ToolTipText = "ログを開きます";
            this.toolStripButtonLog.Click += new System.EventHandler(this.toolStripButtonLog_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 27);
            // 
            // toolStripButtonSettings
            // 
            this.toolStripButtonSettings.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripButtonSettings.Font = new System.Drawing.Font("游ゴシック", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.toolStripButtonSettings.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonSettings.Image")));
            this.toolStripButtonSettings.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonSettings.Name = "toolStripButtonSettings";
            this.toolStripButtonSettings.Size = new System.Drawing.Size(59, 24);
            this.toolStripButtonSettings.Text = "設定";
            this.toolStripButtonSettings.Click += new System.EventHandler(this.toolStripButtonSettings_Click);
            // 
            // toolStripButtonTKK
            // 
            this.toolStripButtonTKK.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonTKK.Image")));
            this.toolStripButtonTKK.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonTKK.Name = "toolStripButtonTKK";
            this.toolStripButtonTKK.Size = new System.Drawing.Size(59, 24);
            this.toolStripButtonTKK.Text = "健診";
            this.toolStripButtonTKK.ToolTipText = "健診結果を表示します";
            this.toolStripButtonTKK.Click += new System.EventHandler(this.toolStripButtonTKK_Click);
            // 
            // toolStripButtonSinryo
            // 
            this.toolStripButtonSinryo.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonSinryo.Image")));
            this.toolStripButtonSinryo.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonSinryo.Name = "toolStripButtonSinryo";
            this.toolStripButtonSinryo.Size = new System.Drawing.Size(89, 24);
            this.toolStripButtonSinryo.Text = "診療手術";
            this.toolStripButtonSinryo.ToolTipText = "診療手術情報を表示します";
            this.toolStripButtonSinryo.Click += new System.EventHandler(this.toolStripButtonSinryo_Click);
            // 
            // toolStripSeparatorDebug2
            // 
            this.toolStripSeparatorDebug2.Name = "toolStripSeparatorDebug2";
            this.toolStripSeparatorDebug2.Size = new System.Drawing.Size(6, 27);
            // 
            // toolStripButtonPMDA
            // 
            this.toolStripButtonPMDA.Image = global::OQSDrug.Properties.Resources.PMDA;
            this.toolStripButtonPMDA.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonPMDA.Name = "toolStripButtonPMDA";
            this.toolStripButtonPMDA.Size = new System.Drawing.Size(104, 24);
            this.toolStripButtonPMDA.Text = "PMDA薬情";
            this.toolStripButtonPMDA.Click += new System.EventHandler(this.toolStripButtonPMDA_Click);
            // 
            // toolStripSeparatorDebug1
            // 
            this.toolStripSeparatorDebug1.Name = "toolStripSeparatorDebug1";
            this.toolStripSeparatorDebug1.Size = new System.Drawing.Size(6, 27);
            this.toolStripSeparatorDebug1.Visible = false;
            // 
            // toolStripComboBoxDBProviders
            // 
            this.toolStripComboBoxDBProviders.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.toolStripComboBoxDBProviders.Name = "toolStripComboBoxDBProviders";
            this.toolStripComboBoxDBProviders.Size = new System.Drawing.Size(180, 27);
            this.toolStripComboBoxDBProviders.ToolTipText = "OleDbプロバイダを設定します";
            this.toolStripComboBoxDBProviders.Visible = false;
            this.toolStripComboBoxDBProviders.SelectedIndexChanged += new System.EventHandler(this.toolStripComboBoxDBProviders_SelectedIndexChanged);
            // 
            // toolStripComboBoxConnectionMode
            // 
            this.toolStripComboBoxConnectionMode.AutoSize = false;
            this.toolStripComboBoxConnectionMode.Name = "toolStripComboBoxConnectionMode";
            this.toolStripComboBoxConnectionMode.Size = new System.Drawing.Size(40, 23);
            this.toolStripComboBoxConnectionMode.ToolTipText = "データベース接続モード";
            this.toolStripComboBoxConnectionMode.Visible = false;
            // 
            // toolStripTextBoxDebug
            // 
            this.toolStripTextBoxDebug.AutoSize = false;
            this.toolStripTextBoxDebug.Font = new System.Drawing.Font("Yu Gothic UI", 9F);
            this.toolStripTextBoxDebug.Name = "toolStripTextBoxDebug";
            this.toolStripTextBoxDebug.Size = new System.Drawing.Size(60, 27);
            this.toolStripTextBoxDebug.ToolTipText = "デバッグ用xmlの患者ID(枝番付)";
            this.toolStripTextBoxDebug.Visible = false;
            // 
            // toolStripButtonDebug
            // 
            this.toolStripButtonDebug.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonDebug.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonDebug.Image")));
            this.toolStripButtonDebug.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonDebug.Name = "toolStripButtonDebug";
            this.toolStripButtonDebug.Size = new System.Drawing.Size(58, 24);
            this.toolStripButtonDebug.Text = "Debug";
            this.toolStripButtonDebug.ToolTipText = "xmlファイルを読み込みます";
            this.toolStripButtonDebug.Visible = false;
            this.toolStripButtonDebug.Click += new System.EventHandler(this.toolStripButtonDebug_Click);
            // 
            // buttonMDB
            // 
            this.buttonMDB.Enabled = false;
            this.buttonMDB.Location = new System.Drawing.Point(8, 65);
            this.buttonMDB.Margin = new System.Windows.Forms.Padding(0);
            this.buttonMDB.Name = "buttonMDB";
            this.buttonMDB.Size = new System.Drawing.Size(137, 23);
            this.buttonMDB.TabIndex = 32;
            this.buttonMDB.Text = "OQSDrug_data.mdb";
            this.buttonMDB.UseVisualStyleBackColor = true;
            // 
            // buttonPG
            // 
            this.buttonPG.Enabled = false;
            this.buttonPG.Location = new System.Drawing.Point(148, 65);
            this.buttonPG.Margin = new System.Windows.Forms.Padding(0);
            this.buttonPG.Name = "buttonPG";
            this.buttonPG.Size = new System.Drawing.Size(140, 23);
            this.buttonPG.TabIndex = 33;
            this.buttonPG.Text = "PostgreSQL";
            this.buttonPG.UseVisualStyleBackColor = true;
            // 
            // buttonDynamics
            // 
            this.buttonDynamics.Enabled = false;
            this.buttonDynamics.Location = new System.Drawing.Point(7, 95);
            this.buttonDynamics.Name = "buttonDynamics";
            this.buttonDynamics.Size = new System.Drawing.Size(281, 23);
            this.buttonDynamics.TabIndex = 34;
            this.buttonDynamics.Text = "ダイナミクス";
            this.buttonDynamics.UseVisualStyleBackColor = true;
            // 
            // buttonOQSFolder
            // 
            this.buttonOQSFolder.Enabled = false;
            this.buttonOQSFolder.Location = new System.Drawing.Point(7, 124);
            this.buttonOQSFolder.Name = "buttonOQSFolder";
            this.buttonOQSFolder.Size = new System.Drawing.Size(281, 23);
            this.buttonOQSFolder.TabIndex = 35;
            this.buttonOQSFolder.Text = "OQSFolder";
            this.buttonOQSFolder.UseVisualStyleBackColor = true;
            // 
            // pictureBoxDB
            // 
            this.pictureBoxDB.Image = global::OQSDrug.Properties.Resources.Hourglass;
            this.pictureBoxDB.Location = new System.Drawing.Point(294, 67);
            this.pictureBoxDB.Name = "pictureBoxDB";
            this.pictureBoxDB.Size = new System.Drawing.Size(22, 22);
            this.pictureBoxDB.TabIndex = 36;
            this.pictureBoxDB.TabStop = false;
            // 
            // pictureBoxDynamics
            // 
            this.pictureBoxDynamics.Image = global::OQSDrug.Properties.Resources.Hourglass;
            this.pictureBoxDynamics.Location = new System.Drawing.Point(294, 96);
            this.pictureBoxDynamics.Name = "pictureBoxDynamics";
            this.pictureBoxDynamics.Size = new System.Drawing.Size(22, 22);
            this.pictureBoxDynamics.TabIndex = 37;
            this.pictureBoxDynamics.TabStop = false;
            // 
            // pictureBoxOQSFolder
            // 
            this.pictureBoxOQSFolder.Image = global::OQSDrug.Properties.Resources.Hourglass;
            this.pictureBoxOQSFolder.Location = new System.Drawing.Point(294, 125);
            this.pictureBoxOQSFolder.Name = "pictureBoxOQSFolder";
            this.pictureBoxOQSFolder.Size = new System.Drawing.Size(22, 22);
            this.pictureBoxOQSFolder.TabIndex = 38;
            this.pictureBoxOQSFolder.TabStop = false;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.buttonPG);
            this.groupBox2.Controls.Add(this.pictureBoxOQSFolder);
            this.groupBox2.Controls.Add(this.buttonYZ);
            this.groupBox2.Controls.Add(this.pictureBoxDynamics);
            this.groupBox2.Controls.Add(this.buttonSR);
            this.groupBox2.Controls.Add(this.pictureBoxDB);
            this.groupBox2.Controls.Add(this.buttonYZXML);
            this.groupBox2.Controls.Add(this.buttonOQSFolder);
            this.groupBox2.Controls.Add(this.buttonKS);
            this.groupBox2.Controls.Add(this.buttonDynamics);
            this.groupBox2.Controls.Add(this.buttonKSXML);
            this.groupBox2.Controls.Add(this.buttonMDB);
            this.groupBox2.Location = new System.Drawing.Point(13, 81);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(362, 153);
            this.groupBox2.TabIndex = 39;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Status";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.ClientSize = new System.Drawing.Size(973, 717);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.checkBoxAutoStart);
            this.Controls.Add(this.buttonReload);
            this.Controls.Add(this.listViewLog);
            this.Controls.Add(this.StartStop);
            this.Controls.Add(this.toolStripVersion);
            this.Controls.Add(this.dataGridView1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.Text = "OQSDrugメイン";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.toolStripVersion.ResumeLayout(false);
            this.toolStripVersion.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxDB)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxDynamics)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxOQSFolder)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.DataGridView dataGridView1;
        private ClickThroughToolStrip toolStripVersion;
        private System.Windows.Forms.ToolStripButton toolStripButtonSettings;
        private System.Windows.Forms.CheckBox StartStop;
        private System.Windows.Forms.ListView listViewLog;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripComboBox toolStripComboBoxDBProviders;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Button buttonYZ;
        private System.Windows.Forms.Button buttonSR;
        private System.Windows.Forms.Button buttonYZXML;
        private System.Windows.Forms.Button buttonKS;
        private System.Windows.Forms.Button buttonKSXML;
        private System.Windows.Forms.CheckBox checkBoxAutoview;
        private System.Windows.Forms.ToolStripButton toolStripButtonViewer;
        private System.Windows.Forms.ToolStripButton toolStripButtonExit;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton toolStripButtonVersion;
        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.ToolStripButton toolStripButtonToTaskTray;
        private System.Windows.Forms.Button buttonReload;
        private System.Windows.Forms.CheckBox checkBoxAutoStart;
        private System.Windows.Forms.ToolStripButton toolStripButtonTKK;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox checkBoxAutoTKK;
        private System.Windows.Forms.ToolStripButton toolStripButtonDebug;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparatorDebug1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparatorDebug2;
        private System.Windows.Forms.ToolStripTextBox toolStripTextBoxDebug;
        private System.Windows.Forms.ToolStripButton toolStripButtonSinryo;
        private System.Windows.Forms.CheckBox checkBoxAutoSR;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripTextBox toolStripTextBoxPtIDmain;
        private System.Windows.Forms.ToolStripButton toolStripButtonLog;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ToolStripComboBox toolStripComboBoxConnectionMode;
        private System.Windows.Forms.Button buttonMDB;
        private System.Windows.Forms.Button buttonPG;
        private System.Windows.Forms.Button buttonDynamics;
        private System.Windows.Forms.Button buttonOQSFolder;
        private System.Windows.Forms.PictureBox pictureBoxDB;
        private System.Windows.Forms.PictureBox pictureBoxDynamics;
        private System.Windows.Forms.PictureBox pictureBoxOQSFolder;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.ToolStripButton toolStripButtonPMDA;
    }
}

