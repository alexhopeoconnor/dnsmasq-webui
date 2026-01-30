# Test data for docker-compose.test.yml

- **hosts** – Shared by app and dnsmasq (read/write by app, `addn-hosts` for dnsmasq).
- **dnsmasq.conf** – Sample main config (reference only; not used by the test containers).
- **dnsmasq-test.conf** – Config for dnsmasq when run inside the app container (DHCP-only, paths under `/data`).
- **dnsmasq.d/dhcp.conf** – DHCP static hosts; app edits this, dnsmasq loads it via `conf-dir`.
- **dnsmasq.d/01-other.conf** – Other snippet (not managed by app).
- **leases** – Empty at start. dnsmasq creates and populates it when the dhcp-client container gets a lease. App reads it (read-only).

All of this is bind-mounted as `/data` in the **app** container; the app runs as the main process and dnsmasq runs in the background in the same container.
