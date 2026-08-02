#!/bin/bash
# Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
#
# Deductive verification gate: prove the ACSL contracts of the pure helper
# sources with Frama-C/WP (plus generated RTE guards). The verified sources
# are TE-free by design (see bflat-ts/lib/ts_pure.h), so no TE build tree is
# needed.
#
# Usage: scripts/frama_c_check.sh
# Exits non-zero when Frama-C fails or any proof obligation stays unproved.

set -euo pipefail

TOPDIR=$(cd "$(dirname "$0")/.." && pwd)
cd "${TOPDIR}"

VERIFIED_SOURCES=(
    bflat-ts/lib/ts_pure.c
)

if ! command -v frama-c >/dev/null; then
    echo "error: frama-c not found in PATH" >&2
    exit 1
fi

echo "Using $(frama-c --version)"

# WP drives SMT provers through why3, which needs a one-time prover
# detection pass (idempotent). Debian bookworm ships no alt-ergo, so the
# default prover is z3; override with WP_PROVER if needed.
WP_PROVER=${WP_PROVER:-z3}
why3 config detect >/dev/null 2>&1 || true

log=$(mktemp)
trap 'rm -f "${log}"' EXIT

frama-c -wp -wp-rte -wp-prover "${WP_PROVER}" -wp-timeout 30 \
        "${VERIFIED_SOURCES[@]}" 2>&1 | tee "${log}"

# WP prints "Proved goals: X / Y"; require every scheduled goal proved.
proved=$(sed -n 's/.*Proved goals:[[:space:]]*\([0-9]*\)[[:space:]]*\/[[:space:]]*\([0-9]*\).*/\1 \2/p' "${log}" | tail -1)

if [ -z "${proved}" ]; then
    echo "error: could not find 'Proved goals' in Frama-C output" >&2
    exit 1
fi

read -r ok total <<< "${proved}"

if [ "${total}" -eq 0 ]; then
    echo "error: no proof obligations were generated" >&2
    exit 1
fi

if [ "${ok}" -ne "${total}" ]; then
    echo "error: only ${ok} of ${total} proof obligations proved" >&2
    exit 1
fi

echo "Frama-C/WP: all ${total} proof obligations proved"
