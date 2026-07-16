namespace FormulaExplorer.Core.Grid
{
    /// <summary>
    /// Which scope a name lookup should prefer. Worksheet scope is tried before workbook
    /// scope (constraint #15): a non-null <see cref="Sheet"/> means "try this sheet first,
    /// then fall back to workbook scope".
    /// </summary>
    public readonly struct ScopeHint
    {
        public ScopeHint(string? book, string? sheet)
        {
            Book = book;
            Sheet = sheet;
        }

        public string? Book { get; }
        public string? Sheet { get; }

        /// <summary>Workbook-only lookup (no worksheet-scope preference).</summary>
        public static ScopeHint Workbook(string? book) => new ScopeHint(book, null);
    }
}
