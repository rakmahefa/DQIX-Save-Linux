# Linux port notes — runtime, packages and validated distributions

Companion documents: [save-format.md](save-format.md),
[validation-report.md](validation-report.md), [screen-parity.md](screen-parity.md).

## System libraries required by the Avalonia client

The linux-x64 build is **self-contained** in the packages below (the .NET 10
runtime is bundled). Avalonia still links a few system libraries at runtime:

| Library | Package (Debian/Ubuntu) | Used for |
| --- | --- | --- |
| libfontconfig.so.1 | `libfontconfig1` | font resolution (Skia) |
| libX11.so.6 | `libx11-6` | X11 backend (XWayland included) |
| libxcb.so.1 | `libxcb1` | pulled by libX11 |
| libICE/libSM | optional | only for some window-manager integrations |
| Wayland | optional | Avalonia 12.1 ships a native Wayland backend; X11 is the default (`--x11` flag not needed) |

No `glibc` constraints beyond Ubuntu 20.04-era (the publish RID is
`linux-x64` against a stock 10.0 runtime).

## Packaging

Two packagers live in `pack/linux/`:

- **`build-deb.sh [version] [out]`** — self-contained publish into
  `/opt/dq9-save-editor`, launcher in `/usr/bin/dq9-save-editor`,
  desktop entry in `/usr/share/applications`. Produces
  `dq9-save-editor_<version>_amd64.deb`.
- **`build-appimage.sh [version] [out]`** — self-contained publish into an
  AppDir plus generated `.desktop`, `AppRun` and a 64x64 placeholder icon;
  packaged with `appimagetool` (downloaded on demand and extracted with
  `--appimage-extract`, so FUSE is not required to build). Produces
  `dq9-save-editor-<version>-x86_64.AppImage` (+ `.zsync`).

Both are exercised in CI (job `linux-packages`) including a no-FUSE smoke
test of the AppImage: `DQ9_SMOKE=headless ./dist/dq9-save-editor-*.AppImage
--appimage-extract-and-run`.

## Validated environments

| Environment | Status | Notes |
| --- | --- | --- |
| Ubuntu 24.04 container (CI image), Xvfb | validated | tests + headless + UI smoke |
| Ubuntu 24.04, Xvfb 1280x800x24, DPI 96 | validated | `DQ9_SMOKE_UI=1` |
| Same, DPI 192 (`GDK_SCALE`-style scaling via Avalonia `--global-scale-factor 2`) | smoke only | layout scales through Avalonia layout engine |
| Debian 13 / Ubuntu 22.04 | expected compatible | libfontconfig1 + libx11-6 are the only runtime deps; not executed end-to-end in CI |
| AppImage on a clean Ubuntu 24.04 container | validated via `--appimage-extract-and-run` | full headless pipeline inside the image |
| Wayland session | expected compatible | Avalonia 12.1 native backend; not exercised in CI (no compositor) |

## Archive contents

- `DQ9.Avalonia-linux-x64.tar.gz` — framework-dependent publish
  (requires .NET 10 runtime), CI artifact.
- `dq9-save-editor_<version>_amd64.deb` — self-contained Debian package.
- `dq9-save-editor-<version>-x86_64.AppImage` — self-contained AppImage.

The tar.gz is the "developer" artifact; the .deb and the AppImage are the
end-user artifacts. All three are verified with the headless smoke pipeline
before upload.
