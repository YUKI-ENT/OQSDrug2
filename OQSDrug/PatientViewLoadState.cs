using System.Threading;

namespace OQSDrug
{
    // Older asynchronous reads must not overwrite a newer patient selection or refresh.
    internal sealed class PatientViewLoadState
    {
        private int revision;
        public int Begin() => Interlocked.Increment(ref revision);
        public bool IsCurrent(int value) => value == Volatile.Read(ref revision);
    }
}
