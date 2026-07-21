#!/usr/bin/env python3
# SPDX-License-Identifier: MIT
# Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
#
# Measure "startup steps" in a ziskemu -v execution trace.
#
# Startup steps = number of instruction steps executed before the binary
# first enters the specified symbol (default: __managed__Main, which is
# the bflat / NativeAOT entry point for the C# Main() method).
#
# The count covers the entire .NET NativeAOT runtime initialisation path:
#   _start  →  __libc_start_main  →  uBootstrap_main
#           →  uBootstrap_InitializeRuntime  →  __managed__Main
#
# Usage:
#   zisk_startup_steps.py --elf BINARY --trace TRACE [--symbol SYMBOL]
#
# Output (one line to stdout):
#   startup_steps=N total_steps=M
#
# Requirements:
#   - Python 3.9+
#   - readelf or llvm-readelf in PATH (handles cross-architecture ELFs)

import argparse
import bisect
import re
import shutil
import subprocess
import sys
from pathlib import Path


# ---------------------------------------------------------------------------
# Constants
# ---------------------------------------------------------------------------

DEFAULT_SYMBOL = "__managed__Main"

# Matches lines emitted by ziskemu -v that contain a program-counter field.
# Both hex (0x…) and decimal forms are accepted.
PC_RE = re.compile(r'\bpc=(?:0[xX]([0-9a-fA-F]+)|(\d+))')


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def die(msg: str) -> None:
    print(f"error: {msg}", file=sys.stderr)
    sys.exit(1)


def find_readelf() -> str:
    for cmd in ("readelf", "llvm-readelf"):
        if shutil.which(cmd):
            return cmd
    die("neither 'readelf' nor 'llvm-readelf' found in PATH")


# ---------------------------------------------------------------------------
# ELF symbol table loading
# ---------------------------------------------------------------------------

def load_symbols(elf: str, readelf: str) -> tuple[list[int], list[str]]:
    """
    Run ``readelf -sW`` on *elf* and return *(addrs, names)* — two parallel
    sorted lists of all FUNC symbols with a defined section index.

    Duplicate addresses are collapsed (first name wins after sorting).
    """
    try:
        result = subprocess.run(
            [readelf, "-sW", elf],
            capture_output=True,
            text=True,
            check=True,
        )
    except subprocess.CalledProcessError as exc:
        die(f"readelf failed: {exc.stderr.strip()}")

    addrs: list[int] = []
    names: list[str] = []
    in_symtab = False

    for line in result.stdout.splitlines():
        if not in_symtab:
            if "Symbol table '.symtab'" in line:
                in_symtab = True
            continue

        # Blank line marks end of the symbol table block.
        if not line.strip():
            break

        parts = line.split()
        # Typical readelf -sW columns:
        # Num:  Value  Size  Type  Bind  Vis  Ndx  Name
        if len(parts) < 8 or not parts[0].endswith(":"):
            continue

        try:
            addr = int(parts[1], 16)
        except ValueError:
            continue

        sym_type  = parts[3]   # FUNC, OBJECT, …
        sym_ndx   = parts[6]   # section index or UND/ABS/COM

        if sym_type != "FUNC" or sym_ndx == "UND" or addr == 0:
            continue

        addrs.append(addr)
        names.append(parts[7])

    # Sort by address and remove duplicate addresses (keep first).
    pairs = sorted(zip(addrs, names))
    deduped: list[tuple[int, str]] = []
    for pair in pairs:
        if not deduped or deduped[-1][0] != pair[0]:
            deduped.append(pair)

    if not deduped:
        return [], []

    a, n = zip(*deduped)
    return list(a), list(n)


# ---------------------------------------------------------------------------
# Trace analysis
# ---------------------------------------------------------------------------

def count_startup_steps(
    trace_path: str,
    addrs: list[int],
    names: list[str],
    target_symbol: str,
) -> tuple[int, int]:
    """
    Scan *trace_path* line by line, resolving each ``pc=`` value to a
    function via binary search on *addrs*.

    Returns *(startup_steps, total_steps)* where:
      - *startup_steps* = number of steps executed **before** the first
        instruction inside *target_symbol*
      - *total_steps*   = total number of ``pc=`` entries in the trace

    If *target_symbol* is never reached, *startup_steps* equals
    *total_steps* (the program exited without entering the symbol).
    """
    if not addrs:
        die("symbol table is empty; cannot resolve pc= values")

    # Locate the target symbol in the sorted list.
    target_lo: int | None = None
    target_hi: int | None = None

    for i, name in enumerate(names):
        if name == target_symbol:
            target_lo = addrs[i]
            target_hi = addrs[i + 1] if i + 1 < len(addrs) else (addrs[i] + 0x1_0000_0000)
            break

    if target_lo is None:
        die(f"symbol '{target_symbol}' not found in ELF symbol table")

    startup_steps: int | None = None
    total_steps: int = 0

    with open(trace_path, errors="replace") as fh:
        for line in fh:
            m = PC_RE.search(line)
            if not m:
                continue

            pc = int(m.group(1), 16) if m.group(1) else int(m.group(2))

            # First time we land inside the target symbol → record startup.
            if startup_steps is None and target_lo <= pc < target_hi:
                startup_steps = total_steps

            total_steps += 1

    # Target never reached.
    if startup_steps is None:
        startup_steps = total_steps

    return startup_steps, total_steps


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

def main() -> None:
    ap = argparse.ArgumentParser(
        description=(
            "Count instruction steps before a symbol is first entered "
            "in a ziskemu -v execution trace."
        ),
    )
    ap.add_argument(
        "--elf",
        required=True,
        metavar="BINARY",
        help="path to the riscv64 ELF binary (must have a .symtab section)",
    )
    ap.add_argument(
        "--trace",
        required=True,
        metavar="FILE",
        help="ziskemu -v trace file (contains 'pc=<addr>' on each step line)",
    )
    ap.add_argument(
        "--symbol",
        default=DEFAULT_SYMBOL,
        metavar="NAME",
        help=(
            f"ELF function name to treat as the 'main' boundary "
            f"(default: {DEFAULT_SYMBOL})"
        ),
    )
    args = ap.parse_args()

    if not Path(args.elf).is_file():
        die(f"ELF file not found: {args.elf}")
    if not Path(args.trace).is_file():
        die(f"trace file not found: {args.trace}")

    readelf = find_readelf()

    print(f"[1/3] Loading symbols from {args.elf} ...", file=sys.stderr)
    addrs, names = load_symbols(args.elf, readelf)
    if not addrs:
        die("no FUNC symbols found in '.symtab'; was the binary stripped?")

    print(f"[2/3] Scanning trace for '{args.symbol}' ...", file=sys.stderr)
    startup, total = count_startup_steps(args.trace, addrs, names, args.symbol)

    pct = (startup / total * 100.0) if total > 0 else 0.0
    print(
        f"[3/3] Done: startup={startup:,} / total={total:,} "
        f"({pct:.1f}% in startup)",
        file=sys.stderr,
    )

    # Machine-readable single-line output consumed by run_perf.c
    print(f"startup_steps={startup} total_steps={total}")


if __name__ == "__main__":
    main()