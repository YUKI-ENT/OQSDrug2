namespace OQSDrug
{
    partial class Form2
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form2));
            this.textBoxDatadyna = new System.Windows.Forms.TextBox();
            this.textBoxOQSFolder = new System.Windows.Forms.TextBox();
            this.checkBoxKeepXml = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.comboBoxTimerSecond = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.textBoxMCode = new System.Windows.Forms.TextBox();
            this.buttonDatadyna = new System.Windows.Forms.Button();
            this.buttonOQSFolder = new System.Windows.Forms.Button();
            this.buttonSaveExit = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonOQSDrugData = new System.Windows.Forms.Button();
            this.labelMDB1 = new System.Windows.Forms.Label();
            this.textBoxOQSDrugData = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.textBoxTemprs = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.checkBoxTopmost = new System.Windows.Forms.CheckBox();
            this.buttonViewerPositionReset = new System.Windows.Forms.Button();
            this.buttonImport = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.checkBoxMinimumStart = new System.Windows.Forms.CheckBox();
            this.toolTipSetting = new System.Windows.Forms.ToolTip(this.components);
            this.checkBoxAutoStart = new System.Windows.Forms.CheckBox();
            this.checkBoxRSBreloadXml = new System.Windows.Forms.CheckBox();
            this.textBoxRSBxmlURL = new System.Windows.Forms.TextBox();
            this.labelPG1 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.comboBoxLLMModels = new System.Windows.Forms.ComboBox();
            this.buttonGetModels = new System.Windows.Forms.Button();
            this.textBoxLLMtimeout = new System.Windows.Forms.TextBox();
            this.labelLLMtimeout = new System.Windows.Forms.Label();
            this.labelLLMmodel = new System.Windows.Forms.Label();
            this.labelLLMport = new System.Windows.Forms.Label();
            this.textBoxLLMport = new System.Windows.Forms.TextBox();
            this.labelLLMserver = new System.Windows.Forms.Label();
            this.textBoxLLMserver = new System.Windows.Forms.TextBox();
            this.checkBoxAI = new System.Windows.Forms.CheckBox();
            this.buttonPGupload = new System.Windows.Forms.Button();
            this.radioButtonPG = new System.Windows.Forms.RadioButton();
            this.textBoxPGpass = new System.Windows.Forms.TextBox();
            this.labelPG4 = new System.Windows.Forms.Label();
            this.radioButtonMDB = new System.Windows.Forms.RadioButton();
            this.labelPG3 = new System.Windows.Forms.Label();
            this.textBoxPGuser = new System.Windows.Forms.TextBox();
            this.labelPG2 = new System.Windows.Forms.Label();
            this.textBoxPGport = new System.Windows.Forms.TextBox();
            this.textBoxPGaddress = new System.Windows.Forms.TextBox();
            this.comboBoxRSBID = new System.Windows.Forms.ComboBox();
            this.label17 = new System.Windows.Forms.Label();
            this.label18 = new System.Windows.Forms.Label();
            this.comboBoxViewSpan = new System.Windows.Forms.ComboBox();
            this.label19 = new System.Windows.Forms.Label();
            this.checkBoxOmitMyOrg = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // textBoxDatadyna
            // 
            this.textBoxDatadyna.Location = new System.Drawing.Point(180, 189);
            this.textBoxDatadyna.Name = "textBoxDatadyna";
            this.textBoxDatadyna.Size = new System.Drawing.Size(365, 19);
            this.textBoxDatadyna.TabIndex = 2;
            this.toolTipSetting.SetToolTip(this.textBoxDatadyna, resources.GetString("textBoxDatadyna.ToolTip"));
            // 
            // textBoxOQSFolder
            // 
            this.textBoxOQSFolder.Location = new System.Drawing.Point(180, 262);
            this.textBoxOQSFolder.Name = "textBoxOQSFolder";
            this.textBoxOQSFolder.Size = new System.Drawing.Size(365, 19);
            this.textBoxOQSFolder.TabIndex = 4;
            this.toolTipSetting.SetToolTip(this.textBoxOQSFolder, "オン資PCのreq/res/face等のフォルダが有る場所を指定します");
            // 
            // checkBoxKeepXml
            // 
            this.checkBoxKeepXml.AutoSize = true;
            this.checkBoxKeepXml.Location = new System.Drawing.Point(38, 366);
            this.checkBoxKeepXml.Name = "checkBoxKeepXml";
            this.checkBoxKeepXml.Size = new System.Drawing.Size(104, 16);
            this.checkBoxKeepXml.TabIndex = 29;
            this.checkBoxKeepXml.Text = "xmlを削除しない";
            this.toolTipSetting.SetToolTip(this.checkBoxKeepXml, "ON: RESフォルダにxml薬歴を残します。\r\nRSBaseでxml薬歴や健診歴を取得するときはこれをONにしてください。\r\nresフォルダに残ったファイルは適" +
        "宜手動で削除してください。");
            this.checkBoxKeepXml.UseVisualStyleBackColor = true;
            this.checkBoxKeepXml.CheckedChanged += new System.EventHandler(this.checkBoxKeepXml_CheckedChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(27, 192);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(103, 12);
            this.label1.TabIndex = 5;
            this.label1.Text = "②ダイナミクスの場所";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(27, 265);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(109, 12);
            this.label2.TabIndex = 6;
            this.label2.Text = "③OQSフォルダの場所";
            this.toolTipSetting.SetToolTip(this.label2, "オン資PCのreq/res/face等のフォルダが有る場所を指定します");
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(27, 313);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(97, 12);
            this.label4.TabIndex = 8;
            this.label4.Text = "⑤タイマー間隔(秒)";
            // 
            // comboBoxTimerSecond
            // 
            this.comboBoxTimerSecond.FormattingEnabled = true;
            this.comboBoxTimerSecond.Location = new System.Drawing.Point(180, 310);
            this.comboBoxTimerSecond.Name = "comboBoxTimerSecond";
            this.comboBoxTimerSecond.Size = new System.Drawing.Size(53, 20);
            this.comboBoxTimerSecond.TabIndex = 14;
            this.toolTipSetting.SetToolTip(this.comboBoxTimerSecond, "情報閲覧を行う間隔を秒数で指定します。");
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(27, 290);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(92, 12);
            this.label5.TabIndex = 10;
            this.label5.Text = "④医療機関コード";
            // 
            // textBoxMCode
            // 
            this.textBoxMCode.Location = new System.Drawing.Point(180, 287);
            this.textBoxMCode.Name = "textBoxMCode";
            this.textBoxMCode.Size = new System.Drawing.Size(100, 19);
            this.textBoxMCode.TabIndex = 12;
            this.toolTipSetting.SetToolTip(this.textBoxMCode, "10桁のものです");
            // 
            // buttonDatadyna
            // 
            this.buttonDatadyna.Location = new System.Drawing.Point(551, 189);
            this.buttonDatadyna.Name = "buttonDatadyna";
            this.buttonDatadyna.Size = new System.Drawing.Size(21, 19);
            this.buttonDatadyna.TabIndex = 3;
            this.buttonDatadyna.Text = "...";
            this.buttonDatadyna.UseVisualStyleBackColor = true;
            this.buttonDatadyna.Click += new System.EventHandler(this.buttonDatadyna_Click);
            // 
            // buttonOQSFolder
            // 
            this.buttonOQSFolder.Location = new System.Drawing.Point(551, 262);
            this.buttonOQSFolder.Name = "buttonOQSFolder";
            this.buttonOQSFolder.Size = new System.Drawing.Size(21, 19);
            this.buttonOQSFolder.TabIndex = 5;
            this.buttonOQSFolder.Text = "...";
            this.buttonOQSFolder.UseVisualStyleBackColor = true;
            this.buttonOQSFolder.Click += new System.EventHandler(this.buttonOQSFolder_Click);
            // 
            // buttonSaveExit
            // 
            this.buttonSaveExit.Location = new System.Drawing.Point(488, 567);
            this.buttonSaveExit.Name = "buttonSaveExit";
            this.buttonSaveExit.Size = new System.Drawing.Size(93, 23);
            this.buttonSaveExit.TabIndex = 50;
            this.buttonSaveExit.Text = "保存して閉じる";
            this.buttonSaveExit.UseVisualStyleBackColor = true;
            this.buttonSaveExit.Click += new System.EventHandler(this.buttonSaveExit_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Location = new System.Drawing.Point(407, 567);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 44;
            this.buttonCancel.Text = "キャンセル";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // buttonOQSDrugData
            // 
            this.buttonOQSDrugData.Location = new System.Drawing.Point(528, 17);
            this.buttonOQSDrugData.Name = "buttonOQSDrugData";
            this.buttonOQSDrugData.Size = new System.Drawing.Size(21, 19);
            this.buttonOQSDrugData.TabIndex = 1;
            this.buttonOQSDrugData.Text = "...";
            this.buttonOQSDrugData.UseVisualStyleBackColor = true;
            this.buttonOQSDrugData.Click += new System.EventHandler(this.buttonOQSDrugData_Click);
            // 
            // labelMDB1
            // 
            this.labelMDB1.AutoSize = true;
            this.labelMDB1.Location = new System.Drawing.Point(96, 20);
            this.labelMDB1.Name = "labelMDB1";
            this.labelMDB1.Size = new System.Drawing.Size(135, 12);
            this.labelMDB1.TabIndex = 21;
            this.labelMDB1.Text = "OQSDrug_data.mdbの場所";
            this.toolTipSetting.SetToolTip(this.labelMDB1, "通常はダイナミクスのdatadyna.mdbのあるフォルダに配置・設定してください");
            // 
            // textBoxOQSDrugData
            // 
            this.textBoxOQSDrugData.Location = new System.Drawing.Point(237, 17);
            this.textBoxOQSDrugData.Name = "textBoxOQSDrugData";
            this.textBoxOQSDrugData.Size = new System.Drawing.Size(282, 19);
            this.textBoxOQSDrugData.TabIndex = 0;
            this.toolTipSetting.SetToolTip(this.textBoxOQSDrugData, "通常はダイナミクスのdatadyna.mdbのあるフォルダに配置・設定してください");
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(12, 9);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(55, 12);
            this.label13.TabIndex = 51;
            this.label13.Text = "メイン設定";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(17, 452);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(64, 12);
            this.label15.TabIndex = 53;
            this.label15.Text = "Viewer設定";
            // 
            // textBoxTemprs
            // 
            this.textBoxTemprs.Location = new System.Drawing.Point(407, 467);
            this.textBoxTemprs.Name = "textBoxTemprs";
            this.textBoxTemprs.Size = new System.Drawing.Size(151, 19);
            this.textBoxTemprs.TabIndex = 54;
            this.textBoxTemprs.Visible = false;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(40, 470);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(148, 12);
            this.label14.TabIndex = 55;
            this.label14.Text = "⑪ ダイナ/RSBase 連携方式";
            this.toolTipSetting.SetToolTip(this.label14, "RSBaseのID連携を設定すると\r\nカルテ遷移に連動して自動で薬歴ビュワーを起動します");
            // 
            // checkBoxTopmost
            // 
            this.checkBoxTopmost.AutoSize = true;
            this.checkBoxTopmost.Location = new System.Drawing.Point(41, 492);
            this.checkBoxTopmost.Name = "checkBoxTopmost";
            this.checkBoxTopmost.Size = new System.Drawing.Size(163, 16);
            this.checkBoxTopmost.TabIndex = 56;
            this.checkBoxTopmost.Text = "⑫ 薬歴・健診を最前面表示";
            this.checkBoxTopmost.UseVisualStyleBackColor = true;
            // 
            // buttonViewerPositionReset
            // 
            this.buttonViewerPositionReset.Location = new System.Drawing.Point(210, 488);
            this.buttonViewerPositionReset.Name = "buttonViewerPositionReset";
            this.buttonViewerPositionReset.Size = new System.Drawing.Size(218, 23);
            this.buttonViewerPositionReset.TabIndex = 57;
            this.buttonViewerPositionReset.Text = "薬歴・健診Windowの位置サイズをリセット";
            this.buttonViewerPositionReset.UseVisualStyleBackColor = true;
            this.buttonViewerPositionReset.Click += new System.EventHandler(this.buttonViewerPositionReset_Click);
            // 
            // buttonImport
            // 
            this.buttonImport.Location = new System.Drawing.Point(245, 567);
            this.buttonImport.Name = "buttonImport";
            this.buttonImport.Size = new System.Drawing.Size(75, 23);
            this.buttonImport.TabIndex = 58;
            this.buttonImport.Text = "インポート";
            this.buttonImport.UseVisualStyleBackColor = true;
            this.buttonImport.Click += new System.EventHandler(this.buttonImport_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(326, 567);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 59;
            this.button2.Text = "エクスポート";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.buttonExport_Click);
            // 
            // checkBoxMinimumStart
            // 
            this.checkBoxMinimumStart.AutoSize = true;
            this.checkBoxMinimumStart.Location = new System.Drawing.Point(31, 397);
            this.checkBoxMinimumStart.Name = "checkBoxMinimumStart";
            this.checkBoxMinimumStart.Size = new System.Drawing.Size(108, 16);
            this.checkBoxMinimumStart.TabIndex = 60;
            this.checkBoxMinimumStart.Text = "⑦最小化して開く";
            this.toolTipSetting.SetToolTip(this.checkBoxMinimumStart, "タスクトレイに最小化して起動します");
            this.checkBoxMinimumStart.UseVisualStyleBackColor = true;
            // 
            // checkBoxAutoStart
            // 
            this.checkBoxAutoStart.AutoSize = true;
            this.checkBoxAutoStart.Location = new System.Drawing.Point(242, 312);
            this.checkBoxAutoStart.Name = "checkBoxAutoStart";
            this.checkBoxAutoStart.Size = new System.Drawing.Size(102, 16);
            this.checkBoxAutoStart.TabIndex = 70;
            this.checkBoxAutoStart.Text = "自動開始/停止";
            this.toolTipSetting.SetToolTip(this.checkBoxAutoStart, "起動時に設定値がすべてOKなら、自動で取込動作を開始します");
            this.checkBoxAutoStart.UseVisualStyleBackColor = true;
            // 
            // checkBoxRSBreloadXml
            // 
            this.checkBoxRSBreloadXml.AutoSize = true;
            this.checkBoxRSBreloadXml.Enabled = false;
            this.checkBoxRSBreloadXml.Location = new System.Drawing.Point(148, 366);
            this.checkBoxRSBreloadXml.Name = "checkBoxRSBreloadXml";
            this.checkBoxRSBreloadXml.Size = new System.Drawing.Size(164, 16);
            this.checkBoxRSBreloadXml.TabIndex = 74;
            this.checkBoxRSBreloadXml.Text = "RSBaseでxml reload   URL:";
            this.toolTipSetting.SetToolTip(this.checkBoxRSBreloadXml, "xml薬歴や健診歴を取得後、RSBaseのxmlreloadを実行します。\r\nこのPCにRSBaseがインストールされている必要があります。");
            this.checkBoxRSBreloadXml.UseVisualStyleBackColor = true;
            // 
            // textBoxRSBxmlURL
            // 
            this.textBoxRSBxmlURL.Enabled = false;
            this.textBoxRSBxmlURL.Location = new System.Drawing.Point(310, 364);
            this.textBoxRSBxmlURL.Name = "textBoxRSBxmlURL";
            this.textBoxRSBxmlURL.Size = new System.Drawing.Size(235, 19);
            this.textBoxRSBxmlURL.TabIndex = 75;
            this.toolTipSetting.SetToolTip(this.textBoxRSBxmlURL, "リロードするURLを指定します");
            // 
            // labelPG1
            // 
            this.labelPG1.AutoSize = true;
            this.labelPG1.Location = new System.Drawing.Point(150, 45);
            this.labelPG1.Name = "labelPG1";
            this.labelPG1.Size = new System.Drawing.Size(81, 12);
            this.labelPG1.TabIndex = 77;
            this.labelPG1.Text = "サーバーアドレス";
            this.toolTipSetting.SetToolTip(this.labelPG1, "通常はダイナミクスのdatadyna.mdbのあるフォルダに配置・設定してください");
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.comboBoxLLMModels);
            this.groupBox1.Controls.Add(this.buttonGetModels);
            this.groupBox1.Controls.Add(this.textBoxLLMtimeout);
            this.groupBox1.Controls.Add(this.labelLLMtimeout);
            this.groupBox1.Controls.Add(this.labelLLMmodel);
            this.groupBox1.Controls.Add(this.labelLLMport);
            this.groupBox1.Controls.Add(this.textBoxLLMport);
            this.groupBox1.Controls.Add(this.labelLLMserver);
            this.groupBox1.Controls.Add(this.textBoxLLMserver);
            this.groupBox1.Controls.Add(this.checkBoxAI);
            this.groupBox1.Controls.Add(this.buttonPGupload);
            this.groupBox1.Controls.Add(this.radioButtonPG);
            this.groupBox1.Controls.Add(this.textBoxPGpass);
            this.groupBox1.Controls.Add(this.labelPG4);
            this.groupBox1.Controls.Add(this.radioButtonMDB);
            this.groupBox1.Controls.Add(this.labelPG3);
            this.groupBox1.Controls.Add(this.labelMDB1);
            this.groupBox1.Controls.Add(this.textBoxOQSDrugData);
            this.groupBox1.Controls.Add(this.textBoxPGuser);
            this.groupBox1.Controls.Add(this.buttonOQSDrugData);
            this.groupBox1.Controls.Add(this.labelPG2);
            this.groupBox1.Controls.Add(this.textBoxPGport);
            this.groupBox1.Controls.Add(this.textBoxPGaddress);
            this.groupBox1.Controls.Add(this.labelPG1);
            this.groupBox1.Location = new System.Drawing.Point(29, 25);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(555, 158);
            this.groupBox1.TabIndex = 85;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "① データベース形式";
            this.toolTipSetting.SetToolTip(this.groupBox1, "OQSDrugのバックエンドデータベースを設定します");
            // 
            // comboBoxLLMModels
            // 
            this.comboBoxLLMModels.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxLLMModels.FormattingEnabled = true;
            this.comboBoxLLMModels.Location = new System.Drawing.Point(237, 122);
            this.comboBoxLLMModels.Name = "comboBoxLLMModels";
            this.comboBoxLLMModels.Size = new System.Drawing.Size(182, 20);
            this.comboBoxLLMModels.TabIndex = 95;
            this.toolTipSetting.SetToolTip(this.comboBoxLLMModels, "LLMアドレスとポートを設定し、リスト取得を押すと\r\n一覧から選択できるようになります");
            this.comboBoxLLMModels.SelectionChangeCommitted += new System.EventHandler(this.comboBoxLLMModels_SelectionChangeCommitted);
            // 
            // buttonGetModels
            // 
            this.buttonGetModels.Location = new System.Drawing.Point(189, 120);
            this.buttonGetModels.Name = "buttonGetModels";
            this.buttonGetModels.Size = new System.Drawing.Size(41, 23);
            this.buttonGetModels.TabIndex = 94;
            this.buttonGetModels.Text = "取得";
            this.buttonGetModels.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
            this.buttonGetModels.UseVisualStyleBackColor = true;
            this.buttonGetModels.Click += new System.EventHandler(this.buttonGetModels_Click);
            // 
            // textBoxLLMtimeout
            // 
            this.textBoxLLMtimeout.Location = new System.Drawing.Point(505, 122);
            this.textBoxLLMtimeout.Name = "textBoxLLMtimeout";
            this.textBoxLLMtimeout.Size = new System.Drawing.Size(38, 19);
            this.textBoxLLMtimeout.TabIndex = 93;
            // 
            // labelLLMtimeout
            // 
            this.labelLLMtimeout.AutoSize = true;
            this.labelLLMtimeout.Location = new System.Drawing.Point(425, 125);
            this.labelLLMtimeout.Name = "labelLLMtimeout";
            this.labelLLMtimeout.Size = new System.Drawing.Size(78, 12);
            this.labelLLMtimeout.TabIndex = 92;
            this.labelLLMtimeout.Text = "タイムアウト(秒)";
            // 
            // labelLLMmodel
            // 
            this.labelLLMmodel.AutoSize = true;
            this.labelLLMmodel.Location = new System.Drawing.Point(149, 125);
            this.labelLLMmodel.Name = "labelLLMmodel";
            this.labelLLMmodel.Size = new System.Drawing.Size(34, 12);
            this.labelLLMmodel.TabIndex = 91;
            this.labelLLMmodel.Text = "モデル";
            // 
            // labelLLMport
            // 
            this.labelLLMport.AutoSize = true;
            this.labelLLMport.Location = new System.Drawing.Point(454, 100);
            this.labelLLMport.Name = "labelLLMport";
            this.labelLLMport.Size = new System.Drawing.Size(26, 12);
            this.labelLLMport.TabIndex = 88;
            this.labelLLMport.Text = "Port";
            // 
            // textBoxLLMport
            // 
            this.textBoxLLMport.Location = new System.Drawing.Point(486, 97);
            this.textBoxLLMport.Name = "textBoxLLMport";
            this.textBoxLLMport.Size = new System.Drawing.Size(63, 19);
            this.textBoxLLMport.TabIndex = 89;
            // 
            // labelLLMserver
            // 
            this.labelLLMserver.AutoSize = true;
            this.labelLLMserver.Location = new System.Drawing.Point(150, 100);
            this.labelLLMserver.Name = "labelLLMserver";
            this.labelLLMserver.Size = new System.Drawing.Size(62, 12);
            this.labelLLMserver.TabIndex = 87;
            this.labelLLMserver.Text = "LLMアドレス";
            this.toolTipSetting.SetToolTip(this.labelLLMserver, "通常はダイナミクスのdatadyna.mdbのあるフォルダに配置・設定してください");
            // 
            // textBoxLLMserver
            // 
            this.textBoxLLMserver.Location = new System.Drawing.Point(237, 97);
            this.textBoxLLMserver.Name = "textBoxLLMserver";
            this.textBoxLLMserver.Size = new System.Drawing.Size(211, 19);
            this.textBoxLLMserver.TabIndex = 86;
            // 
            // checkBoxAI
            // 
            this.checkBoxAI.AutoSize = true;
            this.checkBoxAI.Location = new System.Drawing.Point(65, 99);
            this.checkBoxAI.Name = "checkBoxAI";
            this.checkBoxAI.Size = new System.Drawing.Size(83, 16);
            this.checkBoxAI.TabIndex = 85;
            this.checkBoxAI.Text = "AI自動検索";
            this.checkBoxAI.UseVisualStyleBackColor = true;
            this.checkBoxAI.CheckedChanged += new System.EventHandler(this.checkBoxAI_CheckedChanged);
            // 
            // buttonPGupload
            // 
            this.buttonPGupload.Image = global::OQSDrug.Properties.Resources.pg24;
            this.buttonPGupload.Location = new System.Drawing.Point(32, 58);
            this.buttonPGupload.Name = "buttonPGupload";
            this.buttonPGupload.Size = new System.Drawing.Size(77, 36);
            this.buttonPGupload.TabIndex = 84;
            this.buttonPGupload.Text = "設定";
            this.buttonPGupload.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.buttonPGupload.UseVisualStyleBackColor = true;
            this.buttonPGupload.Click += new System.EventHandler(this.buttonPGupload_Click);
            // 
            // radioButtonPG
            // 
            this.radioButtonPG.AutoSize = true;
            this.radioButtonPG.Location = new System.Drawing.Point(13, 43);
            this.radioButtonPG.Name = "radioButtonPG";
            this.radioButtonPG.Size = new System.Drawing.Size(83, 16);
            this.radioButtonPG.TabIndex = 1;
            this.radioButtonPG.TabStop = true;
            this.radioButtonPG.Text = "PostgreSQL";
            this.radioButtonPG.UseVisualStyleBackColor = true;
            this.radioButtonPG.CheckedChanged += new System.EventHandler(this.radioButtonPG_CheckedChanged);
            // 
            // textBoxPGpass
            // 
            this.textBoxPGpass.Location = new System.Drawing.Point(449, 65);
            this.textBoxPGpass.Name = "textBoxPGpass";
            this.textBoxPGpass.PasswordChar = '*';
            this.textBoxPGpass.Size = new System.Drawing.Size(100, 19);
            this.textBoxPGpass.TabIndex = 81;
            // 
            // labelPG4
            // 
            this.labelPG4.AutoSize = true;
            this.labelPG4.Location = new System.Drawing.Point(391, 68);
            this.labelPG4.Name = "labelPG4";
            this.labelPG4.Size = new System.Drawing.Size(52, 12);
            this.labelPG4.TabIndex = 83;
            this.labelPG4.Text = "パスワード";
            // 
            // radioButtonMDB
            // 
            this.radioButtonMDB.AutoSize = true;
            this.radioButtonMDB.Location = new System.Drawing.Point(13, 18);
            this.radioButtonMDB.Name = "radioButtonMDB";
            this.radioButtonMDB.Size = new System.Drawing.Size(48, 16);
            this.radioButtonMDB.TabIndex = 0;
            this.radioButtonMDB.TabStop = true;
            this.radioButtonMDB.Text = "MDB";
            this.radioButtonMDB.UseVisualStyleBackColor = true;
            this.radioButtonMDB.CheckedChanged += new System.EventHandler(this.radioButtonMDB_CheckedChanged);
            // 
            // labelPG3
            // 
            this.labelPG3.AutoSize = true;
            this.labelPG3.Location = new System.Drawing.Point(174, 68);
            this.labelPG3.Name = "labelPG3";
            this.labelPG3.Size = new System.Drawing.Size(57, 12);
            this.labelPG3.TabIndex = 82;
            this.labelPG3.Text = "ユーザー名";
            // 
            // textBoxPGuser
            // 
            this.textBoxPGuser.Location = new System.Drawing.Point(237, 65);
            this.textBoxPGuser.Name = "textBoxPGuser";
            this.textBoxPGuser.Size = new System.Drawing.Size(100, 19);
            this.textBoxPGuser.TabIndex = 80;
            // 
            // labelPG2
            // 
            this.labelPG2.AutoSize = true;
            this.labelPG2.Location = new System.Drawing.Point(454, 45);
            this.labelPG2.Name = "labelPG2";
            this.labelPG2.Size = new System.Drawing.Size(26, 12);
            this.labelPG2.TabIndex = 78;
            this.labelPG2.Text = "Port";
            // 
            // textBoxPGport
            // 
            this.textBoxPGport.Location = new System.Drawing.Point(486, 42);
            this.textBoxPGport.Name = "textBoxPGport";
            this.textBoxPGport.Size = new System.Drawing.Size(63, 19);
            this.textBoxPGport.TabIndex = 79;
            // 
            // textBoxPGaddress
            // 
            this.textBoxPGaddress.Location = new System.Drawing.Point(237, 42);
            this.textBoxPGaddress.Name = "textBoxPGaddress";
            this.textBoxPGaddress.Size = new System.Drawing.Size(211, 19);
            this.textBoxPGaddress.TabIndex = 76;
            // 
            // comboBoxRSBID
            // 
            this.comboBoxRSBID.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxRSBID.FormattingEnabled = true;
            this.comboBoxRSBID.Items.AddRange(new object[] {
            "ID.dat",
            "temp_rs.txt",
            "thept.txt",
            "ダイナC:\\DynaID",
            "ダイナD:\\DynaID"});
            this.comboBoxRSBID.Location = new System.Drawing.Point(210, 467);
            this.comboBoxRSBID.Name = "comboBoxRSBID";
            this.comboBoxRSBID.Size = new System.Drawing.Size(124, 20);
            this.comboBoxRSBID.TabIndex = 61;
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Font = new System.Drawing.Font("MS UI Gothic", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.label17.Location = new System.Drawing.Point(168, 211);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(364, 33);
            this.label17.TabIndex = 64;
            this.label17.Text = "本アプリを動かすPCでダイナミクスクライアントが稼働中の場合はクライアントダイナを、\r\nクライアントが稼働してない場合は、サーバーのdatadyna.mdbを指定" +
    "してください。\r\nクライアントを指定した場合は、クライアントアップデート後再指定が必要です";
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(41, 518);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(109, 12);
            this.label18.TabIndex = 65;
            this.label18.Text = "⑬初期表示期間(月)";
            // 
            // comboBoxViewSpan
            // 
            this.comboBoxViewSpan.FormattingEnabled = true;
            this.comboBoxViewSpan.Location = new System.Drawing.Point(210, 515);
            this.comboBoxViewSpan.Name = "comboBoxViewSpan";
            this.comboBoxViewSpan.Size = new System.Drawing.Size(65, 20);
            this.comboBoxViewSpan.TabIndex = 66;
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Location = new System.Drawing.Point(281, 518);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(65, 12);
            this.label19.TabIndex = 67;
            this.label19.Text = "(全期間は0)";
            // 
            // checkBoxOmitMyOrg
            // 
            this.checkBoxOmitMyOrg.AutoSize = true;
            this.checkBoxOmitMyOrg.Location = new System.Drawing.Point(41, 542);
            this.checkBoxOmitMyOrg.Name = "checkBoxOmitMyOrg";
            this.checkBoxOmitMyOrg.Size = new System.Drawing.Size(100, 16);
            this.checkBoxOmitMyOrg.TabIndex = 69;
            this.checkBoxOmitMyOrg.Text = "⑭ 自施設除外";
            this.checkBoxOmitMyOrg.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(29, 348);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(127, 12);
            this.label3.TabIndex = 86;
            this.label3.Text = "⑥ RSBase xml取り込み";
            // 
            // Form2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(606, 604);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textBoxRSBxmlURL);
            this.Controls.Add(this.checkBoxRSBreloadXml);
            this.Controls.Add(this.checkBoxKeepXml);
            this.Controls.Add(this.checkBoxAutoStart);
            this.Controls.Add(this.checkBoxOmitMyOrg);
            this.Controls.Add(this.label19);
            this.Controls.Add(this.comboBoxViewSpan);
            this.Controls.Add(this.label18);
            this.Controls.Add(this.label17);
            this.Controls.Add(this.comboBoxRSBID);
            this.Controls.Add(this.checkBoxMinimumStart);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.buttonImport);
            this.Controls.Add(this.buttonViewerPositionReset);
            this.Controls.Add(this.checkBoxTopmost);
            this.Controls.Add(this.label14);
            this.Controls.Add(this.textBoxTemprs);
            this.Controls.Add(this.label15);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonSaveExit);
            this.Controls.Add(this.buttonOQSFolder);
            this.Controls.Add(this.buttonDatadyna);
            this.Controls.Add(this.textBoxMCode);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.comboBoxTimerSecond);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBoxOQSFolder);
            this.Controls.Add(this.textBoxDatadyna);
            this.Controls.Add(this.groupBox1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form2";
            this.Text = "設定";
            this.Load += new System.EventHandler(this.Form2_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxDatadyna;
        private System.Windows.Forms.TextBox textBoxOQSFolder;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox comboBoxTimerSecond;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBoxMCode;
        private System.Windows.Forms.Button buttonDatadyna;
        private System.Windows.Forms.Button buttonOQSFolder;
        private System.Windows.Forms.Button buttonSaveExit;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Button buttonOQSDrugData;
        private System.Windows.Forms.Label labelMDB1;
        private System.Windows.Forms.TextBox textBoxOQSDrugData;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.TextBox textBoxTemprs;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.CheckBox checkBoxTopmost;
        private System.Windows.Forms.Button buttonViewerPositionReset;
        private System.Windows.Forms.Button buttonImport;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.CheckBox checkBoxMinimumStart;
        private System.Windows.Forms.ToolTip toolTipSetting;
        private System.Windows.Forms.ComboBox comboBoxRSBID;
        private System.Windows.Forms.CheckBox checkBoxKeepXml;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.ComboBox comboBoxViewSpan;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.CheckBox checkBoxOmitMyOrg;
        private System.Windows.Forms.CheckBox checkBoxAutoStart;
        private System.Windows.Forms.CheckBox checkBoxRSBreloadXml;
        private System.Windows.Forms.TextBox textBoxRSBxmlURL;
        private System.Windows.Forms.TextBox textBoxPGaddress;
        private System.Windows.Forms.Label labelPG1;
        private System.Windows.Forms.Label labelPG2;
        private System.Windows.Forms.TextBox textBoxPGport;
        private System.Windows.Forms.TextBox textBoxPGuser;
        private System.Windows.Forms.TextBox textBoxPGpass;
        private System.Windows.Forms.Label labelPG3;
        private System.Windows.Forms.Label labelPG4;
        private System.Windows.Forms.Button buttonPGupload;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton radioButtonPG;
        private System.Windows.Forms.RadioButton radioButtonMDB;
        private System.Windows.Forms.CheckBox checkBoxAI;
        private System.Windows.Forms.Label labelLLMport;
        private System.Windows.Forms.TextBox textBoxLLMport;
        private System.Windows.Forms.Label labelLLMserver;
        private System.Windows.Forms.TextBox textBoxLLMserver;
        private System.Windows.Forms.Label labelLLMtimeout;
        private System.Windows.Forms.Label labelLLMmodel;
        private System.Windows.Forms.TextBox textBoxLLMtimeout;
        private System.Windows.Forms.ComboBox comboBoxLLMModels;
        private System.Windows.Forms.Button buttonGetModels;
        private System.Windows.Forms.Label label3;
    }
}