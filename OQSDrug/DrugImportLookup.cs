using System;
using System.Collections.Generic;

namespace OQSDrug
{
    // One patient, one XML, one transaction. Only rows already in drug_history
    // belong here: pending COPY rows must not suppress other XML detail groups.
    internal sealed class DrugImportLookup
    {
        private readonly Dictionary<Tuple<string, string>, List<Entry>> entries =
            new Dictionary<Tuple<string, string>, List<Entry>>();

        private sealed class Entry
        {
            internal int Id;
            internal int Source;
        }

        internal void Add(int id, int? source, string diDate, string meTrDiHCd, string prlsHCd)
        {
            if (diDate == null) return; // SQL NULL never equals the parameter.
            var entry = new Entry { Id = id, Source = source ?? 9 };
            AddKey(diDate, meTrDiHCd, entry);
            if (!String.Equals(meTrDiHCd, prlsHCd, StringComparison.Ordinal))
                AddKey(diDate, prlsHCd, entry); // SQL OR returns each row only once.
        }

        private void AddKey(string date, string code, Entry entry)
        {
            if (code == null) return; // Empty strings still participate in matching.
            var key = Tuple.Create(date, code);
            List<Entry> rows;
            if (!entries.TryGetValue(key, out rows))
                entries.Add(key, rows = new List<Entry>());
            rows.Add(entry);
        }

        internal bool ShouldImport(string diDate, string miCode, int source, List<int> idsToRevise)
        {
            List<Entry> rows;
            if (diDate == null || miCode == null ||
                !entries.TryGetValue(Tuple.Create(diDate, miCode), out rows)) return true;
            bool import = true;
            foreach (var row in rows)
            {
                if (row.Source > source) idsToRevise.Add(row.Id);
                else import = false;
            }
            return import;
        }
    }
}
