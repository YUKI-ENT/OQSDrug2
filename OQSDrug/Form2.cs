using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml.XPath;
using static OQSDrug.CommonFunctions;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;

namespace OQSDrug
{
    public partial class Form2 : Form
    {
        private Form1 form1;

        public Form2(Form1 parentForm)
        {
            InitializeComponent();
            form1 = parentForm; // Form1のインスタンスを受け取る
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            initForm();
        }
            
        private void initForm()
        {
            // 設定値を読み込む
            textBoxOQSDrugData.Text = Properties.Settings.Default.OQSDrugData;
            textBoxDatadyna.Text = Properties.Settings.Default.Datadyna;
            textBoxOQSFolder.Text = Properties.Settings.Default.OQSFolder;
            
            textBoxMCode.Text = Properties.Settings.Default.MCode;

            comboBoxTimerSecond.Items.AddRange(new object[] { 10, 30, 60 });
            int TimerInterval = Properties.Settings.Default.TimerInterval;
            // 該当する値を選択
            if (comboBoxTimerSecond.Items.Contains(TimerInterval))
            {
                comboBoxTimerSecond.SelectedItem = TimerInterval;
            }
            else
            {
                comboBoxTimerSecond.SelectedItem = 30;
            }

            //PDFはやめる
            Properties.Settings.Default.DrugFileCategory = 4;
            Properties.Settings.Default.YZspan = 24;
            Properties.Settings.Default.KensinFileCategory = 0;
            Properties.Settings.Default.Save();
            

            textBoxTemprs.Text = Properties.Settings.Default.temprs;

            checkBoxTopmost.Checked = Properties.Settings.Default.ViewerTopmost;

            checkBoxMinimumStart.Checked = Properties.Settings.Default.MinimumStart;

            int savedIndex = Properties.Settings.Default.RSBID;
            if (savedIndex >= 0 && savedIndex < comboBoxRSBID.Items.Count)
            {
                comboBoxRSBID.SelectedIndex = savedIndex;
            }
            else
            {
                comboBoxRSBID.SelectedIndex = 0;
            }

            checkBoxKeepXml.Checked = Properties.Settings.Default.KeepXml;

            checkBoxRSBreloadXml.Checked = Properties.Settings.Default.RSBXml;
            textBoxRSBxmlURL.Text = Properties.Settings.Default.RSBXmlURL;

            //comboBoxDynaTable.SelectedItem = Properties.Settings.Default.DynaTable;

            comboBoxViewSpan.Items.AddRange(new object[] { 0, 1, 3, 6, 12 });
            if (comboBoxViewSpan.Items.Contains(Properties.Settings.Default.ViewerSpan))
            {
                comboBoxViewSpan.SelectedItem = Properties.Settings.Default.ViewerSpan;
            }
            else
            {
                comboBoxViewSpan.SelectedIndex = 3;
            }

            checkBoxOmitMyOrg.Checked = Properties.Settings.Default.OmitMyOrg;

            checkBoxAutoStart.Checked = Properties.Settings.Default.AutoStart;

            textBoxPGaddress.Text = Properties.Settings.Default.PGaddress;
            textBoxPGport.Text = Properties.Settings.Default.PGport.ToString();
            textBoxPGuser.Text = Properties.Settings.Default.PGuser;
            textBoxPGpass.Text = decodePassword( Properties.Settings.Default.PGpass);

            textBoxLLMserver.Text = Properties.Settings.Default.LLMserver;
            textBoxLLMport.Text = Properties.Settings.Default.LLMport.ToString();
            //textBoxLLMmodel.Text = Properties.Settings.Default.LLMmodel;
            textBoxLLMtimeout.Text = Properties.Settings.Default.LLMtimeout.ToString();

            checkBoxAI.Checked = Properties.Settings.Default.AIauto;


            //LLM Model list
            // 設定から既定モデル名を取得
            var def = (Properties.Settings.Default.LLMmodel ?? "").Trim();

            // 表示用バインディング
            comboBoxLLMModels.DisplayMember = "Name";
            comboBoxLLMModels.ValueMember = "Name";

            // 既定があればそれだけ先に見せる（未設定ならプレースホルダ）
            List<ModelInfo> _models = null;
            if (!string.IsNullOrWhiteSpace(def))
                _models = new List<ModelInfo> { new ModelInfo { Name = def } };
            else
                _models = new List<ModelInfo> { new ModelInfo { Name = "(モデル未設定)" } };

            comboBoxLLMModels.DataSource = null;            // 念のため
            comboBoxLLMModels.DataSource = _models;
            comboBoxLLMModels.SelectedIndex = 0;

            if (Properties.Settings.Default.DBtype != "pg")
            {
                radioButtonMDB.Checked = true;
                radioButtonPG.Checked = false;

                textBoxOQSDrugData.Enabled = true;
                buttonOQSDrugData.Enabled = true;
                labelMDB1.Enabled = true;
                labelPG1.Enabled = false;
                labelPG2.Enabled = false;
                labelPG3.Enabled = false;
                labelPG4.Enabled = false;
                labelLLMserver.Enabled = false;
                labelLLMport.Enabled = false;
                labelLLMmodel.Enabled = false;
                labelLLMtimeout.Enabled = false;

                textBoxPGaddress.Enabled = false;
                textBoxPGport.Enabled = false;
                textBoxPGuser.Enabled = false;
                textBoxPGpass.Enabled = false;
                textBoxLLMserver.Enabled = false;
                textBoxLLMport.Enabled = false;
                //textBoxLLMmodel.Enabled = false ;
                textBoxLLMtimeout.Enabled = false;
                comboBoxLLMModels.Enabled = false;
                buttonGetModels.Enabled = false;

                checkBoxAI.Enabled = false ;
            }
            else
            {
                radioButtonMDB.Checked = false;
                radioButtonPG.Checked = true;

                textBoxOQSDrugData.Enabled = false;
                buttonOQSDrugData.Enabled = false;
                labelMDB1.Enabled = false;
                labelPG1.Enabled = true;
                labelPG2.Enabled = true;
                labelPG3.Enabled = true;
                labelPG4.Enabled = true;
                labelLLMserver.Enabled = true;
                labelLLMport.Enabled = true;
                labelLLMmodel.Enabled = true;
                labelLLMtimeout.Enabled = true;

                textBoxPGaddress.Enabled = true;
                textBoxPGport.Enabled = true;
                textBoxPGuser.Enabled = true;
                textBoxPGpass.Enabled = true;
                textBoxLLMserver.Enabled = true;
                textBoxLLMport.Enabled = true;
                //textBoxLLMmodel.Enabled = true;
                textBoxLLMtimeout.Enabled = true;
                comboBoxLLMModels.Enabled = true;
                buttonGetModels.Enabled = true;

                checkBoxAI.Enabled = true;
            }

            //DIviewer
            if(Properties.Settings.Default.DIviewer == "RSB")
            {
                radioButtonRSB.Checked = true;
                radioButtonSGML.Checked = false;
            }
            else
            {
                radioButtonRSB.Checked = false;
                radioButtonSGML.Checked = true;
            }

        }

        private void SaveSettings()
        {
            Properties.Settings.Default.OQSDrugData = textBoxOQSDrugData.Text;
            Properties.Settings.Default.Datadyna =textBoxDatadyna.Text;
            Properties.Settings.Default.OQSFolder = textBoxOQSFolder.Text;
            

            Properties.Settings.Default.MCode = textBoxMCode.Text;

            Properties.Settings.Default.TimerInterval = Convert.ToUInt16(comboBoxTimerSecond.SelectedItem.ToString());

          
            Properties.Settings.Default.temprs = textBoxTemprs.Text;

            Properties.Settings.Default.ViewerTopmost = checkBoxTopmost.Checked ;

            Properties.Settings.Default.MinimumStart = checkBoxMinimumStart.Checked;

            Properties.Settings.Default.RSBID = comboBoxRSBID.SelectedIndex;

            Properties.Settings.Default.KeepXml = checkBoxKeepXml.Checked;
            Properties.Settings.Default.RSBXml = checkBoxRSBreloadXml.Checked;
            Properties.Settings.Default.RSBXmlURL = textBoxRSBxmlURL.Text;

            //Properties.Settings.Default.DynaTable = comboBoxDynaTable.SelectedItem.ToString();

            Properties.Settings.Default.ViewerSpan = Convert.ToInt16(comboBoxViewSpan.SelectedItem.ToString());

            Properties.Settings.Default.OmitMyOrg = checkBoxOmitMyOrg.Checked;

            Properties.Settings.Default.AutoStart = checkBoxAutoStart.Checked;

            Properties.Settings.Default.PGaddress = textBoxPGaddress.Text;
            Properties.Settings.Default.PGport    = Convert.ToInt16( textBoxPGport.Text);
            Properties.Settings.Default.PGuser = textBoxPGuser.Text;
            Properties.Settings.Default.PGpass = encodePassword(textBoxPGpass.Text);

            if(radioButtonMDB.Checked)
            {
                Properties.Settings.Default.DBtype = "mdb";
            }
            else
            {
                Properties.Settings.Default.DBtype = "pg";
            }

            Properties.Settings.Default.AIauto = checkBoxAI.Checked;
            Properties.Settings.Default.LLMserver = textBoxLLMserver.Text;
            Properties.Settings.Default.LLMport = Convert.ToInt16(textBoxLLMport.Text);
            //Properties.Settings.Default.LLMmodel = textBoxLLMmodel.Text;
            Properties.Settings.Default.LLMtimeout = Convert.ToInt16(textBoxLLMtimeout.Text);

            Properties.Settings.Default.DIviewer = (radioButtonRSB.Checked) ? "RSB" : "SGML"; 

            Properties.Settings.Default.Save();


        }

        private void buttonOQSDrugData_Click(object sender, EventArgs e)
        {
            // ファイル選択ダイアログの設定
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "MDB files (*.mdb)|*.mdb|All files (*.*)|*.*";
            openFileDialog.Title = "OQSDrug_data.mdbを選択してください";

            // ダイアログを表示して結果を確認
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // 選択したファイルパスをテキストボックスに設定
                textBoxOQSDrugData.Text = openFileDialog.FileName;
            }
            
        }

        private void buttonDatadyna_Click(object sender, EventArgs e)
        {
            // ファイル選択ダイアログの設定
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "MDB files (*.mdb)|*.mdb|All files (*.*)|*.*";
            openFileDialog.Title = "Datadyna.mdbを選択してください";

            // ダイアログを表示して結果を確認
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // 選択したファイルパスをテキストボックスに設定
                textBoxDatadyna.Text = openFileDialog.FileName;
            }
        }

        private void buttonOQSFolder_Click(object sender, EventArgs e)
        {
            // フォルダ選択ダイアログを開く
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "OQSフォルダを選択してください";
                folderDialog.ShowNewFolderButton = true;

                // ダイアログを表示して結果を確認
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    // 選択したフォルダパスをテキストボックスに設定
                    textBoxOQSFolder.Text = folderDialog.SelectedPath;
                }
            }
        }

                        
        private void buttonCancel_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("設定を保存せずに閉じますがよろしいですか？", "確認", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                this.Close();
            }
        }

        private void buttonSaveExit_Click(object sender, EventArgs e)
        {
            SaveSettings();

            this.Close();
        }


        private void buttonSave_Click(object sender, EventArgs e)
        {
            SaveSettings();
            // 確認のためメッセージ表示
            MessageBox.Show("設定が保存されました");
        }

        private void buttonViewerPositionReset_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("薬歴フォームを閉じて、位置をリセットします", "", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                // Form1からForm3を取得して閉じる
                if (form1.formDIInstance != null && !form1.formDIInstance.IsDisposed)
                {
                    form1.formDIInstance.Close();
                }
                Properties.Settings.Default.ViewerBounds = new Rectangle(100, 100, 800, 600);

                if (form1.formTKKInstance != null && !form1.formTKKInstance.IsDisposed)
                {
                    form1.formTKKInstance.Close();
                }
                Properties.Settings.Default.TKKBounds = new Rectangle(100, 100, 500, 600);
                MessageBox.Show("リセットしました");
            }
        }

        /// <summary>
        /// 現在の設定をXMLファイルにエクスポート（保存）
        /// </summary>
        private void buttonExport_Click(object sender, EventArgs e)
        {
            string settingsFilePath;

            SaveFileDialog op = new SaveFileDialog();
            op.Title = "設定の保存先";
            op.FileName = "OQSDrug.config";
            op.InitialDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            op.Filter = "configファイル(*.config)|*.config|すべてのファイル(*.*)|*.*";
            op.FilterIndex = 1;
            op.RestoreDirectory = true;
            op.CheckFileExists = false;
            op.CheckPathExists = true;

            if (op.ShowDialog(this) == DialogResult.OK)
            {
                settingsFilePath = op.FileName;
                Properties.Settings.Default.Save();
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
                config.SaveAs(settingsFilePath);
                MessageBox.Show("設定を保存しました");
            }
            op.Dispose();
        }
        private void buttonImport_Click(object sender, EventArgs e)
        {
            OpenFileDialog op = new OpenFileDialog();
            op.Title = "設定ファイルの読込";
            op.FileName = "OQSDrug.config";
            op.InitialDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            op.Filter = "configファイル(*.config)|*.config|すべてのファイル(*.*)|*.*";
            op.FilterIndex = 1;
            op.RestoreDirectory = true;
            op.CheckFileExists = false;
            op.CheckPathExists = true;

            if (op.ShowDialog(this) == DialogResult.OK)
            {
                string settingsFilePath = op.FileName;
                Properties.Settings appSettings = Properties.Settings.Default;

                try
                {
                    var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);

                    string appSettingsXmlName = Properties.Settings.Default.Context["GroupName"].ToString();
                    // returns "MyApplication.Properties.Settings";

                    // Open settings file as XML
                    var import = XDocument.Load(settingsFilePath);
                    // Get the whole XML inside the settings node
                    var settings = import.XPathSelectElements("//" + appSettingsXmlName);

                    config.GetSectionGroup("userSettings")
                        .Sections[appSettingsXmlName]
                        .SectionInformation
                        .SetRawXml(settings.Single().ToString());
                    config.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection("userSettings");

                    appSettings.Reload();
                    initForm();

                    MessageBox.Show("設定を読み込みました");
                }
                catch (Exception ex) // Should make this more specific
                {
                    // Could not import settings.
                    appSettings.Reload(); // from last set saved, not defaults
                    MessageBox.Show(ex.ToString());
                }
            }

            op.Dispose();

        }

       
        private void checkBoxKeepXml_CheckedChanged(object sender, EventArgs e)
        {
            checkBoxRSBreloadXml.Enabled = checkBoxKeepXml.Checked;
            textBoxRSBxmlURL.Enabled = checkBoxKeepXml.Checked;

            if(!checkBoxKeepXml.Checked ) checkBoxRSBreloadXml.Checked = false;
        }

        
        private void buttonPGupload_Click(object sender, EventArgs e)
        {
            SaveSettings();

            using (var uploadForm = new FormPGupload())
            {
                uploadForm.ShowDialog(this);  // 親フォームを this に指定（任意）
            }

        }

        private void SetDBtype()
        {
            if (radioButtonMDB.Checked)
            {
                textBoxOQSDrugData.Enabled = true;
                buttonOQSDrugData.Enabled = true;
                labelMDB1.Enabled = true;
                labelPG1.Enabled = false;
                labelPG2.Enabled = false;
                labelPG3.Enabled = false;
                labelPG4.Enabled = false;

                textBoxPGaddress.Enabled = false;
                textBoxPGport.Enabled = false;
                textBoxPGuser.Enabled = false;
                textBoxPGpass.Enabled = false;

                checkBoxAI.Enabled = false;
                textBoxLLMserver.Enabled = false;
                textBoxLLMport.Enabled = false;
                buttonGetModels.Enabled = false;
                comboBoxLLMModels.Enabled = false;
                textBoxLLMtimeout.Enabled = false;

                labelLLMserver.Enabled = false;
                labelLLMmodel.Enabled = false;
                labelLLMport.Enabled = false;
                labelLLMtimeout.Enabled = false;

            }
            else
            {
                textBoxOQSDrugData.Enabled = false;
                buttonOQSDrugData.Enabled = false;
                labelMDB1.Enabled = false;
                labelPG1.Enabled = true;
                labelPG2.Enabled = true;
                labelPG3.Enabled = true;
                labelPG4.Enabled = true;

                textBoxPGaddress.Enabled = true;
                textBoxPGport.Enabled = true;
                textBoxPGuser.Enabled = true;
                textBoxPGpass.Enabled = true;

                checkBoxAI.Enabled = true;
                textBoxLLMserver.Enabled = true;
                textBoxLLMport.Enabled = true;
                buttonGetModels.Enabled = true;
                comboBoxLLMModels.Enabled = true;
                textBoxLLMtimeout.Enabled = true;

                labelLLMserver.Enabled = true;
                labelLLMmodel.Enabled = true;
                labelLLMport.Enabled = true;
                labelLLMtimeout.Enabled = true;
            }
        }

        private void radioButtonMDB_CheckedChanged(object sender, EventArgs e)
        {
            SetDBtype();
        }

        private void radioButtonPG_CheckedChanged(object sender, EventArgs e)
        {
            SetDBtype();
        }

        private void checkBoxAI_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxAI.Checked)
            {
                if (textBoxLLMserver.Text.Length == 0 || textBoxLLMport.Text.Length == 0)
                {
                    MessageBox.Show("AI自動検索機能を利用するときは、先にLLMサーバーのAPIアドレスとポートを設定してください");
                    checkBoxAI.Checked = false;
                }
            }
        }

        private async void buttonGetModels_Click(object sender, EventArgs e)
        {
            // 入力チェック
            if (string.IsNullOrWhiteSpace(textBoxLLMserver.Text) || string.IsNullOrWhiteSpace(textBoxLLMport.Text))
            {
                MessageBox.Show("LLMサーバーのアドレスとポートを設定してから実行してください");
                return;
            }

            // 設定保存（サーバー/ポート）
            SaveSettings();

            // URL 組み立て
            string ollamaUrl = $"http://{textBoxLLMserver.Text.Trim()}:{textBoxLLMport.Text.Trim()}";

            // UI保護
            buttonGetModels.Enabled = false;
            Cursor prev = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;

            try
            {
                // モデル一覧取得
                List<ModelInfo> modelList = await CommonFunctions.GetOllamaModelsAsync(ollamaUrl); // ここでグローバルollamaListにもセットされる

                CommonFunctions.SetModelsToComboBox(comboBoxLLMModels, modelList, Properties.Settings.Default.LLMmodel);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"モデル一覧の取得に失敗しました。\n{ex.Message}", "Ollamaエラー");
            }
            finally
            {
                buttonGetModels.Enabled = true;
                Cursor.Current = prev;
            }
        }

        private void comboBoxLLMModels_SelectionChangeCommitted(object sender, EventArgs e)
        {
            var selected = comboBoxLLMModels.SelectedItem as ModelInfo;
            if (selected != null)
            {
                Properties.Settings.Default.LLMmodel = selected.Name;   // ValueMemberをDigestにして保存したいならここをDigestに
                Properties.Settings.Default.Save();
                
            }
        }
    }
}
