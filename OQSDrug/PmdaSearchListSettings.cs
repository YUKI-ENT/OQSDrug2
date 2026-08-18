using System;
using System.Collections.Generic;
using System.Linq;

namespace OQSDrug
{
    internal static class PmdaSearchListSettings
    {
        internal static readonly string[] DefaultItems =
        {
            "眼圧 OR 緑内障",
            "前立腺 OR 尿閉",
            "腎機能 OR 腎障害",
            "肝機能 OR 肝障害",
            "妊婦 OR 妊娠",
            "授乳婦 OR 授乳",
            "QT OR Torsade"
        };

        internal static object[] GetItems()
        {
            return Parse(Properties.Settings.Default.PMDASearchList)
                .Cast<object>()
                .ToArray();
        }

        internal static string GetEditorText()
        {
            string[] configured = Parse(Properties.Settings.Default.PMDASearchList, false);
            return string.Join(Environment.NewLine,
                configured.Length == 0 ? DefaultItems : configured);
        }

        internal static string NormalizeForStorage(string value)
        {
            return string.Join(Environment.NewLine, Parse(value, false));
        }

        internal static string GetDefaultEditorText()
        {
            return string.Join(Environment.NewLine, DefaultItems);
        }

        private static string[] Parse(string value, bool useDefaultsWhenEmpty = true)
        {
            IEnumerable<string> lines = (value ?? string.Empty)
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0)
                .Distinct(StringComparer.CurrentCultureIgnoreCase);

            string[] result = lines.ToArray();
            return result.Length == 0 && useDefaultsWhenEmpty
                ? DefaultItems.ToArray()
                : result;
        }
    }
}
