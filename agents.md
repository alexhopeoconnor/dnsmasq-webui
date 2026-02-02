# Agent instructions: dnsmasq-webui

## Test harness (Docker)

The test harness runs the app and dnsmasq in Docker with DHCP clients so you can test the UI and effective config against real dnsmasq.

**Script:** `scripts/prepare-test-mount.sh`  
**Compose file:** `docker-compose.test.yml`

### Start the harness

From the repo root:

```bash
./scripts/prepare-test-mount.sh
```

This will:

1. Clear `testdata-mount/` (unless you pass `--no-clear`)
2. Sync `testdata/` → `testdata-mount/`
3. Remove any `*dnsmasq-webui*.conf` in the mount so dnsmasq starts clean
4. Run `docker compose -f docker-compose.test.yml up -d --build`

- **Quick restart (no image rebuild):** `./scripts/prepare-test-mount.sh --no-build`
- **Prepare mount only (no start):** `./scripts/prepare-test-mount.sh --prepare-only`  
  Then start manually: `TESTDATA_MOUNT=./testdata-mount docker compose -f docker-compose.test.yml up -d [--build]`
- **Preserve mount (e.g. keep leases):** `./scripts/prepare-test-mount.sh --no-clear --no-build`

App is at **http://localhost:8080**. Main config path in the container is `/data/dnsmasq-test.conf`; managed file is `zz-dnsmasq-webui.conf` in the same directory.

### Stop the harness

```bash
./scripts/prepare-test-mount.sh --stop
```

Runs `docker compose -f docker-compose.test.yml down` (stops and removes containers/networks). Does not change the mount directory.

### Clean (stop + clear mount)

```bash
./scripts/prepare-test-mount.sh --tidy
```

Stops the harness and deletes the contents of `testdata-mount/` so the next run starts from a clean sync of `testdata/`.

### Custom paths

- `--source DIR` — Source to sync from (default: `testdata`)
- `--mount DIR` — Mount directory (default: `testdata-mount`). The script exports `TESTDATA_MOUNT=./DIR` when you use `--mount`, so compose uses it.

Example: sync from a custom dir and start:

```bash
./scripts/prepare-test-mount.sh --source myfixtures --mount mymount
```

### Help

```bash
./scripts/prepare-test-mount.sh -h
```

## Unit tests

From the repo root:

```bash
dotnet test
```

Build only: `dotnet build`

## Test data

- **testdata/** — Source for the harness mount. Synced to `testdata-mount/` by `prepare-test-mount.sh`. Unit tests read from the copy in the test output dir. See **testdata/README** for the full layout.
- **testdata/dnsmasq-test.conf** — Harness main config (`/data/dnsmasq-test.conf`). Richer config: multiple `server=`, `addn-hosts=`, `address=`, `listen-address=`, `conf-dir=/data/dnsmasq.d`.
- **testdata/dnsmasq.d/** — Included configs (01-other.conf, 02-servers.conf, dhcp.conf). App creates `zz-dnsmasq-webui.conf` here when running.
- **testdata/hosts**, **testdata/hosts.extra** — Addn-hosts files so effective config has multiple addn-hosts; UI can show source per path.
