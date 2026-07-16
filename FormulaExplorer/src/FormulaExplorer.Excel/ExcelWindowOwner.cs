using System.Windows;
using System.Windows.Interop;
using ExcelDna.Integration;

namespace FormulaExplorer.Excel
{
    /// <summary>
    /// Sets a WPF window's owner to Excel's main hwnd (constraint: windows float above Excel
    /// but not above everything). Uses <c>WindowInteropHelper</c> + <c>ExcelDnaUtil.WindowHandle</c>.
    /// </summary>
    internal static class ExcelWindowOwner
    {
        public static void SetOwner(Window window)
        {
            var helper = new WindowInteropHelper(window)
            {
                Owner = ExcelDnaUtil.WindowHandle
            };
            // Assigning Owner via the helper is enough; the window is created lazily on Show().
            _ = helper;
        }
    }
}
