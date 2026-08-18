using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Npgsql;

namespace OQSDrug
{
    public partial class FormSGML_DI
    {
        private sealed class WomenCardControls
        {
            public GroupBox Card { get; set; }
            public Label Level { get; set; }
            public Label Assessment { get; set; }
            public Label Evidence { get; set; }
            public Label Meta { get; set; }
        }

        private sealed class WomenSummaryInfo
        {
            public string PopulationType { get; set; }
            public string AssessmentCode { get; set; }
            public string AssessmentText { get; set; }
            public bool NeedsReview { get; set; }
            public int StatementCount { get; set; }
            public string PreparedYm { get; set; }
            public string EvidenceText { get; set; }
            public string SectionCode { get; set; }
            public string HeadingPath { get; set; }
        }

        private readonly Dictionary<string, WomenCardControls> _womenCards =
            new Dictionary<string, WomenCardControls>(StringComparer.Ordinal);
        private TabPage _tabWomen;
        private Label _labelWomenStatus;
        private DataGridView _dgvWomenStatements;
        private int _womenLoadSequence;
        private Task<bool> _womenAvailabilityTask;
        private bool _womenTabAvailable;

        private void InitializeWomenTab()
        {
            _tabWomen = new TabPage
            {
                Name = "tabWomen",
                Text = "妊婦・授乳(AI)",
                Padding = new Padding(8),
                BackColor = Color.FromArgb(244, 246, 248),
                UseVisualStyleBackColor = false
            };

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = _tabWomen.BackColor,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 190F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _labelWomenStatus = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(65, 72, 82),
                Text = "薬剤を選択すると妊婦・授乳情報を表示します。"
            };
            root.Controls.Add(_labelWomenStatus, 0, 0);

            var cards = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            cards.Controls.Add(CreateWomenCard("PREGNANCY", "妊婦（9.5）"), 0, 0);
            cards.Controls.Add(CreateWomenCard("LACTATION", "授乳婦（9.6）"), 1, 0);
            root.Controls.Add(cards, 0, 1);

            var detailsTitle = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft,
                Font = new Font("Meiryo UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(32, 38, 46),
                Text = "根拠・関連原文（背景情報を含む）"
            };
            root.Controls.Add(detailsTitle, 0, 2);

            _dgvWomenStatements = CreateWomenStatementsGrid();
            root.Controls.Add(_dgvWomenStatements, 0, 3);

            _tabWomen.Controls.Add(root);
        }

        private async Task<bool> EnsureWomenTabAvailabilityAsync()
        {
            if (_womenAvailabilityTask == null)
            {
                _womenAvailabilityTask = CheckWomenTablesAvailableAsync();
            }

            bool available = await _womenAvailabilityTask;
            _womenTabAvailable = available;
            UpdateAiTabOrder();

            return available;
        }

        private async Task<bool> CheckWomenTablesAvailableAsync()
        {
            try
            {
                using (IDbConnection connection = CommonFunctions.GetDbConnection(true))
                {
                    var dbConnection = connection as DbConnection;
                    if (dbConnection != null)
                    {
                        await dbConnection.OpenAsync();
                    }
                    else
                    {
                        connection.Open();
                    }

                    using (IDbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            SELECT
                                to_regclass('public.sgml_women_summary') IS NOT NULL
                                AND to_regclass('public.sgml_women_statement') IS NOT NULL;";
                        var dbCommand = command as DbCommand;
                        object result = dbCommand != null
                            ? await dbCommand.ExecuteScalarAsync()
                            : command.ExecuteScalar();
                        return result != null && result != DBNull.Value && Convert.ToBoolean(result);
                    }
                }
            }
            catch (Exception ex)
            {
                // 接続障害や権限不足はテーブル不存在と断定せず、従来どおりタブ内でエラーを通知する。
                _ = CommonFunctions.AddLogAsync("妊婦・授乳テーブル存在確認エラー: " + ex.Message);
                return true;
            }
        }

        private Control CreateWomenCard(string populationType, string title)
        {
            var card = new GroupBox
            {
                Dock = DockStyle.Fill,
                Text = title,
                Font = new Font("Meiryo UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(32, 38, 46),
                BackColor = Color.White,
                Padding = new Padding(10),
                Margin = populationType == "PREGNANCY"
                    ? new Padding(0, 0, 5, 0)
                    : new Padding(5, 0, 0, 0)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));

            var level = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Meiryo UI", 9.5F, FontStyle.Bold),
                Text = "未読込",
                BackColor = Color.FromArgb(232, 235, 239),
                ForeColor = Color.FromArgb(70, 76, 84),
                Margin = new Padding(0, 0, 0, 4)
            };
            var assessment = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Meiryo UI", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                Text = "—"
            };
            var evidence = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Meiryo UI", 8.5F, FontStyle.Regular),
                TextAlign = ContentAlignment.TopLeft,
                AutoEllipsis = true,
                ForeColor = Color.FromArgb(48, 54, 62),
                Text = "代表根拠: —"
            };
            var meta = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Meiryo UI", 8F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                ForeColor = Color.DimGray,
                Text = "—"
            };

            layout.Controls.Add(level, 0, 0);
            layout.Controls.Add(assessment, 0, 1);
            layout.Controls.Add(evidence, 0, 2);
            layout.Controls.Add(meta, 0, 3);
            card.Controls.Add(layout);

            _womenCards[populationType] = new WomenCardControls
            {
                Card = card,
                Level = level,
                Assessment = assessment,
                Evidence = evidence,
                Meta = meta
            };
            return card;
        }

        private static DataGridView CreateWomenStatementsGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCellsExceptHeaders
            };
            grid.DefaultCellStyle.Font = new Font("Meiryo UI", 8.5F);
            grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Meiryo UI", 8.5F, FontStyle.Bold);

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Population",
                HeaderText = "対象",
                Width = 58
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ExpressionType",
                HeaderText = "情報区分",
                Width = 100
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Classification",
                HeaderText = "判定",
                Width = 132,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = new Font("Meiryo UI", 8.5F, FontStyle.Bold)
                }
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Assessment",
                HeaderText = "評価",
                Width = 175
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Evidence",
                HeaderText = "添付文書の原文",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                MinimumWidth = 220
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Source",
                HeaderText = "出典",
                Width = 120
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Review",
                HeaderText = "確認",
                Width = 82
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Level",
                Name = "Level",
                Visible = false
            });
            grid.CellFormatting += WomenStatementsGrid_CellFormatting;
            return grid;
        }

        private static void WomenStatementsGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            var grid = sender as DataGridView;
            if (grid == null || e.RowIndex < 0 || grid.Columns[e.ColumnIndex].DataPropertyName != "Classification")
            {
                return;
            }

            var levelValue = grid.Rows[e.RowIndex].Cells["Level"].Value;
            Color backColor;
            Color foreColor;
            GetWomenLevelColors(Convert.ToString(levelValue), out backColor, out foreColor);
            e.CellStyle.BackColor = backColor;
            e.CellStyle.ForeColor = foreColor;
        }

        private async Task LoadWomenInfoAsync(string packageInsertNo)
        {
            ClearWomenInfo("妊婦・授乳情報を読み込んでいます…");
            int sequence = ++_womenLoadSequence;

            try
            {
                if (!await EnsureWomenTabAvailabilityAsync())
                {
                    return;
                }

                var summaries = new List<WomenSummaryInfo>();
                var statements = CreateWomenStatementsTable();

                using (IDbConnection connection = CommonFunctions.GetDbConnection(true))
                {
                    var dbConnection = connection as DbConnection;
                    if (dbConnection != null)
                    {
                        await dbConnection.OpenAsync();
                    }
                    else
                    {
                        connection.Open();
                    }

                    using (IDbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            SELECT
                                s.population_type,
                                s.assessment_code,
                                s.assessment_text,
                                s.needs_review,
                                s.statement_count,
                                s.prepared_ym::text,
                                COALESCE(r.evidence_text, ''),
                                COALESCE(r.section_code, ''),
                                COALESCE(r.heading_path, '')
                            FROM public.sgml_women_summary s
                            LEFT JOIN public.sgml_women_statement r
                              ON r.statement_id = s.reason_statement_id
                             AND r.is_current
                            WHERE s.package_insert_no = @pkg
                            ORDER BY CASE s.population_type
                                WHEN 'PREGNANCY' THEN 1
                                WHEN 'LACTATION' THEN 2
                                ELSE 9
                            END;";
                        CommonFunctions.AddDbParameter(command, "@pkg", packageInsertNo);

                        var dbCommand = (DbCommand)command;
                        using (var reader = await dbCommand.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                summaries.Add(new WomenSummaryInfo
                                {
                                    PopulationType = ReadString(reader, 0),
                                    AssessmentCode = ReadString(reader, 1),
                                    AssessmentText = ReadString(reader, 2),
                                    NeedsReview = !reader.IsDBNull(3) && Convert.ToBoolean(reader.GetValue(3)),
                                    StatementCount = reader.IsDBNull(4) ? 0 : Convert.ToInt32(reader.GetValue(4)),
                                    PreparedYm = ReadString(reader, 5),
                                    EvidenceText = ReadString(reader, 6),
                                    SectionCode = ReadString(reader, 7),
                                    HeadingPath = ReadString(reader, 8)
                                });
                            }
                        }
                    }

                    using (IDbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            SELECT
                                population_type,
                                COALESCE(expression_type, ''),
                                COALESCE(classification_code, ''),
                                COALESCE(assessment_text, ''),
                                evidence_text,
                                COALESCE(section_code, ''),
                                COALESCE(heading_path, ''),
                                COALESCE(extraction_method, ''),
                                COALESCE(review_status, '')
                            FROM public.sgml_women_statement
                            WHERE is_current
                              AND package_insert_no = @pkg
                            ORDER BY
                                CASE population_type WHEN 'PREGNANCY' THEN 1 WHEN 'LACTATION' THEN 2 ELSE 9 END,
                                CASE display_level WHEN 'RED' THEN 1 WHEN 'YELLOW' THEN 2 WHEN 'BLUE' THEN 3 ELSE 4 END,
                                source_block_id,
                                statement_id;";
                        CommonFunctions.AddDbParameter(command, "@pkg", packageInsertNo);

                        var dbCommand = (DbCommand)command;
                        using (var reader = await dbCommand.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string classificationCode = ReadString(reader, 2);
                                string section = ReadString(reader, 5);
                                string heading = ReadString(reader, 6);
                                string extractionMethod = ReadString(reader, 7);
                                string reviewStatus = ReadString(reader, 8);
                                statements.Rows.Add(
                                    GetPopulationText(ReadString(reader, 0)),
                                    GetExpressionTypeText(ReadString(reader, 1)),
                                    string.IsNullOrEmpty(classificationCode)
                                        ? "背景情報"
                                        : GetAssessmentCodeText(classificationCode),
                                    ReadString(reader, 3),
                                    ReadString(reader, 4),
                                    BuildWomenSource(section, heading, extractionMethod),
                                    string.Equals(reviewStatus, "NEEDS_REVIEW", StringComparison.Ordinal)
                                        ? "要原文確認"
                                        : "",
                                    GetWomenLevel(classificationCode));
                            }
                        }
                    }
                }

                if (sequence != _womenLoadSequence ||
                    !string.Equals(_currentPackageInsertNo, packageInsertNo, StringComparison.Ordinal))
                {
                    return;
                }

                ApplyWomenInfo(summaries, statements);
            }
            catch (Exception ex)
            {
                if (sequence != _womenLoadSequence ||
                    !string.Equals(_currentPackageInsertNo, packageInsertNo, StringComparison.Ordinal))
                {
                    return;
                }

                if (IsAiTableMissing(ex))
                {
                    _womenTabAvailable = false;
                    UpdateAiTabOrder();
                    _ = CommonFunctions.AddLogAsync("妊婦・授乳テーブルが存在しないためタブを非表示にしました: " + ex.Message);
                    return;
                }

                ClearWomenInfo("妊婦・授乳情報を取得できません。DBの参照権限または公開テーブルを確認してください。");
                _ = CommonFunctions.AddLogAsync("妊婦・授乳情報読み込みエラー: " + ex.Message);
            }
        }

        private static bool IsAiTableMissing(Exception exception)
        {
            Exception current = exception;
            while (current != null)
            {
                var postgresException = current as PostgresException;
                if (postgresException != null &&
                    (postgresException.SqlState == PostgresErrorCodes.UndefinedTable ||
                     postgresException.SqlState == PostgresErrorCodes.InvalidSchemaName))
                {
                    return true;
                }
                current = current.InnerException;
            }
            return false;
        }

        private void ApplyWomenInfo(IList<WomenSummaryInfo> summaries, DataTable statements)
        {
            foreach (WomenSummaryInfo summary in summaries)
            {
                WomenCardControls controls;
                if (!_womenCards.TryGetValue(summary.PopulationType, out controls))
                {
                    continue;
                }

                string level = GetWomenLevel(summary.AssessmentCode);
                Color backColor;
                Color foreColor;
                GetWomenLevelColors(level, out backColor, out foreColor);
                controls.Level.BackColor = backColor;
                controls.Level.ForeColor = foreColor;
                controls.Level.Text = GetAssessmentCodeText(summary.AssessmentCode) +
                    (summary.NeedsReview ? "  ⚠ 要原文確認" : "");
                controls.Assessment.Text = string.IsNullOrWhiteSpace(summary.AssessmentText)
                    ? "評価文なし"
                    : summary.AssessmentText;
                controls.Evidence.Text = string.IsNullOrWhiteSpace(summary.EvidenceText)
                    ? "代表根拠: 該当する明示的な根拠文はありません。"
                    : "代表根拠: " + summary.EvidenceText;
                toolTip1.SetToolTip(controls.Evidence, controls.Evidence.Text);

                var sourceParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(summary.PreparedYm)) sourceParts.Add("添付文書 " + summary.PreparedYm);
                if (!string.IsNullOrWhiteSpace(summary.SectionCode)) sourceParts.Add("節 " + summary.SectionCode);
                if (!string.IsNullOrWhiteSpace(summary.HeadingPath)) sourceParts.Add(summary.HeadingPath);
                sourceParts.Add(summary.StatementCount + "件");
                controls.Meta.Text = string.Join(" / ", sourceParts);
            }

            _dgvWomenStatements.DataSource = statements;
            if (summaries.Count == 0)
            {
                _labelWomenStatus.Text = "この添付文書の妊婦・授乳情報は登録されていません。";
            }
            else
            {
                _labelWomenStatus.Text =
                    "AIを用いて添付文書の記載を整理した参考情報です。GRAY（無記載・明確な推奨なし）は安全を意味しません。";
            }
        }

        private void ClearWomenInfo(string status)
        {
            ++_womenLoadSequence;
            if (_labelWomenStatus != null)
            {
                _labelWomenStatus.Text = status;
            }

            foreach (WomenCardControls controls in _womenCards.Values)
            {
                controls.Level.Text = "データなし";
                controls.Level.BackColor = Color.FromArgb(232, 235, 239);
                controls.Level.ForeColor = Color.FromArgb(70, 76, 84);
                controls.Assessment.Text = "—";
                controls.Evidence.Text = "代表根拠: —";
                controls.Meta.Text = "—";
            }

            if (_dgvWomenStatements != null)
            {
                _dgvWomenStatements.DataSource = CreateWomenStatementsTable();
            }
        }

        private static DataTable CreateWomenStatementsTable()
        {
            var table = new DataTable();
            table.Columns.Add("Population", typeof(string));
            table.Columns.Add("ExpressionType", typeof(string));
            table.Columns.Add("Classification", typeof(string));
            table.Columns.Add("Assessment", typeof(string));
            table.Columns.Add("Evidence", typeof(string));
            table.Columns.Add("Source", typeof(string));
            table.Columns.Add("Review", typeof(string));
            table.Columns.Add("Level", typeof(string));
            return table;
        }

        private static string ReadString(IDataRecord record, int ordinal)
        {
            return record.IsDBNull(ordinal) ? "" : Convert.ToString(record.GetValue(ordinal));
        }

        private static string BuildWomenSource(string section, string heading, string extractionMethod)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(section)) parts.Add("節 " + section);
            if (!string.IsNullOrWhiteSpace(heading)) parts.Add(heading);
            if (!string.IsNullOrWhiteSpace(extractionMethod)) parts.Add(extractionMethod);
            return string.Join(" / ", parts);
        }

        private static string GetPopulationText(string populationType)
        {
            switch (populationType)
            {
                case "PREGNANCY": return "妊婦";
                case "LACTATION": return "授乳";
                default: return populationType;
            }
        }

        private static string GetAssessmentCodeText(string code)
        {
            switch (code)
            {
                case "CONTRAINDICATED": return "投与禁忌";
                case "AVOID": return "投与・授乳を避ける";
                case "STOP_BREASTFEEDING": return "授乳を中止";
                case "PREFER_AVOID": return "投与しないことが望ましい";
                case "BENEFIT_RISK": return "有益性が危険性を上回る場合のみ";
                case "CONSIDER_CONTINUE_OR_STOP": return "授乳の継続・中止を検討";
                case "UNCLASSIFIABLE": return "分類不能";
                case "ACCEPTABLE": return "明示的に使用可能";
                case "NO_EXPLICIT_RECOMMENDATION": return "明確な推奨記載なし";
                case "SECTION_ABSENT": return "関連章なし";
                default: return string.IsNullOrWhiteSpace(code) ? "背景情報" : code;
            }
        }

        private static string GetExpressionTypeText(string expressionType)
        {
            switch (expressionType)
            {
                case "RECOMMENDATION": return "推奨";
                case "MILK_TRANSFER": return "乳汁移行";
                case "INFANT_EFFECT": return "乳児への影響";
                case "ANIMAL_FINDING": return "動物所見";
                case "PLACENTAL_TRANSFER": return "胎盤通過";
                case "HUMAN_FINDING": return "ヒト所見";
                case "OTHER_INFORMATION": return "その他";
                default: return string.IsNullOrWhiteSpace(expressionType) ? "その他" : expressionType;
            }
        }

        private static string GetWomenLevel(string assessmentCode)
        {
            switch (assessmentCode)
            {
                case "CONTRAINDICATED":
                case "AVOID":
                case "STOP_BREASTFEEDING":
                    return "RED";
                case "PREFER_AVOID":
                case "BENEFIT_RISK":
                case "CONSIDER_CONTINUE_OR_STOP":
                case "UNCLASSIFIABLE":
                    return "YELLOW";
                case "ACCEPTABLE":
                    return "BLUE";
                default:
                    return "GRAY";
            }
        }

        private static void GetWomenLevelColors(string level, out Color backColor, out Color foreColor)
        {
            switch (level)
            {
                case "RED":
                    backColor = Color.FromArgb(255, 220, 220);
                    foreColor = Color.FromArgb(150, 25, 25);
                    break;
                case "YELLOW":
                    backColor = Color.FromArgb(255, 241, 184);
                    foreColor = Color.FromArgb(105, 76, 0);
                    break;
                case "BLUE":
                    backColor = Color.FromArgb(216, 235, 255);
                    foreColor = Color.FromArgb(25, 75, 135);
                    break;
                default:
                    backColor = Color.FromArgb(232, 235, 239);
                    foreColor = Color.FromArgb(70, 76, 84);
                    break;
            }
        }
    }
}
