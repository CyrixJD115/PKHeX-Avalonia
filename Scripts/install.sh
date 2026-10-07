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
#   curl -fsSL https://raw.githubusercontent.com/realgarit/PKHeX-Avalonia/main/Scripts/install.sh | bash -s -- --uninstall
#
# Options:
#   --version <x.y.z>   install a specific release instead of the latest
#   --uninstall         remove the installed app, menu entry, and icon (keeps save files and app data)
#   -h, --help          show this help
#
# Colors are automatic when stdout is a terminal; set NO_COLOR=1 to force plain output.
set -euo pipefail

REPO="realgarit/PKHeX-Avalonia"
CANONICAL_NAME="PKHeX-Avalonia.AppImage"
DESKTOP_ID="io.pkhex.avalonia"

# --- output helpers --------------------------------------------------------------------
# Plain output when stdout is not a TTY (piped to a log), NO_COLOR is set, or TERM is dumb.
# Palette matches the app's own branding: the icon's Poké Ball red (~#CD1818 → 256-color 160),
# the white of the app text, and the CompactAccentBrush rose (#B05763 → 256-color 131).
if [ -t 1 ] && [ -z "${NO_COLOR:-}" ] && [ "${TERM:-}" != "dumb" ]; then
    C_RESET=$'\033[0m'
    C_BOLD=$'\033[1m'
    C_DIM=$'\033[2m'
    C_RED=$'\033[1;38;5;160m'      # brand red (icon Poké Ball red)
    C_WHITE=$'\033[1;97m'          # bold bright white (app foreground)
    C_ACCENT=$'\033[38;5;131m'     # app accent rose (CompactAccentBrush)
    C_OK=$'\033[38;5;114m'
    C_WARN=$'\033[38;5;215m'
    C_ERR=$'\033[1;38;5;203m'
else
    C_RESET=""; C_BOLD=""; C_DIM=""; C_RED=""; C_WHITE=""
    C_ACCENT=""; C_OK=""; C_WARN=""; C_ERR=""
fi

info() { printf '%s\n' "${C_ACCENT}  ▶${C_RESET} $*"; }
ok()   { printf '%s\n' "${C_OK}  ✓${C_RESET} $*"; }
warn() { printf '%s\n' "${C_WARN}  !${C_RESET} $*" >&2; }
die()  { printf '%s\n' "${C_ERR}  ✗ $*${C_RESET}" >&2; exit 1; }

print_banner() {
    printf '%s' "${C_RED}"
    cat <<'BANNER_PKH'
██████╗ ██╗  ██╗██╗  ██╗███████╗██╗  ██╗
██╔══██╗██║ ██╔╝██║  ██║██╔════╝╚██╗██╔╝
██████╔╝█████╔╝ ███████║█████╗   ╚███╔╝     █████╗
██╔═══╝ ██╔═██╗ ██╔══██║██╔══╝   ██╔██╗     ╚════╝
██║     ██║  ██╗██║  ██║███████╗██╔╝ ██╗
╚═╝     ╚═╝  ╚═╝╚═╝  ╚═╝╚══════╝╚═╝  ╚═╝
BANNER_PKH
    printf '%s' "${C_RESET}${C_WHITE}"
    cat <<'BANNER_AVALONIA'

 █████╗ ██╗   ██╗ █████╗ ██╗      ██████╗ ███╗   ██╗██╗ █████╗
██╔══██╗██║   ██║██╔══██╗██║     ██╔═══██╗████╗  ██║██║██╔══██╗
███████║██║   ██║███████║██║     ██║   ██║██╔██╗ ██║██║███████║
██╔══██║╚██╗ ██╔╝██╔══██║██║     ██║   ██║██║╚██╗██║██║██╔══██║
██║  ██║ ╚████╔╝ ██║  ██║███████╗╚██████╔╝██║ ╚████║██║██║  ██║
╚═╝  ╚═╝  ╚═══╝  ╚═╝  ╚═╝╚══════╝ ╚═════╝ ╚═╝  ╚═══╝╚═╝╚═╝  ╚═╝
BANNER_AVALONIA
    printf '%s\n' "${C_RESET}"
    printf '%s\n' "${C_DIM}    Pokémon save file editor for Linux. User-local install, no root required.${C_RESET}"
    printf '\n'
}

print_summary() {
    local version="$1"
    printf '\n'
    printf '%s\n' "${C_WHITE}  PKHeX-Avalonia ${version} installed${C_RESET}"
    printf '%s\n' "${C_DIM}  ─────────────────────────────────────────────${C_RESET}"
    printf '%s\n' "    ${C_BOLD}App        ${C_RESET}${INSTALL_PATH}"
    printf '%s\n' "    ${C_BOLD}Menu entry ${C_RESET}${DESKTOP_FILE}"
    printf '%s\n' ""
    printf '%s\n' "  Launch it from your application menu, or run the AppImage directly."
    printf '%s\n' "  The in-app updater keeps this copy updated in place."
    printf '%s\n' "  ${C_DIM}Uninstall any time: re-run this script with${C_RESET} ${C_ACCENT}--uninstall${C_RESET}"
}

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

Usage:
  curl -fsSL https://raw.githubusercontent.com/realgarit/PKHeX-Avalonia/main/Scripts/install.sh | bash
  curl -fsSL https://raw.githubusercontent.com/realgarit/PKHeX-Avalonia/main/Scripts/install.sh | bash -s -- --uninstall

Options:
  --version <x.y.z>   install a specific release instead of the latest
  --uninstall         remove the installed app, menu entry, and icon
                      (save files and app data under ~/.local/share/PKHeX-Avalonia are kept)
  -h, --help          show this help

Set NO_COLOR=1 to disable the colored output.
HELP
            exit 0
            ;;
        *) echo "Unknown option: $1 (try --help)" >&2; exit 2 ;;
    esac
done

command -v curl >/dev/null 2>&1 || die "curl is required (pacman/apt/dnf install curl)"

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
    print_banner
    info "Uninstalling PKHeX-Avalonia (user-local files only)…"
    REMOVED=0
    for target in "$DESKTOP_FILE" "$INSTALL_PATH" "$ICON_FILE"; do
        if [ -e "$target" ]; then
            rm -f "$target"
            ok "Removed $(basename "$target")"
            REMOVED=$((REMOVED + 1))
        fi
    done
    rmdir "$OPT_DIR" 2>/dev/null || true
    if [ "$REMOVED" -eq 0 ]; then
        ok "Nothing to uninstall. PKHeX-Avalonia is not installed for this user."
        exit 0
    fi
    refresh_databases
    printf '%s\n' ""
    ok "PKHeX-Avalonia removed from the application menu."
    printf '%s\n' "  Your save files and app data (${DATA_HOME}/PKHeX-Avalonia) are untouched."
    exit 0
fi

print_banner
info "Resolving release…"

# --- resolve the release ---------------------------------------------------------------
RELEASE_JSON="$(mktemp)"
trap 'rm -f "$RELEASE_JSON"' EXIT

if [ -n "$VERSION" ]; then
    RELEASE_TAG="v$VERSION"
    if ! curl -fsSL --retry 3 "https://api.github.com/repos/$REPO/releases/tags/$RELEASE_TAG" -o "$RELEASE_JSON"; then
        die "Could not find release $RELEASE_TAG on $REPO."
    fi
    ok "Pinned release: ${C_BOLD}${RELEASE_TAG}${C_RESET}"
else
    if ! curl -fsSL --retry 3 "https://api.github.com/repos/$REPO/releases/latest" -o "$RELEASE_JSON"; then
        echo "Could not reach the GitHub API (offline, or rate-limited — retry in a bit," >&2
        echo "or pin a version with: install.sh --version 1.87.4)" >&2
        exit 1
    fi
    RELEASE_TAG="$(sed -n 's/.*"tag_name": *"\([^"]*\)".*/\1/p' "$RELEASE_JSON" | head -n1)"
    VERSION="${RELEASE_TAG#v}"
    ok "Latest release: ${C_BOLD}${RELEASE_TAG}${C_RESET}"
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

info "Downloading $ASSET …"
curl -fL --retry 3 -o "$STAGING" "$DOWNLOAD_URL"

if [ -n "$DIGEST" ] && command -v sha256sum >/dev/null 2>&1; then
    info "Verifying SHA-256 checksum…"
    echo "${DIGEST}  ${STAGING}" | sha256sum --check --strict - || {
        die "Checksum mismatch — the download is corrupt or was tampered with. Aborting."
    }
    ok "Checksum verified"
elif [ -z "$DIGEST" ]; then
    warn "Release has no published checksum; skipping verification."
fi

# --- install ---------------------------------------------------------------------------
info "Installing to $OPT_DIR …"
mkdir -p "$OPT_DIR" "$APPS_DIR" "$ICON_DIR"
mv -f "$STAGING" "$INSTALL_PATH"
chmod 755 "$INSTALL_PATH"

# The icon ships in the repository next to the app; prefer the tagged copy, fall back to main.
ICON_URL_BASE="https://raw.githubusercontent.com/$REPO"
if ! curl -fsSL --retry 2 -o "$ICON_FILE" "$ICON_URL_BASE/v${VERSION}/PKHeX.Avalonia/Assets/Icons/icon.png"; then
    if ! curl -fsSL --retry 2 -o "$ICON_FILE" "$ICON_URL_BASE/main/PKHeX.Avalonia/Assets/Icons/icon.png"; then
        warn "Could not download the icon; the menu entry will use a generic icon."
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
ok "Menu entry registered (${DESKTOP_ID})"

print_summary "$VERSION"
