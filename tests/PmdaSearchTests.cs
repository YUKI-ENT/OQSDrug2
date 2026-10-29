using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

internal static partial class SearchHost
{
    public static List<string[]> SGMLDI = new List<string[]>();
    public static List<Tuple<string[], double>> DatabaseResults = new List<Tuple<string[], double>>();
    public static int DatabaseCalls;
    public static string DatabaseCode;

    private static Task<List<Tuple<string[], double>>> SearchSgmlRawdataAsync(string name, string ingredient, string code)
    {
        DatabaseCalls++;
        DatabaseCode = code;
        return Task.FromResult(DatabaseResults);
    }
}

internal static class PmdaSearchTests
{
    private const string Code = "2142004F1010";
    private const string Name = "アプレゾリン錠１０ｍｇ";
    private static readonly string[] Correct = { "アプレゾリン錠10mg", "ヒドララジン塩酸塩", Code, "先発", "10.8", "test" };
    private static readonly string[] Wrong = { "テルネリン錠1mg", "チザニジン塩酸塩", "unrelated", "先発", "", "test" };

    private static void Assert(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }

    private static void Reset(params string[][] records)
    {
        SearchHost.SGMLDI = records.ToList();
        SearchHost.DatabaseResults.Clear();
        SearchHost.DatabaseCalls = 0;
    }

    public static void Main()
    {
        // Reproduce the incorrect early return: even an exact name with a different
        // YJ must not prevent looking for the prescribed YJ in the raw database.
        var misleading = new[] { Name, "", "different", "先発", "", "test" };
        Reset(Wrong, misleading);
        SearchHost.DatabaseResults.Add(Tuple.Create(Correct, 2.0));
        var results = SearchHost.SearchSgmlCandidatesAsync(Name, "", Code).GetAwaiter().GetResult();
        Assert(SearchHost.DatabaseCalls == 1 && SearchHost.DatabaseCode == Code, "Missing indexed YJ must reach database with its code");
        Assert(results[0].Item1 == Correct, "Unrelated fuzzy candidate hid the prescribed drug");

        Reset(Wrong, Correct, misleading);
        results = SearchHost.SearchSgmlCandidatesAsync(Name, "", " 2142004f1010 ").GetAwaiter().GetResult();
        Assert(results[0].Item1 == Correct && SearchHost.DatabaseCalls == 1, "Cached exact YJ should win while still searching other brands");

        var samePrefix = (string[])Correct.Clone();
        // The ninth character differs: only the first eight characters match.
        samePrefix[2] = "2142004F2999";
        Reset(samePrefix);
        SearchHost.DatabaseResults.Add(Tuple.Create(Correct, 2.0));
        results = SearchHost.SearchSgmlCandidatesAsync(Name, "", Code).GetAwaiter().GetResult();
        Assert(results[0].Item1 == Correct && SearchHost.DatabaseCalls == 1, "Cached prefix must not hide database exact YJ");

        Reset(samePrefix, Wrong);
        results = SearchHost.SearchSgmlCandidatesAsync(Name, "", Code).GetAwaiter().GetResult();
        Assert(results[0].Item1 == samePrefix && results[0].Item2 == 1.9, "Eight-character prefix fallback must match even when ninth character differs");

        Reset(Correct, Wrong);
        results = SearchHost.SearchSgmlCandidatesAsync(Name, "", "").GetAwaiter().GetResult();
        Assert(results[0].Item1 == Correct && SearchHost.DatabaseCalls == 1, "Name fallback must normalize full-width dosage");

        Reset(Wrong);
        results = SearchHost.SearchSgmlCandidatesAsync("", "", "").GetAwaiter().GetResult();
        Assert(results.Count == 0 && SearchHost.DatabaseCalls == 0, "Empty input must not search");
        // Synthetic codes: verify brand enumeration without depending on a real DB.
        var generic = new[] { "レボフロキサシン錠（テスト後発品）", "レボフロキサシン", "9999999F2000", "", "", "test" };
        var originator = new[] { "クラビット錠（テスト先発品）", "レボフロキサシン", "9999999F1000", "先発", "", "test" };
        var other = new[] { "別メーカー錠", "レボフロキサシン", "9999999F3000", "", "", "test" };
        Reset(generic, other, Wrong);
        SearchHost.DatabaseResults.Add(Tuple.Create(originator, 1.9));
        SearchHost.DatabaseResults.Add(Tuple.Create(generic, 2.0));
        results = SearchHost.SearchSgmlCandidatesAsync(generic[0], generic[1], generic[2]).GetAwaiter().GetResult();
        Assert(results.Count == 3 && results[0].Item1 == generic && results[1].Item1 == originator
            && results[2].Item1 == other, "Exact generic must retain originator and other generics, without duplicate YJs or unrelated names");

        // All code-family candidates must survive the old 20-result cutoff.
        Reset(generic, originator);
        for (int i = 3000; i < 3030; i++)
            SearchHost.SGMLDI.Add(new[] { "テスト後発品" + i, "", "9999999F" + i, "", "", "test" });
        results = SearchHost.SearchSgmlCandidatesAsync(generic[0], generic[1], generic[2]).GetAwaiter().GetResult();
        Assert(results.Count == 32 && results[0].Item1 == generic && results[1].Item1 == originator,
            "Code-family enumeration must not drop brands beyond 20 results");
        Console.WriteLine("PASS: 8 PMDA search regression scenarios.");
    }
}
