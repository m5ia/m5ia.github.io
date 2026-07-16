using FormulaExplorer.Core.Grid;
using FormulaExplorer.Core.ViewModels;
using Xunit;

namespace FormulaExplorer.Core.Tests
{
    public class ExplorerViewModelTests
    {
        [Fact]
        public void Title_is_generic_when_no_address()
        {
            var vm = new ExplorerViewModel();
            Assert.Equal("Formula Explorer", vm.Title);
        }

        [Fact]
        public void Title_includes_address_and_raises_change_notification()
        {
            var vm = new ExplorerViewModel();
            var changed = new System.Collections.Generic.List<string?>();
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.CellAddress = "Sheet1!$A$1";

            Assert.Equal("Formula Explorer — Sheet1!$A$1", vm.Title);
            Assert.Contains(nameof(ExplorerViewModel.CellAddress), changed);
            Assert.Contains(nameof(ExplorerViewModel.Title), changed);
        }
    }

    public class FakeGridTests
    {
        [Fact]
        public void GetFormula_roundtrips_through_the_seam()
        {
            var grid = new FakeGrid().Set("$A$1", "=1+1", 2.0);
            var addr = new RefAddress("Book1.xlsx", "Sheet1", "$A$1", true);

            Assert.Equal("=1+1", ((IGridContext)grid).GetFormula(addr));
            Assert.Equal(2.0, ((IGridContext)grid).GetValue(addr).Value);
        }
    }

    public class RefAddressTests
    {
        [Fact]
        public void ToString_quotes_book_qualified_address()
        {
            var addr = new RefAddress("Book1.xlsx", "Sheet 1", "$A$1", true);
            Assert.Equal("'[Book1.xlsx]Sheet 1'!$A$1", addr.ToString());
        }

        [Fact]
        public void ToString_leaves_bare_sheet_unquoted()
        {
            var addr = new RefAddress(null, "Sheet1", "$A$1", true);
            Assert.Equal("Sheet1!$A$1", addr.ToString());
        }
    }
}
