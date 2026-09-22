using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OQSDrug
{
    // 実行中も含め同じ患者はまとめ、異なる患者はFIFOで保持する。
    internal sealed class AutoLlmQueue
    {
        private sealed class Job
        {
            internal long Patient;
            internal Func<long, Task> Process;
        }
        private readonly object sync = new object();
        private readonly Queue<Job> patients = new Queue<Job>();
        private readonly Dictionary<long, TaskCompletionSource<bool>> pending = new Dictionary<long, TaskCompletionSource<bool>>();
        private bool running;

        internal Task Enqueue(long patient, Func<long, Task> process)
        {
            lock (sync)
            {
                if (pending.TryGetValue(patient, out var existing)) return existing.Task;
                var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                pending.Add(patient, completion);
                patients.Enqueue(new Job { Patient = patient, Process = process });
                if (!running)
                {
                    running = true;
                    Task.Run(DrainAsync);
                }
                return completion.Task;
            }
        }

        private async Task DrainAsync()
        {
            while (true)
            {
                Job job;
                TaskCompletionSource<bool> completion;
                lock (sync)
                {
                    if (patients.Count == 0) { running = false; return; }
                    job = patients.Dequeue();
                    completion = pending[job.Patient];
                }
                Exception error = null;
                try { await job.Process(job.Patient).ConfigureAwait(false); }
                catch (Exception ex) { error = ex; }
                lock (sync)
                {
                    pending.Remove(job.Patient);
                    if (error == null) completion.TrySetResult(true);
                    else completion.TrySetException(error);
                }
            }
        }
    }
}
