using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FormulaExplorer.Core.ViewModels
{
    /// <summary>
    /// View-model backing a single Explorer window. Constructed in the Excel project from a
    /// live cell; bound by the WPF window in the UI project. Contains no COM and no Excel types.
    /// <para>
    /// M1 scope: just the window title (cell address) and the raw formula text. The structural
    /// tree, values and locations arrive in later milestones.
    /// </para>
    /// </summary>
    public sealed class ExplorerViewModel : INotifyPropertyChanged
    {
        private string _cellAddress = "";
        private string _rawFormula = "";

        /// <summary>Human-readable origin address, e.g. "Sheet1!$A$1" — shown in the title bar.</summary>
        public string CellAddress
        {
            get => _cellAddress;
            set => Set(ref _cellAddress, value);
        }

        /// <summary>The invariant <c>Formula2</c> text of the origin cell (M1 shows only this).</summary>
        public string RawFormula
        {
            get => _rawFormula;
            set => Set(ref _rawFormula, value);
        }

        /// <summary>Title bar text.</summary>
        public string Title => string.IsNullOrEmpty(CellAddress)
            ? "Formula Explorer"
            : "Formula Explorer — " + CellAddress;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            if (name == nameof(CellAddress))
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
        }
    }
}
