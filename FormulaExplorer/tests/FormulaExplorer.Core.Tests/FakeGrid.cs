using System.Collections.Generic;
using FormulaExplorer.Core.Grid;

namespace FormulaExplorer.Core.Tests
{
    /// <summary>
    /// In-memory <see cref="IGridContext"/> for headless tests. M1 is a skeleton: it stores
    /// per-cell formulas/values and answers the read methods. Evaluation and name resolution
    /// grow as the corresponding milestones land; for now they return conservative results.
    /// </summary>
    internal sealed class FakeGrid : IGridContext
    {
        private readonly Dictionary<string, string> _formulas = new Dictionary<string, string>();
        private readonly Dictionary<string, object?> _values = new Dictionary<string, object?>();

        public FakeGrid Set(string a1, string? formula, object? value)
        {
            if (formula != null) _formulas[a1] = formula;
            _values[a1] = value;
            return this;
        }

        public EvalResult Evaluate(string fullyQualifiedExpr) => EvalResult.Empty;

        public string? GetFormula(RefAddress address) =>
            _formulas.TryGetValue(address.A1, out var f) ? f : null;

        public EvalResult GetValue(RefAddress address) =>
            _values.TryGetValue(address.A1, out var v) ? EvalResult.FromValue(v) : EvalResult.Empty;

        public RangeShape GetRangeShape(RefAddress address) =>
            new RangeShape(1, 1, false, false, null);

        public NameDefinition? ResolveName(string name, ScopeHint scope) => null;

        public string? GetValidationFormula(RefAddress address) => null;
    }
}
