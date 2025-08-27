namespace OQSDrug
{
    partial class FormSR
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormSR));
            //this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStrip1 = new ClickThroughToolStrip();

            this.toolStripComboBoxPtID = new System.Windows.Forms.ToolStripComboBox();
            this.toolStripButtonClose = new System.Windows.Forms.ToolStripButton();
            this.dataGridViewSinryo = new System.Windows.Forms.DataGridView();
            this.toolStripButtonSum = new System.Windows.Forms.ToolStripButton();
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewSinryo)).BeginInit();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.Font = new System.Drawing.Font("メイリオ", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripComboBoxPtID,
            this.toolStripButtonClose,
            this.toolStripButtonSum});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(800, 27);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripComboBoxPtID
            // 
            this.toolStripComboBoxPtID.AutoSize = false;
            this.toolStripComboBoxPtID.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.toolStripComboBoxPtID.Font = new System.Drawing.Font("ＭＳ ゴシック", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.toolStripComboBoxPtID.Name = "toolStripComboBoxPtID";
            this.toolStripComboBoxPtID.Size = new System.Drawing.Size(200, 21);
            this.toolStripComboBoxPtID.SelectedIndexChanged += new System.EventHandler(this.toolStripComboBoxPtID_SelectedIndexChanged);
            // 
            // toolStripButtonClose
            // 
            this.toolStripButtonClose.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripButtonClose.Image = global::OQSDrug.Properties.Resources.Exit;
            this.toolStripButtonClose.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonClose.Name = "toolStripButtonClose";
            this.toolStripButtonClose.Size = new System.Drawing.Size(68, 24);
            this.toolStripButtonClose.Text = "閉じる";
            this.toolStripButtonClose.Click += new System.EventHandler(this.toolStripButtonClose_Click);
            // 
            // dataGridViewSinryo
            // 
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Yu Gothic UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridViewSinryo.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dataGridViewSinryo.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Meiryo UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dataGridViewSinryo.DefaultCellStyle = dataGridViewCellStyle2;
            this.dataGridViewSinryo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridViewSinryo.Location = new System.Drawing.Point(0, 27);
            this.dataGridViewSinryo.Name = "dataGridViewSinryo";
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Meiryo UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.dataGridViewSinryo.RowsDefaultCellStyle = dataGridViewCellStyle3;
            this.dataGridViewSinryo.RowTemplate.Height = 21;
            this.dataGridViewSinryo.Size = new System.Drawing.Size(800, 423);
            this.dataGridViewSinryo.TabIndex = 1;
            // 
            // toolStripButtonSum
            // 
            this.toolStripButtonSum.AutoSize = false;
            this.toolStripButtonSum.CheckOnClick = true;
            this.toolStripButtonSum.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonSum.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonSum.Image")));
            this.toolStripButtonSum.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonSum.Name = "toolStripButtonSum";
            this.toolStripButtonSum.Size = new System.Drawing.Size(65, 22);
            this.toolStripButtonSum.Text = "月毎集計";
            this.toolStripButtonSum.CheckedChanged += new System.EventHandler(this.toolStripButtonSum_CheckedChanged);
            // 
            // FormSR
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.dataGridViewSinryo);
            this.Controls.Add(this.toolStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormSR";
            this.Text = "診療手術情報";
            this.Load += new System.EventHandler(this.FormSR_Load);
            this.LocationChanged += new System.EventHandler(this.FormSR_LocationChanged);
            this.SizeChanged += new System.EventHandler(this.FormSR_LocationChanged);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewSinryo)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        //private System.Windows.Forms.ToolStrip toolStrip1;
        private ClickThroughToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripComboBox toolStripComboBoxPtID;
        private System.Windows.Forms.ToolStripButton toolStripButtonClose;
        private System.Windows.Forms.DataGridView dataGridViewSinryo;
        private System.Windows.Forms.ToolStripButton toolStripButtonSum;
    }
}