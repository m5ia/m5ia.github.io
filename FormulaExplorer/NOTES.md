# NOTES — Excel behaviours discovered the hard way

This file is the real asset. Every Excel behaviour we hit, with the formula or action that
exposed it, goes here. Design decisions that follow from a behaviour go here too.

---

## Architecture invariants (do not erode)

- **`FormulaExplorer.Core` has ZERO Excel/COM references.** Everything it needs from a live
  workbook crosses the `IGridContext` seam. If a COM type ever appears in Core, the interface
  is wrong — fix the interface, not by adding a reference.
- **`FormulaExplorer.Excel` is the only project that touches COM.**
- **`FormulaExplorer.UI` is WPF only** — no COM, no business logic; it binds to Core view-models.
- **All COM goes through `ComGate`** (see constraint #7 below).

---

## Constraints already designed around (from the spec) and how M1 handles them

Only the ones M1 actually touches are "done"; the rest are noted so we don't rediscover them.

- **#4 Never write to a cell.** M1 read path is `ActiveCell` address + `Formula2` only. Read-only.
- **#7 COM from non-macro contexts throws `0x800AC472` (VBA_E_IGNORE) when Excel is busy.**
  `ComGate` is the single choke point: it marshals to the main thread via
  `ExcelAsyncUtil.QueueAsMacro` when called off-thread (e.g. WPF handlers) and retries with
  backoff on VBA_E_IGNORE. Built in M1 as required. The main-thread id is captured in `AutoOpen`
  so a main-thread caller runs directly (re-queueing from the main thread would deadlock).
- **#8 `OnKey` needs a registered XLL command.** `Commands.ShowExplorer` is
  `[ExcelCommand(Name = "FormulaExplorer_Show")]`; `AutoOpen` binds `^q` to it. We re-register on
  `WorkbookActivate` because other add-ins can clobber the binding. `AutoClose` restores Ctrl+Q
  by calling `OnKey("^q", Type.Missing)` — **procedure OMITTED, not `""`**: passing `""` DISABLES
  the key rather than restoring Excel's default (Quick Analysis).
  - M1 steals **only Ctrl+Q**. Ctrl+Backspace is stolen in M4 with the back-stack.
- **#9 Read `Formula2`, never `Formula`/`FormulaLocal`.** `Formula` injects implicit-intersection
  `@`; `FormulaLocal` is locale-dependent. We access `Formula2` **late-bound (`dynamic`)** so we
  don't pin a specific Office PIA that predates the property, and fall back to `Formula` on
  pre-2019 Excel (catch `COMException` / `RuntimeBinderException`).
- **#16 Windows Store Office ignores per-user COM add-ins.** Installer needs a per-machine option
  (M9). Noted now so it isn't forgotten.

Constraints #1, #2, #3, #5, #6, #10–#15 belong to later milestones; each will get an entry here
with the formula that exposed it, as we implement it.

---

## Open questions / things to verify in front of Excel (M1)

These cannot be verified without Excel on Windows. Click-by-click checks are in the commit
message / handoff. If any turns out wrong, STOP and flag it (do not silently swap the stack).

1. **WPF window pumps messages on Excel's main thread.** We show a non-modal WPF `Window` on
   Excel's main (STA) thread and rely on Excel's own message loop to pump WPF messages. This is
   the standard Excel-DNA pattern; verify Esc/close/resize actually respond.
2. **`ExcelRibbon` auto-discovery.** We rely on Excel-DNA finding the single `ExcelRibbon`-derived
   class and wiring `GetCustomUI` without a `<CustomUI>` entry in the `.dna`. Verify the tab shows.
3. **`.dna` packing of project references.** `FormulaExplorer.Excel-AddIn.dna` lists
   `FormulaExplorer.Core.dll` / `FormulaExplorer.UI.dll` as packed `<Reference>`s. Verify the
   packed `-AddIn64-packed.xll` loads standalone.
4. **Debug launch path.** `launchSettings.json` and the csproj `StartProgram` point at
   `Office16`. Adjust for your Office version/bitness.

---

## Corpus / fixtures

- `tests/corpus/formulas.txt` — grows every time something breaks. Parse-tree snapshots land here
  from M2 onward.
- Recorded replay fixtures (`RecordingGridContext` → JSON) land in `tests/.../fixtures/` from M3.
