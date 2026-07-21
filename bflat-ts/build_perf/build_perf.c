/** @file
 * @brief bflat build-performance test
 *
 * Compiles a C# source file using bflat inside a Docker container and
 * measures the wall-clock time of the compilation step.  The compiled
 * binary is never executed; only the build time matters.
 *
 * @par Parameters (same as build.c minus run/qemu/zisk):
 * @param ta               Test Agent name (must have Docker available)
 * @param cs_file          C# source filename (from the cs/ sub-directory)
 * @param bflat_image      Docker image providing the bflat compiler
 * @param bflat_arch       Target architecture passed to @c --arch
 * @param bflat_libc       Target libc passed to @c --libc
 * @param bflat_stdlib     C# stdlib (@c dotnet / @c zero / omit)
 * @param verbose          Pass @c --verbose to bflat if @c TRUE
 * @param no_globalization Pass @c --no-globalization if @c TRUE
 * @param no_stacktrace_data Pass @c --no-stacktrace-data if @c TRUE
 * @param no_pthread       Pass @c --no-pthread if @c TRUE
 * @param no_pie           Pass @c --no-pie if @c TRUE
 * @param bflat_extlib     Value for @c --extlib (empty to omit)
 *
 * @par Measured metric:
 * The wall-clock duration from @c tapi_job_start to @c tapi_job_wait
 * completion is reported as:
 * @code
 *   build_perf: cs=<file> arch=<arch> libc=<libc> build_time_ms=<N>
 * @endcode
 *
 * Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
 *
 * @author Maksim Menshikov <maksim.menshikov@nethermind.io>
 */

#ifndef TE_TEST_NAME
#define TE_TEST_NAME "build_perf"
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
#include "te_mi_log.h"

#include <time.h>
#include <unistd.h>
#include <limits.h>

/** Path inside the container where sources are mounted */
#define CONTAINER_SRC_DIR  "/src"

/** Build timeout: 10 minutes to accommodate large source files */
#define BUILD_TIMEOUT_MS  (10 * 60 * 1000)

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
    const char         *bflat_extlib;

    rcf_rpc_server     *rpcs              = NULL;
    char               *test_dir          = NULL;
    char               *src_dir           = NULL;
    ts_container        container         = TS_CONTAINER_INIT;
    ts_container_params_docker dp         = TS_CONTAINER_PARAMS_DOCKER;
    tapi_job_t         *job               = NULL;
    tapi_job_channel_t *out_channels[2];
    tapi_job_status_t   status;
    bool                container_created = false;
    int                 max_build_time_ms = 0;

    te_string           local_cs_path  = TE_STRING_INIT;
    te_string           remote_cs_path = TE_STRING_INIT;
    te_string           remote_out     = TE_STRING_INIT;

    struct timespec     t_start, t_end;
    long long           build_elapsed_ms = -1;

    TEST_START;
    TEST_GET_STRING_PARAM(ta);
    TEST_GET_STRING_PARAM(cs_file);
    TEST_GET_OPT_STRING_PARAM(bflat_image);
    bflat_image = tsapi_bflat_resolve_image(bflat_image);
    TEST_GET_STRING_PARAM(bflat_arch);
    TEST_GET_STRING_PARAM(bflat_libc);
    TEST_GET_OPT_STRING_PARAM(bflat_stdlib);
    TEST_GET_BOOL_PARAM(verbose);
    TEST_GET_BOOL_PARAM(no_globalization);
    TEST_GET_BOOL_PARAM(no_stacktrace_data);
    TEST_GET_BOOL_PARAM(no_pthread);
    TEST_GET_BOOL_PARAM(no_pie);
    TEST_GET_OPT_STRING_PARAM(bflat_extlib);
    TEST_GET_INT_PARAM(max_build_time_ms);

    TEST_STEP("Resolve local path to '%s'", cs_file);
    {
        char    exe_buf[PATH_MAX];
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
    CHECK_RC(rcf_rpc_server_create(ta, "rpcs_build_perf", &rpcs));

    TEST_STEP("Create temporary source directory on agent");
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

    TEST_STEP("Create Docker container from image '%s'", bflat_image);
    dp.params.name     = bflat_image;
    dp.params.prebuilt = TRUE;
    CHECK_RC(ts_container_create(rpcs, TS_CONTAINER_TYPE_DOCKER,
                                 &dp.params, &container));
    container_created = true;

    TEST_STEP("Share '%s' -> '%s'", src_dir, CONTAINER_SRC_DIR);
    CHECK_RC(ts_container_share_folder(&container,
                                       src_dir, CONTAINER_SRC_DIR));

    TEST_STEP("Build bflat job: arch=%s libc=%s stdlib=%s extlib=%s file=%s",
              bflat_arch, bflat_libc,
              bflat_stdlib  != NULL ? bflat_stdlib  : "(default)",
              bflat_extlib  != NULL ? bflat_extlib  : "(none)",
              cs_file);
    {
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
        if (bflat_extlib != NULL && bflat_libc != NULL &&
            strcmp(bflat_libc, "zisk") == 0)
        {
            ARGV_ADD("--extlib"); ARGV_ADD(bflat_extlib);
        }
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

    TEST_STEP("Start bflat (wall-clock timer starts now)");
    clock_gettime(CLOCK_MONOTONIC, &t_start);
    CHECK_RC(tapi_job_start(job));

    TEST_STEP("Wait for bflat (timeout %d ms)", BUILD_TIMEOUT_MS);
    CHECK_RC(tapi_job_wait(job, BUILD_TIMEOUT_MS, &status));
    clock_gettime(CLOCK_MONOTONIC, &t_end);

    build_elapsed_ms =
        (long long)(t_end.tv_sec  - t_start.tv_sec)  * 1000LL +
        (long long)(t_end.tv_nsec - t_start.tv_nsec) / 1000000LL;

    TEST_STEP("Check exit status");
    if (status.type != TAPI_JOB_STATUS_EXITED)
        TEST_FAIL("bflat was killed by a signal (signo=%d)", status.value);
    if (status.value != 0)
        TEST_FAIL("bflat exited with non-zero status %d", status.value);

    TEST_ARTIFACT("Build time: %lld ms (cs=%s arch=%s libc=%s)",
                  build_elapsed_ms, cs_file, bflat_arch, bflat_libc);

    TEST_STEP("Check thresholds");
    if (max_build_time_ms > 0 && build_elapsed_ms >= 0 &&
        build_elapsed_ms > (long long)max_build_time_ms)
    {
        TEST_VERDICT("Build time exceeds threshold");
    }

    {
        te_mi_logger *logger;

        if (te_mi_logger_meas_create("bflat build", &logger) == 0)
        {
            te_mi_logger_add_meas(logger, NULL, TE_MI_MEAS_TIME,
                                  "Build time",
                                  TE_MI_MEAS_AGGR_SINGLE,
                                  (double)build_elapsed_ms,
                                  TE_MI_MEAS_MULTIPLIER_MILLI);
            te_mi_logger_destroy(logger);
        }
    }

    TEST_SUCCESS;

cleanup:
    if (job != NULL)
        CLEANUP_CHECK_RC(tapi_job_destroy(job, -1));

    if (container_created)
        ts_container_destroy(&container);

    if (src_dir != NULL && rpcs != NULL)
    {
        tarpc_pid_t pid;

        RPC_AWAIT_ERROR(rpcs);
        pid = rpc_te_shell_cmd(rpcs, "rm -rf %s", -1,
                               NULL, NULL, NULL, src_dir);
        if (pid > 0)
        {
            RPC_AWAIT_ERROR(rpcs);
            rpc_waitpid(rpcs, pid, NULL, 0);
        }
    }

    te_string_free(&local_cs_path);
    te_string_free(&remote_cs_path);
    te_string_free(&remote_out);
    free(test_dir);
    free(src_dir);

    if (rpcs != NULL)
        CLEANUP_CHECK_RC(rcf_rpc_server_destroy(rpcs));

    TEST_END;
}