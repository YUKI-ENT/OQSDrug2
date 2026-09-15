using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Npgsql;

namespace OQSDrug
{
    internal sealed class ChartMedication
    {
        public string Order = "", InternalCode = "", Name = "", Quantity = "", ReceptCode = "", YjCode = "";
        public string GenericName = "", BrandName = "", Institution = "", Latest = "";
        public bool IsConfirmation => Normalize(Name).Contains("処方箋料");
        internal static string Normalize(string value) => Regex.Replace((value ?? "").Normalize(NormalizationForm.FormKC), @"\s+", "").ToUpperInvariant();
    }

    internal sealed class ChartMedicationSnapshot
    {
        public long ChartId;
        public string Visit;
        public string PatientName = "";
        public DateTime VisitDate;
        public List<ChartMedication> Medications = new List<ChartMedication>();
        public string Context => ChartId.ToString(CultureInfo.InvariantCulture) + ":" + Visit + ":" + VisitDate.ToString("yyyyMMdd");
        public string Confirmation => Fingerprint(Medications.Where(m => m.IsConfirmation));
        public string Drugs => Fingerprint(Medications.Where(m => !m.IsConfirmation));
        public string Signature => Context + "|" + Fingerprint(Medications);
        private static string Fingerprint(IEnumerable<ChartMedication> meds) => string.Join("|", meds.Select(m =>
            string.Join("", new[] { m.Order, m.InternalCode, m.Name, m.Quantity, m.ReceptCode }.Select(s => (s ?? "").Length + ":" + s)))
            .OrderBy(s => s, StringComparer.Ordinal));
    }

    // Only a new confirmation or an initially confirmed visit triggers automatic checks.
    // Editing drugs while the old fee remains requires manual checking or a new confirmation.
    internal sealed class MedicationConfirmationState
    {
        private string context, observed, confirmed, completed;
        private int stable;
        private bool pending;
        public bool Observe(ChartMedicationSnapshot snapshot)
        {
            if (context != snapshot.Context) { context = snapshot.Context; observed = confirmed = completed = null; pending = false; stable = 0; }
            string marker = snapshot.Confirmation;
            if (marker.Length == 0) pending = false;
            else if (marker != confirmed) pending = true;
            confirmed = marker;
            stable = observed == snapshot.Signature ? stable + 1 : 1;
            observed = snapshot.Signature;
            return pending && stable >= 2 && observed != completed;
        }
        public void Complete(ChartMedicationSnapshot snapshot) { completed = snapshot.Signature; pending = false; }
        public void Reset() { context = observed = confirmed = completed = null; pending = false; stable = 0; }
    }

    internal sealed class MedicationInteractionHit
    {
        public string Current, History, Latest, Institution, Section, Evidence, Text, Mechanism;
    }
    internal sealed class MedicationInteractionResult
    {
        public List<MedicationInteractionHit> Koro = new List<MedicationInteractionHit>();
        public List<MedicationInteractionHit> Text = new List<MedicationInteractionHit>();
        public string Status;
        public string NoticeKey => string.Join("|", Koro.Concat(Text).Select(h => h.Section + ":" + h.Current + ":" + h.History + ":" + h.Evidence + ":" + h.Text).Distinct().OrderBy(x => x));
    }

    internal static class MedicationInteractionCheck
    {
        private static async Task<DataTable> Query(NpgsqlConnection conn, string sql, params NpgsqlParameter[] parameters)
        {
            using (var command = new NpgsqlCommand(sql, conn))
            {
                command.Parameters.AddRange(parameters);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    var table = new DataTable();
                    table.Load(reader);
                    return table;
                }
            }
        }
        private static string S(DataRow row, string name) => Convert.ToString(row[name]).Trim();
        private static string Prefix(string code) => (code ?? "").Length >= 7 ? code.Substring(0, 7) : "";

        internal static bool MatchKoro(string current, string history, string self, string target)
        {
            return !string.IsNullOrWhiteSpace(current) && !string.IsNullOrWhiteSpace(history)
                && ((current == self && history == target) || (current == target && history == self));
        }

        internal static string MatchName(string partner, ChartMedication drug)
        {
            // Never interpret a generic class heading as a concrete drug name.
            var aliases = new[] { drug.Name, drug.BrandName, drug.GenericName }.Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(ChartMedication.Normalize).Distinct().ToArray();
            foreach (string part in Regex.Split(partner ?? "", @"[\r\n、,，;；（）()]+"))
            {
                string token = ChartMedication.Normalize(part).TrimEnd('等');
                if (token.Length < 4 || token.Contains("阻害") || token.Contains("誘導") || token.Contains("除く") || token.Contains("以外")) continue;
                if (aliases.Any(a => a == token || a.Contains(token))) return part.Trim();
            }
            return null;
        }

        internal static async Task<MedicationInteractionResult> CheckAsync(ChartMedicationSnapshot snapshot, int months)
        {
            if (Properties.Settings.Default.DBtype != "pg") throw new InvalidOperationException("相互作用チェックにはPostgreSQLが必要です");
            var result = new MedicationInteractionResult();
            using (var conn = (NpgsqlConnection)CommonFunctions.GetDbConnection(true))
            {
                await conn.OpenAsync();
                var history = new List<ChartMedication>();
                using (var table = await Query(conn, @"SELECT DISTINCT ON (COALESCE(NULLIF(drugc,''),drugn)) drugc, drugn, ingren, didate,
                        COALESCE(NULLIF(prlshnm,''), metrdihnm) AS institution
                    FROM drug_history WHERE ptidmain=@pt AND prisorg<>1 AND revised IS NOT TRUE
                        AND didate>=@start AND didate<=@end
                    ORDER BY COALESCE(NULLIF(drugc,''),drugn), didate DESC, id DESC",
                    new NpgsqlParameter("pt", snapshot.ChartId / 10),
                    new NpgsqlParameter("start", snapshot.VisitDate.AddMonths(-months).ToString("yyyyMMdd")),
                    new NpgsqlParameter("end", snapshot.VisitDate.ToString("yyyyMMdd"))))
                {
                    foreach (DataRow r in table.Rows) history.Add(new ChartMedication { ReceptCode = CommonFunctions.NormalizeDrugCode(S(r,"drugc")), Name = S(r,"drugn"),
                        GenericName = S(r,"ingren"), Latest = S(r,"didate"), Institution = S(r,"institution") });
                }
                var current = snapshot.Medications.Where(m => !m.IsConfirmation && !string.IsNullOrWhiteSpace(m.Name)).ToList();
                var all = current.Concat(history).ToList();
                string[] codes = all.Select(m => m.ReceptCode).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToArray();
                using (var mapping = await Query(conn, @"SELECT DISTINCT ON (d.drugc) d.drugc, d.yj_code, s.brand_name_ja, s.generic_name_ja
                    FROM drug_code_map d LEFT JOIN sgml_rawdata s ON s.yj_code=d.yj_code
                    WHERE d.drugc=ANY(@codes) ORDER BY d.drugc, d.updated_at DESC NULLS LAST, s.updated_at DESC NULLS LAST",
                    new NpgsqlParameter("codes", codes)))
                {
                    foreach (DataRow r in mapping.Rows)
                        foreach (var med in all.Where(m => m.ReceptCode == S(r,"drugc")))
                        {
                            med.YjCode = S(r,"yj_code"); med.BrandName = S(r,"brand_name_ja");
                            if (!string.IsNullOrWhiteSpace(S(r,"generic_name_ja"))) med.GenericName = S(r,"generic_name_ja");
                        }
                }
                using (var koroRows = await Query(conn, "SELECT * FROM drug_contraindication WHERE self_code=ANY(@codes) OR target_code=ANY(@codes)", new NpgsqlParameter("codes", codes)))
                using (var textRows = await Query(conn, @"SELECT DISTINCT LEFT(yj_code,7) AS yj7, section_type, partner_name_ja, symptoms_measures_ja, mechanism_ja
                    FROM sgml_interaction WHERE LEFT(yj_code,7)=ANY(@prefixes)
                    AND (section_type LIKE '%禁忌%' OR section_type LIKE '%注意%')",
                    new NpgsqlParameter("prefixes", all.Select(m => Prefix(m.YjCode)).Where(p => p.Length == 7).Distinct().ToArray())))
                {
                    foreach (var c in current) foreach (var h in history)
                    {
                        foreach (DataRow row in koroRows.Rows)
                        {
                            if (MatchKoro(c.ReceptCode, h.ReceptCode, S(row,"self_code"), S(row,"target_code")))
                                AddHit(result.Koro, c, h, "併用禁忌", "レセ電コード一致", S(row,"symptom_action"), S(row,"mechanism"));
                        }
                        foreach (DataRow row in textRows.Rows)
                        {
                            string match = null;
                            if (S(row,"yj7") == Prefix(h.YjCode)) match = MatchName(S(row,"partner_name_ja"), c);
                            if (match == null && S(row,"yj7") == Prefix(c.YjCode)) match = MatchName(S(row,"partner_name_ja"), h);
                            if (match != null) AddHit(result.Text, c, h, S(row,"section_type"), "名称一致: " + match,
                                S(row,"partner_name_ja") + "\r\n" + S(row,"symptoms_measures_ja"), S(row,"mechanism_ja"));
                        }
                    }
                    result.Status = "他院薬 " + history.Count + "剤 / 入力薬 " + current.Count + "行 / レセ電コード未取得 "
                        + current.Count(m => string.IsNullOrWhiteSpace(m.ReceptCode)) + "行 / YJ未対応 " + all.Count(m => string.IsNullOrWhiteSpace(m.YjCode))
                        + "行 / 参照KORO " + koroRows.Rows.Count + "件・相互作用 " + textRows.Rows.Count + "件。薬効群のみの記載・未知の略称は未対応。";
                }
            }
            return result;
        }
        private static void AddHit(List<MedicationInteractionHit> hits, ChartMedication c, ChartMedication h, string section, string evidence, string text, string mechanism)
        {
            if (hits.Any(x => x.Current == c.Name && x.History == h.Name && x.Section == section && x.Text == text)) return;
            hits.Add(new MedicationInteractionHit { Current = c.Name, History = h.Name, Latest = h.Latest, Institution = h.Institution,
                Section = section, Evidence = evidence, Text = text, Mechanism = mechanism });
        }
    }
}
