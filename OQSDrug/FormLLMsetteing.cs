using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using static OQSDrug.CommonFunctions;

namespace OQSDrug
{
    public partial class FormLLMsetteing : Form
    {
        private DataTable _tplTable;               // ai_prompt_tpl を保持
        private bool _loading = false;             // 反映中フラグ（イベントの再入抑制）

        public FormLLMsetteing()
        {
            InitializeComponent();

            // イベント登録
            listBoxTemplates.SelectedIndexChanged += listBoxTemplates_SelectedIndexChanged;
            
        }

        private async void FormLLMsetteing_Load(object sender, EventArgs e)
        {
            _loading = true;
            try
            {
                
                
                _tplTable = await LoadPromptTemplatesAsync(); // 2) 中身を取得

                //models設定
                SetModelsToComboBox(comboBoxModels, ollamaModelList);

                listBoxTemplates.SelectedIndexChanged -= listBoxTemplates_SelectedIndexChanged;
                ShowListBox(_tplTable, 0);  // 3) コンボへバインド
                listBoxTemplates.SelectedIndex = -1;
                listBoxTemplates.SelectedIndexChanged += listBoxTemplates_SelectedIndexChanged;
                
                _loading = false;
                // 先頭を選択して編集欄へ反映
                if (listBoxTemplates.Items.Count > 0)
                    BeginInvoke(new Action(() =>
                    {
                        listBoxTemplates.SelectedIndex = 0;
                    }));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"テンプレートの初期化に失敗しました。\n{ex.Message}", "エラー");
            }
            finally
            {
                _loading = false;
            }
        }

        

        /// <summary>テンプレ一覧（id, tpl_name, model_name, auto_fetch, prompt, prompt_len, payload_type, options_json）</summary>
        private async Task<DataTable> LoadPromptTemplatesAsync()
        {
            var dt = new DataTable();

            const string sql = @"
                SELECT id, tpl_name, model_name, auto_fetch, prompt, prompt_len, options_json
                  FROM public.ai_prompt_tpl
                 ORDER BY id ASC;";

            using (var conn = (NpgsqlConnection)CommonFunctions.GetDbConnection(/*pgsql*/true))
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(sql, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    dt.Load(reader);
                }
            }
            return dt;
        }

        private void ShowListBox(DataTable dt, long tplId = 0)
        {
            // ▼ イベント抑止開始
            _loading = true;
            try
            {
                _tplTable = dt; // ← グローバル側も入れ替える（重要）
                if (dt == null) { listBoxTemplates.DataSource = null; return; }


                // 表示用列が無ければ追加
                if (!dt.Columns.Contains("display_name"))
                {
                    dt.Columns.Add("display_name", typeof(string));
                    foreach (DataRow r in dt.Rows)
                    {
                        var name = r["tpl_name"] as string ?? "";
                        var model = r["model_name"] as string ?? "";
                        r["display_name"] = string.IsNullOrWhiteSpace(model) ? name : $"{name} ({model})";
                    }
                    dt.AcceptChanges();
                }

                listBoxTemplates.BeginUpdate();

                listBoxTemplates.DisplayMember = "display_name";
                listBoxTemplates.ValueMember = "id";
                listBoxTemplates.DataSource = dt;

                if (listBoxTemplates.Items.Count > 0)
                {
                    if (tplId > 0)
                    {
                        // 指定IDがある → ValueMember=id の行を選択
                        listBoxTemplates.SelectedValue = tplId;

                        // 指定IDが存在しなかった場合のフォールバック
                        if (listBoxTemplates.SelectedIndex == -1)
                            listBoxTemplates.SelectedIndex = 0;
                    }
                    else
                    {
                        // 0なら先頭を選択
                        listBoxTemplates.SelectedIndex = 0;
                    }
                }                
            }
            finally
            {
                listBoxTemplates.EndUpdate();
                // ▼ イベント抑止解除
                _loading = false;
            }
        }


        private void listBoxTemplates_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loading) return;

            try
            {
                if (_tplTable == null || listBoxTemplates.SelectedValue == null) return;

                long id;
                if (!long.TryParse(listBoxTemplates.SelectedValue.ToString(), out id)) return;

                DataRow[] rows = _tplTable.Select($"id = {id}");
                if (rows.Length == 0) return;

                var row = rows[0];

                // --- 基本フィールドをフォームへ ---
                textBoxTplTitle.Text = row["tpl_name"] as string ?? "";
                checkBoxAutofetch.Checked = row.Table.Columns.Contains("auto_fetch") &&
                                            row["auto_fetch"] != DBNull.Value &&
                                            (bool)row["auto_fetch"];
                textBoxPrompt.Text = row["prompt"] as string ?? "";

                // モデル（既存ヘルパ）
                var modelName = row.Table.Columns.Contains("model_name") && row["model_name"] != DBNull.Value
                              ? row["model_name"].ToString()
                              : "";
                SetModelsToComboBox(comboBoxModels, ollamaModelList, modelName);

                // --- options_json → 各コントロールへ ---
                string json = row.Table.Columns.Contains("options_json") && row["options_json"] != DBNull.Value
                            ? row["options_json"].ToString()
                            : "{}";

                var opts = SafeDeserializeDict(json);

                // 既定値（キーが無い/型が変でも安全にフォールバック）
                numericUpDownMonth.Value = ClampToNUD(numericUpDownMonth, GetOpt(opts, "months", 6));
                checkBoxExcludeMyOrg.Checked = GetOpt(opts, "excludeMyOrg", false);

                // しきい値をコントロールに持っていないならスキップ（持っているなら適宜反映）
                // 例）numericUpDownChronicThr / numericUpDownAcuteThr を用意しているなら:
                // numericUpDownChronicThr.Value = ClampToNUD(numericUpDownChronicThr, (decimal)GetOpt(opts, "chronicThreshold", 0.6));
                // numericUpDownAcuteThr.Value   = ClampToNUD(numericUpDownAcuteThr,   (decimal)GetOpt(opts, "acuteThreshold",   0.6));

                numericUpDownMaxMeds.Value = ClampToNUD(numericUpDownMaxMeds, GetOpt(opts, "maxMeds", 15));
                checkBoxThera.Checked = GetOpt(opts, "includeThera", true);
                checkBoxIndication.Checked = GetOpt(opts, "includeIndications", true);
                numericUpDownMaxIndication.Value = ClampToNUD(numericUpDownMaxIndication, GetOpt(opts, "maxIndications", 6));
                numericUpDownMaxIndicationChar.Value = ClampToNUD(numericUpDownMaxIndicationChar, GetOpt(opts, "indicationMaxChars", 60));
                checkBoxDrugC.Checked = GetOpt(opts, "includeDrugC", false);
                numericUpDownChronicThresh.Value = ClampToNUD(numericUpDownChronicThresh, GetOpt(opts, "chronicThreshold",0.6m));
                numericUpDownAcuteThresh.Value = ClampToNUD(numericUpDownAcuteThresh, GetOpt(opts, "acuteThreshold", 0.6m));
                // 未使用だが互換のため（保存コードで使うなら維持）
                // bool attachKnowledge = GetOpt(opts, "attachKnowledge", true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"テンプレートの反映に失敗しました。\n{ex.Message}", "エラー");
            }
        }

        // JSON→Dictionary （壊れたJSONでも落ちない）
        private Dictionary<string, object> SafeDeserializeDict(string json)
        {
            try
            {
                var ser = new JavaScriptSerializer();
                return ser.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
            }
            catch { return new Dictionary<string, object>(); }
        }

        // 型安全に取り出し（無ければ既定値）
        private T GetOpt<T>(Dictionary<string, object> dict, string key, T def)
        {
            if (dict == null || !dict.ContainsKey(key) || dict[key] == null) return def;
            try
            {
                object v = dict[key];
                if (v is T t) return t;
                var ty = typeof(T);
                if (ty == typeof(int)) return (T)(object)Convert.ToInt32(v);
                if (ty == typeof(double)) return (T)(object)Convert.ToDouble(v);
                if (ty == typeof(bool)) return (T)(object)Convert.ToBoolean(v);
                if (ty == typeof(string)) return (T)(object)Convert.ToString(v);
                if (ty == typeof(decimal)) return (T)(object)Convert.ToDecimal(v);
                return (T)Convert.ChangeType(v, ty);
            }
            catch { return def; }
        }

        // NumericUpDown の範囲に丸めて代入できる値を返す

        private decimal ClampToNUD(NumericUpDown nud, decimal d)
        {
            if (d < nud.Minimum) d = nud.Minimum;
            if (d > nud.Maximum) d = nud.Maximum;
            return d;
        }

        private void toolStripButtonAddnew_Click(object sender, EventArgs e)
        {
            _loading = true;
            try
            {
                // コンボの選択を外し、編集欄をクリア
                listBoxTemplates.SelectedIndex = -1;

                textBoxTplTitle.Text = "";
                
                SetModelsToComboBox(comboBoxModels,ollamaModelList,Properties.Settings.Default.LLMmodel);

                checkBoxAutofetch.Checked = false;
                textBoxPrompt.Text = "";
                //comboBoxPayloadType.SelectedIndex = -1;
                //comboBoxPayloadType.Tag = 0; // 未設定

                // ステータスなど表示するならここで
            }
            finally
            {
                _loading = false;
            }
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // ▼ options_json をコントロールから構築して JSON 文字列化
        private string BuildOptionsJsonFromForm()
        {
            var opt = new Dictionary<string, object>
            {
                ["months"] = (int)numericUpDownMonth.Value,
                ["excludeMyOrg"] = checkBoxExcludeMyOrg.Checked,
                ["chronicThreshold"] = numericUpDownChronicThresh.Value,  // 固定ならここで。コントロール化してもOK
                ["acuteThreshold"] = numericUpDownAcuteThresh.Value,  // 固定ならここで
                ["maxMeds"] = (int)numericUpDownMaxMeds.Value,
                ["includeThera"] = checkBoxThera.Checked,
                ["includeIndications"] = checkBoxIndication.Checked,
                ["maxIndications"] = (int)numericUpDownMaxIndication.Value,
                ["indicationMaxChars"] = (int)numericUpDownMaxIndicationChar.Value,
                ["includeDrugC"] = checkBoxDrugC.Checked
            };
            var ser = new JavaScriptSerializer();
            return ser.Serialize(opt); // → string（後で Jsonb として渡す）
        }

        private async void buttonSave_Click(object sender, EventArgs e)
        {
            try
            {
                var res = await SaveTemplateAsync(SaveMode.Upsert);
                MessageBox.Show("保存しました。");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存に失敗しました。\n{ex.Message}", "エラー");
            }
        }
        // ▼ 選択中のテンプレIDを安全に取得（データバインド: ValueMember="id" 前提）
        private long? GetSelectedTplId()
        {
            if (listBoxTemplates.SelectedValue == null) return null;
            long id;
            return long.TryParse(listBoxTemplates.SelectedValue.ToString(), out id) ? (long?)id : null;
        }

        private async void buttonDelete_Click(object sender, EventArgs e)
        {
            try
            {
                var id = GetSelectedTplId();
                if (id == null)
                {
                    MessageBox.Show("削除するテンプレートを選択してください。");
                    return;
                }

                if (MessageBox.Show("選択テンプレートを削除します。よろしいですか？",
                                    "確認", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;

                using (var conn = (NpgsqlConnection)CommonFunctions.GetDbConnection(false))
                {
                    await conn.OpenAsync();
                    const string sql = "DELETE FROM public.ai_prompt_tpl WHERE id = @id;";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id.Value);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                // 再読込（選択はクリアされる）
                DataTable dtTemplate = await LoadPromptTemplatesAsync();
                ShowListBox(dtTemplate, 0); MessageBox.Show("削除しました。");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"削除に失敗しました。\n{ex.Message}", "エラー");
            }
        }

        private enum SaveMode
        {
            Upsert,   // 既存IDがあれば UPDATE、なければ INSERT
            SaveAsNew // 常に INSERT（別名で保存）
        }

        private sealed class SaveResult
        {
            public long Id { get; set; }
            public bool IsInsert { get; set; }
        }

        /// <summary>
        /// フォームの入力値を DB へ保存（Upsert / SaveAsNew）し、
        /// 保存後に一覧を再読込して対象IDを選択します。
        /// </summary>
        private async Task<SaveResult> SaveTemplateAsync(SaveMode mode)
        {
            // 1) フォーム値取得
            string tplName = textBoxTplTitle.Text?.Trim() ?? "";
            string modelName = comboBoxModels.Text?.Trim() ?? "";
            bool autoFetch = checkBoxAutofetch.Checked;
            string prompt = textBoxPrompt.Text ?? "";
            int promptLen = prompt.Length;

            if (string.IsNullOrWhiteSpace(tplName))
                throw new InvalidOperationException("タイトルを入力してください。");

            string optionsJson = BuildOptionsJsonFromForm();
            long? selectedId = GetSelectedTplId(); // ListBoxの選択ID（Upsert時のみ使用）

            using (var conn = (Npgsql.NpgsqlConnection)CommonFunctions.GetDbConnection(false))
            {
                await conn.OpenAsync();

                // 2) SaveAsNew のときは重複タイトルチェック
                if (mode == SaveMode.SaveAsNew)
                {
                    const string dupSql = @"SELECT COUNT(*) FROM public.ai_prompt_tpl WHERE tpl_name = @name;";
                    using (var cmd = new Npgsql.NpgsqlCommand(dupSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", tplName);
                        var cnt = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                        if (cnt > 0)
                            throw new InvalidOperationException("同じタイトルのテンプレートが既に存在します。タイトルを変更してから保存してください。");
                    }
                }

                long newId;
                bool isInsert;

                if (mode == SaveMode.SaveAsNew || selectedId == null)
                {
                    // 3) INSERT
                    const string insertSql = @"
                        INSERT INTO public.ai_prompt_tpl
                          (tpl_name, model_name, auto_fetch, prompt, prompt_len, updated_at, options_json)
                        VALUES
                          (@tpl_name, @model_name, @auto_fetch, @prompt, @prompt_len, NOW(), @options_json)
                        RETURNING id;";

                    using (var cmd = new Npgsql.NpgsqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@tpl_name", tplName);
                        cmd.Parameters.AddWithValue("@model_name", (object)modelName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@auto_fetch", autoFetch);
                        cmd.Parameters.AddWithValue("@prompt", (object)prompt ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@prompt_len", (object)promptLen ?? DBNull.Value);
                        var p = cmd.Parameters.Add("@options_json", NpgsqlTypes.NpgsqlDbType.Jsonb);
                        p.Value = optionsJson;

                        newId = (long)await cmd.ExecuteScalarAsync();
                        isInsert = true;
                    }
                }
                else
                {
                    // 4) UPDATE
                    const string updateSql = @"
                        UPDATE public.ai_prompt_tpl
                           SET tpl_name     = @tpl_name,
                               model_name   = @model_name,
                               auto_fetch   = @auto_fetch,
                               prompt       = @prompt,
                               prompt_len   = @prompt_len,
                               updated_at   = NOW(),
                               options_json = @options_json
                         WHERE id = @id;";

                    using (var cmd = new Npgsql.NpgsqlCommand(updateSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@tpl_name", tplName);
                        cmd.Parameters.AddWithValue("@model_name", (object)modelName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@auto_fetch", autoFetch);
                        cmd.Parameters.AddWithValue("@prompt", (object)prompt ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@prompt_len", (object)promptLen ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@id", selectedId.Value);
                        var p = cmd.Parameters.Add("@options_json", NpgsqlTypes.NpgsqlDbType.Jsonb);
                        p.Value = optionsJson;

                        await cmd.ExecuteNonQueryAsync();
                        newId = selectedId.Value;
                        isInsert = false;
                    }
                }

                // 5) 再読込して対象IDを選択（_loading ガードを使う前提）
                var dt = await LoadPromptTemplatesAsync();
                ShowListBox(dt, newId);

                return new SaveResult { Id = newId, IsInsert = isInsert };
            }
        }

        private async void buttonSaveAs_Click(object sender, EventArgs e)
        {
            try
            {
                var res = await SaveTemplateAsync(SaveMode.SaveAsNew);
                MessageBox.Show("別名で保存しました。");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"別名で保存に失敗しました。\n{ex.Message}", "エラー");
            }
        }
    }
}
