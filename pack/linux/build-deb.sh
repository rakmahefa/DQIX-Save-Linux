#!/usr/bin/env bash
# Builds a Debian package for DQ9 Save Editor (Avalonia client).
# Layout: /opt/dq9-save-editor  (framework-dependent publish)
#         /usr/bin/dq9-save-editor (launcher script)
#         /usr/share/applications/dq9-save-editor.desktop
# Requires: dotnet publish (done here), dpkg-deb.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
VERSION="${1:-0.7.0}"
OUT="${2:-$ROOT/dist}"
WORK="$OUT/debroot"
PKG="dq9-save-editor"

rm -rf "$WORK"
mkdir -p "$WORK/opt/$PKG" "$WORK/usr/bin" "$WORK/usr/share/applications" \
         "$WORK/usr/share/doc/$PKG" "$WORK/DEBIAN" "$OUT"

echo "==> dotnet publish (linux-x64, framework-dependent)"
dotnet publish "$ROOT/src/DQ9.Avalonia/DQ9.Avalonia.csproj" \
    -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=false \
    -o "$WORK/opt/$PKG" /p:DebugType=None /p:DebugSymbols=false

cat > "$WORK/usr/bin/$PKG" <<LAUNCH
#!/bin/sh
exec /opt/$PKG/DQ9.Avalonia "\$@"
LAUNCH
chmod 755 "$WORK/usr/bin/$PKG"

cat > "$WORK/usr/share/applications/$PKG.desktop" <<DESKTOP
[Desktop Entry]
Type=Application
Name=DQ9 Save Editor
Comment=Dragon Quest IX save data editor (Avalonia)
Exec=/opt/$PKG/DQ9.Avalonia
Icon=dq9-save-editor
Categories=Game;Utility;
DESKTOP

INSTALLED_SIZE=$(du -sk "$WORK" | cut -f1)
cat > "$WORK/DEBIAN/control" <<CONTROL
Package: $PKG
Version: $VERSION
Section: games
Priority: optional
Architecture: amd64
Installed-Size: $INSTALLED_SIZE
Depends: libfontconfig1, libx11-6
Maintainer: DQ9 port project <dq9-port@invalid>
Description: Dragon Quest IX save data editor (Avalonia client)
 Cross-platform editor for Dragon Quest IX save files, built on the
 portable DQ9.Core engine. Native Linux client replacing the legacy
 WinForms application.
CONTROL

echo "==> dpkg-deb --build"
dpkg-deb --root-owner-group --build "$WORK" "$OUT/${PKG}_${VERSION}_amd64.deb"
echo "==> $(ls -la "$OUT/${PKG}_${VERSION}_amd64.deb")"
