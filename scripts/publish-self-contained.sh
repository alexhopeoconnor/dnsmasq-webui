#!/usr/bin/env sh
# Build a self-contained folder publish for Linux (no single-file; folder is recommended
# for ASP.NET Core). Copy the publish directory to the target host and run the binary.
#
# Platform auto-detection: if you do not pass a RID, the script picks one from /etc/os-release
# and uname -m. We use generic linux-* (glibc) or linux-musl-* (Alpine). Trimming is off by default because PublishTrimmed
# can cause Blazor routing/404 issues. The script cleans the target RID before publish
# so previous artefacts (e.g. from a different --trim or RID) cannot pollute the build.
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PROJECT="$REPO_ROOT/src/DnsmasqWebUI/DnsmasqWebUI.csproj"

# Portable RIDs (built in CI). Ubuntu RIDs for local builds only (CI does not build these;
# use when .NET from distro repo so the binary runs on this machine without TypeLoadException).
VALID_RIDS="linux-x64 linux-arm64 linux-arm linux-musl-x64 linux-musl-arm64 ubuntu.24.04-x64 ubuntu.24.04-arm64 ubuntu.22.04-x64 ubuntu.22.04-arm64"
TRIM=false
CLEAN=true
RID=""
AUTO_RID=false

# Detect architecture: x86_64/amd64 -> x64, aarch64 -> arm64, armv7l/armhf -> arm
detect_arch() {
  case "$(uname -m)" in
    x86_64|amd64) echo "x64" ;;
    aarch64|arm64) echo "arm64" ;;
    armv7l|armhf) echo "arm" ;;
    *) echo "x64" ;;
  esac
}

# Pick RID from OS and arch. We only add explicit distro RIDs where we've seen
# TypeLoadException with portable linux-x64 (Ubuntu when .NET from distro). Alpine
# uses musl. Other distros (Debian, Fedora, etc.) fall through to linux-x64/linux-arm64.
default_rid() {
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

while [ $# -gt 0 ]; do
  case "$1" in
    -h|-?|--help)
      echo "Usage: $0 [OPTIONS] [RID]"
      echo ""
      echo "Build a self-contained folder publish for Linux. If RID is omitted, the script"
      echo "auto-detects from the current OS and architecture (recommended on Ubuntu and Alpine)."
      echo "Pass options first, then RID if desired (e.g. $0 --trim linux-x64)."
      echo ""
      echo "Options:"
      echo "  --trim       Enable trimming (smaller output; can cause 404/routing issues with Blazor)"
      echo "  --no-clean   Skip clean before publish (faster; use only if same RID and options as last run)"
      echo "  -h, -?, --help  Show this help"
      echo ""
      echo "Supported RIDs:"
      echo "  Generic (glibc):"
      echo "    linux-x64       Most servers/desktops (Debian, Fedora, etc.)"
      echo "    linux-arm64     Raspberry Pi 4/5, aarch64"
      echo "    linux-arm       32-bit ARM (older Pi)"
      echo "  Alpine (musl):"
      echo "    linux-musl-x64  Alpine amd64"
      echo "    linux-musl-arm64 Alpine aarch64"
      echo "  Ubuntu (local builds only; not in CI releases; use if prebuilt linux-x64 fails):"
      echo "    ubuntu.24.04-x64   ubuntu.24.04-arm64"
      echo "    ubuntu.22.04-x64   ubuntu.22.04-arm64"
      echo ""
      echo "Output: src/DnsmasqWebUI/bin/Release/net9.0/<RID>/publish/"
      echo ""
      echo "Examples:"
      echo "  $0                          # Publish for current machine (auto-detect RID)"
      echo "  $0 linux-x64                # Publish for glibc amd64 (Debian, Ubuntu, etc.)"
      echo "  $0 linux-arm64              # Publish for Raspberry Pi 4/5 or other aarch64"
      echo "  $0 linux-musl-x64           # Publish for Alpine (e.g. Docker)"
      echo "  $0 --trim linux-x64         # Smaller build (not recommended for Blazor)"
      echo "  $0 --no-clean linux-x64     # Skip clean (faster; same RID/options as last run)"
      exit 0
      ;;
    --trim)
      TRIM=true
      shift
      ;;
    --no-clean)
      CLEAN=false
      shift
      ;;
    -*)
      echo "Error: unknown option $1" >&2
      echo "Use -h, -?, or --help for usage." >&2
      exit 1
      ;;
    *)
      RID="$1"
      shift
      break
      ;;
  esac
done

if [ $# -gt 0 ]; then
  echo "Error: unexpected argument: $1" >&2
  echo "Use -h, -?, or --help for usage." >&2
  exit 1
fi

if [ -z "$RID" ]; then
  RID="$(default_rid)"
  AUTO_RID=true
fi

case "$RID" in
  linux-x64|linux-arm64|linux-arm|linux-musl-x64|linux-musl-arm64|ubuntu.24.04-x64|ubuntu.24.04-arm64|ubuntu.22.04-x64|ubuntu.22.04-arm64) ;;
  *)
    echo "Unknown RID: $RID" >&2
    echo "Supported: $VALID_RIDS" >&2
    exit 1
    ;;
esac

if [ "$AUTO_RID" = true ]; then
  echo "Detected RID: $RID (override by passing a RID as argument)"
fi

# Clean first so previous publish artefacts (e.g. different --trim or stale obj) cannot pollute this build.
# Use -c Release only (no -r) so clean does not require the RID in project.assets.json.
if [ "$CLEAN" = true ]; then
  echo "Cleaning Release..."
  dotnet clean "$PROJECT" -c Release -nologo -v q
fi

if [ "$TRIM" = true ]; then
  echo "Publishing self-contained (trimmed, folder) for $RID..."
  dotnet publish "$PROJECT" \
    -c Release \
    -r "$RID" \
    --self-contained true \
    -p:PublishTrimmed=true
else
  echo "Publishing self-contained (no trim, folder) for $RID..."
  dotnet publish "$PROJECT" \
    -c Release \
    -r "$RID" \
    --self-contained true
fi

OUT_DIR="$REPO_ROOT/src/DnsmasqWebUI/bin/Release/net9.0/$RID/publish"
echo ""
echo "Done. Output: $OUT_DIR"
echo "Run on this host:    $OUT_DIR/DnsmasqWebUI"
echo "Copy to another:    rsync -av $OUT_DIR/ user@host:/opt/dnsmasq-webui/"
