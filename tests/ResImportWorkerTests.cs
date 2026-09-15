using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OQSDrug;

internal static class ResImportWorkerTests
{
    static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    static async Task Until(Func<bool> condition, string message)
    {
        for (int i = 0; i < 200; i++)
        {
            if (condition()) return;
            await Task.Delay(10);
        }
        throw new Exception(message);
    }

    public static int Main()
    {
        try { Run().GetAwaiter().GetResult(); return 0; }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    static async Task Run()
    {
        // Simulate outstanding reqResults: late responses are not in the first pass's file snapshot.
        var available = new ConcurrentDictionary<int, bool>();
        var completed = new ConcurrentDictionary<int, bool>();
        for (int i = 0; i < 20; i++) available[i] = true;
        var firstPass = new TaskCompletionSource<bool>();
        var release = new TaskCompletionSource<bool>();
        int passes = 0, active = 0, maxActive = 0;
        using (var worker = new ResImportWorker(() => true, async () =>
        {
            int concurrent = Interlocked.Increment(ref active);
            maxActive = Math.Max(maxActive, concurrent);
            try
            {
                var snapshot = available.Keys.ToArray();
                if (Interlocked.Increment(ref passes) == 1)
                {
                    firstPass.SetResult(true);
                    await release.Task;
                }
                foreach (int id in snapshot)
                {
                    await Task.Delay(2);
                    completed[id] = true;
                }
            }
            finally { Interlocked.Decrement(ref active); }
        }, ex => { throw ex; }, pollMilliseconds: 30, settleMilliseconds: 0))
        {
            await Until(() => firstPass.Task.IsCompleted, "Existing responses were not scanned without watcher events");
            // More than several simulated request-timer periods pass while the first import is active.
            await Task.Delay(150);
            Assert(active == 1 && completed.IsEmpty, "Slow import was interrupted or overlapped");
            for (int i = 20; i < 25; i++) available[i] = true; // deliberately no notification
            release.SetResult(true);
            await Until(() => completed.Count == 25, "Late responses were stranded after a missed event");
            Assert(maxActive == 1, "Response passes ran concurrently");
        }
        Console.WriteLine("PASS: 25 responses including late arrivals; no-event polling; long import survives; one pass at a time");

        var inProgress = new TaskCompletionSource<bool>();
        var finish = new TaskCompletionSource<bool>();
        int notificationPasses = 0;
        using (var worker = new ResImportWorker(() => true, async () =>
        {
            if (Interlocked.Increment(ref notificationPasses) == 1)
            {
                inProgress.SetResult(true);
                await finish.Task;
            }
        }, ex => Task.CompletedTask, pollMilliseconds: 10000, settleMilliseconds: 0))
        {
            worker.Notify();
            await Until(() => inProgress.Task.IsCompleted, "Initial notification did not start a pass");
            for (int i = 0; i < 20; i++) worker.Notify();
            finish.SetResult(true);
            await Until(() => notificationPasses >= 2, "Events during import were lost until the polling timeout");
        }
        Console.WriteLine("PASS: notifications received during processing trigger the next pass");

        int attempts = 0, errors = 0;
        using (var worker = new ResImportWorker(() => true, () =>
        {
            int attempt = Interlocked.Increment(ref attempts);
            if (attempt == 1) return Task.CompletedTask; // DB busy: a skipped pass
            if (attempt == 2) throw new InvalidOperationException("Simulated DB error");
            return Task.CompletedTask;
        }, ex => { Interlocked.Increment(ref errors); return Task.CompletedTask; }, 20, 0))
        {
            await Until(() => attempts >= 3, "Skipped/failed pass was not retried");
            Assert(errors == 1, "Exception was not reported");
        }
        Console.WriteLine("PASS: DB-busy skip and exception are retried without new events");

        int enabled = 0, runs = 0;
        var stopStarted = new TaskCompletionSource<bool>();
        var finishOnStop = new TaskCompletionSource<bool>();
        var stoppable = new ResImportWorker(() => Volatile.Read(ref enabled) != 0, async () =>
        {
            Interlocked.Increment(ref runs);
            stopStarted.SetResult(true);
            await finishOnStop.Task;
        }, ex => Task.CompletedTask, 20, 0);
        await Task.Delay(80);
        Assert(runs == 0, "Import started while acquisition was stopped");
        Interlocked.Exchange(ref enabled, 1);
        await Until(() => stopStarted.Task.IsCompleted, "Import did not resume");
        stoppable.Dispose();
        Assert(!stoppable.Completion.IsCompleted, "Disposal interrupted an active pass");
        stoppable.Notify(); // no replacement pass on watcher/lifecycle changes
        finishOnStop.SetResult(true);
        await Until(() => stoppable.Completion.IsCompleted, "Worker did not exit after its active pass");
        Assert(runs == 1, "A new pass started after disposal");
        Console.WriteLine("PASS: paused state, resume, and shutdown after the active pass");
    }
}
