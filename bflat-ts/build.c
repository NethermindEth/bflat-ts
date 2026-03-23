/** @file
 * @brief Simple bflat build test
 *
 * Test that compiles a C# source file using bflat inside a Docker container.
 *
 * Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
 *
 * @author Maksim Menshikov <maksim.menshikov@nethermind.io>
 */

/** @page bflat_ts_build Build a C# program with bflat
 *
 * @objective Verify that bflat can successfully compile a C# source file
 *            targeting a given architecture/libc inside the specified bflat
 *            Docker image.
 *
 * @param ta            Test Agent name (must have Docker available)
 * @param cs_file       C# source filename (resolved from the @c cs/ subdir
 *                      next to the test binary)
 * @param bflat_image   Docker image providing the bflat compiler
 * @param bflat_arch    Target architecture passed to @c --arch
 * @param bflat_libc    Target libc passed to @c --libc
 * @param run           If @c TRUE, run the compiled binary under qemu after
 *                      a successful build
 * @param qemu_path     Path to qemu-riscv64-static on the agent, or empty to
 *                      use the default (#TSAPI_QEMU_DEFAULT_PATH)
 *
 * @par Scenario:
 *
 */

#ifndef TE_TEST_NAME
#define TE_TEST_NAME "build"
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
#include "tsapi_qemu.h"

#include <unistd.h>
#include <limits.h>

/** Path inside the container where sources are mounted */
#define CONTAINER_SRC_DIR  "/src"

/** Build timeout: 5 minutes should be enough even on a slow machine */
#define BUILD_TIMEOUT_MS  (5 * 60 * 1000)

/** Run timeout for the compiled binary under qemu */
#define RUN_TIMEOUT_MS    (30 * 1000)

int
main(int argc, char **argv)
{
    const char         *ta;
    const char         *cs_file;
    const char         *bflat_image;
    const char         *bflat_arch;
    const char         *bflat_libc;
    const char         *bflat_stdlib;
    bool                verbose;
    bool                no_globalization;
    bool                no_stacktrace_data;
    bool                no_pthread;
    bool                no_pie;
    bool                run;
    const char         *qemu_path;

    rcf_rpc_server     *rpcs              = NULL;
    char               *test_dir          = NULL;
    char               *src_dir           = NULL;
    ts_container        container         = TS_CONTAINER_INIT;
    ts_container_params_docker dp         = TS_CONTAINER_PARAMS_DOCKER;
    tapi_job_t         *job               = NULL;
    tapi_job_t         *run_job           = NULL;
    tapi_job_channel_t *out_channels[2];
    tapi_job_channel_t *run_channels[2];
    tapi_job_status_t   status;
    tapi_job_status_t   run_status;
    bool                container_created = false;
    tsapi_qemu_runner   qemu              = TSAPI_QEMU_RUNNER_INIT;
    bool                qemu_created      = false;

    te_string           local_cs_path     = TE_STRING_INIT;
    te_string           remote_cs_path    = TE_STRING_INIT;
    te_string           remote_out        = TE_STRING_INIT;
    te_string           agent_binary_path = TE_STRING_INIT;

    TEST_START;
    TEST_GET_STRING_PARAM(ta);
    TEST_GET_STRING_PARAM(cs_file);
    TEST_GET_STRING_PARAM(bflat_image);
    TEST_GET_STRING_PARAM(bflat_arch);
    TEST_GET_STRING_PARAM(bflat_libc);
    TEST_GET_OPT_STRING_PARAM(bflat_stdlib);
    TEST_GET_BOOL_PARAM(verbose);
    TEST_GET_BOOL_PARAM(no_globalization);
    TEST_GET_BOOL_PARAM(no_stacktrace_data);
    TEST_GET_BOOL_PARAM(no_pthread);
    TEST_GET_BOOL_PARAM(no_pie);
    TEST_GET_BOOL_PARAM(run);
    TEST_GET_OPT_STRING_PARAM(qemu_path);

    TEST_STEP("Resolve local path to '%s'", cs_file);
    {
        char exe_buf[PATH_MAX];
        ssize_t exe_len;

        exe_len = readlink("/proc/self/exe", exe_buf, sizeof(exe_buf) - 1);
        if (exe_len > 0)
        {
            exe_buf[exe_len] = '\0';
            test_dir = te_dirname(exe_buf);
        }
        else
        {
            WARN("readlink(/proc/self/exe) failed, falling back to argv[0]");
            test_dir = te_dirname(argv[0]);
        }
    }
    if (test_dir == NULL)
        TEST_FAIL("Failed to determine test binary directory");

    CHECK_RC(te_string_append(&local_cs_path, "%s/cs/%s", test_dir, cs_file));
    CHECK_RC(te_string_append(&remote_cs_path,
                              CONTAINER_SRC_DIR "/%s", cs_file));
    CHECK_RC(te_string_append(&remote_out,
                              CONTAINER_SRC_DIR "/%.*s",
                              (int)(strrchr(cs_file, '.') != NULL
                                    ? strrchr(cs_file, '.') - cs_file
                                    : (int)strlen(cs_file)),
                              cs_file));

    TEST_STEP("Create RPC server on agent '%s'", ta);
    CHECK_RC(rcf_rpc_server_create(ta, "rpcs_build", &rpcs));

    TEST_STEP("Create temporary source directory on the agent");
    src_dir = tapi_file_make_custom_pathname(NULL, "/tmp", NULL);
    if (src_dir == NULL)
        TEST_FAIL("Failed to generate temporary directory name");

    RPC_AWAIT_ERROR(rpcs);
    if (rpc_mkdir(rpcs, src_dir, RPC_S_IRWXU) != 0)
        TEST_FAIL("Failed to create temporary directory '%s'", src_dir);

    TEST_STEP("Copy '%s' to agent '%s:%s'",
              local_cs_path.ptr, ta, src_dir);
    {
        te_string agent_cs_path = TE_STRING_INIT;

        CHECK_RC(te_string_append(&agent_cs_path, "%s/%s", src_dir, cs_file));
        CHECK_RC(tapi_file_copy_ta(NULL, local_cs_path.ptr,
                                   ta, agent_cs_path.ptr));
        te_string_free(&agent_cs_path);
    }

    TEST_STEP("Create Docker container backed by bflat image '%s'",
              bflat_image);
    dp.params.name     = bflat_image;
    dp.params.prebuilt = TRUE;
    CHECK_RC(ts_container_create(rpcs, TS_CONTAINER_TYPE_DOCKER,
                                 &dp.params, &container));
    container_created = true;

    TEST_STEP("Share source directory '%s' -> '%s'",
              src_dir, CONTAINER_SRC_DIR);
    CHECK_RC(ts_container_share_folder(&container,
                                       src_dir, CONTAINER_SRC_DIR));

    TEST_STEP("Create bflat build job: arch=%s libc=%s stdlib=%s file=%s",
              bflat_arch, bflat_libc,
              bflat_stdlib != NULL ? bflat_stdlib : "(default)", cs_file);
    {
        /* Fixed upper bound: base args + all optional flags + src + out + NULL */
        const char *bflat_argv[32];
        int         bflat_argc = 0;

#define ARGV_ADD(arg_)  bflat_argv[bflat_argc++] = (arg_)

        ARGV_ADD("bflat");
        ARGV_ADD("build");
        ARGV_ADD("-x");
        if (verbose)
            ARGV_ADD("--verbose");
        ARGV_ADD("--arch");      ARGV_ADD(bflat_arch);
        ARGV_ADD("--os");        ARGV_ADD("linux");
        ARGV_ADD("--libc");      ARGV_ADD(bflat_libc);
        if (bflat_stdlib != NULL)
        {
            ARGV_ADD("--stdlib"); ARGV_ADD(bflat_stdlib);
        }
        if (no_globalization)
            ARGV_ADD("--no-globalization");
        if (no_stacktrace_data)
            ARGV_ADD("--no-stacktrace-data");
        if (no_pthread)
            ARGV_ADD("--no-pthread");
        if (no_pie)
            ARGV_ADD("--no-pie");
        ARGV_ADD(remote_cs_path.ptr);
        ARGV_ADD("--out"); ARGV_ADD(remote_out.ptr);
        ARGV_ADD(NULL);

#undef ARGV_ADD

        job = ts_container_run(&container, bflat_argv);
    }
    if (job == NULL)
        TEST_FAIL("Failed to create bflat build job");

    TEST_STEP("Attach output channels for logging");
    CHECK_RC(tapi_job_alloc_output_channels(job, 2, out_channels));
    CHECK_RC(tapi_job_attach_filter(TAPI_JOB_CHANNEL_SET(out_channels[0]),
                                    "bflat stdout", false, TE_LL_RING, NULL));
    CHECK_RC(tapi_job_attach_filter(TAPI_JOB_CHANNEL_SET(out_channels[1]),
                                    "bflat stderr", false, TE_LL_ERROR, NULL));

    TEST_STEP("Start bflat build");
    CHECK_RC(tapi_job_start(job));

    TEST_STEP("Wait for bflat to finish (timeout %d ms)", BUILD_TIMEOUT_MS);
    CHECK_RC(tapi_job_wait(job, BUILD_TIMEOUT_MS, &status));

    TEST_STEP("Check exit status");
    if (status.type != TAPI_JOB_STATUS_EXITED)
        TEST_FAIL("bflat was killed by a signal (signo=%d)", status.value);
    if (status.value != 0)
        TEST_FAIL("bflat exited with non-zero status %d", status.value);

    RING("bflat successfully compiled '%s' (arch=%s libc=%s)",
         cs_file, bflat_arch, bflat_libc);

    if (run)
    {
        /* The container mounts src_dir as CONTAINER_SRC_DIR, so the binary
         * path on the agent is src_dir + suffix after CONTAINER_SRC_DIR. */
        CHECK_RC(te_string_append(&agent_binary_path, "%s%s",
                                  src_dir,
                                  remote_out.ptr + strlen(CONTAINER_SRC_DIR)));

        TEST_STEP("Create qemu runner (qemu='%s')",
                  qemu_path != NULL ? qemu_path : TSAPI_QEMU_DEFAULT_PATH);
        CHECK_RC(tsapi_qemu_runner_create(rpcs, qemu_path, &qemu));
        qemu_created = true;

        TEST_STEP("Run '%s' under qemu", agent_binary_path.ptr);
        run_job = tsapi_qemu_run(&qemu, agent_binary_path.ptr, NULL);
        if (run_job == NULL)
            TEST_FAIL("Failed to create qemu run job for '%s'",
                      agent_binary_path.ptr);

        CHECK_RC(tapi_job_alloc_output_channels(run_job, 2, run_channels));
        CHECK_RC(tapi_job_attach_filter(
                     TAPI_JOB_CHANNEL_SET(run_channels[0]),
                     "qemu stdout", false, TE_LL_RING, NULL));
        CHECK_RC(tapi_job_attach_filter(
                     TAPI_JOB_CHANNEL_SET(run_channels[1]),
                     "qemu stderr", false, TE_LL_ERROR, NULL));

        CHECK_RC(tapi_job_start(run_job));

        TEST_STEP("Wait for qemu to finish (timeout %d ms)", RUN_TIMEOUT_MS);
        CHECK_RC(tapi_job_wait(run_job, RUN_TIMEOUT_MS, &run_status));

        if (run_status.type != TAPI_JOB_STATUS_EXITED)
            TEST_FAIL("Binary was killed by signal (signo=%d)",
                      run_status.value);
        if (run_status.value != 0)
            TEST_FAIL("Binary exited with non-zero status %d",
                      run_status.value);

        RING("Binary ran successfully under qemu");
    }

    TEST_SUCCESS;

cleanup:
    if (run_job != NULL)
        CLEANUP_CHECK_RC(tapi_job_destroy(run_job, -1));

    if (qemu_created)
        tsapi_qemu_runner_destroy(&qemu);

    if (job != NULL)
        CLEANUP_CHECK_RC(tapi_job_destroy(job, -1));

    if (container_created)
        ts_container_destroy(&container);

    if (src_dir != NULL && rpcs != NULL)
    {
        tarpc_pid_t pid;

        RPC_AWAIT_ERROR(rpcs);
        pid = rpc_te_shell_cmd(rpcs, "rm -rf %s", -1, NULL, NULL, NULL,
                               src_dir);
        if (pid > 0)
        {
            RPC_AWAIT_ERROR(rpcs);
            rpc_waitpid(rpcs, pid, NULL, 0);
        }
    }

    te_string_free(&local_cs_path);
    te_string_free(&remote_cs_path);
    te_string_free(&remote_out);
    te_string_free(&agent_binary_path);
    free(test_dir);
    free(src_dir);

    if (rpcs != NULL)
        CLEANUP_CHECK_RC(rcf_rpc_server_destroy(rpcs));

    TEST_END;
}