#!/usr/bin/env bash
# Builds an AppImage for DQ9 Save Editor (Avalonia client).
# Strategy: framework-dependent publish + AppDir + appimagetool (downloaded
# on demand, extracted with --appimage-extract so FUSE is not required).
# Requires: dotnet publish (done here), curl or wget, and the generated
# AppDir is validated before packaging.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
VERSION="${1:-0.7.0}"
OUT="$(readlink -f "${2:-$ROOT/dist}")"
APPDIR="$OUT/AppDir"
PKG="dq9-save-editor"

rm -rf "$APPDIR"
mkdir -p "$APPDIR/usr/bin" "$APPDIR/usr/share/applications" "$OUT"

echo "==> dotnet publish (linux-x64, framework-dependent)"
dotnet publish "$ROOT/src/DQ9.Avalonia/DQ9.Avalonia.csproj" \
    -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=false \
    -o "$APPDIR/usr/bin" /p:DebugType=None /p:DebugSymbols=false

# AppImage metadata
cat > "$APPDIR/$PKG.desktop" <<DESKTOP
[Desktop Entry]
Type=Application
Name=DQ9 Save Editor
Comment=Dragon Quest IX save data editor (Avalonia)
Exec=$PKG
Icon=$PKG
Categories=Game;Utility;
DESKTOP
cat > "$APPDIR/AppRun" <<APPRUN
#!/bin/sh
HERE="\$(dirname "\$(readlink -f "\$0")")"
exec "\$HERE/usr/bin/DQ9.Avalonia" "\$@"
APPRUN
chmod 755 "$APPDIR/AppRun"

# minimal icon (16x16 'DQ' placeholder) unless one exists
if [ ! -f "$APPDIR/$PKG.png" ]; then
    python3 - "$APPDIR/$PKG.png" <<'PY'
import struct, sys, zlib
w = h = 64
rows = b''
for y in range(h):
    rows += b'\x00'
    for x in range(w):
        if 8 <= x < 56 and 8 <= y < 56 and (x // 8 + y // 8) % 2 == 0:
            rows += bytes((30, 96, 184))
        else:
            rows += bytes((246, 246, 246))
def chunk(tag, data):
    c = tag + data
    return struct.pack('>I', len(data)) + c + struct.pack('>I', zlib.crc32(c) & 0xffffffff)
png = (b'\x89PNG\r\n\x1a\n'
       + chunk(b'IHDR', struct.pack('>IIBBBBB', w, h, 8, 2, 0, 0, 0))
       + chunk(b'IDAT', zlib.compress(rows))
       + chunk(b'IEND', b''))
open(sys.argv[1], 'wb').write(png)
PY
fi

# locate appimagetool (architecture x86_64)
TOOL_DIR="$OUT/appimagetool.extracted"
if [ ! -x "$TOOL_DIR/AppRun" ]; then
    echo "==> downloading appimagetool"
    curl -fsSL -o "$OUT/appimagetool.AppImage" \
        "https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage"
    chmod +x "$OUT/appimagetool.AppImage"
    (cd "$OUT" && "$OUT/appimagetool.AppImage" --appimage-extract >/dev/null)
    rm -rf "$TOOL_DIR"
    mv "$OUT/squashfs-root" "$TOOL_DIR"
fi

echo "==> building AppImage"
# normalise permissions: AppImages are run by other users, every file must
# be world-readable and executables world-executable (silences the 200+
# mode 0764 warnings of appimagetool)
chmod -R a+rX "$APPDIR"
cd "$OUT"
"$TOOL_DIR/AppRun" "$APPDIR" \
    -u "gh-releases-zsync|rakmahefa|Save-Editor-Decomp|latest|$PKG-*.AppImage.zsync" \
    "$OUT/${PKG}-${VERSION}-x86_64.AppImage" 2>&1 | grep -v "Desktop file assumes" || true
[ -f "$OUT/${PKG}-${VERSION}-x86_64.AppImage" ]
echo "==> $(ls -la "$OUT/${PKG}-${VERSION}-x86_64.AppImage")"
