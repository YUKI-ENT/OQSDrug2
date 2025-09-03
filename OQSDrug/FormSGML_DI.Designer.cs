namespace OQSDrug
{
    partial class FormSGML_DI
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormSGML_DI));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripTextBoxTitle = new System.Windows.Forms.ToolStripLabel();
            this.toolStripButtonClose = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonClear = new System.Windows.Forms.ToolStripButton();
            this.btnSearch = new System.Windows.Forms.ToolStripButton();
            this.toolStripTextBoxSearch = new System.Windows.Forms.ToolStripTextBox();
            this.dgvList = new System.Windows.Forms.DataGridView();
            this.tabMain = new System.Windows.Forms.TabControl();
            this.tabSections = new System.Windows.Forms.TabPage();
            this.tabSectionsInner = new System.Windows.Forms.TabControl();
            this.tabInter = new System.Windows.Forms.TabPage();
            this.dgvInter = new System.Windows.Forms.DataGridView();
            this.tabContra = new System.Windows.Forms.TabPage();
            this.dgvContra = new System.Windows.Forms.DataGridView();
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvList)).BeginInit();
            this.tabMain.SuspendLayout();
            this.tabSections.SuspendLayout();
            this.tabInter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvInter)).BeginInit();
            this.tabContra.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvContra)).BeginInit();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripTextBoxTitle,
            this.toolStripButtonClose,
            this.toolStripSeparator2,
            this.toolStripButtonClear,
            this.btnSearch,
            this.toolStripTextBoxSearch});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(800, 25);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripTextBoxTitle
            // 
            this.toolStripTextBoxTitle.Font = new System.Drawing.Font("Yu Gothic UI Semibold", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.toolStripTextBoxTitle.Name = "toolStripTextBoxTitle";
            this.toolStripTextBoxTitle.Size = new System.Drawing.Size(0, 22);
            // 
            // toolStripButtonClose
            // 
            this.toolStripButtonClose.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripButtonClose.Image = global::OQSDrug.Properties.Resources.Exit;
            this.toolStripButtonClose.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonClose.Name = "toolStripButtonClose";
            this.toolStripButtonClose.Size = new System.Drawing.Size(57, 22);
            this.toolStripButtonClose.Text = "閉じる";
            this.toolStripButtonClose.Click += new System.EventHandler(this.toolStripButtonClose_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButtonClear
            // 
            this.toolStripButtonClear.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripButtonClear.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonClear.Image = global::OQSDrug.Properties.Resources.Delete;
            this.toolStripButtonClear.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonClear.Name = "toolStripButtonClear";
            this.toolStripButtonClear.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonClear.Text = "クリア";
            this.toolStripButtonClear.Click += new System.EventHandler(this.toolStripButtonClear_Click);
            // 
            // btnSearch
            // 
            this.btnSearch.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.btnSearch.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnSearch.Image = global::OQSDrug.Properties.Resources.Find;
            this.btnSearch.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(23, 22);
            this.btnSearch.Text = "検索";
            this.btnSearch.Click += new System.EventHandler(this.btnSearch_Click);
            // 
            // toolStripTextBoxSearch
            // 
            this.toolStripTextBoxSearch.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripTextBoxSearch.Font = new System.Drawing.Font("Yu Gothic UI", 9F);
            this.toolStripTextBoxSearch.Name = "toolStripTextBoxSearch";
            this.toolStripTextBoxSearch.Size = new System.Drawing.Size(150, 25);
            this.toolStripTextBoxSearch.ToolTipText = "検索薬剤名を入れてください";
            this.toolStripTextBoxSearch.KeyDown += new System.Windows.Forms.KeyEventHandler(this.toolStripTextBoxSearch_KeyDown);
            // 
            // dgvList
            // 
            this.dgvList.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Meiryo UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvList.DefaultCellStyle = dataGridViewCellStyle1;
            this.dgvList.Dock = System.Windows.Forms.DockStyle.Top;
            this.dgvList.Location = new System.Drawing.Point(0, 25);
            this.dgvList.MultiSelect = false;
            this.dgvList.Name = "dgvList";
            this.dgvList.RowTemplate.Height = 21;
            this.dgvList.Size = new System.Drawing.Size(800, 150);
            this.dgvList.TabIndex = 1;
            // 
            // tabMain
            // 
            this.tabMain.Controls.Add(this.tabSections);
            this.tabMain.Controls.Add(this.tabInter);
            this.tabMain.Controls.Add(this.tabContra);
            this.tabMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabMain.Font = new System.Drawing.Font("Meiryo UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.tabMain.Location = new System.Drawing.Point(0, 175);
            this.tabMain.Name = "tabMain";
            this.tabMain.SelectedIndex = 0;
            this.tabMain.Size = new System.Drawing.Size(800, 517);
            this.tabMain.TabIndex = 2;
            this.tabMain.SelectedIndexChanged += new System.EventHandler(this.tabMain_SelectedIndexChanged);
            // 
            // tabSections
            // 
            this.tabSections.Controls.Add(this.tabSectionsInner);
            this.tabSections.Font = new System.Drawing.Font("Meiryo UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.tabSections.Location = new System.Drawing.Point(4, 26);
            this.tabSections.Name = "tabSections";
            this.tabSections.Padding = new System.Windows.Forms.Padding(3);
            this.tabSections.Size = new System.Drawing.Size(792, 487);
            this.tabSections.TabIndex = 2;
            this.tabSections.Text = "添付文書";
            this.tabSections.UseVisualStyleBackColor = true;
            // 
            // tabSectionsInner
            // 
            this.tabSectionsInner.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabSectionsInner.Location = new System.Drawing.Point(3, 3);
            this.tabSectionsInner.Name = "tabSectionsInner";
            this.tabSectionsInner.SelectedIndex = 0;
            this.tabSectionsInner.Size = new System.Drawing.Size(786, 481);
            this.tabSectionsInner.TabIndex = 0;
            // 
            // tabInter
            // 
            this.tabInter.Controls.Add(this.dgvInter);
            this.tabInter.Font = new System.Drawing.Font("Meiryo UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.tabInter.Location = new System.Drawing.Point(4, 26);
            this.tabInter.Name = "tabInter";
            this.tabInter.Padding = new System.Windows.Forms.Padding(3);
            this.tabInter.Size = new System.Drawing.Size(792, 487);
            this.tabInter.TabIndex = 1;
            this.tabInter.Text = "相互作用リスト(PMDA)";
            this.tabInter.UseVisualStyleBackColor = true;
            // 
            // dgvInter
            // 
            this.dgvInter.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvInter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvInter.Location = new System.Drawing.Point(3, 3);
            this.dgvInter.Name = "dgvInter";
            this.dgvInter.RowHeadersVisible = false;
            this.dgvInter.RowTemplate.Height = 21;
            this.dgvInter.Size = new System.Drawing.Size(786, 481);
            this.dgvInter.TabIndex = 0;
            this.dgvInter.DataBindingComplete += new System.Windows.Forms.DataGridViewBindingCompleteEventHandler(this.dgvInter_DataBindingComplete);
            // 
            // tabContra
            // 
            this.tabContra.Controls.Add(this.dgvContra);
            this.tabContra.Location = new System.Drawing.Point(4, 26);
            this.tabContra.Name = "tabContra";
            this.tabContra.Padding = new System.Windows.Forms.Padding(3);
            this.tabContra.Size = new System.Drawing.Size(792, 487);
            this.tabContra.TabIndex = 3;
            this.tabContra.Text = "禁忌リスト(厚労)";
            this.tabContra.UseVisualStyleBackColor = true;
            // 
            // dgvContra
            // 
            this.dgvContra.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvContra.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvContra.Location = new System.Drawing.Point(3, 3);
            this.dgvContra.Name = "dgvContra";
            this.dgvContra.RowTemplate.Height = 21;
            this.dgvContra.Size = new System.Drawing.Size(786, 481);
            this.dgvContra.TabIndex = 0;
            this.dgvContra.DataBindingComplete += new System.Windows.Forms.DataGridViewBindingCompleteEventHandler(this.dgvContra_DataBindingComplete);
            // 
            // FormSGML_DI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 692);
            this.Controls.Add(this.tabMain);
            this.Controls.Add(this.dgvList);
            this.Controls.Add(this.toolStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormSGML_DI";
            this.Text = "PMDA添付文書";
            this.LocationChanged += new System.EventHandler(this.FormSGML_DI_LocationChanged);
            this.SizeChanged += new System.EventHandler(this.FormSGML_DI_SizeChanged);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvList)).EndInit();
            this.tabMain.ResumeLayout(false);
            this.tabSections.ResumeLayout(false);
            this.tabInter.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvInter)).EndInit();
            this.tabContra.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvContra)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton btnSearch;
        private System.Windows.Forms.DataGridView dgvList;
        private System.Windows.Forms.TabControl tabMain;
        private System.Windows.Forms.TabPage tabInter;
        private System.Windows.Forms.DataGridView dgvInter;
        private System.Windows.Forms.TabPage tabSections;
        private System.Windows.Forms.TabControl tabSectionsInner;
        private System.Windows.Forms.ToolStripButton toolStripButtonClose;
        private System.Windows.Forms.ToolStripTextBox toolStripTextBoxSearch;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripLabel toolStripTextBoxTitle;
        private System.Windows.Forms.ToolStripButton toolStripButtonClear;
        private System.Windows.Forms.TabPage tabContra;
        private System.Windows.Forms.DataGridView dgvContra;
    }
}