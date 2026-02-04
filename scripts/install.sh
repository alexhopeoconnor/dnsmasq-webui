#!/usr/bin/env sh
# Download and install dnsmasq-webui from a GitHub release. Picks the binary
# matching the current OS/arch (RID). Run from a clone or via:
#   curl -sSL https://raw.githubusercontent.com/alexhopeoconnor/dnsmasq-webui/main/scripts/install.sh | sh
# When run from a clone, repo is detected from git remote origin.
set -e

GITHUB_REPO="${GITHUB_REPO:-}"
REPO_DEFAULT="${REPO_DEFAULT:-alexhopeoconnor/dnsmasq-webui}"

VERSION=""
LIST=false
INSTALL_DIR=""
SYSTEM_INSTALL=false
SERVICE=false
UPDATE=false

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
  echo "                     Only supported on systemd-based systems; errors if systemd is not available."
  echo ""
  echo "Examples:"
  echo "  $0                          # Install latest (clone: auto repo; curl: use REPO_DEFAULT or --repo)"
  echo "  $0 --update                  # Reinstall latest to default dir (upgrade)"
  echo "  $0 --version v1.0.0         # Install v1.0.0 to default dir"
  echo "  $0 --dir /opt/dnsmasq-webui  # Install to custom dir"
  echo "  sudo $0 --system             # System-wide install to /opt, runnable as dnsmasq-webui"
  echo "  $0 --service                 # Install + user systemd service (starts when you log in)"
  echo "  sudo $0 --system --service   # Install + system systemd service (starts at boot)"
  echo "  $0 --list                   # List releases"
  echo ""
  echo "After install, configure via appsettings.json or Dnsmasq__* environment variables, then run the binary (or dnsmasq-webui if symlink created). If you used --service, enable/start with systemctl."
  exit 0
}

# Detect owner/repo from git remote, REPO_DEFAULT, or require --repo/GITHUB_REPO.
detect_repo() {
  if [ -n "$GITHUB_REPO" ]; then
    return
  fi
  if command -v git >/dev/null 2>&1 && git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
    origin="$(git remote get-url origin 2>/dev/null)" || true
    if [ -n "$origin" ]; then
      GITHUB_REPO="$(echo "$origin" | sed -E 's|^https://github\.com/||; s|^git@github\.com:||; s|\.git$||; s|/$||')"
      if [ -n "$GITHUB_REPO" ]; then
        return
      fi
    fi
  fi
  if [ -n "$REPO_DEFAULT" ]; then
    GITHUB_REPO="$REPO_DEFAULT"
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

detect_rid() {
  local arch
  arch="$(detect_arch)"
  if [ -f /etc/os-release ]; then
    . /etc/os-release
    case "${ID:-}" in
      ubuntu)
        case "${VERSION_ID:-}" in
          24.04) echo "ubuntu.24.04-$arch"; return ;;
          22.04) echo "ubuntu.22.04-$arch"; return ;;
        esac ;;
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

# GET release (latest or by tag). Output raw JSON to stdout.
fetch_release() {
  local api_url
  if [ -z "$VERSION" ] || [ "$VERSION" = "latest" ]; then
    api_url="https://api.github.com/repos/$GITHUB_REPO/releases/latest"
  else
    api_url="https://api.github.com/repos/$GITHUB_REPO/releases/tags/$VERSION"
  fi
  resp="$(curl -sSL -A "dnsmasq-webui-install/1.0" -w "\n%{http_code}" "$api_url")"
  code="$(echo "$resp" | tail -n1)"
  body="$(echo "$resp" | sed '$d')"
  if [ "$code" != "200" ]; then
    echo "Error: GitHub API returned $code for $api_url" >&2
    echo "$body" | jq -r '.message // .' 2>/dev/null || echo "$body" >&2
    exit 1
  fi
  echo "$body"
}

# List releases and exit.
list_releases() {
  check_jq
  detect_repo
  api_url="https://api.github.com/repos/$GITHUB_REPO/releases?per_page=20"
  resp="$(curl -sSL -A "dnsmasq-webui-install/1.0" -w "\n%{http_code}" "$api_url")"
  code="$(echo "$resp" | tail -n1)"
  body="$(echo "$resp" | sed '$d')"
  if [ "$code" != "200" ]; then
    echo "Error: GitHub API returned $code" >&2
    exit 1
  fi
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

# Main install: fetch release, find asset for RID, download, extract, optionally symlink.
do_install() {
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
  echo "Detected RID: $rid"
  echo "Fetching release..."
  release_json="$(fetch_release)"
  tag="$(echo "$release_json" | jq -r '.tag_name')"
  echo "Release: $tag"
  url="$(find_asset_url "$release_json" "$rid")"
  if [ -z "$url" ] || [ "$url" = "null" ]; then
    echo "Error: No asset found for RID $rid in release $tag." >&2
    echo "Available assets:" >&2
    echo "$release_json" | jq -r '.assets[].name' | sed 's/^/  /' >&2
    exit 1
  fi

  mkdir -p "$INSTALL_DIR"
  tmpzip="${TMPDIR:-/tmp}/dnsmasq-webui-$rid.zip"
  echo "Downloading $url ..."
  curl -sSL -A "dnsmasq-webui-install/1.0" -o "$tmpzip" "$url"
  echo "Extracting to $INSTALL_DIR ..."
  unzip -o -q "$tmpzip" -d "$INSTALL_DIR"
  rm -f "$tmpzip"

  if [ "$SYSTEM_INSTALL" = true ]; then
    ln -sf "$INSTALL_DIR/DnsmasqWebUI" /usr/local/bin/dnsmasq-webui 2>/dev/null || true
    echo ""
    echo "Installed to $INSTALL_DIR (system-wide)"
    echo "Run: dnsmasq-webui   (or $INSTALL_DIR/DnsmasqWebUI)"
  else
    # User install: create ~/.local/bin symlink so `dnsmasq-webui` works if ~/.local/bin is in PATH
    LOCAL_BIN="${HOME:-}/.local/bin"
    if [ -n "${HOME:-}" ] && [ -d "$(dirname "$LOCAL_BIN")" ]; then
      mkdir -p "$LOCAL_BIN"
      if [ -w "$LOCAL_BIN" ]; then
        ln -sf "$INSTALL_DIR/DnsmasqWebUI" "$LOCAL_BIN/dnsmasq-webui" 2>/dev/null && echo "Symlink: $LOCAL_BIN/dnsmasq-webui -> $INSTALL_DIR/DnsmasqWebUI" || true
      fi
    fi
    echo ""
    echo "Installed to $INSTALL_DIR"
    echo "Run: $INSTALL_DIR/DnsmasqWebUI"
    if [ -f "$LOCAL_BIN/dnsmasq-webui" ] 2>/dev/null; then
      echo "  or: dnsmasq-webui   (if ~/.local/bin is in your PATH)"
    fi
  fi

  if [ "$SERVICE" = true ]; then
    install_systemd_unit "$INSTALL_DIR"
  fi

  echo "Configure via appsettings.json in that directory or Dnsmasq__* environment variables (e.g. Dnsmasq__MainConfigPath=/etc/dnsmasq.conf)."
}

# Install systemd unit. Call only when SERVICE=true and systemctl exists.
# $1 = INSTALL_DIR (where DnsmasqWebUI binary and appsettings.json live)
install_systemd_unit() {
  local dir bin
  dir="$1"
  bin="$dir/DnsmasqWebUI"
  if [ ! -f "$bin" ]; then
    echo "Warning: Binary $bin not found; skipping systemd unit install." >&2
    return 0
  fi
  if [ "$SYSTEM_INSTALL" = true ]; then
    cat > /etc/systemd/system/dnsmasq-webui.service << EOF
[Unit]
Description=dnsmasq-webui - Web UI for dnsmasq
After=network-online.target
Wants=network-online.target

[Service]
Type=exec
WorkingDirectory=$dir
ExecStart=$bin
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
      VERSION="$1"
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

if [ "$LIST" = true ]; then
  list_releases
fi

if [ "$UPDATE" = true ]; then
  INSTALL_DIR="$(default_install_dir)"
  VERSION="latest"
fi

do_install
