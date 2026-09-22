using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OQSDrug
{
    internal sealed class LlmHttpException : HttpRequestException
    {
        internal int StatusCode { get; }
        internal TimeSpan? RetryAfter { get; }
        internal LlmHttpException(string message, int statusCode, TimeSpan? retryAfter) : base(message)
        {
            StatusCode = statusCode;
            RetryAfter = retryAfter;
        }
    }

    internal static class LlmRetryPolicy
    {
        internal static bool IsTransient(Exception error)
        {
            if (error is LlmHttpException http)
                return http.StatusCode == 408 || http.StatusCode == 429 || http.StatusCode == 502
                    || http.StatusCode == 503 || http.StatusCode == 504;
            return error is TimeoutException || error is HttpRequestException;
        }

        internal static async Task<string> ExecuteAsync(Func<Task<string>> send, CancellationToken ct,
            Action<string> onStatus = null, Func<TimeSpan, CancellationToken, Task> delay = null)
        {
            delay = delay ?? ((span, token) => Task.Delay(span, token));
            for (int attempt = 1; ; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                try { return await send().ConfigureAwait(false); }
                catch (Exception ex) when (!ct.IsCancellationRequested && attempt < 3 && IsTransient(ex))
                {
                    var retryAfter = (ex as LlmHttpException)?.RetryAfter;
                    // サーバー指定の待ち時間を尊重。指定がなければ15秒、45秒。
                    var wait = retryAfter.HasValue && retryAfter.Value > TimeSpan.Zero
                        ? retryAfter.Value : TimeSpan.FromSeconds(attempt == 1 ? 15 : 45);
                    onStatus?.Invoke($"一時エラー: {wait.TotalSeconds:0}秒後に再試行 ({attempt + 1}/3): {ex.Message}");
                    await delay(wait, ct).ConfigureAwait(false);
                }
            }
        }
    }
}
