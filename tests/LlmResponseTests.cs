using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
class LlmResponseTests {
 static Type common;
 static int checks;
 static void Check(bool ok) { if (!ok) throw new Exception("Assertion " + checks); checks++; }
 static async Task Run() {
  var dt = new DataTable(); dt.Columns.Add("options_json", typeof(string));
  var row = dt.Rows.Add("{}");
  foreach (string json in new[] { "{}", "{\"disableThinking\":true}", "{\"disableThinking\":false}", "bad json", "{\"disableThinking\":\"invalid\"}" }) {
   row[0] = json;
   Check((bool)common.GetMethod("IsThinkingDisabled").Invoke(null, new object[] {row}) == json.Contains(":true"));
  }
  var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
  try {
   int port = ((IPEndPoint)listener.LocalEndpoint).Port;
   foreach (bool disabled in new[] {true, false, true}) {
    var accept = listener.AcceptTcpClientAsync();
    var task = (Task<string>)common.GetMethod("CallLlmQueuedAsync").Invoke(null,
     new object[] {"http://127.0.0.1:" + port + "/v1/chat/completions", "test-model", "test-prompt", 5000, CancellationToken.None, null, false, disabled});
    using (var client = await accept) {
     var stream = client.GetStream();
     var reader = new StreamReader(stream, Encoding.UTF8, false, 1024, true);
     int length = 0; string line;
     while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync()))
      if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase)) length = int.Parse(line.Substring(15));
     var chars = new char[length]; int read = 0;
     while (read < length) read += await reader.ReadAsync(chars, read, length - read);
     var body = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(new string(chars));
     Check(disabled ? body.ContainsKey("reasoning_effort") && (string)body["reasoning_effort"] == "none" : !body.ContainsKey("reasoning_effort"));
     Check((string)body["model"] == "test-model"); Check(!body.ContainsKey("thinking"));
     var response = Encoding.UTF8.GetBytes("{\"choices\":[{\"message\":{\"content\":\"ok\"}}]}");
     var header = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: " + response.Length + "\r\nConnection: close\r\n\r\n");
     await stream.WriteAsync(header, 0, header.Length); await stream.WriteAsync(response, 0, response.Length);
    }
    Check(await task == "ok");
   }
  } finally { listener.Stop(); }
    var flags = BindingFlags.NonPublic | BindingFlags.Static;
  var describe = common.GetMethod("DescribeLlmResponse", flags);
  var extract = common.GetMethod("ExtractChatCompletionText", flags);
  Func<string, string> summary = body => (string)describe.Invoke(null, new object[] {body});
  Func<string, string> text = body => (string)extract.Invoke(null, new object[] {body});
  Check(summary("").Contains("empty_body"));
  Check(summary("<html>bad gateway</html>").Contains("invalid_json"));
  Check(summary("[]").Contains("not_object"));
  Check(summary("{\"message\":{\"content\":\"secret\"}}").Contains("\"nativeOllamaResponse\":true"));
  string reasoningOnly = "{\"choices\":[{\"finish_reason\":\"length\",\"message\":{\"content\":null,\"reasoning_content\":\"private-thought\"}}],\"usage\":{\"prompt_tokens\":20,\"completion_tokens\":50,\"total_tokens\":70}}";
  string details = summary(reasoningOnly);
  Check(details.Contains("\"finish_reason\":\"length\""));
  Check(details.Contains("\"reasoningChars\":15"));
  Check(details.Contains("\"completion_tokens\":50"));
  Check(!details.Contains("private-thought"));
  Check(text(reasoningOnly) == "");
  Check(text("{\"choices\":[{\"message\":{\"content\":[{\"type\":\"text\",\"text\":\"answer\"}]}}]}") == "answer");
  Check(text("{\"choices\":[{\"message\":{\"content\":[]}}]}") == "");
  try { text(""); throw new Exception("Expected empty-body failure"); }
  catch (TargetInvocationException e) { Check(e.InnerException.Message.Contains("HTTP")); }
  try { text("{\"choices\":[]}"); throw new Exception("Expected missing-content failure"); }
  catch (TargetInvocationException e) { Check(e.InnerException.Message.Contains("choices")); }
  Check(!summary("{\"choices\":[{\"message\":{\"content\":\"private-answer\"}}]}").Contains("private-answer"));
  Console.WriteLine("PASS: " + checks + " LLM response assertions");
 }
 static void Main(string[] args) {
  AppDomain.CurrentDomain.AssemblyResolve += (s,e) => { var p = Path.Combine(args[0], new AssemblyName(e.Name).Name + ".dll"); return File.Exists(p) ? Assembly.LoadFrom(p) : null; };
  common = Assembly.LoadFrom(Path.Combine(args[0], "OQSDrug.exe")).GetType("OQSDrug.CommonFunctions");
  var task = Run(); if (!task.Wait(20000)) throw new Exception("Timeout"); task.GetAwaiter().GetResult();
 }
}

