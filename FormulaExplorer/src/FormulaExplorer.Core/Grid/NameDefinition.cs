namespace FormulaExplorer.Core.Grid
{
    public enum NameKind
    {
        Constant,
        Formula,
        Lambda,
        RefError,
        External
    }

    /// <summary>
    /// A resolved defined name / LAMBDA / validation name (M8).
    /// External links and #REF! names must degrade gracefully, not crash (constraint #15).
    /// </summary>
    public readonly struct NameDefinition
    {
        public NameDefinition(string name, NameKind kind, string refersTo, ScopeHint scope)
        {
            Name = name;
            Kind = kind;
            RefersTo = refersTo;
            Scope = scope;
        }

        public string Name { get; }
        public NameKind Kind { get; }

        /// <summary>The invariant RefersTo text (constant, formula, or LAMBDA), spliced into the AST as a subtree (M8).</summary>
        public string RefersTo { get; }

        public ScopeHint Scope { get; }
    }
}
