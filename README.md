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
| `--bflat-image=<IMAGES>` | Override the bflat Docker images: one image for riscv64, or a list `arch=image,...` (default: the images pinned in `bflat-ts/lib/tsapi_bflat.h`) |
| `--bflat-extlib=<PKGS>` | Override the runtime packages of some platforms: a list `platform=package,...` (default: the packages of each `package.xml`) |
| `--bflat-codegenopt=<OPTS>` | Pass extra `--codegenopt` values to bflat: a list `name=value,...` (default: none) |
| `--nethermind-rev=<REV>` | Pin the Nethermind revision (branch/tag/SHA) used by ZiskGuest tests; latest commit if omitted |

Any other option is passed through to TE's `dispatcher.sh` — for example, `--tester-run=bflat-ts/simple` to run a single package. See `./scripts/run.sh --help` for the full list.

To build and run the suite inside a Docker container (see `scripts/docker/build/Dockerfile`):

```bash
./scripts/run.sh docker guess
```

## Test packages

The packages that build with `build` iterate over the targets zisk, zisk_sim, sp1 and openvm on riscv64, and airbender on riscv32: the riscv32 bflat image builds the guest with the libairbender of a bflat-airbender release, and the Airbender simulator runs it.

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

The bflat Docker image can also be overridden per-test with the `bflat_image` test parameter, or globally with the `TS_BFLAT_IMAGE` environment variable (`--bflat-image=` of `scripts/run.sh`).

The image depends on the target architecture: the riscv32 target (Airbender) is only in the `bflat-riscv64-11-riscv32` image. `TS_BFLAT_IMAGE` is a comma-separated list. An entry `arch=image` names the image of that architecture, and an entry without `=` names the image of riscv64, so one image keeps its old meaning:

```bash
./scripts/run.sh --bflat-image=riscv64=nethermindeth/bflat-riscv64-11:<tag>,riscv32=nethermindeth/bflat-riscv64-11-riscv32:<tag>
```

An architecture that the list does not name takes the image pinned in `lib/tsapi_bflat.h`.

The runtime packages (`bflat_extlib` in each `package.xml`) can be overridden globally with the `TS_BFLAT_EXTLIB` environment variable (`--bflat-extlib=` of `scripts/run.sh`), to test a candidate package before its release. It is a comma-separated list of `platform=package` entries. The platform is one of `zisk`, `zisk_sim`, `sp1`, `openvm` and `airbender`; `zisk_sim` takes the package of `zisk` when the list does not name it. The package takes any form of `bflat_extlib`: a `.nupkg` URL, a path in the container, or `<url>.tar.gz#<manifest>`. A platform that the list does not name keeps its package:

```bash
./scripts/run.sh --bflat-extlib=sp1=<URL>.nupkg,airbender=<URL>.tar.gz#libairbender.bflat.manifest
```

`TS_BFLAT_CODEGENOPT` (`--bflat-codegenopt=` of `scripts/run.sh`) adds `--codegenopt` values to every bflat build: a comma-separated list of up to 8 `name=value` entries. A malformed list of either variable fails every build.

## CI

`.github/workflows/compiler-tests.yml` runs the suite on the zk-testing service. Run it by hand (`workflow_dispatch`), or call it from another repository (`workflow_call`) with these inputs:

| Input | Description |
|-------|-------------|
| `bflat_branch` | bflat-ts branch (default: the current one; `main` for a caller in another repository) |
| `nethermind_branch` | Nethermind branch (default: the default one) |
| `full` | Run the full test suite (`--full`) |
| `test` | Test paths, space or comma separated (default: all) |
| `zisk_image` | ZisK Docker image (default: the pinned one) |
| `bflat_image` | bflat Docker image for riscv64, or a list `arch=image,...` (`TS_BFLAT_IMAGE`) |
| `bflat_riscv32_image` | bflat Docker image for riscv32 |
| `require_pass` | Fail when a test fails, not only when the run fails |
| `extlib` | Runtime package overrides, a list `platform=package,...` (`TS_BFLAT_EXTLIB`) |
| `codegenopt` | Extra `--codegenopt` values, a list `name=value,...` (`TS_BFLAT_CODEGENOPT`) |

The workflow refuses a malformed `extlib` or `codegenopt` before it queues a run. A caller passes the secrets `HORIZON_HOOK_TOKEN` and `HORIZON_API_TOKEN` (or `secrets: inherit`) and grants `checks: write` and `contents: read`:

```yaml
jobs:
  bflat-ts:
    permissions:
      checks: write
      contents: read
    uses: NethermindEth/bflat-ts/.github/workflows/compiler-tests.yml@main
    with:
      extlib: sp1=<URL>.nupkg
    secrets: inherit
```

## Test a candidate

`.github/workflows/zkvm-candidate.yml` tests a candidate toolchain in one dispatch, without a release. It runs this suite on zk-testing and the Nethermind guest A/B, and writes one summary with a verdict per guest and the A/B tables. A stage runs only when its inputs ask for it.

| Input | Description |
|-------|-------------|
| `nethermind_head` | Nethermind branch or commit to test (required, no tags) |
| `nethermind_base` | A/B base, a branch or commit (empty = `nethermind_head` with a candidate input, else `master`) |
| `bflat_riscv64` | bflat-riscv64 ref; the workflow takes the image `:<sha>` from ghcr, or builds it |
| `dotnet_riscv` | dotnet-riscv `build.yml` run id; the image is built with that runtime |
| `packages` | `platform=value,...` with zisk, sp1, openvm, airbender; value is a git ref, `run:<id>` or a release asset URL of the platform repository (bflat-libziskos, bflat-sp1, bflat-openvm, bflat-airbender; a `.tar.gz` may end with `#<manifest>`) |
| `codegenopt` | Extra `--codegenopt` values, `name=value,...`, at most 8; a value has letters, digits and `._:+-`, at most 64 |
| `suite` | `full` (default) or `simple` |
| `guests`, `ab` | Guests to report and measure; `ab=false` skips the A/B |

A commit must be on a branch of the NethermindEth repository, and a run must come from a branch of that repository and succeed. Give a branch name for a commit that is not on the default branch and is not a branch head. The Nethermind refs take branches and commits only, not tags.

When `bflat_riscv64`, `dotnet_riscv`, `packages` or `codegenopt` is set, the A/B gives the candidate to the head arm only (`candidate_arm=head` of the Nethermind workflow). Both arms then build `nethermind_head`, unless `nethermind_base` is set, so the table shows the effect of the candidate alone. Without a candidate input, the A/B compares `nethermind_base` (default `master`) with `nethermind_head` on the same toolchain.

The workflow reuses an existing image `:<sha>[-rt<run id>]` only when its labels show that `build-image.yml` of bflat-riscv64 built it from that commit and runtime, in a run of `build-riscv64.yml` of bflat-riscv64 or of this workflow that has not failed (`.github/scripts/check-bflat-image.sh`). An image that fails the check stops the run, because `build-image.yml` does not replace an immutable tag.

A JIT option only, on master against master without it:

```bash
gh workflow run zkvm-candidate.yml -R NethermindEth/bflat-ts \
  -f nethermind_head=master -f codegenopt=<name>=<value>
```

A Nethermind change on the pinned toolchain, against master:

```bash
gh workflow run zkvm-candidate.yml -R NethermindEth/bflat-ts \
  -f nethermind_head=<branch> -f suite=simple
```

A runtime package from a pull request run, on the simple suite:

```bash
gh workflow run zkvm-candidate.yml -R NethermindEth/bflat-ts \
  -f nethermind_head=master -f packages=sp1=run:<run id> -f suite=simple
```

A full toolchain: a dotnet-riscv run, a bflat-riscv64 branch and a package branch:

```bash
gh workflow run zkvm-candidate.yml -R NethermindEth/bflat-ts \
  -f nethermind_head=<branch> -f dotnet_riscv=<run id> \
  -f bflat_riscv64=<branch> -f packages=zisk=<branch>,airbender=<branch>
```

## Expected results (TRC)

Expected test results are tracked in the Testing Results Comparator database at `conf/trc.xml`. To generate a TRC report:

```bash
./scripts/trc.sh
```

## License

MIT License. Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind).

## Contributing

Contributions are welcome! Please open an issue or a pull request on GitHub.
