using Newtonsoft.Json.Linq;
using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace OQSDrug
{
    public partial class FormSGML_DI : Form
    {
        private FormDI _parentForm;
        private List<Tuple<string[], double>> _results;

        // ★ 添付文書データの「古さ」判定に使う基準日（ここを書き換えればOK）
        private static readonly DateTime DataUpdatedThreshold = new DateTime(2025, 11, 1);

        // ★ 一度警告を出したら、以降は何度も出さないようにするフラグ
        private bool _dataUpdatedWarningShown = false;

        // 文書内検索関係 添付文書タブ1つ分の情報
        private class SectionInfo
        {
            public string Title { get; set; } = "";
            public TabPage Tab { get; set; } = null;
            public RichTextBox RichTextBox { get; set; } = null;
        }
        // 検索ヒット1件
        private class SearchHit
        {
            public int SectionIndex { get; set; }
            public int CharIndex { get; set; }
            public int Length { get; set; }
            public string SectionTitle { get; set; } = "";
            public string Preview { get; set; } = "";
        }

        private readonly List<SectionInfo> _sections = new List<SectionInfo>();
        private readonly List<SearchHit> _searchHits = new List<SearchHit>();
        private int _searchHitIndex = -1;

        // 小見出し（9.1, 10.1 など）
        private sealed class SubDef
        {
            public string SubTitle { get; set; }
            public string[] Names { get; set; } = Array.Empty<string>();  // 要素名候補（例: "UseInPregnantWomen"）
        }

        // 章（1〜26）
        private sealed class SectionDef
        {
            public string Title { get; set; }                  // 例: "1. 警告"
            public string[] Names { get; set; } = Array.Empty<string>();   // 章全体の要素名候補
            public SubDef[] Subs { get; set; } = Array.Empty<SubDef>();    // 小見出し
        }

        // 26分類（必要に応じて Names を追加・調整してください）
        private readonly SectionDef[] SectionDefs = new[]
{
    new SectionDef {
        Title = "1. 警告",
        Names = new[]{ "Warnings" }
    },

    new SectionDef {
        Title = "2. 禁忌",
        Names = new[]{ "ContraIndications" }
    },

    new SectionDef {
        Title = "3. 組成・性状",
        Names = new[]{ "CompositionAndProperty", "Composition", "Properties" },
        Subs  = new[]{
            new SubDef { SubTitle = "3.1 組成",         Names = new[]{ "Composition" } },
            new SubDef { SubTitle = "3.2 製剤の性状",   Names = new[]{ "Property", "Properties" } },
        }
    },

    new SectionDef {
        Title = "4. 効能又は効果",
        Names = new[]{ "IndicationsOrEfficacy" }
    },

    new SectionDef {
        Title = "5. 効能又は効果に関連する注意",
        Names = new[]{  "IndicationsOrEfficacyRelatedPrecautions", "EfficacyRelatedPrecautions" } 
    },

    new SectionDef {
        Title = "6. 用法及び用量",
        Names = new[]{ "InfoDoseAdmin" }
    },

    new SectionDef {
        Title = "7. 用法及び用量に関連する注意",
        Names = new[]{ "InfoPrecautionsDosage", "InfoDoseAdminRelatedPrecautions" }
    },

    new SectionDef {
        Title = "8. 重要な基本的注意",
        Names = new[]{ "ImportantPrecautions" }
    },

    new SectionDef {
        Title = "9. 特定の背景を有する患者に関する注意",
        Names = new[]{ "PrecautionsForPatientsWithSpecifiedBackgrounds", "UseInSpecificPopulations" },
        Subs  = new[]{
            new SubDef { SubTitle = "9.1 合併症・既往歴等のある患者",
                         Names = new[]{ "ComplicationsAndHistoryPatients", "PatientsWithComplicationsOrHistory" } },
            new SubDef { SubTitle = "9.2 腎機能障害患者",
                         Names = new[]{ "UseInRenalImpairment", "PatientsWithRenalImpairment" } },
            new SubDef { SubTitle = "9.3 肝機能障害患者",
                         Names = new[]{ "UseInHepaticImpairment", "PatientsWithHepaticImpairment" } },
            new SubDef { SubTitle = "9.4 生殖能を有する者",
                         Names = new[]{ "UseInFertileMenAndWomen", "FertileMenAndWomen" } },
            new SubDef { SubTitle = "9.5 妊婦",
                         Names = new[]{ "UseInPregnantWomen" } },
            new SubDef { SubTitle = "9.6 授乳婦",
                         Names = new[]{ "UseInNursingMothers" } },
            new SubDef { SubTitle = "9.7 小児等",
                         Names = new[]{ "UseInChildren" } },
            new SubDef { SubTitle = "9.8 高齢者",
                         Names = new[]{ "UseInElderly" } },
        }
    },

    new SectionDef {
        Title = "10. 相互作用",
        Names = new[]{ "Interactions" },
        Subs  = new[]{
            new SubDef { SubTitle = "10.1 併用禁忌（併用しないこと）",
                         Names = new[]{ "ContraIndicatedCombinations" } },
            new SubDef { SubTitle = "10.2 併用注意（併用に注意すること）",
                         Names = new[]{ "PrecautionsForCombinations" } },
        }
    },

    new SectionDef {
        Title = "11. 副作用",
        Names = new[]{ "AdverseEvents", "AdverseReactions" },
        Subs  = new[]{
            new SubDef { SubTitle = "11.1 重大な副作用",
                         Names = new[]{ "SeriousAdverseReactions", "ImportantAdverseReactions" } },
            new SubDef { SubTitle = "11.2 その他の副作用",
                         Names = new[]{ "OtherAdverseReactions", "OtherAdverseDrugReactions", "OtherAdverseReactionsTable" } },
        }
    },

    new SectionDef {
        Title = "12. 臨床検査結果に及ぼす影響",
        Names = new[]{ "InfluenceOnClinicalLabTest", "EffectsOnLabTest", "InfluenceOnLaboratoryValues" }
    },

    new SectionDef {
        Title = "13. 過量投与",
        Names = new[]{ "Overdosage", "Overdose", "OverDosage" }
    },

    new SectionDef {
        Title = "14. 適用上の注意",
        Names = new[]{ "PrecautionsForApplication" }
    },

    new SectionDef {
        Title = "15. その他の注意",
        Names = new[]{ "OtherPrecautions" },
        Subs  = new[]{
            new SubDef { SubTitle = "15.1 臨床使用に基づく情報",
                         Names = new[]{ "InformationBasedOnClinicalUse" } },
            new SubDef { SubTitle = "15.2 非臨床試験に基づく情報",
                         Names = new[]{ "InformationBasedOnNonclinicalStudies" } },
        }
    },

    new SectionDef {
        Title = "16. 薬物動態",
        Names = new[]{ "Pharmacokinetics" },
        Subs  = new[]{
            new SubDef { SubTitle = "16.1 血中濃度", Names = new[]{ "BloodLevel" } },
            new SubDef { SubTitle = "16.2 吸収",     Names = new[]{ "Absorption" } },
            new SubDef { SubTitle = "16.3 分布",     Names = new[]{ "Distribution" } },
            new SubDef { SubTitle = "16.4 代謝",     Names = new[]{ "Metabolism" } },
            new SubDef { SubTitle = "16.5 排泄",     Names = new[]{ "Excretion" } },
            new SubDef { SubTitle = "16.6 特定の背景を有する患者",
                         Names = new[]{ "UseInSpecificPopulations", "SpecialPopulations" } },
            new SubDef { SubTitle = "16.7 薬物相互作用",
                         Names = new[]{ "DrugDrugInteractions" } },
            new SubDef { SubTitle = "16.8 その他",
                         Names = new[]{ "Others" } },
        }
    },

    new SectionDef {
        Title = "17. 臨床成績",
        Names = new[]{ "ClinicalResults", "ResultsOfClinicalTrials" },
        Subs  = new[]{
            new SubDef { SubTitle = "17.1 有効性及び安全性に関する試験",
                         Names = new[]{ "EfficacyAndSafetyStudies" } },
            new SubDef { SubTitle = "17.2 製造販売後調査等",
                         Names = new[]{ "PostMarketingStudies" } },
            new SubDef { SubTitle = "17.3 その他",
                         Names = new[]{ "Others" } },
        }
    },

    new SectionDef {
        Title = "18. 薬効薬理",
        Names = new[]{ "EfficacyPharmacology" },
        Subs  = new[]{
            new SubDef { SubTitle = "18.1 作用機序",
                         Names = new[]{ "MechanismOfAction" } },
        }
    },

    new SectionDef {
        Title = "19. 有効成分に関する理化学的知見",
        Names = new[]{"PhyschemOfActIngredients", "PhysicochemicalKnowledge", "KnowledgeAboutActiveIngredient" }
    },

    new SectionDef {
        Title = "20. 取扱い上の注意",
        Names = new[]{ "PrecautionsForHandling", "HandlingPrecautions" }
    },

    new SectionDef {
        Title = "21. 承認条件",
        Names = new[]{ "ConditionsOfApproval" }
    },

    new SectionDef {
        Title = "22. 包装",
        Names = new[]{ "Package", "Packaging" }
    },

    new SectionDef {
        Title = "23. 主要文献",
        Names = new[]{ "MainLiterature", "MainReferences" }
    },

    new SectionDef {
        Title = "24. 文献請求先及び問い合わせ先",
        Names = new[]{ "AddresseeOfLiteratureRequest", "ContactInformation", "Inquiries" }
    },

    new SectionDef {
        Title = "25. 保険給付上の注意",
        Names = new[]{ "AttentionOfInsurance", "PrecautionsForInsuranceCoverage", "InsuranceCautions" }
    },

    new SectionDef {
        Title = "26. 製造販売業者等",
        Names = new[]{ "NameAddressManufact", "MarketingAuthorizationHolder", "MAH" }
    },
};


        // フォームクラス内のフィールド
        private XDocument _currentXml;
        // PMDAパッケージ添付文書の名前空間（ns0 の中身）
        private XNamespace _pi;                // 例: "http://info.pmda.go.jp/namespace/prescription_drugs/package_insert/1.0"
        private string _currentDrugName = "";
        private string _currentThera = "";
        private string _currentYj = "";
        private string _currentIndication;

        private readonly BindingSource _bsContra = new BindingSource();

        // xml:lang 取得用（固定）
        private static readonly XNamespace _xml = XNamespace.Xml;

        private readonly string _table;

        private readonly string _initialYj;

        private const int SnapDistance = 16; // 吸着の距離（ピクセル）
        private int SnapCompPixel = 8;  //余白補正

        public FormSGML_DI(FormDI parentFormDI)
        {
            InitializeComponent();
            _table = "public.sgml_rawdata";

            _parentForm = parentFormDI;

            // テーブル名の軽いバリデーション（スキーマ.テーブル だけ許容）
            if (!Regex.IsMatch(_table, @"^[A-Za-z0-9_\.]+$"))
            {
                MessageBox.Show("不正なテーブル名が設定されています。App.config を確認してください。");
                Close();
                return;
            }

            // UI 初期設定
            dgvList.ReadOnly = true;
            dgvList.AllowUserToAddRows = false;
            dgvList.AllowUserToDeleteRows = false;
            dgvList.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvList.MultiSelect = false;
            dgvList.RowHeadersVisible = false;
            //dgvList.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;

            dgvInter.ReadOnly = true;
            dgvInter.AllowUserToAddRows = false;
            dgvInter.AllowUserToDeleteRows = false;
            dgvInter.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            //tvXml.HideSelection = false;
            //tvXml.PathSeparator = "/";

            toolStripTextBoxSearch.Control.ImeMode = ImeMode.Hiragana;
            toolStriptextBoxDocSerach.Control.ImeMode = ImeMode.Hiragana;

            // イベント
            dgvList.SelectionChanged += dgvList_SelectionChanged;

            SetupContraGrid();

            // 起動時に全件は重いので、適当に最新100件などにする場合は txtYj 空で検索（任意）
            // await LoadListAsync();
        }

        /// <summary>
        /// FuzzySearch の結果をセットして画面に表示
        /// </summary>
        public async void SetDrugLists(List<Tuple<string[], double>> results)
        {
            //イベント止める
            dgvList.SelectionChanged -= dgvList_SelectionChanged;

            //tabをもどす

            tabMain.SelectedIndex = 0;

            //詳細タブをすべてクリア
            tabSectionsInner.TabPages.Clear();
            toolStripTextBoxTitle.Text = "";
            
            _results = results ?? new List<Tuple<string[], double>>();

            _currentDrugName = "";
            _currentThera = "";
            _currentYj = "";

            // 先に結果の yj_code を収集
            var yjs = new HashSet<string>(StringComparer.Ordinal);
            foreach (var t in _results)
            {
                var rec = t.Item1; // [0]=商品名, [1]=一般名, [2]=yj_code, [3]=先発, [4]=薬価, [5]=packageNo
                var yj = (rec != null && rec.Length > 2) ? rec[2] : null;
                if (!string.IsNullOrEmpty(yj)) yjs.Add(yj);
            }

            // 表示用DataTableを構築（「添付文書番号」列を含める）
            var dt = BuildResultTable(_results);
            dgvList.AutoGenerateColumns = false;
            dgvList.AllowUserToResizeRows = false;
            dgvList.Columns.Clear();

            dgvList.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "添付文書番号", DataPropertyName = "pkg", Visible = false });
            dgvList.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "商品名", DataPropertyName = "brand", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            dgvList.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "一般名", DataPropertyName = "generic", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            dgvList.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "YJコード", DataPropertyName = "yj", Width = 120 });
            dgvList.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "先発", DataPropertyName = "originator", Width = 60 });
            dgvList.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "薬価",
                DataPropertyName = "price",
                Width = 80,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
            });
            dgvList.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Score",
                DataPropertyName = "score",
                Width = 70,
                Visible = false,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
            });

            dgvList.DataSource = dt;

            if (dgvList.Rows.Count > 0)
            {
                dgvList.ClearSelection();
                dgvList.Rows[0].Selected = true;
                await LoadDetailsAsync(); // ←既存の関数をそのまま呼ぶ
            }

            LoadContraGrid(_currentYj);

            dgvList.SelectionChanged += dgvList_SelectionChanged;

        }

        /// <summary>
        /// 一覧の読み込み
        /// </summary>
        private  DataTable BuildResultTable(List<Tuple<string[], double>> results)
        {
            var dt = new DataTable();
            dt.Columns.Add("pkg", typeof(string));      // 添付文書番号
            dt.Columns.Add("brand", typeof(string));
            dt.Columns.Add("generic", typeof(string));
            dt.Columns.Add("yj", typeof(string));
            dt.Columns.Add("originator", typeof(string));
            dt.Columns.Add("price", typeof(string));
            dt.Columns.Add("score", typeof(string));

            foreach (var t in results)
            {
                var rec = t.Item1;
                var score = t.Item2;

                string brand = SafeArray(rec, 0);
                string generic = SafeArray(rec, 1);
                string yj = SafeArray(rec, 2);
                string originator = SafeArray(rec, 3);
                string price = SafeArray(rec, 4);
                string pkg = SafeArray(rec, 5);  // ← SQLで取得済みをそのまま使う

                dt.Rows.Add(pkg, brand, generic, yj, originator, price, score.ToString("0.000"));
            }
            return dt;
        }

        private static string SafeArray(string[] a, int idx) =>
        (a != null && idx >= 0 && idx < a.Length) ? (a[idx] ?? "") : "";


        /// <summary>
        /// 選択された行の doc_xml と interactions_flat を取得して表示
        /// </summary>
        private async Task LoadDetailsAsync()
        {
            if (dgvList.CurrentRow == null) return;
            var row = dgvList.CurrentRow;
            if (row.DataBoundItem == null) return;

            // DataPropertyName ベースで取得（SetDrugLists で DataPropertyName = "pkg","yj","brand" を設定済み）
            var pkg = GetCellValueByProp(dgvList, row, "pkg");
            _currentYj = GetCellValueByProp(dgvList, row, "yj");
            _currentDrugName = GetCellValueByProp(dgvList, row, "brand"); 

            if (string.IsNullOrWhiteSpace(pkg) || string.IsNullOrWhiteSpace(_currentYj)) return;

            try
            {
                using (IDbConnection conn = CommonFunctions.GetDbConnection(true))
                {
                    if (conn is System.Data.Common.DbConnection dbc) await dbc.OpenAsync();
                    else conn.Open();

                    using (IDbCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = $@"
                            SELECT 
                              doc_xml::text           AS doc_xml_text,
                              therapeutic_class_ja    AS thera,
                              updated_at
                            FROM {_table}
                            WHERE package_insert_no = @pkg AND yj_code = @yj
                            LIMIT 1;";

                        CommonFunctions.AddDbParameter(cmd, "@pkg", pkg);
                        CommonFunctions.AddDbParameter(cmd, "@yj", _currentYj);

                        string xmlText = null;
                        DateTime? updatedAt = null;

                        using (var rd = await ((System.Data.Common.DbCommand)cmd).ExecuteReaderAsync())
                        {
                            if (await rd.ReadAsync())
                            {
                                // DBNull 安全に読む
                                xmlText = rd.IsDBNull(0) ? null : rd.GetString(0);
                                _currentThera = rd.IsDBNull(1) ? null : rd.GetString(1);

                                if (!rd.IsDBNull(2)) updatedAt = rd.GetDateTime(2);
                                
                            }
                        }

                        // ★ 一度だけ、基準日より古ければ警告を出す
                        if (!_dataUpdatedWarningShown && updatedAt.HasValue)
                        {
                            if (updatedAt.Value < DataUpdatedThreshold)
                            {
                                _dataUpdatedWarningShown = true;  // 以降は出さない

                                MessageBox.Show(
                                    this,
                                    $"この添付文書データの最終更新日は {updatedAt.Value:yyyy/MM/dd} です。\n" +
                                    $"基準日より古いため、表示が乱れることがあります。最新データへ更新してください。",
                                    "添付文書データの更新確認",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning
                                );
                            }
                        }

                        _currentXml = null;
                        _pi = null;

                        if (!string.IsNullOrEmpty(xmlText))
                        {
                            try
                            {
                                _currentXml = System.Xml.Linq.XDocument.Parse(xmlText);
                                _pi = _currentXml.Root?.Name.Namespace;
                            }
                            catch
                            {
                                _currentXml = null;
                                _pi = null;
                            }
                        }

                        // UI反映はスレッドセーフに
                        void UpdateUi()
                        {
                            toolStripTextBoxTitle.Text = _currentDrugName ?? string.Empty;
                        }
                        if (InvokeRequired) BeginInvoke((Action)UpdateUi); else UpdateUi();

                        BuildSectionTabs();                 // 既存ロジック

                        await LoadInteractionsFromDbAsync(pkg, _currentYj);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("詳細の取得に失敗しました: " + ex.Message);
            }
        }

        private async Task LoadInteractionsFromDbAsync(string pkg, string yj)
        {
            // FormDI / CommonFunctions.SetInteractionView と揃えたいカラム構成
            var dt = new DataTable();
            dt.Columns.Add("section_type", typeof(string));           // 区分（併用禁忌/併用注意 など）
            dt.Columns.Add("partner_name_ja", typeof(string));        // 相互作用相手
            dt.Columns.Add("symptoms_measures_ja", typeof(string));   // 説明（症状・対応）
            dt.Columns.Add("mechanism_ja", typeof(string));           // 機序

            try
            {
                using (IDbConnection conn = CommonFunctions.GetDbConnection(true))
                {
                    if (conn is System.Data.Common.DbConnection dbc)
                        await dbc.OpenAsync();
                    else
                        conn.Open();

                    using (IDbCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            SELECT
                                section_type,
                                partner_name_ja,
                                symptoms_measures_ja,
                                mechanism_ja
                            FROM public.sgml_interaction
                            WHERE package_insert_no = @pkg
                              AND yj_code           = @yj
                            ORDER BY
                                CASE
                                  WHEN section_type = '併用禁忌' THEN 0
                                  WHEN section_type = '併用注意' THEN 1
                                  ELSE 2
                                END,
                                partner_name_ja;";

                        CommonFunctions.AddDbParameter(cmd, "@pkg", pkg);
                        CommonFunctions.AddDbParameter(cmd, "@yj", yj);

                        using (var rd = await ((System.Data.Common.DbCommand)cmd).ExecuteReaderAsync())
                        {
                            while (await rd.ReadAsync())
                            {
                                string sec = rd.IsDBNull(0) ? "" : rd.GetString(0);
                                string name = rd.IsDBNull(1) ? "" : rd.GetString(1);
                                string sym = rd.IsDBNull(2) ? "" : rd.GetString(2);
                                string mech = rd.IsDBNull(3) ? "" : rd.GetString(3);

                                dt.Rows.Add(sec, name, sym, mech);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("相互作用の読み込みに失敗しました: " + ex.Message);
            }

            // UIスレッドで DataSource 設定
            void UpdateGrid()
            {
                dgvInter.DataSource = dt;

                // 起動時の例外対策でタブ切り替え時にやってるなら、この2行はコメントのままでもOK
                //CommonFunctions.SetInteractionView(dgvInter);
                //CommonFunctions.SetInteractionColors(dgvInter);
            }
            if (InvokeRequired) BeginInvoke((Action)UpdateGrid); else UpdateGrid();
        }


        private string GetCellValueByProp(DataGridView dgv, DataGridViewRow row, string propName)
        {
            var col = dgv.Columns
                         .Cast<DataGridViewColumn>()
                         .FirstOrDefault(c => string.Equals(c.DataPropertyName, propName, StringComparison.Ordinal));
            if (col == null) return null;
            return Convert.ToString(row.Cells[col.Index].Value);
        }



        private void AppendXmlNode(XmlNode xnode, TreeNode tnode)
        {
            // 属性
            if (xnode.Attributes?.Count > 0)
            {
                var attrs = tnode.Nodes.Add("@attributes");
                foreach (XmlAttribute a in xnode.Attributes)
                    attrs.Nodes.Add($"{a.Name} = \"{a.Value}\"");
            }

            // 子
            foreach (XmlNode ch in xnode.ChildNodes)
            {
                if (ch.NodeType == XmlNodeType.Element)
                {
                    var c = tnode.Nodes.Add(ch.Name);
                    AppendXmlNode(ch, c);
                }
                else if (ch.NodeType == XmlNodeType.Text || ch.NodeType == XmlNodeType.CDATA)
                {
                    var val = (ch.Value ?? "").Trim();
                    if (!string.IsNullOrEmpty(val))
                        tnode.Nodes.Add($"#text: {Trim(val)}");
                }
                // コメントやProcessingInstructionが必要ならここで追加
            }
        }

        private static string Trim(string s, int max = 200)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return (s.Length > max) ? (s.Substring(0, max) + " …") : s;
        }

        private void BuildSectionTabs()
        {
            tabSectionsInner.TabPages.Clear();

            // 検索情報もリセット
            _sections.Clear();
            _searchHits.Clear();
            _searchHitIndex = -1;
            toolStriptextBoxDocSerach.Text = string.Empty;
            labelMatches.Text = string.Empty;

            // まず Indication を XML から取得（_currentXml/_pi がある場合のみ）
            if (_currentXml != null && _pi != null)
            {
                _currentIndication = NormalizeBlankLines(GatherByNames(new[] { "IndicationsOrEfficacy" }));
            }
            // ContraIndications、ImportantPrecautions も概要に
            string contraIndication    = NormalizeBlankLines(GatherByNames(new[] { "ContraIndications" }));
            string importantPrecaution = NormalizeBlankLines(GatherByNames(new[] { "ImportantPrecautions" }));
            string Warings             = NormalizeBlankLines(GatherByNames(new[] { "Warnings" }));
            
            // ① 概要タブ（最初に追加）
            AddSummaryTab(_currentDrugName, _currentThera, _currentYj, _currentIndication, contraIndication, importantPrecaution, Warings);

            if (_currentXml == null || _pi == null) return;

            foreach (var sec in SectionDefs)
            {
                // 章（上位）
                var chapter = GatherByNames(sec.Names);

                // 小見出し（下位）
                var sb = new StringBuilder();
                if (!string.IsNullOrWhiteSpace(chapter))
                    sb.AppendLine(chapter);

                foreach (var sub in sec.Subs)
                {
                    var subText = GatherByNames(sub.Names);
                    if (!string.IsNullOrWhiteSpace(subText))
                    {
                        if (sb.Length > 0) sb.AppendLine().AppendLine();
                        sb.AppendLine(sub.SubTitle);
                        sb.AppendLine(new string('―', Math.Min(20, sub.SubTitle.Length)));
                        sb.AppendLine(subText.Trim());
                    }
                }

                var body = NormalizeBlankLines(sb.ToString().Trim());
                if (string.IsNullOrWhiteSpace(body)) continue; // ← 内容が無ければタブを作らない

                var page = new TabPage(sec.Title);
                var rtb = new RichTextBox
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    DetectUrls = true,
                    WordWrap = true,
                    BorderStyle = BorderStyle.Fixed3D,
                    BackColor = Color.FloralWhite,
                    HideSelection = false,
                    Font = new System.Drawing.Font("Meiryo UI", 12F)
                };

                // 見出し（章タイトル）を太字
                rtb.SelectionFont = new System.Drawing.Font(rtb.Font, System.Drawing.FontStyle.Bold);
                rtb.AppendText(sec.Title + Environment.NewLine + Environment.NewLine);
                rtb.SelectionFont = new System.Drawing.Font(rtb.Font, System.Drawing.FontStyle.Regular);
                rtb.AppendText(body);

                page.Controls.Add(rtb);
                tabSectionsInner.TabPages.Add(page);

                // ★ ここで SectionInfo を登録する ★
                SectionInfo info = new SectionInfo();
                info.Title = sec.Title;
                info.Tab = page;
                info.RichTextBox = rtb;
                _sections.Add(info);

                // ここでカーソルを先頭に戻す
                rtb.Select(0, 0);
                rtb.ScrollToCaret();
            }

            if (tabSectionsInner.TabPages.Count == 0)
            {
                var empty = new TabPage("添付文書");
                empty.Controls.Add(new Label
                {
                    Dock = DockStyle.Fill,
                    TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                    Text = "この文書では表示できる本文が見つかりませんでした。"
                });
                tabSectionsInner.TabPages.Add(empty);
            }
        }

        // 概要タブを作るヘルパ
        private void AddSummaryTab(string drugName, string thera, string yj,
    string indicationText, string contraIndication, string importantPrecaution, string Warnings)
        {
            var page = new TabPage("概要");

            var rtb = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                DetectUrls = true,
                WordWrap = true,
                BorderStyle = BorderStyle.Fixed3D,
                BackColor = Color.FloralWhite,
                HideSelection = false,
                Font = new System.Drawing.Font("Meiryo UI", 12F) // ←この値はベースなのでこのままでOK
            };

            // ---- フォント定義 ----
            var fontTitleBold = new Font("Meiryo UI", 12F, FontStyle.Bold);    // 見出し・タイトル
            var fontBody = new Font("Meiryo UI", 11F, FontStyle.Regular);      // 本文
            var fontBodyBold = new Font("Meiryo UI", 11F, FontStyle.Bold);     // 必要なら使える本文太字

            // ---- タイトル ----
            rtb.SelectionColor = Color.Black;
            rtb.SelectionFont = fontTitleBold;
            rtb.AppendText((string.IsNullOrWhiteSpace(drugName) ? "(薬剤名未設定)" : drugName) + Environment.NewLine);

            // ---- 薬効分類名 ----
            rtb.SelectionFont = fontTitleBold;
            rtb.AppendText("【薬効分類名】" + Environment.NewLine);

            rtb.SelectionFont = fontBody;
            rtb.AppendText(string.IsNullOrWhiteSpace(thera) ? "(情報なし)" : thera);
            rtb.AppendText(Environment.NewLine + Environment.NewLine);


            // ---- 警告 ----
            if (!string.IsNullOrWhiteSpace(Warnings))
            {
                rtb.SelectionFont = fontTitleBold;
                rtb.SelectionColor = Color.Red;
                rtb.AppendText("【1.警告】" + Environment.NewLine);

                rtb.SelectionFont = fontBody;
                rtb.SelectionColor = Color.Red;
                rtb.AppendText(Warnings);
                rtb.AppendText(Environment.NewLine + Environment.NewLine);
            }

            // ---- 禁忌 ----
            if (!string.IsNullOrWhiteSpace(contraIndication))
            {
                rtb.SelectionFont = fontTitleBold;
                rtb.SelectionColor = Color.Red;
                rtb.AppendText("【2.禁忌】" + Environment.NewLine);

                rtb.SelectionFont = fontBody;
                rtb.SelectionColor = Color.Red;
                rtb.AppendText(contraIndication);
                rtb.AppendText(Environment.NewLine + Environment.NewLine);
            }
 
            // ---- 効能・効果 ----
            rtb.SelectionFont = fontTitleBold;
            rtb.AppendText("【4.効能・効果】" + Environment.NewLine);

            rtb.SelectionFont = fontBody;
            rtb.AppendText(string.IsNullOrWhiteSpace(indicationText) ? "(情報なし)" : indicationText);
            rtb.AppendText(Environment.NewLine + Environment.NewLine);


            // ---- 重要な基本的注意 ----
            if (!string.IsNullOrWhiteSpace(importantPrecaution))
            {
                rtb.SelectionColor = Color.Black; // 色リセット
                rtb.SelectionFont = fontTitleBold;
                rtb.AppendText("【8.重要な基本的注意】" + Environment.NewLine);

                rtb.SelectionFont = fontBody;
                rtb.AppendText(importantPrecaution);
                rtb.AppendText(Environment.NewLine);
            }

            // ---- YJコード ----
            if (!string.IsNullOrWhiteSpace(yj))
            {
                rtb.SelectionFont = fontBody;
                rtb.AppendText(Environment.NewLine + "YJコード: " + yj + Environment.NewLine);
            }

            page.Controls.Add(rtb);
            tabSectionsInner.TabPages.Add(page);

            // ★ ここで SectionInfo を登録する ★
            SectionInfo info = new SectionInfo();
            info.Title = "概要";
            info.Tab = page;
            info.RichTextBox = rtb;
            _sections.Add(info);

            // 先頭へスクロール
            rtb.Select(0, 0);
            rtb.ScrollToCaret();
        }


        //空行削除
        private static string NormalizeBlankLines(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // 改行コードを一旦統一
            var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');

            var lines = normalized.Split('\n');
            var sb = new StringBuilder(text.Length);

            foreach (var rawLine in lines)
            {
                var line = rawLine.TrimEnd(); // 行末の空白は消す（任意）

                // 完全な空行（空白のみ含む行）はスキップ
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                sb.Append(line);
                sb.Append(Environment.NewLine);
            }

            return sb.ToString().TrimEnd('\r', '\n');
        }


        // 指定の要素集合から日本語Langを優先して本文を集約
        private string GatherTextFromElements(IEnumerable<XElement> elems)
        {
            if (elems == null) return null;
            var buf = new StringBuilder();

            foreach (var el in elems)
            {
                var t = ExtractDeepText(el);
                if (!string.IsNullOrWhiteSpace(t))
                {
                    if (buf.Length > 0) buf.AppendLine().AppendLine();
                    buf.Append(t.Trim());
                }
            }

            var s = buf.ToString().Trim();
            if (!string.IsNullOrEmpty(s))
            {
                s = Regex.Replace(s, "[ \\t　]+", " ");
                s = Regex.Replace(s, "(\\r?\\n){3,}", "\n\n");
            }
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }

        private string ExtractDeepText(XElement el)
        {
            if (el == null) return null;

            // 1) Lang[@xml:lang='ja'] を全回収
            var langs = el.Descendants(_pi + "Lang")
                          .Where(x => (string)x.Attribute(_xml + "lang") == "ja")
                          .Select(x => x.Value?.Trim())
                          .Where(s => !string.IsNullOrEmpty(s))
                          .ToList();

            if (langs.Count > 0)
                return string.Join("\n\n", langs);

            // 2) フォールバック：配下のテキストノードを連結
            var allText = string.Concat(el
                .DescendantNodes()
                .OfType<XText>()
                .Select(t => t.Value));

            return string.IsNullOrWhiteSpace(allText) ? null : allText.Trim();
        }

        // 例: Names = { "ContraIndications", "Warning" } なら、それぞれの Descendants を集約
        private string GatherByNames(string[] names)
        {
            if (_currentXml == null || _pi == null || names == null || names.Length == 0) return null;

            var targets = names
                .SelectMany(n => _currentXml.Descendants(_pi + n))
                .ToList();

            return GatherTextFromElements(targets);
        }

        private void CollectAllTextNodes(XmlNode node, System.Collections.Generic.List<string> acc)
        {
            foreach (XmlNode ch in node.ChildNodes)
            {
                if (ch.NodeType == XmlNodeType.Text || ch.NodeType == XmlNodeType.CDATA)
                {
                    var s = (ch.Value ?? "").Trim();
                    if (!string.IsNullOrEmpty(s)) acc.Add(s);
                }
                else
                {
                    CollectAllTextNodes(ch, acc);
                }
            }
        }

        private  async void btnSearch_Click(object sender, EventArgs e)
        {
            if (toolStripTextBoxSearch.Text.Length > 0)
            {
                string searchText = toolStripTextBoxSearch.Text;

                List<Tuple<string[], double>> topResults = await CommonFunctions.FuzzySearchAsync(searchText, searchText,"", CommonFunctions.SGMLDI, 0.1, 0.4, 0);
                if (topResults.Count > 0)
                {
                    SetDrugLists(topResults);
                }
                else
                {
                    MessageBox.Show("SGML薬情に該当薬剤が見つかりませんでした。");
                }
            }
        }

        private void toolStripButtonClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void tabMain_SelectedIndexChanged(object sender, EventArgs e)
        {
            
        }

        private void dgvInter_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            // ★ここで既存の共通フォーマッタを適用（列ヘッダや非表示設定など）
            CommonFunctions.SetInteractionView(dgvInter);
            //Color
            CommonFunctions.SetInteractionColors(dgvInter);
        }

        private async void dgvList_SelectionChanged(object sender, EventArgs e)
        {
            await LoadDetailsAsync();
        }

        private void toolStripTextBoxSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnSearch_Click(sender, EventArgs.Empty);
            }
        }

        private void toolStripButtonClear_Click(object sender, EventArgs e)
        {
            toolStripTextBoxSearch.Text = string.Empty;
        }

        private void SetupContraGrid()
        {
            dgvContra.AutoGenerateColumns = false;
            dgvContra.ReadOnly = true;
            dgvContra.AllowUserToAddRows = false;
            dgvContra.AllowUserToDeleteRows = false;
            dgvContra.AllowUserToResizeRows = false;
            dgvContra.AllowUserToResizeColumns = true;
            dgvContra.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dgvContra.MultiSelect = false;
            dgvContra.RowHeadersVisible = false;

            dgvContra.Columns.Clear();
            dgvContra.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "section_type",
                DataPropertyName = "section_type",
                HeaderText = "区分",
                Width = 80,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            });
            dgvContra.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "partner_name_ja",
                DataPropertyName = "partner_name_ja",
                HeaderText = "相互作用相手",
                Width = 200,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            });
            dgvContra.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "symptoms_measures_ja",
                DataPropertyName = "symptoms_measures_ja",
                HeaderText = "説明",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 200
            });
            dgvContra.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "mechanism_ja",
                DataPropertyName = "mechanism_ja",
                HeaderText = "機序・要因",
                Width = 220,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            });

            foreach (DataGridViewColumn col in dgvContra.Columns)
                col.SortMode = DataGridViewColumnSortMode.Automatic;

            // データは常に BindingSource 経由に
            if (dgvContra.DataSource != _bsContra) dgvContra.DataSource = _bsContra;
        }

        public void LoadContraGrid(string currentYj)
        {
            // 初回のみ
            if (dgvContra.Columns.Count == 0) SetupContraGrid();

            var dt = new DataTable();
            dt.Columns.Add("section_type", typeof(string));           // 常に「禁忌」
            dt.Columns.Add("partner_name_ja", typeof(string));        // 相手薬
            dt.Columns.Add("symptoms_measures_ja", typeof(string));   // 症状・対応
            dt.Columns.Add("mechanism_ja", typeof(string));           // 機序・要因

            if (string.IsNullOrWhiteSpace(currentYj))
            {
                _bsContra.DataSource = dt; _bsContra.ResetBindings(false);
                return;
            }

            const string sql = @"
                WITH my AS (
                  SELECT @yj::text AS yj, LEFT(@yj::text, 7) AS yj7
                )
                -- 自分=SELF で登録されている禁忌（相手=TARGET を表示）
                SELECT
                  '禁忌' AS section_type,
                  COALESCE(s_t.brand_name_ja, c.target_name, c.target_generic) AS partner_name_ja,
                  COALESCE(c.symptom_action, '') AS symptoms_measures_ja,
                  COALESCE(c.mechanism, '')      AS mechanism_ja
                FROM public.drug_contraindication c
                JOIN my ON TRUE
                LEFT JOIN public.drug_code_map m_self
                       ON m_self.drugc = c.self_code              -- レセプト→YJ
                LEFT JOIN public.drug_code_map m_tgt
                       ON m_tgt.drugc = c.target_code
                LEFT JOIN public.sgml_rawdata s_t
                       ON s_t.yj_code = m_tgt.yj_code
                WHERE (m_self.yj_code = my.yj OR (m_self.yj7 IS NOT NULL AND m_self.yj7 = my.yj7))

                UNION ALL

                -- 自分=TARGET で登録されている禁忌（相手=SELF を表示）…逆方向も拾う
                SELECT
                  '禁忌' AS section_type,
                  COALESCE(s_s.brand_name_ja, c.self_name, c.self_generic) AS partner_name_ja,
                  COALESCE(c.symptom_action, '') AS symptoms_measures_ja,
                  COALESCE(c.mechanism, '')      AS mechanism_ja
                FROM public.drug_contraindication c
                JOIN my ON TRUE
                LEFT JOIN public.drug_code_map m_self
                       ON m_self.drugc = c.self_code
                LEFT JOIN public.drug_code_map m_tgt
                       ON m_tgt.drugc = c.target_code
                LEFT JOIN public.sgml_rawdata s_s
                       ON s_s.yj_code = m_self.yj_code
                WHERE (m_tgt.yj_code = my.yj OR (m_tgt.yj7 IS NOT NULL AND m_tgt.yj7 = my.yj7))
                ;";

            try
            {
                using (var conn = CommonFunctions.GetDbConnection(true))
                {
                    if (conn is System.Data.Common.DbConnection dbc) dbc.Open();
                    else conn.Open();

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = sql;
                        CommonFunctions.AddDbParameter(cmd, "@yj", currentYj);

                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                string section = r.IsDBNull(0) ? "" : r.GetString(0);
                                string partner = r.IsDBNull(1) ? "" : r.GetString(1);
                                string sym = r.IsDBNull(2) ? "" : r.GetString(2);
                                string mech = r.IsDBNull(3) ? "" : r.GetString(3);
                                dt.Rows.Add(section, partner, sym, mech);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // ここはお好みでログ
                CommonFunctions.AddLogAsync("禁忌リスト読み込みエラー: " + ex.Message);
            }

            _bsContra.DataSource = dt;
            _bsContra.ResetBindings(false);

            // 既存の色分け関数が「区分=禁忌」で塗るなら流用可
            CommonFunctions.SetInteractionColors(dgvContra);
        }

        private void dgvContra_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            CommonFunctions.SetInteractionColors(dgvContra);
        }

        private void FormSGML_DI_LocationChanged(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                CommonFunctions.SnapToScreenEdges(this, SnapDistance, SnapCompPixel);
            }
        }

        private void FormSGML_DI_SizeChanged(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                CommonFunctions.SnapToScreenEdges(this, SnapDistance, SnapCompPixel);
            }
        }

        private void FormSGML_DI_Shown(object sender, EventArgs e)
        {
            toolStripTextBoxSearch.Focus();
        }

        private void RunGlobalSearch()
        {
            string keyword = toolStriptextBoxDocSerach.Text;
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return;
            }

            _searchHits.Clear();
            _searchHitIndex = -1;

            StringComparison comparison = StringComparison.CurrentCultureIgnoreCase;

            for (int i = 0; i < _sections.Count; i++)
            {
                SectionInfo sec = _sections[i];
                string text = sec.RichTextBox.Text;
                int index = 0;

                while (index < text.Length)
                {
                    int found = text.IndexOf(keyword, index, comparison);
                    if (found < 0)
                    {
                        break;
                    }

                    int startPreview = Math.Max(found - 20, 0);
                    int lenPreview = Math.Min(keyword.Length + 40, text.Length - startPreview);
                    string preview = text.Substring(startPreview, lenPreview);
                    preview = preview.Replace("\r", " ").Replace("\n", " ");

                    SearchHit hit = new SearchHit();
                    hit.SectionIndex = i;
                    hit.CharIndex = found;
                    hit.Length = keyword.Length;
                    hit.SectionTitle = sec.Title;
                    hit.Preview = preview;

                    _searchHits.Add(hit);

                    index = found + keyword.Length;
                }
            }

            if (_searchHits.Count == 0)
            {
                MessageBox.Show("該当する文字列が見つかりません。");
                toolStripTextBoxTitle.Text = "";
                return;
            }

            _searchHitIndex = 0;
            JumpToHit(_searchHits[_searchHitIndex]);
            UpdateSearchStatusLabel();
        }
        private void JumpToHit(SearchHit hit)
        {
            if (hit.SectionIndex < 0 || hit.SectionIndex >= _sections.Count)
            {
                return;
            }

            SectionInfo sec = _sections[hit.SectionIndex];

            // 添付文書タブを前面に
            tabMain.SelectedTab = tabSections;       // 外側タブ（名前に合わせて修正してください）
            tabSectionsInner.SelectedTab = sec.Tab;  // 内側タブ

            RichTextBox rtb = sec.RichTextBox;
            if (hit.CharIndex >= 0 && hit.CharIndex + hit.Length <= rtb.TextLength)
            {
                rtb.SelectionStart = hit.CharIndex;
                rtb.SelectionLength = hit.Length;
                rtb.ScrollToCaret();
                rtb.Focus();
            }
        }
        private void MoveToNextHit()
        {
            if (_searchHits.Count == 0)
            {
                return;
            }

            _searchHitIndex++;
            if (_searchHitIndex >= _searchHits.Count)
            {
                _searchHitIndex = 0; // ループ
            }

            JumpToHit(_searchHits[_searchHitIndex]);
            UpdateSearchStatusLabel();
        }

        private void MoveToPrevHit()
        {
            if (_searchHits.Count == 0)
            {
                return;
            }

            _searchHitIndex--;
            if (_searchHitIndex < 0)
            {
                _searchHitIndex = _searchHits.Count - 1;
            }

            JumpToHit(_searchHits[_searchHitIndex]);
            UpdateSearchStatusLabel();
        }

        private void UpdateSearchStatusLabel()
        {
            if (_searchHits.Count == 0)
            {
                labelMatches.Text = "";
            }
            else
            {
                labelMatches.Text =
                    string.Format("{0} / {1} 件ヒット", _searchHitIndex + 1, _searchHits.Count);
            }
        }

        private void buttonDocSerach_Click(object sender, EventArgs e)
        {
            RunGlobalSearch();
        }

        private void buttonDocNext_Click(object sender, EventArgs e)
        {
            if (_searchHits.Count == 0)
            {
                RunGlobalSearch();
            }
            else
            {
                MoveToNextHit();
            }
        }

        private void buttonDocPrev_Click(object sender, EventArgs e)
        {
            if (_searchHits.Count == 0)
            {
                RunGlobalSearch();
            }
            else
            {
                MoveToPrevHit();
            }
        }

        private void textBoxDocSerach_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                RunGlobalSearch();
                e.SuppressKeyPress = true; // ビープ音防止
            }
        }
    }
}
