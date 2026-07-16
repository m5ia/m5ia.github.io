namespace FormulaExplorer.Core.Grid
{
    /// <summary>
    /// Shape and storage nature of a range. Needed for positional contributor mapping (M6)
    /// and for correct write-back of array / spilled formulas (constraint #14).
    /// </summary>
    public readonly struct RangeShape
    {
        public RangeShape(int rows, int cols, bool hasArray, bool hasSpill, RefAddress? spillParent)
        {
            Rows = rows;
            Cols = cols;
            HasArray = hasArray;
            HasSpill = hasSpill;
            SpillParent = spillParent;
        }

        public int Rows { get; }
        public int Cols { get; }

        /// <summary>Legacy CSE array formula (Ctrl+Shift+Enter).</summary>
        public bool HasArray { get; }

        /// <summary>Dynamic-array spill anchor.</summary>
        public bool HasSpill { get; }

        /// <summary>For a spilled cell, the anchor that owns the formula; null otherwise.</summary>
        public RefAddress? SpillParent { get; }
    }
}
