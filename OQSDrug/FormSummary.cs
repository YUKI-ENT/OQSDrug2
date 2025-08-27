using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static OQSDrug.FormTKK;

namespace OQSDrug
{
    public partial class FormSummary : Form
    {
        public FormSummary(Form1 parentForm)
        {
            InitializeComponent();

            _parentForm = parentForm;
        }

        public sealed class MedRankRow
        {
            public string DrugC { get; set; } = "";   // レセプトコード（そのまま）
            public string DrugN { get; set; } = "";   // 薬剤名（コード空欄時のため常に保持）
            public long PtID { get; set; }
            public double ChronicScore { get; set; }  // 0〜1
            public double AcuteScore { get; set; }    // 0〜1
            public string LatestDate { get; set; } = "";  // "yyyyMMdd" 形式の最新投薬日
            public int DaysSinceLast { get; set; }        // 直近性（小さいほど最近）
            public string RankLabel { get; set; } = "";   // ①〜④相当のラベル
            public string Note { get; set; } = "";        // 短い説明（中断リスクなど）
        }

        private Form1 _parentForm;
        private List<(long PtID, string PtName)> ptData = new List<(long PtID, string PtName)>();

        private async Task ShowSummary(long ptId)
        {
            try
            {
                int months = 24;
                int? excludePrIsOrg = 1; // 自院を除外する場合の例

                var rows = await GetMedRanksAsync(ptId, months, excludePrIsOrg);

                dataGridViewSummary.AutoGenerateColumns = true;
                dataGridViewSummary.DataSource = rows; // DrugC / DrugN / PtID / ChronicScore / AcuteScore
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ====== ここから集計ロジック（このファイル内に完結）======

        // 正規化上限（調整用）
        // スコアの正規化上限（調整用）
        private const double GAP_CV_MAX = 1.2;
        private const double MONTH_CV_MAX = 1.5;

        // “最近”の区間（調整してOK）
        private const int RECENT_6W = 45; // 6週間程度
        private const int RECENT_3M = 90; // 3ヶ月

        private enum RouteMajor { Unknown, Oral, Injection, ExternalOrDental }

        private sealed class RouteWeight
        {
            public double ChronicW;
            public double AcuteW;
            public RouteWeight(double c, double a) { ChronicW = c; AcuteW = a; }
        }

        // 経路別の重み（後で調整しやすいよう一元管理）
        private static readonly Dictionary<RouteMajor, RouteWeight> RouteWeights =
            new Dictionary<RouteMajor, RouteWeight>
            {
        { RouteMajor.Oral,             new RouteWeight(1.00, 1.00) },
        { RouteMajor.ExternalOrDental, new RouteWeight(0.95, 0.95) }, // 貼付/外用など
        { RouteMajor.Injection,        new RouteWeight(0.60, 1.10) }, // 注射は急性寄りを強調
        { RouteMajor.Unknown,          new RouteWeight(1.00, 1.00) },
            };

        private sealed class DrugHistoryRow
        {
            public DateTime DiDate;
            public string DrugC;
            public string DrugN;
        }

        // ラベル＆注記生成
        private static void MakeLabelAndNote(double chronic, double acute, int daysSinceLast, double coverage,
                                             out string label, out string note)
        {
            if (daysSinceLast <= RECENT_6W && chronic >= 0.55)
            {
                label = "継続中（慢性）";
                note = string.Format("直近{0}日以内・月カバレッジ{1:P0}", daysSinceLast, coverage);
                return;
            }
            if (chronic >= 0.55 && daysSinceLast > RECENT_3M)
            {
                label = "継続傾向だが3ヶ月空白";
                note = string.Format("最後の投薬から{0}日・月カバレッジ{1:P0}", daysSinceLast, coverage);
                return;
            }
            if (acute >= 0.60 && daysSinceLast <= RECENT_3M)
            {
                label = "急性（最近）";
                note = string.Format("直近{0}日以内に短期投薬傾向", daysSinceLast);
                return;
            }
            if (acute >= 0.60 && daysSinceLast > RECENT_3M)
            {
                label = "急性（過去）";
                note = string.Format("最後の投薬から{0}日", daysSinceLast);
                return;
            }
            if (chronic >= 0.45 && daysSinceLast <= RECENT_3M)
            {
                label = "準継続（要確認）";
                note = string.Format("直近{0}日以内・月カバレッジ{1:P0}", daysSinceLast, coverage);
                return;
            }
            label = "散発";
            note = string.Format("最後の投薬から{0}日", daysSinceLast);
        }

        // ※ MedRankRow に以下の列がある前提（FormSummary.cs 冒頭の定義を拡張）
        // public string LatestDate { get; set; } = "";
        // public int    DaysSinceLast { get; set; }
        // public string RankLabel { get; set; } = "";
        // public string Note { get; set; } = "";

        /// <summary>
        /// 入力：患者ID、対象期間（月）、自院除外PrIsOrg値(例:1を除外)。
        /// 出力：薬剤ごとの Chronic/Acute スコア（0-1）＋ラベル等。
        /// 依存：CommonFunctions.GetDbConnection(), ConvertSqlForOleDb(), AddDbParameter(), ReceptToMedisCodeMap
        /// </summary>
        private async Task<List<MedRankRow>> GetMedRanksAsync(long ptId, int months, int? excludePrIsOrgValue)
        {
            var refDate = DateTime.Today.Date;
            var startDate = refDate.AddMonths(-months);
            string startDateStr = startDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

            // 1) drug_history 抽出（Access対策：?の数と順序を一致させる）
            var rows = new List<DrugHistoryRow>();
            using (IDbConnection conn = CommonFunctions.GetDbConnection())
            {
                var dbConn = conn as DbConnection;
                if (dbConn != null) await dbConn.OpenAsync(); else conn.Open();

                using (IDbCommand cmd = conn.CreateCommand())
                {
                    string sql = @"
                SELECT didate, drugc, drugn, prisorg
                FROM drug_history
                WHERE ptidmain = @PtID
                  AND didate >= @StartDate
            ";
                    if (excludePrIsOrgValue.HasValue)
                        sql += "  AND prisorg <> @ExPrIsOrg \n";

                    sql += "ORDER BY drugc, drugn, didate;";
                    cmd.CommandText = CommonFunctions.ConvertSqlForOleDb(sql);

                    CommonFunctions.AddDbParameter(cmd, "@PtID", ptId);
                    CommonFunctions.AddDbParameter(cmd, "@StartDate", startDateStr);
                    if (excludePrIsOrgValue.HasValue)
                        CommonFunctions.AddDbParameter(cmd, "@ExPrIsOrg", excludePrIsOrgValue.Value);

                    var dbc = cmd as DbCommand;
                    if (dbc != null)
                    {
                        using (DbDataReader r = await dbc.ExecuteReaderAsync())
                        {
                            while (await r.ReadAsync())
                            {
                                DateTime di = DateTime.ParseExact(r["didate"].ToString(), "yyyyMMdd", CultureInfo.InvariantCulture);
                                rows.Add(new DrugHistoryRow
                                {
                                    DiDate = di,
                                    DrugC = (r["drugc"] == DBNull.Value ? "" : r["drugc"].ToString().Trim()),
                                    DrugN = (r["drugn"] == DBNull.Value ? "" : r["drugn"].ToString().Trim())
                                });
                            }
                        }
                    }
                    else
                    {
                        using (IDataReader r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                DateTime di = DateTime.ParseExact(r["didate"].ToString(), "yyyyMMdd", CultureInfo.InvariantCulture);
                                rows.Add(new DrugHistoryRow
                                {
                                    DiDate = di,
                                    DrugC = (r["drugc"] == DBNull.Value ? "" : r["drugc"].ToString().Trim()),
                                    DrugN = (r["drugn"] == DBNull.Value ? "" : r["drugn"].ToString().Trim())
                                });
                            }
                        }
                    }
                }
            }

            // 2) グループ化（コード空欄は名称で補助）
            var groups = rows.GroupBy(x => string.IsNullOrEmpty(x.DrugC) ? ("#NOCODE:" + x.DrugN) : x.DrugC);

            var results = new List<MedRankRow>();

            foreach (var g in groups)
            {
                var entries = g.OrderBy(x => x.DiDate).ToList();
                if (entries.Count == 0) continue;

                string drugc = entries[entries.Count - 1].DrugC ?? "";
                string drugn = string.IsNullOrWhiteSpace(entries[entries.Count - 1].DrugN) ? "(名称未設定)" : entries[entries.Count - 1].DrugN;

                // 投与日（重複除去）
                var dates = entries.Select(e => e.DiDate.Date).Distinct().OrderBy(d => d).ToList();
                int fills = dates.Count;
                int daysSinceLast = (int)(refDate - dates[dates.Count - 1]).TotalDays;

                // 規則性メトリクス
                double? gapCv = ComputeGapCv(dates);      // 調剤間隔CV
                double? monthCv = ComputeMonthCv(dates);    // 月別ヒストグラムCV
                double coverage = ComputeCoverageMonths(dates, startDate, refDate); // 期間内に「投薬のあった月」の比率 0-1

                // 3) 剤型（経路大区分）を薬価コードで判定（Recept→MedisCode）
                RouteMajor route = RouteMajor.Unknown;
                if (!string.IsNullOrEmpty(drugc))
                {
                    string medis;
                    if (CommonFunctions.ReceptToMedisCodeMap != null
                        && CommonFunctions.ReceptToMedisCodeMap.TryGetValue(drugc, out medis))
                    {
                        route = ParseRouteFromMedisCode(medis);
                    }
                }

                // 4) スコア算出＋剤型重み
                double chronic; double acute;
                ComputeScores(daysSinceLast, fills, gapCv, monthCv, coverage, out chronic, out acute);

                RouteWeight weight;
                if (!RouteWeights.TryGetValue(route, out weight))
                    weight = RouteWeights[RouteMajor.Unknown];

                chronic = Clamp01(chronic * weight.ChronicW);
                acute = Clamp01(acute * weight.AcuteW);

                // 表示用：最新日(yyyyMMdd)、ラベル/注記
                string latestStr = dates[dates.Count - 1].ToString("yyyyMMdd", CultureInfo.InvariantCulture);
                string label, note;
                MakeLabelAndNote(chronic, acute, daysSinceLast, coverage, out label, out note);

                results.Add(new MedRankRow
                {
                    DrugC = drugc,
                    DrugN = drugn,
                    PtID = (int)ptId,
                    ChronicScore = Math.Round(chronic, 4),
                    AcuteScore = Math.Round(acute, 4),
                    LatestDate = latestStr,
                    DaysSinceLast = daysSinceLast,
                    RankLabel = label,
                    Note = note
                });
            }

            return results
                .OrderByDescending(x => x.ChronicScore)
                .ThenByDescending(x => x.AcuteScore)
                .ThenBy(x => x.DrugC) // 安定化
                .ToList();
        }

        // ---- 内部ヘルパ ----

        private static RouteMajor ParseRouteFromMedisCode(string medisCodeRaw)
        {
            if (string.IsNullOrWhiteSpace(medisCodeRaw)) return RouteMajor.Unknown;

            // ハイフン等を除去して英数字のみ（念のため）
            char[] buf = medisCodeRaw.Trim().ToCharArray();
            var list = new List<char>(buf.Length);
            for (int i = 0; i < buf.Length; i++)
            {
                if (char.IsLetterOrDigit(buf[i])) list.Add(buf[i]);
            }
            var s = new string(list.ToArray());

            if (s.Length < 5) return RouteMajor.Unknown;
            char c = s[4];

            // 経路の大区分（薬価基準コードの5桁目）
            if (c == '0' || c == '1' || c == '2' || c == '3') return RouteMajor.Oral;
            if (c == '4' || c == '5' || c == '6') return RouteMajor.Injection;
            if (c == '7' || c == '8' || c == '9') return RouteMajor.ExternalOrDental;

            return RouteMajor.Unknown;
        }

        private static void ComputeScores(
            int daysSinceLast, int fills, double? gapCv, double? monthCv, double coverage,
            out double chronic, out double acute)
        {
            // 規則性（低いほど良い）→ “良さ”に変換
            double regGap = 1.0 - Math.Min(1.0, (gapCv.HasValue ? (gapCv.Value / GAP_CV_MAX) : 1.0));
            double regMon = 1.0 - Math.Min(1.0, (monthCv.HasValue ? (monthCv.Value / MONTH_CV_MAX) : 1.0));
            double regularity = 0.65 * regGap + 0.35 * regMon;

            // 直近性（近いほど高い）
            double recency = 1.0 / (1.0 + (double)Math.Max(0, daysSinceLast) / 30.0); // 30日で≈0.5

            // フィル回数（多い=慢性寄り）  ※必要なら係数に反映も可
            double fillsScore = Math.Min(1.0, (double)fills / 3.0);

            // 慢性：規則性＋カバレッジ＋直近の順に重視
            chronic = 0.5 * regularity + 0.35 * coverage + 0.15 * recency;

            // 急性：直近＋不規則（=1-regularity）＋低カバレッジ
            acute = 0.60 * recency + 0.25 * (1.0 - regularity) + 0.15 * (1.0 - coverage);

            // フィルが多いと急性はやや抑制（任意）
            if (fillsScore >= 0.67) acute *= 0.85;
        }

        private static double? ComputeGapCv(List<DateTime> dates)
        {
            if (dates == null || dates.Count < 3) return (double?)null;
            var gaps = new List<int>();
            for (int i = 1; i < dates.Count; i++)
                gaps.Add((int)(dates[i] - dates[i - 1]).TotalDays);

            double mu = gaps.Average();
            if (mu <= 0) return (double?)null;

            double sum = 0.0;
            for (int i = 0; i < gaps.Count; i++)
            {
                double d = gaps[i] - mu;
                sum += d * d;
            }
            double sd = Math.Sqrt(sum / gaps.Count);
            return sd / mu;
        }

        private static double? ComputeMonthCv(List<DateTime> dates)
        {
            if (dates == null || dates.Count == 0) return (double?)null;

            var countsByMonth = dates
                .GroupBy(d => new DateTime(d.Year, d.Month, 1))
                .Select(g => g.Count())
                .ToList();

            if (countsByMonth.Count <= 1) return (double?)null;

            double mu = countsByMonth.Average();
            if (mu <= 0) return (double?)null;

            double sum = 0.0;
            for (int i = 0; i < countsByMonth.Count; i++)
            {
                double d = countsByMonth[i] - mu;
                sum += d * d;
            }
            double sd = Math.Sqrt(sum / countsByMonth.Count);
            return sd / mu;
        }

        private static double ComputeCoverageMonths(List<DateTime> dates, DateTime start, DateTime end)
        {
            if (dates == null || dates.Count == 0) return 0.0;

            // 期間内の「投薬のあった月」の割合
            var monthsWithFill = dates
                .Select(d => new DateTime(d.Year, d.Month, 1))
                .Distinct()
                .Count();

            int totalMonths = ((end.Year - start.Year) * 12 + (end.Month - start.Month)) + 1;
            if (totalMonths <= 0) totalMonths = 1;

            double ratio = (double)monthsWithFill / (double)totalMonths;
            if (ratio < 0.0) ratio = 0.0;
            if (ratio > 1.0) ratio = 1.0;
            return ratio;
        }

        private static double Clamp01(double v)
        {
            if (v < 0) return 0;
            if (v > 1) return 1;
            return v;
        }

        public async Task LoadDataIntoComboBoxes()
        {
            if (!await CommonFunctions.WaitForDbUnlock(2000))
            {
                MessageBox.Show("データベースがロックされており、LoadDataIntoComboBoxes に失敗しました。もう一度やり直してみてください。");
                return;
            }

            string sql = @"
                select ptidmain, ptname, maxid
                from (
                    select ptidmain, ptname, max(id) as maxid
                    from drug_history
                    group by ptidmain, ptname
                ) as sub
                order by maxid desc;";

            sql = CommonFunctions.ConvertSqlForOleDb(sql);

            var localList = new List<(long PtID, string PtName)>();

            using (IDbConnection connection = CommonFunctions.GetDbConnection(true))
            {
                try
                {
                    await ((DbConnection)connection).OpenAsync();

                    CommonFunctions.DataDbLock = true;

                    using (IDbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = sql;

                        using (var reader = await ((DbCommand)command).ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                long ptIDMain = reader["ptidmain"] != DBNull.Value ? Convert.ToInt64(reader["ptidmain"]) : 0;
                                string ptName = reader["ptname"] != DBNull.Value ? Convert.ToString(reader["ptname"]) : "";

                                // 表示用の文字列
                                string displayName = $"{ptIDMain.ToString().PadLeft(6, ' ')} : {ptName}";

                                localList.Add((ptIDMain, displayName));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("データの取得中にエラーが発生しました: " + ex.Message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                finally
                {
                    CommonFunctions.DataDbLock = false;
                }
            }

            // UI 更新
            try
            {
                ptData = localList;

                toolStrip1.Invoke(new Action(() =>
                {
                    toolStripComboBoxPt.SelectedIndexChanged -= toolStripComboBoxPt_SelectedIndexChanged;

                    toolStripComboBoxPt.Items.Clear();
                    toolStripComboBoxPt.SelectedIndex = -1;

                    foreach (var item in ptData)
                    {
                        toolStripComboBoxPt.Items.Add(new PtItem
                        {
                            PtID = item.PtID,
                            DisplayText = item.PtName // ← 修正: DisplayNameではなくPtName
                        });
                    }

                    toolStripComboBoxPt.SelectedIndexChanged += toolStripComboBoxPt_SelectedIndexChanged;

                    int index = ptData.FindIndex(p => p.PtID == _parentForm.tempId);
                    toolStripComboBoxPt.SelectedIndex = (index >= 0) ? index : -1;
                    
                }));
            }
            catch (Exception ex)
            {
                MessageBox.Show("コンボボックスの更新中にエラーが発生しました: " + ex.Message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void toolStripComboBoxPt_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (toolStripComboBoxPt.SelectedItem is PtItem selectedPt)
                {
                    // 選択された PtID を取得
                    long ptID = selectedPt.PtID;

                    //tempIDを設定
                    _parentForm.tempId = ptID;

                    // sender が toolstripButton かどうかを判定
                    if (!(sender is ToolStripButton stripButton))
                    {
                        //コンボボックスからの場合表示期間をリセットする
                        //ShowSpan = Properties.Settings.Default.ViewerSpan;
                        //RemovetoolStripButtonSpanEvent();
                        //SetSpanButtonState(ShowSpan);
                        //SettoolStripButtonSpanEvent();
                    }

                    // DataGridView に表示するデータを取得
                    await ShowSummary(ptID);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"患者ID選択時にエラーが発生しました:{ex.Message}");
            }
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private async void FormSummary_Load(object sender, EventArgs e)
        {
            await LoadDataIntoComboBoxes();
        }
    }
}
