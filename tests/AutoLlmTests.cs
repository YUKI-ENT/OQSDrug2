using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using OQSDrug;

internal static class AutoLlmTests
{
    private static int count;
    private static void Check(bool value, string name)
    {
        count++;
        if (!value) throw new Exception(name);
    }
    private static async Task Run()
    {
        var queue = new AutoLlmQueue();
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var order = new List<long>();
        Func<long, Task> process = async patient =>
        {
            order.Add(patient);
            if (patient == 1) { entered.TrySetResult(true); await release.Task; }
            if (patient == 2) throw new InvalidOperationException("one patient fails");
        };
        var first = queue.Enqueue(1, process);
        await entered.Task;
        var second = queue.Enqueue(2, process);
        var third = queue.Enqueue(3, process);
        Check(ReferenceEquals(first, queue.Enqueue(1, process)), "deduplicate active patient");
        Check(ReferenceEquals(second, queue.Enqueue(2, process)), "deduplicate waiting patient");
        Check(!third.IsCompleted, "retain busy requests");
        release.SetResult(true);
        await first;
        try { await second; throw new Exception("expected failure"); }
        catch (InvalidOperationException) { }
        await third;
        Check(order.SequenceEqual(new long[] { 1, 2, 3 }), "FIFO continues after failure");
        await queue.Enqueue(2, _ => Task.CompletedTask);
        Check(true, "failed patient can be queued again");

        foreach (Exception transient in new Exception[] {
            new LlmHttpException("busy", 503, null), new LlmHttpException("rate limit", 429, null),
            new LlmHttpException("timeout", 408, null), new LlmHttpException("gateway", 502, null),
            new LlmHttpException("gateway timeout", 504, null), new TimeoutException(), new HttpRequestException() })
        {
            int calls = 0;
            var delays = new List<double>();
            string answer = await LlmRetryPolicy.ExecuteAsync(() =>
            {
                if (++calls < 3) throw transient;
                return Task.FromResult("ok");
            }, CancellationToken.None, delay: (wait, ct) => { delays.Add(wait.TotalSeconds); return Task.CompletedTask; });
            Check(answer == "ok" && calls == 3 && delays.SequenceEqual(new double[] { 15, 45 }), "transient recovery " + transient.Message);
        }
        foreach (int status in new[] { 400, 401, 403, 404, 422 })
        {
            int calls = 0;
            try
            {
                await LlmRetryPolicy.ExecuteAsync(() => { calls++; throw new LlmHttpException("permanent", status, null); }, CancellationToken.None);
                throw new Exception("expected permanent error");
            }
            catch (LlmHttpException) { Check(calls == 1, "no retry for " + status); }
        }
        int attempts = 0;
        try
        {
            await LlmRetryPolicy.ExecuteAsync(() => { attempts++; throw new TimeoutException(); }, CancellationToken.None,
                delay: (wait, ct) => Task.CompletedTask);
            throw new Exception("expected retry exhaustion");
        }
        catch (TimeoutException) { Check(attempts == 3, "retry limit"); }
        int retryCalls = 0;
        TimeSpan observed = TimeSpan.Zero;
        await LlmRetryPolicy.ExecuteAsync(() =>
        {
            if (++retryCalls == 1) throw new LlmHttpException("retry after", 429, TimeSpan.FromSeconds(75));
            return Task.FromResult("ok");
        }, CancellationToken.None, delay: (wait, ct) => { observed = wait; return Task.CompletedTask; });
        Check(observed.TotalSeconds == 75, "Retry-After respected");
        using (var cts = new CancellationTokenSource())
        {
            int calls = 0;
            try
            {
                await LlmRetryPolicy.ExecuteAsync(() => { calls++; throw new TimeoutException(); }, cts.Token,
                    delay: (wait, ct) => { cts.Cancel(); return Task.FromCanceled(ct); });
                throw new Exception("expected cancel");
            }
            catch (OperationCanceledException) { Check(calls == 1, "cancel while waiting"); }
        }
        Console.WriteLine("PASS: " + count + " assertions");
    }
    private static int Main()
    {
        var run = Run();
        if (!run.Wait(TimeSpan.FromSeconds(10))) throw new Exception("tests timed out");
        run.GetAwaiter().GetResult();
        return 0;
    }
}
