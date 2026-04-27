using System;
using System.Collections.Generic;
using Npgsql;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace OQSDrug
{
    internal static class QualificationColumnNames
    {
        public const string ProcessExecutedAt = "\u51e6\u7406\u5b9f\u884c\u65e5\u6642";
        public const string ArbitraryFileIdentifier = "\u4efb\u610f\u306e\u30d5\u30a1\u30a4\u30eb\u8b58\u5225\u5b50";
        public const string ProcessStatus = "\u51e6\u7406\u7d50\u679c\u72b6\u6cc1";
        public const string ProcessCode = "\u51e6\u7406\u7d50\u679c\u30b3\u30fc\u30c9";
        public const string ProcessMessage = "\u51e6\u7406\u7d50\u679c\u30e1\u30c3\u30bb\u30fc\u30b8";
        public const string QualificationValidity = "\u8cc7\u683c\u6709\u52b9\u6027";
        public const string InsurerNumber = "\u4fdd\u967a\u8005\u756a\u53f7";
        public const string CardSymbol = "\u88ab\u4fdd\u967a\u8005\u8a3c\u8a18\u53f7";
        public const string CardNumber = "\u88ab\u4fdd\u967a\u8005\u8a3c\u756a\u53f7";
        public const string CardBranchNumber = "\u88ab\u4fdd\u967a\u8005\u8a3c\u679d\u756a";
        public const string Name = "\u6c0f\u540d";
        public const string KanaName = "\u6c0f\u540d\u30ab\u30ca";
        public const string BirthDate = "\u751f\u5e74\u6708\u65e5";
        public const string BirthDateSeireki = "\u751f\u5e74\u6708\u65e5\u897f\u66a6";
        public const string Address = "\u4f4f\u6240";
        public const string PostalCode = "\u90f5\u4fbf\u756a\u53f7";
        public const string QueryNumber = "\u7167\u4f1a\u756a\u53f7";
        public const string QueryCategory = "\u7167\u4f1a\u533a\u5206";
        public const string QueryCategoryDisplay = "\u7167\u4f1a\u533a\u5206\u8868\u793a";
        public const string QualificationStartDate = "\u8cc7\u683c\u53d6\u5f97\u5e74\u6708\u65e5";
        public const string QualificationEndDate = "\u8cc7\u683c\u55aa\u5931\u5e74\u6708\u65e5";
        public const string SexCode = "\u6027\u5225\u30b3\u30fc\u30c9";
        public const string ReceptionSent = "\u53d7\u4ed8\u9001";
        public const string Hidden = "\u975e\u8868\u793a";
        public const string KarteNumber = "\u30ab\u30eb\u30c6\u756a\u53f7";
        public const string PatientMasterName = "\u60a3\u8005\u30de\u30b9\u30bf\u30fc\u6c0f\u540d";
        public const string PatientMasterQueryNumber = "\u60a3\u8005\u30de\u30b9\u30bf\u30fc\u7167\u4f1a\u756a\u53f7";
        public const string PatientMasterInsurerNumber = "\u60a3\u8005\u30de\u30b9\u30bf\u30fc\u4fdd\u967a\u8005\u756a\u53f7";
        public const string SymbolShort = "\u8a18\u53f7";
        public const string NumberShort = "\u756a\u53f7";
        public const string BranchShort = "\u679d\u756a";
    }

    internal sealed class ImportedQualificationRecord
    {
        public long StorageId { get; set; }
        public int Sequence { get; set; }
        public BulkQualificationKind Kind { get; set; }
        public string SourceFilePath { get; set; } = string.Empty;
        public string XmlIdentifier { get; set; } = string.Empty;
        public DateTime ImportedAt { get; set; }
        public string Name { get; set; } = string.Empty;
        public string KanaName { get; set; } = string.Empty;
        public string BirthDate { get; set; } = string.Empty;
        public string InsurerNumber { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
        public string BranchNumber { get; set; } = string.Empty;
        public string QualificationValidity { get; set; } = string.Empty;
        public string QualificationStartDate { get; set; } = string.Empty;
        public string QualificationEndDate { get; set; } = string.Empty;
        public string ProcessingResultStatus { get; set; } = string.Empty;
        public string ProcessingResultCode { get; set; } = string.Empty;
        public string ProcessingResultMessage { get; set; } = string.Empty;
        public string QueryNumber { get; set; } = string.Empty;
        public string InquiryClassification { get; set; } = string.Empty;
        public string InquiryClassificationDisplay { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string SexCode { get; set; } = string.Empty;
        public long MatchedPatientId { get; set; }
        public string MatchedPatientName { get; set; } = string.Empty;
        public string MatchStatus { get; set; } = "Pending";
        public bool IsSent { get; set; }
        public string LastSendMessage { get; set; } = string.Empty;
        public Dictionary<string, object> DynamicsValues { get; } = new Dictionary<string, object>(StringComparer.Ordinal);
        public Dictionary<string, object> BulkToolValues { get; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public string DetailText()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"取得日時: {FormatImportedAt()}");
            sb.AppendLine($"種別: {GetKindDisplayName(Kind)}");
            sb.AppendLine($"氏名: {Name}");
            sb.AppendLine($"カナ: {KanaName}");
            sb.AppendLine($"生年月日: {BirthDate}");
            sb.AppendLine($"資格: {QualificationValidity}");
            sb.AppendLine($"資格取得日: {QualificationStartDate}");
            sb.AppendLine($"資格喪失日: {QualificationEndDate}");
            sb.AppendLine($"照会番号: {QueryNumber}");
            sb.AppendLine($"処理状況: {ProcessingResultStatus}");
            sb.AppendLine($"処理コード: {ProcessingResultCode}");
            sb.AppendLine($"メッセージ: {ProcessingResultMessage}");
            sb.AppendLine($"住所: {Address}");
            sb.AppendLine($"郵便番号: {PostalCode}");
            sb.AppendLine($"照合: {MatchStatus}");
            if (MatchedPatientId > 0)
            {
                sb.AppendLine($"カルテ番号: {MatchedPatientId}");
                sb.AppendLine($"患者マスター氏名: {MatchedPatientName}");
            }

            if (!string.IsNullOrWhiteSpace(LastSendMessage))
            {
                sb.AppendLine($"送信: {LastSendMessage}");
            }

            AppendFaceSummary(sb);
            return sb.ToString().TrimEnd();
        }

        public string FormatImportedAt()
        {
            return ImportedAt == DateTime.MinValue ? string.Empty : ImportedAt.ToString("yyyy/MM/dd HH:mm:ss");
        }

        public static string GetKindDisplayName(BulkQualificationKind kind)
        {
            switch (kind)
            {
                case BulkQualificationKind.MedicalAid:
                    return "医療扶助";
                case BulkQualificationKind.Houmon:
                    return "訪問診療";
                case BulkQualificationKind.Online:
                    return "オンライン診療";
                default:
                    return kind.ToString();
            }
        }

        private void AppendFaceSummary(StringBuilder sb)
        {
            sb.AppendLine();
            sb.AppendLine("face送信内容:");
            AppendBulkValue(sb, "被保険者証区分", "INSCC");
            AppendBulkValue(sb, "保険者番号/公費負担者番号", "INSN");
            AppendBulkValue(sb, "記号", "INSCS");
            AppendBulkValue(sb, "番号/受給者番号", "INSIN");
            AppendBulkValue(sb, "枝番", "INSBN");
            AppendBulkValue(sb, "本人家族区分", "PFC");
            AppendBulkValue(sb, "被保険者氏名", "IEDN");
            AppendBulkValue(sb, "氏名その他", "NOO");
            AppendBulkValue(sb, "カナその他", "NOOK");
            AppendBulkValue(sb, "性別1", "S1");
            AppendBulkValue(sb, "性別2", "S2");
            AppendBulkValue(sb, "資格喪失事由", "ROL");
            AppendBulkValue(sb, "保険者名/福祉事務所名", "IERN");
            AppendBulkValue(sb, "高齢受給者証交付日", "ERCD");
            AppendBulkValue(sb, "高齢受給者証開始日", "ERVSD");
            AppendBulkValue(sb, "高齢受給者証終了日", "ERVED");
            AppendBulkValue(sb, "高齢受給者負担割合", "ERCR");
            AppendBulkValue(sb, "限度額同意フラグ", "LACRCF");
            AppendBulkValue(sb, "限度額同意日時", "LACRCT");
            AppendBulkValue(sb, "限度額区分", "LACC");
            AppendBulkValue(sb, "限度額適用区分", "LACCF");
            AppendBulkValue(sb, "限度額交付日", "LACD");
            AppendBulkValue(sb, "限度額開始日", "LACVSD");
            AppendBulkValue(sb, "限度額終了日", "LACVED");
            AppendBulkValue(sb, "特定疾病同意フラグ", "SDCRCF");
            AppendBulkValue(sb, "特定疾病同意日時", "SDCRCT");
            AppendBulkValue(sb, "特定疾病区分", "SDDC");
            AppendBulkValue(sb, "特定疾病交付日", "SDCD");
            AppendBulkValue(sb, "特定疾病開始日", "SDVSD");
            AppendBulkValue(sb, "特定疾病終了日", "SDVED");
            AppendBulkValue(sb, "特定疾病自己負担限度額", "SDSP");
            AppendBulkValue(sb, "医療券件数", "NOFT");
            AppendBulkValue(sb, "医療券・調剤券別", "TT");
            AppendBulkValue(sb, "医療券開始日", "MTVD");
            AppendBulkValue(sb, "医療券終了日", "MTED");
            AppendBulkValue(sb, "交付番号", "MTIN");
            AppendBulkValue(sb, "診療年月", "MTM");
            AppendBulkValue(sb, "指定医療機関コード", "DMIC");
            AppendBulkValue(sb, "指定医療機関確認フラグ", "DMIF");
            AppendBulkValue(sb, "指定医療機関名", "DMIN");
            AppendBulkValue(sb, "処方箋発行元医療機関コード", "PIMIC");
            AppendBulkValue(sb, "処方箋発行元医療機関名", "PIMIN");
            AppendBulkValue(sb, "診療別", "MTT");
            AppendBulkValue(sb, "本人支払額", "SPA");
            AppendBulkValue(sb, "地区担当員名", "DCN");
            AppendBulkValue(sb, "取扱担当者名", "HCN");
            AppendBulkValue(sb, "単独・併用別", "SOCU");
            AppendBulkValue(sb, "社会保険状況", "SOSI");
            AppendBulkValue(sb, "整合性フラグ", "CF");
            AppendBulkValue(sb, "感染症該当状況", "SOI");
            AppendBulkValue(sb, "後期高齢者該当状況", "SOEMC");
            AppendBulkValue(sb, "都道府県費該当状況", "SOPE");
            AppendBulkValue(sb, "備考1", "R1");
            AppendBulkValue(sb, "備考2", "R2");
            AppendBulkValue(sb, "備考3", "R3");
        }

        private void AppendBulkValue(StringBuilder sb, string label, string key)
        {
            if (!BulkToolValues.TryGetValue(key, out object value) || value == null || value == DBNull.Value)
            {
                return;
            }

            string text = Convert.ToString(value);
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            sb.AppendLine($"{label}: {text.Trim()}");
        }
    }

    internal sealed class QualificationImportSession
    {
        public BulkQualificationKind Kind { get; set; }
        public string SourceFilePath { get; set; } = string.Empty;
        public string ExtractedXmlPath { get; set; } = string.Empty;
        public string WorkingDirectory { get; set; } = string.Empty;
        public DateTime ImportedAt { get; set; }
        public List<ImportedQualificationRecord> Records { get; } = new List<ImportedQualificationRecord>();
    }

    internal sealed class BulkColumnMapping
    {
        public BulkColumnMapping(string alias, string columnName)
        {
            Alias = alias;
            ColumnName = columnName;
        }

        public string Alias { get; }
        public string ColumnName { get; }
    }

    internal static class BulkToolUploadSchema
    {
        public const string TableName = "bulk_import_history";

        public static readonly BulkColumnMapping[] BulkColumnMappings = new[]
        {
            Map("KARTENO", "karte_number"),
            Map("MEDTYP", "medical_type_code"),
            Map("MTBR", "medical_ticket_branch"),
            Map("CNFTYPPT", "confirmation_type_patient"),
            Map("CNFTYPBR", "confirmation_type_branch"),
            Map("PET", "ProcessExecutionTime"),
            Map("QCD", "QualificationConfirmationDate"),
            Map("MIC", "MedicalInstitutionCode"),
            Map("AFI", "ArbitraryFileIdentifier"),
            Map("RC", "ReferenceClassification"),
            Map("SOR", "SegmentOfResult"),
            Map("CCI", "CharacterCodeIdentifier"),
            Map("PRS", "ProcessingResultStatus"),
            Map("QV", "QualificationValidity"),
            Map("INSCC", "InsuredCardClassification"),
            Map("INSN", "InsurerNumber"),
            Map("INSCS", "InsuredCardSymbol"),
            Map("INSIN", "InsuredIdentificationNumber"),
            Map("INSBN", "InsuredBranchNumber"),
            Map("KFB", "PublicExpenseNumber"),
            Map("KJB", "BeneficiaryNumber"),
            Map("PFC", "PersonalFamilyClassification"),
            Map("IEDN", "InsuredName"),
            Map("NPT", "Name"),
            Map("NOO", "NameOfOther"),
            Map("NK", "NameKana"),
            Map("NOOK", "NameOfOtherKana"),
            Map("S1", "Sex1"),
            Map("S2", "Sex2"),
            Map("BD", "Birthdate"),
            Map("AD", "Address"),
            Map("PN", "PostNumber"),
            Map("QD", "QualificationDate"),
            Map("DD", "DisqualificationDate"),
            Map("ICID", "InsuredCertificateIssuanceDate"),
            Map("ICVD", "InsuredCardValidDate"),
            Map("ICED", "InsuredCardExpirationDate"),
            Map("IPCR", "InsuredPartialContributionRatio"),
            Map("PC", "PreschoolClassification"),
            Map("ROL", "ReasonOfLoss"),
            Map("IERN", "InsurerName"),
            Map("JFN", "WelfareOfficeName"),
            Map("ERCD", "ElderlyRecipientCertificateDate"),
            Map("ERVSD", "ElderlyRecipientValidStartDate"),
            Map("ERVED", "ElderlyRecipientValidEndDate"),
            Map("ERCR", "ElderlyRecipientContributionRatio"),
            Map("LACRCF", "LimitApplicationCertificateRelatedConsFlg"),
            Map("LACRCT", "LimitApplicationCertificateRelatedConsTime"),
            Map("LACC", "LimitApplicationCertificateClassification"),
            Map("LACCF", "LimitApplicationCertificateClassificationFlag"),
            Map("LACD", "LimitApplicationCertificateDate"),
            Map("LACVSD", "LimitApplicationCertificateValidStartDate"),
            Map("LACVED", "LimitApplicationCertificateValidEndDate"),
            Map("LACLTD", "LimitApplicationCertificateLongTermDate"),
            Map("SDCRCF", "SpecificDiseasesCertificateRelatedConsFlg"),
            Map("SDCRCT", "SpecificDiseasesCertificateRelatedConsTime"),
            Map("SDDC", "SpecificDiseasesDiseaseCategory"),
            Map("SDCD", "SpecificDiseasesCertificateDate"),
            Map("SDVSD", "SpecificDiseasesValidStartDate"),
            Map("SDVED", "SpecificDiseasesValidEndDate"),
            Map("SDSP", "SpecificDiseasesSelfPay"),
            Map("NOFT", "NumberOfMedicalTickets"),
            Map("TT", "TicketType"),
            Map("MTVD", "MedicalTicketValidDate"),
            Map("MTED", "MedicalTicketExpirationDate"),
            Map("MTIN", "IssueNumber"),
            Map("MTM", "MedicalTreatmentMonth"),
            Map("DMIC", "DesignatedMedicalInstitutionCode"),
            Map("DMIF", "DesignatedMedicalInstitutionFlag"),
            Map("DMIN", "DesignatedMedicalInstitutionName"),
            Map("PIMIC", "PrescriptionIssuerMedicalInstitutionCode"),
            Map("PIMIN", "PrescriptionIssuerMedicalInstitutionName"),
            Map("INJN1", "InjuryName1"),
            Map("INJN2", "InjuryName2"),
            Map("INJN3", "InjuryName3"),
            Map("MTT", "MedicalTreatmentType"),
            Map("SPA", "SelfPayAmount"),
            Map("DCN", "DistrictContactName"),
            Map("HCN", "HandlingContactName"),
            Map("SOCU", "SingleOrCombinedUse"),
            Map("SOSI", "StatusOfSocialInsurance"),
            Map("CF", "ConsistencyFlag"),
            Map("SOI", "StatusOfInfecton"),
            Map("SOEMC", "StatusOfElderlyMedicalCare"),
            Map("SOPE", "StatusOfPrefecturalExpenses"),
            Map("R1", "Remarks1"),
            Map("R2", "Remarks2"),
            Map("R3", "Remarks3"),
            Map("SHCICF", "SpecificHealthCheckupsInfoConsFlg"),
            Map("SHCICT", "SpecificHealthCheckupsInfoConsTime"),
            Map("SHCIAT", "SpecificHealthCheckupsInfoAcquisitionTime"),
            Map("PICF", "PharmaceuticalInfoConsFlg"),
            Map("PICT", "PharmaceuticalInfoConsTime"),
            Map("PIAT", "PharmaceuticalInfoAcquisitionTime"),
            Map("DICF", "DiagnosisInfoConsFlg"),
            Map("DICT", "DiagnosisInfoConsTime"),
            Map("DIAT", "DiagnosisInfoAcquisitionTime"),
            Map("OICF", "OperationInfoConsFlg"),
            Map("OICT", "OperationInfoConsTime"),
            Map("OIAT", "OperationInfoAcquisitionTime"),
            Map("RN", "ReferenceNumber")
        };

        public static readonly string[] BulkColumns = BulkColumnMappings.Select(m => m.ColumnName).ToArray();

        public static IEnumerable<string> AllColumns()
        {
            yield return "import_kind";
            yield return "source_file_path";
            yield return "extracted_xml_path";
            yield return "sequence_no";
            yield return "match_status";
            yield return "sent_to_face";
            yield return "face_output_message";
            yield return "created_at";
            yield return "updated_at";

            foreach (BulkColumnMapping mapping in BulkColumnMappings)
            {
                yield return mapping.ColumnName;
            }
        }

        public static bool IsBooleanColumn(string columnName)
        {
            return string.Equals(columnName, "sent_to_face", StringComparison.OrdinalIgnoreCase);
        }

        public static string GetPostgresColumnType(string columnName)
        {
            if (string.Equals(columnName, "created_at", StringComparison.OrdinalIgnoreCase)
                || string.Equals(columnName, "updated_at", StringComparison.OrdinalIgnoreCase))
            {
                return "timestamp without time zone";
            }

            if (string.Equals(columnName, "sequence_no", StringComparison.OrdinalIgnoreCase)
                || string.Equals(columnName, "karte_number", StringComparison.OrdinalIgnoreCase))
            {
                return "bigint";
            }

            if (string.Equals(columnName, "medical_type_code", StringComparison.OrdinalIgnoreCase)
                || string.Equals(columnName, "medical_ticket_branch", StringComparison.OrdinalIgnoreCase)
                || string.Equals(columnName, "confirmation_type_patient", StringComparison.OrdinalIgnoreCase)
                || string.Equals(columnName, "confirmation_type_branch", StringComparison.OrdinalIgnoreCase)
                || string.Equals(columnName, "NumberOfMedicalTickets", StringComparison.OrdinalIgnoreCase))
            {
                return "integer";
            }

            if (IsBooleanColumn(columnName))
            {
                return "boolean";
            }

            return "text";
        }

        public static string GetColumnForAlias(string alias)
        {
            BulkColumnMapping mapping = BulkColumnMappings.FirstOrDefault(m => string.Equals(m.Alias, alias, StringComparison.OrdinalIgnoreCase));
            return mapping == null ? alias : mapping.ColumnName;
        }

        public static string GetAliasForColumn(string columnName)
        {
            BulkColumnMapping mapping = BulkColumnMappings.FirstOrDefault(m => string.Equals(m.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));
            return mapping == null ? columnName : mapping.Alias;
        }

        private static BulkColumnMapping Map(string alias, string columnName)
        {
            return new BulkColumnMapping(alias, columnName);
        }
    }

    internal sealed class QualificationSendSummary
    {
        public int SentCount { get; set; }
        public int FailedCount { get; set; }
    }

    internal static class QualificationImportParser
    {
        public static QualificationImportSession Parse(BulkQualificationKind kind, BulkQualificationImportResult importResult)
        {
            var session = new QualificationImportSession
            {
                Kind = kind,
                SourceFilePath = importResult.SourceFilePath ?? string.Empty,
                ExtractedXmlPath = importResult.ExtractedXmlPath ?? string.Empty,
                WorkingDirectory = importResult.WorkingDirectory ?? string.Empty,
                ImportedAt = DateTime.Now
            };

            var xml = new XmlDocument();
            xml.Load(importResult.ExtractedXmlPath);

            XmlNode headerNode = xml.SelectSingleNode("//*[local-name()='MessageHeader']");
            XmlNode bodyNode = xml.SelectSingleNode("//*[local-name()='MessageBody']");
            if (bodyNode == null)
            {
                return session;
            }

            string processExecutedAt = NormalizeDateTimeString(FindValue(headerNode, "ProcessExecutionTime"));
            if (string.IsNullOrWhiteSpace(processExecutedAt))
            {
                processExecutedAt = session.ImportedAt.ToString("yyyyMMddHHmmss");
            }

            string medicalInstitutionCode = FirstNonEmpty(
                FindValue(headerNode, "MedicalInstitutionCode"),
                Properties.Settings.Default.MCode);
            string fileIdentifier = FirstNonEmpty(
                FindValue(headerNode, "ArbitraryFileIdentifier", "ArbitraryIdentifier"),
                Path.GetFileNameWithoutExtension(importResult.ExtractedXmlPath));
            string characterCodeIdentifier = FindValue(headerNode, "CharacterCodeIdentifier");
            string processStatus = FindValue(bodyNode, "ProcessingResultStatus");
            string processCode = FindValue(bodyNode, "ProcessingResultCode");
            string processMessage = FindValue(bodyNode, "ProcessingResultMessage", "MessageContents", "Message");

            int sequence = 1;
            foreach (XmlNode bulkConfirmUnit in bodyNode.ChildNodes.Cast<XmlNode>().Where(IsElementNamed("BulkConfirmUnit")))
            {
                List<string> referenceNumbers = bulkConfirmUnit.ChildNodes
                    .Cast<XmlNode>()
                    .Where(IsElementNamed("ReferenceNumber"))
                    .Select(node => (node.InnerText ?? string.Empty).Trim())
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .ToList();

                string qualificationValidity = FindValue(bulkConfirmUnit, "QualificationValidity");
                string unitStatus = FirstNonEmpty(FindValue(bulkConfirmUnit, "ProcessingResultStatus"), processStatus);
                string unitCode = FirstNonEmpty(FindValue(bulkConfirmUnit, "ProcessingResultCode"), processCode);
                string unitMessage = FirstNonEmpty(FindValue(bulkConfirmUnit, "ProcessingResultMessage", "MessageContents", "Message"), processMessage);

                List<ImportedQualificationRecord> unitRecords = ParseBulkConfirmUnitResults(
                    kind,
                    session,
                    bulkConfirmUnit,
                    referenceNumbers,
                    qualificationValidity,
                    unitStatus,
                    unitCode,
                    unitMessage,
                    processExecutedAt,
                    medicalInstitutionCode,
                    fileIdentifier,
                    characterCodeIdentifier,
                    ref sequence);

                int confirmationType = CalculateConfirmationType(unitRecords);
                for (int index = 0; index < unitRecords.Count; index++)
                {
                    ImportedQualificationRecord record = unitRecords[index];
                    record.BulkToolValues["MEDTYP"] = GetMedicalTypeCode(kind);
                    record.BulkToolValues["MTBR"] = index;
                    record.BulkToolValues["CNFTYPPT"] = confirmationType;
                    PopulateDynamicsValues(record);
                    session.Records.Add(record);
                }
            }

            return session;
        }

        private static void PopulateDynamicsValues(ImportedQualificationRecord record)
        {
            record.DynamicsValues[QualificationColumnNames.ProcessExecutedAt] = record.ImportedAt;
            record.DynamicsValues[QualificationColumnNames.ArbitraryFileIdentifier] = record.XmlIdentifier;
            record.DynamicsValues[QualificationColumnNames.ProcessStatus] = record.ProcessingResultStatus;
            record.DynamicsValues[QualificationColumnNames.ProcessCode] = record.ProcessingResultCode;
            record.DynamicsValues[QualificationColumnNames.ProcessMessage] = record.ProcessingResultMessage;
            record.DynamicsValues[QualificationColumnNames.QualificationValidity] = record.QualificationValidity;
            record.DynamicsValues[QualificationColumnNames.InsurerNumber] = record.InsurerNumber;
            record.DynamicsValues[QualificationColumnNames.CardSymbol] = record.Symbol;
            record.DynamicsValues[QualificationColumnNames.CardNumber] = record.Number;
            record.DynamicsValues[QualificationColumnNames.CardBranchNumber] = record.BranchNumber;
            record.DynamicsValues[QualificationColumnNames.Name] = record.Name;
            record.DynamicsValues[QualificationColumnNames.KanaName] = record.KanaName;
            record.DynamicsValues[QualificationColumnNames.BirthDate] = record.BirthDate;
            record.DynamicsValues[QualificationColumnNames.Address] = record.Address;
            record.DynamicsValues[QualificationColumnNames.PostalCode] = record.PostalCode;
            record.DynamicsValues[QualificationColumnNames.QueryNumber] = record.QueryNumber;
            record.DynamicsValues[QualificationColumnNames.QueryCategory] = record.InquiryClassification;
            record.DynamicsValues[QualificationColumnNames.QueryCategoryDisplay] = record.InquiryClassificationDisplay;
            record.DynamicsValues[QualificationColumnNames.QualificationStartDate] = record.QualificationStartDate;
            record.DynamicsValues[QualificationColumnNames.QualificationEndDate] = record.QualificationEndDate;
            record.DynamicsValues[QualificationColumnNames.SexCode] = ParseSexCode(record.SexCode);
            record.DynamicsValues[QualificationColumnNames.BirthDateSeireki] = record.BirthDate;
            record.DynamicsValues[QualificationColumnNames.ReceptionSent] = false;
            record.DynamicsValues[QualificationColumnNames.Hidden] = false;
        }

        private static List<ImportedQualificationRecord> ParseBulkConfirmUnitResults(
            BulkQualificationKind kind,
            QualificationImportSession session,
            XmlNode bulkConfirmUnit,
            IList<string> referenceNumbers,
            string qualificationValidity,
            string processStatus,
            string processCode,
            string processMessage,
            string processExecutedAt,
            string medicalInstitutionCode,
            string fileIdentifier,
            string characterCodeIdentifier,
            ref int sequence)
        {
            var resultNodes = bulkConfirmUnit.SelectNodes(".//*[local-name()='ResultOfQualificationConfirmation']");
            var records = new List<ImportedQualificationRecord>();
            if (resultNodes == null)
            {
                return records;
            }

            int resultIndex = 0;
            foreach (XmlNode resultNode in resultNodes)
            {
                var record = new ImportedQualificationRecord
                {
                    Sequence = sequence++,
                    Kind = kind,
                    SourceFilePath = session.SourceFilePath,
                    ImportedAt = session.ImportedAt,
                    XmlIdentifier = fileIdentifier,
                    ProcessingResultStatus = processStatus,
                    ProcessingResultCode = processCode,
                    ProcessingResultMessage = processMessage,
                    QualificationValidity = qualificationValidity,
                    InquiryClassification = GetInquiryClassification(kind),
                    InquiryClassificationDisplay = GetInquiryClassificationDisplay(kind),
                    QueryNumber = resultIndex < referenceNumbers.Count ? referenceNumbers[resultIndex] : referenceNumbers.FirstOrDefault() ?? string.Empty
                };

                PopulateRecordFromQualificationResult(record, resultNode);
                record.BulkToolValues["PET"] = processExecutedAt;
                record.BulkToolValues["QCD"] = processExecutedAt.Length >= 8 ? processExecutedAt.Substring(0, 8) : processExecutedAt;
                record.BulkToolValues["MIC"] = medicalInstitutionCode;
                record.BulkToolValues["AFI"] = fileIdentifier;
                record.BulkToolValues["RC"] = GetInquiryClassification(kind);
                record.BulkToolValues["SOR"] = "1";
                record.BulkToolValues["CCI"] = characterCodeIdentifier;
                record.BulkToolValues["PRS"] = processStatus;
                if (!string.IsNullOrWhiteSpace(record.QueryNumber))
                {
                    record.BulkToolValues["RN"] = record.QueryNumber;
                }

                records.Add(record);
                resultIndex++;
            }

            return records;
        }

        private static int GetMedicalTypeCode(BulkQualificationKind kind)
        {
            switch (kind)
            {
                case BulkQualificationKind.MedicalAid:
                    return 1;
                case BulkQualificationKind.Houmon:
                    return 2;
                default:
                    return 3;
            }
        }

        private static string GetInquiryClassification(BulkQualificationKind kind)
        {
            switch (kind)
            {
                case BulkQualificationKind.MedicalAid:
                    return "A";
                case BulkQualificationKind.Houmon:
                    return "B";
                default:
                    return "C";
            }
        }

        private static string GetInquiryClassificationDisplay(BulkQualificationKind kind)
        {
            switch (kind)
            {
                case BulkQualificationKind.MedicalAid:
                    return "Fujyo";
                case BulkQualificationKind.Houmon:
                    return "Houmon";
                default:
                    return "Online";
            }
        }

        private static void PopulateRecordFromQualificationResult(ImportedQualificationRecord record, XmlNode resultNode)
        {
            string insuredCardClassification = FindValue(resultNode, "InsuredCardClassification");
            string insurerNumber = FirstNonEmpty(
                FindValue(resultNode, "InsurerNumber"),
                FindValue(resultNode, "PublicExpenseNumber"));
            bool isPublicAid = string.Equals(insuredCardClassification, "A1", StringComparison.OrdinalIgnoreCase)
                || insurerNumber.StartsWith("12", StringComparison.Ordinal);

            record.InsurerNumber = insurerNumber;
            record.Symbol = FindValue(resultNode, "InsuredCardSymbol");
            record.Number = FirstNonEmpty(
                FindValue(resultNode, "InsuredIdentificationNumber"),
                FindValue(resultNode, "BeneficiaryNumber"));
            record.BranchNumber = FindValue(resultNode, "InsuredBranchNumber");
            record.Name = FindValue(resultNode, "Name");
            record.KanaName = FindValue(resultNode, "NameKana");
            record.BirthDate = NormalizeDateString(FindValue(resultNode, "Birthdate"));
            record.Address = FindValue(resultNode, "Address");
            record.PostalCode = FindValue(resultNode, "PostNumber");
            record.QualificationStartDate = NormalizeDateString(FindValue(resultNode, "QualificationDate"));
            record.QualificationEndDate = NormalizeDateString(FindValue(resultNode, "DisqualificationDate"));
            record.SexCode = FirstNonEmpty(FindValue(resultNode, "Sex1"), FindValue(resultNode, "Sex2"));

            record.BulkToolValues["CNFTYPBR"] = isPublicAid ? 2 : 1;
            record.BulkToolValues["QV"] = record.QualificationValidity;
            record.BulkToolValues["INSCC"] = insuredCardClassification;
            AddBulkValue(record, "INSN", insurerNumber);
            AddBulkValue(record, "INSCS", record.Symbol);
            AddBulkValue(record, "INSIN", record.Number);
            AddBulkValue(record, "INSBN", record.BranchNumber);
            AddBulkValue(record, "KFB", insurerNumber);
            AddBulkValue(record, "KJB", record.Number);
            AddBulkValue(record, "PFC", FindValue(resultNode, "PersonalFamilyClassification"));
            AddBulkValue(record, "IEDN", FindValue(resultNode, "InsuredName"));
            AddBulkValue(record, "NPT", record.Name);
            AddBulkValue(record, "NOO", FindValue(resultNode, "NameOfOther"));
            AddBulkValue(record, "NK", record.KanaName);
            AddBulkValue(record, "NOOK", FindValue(resultNode, "NameOfOtherKana"));
            AddBulkValue(record, "S1", FindValue(resultNode, "Sex1"));
            AddBulkValue(record, "S2", FindValue(resultNode, "Sex2"));
            AddBulkValue(record, "BD", record.BirthDate);
            AddBulkValue(record, "AD", record.Address);
            AddBulkValue(record, "PN", record.PostalCode);
            AddBulkValue(record, "QD", record.QualificationStartDate);
            AddBulkValue(record, "DD", record.QualificationEndDate);
            AddBulkValue(record, "ICID", NormalizeDateString(FindValue(resultNode, "InsuredCertificateIssuanceDate")));
            AddBulkValue(record, "ICVD", NormalizeDateString(FindValue(resultNode, "InsuredCardValidDate")));
            AddBulkValue(record, "ICED", NormalizeDateString(FindValue(resultNode, "InsuredCardExpirationDate")));
            AddBulkValue(record, "IPCR", FindValue(resultNode, "InsuredPartialContributionRatio"));
            AddBulkValue(record, "PC", FindValue(resultNode, "PreschoolClassification"));
            AddBulkValue(record, "ROL", FindValue(resultNode, "ReasonOfLoss"));
            AddBulkValue(record, "IERN", FirstNonEmpty(FindValue(resultNode, "InsurerName"), FindValue(resultNode, "WelfareOfficeName")));
            AddBulkValue(record, "JFN", FindValue(resultNode, "WelfareOfficeName"));
            AddBulkValue(record, "ERCD", NormalizeDateString(FindValue(resultNode, "ElderlyRecipientCertificateDate")));
            AddBulkValue(record, "ERVSD", NormalizeDateString(FindValue(resultNode, "ElderlyRecipientValidStartDate")));
            AddBulkValue(record, "ERVED", NormalizeDateString(FindValue(resultNode, "ElderlyRecipientValidEndDate")));
            AddBulkValue(record, "ERCR", FindValue(resultNode, "ElderlyRecipientContributionRatio"));
            AddBulkValue(record, "LACRCF", FindValue(resultNode, "LimitApplicationCertificateRelatedConsFlg"));
            AddBulkValue(record, "LACRCT", NormalizeDateTimeString(FindValue(resultNode, "LimitApplicationCertificateRelatedConsTime")));
            AddBulkValue(record, "LACC", FindValue(resultNode, "LimitApplicationCertificateClassification"));
            AddBulkValue(record, "LACCF", FindValue(resultNode, "LimitApplicationCertificateClassificationFlag"));
            AddBulkValue(record, "LACD", NormalizeDateString(FindValue(resultNode, "LimitApplicationCertificateDate")));
            AddBulkValue(record, "LACVSD", NormalizeDateString(FindValue(resultNode, "LimitApplicationCertificateValidStartDate")));
            AddBulkValue(record, "LACVED", NormalizeDateString(FindValue(resultNode, "LimitApplicationCertificateValidEndDate")));
            AddBulkValue(record, "LACLTD", NormalizeDateString(FindValue(resultNode, "LimitApplicationCertificateLongTermDate")));
            AddBulkValue(record, "SDCRCF", FindValue(resultNode, "SpecificDiseasesCertificateRelatedConsFlg"));
            AddBulkValue(record, "SDCRCT", NormalizeDateTimeString(FindValue(resultNode, "SpecificDiseasesCertificateRelatedConsTime")));
            AddBulkValue(record, "SHCICF", FindValue(resultNode, "SpecificHealthCheckupsInfoConsFlg"));
            AddBulkValue(record, "SHCICT", NormalizeDateTimeString(FindValue(resultNode, "SpecificHealthCheckupsInfoConsTime")));
            AddBulkValue(record, "SHCIAT", NormalizeDateTimeString(FindValue(resultNode, "SpecificHealthCheckupsInfoAvailableTime")));
            AddBulkValue(record, "PICF", FindValue(resultNode, "PharmacistsInfoConsFlg"));
            AddBulkValue(record, "PICT", NormalizeDateTimeString(FindValue(resultNode, "PharmacistsInfoConsTime")));
            AddBulkValue(record, "PIAT", NormalizeDateTimeString(FindValue(resultNode, "PharmacistsInfoAvailableTime")));
            AddBulkValue(record, "DICF", FindValue(resultNode, "DiagnosisInfoConsFlg"));
            AddBulkValue(record, "DICT", NormalizeDateTimeString(FindValue(resultNode, "DiagnosisInfoConsTime")));
            AddBulkValue(record, "DIAT", NormalizeDateTimeString(FindValue(resultNode, "DiagnosisInfoAvailableTime")));
            AddBulkValue(record, "OICF", FindValue(resultNode, "OperationInfoConsFlg"));
            AddBulkValue(record, "OICT", NormalizeDateTimeString(FindValue(resultNode, "OperationInfoConsTime")));
            AddBulkValue(record, "OIAT", NormalizeDateTimeString(FindValue(resultNode, "OperationInfoAvailableTime")));

            PopulateSpecificDiseasesValues(record, resultNode);
            PopulateMedicalTicketValues(record, resultNode);
        }

        private static void PopulateSpecificDiseasesValues(ImportedQualificationRecord record, XmlNode resultNode)
        {
            XmlNodeList certificateNodes = resultNode.SelectNodes(".//*[local-name()='SpecificDiseasesCertificateInfo']");
            if (certificateNodes == null || certificateNodes.Count == 0)
            {
                return;
            }

            AddBulkValue(record, "SDDC", JoinChildValues(certificateNodes, "SpecificDiseasesDiseaseCategory"));
            AddBulkValue(record, "SDCD", JoinChildValues(certificateNodes, "SpecificDiseasesCertificateDate", true));
            AddBulkValue(record, "SDVSD", JoinChildValues(certificateNodes, "SpecificDiseasesValidStartDate", true));
            AddBulkValue(record, "SDVED", JoinChildValues(certificateNodes, "SpecificDiseasesValidEndDate", true));
            AddBulkValue(record, "SDSP", JoinChildValues(certificateNodes, "SpecificDiseasesSelfPay"));
        }

        private static void PopulateMedicalTicketValues(ImportedQualificationRecord record, XmlNode resultNode)
        {
            XmlNodeList ticketNodes = resultNode.SelectNodes(".//*[local-name()='MedicalTicketInfo']");
            if (ticketNodes == null || ticketNodes.Count == 0)
            {
                return;
            }

            record.BulkToolValues["NOFT"] = ticketNodes.Count;
            AddBulkValue(record, "TT", JoinChildValues(ticketNodes, "TicketType"));
            AddBulkValue(record, "MTVD", JoinChildValues(ticketNodes, "MedicalTicketValidDate", true));
            AddBulkValue(record, "MTED", JoinChildValues(ticketNodes, "MedicalTicketExpirationDate", true));
            AddBulkValue(record, "MTIN", JoinChildValues(ticketNodes, "IssueNumber"));
            AddBulkValue(record, "MTM", JoinChildValues(ticketNodes, "MedicalTreatmentMonth"));
            AddBulkValue(record, "DMIC", JoinChildValues(ticketNodes, "DesignatedMedicalInstitutionCode"));
            AddBulkValue(record, "DMIF", JoinChildValues(ticketNodes, "DesignatedMedicalInstitutionFlag"));
            AddBulkValue(record, "DMIN", JoinChildValues(ticketNodes, "DesignatedMedicalInstitutionName"));
            AddBulkValue(record, "PIMIC", JoinChildValues(ticketNodes, "PrescriptionIssuerMedicalInstitutionCode"));
            AddBulkValue(record, "PIMIN", JoinChildValues(ticketNodes, "PrescriptionIssuerMedicalInstitutionName"));
            AddBulkValue(record, "INJN1", JoinChildValues(ticketNodes, "InjuryName1"));
            AddBulkValue(record, "INJN2", JoinChildValues(ticketNodes, "InjuryName2"));
            AddBulkValue(record, "INJN3", JoinChildValues(ticketNodes, "InjuryName3"));
            AddBulkValue(record, "MTT", JoinChildValues(ticketNodes, "MedicalTreatmentType"));
            AddBulkValue(record, "SPA", JoinChildValues(ticketNodes, "SelfPayAmount"));
            AddBulkValue(record, "DCN", JoinChildValues(ticketNodes, "DistrictContactName"));
            AddBulkValue(record, "HCN", JoinChildValues(ticketNodes, "HandlingContactName"));
            AddBulkValue(record, "SOCU", JoinChildValues(ticketNodes, "SingleOrCombinedUse"));
            AddBulkValue(record, "SOSI", JoinChildValues(ticketNodes, "StatusOfSocialInsurance"));
            AddBulkValue(record, "CF", JoinChildValues(ticketNodes, "ConsistencyFlag"));
            AddBulkValue(record, "SOI", JoinChildValues(ticketNodes, "StatusOfInfecton"));
            AddBulkValue(record, "SOEMC", JoinChildValues(ticketNodes, "StatusOfElderlyMedicalCare"));
            AddBulkValue(record, "SOPE", JoinChildValues(ticketNodes, "StatusOfPrefecturalExpenses"));
            AddBulkValue(record, "R1", JoinChildValues(ticketNodes, "Remarks1"));
            AddBulkValue(record, "R2", JoinChildValues(ticketNodes, "Remarks2"));
            AddBulkValue(record, "R3", JoinChildValues(ticketNodes, "Remarks3"));
        }

        private static int CalculateConfirmationType(IList<ImportedQualificationRecord> records)
        {
            bool hasInsurance = records.Any(r => ReadBulkInt(r, "CNFTYPBR") == 1);
            bool hasPublicAid = records.Any(r => ReadBulkInt(r, "CNFTYPBR") == 2);
            if (hasInsurance && hasPublicAid)
            {
                return 3;
            }

            if (hasPublicAid)
            {
                return 2;
            }

            return 1;
        }

        private static short ParseSexCode(string sexCode)
        {
            short parsed;
            return short.TryParse(sexCode, out parsed) ? parsed : (short)0;
        }

        private static string FindValue(XmlNode node, params string[] names)
        {
            if (node == null || names == null)
            {
                return string.Empty;
            }

            foreach (string name in names)
            {
                foreach (XmlNode descendant in EnumerateSelfAndDescendants(node))
                {
                    if (descendant.NodeType != XmlNodeType.Element)
                    {
                        continue;
                    }

                    if (string.Equals(descendant.LocalName, name, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(descendant.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        return (descendant.InnerText ?? string.Empty).Trim();
                    }
                }
            }

            return string.Empty;
        }

        private static IEnumerable<XmlNode> EnumerateSelfAndDescendants(XmlNode node)
        {
            if (node == null)
            {
                yield break;
            }

            yield return node;
            foreach (XmlNode child in node.ChildNodes)
            {
                foreach (XmlNode descendant in EnumerateSelfAndDescendants(child))
                {
                    yield return descendant;
                }
            }
        }

        private static string NormalizeDateString(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string digits = new string(value.Where(char.IsDigit).ToArray());
            return digits.Length >= 8 ? digits.Substring(0, 8) : value.Trim();
        }

        private static string NormalizeDateTimeString(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string digits = new string(value.Where(char.IsDigit).ToArray());
            return digits.Length >= 14 ? digits.Substring(0, 14) : digits;
        }

        private static string FirstNonEmpty(params string[] values)
        {
            return values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim() ?? string.Empty;
        }

        private static Func<XmlNode, bool> IsElementNamed(string name)
        {
            return node => node != null
                && node.NodeType == XmlNodeType.Element
                && string.Equals(node.LocalName, name, StringComparison.OrdinalIgnoreCase);
        }

        private static string JoinChildValues(XmlNodeList nodes, string childName, bool normalizeDate = false)
        {
            var values = new List<string>();
            foreach (XmlNode node in nodes.Cast<XmlNode>())
            {
                string value = FindValue(node, childName);
                if (normalizeDate)
                {
                    value = NormalizeDateString(value);
                }

                if (!string.IsNullOrWhiteSpace(value))
                {
                    values.Add(value.Trim());
                }
            }

            return string.Join(" / ", values);
        }

        private static void AddBulkValue(ImportedQualificationRecord record, string columnName, string value)
        {
            if (string.IsNullOrWhiteSpace(columnName) || string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            record.BulkToolValues[columnName] = value.Trim();
        }

        private static int ReadBulkInt(ImportedQualificationRecord record, string columnName)
        {
            if (record == null || !record.BulkToolValues.TryGetValue(columnName, out object value) || value == null)
            {
                return 0;
            }

            int parsed;
            return int.TryParse(value.ToString(), out parsed) ? parsed : 0;
        }
    }

    internal static class QualificationDynamicsSender
    {
        public static void ResolvePatientMatches(IList<ImportedQualificationRecord> records, DataTable dynaTable)
        {
            if (records == null || dynaTable == null || dynaTable.Rows.Count == 0)
            {
                return;
            }

            foreach (ImportedQualificationRecord record in records)
            {
                DataRow matchedRow = FindMatchingRow(record, dynaTable);
                if (matchedRow == null)
                {
                    record.MatchedPatientId = 0;
                    record.MatchedPatientName = string.Empty;
                    record.MatchStatus = "NoMatch";
                    continue;
                }

                long patientId = ReadInt64(matchedRow, QualificationColumnNames.KarteNumber);
                record.MatchedPatientId = patientId;
                record.MatchedPatientName = ReadString(matchedRow, QualificationColumnNames.Name);
                record.MatchStatus = patientId > 0 ? "Matched" : "NoKarte";

                record.DynamicsValues[QualificationColumnNames.KarteNumber] = patientId;
                record.DynamicsValues[QualificationColumnNames.PatientMasterName] = record.MatchedPatientName;
                record.DynamicsValues[QualificationColumnNames.PatientMasterQueryNumber] = ReadString(matchedRow, QualificationColumnNames.PatientMasterQueryNumber);
                record.DynamicsValues[QualificationColumnNames.SymbolShort] = ReadString(matchedRow, QualificationColumnNames.SymbolShort, QualificationColumnNames.CardSymbol);
                record.DynamicsValues[QualificationColumnNames.NumberShort] = ReadString(matchedRow, QualificationColumnNames.NumberShort, QualificationColumnNames.CardNumber);
                record.DynamicsValues[QualificationColumnNames.PatientMasterInsurerNumber] = ReadString(matchedRow, QualificationColumnNames.PatientMasterInsurerNumber, QualificationColumnNames.InsurerNumber);
                record.DynamicsValues[QualificationColumnNames.BranchShort] = ReadString(matchedRow, QualificationColumnNames.BranchShort, QualificationColumnNames.CardBranchNumber);
                record.DynamicsValues[QualificationColumnNames.BirthDateSeireki] = ReadString(matchedRow, QualificationColumnNames.BirthDateSeireki);
            }
        }

        private static DataRow FindMatchingRow(ImportedQualificationRecord record, DataTable dynaTable)
        {
            IEnumerable<DataRow> rows = dynaTable.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(record.InsurerNumber)
                || !string.IsNullOrWhiteSpace(record.Symbol)
                || !string.IsNullOrWhiteSpace(record.Number))
            {
                IEnumerable<DataRow> insuranceMatches = rows.Where(row =>
                    EqualsColumn(row, QualificationColumnNames.InsurerNumber, record.InsurerNumber)
                    && EqualsColumn(row, QualificationColumnNames.CardSymbol, record.Symbol, QualificationColumnNames.SymbolShort)
                    && EqualsColumn(row, QualificationColumnNames.CardNumber, record.Number, QualificationColumnNames.NumberShort)
                    && EqualsColumn(row, QualificationColumnNames.CardBranchNumber, record.BranchNumber, QualificationColumnNames.BranchShort));

                DataRow insuranceRow = SelectBestMatch(insuranceMatches);
                if (insuranceRow != null)
                {
                    return insuranceRow;
                }
            }

            if (!string.IsNullOrWhiteSpace(record.Name) && !string.IsNullOrWhiteSpace(record.BirthDate))
            {
                IEnumerable<DataRow> nameMatches = rows.Where(row =>
                    EqualsColumn(row, QualificationColumnNames.Name, record.Name)
                    && EqualsColumn(row, QualificationColumnNames.BirthDateSeireki, record.BirthDate, QualificationColumnNames.BirthDate));

                DataRow nameRow = SelectBestMatch(nameMatches);
                if (nameRow != null)
                {
                    return nameRow;
                }
            }

            return null;
        }

        private static DataRow SelectBestMatch(IEnumerable<DataRow> rows)
        {
            return rows
                .Where(row => ReadInt64(row, QualificationColumnNames.KarteNumber) > 0)
                .OrderByDescending(row => ReadInt64(row, QualificationColumnNames.KarteNumber))
                .FirstOrDefault();
        }

        private static bool EqualsColumn(DataRow row, string primaryColumn, string expected, params string[] alternateColumns)
        {
            if (string.IsNullOrWhiteSpace(expected))
            {
                return true;
            }

            string actual = ReadString(row, primaryColumn, alternateColumns);
            return string.Equals(NormalizeKey(actual), NormalizeKey(expected), StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return new string(value.Trim().Where(ch => !char.IsWhiteSpace(ch) && ch != '-' && ch != '\uFF0D').ToArray());
        }

        private static string ReadString(DataRow row, string primaryColumn, params string[] alternateColumns)
        {
            IEnumerable<string> columns = new[] { primaryColumn }.Concat(alternateColumns ?? Array.Empty<string>());
            foreach (string column in columns)
            {
                if (!row.Table.Columns.Contains(column) || row[column] == DBNull.Value)
                {
                    continue;
                }

                return row[column].ToString().Trim();
            }

            return string.Empty;
        }

        private static long ReadInt64(DataRow row, string columnName)
        {
            if (!row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
            {
                return 0;
            }

            long value;
            return long.TryParse(row[columnName].ToString(), out value) ? value : 0;
        }
    }

    internal static class QualificationImportStore
    {
        public static async Task EnsureStorageTableAsync(Func<string, Task> logAsync = null)
        {
            if (!string.Equals(Properties.Settings.Default.DBtype, "pg", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await EnsurePostgresStorageTableAsync(logAsync).ConfigureAwait(false);
        }

        public static async Task SaveSessionAsync(QualificationImportSession session, Func<string, Task> logAsync = null)
        {
            if (session == null || session.Records.Count == 0)
            {
                return;
            }

            await EnsureStorageTableAsync(logAsync).ConfigureAwait(false);
            if (!string.Equals(Properties.Settings.Default.DBtype, "pg", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await SaveSessionToPostgresAsync(session, logAsync).ConfigureAwait(false);
        }

        public static async Task<QualificationImportSession> LoadLatestSessionAsync(BulkQualificationKind kind, Func<string, Task> logAsync = null)
        {
            if (!string.Equals(Properties.Settings.Default.DBtype, "pg", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            await EnsureStorageTableAsync(logAsync).ConfigureAwait(false);
            return await LoadLatestSessionFromPostgresAsync(kind, logAsync).ConfigureAwait(false);
        }

        public static async Task<QualificationImportSession> LoadRecentSessionAsync(int maxRecords, Func<string, Task> logAsync = null)
        {
            if (!string.Equals(Properties.Settings.Default.DBtype, "pg", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            await EnsureStorageTableAsync(logAsync).ConfigureAwait(false);
            return await LoadRecentSessionFromPostgresAsync(Math.Max(1, maxRecords), logAsync).ConfigureAwait(false);
        }

        public static async Task UpdateSendResultsAsync(IReadOnlyList<ImportedQualificationRecord> records, Func<string, Task> logAsync = null)
        {
            if (records == null || records.Count == 0)
            {
                return;
            }

            var targetRecords = records.Where(r => r != null && r.StorageId > 0).ToList();
            if (targetRecords.Count == 0)
            {
                return;
            }

            if (!string.Equals(Properties.Settings.Default.DBtype, "pg", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await UpdateSendResultsInPostgresAsync(targetRecords, logAsync).ConfigureAwait(false);
        }

        private static async Task EnsurePostgresStorageTableAsync(Func<string, Task> logAsync)
        {
            using (var connection = CommonFunctions.GetDbConnection(false))
            {
                await OpenAsync(connection).ConfigureAwait(false);

                var columnDefs = new List<string>
                {
                    "id bigserial primary key"
                };

                foreach (string column in BulkToolUploadSchema.AllColumns())
                {
                    columnDefs.Add($"\"{column}\" {BulkToolUploadSchema.GetPostgresColumnType(column)}");
                }

                string createSql = $"CREATE TABLE IF NOT EXISTS public.{BulkToolUploadSchema.TableName} ({string.Join(", ", columnDefs)})";
                using (IDbCommand command = connection.CreateCommand())
                {
                    command.CommandText = createSql;
                    await ExecuteNonQueryAsync(command).ConfigureAwait(false);
                }

                using (IDbCommand indexCommand = connection.CreateCommand())
                {
                    indexCommand.CommandText =
                        $"CREATE INDEX IF NOT EXISTS ix_{BulkToolUploadSchema.TableName}_afi_rn " +
                        $"ON public.{BulkToolUploadSchema.TableName} (\"ArbitraryFileIdentifier\", \"ReferenceNumber\")";
                    await ExecuteNonQueryAsync(indexCommand).ConfigureAwait(false);
                }
            }

            if (logAsync != null)
            {
                await logAsync($"PG一括資格保存テーブルを確認しました: {BulkToolUploadSchema.TableName}").ConfigureAwait(false);
            }
        }

        private static async Task SaveSessionToPostgresAsync(QualificationImportSession session, Func<string, Task> logAsync)
        {
            List<string> insertColumns = BulkToolUploadSchema.AllColumns().ToList();
            string columnSql = string.Join(", ", insertColumns.Select(c => $"\"{c}\""));
            string parameterSql = string.Join(", ", insertColumns.Select((c, i) => $"@p{i}"));
            string insertSql =
                $"INSERT INTO public.{BulkToolUploadSchema.TableName} ({columnSql}) VALUES ({parameterSql}) RETURNING id";

            using (var connection = CommonFunctions.GetDbConnection(false))
            {
                await OpenAsync(connection).ConfigureAwait(false);

                foreach (ImportedQualificationRecord record in session.Records)
                {
                    using (IDbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = insertSql;

                        for (int index = 0; index < insertColumns.Count; index++)
                        {
                            string column = insertColumns[index];
                            AddParameter(command, $"@p{index}", ToDbValue(column, GetRecordValue(session, record, column)));
                        }

                        object id = await ExecuteScalarAsync(command).ConfigureAwait(false);
                        record.StorageId = id == null || id == DBNull.Value ? 0 : Convert.ToInt64(id);
                    }
                }
            }

            if (logAsync != null)
            {
                await logAsync($"PG一括資格取込データを保存しました: {session.Records.Count}件").ConfigureAwait(false);
            }
        }

        private static async Task<QualificationImportSession> LoadLatestSessionFromPostgresAsync(BulkQualificationKind kind, Func<string, Task> logAsync)
        {
            string importKind = kind.ToString();
            string latestSql = $@"
SELECT created_at
FROM public.{BulkToolUploadSchema.TableName}
WHERE import_kind = @p0
ORDER BY created_at DESC
LIMIT 1";

            DateTime? latestCreatedAt = null;
            using (var connection = CommonFunctions.GetDbConnection(false))
            {
                await OpenAsync(connection).ConfigureAwait(false);
                using (IDbCommand command = connection.CreateCommand())
                {
                    command.CommandText = latestSql;
                    AddParameter(command, "@p0", importKind);
                    object result = await ExecuteScalarAsync(command).ConfigureAwait(false);
                    if (result != null && result != DBNull.Value)
                    {
                        latestCreatedAt = Convert.ToDateTime(result);
                    }
                }
            }

            if (!latestCreatedAt.HasValue)
            {
                if (logAsync != null)
                {
                    await logAsync($"{kind} の保存済み資格情報は見つかりませんでした").ConfigureAwait(false);
                }

                return null;
            }

            string loadSql = $@"
SELECT *
FROM public.{BulkToolUploadSchema.TableName}
WHERE import_kind = @p0
  AND created_at = @p1
ORDER BY sequence_no";

            QualificationImportSession session = null;
            using (var connection = CommonFunctions.GetDbConnection(false))
            {
                await OpenAsync(connection).ConfigureAwait(false);
                using (IDbCommand command = connection.CreateCommand())
                {
                    command.CommandText = loadSql;
                    AddParameter(command, "@p0", importKind);
                    AddParameter(command, "@p1", latestCreatedAt.Value);

                    using (DbDataReader reader = await ((DbCommand)command).ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            if (session == null)
                            {
                                session = new QualificationImportSession
                                {
                                    Kind = kind,
                                    SourceFilePath = ReadString(reader, "source_file_path"),
                                    ExtractedXmlPath = ReadString(reader, "extracted_xml_path"),
                                    ImportedAt = latestCreatedAt.Value
                                };
                            }

                            session.Records.Add(MapImportedRecord(reader, kind));
                        }
                    }
                }
            }

            if (session != null && logAsync != null)
            {
                await logAsync($"{kind} の保存済み資格情報を読み込みました: {session.Records.Count}件").ConfigureAwait(false);
            }

            return session;
        }

        private static async Task<QualificationImportSession> LoadRecentSessionFromPostgresAsync(int maxRecords, Func<string, Task> logAsync)
        {
            string loadSql = $@"
SELECT *
FROM public.{BulkToolUploadSchema.TableName}
ORDER BY created_at DESC, id DESC
LIMIT @p0";

            var session = new QualificationImportSession
            {
                Kind = BulkQualificationKind.Houmon,
                SourceFilePath = "Bulk Console",
                ExtractedXmlPath = string.Empty,
                ImportedAt = DateTime.Now
            };

            using (var connection = CommonFunctions.GetDbConnection(false))
            {
                await OpenAsync(connection).ConfigureAwait(false);
                using (IDbCommand command = connection.CreateCommand())
                {
                    command.CommandText = loadSql;
                    AddParameter(command, "@p0", maxRecords);

                    using (DbDataReader reader = await ((DbCommand)command).ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            BulkQualificationKind kind = ParseKind(ReadString(reader, "import_kind"));
                            session.Records.Add(MapImportedRecord(reader, kind));
                        }
                    }
                }
            }

            if (session.Records.Count > 0)
            {
                session.ImportedAt = session.Records.Max(r => r.ImportedAt);
            }

            if (logAsync != null)
            {
                await logAsync($"Bulk保存済み資格情報を読み込みました: {session.Records.Count}件").ConfigureAwait(false);
            }

            return session.Records.Count == 0 ? null : session;
        }

        private static async Task UpdateSendResultsInPostgresAsync(IReadOnlyList<ImportedQualificationRecord> records, Func<string, Task> logAsync)
        {
            string updateSql =
                $"UPDATE public.{BulkToolUploadSchema.TableName} " +
                "SET \"match_status\" = @p0, \"sent_to_face\" = @p1, \"face_output_message\" = @p2, \"updated_at\" = @p3 " +
                "WHERE id = @p4";

            using (var connection = CommonFunctions.GetDbConnection(false))
            {
                await OpenAsync(connection).ConfigureAwait(false);

                foreach (ImportedQualificationRecord record in records)
                {
                    using (IDbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = updateSql;
                        AddParameter(command, "@p0", ToDbValue("match_status", record.MatchStatus));
                        AddParameter(command, "@p1", ToDbValue("sent_to_face", record.IsSent));
                        AddParameter(command, "@p2", ToDbValue("face_output_message", record.LastSendMessage));
                        AddParameter(command, "@p3", DateTime.Now);
                        AddParameter(command, "@p4", record.StorageId);
                        await ExecuteNonQueryAsync(command).ConfigureAwait(false);
                    }
                }
            }

            if (logAsync != null)
            {
                await logAsync($"PG一括資格送信結果を保存しました: {records.Count}件").ConfigureAwait(false);
            }
        }

        private static object GetRecordValue(QualificationImportSession session, ImportedQualificationRecord record, string columnName)
        {
            switch (columnName)
            {
                case "import_kind":
                    return record.Kind.ToString();
                case "source_file_path":
                    return session.SourceFilePath;
                case "extracted_xml_path":
                    return session.ExtractedXmlPath;
                case "sequence_no":
                    return record.Sequence;
                case "match_status":
                    return record.MatchStatus;
                case "sent_to_face":
                    return record.IsSent;
                case "face_output_message":
                    return record.LastSendMessage;
                case "created_at":
                case "updated_at":
                    return record.ImportedAt;
                default:
                    string alias = BulkToolUploadSchema.GetAliasForColumn(columnName);
                    if (string.Equals(alias, "KARTENO", StringComparison.OrdinalIgnoreCase))
                    {
                        return record.MatchedPatientId > 0 ? (object)record.MatchedPatientId : DBNull.Value;
                    }

                    if (record.BulkToolValues.TryGetValue(alias, out object value)
                        || record.BulkToolValues.TryGetValue(columnName, out value))
                    {
                        return value;
                    }

                    return string.Empty;
            }
        }

        private static object ToDbValue(string columnName, object value)
        {
            if (BulkToolUploadSchema.IsBooleanColumn(columnName))
            {
                if (value == null || value == DBNull.Value)
                {
                    return false;
                }

                bool boolValue;
                return bool.TryParse(value.ToString(), out boolValue) ? (object)boolValue : false;
            }

            if (string.Equals(columnName, "created_at", StringComparison.OrdinalIgnoreCase)
                || string.Equals(columnName, "updated_at", StringComparison.OrdinalIgnoreCase))
            {
                if (value is DateTime dt)
                {
                    return dt;
                }

                DateTime parsed;
                return DateTime.TryParse(Convert.ToString(value), out parsed) ? (object)parsed : DBNull.Value;
            }

            if (string.Equals(columnName, "sequence_no", StringComparison.OrdinalIgnoreCase)
                || string.Equals(columnName, "karte_number", StringComparison.OrdinalIgnoreCase))
            {
                long parsed;
                return long.TryParse(Convert.ToString(value), out parsed) ? (object)parsed : DBNull.Value;
            }

            if (string.Equals(columnName, "medical_type_code", StringComparison.OrdinalIgnoreCase)
                || string.Equals(columnName, "medical_ticket_branch", StringComparison.OrdinalIgnoreCase)
                || string.Equals(columnName, "confirmation_type_patient", StringComparison.OrdinalIgnoreCase)
                || string.Equals(columnName, "confirmation_type_branch", StringComparison.OrdinalIgnoreCase)
                || string.Equals(columnName, "NumberOfMedicalTickets", StringComparison.OrdinalIgnoreCase))
            {
                int parsed;
                return int.TryParse(Convert.ToString(value), out parsed) ? (object)parsed : DBNull.Value;
            }

            if (value == null)
            {
                return string.Empty;
            }

            return Convert.ToString(value) ?? string.Empty;
        }

        private static void AddParameter(IDbCommand command, string name, object value)
        {
            if (command is NpgsqlCommand npgsqlCommand)
            {
                npgsqlCommand.Parameters.AddWithValue(name, value ?? DBNull.Value);
                return;
            }

            DbParameter parameter = ((DbCommand)command).CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        private static ImportedQualificationRecord MapImportedRecord(IDataRecord record, BulkQualificationKind kind)
        {
            var imported = new ImportedQualificationRecord
            {
                StorageId = ReadInt64(record, "id"),
                Sequence = ReadInt32(record, "sequence_no"),
                Kind = kind,
                SourceFilePath = ReadString(record, "source_file_path"),
                XmlIdentifier = ReadMappedString(record, "AFI"),
                ImportedAt = ReadDateTime(record, "created_at"),
                Name = ReadMappedString(record, "NPT"),
                KanaName = ReadMappedString(record, "NK"),
                BirthDate = ReadMappedString(record, "BD"),
                InsurerNumber = ReadMappedString(record, "INSN"),
                Symbol = ReadMappedString(record, "INSCS"),
                Number = ReadMappedString(record, "INSIN"),
                BranchNumber = ReadMappedString(record, "INSBN"),
                QualificationValidity = ReadMappedString(record, "QV"),
                ProcessingResultStatus = ReadMappedString(record, "PRS"),
                QueryNumber = ReadMappedString(record, "RN"),
                Address = ReadMappedString(record, "AD"),
                QualificationStartDate = ReadMappedString(record, "QD"),
                QualificationEndDate = ReadMappedString(record, "DD"),
                MatchStatus = ReadString(record, "match_status"),
                LastSendMessage = ReadString(record, "face_output_message"),
                IsSent = ReadBool(record, "sent_to_face"),
                MatchedPatientId = ReadMappedInt64(record, "KARTENO")
            };

            imported.InquiryClassification = GetInquiryClassification(kind);
            imported.InquiryClassificationDisplay = GetInquiryClassificationDisplay(kind);
            imported.MatchedPatientName = imported.Name;

            foreach (BulkColumnMapping mapping in BulkToolUploadSchema.BulkColumnMappings)
            {
                if (TryGetValue(record, mapping.ColumnName, out object value) && value != null && value != DBNull.Value)
                {
                    imported.BulkToolValues[mapping.ColumnName] = value;
                    imported.BulkToolValues[mapping.Alias] = value;
                }
            }

            if (!imported.BulkToolValues.ContainsKey("KFB"))
            {
                imported.BulkToolValues["KFB"] = imported.InsurerNumber;
            }

            if (!imported.BulkToolValues.ContainsKey("KJB"))
            {
                imported.BulkToolValues["KJB"] = imported.Number;
            }

            if (!imported.BulkToolValues.ContainsKey("JFN") && imported.BulkToolValues.TryGetValue("IERN", out object insurerName))
            {
                imported.BulkToolValues["JFN"] = insurerName;
            }

            return imported;
        }

        private static BulkQualificationKind ParseKind(string value)
        {
            BulkQualificationKind kind;
            return Enum.TryParse(value, true, out kind) ? kind : BulkQualificationKind.Houmon;
        }

        private static bool TryGetValue(IDataRecord record, string columnName, out object value)
        {
            value = null;
            for (int i = 0; i < record.FieldCount; i++)
            {
                if (string.Equals(record.GetName(i), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    value = record.IsDBNull(i) ? null : record.GetValue(i);
                    return true;
                }
            }

            return false;
        }

        private static int GetMedicalTypeCode(BulkQualificationKind kind)
        {
            switch (kind)
            {
                case BulkQualificationKind.MedicalAid:
                    return 1;
                case BulkQualificationKind.Houmon:
                    return 2;
                default:
                    return 3;
            }
        }

        private static string GetInquiryClassification(BulkQualificationKind kind)
        {
            switch (kind)
            {
                case BulkQualificationKind.MedicalAid:
                    return "A";
                case BulkQualificationKind.Houmon:
                    return "B";
                default:
                    return "C";
            }
        }

        private static string GetInquiryClassificationDisplay(BulkQualificationKind kind)
        {
            switch (kind)
            {
                case BulkQualificationKind.MedicalAid:
                    return "Fujyo";
                case BulkQualificationKind.Houmon:
                    return "Houmon";
                default:
                    return "Online";
            }
        }

        private static async Task OpenAsync(IDbConnection connection)
        {
            if (connection is DbConnection dbConnection)
            {
                await dbConnection.OpenAsync().ConfigureAwait(false);
                return;
            }

            connection.Open();
        }

        private static async Task<int> ExecuteNonQueryAsync(IDbCommand command)
        {
            if (command is DbCommand dbCommand)
            {
                return await dbCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            return command.ExecuteNonQuery();
        }

        private static async Task<object> ExecuteScalarAsync(IDbCommand command)
        {
            if (command is DbCommand dbCommand)
            {
                return await dbCommand.ExecuteScalarAsync().ConfigureAwait(false);
            }

            return command.ExecuteScalar();
        }

        private static string ReadMappedString(IDataRecord record, string alias)
        {
            string columnName = BulkToolUploadSchema.GetColumnForAlias(alias);
            if (!TryGetValue(record, columnName, out object value) || value == null || value == DBNull.Value)
            {
                return string.Empty;
            }

            return Convert.ToString(value) ?? string.Empty;
        }

        private static long ReadMappedInt64(IDataRecord record, string alias)
        {
            string value = ReadMappedString(record, alias);
            long parsed;
            return long.TryParse(value, out parsed) ? parsed : 0;
        }

        private static string ReadString(IDataRecord record, string columnName)
        {
            int ordinal = record.GetOrdinal(columnName);
            return record.IsDBNull(ordinal) ? string.Empty : Convert.ToString(record.GetValue(ordinal)) ?? string.Empty;
        }

        private static int ReadInt32(IDataRecord record, string columnName)
        {
            int ordinal = record.GetOrdinal(columnName);
            return record.IsDBNull(ordinal) ? 0 : Convert.ToInt32(record.GetValue(ordinal));
        }

        private static long ReadInt64(IDataRecord record, string columnName)
        {
            int ordinal = record.GetOrdinal(columnName);
            return record.IsDBNull(ordinal) ? 0 : Convert.ToInt64(record.GetValue(ordinal));
        }

        private static DateTime ReadDateTime(IDataRecord record, string columnName)
        {
            int ordinal = record.GetOrdinal(columnName);
            return record.IsDBNull(ordinal) ? DateTime.MinValue : Convert.ToDateTime(record.GetValue(ordinal));
        }

        private static bool ReadBool(IDataRecord record, string columnName)
        {
            int ordinal = record.GetOrdinal(columnName);
            return !record.IsDBNull(ordinal) && Convert.ToBoolean(record.GetValue(ordinal));
        }
    }
}
