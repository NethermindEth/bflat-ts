#!/usr/bin/env bash
# SPDX-License-Identifier: MIT
# Copyright (c) 2025-2026 Demerzel Solutions Limited (Nethermind)
#
# Run ziskemu and reconstruct the call stack from its execution trace.
#
# Usage:
#   zisk_backtrace.sh <full ziskemu command line>
#
# Example:
#   zisk_backtrace.sh ziskemu -e ./my_program -i input.bin
#
# The -v flag (verbose trace) is added automatically if not present.
# Reconstructed call stack is written to stdout; progress goes to stderr.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BIN_DIR="${SCRIPT_DIR}/bin"

fail() {
    echo "error: $*" >&2
    exit 1
}

# llvm-readelf/llvm-objdump are preferred: the system tools on macOS do not
# support RISC-V.  On Linux both work, but llvm variants are tried first.
if command -v llvm-readelf &>/dev/null; then
    READELF_CMD="llvm-readelf"
elif command -v readelf &>/dev/null; then
    READELF_CMD="readelf"
else
    fail "neither llvm-readelf nor readelf found in PATH"
fi

if command -v llvm-objdump &>/dev/null; then
    OBJDUMP_CMD="llvm-objdump"
elif command -v objdump &>/dev/null; then
    OBJDUMP_CMD="objdump"
else
    fail "neither llvm-objdump nor objdump found in PATH"
fi

[[ $# -lt 1 ]] && fail "usage: $(basename "$0") <ziskemu command line>"

args=("$@")

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

# Verbose trace output is required for call stack reconstruction.
has_v=0
for arg in "${args[@]}"; do
    [[ "${arg}" == "-v" || "${arg}" == "--verbose" ]] && has_v=1 && break
done
[[ ${has_v} -eq 0 ]] && args+=("-v")

WORK_DIR="$(mktemp -d)"
trap 'rm -rf "${WORK_DIR}"' EXIT

READELF="${WORK_DIR}/readelf.txt"
DISASM="${WORK_DIR}/disasm.txt"

echo "[1/3] Reading symbol table from: ${ELF_PATH}" >&2
"${READELF_CMD}" -SW -s "${ELF_PATH}" > "${READELF}"

echo "[2/3] Disassembling: ${ELF_PATH}" >&2
"${OBJDUMP_CMD}" -d "${ELF_PATH}" > "${DISASM}"

# ziskemu output is streamed through the pipeline without touching disk:
#   ziskemu | zisk_decoder | zisk_disasm | zisk_backtrace
echo "[3/3] Running and analysing..." >&2
{ "${args[@]}" 2>&1 || true; } \
    | "${BIN_DIR}/zisk_decoder" "${READELF}" /dev/stdin \
    | "${BIN_DIR}/zisk_disasm" \
          --decoded /dev/stdin \
          --disasm  "${DISASM}" \
          --out     /dev/stdout \
    | "${BIN_DIR}/zisk_backtrace" --in /dev/stdin