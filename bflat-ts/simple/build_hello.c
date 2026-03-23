/** @file
 * @brief Simple bflat build test
 *
 * Test that compiles a C# source file using bflat inside a Docker container.
 *
 * Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
 *
 * @author Maksim Menshikov <maksim.menshikov@nethermind.io>
 */

/** @page bflat_ts_simple_build_hello Build a minimal C# program with bflat
 *
 * @objective Verify that bflat can successfully compile a minimal C# source
 *            file targeting riscv64/linux inside the bflat-riscv64 Docker
 *            image.
 *
 * @param ta    Test Agent name (must have Docker available)
 *
 * @par Scenario:
 *
 */

#define TE_TEST_NAME "simple/build_hello"

#include "te_config.h"
#include "tapi_test.h"
#include "tapi_job.h"
#include "tapi_file.h"
#include "rcf_rpc.h"
#include "tapi_rpc_unistd.h"
#include "tapi_rpc_stdio.h"
#include "tapi_rpc_signal.h"
#include "ts_container.h"

/** Docker image that provides the bflat compiler for riscv64 */
#define BFLAT_IMAGE  "nethermindeth/bflat-riscv64:latest"

/** Path inside the container where sources are mounted */
#define CONTAINER_SRC_DIR  "/src"

/** Build timeout: 5 minutes should be enough even on a slow machine */
#define BUILD_TIMEOUT_MS  (5 * 60 * 1000)

/** Minimal C# program using top-level statements */
static const char hello_cs[] =
    "using System;\n"
    "Console.WriteLine(\"Hello, bflat!\");\n";

int
main(int argc, char **argv)
{
    const char         *ta;
    rcf_rpc_server     *rpcs              = NULL;
    char               *src_dir           = NULL;
    ts_container        container         = TS_CONTAINER_INIT;
    ts_container_params_docker dp         = TS_CONTAINER_PARAMS_DOCKER;
    tapi_job_t         *job               = NULL;
    tapi_job_channel_t *out_channels[2];
    tapi_job_status_t   status;
    bool                container_created = false;

    const char *bflat_argv[] = {
        "bflat", "build",
        "-x",
        "--verbose",
        "--arch",              "riscv64",
        "--os",                "linux",
        "--libc",              "musl",
        "--no-globalization",
        "--no-stacktrace-data",
        CONTAINER_SRC_DIR "/hello.cs",
        "--out", CONTAINER_SRC_DIR "/hello",
        NULL
    };

    TEST_START;
    TEST_GET_STRING_PARAM(ta);

    TEST_STEP("Create RPC server on agent '%s'", ta);
    CHECK_RC(rcf_rpc_server_create(ta, "rpcs_build_hello", &rpcs));

    TEST_STEP("Create temporary source directory on the agent");
    src_dir = tapi_file_make_custom_pathname(NULL, "/tmp", NULL);
    if (src_dir == NULL)
        TEST_FAIL("Failed to generate temporary directory name");

    RPC_AWAIT_ERROR(rpcs);
    if (rpc_mkdir(rpcs, src_dir, RPC_S_IRWXU) != 0)
        TEST_FAIL("Failed to create temporary directory '%s'", src_dir);

    TEST_STEP("Write hello.cs to the temporary directory");
    {
        te_string hello_path = TE_STRING_INIT;

        CHECK_RC(te_string_append(&hello_path, "%s/hello.cs", src_dir));
        CHECK_RC(tapi_file_create_ta(ta, hello_path.ptr, "%s", hello_cs));
        te_string_free(&hello_path);
    }

    TEST_STEP("Create Docker container backed by bflat image (%s)", BFLAT_IMAGE);
    dp.params.name     = BFLAT_IMAGE;
    dp.params.prebuilt = TRUE;
    CHECK_RC(ts_container_create(rpcs, TS_CONTAINER_TYPE_DOCKER,
                                 &dp.params, &container));
    container_created = true;

    TEST_STEP("Share source directory '%s' -> '%s'",
              src_dir, CONTAINER_SRC_DIR);
    CHECK_RC(ts_container_share_folder(&container,
                                       src_dir, CONTAINER_SRC_DIR));

    TEST_STEP("Create bflat build job");
    job = ts_container_run(&container, bflat_argv);
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

    RING("bflat successfully compiled hello.cs");

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
        pid = rpc_te_shell_cmd(rpcs, "rm -rf %s", -1, NULL, NULL, NULL,
                               src_dir);
        if (pid > 0)
        {
            RPC_AWAIT_ERROR(rpcs);
            rpc_waitpid(rpcs, pid, NULL, 0);
        }
        free(src_dir);
    }

    if (rpcs != NULL)
        CLEANUP_CHECK_RC(rcf_rpc_server_destroy(rpcs));

    TEST_END;
}