#!/bin/bash
# Stop what is left of the last run started from this directory, and nothing
# of another run on the same host (scripts/own_run.sh).
export TS_TOPDIR="$(cd "$(dirname "$(which "$0")")"/.. ; pwd -P)"
cd "$TS_TOPDIR"
sudo bash -c ". '${TS_TOPDIR}/scripts/own_run.sh'; ts_stop_own_run '${TS_TOPDIR}' '$(cat "${TS_TOPDIR}/rcf_ports" 2>/dev/null)'"
rm -f ta.*
