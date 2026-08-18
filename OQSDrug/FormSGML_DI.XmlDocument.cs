using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace OQSDrug
{
    public partial class FormSGML_DI
    {
        // 2026-08-18 00:00:00 より後に更新された行を、原文保存形式として扱う。
        private static readonly DateTime RawXmlFormatThreshold = new DateTime(2026, 8, 18);

        private TabPage _tabXmlDocument;
        private WebBrowser _xmlDocumentBrowser;
        private RichTextBox _xmlSourceText;
        private ListBox _xmlSectionList;
        private ComboBox _xmlSearchCombo;
        private Button _xmlSearchButton;
        private Button _xmlSearchPreviousButton;
        private Button _xmlSearchNextButton;
        private Label _xmlSearchStatus;
        private XDocument _xmlViewerDocument;
        private Dictionary<string, string> _xmlViewerBrandNames;
        private List<XmlViewerSection> _xmlViewerSections = new List<XmlViewerSection>();
        private DateTime? _xmlViewerUpdatedAt;
        private string _xmlViewerSearchKeyword = string.Empty;
        private int _xmlViewerSearchHitCount;
        private int _xmlViewerSearchHitIndex = -1;
        private string _pendingXmlAnchor;

        private sealed class XmlViewerSection
        {
            public string Title { get; set; }
            public string Anchor { get; set; }
            public XElement Element { get; set; }

            public override string ToString()
            {
                return Title ?? string.Empty;
            }
        }

        private sealed class XmlSearchRenderState
        {
            public string[] Keywords { get; set; }
            public int HitCount { get; set; }
        }

        private void InitializeXmlDocumentTab()
        {
            _tabXmlDocument = new TabPage
            {
                Name = "tabXmlDocument",
                Text = "添付文書(XML)",
                Padding = new Padding(3),
                UseVisualStyleBackColor = true
            };

            var viewTabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Meiryo UI", 9F)
            };

            var renderedPage = new TabPage
            {
                Text = "文書表示",
                Padding = new Padding(0),
                UseVisualStyleBackColor = true
            };
            var renderedRoot = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            renderedRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            renderedRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var searchPanel = CreateXmlSearchPanel();
            renderedRoot.Controls.Add(searchPanel, 0, 0);

            var documentSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                FixedPanel = FixedPanel.Panel1,
                SplitterWidth = 6,
                BackColor = Color.Gainsboro
            };
            documentSplit.Resize += delegate
            {
                const int navigationMinimum = 170;
                const int documentMinimum = 300;
                int maximum = documentSplit.Width - documentMinimum - documentSplit.SplitterWidth;
                if (maximum >= navigationMinimum)
                {
                    int desired = Math.Max(navigationMinimum, 210);
                    documentSplit.SplitterDistance = Math.Min(desired, maximum);
                }
            };
            _xmlSectionList = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Meiryo UI", 10F),
                HorizontalScrollbar = true
            };
            _xmlSectionList.SelectedIndexChanged += XmlSectionList_SelectedIndexChanged;
            documentSplit.Panel1.Controls.Add(_xmlSectionList);

            _xmlDocumentBrowser = new WebBrowser
            {
                Dock = DockStyle.Fill,
                AllowWebBrowserDrop = false,
                IsWebBrowserContextMenuEnabled = true,
                ScriptErrorsSuppressed = true,
                WebBrowserShortcutsEnabled = true
            };
            _xmlDocumentBrowser.DocumentCompleted += XmlDocumentBrowser_DocumentCompleted;
            documentSplit.Panel2.Controls.Add(_xmlDocumentBrowser);
            renderedRoot.Controls.Add(documentSplit, 0, 1);
            renderedPage.Controls.Add(renderedRoot);

            var sourcePage = new TabPage
            {
                Text = "XML原文",
                Padding = new Padding(3),
                UseVisualStyleBackColor = true
            };
            _xmlSourceText = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                DetectUrls = false,
                WordWrap = false,
                BackColor = Color.White,
                Font = new Font("Consolas", 9F)
            };
            sourcePage.Controls.Add(_xmlSourceText);

            viewTabs.TabPages.Add(renderedPage);
            viewTabs.TabPages.Add(sourcePage);
            _tabXmlDocument.Controls.Add(viewTabs);
            tabMain.TabPages.Add(_tabXmlDocument);

            ClearXmlDocumentViewer("薬剤を選択するとXML形式の添付文書を表示します。");
            SetXmlDocumentMode(false);
        }

        private Control CreateXmlSearchPanel()
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(5, 4, 5, 2),
                BackColor = SystemColors.ControlLight
            };

            panel.Controls.Add(new Label
            {
                AutoSize = true,
                Margin = new Padding(0, 4, 4, 0),
                Text = "文書内検索（OR可）"
            });

            _xmlSearchCombo = new ComboBox
            {
                Width = 180,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems
            };
            ReloadXmlSearchItems();
            _xmlSearchCombo.KeyDown += XmlSearchCombo_KeyDown;
            panel.Controls.Add(_xmlSearchCombo);

            _xmlSearchButton = CreateXmlSearchButton("検索", 58, delegate { RunXmlDocumentSearch(); });
            _xmlSearchPreviousButton = CreateXmlSearchButton("前へ", 52, delegate { MoveXmlSearchHit(-1); });
            _xmlSearchNextButton = CreateXmlSearchButton("次へ", 52, delegate { MoveXmlSearchHit(1); });
            panel.Controls.Add(_xmlSearchButton);
            panel.Controls.Add(_xmlSearchPreviousButton);
            panel.Controls.Add(_xmlSearchNextButton);

            _xmlSearchStatus = new Label
            {
                AutoSize = true,
                Margin = new Padding(8, 4, 0, 0),
                ForeColor = Color.FromArgb(65, 72, 82)
            };
            panel.Controls.Add(_xmlSearchStatus);
            return panel;
        }

        private void ReloadXmlSearchItems()
        {
            if (_xmlSearchCombo == null) return;

            string currentText = _xmlSearchCombo.Text;
            _xmlSearchCombo.BeginUpdate();
            _xmlSearchCombo.Items.Clear();
            _xmlSearchCombo.Items.AddRange(PmdaSearchListSettings.GetItems());
            _xmlSearchCombo.EndUpdate();
            _xmlSearchCombo.Text = currentText;
        }

        private static Button CreateXmlSearchButton(string text, int width, EventHandler click)
        {
            var button = new Button
            {
                Text = text,
                Width = width,
                Height = 25,
                Margin = new Padding(3, 0, 0, 0)
            };
            button.Click += click;
            return button;
        }

        private void ClearXmlDocumentViewer(string message)
        {
            _xmlViewerDocument = null;
            _xmlViewerBrandNames = null;
            _xmlViewerSections.Clear();
            _xmlViewerUpdatedAt = null;
            _xmlViewerSearchKeyword = string.Empty;
            _xmlViewerSearchHitCount = 0;
            _xmlViewerSearchHitIndex = -1;
            _pendingXmlAnchor = null;

            if (_xmlSectionList != null)
                _xmlSectionList.Items.Clear();
            if (_xmlSearchStatus != null)
                _xmlSearchStatus.Text = string.Empty;
            if (_xmlSourceText != null)
                _xmlSourceText.Text = string.Empty;

            if (_xmlDocumentBrowser != null)
                _xmlDocumentBrowser.DocumentText = BuildViewerMessageHtml(message, false);
        }

        private void DisplayXmlDocument(string xmlText, DateTime? updatedAt)
        {
            if (_xmlSourceText == null || _xmlDocumentBrowser == null) return;

            bool useXmlViewer = updatedAt.HasValue && updatedAt.Value > RawXmlFormatThreshold;
            SetXmlDocumentMode(useXmlViewer);

            if (!useXmlViewer)
            {
                ClearXmlDocumentViewer(
                    "このデータは旧形式です。従来の「添付文書」タブを使用してください。" +
                    "（XML表示の対象: updated_at が 2026-08-18 00:00:00 より後）");
                return;
            }

            if (string.IsNullOrWhiteSpace(xmlText))
            {
                ClearXmlDocumentViewer("doc_xml が空のため、XML形式の添付文書を表示できません。");
                return;
            }

            ReloadXmlSearchItems();
            _xmlSourceText.Text = xmlText;

            try
            {
                _xmlViewerDocument = ParseXmlDocument(xmlText);
                _xmlViewerBrandNames = BuildApprovalBrandNameMap(_xmlViewerDocument);
                _xmlViewerUpdatedAt = updatedAt;
                _xmlViewerSections = BuildXmlViewerSections(_xmlViewerDocument);
                PopulateXmlSectionList();
                RenderCurrentXmlDocument();
            }
            catch (Exception ex)
            {
                _xmlDocumentBrowser.DocumentText = BuildViewerMessageHtml(
                    "XMLを解析できませんでした。XML原文タブで内容を確認してください。\r\n" + ex.Message,
                    true);
            }
        }

        private void SetXmlDocumentMode(bool useXmlViewer)
        {
            if (tabMain == null || tabSections == null || _tabXmlDocument == null) return;

            TabPage selected = tabMain.SelectedTab;
            if (useXmlViewer)
            {
                if (tabMain.TabPages.Contains(tabSections)) tabMain.TabPages.Remove(tabSections);
                if (!tabMain.TabPages.Contains(_tabXmlDocument)) tabMain.TabPages.Insert(0, _tabXmlDocument);
                panelDocSearch.Visible = false;
                if (selected == tabSections || selected == null) tabMain.SelectedTab = _tabXmlDocument;
            }
            else
            {
                if (tabMain.TabPages.Contains(_tabXmlDocument)) tabMain.TabPages.Remove(_tabXmlDocument);
                if (!tabMain.TabPages.Contains(tabSections)) tabMain.TabPages.Insert(0, tabSections);
                panelDocSearch.Visible = true;
                if (selected == _tabXmlDocument || selected == null) tabMain.SelectedTab = tabSections;
            }
        }

        private List<XmlViewerSection> BuildXmlViewerSections(XDocument document)
        {
            var sections = new List<XmlViewerSection>
            {
                new XmlViewerSection { Title = "概要", Anchor = "xml-summary" }
            };
            if (document.Root == null) return sections;

            var added = new HashSet<XElement>();
            foreach (SectionDef definition in SectionDefs)
            {
                XElement element = document.Root.DescendantsAndSelf()
                    .FirstOrDefault(e => definition.Names.Contains(e.Name.LocalName));
                if (element == null || !added.Add(element)) continue;

                sections.Add(new XmlViewerSection
                {
                    Title = definition.Title,
                    Anchor = "xml-section-" + sections.Count,
                    Element = element
                });
            }
            return sections;
        }

        private void PopulateXmlSectionList()
        {
            _xmlSectionList.BeginUpdate();
            _xmlSectionList.Items.Clear();
            foreach (XmlViewerSection section in _xmlViewerSections)
                _xmlSectionList.Items.Add(section);
            _xmlSectionList.EndUpdate();

            if (_xmlSectionList.Items.Count > 0)
                _xmlSectionList.SelectedIndex = 0;
        }

        private void RenderCurrentXmlDocument()
        {
            if (_xmlViewerDocument == null || _xmlViewerBrandNames == null || !_xmlViewerUpdatedAt.HasValue)
                return;

            var searchState = string.IsNullOrWhiteSpace(_xmlViewerSearchKeyword)
                ? null
                : new XmlSearchRenderState { Keywords = ParseXmlSearchKeywords(_xmlViewerSearchKeyword) };

            _xmlDocumentBrowser.DocumentText = BuildRenderedDocumentHtml(
                _xmlViewerDocument,
                _xmlViewerBrandNames,
                _xmlViewerUpdatedAt.Value,
                _xmlViewerSections,
                searchState);

            _xmlViewerSearchHitCount = searchState == null ? 0 : searchState.HitCount;
            if (_xmlViewerSearchHitCount == 0)
                _xmlViewerSearchHitIndex = -1;
            UpdateXmlSearchStatus();
        }

        private static string[] ParseXmlSearchKeywords(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText)) return new string[0];

            return Regex.Split(searchText.Trim(), @"\s+OR\s+", RegexOptions.IgnoreCase)
                .Select(keyword => keyword.Trim())
                .Where(keyword => keyword.Length > 0)
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderByDescending(keyword => keyword.Length)
                .ToArray();
        }

        private void RunXmlDocumentSearch()
        {
            if (_xmlViewerDocument == null) return;

            string keyword = (_xmlSearchCombo.Text ?? string.Empty).Trim();
            _xmlViewerSearchKeyword = keyword;
            _xmlViewerSearchHitIndex = string.IsNullOrEmpty(keyword) ? -1 : 0;
            _pendingXmlAnchor = string.IsNullOrEmpty(keyword) ? "xml-summary" : "xml-hit-0";
            RenderCurrentXmlDocument();
        }

        private void MoveXmlSearchHit(int direction)
        {
            if (_xmlViewerSearchHitCount <= 0)
            {
                RunXmlDocumentSearch();
                return;
            }

            _xmlViewerSearchHitIndex += direction;
            if (_xmlViewerSearchHitIndex >= _xmlViewerSearchHitCount)
                _xmlViewerSearchHitIndex = 0;
            else if (_xmlViewerSearchHitIndex < 0)
                _xmlViewerSearchHitIndex = _xmlViewerSearchHitCount - 1;

            UpdateXmlSearchStatus();
            ScrollToXmlAnchor("xml-hit-" + _xmlViewerSearchHitIndex);
        }

        private void UpdateXmlSearchStatus()
        {
            if (_xmlSearchStatus == null) return;

            if (string.IsNullOrWhiteSpace(_xmlViewerSearchKeyword))
                _xmlSearchStatus.Text = string.Empty;
            else if (_xmlViewerSearchHitCount == 0)
                _xmlSearchStatus.Text = "0件";
            else
                _xmlSearchStatus.Text = string.Format(
                    "{0} / {1}件", _xmlViewerSearchHitIndex + 1, _xmlViewerSearchHitCount);
        }

        private void XmlSearchCombo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            RunXmlDocumentSearch();
            e.SuppressKeyPress = true;
        }

        private void XmlSectionList_SelectedIndexChanged(object sender, EventArgs e)
        {
            var section = _xmlSectionList.SelectedItem as XmlViewerSection;
            if (section != null) ScrollToXmlAnchor(section.Anchor);
        }

        private void XmlDocumentBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_pendingXmlAnchor))
            {
                string anchor = _pendingXmlAnchor;
                _pendingXmlAnchor = null;
                ScrollToXmlAnchor(anchor);
            }
        }

        private void ScrollToXmlAnchor(string anchor)
        {
            if (string.IsNullOrEmpty(anchor) || _xmlDocumentBrowser.Document == null)
            {
                _pendingXmlAnchor = anchor;
                return;
            }

            HtmlElement element = _xmlDocumentBrowser.Document.GetElementById(anchor);
            if (element == null)
            {
                _pendingXmlAnchor = anchor;
                return;
            }

            element.ScrollIntoView(true);
            _xmlDocumentBrowser.Focus();
        }

        private static XDocument ParseXmlDocument(string xmlText)
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreComments = false,
                IgnoreProcessingInstructions = false,
                IgnoreWhitespace = false
            };

            using (var textReader = new StringReader(xmlText))
            using (XmlReader reader = XmlReader.Create(textReader, settings))
            {
                return XDocument.Load(reader, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
            }
        }

        private static Dictionary<string, string> BuildApprovalBrandNameMap(XDocument document)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            if (document.Root == null) return result;

            foreach (XElement detail in document.Descendants()
                .Where(e => e.Name.LocalName == "DetailBrandName"))
            {
                XAttribute idAttribute = detail.Attributes()
                    .FirstOrDefault(a => a.Name.LocalName == "id");
                if (idAttribute == null || string.IsNullOrWhiteSpace(idAttribute.Value)) continue;

                XElement approvalName = detail.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "ApprovalBrandName");
                if (approvalName == null) continue;

                XElement japanese = approvalName.DescendantsAndSelf()
                    .FirstOrDefault(e => e.Name.LocalName == "Lang" &&
                        string.Equals((string)e.Attribute(XNamespace.Xml + "lang"), "ja", StringComparison.OrdinalIgnoreCase));
                string name = NormalizeViewerText((japanese ?? approvalName).Value);
                if (!string.IsNullOrWhiteSpace(name))
                    result[idAttribute.Value] = name;
            }

            return result;
        }

        private string BuildRenderedDocumentHtml(
            XDocument document,
            IDictionary<string, string> brandNames,
            DateTime updatedAt,
            IList<XmlViewerSection> sections,
            XmlSearchRenderState searchState)
        {
            var body = new StringBuilder(32768);
            string title = string.IsNullOrWhiteSpace(_currentDrugName) ? "添付文書" : _currentDrugName;

            body.Append("<div class='header'><h1>").Append(HtmlEncode(title)).Append("</h1>");
            body.Append("<div class='meta'>XML原文からアプリ内で生成 / 更新: ")
                .Append(FormatUpdatedAtJst(updatedAt))
                .Append("</div></div>");

            if (document.Root != null)
            {
                var sectionMap = sections
                    .Where(s => s.Element != null)
                    .ToDictionary(s => s.Element, s => s);

                AppendXmlSummary(body, brandNames, sections);

                // 既知の章だけを抜き出さず、ルート全体を走査して未知要素の本文も保持する。
                RenderXmlNode(document.Root, body, brandNames, sectionMap, searchState);
            }

            return WrapViewerHtml(title, body.ToString());
        }

        private static string FormatUpdatedAtJst(DateTime updatedAt)
        {
            DateTime utc = updatedAt.Kind == DateTimeKind.Local
                ? updatedAt.ToUniversalTime()
                : DateTime.SpecifyKind(updatedAt, DateTimeKind.Utc);

            try
            {
                TimeZoneInfo jst = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(utc, jst).ToString("yyyy-MM-dd HH:mm:ss 'JST'");
            }
            catch (TimeZoneNotFoundException)
            {
                return utc.AddHours(9).ToString("yyyy-MM-dd HH:mm:ss 'JST'");
            }
            catch (InvalidTimeZoneException)
            {
                return utc.AddHours(9).ToString("yyyy-MM-dd HH:mm:ss 'JST'");
            }
        }

        private static void RenderXmlNode(
            XNode node,
            StringBuilder html,
            IDictionary<string, string> brandNames,
            IDictionary<XElement, XmlViewerSection> sectionMap,
            XmlSearchRenderState searchState)
        {
            var text = node as XText;
            if (text != null)
            {
                string value = NormalizeViewerText(text.Value);
                if (!string.IsNullOrEmpty(value)) AppendSearchableText(html, value, searchState);
                return;
            }

            var instruction = node as XProcessingInstruction;
            if (instruction != null)
            {
                if (string.Equals(instruction.Target, "enter", StringComparison.OrdinalIgnoreCase))
                    html.Append("<br>");
                return;
            }

            var element = node as XElement;
            if (element == null) return; // コメントは表示内容に含めない。

            string localName = element.Name.LocalName;
            if (localName == "ApprovalBrandNameRef")
            {
                string reference = (string)element.Attributes()
                    .FirstOrDefault(a => a.Name.LocalName == "ref");
                string brandName;
                if (!string.IsNullOrWhiteSpace(reference) && brandNames.TryGetValue(reference, out brandName))
                    AppendSearchableText(html, brandName, searchState);
                else if (!string.IsNullOrWhiteSpace(reference))
                    html.Append("<span class='unresolved'>[製品名参照: ")
                        .Append(HtmlEncode(reference)).Append("]</span>");
                return;
            }

            XmlViewerSection section;
            bool isSection = sectionMap.TryGetValue(element, out section);
            if (isSection)
                html.Append("<div class='section' id='").Append(section.Anchor).Append("'><h2>")
                    .Append(HtmlEncode(section.Title)).Append("</h2>");

            string opening;
            string closing;
            GetHtmlContainer(localName, out opening, out closing);
            html.Append(opening);

            if (localName == "Graphic" || localName == "Image" || localName == "Figure")
            {
                XAttribute source = element.Attributes().FirstOrDefault(a =>
                    a.Name.LocalName == "src" || a.Name.LocalName == "href");
                if (source != null)
                    html.Append("<span class='asset'>[画像: ").Append(HtmlEncode(source.Value)).Append("]</span>");
            }

            foreach (XNode child in element.Nodes())
                RenderXmlNode(child, html, brandNames, sectionMap, searchState);

            html.Append(closing);
            if (isSection) html.Append("</div>");
        }

        private void AppendXmlSummary(
            StringBuilder html,
            IDictionary<string, string> brandNames,
            IEnumerable<XmlViewerSection> sections)
        {
            string[] importantPrefixes = { "1.", "2.", "4.", "8." };
            List<XmlViewerSection> important = sections
                .Where(s => s.Element != null && importantPrefixes.Any(p => s.Title.StartsWith(p, StringComparison.Ordinal)))
                .ToList();

            html.Append("<div class='summary' id='xml-summary'><h2>概要</h2>");
            html.Append("<div class='summary-item'><h3>【薬効分類名】</h3><p>")
                .Append(HtmlEncode(string.IsNullOrWhiteSpace(_currentThera) ? "（情報なし）" : _currentThera))
                .Append("</p></div>");
            if (important.Count == 0)
            {
                html.Append("<p>概要に表示できる章がありません。</p>");
            }
            else
            {
                var emptyMap = new Dictionary<XElement, XmlViewerSection>();
                foreach (XmlViewerSection section in important)
                {
                    string summaryClass = section.Title.StartsWith("1.", StringComparison.Ordinal) ||
                        section.Title.StartsWith("2.", StringComparison.Ordinal)
                        ? "summary-item warning" : "summary-item";
                    html.Append("<div class='").Append(summaryClass).Append("'><h3>")
                        .Append(HtmlEncode(section.Title)).Append("</h3>");
                    RenderXmlNode(section.Element, html, brandNames, emptyMap, null);
                    html.Append("</div>");
                }
            }
            html.Append("</div>");
        }

        private static void AppendSearchableText(
            StringBuilder html,
            string value,
            XmlSearchRenderState searchState)
        {
            if (searchState == null || searchState.Keywords == null || searchState.Keywords.Length == 0)
            {
                html.Append(HtmlEncode(value));
                return;
            }

            int position = 0;
            while (position < value.Length)
            {
                int found = -1;
                string matchedKeyword = null;
                foreach (string keyword in searchState.Keywords)
                {
                    int candidate = value.IndexOf(
                        keyword,
                        position,
                        StringComparison.CurrentCultureIgnoreCase);
                    if (candidate < 0) continue;

                    if (found < 0 || candidate < found ||
                        (candidate == found && keyword.Length > matchedKeyword.Length))
                    {
                        found = candidate;
                        matchedKeyword = keyword;
                    }
                }

                if (found < 0)
                {
                    html.Append(HtmlEncode(value.Substring(position)));
                    break;
                }

                if (found > position)
                    html.Append(HtmlEncode(value.Substring(position, found - position)));

                html.Append("<span class='search-hit' id='xml-hit-").Append(searchState.HitCount)
                    .Append("' style='background-color:#ffea73;color:#111;border:1px solid #e0bd20;padding:1px 2px;'>")
                    .Append(HtmlEncode(value.Substring(found, matchedKeyword.Length)))
                    .Append("</span>");
                searchState.HitCount++;
                position = found + matchedKeyword.Length;
            }
        }

        private static void GetHtmlContainer(string localName, out string opening, out string closing)
        {
            switch (localName)
            {
                case "Table":
                    opening = "<table>";
                    closing = "</table>";
                    return;
                case "Tr":
                case "Row":
                case "TableRow":
                    opening = "<tr>";
                    closing = "</tr>";
                    return;
                case "Th":
                case "HeaderCell":
                    opening = "<th>";
                    closing = "</th>";
                    return;
                case "Td":
                case "Cell":
                case "TableCell":
                    opening = "<td>";
                    closing = "</td>";
                    return;
                case "List":
                    opening = "<ul>";
                    closing = "</ul>";
                    return;
                case "ListItem":
                    opening = "<li>";
                    closing = "</li>";
                    return;
                case "Paragraph":
                case "Para":
                case "Detail":
                    opening = "<p>";
                    closing = "</p>";
                    return;
                case "Title":
                case "SubTitle":
                case "Heading":
                    opening = "<h3>";
                    closing = "</h3>";
                    return;
                case "Sup":
                    opening = "<sup>";
                    closing = "</sup>";
                    return;
                case "Sub":
                    opening = "<sub>";
                    closing = "</sub>";
                    return;
                default:
                    opening = string.Empty;
                    closing = string.Empty;
                    return;
            }
        }

        private static string NormalizeViewerText(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            // XMLのインデントだけを除去し、日本語本文中の空白は残す。
            string normalized = value.Replace("\r\n", "\n").Replace('\r', '\n');
            if (string.IsNullOrWhiteSpace(normalized)) return string.Empty;
            return normalized.Trim();
        }

        private static string BuildViewerMessageHtml(string message, bool isError)
        {
            string cssClass = isError ? "message error" : "message";
            string content = "<div class='" + cssClass + "'>" +
                HtmlEncode(message).Replace("\r\n", "<br>").Replace("\n", "<br>") + "</div>";
            return WrapViewerHtml("添付文書(XML)", content);
        }

        private static string WrapViewerHtml(string title, string body)
        {
            return "<!doctype html><html><head><meta charset='utf-8'><title>" + HtmlEncode(title) +
                "</title><style>" +
                "body{font-family:'Meiryo UI','Yu Gothic UI',sans-serif;margin:0;padding:22px 30px;color:#252b33;background:#fff;line-height:1.75;}" +
                ".header{border-bottom:3px solid #365f91;margin-bottom:22px;padding-bottom:12px;}" +
                "h1{font-size:24px;margin:0 0 4px;color:#20364f;}h2{font-size:19px;color:#234f7d;background:#eef4fa;border-left:6px solid #3d75aa;padding:7px 10px;margin:26px 0 12px;}" +
                "h3{font-size:16px;margin:16px 0 6px;}.section{max-width:1100px;}p{margin:7px 0;}" +
                "table{border-collapse:collapse;width:100%;margin:12px 0;}th,td{border:1px solid #9da8b3;padding:7px 9px;vertical-align:top;}th{background:#edf2f6;}" +
                ".summary{max-width:1100px;margin-bottom:28px}.summary-item{border:1px solid #ccd6df;background:#f8fafc;margin:10px 0;padding:8px 12px}.summary-item.warning{border-color:#d8aaaa;background:#fff5f5;color:#7d2020;}" +
                "span.search-hit{background-color:#ffea73;color:#111;border:1px solid #e0bd20;padding:1px 2px;}" +
                ".meta{font-size:12px;color:#66717e}.message{margin:40px auto;max-width:760px;padding:18px 22px;border:1px solid #aebdca;background:#f4f8fb;}" +
                ".error{border-color:#c98787;background:#fff3f3;color:#7c2424}.unresolved,.asset{color:#8a4f16;background:#fff4dc;padding:1px 3px;}" +
                "</style></head><body>" + body + "</body></html>";
        }

        private static string HtmlEncode(string value)
        {
            return WebUtility.HtmlEncode(value ?? string.Empty);
        }
    }
}
