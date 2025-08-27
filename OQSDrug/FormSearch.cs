using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace OQSDrug
{
    public partial class FormSearch : Form
    {
        //private Form3 _parentForm3;
        private FormDI _parentForm;

        private static string tempHtmlFile =  Path.Combine(Path.GetTempPath(), "tempPostForm.html"); 

        public FormSearch(FormDI parentFormDI)
        {
            InitializeComponent();
            _parentForm = parentFormDI;
        }

        // フォームのロード時に前回の位置を復元
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // 設定から位置を取得
            var savedBounds = Properties.Settings.Default.SearchBounds;

            if (savedBounds != Rectangle.Empty)
            {
                this.StartPosition = FormStartPosition.Manual; // マニュアルモードに設定
                this.Bounds = savedBounds; // 保存された位置を適用
            }
        }
        // フォームの閉じる直前に現在の位置を保存
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            // 現在の位置を保存
            Properties.Settings.Default.SearchBounds = this.Bounds;

            // 設定を保存
            Properties.Settings.Default.Save();
        }

        // Form3 から HTML を受け取るためのメソッド
        public void SetDrugLists(List<Tuple<string[], double>> results)
        {
            listBoxDrugs.Items.Clear();

            foreach (var result in results)
            {
                string[] drugLists = result.Item1;  // レコード（drugLists[0], drugLists[1]）
                //double similarity = result.Item2;   // 類似度
                string Senpatsu = (drugLists[3] == "先発") ? "【先発】" : ""; 

                // 表示名は drugLists[0] に類似度を加えて表示
                string displayText = $"{Senpatsu}{drugLists[0]} ({drugLists[1]})"; 

                // 実際に取得したい値は drugLists[1] にする
                listBoxDrugs.Items.Add(new KeyValuePair<string, string>(displayText, drugLists[0]));
            }
            listBoxDrugs.DisplayMember = "Key";  // 表示されるのは Key（最初の値）
            listBoxDrugs.ValueMember = "Value";  // 実際に取得されるのは Value（2番目の値）

            if (results.Count > 0)
            {
                listBoxDrugs.SelectedIndex = 0;

                listBoxDrugs_DoubleClick(null, EventArgs.Empty);
            }
        }


        static string GenerateHtmlForm(string url, string postData)
        {
            // POSTデータを解析してフォームに変換
            var formInputs = string.Empty;
            foreach (var param in postData.Split('&'))
            {
                var keyValue = param.Split('=');
                if (keyValue.Length == 2)
                {
                    formInputs += $"<input type=\"hidden\" name=\"{keyValue[0]}\" value=\"{keyValue[1]}\">\n";
                }
            }

            // HTMLフォームを生成
            return $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                    <head><META HTTP-EQUIV=""Content-Type"" Content=""text/html; charset=Shift_JIS"">
                        <title>POST Form</title>
                    </head>
                    <body onload=""document.forms[0].submit();"">
                        <form action=""{url}"" method=""post"">
                            {formInputs}
                        </form>
                    </body>
                    </html>";
        }

        private async void buttonSearch_Click(object sender, EventArgs e)
        {
            if(textBoxDrugName.Text.Length > 0)
            {
                List<Tuple<string[], double>> topResults = await _parentForm.FuzzySearchAsync(textBoxDrugName.Text, "", "", CommonFunctions.RSBDI, 0.1, 0.4, 0);
                if (topResults.Count > 0)
                {
                    SetDrugLists(topResults);
                }
                else
                {
                    MessageBox.Show("RSB薬情に該当薬剤が見つかりませんでした。");
                }
            }
        }

        static string ConvertToShiftJisString(string utf8input)
        {
            try
            {
                // UTF-8からShift_JISに変換
                Encoding utf8 = Encoding.UTF8;
                Encoding shiftJis = Encoding.GetEncoding("Shift_JIS");

                // UTF-8文字列をバイト配列に変換
                byte[] utf8Bytes = utf8.GetBytes(utf8input);

                // バイト配列をShift_JISのバイト配列に変換
                byte[] shiftJisBytes = Encoding.Convert(utf8, shiftJis, utf8Bytes);

                // Shift_JISバイト配列を文字列に変換
                return shiftJis.GetString(shiftJisBytes);
            }
            catch (Exception ex)
            {
                MessageBox.Show("文字コード変換でエラー:" + ex.Message);
                return null;
            }
        }

        private void listBoxDrugs_SelectedIndexChanged(object sender, EventArgs e)
        {
            //if (listBoxDrugs.SelectedItem is KeyValuePair<string, string> selectedItem)
            //{
            //    string key = selectedItem.Key;  // 表示された値（Key）
            //    string value = selectedItem.Value;  // 実際に取得したい値（Value）
                
            //    textBoxDrugName.Text = value;
            //}
        }

        private void RequestRSB(string url, string payload)
        {
            if (payload.Length > 0)
            {
                string sjisString = ConvertToShiftJisString(payload);

                string postData = "yakka=" + sjisString;

                // HTMLファイルを生成
                string htmlContent = GenerateHtmlForm(url, postData);
                File.WriteAllText(tempHtmlFile, htmlContent, Encoding.GetEncoding("Shift_JIS"));

                // 標準ブラウザでHTMLを開く
                Process.Start(new ProcessStartInfo
                {
                    FileName = tempHtmlFile,
                    UseShellExecute = true
                });
            }
        }

        private void listBoxDrugs_DoubleClick(object sender, EventArgs e)
        {
            if (listBoxDrugs.SelectedItem is KeyValuePair<string, string> selectedItem)
            {
                string value = selectedItem.Value;

                if (value.Length > 0)
                {
                    RequestRSB("http://localhost/~rsn/kinki.cgi", value);
                }
            }
                
        }

        private void textBoxDrugName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                buttonSearch_Click(sender, e);
            }
        }

        private void buttonExit_Click(object sender, EventArgs e)
        {
            File.Delete(tempHtmlFile);
            this.Close();
        }
    }
}
