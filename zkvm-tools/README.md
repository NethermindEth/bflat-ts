# zkvm-tools

[![CI](https://github.com/NethermindEth/zkvm-tools/actions/workflows/ci.yml/badge.svg)](https://github.com/NethermindEth/zkvm-tools/actions/workflows/ci.yml)

Debugging tools for zkVMs for C#.

## Build

```bash
./build.sh
```

Binaries are placed in `./bin/`.

## Tools

### zisk_trace.sh

Runs ziskemu and annotates every `pc=` value in its output with a function name and offset.

```bash
./zisk_trace.sh <ziskemu command>
./zisk_trace.sh ziskemu -v -e ./my_program -i input.bin
```

### zisk_profile.py

Profiles a ziskemu execution log and produces a self-contained HTML report with per-function
and per-class step counts.

```bash
./zisk_trace.sh --profile=report.html ziskemu -e ./my_program
# or standalone:
ziskemu -v -e ./my_program 2>&1 | python3 zisk_profile.py --elf ./my_program --out report.html
```

### zisk_backtrace.sh

Runs ziskemu and reconstructs the call stack at the point of termination or crash.

```bash
./zisk_backtrace.sh <ziskemu command>
./zisk_backtrace.sh ziskemu -e ./my_program
```

The `-v` flag is added automatically if not present.

## Requirements

- `llvm-readelf` or `readelf`
- `llvm-objdump` or `objdump`
- CMake ≥ 3.16, a C++17 compiler
- Python 3.9+ (for `zisk_profile.py`)
