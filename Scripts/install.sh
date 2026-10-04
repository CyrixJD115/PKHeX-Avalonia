#!/usr/bin/env bash
# PKHeX-Avalonia — Linux installer.
#
# Downloads a PKHeX-Avalonia AppImage release and registers it in the application menu.
# Everything is installed into the user's own XDG directories (~/.local/...), so the script
# behaves identically on every distro and never needs root. The AppImage is installed under the
# stable name PKHeX-Avalonia.AppImage, which is also the path the in-app self-updater swaps in
# place — the menu entry keeps working across updates.
#
# Usage:
#   curl -fsSL https://raw.githubusercontent.com/realgarit/PKHeX-Avalonia/main/Scripts/install.sh | bash
#
# Options:
#   --version <x.y.z>   install a specific release instead of the latest
#   --uninstall         remove the installed app, menu entry, and icon
#   -h, --help          show this help
set -euo pipefail

REPO="realgarit/PKHeX-Avalonia"
CANONICAL_NAME="PKHeX-Avalonia.AppImage"
DESKTOP_ID="io.pkhex.avalonia"

VERSION=""
UNINSTALL=false
while [ $# -gt 0 ]; do
    case "$1" in
        --version)
            [ $# -ge 2 ] || { echo "--version needs a value (e.g. --version 1.87.4)" >&2; exit 2; }
            VERSION="${2#v}"
            shift 2
            ;;
        --uninstall) UNINSTALL=true; shift ;;
        -h|--help)
            cat <<'HELP'
PKHeX-Avalonia Linux installer — installs the AppImage release into your
user directories (~/.local/...) and adds it to the application menu.

Options:
  --version <x.y.z>   install a specific release instead of the latest
  --uninstall         remove the installed app, menu entry, and icon
  -h, --help          show this help
HELP
            exit 0
            ;;
        *) echo "Unknown option: $1 (try --help)" >&2; exit 2 ;;
    esac
done

command -v curl >/dev/null 2>&1 || { echo "curl is required (pacman/apt/dnf install curl)" >&2; exit 1; }

ARCH="$(uname -m)"
[ "$ARCH" = "x86_64" ] || {
    echo "The AppImage build is x86_64 only; this machine is ${ARCH}." >&2
    echo "Use the 'PKHeX-Avalonia-linux-x64.zip' portable build from ${REPO} releases instead." >&2
    exit 1
}

DATA_HOME="${XDG_DATA_HOME:-$HOME/.local/share}"
OPT_DIR="$HOME/.local/opt/PKHeX-Avalonia"
APPS_DIR="$DATA_HOME/applications"
ICON_DIR="$DATA_HOME/icons/hicolor/64x64/apps"
INSTALL_PATH="$OPT_DIR/$CANONICAL_NAME"
DESKTOP_FILE="$APPS_DIR/$DESKTOP_ID.desktop"
ICON_FILE="$ICON_DIR/$DESKTOP_ID.png"

refresh_databases() {
    if command -v update-desktop-database >/dev/null 2>&1; then
        update-desktop-database "$APPS_DIR" >/dev/null 2>&1 || true
    fi
    if command -v gtk-update-icon-cache >/dev/null 2>&1; then
        gtk-update-icon-cache -f -t "$DATA_HOME/icons/hicolor" >/dev/null 2>&1 || true
    fi
}

if $UNINSTALL; then
    rm -f "$DESKTOP_FILE" "$INSTALL_PATH" "$ICON_FILE"
    rmdir "$OPT_DIR" 2>/dev/null || true
    refresh_databases
    echo "PKHeX-Avalonia removed from the application menu."
    exit 0
fi

# --- resolve the release ---------------------------------------------------------------
RELEASE_JSON="$(mktemp)"
trap 'rm -f "$RELEASE_JSON"' EXIT

if [ -n "$VERSION" ]; then
    RELEASE_TAG="v$VERSION"
    if ! curl -fsSL --retry 3 "https://api.github.com/repos/$REPO/releases/tags/$RELEASE_TAG" -o "$RELEASE_JSON"; then
        echo "Could not find release $RELEASE_TAG on $REPO." >&2
        exit 1
    fi
else
    if ! curl -fsSL --retry 3 "https://api.github.com/repos/$REPO/releases/latest" -o "$RELEASE_JSON"; then
        echo "Could not reach the GitHub API (offline, or rate-limited — retry in a bit," >&2
        echo "or pin a version with: install.sh --version 1.87.4)" >&2
        exit 1
    fi
    RELEASE_TAG="$(sed -n 's/.*"tag_name": *"\([^"]*\)".*/\1/p' "$RELEASE_JSON" | head -n1)"
    VERSION="${RELEASE_TAG#v}"
fi
[ -n "$VERSION" ] || { echo "Could not determine the release version from the GitHub response." >&2; exit 1; }

ASSET="PKHeX-Avalonia-${VERSION}-x86_64.AppImage"
DOWNLOAD_URL="https://github.com/$REPO/releases/download/v${VERSION}/${ASSET}"

# The GitHub API reports a sha256 digest per asset; verify against it when present (same policy
# as the in-app updater: an absent digest is warned about, not fatal). The digest line follows the
# asset's "name" line inside the same JSON object (~27 lines), well before the next asset begins.
DIGEST="$(grep -A 30 "\"name\": \"$ASSET\"" "$RELEASE_JSON" | sed -n 's/.*"digest": *"sha256:\([0-9a-f]\{64\}\)".*/\1/p' | head -n1)"

# --- download + verify -----------------------------------------------------------------
STAGING="$(mktemp --suffix=.AppImage 2>/dev/null || mktemp)"
trap 'rm -f "$RELEASE_JSON" "$STAGING"' EXIT

echo "Downloading $DOWNLOAD_URL ..."
curl -fL --retry 3 -o "$STAGING" "$DOWNLOAD_URL"

if [ -n "$DIGEST" ] && command -v sha256sum >/dev/null 2>&1; then
    echo "Verifying checksum ..."
    echo "${DIGEST}  ${STAGING}" | sha256sum --check --strict - || {
        echo "Checksum mismatch — the download is corrupt or was tampered with. Aborting." >&2
        exit 1
    }
elif [ -z "$DIGEST" ]; then
    echo "Note: release has no published checksum; skipping verification." >&2
fi

# --- install ---------------------------------------------------------------------------
mkdir -p "$OPT_DIR" "$APPS_DIR" "$ICON_DIR"
mv -f "$STAGING" "$INSTALL_PATH"
chmod 755 "$INSTALL_PATH"

# The icon ships in the repository next to the app; prefer the tagged copy, fall back to main.
ICON_URL_BASE="https://raw.githubusercontent.com/$REPO"
if ! curl -fsSL --retry 2 -o "$ICON_FILE" "$ICON_URL_BASE/v${VERSION}/PKHeX.Avalonia/Assets/Icons/icon.png"; then
    if ! curl -fsSL --retry 2 -o "$ICON_FILE" "$ICON_URL_BASE/main/PKHeX.Avalonia/Assets/Icons/icon.png"; then
        echo "Note: could not download the icon; the menu entry will use a generic icon." >&2
        rm -f "$ICON_FILE"
    fi
fi

# Desktop Entry spec: the exec value is double-quoted with $ ` " \ escaped.
ESCAPED_EXEC="$(printf '%s' "$INSTALL_PATH" | sed 's/["\\$`]/\\&/g')"
cat > "$DESKTOP_FILE" <<EOF
[Desktop Entry]
Type=Application
Name=PKHeX-Avalonia
Comment=Pokémon save file editor
Exec="$ESCAPED_EXEC"
TryExec=$INSTALL_PATH
Icon=$DESKTOP_ID
Categories=Utility;
Terminal=false
StartupWMClass=PKHeX.Avalonia
EOF

refresh_databases

echo ""
echo "Installed PKHeX-Avalonia ${VERSION}:"
echo "  App:       $INSTALL_PATH"
echo "  Menu entry: $DESKTOP_FILE"
echo ""
echo "Launch it from your application menu, or run: $INSTALL_PATH"
echo "The in-app updater keeps this copy updated in place."
echo "To remove it later: this script with --uninstall, or Settings inside the app."
