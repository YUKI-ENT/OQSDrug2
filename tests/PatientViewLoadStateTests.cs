using System;
using System.Threading.Tasks;
using OQSDrug;

internal static class PatientViewLoadStateTests
{
    public static int Main()
    {
        try { Run().GetAwaiter().GetResult(); return 0; }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    static async Task Run()
    {
        var state = new PatientViewLoadState();
        string displayed = null;
        Func<string, TaskCompletionSource<bool>, Task> load = async (data, ready) =>
        {
            int revision = state.Begin();
            await ready.Task;
            if (state.IsCurrent(revision)) displayed = data;
        };

        // The form's initial pre-import query returns AFTER the post-import refresh.
        var oldReady = new TaskCompletionSource<bool>();
        var newReady = new TaskCompletionSource<bool>();
        Task oldRead = load("old history", oldReady);
        Task refreshedRead = load("imported history", newReady);
        newReady.SetResult(true);
        await refreshedRead;
        oldReady.SetResult(true);
        await oldRead;
        Require(displayed == "imported history", "Older read replaced imported data");
        Console.WriteLine("PASS: a late pre-import read cannot overwrite the import refresh");

        // Switch A -> B -> A while A's first query is still running.
        var firstAReady = new TaskCompletionSource<bool>();
        var bReady = new TaskCompletionSource<bool>();
        var secondAReady = new TaskCompletionSource<bool>();
        Task firstA = load("old A", firstAReady);
        Task b = load("B", bReady);
        Task secondA = load("latest A", secondAReady);
        secondAReady.SetResult(true);
        await secondA;
        bReady.SetResult(true);
        firstAReady.SetResult(true);
        await Task.WhenAll(firstA, b);
        Require(displayed == "latest A", "Patient switching displayed stale results");
        Console.WriteLine("PASS: switching A -> B -> A rejects both obsolete reads");

        var clearedReady = new TaskCompletionSource<bool>();
        Task cleared = load("cleared selection", clearedReady);
        state.Begin(); // selection cleared, no replacement DB query
        displayed = null;
        clearedReady.SetResult(true);
        await cleared;
        Require(displayed == null, "Cleared patient selection was repopulated by a stale read");
        Console.WriteLine("PASS: clearing selection invalidates the outstanding read");
    }

    static void Require(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }
}
