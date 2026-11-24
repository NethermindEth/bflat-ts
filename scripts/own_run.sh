#!/bin/bash
# SPDX-License-Identifier: MIT
# Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
#
# Stop what is left of one run of the suite after its dispatcher has exited,
# and nothing of another run on the same host: the engine processes (te_*)
# whose TE_RUN_DIR is the run directory, the agents that listen on the RCF
# ports of the run, and the containers those agents started (their --cidfile
# is in the working directory of the agent). It is not meant for a run whose
# dispatcher is still running.
#
# Usage (sourced): ts_stop_own_run <run dir> "<port> <port>"

ts_stop_own_run()
{
    local run_dir="$1"
    local ports="$2"
    local pids=""
    local p
    local env_file

    # Engine processes: Logger, RCF, Configurator, Tester. Most of them leave
    # the session of run.sh, but all keep its environment.
    for env_file in $(timeout 60 grep -lxzF "TE_RUN_DIR=${run_dir}" /proc/[0-9]*/environ 2>/dev/null) ; do
        p="${env_file#/proc/}"
        p="${p%/environ}"
        case "$(cat "/proc/$p/comm" 2>/dev/null)" in
            te_*) pids="${pids} $p" ;;
        esac
    done

    # Agents: each runs in a session of its own, with its RPC servers.
    local port
    local agent
    local cid
    local f
    for port in ${ports} ; do
        for agent in $(pgrep -f "/ta agt_[A-Za-z0-9_]+ ${port} ") ; do
            for f in "/proc/${agent}/cwd"/* ; do
                [ -f "$f" ] || continue
                cid="$(head -c 64 "$f" 2>/dev/null | tr -d '\0')"
                if [[ "${cid}" =~ ^[0-9a-f]{64}$ ]] ; then
                    docker kill "${cid}" >/dev/null 2>&1 || true
                fi
            done
            pkill -TERM -s "$(ps -o sid= -p "${agent}" | tr -d ' ')" 2>/dev/null || true
        done
    done

    [ -n "${pids}" ] && kill -TERM ${pids} 2>/dev/null
    sleep 5
    [ -n "${pids}" ] && kill -KILL ${pids} 2>/dev/null
    for port in ${ports} ; do
        for agent in $(pgrep -f "/ta agt_[A-Za-z0-9_]+ ${port} ") ; do
            pkill -KILL -s "$(ps -o sid= -p "${agent}" | tr -d ' ')" 2>/dev/null || true
        done
    done
    return 0
}
