#!/usr/bin/env bash
# Extract per-option HTML from the dnsmasq man page for use as field help in the web UI.
#
# Runs a temporary Python (BeautifulSoup) container to fetch the page, parse the OPTIONS
# <DL>/<DT>/<DD> blocks, and write one .html file per option. The container is removed
# when done. If the page URL or structure changes, pass a new --url.
#
# Usage:
#   ./scripts/extract-option-help.sh [--url URL] [--output-dir DIR]
#
# Requires: Docker (python:3-slim image will be pulled if missing)

set -e

DEFAULT_URL="https://thekelleys.org.uk/dnsmasq/docs/dnsmasq-man.html"
DEFAULT_OUTPUT_DIR="src/DnsmasqWebUI/wwwroot/option-help"
URL="$DEFAULT_URL"
OUTPUT_DIR="$DEFAULT_OUTPUT_DIR"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --url)        URL="$2"; shift 2 ;;
    --output-dir) OUTPUT_DIR="$2"; shift 2 ;;
    *) echo "Unknown option: $1" >&2; exit 1 ;;
  esac
done

[[ "$OUTPUT_DIR" != /* ]] && OUTPUT_DIR="$PWD/$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

SCRIPT_FILE="$(mktemp)"
trap 'rm -f "$SCRIPT_FILE"' EXIT

# Embed Python script (uses requests + BeautifulSoup; runs inside container)
cat << 'PYTHON_SCRIPT' > "$SCRIPT_FILE"
import os
import re
import sys

try:
    import requests
    from bs4 import BeautifulSoup
except ImportError:
    sys.stderr.write("Installing requests and beautifulsoup4...\n")
    import subprocess
    subprocess.check_call([sys.executable, "-m", "pip", "install", "-q", "requests", "beautifulsoup4"])
    import requests
    from bs4 import BeautifulSoup

OUT_DIR = "/out"
URL = os.environ.get("URL", "https://thekelleys.org.uk/dnsmasq/docs/dnsmasq-man.html")

# One per line; keep in sync with DnsmasqOptionTooltips / EffectiveConfigSections. server + local = one UI row.
OPTION_KEYS = [
    "no-hosts",
    "addn-hosts",
    "hostsdir",
    "read-ethers",
    "server",
    "local",
    "rev-server",
    "address",
    "resolv-file",
    "no-resolv",
    "domain-needed",
    "port",
    "log-queries",
    "strict-order",
    "all-servers",
    "auth-ttl",
    "edns-packet-max",
    "query-port",
    "port-limit",
    "min-port",
    "max-port",
    "dns-loop-detect",
    "stop-dns-rebind",
    "rebind-localhost-ok",
    "clear-on-reload",
    "expand-hosts",
    "bogus-priv",
    "rebind-domain-ok",
    "bogus-nxdomain",
    "ignore-address",
    "alias",
    "filter-rr",
    "filterwin2k",
    "filter-A",
    "filter-AAAA",
    "localise-queries",
    "fast-dns-retry",
    "ipset",
    "nftset",
    "domain",
    "cname",
    "mx-host",
    "srv-host",
    "ptr-record",
    "txt-record",
    "naptr-record",
    "host-record",
    "dynamic-host",
    "interface-name",
    "mx-target",
    "localmx",
    "selfmx",
    "dhcp-authoritative",
    "dhcp-rapid-commit",
    "leasefile-ro",
    "dhcp-script",
    "dhcp-leasefile",
    "dhcp-lease-max",
    "dhcp-ttl",
    "dhcp-range",
    "dhcp-host",
    "dhcp-option",
    "dhcp-option-force",
    "dhcp-match",
    "dhcp-mac",
    "dhcp-name-match",
    "dhcp-ignore-names",
    "dhcp-hostsfile",
    "dhcp-optsfile",
    "dhcp-hostsdir",
    "dhcp-boot",
    "dhcp-ignore",
    "dhcp-vendorclass",
    "dhcp-userclass",
    "ra-param",
    "slaac",
    "enable-tftp",
    "tftp-secure",
    "tftp-no-fail",
    "tftp-no-blocksize",
    "tftp-root",
    "pxe-prompt",
    "pxe-service",
    "dnssec",
    "dnssec-check-unsigned",
    "proxy-dnssec",
    "trust-anchor",
    "cache-rr",
    "cache-size",
    "local-ttl",
    "no-negcache",
    "neg-ttl",
    "max-ttl",
    "max-cache-ttl",
    "min-cache-ttl",
    "no-poll",
    "bind-interfaces",
    "bind-dynamic",
    "log-debug",
    "log-async",
    "local-service",
    "interface",
    "listen-address",
    "except-interface",
    "auth-server",
    "no-dhcp-interface",
    "no-dhcpv4-interface",
    "no-dhcpv6-interface",
    "pid-file",
    "user",
    "group",
    "log-facility",
    "enable-dbus",
    "enable-ubus",
    "enable-ra",
    "log-dhcp",
    "keep-in-foreground",
    "no-daemon",
    "conntrack",
]

long_opt_re = re.compile(r"--([a-z][a-z0-9-]*)(?:=[^,\s]*)?", re.IGNORECASE)

def option_names_from_dt(dt):
    return [m.group(1).lower() for m in long_opt_re.finditer(dt.get_text())]

def main():
    r = requests.get(URL, timeout=30)
    r.raise_for_status()
    soup = BeautifulSoup(r.text, "html.parser")
    h2 = None
    for el in soup.find_all("h2"):
        if el.get_text(strip=True).upper() == "OPTIONS":
            h2 = el
            break
    if not h2:
        sys.stderr.write("Could not find OPTIONS H2\n")
        sys.exit(1)
    dl = h2.find_next("dl")
    if not dl:
        sys.stderr.write("Could not find DL after OPTIONS\n")
        sys.exit(1)
    # Build option -> (dt_html, dd_html) from raw HTML; split by <dt> so we get one block per option.
    raw_dl = str(dl)
    option_to_block = {}
    blocks = re.split(r"<dt\b", raw_dl, flags=re.IGNORECASE)
    for block in blocks[1:]:
        p_dt = block.find("</dt>") if "</dt>" in block.lower() else len(block) + 1
        m_dd = re.search(r"<dd\b", block, re.IGNORECASE)
        p_dd = m_dd.start() if m_dd else len(block) + 1
        end_pos = min(p_dt, p_dd)
        if end_pos > len(block):
            continue
        dt_part = block[:end_pos].strip()
        rest = block[end_pos:]
        if rest.lower().startswith("</dt>"):
            rest = rest[6:].lstrip()
        if ">" in dt_part:
            dt_part = dt_part.split(">", 1)[1]
        dt_html = "<dt>" + dt_part + "</dt>"
        dd_match = re.search(r"<dd\b[^>]*>(.*)", rest, re.DOTALL | re.IGNORECASE)
        dd_body = dd_match.group(1) if dd_match else ""
        dd_body = re.split(r"</dd>|<dt\s|<dt>", dd_body, flags=re.IGNORECASE)[0].strip()
        dd_html = "<dd>" + dd_body + "</dd>" if dd_body else "<dd></dd>"
        soup_dt = BeautifulSoup(dt_html, "html.parser").find("dt")
        if not soup_dt:
            continue
        for n in option_names_from_dt(soup_dt):
            if n not in option_to_block:
                option_to_block[n] = (dt_html, dd_html)
    os.makedirs(OUT_DIR, exist_ok=True)
    written = 0
    for key in OPTION_KEYS:
        norm = key.lower()
        if norm not in option_to_block:
            continue
        dt_html, dd_html = option_to_block[norm]
        frag = "\n".join([dt_html, dd_html])
        safe = re.sub(r"[^a-z0-9-]", "", norm) or key
        path = os.path.join(OUT_DIR, f"{safe}.html")
        with open(path, "w", encoding="utf-8") as f:
            f.write(frag)
        written += 1
    print(f"Wrote {written} files to {OUT_DIR}")

if __name__ == "__main__":
    main()
PYTHON_SCRIPT

echo "Fetching man page and extracting option help (Docker python:3-slim) ..."
docker run --rm \
  -v "$OUTPUT_DIR:/out" \
  -v "$SCRIPT_FILE:/script.py:ro" \
  -e "URL=$URL" \
  -e "PIP_ROOT_USER_ACTION=ignore" \
  python:3-slim \
  sh -c "pip install -q requests beautifulsoup4 && python /script.py"

echo "Done. Files in $OUTPUT_DIR"
