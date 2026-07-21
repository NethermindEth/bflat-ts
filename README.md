# Bflat Test Suite

Test suite for [Nethermind's Bflat](https://github.com/NethermindEth/bflat-riscv64), a fork of the C# NativeAOT compiler that builds fully static RISC-V64 binaries for zkVMs.

## Motivation

Bflat targets zkVMs with a limited RISC-V64 instruction subset — no floating point, no compressed instructions, no operating system. Many parts of the .NET runtime are replaced by link-time modules (`nofp`, `pal`, `rhp`, `tls`, `rng_stupid`, ...), and each of those replacements needs dedicated verification. This suite exercises the compiler and every runtime module in isolation, as well as end-to-end scenarios up to stateless Ethereum block execution on Zisk.

## Prerequisites

 - [Test Environment](https://github.com/ts-factory/test-environment) sources checked out next to this repository:
   ```
   workdir/
   ├── bflat-ts/
   └── test-environment/
   ```
 - Docker (used both to run the bflat compiler image and, optionally, to run the whole suite in a container).
 - `ziskemu` for running compiled binaries.

## Running

The easiest way, given the folder structure above:

```bash
./scripts/run.sh guess
```

Useful options:

| Option | Description |
|--------|-------------|
| `--cfg=<CFG>` | Configuration to use (`localhost` by default, see `conf/run.conf.*`) |
| `--bflat-image=<IMAGE>` | Override the bflat Docker image (default: `nethermindeth/bflat-riscv64:latest`) |
| `--nethermind-rev=<REV>` | Pin the Nethermind revision (branch/tag/SHA) used by ZiskGuest tests; latest commit if omitted |

Any other option is passed through to TE's `dispatcher.sh` — for example, `--tester-run=bflat-ts/simple` to run a single package. See `./scripts/run.sh --help` for the full list.

To build and run the suite inside a Docker container (see `scripts/docker/build/Dockerfile`):

```bash
./scripts/run.sh docker guess
```

## Test packages

| Package | Description |
|---------|-------------|
| simple | Simple bflat build tests |
| args | `Main()` argument variations |
| nofp | Floating-point stubs (nofp module) |
| pal | Platform Abstraction Layer (pal) stubs |
| rhp | Runtime Heap Platform (rhp) stubs |
| rng | Random number generation (rng_stupid) |
| tls | Thread-local storage (tls + rhp thread statics) |
| tls_stress | TLS/TSS slot overflow and independence |
| dispatch | Interface and virtual dispatch |
| dispatch_stress | Interface and virtual dispatch stress (t5 register clobber) |
| generics | Generic types and collections |
| gvm | Generic Virtual Method / TypeLoader crash patterns (`Comparer<T>.Default`) |
| exception_handler | Managed exception handler (ZkvmThrow) |
| format | Number and string formatting beyond basic cases |
| memory | Heap allocator correctness under grow and large allocations |
| build_perf | Build-performance tests — compile time measurement |
| run_perf | Run-performance tests — startup steps analysis under ziskemu |
| zisk_guest | Nethermind ZiskGuest integration — stateless block execution on Zisk |

The bflat Docker image can also be overridden per-test with the `bflat_image` test parameter, or globally with the `TS_BFLAT_IMAGE` environment variable.

## Expected results (TRC)

Expected test results are tracked in the Testing Results Comparator database at `conf/trc.xml`. To generate a TRC report:

```bash
./scripts/trc.sh
```

## License

MIT License. Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind).

## Contributing

Contributions are welcome! Please open an issue or a pull request on GitHub.
