namespace OQSDrug
{
    partial class FormPGupload
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormPGupload));
            this.buttonMigrate = new System.Windows.Forms.Button();
            this.textBoxStatus = new System.Windows.Forms.TextBox();
            this.labelServer = new System.Windows.Forms.Button();
            this.labelDB = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxMDB = new System.Windows.Forms.TextBox();
            this.buttonSelectMDB = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.buttonCreate = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label3 = new System.Windows.Forms.Label();
            this.buttonClose = new System.Windows.Forms.Button();
            this.buttonDump = new System.Windows.Forms.Button();
            this.buttonImportSGML = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonMigrate
            // 
            this.buttonMigrate.Enabled = false;
            this.buttonMigrate.Font = new System.Drawing.Font("Meiryo UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.buttonMigrate.Location = new System.Drawing.Point(6, 134);
            this.buttonMigrate.Name = "buttonMigrate";
            this.buttonMigrate.Size = new System.Drawing.Size(544, 38);
            this.buttonMigrate.TabIndex = 0;
            this.buttonMigrate.Text = "データ移行開始";
            this.buttonMigrate.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
            this.buttonMigrate.UseVisualStyleBackColor = true;
            this.buttonMigrate.Click += new System.EventHandler(this.buttonMigrate_Click);
            // 
            // textBoxStatus
            // 
            this.textBoxStatus.Location = new System.Drawing.Point(574, 12);
            this.textBoxStatus.Multiline = true;
            this.textBoxStatus.Name = "textBoxStatus";
            this.textBoxStatus.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxStatus.Size = new System.Drawing.Size(341, 470);
            this.textBoxStatus.TabIndex = 1;
            // 
            // labelServer
            // 
            this.labelServer.Enabled = false;
            this.labelServer.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.labelServer.Font = new System.Drawing.Font("Meiryo UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelServer.Location = new System.Drawing.Point(12, 28);
            this.labelServer.Name = "labelServer";
            this.labelServer.Size = new System.Drawing.Size(275, 30);
            this.labelServer.TabIndex = 4;
            this.labelServer.Text = "PostgreSQLサーバー確認中...";
            this.labelServer.UseVisualStyleBackColor = true;
            // 
            // labelDB
            // 
            this.labelDB.Enabled = false;
            this.labelDB.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.labelDB.Font = new System.Drawing.Font("Meiryo UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelDB.Location = new System.Drawing.Point(293, 28);
            this.labelDB.Name = "labelDB";
            this.labelDB.Size = new System.Drawing.Size(255, 30);
            this.labelDB.TabIndex = 5;
            this.labelDB.Text = "OQSDrug_dataデータベース確認中...";
            this.labelDB.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Meiryo UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.label1.Location = new System.Drawing.Point(35, 84);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(75, 17);
            this.label1.TabIndex = 6;
            this.label1.Text = "移行元mdb";
            // 
            // textBoxMDB
            // 
            this.textBoxMDB.Font = new System.Drawing.Font("Meiryo UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.textBoxMDB.Location = new System.Drawing.Point(38, 104);
            this.textBoxMDB.Name = "textBoxMDB";
            this.textBoxMDB.Size = new System.Drawing.Size(482, 24);
            this.textBoxMDB.TabIndex = 7;
            // 
            // buttonSelectMDB
            // 
            this.buttonSelectMDB.Font = new System.Drawing.Font("ＭＳ Ｐゴシック", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.buttonSelectMDB.Location = new System.Drawing.Point(526, 104);
            this.buttonSelectMDB.Name = "buttonSelectMDB";
            this.buttonSelectMDB.Size = new System.Drawing.Size(24, 23);
            this.buttonSelectMDB.TabIndex = 8;
            this.buttonSelectMDB.Text = "...";
            this.buttonSelectMDB.UseVisualStyleBackColor = true;
            this.buttonSelectMDB.Click += new System.EventHandler(this.buttonSelectMDB_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.buttonCreate);
            this.groupBox1.Font = new System.Drawing.Font("Meiryo UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.groupBox1.Location = new System.Drawing.Point(12, 64);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(556, 90);
            this.groupBox1.TabIndex = 9;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "新規作成";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Meiryo UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.label2.ForeColor = System.Drawing.Color.Red;
            this.label2.Location = new System.Drawing.Point(35, 20);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(479, 17);
            this.label2.TabIndex = 10;
            this.label2.Text = "新規にPostgreSQL上にデータベースを作成しますので、既存のデータが消去されます！";
            this.label2.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // buttonCreate
            // 
            this.buttonCreate.Enabled = false;
            this.buttonCreate.Font = new System.Drawing.Font("Meiryo UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.buttonCreate.Location = new System.Drawing.Point(6, 40);
            this.buttonCreate.Name = "buttonCreate";
            this.buttonCreate.Size = new System.Drawing.Size(544, 38);
            this.buttonCreate.TabIndex = 9;
            this.buttonCreate.Text = "データベース/テーブル新規作成";
            this.buttonCreate.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
            this.buttonCreate.UseVisualStyleBackColor = true;
            this.buttonCreate.Click += new System.EventHandler(this.buttonCreate_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.textBoxMDB);
            this.groupBox2.Controls.Add(this.buttonMigrate);
            this.groupBox2.Controls.Add(this.buttonSelectMDB);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.groupBox2.Font = new System.Drawing.Font("Meiryo UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.groupBox2.Location = new System.Drawing.Point(12, 160);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(556, 182);
            this.groupBox2.TabIndex = 10;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "mdbからデータ移行";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Meiryo UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.label3.ForeColor = System.Drawing.Color.Red;
            this.label3.Location = new System.Drawing.Point(35, 24);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(462, 51);
            this.label3.TabIndex = 11;
            this.label3.Text = "既存のOQSDrug_data.mdbをPostgreSQLにアップロードします。\r\n既存のPostgreSQL上のデータが上書き消去されます！\r\n数時間かかる場" +
    "合もあり、その間は他のクライアントでOQSDrugを起動しないでください。";
            // 
            // buttonClose
            // 
            this.buttonClose.Font = new System.Drawing.Font("Meiryo UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.buttonClose.Location = new System.Drawing.Point(775, 505);
            this.buttonClose.Name = "buttonClose";
            this.buttonClose.Size = new System.Drawing.Size(129, 41);
            this.buttonClose.TabIndex = 11;
            this.buttonClose.Text = "閉じる";
            this.buttonClose.UseVisualStyleBackColor = true;
            this.buttonClose.Click += new System.EventHandler(this.buttonClose_Click);
            // 
            // buttonDump
            // 
            this.buttonDump.Enabled = false;
            this.buttonDump.Font = new System.Drawing.Font("Meiryo UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.buttonDump.Location = new System.Drawing.Point(18, 444);
            this.buttonDump.Name = "buttonDump";
            this.buttonDump.Size = new System.Drawing.Size(544, 38);
            this.buttonDump.TabIndex = 11;
            this.buttonDump.Text = "バックアップ";
            this.buttonDump.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
            this.buttonDump.UseVisualStyleBackColor = true;
            this.buttonDump.Click += new System.EventHandler(this.buttonDump_Click);
            // 
            // buttonImportSGML
            // 
            this.buttonImportSGML.Enabled = false;
            this.buttonImportSGML.Font = new System.Drawing.Font("Meiryo UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.buttonImportSGML.Location = new System.Drawing.Point(18, 366);
            this.buttonImportSGML.Name = "buttonImportSGML";
            this.buttonImportSGML.Size = new System.Drawing.Size(544, 38);
            this.buttonImportSGML.TabIndex = 12;
            this.buttonImportSGML.Text = "Backupデータのインポート";
            this.buttonImportSGML.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
            this.buttonImportSGML.UseVisualStyleBackColor = true;
            this.buttonImportSGML.Click += new System.EventHandler(this.buttonImportSGML_Click);
            // 
            // FormPGupload
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(916, 558);
            this.Controls.Add(this.buttonImportSGML);
            this.Controls.Add(this.buttonDump);
            this.Controls.Add(this.buttonClose);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.textBoxStatus);
            this.Controls.Add(this.labelDB);
            this.Controls.Add(this.labelServer);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormPGupload";
            this.Text = "PostgreSQL設定";
            this.Load += new System.EventHandler(this.FormPGupload_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonMigrate;
        private System.Windows.Forms.TextBox textBoxStatus;
        private System.Windows.Forms.Button labelServer;
        private System.Windows.Forms.Button labelDB;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxMDB;
        private System.Windows.Forms.Button buttonSelectMDB;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button buttonCreate;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button buttonClose;
        private System.Windows.Forms.Button buttonDump;
        private System.Windows.Forms.Button buttonImportSGML;
    }
}