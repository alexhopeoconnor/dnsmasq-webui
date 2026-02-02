#!/usr/bin/env sh
# Prepare the testdata mount and optionally start or stop the Docker test harness
# (app + dnsmasq + DHCP client). See testdata/README.md and docker-compose.test.yml.
#
# What this script does:
#   Start (default): clear mount (unless --no-clear), sync source -> mount, remove *dnsmasq-webui*.conf,
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
NO_BUILD=false
RECREATE=false
NO_CLEAR=false
STOP=false
TIDY=false

usage() {
  echo "Usage: $0 [OPTIONS] [--]"
  echo ""
  echo "Prepare the testdata mount directory and optionally start the Docker test harness"
  echo "(app with dnsmasq in one container, plus a DHCP client). The mount is synced from"
  echo "testdata/ by default (includes sample leases file so the harness shows leases)."
  echo ""
  echo "Steps:"
  echo "  1. Clear mount dir (unless --no-clear), then sync source -> mount."
  echo "  2. Remove any leftover *dnsmasq-webui*.conf so dnsmasq starts clean."
  echo "  3. If not --prepare-only: docker compose -f $COMPOSE_FILE up -d [options]."
  echo ""
  echo "Path options:"
  echo "  --source DIR        Source to copy from (default: testdata)"
  echo "  --mount DIR         Target mount directory (default: testdata-mount)"
  echo "                      Compose uses TESTDATA_MOUNT; script exports it if you use --mount."
  echo ""
  echo "Mount behaviour:"
  echo "  (default)           Clear mount dir completely, then sync. Use for a clean run."
  echo "  --no-clear          Do not clear mount dir; only sync over existing contents."
  echo "                      Use to preserve leases or debug files between runs."
  echo ""
  echo "Compose behaviour:"
  echo "  --prepare-only      Only prepare the mount; do not run docker compose."
  echo "                      Use to inspect or edit the mount before starting containers."
  echo "  --no-build           Do not pass --build to docker compose (use existing images)."
  echo "                      Use for a quick restart when only the mount changed."
  echo "  --recreate           Pass --force-recreate to docker compose (recreate containers)."
  echo "                      Use to ensure fresh container state and mounts."
  echo ""
  echo "Stop / tidy:"
  echo "  --stop              Stop the test harness: docker compose down (no prepare, no start)."
  echo "  --tidy              Stop the harness and clear the mount directory for a clean next run."
  echo "                      Uses default mount dir unless --mount DIR is given."
  echo ""
  echo "Other:"
  echo "  -h, --help          Show this help and exit."
  echo ""
  echo "Examples:"
  echo "  $0"
  echo "                      Full run: clear mount, sync testdata, build and start containers."
  echo ""
  echo "  $0 --no-build"
  echo "                      Clear and sync, then start containers without rebuilding images."
  echo ""
  echo "  $0 --recreate"
  echo "                      Clear and sync, then up --build --force-recreate (clean containers)."
  echo ""
  echo "  $0 --no-clear --no-build"
  echo "                      Preserve mount contents, sync over it, start without rebuild."
  echo ""
  echo "  $0 --prepare-only"
  echo "                      Only clear and sync testdata -> testdata-mount; no containers."
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
    -h|--help)
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
    --no-clear)
      NO_CLEAR=true
      shift
      ;;
    --prepare-only)
      PREPARE_ONLY=true
      shift
      ;;
    --no-build)
      NO_BUILD=true
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
    *)
      echo "Error: unknown option $1" >&2
      usage >&2
      exit 1
      ;;
  esac
done

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

if [ "$NO_CLEAR" = false ]; then
  echo "Clearing mount directory: $MOUNT_DIR"
  find "$MOUNT_DIR" -mindepth 1 -delete 2>/dev/null || true
fi

if command -v rsync >/dev/null 2>&1; then
  rsync -a --exclude='leases' "$SOURCE_DIR/" "$MOUNT_DIR/"
else
  cp -r "$SOURCE_DIR/." "$MOUNT_DIR/"
  rm -f "$MOUNT_DIR/leases"
fi

# Remove any leftover managed config from previous runs so dnsmasq starts clean (app will create zz-dnsmasq-webui.conf on startup).
find "$MOUNT_DIR" -name '*dnsmasq-webui*.conf' -type f -delete 2>/dev/null || true

echo "Mount directory ready: $MOUNT_DIR (source: $SOURCE_DIR)."

if [ "$PREPARE_ONLY" = true ]; then
  echo "To start the harness: TESTDATA_MOUNT=./$MOUNT_DIR docker compose -f $COMPOSE_FILE up -d --build"
  exit 0
fi

# Compose uses TESTDATA_MOUNT for the data volume (default: ./testdata-mount)
export TESTDATA_MOUNT="./$MOUNT_DIR"

COMPOSE_CMD="docker compose -f $COMPOSE_FILE up -d"
if [ "$NO_BUILD" = true ]; then
  COMPOSE_CMD="$COMPOSE_CMD"
else
  COMPOSE_CMD="$COMPOSE_CMD --build"
fi
if [ "$RECREATE" = true ]; then
  COMPOSE_CMD="$COMPOSE_CMD --force-recreate"
fi

echo "Running: $COMPOSE_CMD"
exec $COMPOSE_CMD
