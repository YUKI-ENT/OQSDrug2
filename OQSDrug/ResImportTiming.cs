using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace OQSDrug
{
    // One instance per file. Repeated SQL timings are aggregated, never logged per row.
    internal sealed class ResImportTiming : IDisposable
    {
        private sealed class Stat
        {
            public string Name;
            public int Count;
            public double Total;
            public double Max;
        }

        private readonly string context;
        private readonly Action<string> log;
        private readonly Stopwatch total = Stopwatch.StartNew();
        private readonly List<Stat> stats = new List<Stat>();

        public ResImportTiming(string context, Action<string> log)
        {
            this.context = context;
            this.log = log;
            Write("開始");
        }

        private void Write(string message)
        {
            try { log("[res計測] " + context + " " + message); }
            catch { /* Diagnostics must not change import behavior. */ }
        }

        public IDisposable Measure(string name, bool progress = false)
        {
            var stat = stats.Find(s => s.Name == name);
            if (stat == null)
            {
                stat = new Stat { Name = name };
                stats.Add(stat);
            }
            if (progress) Write(name + " 開始");
            return new Scope(this, stat, progress);
        }

        public void Dispose()
        {
            total.Stop();
            var parts = new List<string>();
            foreach (var stat in stats)
                parts.Add($"{stat.Name}={stat.Total:F1}ms/{stat.Count}回(最大{stat.Max:F1}ms)");
            Write($"計測終了（処理成功を示すものではありません） 合計={total.Elapsed.TotalMilliseconds:F1}ms; " + string.Join("; ", parts));
        }

        private sealed class Scope : IDisposable
        {
            private readonly ResImportTiming owner;
            private readonly Stat stat;
            private readonly bool progress;
            private readonly Stopwatch watch = Stopwatch.StartNew();
            private bool disposed;

            public Scope(ResImportTiming owner, Stat stat, bool progress)
            {
                this.owner = owner;
                this.stat = stat;
                this.progress = progress;
            }

            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                watch.Stop();
                double ms = watch.Elapsed.TotalMilliseconds;
                stat.Count++;
                stat.Total += ms;
                stat.Max = Math.Max(stat.Max, ms);
                if (progress) owner.Write($"{stat.Name} 終了 {ms:F1}ms");
            }
        }
    }
}
