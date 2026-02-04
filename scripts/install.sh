#!/usr/bin/env sh
# Download and install dnsmasq-webui from a GitHub release. Picks the binary
# matching the current OS/arch (RID). Run from a clone or via:
#   curl -sSL https://raw.githubusercontent.com/OWNER/REPO/main/scripts/install.sh | sh -s -- [OPTIONS]
# When run from a clone, repo is detected from git remote origin. Otherwise set
# GITHUB_REPO=owner/repo or pass --repo owner/repo.
set -e

GITHUB_REPO="${GITHUB_REPO:-}"
VERSION=""
LIST=false
INSTALL_DIR=""

usage() {
  echo "Usage: $0 [OPTIONS]"
  echo ""
  echo "Download and install dnsmasq-webui from a GitHub release for this machine's OS/arch."
  echo ""
  echo "Repo (when not in a clone):"
  echo "  --repo OWNER/REPO   GitHub owner/repo (e.g. myuser/dnsmasq-webui)."
  echo "                      Or set GITHUB_REPO=owner/repo."
  echo "                      When run inside a git clone with origin, repo is detected automatically."
  echo ""
  echo "Release:"
  echo "  --list             List available releases (tag, name, published_at) and exit."
  echo "  --version TAG      Install from release with tag TAG (e.g. v1.0.0). Default: latest."
  echo "  -h, -?, --help     Show this help."
  echo ""
  echo "Install:"
  echo "  --dir DIR          Install into DIR (default: ./dnsmasq-webui in current directory)."
  echo ""
  echo "Examples:"
  echo "  $0                                    # In clone: install latest into ./dnsmasq-webui"
  echo "  $0 --repo owner/dnsmasq-webui         # Install latest from owner/dnsmasq-webui"
  echo "  $0 --repo owner/dnsmasq-webui --version v1.0.0"
  echo "  $0 --repo owner/dnsmasq-webui --list  # List releases"
  echo "  GITHUB_REPO=owner/dnsmasq-webui $0 --dir /opt/dnsmasq-webui"
  echo ""
  echo "After install, configure via appsettings.json or Dnsmasq__* environment variables, then run the binary."
  exit 0
}

# Detect owner/repo from git remote if we're in a clone.
detect_repo() {
  if [ -n "$GITHUB_REPO" ]; then
    return
  fi
  if command -v git >/dev/null 2>&1 && git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
    origin="$(git remote get-url origin 2>/dev/null)" || true
    if [ -n "$origin" ]; then
      # https://github.com/owner/repo or git@github.com:owner/repo.git
      GITHUB_REPO="$(echo "$origin" | sed -E 's|^https://github\.com/||; s|^git@github\.com:||; s|\.git$||; s|/$||')"
      if [ -n "$GITHUB_REPO" ]; then
        return
      fi
    fi
  fi
  echo "Error: GitHub repo not set. Use --repo owner/repo or set GITHUB_REPO=owner/repo (or run from a clone)." >&2
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

# Main install: fetch release, find asset for RID, download, extract.
do_install() {
  check_jq
  detect_repo
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
  if [ -z "$INSTALL_DIR" ]; then
    INSTALL_DIR="./dnsmasq-webui"
  fi
  mkdir -p "$INSTALL_DIR"
  tmpzip="${TMPDIR:-/tmp}/dnsmasq-webui-$rid.zip"
  echo "Downloading $url ..."
  curl -sSL -A "dnsmasq-webui-install/1.0" -o "$tmpzip" "$url"
  echo "Extracting to $INSTALL_DIR ..."
  unzip -o -q "$tmpzip" -d "$INSTALL_DIR"
  rm -f "$tmpzip"
  echo ""
  echo "Installed to $INSTALL_DIR"
  echo "Run: $INSTALL_DIR/DnsmasqWebUI"
  echo "Configure via appsettings.json in that directory or Dnsmasq__* environment variables (e.g. Dnsmasq__MainConfigPath=/etc/dnsmasq.conf)."
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
    --dir)
      shift
      [ $# -gt 0 ] || { echo "Error: --dir requires DIR" >&2; exit 1; }
      INSTALL_DIR="$1"
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

do_install
