#!/bin/bash
# Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
#
# Static analysis of every C source in the test suite with cppcheck.
#
# The TE build tree is not required: missing-include noise is suppressed, and
# the TE control-flow macros that never return (they jump to the test's
# cleanup label) are mapped to exit() so control-flow analysis does not walk
# through them - without this, error paths produce false positives such as
# "resource freed twice".
#
# Usage: scripts/static_analysis.sh
# Exits non-zero when cppcheck reports any diagnostic.

set -euo pipefail

TOPDIR=$(cd "$(dirname "$0")/.." && pwd)
cd "${TOPDIR}"

mapfile -t C_FILES < <(git ls-files 'bflat-ts/*.c')

if [ "${#C_FILES[@]}" -eq 0 ]; then
    echo "error: no C sources found" >&2
    exit 1
fi

echo "Analyzing ${#C_FILES[@]} C file(s) with $(cppcheck --version)"

cppcheck \
    --std=c11 \
    --language=c \
    --enable=warning,portability,performance \
    --inline-suppr \
    --suppress=missingInclude \
    --suppress=missingIncludeSystem \
    -I bflat-ts/lib \
    "-DTEST_FAIL(...)=exit(1)" \
    "-DTEST_SKIP(...)=exit(0)" \
    "-DTEST_STOP(...)=exit(1)" \
    --error-exitcode=1 \
    --quiet \
    "${C_FILES[@]}"

echo "cppcheck: no issues found"
