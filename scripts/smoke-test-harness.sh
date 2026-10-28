#!/usr/bin/env sh
# Run an isolated end-to-end smoke test against the Docker dnsmasq harness.
#
# This deliberately never touches testdata-mount or the default compose project. It creates a
# temporary bind mount, seeds a managed overlay before dnsmasq starts, and proves the overlay is
# live after the app starts (as well as checking the API's include order and Hosts source inventory).
set -eu

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
COMPOSE_FILE="$REPO_ROOT/docker-compose.test.yml"
SOURCE_DIR="$REPO_ROOT/testdata"
URL=""
DNSMASQ_VERSION="${DNSMASQ_VERSION:-latest}"
KEEP=false
TIMEOUT_SECONDS=120
PROJECT_NAME="dnsmasq-webui-smoke-$$"
NETWORK_NAME="$PROJECT_NAME-testnet"
NETWORK_CREATED=false
DNS_ADDRESS=""
MOUNT_DIR=""
STARTED=false

usage() {
  echo "Usage: $0 [OPTIONS]"
  echo ""
  echo "Run the Docker harness in an isolated temporary mount and verify the managed overlay end to end."
  echo ""
  echo "Options:"
  echo "  --dnsmasq-version VERSION  Pass DNSMASQ_VERSION to the Docker build (default: latest)."
  echo "  --timeout SECONDS          Readiness/DNS wait timeout (default: 120)."
  echo "  --keep                     Keep the temporary containers and mount for inspection on completion/failure."
  echo "  -h, --help                 Show this help."
}

fail() {
  echo "Smoke test failed: $*" >&2
  if [ "$STARTED" = true ]; then
    echo "--- app logs ---" >&2
    docker compose -p "$PROJECT_NAME" -f "$COMPOSE_FILE" logs --no-color app >&2 || true
  fi
  exit 1
}

cleanup() {
  if [ "$KEEP" = true ]; then
    if [ -n "$MOUNT_DIR" ]; then
      echo "Keeping harness project $PROJECT_NAME and mount $MOUNT_DIR"
    fi
    return
  fi

  if [ "$STARTED" = true ]; then
    docker compose -p "$PROJECT_NAME" -f "$COMPOSE_FILE" down --remove-orphans >/dev/null 2>&1 || true
  fi
  if [ "$NETWORK_CREATED" = true ]; then
    docker network rm "$NETWORK_NAME" >/dev/null 2>&1 || true
  fi

  case "$MOUNT_DIR" in
    /tmp/dnsmasq-webui-smoke.*)
      rm -rf "$MOUNT_DIR"
      ;;
  esac
}

require_command() {
  command -v "$1" >/dev/null 2>&1 || fail "required command is not available: $1"
}

get_json() {
  curl --fail --silent --show-error "$URL$1"
}

wait_for_ready() {
  elapsed=0
  while [ "$elapsed" -lt "$TIMEOUT_SECONDS" ]; do
    if get_json "/healthz/ready" | jq -e '.status == "ok"' >/dev/null 2>&1; then
      return
    fi
    sleep 2
    elapsed=$((elapsed + 2))
  done
  fail "app did not become ready within ${TIMEOUT_SECONDS}s"
}

wait_for_dns_answer() {
  name="$1"
  address="$2"
  elapsed=0
  output=""
  while [ "$elapsed" -lt "$TIMEOUT_SECONDS" ]; do
    output="$(docker compose -p "$PROJECT_NAME" -f "$COMPOSE_FILE" exec -T dhcp-client-dns-a nslookup "$name" "$DNS_ADDRESS" 2>&1 || true)"
    if printf '%s\n' "$output" | grep -F "$address" >/dev/null 2>&1; then
      return
    fi
    sleep 2
    elapsed=$((elapsed + 2))
  done
  echo "$output" >&2
  fail "dnsmasq did not answer $name with $address"
}

while [ $# -gt 0 ]; do
  case "$1" in
    --dnsmasq-version)
      shift
      [ $# -gt 0 ] || fail "--dnsmasq-version requires a value"
      DNSMASQ_VERSION="$1"
      shift
      ;;
    --timeout)
      shift
      [ $# -gt 0 ] || fail "--timeout requires a value"
      TIMEOUT_SECONDS="$1"
      shift
      ;;
    --keep)
      KEEP=true
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      fail "unknown option: $1"
      ;;
  esac
done

case "$TIMEOUT_SECONDS" in
  *[!0-9]*|"") fail "--timeout must be a positive integer" ;;
esac
if [ "$TIMEOUT_SECONDS" -le 0 ]; then
  fail "--timeout must be a positive integer"
fi

require_command docker
require_command curl
require_command jq
require_command tar
docker info >/dev/null 2>&1 || fail "Docker daemon is not available"

MOUNT_DIR="$(mktemp -d /tmp/dnsmasq-webui-smoke.XXXXXX)"
trap cleanup EXIT INT TERM

if command -v rsync >/dev/null 2>&1; then
  rsync -a \
    --exclude='leases' \
    --exclude='zz-dnsmasq-webui.conf' \
    --exclude='zz-dnsmasq-webui.hosts' \
    "$SOURCE_DIR/" "$MOUNT_DIR/"
else
  (cd "$SOURCE_DIR" && tar cf - --exclude=leases --exclude=zz-dnsmasq-webui.conf --exclude=zz-dnsmasq-webui.hosts .) | (cd "$MOUNT_DIR" && tar xf -)
fi

# Seed a non-empty overlay before dnsmasq starts. The app must append its conf-file= line and then
# restart/reload dnsmasq; otherwise these names are visible to the API but not resolvable by dnsmasq.
cp "$SOURCE_DIR/harness/managed-overlay.conf" "$MOUNT_DIR/zz-dnsmasq-webui.conf"
cp "$SOURCE_DIR/harness/managed-overlay.hosts" "$MOUNT_DIR/zz-dnsmasq-webui.hosts"

# Let Docker reserve an unused subnet instead of competing with the normal
# harness's fixed network. Only the disposable fixture copy is rewritten.
docker network create "$NETWORK_NAME" >/dev/null
NETWORK_CREATED=true
subnet="$(docker network inspect "$NETWORK_NAME" | jq -er '.[0].IPAM.Config[] | select(.Subnet | contains(":") | not) | .Subnet')"
network_address="${subnet%/*}"
prefix_length="${subnet#*/}"
if [ "$prefix_length" -gt 24 ] || [ "${network_address##*.}" != 0 ]; then
  fail "Docker allocated $subnet; the DHCP smoke fixtures require an IPv4 subnet of /24 or larger"
fi
network_prefix="${network_address%.*}"
DNS_ADDRESS="$network_prefix.2"
find "$MOUNT_DIR" -type f -exec sed -i "s/172\\.28\\.0\\./$network_prefix./g" {} +

# Render a standalone config so all later commands use the same mount and
# build version. Publish an ephemeral loopback port and discover it after up.
base_config="$MOUNT_DIR/compose.base.json"
smoke_config="$MOUNT_DIR/compose.smoke.json"
TESTDATA_MOUNT="$MOUNT_DIR" DNSMASQ_VERSION="$DNSMASQ_VERSION" \
  docker compose -p "$PROJECT_NAME" -f "$COMPOSE_FILE" config --format json > "$base_config"
jq --arg network "$NETWORK_NAME" --arg address "$DNS_ADDRESS" '
  .networks.testnet = {name: $network, external: true} |
  .services.app.networks.testnet.ipv4_address = $address |
  .services.app.ports = [{target: 8080, published: "0", host_ip: "127.0.0.1", protocol: "tcp"}]
' "$base_config" > "$smoke_config"
rm "$base_config"
COMPOSE_FILE="$smoke_config"

echo "Starting isolated Docker harness ($PROJECT_NAME)..."
STARTED=true
docker compose -p "$PROJECT_NAME" -f "$COMPOSE_FILE" up -d --build
URL="http://$(docker compose -p "$PROJECT_NAME" -f "$COMPOSE_FILE" port app 8080)"

wait_for_ready

config_set="$(get_json "/api/config/set")"
expected_files="$(printf '%s\n' \
  /data/dnsmasq-test.conf \
  /data/dnsmasq.d/01-other.conf \
  /data/dnsmasq.d/02-servers.conf \
  /data/dnsmasq.d/03-more.conf \
  /data/dnsmasq.d/dhcp.conf \
  /data/zz-dnsmasq-webui.conf)"
actual_files="$(printf '%s' "$config_set" | jq -r '.files[].path')"
if [ "$actual_files" != "$expected_files" ]; then
  echo "Expected config load order:" >&2
  echo "$expected_files" >&2
  echo "Actual config load order:" >&2
  echo "$actual_files" >&2
  fail "config include order does not put the managed overlay last"
fi

status="$(get_json "/api/status")"
if ! printf '%s' "$status" | jq -e \
  --arg managedHosts /data/zz-dnsmasq-webui.hosts \
  --arg startupAddress '/startup-overlay.smoke.test/192.0.2.61' \
  '.dnsmasqStatus == "active" and
   .managedHostsPathExists == true and
   (.addnHostsPaths | index($managedHosts) != null) and
   (.effectiveConfig.addressValues | index($startupAddress) != null)' >/dev/null; then
  fail "status endpoint does not expose the seeded managed overlay as effective"
fi

sources="$(get_json "/api/hosts/sources")"
if ! printf '%s' "$sources" | jq -e \
  --arg managedHosts /data/zz-dnsmasq-webui.hosts \
  --arg firstHosts /data/hosts \
  --arg secondHosts /data/hosts.extra \
  'any(.[]; .sourceKind == "Managed" and .path == $managedHosts and .isActive == true) and
   any(.[]; .sourceKind == "AddnHosts" and .path == $firstHosts and .isActive == true) and
   any(.[]; .sourceKind == "AddnHosts" and .path == $secondHosts and .isActive == true)' >/dev/null; then
  fail "Hosts source inventory is incomplete or reports an inactive managed source"
fi

unified_hosts="$(get_json "/api/hosts/unified")"
if ! printf '%s' "$unified_hosts" | jq -e \
  'any(.[]; .sourceKind == "Managed" and .isActive == true and (.names | index("startup-host.smoke.test") != null))' >/dev/null; then
  fail "unified Hosts endpoint does not contain the active managed host"
fi

docker compose -p "$PROJECT_NAME" -f "$COMPOSE_FILE" exec -T app \
  dnsmasq --test --conf-file=/data/dnsmasq-test.conf >/dev/null || fail "dnsmasq rejects the effective configuration"

wait_for_dns_answer startup-overlay.smoke.test 192.0.2.61
wait_for_dns_answer startup-host.smoke.test 192.0.2.62

echo "Smoke test passed: managed overlay is active in dnsmasq, config load order is correct, and Hosts sources are authoritative."
