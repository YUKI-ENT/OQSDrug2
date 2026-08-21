using System;
using System.Drawing;
using System.Windows.Forms;

namespace OQSDrug
{
    public partial class Form2
    {
        private TabPage _tabPagePmdaDocument;
        private TextBox _textBoxPmdaSearchList;

        private void InitializePmdaDocumentSettingsTab()
        {
            _tabPagePmdaDocument = new TabPage
            {
                Name = "tabPagePmdaDocument",
                Text = "PMDA添付文書",
                Padding = new Padding(12),
                UseVisualStyleBackColor = true
            };

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));

            root.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Meiryo UI", 10F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "検索リスト"
            }, 0, 0);

            root.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(65, 72, 82),
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "XML添付文書の表示時に全項目を自動検索します。1行に1項目、複数語は OR で区切ってください。ヒットした項目だけが要確認として表示されます。"
            }, 0, 1);

            _textBoxPmdaSearchList = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                AcceptsReturn = true,
                AcceptsTab = false,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = new Font("Meiryo UI", 10F)
            };
            root.Controls.Add(_textBoxPmdaSearchList, 0, 2);

            var resetPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(0, 5, 0, 0)
            };
            var resetButton = new Button
            {
                AutoSize = true,
                Text = "既定のリストに戻す"
            };
            resetButton.Click += delegate
            {
                _textBoxPmdaSearchList.Text = PmdaSearchListSettings.GetDefaultEditorText();
            };
            resetPanel.Controls.Add(resetButton);
            root.Controls.Add(resetPanel, 0, 3);

            _tabPagePmdaDocument.Controls.Add(root);
            tabControl1.TabPages.Add(_tabPagePmdaDocument);
        }

        private void LoadPmdaDocumentSettings()
        {
            if (_textBoxPmdaSearchList != null)
                _textBoxPmdaSearchList.Text = PmdaSearchListSettings.GetEditorText();
        }

        private void SavePmdaDocumentSettings()
        {
            if (_textBoxPmdaSearchList != null)
            {
                Properties.Settings.Default.PMDASearchList =
                    PmdaSearchListSettings.NormalizeForStorage(_textBoxPmdaSearchList.Text);
            }
        }
    }
}
