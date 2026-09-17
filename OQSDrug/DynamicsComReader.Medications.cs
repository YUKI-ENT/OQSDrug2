using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace OQSDrug
{
    internal static partial class DynamicsComReader
    {
        internal static System.Threading.Tasks.Task<ChartMedicationSnapshot> ReadMedicationsAsync()
            => DispatchAsync(() =>
            {
                object app;
                try { app = Marshal.GetActiveObject("Access.Application"); }
                catch (COMException ex) when (ex.ErrorCode == unchecked((int)0x800401E3)) { return null; }
                return ReadMedicationApplication(app);
            });

        private static object CallAutomation(object target, string name, params object[] args)
        {
            try
            {
                return target.GetType().InvokeMember(name, BindingFlags.InvokeMethod |
                    BindingFlags.OptionalParamBinding | BindingFlags.Public | BindingFlags.Instance, null, target, args);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw new InvalidOperationException("COM " + name + ": " + ex.InnerException.Message, ex.InnerException);
            }
        }

        private static string ControlText(object controls, string name)
        {
            object control = null;
            try
            {
                control = GetAutomationProperty(controls, "Item", name);
                return Convert.ToString(GetAutomationProperty(control, "Value"), CultureInfo.InvariantCulture).Trim();
            }
            finally { Release(control); }
        }

        // Reads a detached copy; never changes the live form's record, Dirty flag or bookmark.
        internal static ChartMedicationSnapshot ReadMedicationApplication(object app)
        {
            object project = null, allForms = null, metadata = null, forms = null, patient = null;
            object controls = null, sub = null, form = null, subControls = null, records = null, db = null;
            try
            {
                project = GetAutomationProperty(app, "CurrentProject");
                allForms = GetAutomationProperty(project, "AllForms");
                // Keep the availability check independent of the number of forms in Access.
                metadata = GetAutomationProperty(allForms, "Item", "患者マスター");
                if (!Convert.ToBoolean(GetAutomationProperty(metadata, "IsLoaded"))) return null;
                forms = GetAutomationProperty(app, "Forms");
                patient = GetAutomationProperty(forms, "Item", "患者マスター");
                if (Convert.ToBoolean(GetAutomationProperty(patient, "NewRecord"))) return null;
                controls = GetAutomationProperty(patient, "Controls");
                string chart = ControlText(controls, "カルテ番号");
                if (!long.TryParse(chart, out long chartId) || chartId / 10 <= 0) return null;
                sub = GetAutomationProperty(controls, "Item", "受診投薬サブフォーム");
                form = GetAutomationProperty(sub, "Form");
                if (Convert.ToBoolean(GetAutomationProperty(form, "Dirty")))
                    throw new InvalidOperationException("薬剤入力中・保存待ち");
                subControls = GetAutomationProperty(form, "Controls");
                string visit = ControlText(subControls, "受診コード");
                if (string.IsNullOrEmpty(visit)) return null;
                records = GetAutomationProperty(form, "RecordsetClone");
                var snapshot = new ChartMedicationSnapshot { ChartId = chartId, Visit = visit };
                snapshot.PatientName = ControlText(controls, "氏名");
                using (var rows = CopyRecordset(records))
                    snapshot.Medications = ParseMedicationRows(rows, chartId, visit);
                db = CallAutomation(app, "CurrentDb");
                using (var visitRows = QueryDatabase(db, "SELECT [受診日] FROM [受診] WHERE [カルテ番号]="
                    + chartId.ToString(CultureInfo.InvariantCulture) + " AND CStr([受診コード])='" + visit.Replace("'", "''") + "'"))
                {
                    if (visitRows.Rows.Count != 1 || visitRows.Rows[0][0] == DBNull.Value)
                        throw new InvalidOperationException("受診日を一意に確認できません");
                    snapshot.VisitDate = Convert.ToDateTime(visitRows.Rows[0][0], CultureInfo.CurrentCulture).Date;
                }
                var codes = snapshot.Medications.Where(m => long.TryParse(m.InternalCode, out long n) && n >= 0 && n <= 99999)
                    .Select(m => long.Parse(m.InternalCode).ToString(CultureInfo.InvariantCulture)).Distinct().ToArray();
                if (codes.Length > 0)
                {
                    using (var master = QueryDatabase(db, "SELECT [薬コード], [厚生省コード] FROM [薬マスター] WHERE [薬コード] IN ("
                        + string.Join(",", codes) + ") AND [不使用]<>2"))
                    {
                        foreach (DataRow row in master.Rows)
                            foreach (var med in snapshot.Medications.Where(m => m.InternalCode == Convert.ToString(row[0])))
                                med.ReceptCode = CommonFunctions.NormalizeDrugCode(Convert.ToString(row[1]));
                    }
                }
                if (chart != ControlText(controls, "カルテ番号") || visit != ControlText(subControls, "受診コード")
                    || Convert.ToBoolean(GetAutomationProperty(form, "Dirty")))
                    throw new InvalidOperationException("取得中に患者・受診・薬剤が変更されました");
                return snapshot;
            }
            finally
            {
                if (records != null) { try { CallAutomation(records, "Close"); } catch { } }
                Release(records); Release(db); Release(subControls); Release(form); Release(sub);
                Release(controls); Release(patient); Release(forms); Release(metadata); Release(allForms); Release(project); Release(app);
            }
        }

        internal static List<ChartMedication> ParseMedicationRows(DataTable rows, long chartId, string visit)
        {
            string[] names = { "受診コード", "カルテ番号", "順番", "薬コード", "薬名", "数量" };
            var cols = names.Select((name, i) => rows.Columns.Contains(name) ? name : "式" + (i + 1)).ToArray();
            if (cols.Any(c => !rows.Columns.Contains(c))) throw new InvalidOperationException("受診投薬の必要列がありません");
            var meds = new List<ChartMedication>();
            foreach (DataRow row in rows.Rows)
            {
                if (Convert.ToString(row[cols[0]]) != visit) continue;
                if (!long.TryParse(Convert.ToString(row[cols[1]]), out long id) || id != chartId)
                    throw new InvalidOperationException("受診投薬の患者番号が一致しません");
                meds.Add(new ChartMedication { Order = Convert.ToString(row[cols[2]]), InternalCode = Convert.ToString(row[cols[3]]),
                    Name = Convert.ToString(row[cols[4]]), Quantity = Convert.ToString(row[cols[5]], CultureInfo.InvariantCulture) });
            }
            return meds;
        }

        private static DataTable QueryDatabase(object db, string sql)
        {
            object rs = null;
            try { rs = CallAutomation(db, "OpenRecordset", sql, 4, 4); return CopyRecordset(rs); }
            finally
            {
                if (rs != null) { try { CallAutomation(rs, "Close"); } catch { } }
                Release(rs);
            }
        }

        private static DataTable CopyRecordset(object rs)
        {
            var table = new DataTable();
            object fields = null;
            try
            {
                fields = GetAutomationProperty(rs, "Fields");
                int count = Convert.ToInt32(GetAutomationProperty(fields, "Count"));
                for (int i = 0; i < count; i++)
                {
                    object field = null;
                    try { field = GetAutomationProperty(fields, "Item", i); table.Columns.Add(Convert.ToString(GetAutomationProperty(field, "Name")), typeof(object)); }
                    finally { Release(field); }
                }
                if (Convert.ToBoolean(GetAutomationProperty(rs, "BOF")) && Convert.ToBoolean(GetAutomationProperty(rs, "EOF"))) return table;
                CallAutomation(rs, "MoveFirst");
                while (!Convert.ToBoolean(GetAutomationProperty(rs, "EOF")))
                {
                    var batch = (Array)CallAutomation(rs, "GetRows", 256);
                    if (batch.Rank != 2 || batch.GetLength(0) != count || batch.GetLength(1) == 0)
                        throw new InvalidOperationException("受診投薬の読み取りが進みません");
                    for (int r = 0; r < batch.GetLength(1); r++)
                    {
                        var values = new object[count];
                        for (int c = 0; c < count; c++) values[c] = batch.GetValue(c + batch.GetLowerBound(0), r + batch.GetLowerBound(1)) ?? DBNull.Value;
                        table.Rows.Add(values);
                    }
                    if (table.Rows.Count > 10000) throw new InvalidOperationException("受診投薬の取得件数が上限を超えました");
                }
                return table;
            }
            catch { table.Dispose(); throw; }
            finally { Release(fields); }
        }
    }
}
