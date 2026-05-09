/** @file
 * @brief Build prologue: compile Nethermind ZiskGuest ELF with bflat
 *
 * Prologue script for the @c zisk_guest test package.  Always updates the
 * Nethermind source tree before building so that the freshest code is
 * compiled.  Steps:
 *
 *  -# Reads @c ${TS_TOPDIR}/.nethermind_path to locate the Nethermind source
 *     tree and resolves it to an absolute path via @c realpath(3).
 *  -# Updates the repository: @c git @c fetch @c origin followed by
 *     @c git @c reset @c --hard @c FETCH_HEAD.
 *  -# Removes any stale @c nethermind binary so the build always produces a
 *     fresh artefact from the updated sources.
 *  -# Runs @c dotnet build on @c Nethermind.Stateless.ZiskGuest.csproj
 *     (redirecting all output to @c /tmp/zisk_guest_dotnet_build.log).
 *  -# Compiles the resulting managed artifacts to a native riscv64/zisk ELF
 *     using @c bflat inside a Docker container, mounting the artifact and
 *     source directories from the Nethermind tree.
 *  -# Moves @c Program.patched to
 *     @c ${TS_TOPDIR}/bflat-ts/zisk_guest/bin/nethermind.
 *  -# Verifies that the binary is present and logs its size.
 *
 * @param ta          Test Agent name (must have Docker available)
 * @param bflat_image bflat Docker image; @c - or omitted picks up
 *                    @c TS_BFLAT_IMAGE from the environment, falling back to
 *                    #TSAPI_BFLAT_DEFAULT_IMAGE
 *
 * Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
 *
 * @author Maksim Menshikov <maksim.menshikov@nethermind.io>
 */

#ifndef TE_TEST_NAME
#define TE_TEST_NAME "build_guest"
#endif

#include "te_config.h"
#include "tapi_test.h"
#include "tapi_job.h"
#include "tapi_file.h"
#include "te_file.h"
#include "te_string.h"
#include "rcf_rpc.h"
#include "tapi_rpc_unistd.h"
#include "tapi_rpc_stdio.h"
#include "tapi_rpc_signal.h"
#include "ts_container.h"
#include "tsapi_bflat.h"
#include <stdio.h>
#include <stdlib.h>
#include <sys/stat.h>
#include <unistd.h>
#include <limits.h>
#include <string.h>

/** bflat compilation timeout: 10 minutes */
#define BUILD_TIMEOUT_MS    (10 * 60 * 1000)

/** Container mount-point for managed artifact DLLs */
#define CONTAINER_BIN_DIR   "/nethermind/bin"

/** Container mount-point for the C# source directory */
#define CONTAINER_SRC_DIR   "/nethermind/src"


int
main(int argc, char **argv)
{
    const char         *ta;
    const char         *bflat_image;

    rcf_rpc_server     *rpcs              = NULL;
    ts_container        container         = TS_CONTAINER_INIT;
    ts_container_params_docker dp         = TS_CONTAINER_PARAMS_DOCKER;
    bool                container_created = false;
    tapi_job_t         *build_job         = NULL;
    tapi_job_channel_t *build_ch[2];
    tapi_job_status_t   build_status;

    te_string   ts_topdir_s  = TE_STRING_INIT;
    te_string   nm_dir_s     = TE_STRING_INIT;
    te_string   bin_path_s   = TE_STRING_INIT;
    te_string   artifacts_s  = TE_STRING_INIT;
    te_string   guest_s      = TE_STRING_INIT;
    te_string   csproj_s     = TE_STRING_INIT;
    te_string   cmd_s        = TE_STRING_INIT;

    TEST_START;
    TEST_GET_STRING_PARAM(ta);
    TEST_GET_OPT_STRING_PARAM(bflat_image);

    bflat_image = tsapi_bflat_resolve_image(bflat_image);

    /* ------------------------------------------------------------------ */
    TEST_STEP("Read TS_TOPDIR environment variable");
    {
        const char *env = getenv("TS_TOPDIR");

        if (env == NULL)
            TEST_FAIL("TS_TOPDIR environment variable is not set");
        CHECK_RC(te_string_append(&ts_topdir_s, "%s", env));
        RING("TS_TOPDIR = %s", ts_topdir_s.ptr);
    }

    /* ------------------------------------------------------------------ */
    TEST_STEP("Read .nethermind_path and resolve Nethermind directory");
    {
        char  nm_path_file[PATH_MAX];
        char  nm_path_raw[PATH_MAX];
        char  nm_path_combined[PATH_MAX * 2 + 2];
        char  nm_path_abs[PATH_MAX];
        FILE *f;
        char *p;

        snprintf(nm_path_file, sizeof(nm_path_file),
                 "%s/.nethermind_path", ts_topdir_s.ptr);

        f = fopen(nm_path_file, "r");
        if (f == NULL)
            TEST_FAIL("Cannot open '%s'", nm_path_file);

        if (fgets(nm_path_raw, sizeof(nm_path_raw), f) == NULL)
        {
            fclose(f);
            TEST_FAIL("Cannot read nethermind path from '%s'", nm_path_file);
        }
        fclose(f);

        p = strchr(nm_path_raw, '\n');
        if (p != NULL)
            *p = '\0';
        p = strchr(nm_path_raw, '\r');
        if (p != NULL)
            *p = '\0';

        if (nm_path_raw[0] == '/')
        {
            if (realpath(nm_path_raw, nm_path_abs) == NULL)
                TEST_FAIL("realpath('%s') failed", nm_path_raw);
        }
        else
        {
            snprintf(nm_path_combined, sizeof(nm_path_combined),
                     "%s/%s", ts_topdir_s.ptr, nm_path_raw);
            if (realpath(nm_path_combined, nm_path_abs) == NULL)
                TEST_FAIL("realpath('%s') failed", nm_path_combined);
        }

        CHECK_RC(te_string_append(&nm_dir_s, "%s", nm_path_abs));
        RING("Nethermind directory: %s", nm_dir_s.ptr);
    }

    /* ------------------------------------------------------------------ */
    TEST_STEP("Update Nethermind repository (git pull + git reset --hard)");
    {
        char  git_cmd[PATH_MAX + 64];
        int   git_rc;

        /*
         * git fetch origin — update all remote-tracking refs without touching
         * the working tree.  Works regardless of the branch name (main,
         * master, etc.) and always sets FETCH_HEAD to the fetched tip.
         */
        snprintf(git_cmd, sizeof(git_cmd),
                 "git -C '%s' fetch origin 2>&1", nm_dir_s.ptr);
        RING("Running: %s", git_cmd);
        git_rc = system(git_cmd);
        if (git_rc != 0)
            TEST_FAIL("'git fetch origin' failed in '%s' (exit code %d)",
                      nm_dir_s.ptr, git_rc);

        /*
         * git reset --hard FETCH_HEAD — reset the working tree to whatever
         * was just fetched, discarding any local modifications.  FETCH_HEAD
         * is always valid after a successful fetch and is branch-name
         * agnostic.
         */
        snprintf(git_cmd, sizeof(git_cmd),
                 "git -C '%s' reset --hard FETCH_HEAD 2>&1", nm_dir_s.ptr);
        RING("Running: %s", git_cmd);
        git_rc = system(git_cmd);
        if (git_rc != 0)
            TEST_FAIL("'git reset --hard FETCH_HEAD' failed in '%s' "
                      "(exit code %d)", nm_dir_s.ptr, git_rc);

        RING("Nethermind repository updated successfully");
    }

    /* ------------------------------------------------------------------ */
    TEST_STEP("Remove stale binary so a fresh build is always performed");
    CHECK_RC(te_string_append(&bin_path_s,
                              "%s/bflat-ts/zisk_guest/bin/nethermind",
                              ts_topdir_s.ptr));

    if (access(bin_path_s.ptr, F_OK) == 0)
    {
        if (unlink(bin_path_s.ptr) != 0)
            TEST_FAIL("Failed to remove stale binary '%s'", bin_path_s.ptr);
        RING("Removed stale binary: %s", bin_path_s.ptr);
    }
    else
    {
        RING("No stale binary found at '%s'", bin_path_s.ptr);
    }

    /* ------------------------------------------------------------------ */
    TEST_STEP("Create RPC server on agent '%s'", ta);
    CHECK_RC(rcf_rpc_server_create(ta, "rpcs_build_guest", &rpcs));

    CHECK_RC(te_string_append(&artifacts_s,
                              "%s/src/Nethermind/artifacts/bin/"
                              "Nethermind.Stateless.ZiskGuest/release",
                              nm_dir_s.ptr));
    CHECK_RC(te_string_append(&guest_s,
                              "%s/src/Nethermind/"
                              "Nethermind.Stateless.ZiskGuest",
                              nm_dir_s.ptr));
    CHECK_RC(te_string_append(&csproj_s,
                              "%s/Nethermind.Stateless.ZiskGuest.csproj",
                              guest_s.ptr));

    /* ------------------------------------------------------------------ */
    TEST_STEP("Run dotnet build for Nethermind.Stateless.ZiskGuest");
    {
        tarpc_pid_t     build_pid;
        rpc_wait_status build_ws;
        int             old_timeout;

        RPC_AWAIT_ERROR(rpcs);
        build_pid = rpc_te_shell_cmd(rpcs,
            "dotnet build -c release -p:EnableZkEvm=true '%s'"
            " >/tmp/zisk_guest_dotnet_build.log 2>&1",
            -1, NULL, NULL, NULL, csproj_s.ptr);
        if (build_pid < 0)
            TEST_FAIL("Failed to start dotnet build");

        old_timeout = rpcs->timeout;
        rpcs->timeout = BUILD_TIMEOUT_MS;
        RPC_AWAIT_ERROR(rpcs);
        rpc_waitpid(rpcs, build_pid, &build_ws, 0);
        rpcs->timeout = old_timeout;
        if (build_ws.flag != RPC_WAIT_STATUS_EXITED || build_ws.value != 0)
            TEST_FAIL("dotnet build failed; "
                      "see /tmp/zisk_guest_dotnet_build.log");
    }

    /* ------------------------------------------------------------------ */
    TEST_STEP("Create bflat Docker container (image='%s')", bflat_image);
    dp.params.name     = bflat_image;
    dp.params.prebuilt = TRUE;
    CHECK_RC(ts_container_create(rpcs, TS_CONTAINER_TYPE_DOCKER,
                                 &dp.params, &container));
    container_created = true;

    TEST_STEP("Mount artifact binaries '%s' -> '%s'",
              artifacts_s.ptr, CONTAINER_BIN_DIR);
    CHECK_RC(ts_container_share_folder(&container,
                                       artifacts_s.ptr, CONTAINER_BIN_DIR));

    TEST_STEP("Mount C# source directory '%s' -> '%s'",
              guest_s.ptr, CONTAINER_SRC_DIR);
    CHECK_RC(ts_container_share_folder(&container,
                                       guest_s.ptr, CONTAINER_SRC_DIR));

    TEST_STEP("Compile ZiskGuest with bflat (arch=riscv64 libc=zisk)");
    {
        const char *bflat_argv[64];
        int         bflat_argc = 0;

#define ARGV_ADD(x)  bflat_argv[bflat_argc++] = (x)
        ARGV_ADD("bflat");
        ARGV_ADD("build");
        ARGV_ADD("--arch");             ARGV_ADD("riscv64");
        ARGV_ADD("--os");               ARGV_ADD("linux");
        ARGV_ADD("--stdlib");           ARGV_ADD("dotnet");
        ARGV_ADD("--libc");             ARGV_ADD("zisk");
        ARGV_ADD("--no-pie");
        ARGV_ADD("--no-pthread");
        ARGV_ADD("--no-stacktrace-data");
        ARGV_ADD("--no-globalization");
        ARGV_ADD("--nostdlibrefs");
        ARGV_ADD("-r");
        ARGV_ADD("{std}/System.Diagnostics.StackTrace.dll");
        ARGV_ADD("-r");
        ARGV_ADD("{std}/System.Diagnostics.Tracing.dll");
        ARGV_ADD("-r");
        ARGV_ADD("{std}/System.Memory.dll");
        ARGV_ADD("-r");
        ARGV_ADD("{std}/System.Runtime.dll");
        ARGV_ADD("-r");
        ARGV_ADD(CONTAINER_BIN_DIR "/BouncyCastle.Cryptography.dll");
        ARGV_ADD("-r");
        ARGV_ADD(CONTAINER_BIN_DIR "/Microsoft.Extensions.ObjectPool.dll");
        ARGV_ADD("-r");
        ARGV_ADD(CONTAINER_BIN_DIR "/Nethermind.Abi.dll");
        ARGV_ADD("-r");
        ARGV_ADD(CONTAINER_BIN_DIR "/Nethermind.Blockchain.dll");
        ARGV_ADD("-r");
        ARGV_ADD(CONTAINER_BIN_DIR "/Nethermind.Consensus.dll");
        ARGV_ADD("-r");
        ARGV_ADD(CONTAINER_BIN_DIR "/Nethermind.Core.dll");
        ARGV_ADD("-r");
        ARGV_ADD(CONTAINER_BIN_DIR "/Nethermind.Crypto.dll");
        ARGV_ADD("-r");
        ARGV_ADD(CONTAINER_BIN_DIR "/Nethermind.Db.dll");
        ARGV_ADD("-r");
        ARGV_ADD(CONTAINER_BIN_DIR "/Nethermind.Evm.dll");
        ARGV_ADD("-r");
        ARGV_ADD(CONTAINER_BIN_DIR "/Nethermind.Evm.Precompiles.dll");
        ARGV_ADD("-r");
        ARGV_ADD(CONTAINER_BIN_DIR "/Nethermind.Int256.dll");
        ARGV_ADD("-r");
        ARGV_ADD(CONTAINER_BIN_DIR "/Nethermind.Logging.dll");
        ARGV_ADD("-r");
        ARGV_ADD(CONTAINER_BIN_DIR "/Nethermind.Serialization.Rlp.dll");
        ARGV_ADD("-r");
        ARGV_ADD(CONTAINER_BIN_DIR "/Nethermind.Specs.dll");
        ARGV_ADD("-r");
        ARGV_ADD(CONTAINER_BIN_DIR "/Nethermind.State.dll");
        ARGV_ADD("-r");
        ARGV_ADD(CONTAINER_BIN_DIR "/Nethermind.Stateless.Executor.dll");
        ARGV_ADD("-r");
        ARGV_ADD(CONTAINER_BIN_DIR "/Nethermind.Trie.dll");
        ARGV_ADD("-r");
        ARGV_ADD(CONTAINER_BIN_DIR "/Nethermind.TxPool.dll");
        ARGV_ADD("--extlib");
        ARGV_ADD(CONTAINER_BIN_DIR "/libziskos.bflat.manifest");
        ARGV_ADD("-o");
        ARGV_ADD(CONTAINER_SRC_DIR "/Program.patched");
        ARGV_ADD(CONTAINER_SRC_DIR "/Program.cs");
        ARGV_ADD(NULL);
#undef ARGV_ADD

        build_job = ts_container_run(&container, bflat_argv);
    }
    if (build_job == NULL)
        TEST_FAIL("Failed to create bflat build job");

    CHECK_RC(tapi_job_alloc_output_channels(build_job, 2, build_ch));
    CHECK_RC(tapi_job_attach_filter(TAPI_JOB_CHANNEL_SET(build_ch[0]),
                                    "bflat stdout", false, TE_LL_RING, NULL));
    CHECK_RC(tapi_job_attach_filter(TAPI_JOB_CHANNEL_SET(build_ch[1]),
                                    "bflat stderr", false, TE_LL_ERROR, NULL));

    CHECK_RC(tapi_job_start(build_job));

    TEST_STEP("Wait for bflat to finish (timeout %d ms)", BUILD_TIMEOUT_MS);
    CHECK_RC(tapi_job_wait(build_job, BUILD_TIMEOUT_MS, &build_status));

    if (build_status.type != TAPI_JOB_STATUS_EXITED)
        TEST_FAIL("bflat was killed by a signal (signo=%d)",
                  build_status.value);
    if (build_status.value != 0)
        TEST_FAIL("bflat exited with non-zero status %d",
                  build_status.value);

    RING("bflat compiled ZiskGuest successfully (arch=riscv64 libc=zisk)");

    /* ------------------------------------------------------------------ */
    TEST_STEP("Move compiled binary to '%s'", bin_path_s.ptr);
    {
        tarpc_pid_t     mv_pid;
        rpc_wait_status mv_ws;

        CHECK_RC(te_string_append(&cmd_s,
            "mkdir -p '%s/bflat-ts/zisk_guest/bin'"
            " && mv '%s/Program.patched' '%s/bflat-ts/zisk_guest/bin/nethermind'"
            " && rm -f '%s/Program'",
            ts_topdir_s.ptr,
            guest_s.ptr, ts_topdir_s.ptr,
            guest_s.ptr));

        RPC_AWAIT_ERROR(rpcs);
        mv_pid = rpc_te_shell_cmd(rpcs, "%s", -1, NULL, NULL, NULL,
                                  cmd_s.ptr);
        if (mv_pid < 0)
            TEST_FAIL("Failed to start move command");

        RPC_AWAIT_ERROR(rpcs);
        rpc_waitpid(rpcs, mv_pid, &mv_ws, 0);
        if (mv_ws.flag != RPC_WAIT_STATUS_EXITED || mv_ws.value != 0)
            TEST_FAIL("Move command failed for '%s'", bin_path_s.ptr);
    }

    /* ------------------------------------------------------------------ */
    TEST_STEP("Verify binary at '%s'", bin_path_s.ptr);
    if (access(bin_path_s.ptr, F_OK) != 0)
        TEST_FAIL("Binary not found at '%s' after build", bin_path_s.ptr);

    {
        struct stat st;

        if (stat(bin_path_s.ptr, &st) == 0)
            RING("ZiskGuest binary ready: %s (%lld bytes)",
                 bin_path_s.ptr, (long long)st.st_size);
    }

    TEST_SUCCESS;

cleanup:
    if (build_job != NULL)
        CLEANUP_CHECK_RC(tapi_job_destroy(build_job, -1));

    if (container_created)
        ts_container_destroy(&container);

    te_string_free(&ts_topdir_s);
    te_string_free(&nm_dir_s);
    te_string_free(&bin_path_s);
    te_string_free(&artifacts_s);
    te_string_free(&guest_s);
    te_string_free(&csproj_s);
    te_string_free(&cmd_s);

    if (rpcs != NULL)
        CLEANUP_CHECK_RC(rcf_rpc_server_destroy(rpcs));

    TEST_END;
}