#!/bin/bash
# SPDX-License-Identifier: MIT
# Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
#
# Create a portable log bundle and a Bublik-ready tar archive from
# the raw TE log produced by the most recent test run.
#
# Usage:
#   ./scripts/bundle.sh [<output_dir>]
#
# Produces in <output_dir> (defaults to the current directory):
#   raw_log_bundle.tpxz   -- TE log bundle (can be opened with log.sh)
#   te_logs.tar           -- tar wrapping meta_data.json + raw_log_bundle.tpxz
#                            (copy this file to Bublik's incoming directory)
#
# Environment variables:
#   TE_LOG_RAW   path to the raw log (default: ./tmp_raw_log)
#   TE_META      path to meta_data.json (default: ./meta_data.json)

set -eo pipefail

RUNDIR="$(pwd -P)"
export TS_TOPDIR="$(cd "$(dirname "$(which "$0")")"/.. ; pwd -P)"

# Source env.sh if present (sets TE_BASE when test-environment is a sibling).
if [ -e "${RUNDIR}/scripts/env.sh" ] ; then
    # shellcheck source=/dev/null
    source "${RUNDIR}/scripts/env.sh"
elif [ -e "${TS_TOPDIR}/scripts/env.sh" ] ; then
    # shellcheck source=/dev/null
    source "${TS_TOPDIR}/scripts/env.sh"
fi

if [ -z "${TE_BASE:-}" ] ; then
    echo "error: TE_BASE is not set and could not be guessed." >&2
    echo "       Set TE_BASE to the test-environment source directory." >&2
    exit 1
fi

# Load TE path helpers (sets PATH, LD_LIBRARY_PATH, TE_INSTALL, …).
# shellcheck source=/dev/null
. "${TE_BASE}/scripts/guess.sh"

# ── Resolve paths ─────────────────────────────────────────────────────────────
RAW_LOG="${TE_LOG_RAW:-${RUNDIR}/tmp_raw_log}"
META="${TE_META:-${RUNDIR}/meta_data.json}"
OUT_DIR="${1:-${RUNDIR}}"

BUNDLE="${OUT_DIR}/raw_log_bundle.tpxz"
TAR="${OUT_DIR}/te_logs.tar"

# ── Validate inputs ───────────────────────────────────────────────────────────
if [ ! -f "${RAW_LOG}" ] ; then
    echo "error: raw log not found at '${RAW_LOG}'." >&2
    echo "       Run the test suite first, or set TE_LOG_RAW to the correct path." >&2
    exit 1
fi

if [ ! -f "${META}" ] ; then
    echo "error: meta_data.json not found at '${META}'." >&2
    echo "       Run the test suite first, or set TE_META to the correct path." >&2
    exit 1
fi

mkdir -p "${OUT_DIR}"

# ── Step 1: create .tpxz bundle ───────────────────────────────────────────────
echo "Creating log bundle:"
echo "  raw log : ${RAW_LOG}"
echo "  bundle  : ${BUNDLE}"

rgt-log-bundle-create --raw-log="${RAW_LOG}" --bundle="${BUNDLE}"

echo "Bundle created: ${BUNDLE}"

# ── Step 2: wrap into te_logs.tar for Bublik ──────────────────────────────────
# Bublik expects a tar containing exactly:
#   meta_data.json
#   raw_log_bundle.tpxz
#
# We use a temporary staging directory so the tar entries have bare names
# (no path prefixes) regardless of where the source files live.
STAGE="$(mktemp -d)"
trap 'rm -rf "${STAGE}"' EXIT

ln -s "$(realpath "${META}")"   "${STAGE}/meta_data.json"
ln -s "$(realpath "${BUNDLE}")" "${STAGE}/raw_log_bundle.tpxz"

echo "Creating Bublik archive:"
echo "  meta    : ${META}"
echo "  bundle  : ${BUNDLE}"
echo "  tar     : ${TAR}"

tar -cf "${TAR}" -C "${STAGE}" --dereference \
    meta_data.json raw_log_bundle.tpxz

echo "Done."
echo ""
echo "Copy to Bublik incoming directory:"
echo "  scp ${TAR} <bublik-host>:/srv/bublik/logs/incoming/"