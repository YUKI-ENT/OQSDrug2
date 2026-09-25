using System;
using System.Threading.Tasks;

// Compile the production polling code and models with in-memory COM/DB boundaries.
namespace OQSDrug
{
    public partial class Form1
    {
        private int patientLinkRevision = 0;
        private long? lastComChartId = 123451;
        private object comPatientTimer = new object();
        private FormDI formDIInstance = null;
        public bool IsDisposed => false;
        public bool Disposing => false;
        internal async Task AutoPoll()
        {
            nextInteractionAttempt = DateTime.MinValue;
            await PollInteractionAsync();
        }
        internal bool ShouldPopup(ChartMedicationSnapshot snapshot, MedicationInteractionResult result)
            => ShouldShowInteractionPopup(snapshot, result);
        internal void ResetForPopupTest() => ResetInteraction();
        internal bool HasResult => interactionResult != null;
        internal string Status => interactionStatus;
    }
    internal class FormDI
    {
        internal bool IsDisposed => false;
        internal void UpdateInteractionCheck(bool enabled, int revision, long patient,
            ChartMedicationSnapshot snapshot, MedicationInteractionResult result, string status, bool busy, int months) { }
    }
    internal static class DynamicsComReader
    {
        internal static ChartMedicationSnapshot Snapshot;
        internal static int Reads;
        internal static Task<ChartMedicationSnapshot> ReadMedicationsAsync()
        {
            Reads++;
            return Task.FromResult(Snapshot);
        }
    }
    internal static class MedicationInteractionCheck
    {
        internal static int Checks;
        internal static Action DuringCheck;
        internal static DateTime VisitDate;
        internal static int Months;
        internal static Task<MedicationInteractionResult> CheckAsync(ChartMedicationSnapshot snapshot, int months)
        {
            Checks++;
            VisitDate = snapshot.VisitDate;
            Months = months;
            DuringCheck?.Invoke();
            return Task.FromResult(new MedicationInteractionResult());
        }
    }
    internal static class CommonFunctions
    {
        internal static Task AddLogAsync(string message) => Task.CompletedTask;
    }
    internal static class InteractionManualCheckTests
    {
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
        private static ChartMedicationSnapshot Snapshot(DateTime date, bool confirmed = false)
        {
            var snapshot = new ChartMedicationSnapshot { ChartId = 123451, Visit = "100", VisitDate = date };
            snapshot.Medications.Add(new ChartMedication { InternalCode = "10001", Name = "テスト薬" });
            if (confirmed) snapshot.Medications.Add(new ChartMedication { InternalCode = "50", Name = "処方箋料" });
            return snapshot;
        }
        private static async Task Run()
        {
            var popupForm = new Form1();
            var popupSnapshot = Snapshot(DateTime.Today);
            var redResult = new MedicationInteractionResult();
            redResult.Koro.Add(new MedicationInteractionHit { Current = "A", History = "B" });
            Require(!popupForm.ShouldPopup(popupSnapshot, redResult), "Popup shown while disabled");
            Properties.Settings.Default.InteractionErrorPopupEnabled = true;
            Require(popupForm.ShouldPopup(popupSnapshot, redResult), "Red result did not notify");
            Require(!popupForm.ShouldPopup(popupSnapshot, redResult), "Same result notified twice");
            redResult.Text.Add(new MedicationInteractionHit { Current = "C", History = "D" });
            Require(popupForm.ShouldPopup(popupSnapshot, redResult), "Changed result did not notify");
            Require(!popupForm.ShouldPopup(popupSnapshot, new MedicationInteractionResult()), "Green result notified");
            Require(popupForm.ShouldPopup(popupSnapshot, redResult), "Red result after green did not notify");
            popupForm.ResetForPopupTest();
            Require(popupForm.ShouldPopup(popupSnapshot, redResult), "Reset did not clear notification state");
            Properties.Settings.Default.InteractionErrorPopupEnabled = false;
            Console.WriteLine("PASS: popup setting, red/green results, duplicate suppression, changed result and reset");
            foreach (int days in new[] { -10, 1 })
            foreach (bool confirmed in new[] { false, true })
            {
                var form = new Form1();
                DynamicsComReader.Snapshot = Snapshot(DateTime.Today.AddDays(days), confirmed);
                int checks = MedicationInteractionCheck.Checks;
                await form.AutoPoll(); await form.AutoPoll();
                Require(MedicationInteractionCheck.Checks == checks, "Non-today visit automatically checked");
                await form.RequestInteractionCheckAsync(12345, 12);
                Require(form.HasResult && MedicationInteractionCheck.Checks == checks + 1, "Manual non-today check failed");
                Require(MedicationInteractionCheck.VisitDate == DateTime.Today.AddDays(days)
                    && MedicationInteractionCheck.Months == 12, "Visit/period not passed to checker");
                int reads = DynamicsComReader.Reads;
                await form.AutoPoll();
                Require(form.HasResult && DynamicsComReader.Reads == reads, "Auto polling erased manual result");
                await form.RequestInteractionCheckAsync(12345, 3);
                Require(MedicationInteractionCheck.Checks == checks + 2 && DynamicsComReader.Reads == reads + 2,
                    "Repeated button press did not fetch and recheck");
            }
            var today = new Form1();
            DynamicsComReader.Snapshot = Snapshot(DateTime.Today);
            await today.RequestInteractionCheckAsync(12345, 6);
            int count = MedicationInteractionCheck.Checks;
            DynamicsComReader.Snapshot = Snapshot(DateTime.Today, true);
            await today.AutoPoll(); await today.AutoPoll();
            Require(MedicationInteractionCheck.Checks == count + 1, "Today confirmation no longer triggers automatic check");

            var changed = new Form1();
            DynamicsComReader.Snapshot = Snapshot(DateTime.Today.AddDays(-1));
            MedicationInteractionCheck.DuringCheck = () => DynamicsComReader.Snapshot = Snapshot(DateTime.Today.AddDays(-2));
            await changed.RequestInteractionCheckAsync(12345, 6);
            Require(!changed.HasResult, "Changed visit result accepted");
            MedicationInteractionCheck.DuringCheck = null;
            count = MedicationInteractionCheck.Checks;
            await changed.RequestInteractionCheckAsync(99999, 6);
            Require(MedicationInteractionCheck.Checks == count, "Mismatched patient checked");
            Console.WriteLine("PASS: manual non-today/repeated checks, result retention, today auto confirmation, visit/patient guards");
        }
        public static void Main() => Run().GetAwaiter().GetResult();
    }
}
namespace OQSDrug.Properties
{
    internal class Settings
    {
        internal static Settings Default = new Settings();
        internal bool DynamicsUseCom => true;
        internal bool InteractionCheckEnabled => true;
        internal bool InteractionErrorPopupEnabled;
    }
}
