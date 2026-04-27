using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Npgsql;

namespace OQSDrug
{
    internal sealed class BulkAutoExecutionOptions
    {
        public BulkQualificationKind Kind { get; set; }
        public bool Enabled { get; set; }
        public bool UseConsentDates { get; set; }
        public int ConsentDaysBack { get; set; }
        public int ConsentDaysForward { get; set; }
        public int ExaminationDaysBack { get; set; }
        public int ExaminationDaysForward { get; set; }
        public int MedicalTreatmentMonthBack { get; set; }
        public int MedicalTreatmentMonthForward { get; set; }
        public int PollIntervalSeconds { get; set; }
        public int AutoIntervalMinutes { get; set; }
        public int MaxRetryCount { get; set; }
        public DateTime? DateFromOverride { get; set; }
        public DateTime? DateToOverride { get; set; }
        public DateTime? MedicalTreatmentMonthOverride { get; set; }

        public static BulkAutoExecutionOptions FromSettings(BulkQualificationKind kind)
        {
            var options = new BulkAutoExecutionOptions
            {
                Kind = kind
            };

            switch (kind)
            {
                case BulkQualificationKind.MedicalAid:
                    options.Enabled = Properties.Settings.Default.BulkMedicalAidAutoEnabled;
                    options.MedicalTreatmentMonthBack = Math.Max(0, Properties.Settings.Default.BulkMedicalAidMonthBack);
                    options.MedicalTreatmentMonthForward = Math.Max(0, Properties.Settings.Default.BulkMedicalAidMonthForward);
                    options.PollIntervalSeconds = Math.Max(5, Properties.Settings.Default.BulkMedicalAidPollIntervalSeconds);
                    options.AutoIntervalMinutes = Math.Max(1, Properties.Settings.Default.BulkMedicalAidAutoIntervalMinutes);
                    options.MaxRetryCount = Math.Max(1, Properties.Settings.Default.BulkMedicalAidMaxRetryCount);
                    break;
                case BulkQualificationKind.Houmon:
                    options.Enabled = Properties.Settings.Default.BulkHoumonAutoEnabled;
                    options.UseConsentDates = true;
                    options.ConsentDaysBack = Math.Max(0, Properties.Settings.Default.BulkHoumonConsentDaysBack);
                    options.ConsentDaysForward = Math.Max(0, Properties.Settings.Default.BulkHoumonConsentDaysForward);
                    options.PollIntervalSeconds = Math.Max(5, Properties.Settings.Default.BulkHoumonPollIntervalSeconds);
                    options.AutoIntervalMinutes = Math.Max(1, Properties.Settings.Default.BulkHoumonAutoIntervalMinutes);
                    options.MaxRetryCount = Math.Max(1, Properties.Settings.Default.BulkHoumonMaxRetryCount);
                    break;
                default:
                    options.Enabled = Properties.Settings.Default.BulkOnlineAutoEnabled;
                    options.UseConsentDates = Properties.Settings.Default.BulkOnlineUseConsentDates;
                    options.ConsentDaysBack = Math.Max(0, Properties.Settings.Default.BulkOnlineConsentDaysBack);
                    options.ConsentDaysForward = Math.Max(0, Properties.Settings.Default.BulkOnlineConsentDaysForward);
                    options.ExaminationDaysBack = Math.Max(0, Properties.Settings.Default.BulkOnlineExaminationDaysBack);
                    options.ExaminationDaysForward = Math.Max(0, Properties.Settings.Default.BulkOnlineExaminationDaysForward);
                    options.PollIntervalSeconds = Math.Max(5, Properties.Settings.Default.BulkOnlinePollIntervalSeconds);
                    options.AutoIntervalMinutes = Math.Max(1, Properties.Settings.Default.BulkOnlineAutoIntervalMinutes);
                    options.MaxRetryCount = Math.Max(1, Properties.Settings.Default.BulkOnlineMaxRetryCount);
                    break;
            }

            return options;
        }

        public static BulkAutoExecutionOptions FromSettings()
        {
            return FromSettings(BulkQualificationKind.Houmon);
        }

        public string GetRequestMode()
        {
            switch (Kind)
            {
                case BulkQualificationKind.MedicalAid:
                    return "medical_month";
                case BulkQualificationKind.Houmon:
                    return "consent";
                default:
                    return UseConsentDates ? "consent" : "examination";
            }
        }

        public DateTime GetDateFrom(DateTime today)
        {
            if (DateFromOverride.HasValue)
            {
                return DateFromOverride.Value.Date;
            }

            if (Kind == BulkQualificationKind.Online && !UseConsentDates)
            {
                return today.Date.AddDays(-ExaminationDaysBack);
            }

            return today.Date.AddDays(-ConsentDaysBack);
        }

        public DateTime GetDateTo(DateTime today)
        {
            if (DateToOverride.HasValue)
            {
                return DateToOverride.Value.Date;
            }

            if (Kind == BulkQualificationKind.Online && !UseConsentDates)
            {
                return today.Date.AddDays(ExaminationDaysForward);
            }

            return today.Date.AddDays(ConsentDaysForward);
        }

        public DateTime GetMedicalTreatmentMonth(DateTime today)
        {
            if (MedicalTreatmentMonthOverride.HasValue)
            {
                DateTime value = MedicalTreatmentMonthOverride.Value;
                return new DateTime(value.Year, value.Month, 1);
            }

            DateTime month = new DateTime(today.Year, today.Month, 1);
            return month.AddMonths(-MedicalTreatmentMonthBack);
        }

        public DateTime GetConsentDateFrom(DateTime today)
        {
            return GetDateFrom(today);
        }

        public DateTime GetConsentDateTo(DateTime today)
        {
            return GetDateTo(today);
        }

        public DateTime GetMedicalTreatmentMonthTo(DateTime today)
        {
            if (MedicalTreatmentMonthOverride.HasValue)
            {
                DateTime value = MedicalTreatmentMonthOverride.Value;
                return new DateTime(value.Year, value.Month, 1);
            }

            DateTime month = new DateTime(today.Year, today.Month, 1);
            return month.AddMonths(MedicalTreatmentMonthForward);
        }

        public string GetSpanText(DateTime today)
        {
            if (Kind == BulkQualificationKind.MedicalAid)
            {
                DateTime fromMonth = GetMedicalTreatmentMonth(today);
                DateTime toMonth = GetMedicalTreatmentMonthTo(today);
                return fromMonth == toMonth
                    ? fromMonth.ToString("yyyy/MM")
                    : $"{fromMonth:yyyy/MM} - {toMonth:yyyy/MM}";
            }

            DateTime from = GetDateFrom(today);
            DateTime to = GetDateTo(today);
            string mode = (Kind == BulkQualificationKind.Houmon || UseConsentDates) ? "同意日" : "受診日";
            return $"{mode}: {from:yyyy/MM/dd} - {to:yyyy/MM/dd}";
        }

        public IEnumerable<DateTime> EnumerateMedicalTreatmentMonths(DateTime today)
        {
            DateTime fromMonth = GetMedicalTreatmentMonth(today);
            DateTime toMonth = GetMedicalTreatmentMonthTo(today);
            if (toMonth < fromMonth)
            {
                toMonth = fromMonth;
            }

            for (DateTime month = fromMonth; month <= toMonth; month = month.AddMonths(1))
            {
                yield return month;
            };
        }
    }

    internal sealed class BulkAutoExecutionJob
    {
        public long Id { get; set; }
        public string JobKind { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string RequestMode { get; set; } = string.Empty;
        public DateTime? ConsentDateFrom { get; set; }
        public DateTime? ConsentDateTo { get; set; }
        public DateTime? ExaminationDateFrom { get; set; }
        public DateTime? ExaminationDateTo { get; set; }
        public string MedicalTreatmentMonth { get; set; } = string.Empty;
        public string UploadRequestFilePath { get; set; } = string.Empty;
        public string UploadResultFilePath { get; set; } = string.Empty;
        public string DownloadRequestFilePath { get; set; } = string.Empty;
        public string DownloadResultFilePath { get; set; } = string.Empty;
        public string ReceptionNumber { get; set; } = string.Empty;
        public string SegmentOfResult { get; set; } = string.Empty;
        public string ProcessingResultStatus { get; set; } = string.Empty;
        public string ProcessingResultCode { get; set; } = string.Empty;
        public string ProcessingResultMessage { get; set; } = string.Empty;
        public int RetryCount { get; set; }
        public int MaxRetryCount { get; set; }
        public DateTime? NextRetryAt { get; set; }
        public string HostName { get; set; } = string.Empty;
        public string HostIp { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    internal static class BulkAutoExecutionStore
    {
        public const string TableName = "bulk_execution_jobs";

        public static async Task EnsureTableAsync(Func<string, Task> logAsync = null)
        {
            if (!string.Equals(Properties.Settings.Default.DBtype, "pg", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            using (IDbConnection connection = CommonFunctions.GetDbConnection(false))
            {
                await OpenAsync(connection).ConfigureAwait(false);

                string createSql = @"
CREATE TABLE IF NOT EXISTS public.bulk_execution_jobs (
    id bigserial PRIMARY KEY,
    job_kind text NOT NULL,
    status text NOT NULL,
    request_mode text NOT NULL,
    consent_date_from date NULL,
    consent_date_to date NULL,
    examination_date_from date NULL,
    examination_date_to date NULL,
    medical_treatment_month text NULL,
    upload_request_file_path text NULL,
    upload_result_file_path text NULL,
    download_request_file_path text NULL,
    download_result_file_path text NULL,
    reception_number text NULL,
    segment_of_result text NULL,
    processing_result_status text NULL,
    processing_result_code text NULL,
    processing_result_message text NULL,
    retry_count integer NOT NULL DEFAULT 0,
    max_retry_count integer NOT NULL DEFAULT 1,
    next_retry_at timestamp without time zone NULL,
    host_name text NULL,
    host_ip text NULL,
    created_at timestamp without time zone NOT NULL,
    updated_at timestamp without time zone NOT NULL
)";

                using (IDbCommand command = connection.CreateCommand())
                {
                    command.CommandText = createSql;
                    await ExecuteNonQueryAsync(command).ConfigureAwait(false);
                }

                using (IDbCommand command = connection.CreateCommand())
                {
                    command.CommandText =
                        "ALTER TABLE public.bulk_execution_jobs " +
                        "ADD COLUMN IF NOT EXISTS medical_treatment_month text NULL";
                    await ExecuteNonQueryAsync(command).ConfigureAwait(false);
                }

                using (IDbCommand command = connection.CreateCommand())
                {
                    command.CommandText =
                        "CREATE INDEX IF NOT EXISTS ix_bulk_execution_jobs_kind_status " +
                        "ON public.bulk_execution_jobs (job_kind, status, next_retry_at)";
                    await ExecuteNonQueryAsync(command).ConfigureAwait(false);
                }

                using (IDbCommand command = connection.CreateCommand())
                {
                    command.CommandText =
                        "CREATE INDEX IF NOT EXISTS ix_bulk_execution_jobs_kind_created " +
                        "ON public.bulk_execution_jobs (job_kind, created_at DESC)";
                    await ExecuteNonQueryAsync(command).ConfigureAwait(false);
                }
            }

            if (logAsync != null)
            {
                await logAsync("Bulk実行管理テーブルを確認しました: " + TableName).ConfigureAwait(false);
            }
        }

        public static async Task<BulkAutoExecutionJob> GetActiveJobAsync(string jobKind)
        {
            const string sql = @"
SELECT *
FROM public.bulk_execution_jobs
WHERE job_kind = @p0
  AND status NOT IN ('completed', 'failed')
ORDER BY created_at ASC
LIMIT 1";

            return await LoadSingleAsync(sql, jobKind).ConfigureAwait(false);
        }

        public static async Task<bool> HasRecentJobAsync(string jobKind, DateTime threshold)
        {
            const string sql = @"
SELECT COUNT(*)
FROM public.bulk_execution_jobs
WHERE job_kind = @p0
  AND created_at >= @p1";

            using (IDbConnection connection = CommonFunctions.GetDbConnection(false))
            {
                await OpenAsync(connection).ConfigureAwait(false);
                using (IDbCommand command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    AddParameter(command, "@p0", jobKind);
                    AddParameter(command, "@p1", threshold);
                    object result = await ExecuteScalarAsync(command).ConfigureAwait(false);
                    return result != null && result != DBNull.Value && Convert.ToInt32(result) > 0;
                }
            }
        }

        public static async Task<BulkAutoExecutionJob> CreateHoumonJobAsync(
            DateTime consentDateFrom,
            DateTime consentDateTo,
            int maxRetryCount)
        {
            HostIdentity host = GetHostIdentity();
            DateTime now = DateTime.Now;

            const string sql = @"
INSERT INTO public.bulk_execution_jobs (
    job_kind, status, request_mode,
    consent_date_from, consent_date_to,
    retry_count, max_retry_count, next_retry_at,
    host_name, host_ip, created_at, updated_at)
VALUES (
    @p0, @p1, @p2,
    @p3, @p4,
    @p5, @p6, @p7,
    @p8, @p9, @p10, @p11)
RETURNING *";

            using (IDbConnection connection = CommonFunctions.GetDbConnection(false))
            {
                await OpenAsync(connection).ConfigureAwait(false);
                using (IDbCommand command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    AddParameter(command, "@p0", "houmon");
                    AddParameter(command, "@p1", "pending_request");
                    AddParameter(command, "@p2", "consent");
                    AddParameter(command, "@p3", consentDateFrom.Date);
                    AddParameter(command, "@p4", consentDateTo.Date);
                    AddParameter(command, "@p5", 0);
                    AddParameter(command, "@p6", maxRetryCount);
                    AddParameter(command, "@p7", now);
                    AddParameter(command, "@p8", host.Name);
                    AddParameter(command, "@p9", host.IpAddress);
                    AddParameter(command, "@p10", now);
                    AddParameter(command, "@p11", now);

                    using (DbDataReader reader = await ((DbCommand)command).ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        if (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            return Map(reader);
                        }
                    }
                }
            }

            throw new InvalidOperationException("Bulk自動実行ジョブの作成に失敗しました。");
        }

        public static Task<BulkAutoExecutionJob> CreateOnlineJobAsync(
            bool useConsentDates,
            DateTime from,
            DateTime to,
            int maxRetryCount)
        {
            return CreateJobAsync(
                "online",
                useConsentDates ? "consent" : "examination",
                useConsentDates ? (DateTime?)from.Date : null,
                useConsentDates ? (DateTime?)to.Date : null,
                useConsentDates ? (DateTime?)null : from.Date,
                useConsentDates ? (DateTime?)null : to.Date,
                null,
                maxRetryCount);
        }

        public static Task<BulkAutoExecutionJob> CreateMedicalAidJobAsync(
            DateTime medicalTreatmentMonth,
            int maxRetryCount)
        {
            return CreateJobAsync(
                "medicalaid",
                "medical_month",
                null,
                null,
                null,
                null,
                medicalTreatmentMonth.ToString("yyyyMM"),
                maxRetryCount);
        }

        public static Task UpdateAfterUploadRequestAsync(long jobId, string filePath)
        {
            return UpdateAsync(
                @"UPDATE public.bulk_execution_jobs
                  SET status = @p0,
                      upload_request_file_path = @p1,
                      next_retry_at = @p2,
                      updated_at = @p3
                  WHERE id = @p4",
                "waiting_upload_result",
                filePath ?? string.Empty,
                DateTime.Now,
                DateTime.Now,
                jobId);
        }

        public static Task UpdateAfterUploadResultAsync(long jobId, BulkQualificationUploadResult result, string filePath)
        {
            return UpdateAsync(
                @"UPDATE public.bulk_execution_jobs
                  SET status = @p0,
                      upload_result_file_path = @p1,
                      reception_number = @p2,
                      segment_of_result = @p3,
                      processing_result_status = @p4,
                      processing_result_code = @p5,
                      processing_result_message = @p6,
                      next_retry_at = @p7,
                      updated_at = @p8
                  WHERE id = @p9",
                "pending_download_request",
                filePath ?? string.Empty,
                result == null ? string.Empty : result.ReceptionNumber ?? string.Empty,
                result == null ? string.Empty : result.SegmentOfResult ?? string.Empty,
                result == null ? string.Empty : result.ProcessingResultStatus ?? string.Empty,
                result == null ? string.Empty : result.ProcessingResultCode ?? string.Empty,
                result == null ? string.Empty : result.ProcessingResultMessage ?? string.Empty,
                DateTime.Now,
                DateTime.Now,
                jobId);
        }

        public static Task UpdateAfterDownloadRequestAsync(long jobId, string filePath)
        {
            return UpdateAsync(
                @"UPDATE public.bulk_execution_jobs
                  SET status = @p0,
                      download_request_file_path = @p1,
                      download_result_file_path = NULL,
                      next_retry_at = @p2,
                      updated_at = @p3
                  WHERE id = @p4",
                "waiting_download_result",
                filePath ?? string.Empty,
                DateTime.Now,
                DateTime.Now,
                jobId);
        }

        public static Task UpdateForRetryAsync(long jobId, int retryCount, int pollIntervalSeconds, BulkQualificationImportResult result, string filePath)
        {
            return UpdateAsync(
                @"UPDATE public.bulk_execution_jobs
                  SET status = @p0,
                      retry_count = @p1,
                      download_result_file_path = @p2,
                      segment_of_result = @p3,
                      processing_result_status = @p4,
                      processing_result_code = @p5,
                      processing_result_message = @p6,
                      download_request_file_path = NULL,
                      next_retry_at = @p7,
                      updated_at = @p8
                  WHERE id = @p9",
                "pending_download_request",
                retryCount,
                filePath ?? string.Empty,
                result == null ? string.Empty : result.SegmentOfResult ?? string.Empty,
                result == null ? string.Empty : result.ProcessingResultStatus ?? string.Empty,
                result == null ? string.Empty : result.ProcessingResultCode ?? string.Empty,
                result == null ? string.Empty : result.ProcessingResultMessage ?? string.Empty,
                DateTime.Now.AddSeconds(pollIntervalSeconds),
                DateTime.Now,
                jobId);
        }

        public static Task MarkCompletedAsync(long jobId, BulkQualificationImportResult result, string filePath)
        {
            return UpdateAsync(
                @"UPDATE public.bulk_execution_jobs
                  SET status = @p0,
                      download_result_file_path = @p1,
                      segment_of_result = @p2,
                      processing_result_status = @p3,
                      processing_result_code = @p4,
                      processing_result_message = @p5,
                      next_retry_at = NULL,
                      updated_at = @p6
                  WHERE id = @p7",
                "completed",
                filePath ?? string.Empty,
                result == null ? string.Empty : result.SegmentOfResult ?? string.Empty,
                result == null ? string.Empty : result.ProcessingResultStatus ?? string.Empty,
                result == null ? string.Empty : result.ProcessingResultCode ?? string.Empty,
                result == null ? string.Empty : result.ProcessingResultMessage ?? string.Empty,
                DateTime.Now,
                jobId);
        }

        public static Task MarkFailedAsync(long jobId, string message, string resultCode = null, string resultStatus = null, string segment = null)
        {
            return UpdateAsync(
                @"UPDATE public.bulk_execution_jobs
                  SET status = @p0,
                      segment_of_result = COALESCE(@p1, segment_of_result),
                      processing_result_status = COALESCE(@p2, processing_result_status),
                      processing_result_code = COALESCE(@p3, processing_result_code),
                      processing_result_message = @p4,
                      next_retry_at = NULL,
                      updated_at = @p5
                  WHERE id = @p6",
                "failed",
                ToDbNullable(segment),
                ToDbNullable(resultStatus),
                ToDbNullable(resultCode),
                message ?? string.Empty,
                DateTime.Now,
                jobId);
        }

        private static async Task<BulkAutoExecutionJob> LoadSingleAsync(string sql, params object[] parameters)
        {
            using (IDbConnection connection = CommonFunctions.GetDbConnection(false))
            {
                await OpenAsync(connection).ConfigureAwait(false);
                using (IDbCommand command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        AddParameter(command, "@p" + i.ToString(), parameters[i]);
                    }

                    using (DbDataReader reader = await ((DbCommand)command).ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        if (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            return Map(reader);
                        }
                    }
                }
            }

            return null;
        }

        private static async Task UpdateAsync(string sql, params object[] parameters)
        {
            using (IDbConnection connection = CommonFunctions.GetDbConnection(false))
            {
                await OpenAsync(connection).ConfigureAwait(false);
                using (IDbCommand command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        AddParameter(command, "@p" + i.ToString(), parameters[i]);
                    }

                    await ExecuteNonQueryAsync(command).ConfigureAwait(false);
                }
            }
        }

        private static async Task<BulkAutoExecutionJob> CreateJobAsync(
            string jobKind,
            string requestMode,
            DateTime? consentDateFrom,
            DateTime? consentDateTo,
            DateTime? examinationDateFrom,
            DateTime? examinationDateTo,
            string medicalTreatmentMonth,
            int maxRetryCount)
        {
            HostIdentity host = GetHostIdentity();
            DateTime now = DateTime.Now;

            const string sql = @"
INSERT INTO public.bulk_execution_jobs (
    job_kind, status, request_mode,
    consent_date_from, consent_date_to,
    examination_date_from, examination_date_to, medical_treatment_month,
    retry_count, max_retry_count, next_retry_at,
    host_name, host_ip, created_at, updated_at)
VALUES (
    @p0, @p1, @p2,
    @p3, @p4,
    @p5, @p6, @p7,
    @p8, @p9, @p10,
    @p11, @p12, @p13, @p14)
RETURNING *";

            using (IDbConnection connection = CommonFunctions.GetDbConnection(false))
            {
                await OpenAsync(connection).ConfigureAwait(false);
                using (IDbCommand command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    AddParameter(command, "@p0", jobKind);
                    AddParameter(command, "@p1", "pending_request");
                    AddParameter(command, "@p2", requestMode);
                    AddParameter(command, "@p3", consentDateFrom.HasValue ? (object)consentDateFrom.Value : DBNull.Value);
                    AddParameter(command, "@p4", consentDateTo.HasValue ? (object)consentDateTo.Value : DBNull.Value);
                    AddParameter(command, "@p5", examinationDateFrom.HasValue ? (object)examinationDateFrom.Value : DBNull.Value);
                    AddParameter(command, "@p6", examinationDateTo.HasValue ? (object)examinationDateTo.Value : DBNull.Value);
                    AddParameter(command, "@p7", string.IsNullOrWhiteSpace(medicalTreatmentMonth) ? (object)DBNull.Value : medicalTreatmentMonth);
                    AddParameter(command, "@p8", 0);
                    AddParameter(command, "@p9", maxRetryCount);
                    AddParameter(command, "@p10", now);
                    AddParameter(command, "@p11", host.Name);
                    AddParameter(command, "@p12", host.IpAddress);
                    AddParameter(command, "@p13", now);
                    AddParameter(command, "@p14", now);

                    using (DbDataReader reader = await ((DbCommand)command).ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        if (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            return Map(reader);
                        }
                    }
                }
            }

            throw new InvalidOperationException("Bulk自動実行ジョブの作成に失敗しました。");
        }

        private static BulkAutoExecutionJob Map(IDataRecord record)
        {
            return new BulkAutoExecutionJob
            {
                Id = ReadInt64(record, "id"),
                JobKind = ReadString(record, "job_kind"),
                Status = ReadString(record, "status"),
                RequestMode = ReadString(record, "request_mode"),
                ConsentDateFrom = ReadNullableDateTime(record, "consent_date_from"),
                ConsentDateTo = ReadNullableDateTime(record, "consent_date_to"),
                ExaminationDateFrom = ReadNullableDateTime(record, "examination_date_from"),
                ExaminationDateTo = ReadNullableDateTime(record, "examination_date_to"),
                MedicalTreatmentMonth = ReadString(record, "medical_treatment_month"),
                UploadRequestFilePath = ReadString(record, "upload_request_file_path"),
                UploadResultFilePath = ReadString(record, "upload_result_file_path"),
                DownloadRequestFilePath = ReadString(record, "download_request_file_path"),
                DownloadResultFilePath = ReadString(record, "download_result_file_path"),
                ReceptionNumber = ReadString(record, "reception_number"),
                SegmentOfResult = ReadString(record, "segment_of_result"),
                ProcessingResultStatus = ReadString(record, "processing_result_status"),
                ProcessingResultCode = ReadString(record, "processing_result_code"),
                ProcessingResultMessage = ReadString(record, "processing_result_message"),
                RetryCount = ReadInt32(record, "retry_count"),
                MaxRetryCount = ReadInt32(record, "max_retry_count"),
                NextRetryAt = ReadNullableDateTime(record, "next_retry_at"),
                HostName = ReadString(record, "host_name"),
                HostIp = ReadString(record, "host_ip"),
                CreatedAt = ReadDateTime(record, "created_at"),
                UpdatedAt = ReadDateTime(record, "updated_at")
            };
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

        private static object ToDbNullable(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? (object)DBNull.Value : value;
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

        private static DateTime? ReadNullableDateTime(IDataRecord record, string columnName)
        {
            int ordinal = record.GetOrdinal(columnName);
            return record.IsDBNull(ordinal) ? (DateTime?)null : Convert.ToDateTime(record.GetValue(ordinal));
        }

        private sealed class HostIdentity
        {
            public string Name { get; set; } = string.Empty;
            public string IpAddress { get; set; } = string.Empty;
        }

        private static HostIdentity GetHostIdentity()
        {
            string hostName = Environment.MachineName ?? string.Empty;
            string hostIp = string.Empty;

            try
            {
                IPAddress[] addresses = Dns.GetHostAddresses(Dns.GetHostName());
                IPAddress address = addresses.FirstOrDefault(ip =>
                    ip.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(ip));
                if (address != null)
                {
                    hostIp = address.ToString();
                }
            }
            catch
            {
                hostIp = string.Empty;
            }

            return new HostIdentity
            {
                Name = hostName,
                IpAddress = hostIp
            };
        }
    }
}
