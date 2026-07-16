namespace FormulaExplorer.Core.Grid
{
    /// <summary>
    /// The single seam between Excel-free Core logic and a live workbook.
    /// Implemented for real in <c>FormulaExplorer.Excel</c>; faked in <c>Core.Tests</c>
    /// via <c>FakeGrid</c> and replayed from recorded JSON fixtures.
    /// <para>
    /// Every method is READ-ONLY with respect to the workbook (constraint #4): no scratch
    /// cells, no hidden sheets, nothing that dirties the file or destroys the undo stack.
    /// </para>
    /// </summary>
    public interface IGridContext
    {
        /// <summary>
        /// Evaluate a fully-qualified expression string. The caller must qualify every
        /// reference first (constraint #2), because <c>Application.Evaluate</c> resolves
        /// unqualified refs against the ACTIVE sheet, not the formula's sheet.
        /// <para>
        /// Returns a value, an Excel error, or — for reference-returning expressions
        /// (OFFSET / INDEX in reference form / INDIRECT / CHOOSE over refs) — a
        /// <see cref="RefAddress"/> (constraint #3: free target resolution).
        /// </para>
        /// <para>
        /// The implementation owns the 255-char limit (constraint #1: evaluate bottom-up
        /// and substitute child values as literals) and per-session caching of volatile /
        /// side-effecting functions (constraint #6).
        /// </para>
        /// </summary>
        EvalResult Evaluate(string fullyQualifiedExpr);

        /// <summary>
        /// The cell's <c>Formula2</c> text — invariant, comma-separated, no implicit-intersection
        /// <c>@</c> injection (constraint #9). Null if the cell holds no formula. Implementations
        /// fall back to <c>Formula</c> only when <c>Formula2</c> is unavailable (pre-2019 Excel).
        /// </summary>
        string? GetFormula(RefAddress address);

        /// <summary>Computed/displayed value of a cell or range, WITHOUT re-rolling volatiles.</summary>
        EvalResult GetValue(RefAddress address);

        /// <summary>Row/col dimensions and array/spill nature of a range (constraint #14).</summary>
        RangeShape GetRangeShape(RefAddress address);

        /// <summary>
        /// Resolve a defined name, worksheet scope before workbook scope (constraint #15).
        /// Returns null when the name is undefined. #REF! / external names resolve to a
        /// <see cref="NameDefinition"/> with the appropriate <see cref="NameKind"/> rather than throwing.
        /// </summary>
        NameDefinition? ResolveName(string name, ScopeHint scope);

        /// <summary>
        /// The data-validation <c>Formula1</c> for a cell, or null if the cell has no validation (M8).
        /// Enables the "Ctrl+Q on a drop-down jumps to the list source" behaviour.
        /// </summary>
        string? GetValidationFormula(RefAddress address);
    }
}
