using System.Windows;
using System.Windows.Input;
using FormulaExplorer.Core.ViewModels;

namespace FormulaExplorer.UI
{
    /// <summary>
    /// A single non-modal Explorer window. Owned by Excel's main hwnd (set by the caller in
    /// the Excel project via WindowInteropHelper) so it floats above Excel but not above
    /// everything else. Pure view: it binds to an <see cref="ExplorerViewModel"/> and knows
    /// nothing about COM or Excel.
    /// </summary>
    public partial class ExplorerWindow : Window
    {
        public ExplorerWindow(ExplorerViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        /// <summary>M1: Esc closes the window.</summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                Close();
                return;
            }
            base.OnKeyDown(e);
        }
    }
}
