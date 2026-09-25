using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OQSDrug
{
    public partial class Form1
    {
        private readonly MedicationConfirmationState confirmationState = new MedicationConfirmationState();
        private bool interactionBusy, interactionComplete;
        private ChartMedicationSnapshot interactionSnapshot;
        private MedicationInteractionResult interactionResult;
        private string interactionStatus = "処方確定待ち";
        private string interactionContext, interactionSignature;
        private DateTime nextInteractionErrorLog, nextInteractionAttempt;
        private int interactionGeneration, interactionViewRevision, interactionHistoryRevision;
        private int interactionMonths = 6;
        internal bool InteractionEnabled => Properties.Settings.Default.DynamicsUseCom && Properties.Settings.Default.InteractionCheckEnabled;

        internal void RefreshInteractionTab(FormDI viewer)
        {
            if (viewer == null || viewer.IsDisposed) return;
            viewer.UpdateInteractionCheck(InteractionEnabled, interactionViewRevision,
                lastComChartId.GetValueOrDefault() / 10, interactionSnapshot, interactionResult,
                interactionStatus, interactionBusy, interactionMonths);
        }

        private void PublishInteraction()
        {
            interactionViewRevision++;
            RefreshInteractionTab(formDIInstance);
        }

        private void SetInteractionStatus(string status, bool clearResult = true)
        {
            if (interactionStatus == status && (!clearResult || interactionResult == null)) return;
            interactionStatus = status;
            if (clearResult) interactionResult = null;
            PublishInteraction();
        }

        private void ResetInteraction()
        {
            interactionGeneration++;
            confirmationState.Reset();
            interactionContext = interactionSignature = null;
            interactionSnapshot = null;
            interactionResult = null;
            interactionComplete = false;
            interactionStatus = "処方確定待ち";
            nextInteractionAttempt = DateTime.MinValue;
            PublishInteraction();
        }

        // Completed checks are frozen; history updates require an explicit recheck.
        private void InvalidateInteractionHistory(long patientId)
        {
            if (!InteractionEnabled || interactionSnapshot?.ChartId / 10 != patientId) return;
            interactionHistoryRevision++;
            if (interactionComplete)
                SetInteractionStatus("他院薬歴が更新されました。「再取得・チェック」で更新してください。");
        }

        internal async Task RequestInteractionCheckAsync(long patientId, int months)
        {
            if (!InteractionEnabled || patientId <= 0 || patientId != lastComChartId.GetValueOrDefault() / 10) return;
            interactionMonths = months;
            await PollInteractionAsync(true);
        }

        private async Task PollInteractionAsync(bool manual = false)
        {
            if (!InteractionEnabled || interactionBusy || comPatientTimer == null || IsDisposed || Disposing) return;
            if (!manual && (interactionComplete || DateTime.UtcNow < nextInteractionAttempt)) return;
            interactionBusy = true;
            int revision = patientLinkRevision, generation = interactionGeneration;
            long? expectedChart = lastComChartId;
            Func<bool> stale = () => revision != patientLinkRevision || generation != interactionGeneration || IsDisposed || Disposing;
            try
            {
                var snapshot = await DynamicsComReader.ReadMedicationsAsync();
                if (stale()) return;
                if (snapshot == null) { ResetInteraction(); return; }
                if (expectedChart.HasValue && snapshot.ChartId != expectedChart.Value)
                {
                    ResetInteraction();
                    return;
                }
                if (interactionContext != snapshot.Context)
                {
                    confirmationState.Reset();
                    interactionComplete = false;
                    interactionResult = null;
                    interactionContext = snapshot.Context;
                }
                bool changed = snapshot.Signature != interactionSignature;
                interactionSnapshot = snapshot;
                interactionSignature = snapshot.Signature;
                if (changed)
                {
                    interactionResult = null;
                    interactionStatus = "入力変更・処方確定待ち";
                    PublishInteraction();
                }
                bool isToday = snapshot.VisitDate.Date == DateTime.Today;
                if (!manual && !isToday)
                {
                    SetInteractionStatus("表示中の受診は本日ではありません。「再取得・チェック」で手動チェックできます。");
                    return;
                }
                bool autoCheck = confirmationState.Observe(snapshot);
                if (!manual && !autoCheck) return;
                SetInteractionStatus("チェック中");
                int historyRevision = interactionHistoryRevision;
                var result = await MedicationInteractionCheck.CheckAsync(snapshot, interactionMonths);
                if (stale()) return;
                var verify = await DynamicsComReader.ReadMedicationsAsync();
                if (stale()) return;
                if (historyRevision != interactionHistoryRevision)
                {
                    SetInteractionStatus("他院薬歴が更新されたため再チェック待ち");
                    return;
                }
                if (verify == null || verify.Signature != snapshot.Signature)
                {
                    SetInteractionStatus("患者・受診・薬剤が変わったため結果を破棄しました");
                    if (verify == null || verify.Context != snapshot.Context) ResetInteraction();
                    return;
                }
                confirmationState.Complete(snapshot);
                // A manual check of a non-today visit stays visible even without a confirmation row.
                interactionComplete = !isToday || snapshot.Confirmation.Length > 0;
                interactionResult = result;
                interactionStatus = manual && !isToday
                    ? "手動チェック完了（受診日：" + snapshot.VisitDate.ToString("yyyy/MM/dd") + "）。再チェックは「再取得・チェック」を押してください。"
                    : interactionComplete
                    ? "チェック完了（自動チェック終了）。再編集・同じ患者の別受診は「再取得・チェック」を押してください。"
                    : "手動チェック完了。処方確定後に自動チェックします。";
                PublishInteraction();
            }
            catch (Exception ex)
            {
                if (stale()) return;
                SetInteractionStatus("未チェック: " + ex.Message);
                if (DateTime.UtcNow >= nextInteractionErrorLog)
                {
                    nextInteractionErrorLog = DateTime.UtcNow.AddSeconds(30);
                    await CommonFunctions.AddLogAsync("相互作用チェック待ち: " + ex.Message);
                }
            }
            finally
            {
                interactionBusy = false;
                nextInteractionAttempt = DateTime.UtcNow.AddSeconds(3);
                if (!stale()) RefreshInteractionTab(formDIInstance);
            }
        }
    }
}
