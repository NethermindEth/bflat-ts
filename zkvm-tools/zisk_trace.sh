#!/usr/bin/env bash
# SPDX-License-Identifier: MIT
# Copyright (c) 2025-2026 Demerzel Solutions Limited (Nethermind)
#
# Run ziskemu and annotate every pc= value in its output with a
# function name and hex offset from the ELF symbol table.
#
# Usage:
#   zisk_trace.sh [--profile=FILE] <full ziskemu command line>
#
# Example:
#   zisk_trace.sh ziskemu -e ./my_program -i input.bin
#   zisk_trace.sh --profile=report.html ziskemu -e ./my_program

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BIN_DIR="${SCRIPT_DIR}/bin"

fail() {
    echo "error: $*" >&2
    exit 1
}

if command -v readelf &>/dev/null; then
    READELF_CMD="readelf"
elif command -v llvm-readelf &>/dev/null; then
    READELF_CMD="llvm-readelf"
else
    fail "neither readelf nor llvm-readelf found in PATH"
fi

[[ $# -lt 1 ]] && fail "usage: $(basename "$0") [--profile=FILE] <ziskemu command line>"

# Strip --profile=... from args before passing to ziskemu.
PROFILE_OUT=""
args=()
for arg in "$@"; do
    case "${arg}" in
        --profile=*) PROFILE_OUT="${arg#--profile=}" ;;
        *) args+=("${arg}") ;;
    esac
done

[[ ${#args[@]} -lt 1 ]] && fail "no ziskemu command given after flags"

# Locate the ELF path passed via -e / --elf; needed separately both by
# zisk_decoder (symbol table) and zisk_profile.py (symbol parsing).
ELF_PATH=""
for i in "${!args[@]}"; do
    if [[ "${args[$i]}" == "-e" || "${args[$i]}" == "--elf" ]]; then
        next=$(( i + 1 ))
        [[ $next -lt ${#args[@]} ]] && ELF_PATH="${args[$next]}"
        break
    fi
done

[[ -z "${ELF_PATH}" ]] && fail "pass the ELF with -e / --elf"
[[ -f "${ELF_PATH}" ]] || fail "ELF file not found: ${ELF_PATH}"

READELF_FILE="$(mktemp)"
trap 'rm -f "${READELF_FILE}"' EXIT

echo "[1/2] Reading symbol table from: ${ELF_PATH}" >&2
"${READELF_CMD}" -SW -s "${ELF_PATH}" >"${READELF_FILE}"

# Emu::print_regs() lines are register dumps unrelated to control flow.
# Emu::run() lines carry ctx.pc= and form the actual execution trace.
#
# In profile mode the filtered stream is split with tee: one copy goes to
# zisk_decoder (annotated trace on stdout), the other is consumed by
# zisk_profile.py via its stdin — no intermediate files at any point.
echo "[2/2] Running ziskemu..." >&2

if [[ -n "${PROFILE_OUT}" ]]; then
    { "${args[@]}" 2>&1 || true; } \
        | grep -v 'Emu::print_regs()' \
        | tee >(python3 "${SCRIPT_DIR}/zisk_profile.py" \
                    --elf "${ELF_PATH}" \
                    --out "${PROFILE_OUT}" 2>&1 >&2) \
        | "${BIN_DIR}/zisk_decoder" "${READELF_FILE}" /dev/stdin
else
    { "${args[@]}" 2>&1 || true; } \
        | grep -v 'Emu::print_regs()' \
        | "${BIN_DIR}/zisk_decoder" "${READELF_FILE}" /dev/stdin
fi