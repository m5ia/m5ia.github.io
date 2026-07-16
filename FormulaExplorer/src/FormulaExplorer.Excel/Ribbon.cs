using System.Runtime.InteropServices;
using ExcelDna.Integration.CustomUI;

namespace FormulaExplorer.Excel
{
    /// <summary>
    /// Ribbon tab via Excel-DNA CustomUI. Excel-DNA auto-discovers the single ExcelRibbon-derived
    /// class and wires its <see cref="GetCustomUI"/>. M1: one "Close All" button.
    /// </summary>
    [ComVisible(true)]
    public sealed class Ribbon : ExcelRibbon
    {
        public override string GetCustomUI(string ribbonId)
        {
            return @"<customUI xmlns='http://schemas.microsoft.com/office/2009/07/customui'>
  <ribbon>
    <tabs>
      <tab id='feTab' label='Formula Explorer'>
        <group id='feWindowsGroup' label='Windows'>
          <button id='feCloseAll'
                  label='Close All'
                  size='large'
                  imageMso='WindowClose'
                  onAction='OnCloseAll'
                  screentip='Close all Formula Explorer windows' />
        </group>
      </tab>
    </tabs>
  </ribbon>
</customUI>";
        }

        /// <summary>Ribbon callback for the Close All button. Runs on Excel's main thread.</summary>
        public void OnCloseAll(IRibbonControl control)
        {
            WindowManager.CloseAll();
        }
    }
}
