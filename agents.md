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

1. Run `docker compose down`, then sync `testdata/` → `testdata-mount/` (preserving mount contents unless you pass `--clear`). Sync skips `leases` and `zz-dnsmasq-webui.conf` / `zz-dnsmasq-webui.hosts` so rebuilds keep DHCP leases and UI-written managed files.
2. Optionally remove managed `*dnsmasq-webui*.conf` / `*dnsmasq-webui*.hosts` in the mount **only** if you pass `--reset-managed`.
3. Resolve the requested dnsmasq version (`latest` by default) to a concrete value when building images.
4. Run `docker compose -f docker-compose.test.yml up -d` (add `--build` / `--no-cache-build` when you need a rebuild).

- **Quick restart (no image rebuild):** `./scripts/prepare-test-mount.sh --no-build`
- **Force a fresh image build:** `./scripts/prepare-test-mount.sh --no-cache-build`
- **Pin dnsmasq version:** `./scripts/prepare-test-mount.sh --dnsmasq-version 2.91 --build`
- **Use distro package:** `./scripts/prepare-test-mount.sh --dnsmasq-version distro --build`
- **Prepare mount only (no start):** `./scripts/prepare-test-mount.sh --prepare-only`  
  Then start manually with the version printed by the script: `TESTDATA_MOUNT=./testdata-mount DNSMASQ_VERSION=<resolved-version> docker compose -f docker-compose.test.yml up -d [--build]`
- **Preserve mount (e.g. keep leases and managed config):** default; also use `./scripts/prepare-test-mount.sh --no-build` for a quick up without rebuilding images.
- **Drop managed conf/hosts only (keep leases):** `./scripts/prepare-test-mount.sh --reset-managed` (often combined with `--build`).

App is at **http://localhost:8080**. Main config path in the container is `/data/dnsmasq-test.conf`; managed file is `zz-dnsmasq-webui.conf` in the same directory. The app service has a healthcheck (`GET /healthz/ready`); DHCP client services use `depends_on: app: condition: service_healthy` so they start only after the app (and dnsmasq) is ready.

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
- `--dnsmasq-version V` — Dnsmasq version for the harness image: `latest` (default), `distro`, or an exact upstream version like `2.91`.
- `--reset-managed` — After sync, delete `*dnsmasq-webui*.conf` and `*dnsmasq-webui*.hosts` under the mount so the app recreates them.
- `--no-cache-build` — Force a fresh image build with `docker compose build --pull --no-cache` before start.

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
- **testdata/dnsmasq.d/** — Included configs (01-other.conf, 02-servers.conf, 03-more.conf, dhcp.conf). App creates `zz-dnsmasq-webui.conf` here when running.
- **testdata/hosts**, **testdata/hosts.extra** — Addn-hosts files so effective config has multiple addn-hosts; UI can show source per path.

## When adding new effective-config options

When adding a new dnsmasq option to the Effective Config UI (new key in `DnsmasqConfKeys`, section, kind map, parser/write behavior, tooltip, etc.):

1. **Option help:** Add the option key (lowercase, e.g. `no-round-robin`) to the `OPTION_KEYS` list in `scripts/extract-option-help.sh` (keep in sync with `EffectiveConfigSections`). Then run `./scripts/extract-option-help.sh` so `wwwroot/option-help/<key>.html` is generated if the option exists in the dnsmasq man page.
2. **Testdata (optional):** To show the option in the UI when using the harness or testdata, add a line to e.g. `testdata/dnsmasq.d/03-more.conf` and update `testdata/README` if needed.
