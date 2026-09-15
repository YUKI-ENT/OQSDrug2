using System;
using System.Threading;
using System.Threading.Tasks;

namespace OQSDrug
{
    // One receiver, independent of the request timer. Notifications accelerate periodic scans.
    internal sealed class ResImportWorker : IDisposable
    {
        private readonly Func<bool> enabled;
        private readonly Func<Task> scan;
        private readonly Func<Exception, Task> reportError;
        private readonly int pollMilliseconds;
        private readonly int settleMilliseconds;
        private readonly SemaphoreSlim wake = new SemaphoreSlim(0, 1);
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private readonly object lifecycle = new object();
        private bool stopped;

        public Task Completion { get; }

        public ResImportWorker(Func<bool> enabled, Func<Task> scan, Func<Exception, Task> reportError,
            int pollMilliseconds = 5000, int settleMilliseconds = 2000)
        {
            if (pollMilliseconds <= 0 || settleMilliseconds < 0) throw new ArgumentOutOfRangeException();
            this.enabled = enabled ?? throw new ArgumentNullException(nameof(enabled));
            this.scan = scan ?? throw new ArgumentNullException(nameof(scan));
            this.reportError = reportError ?? throw new ArgumentNullException(nameof(reportError));
            this.pollMilliseconds = pollMilliseconds;
            this.settleMilliseconds = settleMilliseconds;
            Completion = Task.Run(RunAsync);
        }

        public void Notify()
        {
            lock (lifecycle)
            {
                if (!stopped && wake.CurrentCount == 0) wake.Release();
            }
        }

        private async Task RunAsync()
        {
            var token = cancellation.Token;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    bool notified = await wake.WaitAsync(pollMilliseconds, token).ConfigureAwait(false);
                    if (!enabled()) continue;
                    if (notified && settleMilliseconds > 0)
                        await Task.Delay(settleMilliseconds, token).ConfigureAwait(false);
                    if (!enabled()) continue;
                    token.ThrowIfCancellationRequested();
                    try
                    {
                        // Await the full pass. Notifications arriving during it remain pending.
                        // A missing notification or failed pass is recovered by the next periodic scan.
                        await scan().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        try { await reportError(ex).ConfigureAwait(false); }
                        catch { /* Logging failure must not stop the receiver. */ }
                    }
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        }

        public void Dispose()
        {
            lock (lifecycle)
            {
                if (stopped) return;
                stopped = true;
                cancellation.Cancel();
            }
            // Let an in-flight pass finish; never start a replacement receiver on watcher changes.
            Completion.ContinueWith(_ =>
            {
                wake.Dispose();
                cancellation.Dispose();
            }, TaskScheduler.Default);
        }
    }
}
