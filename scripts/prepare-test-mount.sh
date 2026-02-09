#!/usr/bin/env sh
# Prepare the testdata mount and optionally start or stop the Docker test harness
# (app + dnsmasq + DHCP client). See testdata/README.md and docker-compose.test.yml.
#
# What this script does:
#   Start (default): sync source -> mount (preserving existing unless --clear), clean up previous test data,
#     then docker compose up -d [--build] [--force-recreate].
#   --stop: docker compose down (stop and remove containers/networks).
#   --tidy: docker compose down, then clear the mount directory for a clean next run.
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
COMPOSE_FILE="docker-compose.test.yml"

SOURCE_DIR=""
MOUNT_DIR=""
PREPARE_ONLY=false
BUILD=false
RECREATE=false
CLEAR=false
STOP=false
TIDY=false

usage() {
  echo "Usage: $0 [OPTIONS] [--]"
  echo ""
  echo "Prepare the testdata mount directory and optionally start or stop the Docker test harness"
  echo "(app + dnsmasq + DHCP client). By default: sync testdata -> mount (preserving existing), then up -d (no rebuild)."
  echo ""
  echo "Steps (when starting):"
  echo "  1. Optionally clear mount dir (only with --clear), then sync source -> mount."
  echo "  2. Clean up previous test data (e.g. managed config) so the harness starts clean."
  echo "  3. If not --prepare-only: docker compose -f $COMPOSE_FILE up -d [options]."
  echo ""
  echo "Path options:"
  echo "  --source DIR        Source to copy from (default: testdata)"
  echo "  --mount DIR         Target mount directory (default: testdata-mount)"
  echo "                      Compose uses TESTDATA_MOUNT; script exports it if you use --mount."
  echo ""
  echo "Mount behaviour:"
  echo "  (default)           Preserve mount dir; sync source over existing contents."
  echo "  --clear             Clear mount dir completely before sync. Use for a clean run."
  echo ""
  echo "Compose behaviour:"
  echo "  --prepare-only      Only prepare the mount; do not run docker compose."
  echo "                      Use to inspect or edit the mount before starting containers."
  echo "  --build             Pass --build to docker compose (rebuild images before starting)."
  echo "                      Use after changing the app or Dockerfile. Default: use existing images."
  echo "  --no-build          Do not rebuild (default). Use existing images for a quick restart."
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
  echo "  $0 --clear"
  echo "                      Clear mount, sync testdata, start (clean run, no rebuild)."
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
    --clear)
      CLEAR=true
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
    --no-build)
      BUILD=false
      shift
      ;;
    --recreate)
      RECREATE=true
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

# Take the harness down so all containers (including one-shot DHCP clients) are recreated on up.
echo "Stopping test harness: docker compose -f $COMPOSE_FILE down"
docker compose -f "$COMPOSE_FILE" down

mkdir -p "$MOUNT_DIR"

if [ "$CLEAR" = true ]; then
  echo "Clearing mount directory: $MOUNT_DIR"
  find "$MOUNT_DIR" -mindepth 1 -delete 2>/dev/null || true
fi

if command -v rsync >/dev/null 2>&1; then
  rsync -a --exclude='leases' "$SOURCE_DIR/" "$MOUNT_DIR/"
else
  cp -r "$SOURCE_DIR/." "$MOUNT_DIR/"
  rm -f "$MOUNT_DIR/leases"
fi

# Clean up previous test data (e.g. managed config) so the harness starts clean (app will create zz-dnsmasq-webui.conf on startup).
find "$MOUNT_DIR" -name '*dnsmasq-webui*.conf' -type f -delete 2>/dev/null || true

echo "Mount directory ready: $MOUNT_DIR (source: $SOURCE_DIR)."

if [ "$PREPARE_ONLY" = true ]; then
  case "$MOUNT_DIR" in
    /*) MOUNT_EXPORT="$MOUNT_DIR" ;;
    *)  MOUNT_EXPORT="./$MOUNT_DIR" ;;
  esac
  echo "To start the harness: TESTDATA_MOUNT=$MOUNT_EXPORT docker compose -f $COMPOSE_FILE up -d [--build]"
  exit 0
fi

# Compose uses TESTDATA_MOUNT for the data volume (default: ./testdata-mount)
case "$MOUNT_DIR" in
  /*) export TESTDATA_MOUNT="$MOUNT_DIR" ;;
  *)  export TESTDATA_MOUNT="./$MOUNT_DIR" ;;
esac

COMPOSE_CMD="docker compose -f $COMPOSE_FILE up -d"
if [ "$BUILD" = true ]; then
  COMPOSE_CMD="$COMPOSE_CMD --build"
fi
if [ "$RECREATE" = true ]; then
  COMPOSE_CMD="$COMPOSE_CMD --force-recreate"
fi

echo "Running: $COMPOSE_CMD"
exec $COMPOSE_CMD
