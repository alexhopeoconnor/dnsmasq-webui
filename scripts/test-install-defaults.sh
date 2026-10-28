#!/usr/bin/env sh
# Focused regression checks for install.sh's host binding defaults. These tests
# load only the helper functions, so they neither download a release nor touch
# systemd or an install directory.
set -eu

SCRIPT_DIR="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"
REPO_DIR="$(CDPATH= cd -- "$SCRIPT_DIR/.." && pwd)"
INSTALL_SCRIPT="$SCRIPT_DIR/install.sh"
TEST_DIR="$(mktemp -d)"

cleanup() {
  rm -rf "$TEST_DIR"
  rm -f "${CONFIG_SET_FILE:-}"
}
trap cleanup EXIT HUP INT TERM

fail() {
  echo "FAIL: $*" >&2
  exit 1
}

assert_line() {
  expected="$1"
  file="$2"
  grep -Fqx "$expected" "$file" || fail "expected '$expected' in $file"
}

# Do not source argument parsing and the install entry point. The helper block
# includes the same defaults and write_env_file implementation used in release
# and source installs.
helpers="$TEST_DIR/install-helpers.sh"
sed '/^# Parse args$/,$d' "$INSTALL_SCRIPT" > "$helpers"
. "$helpers"

# The sourced script installs an EXIT trap of its own; restore the test cleanup.
trap cleanup EXIT HUP INT TERM

CONFIG_VARS_FILE="$TEST_DIR/config-vars"
: > "$CONFIG_VARS_FILE"
env_file="$TEST_DIR/dnsmasq-webui.env"

write_env_file "$env_file" "$DEFAULT_ASPNETCORE_URLS"
assert_line 'ASPNETCORE_URLS=http://127.0.0.1:8080' "$env_file"

printf '%s\n' 'ASPNETCORE_URLS=http://0.0.0.0:8181' > "$CONFIG_VARS_FILE"
write_env_file "$env_file" "$DEFAULT_ASPNETCORE_URLS"
assert_line 'ASPNETCORE_URLS="http://0.0.0.0:8181"' "$env_file"
if grep -Fq '127.0.0.1:8080' "$env_file"; then
  fail "an explicit URL override was replaced by the default"
fi

printf '%s\n' 'ASPNETCORE_URLS="http://127.0.0.1:9191"' > "$env_file"
printf '%s\n' 'Application__ApplicationTitle=Tree DNS' > "$CONFIG_VARS_FILE"
write_env_file "$env_file" "$DEFAULT_ASPNETCORE_URLS"
assert_line 'ASPNETCORE_URLS="http://127.0.0.1:9191"' "$env_file"
assert_line 'Application__ApplicationTitle="Tree DNS"' "$env_file"

if grep -Fq 'Environment=ASPNETCORE_URLS=' "$INSTALL_SCRIPT"; then
  fail "the systemd unit hard-codes a URL instead of allowing EnvironmentFile overrides"
fi

assert_line 'ENV ASPNETCORE_URLS=http://+:8080' "$REPO_DIR/Dockerfile"
if ! grep -Fq '127.0.0.1:8080:8080' "$REPO_DIR/docker-compose.test.yml"; then
  fail "the local test harness publishes its management port beyond loopback"
fi
if ! grep -Fq '"applicationUrl": "http://localhost:5201"' "$REPO_DIR/src/DnsmasqWebUI/Properties/launchSettings.json"; then
  fail "the development HTTP launch profile no longer binds to localhost"
fi

echo "install binding default checks passed"
