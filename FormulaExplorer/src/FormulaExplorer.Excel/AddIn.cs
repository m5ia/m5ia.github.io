using System;
using ExcelDna.Integration;
using ExcelDna.Logging;
using Microsoft.Office.Interop.Excel;

namespace FormulaExplorer.Excel
{
    /// <summary>
    /// Add-in lifecycle. Owns OnKey registration for the hotkeys we deliberately steal, and
    /// restores them on unload.
    /// <para>
    /// M1 steals only Ctrl+Q (Quick Analysis). Ctrl+Backspace ("scroll to active cell") is
    /// stolen in M4 when the back-stack exists. We re-register on WorkbookActivate because
    /// other add-ins can clobber OnKey bindings (constraint #8).
    /// </para>
    /// </summary>
    public sealed class AddIn : IExcelAddIn
    {
        // Ctrl+Q in OnKey syntax.
        private const string CtrlQ = "^q";

        // Held to keep the COM event subscription alive for the add-in's lifetime.
        private static Application? _app;
        private static AppEvents_WorkbookActivateEventHandler? _workbookActivateHandler;

        public void AutoOpen()
        {
            ComGate.Initialize();

            try
            {
                ComGate.Run(() =>
                {
                    _app = (Application)ExcelDnaUtil.Application;
                    RegisterHotkeys(_app);

                    _workbookActivateHandler = _ => ReRegisterHotkeys();
                    _app.WorkbookActivate += _workbookActivateHandler;
                });
            }
            catch (Exception ex)
            {
                LogDisplay.WriteLine("Formula Explorer: AutoOpen failed — " + ex.Message);
            }
        }

        public void AutoClose()
        {
            try
            {
                ComGate.Run(() =>
                {
                    if (_app != null)
                    {
                        if (_workbookActivateHandler != null)
                        {
                            _app.WorkbookActivate -= _workbookActivateHandler;
                            _workbookActivateHandler = null;
                        }

                        // Restore Ctrl+Q to Excel's default: pass the key with the procedure
                        // OMITTED (Type.Missing). Passing "" would DISABLE the key instead
                        // of restoring it (constraint #8).
                        _app.OnKey(CtrlQ, Type.Missing);
                    }
                });
            }
            catch (Exception ex)
            {
                LogDisplay.WriteLine("Formula Explorer: AutoClose cleanup failed — " + ex.Message);
            }
            finally
            {
                WindowManager.CloseAll();
                _app = null;
            }
        }

        private static void RegisterHotkeys(Application app)
        {
            app.OnKey(CtrlQ, Commands.ShowExplorerMacro);
        }

        private static void ReRegisterHotkeys()
        {
            try
            {
                if (_app != null)
                    RegisterHotkeys(_app);
            }
            catch (Exception ex)
            {
                LogDisplay.WriteLine("Formula Explorer: re-register on WorkbookActivate failed — " + ex.Message);
            }
        }
    }
}
