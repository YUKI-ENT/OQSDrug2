using System;
using System.Linq;
using OQSDrug;

internal static class MedicationContinuityTests
{
    private static int assertions;
    private static void Check(bool value, string name)
    {
        assertions++;
        if (!value) throw new Exception(name);
    }
    private static int Main()
    {
        var today = new DateTime(2026, 9, 18);
        foreach (int interval in new[] { 28, 60, 90 })
        {
            var dates = new[] { today.AddDays(-interval * 2 - 20), today.AddDays(-interval - 20), today.AddDays(-20) };
            var result = MedicationContinuity.Evaluate(dates, today);
            Check(result.ChronicFloor >= 0.6, "repeated " + interval);
            Check(result.IntervalDays == interval, "interval " + interval);
            Check(result.EstimatedEnd == null, "external quantity must not imply duration");
            var duplicate = MedicationContinuity.Evaluate(dates.Concat(dates), today);
            Check(duplicate.Fills == 3 && duplicate.ChronicFloor == result.ChronicFloor, "deduplication");
        }
        foreach (int days in new[] { 60, 90 })
        {
            var date = today.AddDays(-days + 1);
            var courses = new[] { new MedicationCourse { Date = date, Days = days } };
            var result = MedicationContinuity.Evaluate(new[] { date }, today, courses);
            Check(result.ChronicFloor >= 0.6 && result.EstimatedEnd == today, "long oral course " + days);
            Check(result.Label.Contains("期間内"), "inclusive end date");
            var after = MedicationContinuity.Evaluate(new[] { date }, today.AddDays(1), courses);
            Check(after.Label.Contains("終了予定日経過"), "after end is not guaranteed active");
            var prn = MedicationContinuity.Evaluate(new[] { date }, today, courses, true);
            Check(prn.EstimatedEnd == null && prn.ChronicFloor == 0, "PRN times are not days");
        }
        var single = MedicationContinuity.Evaluate(new[] { today }, today);
        Check(single.ChronicFloor == 0 && single.EstimatedEnd == null, "unknown single prescription");
        var shortDates = new[] { today.AddDays(-7), today };
        Check(MedicationContinuity.Evaluate(shortDates, today).ChronicFloor == 0, "short course");
        var monthly = new[] { today.AddDays(-60), today.AddDays(-30), today };
        var shortCourse = new[] { new MedicationCourse { Date = today, Days = 7 } };
        Check(MedicationContinuity.Evaluate(monthly, today, shortCourse).ChronicFloor == 0, "repeated short oral courses");
        var irregular = new[] { today.AddDays(-170), today.AddDays(-30), today };
        Check(MedicationContinuity.Evaluate(irregular, today).ChronicFloor == 0, "irregular prescriptions");
        var old = new[] { today.AddDays(-360), today.AddDays(-270), today.AddDays(-180) };
        var oldResult = MedicationContinuity.Evaluate(old, today);
        Check(oldResult.ChronicFloor >= 0.6 && oldResult.Label.Contains("未確認"), "historical chronic is not current use");
        var two = MedicationContinuity.Evaluate(new[] { today.AddDays(-160), today.AddDays(-70) }, today);
        Check(two.ChronicFloor >= 0.6 && two.Label.Contains("継続の可能性"), "two long interval fills");
        Check(MedicationContinuity.Evaluate(new[] { today, today.AddDays(90) }, today).Fills == 1, "future records ignored");
        Check(MedicationContinuity.Evaluate(new DateTime[0], today).Fills == 0, "empty history");
        Console.WriteLine("PASS: " + assertions + " assertions");
        return 0;
    }
}
