# P6 validation report

Scope: verification of the native Linux port (P0-P5) against the functional
contract of the legacy WinForms client, on .NET 10 (10.0.403), Ubuntu 24.04
container, Avalonia 12.1.5.

## 1. Engine invariants

| Check | Method | Result |
| --- | --- | --- |
| Open/save round-trip without edits is byte-identical | `SaveFormatTests.OpenThenSave_RandomPayload_IsByteForByteIdentical` (24 seeded random payloads) | PASS |
| CRC1 / CRC2 recomputed and valid after edits | `ValidationTests.SaveAfterEdits_ReproducesValidCrcs` | PASS |
| Screen edits stay byte-local (money: 11448-11451; name: 456-466) | `ValidationTests.MoneyEdit_ChangesOnlyItsDocumentedBytes`, `ValidationTests.CharacterNameEdit_StaysWithinItsField` | PASS |
| Block switch preserves edits (block 2 = intermission copy) | `ValidationTests.EditsSurviveBlockSwitch` | PASS |
| Truncated / malformed files rejected by `CheckFile` | `ValidationTests.TruncatedFile_IsRejectedByCheckFile`, P0 suite | PASS |
| Undo / redo depth, edit flag reset on load | `UndoRedoTests` (P0) | PASS |
| Full-screen pipeline (10 screens × 1 edit) save → reopen → save is byte-stable | `ValidationTests.FullScreenPipeline_RoundTripIsByteStable` | PASS |

## 2. Map search

| Check | Method | Result |
| --- | --- | --- |
| 24 frozen entries match the reference baseline | `MapSearchTests.CalculateRange_MatchesTheFrozenBaseline` | PASS |
| 1-worker vs 4-worker output identical | P0 opt-in full baseline (`DQ9_SEARCH_FULL_BASELINE=1`, 389 592 entries) + `TreasureMapTests.SearchService_SameOutputWithOneAndFourWorkers` (4 096-row slice) | PASS |
| Cache file size validation, invalid size rejected | `TreasureMapTests.SearchService_CacheWithWrongSize_IsRejectedOnLoad` | PASS |
| Cancellation leaves no partial cache | `TreasureMapTests.SearchService_Cancellation_StopsGeneration` | PASS |
| Deterministic search results | `TreasureMapTests.SearchScreen_NameConditions_AreDeterministic` | PASS |

## 3. Paths and environment

| Check | Result |
| --- | --- |
| `XDG_CACHE_HOME` set to a rooted path is honoured | PASS (`ValidationTests.XdgPaths_HonoursCacheEnvVar(defined: True)`) |
| Relative `XDG_CACHE_HOME` ignored, profile fallback applies (`~/.cache/dq9-save-editor/`) | PASS (`defined: False`) |
| Nothing is written to the application directory | cache and config paths injected via `XdgPaths`; P2 audit |

## 4. Application runtime

| Check | Result |
| --- | --- |
| Headless smoke: open → edit (money, character HP, quest state) → save-as → reopen → verify | PASS (`DQ9_SMOKE=headless`) |
| UI smoke under Xvfb (1280×800×24): window shows and closes | PASS (`DQ9_SMOKE_UI=1 DISPLAY=:99 xvfb`) |
| Publish linux-x64 produces an ELF x86-64 binary | `file` + `readelf` checks (P3, CI-enforced) |
| Windows cross-build still compiles (net10.0-windows, EnableWindowsTargeting) | solution build 0 errors |

## 5. Test suite status

`dotnet test` — **117 tests, 0 failures** (~90 s, includes one full
map-search cache pass). Categories: save format (P0), encoding (P0),
undo/redo (P0), XDG paths (P2), map search baseline (P0), editor screens
(P4, 34 incl. the previously deferred editors), search pickers (P4, 5),
treasure maps + search (P5, 14), validation (P6, 8).

## 6. Known functional differences with the WinForms client

- **UI review of 2026-11-03**: the whole Avalonia shell was rendered
  offscreen (headless Skia, `DQ9_SMOKE_SHOTS=<dir>`): 12 business tabs + 2
  modal pickers verified. This review fixed two shell regressions: the
  TabControl was bound to the screen registry instead of the screen list
  (tab area empty since P3) and several new rows read engine values before
  a file was open.
- **Equipment / item picker**: the WinForms `ItemSelectWindow` is now
  replaced by the searchable `SearchPickerWindow` modal (type context,
  case-insensitive regex search, "(Empty)" first row) wired on the Items
  tab, in addition to the per-context ComboBox pickers. An item equipped
  but absent from the ComboBox list is displayed as "(none)" without
  rewriting the underlying value.
- **Character screen**: the skill / specialty effect bit editor, per-job
  revise grids ("vocation to edit" + 9 seeds 0-999), job skill levels
  (0-100), revocations and Zoom / Egg On are now exposed; they reuse the
  engine APIs (`CharacterData.IsLearnSkillSpecialtyEffect`,
  `GetRevisePower`…) covered by unit tests.
- **Staying list**: the completion-rate group of the 1 722-line
  `StayingListGroupBox` (monster / item / fashion / alchemy, 0-127) is now
  exposed next to the guest list core (name, level, job, place, lodging
  date, treasure map, move/delete/create).
- **Map search**: the WinForms dual-window layout (conditions window +
  results window) is one tab with a detail pane; result export to file is
  not implemented.
- **Wi-Fi Shopping**: item price is displayed read-only (engine writes it
  only through the DQVC download flow which is obsolete).
- **Windows presentation**: the legacy client remains the reference Windows
  build (P7 keeps the WinForms project); DQ9.Avalonia is also Windows-
  capable but not yet in the release matrix.
- **Preset windows** (`PresetWindow` / `Preset` / `PresetFigure`) and the
  About box remain WinForms-only conveniences.

All differences are engine-level no-ops or missing UI surfaces; no byte
format, CRC or undo behaviour diverges from the reference client.

## 7. Environment notes for reproducibility

```sh
export PATH="$HOME/.dotnet:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"
dotnet test tests/DQ9.Core.Tests/DQ9.Core.Tests.csproj
DQ9_SMOKE=headless dotnet src/DQ9.Avalonia/bin/Debug/net10.0/DQ9.Avalonia.dll
xvfb-run -a env DQ9_SMOKE_UI=1 dotnet src/DQ9.Avalonia/bin/Debug/net10.0/DQ9.Avalonia.dll
```
