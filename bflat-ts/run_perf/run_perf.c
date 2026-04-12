/** @file
 * @brief bflat run-performance test — startup steps analysis
 *
 * Compiles a C# source with bflat (inside a Docker container), then runs
 * the resulting riscv64 ELF under @c ziskemu @c -v (verbose trace mode)
 * inside a Zisk Docker container.  The full instruction trace is saved to
 * a file in the shared volume, which is then analysed on the agent with
 * @c zisk_startup_steps.py to measure:
 *
 *   - @b startup_steps  — instruction count from @c _start until
 *                         @c __managed__Main is first entered (i.e. the
 *                         entire .NET NativeAOT runtime initialisation)
 *   - @b total_steps    — total instruction count of the whole execution
 *
 * Both metrics are reported through @c RING() so they appear in the TE
 * verdict log and can be scraped for trend analysis.
 *
 * @param ta               Test Agent (must have Docker and Python 3 + readelf)
 * @param cs_file          C# source filename (from the @c cs/ sub-directory)
 * @param bflat_image      Docker image providing the bflat compiler
 * @param bflat_arch       Target architecture for bflat (@c riscv64)
 * @param bflat_libc       Must be @c zisk (real zkVM emulator required)
 * @param bflat_stdlib     C# stdlib (@c dotnet / @c zero, or omit)
 * @param verbose          Pass @c --verbose to bflat if @c TRUE
 * @param no_globalization Pass @c --no-globalization if @c TRUE
 * @param no_stacktrace_data Pass @c --no-stacktrace-data if @c TRUE
 * @param no_pthread       Pass @c --no-pthread if @c TRUE
 * @param no_pie           Pass @c --no-pie if @c TRUE
 * @param bflat_extlib     Value for @c --extlib (ziskos library)
 * @param zisk_image       Zisk Docker image, or empty for the default
 * @param run_timeout_ms   Timeout for the ziskemu run (ms); 0 = default
 * @param zisk_ta          Agent to run Zisk container on; if omitted uses @p ta
 *
 * @par Output metric (RING):
 * @code
 *   run_perf: cs=<file> startup_steps=<N> total_steps=<M>
 * @endcode
 *
 * Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
 *
 * @author Maksim Menshikov <maksim.menshikov@nethermind.io>
 */

#ifndef TE_TEST_NAME
#define TE_TEST_NAME "run_perf"
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
#include "tsapi_zisk.h"
#include "te_mi_log.h"

#include <stdio.h>
#include <time.h>
#include <unistd.h>
#include <limits.h>

/** Path inside the Zisk container where host binaries are mounted */
#define CONTAINER_BIN_DIR   "/n"

/** Path inside the bflat container where sources are mounted */
#define CONTAINER_SRC_DIR   "/src"

/** Build timeout: 10 minutes */
#define BUILD_TIMEOUT_MS    (10 * 60 * 1000)

/** Default ziskemu run timeout: 30 minutes (full proof generation is slow) */
#define RUN_TIMEOUT_MS_DEFAULT  (30 * 60 * 1000)

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
    const char         *zisk_image;
    int                 run_timeout_ms;
    const char         *zisk_ta = NULL;
    rcf_rpc_server     *zisk_rpcs = NULL;

    rcf_rpc_server     *rpcs              = NULL;
    char               *test_dir          = NULL;
    char               *src_dir           = NULL;
    char               *zisk_src_dir      = NULL;
    bool                zisk_src_created  = false;
    ts_container        bflat_container   = TS_CONTAINER_INIT;
    ts_container_params_docker bflat_dp   = TS_CONTAINER_PARAMS_DOCKER;
    tapi_job_t         *build_job         = NULL;
    tapi_job_t         *run_job           = NULL;
    tapi_job_channel_t  *out_ch[2];
    tapi_job_channel_t  *run_ch[2];
    tapi_job_status_t   build_status;
    tapi_job_status_t   run_status;
    bool                bflat_created     = false;
    tsapi_zisk_runner   zisk              = TSAPI_ZISK_RUNNER_INIT;
    bool                zisk_created      = false;

    te_string   local_cs_path     = TE_STRING_INIT;
    te_string   remote_cs_path    = TE_STRING_INIT;
    te_string   remote_out        = TE_STRING_INIT;
    te_string   trace_path_agent  = TE_STRING_INIT;
    te_string   results_path_agent= TE_STRING_INIT;
    te_string   results_path_local= TE_STRING_INIT;
    te_string   run_cmd           = TE_STRING_INIT;

    long long   startup_steps   = -1;
    long long   total_steps     = -1;
    long long   run_elapsed_ms  = -1;

    struct timespec t_run_start, t_run_end;

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
    TEST_GET_OPT_STRING_PARAM(bflat_extlib);
    TEST_GET_OPT_STRING_PARAM(zisk_image);
    TEST_GET_INT_PARAM(run_timeout_ms);
    TEST_GET_TA(zisk, zisk_ta);

    /* ------------------------------------------------------------------ */
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

    /* ------------------------------------------------------------------ */
    TEST_STEP("Create RPC server on agent '%s'", ta);
    CHECK_RC(rcf_rpc_server_create(ta, "rpcs_run_perf_build", &rpcs));

    TEST_STEP("Create temporary source directory on agent");
    src_dir = tapi_file_make_custom_pathname(NULL, "/tmp", NULL);
    if (src_dir == NULL)
        TEST_FAIL("Failed to generate temporary directory name");

    RPC_AWAIT_ERROR(rpcs);
    if (rpc_mkdir(rpcs, src_dir, RPC_S_IRWXU) != 0)
        TEST_FAIL("Failed to create temporary directory '%s'", src_dir);

    TEST_STEP("Copy '%s' to agent '%s:%s'", local_cs_path.ptr, ta, src_dir);
    {
        te_string agent_cs_path = TE_STRING_INIT;

        CHECK_RC(te_string_append(&agent_cs_path, "%s/%s", src_dir, cs_file));
        CHECK_RC(tapi_file_copy_ta(NULL, local_cs_path.ptr,
                                   ta, agent_cs_path.ptr));
        te_string_free(&agent_cs_path);
    }

    /* ------------------------------------------------------------------ */
    TEST_STEP("Create bflat Docker container from image '%s'", bflat_image);
    bflat_dp.params.name     = bflat_image;
    bflat_dp.params.prebuilt = TRUE;
    CHECK_RC(ts_container_create(rpcs, TS_CONTAINER_TYPE_DOCKER,
                                 &bflat_dp.params, &bflat_container));
    bflat_created = true;

    TEST_STEP("Share '%s' -> '%s' in bflat container", src_dir, CONTAINER_SRC_DIR);
    CHECK_RC(ts_container_share_folder(&bflat_container,
                                       src_dir, CONTAINER_SRC_DIR));

    TEST_STEP("Build bflat job: arch=%s libc=%s extlib=%s file=%s",
              bflat_arch, bflat_libc,
              bflat_extlib != NULL ? bflat_extlib : "(none)",
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
        if (no_globalization)    ARGV_ADD("--no-globalization");
        if (no_stacktrace_data)  ARGV_ADD("--no-stacktrace-data");
        if (no_pthread)          ARGV_ADD("--no-pthread");
        if (no_pie)              ARGV_ADD("--no-pie");
        if (bflat_extlib != NULL)
        {
            ARGV_ADD("--extlib"); ARGV_ADD(bflat_extlib);
        }
        ARGV_ADD(remote_cs_path.ptr);
        ARGV_ADD("--out"); ARGV_ADD(remote_out.ptr);
        ARGV_ADD(NULL);
#undef ARGV_ADD

        build_job = ts_container_run(&bflat_container, bflat_argv);
    }
    if (build_job == NULL)
        TEST_FAIL("Failed to create bflat build job");

    TEST_STEP("Attach output channels for build logging");
    CHECK_RC(tapi_job_alloc_output_channels(build_job, 2, out_ch));
    CHECK_RC(tapi_job_attach_filter(TAPI_JOB_CHANNEL_SET(out_ch[0]),
                                    "bflat stdout", false, TE_LL_RING, NULL));
    CHECK_RC(tapi_job_attach_filter(TAPI_JOB_CHANNEL_SET(out_ch[1]),
                                    "bflat stderr", false, TE_LL_ERROR, NULL));

    TEST_STEP("Start bflat build");
    CHECK_RC(tapi_job_start(build_job));

    TEST_STEP("Wait for bflat build (timeout %d ms)", BUILD_TIMEOUT_MS);
    CHECK_RC(tapi_job_wait(build_job, BUILD_TIMEOUT_MS, &build_status));

    if (build_status.type != TAPI_JOB_STATUS_EXITED)
        TEST_FAIL("bflat killed by signal %d", build_status.value);
    if (build_status.value != 0)
        TEST_FAIL("bflat exited with non-zero status %d", build_status.value);

    RING("Build succeeded: cs=%s arch=%s libc=%s", cs_file, bflat_arch, bflat_libc);

    /* ------------------------------------------------------------------ */
    /* Zisk run: use a potentially separate agent for heavy zkVM work       */
    /* ------------------------------------------------------------------ */
    {
        rcf_rpc_server *run_rpcs  = rpcs;
        const char     *run_src   = src_dir;

        if (zisk_ta != NULL && strcmp(zisk_ta, ta) != 0)
        {
            TEST_STEP("Create RPC server on Zisk agent '%s'", zisk_ta);
            CHECK_RC(rcf_rpc_server_create(zisk_ta, "rpcs_run_perf_zisk",
                                           &zisk_rpcs));

            TEST_STEP("Create temporary directory on Zisk agent '%s'", zisk_ta);
            zisk_src_dir = tapi_file_make_custom_pathname(NULL, "/tmp", NULL);
            if (zisk_src_dir == NULL)
                TEST_FAIL("Failed to generate temp dir name for zisk_ta");

            RPC_AWAIT_ERROR(zisk_rpcs);
            if (rpc_mkdir(zisk_rpcs, zisk_src_dir, RPC_S_IRWXU) != 0)
                TEST_FAIL("Failed to create dir '%s' on '%s'",
                          zisk_src_dir, zisk_ta);
            zisk_src_created = true;

            TEST_STEP("Copy binary from '%s' to Zisk agent '%s'", ta, zisk_ta);
            {
                const char *stem_end = strrchr(cs_file, '.');
                int         stem_len = (stem_end != NULL)
                                       ? (int)(stem_end - cs_file)
                                       : (int)strlen(cs_file);
                te_string src_p = TE_STRING_INIT;
                te_string dst_p = TE_STRING_INIT;

                CHECK_RC(te_string_append(&src_p, "%s/%.*s.patched",
                                          src_dir, stem_len, cs_file));
                CHECK_RC(te_string_append(&dst_p, "%s/%.*s.patched",
                                          zisk_src_dir, stem_len, cs_file));
                CHECK_RC(tapi_file_copy_ta(ta, src_p.ptr,
                                           zisk_ta, dst_p.ptr));
                te_string_free(&src_p);
                te_string_free(&dst_p);
            }

            run_rpcs = zisk_rpcs;
            run_src  = zisk_src_dir;
        }

        TEST_STEP("Create Zisk runner (image='%s') on agent '%s'",
                  zisk_image != NULL ? zisk_image : TSAPI_ZISK_DEFAULT_IMAGE,
                  run_rpcs->ta);
        CHECK_RC(tsapi_zisk_runner_create(run_rpcs, zisk_image, &zisk));
        zisk_created = true;

        /* Build the stem from cs_file (strip extension) */
        {
            const char *stem_end = strrchr(cs_file, '.');
            int         stem_len = (stem_end != NULL)
                                   ? (int)(stem_end - cs_file)
                                   : (int)strlen(cs_file);

            /*
             * bflat post-processes the zisk ELF with patch_elf.py,
             * producing <stem>.patched — that is the file ziskemu must run.
             * The raw <stem> ELF is an intermediate artifact and will cause
             * ziskemu to exit with status 101 if used directly.
             */

            /* Trace file ends up in run_src on the agent */
            CHECK_RC(te_string_append(&trace_path_agent,
                                      "%s/%.*s.patched.trace",
                                      run_src, stem_len, cs_file));
            CHECK_RC(te_string_append(&results_path_agent,
                                      "%s/%.*s.patched.startup",
                                      run_src, stem_len, cs_file));

            /*
             * Run command inside Zisk container:
             *   sh -c 'ziskemu -v -e /n/<stem>.patched \
             *             > /n/<stem>.patched.trace 2>&1'
             *
             * The shared mount point is CONTAINER_BIN_DIR (/n).
             * Using sh -c lets us redirect output to the shared volume.
             */
            CHECK_RC(te_string_append(&run_cmd,
                                      "ziskemu -v -e " CONTAINER_BIN_DIR
                                      "/%.*s.patched > " CONTAINER_BIN_DIR
                                      "/%.*s.patched.trace 2>&1",
                                      stem_len, cs_file,
                                      stem_len, cs_file));
        }

        TEST_STEP("Create Zisk run job (verbose trace -> '%s')",
                  trace_path_agent.ptr);
        {
            const char *zisk_argv[] = {
                "sh", "-c", run_cmd.ptr, NULL
            };
            run_job = tsapi_zisk_run(&zisk, run_src, "", NULL);
            /* Override: run_job from tsapi_zisk_run would try to run
             * ziskemu directly; we need sh -c for redirection. */
            if (run_job != NULL)
            {
                CLEANUP_CHECK_RC(tapi_job_destroy(run_job, -1));
                run_job = NULL;
            }
            run_job = ts_container_run(&zisk.container, zisk_argv);
        }
        if (run_job == NULL)
            TEST_FAIL("Failed to create Zisk run job");

        TEST_STEP("Attach output channels for run logging");
        CHECK_RC(tapi_job_alloc_output_channels(run_job, 2, run_ch));
        CHECK_RC(tapi_job_attach_filter(TAPI_JOB_CHANNEL_SET(run_ch[0]),
                                        "zisk stdout", false, TE_LL_RING, NULL));
        CHECK_RC(tapi_job_attach_filter(TAPI_JOB_CHANNEL_SET(run_ch[1]),
                                        "zisk stderr", false, TE_LL_ERROR, NULL));

        TEST_STEP("Start ziskemu (wall-clock timer starts now)");
        clock_gettime(CLOCK_MONOTONIC, &t_run_start);
        CHECK_RC(tapi_job_start(run_job));

        {
            int      eff_timeout = (run_timeout_ms > 0) ? run_timeout_ms
                                                        : RUN_TIMEOUT_MS_DEFAULT;
            te_errno wait_rc;

            TEST_STEP("Wait for ziskemu (timeout %d ms)", eff_timeout);
            wait_rc = tapi_job_wait(run_job, eff_timeout, &run_status);
            if (TE_RC_GET_ERROR(wait_rc) == TE_EINPROGRESS)
                TEST_FAIL("ziskemu timed out after %d ms", eff_timeout);
            CHECK_RC(wait_rc);
        }
        clock_gettime(CLOCK_MONOTONIC, &t_run_end);
        run_elapsed_ms =
            (long long)(t_run_end.tv_sec  - t_run_start.tv_sec)  * 1000LL +
            (long long)(t_run_end.tv_nsec - t_run_start.tv_nsec) / 1000000LL;

        if (run_status.type != TAPI_JOB_STATUS_EXITED)
            TEST_FAIL("ziskemu killed by signal %d", run_status.value);
        if (run_status.value != 0)
            TEST_FAIL("ziskemu exited with non-zero status %d", run_status.value);

        RING("ziskemu finished; trace written to '%s'", trace_path_agent.ptr);

        /* ---------------------------------------------------------------- */
        /* Analysis: count startup steps on the agent                        */
        /* ---------------------------------------------------------------- */
        TEST_STEP("Run startup-steps analysis on agent");
        {
            const char *stem_end = strrchr(cs_file, '.');
            int         stem_len = (stem_end != NULL)
                                   ? (int)(stem_end - cs_file)
                                   : (int)strlen(cs_file);
            te_string   elf_path = TE_STRING_INIT;
            tarpc_pid_t pid;

            /* Use .patched ELF for symbol resolution — it retains the
             * full .symtab and is the binary that actually ran. */
            CHECK_RC(te_string_append(&elf_path, "%s/%.*s.patched",
                                      run_src, stem_len, cs_file));

            RPC_AWAIT_ERROR(run_rpcs);
            pid = rpc_te_shell_cmd(
                run_rpcs,
                "python3 %s/zisk_startup_steps.py"
                " --elf %s --trace %s > %s 2>&1",
                -1, NULL, NULL, NULL,
                test_dir,
                elf_path.ptr,
                trace_path_agent.ptr,
                results_path_agent.ptr);
            if (pid > 0)
            {
                RPC_AWAIT_ERROR(run_rpcs);
                rpc_waitpid(run_rpcs, pid, NULL, 0);
            }

            te_string_free(&elf_path);
        }

        /* Copy results file back to local temp */
        {
            char *local_tmp = tapi_file_make_custom_pathname(NULL, "/tmp", NULL);
            if (local_tmp != NULL)
            {
                CHECK_RC(te_string_append(&results_path_local, "%s", local_tmp));
                free(local_tmp);
            }
            else
            {
                CHECK_RC(te_string_append(&results_path_local,
                                          "/tmp/run_perf_startup.txt"));
            }

            if (tapi_file_copy_ta(run_rpcs->ta, results_path_agent.ptr,
                                  NULL, results_path_local.ptr) == 0)
            {
                FILE  *fh;
                char   line[256];

                fh = fopen(results_path_local.ptr, "r");
                if (fh != NULL)
                {
                    while (fgets(line, sizeof(line), fh) != NULL)
                    {
                        long long su = -1, tot = -1;
                        if (sscanf(line, "startup_steps=%lld total_steps=%lld",
                                   &su, &tot) == 2)
                        {
                            startup_steps = su;
                            total_steps   = tot;
                            break;
                        }
                    }
                    fclose(fh);
                }
                unlink(results_path_local.ptr);
            }
            else
            {
                WARN("Could not copy startup results file; metrics unavailable");
            }
        }
    }

    RING("run_perf: cs=%s startup_steps=%lld total_steps=%lld run_elapsed_ms=%lld",
         cs_file, startup_steps, total_steps, run_elapsed_ms);

    if (startup_steps >= 0 && total_steps >= 0)
    {
        te_mi_logger *logger;

        if (te_mi_logger_meas_create("bflat run", &logger) == 0)
        {
            te_mi_logger_add_meas(logger, NULL, TE_MI_MEAS_TIME,
                                  "Run time",
                                  TE_MI_MEAS_AGGR_SINGLE,
                                  (double)run_elapsed_ms,
                                  TE_MI_MEAS_MULTIPLIER_MILLI);
            te_mi_logger_add_meas(logger, NULL, TE_MI_MEAS_UNITLESS_VALUE,
                                  "Startup steps",
                                  TE_MI_MEAS_AGGR_SINGLE,
                                  (double)startup_steps,
                                  TE_MI_MEAS_MULTIPLIER_PLAIN);
            te_mi_logger_add_meas(logger, NULL, TE_MI_MEAS_UNITLESS_VALUE,
                                  "Total steps",
                                  TE_MI_MEAS_AGGR_SINGLE,
                                  (double)total_steps,
                                  TE_MI_MEAS_MULTIPLIER_PLAIN);
            te_mi_logger_add_meas(logger, NULL, TE_MI_MEAS_UNITLESS_VALUE,
                                  "Main steps",
                                  TE_MI_MEAS_AGGR_SINGLE,
                                  (double)(total_steps - startup_steps),
                                  TE_MI_MEAS_MULTIPLIER_PLAIN);
            te_mi_logger_destroy(logger);
        }
    }

    TEST_SUCCESS;

cleanup:
    if (run_job != NULL)
        CLEANUP_CHECK_RC(tapi_job_destroy(run_job, -1));

    if (zisk_created)
        tsapi_zisk_runner_destroy(&zisk);

    if (zisk_src_created && zisk_rpcs != NULL)
    {
        tarpc_pid_t pid;

        RPC_AWAIT_ERROR(zisk_rpcs);
        pid = rpc_te_shell_cmd(zisk_rpcs, "rm -rf %s", -1,
                               NULL, NULL, NULL, zisk_src_dir);
        if (pid > 0)
        {
            RPC_AWAIT_ERROR(zisk_rpcs);
            rpc_waitpid(zisk_rpcs, pid, NULL, 0);
        }
    }
    free(zisk_src_dir);

    if (zisk_rpcs != NULL)
        CLEANUP_CHECK_RC(rcf_rpc_server_destroy(zisk_rpcs));

    if (build_job != NULL)
        CLEANUP_CHECK_RC(tapi_job_destroy(build_job, -1));

    if (bflat_created)
        ts_container_destroy(&bflat_container);

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
    te_string_free(&trace_path_agent);
    te_string_free(&results_path_agent);
    te_string_free(&results_path_local);
    te_string_free(&run_cmd);
    free(test_dir);
    free(src_dir);

    if (rpcs != NULL)
        CLEANUP_CHECK_RC(rcf_rpc_server_destroy(rpcs));

    TEST_END;
}