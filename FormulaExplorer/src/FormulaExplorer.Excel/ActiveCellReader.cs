using System;
using System.Runtime.InteropServices;
using ExcelDna.Integration;
using Microsoft.Office.Interop.Excel;

namespace FormulaExplorer.Excel
{
    /// <summary>
    /// Reads the origin cell for an Explorer window. M1 needs only a display address and the
    /// raw formula text. All COM access goes through <see cref="ComGate"/> and stays read-only
    /// (constraint #4).
    /// </summary>
    internal static class ActiveCellReader
    {
        public readonly struct CellSnapshot
        {
            public CellSnapshot(string displayAddress, string formulaText)
            {
                DisplayAddress = displayAddress;
                FormulaText = formulaText;
            }

            /// <summary>e.g. "Sheet1!$A$1" — for the window title.</summary>
            public string DisplayAddress { get; }

            /// <summary>The cell's Formula2 text, or its literal value when it has no formula.</summary>
            public string FormulaText { get; }
        }

        public static CellSnapshot ReadActiveCell()
        {
            return ComGate.Run(() =>
            {
                var app = (Application)ExcelDnaUtil.Application;
                Range cell = app.ActiveCell;

                // Address like "Sheet1!$A$1": local (no book) is enough for the M1 title.
                string local = cell.get_Address(true, true, XlReferenceStyle.xlA1, false, Type.Missing);
                string sheetName = ((Worksheet)cell.Worksheet).Name;
                string display = sheetName + "!" + local;

                string formula = ReadFormula(cell);
                return new CellSnapshot(display, formula);
            });
        }

        /// <summary>
        /// Prefer Formula2 (invariant, no implicit-intersection @ injection — constraint #9).
        /// Access it late-bound so we don't pin a specific Office PIA that predates the property;
        /// fall back to Formula on pre-2019 Excel, then to the displayed value for constants.
        /// </summary>
        private static string ReadFormula(Range cell)
        {
            try
            {
                object f2 = ((dynamic)cell).Formula2;
                if (f2 is string s2 && s2.Length > 0)
                    return s2;
            }
            catch (COMException) { /* Formula2 unavailable (pre-2019): fall through */ }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) { /* property missing */ }

            try
            {
                object f = cell.Formula;
                if (f is string s && s.Length > 0)
                    return s;
            }
            catch (COMException) { /* fall through */ }

            object? v = cell.Value2;
            return v?.ToString() ?? "(empty cell)";
        }
    }
}
