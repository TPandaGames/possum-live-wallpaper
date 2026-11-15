#!/usr/bin/env bash
# Builds pocket-possum_<version>_all.deb from this repository.
#   linux/build-deb.sh [output-dir]
# Needs only dpkg-deb (any Debian/Ubuntu). The package is architecture independent (Python + web assets).
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$HERE/.." && pwd)"
OUT="${1:-$HERE/dist}"
VERSION="$(sed -n 's|.*<Version>\(.*\)</Version>.*|\1|p' "$ROOT/PocketPossum.csproj" | head -1)"
[ -n "$VERSION" ] || { echo "cannot read version from PocketPossum.csproj" >&2; exit 1; }

# Stage on a native Linux filesystem so file modes and owners are exact (not on a Windows mount)
STAGE="$(mktemp -d)"
trap 'rm -rf "$STAGE"' EXIT
PKG="$STAGE/pocket-possum_${VERSION}_all"

install -d "$PKG/DEBIAN" "$PKG/usr/bin" "$PKG/usr/share/pocket-possum/www" \
  "$PKG/usr/share/applications" "$PKG/usr/share/doc/pocket-possum" "$PKG/usr/share/icons/hicolor/scalable/apps"

install -m 0755 "$HERE/pocket-possum" "$PKG/usr/bin/pocket-possum"
sed -i "s/^VERSION = .*/VERSION = \"$VERSION\"/" "$PKG/usr/bin/pocket-possum"

# web assets (the same files the Windows build embeds)
cp -r "$ROOT/wwwroot/." "$PKG/usr/share/pocket-possum/www/"
find "$PKG/usr/share/pocket-possum" -type d -exec chmod 0755 {} +
find "$PKG/usr/share/pocket-possum" -type f -exec chmod 0644 {} +

install -m 0644 "$HERE/pocket-possum.desktop" "$PKG/usr/share/applications/pocket-possum.desktop"
install -m 0644 "$HERE/pocket-possum.svg" "$PKG/usr/share/icons/hicolor/scalable/apps/pocket-possum.svg"
install -m 0644 "$ROOT/README.md" "$PKG/usr/share/doc/pocket-possum/README.md"
install -m 0644 "$ROOT/THIRD_PARTY_NOTICES.md" "$PKG/usr/share/doc/pocket-possum/THIRD_PARTY_NOTICES.md"
install -m 0644 "$HERE/copyright" "$PKG/usr/share/doc/pocket-possum/copyright"
# upstream changelog is Markdown, so it is shipped under its own name rather than as a Debian changelog
install -m 0644 "$ROOT/CHANGELOG.md" "$PKG/usr/share/doc/pocket-possum/CHANGELOG.md"
gzip -9n "$PKG/usr/share/doc/pocket-possum/CHANGELOG.md"
cat > "$PKG/usr/share/doc/pocket-possum/changelog" <<EOF
pocket-possum ($VERSION) unstable; urgency=low

  * Release $VERSION.
  * See CHANGELOG.md.gz for the full history.

 -- TPandaGames <351797388+TPandaGames@users.noreply.github.com>  $(date -R)
EOF
gzip -9n "$PKG/usr/share/doc/pocket-possum/changelog"
chmod 0644 "$PKG/usr/share/doc/pocket-possum/changelog.gz"
install -d "$PKG/usr/share/man/man1"
gzip -9n -c "$HERE/pocket-possum.1" > "$PKG/usr/share/man/man1/pocket-possum.1.gz"
chmod 0644 "$PKG/usr/share/man/man1/pocket-possum.1.gz"

SIZE="$(du -sk --exclude=DEBIAN "$PKG" | cut -f1)"
cat > "$PKG/DEBIAN/control" <<EOF
Package: pocket-possum
Version: $VERSION
Section: x11
Priority: optional
Architecture: all
Maintainer: TPandaGames <351797388+TPandaGames@users.noreply.github.com>
Installed-Size: $SIZE
Depends: python3 (>= 3.9), python3-gi, python3-gi-cairo, gir1.2-gtk-3.0, gir1.2-webkit2-4.1
Recommends: xprintidle, fonts-noto-color-emoji
Homepage: https://github.com/TPandaGames/possum-live-wallpaper
Description: Possum Operational ScreenSaver (POSS): animated screensaver and live wallpaper
 A moonlit porch scene starring a possum (or a cat, raccoon, dog or
 lizard) and a chaotic family, with day and night, four seasons and
 optional real weather sync.
 .
 Runs as a full-screen screensaver on every monitor, as a live desktop
 wallpaper (X11), or in a window. An idle watcher starts the screensaver
 after a chosen time.
EOF

mkdir -p "$OUT"
dpkg-deb --root-owner-group --build "$PKG" "$OUT" >/dev/null
echo "built: $OUT/pocket-possum_${VERSION}_all.deb"
