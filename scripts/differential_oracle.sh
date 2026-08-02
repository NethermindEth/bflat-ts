#!/bin/bash
# Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
#
# Differential oracle: every C# test in the suite must pass on the host
# (reference) .NET runtime. The suite asserts that a test exits 0 under
# bflat + zkVM; this script asserts the same program exits 0 on stock .NET.
# Both gates green means the zkVM runtime behaves like the reference for
# everything the suite covers; a test that fails here encodes wrong .NET
# semantics (or host-specific behavior that must be declared in
# scripts/oracle_expectations.txt).
#
# Tests are compiled with the SDK's csc.dll directly (one in-process Roslyn
# invocation per test, no MSBuild) and executed with `dotnet exec`, in
# parallel. The run environment mirrors the guest configuration: invariant
# globalization and UTC.
#
# Usage: scripts/differential_oracle.sh [test-name-filter ...]
# Exits non-zero on any compile failure, unexpected test failure, or a
# stale expectations entry.

set -euo pipefail

TOPDIR=$(cd "$(dirname "$0")/.." && pwd)
cd "${TOPDIR}"

EXPECTATIONS="${TOPDIR}/scripts/oracle_expectations.txt"
WORK="${ORACLE_WORK:-$(mktemp -d)}"
JOBS=${ORACLE_JOBS:-$(getconf _NPROCESSORS_ONLN)}
TIMEOUT_S=${ORACLE_TIMEOUT_S:-60}

# --- locate the SDK's Roslyn and the shared framework -----------------------
if ! command -v dotnet >/dev/null; then
    echo "error: dotnet not found in PATH" >&2
    exit 1
fi

SDK_LINE=$(dotnet --list-sdks | tail -1)
SDK_VER=${SDK_LINE%% *}
SDK_BASE=$(echo "${SDK_LINE}" | sed 's/.*\[\(.*\)\].*/\1/')
CSC="${SDK_BASE}/${SDK_VER}/Roslyn/bincore/csc.dll"

FW_LINE=$(dotnet --list-runtimes | grep '^Microsoft\.NETCore\.App ' | tail -1)
FW_VER=$(echo "${FW_LINE}" | awk '{print $2}')
FW_DIR=$(echo "${FW_LINE}" | sed 's/.*\[\(.*\)\].*/\1/')/"${FW_VER}"

if [ ! -f "${CSC}" ]; then
    echo "error: csc.dll not found at ${CSC}" >&2
    exit 1
fi

echo "SDK ${SDK_VER}, framework ${FW_VER}, ${JOBS} parallel jobs"
mkdir -p "${WORK}"

# Reference every framework assembly. The facades (System.Runtime & co)
# only forward types, so System.Private.CoreLib must be referenced too for
# the forwards to resolve.
RSP="${WORK}/refs.rsp"
: > "${RSP}"
for dll in "${FW_DIR}"/*.dll; do
    echo "-r:\"${dll}\"" >> "${RSP}"
done

cat > "${WORK}/runtimeconfig.json" <<EOF
{
  "runtimeOptions": {
    "tfm": "net${FW_VER%%.*}.0",
    "framework": { "name": "Microsoft.NETCore.App", "version": "${FW_VER}" }
  }
}
EOF

# --- expectations -----------------------------------------------------------
# Lines: <package>/<test> <mode> # reason
# Modes: compile-only (build, do not run: needs the zkVM environment),
#        skip         (do not build: cannot compile on the host),
#        exit=<N>     (run, expect exit code N instead of 0).
declare -A MODE
if [ -f "${EXPECTATIONS}" ]; then
    while read -r name mode _; do
        case "${name}" in ''|'#'*) continue ;; esac
        MODE["${name}"]="${mode}"
    done < "${EXPECTATIONS}"
fi

# --- collect tests ----------------------------------------------------------
TESTS=()
for cs in bflat-ts/*/cs/*.cs; do
    pkg=$(basename "$(dirname "$(dirname "${cs}")")")
    name="${pkg}/$(basename "${cs}" .cs)"
    if [ "$#" -gt 0 ]; then
        keep=0
        for f in "$@"; do case "${name}" in *"${f}"*) keep=1 ;; esac; done
        [ "${keep}" = 1 ] || continue
    fi
    TESTS+=("${name}:${cs}")
done
echo "${#TESTS[@]} test(s) to check"

# --- per-test worker --------------------------------------------------------
run_one() {
    local entry="$1"
    local name="${entry%%:*}" cs="${entry#*:}"
    local safe="${name//\//__}"
    local dir="${WORK}/t/${safe}"
    local mode="${MODE_LOOKUP:-}"

    mkdir -p "${dir}"

    if [ "${mode}" = "skip" ]; then
        echo "SKIP ${name}" > "${dir}/result"
        return 0
    fi

    if ! dotnet "${CSC}" -nologo -optimize+ -nullable:disable -unsafe \
            "@${RSP}" -out:"${dir}/test.dll" -target:exe "${cs}" \
            > "${dir}/csc.log" 2>&1; then
        echo "COMPILE-FAIL ${name}" > "${dir}/result"
        return 0
    fi

    if [ "${mode}" = "compile-only" ]; then
        echo "COMPILE-ONLY ${name}" > "${dir}/result"
        return 0
    fi

    cp "${WORK}/runtimeconfig.json" "${dir}/test.runtimeconfig.json"

    local expected=0
    case "${mode}" in exit=*) expected="${mode#exit=}" ;; esac

    # The environment mirrors what the guest sees: invariant globalization
    # and predefined-cultures-only (pal's getenv answers "1" for the CoreLib
    # feature flags), one processor (the zisk_subst ProcessorCount
    # substitution), UTC (the invariant timezone).
    local rc=0
    TZ=UTC \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 \
    DOTNET_SYSTEM_GLOBALIZATION_PREDEFINED_CULTURES_ONLY=1 \
    DOTNET_PROCESSOR_COUNT=1 \
    DOTNET_TieredCompilation=0 \
        timeout "${TIMEOUT_S}" dotnet exec "${dir}/test.dll" \
        > "${dir}/stdout.log" 2> "${dir}/stderr.log" < /dev/null || rc=$?

    if [ "${rc}" = "${expected}" ]; then
        echo "PASS ${name}" > "${dir}/result"
    elif [ "${rc}" = 124 ]; then
        echo "TIMEOUT ${name}" > "${dir}/result"
    else
        echo "FAIL(${rc},expected ${expected}) ${name}" > "${dir}/result"
    fi
}
export -f run_one
export WORK RSP CSC TIMEOUT_S

# --- run in parallel --------------------------------------------------------
for entry in "${TESTS[@]}"; do
    name="${entry%%:*}"
    printf 'MODE_LOOKUP=%q run_one %q\n' "${MODE["${name}"]:-}" "${entry}"
done | xargs -P "${JOBS}" -I {} bash -c {}

# --- aggregate --------------------------------------------------------------
pass=0; skipped=0; failed=0
fail_names=()
for entry in "${TESTS[@]}"; do
    name="${entry%%:*}"
    safe="${name//\//__}"
    result=$(cat "${WORK}/t/${safe}/result" 2>/dev/null || echo "MISSING ${name}")
    case "${result}" in
        PASS*) pass=$((pass + 1)) ;;
        SKIP*|COMPILE-ONLY*) skipped=$((skipped + 1)) ;;
        *) failed=$((failed + 1)); fail_names+=("${result}") ;;
    esac
done

# Expectations must stay minimal: an entry naming a test that no longer
# exists is an error.
stale=0
for name in "${!MODE[@]}"; do
    pkg="${name%%/*}" test="${name##*/}"
    if [ ! -f "bflat-ts/${pkg}/cs/${test}.cs" ]; then
        echo "error: stale expectations entry: ${name}"
        stale=1
    fi
done

echo
echo "oracle: ${pass} passed, ${skipped} skipped/compile-only, ${failed} failed"
if [ "${failed}" -gt 0 ]; then
    printf '%s\n' "${fail_names[@]}" | sort | while read -r line; do
        echo "  ${line}"
    done
    exit 1
fi
[ "${stale}" = 0 ] || exit 1
echo "differential oracle: host .NET agrees with the suite"
