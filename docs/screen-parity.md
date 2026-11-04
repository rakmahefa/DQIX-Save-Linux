# Screen parity matrix — WinForms client vs Avalonia client

Reference client: legacy `DQ9_Cheat` WinForms application (net10.0-windows).
New client: `src/DQ9.Avalonia` on the portable `DQ9.Core` engine.
Detailed residual gaps: [validation-report.md §6](validation-report.md).

Legend: ✔ migrated · ◐ partially migrated (core editing, some controls
deferred) · ✘ not migrated.

## Main window tabs

| # | WinForms control | Avalonia tab | Status | Notes |
| --- | --- | --- | --- | --- |
| 1 | `BasisDataControl` | General | ✔ | money/bank/medals, play & multi times, battle counters, alchemy count, save place, profile block, gestures (7 direction pairs + 15 learned party tricks) |
| 2 | `CharacterDataControl` | Characters | ✔ | 13 slots, identity, HP/MP, skill points, job levels, strategy, status, equipment (8 slots), appearance, per-job revise grids (9 seeds, "vocation to edit"), job skill levels (0-100), skill/specialty effect bit editor, Zoom / Egg On |
| 3 | `ItemDataControl` | Items | ✔ | 10 stores (bag…accessory) + 4 tool pouches, item picker by type, counts |
| 4 | `QuestDataControl` / `QuestPanel` | Quests | ✔ | states (4 settable + derived BeforeOrder), dates, clear/order counters + revisions |
| 5 | `TitleDataControl` | Titles & First clear | ✔ | 300+ title flags, count revision, first-clear records |
| 6 | `FirstClearDataControl` | Titles & First clear | ✔ | play times, victory/alchemy/quest/visitor/map counters, first-clear title |
| 7 | `MonsterDataControl` | Monsters | ✔ | 307 entries, victory/discovery/drop counts, lump edit (grouped undo), comp. count revision. Deferred: drop item names from raw counts |
| 8 | `ProcessFlagControl` | Flags | ✔ | disable ruler, 6 change-job flags, destination flags |
| 9 | `AlchemyDataControl` | Alchemy & Collections | ✔ | 471-recipe states + comp. count |
| 10 | `ItemCollectionDataControl` | Alchemy & Collections | ✔ | collection flags + comp. count revision |
| 11 | `SmartItemDataControl` | Alchemy & Collections | ✔ | smart item (DQVC) flags + comp. count revision |
| 12 | `RikkaInnDataControl` | Rikka inn | ✔ | grade, UID, visitor list (30), move/delete/create, name/level/job/place/lodging/map flag, comp-rate editors (monster/item/fashion/alchemy, 0-127) |
| 13 | `WifiShoppingDataControl` | Wi-Fi Shopping | ✔ | message + expiry, next-connect, 6 goods (item + count, price read-only) |
| 14 | `TreasureMapDataControl` | Treasure maps | ✔ | 99 slots, create/delete/move, state/rank/type/devil level/detector/renewer, grotto details, chest lists |
| — | `StayingListGroupBox` | Rikka inn | ✔ | guest list core + completion-rate group (see #12) |

## Secondary windows and selectors

| WinForms window | Avalonia replacement | Status | Notes |
| --- | --- | --- | --- |
| `ItemSelectWindow` | `SearchPickerWindow` modal + ComboBox pickers per context | ✔ | same `ItemDataList` tables; searchable modal list (type context, regex search, "(Empty)" first row) wired on the Items tab |
| `AddressSelectWindow` | ComboBox on General tab | ✔ | DQ areas + prefectures + others |
| `TitleSelectWindow` | ComboBox (first-clear title) | ✔ | full list on the Titles tab |
| `SavePlaceSelectWindow` | ComboBox on General tab | ✔ | full `SavePlaceList` |
| `HandleSelectWindow` | `SearchPickerWindow` modal on General tab | ✔ | occupations + accolades, sex-filtered titles, regex search |
| `PresetWindow` / `Preset` / `PresetFigure` | not migrated | ✘ | preset import/export is a WinForms-side convenience; engine-level preset data untouched |
| `CreateMapDataBaseDialog` | "New map" button + engine `CreateMapData` | ✔ | random base generation comes from the engine |
| `AboutBox` | not migrated | ✘ | version string shown in window title instead |
| Map search windows (`MapSearch/*`) | Map search tab | ✔ | conditions + results merged into one tab with detail pane; result export to file deferred |
| Address/handle/profile editing inside `BasisDataControl` | General tab | ✔ | |

## Engine parity (invariant)

All screens read and write through the same `DQ9.Core` engine types
(`DataValue<T>`, managers) used by the WinForms client; byte offsets,
bit packing, CRC handling and undo/redo semantics are shared code, so
editing behaviour cannot drift between the two clients. Byte-locality is
enforced by tests (`ValidationTests`), and the full-screen pipeline
(10 screens × 1 edit) is byte-stable across a save → reopen → save cycle.
