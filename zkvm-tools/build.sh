#!/usr/bin/env bash
# SPDX-License-Identifier: MIT
# Copyright (c) 2025-2026 Demerzel Solutions Limited (Nethermind)
#
# Build all zisk_* utilities and place binaries in ./bin/.
# Override defaults with: BUILD_TYPE=Debug JOBS=8 ./build.sh

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BIN_DIR="${ROOT_DIR}/bin"
BUILD_TYPE="${BUILD_TYPE:-Release}"
JOBS="${JOBS:-$(nproc 2>/dev/null || sysctl -n hw.logicalcpu 2>/dev/null || echo 4)}"

mkdir -p "${BIN_DIR}"

build_cmake() {
    local name="$1"
    local src="$2"

    cmake -S "${src}" \
          -B "${src}/build" \
          -DCMAKE_BUILD_TYPE="${BUILD_TYPE}" \
          -DCMAKE_RUNTIME_OUTPUT_DIRECTORY="${BIN_DIR}"

    cmake --build "${src}/build" --parallel "${JOBS}"

    echo "${name} -> ${BIN_DIR}/${name}"
}

build_cmake "zisk_decoder"   "${ROOT_DIR}/zisk_decoder"
build_cmake "zisk_backtrace" "${ROOT_DIR}/zisk_backtrace"
build_cmake "zisk_disasm"    "${ROOT_DIR}/zisk_disasm"

echo "done (${BUILD_TYPE}): ${BIN_DIR}/"
