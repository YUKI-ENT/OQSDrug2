using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OQSDrug
{
    public partial class Form1
    {
        private FormInteractionCheck interactionForm;
        private ToolStripButton interactionButton;
        private readonly MedicationConfirmationState confirmationState = new MedicationConfirmationState();
        private bool interactionBusy;
        private string interactionContext, interactionSignature, interactionNotice;
        private DateTime nextInteractionErrorLog;
        private DateTime nextInteractionAttempt;
        private long interactionPatientId;
        private string interactionCompletedSignature;
        private int interactionHistoryRevision;
        private bool interactionHistoryChanged;
        private bool InteractionEnabled => Properties.Settings.Default.DynamicsUseCom && Properties.Settings.Default.InteractionCheckEnabled;

        private void InitializeInteractionUi()
        {
            interactionButton = new ToolStripButton("相互作用チェック") { Visible = false };
            interactionButton.Click += async (s, e) =>
            {
                EnsureInteractionForm(); interactionForm.Show(this);
                await PollInteractionAsync(true);
            };
            toolStripVersion.Items.Add(interactionButton);
        }
        private void EnsureInteractionForm()
        {
            if (interactionForm != null && !interactionForm.IsDisposed) return;
            interactionForm = new FormInteractionCheck();
            interactionForm.FormClosed += (s, e) => { if (ReferenceEquals(interactionForm, s)) interactionForm = null; };
            interactionForm.CheckRequested += async (s, e) => await PollInteractionAsync(true);
        }
        private void ResetInteraction()
        {
            confirmationState.Reset();
            interactionContext = interactionSignature = interactionNotice = null;
            interactionPatientId = 0;
            interactionCompletedSignature = null;
            interactionHistoryChanged = false;
            interactionHistoryRevision++;
            if (interactionForm != null && !interactionForm.IsDisposed) interactionForm.Close();
            interactionForm = null;
        }
        // Called on the UI thread after a successful drug-history import.
        private void InvalidateInteractionHistory(long patientId)
        {
            if (!InteractionEnabled || patientId != interactionPatientId) return;
            interactionHistoryChanged = true;
            interactionHistoryRevision++;
            interactionForm?.SetState("他院薬歴が更新されました・再チェック待ち", true);
        }
        private async Task PollInteractionAsync(bool manual = false)
        {
            if (!InteractionEnabled || interactionBusy || comPatientTimer == null || IsDisposed || Disposing) return;
            if (!manual && DateTime.UtcNow < nextInteractionAttempt) return;
            interactionBusy = true;
            int revision = patientLinkRevision;
            interactionForm?.SetBusy(true);
            try
            {
                var snapshot = await DynamicsComReader.ReadMedicationsAsync();
                if (revision != patientLinkRevision || IsDisposed || Disposing) return;
                if (snapshot == null) { ResetInteraction(); return; }
                if (interactionContext != snapshot.Context)
                {
                    if (!manual) ResetInteraction();
                    else { confirmationState.Reset(); interactionNotice = interactionCompletedSignature = null; interactionHistoryChanged = false; }
                    interactionContext = snapshot.Context;
                }
                interactionPatientId = snapshot.ChartId / 10;
                bool changed = snapshot.Signature != interactionSignature;
                interactionSignature = snapshot.Signature;
                if (changed && interactionForm != null && !interactionForm.IsDisposed)
                {
                    interactionForm.ShowSnapshot(snapshot);
                    interactionForm.SetState("入力変更・確定待ち（確定行が残る再編集は手動チェック）", true);
                }
                if (snapshot.VisitDate != DateTime.Today)
                {
                    interactionForm?.SetState("表示中の受診は本日ではないためチェックしません", true);
                    return;
                }
                bool autoCheck = confirmationState.Observe(snapshot);
                // New imported history must also be checked, but not while the prescription is being edited.
                autoCheck |= interactionHistoryChanged && snapshot.Confirmation.Length > 0
                    && snapshot.Signature == interactionCompletedSignature;
                if (!manual && !autoCheck) return;
                EnsureInteractionForm(); interactionForm.SetBusy(true);
                interactionForm.ShowSnapshot(snapshot); interactionForm.SetState("チェック中", true);
                int months = interactionForm.Months;
                int historyRevision = interactionHistoryRevision;
                var result = await MedicationInteractionCheck.CheckAsync(snapshot, months);
                if (revision != patientLinkRevision || IsDisposed || Disposing) return;
                // Database lookup may take time. Never publish a result for a stale chart or drug list.
                var verify = await DynamicsComReader.ReadMedicationsAsync();
                if (revision != patientLinkRevision || IsDisposed || Disposing) return;
                if (historyRevision != interactionHistoryRevision)
                {
                    interactionForm?.SetState("他院薬歴が更新されたため再チェック待ち", true);
                    return;
                }
                if (verify == null || verify.Signature != snapshot.Signature)
                {
                    interactionForm?.SetState("患者・受診・薬剤が変わったため結果を破棄しました", true);
                    if (verify == null || verify.Context != snapshot.Context) ResetInteraction();
                    return;
                }
                confirmationState.Complete(snapshot);
                interactionCompletedSignature = snapshot.Signature;
                interactionHistoryChanged = false;
                string notice = snapshot.Context + ":" + months + ":" + result.NoticeKey;
                bool popup = result.Koro.Count + result.Text.Count > 0 && notice != interactionNotice;
                interactionNotice = notice;
                EnsureInteractionForm(); interactionForm.ShowResult(snapshot, result, popup || manual);
                if (popup || manual) { interactionForm.Show(this); interactionForm.BringToFront(); }
            }
            catch (Exception ex)
            {
                if (revision != patientLinkRevision || IsDisposed || Disposing) return;
                nextInteractionAttempt = DateTime.UtcNow.AddSeconds(3);
                interactionForm?.SetState("未チェック: " + ex.Message, true);
                if (DateTime.UtcNow >= nextInteractionErrorLog)
                {
                    nextInteractionErrorLog = DateTime.UtcNow.AddSeconds(30);
                    await CommonFunctions.AddLogAsync("相互作用チェック待ち: " + ex.Message);
                }
            }
            finally
            {
                interactionBusy = false;
                if (interactionForm != null && !interactionForm.IsDisposed) interactionForm.SetBusy(false);
            }
        }
    }
}
