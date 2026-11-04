# DQ9 save file format — reference notes

Reverse-engineered from the legacy DQ9_Cheat 0.7 editor (decompiled) and
verified by `tests/DQ9.Core.Tests`. All multi-byte integers are little-endian.

## 1. File layout

| Region | Size | Notes |
|---|---|---|
| Lead header | H bytes (often 0) | Whatever precedes the signature; stored verbatim and written back on save |
| Block 1 | 32 768 bytes | First block, starts with the ASCII signature |
| Block 2 | 32 768 bytes | Second block, same structure |
| Footer | rest | Preserved verbatim on save |

- `SaveDataManager.CheckFile` scans the first 1 024 bytes for the 15-byte
  ASCII signature `"DRAGON QUEST IX"` (its first four bytes "DRAG" are also
  tested as a little-endian uint32 = 1 195 463 236) and returns the signature
  offset H, or -1 when the stream is shorter than 65 536 bytes or the
  signature is missing.
- Both blocks are read starting at position H, so **each 32 768-byte block
  begins with the signature itself** (`Data[0..15)`).
- Files shorter than 65 536 bytes are rejected; nothing else is validated at
  open time (a corrupted CRC does not block loading — see §3).

## 2. Block structure (offsets relative to block start)

| Offset | Size | Content |
|---|---|---|
| 0 | 15 | `"DRAGON QUEST IX"` signature |
| 15 | 1 | Unknown/reserved, preserved |
| 16 | 4 | CRC32 of bytes `[20..36)` |
| 20 | 1 | IsIntermission flag (read clamped to 0/1) |
| 132 | 4 | CRC32 of bytes `[136..28 644)` |
| 136 | 28 508 | Main data region (characters at 136, 13 × 572 bytes; alchemy flags 15 020 + 2 × index; visitors 16 200 + 232 × index; treasure maps 24 206 + 28 × index...) |

Selected field offsets documented by the code (see `src/DQ9.Core/DataManager/*.cs`
for the full map): money 11 448, bank 11 452, medal 11 460, save place 11 476,
profile 26 980..26 988, play time 16 024..16 027, multi-play time 16 028..16 031,
battle counters 16 076..16 088, quests 16 188..., first-clear data 16 168..16 188.

## 3. CRC rules

- CRC32, reflected, polynomial `0xEDB88320`, init and final XOR
  `0xFFFFFFFF` (standard zlib-style CRC32, see `SaveDataManager.MakeCRCTable`).
- CRC 1 covers the 16 bytes at `[20..36)` and is stored at `[16..20)`.
- CRC 2 covers the 28 508 bytes at `[136..28 644)` and is stored at
  `[132..136)`.
- On save both CRCs are recomputed from the current buffer
  (`SaveDataManager.WriteData`); on open they are **not** verified — a file
  with corrupted CRCs still loads, and saving repairs the fields.

## 4. Round-trip guarantees

Opening then saving without edits is byte-for-byte identical for files whose
derived map data is consistent (the loader re-derives treasure-map detail
objects in memory but does not rewrite block bytes). `OpenThenSave_*` tests in
`SaveFormatTests` pin this behaviour for plain, lead-header, footer and
intermission fixtures, including fully randomised main regions.

`SaveDataManager.ReadData` resets the previous footer: a document without a
footer must not inherit one from a previously opened file (regression found
by the P0 tests on the shared singleton).
