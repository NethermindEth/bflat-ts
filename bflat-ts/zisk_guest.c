/** @file
 * @brief Nethermind ZiskGuest integration test
 *
 * Runs the pre-built Nethermind stateless executor (ZiskGuest) ELF binary
 * inside the Zisk emulator on a Test Agent, feeding it a serialised block +
 * witness via an @c input.bin file and, optionally, verifying that the
 * emulator outputs the expected block hash.
 *
 * The guest decodes the payload as a version-prefixed SSZ blob
 * (@c schema:u16be | @c ssz_bytes, schema 0 = SszExecutionPayloadV3,
 * 1 = V4).  On top of that, ziskemu expects the file passed via its
 * @c inputs option to be framed as @c len:u64le | @c bytes[len] |
 * zero-padding to 8 bytes.  Inputs therefore carry the @c .ssz suffix; the
 * legacy pre-SSZ @c .bin inputs are rejected by the guest with
 * "Unsupported schema version".
 *
 * Both the binary and the input file are copied from the local test machine
 * to a temporary directory on the agent before being mounted into the Zisk
 * Docker container.
 *
 * @par Scenario:
 *  -# Resolve local paths to the binary and input file.
 *  -# Copy both files to a temporary directory on the agent.
 *  -# Create a Zisk Docker runner.
 *  -# Start @c ziskemu; capture stdout.
 *  -# Wait for completion; check exit code.
 *  -# If @p expected_hash is provided, find the last @c 0x… line in stdout
 *     and compare it with @p expected_hash (case-insensitive, with or without
 *     the @c 0x prefix).
 *  -# Publish the observed hash as an artifact.
 *
 * @param ta             Test Agent name (must have Docker available)
 * @param binary         Path to the pre-built ZiskGuest ELF, resolved relative
 *                       to the installed test-package directory.
 *                       Default: @c bin/nethermind (produced by
 *                       @c scripts/build_nethermind_guest.sh).
 * @param input_bin      Path to the input file, resolved relative to the
 *                       installed test-package directory.
 *                       Typically @c inputs/<block>.ssz.
 * @param expected_hash  Expected block hash as a hex string
 *                       (e.g. @c 0x1a2b…cafe), or @c - to skip hash
 *                       verification.
 * @param run_timeout_ms Maximum time in milliseconds to wait for @c ziskemu.
 *                       @c 0 means use the built-in default
 *                       (@c #RUN_TIMEOUT_MS_DEFAULT).
 * @param zisk_image     Zisk Docker image to use, or @c - for the default
 *                       (@c #TSAPI_ZISK_DEFAULT_IMAGE).
 * @param zisk_ta        Agent to run the Zisk container on.  If omitted (or
 *                       equal to @p ta) the same agent is used for everything.
 *
 * Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
 *
 * @author Maksim Menshikov <maksim.menshikov@nethermind.io>
 */

#ifndef TE_TEST_NAME
#define TE_TEST_NAME "zisk_guest"
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
#include "tsapi_zisk.h"

#include <string.h>
#include <strings.h>
#include <unistd.h>
#include <limits.h>

/**
 * Default run timeout: 30 minutes.
 *
 * Full Zisk proof generation is slow; emulation-only runs are typically
 * much faster but we keep a generous ceiling.
 */
#define RUN_TIMEOUT_MS_DEFAULT  (30 * 60 * 1000)

/** Name of the binary inside the agent's working directory */
#define GUEST_BINARY_NAME   "nethermind"

/** Name of the input file inside the agent's working directory */
#define INPUT_BIN_NAME      "input.bin"


int
main(int argc, char **argv)
{
    const char         *ta;
    const char         *binary;
    const char         *input_bin;
    const char         *expected_hash;
    int                 run_timeout_ms;
    const char         *zisk_image;
    const char         *zisk_ta          = NULL;

    rcf_rpc_server     *rpcs             = NULL;
    rcf_rpc_server     *zisk_rpcs        = NULL;
    char               *work_dir         = NULL;
    char               *zisk_work_dir    = NULL;
    bool                work_dir_created  = false;
    bool                zisk_work_created = false;
    tapi_job_t         *run_job          = NULL;
    tapi_job_channel_t *run_ch[2];
    tapi_job_channel_t *stdout_filter    = NULL;
    tapi_job_status_t   run_status;
    tsapi_zisk_runner   zisk             = TSAPI_ZISK_RUNNER_INIT;
    bool                zisk_created     = false;
    tapi_job_buffer_t   stdout_buf       = TAPI_JOB_BUFFER_INIT;

    te_string   test_dir     = TE_STRING_INIT;
    te_string   local_bin    = TE_STRING_INIT;
    te_string   local_input  = TE_STRING_INIT;
    te_string   remote_bin   = TE_STRING_INIT;
    te_string   remote_input = TE_STRING_INIT;

    TEST_START;
    TEST_GET_STRING_PARAM(ta);
    TEST_GET_STRING_PARAM(binary);
    TEST_GET_STRING_PARAM(input_bin);
    TEST_GET_OPT_STRING_PARAM(expected_hash);
    TEST_GET_INT_PARAM(run_timeout_ms);
    TEST_GET_OPT_STRING_PARAM(zisk_image);
    TEST_GET_TA(zisk, zisk_ta);

    /* Treat the sentinel value "-" for optional string params as absent */
    if (expected_hash != NULL && strcmp(expected_hash, "-") == 0)
        expected_hash = NULL;
    if (zisk_image != NULL && strcmp(zisk_image, "-") == 0)
        zisk_image = NULL;

    /* ------------------------------------------------------------------ */
    TEST_STEP("Resolve local paths relative to the installed package directory");
    {
        char    exe_buf[PATH_MAX];
        ssize_t exe_len;
        char   *dir;

        exe_len = readlink("/proc/self/exe", exe_buf, sizeof(exe_buf) - 1);
        if (exe_len > 0)
            exe_buf[exe_len] = '\0';
        else
        {
            WARN("readlink(/proc/self/exe) failed; falling back to argv[0]");
            (void)strncpy(exe_buf, argv[0], sizeof(exe_buf) - 1);
            exe_buf[sizeof(exe_buf) - 1] = '\0';
        }

        dir = te_dirname(exe_buf);
        if (dir == NULL)
            TEST_FAIL("Failed to determine test binary directory");
        CHECK_RC(te_string_append(&test_dir, "%s", dir));
        free(dir);
    }

    {
        const char *ts_topdir = getenv("TS_TOPDIR");
        const char *pkg_base  = (ts_topdir != NULL) ? ts_topdir : test_dir.ptr;
        const char *pkg_rel   = (ts_topdir != NULL) ? "bflat-ts/zisk_guest" : ".";

        if (strcmp(binary, "default") == 0)
            CHECK_RC(te_string_append(&local_bin, "%s/%s/bin/nethermind",
                                      pkg_base, pkg_rel));
        else
            CHECK_RC(te_string_append(&local_bin, "%s/%s/%s",
                                      pkg_base, pkg_rel, binary));

        CHECK_RC(te_string_append(&local_input, "%s/%s/%s",
                                  pkg_base, pkg_rel, input_bin));
    }

    RING("nethermind binary : %s", local_bin.ptr);
    RING("input.bin         : %s", local_input.ptr);

    if (access(local_bin.ptr, F_OK) != 0)
        TEST_FAIL("ZiskGuest binary not found at '%s'. "
                  "Run scripts/build_nethermind_guest.sh first.",
                  local_bin.ptr);

    if (access(local_input.ptr, F_OK) != 0)
        TEST_FAIL("Input file not found at '%s'. Inputs are version-prefixed "
                  "SSZ blobs framed for ziskemu; see zisk_guest/package.xml.",
                  local_input.ptr);

    /* ------------------------------------------------------------------ */
    TEST_STEP("Create RPC server on agent '%s'", ta);
    CHECK_RC(rcf_rpc_server_create(ta, "rpcs_zisk_guest", &rpcs));

    /* ------------------------------------------------------------------ */
    TEST_STEP("Create temporary working directory on agent '%s'", ta);
    work_dir = tapi_file_make_custom_pathname(NULL, "/tmp", NULL);
    if (work_dir == NULL)
        TEST_FAIL("Failed to generate temporary directory name");

    RPC_AWAIT_ERROR(rpcs);
    if (rpc_mkdir(rpcs, work_dir, RPC_S_IRWXU) != 0)
        TEST_FAIL("Failed to create temporary directory '%s'", work_dir);
    work_dir_created = true;

    /* ------------------------------------------------------------------ */
    TEST_STEP("Copy ZiskGuest binary to agent");
    CHECK_RC(te_string_append(&remote_bin, "%s/%s", work_dir, GUEST_BINARY_NAME));
    CHECK_RC(tapi_file_copy_ta(NULL, local_bin.ptr, ta, remote_bin.ptr));

    TEST_STEP("Copy input.bin to agent");
    CHECK_RC(te_string_append(&remote_input, "%s/%s", work_dir, INPUT_BIN_NAME));
    CHECK_RC(tapi_file_copy_ta(NULL, local_input.ptr, ta, remote_input.ptr));

    /* ------------------------------------------------------------------ */
    /*
     * If a dedicated Zisk agent is specified and differs from the build
     * agent, copy the working directory there.
     */
    if (zisk_ta != NULL && strcmp(zisk_ta, ta) != 0)
    {
        te_string src_bin_path   = TE_STRING_INIT;
        te_string src_input_path = TE_STRING_INIT;
        te_string dst_bin_path   = TE_STRING_INIT;
        te_string dst_input_path = TE_STRING_INIT;

        TEST_STEP("Create RPC server on Zisk agent '%s'", zisk_ta);
        CHECK_RC(rcf_rpc_server_create(zisk_ta, "rpcs_zisk", &zisk_rpcs));

        TEST_STEP("Create temporary directory on Zisk agent '%s'", zisk_ta);
        zisk_work_dir = tapi_file_make_custom_pathname(NULL, "/tmp", NULL);
        if (zisk_work_dir == NULL)
            TEST_FAIL("Failed to generate temp dir name for Zisk agent");

        RPC_AWAIT_ERROR(zisk_rpcs);
        if (rpc_mkdir(zisk_rpcs, zisk_work_dir, RPC_S_IRWXU) != 0)
            TEST_FAIL("Failed to create dir '%s' on '%s'",
                      zisk_work_dir, zisk_ta);
        zisk_work_created = true;

        TEST_STEP("Copy files from '%s' to Zisk agent '%s'", ta, zisk_ta);
        CHECK_RC(te_string_append(&src_bin_path,
                                  "%s/%s", work_dir, GUEST_BINARY_NAME));
        CHECK_RC(te_string_append(&src_input_path,
                                  "%s/%s", work_dir, INPUT_BIN_NAME));
        CHECK_RC(te_string_append(&dst_bin_path,
                                  "%s/%s", zisk_work_dir, GUEST_BINARY_NAME));
        CHECK_RC(te_string_append(&dst_input_path,
                                  "%s/%s", zisk_work_dir, INPUT_BIN_NAME));

        CHECK_RC(tapi_file_copy_ta(ta,   src_bin_path.ptr,
                                   zisk_ta, dst_bin_path.ptr));
        CHECK_RC(tapi_file_copy_ta(ta,   src_input_path.ptr,
                                   zisk_ta, dst_input_path.ptr));

        te_string_free(&src_bin_path);
        te_string_free(&src_input_path);
        te_string_free(&dst_bin_path);
        te_string_free(&dst_input_path);
    }

    /* Decide which RPC server / work dir to use for Zisk */
    {
        rcf_rpc_server *run_rpcs  = (zisk_rpcs != NULL) ? zisk_rpcs : rpcs;
        const char     *run_dir   = (zisk_work_dir != NULL) ? zisk_work_dir
                                                            : work_dir;

        /* -------------------------------------------------------------- */
        TEST_STEP("Create Zisk runner (image='%s') on agent '%s'",
                  zisk_image != NULL ? zisk_image : TSAPI_ZISK_DEFAULT_IMAGE,
                  run_rpcs->ta);
        CHECK_RC(tsapi_zisk_runner_create(run_rpcs, zisk_image, &zisk));
        zisk_created = true;

        /* -------------------------------------------------------------- */
        TEST_STEP("Create Zisk run job for '%s/%s' with input '%s'",
                  run_dir, GUEST_BINARY_NAME, INPUT_BIN_NAME);
        run_job = tsapi_zisk_run(&zisk, run_dir,
                                 GUEST_BINARY_NAME, INPUT_BIN_NAME, false);
        if (run_job == NULL)
            TEST_FAIL("Failed to create Zisk run job");
    }

    /* Attach output channels — capture stdout when hash check is needed */
    {
        bool capture = (expected_hash != NULL);

        CHECK_RC(tapi_job_alloc_output_channels(run_job, 2, run_ch));
        CHECK_RC(tapi_job_attach_filter(
                     TAPI_JOB_CHANNEL_SET(run_ch[0]),
                     "zisk stdout", capture, TE_LL_RING,
                     capture ? &stdout_filter : NULL));
        CHECK_RC(tapi_job_attach_filter(
                     TAPI_JOB_CHANNEL_SET(run_ch[1]),
                     "zisk stderr", false, TE_LL_ERROR, NULL));
    }

    /* ------------------------------------------------------------------ */
    TEST_STEP("Start Zisk emulator");
    CHECK_RC(tapi_job_start(run_job));

    /* ------------------------------------------------------------------ */
    {
        te_errno wait_rc;
        int      effective_timeout = (run_timeout_ms > 0)
                                     ? run_timeout_ms
                                     : RUN_TIMEOUT_MS_DEFAULT;

        TEST_STEP("Wait for Zisk emulator to finish (timeout=%d ms)",
                  effective_timeout);
        wait_rc = tapi_job_wait(run_job, effective_timeout, &run_status);
        if (TE_RC_GET_ERROR(wait_rc) == TE_EINPROGRESS)
            TEST_FAIL("Zisk emulator timed out after %d ms",
                      effective_timeout);
        CHECK_RC(wait_rc);
    }

    TEST_STEP("Check exit status");
    if (run_status.type != TAPI_JOB_STATUS_EXITED)
        TEST_FAIL("ziskemu was killed by a signal (signo=%d)",
                  run_status.value);
    if (run_status.value == 137)
        TEST_VERDICT("ziskemu was killed by OOM (Out Of Memory)");
    if (run_status.value != 0)
        TEST_FAIL("ziskemu exited with non-zero status %d",
                  run_status.value);

    RING("ZiskGuest execution completed (input=%s)", input_bin);

    /* ------------------------------------------------------------------ */
    if (stdout_filter != NULL)
    {
        te_errno    rd;
        const char *last_hash_line = NULL;
        char       *p;

        /* Drain the filter; tapi_job_receive appends to stdout_buf.data */
        do {
            rd = tapi_job_receive(TAPI_JOB_CHANNEL_SET(stdout_filter),
                                  1000, &stdout_buf);
        } while (rd == 0 && !stdout_buf.eos);

        /*
         * Find the last line that starts with "0x" — that is the block hash
         * printed by IO.WriteLine(block.Hash.ToString()).
         */
        p = stdout_buf.data.ptr;
        if (p != NULL)
        {
            char *line = p;

            while (*p != '\0')
            {
                if (*p == '\n')
                {
                    *p = '\0';
                    if (line[0] == '0' && line[1] == 'x')
                        last_hash_line = line;
                    line = p + 1;
                }
                p++;
            }
            /* Handle last line without trailing newline */
            if (line[0] == '0' && line[1] == 'x')
                last_hash_line = line;
        }

        if (last_hash_line == NULL)
            TEST_FAIL("No '0x…' hash line found in ziskemu stdout "
                      "(input=%s)", input_bin);

        TEST_ARTIFACT("Output hash: %s (input=%s)", last_hash_line, input_bin);

        TEST_STEP("Verify output hash matches expected: '%s'",
                  expected_hash);
        {
            /* Compare without "0x" prefix, case-insensitive */
            const char *actual   = last_hash_line;
            const char *expected = expected_hash;

            if (actual[0] == '0' && actual[1] == 'x')
                actual += 2;
            if (expected[0] == '0' && expected[1] == 'x')
                expected += 2;

            if (strcasecmp(actual, expected) != 0)
                TEST_FAIL("Block hash mismatch:\n"
                          "  expected : %s\n"
                          "  actual   : %s\n"
                          "  input    : %s",
                          expected_hash, last_hash_line, input_bin);
        }

        RING("Hash check passed: %s (input=%s)", last_hash_line, input_bin);
    }

    TEST_SUCCESS;

cleanup:
    te_string_free(&stdout_buf.data);

    if (run_job != NULL)
        CLEANUP_CHECK_RC(tapi_job_destroy(run_job, -1));

    if (zisk_created)
        tsapi_zisk_runner_destroy(&zisk);

    /* Remove the Zisk agent working directory */
    if (zisk_work_created && zisk_rpcs != NULL)
    {
        tarpc_pid_t pid;

        RPC_AWAIT_ERROR(zisk_rpcs);
        pid = rpc_te_shell_cmd(zisk_rpcs, "rm -rf %s", -1,
                               NULL, NULL, NULL, zisk_work_dir);
        if (pid > 0)
        {
            RPC_AWAIT_ERROR(zisk_rpcs);
            rpc_waitpid(zisk_rpcs, pid, NULL, 0);
        }
    }
    free(zisk_work_dir);

    if (zisk_rpcs != NULL)
        CLEANUP_CHECK_RC(rcf_rpc_server_destroy(zisk_rpcs));

    /* Remove the main agent working directory */
    if (work_dir_created && rpcs != NULL)
    {
        tarpc_pid_t pid;

        RPC_AWAIT_ERROR(rpcs);
        pid = rpc_te_shell_cmd(rpcs, "rm -rf %s", -1,
                               NULL, NULL, NULL, work_dir);
        if (pid > 0)
        {
            RPC_AWAIT_ERROR(rpcs);
            rpc_waitpid(rpcs, pid, NULL, 0);
        }
    }
    free(work_dir);

    te_string_free(&test_dir);
    te_string_free(&local_bin);
    te_string_free(&local_input);
    te_string_free(&remote_bin);
    te_string_free(&remote_input);

    if (rpcs != NULL)
        CLEANUP_CHECK_RC(rcf_rpc_server_destroy(rpcs));

    TEST_END;
}