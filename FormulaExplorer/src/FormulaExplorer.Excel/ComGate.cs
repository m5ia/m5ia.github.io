using System;
using System.Runtime.InteropServices;
using System.Threading;
using ExcelDna.Integration;

namespace FormulaExplorer.Excel
{
    /// <summary>
    /// The single choke point for every COM touch (constraint #7). COM calls made from WPF
    /// event handlers throw <c>0x800AC472</c> (VBA_E_IGNORE) whenever Excel is busy — cell edit
    /// mode, a modal dialog, mid-calculation. This helper:
    /// <list type="bullet">
    ///   <item>marshals onto Excel's main thread via <c>ExcelAsyncUtil.QueueAsMacro</c> when
    ///         called from any other thread (e.g. a WPF handler), and</item>
    ///   <item>retries with backoff on VBA_E_IGNORE.</item>
    /// </list>
    /// Nothing else in the add-in should call <c>ExcelDnaUtil.Application</c> members directly.
    /// </summary>
    public static class ComGate
    {
        private const int VBA_E_IGNORE = unchecked((int)0x800AC472);

        // Backoff schedule for a busy Excel. Deliberately short and bounded — a genuinely
        // stuck Excel should surface as an exception, not an infinite spin.
        private static readonly int[] BackoffMs = { 50, 100, 200, 400, 800 };

        // Captured in AutoOpen (which runs on Excel's main thread) so we can tell whether a
        // caller is already on the main thread and must NOT re-queue (that would deadlock).
        private static int _mainThreadId = -1;

        /// <summary>Called once from <c>AutoOpen</c> on the main thread.</summary>
        public static void Initialize()
        {
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        private static bool OnMainThread =>
            _mainThreadId != -1 && Thread.CurrentThread.ManagedThreadId == _mainThreadId;

        /// <summary>Run a COM action, marshalling + retrying as needed.</summary>
        public static void Run(Action action)
        {
            Run(() => { action(); return true; });
        }

        /// <summary>Run a COM function, marshalling + retrying as needed, and return its result.</summary>
        public static T Run<T>(Func<T> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));

            if (OnMainThread)
                return RunWithRetry(func);

            // Off the main thread (typical for WPF handlers): marshal as a macro and block.
            T result = default!;
            Exception? captured = null;
            using (var done = new ManualResetEventSlim(false))
            {
                ExcelAsyncUtil.QueueAsMacro(() =>
                {
                    try { result = RunWithRetry(func); }
                    catch (Exception ex) { captured = ex; }
                    finally { done.Set(); }
                });
                done.Wait();
            }

            if (captured != null)
                throw new ExcelBusyException("COM call failed after marshalling to the main thread.", captured);
            return result;
        }

        private static T RunWithRetry<T>(Func<T> func)
        {
            Exception? last = null;
            for (int attempt = 0; attempt <= BackoffMs.Length; attempt++)
            {
                try
                {
                    return func();
                }
                catch (COMException ex) when (IsBusy(ex))
                {
                    last = ex;
                    if (attempt < BackoffMs.Length)
                        Thread.Sleep(BackoffMs[attempt]);
                }
            }
            throw new ExcelBusyException(
                "Excel stayed busy (VBA_E_IGNORE) through all retries.", last!);
        }

        private static bool IsBusy(COMException ex)
        {
            // Compare on the unsigned HRESULT to dodge sign-extension surprises.
            return unchecked((uint)ex.ErrorCode) == unchecked((uint)VBA_E_IGNORE);
        }
    }

    /// <summary>Raised when a COM call cannot complete because Excel would not accept it.</summary>
    public sealed class ExcelBusyException : Exception
    {
        public ExcelBusyException(string message, Exception inner) : base(message, inner) { }
    }
}
