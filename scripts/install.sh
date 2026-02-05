#!/usr/bin/env sh
# Download and install dnsmasq-webui from a GitHub release. Picks the binary
# matching the current OS/arch (RID). Run from a clone or via:
#   curl -sSL https://raw.githubusercontent.com/alexhopeoconnor/dnsmasq-webui/main/scripts/install.sh | sh
# When run from a clone, repo is detected from git remote origin.
set -e

GITHUB_REPO="${GITHUB_REPO:-}"
REPO_DEFAULT="${REPO_DEFAULT:-alexhopeoconnor/dnsmasq-webui}"

RELEASE_TAG=""
LIST=false
INSTALL_DIR=""
SYSTEM_INSTALL=false
SERVICE=false
UNINSTALL=false
PURGE=false
UPDATE=false
BUILD_FROM_SOURCE=false

# Config to apply at install: env vars DNSMASQ_WEBUI_* and --set KEY=VALUE.
# CONFIG_SET_FILE: --set args appended during parse. CONFIG_VARS_FILE: built after parse (env first, then --set).
CONFIG_SET_FILE=""
CONFIG_VARS_FILE=""
# Create temp files for config; we'll create CONFIG_VARS_FILE after parsing.
CONFIG_SET_FILE="$(mktemp)"
trap 'rm -f "$CONFIG_SET_FILE" "$CONFIG_VARS_FILE"' EXIT

# Default install dir (user-writable, no sudo). Overridden by --dir or --system.
default_install_dir() {
  echo "${HOME}/.local/share/dnsmasq-webui"
}

usage() {
  echo "Usage: $0 [OPTIONS]"
  echo ""
  echo "Download and install dnsmasq-webui from a GitHub release for this machine's OS/arch."
  echo "Supports install, update, and switch-release (re-run with same --dir and different --version)."
  echo ""
  echo "Repo (when not in a clone):"
  echo "  --repo OWNER/REPO   GitHub owner/repo. Or set GITHUB_REPO. Or set REPO_DEFAULT in the script."
  echo "                      When run inside a git clone with origin, repo is detected automatically."
  echo ""
  echo "Release:"
  echo "  --list             List available releases (tag, name, published_at) and exit."
  echo "  --version TAG      Install from release TAG (e.g. v1.0.0). Default: latest."
  echo "  --update           Reinstall latest into the default user directory (~/.local/share/dnsmasq-webui)."
  echo "  --build-from-source  Build from source instead of downloading (requires .NET SDK and a git clone; not supported when run via curl | sh). Auto-detects RID for your machine. Use if the prebuilt binary fails to start (e.g. TypeLoadException)."
  echo "  -h, -?, --help     Show this help."
  echo ""
  echo "Install location:"
  echo "  (default)         Install to ~/.local/share/dnsmasq-webui; create ~/.local/bin/dnsmasq-webui symlink if possible."
  echo "  --dir DIR          Install into DIR instead."
  echo "  --system           Install to /opt/dnsmasq-webui and symlink /usr/local/bin/dnsmasq-webui. Requires root (run with sudo)."
  echo ""
  echo "Service (systemd):"
  echo "  --service          Install a systemd unit so the app can run as a service (start on boot)."
  echo "                     With --system: installs system unit (requires root). Without: installs user unit (no sudo)."
  echo "                     If you already have the other type (user vs system), it is removed first."
  echo "                     Only supported on systemd-based systems; errors if systemd is not available."
  echo ""
  echo "Config (applied at install for service or written to install dir for manual run):"
  echo "  --set KEY=VALUE    Set an env var for the app (e.g. Application__ApplicationTitle=Tree DNS, ASPNETCORE_URLS=http://0.0.0.0:8080)."
  echo "                     Multiple --set allowed. With sudo, use --set (env vars are not passed to the script by default)."
  echo "  Env: DNSMASQ_WEBUI_*  Any env var starting with DNSMASQ_WEBUI_ is passed through (e.g. DNSMASQ_WEBUI_Application__ApplicationTitle=Tree DNS)."
  echo "                     For system install with sudo, prefer --set or run with sudo -E to preserve env."
  echo ""
  echo "Uninstall:"
  echo "  --uninstall        Remove symlinks and systemd units (user and, if root, system). Does not remove install directory."
  echo "  --purge            With --uninstall: also remove the install directory (use --dir or --system to specify which)."
  echo "                     E.g. $0 --uninstall --purge   # remove default user dir"
  echo "                     sudo $0 --uninstall --purge --system   # also remove /opt/dnsmasq-webui"
  echo ""
  echo "Examples:"
  echo "  $0                          # Install latest (clone: auto repo; curl: use REPO_DEFAULT or --repo)"
  echo "  $0 --update                  # Reinstall latest to default dir (upgrade)"
  echo "  $0 --version v1.0.0         # Install v1.0.0 to default dir"
  echo "  $0 --dir /opt/dnsmasq-webui  # Install to custom dir"
  echo "  sudo $0 --system             # System-wide install to /opt, runnable as dnsmasq-webui"
  echo "  $0 --service                 # Install + user systemd service (starts when you log in)"
  echo "  sudo $0 --system --service   # Install + system systemd service (starts at boot)"
  echo "  sudo $0 --system --service --set Application__ApplicationTitle=Tree\ DNS --set ASPNETCORE_URLS=http://0.0.0.0:8080"
  echo "  $0 --uninstall               # Remove services and symlinks only"
  echo "  $0 --uninstall --purge       # Remove services, symlinks, and default install dir"
  echo "  sudo $0 --uninstall --purge --system   # Also remove /opt/dnsmasq-webui"
  echo "  $0 --list                   # List releases"
  echo "  $0 --build-from-source      # Build locally (if prebuilt binary fails on your system)"
  echo ""
  echo "After install, configure via appsettings.json or Dnsmasq__* env vars (or use --set / DNSMASQ_WEBUI_* at install). If you used --service, enable/start with systemctl. If the app fails to start with TypeLoadException or a glibc error, try --build-from-source (requires .NET SDK)."
  exit 0
}

# Trim whitespace and carriage return (e.g. from script downloaded on Windows or with CRLF).
trim_repo() {
  echo "$1" | tr -d '\r' | sed 's/^[[:space:]]*//; s/[[:space:]]*$//'
}

# Build CONFIG_VARS_FILE from env (DNSMASQ_WEBUI_*) then --set lines. Call before do_install when installing.
# Result: key=value per line (env first, then --set so --set overrides). Safe for values with = in them.
collect_config() {
  CONFIG_VARS_FILE="$(mktemp)"
  export CONFIG_VARS_FILE
  # Env vars: strip DNSMASQ_WEBUI_ prefix; key=value per line. set +e so grep (no match) / read (EOF) / [ -f ] do not trigger exit.
  set +e
  env | grep '^DNSMASQ_WEBUI_' > "${CONFIG_VARS_FILE}.env" 2>/dev/null
  if [ -f "${CONFIG_VARS_FILE}.env" ] && [ -s "${CONFIG_VARS_FILE}.env" ]; then
    while IFS= read -r line; do
      key="${line%%=*}"
      key="${key#DNSMASQ_WEBUI_}"
      value="${line#*=}"
      printf '%s=%s\n' "$key" "$value" >> "$CONFIG_VARS_FILE"
    done < "${CONFIG_VARS_FILE}.env"
  fi
  rm -f "${CONFIG_VARS_FILE}.env"
  set -e
  # --set lines (last occurrence of each key wins when systemd reads the file)
  [ -s "$CONFIG_SET_FILE" ] && cat "$CONFIG_SET_FILE" >> "$CONFIG_VARS_FILE" || true
}

# Write env file for the app/service. $1 = path. Adds ASPNETCORE_URLS=http://0.0.0.0:8080 if not in config (for service).
# Reads CONFIG_VARS_FILE. Values are escaped for env file (double-quote wrapped, internal " escaped).
write_env_file() {
  local path default_urls
  path="$1"
  default_urls="${2:-}"   # optional: e.g. "ASPNETCORE_URLS=http://0.0.0.0:8080" to add when missing
  if [ ! -s "$CONFIG_VARS_FILE" ] && [ -z "$default_urls" ]; then
    return
  fi
  : > "$path"
  if [ -n "$default_urls" ]; then
    # Add default only if not already in config
    if ! grep -q '^ASPNETCORE_URLS=' "$CONFIG_VARS_FILE" 2>/dev/null; then
      echo "$default_urls" >> "$path"
    fi
  fi
  if [ -s "$CONFIG_VARS_FILE" ]; then
    while IFS= read -r line; do
      [ -z "$line" ] && continue
      key="${line%%=*}"
      value="${line#*=}"
      # Escape double quotes in value and wrap in double quotes for env file
      value="$(printf '%s' "$value" | sed 's/"/\\"/g')"
      echo "${key}=\"${value}\"" >> "$path"
    done < "$CONFIG_VARS_FILE"
  fi
}

# Detect owner/repo from git remote, REPO_DEFAULT, or require --repo/GITHUB_REPO.
detect_repo() {
  if [ -n "$GITHUB_REPO" ]; then
    GITHUB_REPO="$(trim_repo "$GITHUB_REPO")"
    return
  fi
  if command -v git >/dev/null 2>&1 && git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
    origin="$(git remote get-url origin 2>/dev/null)" || true
    if [ -n "$origin" ]; then
      GITHUB_REPO="$(echo "$origin" | sed -E 's|^https://github\.com/||; s|^git@github\.com:||; s|\.git$||; s|/$||')"
      GITHUB_REPO="$(trim_repo "$GITHUB_REPO")"
      if [ -n "$GITHUB_REPO" ]; then
        return
      fi
    fi
  fi
  if [ -n "$REPO_DEFAULT" ]; then
    GITHUB_REPO="$(trim_repo "$REPO_DEFAULT")"
    return
  fi
  echo "Error: GitHub repo not set. Use --repo owner/repo, set GITHUB_REPO, or run from a clone. For the one-liner, set REPO_DEFAULT in the script." >&2
  exit 1
}

# Require jq for parsing GitHub API JSON.
check_jq() {
  if command -v jq >/dev/null 2>&1; then
    return
  fi
  echo "Error: jq is required to parse GitHub API responses. Install jq (e.g. apt install jq)." >&2
  exit 1
}

# Detect RID (same logic as publish-self-contained.sh).
detect_arch() {
  case "$(uname -m)" in
    x86_64|amd64) echo "x64" ;;
    aarch64|arm64) echo "arm64" ;;
    armv7l|armhf) echo "arm" ;;
    *) echo "x64" ;;
  esac
}

# Map to portable RIDs only (linux-x64, linux-arm64, linux-musl-*). Ubuntu and other
# glibc distros use linux-x64/linux-arm64; Alpine uses linux-musl-*. Used for downloading
# release assets (CI builds portable RIDs only).
detect_rid() {
  local arch
  arch="$(detect_arch)"
  if [ -f /etc/os-release ]; then
    . /etc/os-release
    case "${ID:-}" in
      alpine)
        case "$arch" in
          x64) echo "linux-musl-x64"; return ;;
          arm64) echo "linux-musl-arm64"; return ;;
          *) echo "linux-musl-x64"; return ;;
        esac ;;
    esac
  fi
  case "$arch" in
    arm64) echo "linux-arm64" ;;
    arm) echo "linux-arm" ;;
    *) echo "linux-x64" ;;
  esac
}

# Print detected environment (OS, arch) for user feedback.
print_env_detection() {
  local rid
  rid="$1"
  if [ -f /etc/os-release ]; then
    . /etc/os-release
    echo "Detected: ${ID:-unknown} ${VERSION_ID:-}, $(uname -m) -> RID $rid"
  else
    echo "Detected: $(uname -m) -> RID $rid"
  fi
}

# GET release (latest or by tag). Output raw JSON to stdout.
# Use </dev/null so curl does not consume stdin (script when run via curl | sh).
fetch_release() {
  local api_url
  if [ -z "$RELEASE_TAG" ] || [ "$RELEASE_TAG" = "latest" ]; then
    api_url="https://api.github.com/repos/$GITHUB_REPO/releases/latest"
  else
    api_url="https://api.github.com/repos/$GITHUB_REPO/releases/tags/$RELEASE_TAG"
  fi
  resp="$(curl -sSL -A "dnsmasq-webui-install/1.0" -w "\n%{http_code}" "$api_url" </dev/null)"
  code="$(echo "$resp" | tail -n1)"
  body="$(echo "$resp" | sed '$d')"
  if [ "$code" != "200" ]; then
    echo "Error: GitHub API returned $code for $api_url" >&2
    body="$(printf '%s' "$body" | tr -d '\000-\011\013\014\016-\037')"
    echo "$body" | jq -r '.message // .' 2>/dev/null || echo "$body" >&2
    exit 1
  fi
  # GitHub API can return release body with unescaped control chars; jq rejects them. Strip control chars (keep \n \r).
  body="$(printf '%s' "$body" | tr -d '\000-\011\013\014\016-\037')"
  echo "$body"
}

# List releases and exit.
list_releases() {
  check_jq
  detect_repo
  api_url="https://api.github.com/repos/$GITHUB_REPO/releases?per_page=20"
  resp="$(curl -sSL -A "dnsmasq-webui-install/1.0" -w "\n%{http_code}" "$api_url" </dev/null)"
  code="$(echo "$resp" | tail -n1)"
  body="$(echo "$resp" | sed '$d')"
  if [ "$code" != "200" ]; then
    echo "Error: GitHub API returned $code" >&2
    exit 1
  fi
  body="$(printf '%s' "$body" | tr -d '\000-\011\013\014\016-\037')"
  echo "$body" | jq -r '.[] | "\(.tag_name)  \(.name)  \(.published_at // .created_at)"'
  exit 0
}

# Find asset download URL whose name contains the given RID.
find_asset_url() {
  local release_json rid
  release_json="$1"
  rid="$2"
  echo "$release_json" | jq -r --arg rid "$rid" '
    .assets[] | select(.name | test($rid)) | .browser_download_url
  ' | head -n1
}

# Remove user systemd unit (stop, disable, rm file). No root. Safe if not present.
remove_user_service() {
  if ! command -v systemctl >/dev/null 2>&1; then
    return 0
  fi
  systemctl --user stop dnsmasq-webui.service 2>/dev/null || true
  systemctl --user disable dnsmasq-webui.service 2>/dev/null || true
  rm -f "${HOME:-}/.config/systemd/user/dnsmasq-webui.service" 2>/dev/null || true
  systemctl --user daemon-reload 2>/dev/null || true
}

# Remove system systemd unit (stop, disable, rm file). Requires root. Safe if not present.
remove_system_service() {
  if [ "$(id -u)" -ne 0 ]; then
    return 0
  fi
  if ! command -v systemctl >/dev/null 2>&1; then
    return 0
  fi
  systemctl stop dnsmasq-webui.service 2>/dev/null || true
  systemctl disable dnsmasq-webui.service 2>/dev/null || true
  rm -f /etc/systemd/system/dnsmasq-webui.service 2>/dev/null || true
  systemctl daemon-reload 2>/dev/null || true
}

# Remove user service for a specific user (by name). Call as root with SUDO_USER.
# Used when installing system service so the invoking user doesn't keep a user unit.
remove_user_service_for() {
  local u
  u="$1"
  [ -n "$u" ] || return 0
  if [ "$(id -u)" -ne 0 ]; then
    return 0
  fi
  # Run as that user to stop/disable and remove their user unit
  su "$u" -c 'systemctl --user stop dnsmasq-webui.service 2>/dev/null; systemctl --user disable dnsmasq-webui.service 2>/dev/null; rm -f ~/.config/systemd/user/dnsmasq-webui.service; systemctl --user daemon-reload 2>/dev/null' 2>/dev/null || true
}

# Uninstall: remove services, symlinks, optionally purge install dir(s).
do_uninstall() {
  echo "Uninstalling dnsmasq-webui..."
  # Remove user service (current user)
  remove_user_service
  # Remove system service (if root)
  remove_system_service
  # Remove symlinks
  if [ -n "${HOME:-}" ] && [ -L "${HOME}/.local/bin/dnsmasq-webui" ]; then
    rm -f "${HOME}/.local/bin/dnsmasq-webui"
    echo "Removed symlink ~/.local/bin/dnsmasq-webui"
  fi
  if [ "$(id -u)" -eq 0 ] && [ -L /usr/local/bin/dnsmasq-webui ]; then
    rm -f /usr/local/bin/dnsmasq-webui
    echo "Removed symlink /usr/local/bin/dnsmasq-webui"
  fi
  if [ "$PURGE" = true ]; then
    if [ -n "$INSTALL_DIR" ]; then
      if [ -d "$INSTALL_DIR" ]; then
        rm -rf "$INSTALL_DIR"
        echo "Removed directory $INSTALL_DIR"
      else
        echo "Directory $INSTALL_DIR not found."
      fi
    elif [ "$SYSTEM_INSTALL" = true ] && [ "$(id -u)" -eq 0 ]; then
      if [ -d /opt/dnsmasq-webui ]; then
        rm -rf /opt/dnsmasq-webui
        echo "Removed directory /opt/dnsmasq-webui"
      else
        echo "Directory /opt/dnsmasq-webui not found."
      fi
    else
      local default_dir
      default_dir="$(default_install_dir)"
      if [ -d "$default_dir" ]; then
        rm -rf "$default_dir"
        echo "Removed directory $default_dir"
      else
        echo "Directory $default_dir not found."
      fi
    fi
  else
    echo "Services and symlinks removed. To also remove the install directory, run with --purge (and --dir or --system if needed)."
  fi
  echo "Uninstall complete."
  exit 0
}

# Build from source and install to INSTALL_DIR. Requires .NET SDK and running from a git clone.
do_install_from_source() {
  local script_dir repo_root publish_output rid publish_dir
  script_dir="$(cd "$(dirname "$0")" && pwd)"
  repo_root="$(cd "$script_dir/.." && pwd)"

  if [ ! -f "$repo_root/scripts/publish-self-contained.sh" ]; then
    echo "Error: --build-from-source requires a git clone (run from the repo directory). Not found: $repo_root/scripts/publish-self-contained.sh" >&2
    echo "Do not use --build-from-source when installing via 'curl ... | sh'; clone the repo first, then run ./scripts/install.sh --build-from-source" >&2
    exit 1
  fi
  if ! command -v dotnet >/dev/null 2>&1; then
    echo "Error: --build-from-source requires the .NET SDK. Install from https://dotnet.microsoft.com/download or your distro (e.g. apt install dotnet-sdk-9.0)." >&2
    exit 1
  fi

  if [ "$SYSTEM_INSTALL" = true ]; then
    if [ "$(id -u)" -ne 0 ]; then
      echo "Error: --system installs to /opt and requires root. Run with sudo: sudo $0 --system --build-from-source" >&2
      exit 1
    fi
    INSTALL_DIR="/opt/dnsmasq-webui"
  elif [ -z "$INSTALL_DIR" ]; then
    INSTALL_DIR="$(default_install_dir)"
  fi

  echo "Building from source (auto-detecting RID for your machine)..."
  publish_output="$(cd "$repo_root" && ./scripts/publish-self-contained.sh 2>&1)" || exit $?
  echo "$publish_output"
  rid="$(echo "$publish_output" | sed -n 's/^Detected RID: \([^ ]*\).*/\1/p')"
  if [ -z "$rid" ]; then
    rid="$(echo "$publish_output" | sed -n 's/^Publishing self-contained.* for \([^.]*\)\.\.\./\1/p')"
  fi
  if [ -z "$rid" ]; then
    echo "Error: Could not determine RID from build output." >&2
    exit 1
  fi
  publish_dir="$repo_root/src/DnsmasqWebUI/bin/Release/net9.0/$rid/publish"
  if [ ! -d "$publish_dir" ] || [ ! -f "$publish_dir/DnsmasqWebUI" ]; then
    echo "Error: Build output not found at $publish_dir" >&2
    exit 1
  fi

  echo "Installing to $INSTALL_DIR ..."
  mkdir -p "$INSTALL_DIR"
  cp -a "$publish_dir"/* "$INSTALL_DIR/"
  ln -sf DnsmasqWebUI "$INSTALL_DIR/dnsmasq-webui" 2>/dev/null || true

  if [ "$SYSTEM_INSTALL" = true ]; then
    ln -sf "$INSTALL_DIR/dnsmasq-webui" /usr/local/bin/dnsmasq-webui 2>/dev/null || true
    echo ""
    echo "Installed to $INSTALL_DIR (system-wide)"
    echo "Run: dnsmasq-webui   (or $INSTALL_DIR/dnsmasq-webui)"
  else
    LOCAL_BIN="${HOME:-}/.local/bin"
    if [ -n "${HOME:-}" ] && [ -d "$(dirname "$LOCAL_BIN")" ]; then
      mkdir -p "$LOCAL_BIN"
      if [ -w "$LOCAL_BIN" ]; then
        ln -sf "$INSTALL_DIR/dnsmasq-webui" "$LOCAL_BIN/dnsmasq-webui" 2>/dev/null && echo "Symlink: $LOCAL_BIN/dnsmasq-webui -> $INSTALL_DIR/dnsmasq-webui" || true
      fi
    fi
    echo ""
    echo "Installed to $INSTALL_DIR"
    echo "Run: $INSTALL_DIR/dnsmasq-webui"
    if [ -f "$LOCAL_BIN/dnsmasq-webui" ] 2>/dev/null; then
      echo "  or: dnsmasq-webui   (if ~/.local/bin is in your PATH)"
    fi
  fi

  if [ "$SERVICE" = true ]; then
    if [ "$SYSTEM_INSTALL" = true ] && [ "$(id -u)" -ne 0 ]; then
      echo "Error: Installing a system service (--service with --system) requires root." >&2
      exit 1
    fi
    if ! command -v systemctl >/dev/null 2>&1; then
      echo "Error: systemd not found. --service is only supported on systemd-based systems." >&2
      exit 1
    fi
    install_systemd_unit "$INSTALL_DIR"
  fi

  echo "Configure via appsettings.json in that directory or Dnsmasq__* environment variables (e.g. Dnsmasq__MainConfigPath=/etc/dnsmasq.conf)."
}

# Main install: fetch release, find asset for RID, download, extract, optionally symlink.
do_install() {
  if [ "$BUILD_FROM_SOURCE" = true ]; then
    do_install_from_source
    return
  fi

  echo "Installing dnsmasq-webui..."
  check_jq
  detect_repo

  if [ "$SYSTEM_INSTALL" = true ]; then
    if [ "$(id -u)" -ne 0 ]; then
      echo "Error: --system installs to /opt and requires root. Run with sudo: sudo $0 --system" >&2
      exit 1
    fi
    INSTALL_DIR="/opt/dnsmasq-webui"
  elif [ -z "$INSTALL_DIR" ]; then
    INSTALL_DIR="$(default_install_dir)"
  fi

  if [ "$SERVICE" = true ]; then
    if [ "$SYSTEM_INSTALL" = true ] && [ "$(id -u)" -ne 0 ]; then
      echo "Error: Installing a system service (--service with --system) requires root. Run with sudo: sudo $0 --system --service" >&2
      exit 1
    fi
    if ! command -v systemctl >/dev/null 2>&1; then
      echo "Error: systemd not found (no systemctl). --service is only supported on systemd-based systems." >&2
      exit 1
    fi
  fi

  rid="$(detect_rid)"
  print_env_detection "$rid"
  echo "Fetching release..."
  release_json="$(fetch_release)"
  tag="$(echo "$release_json" | jq -r '.tag_name')"
  echo "Release: $tag"
  url="$(find_asset_url "$release_json" "$rid" | tr -d '\r' | sed 's/^[[:space:]]*//; s/[[:space:]]*$//')"
  if [ -z "$url" ] || [ "$url" = "null" ]; then
    echo "Error: No asset found for RID $rid in release $tag." >&2
    echo "Available assets:" >&2
    echo "$release_json" | jq -r '.assets[].name' | sed 's/^/  /' >&2
    exit 1
  fi

  mkdir -p "$INSTALL_DIR"
  tmpzip="${TMPDIR:-/tmp}/dnsmasq-webui-$rid.zip"
  echo "Downloading $url ..."
  curl -sSL -A "dnsmasq-webui-install/1.0" -o "$tmpzip" "$url" </dev/null
  echo "Extracting to $INSTALL_DIR ..."
  unzip -o -q "$tmpzip" -d "$INSTALL_DIR"
  rm -f "$tmpzip"
  ln -sf DnsmasqWebUI "$INSTALL_DIR/dnsmasq-webui" 2>/dev/null || true

  if [ "$SYSTEM_INSTALL" = true ]; then
    ln -sf "$INSTALL_DIR/dnsmasq-webui" /usr/local/bin/dnsmasq-webui 2>/dev/null || true
    echo ""
    echo "Installed to $INSTALL_DIR (system-wide)"
    echo "Run: dnsmasq-webui   (or $INSTALL_DIR/dnsmasq-webui)"
  else
    # User install: create ~/.local/bin symlink so `dnsmasq-webui` works if ~/.local/bin is in PATH
    LOCAL_BIN="${HOME:-}/.local/bin"
    if [ -n "${HOME:-}" ] && [ -d "$(dirname "$LOCAL_BIN")" ]; then
      mkdir -p "$LOCAL_BIN"
      if [ -w "$LOCAL_BIN" ]; then
        ln -sf "$INSTALL_DIR/dnsmasq-webui" "$LOCAL_BIN/dnsmasq-webui" 2>/dev/null && echo "Symlink: $LOCAL_BIN/dnsmasq-webui -> $INSTALL_DIR/dnsmasq-webui" || true
      fi
    fi
    echo ""
    echo "Installed to $INSTALL_DIR"
    echo "Run: $INSTALL_DIR/dnsmasq-webui"
    if [ -f "$LOCAL_BIN/dnsmasq-webui" ] 2>/dev/null; then
      echo "  or: dnsmasq-webui   (if ~/.local/bin is in your PATH)"
    fi
  fi

  if [ "$SERVICE" = true ]; then
    install_systemd_unit "$INSTALL_DIR"
  elif [ -s "$CONFIG_VARS_FILE" ]; then
    write_env_file "$INSTALL_DIR/dnsmasq-webui.env" "ASPNETCORE_URLS=http://0.0.0.0:8080"
    echo "Wrote config: $INSTALL_DIR/dnsmasq-webui.env (use when running manually: set -a && . $INSTALL_DIR/dnsmasq-webui.env && set +a && $INSTALL_DIR/dnsmasq-webui)"
  fi

  echo "Configure via appsettings.json or Dnsmasq__* env vars (or use --set / DNSMASQ_WEBUI_* at install)."
  echo "If the app fails to start with TypeLoadException or a glibc error, try: $0 --build-from-source (requires .NET SDK)."
}

# Install systemd unit. Call only when SERVICE=true and systemctl exists.
# Removes the other service type first (user vs system) so switching works cleanly.
# $1 = INSTALL_DIR. Uses CONFIG_VARS_FILE to write env file; default ASPNETCORE_URLS=http://0.0.0.0:8080.
install_systemd_unit() {
  local dir bin env_file
  dir="$1"
  if [ -f "$dir/dnsmasq-webui" ]; then
    bin="$dir/dnsmasq-webui"
  elif [ -f "$dir/DnsmasqWebUI" ]; then
    bin="$dir/DnsmasqWebUI"
  else
    echo "Warning: No binary found in $dir; skipping systemd unit install." >&2
    return 0
  fi
  if [ "$SYSTEM_INSTALL" = true ]; then
    # Installing system service: remove existing system unit (idempotent), then remove user unit for whoever ran sudo so they don't have both
    remove_system_service
    if [ -n "${SUDO_USER:-}" ]; then
      remove_user_service_for "$SUDO_USER"
    fi
    env_file="/etc/default/dnsmasq-webui"
    # Only write when we have new config so update preserves existing /etc/default/dnsmasq-webui
    if [ -s "$CONFIG_VARS_FILE" ]; then
      write_env_file "$env_file" ""
      echo "Wrote config: $env_file"
    fi
    cat > /etc/systemd/system/dnsmasq-webui.service << EOF
[Unit]
Description=dnsmasq-webui - Web UI for dnsmasq
After=network-online.target
Wants=network-online.target

[Service]
Type=exec
WorkingDirectory=$dir
ExecStart=$bin
Environment=ASPNETCORE_URLS=http://0.0.0.0:8080
EnvironmentFile=-$env_file
Restart=on-failure
RestartSec=5

[Install]
WantedBy=multi-user.target
EOF
    systemctl daemon-reload
    systemctl enable dnsmasq-webui.service
    echo ""
    echo "System service installed: /etc/systemd/system/dnsmasq-webui.service"
    echo "  sudo systemctl start dnsmasq-webui   # start now"
    echo "  sudo systemctl enable dnsmasq-webui  # already enabled for boot"
    echo "  sudo systemctl status dnsmasq-webui  # check status"
  else
    # Installing user service: remove existing user unit (idempotent)
    remove_user_service
    # We cannot remove system service without root; remind if they might have had one
    if [ -f /etc/systemd/system/dnsmasq-webui.service ] 2>/dev/null; then
      echo "Note: A system-wide service also exists. To remove it and use only this user service: sudo $0 --uninstall  (then re-run without --system --service)." >&2
    fi
    env_file="$dir/dnsmasq-webui.env"
    # Only write when we have new config or file doesn't exist (first install), so update preserves existing env
    if [ -s "$CONFIG_VARS_FILE" ] || [ ! -f "$env_file" ]; then
      write_env_file "$env_file" "ASPNETCORE_URLS=http://0.0.0.0:8080"
      echo "Wrote config: $env_file"
    fi
    mkdir -p "${HOME:-}/.config/systemd/user"
    cat > "${HOME:-}/.config/systemd/user/dnsmasq-webui.service" << EOF
[Unit]
Description=dnsmasq-webui - Web UI for dnsmasq
After=network-online.target
Wants=network-online.target

[Service]
Type=exec
WorkingDirectory=$dir
ExecStart=$bin
EnvironmentFile=$env_file
Restart=on-failure
RestartSec=5

[Install]
WantedBy=default.target
EOF
    systemctl --user daemon-reload
    systemctl --user enable dnsmasq-webui.service
    echo ""
    echo "User service installed: ~/.config/systemd/user/dnsmasq-webui.service"
    echo "  systemctl --user start dnsmasq-webui   # start now"
    echo "  systemctl --user enable dnsmasq-webui   # already enabled (starts when you log in)"
    echo "  systemctl --user status dnsmasq-webui  # check status"
    echo "  To run at boot without login: loginctl enable-linger"
  fi
}

# Parse args
while [ $# -gt 0 ]; do
  case "$1" in
    -h|-?|--help)
      usage
      ;;
    --repo)
      shift
      [ $# -gt 0 ] || { echo "Error: --repo requires OWNER/REPO" >&2; exit 1; }
      GITHUB_REPO="$1"
      shift
      ;;
    --list)
      LIST=true
      shift
      ;;
    --version)
      shift
      [ $# -gt 0 ] || { echo "Error: --version requires TAG" >&2; exit 1; }
      RELEASE_TAG="$1"
      shift
      ;;
    --update)
      UPDATE=true
      shift
      ;;
    --dir)
      shift
      [ $# -gt 0 ] || { echo "Error: --dir requires DIR" >&2; exit 1; }
      INSTALL_DIR="$1"
      shift
      ;;
    --system)
      SYSTEM_INSTALL=true
      shift
      ;;
    --service)
      SERVICE=true
      shift
      ;;
    --set)
      shift
      [ $# -gt 0 ] || { echo "Error: --set requires KEY=VALUE (e.g. Application__ApplicationTitle=Tree DNS)" >&2; exit 1; }
      echo "$1" >> "$CONFIG_SET_FILE"
      shift
      ;;
    --uninstall)
      UNINSTALL=true
      shift
      ;;
    --purge)
      PURGE=true
      shift
      ;;
    --build-from-source)
      BUILD_FROM_SOURCE=true
      shift
      ;;
    -*)
      echo "Error: unknown option $1" >&2
      usage >&2
      exit 1
      ;;
    *)
      echo "Error: unexpected argument $1" >&2
      usage >&2
      exit 1
      ;;
  esac
done

# Validate option combinations to prevent mistakes
if [ "$PURGE" = true ] && [ "$UNINSTALL" != true ]; then
  echo "Error: --purge must be used with --uninstall. E.g. $0 --uninstall --purge" >&2
  exit 1
fi
  if [ "$UNINSTALL" = true ]; then
    if [ "$UPDATE" = true ] || [ -n "$RELEASE_TAG" ] || [ "$SERVICE" = true ] || [ "$BUILD_FROM_SOURCE" = true ]; then
      echo "Error: --uninstall cannot be combined with install/update options (--version, --update, --service, --build-from-source). Run uninstall alone, then install if needed." >&2
      exit 1
    fi
    if [ "$LIST" = true ]; then
      echo "Error: --uninstall cannot be combined with --list." >&2
      exit 1
    fi
  fi
  if [ "$BUILD_FROM_SOURCE" = true ]; then
    if [ "$LIST" = true ]; then
      echo "Error: --build-from-source cannot be combined with --list." >&2
      exit 1
    fi
    if [ -n "$RELEASE_TAG" ] || [ "$UPDATE" = true ]; then
      echo "Error: --build-from-source builds from current source; do not use --version or --update." >&2
      exit 1
    fi
  fi
if [ "$LIST" = true ]; then
  if [ "$UNINSTALL" = true ] || [ "$UPDATE" = true ] || [ "$SERVICE" = true ] || [ -n "$RELEASE_TAG" ]; then
    echo "Error: --list lists releases and exits; do not combine with install/uninstall options." >&2
    exit 1
  fi
  list_releases
fi

if [ "$UNINSTALL" = true ]; then
  if [ "$PURGE" = true ] && [ "$SYSTEM_INSTALL" = true ] && [ "$(id -u)" -ne 0 ]; then
    echo "Error: --uninstall --purge --system requires root. Run with sudo." >&2
    exit 1
  fi
  do_uninstall
  exit 0
fi

if [ "$LIST" = true ]; then
  exit 0
fi

if [ "$UPDATE" = true ]; then
  [ -z "$INSTALL_DIR" ] && INSTALL_DIR="$(default_install_dir)"
  RELEASE_TAG="latest"
fi

# Merge env (DNSMASQ_WEBUI_*) and --set into CONFIG_VARS_FILE for install/service
collect_config

do_install
