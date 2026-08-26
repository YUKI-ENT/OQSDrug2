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
        private ListBox _xmlSectionList;
        private ComboBox _xmlSearchCombo;
        private Button _xmlSearchButton;
        private Button _xmlSearchPreviousButton;
        private Button _xmlSearchNextButton;
        private Label _xmlSearchStatus;
        private FlowLayoutPanel _xmlAutomaticMatchesPanel;
        private RowStyle _xmlAutomaticMatchesRowStyle;
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

        private sealed class XmlReferenceMaps
        {
            public Dictionary<string, string> HeaderLabels { get; } =
                new Dictionary<string, string>(StringComparer.Ordinal);
            public Dictionary<string, string> LiteratureLabels { get; } =
                new Dictionary<string, string>(StringComparer.Ordinal);
            public Dictionary<string, string> CommentLabels { get; } =
                new Dictionary<string, string>(StringComparer.Ordinal);
            public string SelectedBrandId { get; set; }
        }

        private static readonly Dictionary<string, string> XmlSectionNumbers =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "Warnings", "1" },
                { "ContraIndications", "2" },
                { "CompositionAndProperty", "3" },
                { "Composition", "3.1" },
                { "Property", "3.2" },
                { "IndicationsOrEfficacy", "4" },
                { "IndicationsOrEfficacyRelatedPrecautions", "5" },
                { "EfficacyRelatedPrecautions", "5" },
                { "InfoDoseAdmin", "6" },
                { "InfoPrecautionsDosage", "7" },
                { "InfoDoseAdminRelatedPrecautions", "7" },
                { "ImportantPrecautions", "8" },
                { "UseInPatientsWithComplicationsOrHistoryOfDiseasesEtc", "9.1" },
                { "PatientsWithComplicationsOrHistory", "9.1" },
                { "PatientsWithRenalImpairment", "9.2" },
                { "PatientsWithHepaticImpairment", "9.3" },
                { "MalesAndFemalesOfReproductivePotential", "9.4" },
                { "UseInPregnant", "9.5" },
                { "UseInPregnantWomen", "9.5" },
                { "UseInNursing", "9.6" },
                { "UseInNursingMothers", "9.6" },
                { "PediatricUse", "9.7" },
                { "UseInChildren", "9.7" },
                { "UseInTheElderly", "9.8" },
                { "UseInElderly", "9.8" },
                { "Interactions", "10" },
                { "ContraIndicatedCombinations", "10.1" },
                { "PrecautionsForCombinations", "10.2" },
                { "AdverseEvents", "11" },
                { "AdverseReactions", "11" },
                { "SeriousAdverseEvents", "11.1" },
                { "OtherAdverseEvents", "11.2" },
                { "InfluenceOnLaboratoryValues", "12" },
                { "OverDosage", "13" },
                { "PrecautionsForApplication", "14" },
                { "OtherPrecautions", "15" },
                { "InformationBasedOnClinicalUse", "15.1" },
                { "InformationBasedOnNonclinicalStudies", "15.2" },
                { "Pharmacokinetics", "16" },
                { "BloodLevel", "16.1" },
                { "Absorption", "16.2" },
                { "Distribution", "16.3" },
                { "Metabolism", "16.4" },
                { "Excretion", "16.5" },
                { "SpecificPopulation", "16.6" },
                { "DrugAndDrugInteractions", "16.7" },
                { "PharmacokineticsEtc", "16.8" },
                { "ResultsOfClinicalTrials", "17" },
                { "EfficacyAndSafety", "17.1" },
                { "PostMarketingSurveylancesEtc", "17.2" },
                { "ResultsOfClinicalTrialsEtc", "17.3" },
                { "EfficacyPharmacology", "18" },
                { "MechanismOfAction", "18.1" },
                { "PhyschemOfActIngredients", "19" },
                { "PrecautionsForHandling", "20" },
                { "ConditionsOfApproval", "21" },
                { "Package", "22" },
                { "MainLiterature", "23" },
                { "AddresseeOfLiteratureRequest", "24" },
                { "AttentionOfInsurance", "25" },
                { "NameAddressManufact", "26" }
            };

        private void InitializeXmlDocumentTab()
        {
            _tabXmlDocument = new TabPage
            {
                Name = "tabXmlDocument",
                Text = "添付文書(XML)",
                Padding = new Padding(3),
                UseVisualStyleBackColor = true
            };

            var renderedRoot = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            renderedRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            _xmlAutomaticMatchesRowStyle = new RowStyle(SizeType.Absolute, 0F);
            renderedRoot.RowStyles.Add(_xmlAutomaticMatchesRowStyle);
            renderedRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var searchPanel = CreateXmlSearchPanel();
            renderedRoot.Controls.Add(searchPanel, 0, 0);

            _xmlAutomaticMatchesPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoScroll = true,
                Padding = new Padding(7, 5, 7, 4),
                Margin = Padding.Empty,
                BackColor = Color.FromArgb(255, 244, 230),
                Visible = false
            };
            renderedRoot.Controls.Add(_xmlAutomaticMatchesPanel, 0, 1);

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
            renderedRoot.Controls.Add(documentSplit, 0, 2);
            _tabXmlDocument.Controls.Add(renderedRoot);
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
            ClearAutomaticXmlSearchMatches();
            if (_xmlDocumentBrowser != null)
                _xmlDocumentBrowser.DocumentText = BuildViewerMessageHtml(message, false);
        }

        private void DisplayXmlDocument(string xmlText, DateTime? updatedAt)
        {
            if (_xmlDocumentBrowser == null) return;

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

            try
            {
                _xmlViewerDocument = ParseXmlDocument(xmlText);
                _xmlViewerBrandNames = BuildApprovalBrandNameMap(_xmlViewerDocument);
                _xmlViewerUpdatedAt = updatedAt;
                _xmlViewerSections = BuildXmlViewerSections(_xmlViewerDocument);
                PopulateXmlSectionList();
                UpdateAutomaticXmlSearchMatches();
                RenderCurrentXmlDocument();
            }
            catch (Exception ex)
            {
                _xmlDocumentBrowser.DocumentText = BuildViewerMessageHtml(
                    "XMLを解析できませんでした。\r\n" + ex.Message,
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

        private void UpdateAutomaticXmlSearchMatches()
        {
            ClearAutomaticXmlSearchMatches();
            if (_xmlViewerDocument == null || _xmlViewerDocument.Root == null ||
                _xmlAutomaticMatchesPanel == null) return;

            string documentText = _xmlViewerDocument.Root.Value ?? string.Empty;
            var matches = PmdaSearchListSettings.GetSearchItems()
                .Select(item => new
                {
                    SearchText = item,
                    HitCount = CountXmlSearchHits(documentText, ParseXmlSearchKeywords(item))
                })
                .Where(item => item.HitCount > 0)
                .ToList();

            if (matches.Count == 0) return;

            _xmlAutomaticMatchesPanel.Controls.Add(new Label
            {
                AutoSize = true,
                Font = new Font("Meiryo UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(138, 55, 18),
                Margin = new Padding(0, 7, 8, 0),
                Text = string.Format("要確認キーワード {0}項目", matches.Count)
            });

            foreach (var match in matches)
            {
                string searchText = match.SearchText;
                var button = new Button
                {
                    AutoSize = true,
                    Height = 27,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(255, 224, 204),
                    ForeColor = Color.FromArgb(126, 36, 18),
                    Font = new Font("Meiryo UI", 9F, FontStyle.Bold),
                    Margin = new Padding(3, 1, 3, 1),
                    Text = string.Format("{0}  ({1}件)", searchText, match.HitCount),
                    Tag = searchText
                };
                button.FlatAppearance.BorderColor = Color.FromArgb(218, 117, 76);
                button.Click += delegate(object sender, EventArgs e)
                {
                    var clicked = sender as Button;
                    if (clicked == null || _xmlSearchCombo == null) return;
                    _xmlSearchCombo.Text = Convert.ToString(clicked.Tag);
                    RunXmlDocumentSearch();
                };
                _xmlAutomaticMatchesPanel.Controls.Add(button);
            }

            _xmlAutomaticMatchesPanel.Visible = true;
            if (_xmlAutomaticMatchesRowStyle != null)
                _xmlAutomaticMatchesRowStyle.Height = 68F;
        }

        private void ClearAutomaticXmlSearchMatches()
        {
            if (_xmlAutomaticMatchesPanel != null)
            {
                while (_xmlAutomaticMatchesPanel.Controls.Count > 0)
                {
                    Control control = _xmlAutomaticMatchesPanel.Controls[0];
                    _xmlAutomaticMatchesPanel.Controls.RemoveAt(0);
                    control.Dispose();
                }
                _xmlAutomaticMatchesPanel.Visible = false;
            }
            if (_xmlAutomaticMatchesRowStyle != null)
                _xmlAutomaticMatchesRowStyle.Height = 0F;
        }

        private static int CountXmlSearchHits(string text, IEnumerable<string> keywords)
        {
            if (string.IsNullOrEmpty(text) || keywords == null) return 0;

            int count = 0;
            foreach (string keyword in keywords)
            {
                if (string.IsNullOrEmpty(keyword)) continue;
                int position = 0;
                while (position < text.Length)
                {
                    int found = text.IndexOf(keyword, position, StringComparison.CurrentCultureIgnoreCase);
                    if (found < 0) break;
                    count++;
                    position = found + keyword.Length;
                }
            }
            return count;
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

        private static XmlReferenceMaps BuildXmlReferenceMaps(
            XDocument document,
            string selectedYjCode)
        {
            var maps = new XmlReferenceMaps();
            if (document.Root == null) return maps;

            foreach (XElement brand in document.Descendants()
                .Where(e => e.Name.LocalName == "DetailBrandName"))
            {
                string brandId = GetXmlAttributeValue(brand, "id");
                string yjCode = NormalizeViewerText(string.Concat(
                    brand.Descendants()
                        .Where(e => e.Name.LocalName == "YJCode")
                        .Select(e => e.Value)));
                if (!string.IsNullOrEmpty(brandId) &&
                    string.Equals(yjCode, selectedYjCode, StringComparison.OrdinalIgnoreCase))
                {
                    maps.SelectedBrandId = brandId;
                    break;
                }
            }

            int literatureNumber = 0;
            foreach (XElement reference in document.Descendants()
                .Where(e => e.Name.LocalName == "Reference"))
            {
                string id = GetXmlAttributeValue(reference, "id");
                if (string.IsNullOrEmpty(id)) continue;
                literatureNumber++;
                maps.LiteratureLabels[id] = GetTrailingReferenceNumber(id, literatureNumber) + ")";
            }

            int commentNumber = 0;
            foreach (XElement comment in document.Descendants()
                .Where(e => e.Name.LocalName == "Comment"))
            {
                string id = GetXmlAttributeValue(comment, "id");
                if (string.IsNullOrEmpty(id)) continue;
                commentNumber++;
                maps.CommentLabels[id] = "注" + GetTrailingReferenceNumber(id, commentNumber) + ")";
            }

            foreach (XElement target in document.Root.DescendantsAndSelf()
                .Where(e => !string.IsNullOrEmpty(GetXmlAttributeValue(e, "id"))))
            {
                string id = GetXmlAttributeValue(target, "id");
                string label = ResolveHeaderReferenceLabel(target);
                if (!string.IsNullOrEmpty(label)) maps.HeaderLabels[id] = label;
            }

            return maps;
        }

        private static string GetTrailingReferenceNumber(string id, int fallback)
        {
            Match match = Regex.Match(id ?? string.Empty, @"(\d+)$");
            int parsed;
            return match.Success && int.TryParse(match.Groups[1].Value, out parsed)
                ? parsed.ToString()
                : fallback.ToString();
        }

        private static string ResolveHeaderReferenceLabel(XElement target)
        {
            string sectionNumber;
            if (TryGetXmlSectionNumber(target, out sectionNumber)) return sectionNumber;

            List<XElement> itemPath = target.AncestorsAndSelf()
                .Where(e => e.Name.LocalName == "Item")
                .Reverse()
                .ToList();
            XElement section = target.AncestorsAndSelf()
                .FirstOrDefault(e => TryGetXmlSectionNumber(e, out sectionNumber));
            if (section == null) return string.Empty;

            TryGetXmlSectionNumber(section, out sectionNumber);
            if (itemPath.Count == 0) return sectionNumber;
            var suffixes = new List<string>();
            foreach (XElement item in itemPath.Where(i => i.Ancestors().Contains(section)))
            {
                XElement list = item.Parent;
                if (list == null) continue;
                int index = list.Elements()
                    .Where(e => e.Name.LocalName == "Item")
                    .TakeWhile(e => e != item)
                    .Count() + 1;
                suffixes.Add(index.ToString());
            }
            return suffixes.Count == 0
                ? sectionNumber
                : sectionNumber + "." + string.Join(".", suffixes);
        }

        private static bool TryGetXmlSectionNumber(XElement element, out string number)
        {
            number = null;
            if (element == null) return false;

            if (element.Name.LocalName == "UseInSpecificPopulations")
            {
                number = element.Ancestors().Any(e => e.Name.LocalName == "Pharmacokinetics")
                    ? "16.6" : "9";
                return true;
            }

            return XmlSectionNumbers.TryGetValue(element.Name.LocalName, out number);
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
                XmlReferenceMaps references = BuildXmlReferenceMaps(document, _currentYj);
                var sectionMap = sections
                    .Where(s => s.Element != null)
                    .ToDictionary(s => s.Element, s => s);

                AppendXmlSummary(body, brandNames, references, sections);

                // 既知の章だけを抜き出さず、ルート全体を走査して未知要素の本文も保持する。
                RenderXmlNode(document.Root, body, brandNames, references, sectionMap, searchState);
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
            XmlReferenceMaps references,
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
            if (localName == "HeaderRef")
            {
                AppendXmlReference(html, element, references.HeaderLabels, "header");
                return;
            }
            if (localName == "ReferenceBookRef")
            {
                AppendXmlReference(html, element, references.LiteratureLabels, "literature");
                return;
            }
            if (localName == "CommentRef")
            {
                AppendXmlReference(html, element, references.CommentLabels, "comment");
                return;
            }
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

            if ((localName == "CompositionForBrand" || localName == "PropertyForBrand") &&
                !ShouldRenderBrandElement(element, references))
                return;

            if (sectionMap.Count > 0)
            {
                string targetId = GetXmlAttributeValue(element, "id");
                if (!string.IsNullOrEmpty(targetId))
                    html.Append("<span class='xml-ref-target' id='")
                        .Append(BuildXmlReferenceAnchor(targetId)).Append("'></span>");
            }

            if (localName == "CompositionForBrand" || localName == "PropertyForBrand")
            {
                string brandId = GetXmlAttributeValue(element, "ref");
                string brandName;
                html.Append("<div class='brand-block'>");
                if (!string.IsNullOrEmpty(brandId) && brandNames.TryGetValue(brandId, out brandName))
                    html.Append("<h4>").Append(HtmlEncode(brandName)).Append("</h4>");
                RenderXmlChildren(element, html, brandNames, references, sectionMap, searchState);
                html.Append("</div>");
                return;
            }

            if (localName == "Composition")
            {
                html.Append("<h3>3.1 組成</h3>");
                RenderXmlChildren(element, html, brandNames, references, sectionMap, searchState);
                return;
            }
            if (localName == "Property")
            {
                html.Append("<h3>3.2 製剤の性状</h3>");
                RenderXmlChildren(element, html, brandNames, references, sectionMap, searchState);
                return;
            }

            // その他の副作用は通常のHTML表ではなく、行・列・値が参照IDで分離されている。
            // categoryRef × frequencyRef を解決してから二次元表として描画する。
            if (localName == "OtherAdverse" && TryRenderOtherAdverseTable(
                element, html, brandNames, references, sectionMap, searchState))
                return;

            if ((localName == "ContraIndication" || localName == "PrecautionsForCombi") &&
                TryRenderInteractionTable(
                    element, html, brandNames, references, sectionMap, searchState))
                return;

            if ((localName == "CompositionTable" || localName == "PropertyTable") &&
                TryRenderCompositionOrPropertyTable(
                    element, html, brandNames, references, sectionMap, searchState))
                return;

            XmlViewerSection section;
            bool isSection = sectionMap.TryGetValue(element, out section);
            if (isSection)
                html.Append("<div class='section' id='").Append(section.Anchor).Append("'><h2>")
                    .Append(HtmlEncode(section.Title)).Append("</h2>");

            string opening;
            string closing;
            GetHtmlContainer(element, out opening, out closing);
            html.Append(opening);

            if (localName == "Graphic" || localName == "Image" || localName == "Figure")
            {
                XAttribute source = element.Attributes().FirstOrDefault(a =>
                    a.Name.LocalName == "src" || a.Name.LocalName == "href");
                if (source != null)
                    html.Append("<span class='asset'>[画像: ").Append(HtmlEncode(source.Value)).Append("]</span>");
            }

            foreach (XNode child in element.Nodes())
                RenderXmlNode(child, html, brandNames, references, sectionMap, searchState);

            html.Append(closing);
            if (isSection) html.Append("</div>");
        }

        private static void AppendXmlReference(
            StringBuilder html,
            XElement element,
            IDictionary<string, string> labels,
            string referenceType)
        {
            string reference = GetXmlAttributeValue(element, "ref");
            if (string.IsNullOrEmpty(reference)) return;

            string label;
            bool resolved = labels.TryGetValue(reference, out label);
            if (!resolved)
            {
                if (referenceType == "header") label = "参照";
                else if (referenceType == "comment") label = "注";
                else label = "文献";
            }

            string visible;
            if (referenceType == "header") visible = "［" + label + (resolved ? "参照］" : "］");
            else visible = label;

            html.Append("<sup class='xml-reference ").Append(referenceType).Append("-ref'>")
                .Append("<a href='#").Append(BuildXmlReferenceAnchor(reference))
                .Append("' title='").Append(HtmlEncode(reference)).Append("'>")
                .Append(HtmlEncode(visible)).Append("</a></sup>");
        }

        private static string BuildXmlReferenceAnchor(string id)
        {
            string safe = Regex.Replace(id ?? string.Empty, @"[^A-Za-z0-9_-]", "-");
            return "xml-ref-" + (string.IsNullOrEmpty(safe) ? "target" : safe);
        }

        private static bool ShouldRenderBrandElement(
            XElement element,
            XmlReferenceMaps references)
        {
            if (references == null || string.IsNullOrEmpty(references.SelectedBrandId)) return true;
            string brandReference = GetXmlAttributeValue(element, "ref");
            return string.IsNullOrEmpty(brandReference) ||
                string.Equals(
                    brandReference,
                    references.SelectedBrandId,
                    StringComparison.Ordinal);
        }

        private static bool TryRenderInteractionTable(
            XElement element,
            StringBuilder html,
            IDictionary<string, string> brandNames,
            XmlReferenceMaps references,
            IDictionary<XElement, XmlViewerSection> sectionMap,
            XmlSearchRenderState searchState)
        {
            List<XElement> drugs = element.Elements()
                .Where(e => e.Name.LocalName == "Drug")
                .ToList();
            if (drugs.Count == 0) return false;

            string caption = element.Name.LocalName == "ContraIndication"
                ? "10.1 併用禁忌（併用しないこと）"
                : "10.2 併用注意（併用に注意すること）";
            html.Append("<div class='table-scroll'><table class='pmda-table interaction-table'>")
                .Append(BuildTableColGroup(element))
                .Append("<caption>").Append(HtmlEncode(caption)).Append("</caption>")
                .Append("<thead><tr><th scope='col'>薬剤名等</th>")
                .Append("<th scope='col'>臨床症状・措置方法</th>")
                .Append("<th scope='col'>機序・危険因子</th></tr></thead><tbody>");

            foreach (XElement drug in drugs)
            {
                html.Append("<tr>");
                RenderInteractionCell("DrugName", drug, html, brandNames, references, sectionMap, searchState);
                RenderInteractionCell("ClinSymptomsAndMeasures", drug, html, brandNames, references, sectionMap, searchState);
                RenderInteractionCell("MechanismAndRiskFactors", drug, html, brandNames, references, sectionMap, searchState);
                html.Append("</tr>");
            }

            html.Append("</tbody></table></div>");
            return true;
        }

        private static void RenderInteractionCell(
            string localName,
            XElement drug,
            StringBuilder html,
            IDictionary<string, string> brandNames,
            XmlReferenceMaps references,
            IDictionary<XElement, XmlViewerSection> sectionMap,
            XmlSearchRenderState searchState)
        {
            html.Append("<td>");
            List<XElement> values = drug.Elements()
                .Where(e => e.Name.LocalName == localName)
                .ToList();
            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0) html.Append("<br>");
                RenderXmlChildren(values[i], html, brandNames, references, sectionMap, searchState);
            }
            html.Append("</td>");
        }

        private static bool TryRenderCompositionOrPropertyTable(
            XElement element,
            StringBuilder html,
            IDictionary<string, string> brandNames,
            XmlReferenceMaps references,
            IDictionary<XElement, XmlViewerSection> sectionMap,
            XmlSearchRenderState searchState)
        {
            bool isComposition = element.Name.LocalName == "CompositionTable";
            List<XElement> rows = element.Elements().ToList();
            if (rows.Count == 0) return false;

            XElement title = rows.FirstOrDefault(e =>
                e.Name.LocalName == "CompositionAndPropertyTblTitle");
            html.Append("<div class='table-scroll'><table class='pmda-table property-table'>");
            if (title != null)
            {
                html.Append("<caption>");
                RenderXmlChildren(title, html, brandNames, references, sectionMap, searchState);
                html.Append("</caption>");
            }
            html.Append("<tbody>");

            foreach (XElement row in rows)
            {
                string localName = row.Name.LocalName;
                if (row == title) continue;
                if (localName == "CompositionAndPropertyTblFoot")
                {
                    html.Append("<tr class='table-foot'><td colspan='2'>");
                    RenderXmlChildren(row, html, brandNames, references, sectionMap, searchState);
                    html.Append("</td></tr>");
                    continue;
                }

                string label;
                XElement valueElement = row;
                if (localName == "OtherComposition" || localName == "OtherProperty")
                {
                    XElement categoryName = row.Elements()
                        .FirstOrDefault(e => e.Name.LocalName == "CategoryName");
                    XElement content = row.Elements()
                        .FirstOrDefault(e => e.Name.LocalName == "Content");
                    label = categoryName == null ? "その他" : NormalizeViewerText(categoryName.Value);
                    if (content != null) valueElement = content;
                }
                else
                {
                    label = GetCompositionOrPropertyLabel(localName, isComposition);
                }

                if (string.IsNullOrEmpty(label)) label = localName;
                if (!HasDisplayableXmlContent(valueElement) &&
                    localName != "Shape") continue;
                if (localName == "Shape" && !HasDisplayableXmlContent(valueElement)) continue;

                html.Append("<tr><th scope='row'>").Append(HtmlEncode(label)).Append("</th><td>");
                if (localName == "Size" || localName == "Shape")
                    RenderLabeledPropertyChildren(
                        valueElement, html, brandNames, references, sectionMap, searchState);
                else
                    RenderSeparatedXmlChildren(
                        valueElement, html, brandNames, references, sectionMap, searchState);
                html.Append("</td></tr>");
            }

            html.Append("</tbody></table></div>");
            return true;
        }

        private static string GetCompositionOrPropertyLabel(string localName, bool isComposition)
        {
            switch (localName)
            {
                case "ContainedAmount": return "有効成分";
                case "Additives": return "添加剤";
                case "Formulation": return "剤形";
                case "ColorTone": return "色調";
                case "Shape": return "外形";
                case "Size": return "大きさ";
                case "SizeNumber": return "号数";
                case "Weight": return "質量";
                case "IdCode": return "識別コード";
                case "pH": return "pH";
                case "OsmoticRatio": return "浸透圧比";
                case "Odor": return "におい";
                case "Taste": return "味";
                default: return isComposition ? "組成" : string.Empty;
            }
        }

        private static string GetPropertyChildLabel(string localName)
        {
            switch (localName)
            {
                case "ShapeFront": return "表面";
                case "ShapeBack": return "裏面";
                case "ShapeSide": return "側面";
                case "SizeDiameter": return "直径";
                case "SizeLongDiameter": return "長径";
                case "SizeShortDiameter": return "短径";
                case "SizeThickness": return "厚さ";
                case "SizeTotalLength": return "全長";
                case "SizeArea": return "面積";
                default: return string.Empty;
            }
        }

        private static bool HasDisplayableXmlContent(XElement element)
        {
            if (element == null) return false;
            if (!string.IsNullOrWhiteSpace(element.Value)) return true;
            return element.Descendants().Any(e =>
                e.Name.LocalName == "ApprovalBrandNameRef" ||
                e.Name.LocalName == "HeaderRef" ||
                e.Name.LocalName == "ReferenceBookRef" ||
                e.Name.LocalName == "CommentRef");
        }

        private static void RenderSeparatedXmlChildren(
            XElement element,
            StringBuilder html,
            IDictionary<string, string> brandNames,
            XmlReferenceMaps references,
            IDictionary<XElement, XmlViewerSection> sectionMap,
            XmlSearchRenderState searchState)
        {
            List<XNode> children = element.Nodes()
                .Where(n => !(n is XText) || !string.IsNullOrWhiteSpace(((XText)n).Value))
                .ToList();
            for (int i = 0; i < children.Count; i++)
            {
                if (i > 0) html.Append(" ");
                RenderXmlNode(children[i], html, brandNames, references, sectionMap, searchState);
            }
        }

        private static void RenderLabeledPropertyChildren(
            XElement element,
            StringBuilder html,
            IDictionary<string, string> brandNames,
            XmlReferenceMaps references,
            IDictionary<XElement, XmlViewerSection> sectionMap,
            XmlSearchRenderState searchState)
        {
            List<XElement> children = element.Elements()
                .Where(HasDisplayableXmlContent)
                .ToList();
            for (int i = 0; i < children.Count; i++)
            {
                XElement child = children[i];
                if (i > 0) html.Append("<br>");
                string label = GetPropertyChildLabel(child.Name.LocalName);
                if (!string.IsNullOrEmpty(label))
                    html.Append("<span class='sub-label'>").Append(HtmlEncode(label)).Append("：</span>");
                RenderXmlChildren(child, html, brandNames, references, sectionMap, searchState);
            }
        }

        private static bool TryRenderOtherAdverseTable(
            XElement element,
            StringBuilder html,
            IDictionary<string, string> brandNames,
            XmlReferenceMaps references,
            IDictionary<XElement, XmlViewerSection> sectionMap,
            XmlSearchRenderState searchState)
        {
            XElement categoryDefinition = element.Elements()
                .FirstOrDefault(e => e.Name.LocalName == "CategoryDefinition");
            XElement frequencyDefinition = element.Elements()
                .FirstOrDefault(e => e.Name.LocalName == "FrequencyDefinition");
            XElement adverseReactions = element.Elements()
                .FirstOrDefault(e => e.Name.LocalName == "AdverseReactions");
            if (categoryDefinition == null || frequencyDefinition == null || adverseReactions == null)
                return false;

            List<XElement> categories = categoryDefinition.Elements()
                .Where(e => e.Name.LocalName == "Category")
                .ToList();
            List<XElement> frequencies = frequencyDefinition.Elements()
                .Where(e => e.Name.LocalName == "Frequency")
                .ToList();
            if (categories.Count == 0 || frequencies.Count == 0) return false;

            var descriptions = adverseReactions.Elements()
                .Where(e => e.Name.LocalName == "AdverseReactionDescription")
                .GroupBy(e => string.Concat(
                    GetXmlAttributeValue(e, "categoryRef"), "\u001f",
                    GetXmlAttributeValue(e, "frequencyRef")))
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

            html.Append("<div class='table-scroll'><table class='pmda-table adverse-table'>")
                .Append(BuildTableColGroup(element))
                .Append("<thead><tr><th scope='col' class='category-heading'>区分</th>");
            foreach (XElement frequency in frequencies)
            {
                html.Append("<th scope='col'>");
                RenderXmlChildren(frequency, html, brandNames, references, sectionMap, searchState);
                html.Append("</th>");
            }
            html.Append("</tr></thead><tbody>");

            foreach (XElement category in categories)
            {
                string categoryId = GetXmlAttributeValue(category, "id");
                html.Append("<tr><th scope='row'>");
                RenderXmlChildren(category, html, brandNames, references, sectionMap, searchState);
                html.Append("</th>");

                foreach (XElement frequency in frequencies)
                {
                    string key = string.Concat(
                        categoryId, "\u001f", GetXmlAttributeValue(frequency, "id"));
                    List<XElement> matching;
                    html.Append("<td>");
                    if (descriptions.TryGetValue(key, out matching))
                    {
                        for (int i = 0; i < matching.Count; i++)
                        {
                            if (i > 0) html.Append("<br>");
                            RenderXmlChildren(matching[i], html, brandNames, references, sectionMap, searchState);
                        }
                    }
                    html.Append("</td>");
                }
                html.Append("</tr>");
            }

            html.Append("</tbody></table></div>");
            return true;
        }

        private static void RenderXmlChildren(
            XElement element,
            StringBuilder html,
            IDictionary<string, string> brandNames,
            XmlReferenceMaps references,
            IDictionary<XElement, XmlViewerSection> sectionMap,
            XmlSearchRenderState searchState)
        {
            foreach (XNode child in element.Nodes())
                RenderXmlNode(child, html, brandNames, references, sectionMap, searchState);
        }

        private static string GetXmlAttributeValue(XElement element, string localName)
        {
            XAttribute attribute = element.Attributes()
                .FirstOrDefault(a => string.Equals(
                    a.Name.LocalName, localName, StringComparison.OrdinalIgnoreCase));
            return attribute == null ? string.Empty : attribute.Value;
        }

        private void AppendXmlSummary(
            StringBuilder html,
            IDictionary<string, string> brandNames,
            XmlReferenceMaps references,
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
                    RenderXmlNode(section.Element, html, brandNames, references, emptyMap, null);
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

        private static void GetHtmlContainer(XElement element, out string opening, out string closing)
        {
            string localName = element.Name.LocalName;
            switch (localName)
            {
                case "Table":
                case "SimpleTable":
                    opening = "<div class='table-scroll'><table class='pmda-table'>" +
                        BuildTableColGroup(element);
                    closing = "</table></div>";
                    return;
                case "Tr":
                case "Row":
                case "TableRow":
                case "SimpTblRow":
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
                case "SimpTblCell":
                    opening = BuildTableCellOpening(element);
                    closing = "</td>";
                    return;
                case "List":
                case "UnorderedList":
                case "SimpleList":
                    opening = "<ul>";
                    closing = "</ul>";
                    return;
                case "OrderedList":
                    opening = "<ol>";
                    closing = "</ol>";
                    return;
                case "ListItem":
                case "Item":
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

        private static string BuildTableCellOpening(XElement element)
        {
            var opening = new StringBuilder("<td");
            AppendPositiveIntegerHtmlAttribute(opening, "rowspan", GetFirstXmlAttributeValue(
                element, "rowspan", "rspan", "rowSpan"));
            AppendPositiveIntegerHtmlAttribute(opening, "colspan", GetFirstXmlAttributeValue(
                element, "colspan", "cspan", "colSpan"));

            string align = GetFirstXmlAttributeValue(element, "align");
            if (align == "left" || align == "center" || align == "right")
                opening.Append(" class='align-").Append(align).Append("'");

            opening.Append(">");
            return opening.ToString();
        }

        private static string GetFirstXmlAttributeValue(XElement element, params string[] names)
        {
            foreach (string name in names)
            {
                XAttribute attribute = element.Attributes().FirstOrDefault(a =>
                    string.Equals(a.Name.LocalName, name, StringComparison.OrdinalIgnoreCase));
                if (attribute != null) return attribute.Value;
            }
            return string.Empty;
        }

        private static void AppendPositiveIntegerHtmlAttribute(
            StringBuilder html,
            string attributeName,
            string value)
        {
            int parsed;
            if (int.TryParse(value, out parsed) && parsed > 1 && parsed <= 100)
                html.Append(" ").Append(attributeName).Append("='").Append(parsed).Append("'");
        }

        private static string BuildTableColGroup(XElement tableElement)
        {
            XElement widthDefinition = tableElement.Elements()
                .FirstOrDefault(e => e.Name.LocalName == "WidthDefinition");
            if (widthDefinition == null && tableElement.Parent != null)
                widthDefinition = tableElement.Parent.Elements()
                    .FirstOrDefault(e => e.Name.LocalName == "WidthDefinition");
            if (widthDefinition == null) return string.Empty;

            var colGroup = new StringBuilder("<colgroup>");
            int count = 0;
            foreach (XElement col in widthDefinition.Elements()
                .Where(e => e.Name.LocalName == "Col"))
            {
                string width = GetXmlAttributeValue(col, "width").Trim();
                colGroup.Append("<col");
                if (Regex.IsMatch(width, @"^\d+(?:\.\d+)?(?:%|px|pt|em|rem)?$"))
                    colGroup.Append(" style='width:").Append(width).Append("'");
                colGroup.Append(">");
                count++;
            }
            colGroup.Append("</colgroup>");
            return count == 0 ? string.Empty : colGroup.ToString();
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
                ".table-scroll{max-width:100%;overflow-x:auto;margin:12px 0;}table{border-collapse:collapse;width:100%;margin:0;}th,td{border:1px solid #9da8b3;padding:7px 9px;vertical-align:top;}th{background:#edf2f6;font-weight:bold;}" +
                ".pmda-table>tbody>tr:first-child>td{background:#f4f7fa;font-weight:bold;}.interaction-table>tbody>tr:first-child>td,.adverse-table>tbody>tr:first-child>td{background:#fff;font-weight:normal;}.adverse-table th[scope=row]{white-space:nowrap;text-align:left;}.align-center{text-align:center}.align-right{text-align:right}.align-left{text-align:left}" +
                "caption{text-align:left;font-size:16px;font-weight:bold;color:#234f7d;padding:6px 2px}.interaction-table th:first-child{width:25%}.property-table th[scope=row]{width:20%;text-align:left;white-space:nowrap}.property-table p,.interaction-table p,.adverse-table p{margin:0}.table-foot td{font-size:12px;background:#f8fafc}.sub-label{font-weight:bold;color:#46586a}.brand-block{margin:10px 0 18px}.brand-block h4{font-size:14px;margin:6px 0;color:#365f91}.xml-reference a{color:#245f9e;text-decoration:none}.xml-reference a:hover{text-decoration:underline}.xml-ref-target{display:inline;position:relative;top:-6px}" +
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
