using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace OQSDrug
{
    internal enum BulkQualificationKind
    {
        MedicalAid,
        Houmon,
        Online
    }

    internal sealed class BulkQualificationUploadResult
    {
        public string SourceFilePath { get; set; }
        public string ReceptionNumber { get; set; }
        public string SegmentOfResult { get; set; }
        public string ProcessingResultStatus { get; set; }
        public string ProcessingResultCode { get; set; }
        public string ProcessingResultMessage { get; set; }
    }

    internal sealed class BulkQualificationImportResult
    {
        public string SourceFilePath { get; set; }
        public string WorkingDirectory { get; set; }
        public string ExtractedXmlPath { get; set; }
        public string SegmentOfResult { get; set; }
        public string ProcessingResultStatus { get; set; }
        public string ProcessingResultCode { get; set; }
        public string ProcessingResultMessage { get; set; }
        public bool IsZip { get; set; }
        public bool WorkingDirectoryNeedsCleanup { get; set; }
    }

    internal sealed class BulkQualificationService
    {
        private const string XmlExtension = ".xml";
        private const string ZipExtension = ".zip";
        private const string FileNumberStateFileName = "bulk_request_sequence.txt";
        private static readonly object FileNumberLock = new object();

        private readonly string oqsFolder;
        private readonly string medicalInstitutionCode;
        private readonly Func<string, Task> logAsync;
        private readonly Action<BulkExecutionProgressInfo> progressAction;

        public BulkQualificationService(
            string oqsFolder,
            string medicalInstitutionCode,
            Func<string, Task> logAsync = null,
            Action<BulkExecutionProgressInfo> progressAction = null)
        {
            this.oqsFolder = oqsFolder ?? string.Empty;
            this.medicalInstitutionCode = medicalInstitutionCode ?? string.Empty;
            this.logAsync = logAsync;
            this.progressAction = progressAction;
        }

        public Task<string> CreateHoumonRequestAsync(DateTime consentDateFrom, DateTime consentDateTo)
        {
            return CreateRequestAsync(BulkQualificationKind.Houmon, true, consentDateFrom, consentDateTo);
        }

        public Task<string> CreateMedicalAidRequestAsync(DateTime medicalTreatmentMonth)
        {
            return CreateRequestAsync(BulkQualificationKind.MedicalAid, true, medicalTreatmentMonth, medicalTreatmentMonth);
        }

        public Task<string> CreateOnlineRequestAsync(DateTime from, DateTime to, bool useConsentDates)
        {
            return CreateRequestAsync(BulkQualificationKind.Online, useConsentDates, from, to);
        }

        public async Task<string> CreateDownloadRequestFromUploadResultAsync(BulkQualificationKind kind, string uploadResultFilePath)
        {
            ReportProgress(kind, "受付結果読込中", uploadResultFilePath);
            await LogAsync($"[{kind}] アップロード結果からダウンロード要求を作成します: {uploadResultFilePath}").ConfigureAwait(false);

            var uploadResult = await ReadUploadResultAsync(uploadResultFilePath).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(uploadResult.ReceptionNumber))
            {
                throw new InvalidOperationException("ReceptionNumber was not found in upload result XML.");
            }

            return await CreateDownloadRequestAsync(kind, uploadResult.ReceptionNumber).ConfigureAwait(false);
        }

        public async Task<string> CreateDownloadRequestAsync(BulkQualificationKind kind, string receptionNumber)
        {
            if (string.IsNullOrWhiteSpace(receptionNumber))
            {
                throw new InvalidOperationException("ReceptionNumber is required.");
            }

            ReportProgress(kind, "結果取得要求作成中", "ReceptionNumber=" + receptionNumber, receptionNumber);
            string reqFolder = EnsureFolder(Path.Combine(oqsFolder, "req"));
            string filePath = Path.Combine(
                reqFolder,
                BuildFileName(kind, GetPrefix(kind, RequestPhase.DownloadRequest), DateTime.Now, XmlExtension));

            using (var writer = XmlWriter.Create(filePath, CreateWriterSettings()))
            {
                writer.WriteStartDocument();
                writer.WriteRaw("<?xml version=\"1.0\" encoding=\"Shift_JIS\" standalone=\"no\"?>\r\n");
                writer.WriteStartElement("XmlMsg");
                writer.WriteStartElement("MessageHeader");
                writer.WriteElementString("MedicalInstitutionCode", medicalInstitutionCode);
                writer.WriteEndElement();
                writer.WriteStartElement("MessageBody");
                writer.WriteElementString("ReceptionNumber", receptionNumber);
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndDocument();
            }

            await LogAsync($"[{kind}] ダウンロード要求ファイルを作成しました: {filePath} / ReceptionNumber={receptionNumber}").ConfigureAwait(false);
            ReportProgress(kind, "結果取得要求作成完了", filePath, receptionNumber);
            return filePath;
        }

        public async Task<BulkQualificationUploadResult> ReadUploadResultAsync(string uploadResultFilePath)
        {
            string normalizedPath = NormalizeAndValidateInputPath(uploadResultFilePath, XmlExtension);
            ReportProgress(null, "受付番号解析中", normalizedPath);
            await LogAsync($"アップロード結果XMLを読み込みます: {normalizedPath}").ConfigureAwait(false);

            var xml = await LoadXmlAsync(normalizedPath).ConfigureAwait(false);
            var result = new BulkQualificationUploadResult
            {
                SourceFilePath = normalizedPath,
                ReceptionNumber = SelectNodeValue(xml, "//ReceptionNumber"),
                SegmentOfResult = SelectNodeValue(xml, "//SegmentOfResult"),
                ProcessingResultStatus = SelectNodeValue(xml, "//ProcessingResultStatus"),
                ProcessingResultCode = SelectNodeValue(xml, "//ProcessingResultCode"),
                ProcessingResultMessage = SelectNodeValue(xml, "//ProcessingResultMessage")
            };

            await LogAsync(
                $"アップロード結果XMLを解析しました: ReceptionNumber={result.ReceptionNumber}, " +
                $"Segment={result.SegmentOfResult}, Status={result.ProcessingResultStatus}, " +
                $"Code={result.ProcessingResultCode}, Message={result.ProcessingResultMessage}").ConfigureAwait(false);
            ReportProgress(null, "受付番号解析完了", normalizedPath, result.ReceptionNumber);

            return result;
        }

        public async Task<BulkQualificationImportResult> ImportResultAsync(BulkQualificationKind kind, string resultFilePath)
        {
            string normalizedPath = NormalizeAndValidateInputPath(resultFilePath, null);
            string extension = Path.GetExtension(normalizedPath);
            bool isZip = extension.Equals(ZipExtension, StringComparison.OrdinalIgnoreCase);
            ReportProgress(kind, "結果ファイル取込中", normalizedPath);
            await LogAsync($"[{kind}] 結果ファイルの取り込みを開始します: {normalizedPath}").ConfigureAwait(false);

            string baseFolder = EnsureFolder(Path.Combine(GetBulkTempRoot(), kind.ToString().ToLowerInvariant()));
            string workingDirectory = EnsureFolder(Path.Combine(baseFolder, DateTime.Now.ToString("yyyyMMddHHmmssfff") + "_" + Guid.NewGuid().ToString("N")));
            string extractedXmlPath;
            ReportProgress(kind, "一時展開フォルダ作成", workingDirectory);
            await LogAsync($"[{kind}] 展開用フォルダを作成しました: {workingDirectory}").ConfigureAwait(false);

            if (isZip)
            {
                ReportProgress(kind, "ZIP解析中", normalizedPath);
                await LogAsync($"[{kind}] ZIP extraction started.").ConfigureAwait(false);
                using (var archive = ZipFile.OpenRead(normalizedPath))
                {
                    archive.ExtractToDirectory(workingDirectory);
                }

                var xmlFiles = Directory.GetFiles(workingDirectory, "*.xml", SearchOption.AllDirectories);
                await LogAsync($"[{kind}] 展開後にXMLを {xmlFiles.Length} 件検出しました").ConfigureAwait(false);

                extractedXmlPath = xmlFiles
                    .FirstOrDefault(path => Path.GetFileName(path).StartsWith(GetPrefix(kind, RequestPhase.DownloadResult), StringComparison.OrdinalIgnoreCase))
                    ?? xmlFiles.FirstOrDefault();

                if (string.IsNullOrWhiteSpace(extractedXmlPath))
                {
                    throw new FileNotFoundException("Result XML was not found in ZIP.", normalizedPath);
                }

                ReportProgress(kind, "ZIP解析完了", extractedXmlPath);
            }
            else if (extension.Equals(XmlExtension, StringComparison.OrdinalIgnoreCase))
            {
                extractedXmlPath = Path.Combine(workingDirectory, Path.GetFileName(normalizedPath));
                File.Copy(normalizedPath, extractedXmlPath, true);
                ReportProgress(kind, "XMLコピー完了", extractedXmlPath);
                await LogAsync($"[{kind}] XMLファイルを展開用フォルダへコピーしました: {extractedXmlPath}").ConfigureAwait(false);
            }
            else
            {
                throw new InvalidOperationException("Result file must be .zip or .xml.");
            }

            var xml = await LoadXmlAsync(extractedXmlPath).ConfigureAwait(false);
            var result = new BulkQualificationImportResult
            {
                SourceFilePath = normalizedPath,
                WorkingDirectory = workingDirectory,
                ExtractedXmlPath = extractedXmlPath,
                SegmentOfResult = SelectNodeValue(xml, "//SegmentOfResult"),
                ProcessingResultStatus = SelectNodeValue(xml, "//ProcessingResultStatus"),
                ProcessingResultCode = SelectNodeValue(xml, "//ProcessingResultCode"),
                ProcessingResultMessage = SelectNodeValue(xml, "//ProcessingResultMessage"),
                IsZip = isZip,
                WorkingDirectoryNeedsCleanup = true
            };

            await LogAsync(
                $"[{kind}] 結果XMLを読み込みました: Xml={result.ExtractedXmlPath}, " +
                $"Segment={result.SegmentOfResult}, Status={result.ProcessingResultStatus}, " +
                $"Code={result.ProcessingResultCode}, Message={result.ProcessingResultMessage}").ConfigureAwait(false);
            ReportProgress(kind, "結果XML解析完了", result.ExtractedXmlPath);

            return result;
        }

        public async Task CleanupImportResultAsync(BulkQualificationImportResult result)
        {
            if (result == null
                || !result.WorkingDirectoryNeedsCleanup
                || string.IsNullOrWhiteSpace(result.WorkingDirectory)
                || !Directory.Exists(result.WorkingDirectory))
            {
                return;
            }

            try
            {
                Directory.Delete(result.WorkingDirectory, true);
                result.WorkingDirectoryNeedsCleanup = false;
                ReportProgress(null, "一時フォルダ削除完了", result.WorkingDirectory);
                await LogAsync($"展開用フォルダを削除しました: {result.WorkingDirectory}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ReportProgress(null, "一時フォルダ削除エラー", ex.Message);
                await LogAsync($"展開用フォルダ削除エラー: {result.WorkingDirectory} / {ex.Message}").ConfigureAwait(false);
            }
        }

        public async Task<string> WaitForExpectedResultFileAsync(
            string requestFilePath,
            int waitSeconds,
            int pollIntervalSeconds,
            bool allowZip,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            DateTime deadline = DateTime.Now.AddSeconds(Math.Max(0, waitSeconds));
            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                string found = FindExpectedResultFile(requestFilePath, allowZip);
                if (!string.IsNullOrWhiteSpace(found))
                {
                    return found;
                }

                if (waitSeconds <= 0)
                {
                    return null;
                }

                await Task.Delay(Math.Max(1000, pollIntervalSeconds * 1000), cancellationToken).ConfigureAwait(false);
            }
            while (DateTime.Now <= deadline);

            return null;
        }

        private async Task<string> CreateRequestAsync(BulkQualificationKind kind, bool useConsentDates, DateTime from, DateTime to)
        {
            ValidateSettings();
            if (to < from)
            {
                throw new ArgumentException("End date must be on or after start date.");
            }

            string reqFolder = EnsureFolder(Path.Combine(oqsFolder, "req"));
            string filePath = Path.Combine(
                reqFolder,
                BuildFileName(kind, GetPrefix(kind, RequestPhase.Request), DateTime.Now, XmlExtension));

            await LogAsync(
                $"[{kind}] 要求ファイルを作成します: Path={filePath}, " +
                $"Mode={GetRequestModeLabel(kind, useConsentDates)}, " +
                $"From={from:yyyy-MM-dd}, To={to:yyyy-MM-dd}").ConfigureAwait(false);
            ReportProgress(kind, "要求ファイル作成中", BuildRequestSpanText(kind, from, to, useConsentDates));

            using (var writer = XmlWriter.Create(filePath, CreateWriterSettings()))
            {
                writer.WriteStartDocument();
                writer.WriteRaw("<?xml version=\"1.0\" encoding=\"Shift_JIS\" standalone=\"no\"?>\r\n");
                writer.WriteStartElement("XmlMsg");

                writer.WriteStartElement("MessageHeader");
                writer.WriteElementString("MedicalInstitutionCode", medicalInstitutionCode);
                writer.WriteElementString("ArbitraryFileIdentifier", BuildArbitraryFileIdentifier(kind));
                writer.WriteEndElement();

                writer.WriteStartElement("MessageBody");
                writer.WriteStartElement("QualificationConfirmSearchInfo");

                if (kind == BulkQualificationKind.MedicalAid)
                {
                    writer.WriteElementString("MedicalTreatmentMonth", from.ToString("yyyyMM", CultureInfo.InvariantCulture));
                }
                else if (kind == BulkQualificationKind.Houmon || useConsentDates)
                {
                    writer.WriteElementString("ConsentDateFrom", from.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
                    writer.WriteElementString("ConsentDateTo", to.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
                }
                else
                {
                    writer.WriteElementString("ExaminationDateFrom", from.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
                    writer.WriteElementString("ExaminationDateTo", to.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
                }

                if (kind != BulkQualificationKind.MedicalAid)
                {
                    writer.WriteElementString("MedicalTreatmentFlag", kind == BulkQualificationKind.Houmon ? "2" : "3");
                }
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndDocument();
            }

            await LogAsync($"[{kind}] 要求ファイルを保存しました: {filePath}").ConfigureAwait(false);
            ReportProgress(kind, "要求ファイル作成完了", filePath);
            return filePath;
        }

        private string BuildFileName(BulkQualificationKind kind, string prefix, DateTime now, string extension)
        {
            ValidateSettings();

            string fileDate = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            string fileNumber = GetNextFileNumber(fileDate);

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}{1}{2}{3}",
                prefix,
                fileDate,
                fileNumber,
                extension);
        }

        private static XmlWriterSettings CreateWriterSettings()
        {
            return new XmlWriterSettings
            {
                Encoding = Encoding.GetEncoding("Shift_JIS"),
                Indent = true,
                OmitXmlDeclaration = true
            };
        }

        private string BuildArbitraryFileIdentifier(BulkQualificationKind kind)
        {
            DateTime now = DateTime.Now;
            int milliseconds = now.Millisecond;
            string machineName = Environment.MachineName ?? string.Empty;
            if (machineName.Length > 3)
            {
                machineName = machineName.Substring(machineName.Length - 3, 3);
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}{1:HHmmss}{2:000}{3}",
                now.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
                now,
                milliseconds,
                machineName);
        }

        private string GetNextFileNumber(string fileDate)
        {
            string stateDirectory = EnsureFolder(oqsFolder);
            string statePath = Path.Combine(stateDirectory, FileNumberStateFileName);

            lock (FileNumberLock)
            {
                string savedDate = string.Empty;
                int savedNumber = 0;

                if (File.Exists(statePath))
                {
                    try
                    {
                        string[] parts = (File.ReadAllText(statePath, Encoding.UTF8) ?? string.Empty)
                            .Trim()
                            .Split('|');
                        if (parts.Length >= 2)
                        {
                            savedDate = (parts[0] ?? string.Empty).Trim();
                            int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out savedNumber);
                        }
                    }
                    catch
                    {
                        savedDate = string.Empty;
                        savedNumber = 0;
                    }
                }

                int nextNumber = string.Equals(savedDate, fileDate, StringComparison.Ordinal)
                    ? savedNumber + 1
                    : 1;

                File.WriteAllText(
                    statePath,
                    string.Format(CultureInfo.InvariantCulture, "{0}|{1:0000}", fileDate, nextNumber),
                    Encoding.UTF8);

                return nextNumber.ToString("0000", CultureInfo.InvariantCulture);
            }
        }

        private static async Task<XmlDocument> LoadXmlAsync(string filePath)
        {
            string xmlText;
            using (var reader = new StreamReader(filePath, Encoding.GetEncoding("Shift_JIS"), true))
            {
                xmlText = await reader.ReadToEndAsync().ConfigureAwait(false);
            }

            var xml = new XmlDocument();
            xml.LoadXml(xmlText);
            return xml;
        }

        private static string SelectNodeValue(XmlDocument xml, string xpath)
        {
            var node = xml.SelectSingleNode(xpath);
            return node == null ? string.Empty : (node.InnerText ?? string.Empty).Trim();
        }

        private string NormalizeAndValidateInputPath(string filePath, string requiredExtension)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path is required.", nameof(filePath));
            }

            string normalizedPath = Path.GetFullPath(filePath);
            if (!File.Exists(normalizedPath))
            {
                throw new FileNotFoundException("Specified file was not found.", normalizedPath);
            }

            if (!string.IsNullOrWhiteSpace(requiredExtension)
                && !Path.GetExtension(normalizedPath).Equals(requiredExtension, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(requiredExtension + " file is required.");
            }

            return normalizedPath;
        }

        private static string FindExpectedResultFile(string requestFilePath, bool allowZip)
        {
            if (string.IsNullOrWhiteSpace(requestFilePath))
            {
                return null;
            }

            string requestFullPath = Path.GetFullPath(requestFilePath);
            string requestDirectory = Path.GetDirectoryName(requestFullPath);
            if (string.IsNullOrWhiteSpace(requestDirectory))
            {
                return null;
            }

            string baseName = Path.GetFileNameWithoutExtension(requestFullPath);
            if (string.IsNullOrWhiteSpace(baseName))
            {
                return null;
            }

            string expectedBaseName = baseName
                .Replace("01req_", "01res_")
                .Replace("02req_", "02res_");

            string rootDirectory = Directory.GetParent(requestDirectory) == null
                ? requestDirectory
                : Directory.GetParent(requestDirectory).FullName;
            string resultDirectory = Path.Combine(rootDirectory, "res");
            if (!Directory.Exists(resultDirectory))
            {
                return null;
            }

            var candidates = Directory.GetFiles(resultDirectory, expectedBaseName + ".*", SearchOption.TopDirectoryOnly)
                .Where(path =>
                {
                    string extension = Path.GetExtension(path);
                    if (extension.Equals(XmlExtension, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    return allowZip && extension.Equals(ZipExtension, StringComparison.OrdinalIgnoreCase);
                })
                .OrderByDescending(path => File.GetLastWriteTime(path));

            return candidates.FirstOrDefault();
        }

        private string EnsureFolder(string path)
        {
            Directory.CreateDirectory(path);
            return path;
        }

        private static string GetBulkTempRoot()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "OQSDrug", "bulk-temp");
        }

        private void ValidateSettings()
        {
            if (string.IsNullOrWhiteSpace(oqsFolder))
            {
                throw new InvalidOperationException("OQS folder is not configured.");
            }

            if (string.IsNullOrWhiteSpace(medicalInstitutionCode))
            {
                throw new InvalidOperationException("Medical institution code is not configured.");
            }
        }

        private Task LogAsync(string message)
        {
            if (logAsync == null || string.IsNullOrWhiteSpace(message))
            {
                return Task.CompletedTask;
            }

            return logAsync(message);
        }

        private void ReportProgress(BulkQualificationKind? kind, string statusText, string detailText = null, string receptionNumber = null)
        {
            if (progressAction == null)
            {
                return;
            }

            progressAction(new BulkExecutionProgressInfo
            {
                Timestamp = DateTime.Now,
                Kind = kind,
                ProcessName = kind.HasValue ? GetDisplayName(kind.Value) : string.Empty,
                StatusText = statusText ?? string.Empty,
                DetailText = detailText ?? string.Empty,
                ReceptionNumber = receptionNumber ?? string.Empty
            });
        }

        private static string GetPrefix(BulkQualificationKind kind, RequestPhase phase)
        {
            if (kind == BulkQualificationKind.MedicalAid)
            {
                switch (phase)
                {
                    case RequestPhase.Request:
                        return "OQSmutic01req_";
                    case RequestPhase.UploadResult:
                        return "OQSmutic01res_";
                    case RequestPhase.DownloadRequest:
                        return "OQSmutic02req_";
                    default:
                        return "OQSmutic02res_";
                }
            }

            if (kind == BulkQualificationKind.Houmon)
            {
                switch (phase)
                {
                    case RequestPhase.Request:
                        return "OQSmuhvq01req_";
                    case RequestPhase.UploadResult:
                        return "OQSmuhvq01res_";
                    case RequestPhase.DownloadRequest:
                        return "OQSmuhvq02req_";
                    default:
                        return "OQSmuhvq02res_";
                }
            }

            switch (phase)
            {
                case RequestPhase.Request:
                    return "OQSmuonq01req_";
                case RequestPhase.UploadResult:
                    return "OQSmuonq01res_";
                case RequestPhase.DownloadRequest:
                    return "OQSmuonq02req_";
                default:
                    return "OQSmuonq02res_";
            }
        }

        internal static string GetDisplayName(BulkQualificationKind kind)
        {
            switch (kind)
            {
                case BulkQualificationKind.MedicalAid:
                    return "医療扶助 Bulk";
                case BulkQualificationKind.Houmon:
                    return "訪問診療 Bulk";
                default:
                    return "オンライン診療 Bulk";
            }
        }

        private static string GetRequestModeLabel(BulkQualificationKind kind, bool useConsentDates)
        {
            if (kind == BulkQualificationKind.MedicalAid)
            {
                return "MedicalTreatmentMonth";
            }

            if (kind == BulkQualificationKind.Houmon || useConsentDates)
            {
                return "ConsentDate";
            }

            return "ExaminationDate";
        }

        private static string BuildRequestSpanText(BulkQualificationKind kind, DateTime from, DateTime to, bool useConsentDates)
        {
            if (kind == BulkQualificationKind.MedicalAid)
            {
                return from.ToString("yyyy/MM", CultureInfo.InvariantCulture);
            }

            string modeLabel = (kind == BulkQualificationKind.Houmon || useConsentDates) ? "同意日" : "受診日";
            return $"{modeLabel}: {from:yyyy/MM/dd} - {to:yyyy/MM/dd}";
        }

        private enum RequestPhase
        {
            Request,
            UploadResult,
            DownloadRequest,
            DownloadResult
        }
    }
}
