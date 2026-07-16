using System;
using ExcelDna.Integration;
using ExcelDna.Logging;
using FormulaExplorer.Core.ViewModels;

namespace FormulaExplorer.Excel
{
    /// <summary>
    /// XLL commands invoked by <c>Application.OnKey</c>. OnKey needs a registered XLL command
    /// (constraint #8), which is exactly what <c>[ExcelCommand(Name=...)]</c> produces. These run
    /// in a macro context on Excel's main thread, so COM is safe here (still routed via ComGate).
    /// </summary>
    public static class Commands
    {
        /// <summary>Macro name that <c>AutoOpen</c> binds to Ctrl+Q. Keep in sync with AddIn.cs.</summary>
        public const string ShowExplorerMacro = "FormulaExplorer_Show";

        [ExcelCommand(Name = ShowExplorerMacro)]
        public static void ShowExplorer()
        {
            try
            {
                var snapshot = ActiveCellReader.ReadActiveCell();

                var vm = new ExplorerViewModel
                {
                    CellAddress = snapshot.DisplayAddress,
                    RawFormula = snapshot.FormulaText
                };

                WindowManager.ShowNew(vm);
            }
            catch (Exception ex)
            {
                // Never let an exception escape into Excel's macro pump.
                LogDisplay.WriteLine("Formula Explorer: could not open window — " + ex.Message);
            }
        }
    }
}
