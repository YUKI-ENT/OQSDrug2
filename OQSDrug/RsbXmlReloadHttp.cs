using System;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace OQSDrug
{
    internal static class RsbXmlReloadHttp
    {
        internal const string FinalUrlProperty = "RsbXmlReloadFinalUrl";

        public static Task<HttpResponseMessage> GetAsync(string url, int timeoutMilliseconds = 30000)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                || url.IndexOf('\r') >= 0 || url.IndexOf('\n') >= 0)
                throw new ArgumentException("URLはhttp://またはhttps://で指定してください。", nameof(url));
            if (timeoutMilliseconds <= 0) throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds));

            // WinHTTP accepts the original query string without System.Uri converting
            // Windows folder separators to %5C. RSBase does not decode that escape.
            // All COM calls stay on this worker; no browser or UI control is involved.
            return Task.Run(() =>
            {
                dynamic request = Activator.CreateInstance(Type.GetTypeFromProgID("WinHttp.WinHttpRequest.5.1", throwOnError: true));
                try
                {
                    request.SetTimeouts(timeoutMilliseconds, timeoutMilliseconds, timeoutMilliseconds, timeoutMilliseconds);
                    request.Open("GET", url, true);
                    // WinHttpRequestOption_UrlEscapeDisableQuery: do not escape the query.
                    request.Option[8] = true;
                    request.Send();
                    if (!(bool)request.WaitForResponse(timeoutMilliseconds / 1000.0))
                    {
                        request.Abort();
                        throw new TimeoutException("RSBase HTTP応答待ちがタイムアウトしました。");
                    }

                    var response = new HttpResponseMessage((HttpStatusCode)(int)request.Status);
                    try
                    {
                        response.ReasonPhrase = (string)request.StatusText;
                        response.Content = new ByteArrayContent((byte[])request.ResponseBody);
                        response.RequestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                        // Keep the WinHTTP URL separately: RequestUri would escape it again.
                        response.RequestMessage.Properties[FinalUrlProperty] = (string)request.Option[1];
                        string headers = (string)request.GetAllResponseHeaders();
                        foreach (string line in headers.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            int colon = line.IndexOf(':');
                            if (colon <= 0) continue;
                            string name = line.Substring(0, colon);
                            string value = line.Substring(colon + 1).Trim();
                            if (!response.Headers.TryAddWithoutValidation(name, value))
                                response.Content.Headers.TryAddWithoutValidation(name, value);
                        }
                        return response;
                    }
                    catch
                    {
                        response.Dispose();
                        throw;
                    }
                }
                catch (COMException ex) when ((ex.ErrorCode & 0xffff) == 12002)
                {
                    throw new TimeoutException("RSBase HTTP通信がタイムアウトしました。", ex);
                }
                finally
                {
                    Marshal.FinalReleaseComObject(request);
                }
            });
        }
    }
}
