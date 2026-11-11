using System;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;

namespace OQSDrug
{
    // サマリー表示専用。薬剤の同定・集約・用量計算には元の名称とコードを使う。
    internal static class DrugSummaryName
    {
        private static readonly Regex Company = new Regex("「[^」]*」|『[^』]*』|\"[^\"]*\"", RegexOptions.Compiled);
        private static readonly Regex DosageForm = new Regex(
            @"(?:配合)?(?:OD錠|口腔内崩壊錠|チュアブル錠|徐放錠|腸溶錠|錠|カプセル|細粒|顆粒|散(?=\s*[0-9.])|ドライシロップ|シロップ|内用液|経口液|内服液|点眼液|点鼻液|点耳液|吸入液|吸入粉末剤|吸入エアゾール|吸入剤|軟膏|クリーム|ローション|ゲル|テープ|パップ|坐剤|坐薬|膣錠|腟錠|静注用|静注液|静注|注射用|注射液|皮下注|筋注|輸液|注)(?=\s*(?:[0-9.]|$))",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex Strength = new Regex(
            @"\s*[0-9]+(?:[.,][0-9]+)*(?:\s*/\s*[0-9]+(?:[.,][0-9]+)*)?\s*(?:μg|µg|mcg|mg|mL|g|L|%|単位|IU|吸入用|吸入|噴霧用|噴霧)(?=$|[^A-Za-z])",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        internal static string Format(string originalName, bool halfWidthKatakana = true)
        {
            if (string.IsNullOrWhiteSpace(originalName)) return "";
            string name = originalName.Normalize(NormalizationForm.FormKC).Trim();
            name = Company.Replace(name, "").Trim();
            name = Regex.Replace(name, @"\((?:医療用|分包)\)", "").Trim();

            // 剤形以降の規格・包装・「～として」等をまとめて除去する。
            // 製品名の途中の「錠」「散」等は、後続が規格か末尾の場合だけ対象にする。
            var form = DosageForm.Match(name);
            if (form.Success && form.Index > 0)
                name = name.Substring(0, form.Index).Trim();

            name = Strength.Replace(name, "");
            name = Regex.Replace(name,
                @"\s*(?:[0-9]+(?:\.[0-9]+)?)?\s*(?:エリプタ|ディスカス|タービュヘイラー|ツイストヘラー|レスピマット|エアゾール)\s*$", "");
            name = Regex.Replace(name, @"\s+", " ").Trim();

            // 未知の表記で名称全体が消えた場合は元表記に戻す。
            if (name.Length == 0) name = originalName.Normalize(NormalizationForm.FormKC).Trim();
            return halfWidthKatakana ? Strings.StrConv(name, VbStrConv.Narrow, 0x411) : name;
        }
    }
}
