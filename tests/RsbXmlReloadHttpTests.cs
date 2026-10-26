using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using OQSDrug;

internal static class RsbXmlReloadHttpTests
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static async Task<string> ReplyAsync(TcpListener listener, string reply, int delay = 0)
    {
        using (var client = await listener.AcceptTcpClientAsync())
        using (var stream = client.GetStream())
        using (var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true))
        {
            string firstLine = await reader.ReadLineAsync();
            while (!string.IsNullOrEmpty(await reader.ReadLineAsync())) { }
            if (delay > 0) await Task.Delay(delay);
            byte[] bytes = Encoding.GetEncoding("Shift_JIS").GetBytes(reply);
            try { await stream.WriteAsync(bytes, 0, bytes.Length); }
            catch (IOException) when (delay > 0) { /* Client timed out. */ }
            return firstLine;
        }
    }

    private static async Task RunAsync()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            string origin = "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port;
            string target = @"/~rsn/R_uketuke.cgi?xml_drug=Y:\res==auto_close";
            string body = "取込完了";
            int length = Encoding.GetEncoding("Shift_JIS").GetByteCount(body);
            Task<string> wire = ReplyAsync(listener, "HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=shift_jis\r\nContent-Length: " + length + "\r\nConnection: close\r\n\r\n" + body);
            using (var response = await RsbXmlReloadHttp.GetAsync(origin + target))
            {
                Check(response.StatusCode == HttpStatusCode.OK, "Status was lost");
                Check(response.Content.Headers.ContentType.CharSet == "shift_jis", "Charset was lost");
                Check(await response.Content.ReadAsStringAsync() == body, "Japanese response was lost");
                Check((string)response.RequestMessage.Properties[RsbXmlReloadHttp.FinalUrlProperty] == origin + target, "Final URL was escaped");
            }
            Check(await wire == "GET " + target + " HTTP/1.1", "Backslash was escaped on the wire");
            Console.WriteLine("PASS: literal Y:\\res on wire; status, charset, response body and URL preserved");

            wire = ReplyAsync(listener, "HTTP/1.1 404 Not Found\r\nContent-Length: 7\r\nConnection: close\r\n\r\nmissing");
            using (var response = await RsbXmlReloadHttp.GetAsync(origin + target))
            {
                Check(response.StatusCode == HttpStatusCode.NotFound, "Error status was lost");
                Check(await response.Content.ReadAsStringAsync() == "missing", "Error body must be available before EnsureSuccessStatusCode");
            }
            await wire;
            Console.WriteLine("PASS: non-success response body available for diagnostics");

            wire = ReplyAsync(listener, "HTTP/1.1 200 OK\r\nTransfer-Encoding: chunked\r\nConnection: close\r\n\r\n2\r\nOK\r\n0\r\n\r\n");
            using (var response = await RsbXmlReloadHttp.GetAsync(origin + target))
                Check(await response.Content.ReadAsStringAsync() == "OK", "Chunked response failed");
            await wire;
            Console.WriteLine("PASS: chunked response");

            string redirectedTarget = target + "&redirected=1";
            var redirectServer = Task.Run(async () =>
            {
                await ReplyAsync(listener, "HTTP/1.1 302 Found\r\nLocation: " + origin + redirectedTarget + "\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
                return await ReplyAsync(listener, "HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nOK");
            });
            using (var response = await RsbXmlReloadHttp.GetAsync(origin + target))
            {
                Check(response.StatusCode == HttpStatusCode.OK, "Redirect did not complete");
                Check((string)response.RequestMessage.Properties[RsbXmlReloadHttp.FinalUrlProperty] == origin + redirectedTarget, "Redirect final URL lost");
            }
            Check(await redirectServer == "GET " + redirectedTarget + " HTTP/1.1", "Redirect escaped query");
            Console.WriteLine("PASS: redirect preserves query and reports final URL");

            wire = ReplyAsync(listener, "HTTP/1.1 200 OK\r\nContent-Length: 0\r\nConnection: close\r\n\r\n", 500);
            bool timedOut = false;
            try { using (await RsbXmlReloadHttp.GetAsync(origin + target, 100)) { } }
            catch (TimeoutException) { timedOut = true; }
            Check(timedOut, "Response wait did not time out");
            await wire;
            Console.WriteLine("PASS: timeout without opening a browser");
        }
        finally { listener.Stop(); }
    }

    private static int Main()
    {
        try { RunAsync().GetAwaiter().GetResult(); return 0; }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
