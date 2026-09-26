using System;
using System.Collections.Generic;
using System.Linq;
using OQSDrug;

internal static class DrugImportLookupTests
{
    private sealed class Row
    {
        internal int Id;
        internal int? Source;
        internal string Date, Hospital, Pharmacy;
    }

    private static void Assert(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }

    private static bool Matches(string stored, string parameter)
    {
        return stored != null && parameter != null && stored == parameter;
    }

    // Independent reference: the old SELECT predicate followed by its reader loop.
    private static void Compare(List<Row> rows, DrugImportLookup lookup, string date, string code, int source)
    {
        var matches = rows.Where(r => Matches(r.Date, date) &&
            (Matches(r.Hospital, code) || Matches(r.Pharmacy, code))).ToList();
        bool expectedImport = !matches.Any(r => (r.Source ?? 9) <= source);
        var expectedIds = matches.Where(r => (r.Source ?? 9) > source).Select(r => r.Id).OrderBy(i => i);
        var ids = new List<int>();
        bool actualImport = lookup.ShouldImport(date, code, source, ids);
        Assert(expectedImport == actualImport, "Import decision differs from original SQL/reader behavior");
        Assert(expectedIds.SequenceEqual(ids.OrderBy(i => i)), "Revised IDs differ (including OR duplicates)");
    }

    public static int Main()
    {
        try
        {
            var lookup = new DrugImportLookup();
            lookup.Add(1, 3, "20260920", "0010000001", "0010000002");
            var ids = new List<int>();
            Assert(!lookup.ShouldImport("20260920", "0010000001", 3, ids), "Same source must skip");
            Assert(lookup.ShouldImport("20260921", "0010000001", 3, ids), "Different dispensing day must survive");
            Assert(lookup.ShouldImport("20260920", "0010000003", 3, ids), "Different institution must survive");
            Assert(lookup.ShouldImport("20260920", "0010000002", 1, ids) && ids.SequenceEqual(new[] { 1 }),
                "Either institution column must match, with source priority preserved");

            // Matching must remain stable until COPY, even when a previous XML group
            // was accepted. Do not use pending rows to discard same-day details.
            ids.Clear();
            Assert(lookup.ShouldImport("20260920", "0010000002", 1, ids), "Pending COPY changed visibility");
            lookup.Add(2, 1, "20260920", "0010000001", "0010000001");
            ids.Clear();
            Assert(!lookup.ShouldImport("20260920", "0010000001", 2, ids) && ids.SequenceEqual(new[] { 1 }),
                "Higher priority match must skip but still revise lower priority rows");

            var random = new Random(260926);
            string[] dates = { null, "", "20260920", "20260921", "20260820" };
            string[] codes = { null, "", "0010000001", "0010000002", "0030000001" };
            int?[] sources = { null, 0, 1, 2, 3, 9 };
            int comparisons = 0;
            for (int batch = 0; batch < 100; batch++)
            {
                var rows = new List<Row>();
                lookup = new DrugImportLookup();
                for (int id = 0; id < 80; id++)
                {
                    var row = new Row { Id = id, Source = sources[random.Next(sources.Length)],
                        Date = dates[random.Next(dates.Length)], Hospital = codes[random.Next(codes.Length)],
                        Pharmacy = codes[random.Next(codes.Length)] };
                    rows.Add(row);
                    lookup.Add(row.Id, row.Source, row.Date, row.Hospital, row.Pharmacy);
                }
                foreach (string date in dates)
                    foreach (string code in codes)
                        for (int source = 0; source <= 3; source++)
                        {
                            Compare(rows, lookup, date, code, source);
                            comparisons++;
                        }
            }
            Console.WriteLine("PASS: explicit date/institution/source/COPY cases and " + comparisons + " differential comparisons");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
