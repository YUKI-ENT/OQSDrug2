namespace OQSDrug
{
    partial class FormSummary
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormSummary));
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPageDisease = new System.Windows.Forms.TabPage();
            this.dataGridViewSummary = new System.Windows.Forms.DataGridView();
            this.textBoxSummary = new System.Windows.Forms.TextBox();
            this.tabPageInteraction = new System.Windows.Forms.TabPage();
            this.buttonClose = new System.Windows.Forms.Button();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripComboBoxPt = new System.Windows.Forms.ToolStripComboBox();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.toolStripComboBoxSpan = new System.Windows.Forms.ToolStripComboBox();
            this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
            this.tabControl1.SuspendLayout();
            this.tabPageDisease.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewSummary)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPageDisease);
            this.tabControl1.Controls.Add(this.tabPageInteraction);
            this.tabControl1.Location = new System.Drawing.Point(12, 28);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(861, 496);
            this.tabControl1.TabIndex = 0;
            // 
            // tabPageDisease
            // 
            this.tabPageDisease.AccessibleRole = System.Windows.Forms.AccessibleRole.Alert;
            this.tabPageDisease.Controls.Add(this.dataGridViewSummary);
            this.tabPageDisease.Controls.Add(this.textBoxSummary);
            this.tabPageDisease.Location = new System.Drawing.Point(4, 22);
            this.tabPageDisease.Name = "tabPageDisease";
            this.tabPageDisease.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageDisease.Size = new System.Drawing.Size(853, 470);
            this.tabPageDisease.TabIndex = 0;
            this.tabPageDisease.Text = "患者背景";
            this.tabPageDisease.UseVisualStyleBackColor = true;
            // 
            // dataGridViewSummary
            // 
            this.dataGridViewSummary.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridViewSummary.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewSummary.Location = new System.Drawing.Point(6, 304);
            this.dataGridViewSummary.Name = "dataGridViewSummary";
            this.dataGridViewSummary.RowTemplate.Height = 21;
            this.dataGridViewSummary.Size = new System.Drawing.Size(841, 160);
            this.dataGridViewSummary.TabIndex = 1;
            // 
            // textBoxSummary
            // 
            this.textBoxSummary.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxSummary.Font = new System.Drawing.Font("Meiryo UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.textBoxSummary.Location = new System.Drawing.Point(6, 6);
            this.textBoxSummary.Multiline = true;
            this.textBoxSummary.Name = "textBoxSummary";
            this.textBoxSummary.Size = new System.Drawing.Size(841, 294);
            this.textBoxSummary.TabIndex = 0;
            // 
            // tabPageInteraction
            // 
            this.tabPageInteraction.Location = new System.Drawing.Point(4, 22);
            this.tabPageInteraction.Name = "tabPageInteraction";
            this.tabPageInteraction.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageInteraction.Size = new System.Drawing.Size(853, 470);
            this.tabPageInteraction.TabIndex = 1;
            this.tabPageInteraction.Text = "相互作用薬剤";
            this.tabPageInteraction.UseVisualStyleBackColor = true;
            // 
            // buttonClose
            // 
            this.buttonClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonClose.Font = new System.Drawing.Font("Meiryo UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.buttonClose.Location = new System.Drawing.Point(742, 530);
            this.buttonClose.Name = "buttonClose";
            this.buttonClose.Size = new System.Drawing.Size(131, 34);
            this.buttonClose.TabIndex = 1;
            this.buttonClose.Text = "閉じる";
            this.buttonClose.UseVisualStyleBackColor = true;
            this.buttonClose.Click += new System.EventHandler(this.buttonClose_Click);
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripComboBoxPt,
            this.toolStripSeparator1,
            this.toolStripLabel1,
            this.toolStripComboBoxSpan,
            this.toolStripButton1});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(885, 25);
            this.toolStrip1.TabIndex = 2;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripComboBoxPt
            // 
            this.toolStripComboBoxPt.AutoSize = false;
            this.toolStripComboBoxPt.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.toolStripComboBoxPt.Font = new System.Drawing.Font("ＭＳ ゴシック", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.toolStripComboBoxPt.Name = "toolStripComboBoxPt";
            this.toolStripComboBoxPt.Size = new System.Drawing.Size(180, 21);
            this.toolStripComboBoxPt.ToolTipText = "患者一覧";
            this.toolStripComboBoxPt.SelectedIndexChanged += new System.EventHandler(this.toolStripComboBoxPt_SelectedIndexChanged);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(31, 22);
            this.toolStripLabel1.Text = "期間";
            // 
            // toolStripComboBoxSpan
            // 
            this.toolStripComboBoxSpan.Items.AddRange(new object[] {
            "3M",
            "6M",
            "12M",
            "ALL"});
            this.toolStripComboBoxSpan.Name = "toolStripComboBoxSpan";
            this.toolStripComboBoxSpan.Size = new System.Drawing.Size(121, 25);
            // 
            // toolStripButton1
            // 
            this.toolStripButton1.CheckOnClick = true;
            this.toolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton1.Image")));
            this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton1.Name = "toolStripButton1";
            this.toolStripButton1.Size = new System.Drawing.Size(59, 22);
            this.toolStripButton1.Text = "自院除外";
            this.toolStripButton1.ToolTipText = "自院の処方は除外して集計します";
            // 
            // FormSummary
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(885, 576);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.buttonClose);
            this.Controls.Add(this.tabControl1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormSummary";
            this.Text = "AIサマリ";
            this.Load += new System.EventHandler(this.FormSummary_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabPageDisease.ResumeLayout(false);
            this.tabPageDisease.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewSummary)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPageDisease;
        private System.Windows.Forms.TabPage tabPageInteraction;
        private System.Windows.Forms.Button buttonClose;
        private System.Windows.Forms.TextBox textBoxSummary;
        private System.Windows.Forms.DataGridView dataGridViewSummary;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripComboBox toolStripComboBoxPt;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripComboBox toolStripComboBoxSpan;
        private System.Windows.Forms.ToolStripButton toolStripButton1;
    }
}