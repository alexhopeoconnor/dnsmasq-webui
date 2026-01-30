#!/usr/bin/env sh
# Prepare testdata-mount from testdata (or --source) and optionally run the Docker test harness.
# testdata/leases is excluded so dnsmasq creates the real leases file in the container.
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
COMPOSE_FILE="docker-compose.test.yml"

SOURCE_DIR=""
MOUNT_DIR=""
PREPARE_ONLY=false
NO_BUILD=false
RECREATE=false

usage() {
  echo "Usage: $0 [OPTIONS] [--]"
  echo ""
  echo "Copy test data into the mount directory (default: testdata -> testdata-mount),"
  echo "then run 'docker compose -f $COMPOSE_FILE up' unless --prepare-only is set."
  echo ""
  echo "Options:"
  echo "  -h, --help          Show this help"
  echo "  --source DIR        Source directory to copy from (default: testdata)"
  echo "  --mount DIR         Target mount directory (default: testdata-mount)"
  echo "  --prepare-only      Only copy data; do not run docker compose"
  echo "  --no-build          Run 'docker compose up' without --build (use existing images)"
  echo "  --recreate          Pass --force-recreate to docker compose up"
  echo ""
  echo "Examples:"
  echo "  $0                    # Prepare from testdata, then up --build"
  echo "  $0 --no-build         # Prepare, then up without rebuilding"
  echo "  $0 --prepare-only     # Only sync testdata -> testdata-mount"
  echo "  $0 --source myfixtures --mount mymount --prepare-only"
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

if [ ! -d "$SOURCE_DIR" ]; then
  echo "Error: source directory '$SOURCE_DIR' does not exist" >&2
  exit 1
fi

mkdir -p "$MOUNT_DIR"

if command -v rsync >/dev/null 2>&1; then
  rsync -a --exclude=leases "$SOURCE_DIR/" "$MOUNT_DIR/"
else
  find "$MOUNT_DIR" -mindepth 1 -delete 2>/dev/null || true
  cp -r "$SOURCE_DIR/." "$MOUNT_DIR/"
  rm -f "$MOUNT_DIR/leases"
fi

echo "Mount directory ready: $MOUNT_DIR (source: $SOURCE_DIR, leases excluded)."

if [ "$PREPARE_ONLY" = true ]; then
  echo "Run manually: docker compose -f $COMPOSE_FILE up --build"
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
