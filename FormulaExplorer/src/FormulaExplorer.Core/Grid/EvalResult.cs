namespace FormulaExplorer.Core.Grid
{
    public enum EvalKind
    {
        Value,
        Error,
        Reference,
        Empty
    }

    /// <summary>
    /// Result of an <see cref="IGridContext.Evaluate"/> / <see cref="IGridContext.GetValue"/> call.
    /// Exactly one payload is meaningful per <see cref="Kind"/>.
    /// <para>Errors are values, not failures (constraint #5): eager evaluation of an untaken
    /// branch legitimately yields e.g. #DIV/0!, and that must surface as a value.</para>
    /// </summary>
    public readonly struct EvalResult
    {
        public EvalResult(EvalKind kind, object? value, string? errorText, RefAddress reference)
        {
            Kind = kind;
            Value = value;
            ErrorText = errorText;
            Reference = reference;
        }

        public EvalKind Kind { get; }

        /// <summary>Boxed double/string/bool/DateTime when <see cref="Kind"/> == Value.</summary>
        public object? Value { get; }

        /// <summary>"#DIV/0!" etc. when <see cref="Kind"/> == Error.</summary>
        public string? ErrorText { get; }

        /// <summary>Populated when <see cref="Kind"/> == Reference (constraint #3: free target resolution).</summary>
        public RefAddress Reference { get; }

        public static readonly EvalResult Empty =
            new EvalResult(EvalKind.Empty, null, null, default);

        public static EvalResult FromValue(object? value) =>
            new EvalResult(EvalKind.Value, value, null, default);

        public static EvalResult FromError(string errorText) =>
            new EvalResult(EvalKind.Error, null, errorText, default);

        public static EvalResult FromReference(RefAddress reference) =>
            new EvalResult(EvalKind.Reference, null, null, reference);

        public override string ToString()
        {
            switch (Kind)
            {
                case EvalKind.Value: return Value?.ToString() ?? "";
                case EvalKind.Error: return ErrorText ?? "#ERROR";
                case EvalKind.Reference: return Reference.ToString();
                default: return "";
            }
        }
    }
}
