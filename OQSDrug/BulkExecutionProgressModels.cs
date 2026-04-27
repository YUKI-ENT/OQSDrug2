using System;

namespace OQSDrug
{
    internal enum BulkStatusWindowMode
    {
        Hidden = 0,
        Minimized = 1,
        Normal = 2
    }

    internal sealed class BulkExecutionProgressInfo
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public BulkQualificationKind? Kind { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public string StatusText { get; set; } = string.Empty;
        public string DetailText { get; set; } = string.Empty;
        public string ReceptionNumber { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public bool HasError { get; set; }
        public int ImportedCount { get; set; }
    }
}
