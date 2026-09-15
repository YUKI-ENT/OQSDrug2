using System;
using System.Data;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OQSDrug
{
    internal static partial class DynamicsComReader
    {
        internal const string QualificationTable = "WKO資格確認結果表示";
        private static readonly Lazy<Task<Control>> dispatcher = new Lazy<Task<Control>>(StartDispatcher);
        private static readonly SemaphoreSlim readGate = new SemaphoreSlim(1, 1);

        private static Task<Control> StartDispatcher()
        {
            var ready = new TaskCompletionSource<Control>(TaskCreationOptions.RunContinuationsAsynchronously);
            var thread = new Thread(() =>
            {
                try
                {
                    using (var control = new Control())
                    {
                        var handle = control.Handle;
                        var filter = new ComMessageFilter();
                        Marshal.ThrowExceptionForHR(CoRegisterMessageFilter(filter, out var previous));
                        try
                        {
                            ready.SetResult(control);
                            Application.Run();
                        }
                        finally
                        {
                            CoRegisterMessageFilter(previous, out var removed);
                            Release(previous);
                            GC.KeepAlive(filter);
                        }
                    }
                }
                catch (Exception ex) { ready.TrySetException(ex); }
            }) { IsBackground = true, Name = "Dynamics COM reader" };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return ready.Task;
        }

        public static Task<DataTable> ReadAsync(string sql) => DispatchAsync(() => Read(sql));

        public static Task<long?> ReadCurrentPatientAsync() => DispatchAsync(() =>
        {
            object application;
            try { application = Marshal.GetActiveObject("Access.Application"); }
            catch (COMException ex) when (ex.ErrorCode == unchecked((int)0x800401E3))
            {
                return (long?)null; // Access is not running.
            }
            return ReadCurrentPatientApplication(application);
        });

        // null: patient form closed; 0: form open without a valid patient.
        internal static long? ReadCurrentPatientApplication(object application)
        {
            object project = null, allForms = null, metadata = null;
            object forms = null, form = null, controls = null, control = null;
            try
            {
                project = GetAutomationProperty(application, "CurrentProject");
                allForms = GetAutomationProperty(project, "AllForms");
                int count = Convert.ToInt32(GetAutomationProperty(allForms, "Count"));
                for (int i = 0; i < count; i++)
                {
                    metadata = GetAutomationProperty(allForms, "Item", i);
                    if (Convert.ToString(GetAutomationProperty(metadata, "Name")) == "患者マスター")
                    {
                        if (!Convert.ToBoolean(GetAutomationProperty(metadata, "IsLoaded"))) return null;
                        forms = GetAutomationProperty(application, "Forms");
                        form = GetAutomationProperty(forms, "Item", "患者マスター");
                        if (Convert.ToBoolean(GetAutomationProperty(form, "NewRecord"))) return 0;
                        controls = GetAutomationProperty(form, "Controls");
                        control = GetAutomationProperty(controls, "Item", "カルテ番号");
                        object value = GetAutomationProperty(control, "Value");
                        return long.TryParse(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture),
                            out long id) && id > 0 ? id : 0;
                    }
                    Release(metadata);
                    metadata = null;
                }
                return null;
            }
            finally
            {
                Release(control);
                Release(controls);
                Release(form);
                Release(forms);
                Release(metadata);
                Release(allForms);
                Release(project);
                Release(application);
            }
        }

        // Access live forms may fail during dynamic type-info lookup or Name access.
        // Follow DocAssistant's named IDispatch property access instead.
        private static object GetAutomationProperty(object target, string name, params object[] args)
        {
            try
            {
                return target.GetType().InvokeMember(name,
                    BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance,
                    null, target, args);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                if (ex.InnerException is COMException com)
                    throw new COMException("COMプロパティ " + name + " の取得失敗 (0x"
                        + com.ErrorCode.ToString("X8") + "): " + com.Message, com.ErrorCode);
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
            catch (COMException ex)
            {
                throw new COMException("COMプロパティ " + name + " の取得失敗 (0x"
                    + ex.ErrorCode.ToString("X8") + "): " + ex.Message, ex.ErrorCode);
            }
        }

        private static async Task<T> DispatchAsync<T>(Func<T> read)
        {
            // Do not post another callback while COM is pumping messages inside the current call.
            await readGate.WaitAsync().ConfigureAwait(false);
            try
            {
                var control = await dispatcher.Value.ConfigureAwait(false);
                var result = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
                control.BeginInvoke(new Action(() =>
                {
                    try { result.SetResult(read()); }
                    catch (Exception ex) { result.SetException(ex); }
                }));
                return await result.Task.ConfigureAwait(false);
            }
            finally { readGate.Release(); }
        }

        public static async Task<string> CheckAvailabilityAsync()
        {
            try
            {
                using (await ReadAsync("SELECT * FROM [" + QualificationTable + "] WHERE 1=0").ConfigureAwait(false)) { }
                return "OK";
            }
            catch (Exception ex)
            {
                return "電カルCOM接続待ち: " + ex.Message;
            }
        }

        private static DataTable Read(string sql)
        {
            // Attach only: never launch Access, open an MDB, or change the user's current DB.
            return ReadApplication(Marshal.GetActiveObject("Access.Application"), sql);
        }

        internal static DataTable ReadApplication(object application, string sql)
        {
            object forms = null, patientForm = null;
            object database = null, definitions = null, definition = null;
            object recordset = null, fields = null;
            var result = new DataTable();
            try
            {
                forms = ((dynamic)application).Forms;
                patientForm = ((dynamic)forms)["患者マスター"];
                database = ((dynamic)application).CurrentDb();
                definitions = ((dynamic)database).TableDefs;
                definition = ((dynamic)definitions)[QualificationTable];

                // dbOpenSnapshot = 4, dbReadOnly = 4; linked tables cannot use dbOpenTable.
                recordset = ((dynamic)database).OpenRecordset(sql, 4, 4);
                fields = ((dynamic)recordset).Fields;
                int count = ((dynamic)fields).Count;
                for (int i = 0; i < count; i++)
                {
                    object field = null;
                    try
                    {
                        field = ((dynamic)fields)[i];
                        result.Columns.Add((string)((dynamic)field).Name, GetFieldType((int)((dynamic)field).Type));
                    }
                    finally { Release(field); }
                }

                while (!(bool)((dynamic)recordset).EOF)
                {
                    // Transfer batches, not one cross-process COM call per cell.
                    Array rows = (Array)((dynamic)recordset).GetRows(256);
                    int rowCount = rows.GetLength(1);
                    if (rowCount == 0)
                        throw new InvalidOperationException("DAO読み取りが進みませんでした。次の周期で再試行します。");
                    for (int row = 0; row < rowCount; row++)
                    {
                        var values = new object[count];
                        for (int column = 0; column < count; column++)
                            values[column] = rows.GetValue(column + rows.GetLowerBound(0), row + rows.GetLowerBound(1)) ?? DBNull.Value;
                        result.Rows.Add(values);
                    }
                }
                return result;
            }
            catch
            {
                result.Dispose();
                throw;
            }
            finally
            {
                Release(fields);
                if (recordset != null)
                {
                    try { ((dynamic)recordset).Close(); } catch (COMException) { }
                    Release(recordset);
                }
                Release(definition);
                Release(definitions);
                // CurrentDb is a borrowed DAO reference. Never close the client's DB or call Quit.
                Release(database);
                Release(patientForm);
                Release(forms);
                Release(application);
            }
        }

        private static Type GetFieldType(int type)
        {
            switch (type)
            {
                case 1: return typeof(bool);
                case 2: return typeof(byte);
                case 3: return typeof(short);
                case 4: return typeof(int);
                case 5: return typeof(decimal);
                case 6: return typeof(float);
                case 7: return typeof(double);
                case 8: return typeof(DateTime);
                case 9: case 11: return typeof(byte[]);
                case 10: case 12: case 18: return typeof(string);
                case 15: return typeof(Guid);
                case 16: return typeof(long);
                case 19: case 20: return typeof(decimal);
                case 21: return typeof(double);
                default: return typeof(object);
            }
        }

        private static void Release(object value)
        {
            if (value != null && Marshal.IsComObject(value)) Marshal.ReleaseComObject(value);
        }

        [DllImport("ole32.dll")]
        private static extern int CoRegisterMessageFilter(IOleMessageFilter filter, out IOleMessageFilter previous);

        [ComImport, Guid("00000016-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IOleMessageFilter
        {
            [PreserveSig] int HandleInComingCall(int type, IntPtr caller, int ticks, IntPtr info);
            [PreserveSig] int RetryRejectedCall(IntPtr callee, int ticks, int rejectType);
            [PreserveSig] int MessagePending(IntPtr callee, int ticks, int pendingType);
        }

        private sealed class ComMessageFilter : IOleMessageFilter
        {
            public int HandleInComingCall(int type, IntPtr caller, int ticks, IntPtr info) => 0;
            // Only retry a temporarily busy server, for at most 1.5 seconds per call.
            // On failure the normal acquisition timer retries, without a server-busy dialog.
            public int RetryRejectedCall(IntPtr callee, int ticks, int rejectType)
                => rejectType == 2 && ticks < 1500 ? 100 : -1;
            public int MessagePending(IntPtr callee, int ticks, int pendingType) => 2;
        }
    }
}
