#!/bin/sh
# If DNSMASQ_CONF is set, run dnsmasq in the background (same container as the app).
# Container main process is the app (exec below).
if [ -n "$DNSMASQ_CONF" ]; then
  # Ensure dnsmasq log file exists and is writable (dnsmasq may drop to dnsmasq user)
  touch /data/dnsmasq.log 2>/dev/null && chmod a+rw /data/dnsmasq.log 2>/dev/null || true
  dnsmasq -k --conf-file="$DNSMASQ_CONF" &
fi
exec dotnet DnsmasqWebUI.dll
