#!/bin/sh
# If DNSMASQ_CONF is set, run dnsmasq in the background (same container as the app).
# Container main process is the app (exec below).
if [ -n "$DNSMASQ_CONF" ]; then
  dnsmasq -k --conf-file="$DNSMASQ_CONF" &
fi
exec dotnet DnsmasqWebUI.dll
