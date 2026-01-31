#!/bin/sh
# Simulates "systemctl status dnsmasq" output when run in a container without systemd.
# Uses ps/pgrep so the UI looks familiar. Usage: dnsmasq-status.sh

pid=$(pgrep -x dnsmasq)
if [ -z "$pid" ]; then
  echo "● dnsmasq.service - dnsmasq - A lightweight DHCP and caching DNS server"
  echo "     Loaded: (container, no systemd)"
  echo "     Active: inactive (dead)"
  echo ""
  exit 0
fi

# One ps + awk: parse pid,user,etime,rss,args and print systemctl-style (rss in KB -> MB)
ps -o pid=,user=,etime=,rss=,args= -p "$pid" 2>/dev/null | awk '{
  pid=$1; user=$2; etime=$3; rss=$4
  args=""
  for (i=5;i<=NF;i++) args = args (i>5?" ":"") $i
  rss_mb = (rss+0) / 1024
  if (rss_mb < 0.1) rss_mb = 0.0
  printf "● dnsmasq.service - dnsmasq - A lightweight DHCP and caching DNS server\n"
  printf "     Loaded: loaded (container, no systemd)\n"
  printf "     Active: active (running) since container start\n"
  printf "   Main PID: %s (dnsmasq)\n", pid
  printf "      Tasks: 1 (limit: unknown)\n"
  printf "     Memory: %.1fM\n", rss_mb
  printf "        CPU: %s\n", etime
  printf "     CGroup: (container)\n"
  printf "             └─%s %s\n", pid, args
}'
exit 0
