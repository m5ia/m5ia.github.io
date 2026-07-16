using System.Collections.Generic;
using System.Windows;
using FormulaExplorer.Core.ViewModels;
using FormulaExplorer.UI;

namespace FormulaExplorer.Excel
{
    /// <summary>
    /// Owns the set of live Explorer windows so Ribbon &gt; Close All can shut them all, and
    /// so many can be open at once while Excel stays usable. Windows live on Excel's main
    /// (STA) thread; every method here must be called on that thread.
    /// </summary>
    internal static class WindowManager
    {
        private static readonly List<ExplorerWindow> Open = new List<ExplorerWindow>();

        /// <summary>Create, own to Excel's hwnd, and show a non-modal Explorer window.</summary>
        public static void ShowNew(ExplorerViewModel vm)
        {
            var window = new ExplorerWindow(vm);

            // Float above Excel (but not above everything) by owning Excel's main window.
            ExcelWindowOwner.SetOwner(window);

            window.Closed += (_, __) => Open.Remove(window);
            Open.Add(window);
            window.Show();
            window.Activate();
        }

        /// <summary>Ribbon &gt; Close All.</summary>
        public static void CloseAll()
        {
            // Copy first: Close() fires Closed which mutates the list.
            foreach (var w in Open.ToArray())
                w.Close();
            Open.Clear();
        }

        public static int OpenCount => Open.Count;
    }
}
