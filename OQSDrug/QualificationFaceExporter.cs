using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace OQSDrug
{
    internal sealed class QualificationFaceExporter
    {
        private const string FacePrefix = "OQSsiquc01res_face_";
        private const string MultiValueDelimiter = " / ";

        private readonly string oqsFolder;
        private readonly Func<string, Task> logAsync;

        public QualificationFaceExporter(string oqsFolder, Func<string, Task> logAsync = null)
        {
            this.oqsFolder = oqsFolder ?? string.Empty;
            this.logAsync = logAsync;
        }

        public async Task<QualificationSendSummary> ExportAsync(IReadOnlyList<ImportedQualificationRecord> records)
        {
            var summary = new QualificationSendSummary();
            if (records == null || records.Count == 0)
            {
                return summary;
            }

            string faceFolder = Path.Combine(oqsFolder, "face");
            Directory.CreateDirectory(faceFolder);

            List<ImportedQualificationRecord> targetRecords = records
                .Where(r => r != null)
                .ToList();

            if (targetRecords.Count == 0)
            {
                return summary;
            }

            string medicalInstitutionCode = GetMedicalInstitutionCode(targetRecords);
            int fileCounter = 0;

            foreach (IGrouping<string, ImportedQualificationRecord> group in GroupByKarte(targetRecords))
            {
                List<ImportedQualificationRecord> groupRecords = group.ToList();
                fileCounter++;

                try
                {
                    string filePath = BuildFaceFilePath(faceFolder, medicalInstitutionCode, fileCounter, groupRecords[0].Kind);
                    await WriteFaceFileAsync(filePath, groupRecords, medicalInstitutionCode, fileCounter).ConfigureAwait(false);

                    foreach (ImportedQualificationRecord record in groupRecords)
                    {
                        record.IsSent = true;
                        record.LastSendMessage = $"face出力: {Path.GetFileName(filePath)}";
                        summary.SentCount++;
                    }

                    await LogAsync($"face出力完了: {filePath} ({groupRecords.Count}件)").ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    foreach (ImportedQualificationRecord record in groupRecords)
                    {
                        record.IsSent = false;
                        record.LastSendMessage = ex.Message;
                        summary.FailedCount++;
                    }

                    await LogAsync($"face出力エラー: {ex.Message}").ConfigureAwait(false);
                }
            }

            return summary;
        }

        private async Task WriteFaceFileAsync(
            string filePath,
            IReadOnlyList<ImportedQualificationRecord> records,
            string medicalInstitutionCode,
            int fileCounter)
        {
            ImportedQualificationRecord first = records[0];
            string arbitraryFileIdentifier = GetValue(first, "AFI");
            if (!string.IsNullOrWhiteSpace(arbitraryFileIdentifier))
            {
                arbitraryFileIdentifier += fileCounter.ToString("0000");
            }

            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "\t",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace,
                Encoding = Encoding.GetEncoding(932),
                OmitXmlDeclaration = false
            };

            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = XmlWriter.Create(stream, settings))
            {
                writer.WriteStartDocument(false);
                writer.WriteStartElement("XmlMsg");

                writer.WriteStartElement("MessageHeader");
                WriteElement(writer, "ProcessExecutionTime", GetValue(first, "PET"));
                WriteElement(writer, "QualificationConfirmationDate", GetValue(first, "QCD"));
                WriteElement(writer, "MedicalInstitutionCode", FirstNonEmpty(GetValue(first, "MIC"), medicalInstitutionCode));
                WriteElement(writer, "ArbitraryFileIdentifier", arbitraryFileIdentifier);
                WriteElement(writer, "ReferenceClassification", GetValue(first, "RC"));
                WriteElement(writer, "SegmentOfResult", GetValue(first, "SOR"));
                WriteElement(writer, "CharacterCodeIdentifier", GetValue(first, "CCI"));
                writer.WriteEndElement();

                writer.WriteStartElement("MessageBody");
                WriteElement(writer, "ProcessingResultStatus", GetValue(first, "PRS"));
                WriteElement(writer, "QualificationValidity", GetValue(first, "QV"));

                writer.WriteStartElement("ResultList");
                foreach (ImportedQualificationRecord record in records)
                {
                    WriteQualificationResult(writer, record);
                }
                writer.WriteEndElement();

                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndDocument();
                await writer.FlushAsync().ConfigureAwait(false);
            }
        }

        private void WriteQualificationResult(XmlWriter writer, ImportedQualificationRecord record)
        {
            writer.WriteStartElement("ResultOfQualificationConfirmation");

            WriteElement(writer, "InsuredCardClassification", GetValue(record, "INSCC"));

            string insuranceType = GetValue(record, "CNFTYPBR");
            if (string.Equals(insuranceType, "1", StringComparison.OrdinalIgnoreCase))
            {
                WriteOptionalElement(writer, "InsurerNumber", GetValue(record, "INSN"));
                WriteOptionalElement(writer, "InsuredCardSymbol", GetValue(record, "INSCS"));
                WriteOptionalElement(writer, "InsuredIdentificationNumber", GetValue(record, "INSIN"));
                WriteOptionalElement(writer, "InsuredBranchNumber", GetValue(record, "INSBN"));
                WriteOptionalElement(writer, "PersonalFamilyClassification", GetValue(record, "PFC"));
                WriteOptionalElement(writer, "InsuredName", GetValue(record, "IEDN"));
            }
            else if (string.Equals(insuranceType, "2", StringComparison.OrdinalIgnoreCase))
            {
                WriteOptionalElement(writer, "InsurerNumber", FirstNonEmpty(GetValue(record, "KFB"), GetValue(record, "INSN")));
                WriteOptionalElement(writer, "InsuredIdentificationNumber", FirstNonEmpty(GetValue(record, "KJB"), GetValue(record, "INSIN")));
            }

            WriteElement(writer, "Name", GetValue(record, "NPT"));
            WriteOptionalElement(writer, "NameOfOther", GetValue(record, "NOO"));
            WriteOptionalElement(writer, "NameKana", GetValue(record, "NK"));
            WriteOptionalElement(writer, "NameOfOtherKana", GetValue(record, "NOOK"));
            WriteElement(writer, "Sex1", GetValue(record, "S1"));
            WriteOptionalElement(writer, "Sex2", GetValue(record, "S2"));
            WriteElement(writer, "Birthdate", GetValue(record, "BD"));
            WriteOptionalElement(writer, "Address", GetValue(record, "AD"));
            WriteOptionalElement(writer, "PostNumber", GetValue(record, "PN"));
            WriteElement(writer, "QualificationDate", GetValue(record, "QD"));
            WriteOptionalElement(writer, "DisqualificationDate", GetValue(record, "DD"));
            WriteOptionalElement(writer, "InsuredCertificateIssuanceDate", GetValue(record, "ICID"));
            WriteOptionalElement(writer, "InsuredCardValidDate", GetValue(record, "ICVD"));
            WriteOptionalElement(writer, "InsuredCardExpirationDate", GetValue(record, "ICED"));
            WriteOptionalElement(writer, "InsuredPartialContributionRatio", PadLeft(GetValue(record, "IPCR"), 3));
            WriteOptionalElement(writer, "PreschoolClassification", GetValue(record, "PC"));
            WriteOptionalElement(writer, "ReasonOfLoss", GetValue(record, "ROL"));

            string insurerName = string.Equals(insuranceType, "2", StringComparison.OrdinalIgnoreCase)
                ? FirstNonEmpty(GetValue(record, "JFN"), GetValue(record, "IERN"))
                : GetValue(record, "IERN");
            WriteElement(writer, "InsurerName", insurerName);

            WriteSection(writer, "ElderlyRecipientCertificateInfo", new[]
            {
                Tuple.Create("ElderlyRecipientCertificateDate", GetValue(record, "ERCD")),
                Tuple.Create("ElderlyRecipientValidStartDate", GetValue(record, "ERVSD")),
                Tuple.Create("ElderlyRecipientValidEndDate", GetValue(record, "ERVED")),
                Tuple.Create("ElderlyRecipientContributionRatio", GetValue(record, "ERCR"))
            });

            WriteOptionalElement(writer, "LimitApplicationCertificateRelatedConsFlg", GetValue(record, "LACRCF"));
            WriteOptionalElement(writer, "LimitApplicationCertificateRelatedConsTime", GetValue(record, "LACRCT"));
            WriteSection(writer, "LimitApplicationCertificateRelatedInfo", new[]
            {
                Tuple.Create("LimitApplicationCertificateClassification", GetValue(record, "LACC")),
                Tuple.Create("LimitApplicationCertificateClassificationFlag", GetValue(record, "LACCF")),
                Tuple.Create("LimitApplicationCertificateDate", GetValue(record, "LACD")),
                Tuple.Create("LimitApplicationCertificateValidStartDate", GetValue(record, "LACVSD")),
                Tuple.Create("LimitApplicationCertificateValidEndDate", GetValue(record, "LACVED")),
                Tuple.Create("LimitApplicationCertificateLongTermDate", GetValue(record, "LACLTD"))
            });

            WriteOptionalElement(writer, "SpecificDiseasesCertificateRelatedConsFlg", GetValue(record, "SDCRCF"));
            WriteOptionalElement(writer, "SpecificDiseasesCertificateRelatedConsTime", GetValue(record, "SDCRCT"));
            WriteRepeatedSection(
                writer,
                "SpecificDiseasesCertificateList",
                "SpecificDiseasesCertificateInfo",
                new[]
                {
                    Tuple.Create("SpecificDiseasesDiseaseCategory", "SDDC"),
                    Tuple.Create("SpecificDiseasesCertificateDate", "SDCD"),
                    Tuple.Create("SpecificDiseasesValidStartDate", "SDVSD"),
                    Tuple.Create("SpecificDiseasesValidEndDate", "SDVED"),
                    Tuple.Create("SpecificDiseasesSelfPay", "SDSP")
                },
                record);

            WriteRepeatedSection(
                writer,
                "PublicExpenseResultList",
                "MedicalTicketInfo",
                new[]
                {
                    Tuple.Create("TicketType", "TT"),
                    Tuple.Create("MedicalTicketValidDate", "MTVD"),
                    Tuple.Create("MedicalTicketExpirationDate", "MTED"),
                    Tuple.Create("IssueNumber", "MTIN"),
                    Tuple.Create("MedicalTreatmentMonth", "MTM"),
                    Tuple.Create("DesignatedMedicalInstitutionCode", "DMIC"),
                    Tuple.Create("DesignatedMedicalInstitutionFlag", "DMIF"),
                    Tuple.Create("DesignatedMedicalInstitutionName", "DMIN"),
                    Tuple.Create("PrescriptionIssuerMedicalInstitutionCode", "PIMIC"),
                    Tuple.Create("PrescriptionIssuerMedicalInstitutionName", "PIMIN"),
                    Tuple.Create("InjuryName1", "INJN1"),
                    Tuple.Create("InjuryName2", "INJN2"),
                    Tuple.Create("InjuryName3", "INJN3"),
                    Tuple.Create("MedicalTreatmentType", "MTT"),
                    Tuple.Create("SelfPayAmount", "SPA"),
                    Tuple.Create("DistrictContactName", "DCN"),
                    Tuple.Create("HandlingContactName", "HCN"),
                    Tuple.Create("SingleOrCombinedUse", "SOCU"),
                    Tuple.Create("StatusOfSocialInsurance", "SOSI"),
                    Tuple.Create("ConsistencyFlag", "CF"),
                    Tuple.Create("StatusOfInfecton", "SOI"),
                    Tuple.Create("StatusOfElderlyMedicalCare", "SOEMC"),
                    Tuple.Create("StatusOfPrefecturalExpenses", "SOPE"),
                    Tuple.Create("Remarks1", "R1"),
                    Tuple.Create("Remarks2", "R2"),
                    Tuple.Create("Remarks3", "R3")
                },
                record);

            WriteOptionalElement(writer, "SpecificHealthCheckupsInfoConsFlg", GetValue(record, "SHCICF"));
            WriteOptionalElement(writer, "SpecificHealthCheckupsInfoConsTime", GetValue(record, "SHCICT"));
            WriteOptionalElement(writer, "SpecificHealthCheckupsInfoAcquisitionTime", GetValue(record, "SHCIAT"));
            WriteOptionalElement(writer, "PharmaceuticalInfoConsFlg", GetValue(record, "PICF"));
            WriteOptionalElement(writer, "PharmaceuticalInfoConsTime", GetValue(record, "PICT"));
            WriteOptionalElement(writer, "PharmaceuticalInfoAcquisitionTime", GetValue(record, "PIAT"));
            WriteOptionalElement(writer, "DiagnosisInfoConsFlg", GetValue(record, "DICF"));
            WriteOptionalElement(writer, "DiagnosisInfoConsTime", GetValue(record, "DICT"));
            WriteOptionalElement(writer, "DiagnosisInfoAcquisitionTime", GetValue(record, "DIAT"));
            WriteOptionalElement(writer, "OperationInfoConsFlg", GetValue(record, "OICF"));
            WriteOptionalElement(writer, "OperationInfoConsTime", GetValue(record, "OICT"));
            WriteOptionalElement(writer, "OperationInfoAcquisitionTime", GetValue(record, "OIAT"));
            WriteOptionalElement(writer, "ReferenceNumber", GetValue(record, "RN"));

            writer.WriteEndElement();
        }

        private static IEnumerable<IGrouping<string, ImportedQualificationRecord>> GroupByKarte(IEnumerable<ImportedQualificationRecord> records)
        {
            return records.GroupBy(
                r =>
                {
                    string karte = GetValue(r, "KARTENO");
                    return string.IsNullOrWhiteSpace(karte) ? Guid.NewGuid().ToString("N") : karte;
                },
                StringComparer.OrdinalIgnoreCase);
        }

        private static string BuildFaceFilePath(string faceFolder, string medicalInstitutionCode, int fileCounter, BulkQualificationKind kind)
        {
            string fileName =
                FacePrefix +
                medicalInstitutionCode +
                GetSyoriCode(kind).ToString() +
                fileCounter.ToString("00000000000000000") +
                "_" +
                DateTime.Now.ToString("yyyyMMddHHmmss") +
                ".xml";

            return Path.Combine(faceFolder, fileName);
        }

        private static int GetSyoriCode(BulkQualificationKind kind)
        {
            switch (kind)
            {
                case BulkQualificationKind.MedicalAid:
                    return 400;
                case BulkQualificationKind.Houmon:
                    return 600;
                case BulkQualificationKind.Online:
                    return 800;
                default:
                    return 0;
            }
        }

        private static string GetMedicalInstitutionCode(IEnumerable<ImportedQualificationRecord> records)
        {
            return records
                .Select(r => FirstNonEmpty(GetValue(r, "MIC"), Properties.Settings.Default.MCode))
                .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))
                ?? string.Empty;
        }

        private static string GetValue(ImportedQualificationRecord record, string key)
        {
            if (record == null || string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            if (record.BulkToolValues.TryGetValue(key, out object value))
            {
                return value == null || value == DBNull.Value ? string.Empty : Convert.ToString(value)?.Trim() ?? string.Empty;
            }

            return string.Empty;
        }

        private static void WriteElement(XmlWriter writer, string elementName, string value)
        {
            writer.WriteStartElement(elementName);
            writer.WriteString(value ?? string.Empty);
            writer.WriteEndElement();
        }

        private static void WriteOptionalElement(XmlWriter writer, string elementName, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            WriteElement(writer, elementName, value);
        }

        private static void WriteSection(XmlWriter writer, string sectionName, IEnumerable<Tuple<string, string>> values)
        {
            List<Tuple<string, string>> nonEmpty = values
                .Where(v => !string.IsNullOrWhiteSpace(v.Item2))
                .ToList();

            if (nonEmpty.Count == 0)
            {
                return;
            }

            writer.WriteStartElement(sectionName);
            foreach (Tuple<string, string> item in nonEmpty)
            {
                WriteElement(writer, item.Item1, item.Item2);
            }
            writer.WriteEndElement();
        }

        private static void WriteRepeatedSection(
            XmlWriter writer,
            string listElementName,
            string itemElementName,
            IReadOnlyList<Tuple<string, string>> mappings,
            ImportedQualificationRecord record)
        {
            List<string[]> values = mappings
                .Select(m => SplitMultiValues(GetValue(record, m.Item2)))
                .ToList();

            int itemCount = values.Count == 0 ? 0 : values.Max(v => v.Length);
            if (itemCount <= 0)
            {
                return;
            }

            writer.WriteStartElement(listElementName);
            for (int index = 0; index < itemCount; index++)
            {
                bool hasAny = false;
                foreach (string[] array in values)
                {
                    if (index < array.Length && !string.IsNullOrWhiteSpace(array[index]))
                    {
                        hasAny = true;
                        break;
                    }
                }

                if (!hasAny)
                {
                    continue;
                }

                writer.WriteStartElement(itemElementName);
                foreach (var mapping in mappings.Select((m, i) => new { ElementName = m.Item1, Values = values[i] }))
                {
                    string itemValue = index < mapping.Values.Length ? mapping.Values[index] : string.Empty;
                    WriteOptionalElement(writer, mapping.ElementName, itemValue);
                }
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        private static string[] SplitMultiValues(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Array.Empty<string>();
            }

            return value
                .Split(new[] { MultiValueDelimiter }, StringSplitOptions.None)
                .Select(v => v?.Trim() ?? string.Empty)
                .ToArray();
        }

        private static string PadLeft(string value, int totalWidth)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Trim().PadLeft(totalWidth, '0');
        }

        private static string FirstNonEmpty(params string[] values)
        {
            return values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;
        }

        private Task LogAsync(string message)
        {
            return logAsync == null ? Task.CompletedTask : logAsync(message);
        }
    }
}
