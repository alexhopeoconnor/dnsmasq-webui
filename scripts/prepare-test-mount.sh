#!/usr/bin/env sh
# Prepare the testdata mount and optionally start or stop the Docker test harness
# (app + dnsmasq + DHCP client). See testdata/README.md and docker-compose.test.yml.
#
# What this script does:
#   Start (default): sync source -> mount (preserving existing unless --clear); optional --reset-managed
#     removes app-written files in the mount. Then docker compose up -d [--build] [--force-recreate].
#   --stop: docker compose down (stop and remove containers/networks).
#   --tidy: docker compose down, then clear the mount directory for a clean next run.
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
COMPOSE_FILE="docker-compose.test.yml"

SOURCE_DIR=""
MOUNT_DIR=""
DNSMASQ_VERSION=""
PREPARE_ONLY=false
BUILD=false
NO_CACHE_BUILD=false
RECREATE=false
CLEAR=false
RESET_MANAGED=false
STOP=false
TIDY=false
MINIMAL_CONF=false

usage() {
  echo "Usage: $0 [OPTIONS] [--]"
  echo ""
  echo "Prepare the testdata mount directory and optionally start or stop the Docker test harness"
  echo "(app + dnsmasq + DHCP client). By default: sync testdata -> mount (preserving existing), then up -d (no rebuild)."
  echo ""
  echo "Steps (when starting):"
  echo "  1. Optionally clear mount dir (only with --clear), then sync source -> mount."
  echo "      Sync skips app-managed filenames (zz-dnsmasq-webui.conf / .hosts) so rebuilds keep UI state."
  echo "  2. Optionally remove managed *dnsmasq-webui*.conf / *dnsmasq-webui*.hosts (only with --reset-managed)."
  echo "  3. If not --prepare-only: docker compose -f $COMPOSE_FILE up -d [options]."
  echo ""
  echo "Path options:"
  echo "  --source DIR        Source to copy from (default: testdata)"
  echo "  --mount DIR         Target mount directory (default: testdata-mount)"
  echo "                      Compose uses TESTDATA_MOUNT; script exports it if you use --mount."
  echo "  --dnsmasq-version V Dnsmasq version for the harness image:"
  echo "                      latest (default), distro, or an exact upstream version like 2.91."
  echo "                      Non-default values require --build or --no-cache-build; script fails with --no-build."
  echo ""
  echo "Mount behaviour:"
  echo "  (default)           Preserve mount dir; sync source over existing contents."
  echo "  --clear             Clear mount dir completely before sync. Use for a clean run."
  echo "  --reset-managed     After sync, delete *dnsmasq-webui*.conf and *dnsmasq-webui*.hosts under the"
  echo "                      mount so the app recreates managed config/hosts (leases and other files stay)."
  echo ""
  echo "Compose behaviour:"
  echo "  --minimal-conf     Use minimal dnsmasq config (dnsmasq-test-minimal.conf) so effective"
  echo "                      config has few readonly options; good for testing reload/restart failure flows."
  echo "  --prepare-only      Only prepare the mount; do not run docker compose."
  echo "                      Use to inspect or edit the mount before starting containers."
  echo "  --build             Pass --build to docker compose (rebuild images before starting)."
  echo "                      Use after changing the app or Dockerfile. Default: use existing images."
  echo "  --no-cache-build    Rebuild images with --pull --no-cache before starting."
  echo "                      Use to force a fresh image build and refresh 'latest' dnsmasq."
  echo "  --no-build          Do not rebuild (default). Use existing images for a quick restart."
  echo "                      With --no-build, 'latest' is not resolved (no network)."
  echo "  --recreate          Pass --force-recreate to docker compose (recreate containers)."
  echo "                      Use to ensure fresh container state and mounts."
  echo ""
  echo "Stop / tidy (take precedence: other options are ignored when used):"
  echo "  --stop              Stop the test harness: docker compose down (no prepare, no start)."
  echo "  --tidy              Stop the harness and clear the mount directory for a clean next run."
  echo "                      Uses default mount dir unless --mount DIR is given."
  echo ""
  echo "Other:"
  echo "  -h, -?, --help      Show this help and exit."
  echo ""
  echo "Examples:"
  echo "  $0"
  echo "                      Sync testdata to mount (preserving existing), start containers (no rebuild)."
  echo ""
  echo "  $0 --build"
  echo "                      Sync, rebuild images, and start containers."
  echo ""
  echo "  $0 --recreate"
  echo "                      Sync, then up -d --force-recreate (fresh containers)."
  echo ""
  echo "  $0 --no-cache-build"
  echo "                      Force a fresh image build (no Docker build cache), then start containers."
  echo ""
  echo "  $0 --clear"
  echo "                      Clear mount, sync testdata, start (clean run, no rebuild)."
  echo ""
  echo "  $0 --reset-managed --build"
  echo "                      Rebuild images; sync fixtures; drop managed conf/hosts so app writes fresh."
  echo ""
  echo "  $0 --prepare-only"
  echo "                      Only sync testdata -> testdata-mount; no containers."
  echo "                      Then run: docker compose -f $COMPOSE_FILE up -d [--build]"
  echo ""
  echo "  $0 --source myfixtures --mount mymount --prepare-only"
  echo "                      Sync myfixtures -> mymount only. Start with:"
  echo "                      TESTDATA_MOUNT=./mymount docker compose -f $COMPOSE_FILE up -d"
  echo ""
  echo "  $0 --stop"
  echo "                      Stop and remove test harness containers and networks."
  echo ""
  echo "  $0 --tidy"
  echo "                      Stop harness and clear testdata-mount for a clean next run."
  echo ""
  echo "  $0 --minimal-conf"
  echo "                      Use dnsmasq-test-minimal.conf (single file, few options) and start."
  echo ""
  echo "  $0 --dnsmasq-version 2.91 --build"
  echo "                      Rebuild the harness image with dnsmasq 2.91."
}

fetch_url() {
  if command -v curl >/dev/null 2>&1; then
    curl -fsSL "$1"
    return $?
  fi
  if command -v wget >/dev/null 2>&1; then
    wget -qO- "$1"
    return $?
  fi
  echo "Error: need curl or wget to resolve the latest upstream dnsmasq version." >&2
  return 1
}

resolve_dnsmasq_version() {
  if [ "$DNSMASQ_VERSION" != "latest" ]; then
    return 0
  fi

  RESOLVED_VERSION="$(fetch_url "https://thekelleys.org.uk/dnsmasq/" | sed -n 's/.*LATEST_IS_\([0-9][0-9.]*\).*/\1/p' | head -n1)"
  if [ -z "$RESOLVED_VERSION" ]; then
    echo "Error: could not resolve the latest upstream dnsmasq version." >&2
    exit 1
  fi
  DNSMASQ_VERSION="$RESOLVED_VERSION"
}

while [ $# -gt 0 ]; do
  case "$1" in
    -h|-?|--help)
      usage
      exit 0
      ;;
    --source)
      shift
      [ $# -gt 0 ] || { echo "Error: --source requires DIR" >&2; exit 1; }
      SOURCE_DIR="$1"
      shift
      ;;
    --mount)
      shift
      [ $# -gt 0 ] || { echo "Error: --mount requires DIR" >&2; exit 1; }
      MOUNT_DIR="$1"
      shift
      ;;
    --dnsmasq-version)
      shift
      [ $# -gt 0 ] || { echo "Error: --dnsmasq-version requires VERSION" >&2; exit 1; }
      DNSMASQ_VERSION="$1"
      shift
      ;;
    --clear)
      CLEAR=true
      shift
      ;;
    --reset-managed)
      RESET_MANAGED=true
      shift
      ;;
    --prepare-only)
      PREPARE_ONLY=true
      shift
      ;;
    --build)
      BUILD=true
      shift
      ;;
    --no-cache-build)
      NO_CACHE_BUILD=true
      BUILD=true
      shift
      ;;
    --no-build)
      BUILD=false
      NO_CACHE_BUILD=false
      shift
      ;;
    --recreate)
      RECREATE=true
      shift
      ;;
    --minimal-conf)
      MINIMAL_CONF=true
      shift
      ;;
    --stop)
      STOP=true
      shift
      ;;
    --tidy)
      TIDY=true
      shift
      ;;
    --)
      shift
      break
      ;;
    -?*)
      echo "Error: unknown option $1" >&2
      usage >&2
      exit 1
      ;;
    *)
      echo "Error: unexpected argument: $1" >&2
      usage >&2
      exit 1
      ;;
  esac
done

if [ $# -gt 0 ]; then
  echo "Error: unexpected argument: $1" >&2
  usage >&2
  exit 1
fi

cd "$REPO_ROOT"

: "${SOURCE_DIR:=testdata}"
: "${MOUNT_DIR:=testdata-mount}"
: "${DNSMASQ_VERSION:=latest}"

# Stop and/or tidy: no prepare, no start
if [ "$STOP" = true ] || [ "$TIDY" = true ]; then
  echo "Stopping test harness: docker compose -f $COMPOSE_FILE down"
  docker compose -f "$COMPOSE_FILE" down
  if [ "$TIDY" = true ]; then
    if [ -d "$MOUNT_DIR" ]; then
      echo "Clearing mount directory: $MOUNT_DIR"
      find "$MOUNT_DIR" -mindepth 1 -delete 2>/dev/null || true
      echo "Mount directory cleared."
    else
      echo "Mount directory $MOUNT_DIR does not exist; nothing to clear."
    fi
  fi
  exit 0
fi

if [ ! -d "$SOURCE_DIR" ]; then
  echo "Error: source directory '$SOURCE_DIR' does not exist" >&2
  exit 1
fi

# Resolve "latest" only when we will build; avoid network when doing a no-build run.
WILL_BUILD=false
[ "$BUILD" = true ] || [ "$NO_CACHE_BUILD" = true ] && WILL_BUILD=true
if [ "$WILL_BUILD" = false ] && [ "$DNSMASQ_VERSION" != "latest" ] && [ "$DNSMASQ_VERSION" != "distro" ]; then
  echo "Error: --dnsmasq-version $DNSMASQ_VERSION has no effect without --build or --no-cache-build. Add --build or omit --dnsmasq-version for a no-build run." >&2
  exit 1
fi
if [ "$WILL_BUILD" = true ]; then
  resolve_dnsmasq_version
fi

# Take the harness down so all containers (including one-shot DHCP clients) are recreated on up.
echo "Stopping test harness: docker compose -f $COMPOSE_FILE down"
docker compose -f "$COMPOSE_FILE" down

mkdir -p "$MOUNT_DIR"

if [ "$CLEAR" = true ]; then
  echo "Clearing mount directory: $MOUNT_DIR"
  find "$MOUNT_DIR" -mindepth 1 -delete 2>/dev/null || true
fi

# Exclude sample leases (harness creates real lease file) and app-managed filenames so rebuilds preserve
# UI-written config/hosts; use --reset-managed or --clear when you want those removed.
if command -v rsync >/dev/null 2>&1; then
  rsync -a \
    --exclude='leases' \
    --exclude='zz-dnsmasq-webui.conf' \
    --exclude='zz-dnsmasq-webui.hosts' \
    "$SOURCE_DIR/" "$MOUNT_DIR/"
else
  (cd "$SOURCE_DIR" && tar cf - --exclude=leases --exclude=zz-dnsmasq-webui.conf --exclude=zz-dnsmasq-webui.hosts .) | (cd "$MOUNT_DIR" && tar xf -)
fi

if [ "$RESET_MANAGED" = true ]; then
  echo "Removing app-managed files (*dnsmasq-webui*.conf / *dnsmasq-webui*.hosts) under $MOUNT_DIR"
  find "$MOUNT_DIR" \( -name '*dnsmasq-webui*.conf' -o -name '*dnsmasq-webui*.hosts' \) -type f -delete 2>/dev/null || true
fi

echo "Mount directory ready: $MOUNT_DIR (source: $SOURCE_DIR)."

if [ "$PREPARE_ONLY" = true ]; then
  case "$MOUNT_DIR" in
    /*) MOUNT_EXPORT="$MOUNT_DIR" ;;
    *)  MOUNT_EXPORT="./$MOUNT_DIR" ;;
  esac
  START_PREFIX="TESTDATA_MOUNT=$MOUNT_EXPORT DNSMASQ_VERSION=$DNSMASQ_VERSION"
  if [ "$MINIMAL_CONF" = true ]; then
    START_PREFIX="$START_PREFIX TEST_DNSMASQ_CONF=/data/dnsmasq-test-minimal.conf"
  fi
  if [ "$NO_CACHE_BUILD" = true ]; then
    START_CMD="$START_PREFIX docker compose -f $COMPOSE_FILE build --pull --no-cache && $START_PREFIX docker compose -f $COMPOSE_FILE up -d"
  else
    START_CMD="$START_PREFIX docker compose -f $COMPOSE_FILE up -d [--build]"
  fi
  echo "To start the harness: $START_CMD"
  exit 0
fi

# Compose uses TESTDATA_MOUNT for the data volume (default: ./testdata-mount)
case "$MOUNT_DIR" in
  /*) export TESTDATA_MOUNT="$MOUNT_DIR" ;;
  *)  export TESTDATA_MOUNT="./$MOUNT_DIR" ;;
esac

export DNSMASQ_VERSION

case "$DNSMASQ_VERSION" in
  distro)
    echo "Using distro-packaged dnsmasq for the harness image."
    ;;
  *)
    echo "Using dnsmasq $DNSMASQ_VERSION for the harness image."
    ;;
esac

if [ "$MINIMAL_CONF" = true ]; then
  export TEST_DNSMASQ_CONF="/data/dnsmasq-test-minimal.conf"
  echo "Using minimal config: $TEST_DNSMASQ_CONF"
fi

if [ "$NO_CACHE_BUILD" = true ]; then
  BUILD_CMD="docker compose -f $COMPOSE_FILE build --pull --no-cache"
  echo "Running: $BUILD_CMD"
  $BUILD_CMD
fi

COMPOSE_CMD="docker compose -f $COMPOSE_FILE up -d"
if [ "$BUILD" = true ] && [ "$NO_CACHE_BUILD" != true ]; then
  COMPOSE_CMD="$COMPOSE_CMD --build"
fi
if [ "$RECREATE" = true ]; then
  COMPOSE_CMD="$COMPOSE_CMD --force-recreate"
fi

echo "Running: $COMPOSE_CMD"
exec $COMPOSE_CMD
