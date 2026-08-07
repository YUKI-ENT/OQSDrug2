namespace OQSDrug
{
    partial class FormDynaViewer
    {
        /// <summary>
        /// Designer variable
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.SplitContainer split;
        private System.Windows.Forms.DataGridView dgvList;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Label lblSearch;
        private System.Windows.Forms.Panel rightPanel;
        private System.Windows.Forms.Label lblDisplayPeriod;
        private System.Windows.Forms.NumericUpDown numericDisplayMonths;
        private System.Windows.Forms.Label lblMonths;
        private System.Windows.Forms.Button buttonDisplayPeriod;

        /// <summary>
        /// Clean up resources
        /// </summary>
        /// <param name="disposing"></param>
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormDynaViewer));
            this.split = new System.Windows.Forms.SplitContainer();
            this.dgvList = new System.Windows.Forms.DataGridView();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.lblSearch = new System.Windows.Forms.Label();
            this.rightPanel = new System.Windows.Forms.Panel();
            this.lblDisplayPeriod = new System.Windows.Forms.Label();
            this.numericDisplayMonths = new System.Windows.Forms.NumericUpDown();
            this.lblMonths = new System.Windows.Forms.Label();
            this.buttonDisplayPeriod = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.split)).BeginInit();
            this.split.Panel1.SuspendLayout();
            this.split.Panel2.SuspendLayout();
            this.split.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericDisplayMonths)).BeginInit();
            this.SuspendLayout();
            // 
            // split
            // 
            this.split.Dock = System.Windows.Forms.DockStyle.Fill;
            this.split.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.split.Location = new System.Drawing.Point(0, 0);
            this.split.Name = "split";
            // 
            // split.Panel1
            // 
            this.split.Panel1.Controls.Add(this.dgvList);
            this.split.Panel1.Controls.Add(this.txtSearch);
            this.split.Panel1.Controls.Add(this.lblSearch);
            this.split.Panel1.Controls.Add(this.lblDisplayPeriod);
            this.split.Panel1.Controls.Add(this.numericDisplayMonths);
            this.split.Panel1.Controls.Add(this.lblMonths);
            this.split.Panel1.Controls.Add(this.buttonDisplayPeriod);
            // 
            // split.Panel2
            // 
            this.split.Panel2.Controls.Add(this.rightPanel);
            this.split.Size = new System.Drawing.Size(1100, 700);
            this.split.SplitterDistance = 489;
            this.split.TabIndex = 0;
            // 
            // dgvList
            // 
            this.dgvList.AllowUserToAddRows = false;
            this.dgvList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvList.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvList.Location = new System.Drawing.Point(8, 56);
            this.dgvList.Name = "dgvList";
            this.dgvList.ReadOnly = true;
            this.dgvList.RowHeadersVisible = false;
            this.dgvList.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvList.Size = new System.Drawing.Size(478, 636);
            this.dgvList.TabIndex = 0;
            this.dgvList.SelectionChanged += new System.EventHandler(this.DgvList_SelectionChanged);
            // 
            // txtSearch
            // 
            this.txtSearch.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSearch.Location = new System.Drawing.Point(8, 28);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(478, 19);
            this.txtSearch.TabIndex = 1;
            this.txtSearch.KeyUp += new System.Windows.Forms.KeyEventHandler(this.TxtSearch_KeyUp);
            // 
            // lblSearch
            // 
            this.lblSearch.Font = new System.Drawing.Font("Meiryo UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.lblSearch.Location = new System.Drawing.Point(8, 8);
            this.lblSearch.Name = "lblSearch";
            this.lblSearch.Size = new System.Drawing.Size(200, 20);
            this.lblSearch.TabIndex = 2;
            this.lblSearch.Text = "検索 (カルテ番号または氏名):";
            //
            // lblDisplayPeriod
            //
            this.lblDisplayPeriod.AutoSize = true;
            this.lblDisplayPeriod.Font = new System.Drawing.Font("Meiryo UI", 9F);
            this.lblDisplayPeriod.Location = new System.Drawing.Point(208, 8);
            this.lblDisplayPeriod.Name = "lblDisplayPeriod";
            this.lblDisplayPeriod.Size = new System.Drawing.Size(59, 15);
            this.lblDisplayPeriod.TabIndex = 3;
            this.lblDisplayPeriod.Text = "表示期間";
            //
            // numericDisplayMonths
            //
            this.numericDisplayMonths.Location = new System.Drawing.Point(271, 6);
            this.numericDisplayMonths.Maximum = new decimal(new int[] { 24, 0, 0, 0 });
            this.numericDisplayMonths.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numericDisplayMonths.Name = "numericDisplayMonths";
            this.numericDisplayMonths.Size = new System.Drawing.Size(47, 19);
            this.numericDisplayMonths.TabIndex = 4;
            this.numericDisplayMonths.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numericDisplayMonths.Value = new decimal(new int[] { 3, 0, 0, 0 });
            //
            // lblMonths
            //
            this.lblMonths.AutoSize = true;
            this.lblMonths.Font = new System.Drawing.Font("Meiryo UI", 9F);
            this.lblMonths.Location = new System.Drawing.Point(321, 8);
            this.lblMonths.Name = "lblMonths";
            this.lblMonths.Size = new System.Drawing.Size(31, 15);
            this.lblMonths.TabIndex = 5;
            this.lblMonths.Text = "か月";
            //
            // buttonDisplayPeriod
            //
            this.buttonDisplayPeriod.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonDisplayPeriod.Location = new System.Drawing.Point(423, 3);
            this.buttonDisplayPeriod.Name = "buttonDisplayPeriod";
            this.buttonDisplayPeriod.Size = new System.Drawing.Size(63, 23);
            this.buttonDisplayPeriod.TabIndex = 6;
            this.buttonDisplayPeriod.Text = "表示";
            this.buttonDisplayPeriod.UseVisualStyleBackColor = true;
            this.buttonDisplayPeriod.Click += new System.EventHandler(this.buttonDisplayPeriod_Click);
            // 
            // rightPanel
            // 
            this.rightPanel.AutoScroll = true;
            this.rightPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rightPanel.Location = new System.Drawing.Point(0, 0);
            this.rightPanel.Name = "rightPanel";
            this.rightPanel.Size = new System.Drawing.Size(607, 700);
            this.rightPanel.TabIndex = 0;
            // 
            // FormDynaViewer
            // 
            this.ClientSize = new System.Drawing.Size(1100, 700);
            this.Controls.Add(this.split);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormDynaViewer";
            this.Text = "資格確認バックアップ表示";
            this.split.Panel1.ResumeLayout(false);
            this.split.Panel1.PerformLayout();
            this.split.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.split)).EndInit();
            this.split.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericDisplayMonths)).EndInit();
            this.ResumeLayout(false);

        }
    }
}
