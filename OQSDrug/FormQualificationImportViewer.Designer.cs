namespace OQSDrug
{
    partial class FormQualificationImportViewer
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Label labelSummary;
        private System.Windows.Forms.Label labelSelectedCount;
        private System.Windows.Forms.Button buttonCheckAll;
        private System.Windows.Forms.Button buttonClearChecks;
        private System.Windows.Forms.Button buttonSendSelected;
        private System.Windows.Forms.Button buttonClose;
        private System.Windows.Forms.DataGridView dgvQualifications;
        private System.Windows.Forms.TextBox textBoxDetail;
        private System.Windows.Forms.SplitContainer splitContainerMain;

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormQualificationImportViewer));
            this.panelTop = new System.Windows.Forms.Panel();
            this.labelSelectedCount = new System.Windows.Forms.Label();
            this.buttonClose = new System.Windows.Forms.Button();
            this.buttonSendSelected = new System.Windows.Forms.Button();
            this.buttonClearChecks = new System.Windows.Forms.Button();
            this.buttonCheckAll = new System.Windows.Forms.Button();
            this.labelSummary = new System.Windows.Forms.Label();
            this.splitContainerMain = new System.Windows.Forms.SplitContainer();
            this.dgvQualifications = new System.Windows.Forms.DataGridView();
            this.textBoxDetail = new System.Windows.Forms.TextBox();
            this.panelTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).BeginInit();
            this.splitContainerMain.Panel1.SuspendLayout();
            this.splitContainerMain.Panel2.SuspendLayout();
            this.splitContainerMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvQualifications)).BeginInit();
            this.SuspendLayout();
            // 
            // panelTop
            // 
            this.panelTop.Controls.Add(this.labelSelectedCount);
            this.panelTop.Controls.Add(this.buttonClose);
            this.panelTop.Controls.Add(this.buttonSendSelected);
            this.panelTop.Controls.Add(this.buttonClearChecks);
            this.panelTop.Controls.Add(this.buttonCheckAll);
            this.panelTop.Controls.Add(this.labelSummary);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(1084, 56);
            this.panelTop.TabIndex = 0;
            // 
            // labelSelectedCount
            // 
            this.labelSelectedCount.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelSelectedCount.Location = new System.Drawing.Point(545, 20);
            this.labelSelectedCount.Name = "labelSelectedCount";
            this.labelSelectedCount.Size = new System.Drawing.Size(100, 18);
            this.labelSelectedCount.TabIndex = 5;
            this.labelSelectedCount.Text = "選択: 0件";
            this.labelSelectedCount.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // buttonClose
            // 
            this.buttonClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonClose.Location = new System.Drawing.Point(992, 15);
            this.buttonClose.Name = "buttonClose";
            this.buttonClose.Size = new System.Drawing.Size(80, 28);
            this.buttonClose.TabIndex = 4;
            this.buttonClose.Text = "閉じる";
            this.buttonClose.UseVisualStyleBackColor = true;
            this.buttonClose.Click += new System.EventHandler(this.buttonClose_Click);
            // 
            // buttonSendSelected
            // 
            this.buttonSendSelected.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonSendSelected.Location = new System.Drawing.Point(846, 15);
            this.buttonSendSelected.Name = "buttonSendSelected";
            this.buttonSendSelected.Size = new System.Drawing.Size(140, 28);
            this.buttonSendSelected.TabIndex = 3;
            this.buttonSendSelected.Text = "選択レコードを送信";
            this.buttonSendSelected.UseVisualStyleBackColor = true;
            this.buttonSendSelected.Click += new System.EventHandler(this.buttonSendSelected_Click);
            // 
            // buttonClearChecks
            // 
            this.buttonClearChecks.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonClearChecks.Location = new System.Drawing.Point(758, 15);
            this.buttonClearChecks.Name = "buttonClearChecks";
            this.buttonClearChecks.Size = new System.Drawing.Size(82, 28);
            this.buttonClearChecks.TabIndex = 2;
            this.buttonClearChecks.Text = "選択解除";
            this.buttonClearChecks.UseVisualStyleBackColor = true;
            this.buttonClearChecks.Click += new System.EventHandler(this.buttonClearChecks_Click);
            // 
            // buttonCheckAll
            // 
            this.buttonCheckAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCheckAll.Location = new System.Drawing.Point(657, 15);
            this.buttonCheckAll.Name = "buttonCheckAll";
            this.buttonCheckAll.Size = new System.Drawing.Size(95, 28);
            this.buttonCheckAll.TabIndex = 1;
            this.buttonCheckAll.Text = "全件チェック";
            this.buttonCheckAll.UseVisualStyleBackColor = true;
            this.buttonCheckAll.Click += new System.EventHandler(this.buttonCheckAll_Click);
            // 
            // labelSummary
            // 
            this.labelSummary.AutoSize = true;
            this.labelSummary.Location = new System.Drawing.Point(12, 22);
            this.labelSummary.Name = "labelSummary";
            this.labelSummary.Size = new System.Drawing.Size(77, 12);
            this.labelSummary.TabIndex = 0;
            this.labelSummary.Text = "取込結果: 0件";
            // 
            // splitContainerMain
            // 
            this.splitContainerMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerMain.Location = new System.Drawing.Point(0, 56);
            this.splitContainerMain.Name = "splitContainerMain";
            this.splitContainerMain.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainerMain.Panel1
            // 
            this.splitContainerMain.Panel1.Controls.Add(this.dgvQualifications);
            // 
            // splitContainerMain.Panel2
            // 
            this.splitContainerMain.Panel2.Controls.Add(this.textBoxDetail);
            this.splitContainerMain.Size = new System.Drawing.Size(1084, 605);
            this.splitContainerMain.SplitterDistance = 153;
            this.splitContainerMain.TabIndex = 1;
            // 
            // dgvQualifications
            // 
            this.dgvQualifications.AllowUserToAddRows = false;
            this.dgvQualifications.AllowUserToDeleteRows = false;
            this.dgvQualifications.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvQualifications.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvQualifications.Location = new System.Drawing.Point(0, 0);
            this.dgvQualifications.MultiSelect = false;
            this.dgvQualifications.Name = "dgvQualifications";
            this.dgvQualifications.RowHeadersVisible = false;
            this.dgvQualifications.RowTemplate.Height = 21;
            this.dgvQualifications.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvQualifications.Size = new System.Drawing.Size(1084, 153);
            this.dgvQualifications.TabIndex = 0;
            this.dgvQualifications.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvQualifications_CellValueChanged);
            this.dgvQualifications.CurrentCellDirtyStateChanged += new System.EventHandler(this.dgvQualifications_CurrentCellDirtyStateChanged);
            this.dgvQualifications.SelectionChanged += new System.EventHandler(this.dgvQualifications_SelectionChanged);
            // 
            // textBoxDetail
            // 
            this.textBoxDetail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxDetail.Location = new System.Drawing.Point(0, 0);
            this.textBoxDetail.Multiline = true;
            this.textBoxDetail.Name = "textBoxDetail";
            this.textBoxDetail.ReadOnly = true;
            this.textBoxDetail.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxDetail.Size = new System.Drawing.Size(1084, 448);
            this.textBoxDetail.TabIndex = 0;
            // 
            // FormQualificationImportViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1084, 661);
            this.Controls.Add(this.splitContainerMain);
            this.Controls.Add(this.panelTop);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormQualificationImportViewer";
            this.Text = "資格情報取込結果";
            this.Load += new System.EventHandler(this.FormQualificationImportViewer_Load);
            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            this.splitContainerMain.Panel1.ResumeLayout(false);
            this.splitContainerMain.Panel2.ResumeLayout(false);
            this.splitContainerMain.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).EndInit();
            this.splitContainerMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvQualifications)).EndInit();
            this.ResumeLayout(false);

        }
    }
}
