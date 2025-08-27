namespace OQSDrug
{
    partial class Form3
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form3));
            this.dataGridViewDH = new System.Windows.Forms.DataGridView();
            this.comboBoxPtID = new System.Windows.Forms.ComboBox();
            this.buttonClose = new System.Windows.Forms.Button();
            this.checkBoxSum = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.radioButton3M = new System.Windows.Forms.RadioButton();
            this.radioButton6M = new System.Windows.Forms.RadioButton();
            this.radioButton12M = new System.Windows.Forms.RadioButton();
            this.radioButtonAll = new System.Windows.Forms.RadioButton();
            this.checkBoxOmitMyOrg = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewDH)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridViewDH
            // 
            this.dataGridViewDH.AllowUserToAddRows = false;
            this.dataGridViewDH.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridViewDH.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.RaisedVertical;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Meiryo UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridViewDH.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dataGridViewDH.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Meiryo UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dataGridViewDH.DefaultCellStyle = dataGridViewCellStyle2;
            this.dataGridViewDH.Location = new System.Drawing.Point(12, 48);
            this.dataGridViewDH.Name = "dataGridViewDH";
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Meiryo UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridViewDH.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
            this.dataGridViewDH.RowTemplate.Height = 21;
            this.dataGridViewDH.Size = new System.Drawing.Size(1240, 575);
            this.dataGridViewDH.TabIndex = 0;
            // 
            // comboBoxPtID
            // 
            this.comboBoxPtID.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxPtID.Font = new System.Drawing.Font("ＭＳ ゴシック", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.comboBoxPtID.FormattingEnabled = true;
            this.comboBoxPtID.Location = new System.Drawing.Point(12, 11);
            this.comboBoxPtID.Name = "comboBoxPtID";
            this.comboBoxPtID.Size = new System.Drawing.Size(174, 24);
            this.comboBoxPtID.TabIndex = 1;
            this.comboBoxPtID.SelectedIndexChanged += new System.EventHandler(this.comboBoxPtID_SelectedIndexChanged);
            // 
            // buttonClose
            // 
            this.buttonClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonClose.Font = new System.Drawing.Font("Yu Gothic UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.buttonClose.Image = global::OQSDrug.Properties.Resources.Exit;
            this.buttonClose.Location = new System.Drawing.Point(1162, 11);
            this.buttonClose.Name = "buttonClose";
            this.buttonClose.Size = new System.Drawing.Size(90, 29);
            this.buttonClose.TabIndex = 3;
            this.buttonClose.Text = "閉じる";
            this.buttonClose.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.buttonClose.UseVisualStyleBackColor = true;
            this.buttonClose.Click += new System.EventHandler(this.buttonClose_Click);
            // 
            // checkBoxSum
            // 
            this.checkBoxSum.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxSum.AutoSize = true;
            this.checkBoxSum.Font = new System.Drawing.Font("Meiryo UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.checkBoxSum.Location = new System.Drawing.Point(192, 13);
            this.checkBoxSum.Name = "checkBoxSum";
            this.checkBoxSum.Size = new System.Drawing.Size(79, 21);
            this.checkBoxSum.TabIndex = 4;
            this.checkBoxSum.Text = "月毎集計";
            this.checkBoxSum.UseVisualStyleBackColor = true;
            this.checkBoxSum.CheckedChanged += new System.EventHandler(this.checkBoxSum_CheckedChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Meiryo UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.label1.Location = new System.Drawing.Point(372, 14);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(60, 17);
            this.label1.TabIndex = 6;
            this.label1.Text = "表示期間";
            // 
            // radioButton3M
            // 
            this.radioButton3M.Appearance = System.Windows.Forms.Appearance.Button;
            this.radioButton3M.FlatAppearance.BorderSize = 0;
            this.radioButton3M.Font = new System.Drawing.Font("Meiryo UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.radioButton3M.Location = new System.Drawing.Point(428, 10);
            this.radioButton3M.Margin = new System.Windows.Forms.Padding(0);
            this.radioButton3M.Name = "radioButton3M";
            this.radioButton3M.Size = new System.Drawing.Size(42, 27);
            this.radioButton3M.TabIndex = 8;
            this.radioButton3M.TabStop = true;
            this.radioButton3M.Tag = "3";
            this.radioButton3M.Text = "3M";
            this.radioButton3M.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.radioButton3M.UseVisualStyleBackColor = true;
            // 
            // radioButton6M
            // 
            this.radioButton6M.Appearance = System.Windows.Forms.Appearance.Button;
            this.radioButton6M.FlatAppearance.BorderSize = 0;
            this.radioButton6M.Font = new System.Drawing.Font("Meiryo UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.radioButton6M.Location = new System.Drawing.Point(470, 10);
            this.radioButton6M.Margin = new System.Windows.Forms.Padding(0);
            this.radioButton6M.Name = "radioButton6M";
            this.radioButton6M.Size = new System.Drawing.Size(42, 27);
            this.radioButton6M.TabIndex = 9;
            this.radioButton6M.TabStop = true;
            this.radioButton6M.Tag = "6";
            this.radioButton6M.Text = "6M";
            this.radioButton6M.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.radioButton6M.UseVisualStyleBackColor = true;
            // 
            // radioButton12M
            // 
            this.radioButton12M.Appearance = System.Windows.Forms.Appearance.Button;
            this.radioButton12M.FlatAppearance.BorderSize = 0;
            this.radioButton12M.Font = new System.Drawing.Font("Meiryo UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.radioButton12M.Location = new System.Drawing.Point(512, 10);
            this.radioButton12M.Margin = new System.Windows.Forms.Padding(0);
            this.radioButton12M.Name = "radioButton12M";
            this.radioButton12M.Size = new System.Drawing.Size(42, 27);
            this.radioButton12M.TabIndex = 10;
            this.radioButton12M.TabStop = true;
            this.radioButton12M.Tag = "12";
            this.radioButton12M.Text = "12M";
            this.radioButton12M.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.radioButton12M.UseVisualStyleBackColor = true;
            // 
            // radioButtonAll
            // 
            this.radioButtonAll.Appearance = System.Windows.Forms.Appearance.Button;
            this.radioButtonAll.FlatAppearance.BorderSize = 0;
            this.radioButtonAll.Font = new System.Drawing.Font("Meiryo UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.radioButtonAll.Location = new System.Drawing.Point(554, 10);
            this.radioButtonAll.Margin = new System.Windows.Forms.Padding(0);
            this.radioButtonAll.Name = "radioButtonAll";
            this.radioButtonAll.Size = new System.Drawing.Size(42, 27);
            this.radioButtonAll.TabIndex = 11;
            this.radioButtonAll.TabStop = true;
            this.radioButtonAll.Tag = "0";
            this.radioButtonAll.Text = "All";
            this.radioButtonAll.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.radioButtonAll.UseVisualStyleBackColor = true;
            // 
            // checkBoxOmitMyOrg
            // 
            this.checkBoxOmitMyOrg.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxOmitMyOrg.AutoSize = true;
            this.checkBoxOmitMyOrg.Font = new System.Drawing.Font("Meiryo UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.checkBoxOmitMyOrg.Location = new System.Drawing.Point(277, 13);
            this.checkBoxOmitMyOrg.Name = "checkBoxOmitMyOrg";
            this.checkBoxOmitMyOrg.Size = new System.Drawing.Size(92, 21);
            this.checkBoxOmitMyOrg.TabIndex = 12;
            this.checkBoxOmitMyOrg.Text = "自施設除外";
            this.checkBoxOmitMyOrg.UseVisualStyleBackColor = true;
            this.checkBoxOmitMyOrg.CheckedChanged += new System.EventHandler(this.checkBoxOmitMyOrg_CheckedChanged);
            // 
            // Form3
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1264, 635);
            this.Controls.Add(this.checkBoxOmitMyOrg);
            this.Controls.Add(this.radioButtonAll);
            this.Controls.Add(this.radioButton12M);
            this.Controls.Add(this.radioButton6M);
            this.Controls.Add(this.radioButton3M);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.checkBoxSum);
            this.Controls.Add(this.buttonClose);
            this.Controls.Add(this.comboBoxPtID);
            this.Controls.Add(this.dataGridViewDH);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form3";
            this.Text = "薬歴ビュワー";
            this.Load += new System.EventHandler(this.Form3_Load);
            this.LocationChanged += new System.EventHandler(this.Form3_LocationChanged);
            this.SizeChanged += new System.EventHandler(this.Form3_SizeChanged);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewDH)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridViewDH;
        private System.Windows.Forms.ComboBox comboBoxPtID;
        private System.Windows.Forms.Button buttonClose;
        private System.Windows.Forms.CheckBox checkBoxSum;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RadioButton radioButton3M;
        private System.Windows.Forms.RadioButton radioButton6M;
        private System.Windows.Forms.RadioButton radioButton12M;
        private System.Windows.Forms.RadioButton radioButtonAll;
        private System.Windows.Forms.CheckBox checkBoxOmitMyOrg;
    }
}