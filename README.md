# Bflat Test Suite

Test suite for [Nethermind's Bflat](https://github.com/NethermindEth/bflat-riscv64), a fork of the C# NativeAOT compiler that builds fully static RISC-V64 binaries for zkVMs.

## Motivation

Bflat targets zkVMs with a limited RISC-V64 instruction subset — no floating point, no compressed instructions, no operating system. Many parts of the .NET runtime are replaced by link-time modules (`nofp`, `pal`, `rhp`, `tls`, `rng_stupid`, ...), and each of those replacements needs dedicated verification. This suite exercises the compiler and every runtime module in isolation, as well as end-to-end scenarios up to stateless Ethereum block execution on Zisk.

## Relationship to upstream .NET testing

bflat-ts is a deliberate **complement** to the .NET platform's own test suites
(`dotnet/runtime` library tests, CoreCLR/NativeAOT tests) — it does not
replace them, and it does not try to re-validate BCL semantics that upstream
already covers on supported platforms. The suites answer different questions:

 - **Upstream tests** verify that the runtime and libraries behave correctly
   on mainstream platforms, with a full OS, hardware floating point, threads
   and a test harness (xunit) available. Almost none of that exists on a zkVM
   target: there is no filesystem, no processes, no FP or compressed
   instructions — the upstream harness cannot even load there.
 - **bflat-ts** verifies the *delta* that bflat introduces: RISC-V64 NativeAOT
   code generation (soft-float lowering, dispatch, generics/GVM, TLS,
   exception handling), every runtime module that replaces a piece of the
   stock runtime, and the actual execution environments — `ziskemu` for the
   real zkVM target and `qemu-riscv64-static` for the host-loadable
   simulation layout. Each test is a small self-contained program, so a
   failure points directly at a compiler or module regression rather than at
   a library behavior.

Because every run compiles and executes ~1600 cases against a specific bflat
Docker image and tracks the outcome against expectations (TRC), the suite
doubles as a **release-readiness signal** for the toolchain: a green run over
a candidate image means the compiler, the replacement modules and the
end-to-end block-execution path all still hold; any red is a concrete,
reproducible statement of what is not ready yet.

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

## How tests work

Almost every test case follows the same pattern (`bflat-ts/build.c`): compile one
self-checking C# file with bflat inside the compiler Docker image, then execute
the result and require exit code 0 (optionally comparing stdout). Most packages
iterate each case over two targets:

 - `--libc zisk` — the post-processed (`.patched`) ELF runs in `ziskemu`
   inside a Zisk Docker container;
 - `--libc zisk_sim` — a Zisk-like ELF with a host-loadable layout runs under
   `qemu-riscv64-static`.

The bflat image is selected per test (`bflat_image`) or globally
(`TS_BFLAT_IMAGE` / `--bflat-image`); zisk-target tests link `libziskos` via
`--extlib` (`bflat_extlib` parameter).

## Test packages

~1630 test cases across 19 packages; counts below are per-package cases
(most run twice — once per libc target).

| Package | Cases | What it covers |
|---------|------:|----------------|
| simple | 126 | Language basics end-to-end: int/long arithmetic, branches and loops, casts, static calls and recursion, arrays, strings, switch, `Console.Write(Line)` variants, `Main()` shapes, object/struct creation. |
| args | 98 | `Main(string[] args)` handling: argv access, iteration, `Parse`/`TryParse` for every numeric type, join/sort/reverse helpers, exit codes, async `Main`, top-level statements. |
| bcl_subst | 49 | CoreLib paths substituted by zisk modules: `Random` compat (seeded/derived), `Hashtable`, value-type hashing, Frozen collections, `ArrayPool`, dictionary/hashset growth primes, `Array.Copy`/`Buffer`, `DateTime`/`TimeZoneInfo`, `Guid`, `Stopwatch`, `Task`, P/Invoke, `Environment`, reflection invoke, deep-stack `GC.Collect`, type unifier resize. |
| nofp | 102 | Soft-float lowering (no FP instructions may survive): float/double arithmetic, comparisons, int↔FP conversions, boxing, 1D/2D arrays, `Math.Abs`/`Sqrt`, long dependency chains and register-pressure piles. |
| pal | 103 | Platform Abstraction Layer: bump allocator (sizes, alloc/drop patterns, clears), `Environment` surface (variables incl. unknown-index probes, machine name, pid, processor count, tick count, user name, OS version), locks, `GC.Collect`, exit codes. |
| rhp | 146 | Runtime helpers (allocation/casts): `new T[]` for every primitive type × size, boxing/unboxing, `is`/`as`/interface casts, multidimensional arrays, object/string allocation, int formatting, string concat and literals of every length. |
| rng | 109 | `rng_stupid` LCG module: `Next()` ranges and edge cases, `NextBytes` (1 B – 4 KiB), `NextDouble`, seeded reproducibility, 50 seed indices, `RandomNumberGenerator.Fill`. |
| tls | 107 | `[ThreadStatic]` statics via the tls module: every primitive type, refs, structs, generics, 30 per-class slots, sparse field layouts, multi-holder classes, counters. |
| tls_stress | 105 | TLS slot pressure: up to 400-field-wide classes, 128 independent statics, alloc/free cycles, distinct generic instantiations, TSS independence. |
| dispatch | 105 | Virtual and interface dispatch: vtables, up to 10 interface impls, call chains, polymorphic arrays, overloads, sealed/abstract, properties, explicit interface impls. |
| dispatch_stress | 103 | Dispatch under stress (t5 register-clobber regression): hierarchies to depth 15, vtables/interfaces up to 64 slots wide, stride overrides, hot dispatch loops up to 10000 iterations. |
| generics | 100 | Generic types and collections: `List`/`Dictionary`/`HashSet`/`Queue`/`Stack`/`SortedDictionary`/`LinkedList`, growth, generic methods, constraints, inference, nested collections, `typeof`. |
| gvm | 101 | Generic virtual methods and the TypeLoader: GVM echo/wrap per primitive type, GVMs in interfaces and generic classes, call chains and mixed call storms, `Comparer<T>.Default` / `EqualityComparer<T>.Default`. |
| exception_handler | 30 | Managed EH via ZkvmThrow: try/catch/finally ordering, exception filters, rethrow and inner exceptions, nested handlers, `using`/Dispose on throw, custom exceptions, throw-in-finally, framework throws (NRE, bounds, overflow, div-by-zero, invalid cast). |
| format | 131 | Formatting/ToString: numeric bounds for every integer type, hex and `D`/`N` formats, string interpolation, `StringBuilder`, split/join/trim/replace/pad, char/bool/enum, `Convert` number bases. |
| memory | 103 | Allocator under pressure: arrays of every size up to 64 KiB, `List`/`Dictionary`/`HashSet`/`StringBuilder` growth (up to 100k elements), jagged/2D arrays, `stackalloc`, spans, `Array.Resize`, boxing storms. |
| build_perf | 1 | Compile-time measurement: `big_refs.cs` (a wide BCL cross-section — collections, LINQ, JSON, crypto, channels, reflection) must build within a threshold; wall-clock time is logged as an MI measurement. |
| run_perf | 1 | Startup-cost analysis: runs `startup.cs` in `ziskemu` and attributes per-step cycle counts (`zisk_startup_steps.py`). |
| zisk_guest | 10 | End-to-end: the `build_guest` prologue builds Nethermind's stateless ZiskGuest via its upstream Makefile, then replays real mainnet blocks (version-prefixed SSZ inputs) in `ziskemu`, verifying the resulting block hash. |

## Expected results (TRC)

Expected test results are tracked in the Testing Results Comparator database at `conf/trc.xml`. To generate a TRC report:

```bash
./scripts/trc.sh
```

## License

MIT License. Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind).

## Contributing

Contributions are welcome! Please open an issue or a pull request on GitHub.
