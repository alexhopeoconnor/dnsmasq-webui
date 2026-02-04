# dnsmasq-webui

<div align="center">
  <img src="src/DnsmasqWebUI/wwwroot/logo.png" alt="dnsmasq-webui logo" width="200">
</div>

A self-hosted web UI for managing [dnsmasq](https://thekelleys.org.uk/dnsmasq/doc.html) configuration and hosts. It runs alongside your existing dnsmasq setup and edits the same config files; it does not replace dnsmasq.

---

## Purpose and features

**Purpose:** Point the UI at your dnsmasq config (and optionally hosts). Use the browser to view and edit config, managed hosts, and (if you use DHCP) reservations. Reload dnsmasq after changes via a configurable command.

**Features:**

- View and edit **dnsmasq config** (main config + conf-dir), with per-option source (which file or managed block).
- **Managed hosts file** (addn-hosts) editable in the UI; app writes a single managed hosts file and includes it via the managed config.
- **DHCP host entries** (reservations) if you use DHCP; edit in the UI, written into the managed config.
- **Reload dnsmasq** after config changes (configurable command, e.g. `systemctl reload dnsmasq` or `pkill -HUP -x dnsmasq`).
- Optional **status** and **recent logs** commands (e.g. systemctl/journalctl or custom scripts) shown on the Dnsmasq page.
- **Self-contained Linux binaries** per OS/arch (RID); no .NET install required. Can run in Docker or directly on the host.

<!-- SUGGESTED SCREENSHOT: MAIN CONFIG OR CONFIG EDITOR PAGE IN THE BROWSER -->

---

## Quick start and install

**Prerequisites:** Linux. dnsmasq (or a dnsmasq container) should already be set up. For the install script: `curl`, `jq`, `unzip`. (WSL may work but is untested.)

### Install from release

The recommended way to get a prebuilt binary is the **install script**. It picks the right build for your OS/arch, installs to a user or system directory, and can create a `dnsmasq-webui` symlink so you can run it from the terminal.

**Quick start:**

```bash
curl -sSL https://raw.githubusercontent.com/alexhopeoconnor/dnsmasq-webui/main/scripts/install.sh | sh
```

**From a git clone** (repo is detected from `git remote origin`):

```bash
./scripts/install.sh
```

By default the script installs to **`~/.local/share/dnsmasq-webui`** and creates a symlink at `~/.local/bin/dnsmasq-webui` if that directory exists and is writable (so you can run `dnsmasq-webui` if `~/.local/bin` is in your PATH). Then configure (see [Configuration](#configuration)) and run:

```bash
dnsmasq-webui
# or
~/.local/share/dnsmasq-webui/DnsmasqWebUI
```

**System-wide install (requires sudo):**

```bash
sudo ./scripts/install.sh --system
```

Installs to `/opt/dnsmasq-webui` and symlinks `/usr/local/bin/dnsmasq-webui`. Run with `dnsmasq-webui` from any terminal.

**Install as a systemd service (start on boot or at login):**

On systemd-based systems you can install a unit so the app runs as a service. Use `--service` alone for a **user** service (no sudo; starts when you log in), or with `--system` for a **system** service (requires sudo; starts at boot).

```bash
./scripts/install.sh --service                    # User service (no sudo)
sudo ./scripts/install.sh --system --service     # System service (starts at boot)
```

After install: configure the app (see [Configuration](#configuration)), then start the service: `systemctl --user start dnsmasq-webui` or `sudo systemctl start dnsmasq-webui`. User services run only when you’re logged in unless you enable linger: `loginctl enable-linger`.

**Update to latest release:**

```bash
./scripts/install.sh --update
```

Reinstalls the latest release into the default user directory (~/.local/share/dnsmasq-webui).

**Custom directory or specific version:**

```bash
./scripts/install.sh --dir /opt/dnsmasq-webui
./scripts/install.sh --version v1.0.0
```

**Installing from a fork:** Pass the repo explicitly: `./scripts/install.sh --repo owner/dnsmasq-webui` or set `GITHUB_REPO=owner/dnsmasq-webui`. The default repo is `alexhopeoconnor/dnsmasq-webui`.

**Configuration:** Set at least `Dnsmasq__MainConfigPath` to your main dnsmasq config. See [Configuration](#configuration) for all options.

<!-- SUGGESTED SCREENSHOT: TERMINAL SHOWING INSTALL.SH OUTPUT AND "Run: dnsmasq-webui" -->

### Switching to a different release

Re-run the install script with the same `--dir` (or default) and the desired `--version`. The script overwrites that directory with the chosen release. To go back to latest, use `--update` or omit `--version`.

---

## Running with dnsmasq in Docker

If you run dnsmasq in a container and want the UI in the **same** container, you have two main options.

### Option A: Use this repo’s image (app + dnsmasq in one image)

Build the image from this repo’s **Dockerfile**. It builds the app and installs dnsmasq in one image. The **entrypoint** (`scripts/entrypoint.sh`) starts dnsmasq when `DNSMASQ_CONF` is set, then runs the app.

**Build and run:**

```bash
docker build -t dnsmasq-webui .
docker run -d -p 8080:8080 \
  -e DNSMASQ_CONF=/data/dnsmasq.conf \
  -e Dnsmasq__MainConfigPath=/data/dnsmasq.conf \
  -e Dnsmasq__ReloadCommand="pkill -HUP -x dnsmasq" \
  -v /path/on/host:/data \
  --cap-add=NET_ADMIN \
  dnsmasq-webui
```

Mount your dnsmasq config directory as `/data` (or adjust paths and env vars). The app and dnsmasq both use files under `/data`.

### Option B: Build your own image (self-contained binary from a release)

The self-contained publish **works in a container** (it’s a Linux binary). You can add it to your own dnsmasq image by copying a release zip and unzipping into the image (no need to run the install script inside the container).

**Example Dockerfile** (extend your dnsmasq base):

```dockerfile
FROM your-dnsmasq-image:tag
# Install unzip if not present
RUN apt-get update && apt-get install -y --no-install-recommends unzip curl ca-certificates && rm -rf /var/lib/apt/lists/*

# Download and extract the release for linux-x64 (or match your base image: linux-musl-x64 for Alpine, etc.)
ARG RID=linux-x64
ARG VERSION=v1.0.0
RUN curl -sSL "https://github.com/alexhopeoconnor/dnsmasq-webui/releases/download/${VERSION}/dnsmasq-webui-${RID}.zip" -o /tmp/app.zip \
  && unzip -o /tmp/app.zip -d /app && rm /tmp/app.zip

WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Start dnsmasq (if your image doesn’t), then the app. Set DNSMASQ_CONF and Dnsmasq__* as needed.
COPY entrypoint.sh /entrypoint.sh
ENTRYPOINT ["/entrypoint.sh"]
```

Set `Dnsmasq__MainConfigPath`, `Dnsmasq__ReloadCommand`, and other options via environment variables or an `appsettings.json` in `/app`. Mount your config dir and expose 8080.

**Config summary for containers:**

| Env var | Example | Purpose |
|--------|--------|--------|
| `DNSMASQ_CONF` | `/data/dnsmasq.conf` | Path used by entrypoint to start dnsmasq (optional) |
| `Dnsmasq__MainConfigPath` | `/data/dnsmasq.conf` | Main dnsmasq config path (required) |
| `Dnsmasq__ReloadCommand` | `pkill -HUP -x dnsmasq` | Run after config changes |
| `Dnsmasq__StatusShowCommand` | `/app/dnsmasq-status.sh` | Optional status output (e.g. our script in test harness) |
| `ASPNETCORE_URLS` | `http://+:8080` | Port the app listens on |

See [Configuration](#configuration) for all options.

<!-- SUGGESTED SCREENSHOT: EXAMPLE DOCKERFILE SNIPPET OR DOCKER RUN COMMAND SHOWING DNSMASQ + APP IN ONE CONTAINER -->

---

## Configuration

The app is configured via **appsettings.json**, **environment variables**, and **command-line arguments**. Later sources override earlier ones (e.g. CLI overrides env over appsettings).

**Required:** Set at least the main dnsmasq config path.

| Source | Example |
|--------|--------|
| **Environment** | `Dnsmasq__MainConfigPath=/etc/dnsmasq.conf` |
| **appsettings.json** | `"Dnsmasq": { "MainConfigPath": "/etc/dnsmasq.conf" }` |
| **CLI** | `--Dnsmasq:MainConfigPath=/etc/dnsmasq.conf` |

**Dnsmasq options** (use `Dnsmasq__` prefix for env, `Dnsmasq:` section in JSON, `--Dnsmasq:OptionName=value` for CLI):

| Option | Description | Example |
|--------|-------------|---------|
| `MainConfigPath` | Path to main dnsmasq config (required) | `/etc/dnsmasq.conf` |
| `ManagedFileName` | Filename of the managed config file the app writes | `zz-dnsmasq-webui.conf` |
| `ManagedHostsFileName` | Filename of the managed hosts file the app writes | `zz-dnsmasq-webui.hosts` |
| `SystemHostsPath` | Optional path to system hosts (read-only in UI) | `/etc/hosts` |
| `ReloadCommand` | Command run after config changes | `systemctl reload dnsmasq` or `pkill -HUP -x dnsmasq` |
| `StatusCommand` | Optional: check if dnsmasq is running | `pgrep -x dnsmasq` |
| `StatusShowCommand` | Optional: full status output (e.g. systemctl status) | `systemctl status dnsmasq --no-pager` |
| `LogsCommand` | Optional: recent logs (e.g. journalctl) | `journalctl -u dnsmasq -n 100 --no-pager` |

**Host / URLs:** `ASPNETCORE_URLS=http://0.0.0.0:8080` or `--urls=http://0.0.0.0:8080` to bind to a specific address and port.

Place `appsettings.json` in the same directory as the executable (or use the default project paths when running with `dotnet run`).

---

## Scripts

All scripts live under `scripts/` and are intended for Linux (WSL may work but is untested).

### scripts/install.sh

**Purpose:** Download and install dnsmasq-webui from a GitHub release for the current OS/arch (RID). Supports install, **update** (reinstall latest to default dir), and switch-release (re-run with same `--dir` and different `--version`). Default install is user-writable (~/.local/share/dnsmasq-webui) with an optional symlink so you can run `dnsmasq-webui` from the terminal; use `--system` for a system-wide install to /opt (requires sudo). Use `--service` to install a systemd unit (user service without sudo, or system service with `--system`); only supported on systemd-based systems.

**Usage:** `./scripts/install.sh [OPTIONS]`

**Options:**

| Option | Description |
|--------|-------------|
| `--repo OWNER/REPO` | GitHub owner/repo (or set `GITHUB_REPO`). When run from a clone, repo is detected from `git remote origin`. Default repo: `alexhopeoconnor/dnsmasq-webui`. |
| `--list` | List available releases (tag, name, published_at) and exit. |
| `--version TAG` | Install from release TAG (e.g. v1.0.0). Default: latest. |
| `--update` | Reinstall latest into the default user directory (~/.local/share/dnsmasq-webui). |
| `--dir DIR` | Install into DIR instead of default. |
| `--system` | Install to /opt/dnsmasq-webui and symlink /usr/local/bin/dnsmasq-webui. Requires root (run with sudo). |
| `--service` | Install a systemd unit so the app runs as a service. With `--system`: system unit (requires root, starts at boot). Without: user unit (no sudo, starts at login). Only on systemd-based systems. |
| `-h`, `-?`, `--help` | Show help. |

**Examples:**

```bash
# Install latest (from clone: repo auto-detected; from curl: uses alexhopeoconnor/dnsmasq-webui by default, or use --repo)
./scripts/install.sh

# Update to latest release
./scripts/install.sh --update

# Install a specific version to default dir
./scripts/install.sh --version v1.0.0

# System-wide install (requires sudo)
sudo ./scripts/install.sh --system

# Install + systemd service (user service: no sudo; system service: starts at boot)
./scripts/install.sh --service
sudo ./scripts/install.sh --system --service

# Install to custom dir
./scripts/install.sh --dir /opt/dnsmasq-webui

# Switch to another release (overwrites that dir)
./scripts/install.sh --dir ~/.local/share/dnsmasq-webui --version v0.9.0

# List releases (from fork)
./scripts/install.sh --repo alexhopeoconnor/dnsmasq-webui --list
```

---

### scripts/publish-self-contained.sh

**Purpose:** Build a self-contained folder publish for Linux (no single-file). Used locally or in CI to produce the binaries that get zipped and attached to releases. Auto-detects RID when not specified.

**Usage:** `./scripts/publish-self-contained.sh [OPTIONS] [RID]`

**Options:**

| Option | Description |
|--------|-------------|
| `--trim` | Enable trimming (smaller output; can cause 404/routing issues with Blazor). |
| `--no-clean` | Skip clean before publish (faster; use only if same RID and options as last run). |
| `-h`, `-?`, `--help` | Show help. |

**RID:** Optional. If omitted, script picks one from `/etc/os-release` and `uname -m` (e.g. Ubuntu 24.04 → `ubuntu.24.04-x64`, Alpine → `linux-musl-x64`, else `linux-x64` / `linux-arm64` / `linux-arm`).

**Examples:**

```bash
# Publish for current machine (auto-detect RID)
./scripts/publish-self-contained.sh

# Publish for a specific RID
./scripts/publish-self-contained.sh ubuntu.24.04-x64
./scripts/publish-self-contained.sh linux-arm64
./scripts/publish-self-contained.sh linux-musl-x64

# Smaller build (not recommended for Blazor)
./scripts/publish-self-contained.sh --trim linux-x64

# Skip clean for a faster rebuild
./scripts/publish-self-contained.sh --no-clean ubuntu.24.04-x64
```

**Output:** `src/DnsmasqWebUI/bin/Release/net9.0/<RID>/publish/`

---

### scripts/prepare-test-mount.sh

**Purpose:** Prepare the testdata mount and optionally start or stop the Docker test harness (app + dnsmasq + DHCP clients). Syncs a source dir (default: `testdata`) into a mount dir (default: `testdata-mount`), cleans up previous test data, then runs `docker compose -f docker-compose.test.yml up -d` (or only prepares the mount with `--prepare-only`).

**Usage:** `./scripts/prepare-test-mount.sh [OPTIONS] [--]`

**Options:**

| Option | Description |
|--------|-------------|
| `--source DIR` | Source to copy from (default: `testdata`). |
| `--mount DIR` | Target mount directory (default: `testdata-mount`). Compose uses `TESTDATA_MOUNT`; script exports it when you use `--mount`. |
| `--no-clear` | Do not clear mount dir; only sync over existing contents (e.g. preserve leases between runs). |
| `--prepare-only` | Only prepare the mount; do not run docker compose. |
| `--build` | Pass `--build` to docker compose (rebuild images). Default: use existing images. |
| `--no-build` | Do not rebuild (default). |
| `--recreate` | Pass `--force-recreate` to docker compose. |
| `--stop` | Stop the harness: `docker compose down` (no prepare, no start). |
| `--tidy` | Stop the harness and clear the mount directory. |
| `-h`, `-?`, `--help` | Show help. |

**Examples:**

```bash
# Full run: clear mount, sync testdata, start containers (no rebuild)
./scripts/prepare-test-mount.sh

# Rebuild images then start
./scripts/prepare-test-mount.sh --build

# Only prepare the mount; start manually later
./scripts/prepare-test-mount.sh --prepare-only
# Then: TESTDATA_MOUNT=./testdata-mount docker compose -f docker-compose.test.yml up -d

# Preserve mount contents, sync over it, start
./scripts/prepare-test-mount.sh --no-clear

# Stop the harness
./scripts/prepare-test-mount.sh --stop

# Stop and clear the mount for a clean next run
./scripts/prepare-test-mount.sh --tidy
```

---

### scripts/dnsmasq-status.sh

**Purpose:** Simulates `systemctl status dnsmasq`-style output when run inside a container that has no systemd. Used by the test harness (and any similar setup) so the Dnsmasq page can show a status block. Uses `pgrep`/`ps` to show dnsmasq process info. No arguments.

**Usage:** Invoked by the app when `Dnsmasq__StatusShowCommand` points at this script (e.g. in `docker-compose.test.yml`).

---

### scripts/entrypoint.sh

**Purpose:** Container entrypoint when the app and dnsmasq run in the same container. If `DNSMASQ_CONF` is set, starts dnsmasq in the background with that config file; then exec’s the app (`dotnet DnsmasqWebUI.dll`). Used by this repo’s Dockerfile and by the test harness.

**Usage:** Set `DNSMASQ_CONF` to the path of the dnsmasq config file inside the container (e.g. `/data/dnsmasq-test.conf`). No CLI arguments.

<!-- SUGGESTED SCREENSHOT: TERMINAL WITH PREPARE-TEST-MOUNT.SH USAGE OR DOCKER COMPOSE UP OUTPUT -->

---

## Project structure and codebase overview

**Top-level:**

| Path | Description |
|------|-------------|
| `.github/workflows/` | GitHub Actions (e.g. release workflow: build on tag push, attach assets). |
| `scripts/` | Install, publish, test harness, entrypoint, and helper scripts. |
| `src/DnsmasqWebUI/` | Main ASP.NET Core Blazor app. |
| `src/DnsmasqWebUI.Tests/` | Unit tests. |
| `testdata/` | Fixtures and config for the Docker test harness. |
| `Dockerfile` | Multi-stage build: app + dnsmasq in one image. |
| `docker-compose.test.yml` | Test harness: app + dnsmasq + DHCP clients. |
| `DnsmasqWebUI.sln` | Solution file. |

**Codebase overview:**

- **Entry and config:** `Program.cs` wires the host; configuration (e.g. `Dnsmasq__*`) is in `Configuration/` (`DnsmasqOptions`, `ApplicationOptions`). Options are validated at startup.
- **Config and hosts:** Services in `Services/` and `Services/Abstractions/` read and write dnsmasq config and hosts. A **managed** config file and optional managed hosts file are written by the app; the main config must include the managed file (e.g. via a final `conf-file=` line). Caches (`ConfigSetCache`, `HostsCache`) keep in-memory snapshots and refresh on file changes or staleness.
- **Parsers and models:** `Parsers/` parses dnsmasq config lines and files (`DnsmasqConfIncludeParser`, `DnsmasqConfDirectiveParser`, etc.). Models in `Models/` represent config sets, config, sources, DHCP entries, hosts, leases.
- **API and UI:** `Controllers/` expose API endpoints; `Components/` contains Blazor Server components for config editor, hosts, DHCP, Dnsmasq status, and app settings. Static assets in `wwwroot/`.
- **Tests:** `DnsmasqWebUI.Tests` contains unit tests for parsers, config set service, config, and related logic. Run with `dotnet test`.

<!-- SUGGESTED SCREENSHOT: SOLUTION EXPLORER OR FOLDER TREE OF SRC/ -->

---

## Development and test harness

**Prerequisites:** .NET 9 SDK. Docker (and Docker Compose) for the test harness.

**Build and run locally:**

```bash
cd src/DnsmasqWebUI
dotnet run
```

Use `appsettings.json` or launch settings to set `Dnsmasq__MainConfigPath` and other options for your environment.

**Test harness:** Run the app and dnsmasq (plus DHCP clients) in Docker against a mounted testdata directory so you can develop and test without touching system dnsmasq.

1. From the repo root, run:
   ```bash
   ./scripts/prepare-test-mount.sh
   ```
   This clears the mount dir (by default `testdata-mount`), syncs `testdata/` into it, and starts the stack with `docker-compose.test.yml`. Use `--prepare-only` to only prepare the mount, then start compose manually; use `--build` after changing the app or Dockerfile.

2. Open the UI (e.g. http://localhost:8080). The app and dnsmasq use the mounted config; you can edit config/hosts in the UI and trigger reload.

3. Stop: `./scripts/prepare-test-mount.sh --stop`. Clean mount for next run: `./scripts/prepare-test-mount.sh --tidy`.

**Unit tests:**

```bash
dotnet test
```

Run from the repo root or the solution path. Tests do not require Docker.

<!-- SUGGESTED SCREENSHOT: BROWSER SHOWING UI AGAINST TEST HARNESS -->

<!-- SUGGESTED SCREENSHOT: TERMINAL WITH PREPARE-TEST-MOUNT.SH AND DOCKER COMPOSE UP -->

---

## License

This project is licensed under the **MIT License**. See [LICENSE](LICENSE) for the full text.

**dnsmasq** (the DNS/DHCP server this UI manages) is licensed under the **GNU GPL v2 or v3** (see [dnsmasq](https://thekelleys.org.uk/dnsmasq/doc.html)); this project is independent and does not include dnsmasq code.

To add or change the repository license (e.g. with GitHub CLI): `gh repo license view MIT > LICENSE` (or use the GitHub web UI: Add file → Create new file → Choose a license template).

---

## Disclaimer

This codebase was developed with the help of AI-assisted tooling (including LLM-based development tools). It has been reviewed and is maintained by humans. Use it at your own risk; no warranty is provided. See the [LICENSE](LICENSE) for terms.
