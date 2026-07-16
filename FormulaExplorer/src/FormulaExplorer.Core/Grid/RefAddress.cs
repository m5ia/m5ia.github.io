namespace FormulaExplorer.Core.Grid
{
    /// <summary>
    /// A location in the grid, Excel-free.
    /// <para>
    /// <see cref="Book"/> / <see cref="Sheet"/> may be null for a context-relative
    /// address that the reference qualifier (M3) fills in before anything is evaluated.
    /// Once <see cref="IsFullyQualified"/> is true the address is safe to hand to
    /// <see cref="IGridContext.Evaluate"/> (constraint #2: qualify before evaluating).
    /// </para>
    /// </summary>
    public readonly struct RefAddress
    {
        public RefAddress(string? book, string? sheet, string a1, bool isFullyQualified)
        {
            Book = book;
            Sheet = sheet;
            A1 = a1;
            IsFullyQualified = isFullyQualified;
        }

        /// <summary>Workbook file name, e.g. "Book1.xlsx". Null when context-relative.</summary>
        public string? Book { get; }

        /// <summary>Sheet name, e.g. "Sheet 1". Null when context-relative.</summary>
        public string? Sheet { get; }

        /// <summary>A1-style local reference, e.g. "$A$1" or "$A$1:$D$100". Absolute preferred.</summary>
        public string A1 { get; }

        /// <summary>True when Book + Sheet are populated and this is safe to evaluate from any context.</summary>
        public bool IsFullyQualified { get; }

        /// <summary>Fully-qualified display/eval form, e.g. '[Book1.xlsx]Sheet 1'!$A$1.</summary>
        public override string ToString()
        {
            if (!IsFullyQualified || Sheet is null)
                return A1;

            string sheetPart = Book is null ? Sheet : "[" + Book + "]" + Sheet;
            // Excel quotes the sheet-with-book token whenever it isn't a bare identifier.
            bool needsQuoting = Book is not null || RequiresQuoting(Sheet);
            if (needsQuoting)
                sheetPart = "'" + sheetPart.Replace("'", "''") + "'";
            return sheetPart + "!" + A1;
        }

        private static bool RequiresQuoting(string sheet)
        {
            foreach (char c in sheet)
            {
                if (!(char.IsLetterOrDigit(c) || c == '_' || c == '.'))
                    return true;
            }
            return false;
        }
    }
}
