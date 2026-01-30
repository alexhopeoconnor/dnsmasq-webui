# Test data

Used in two ways:

1. **Unit tests** – Parsers and services read from `testdata/` (e.g. `testdata/leases` for lease-format tests).
2. **Docker test harness** – Run `scripts/prepare-test-mount.sh` to copy this into `testdata-mount/` (gitignored). Compose mounts `testdata-mount` as `/data`. **leases** is excluded from the copy so dnsmasq creates and owns the real leases file in the harness.

Files:

- **hosts** – Shared by app and dnsmasq (read/write by app, `addn-hosts` for dnsmasq).
- **dnsmasq.conf** – Sample main config (reference only; not used by the test containers).
- **dnsmasq-test.conf** – Config for dnsmasq when run inside the app container (DHCP-only, paths under `/data`).
- **dnsmasq.d/dhcp.conf** – DHCP static hosts; app edits this, dnsmasq loads it via `conf-dir`.
- **dnsmasq.d/01-other.conf** – Other snippet (not managed by app).
- **leases** – For **unit tests only**. In the Docker harness, dnsmasq creates `/data/leases`; the app watches it and shows live leases.
