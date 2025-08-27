namespace OQSDrug
{
    partial class FormLLMsetteing
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormLLMsetteing));
            this.comboBoxModels = new System.Windows.Forms.ComboBox();
            this.textBoxPrompt = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxTplTitle = new System.Windows.Forms.TextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label9 = new System.Windows.Forms.Label();
            this.numericUpDownAcuteThresh = new System.Windows.Forms.NumericUpDown();
            this.label8 = new System.Windows.Forms.Label();
            this.numericUpDownChronicThresh = new System.Windows.Forms.NumericUpDown();
            this.label7 = new System.Windows.Forms.Label();
            this.numericUpDownMaxMeds = new System.Windows.Forms.NumericUpDown();
            this.numericUpDownMonth = new System.Windows.Forms.NumericUpDown();
            this.checkBoxDrugC = new System.Windows.Forms.CheckBox();
            this.label6 = new System.Windows.Forms.Label();
            this.numericUpDownMaxIndicationChar = new System.Windows.Forms.NumericUpDown();
            this.numericUpDownMaxIndication = new System.Windows.Forms.NumericUpDown();
            this.checkBoxIndication = new System.Windows.Forms.CheckBox();
            this.checkBoxThera = new System.Windows.Forms.CheckBox();
            this.checkBoxExcludeMyOrg = new System.Windows.Forms.CheckBox();
            this.label4 = new System.Windows.Forms.Label();
            this.checkBoxAutofetch = new System.Windows.Forms.CheckBox();
            this.buttonSave = new System.Windows.Forms.Button();
            this.buttonDelete = new System.Windows.Forms.Button();
            this.listBoxTemplates = new System.Windows.Forms.ListBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.buttonAddnew = new System.Windows.Forms.Button();
            this.buttonClose = new System.Windows.Forms.Button();
            this.buttonSaveAs = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownAcuteThresh)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownChronicThresh)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownMaxMeds)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownMonth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownMaxIndicationChar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownMaxIndication)).BeginInit();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // comboBoxModels
            // 
            this.comboBoxModels.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxModels.FormattingEnabled = true;
            this.comboBoxModels.Location = new System.Drawing.Point(52, 38);
            this.comboBoxModels.Name = "comboBoxModels";
            this.comboBoxModels.Size = new System.Drawing.Size(365, 20);
            this.comboBoxModels.TabIndex = 3;
            // 
            // textBoxPrompt
            // 
            this.textBoxPrompt.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.textBoxPrompt.Location = new System.Drawing.Point(3, 27);
            this.textBoxPrompt.Multiline = true;
            this.textBoxPrompt.Name = "textBoxPrompt";
            this.textBoxPrompt.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxPrompt.Size = new System.Drawing.Size(406, 169);
            this.textBoxPrompt.TabIndex = 7;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(40, 12);
            this.label1.TabIndex = 5;
            this.label1.Text = "タイトル";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 41);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(34, 12);
            this.label3.TabIndex = 7;
            this.label3.Text = "モデル";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 18);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(49, 12);
            this.label5.TabIndex = 10;
            this.label5.Text = "期間(月)";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.textBoxPrompt);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Location = new System.Drawing.Point(8, 86);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(412, 199);
            this.groupBox1.TabIndex = 12;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "固定部分";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(362, 12);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(29, 12);
            this.label2.TabIndex = 6;
            this.label2.Text = "文字";
            // 
            // textBoxTplTitle
            // 
            this.textBoxTplTitle.Location = new System.Drawing.Point(52, 12);
            this.textBoxTplTitle.Name = "textBoxTplTitle";
            this.textBoxTplTitle.Size = new System.Drawing.Size(368, 19);
            this.textBoxTplTitle.TabIndex = 1;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label9);
            this.groupBox2.Controls.Add(this.numericUpDownAcuteThresh);
            this.groupBox2.Controls.Add(this.label8);
            this.groupBox2.Controls.Add(this.numericUpDownChronicThresh);
            this.groupBox2.Controls.Add(this.label7);
            this.groupBox2.Controls.Add(this.numericUpDownMaxMeds);
            this.groupBox2.Controls.Add(this.numericUpDownMonth);
            this.groupBox2.Controls.Add(this.checkBoxDrugC);
            this.groupBox2.Controls.Add(this.label6);
            this.groupBox2.Controls.Add(this.numericUpDownMaxIndicationChar);
            this.groupBox2.Controls.Add(this.numericUpDownMaxIndication);
            this.groupBox2.Controls.Add(this.checkBoxIndication);
            this.groupBox2.Controls.Add(this.checkBoxThera);
            this.groupBox2.Controls.Add(this.checkBoxExcludeMyOrg);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Location = new System.Drawing.Point(11, 288);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(409, 180);
            this.groupBox2.TabIndex = 14;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "ペイロード";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(189, 72);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(77, 12);
            this.label9.TabIndex = 27;
            this.label9.Text = "急性判定閾値";
            // 
            // numericUpDownAcuteThresh
            // 
            this.numericUpDownAcuteThresh.DecimalPlaces = 1;
            this.numericUpDownAcuteThresh.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numericUpDownAcuteThresh.Location = new System.Drawing.Point(282, 70);
            this.numericUpDownAcuteThresh.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            65536});
            this.numericUpDownAcuteThresh.Name = "numericUpDownAcuteThresh";
            this.numericUpDownAcuteThresh.Size = new System.Drawing.Size(39, 19);
            this.numericUpDownAcuteThresh.TabIndex = 26;
            this.numericUpDownAcuteThresh.Value = new decimal(new int[] {
            6,
            0,
            0,
            65536});
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(6, 72);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(77, 12);
            this.label8.TabIndex = 25;
            this.label8.Text = "慢性判定閾値";
            // 
            // numericUpDownChronicThresh
            // 
            this.numericUpDownChronicThresh.DecimalPlaces = 1;
            this.numericUpDownChronicThresh.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numericUpDownChronicThresh.Location = new System.Drawing.Point(99, 70);
            this.numericUpDownChronicThresh.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            65536});
            this.numericUpDownChronicThresh.Name = "numericUpDownChronicThresh";
            this.numericUpDownChronicThresh.Size = new System.Drawing.Size(39, 19);
            this.numericUpDownChronicThresh.TabIndex = 24;
            this.numericUpDownChronicThresh.Value = new decimal(new int[] {
            6,
            0,
            0,
            65536});
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(6, 140);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(87, 12);
            this.label7.TabIndex = 23;
            this.label7.Text = "適応症の最大数";
            // 
            // numericUpDownMaxMeds
            // 
            this.numericUpDownMaxMeds.Location = new System.Drawing.Point(99, 44);
            this.numericUpDownMaxMeds.Maximum = new decimal(new int[] {
            30,
            0,
            0,
            0});
            this.numericUpDownMaxMeds.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numericUpDownMaxMeds.Name = "numericUpDownMaxMeds";
            this.numericUpDownMaxMeds.Size = new System.Drawing.Size(39, 19);
            this.numericUpDownMaxMeds.TabIndex = 22;
            this.numericUpDownMaxMeds.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // numericUpDownMonth
            // 
            this.numericUpDownMonth.Location = new System.Drawing.Point(99, 16);
            this.numericUpDownMonth.Maximum = new decimal(new int[] {
            24,
            0,
            0,
            0});
            this.numericUpDownMonth.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownMonth.Name = "numericUpDownMonth";
            this.numericUpDownMonth.Size = new System.Drawing.Size(39, 19);
            this.numericUpDownMonth.TabIndex = 21;
            this.numericUpDownMonth.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // checkBoxDrugC
            // 
            this.checkBoxDrugC.AutoSize = true;
            this.checkBoxDrugC.Location = new System.Drawing.Point(301, 14);
            this.checkBoxDrugC.Name = "checkBoxDrugC";
            this.checkBoxDrugC.Size = new System.Drawing.Size(87, 16);
            this.checkBoxDrugC.TabIndex = 20;
            this.checkBoxDrugC.Text = "医薬品コード";
            this.checkBoxDrugC.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(187, 140);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(153, 12);
            this.label6.TabIndex = 19;
            this.label6.Text = "適応症1つあたりの最大文字数";
            // 
            // numericUpDownMaxIndicationChar
            // 
            this.numericUpDownMaxIndicationChar.Location = new System.Drawing.Point(346, 138);
            this.numericUpDownMaxIndicationChar.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numericUpDownMaxIndicationChar.Name = "numericUpDownMaxIndicationChar";
            this.numericUpDownMaxIndicationChar.Size = new System.Drawing.Size(42, 19);
            this.numericUpDownMaxIndicationChar.TabIndex = 18;
            this.numericUpDownMaxIndicationChar.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // numericUpDownMaxIndication
            // 
            this.numericUpDownMaxIndication.Location = new System.Drawing.Point(99, 138);
            this.numericUpDownMaxIndication.Maximum = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.numericUpDownMaxIndication.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownMaxIndication.Name = "numericUpDownMaxIndication";
            this.numericUpDownMaxIndication.Size = new System.Drawing.Size(39, 19);
            this.numericUpDownMaxIndication.TabIndex = 17;
            this.numericUpDownMaxIndication.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // checkBoxIndication
            // 
            this.checkBoxIndication.AutoSize = true;
            this.checkBoxIndication.Checked = true;
            this.checkBoxIndication.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxIndication.Location = new System.Drawing.Point(8, 120);
            this.checkBoxIndication.Name = "checkBoxIndication";
            this.checkBoxIndication.Size = new System.Drawing.Size(60, 16);
            this.checkBoxIndication.TabIndex = 16;
            this.checkBoxIndication.Text = "適応症";
            this.checkBoxIndication.UseVisualStyleBackColor = true;
            // 
            // checkBoxThera
            // 
            this.checkBoxThera.AutoSize = true;
            this.checkBoxThera.Checked = true;
            this.checkBoxThera.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxThera.Location = new System.Drawing.Point(8, 98);
            this.checkBoxThera.Name = "checkBoxThera";
            this.checkBoxThera.Size = new System.Drawing.Size(72, 16);
            this.checkBoxThera.TabIndex = 15;
            this.checkBoxThera.Text = "薬効分類";
            this.checkBoxThera.UseVisualStyleBackColor = true;
            // 
            // checkBoxExcludeMyOrg
            // 
            this.checkBoxExcludeMyOrg.AutoSize = true;
            this.checkBoxExcludeMyOrg.Checked = true;
            this.checkBoxExcludeMyOrg.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxExcludeMyOrg.Location = new System.Drawing.Point(189, 14);
            this.checkBoxExcludeMyOrg.Name = "checkBoxExcludeMyOrg";
            this.checkBoxExcludeMyOrg.Size = new System.Drawing.Size(84, 16);
            this.checkBoxExcludeMyOrg.TabIndex = 13;
            this.checkBoxExcludeMyOrg.Text = "自施設除外";
            this.checkBoxExcludeMyOrg.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 46);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(65, 12);
            this.label4.TabIndex = 12;
            this.label4.Text = "薬剤数上限";
            // 
            // checkBoxAutofetch
            // 
            this.checkBoxAutofetch.AutoSize = true;
            this.checkBoxAutofetch.Location = new System.Drawing.Point(52, 64);
            this.checkBoxAutofetch.Name = "checkBoxAutofetch";
            this.checkBoxAutofetch.Size = new System.Drawing.Size(72, 16);
            this.checkBoxAutofetch.TabIndex = 5;
            this.checkBoxAutofetch.Text = "自動取得";
            this.checkBoxAutofetch.UseVisualStyleBackColor = true;
            // 
            // buttonSave
            // 
            this.buttonSave.Image = global::OQSDrug.Properties.Resources.Save;
            this.buttonSave.Location = new System.Drawing.Point(280, 356);
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Size = new System.Drawing.Size(89, 28);
            this.buttonSave.TabIndex = 23;
            this.buttonSave.Text = "上書保存";
            this.buttonSave.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.buttonSave.UseVisualStyleBackColor = true;
            this.buttonSave.Click += new System.EventHandler(this.buttonSave_Click);
            // 
            // buttonDelete
            // 
            this.buttonDelete.Image = global::OQSDrug.Properties.Resources.Delete;
            this.buttonDelete.Location = new System.Drawing.Point(280, 322);
            this.buttonDelete.Name = "buttonDelete";
            this.buttonDelete.Size = new System.Drawing.Size(89, 28);
            this.buttonDelete.TabIndex = 21;
            this.buttonDelete.Text = "削除";
            this.buttonDelete.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.buttonDelete.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.buttonDelete.UseVisualStyleBackColor = true;
            this.buttonDelete.Click += new System.EventHandler(this.buttonDelete_Click);
            // 
            // listBoxTemplates
            // 
            this.listBoxTemplates.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.listBoxTemplates.Font = new System.Drawing.Font("MS UI Gothic", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.listBoxTemplates.FormattingEnabled = true;
            this.listBoxTemplates.Location = new System.Drawing.Point(12, 12);
            this.listBoxTemplates.Name = "listBoxTemplates";
            this.listBoxTemplates.Size = new System.Drawing.Size(262, 472);
            this.listBoxTemplates.TabIndex = 25;
            // 
            // groupBox3
            // 
            this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox3.Controls.Add(this.textBoxTplTitle);
            this.groupBox3.Controls.Add(this.comboBoxModels);
            this.groupBox3.Controls.Add(this.groupBox1);
            this.groupBox3.Controls.Add(this.label1);
            this.groupBox3.Controls.Add(this.groupBox2);
            this.groupBox3.Controls.Add(this.checkBoxAutofetch);
            this.groupBox3.Controls.Add(this.label3);
            this.groupBox3.Location = new System.Drawing.Point(375, 12);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(445, 474);
            this.groupBox3.TabIndex = 26;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "プロンプトテンプレート";
            // 
            // buttonAddnew
            // 
            this.buttonAddnew.Image = global::OQSDrug.Properties.Resources.New;
            this.buttonAddnew.Location = new System.Drawing.Point(280, 218);
            this.buttonAddnew.Name = "buttonAddnew";
            this.buttonAddnew.Size = new System.Drawing.Size(89, 28);
            this.buttonAddnew.TabIndex = 27;
            this.buttonAddnew.Text = "新規追加";
            this.buttonAddnew.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.buttonAddnew.UseVisualStyleBackColor = true;
            this.buttonAddnew.Click += new System.EventHandler(this.toolStripButtonAddnew_Click);
            // 
            // buttonClose
            // 
            this.buttonClose.Image = global::OQSDrug.Properties.Resources.Exit;
            this.buttonClose.Location = new System.Drawing.Point(280, 452);
            this.buttonClose.Name = "buttonClose";
            this.buttonClose.Size = new System.Drawing.Size(89, 28);
            this.buttonClose.TabIndex = 28;
            this.buttonClose.Text = "閉じる";
            this.buttonClose.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.buttonClose.UseVisualStyleBackColor = true;
            this.buttonClose.Click += new System.EventHandler(this.buttonClose_Click);
            // 
            // buttonSaveAs
            // 
            this.buttonSaveAs.Image = global::OQSDrug.Properties.Resources.saveas;
            this.buttonSaveAs.Location = new System.Drawing.Point(280, 390);
            this.buttonSaveAs.Name = "buttonSaveAs";
            this.buttonSaveAs.Size = new System.Drawing.Size(89, 28);
            this.buttonSaveAs.TabIndex = 29;
            this.buttonSaveAs.Text = "別名保存";
            this.buttonSaveAs.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.buttonSaveAs.UseVisualStyleBackColor = true;
            this.buttonSaveAs.Click += new System.EventHandler(this.buttonSaveAs_Click);
            // 
            // FormLLMsetteing
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(845, 498);
            this.Controls.Add(this.buttonSaveAs);
            this.Controls.Add(this.buttonClose);
            this.Controls.Add(this.buttonAddnew);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.listBoxTemplates);
            this.Controls.Add(this.buttonDelete);
            this.Controls.Add(this.buttonSave);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormLLMsetteing";
            this.Text = "AIテンプレート設定";
            this.Load += new System.EventHandler(this.FormLLMsetteing_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownAcuteThresh)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownChronicThresh)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownMaxMeds)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownMonth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownMaxIndicationChar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownMaxIndication)).EndInit();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.ComboBox comboBoxModels;
        private System.Windows.Forms.TextBox textBoxPrompt;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox textBoxTplTitle;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.CheckBox checkBoxAutofetch;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button buttonSave;
        private System.Windows.Forms.Button buttonDelete;
        private System.Windows.Forms.ListBox listBoxTemplates;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Button buttonAddnew;
        private System.Windows.Forms.Button buttonClose;
        private System.Windows.Forms.CheckBox checkBoxExcludeMyOrg;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.NumericUpDown numericUpDownMaxIndicationChar;
        private System.Windows.Forms.NumericUpDown numericUpDownMaxIndication;
        private System.Windows.Forms.CheckBox checkBoxIndication;
        private System.Windows.Forms.CheckBox checkBoxThera;
        private System.Windows.Forms.NumericUpDown numericUpDownMaxMeds;
        private System.Windows.Forms.NumericUpDown numericUpDownMonth;
        private System.Windows.Forms.CheckBox checkBoxDrugC;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.NumericUpDown numericUpDownChronicThresh;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.NumericUpDown numericUpDownAcuteThresh;
        private System.Windows.Forms.Button buttonSaveAs;
    }
}