using System;
using System.Collections.Generic;
using System.Linq;

namespace OQSDrug
{
    internal sealed class MedicationCourse
    {
        internal DateTime Date;
        internal int Days;
    }

    // 処方の反復傾向。処方間隔を投与日数・実際の服薬期間と同一視しない。
    internal sealed class MedicationContinuity
    {
        internal int Fills;
        internal double? IntervalDays;
        internal double ChronicFloor;
        internal string Label;
        internal string Note;
        internal DateTime? EstimatedEnd;

        internal static MedicationContinuity Evaluate(IEnumerable<DateTime> history, DateTime referenceDate,
            IEnumerable<MedicationCourse> oralCourses = null, bool asNeeded = false)
        {
            var dates = history.Select(d => d.Date).Where(d => d <= referenceDate.Date)
                .Distinct().OrderBy(d => d).ToList();
            var result = new MedicationContinuity { Fills = dates.Count };
            if (asNeeded)
            {
                result.Label = "頓用・間欠使用（要確認）";
                result.Note = "頓用の用法があるため処方回数を日数として扱わず、現在の使用は未確認。";
                return result;
            }
            if (dates.Count > 0)
            {
                // 同日重複を加算しない。最新処方の通常内服の日数だけを使用。
                int days = (oralCourses ?? Enumerable.Empty<MedicationCourse>())
                    .Where(c => c.Date.Date == dates.Last() && c.Days > 0 && c.Days <= 365)
                    .Select(c => c.Days).DefaultIfEmpty(0).Max();
                if (days > 0)
                {
                    result.EstimatedEnd = dates.Last().AddDays(days - 1);
                    if (days >= 56)
                    {
                        result.ChronicFloor = 0.65;
                        result.Label = referenceDate.Date <= result.EstimatedEnd.Value
                            ? "長期処方（処方上の使用期間内）" : "長期処方（終了予定日経過・要確認）";
                        result.Note = string.Format("通常内服の回数={0}日として推定終了日{1:yyyy-MM-dd}。実際の服薬・中断は未確認。",
                            days, result.EstimatedEnd.Value);
                    }
                }
            }
            if (dates.Count < 2) return result;

            var gaps = dates.Skip(1).Select((d, i) => (d - dates[i]).TotalDays).ToList();
            var ordered = gaps.OrderBy(d => d).ToList();
            int middle = ordered.Count / 2;
            double interval = ordered.Count % 2 == 0
                ? (ordered[middle - 1] + ordered[middle]) / 2 : ordered[middle];
            double mean = gaps.Average();
            double cv = Math.Sqrt(gaps.Average(g => (g - mean) * (g - mean))) / mean;

            // 少なくとも8週間にわたる反復。短期コースの数日おきの記録は対象外。
            // 2回だけの場合は規則性を確定せず、低めの補助スコアとする。
            if ((dates.Last() - dates.First()).TotalDays < 56 || interval < 21 || interval > 120
                || cv > 0.35 || gaps.Any(g => g > interval * 1.5)) return result;

            // 明示された短期内服を、再処方までずっと使用していたと推定しない。
            if (result.EstimatedEnd.HasValue && (result.EstimatedEnd.Value - dates.Last()).TotalDays + 1 < interval * 0.6)
                return result;

            result.IntervalDays = interval;
            result.ChronicFloor = dates.Count == 2 ? 0.65 : 0.75 + 0.10 * (1 - cv / 0.35);
            double elapsed = (referenceDate.Date - dates.Last()).TotalDays;
            // 猶予は履歴のばらつきに対する表示上の目安。中断の確定には使わない。
            double grace = Math.Min(30, Math.Max(14, interval * 0.25));
            if (result.Label == null)
                result.Label = elapsed <= interval + grace
                    ? "長期反復（継続の可能性・要確認）" : "長期反復（次回記録未確認）";
            result.Note += string.Format("{0}回の処方・代表間隔{1:0.#}日・最終処方から{2:0}日。"
                + "この反復間隔は投与日数を示さず、現在の使用・中断は未確認。", dates.Count, interval, elapsed);
            return result;
        }
    }
}
