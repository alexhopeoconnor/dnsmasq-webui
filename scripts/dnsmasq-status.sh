#!/bin/sh
# Simulates "systemctl status dnsmasq" output when run in a container without systemd.
# Uses ps/pgrep. Memory is RSS. Uptime line omitted; Active line has break after semicolon with continuation indented.

pid=$(pgrep -x dnsmasq)
if [ -z "$pid" ]; then
  echo "● dnsmasq.service - dnsmasq - A lightweight DHCP and caching DNS server"
  echo "     Loaded: (container, no systemd)"
  echo "     Active: inactive (dead)"
  echo ""
  exit 0
fi

# Process start time for "Active: ... since ..." (optional; fallback if ps has no lstart)
lstart=$(ps -o lstart= -p "$pid" 2>/dev/null) || lstart=""

# pid, user, etime, rss, args -> systemctl-style output. rss in KB -> MB.
# etime is [[dd-]hh:]mm:ss (e.g. 12:45, 1:12:45, 6-11:22:33)
ps -o pid=,user=,etime=,rss=,args= -p "$pid" 2>/dev/null | awk -v lstart="$lstart" 'function etime_ago(et,   n, t, x, days, hours, mins) {
  days = 0; hours = 0; mins = 0
  n = split(et, t, "-")
  if (n >= 2) {
    days = t[1]+0
    split(t[2], x, ":")
    if (length(x) >= 3) { hours = x[1]+0; mins = x[2]+0 }
    else if (length(x) == 2) { hours = 0; mins = x[1]+0 }
  } else {
    split(et, x, ":")
    if (length(x) == 3) { hours = x[1]+0; mins = x[2]+0 }
    else if (length(x) == 2) { mins = x[1]+0 }
  }
  if (days > 0) return days " day" (days == 1 ? "" : "s") " ago"
  if (hours > 0) return hours " hour" (hours == 1 ? "" : "s") " ago"
  if (mins > 0) return mins " min ago"
  return "just now"
}
function fmt_since(s,   n, a, mon_num, day_pad) {
  n = split(s, a, " ")
  if (n < 5) return s
  m = (index("JanFebMarAprMayJunJulAugSepOctNovDec", a[2]) + 2) / 3
  mon_num = m; if (m < 10) mon_num = "0" m
  day_pad = a[3]; if (length(a[3]) == 1) day_pad = "0" a[3]
  return sprintf("%s %s-%s-%s %s", a[1], a[5], mon_num, day_pad, a[4])
}
{
  pid=$1; user=$2; etime=$3; rss=$4
  args=""
  for (i=5;i<=NF;i++) args = args (i>5?" ":"") $i
  rss_mb = (rss+0) / 1024
  if (rss_mb < 0.1) rss_mb = 0.0

  # Active line: break after semicolon; continuation (e.g. "4 min ago") on next line, indented to align after "Active: "
  if (lstart != "") {
    since = fmt_since(lstart)
    ago = etime_ago(etime)
    active_line = "     Active: active (running) since " since ";\n             " ago
  } else {
    active_line = "     Active: active (running) since container start"
  }

  printf "● dnsmasq.service - dnsmasq - A lightweight DHCP and caching DNS server\n"
  printf "     Loaded: loaded (container, no systemd)\n"
  printf "%s\n", active_line
  printf "   Main PID: %s (dnsmasq)\n", pid
  printf "     Memory: %.1fM\n", rss_mb
  printf "     CGroup: (container)\n"
  printf "             └─%s %s\n", pid, args
}'
exit 0
