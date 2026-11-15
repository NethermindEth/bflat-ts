#!/usr/bin/env bash
# Checks that a bflat image with an immutable tag (<sha>[-rt<run id>]) was
# built by the expected workflow: build-image.yml of NethermindEth/bflat-riscv64
# writes the commit, the dotnet-riscv run and the run that built the image into
# its labels, and the run must be build-riscv64.yml of bflat-riscv64 or
# zkvm-candidate.yml of this repository, on a branch of that repository, and
# must not have failed.
#
# Usage: check-bflat-image.sh <image@sha256:digest> <bflat-riscv64 sha> [<dotnet-riscv run id>]
# Needs docker buildx and GH_TOKEN.
set -euo pipefail

fail() {
  echo "::error::$*" >&2
  exit 1
}

image=$1
sha=$2
dotnet_run=${3:-}

[[ $image =~ ^ghcr\.io/nethermindeth/bflat-riscv64-11(-riscv32)?:[0-9a-f]{40}(-rt[0-9]{1,20})?@sha256:[0-9a-f]{64}$ ]] \
  || fail "$image: not a bflat image with an immutable tag, pinned by digest"
[[ $sha =~ ^[0-9a-f]{40}$ ]] || fail "malformed bflat-riscv64 sha"
[[ -z $dotnet_run || $dotnet_run =~ ^[0-9]{1,20}$ ]] || fail "malformed dotnet-riscv run id"

# A single image gives its config; an index gives one config per platform.
labels=$(docker buildx imagetools inspect "$image" --format '{{json .Image}}' \
  | jq -c 'if has("config") then .config.Labels else .["linux/amd64"].config.Labels end // {}') \
  || fail "$image: cannot read the image config"

label() {
  jq -r --arg k "$1" '.[$k] // ""' <<<"$labels"
}

[ "$(label org.opencontainers.image.source)" = https://github.com/NethermindEth/bflat-riscv64 ] \
  || fail "$image: not built from NethermindEth/bflat-riscv64 (label org.opencontainers.image.source)"
[ "$(label org.opencontainers.image.revision)" = "$sha" ] \
  || fail "$image: not built from $sha (label org.opencontainers.image.revision)"
[ "$(label io.nethermind.bflat.dotnet-riscv-run)" = "$dotnet_run" ] \
  || fail "$image: not built with the runtime of ${dotnet_run:-the release} (label io.nethermind.bflat.dotnet-riscv-run)"

run_repo=$(label io.nethermind.bflat.run-repository)
run_id=$(label io.nethermind.bflat.run-id)
case "$run_repo" in
  NethermindEth/bflat-riscv64) workflow=.github/workflows/build-riscv64.yml ;;
  NethermindEth/bflat-ts) workflow=.github/workflows/zkvm-candidate.yml ;;
  *) fail "$image: built by a run of '${run_repo}', not of bflat-riscv64 or bflat-ts" ;;
esac
[[ $run_id =~ ^[0-9]{1,20}$ ]] || fail "$image: no run id label"

info=$(gh api "repos/$run_repo/actions/runs/$run_id" \
  --jq '[.head_repository.full_name, .path, .status, (.conclusion // ""), .head_branch, .event] | @tsv') \
  || fail "$image: run $run_id of $run_repo not found"
IFS=$'\t' read -r head path status conclusion branch event <<<"$info"
[ "$head" = "$run_repo" ] || fail "$image: run $run_id of $run_repo comes from $head"
[ "${path%%@*}" = "$workflow" ] || fail "$image: run $run_id of $run_repo is not $workflow"
if [ "$run_repo" = NethermindEth/bflat-ts ]; then
  [ "$event" = workflow_dispatch ] && [ "$branch" = main ] \
    || fail "$image: run $run_id of $run_repo is not a dispatch from main"
fi
# The run that pushed the image may still run its later jobs.
[ "$status" != completed ] || [ "$conclusion" = success ] \
  || fail "$image: run $run_id of $run_repo ended with $conclusion"
echo "$image: built by run $run_id of $run_repo ($workflow) from $sha"
