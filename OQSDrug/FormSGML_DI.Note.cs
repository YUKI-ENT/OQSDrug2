using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OQSDrug
{
    public partial class FormSGML_DI
    {
        private readonly Dictionary<string, Button> _noteFilterButtons =
            new Dictionary<string, Button>(StringComparer.Ordinal);
        private TabPage _tabNote;
        private Label _labelNoteStatus;
        private DataGridView _dgvNotes;
        private RichTextBox _noteEvidence;
        private DataTable _noteTable;
        private string _noteFilter = "";
        private int _noteLoadSequence;
        private Task<bool> _noteAvailabilityTask;
        private bool _noteTabAvailable;

        private void InitializeNoteTab()
        {
            _tabNote = new TabPage
            {
                Name = "tabNote",
                Text = "代謝・薬理（AI）",
                Padding = new Padding(8),
                BackColor = Color.FromArgb(242, 245, 249),
                UseVisualStyleBackColor = false
            };

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = _tabNote.BackColor
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 66F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _labelNoteStatus = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(55, 63, 74),
                Text = "薬剤を選択すると代謝・薬理情報を表示します。"
            };
            root.Controls.Add(_labelNoteStatus, 0, 0);

            var filters = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = new Padding(0, 2, 0, 6)
            };
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82F));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            filters.Controls.Add(CreateNoteFilterButton("", "すべて", Color.FromArgb(65, 78, 96)), 0, 0);
            filters.Controls.Add(CreateNoteFilterButton("QT_EFFECT", "QT関連", Color.FromArgb(164, 62, 84)), 1, 0);
            filters.Controls.Add(CreateNoteFilterButton("ORGAN_IMPAIRMENT", "腎・肝機能", Color.FromArgb(166, 105, 28)), 2, 0);
            filters.Controls.Add(CreateNoteFilterButton("METABOLISM_TRANSPORT", "代謝・輸送体", Color.FromArgb(91, 67, 155)), 3, 0);
            filters.Controls.Add(CreateNoteFilterButton("EXCRETION_ELIMINATION", "排泄・透析", Color.FromArgb(35, 117, 120)), 4, 0);
            root.Controls.Add(filters, 0, 1);

            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterWidth = 6,
                BackColor = Color.Gainsboro,
                Panel1MinSize = 100,
                Panel2MinSize = 80
            };
            split.Resize += delegate
            {
                int desired = Math.Max(split.Panel1MinSize, (int)(split.Height * 0.58));
                int maximum = split.Height - split.Panel2MinSize - split.SplitterWidth;
                if (maximum >= split.Panel1MinSize)
                    split.SplitterDistance = Math.Min(desired, maximum);
            };
            _dgvNotes = CreateNoteGrid();
            _dgvNotes.SelectionChanged += NoteGrid_SelectionChanged;
            split.Panel1.Controls.Add(_dgvNotes);

            _noteEvidence = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(37, 43, 51),
                Font = new Font("Meiryo UI", 9F),
                DetectUrls = false,
                Text = "一覧からファクトを選択すると、添付文書の根拠原文を表示します。"
            };
            split.Panel2.Controls.Add(_noteEvidence);
            root.Controls.Add(split, 0, 2);

            _tabNote.Controls.Add(root);
        }

        private Button CreateNoteFilterButton(string noteType, string title, Color color)
        {
            var button = new Button
            {
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = color,
                ForeColor = Color.White,
                Font = new Font("Meiryo UI", 8.5F, FontStyle.Bold),
                Text = title + "\r\n0件",
                Tag = noteType,
                Margin = new Padding(3, 0, 3, 0),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            button.Click += NoteFilterButton_Click;
            _noteFilterButtons[noteType] = button;
            return button;
        }

        private static DataGridView CreateNoteGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCellsExceptHeaders
            };
            grid.DefaultCellStyle.Font = new Font("Meiryo UI", 8.5F);
            grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Meiryo UI", 8.5F, FontStyle.Bold);
            grid.Columns.Add(CreateNoteColumn("Category", "区分", 96));
            grid.Columns.Add(CreateNoteColumn("Relation", "内容", 145));
            grid.Columns.Add(CreateNoteColumn("Target", "対象", 95));
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "NoteText",
                HeaderText = "整理されたファクト",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                MinimumWidth = 250
            });
            grid.Columns.Add(CreateNoteColumn("Review", "検証", 86));
            grid.Columns.Add(CreateNoteColumn("Source", "出典", 115));

            foreach (string hidden in new[] { "NoteType", "Evidence", "Details", "Model", "PreparedYm", "NoteId" })
            {
                grid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    DataPropertyName = hidden,
                    Name = hidden,
                    Visible = false
                });
            }
            return grid;
        }

        private static DataGridViewTextBoxColumn CreateNoteColumn(string property, string header, int width)
        {
            return new DataGridViewTextBoxColumn
            {
                DataPropertyName = property,
                HeaderText = header,
                Width = width
            };
        }

        private async Task<bool> EnsureNoteTabAvailabilityAsync()
        {
            if (_noteAvailabilityTask == null)
            {
                _noteAvailabilityTask = CheckNoteTableAvailableAsync();
            }

            _noteTabAvailable = await _noteAvailabilityTask;
            UpdateAiTabOrder();
            return _noteTabAvailable;
        }

        private async Task<bool> CheckNoteTableAvailableAsync()
        {
            try
            {
                using (IDbConnection connection = CommonFunctions.GetDbConnection(true))
                {
                    var dbConnection = connection as DbConnection;
                    if (dbConnection != null) await dbConnection.OpenAsync();
                    else connection.Open();

                    using (IDbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT to_regclass('public.sgml_note') IS NOT NULL;";
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
                // 接続障害や権限不足はテーブル不存在と断定しない。
                _ = CommonFunctions.AddLogAsync("代謝・薬理テーブル存在確認エラー: " + ex.Message);
                return true;
            }
        }

        private void UpdateAiTabOrder()
        {
            // AI生成・整理情報は、既存の添付文書・相互作用・禁忌タブより後ろに固定する。
            if (!_womenTabAvailable && _tabWomen != null && tabMain.TabPages.Contains(_tabWomen))
                tabMain.TabPages.Remove(_tabWomen);
            if (!_noteTabAvailable && _tabNote != null && tabMain.TabPages.Contains(_tabNote))
                tabMain.TabPages.Remove(_tabNote);

            if (_womenTabAvailable && _tabWomen != null && !tabMain.TabPages.Contains(_tabWomen))
            {
                TabPage selectedTab = tabMain.SelectedTab;
                bool noteIsVisible = _tabNote != null && tabMain.TabPages.Contains(_tabNote);
                tabMain.TabPages.Add(_tabWomen);
                if (noteIsVisible)
                {
                    tabMain.TabPages.Remove(_tabNote);
                    tabMain.TabPages.Add(_tabNote);
                }
                if (selectedTab != null && tabMain.TabPages.Contains(selectedTab))
                    tabMain.SelectedTab = selectedTab;
            }
            if (_noteTabAvailable && _tabNote != null && !tabMain.TabPages.Contains(_tabNote))
                tabMain.TabPages.Add(_tabNote);

            if (_tabWomen != null && _tabNote != null &&
                tabMain.TabPages.Contains(_tabWomen) && tabMain.TabPages.Contains(_tabNote) &&
                tabMain.TabPages.IndexOf(_tabWomen) > tabMain.TabPages.IndexOf(_tabNote))
            {
                TabPage selectedTab = tabMain.SelectedTab;
                tabMain.TabPages.Remove(_tabNote);
                tabMain.TabPages.Add(_tabNote);
                if (selectedTab != null && tabMain.TabPages.Contains(selectedTab))
                    tabMain.SelectedTab = selectedTab;
            }
        }

        private async Task LoadNoteInfoAsync(string packageInsertNo)
        {
            if (!string.Equals(_currentPackageInsertNo, packageInsertNo, StringComparison.Ordinal)) return;
            ClearNoteInfo("代謝・薬理情報を読み込んでいます…");
            int sequence = ++_noteLoadSequence;

            try
            {
                if (!await EnsureNoteTabAvailabilityAsync()) return;

                DataTable table = CreateNoteTable();
                using (IDbConnection connection = CommonFunctions.GetDbConnection(true))
                {
                    var dbConnection = connection as DbConnection;
                    if (dbConnection != null) await dbConnection.OpenAsync();
                    else connection.Open();

                    using (IDbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            SELECT
                                note_id,
                                note_type,
                                relation_type,
                                COALESCE(target_code, ''),
                                COALESCE(target_name, ''),
                                note_text,
                                COALESCE(details_json::text, ''),
                                evidence_text,
                                COALESCE(section_code, ''),
                                COALESCE(heading_path, ''),
                                review_status,
                                COALESCE(model_name, ''),
                                COALESCE(prepared_ym, '')
                            FROM public.sgml_note
                            WHERE is_current
                              AND package_insert_no = @pkg
                            ORDER BY
                                CASE note_type
                                    WHEN 'QT_EFFECT' THEN 1
                                    WHEN 'ORGAN_IMPAIRMENT' THEN 2
                                    WHEN 'METABOLISM_TRANSPORT' THEN 3
                                    WHEN 'EXCRETION_ELIMINATION' THEN 4
                                    ELSE 9
                                END,
                                relation_type,
                                target_code NULLS LAST,
                                note_id;";
                        CommonFunctions.AddDbParameter(command, "@pkg", packageInsertNo);

                        using (var reader = await ((DbCommand)command).ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string noteType = ReadString(reader, 1);
                                string targetCode = ReadString(reader, 3);
                                string targetName = ReadString(reader, 4);
                                string section = ReadString(reader, 8);
                                string heading = ReadString(reader, 9);
                                table.Rows.Add(
                                    ReadString(reader, 0), noteType, GetNoteTypeText(noteType),
                                    GetNoteRelationText(ReadString(reader, 2)),
                                    BuildNoteTarget(targetCode, targetName),
                                    ReadString(reader, 5), ReadString(reader, 7), ReadString(reader, 6),
                                    GetNoteReviewText(ReadString(reader, 10)),
                                    BuildNoteSource(section, heading), ReadString(reader, 11), ReadString(reader, 12));
                            }
                        }
                    }
                }

                if (sequence != _noteLoadSequence ||
                    !string.Equals(_currentPackageInsertNo, packageInsertNo, StringComparison.Ordinal)) return;

                ApplyNoteInfo(table);
            }
            catch (Exception ex)
            {
                if (sequence != _noteLoadSequence ||
                    !string.Equals(_currentPackageInsertNo, packageInsertNo, StringComparison.Ordinal)) return;

                if (IsAiTableMissing(ex))
                {
                    _noteTabAvailable = false;
                    UpdateAiTabOrder();
                    _ = CommonFunctions.AddLogAsync("代謝・薬理テーブルが存在しないためタブを非表示にしました: " + ex.Message);
                    return;
                }

                ClearNoteInfo("代謝・薬理情報を取得できません。DBの参照権限または公開テーブルを確認してください。");
                _ = CommonFunctions.AddLogAsync("代謝・薬理情報読み込みエラー: " + ex.Message);
            }
        }

        private void ApplyNoteInfo(DataTable table)
        {
            _noteTable = table;
            ApplyNoteFilter();
            UpdateNoteFilterCounts();
            _labelNoteStatus.Text = table.Rows.Count == 0
                ? "この添付文書の代謝・薬理情報は登録されていません。情報なしを安全とは解釈できません。"
                : "AI抽出・原文照合済みです。区分色は安全度ではありません。投与可否・用量調節は原文と患者背景を併せて判断してください。";
        }

        private void ClearNoteInfo(string status)
        {
            ++_noteLoadSequence;
            _noteTable = CreateNoteTable();
            if (_labelNoteStatus != null) _labelNoteStatus.Text = status;
            if (_dgvNotes != null) _dgvNotes.DataSource = _noteTable.DefaultView;
            if (_noteEvidence != null)
                _noteEvidence.Text = "一覧からファクトを選択すると、添付文書の根拠原文を表示します。";
            UpdateNoteFilterCounts();
        }

        private static DataTable CreateNoteTable()
        {
            var table = new DataTable();
            foreach (string name in new[]
            {
                "NoteId", "NoteType", "Category", "Relation", "Target", "NoteText",
                "Evidence", "Details", "Review", "Source", "Model", "PreparedYm"
            }) table.Columns.Add(name, typeof(string));
            return table;
        }

        private void NoteFilterButton_Click(object sender, EventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;
            _noteFilter = Convert.ToString(button.Tag);
            ApplyNoteFilter();
        }

        private void NoteGrid_SelectionChanged(object sender, EventArgs e)
        {
            ShowSelectedNoteEvidence();
        }

        private void ApplyNoteFilter()
        {
            if (_noteTable == null || _dgvNotes == null) return;
            DataView view = _noteTable.DefaultView;
            view.RowFilter = string.IsNullOrEmpty(_noteFilter)
                ? ""
                : "NoteType = '" + _noteFilter.Replace("'", "''") + "'";
            _dgvNotes.DataSource = view;
            UpdateNoteFilterAppearance();
            ShowSelectedNoteEvidence();
        }

        private void UpdateNoteFilterCounts()
        {
            if (_noteFilterButtons.Count == 0) return;
            SetNoteFilterText("", "すべて", _noteTable == null ? 0 : _noteTable.Rows.Count);
            SetNoteFilterText("QT_EFFECT", "QT関連", CountNotes("QT_EFFECT"));
            SetNoteFilterText("ORGAN_IMPAIRMENT", "腎・肝機能", CountNotes("ORGAN_IMPAIRMENT"));
            SetNoteFilterText("METABOLISM_TRANSPORT", "代謝・輸送体", CountNotes("METABOLISM_TRANSPORT"));
            SetNoteFilterText("EXCRETION_ELIMINATION", "排泄・透析", CountNotes("EXCRETION_ELIMINATION"));
        }

        private int CountNotes(string noteType)
        {
            return _noteTable == null ? 0 : _noteTable.Select("NoteType = '" + noteType + "'").Length;
        }

        private void SetNoteFilterText(string noteType, string title, int count)
        {
            Button button;
            if (_noteFilterButtons.TryGetValue(noteType, out button))
                button.Text = title + "\r\n" + count + "件";
        }

        private void UpdateNoteFilterAppearance()
        {
            foreach (KeyValuePair<string, Button> item in _noteFilterButtons)
            {
                bool selected = string.Equals(item.Key, _noteFilter, StringComparison.Ordinal);
                item.Value.FlatAppearance.BorderSize = selected ? 3 : 0;
                item.Value.FlatAppearance.BorderColor = Color.White;
            }
        }

        private void ShowSelectedNoteEvidence()
        {
            if (_noteEvidence == null || _dgvNotes == null || _dgvNotes.CurrentRow == null)
            {
                if (_noteEvidence != null) _noteEvidence.Text = "該当するファクトはありません。";
                return;
            }

            var rowView = _dgvNotes.CurrentRow.DataBoundItem as DataRowView;
            if (rowView == null) return;
            DataRow row = rowView.Row;
            var text = new StringBuilder();
            text.AppendLine("【整理されたファクト】").AppendLine(Convert.ToString(row["NoteText"]));
            text.AppendLine().AppendLine("【添付文書の根拠原文】").AppendLine(Convert.ToString(row["Evidence"]));
            text.AppendLine().Append("出典: ").Append(Convert.ToString(row["Source"]));
            if (!string.IsNullOrWhiteSpace(Convert.ToString(row["PreparedYm"])))
                text.Append(" / 添付文書 ").Append(Convert.ToString(row["PreparedYm"]));
            text.Append(" / note_id ").Append(Convert.ToString(row["NoteId"]));
            text.AppendLine().Append("検証: ").Append(Convert.ToString(row["Review"]));
            if (!string.IsNullOrWhiteSpace(Convert.ToString(row["Model"])))
                text.Append(" / ").Append(Convert.ToString(row["Model"]));
            if (!string.IsNullOrWhiteSpace(Convert.ToString(row["Details"])))
                text.AppendLine().Append("構造化詳細: ").Append(Convert.ToString(row["Details"]));
            _noteEvidence.Text = text.ToString();
        }

        private static string GetNoteTypeText(string noteType)
        {
            switch (noteType)
            {
                case "QT_EFFECT": return "QT関連";
                case "ORGAN_IMPAIRMENT": return "腎・肝機能";
                case "METABOLISM_TRANSPORT": return "代謝・輸送体";
                case "EXCRETION_ELIMINATION": return "排泄・透析";
                default: return noteType;
            }
        }

        private static string GetNoteReviewText(string reviewStatus)
        {
            switch (reviewStatus)
            {
                case "HUMAN_REVIEWED": return "人手確認済";
                case "AUTO_VALIDATED": return "機械検証済";
                default: return reviewStatus;
            }
        }

        private static string BuildNoteTarget(string targetCode, string targetName)
        {
            if (string.IsNullOrWhiteSpace(targetCode)) return targetName;
            if (string.IsNullOrWhiteSpace(targetName) || string.Equals(targetCode, targetName, StringComparison.OrdinalIgnoreCase))
                return targetCode;
            return targetName + " (" + targetCode + ")";
        }

        private static string BuildNoteSource(string section, string heading)
        {
            if (string.IsNullOrWhiteSpace(section)) return heading;
            if (string.IsNullOrWhiteSpace(heading)) return "節 " + section;
            return "節 " + section + " / " + heading;
        }

        private static string GetNoteRelationText(string relation)
        {
            var names = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "URINARY_EXCRETION", "尿中排泄" }, { "URINARY_RECOVERY", "尿中回収" },
                { "FECAL_RECOVERY", "糞中回収" }, { "BILIARY_EXCRETION", "胆汁中排泄" },
                { "RENAL_CLEARANCE", "腎クリアランス" }, { "DIALYZABLE", "透析で除去される" },
                { "NOT_DIALYZABLE", "透析で除去されにくい" }, { "OTHER_ELIMINATION", "その他の排泄" },
                { "METABOLIZED_BY", "代謝酵素" }, { "METABOLISM_PATHWAY", "代謝経路" },
                { "HEPATIC_METABOLISM", "肝代謝" },
                { "CLEARANCE_DEPENDS_ON_HEPATIC_BLOOD_FLOW", "肝血流依存性" },
                { "SUBSTRATE_OF", "基質" }, { "INHIBITS", "阻害" }, { "INDUCES", "誘導" },
                { "NOT_METABOLIZED_BY", "代謝されない" }, { "NOT_SUBSTRATE_OF", "基質ではない" },
                { "NOT_INHIBITS", "阻害しない" }, { "NOT_INDUCES", "誘導しない" },
                { "EXPOSURE_INCREASED_BY_INHIBITION", "阻害により曝露増加" },
                { "EXPOSURE_DECREASED_BY_INDUCTION", "誘導により曝露低下" },
                { "EXPOSURE_INCREASES_WITH_RENAL_IMPAIRMENT", "腎機能低下で曝露増加" },
                { "EXPOSURE_DECREASES_WITH_RENAL_IMPAIRMENT", "腎機能低下で曝露低下" },
                { "EXPOSURE_INCREASES_WITH_HEPATIC_IMPAIRMENT", "肝機能低下で曝露増加" },
                { "EXPOSURE_DECREASES_WITH_HEPATIC_IMPAIRMENT", "肝機能低下で曝露低下" },
                { "HALF_LIFE_CHANGES_WITH_IMPAIRMENT", "機能障害で半減期変化" },
                { "CLEARANCE_CHANGES_WITH_IMPAIRMENT", "機能障害でクリアランス変化" },
                { "ELIMINATION_DELAYED_WITH_RENAL_IMPAIRMENT", "腎機能低下で排泄遅延" },
                { "METABOLISM_DELAYED_WITH_HEPATIC_IMPAIRMENT", "肝機能低下で代謝遅延" },
                { "PROTEIN_BINDING_DECREASES_WITH_HEPATIC_IMPAIRMENT", "肝機能低下で蛋白結合低下" },
                { "NO_MEANINGFUL_PK_CHANGE", "臨床的に意味のあるPK変化なし" },
                { "QT_PROLONGATION", "QT/QTc延長" }, { "TORSADES_DE_POINTES", "Torsade de pointes" }
            };
            string text;
            return names.TryGetValue(relation, out text) ? text : relation;
        }
    }
}
