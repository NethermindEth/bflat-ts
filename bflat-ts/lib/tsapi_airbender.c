/** @file
 * @brief Bflat Test Suite
 *
 * Airbender simulator runner implementation
 *
 * Copyright (C) 2026 Demerzel Solutions Limited (Nethermind)
 *
 * @author Maksim Menshikov <maksim.menshikov@nethermind.io>
 */
#include <string.h>

#include "te_config.h"
#include "te_errno.h"
#include "te_string.h"
#include "tapi_job.h"
#include "tapi_test.h"
#include "tsapi_airbender.h"

/* See description in tsapi_airbender.h */
te_errno
tsapi_airbender_runner_create(rcf_rpc_server         *rpcs,
                              const char             *image,
                              tsapi_airbender_runner *runner)
{
    ts_container_params_docker dp = TS_CONTAINER_PARAMS_DOCKER;
    te_errno rc;

    if (runner == NULL)
        return TE_EINVAL;

    if (image == NULL)
        image = TSAPI_AIRBENDER_DEFAULT_IMAGE;

    dp.params.name     = image;
    dp.params.prebuilt = TRUE;

    rc = ts_container_create(rpcs, TS_CONTAINER_TYPE_DOCKER,
                             &dp.params, &runner->container);
    if (rc != 0)
    {
        ERROR("Failed to create Airbender runner container '%s': %r",
              image, rc);
        return rc;
    }

    runner->container_created = true;
    return 0;
}

/* See description in tsapi_airbender.h */
tapi_job_t *
tsapi_airbender_run(tsapi_airbender_runner *runner,
                    const char             *binary_dir,
                    const char             *binary_name,
                    const char             *input_hex)
{
    te_errno    rc;
    te_string   bin_path   = TE_STRING_INIT;
    te_string   text_path  = TE_STRING_INIT;
    te_string   input_path = TE_STRING_INIT;
    const char *argv[10];
    int         argc = 0;
    tapi_job_t *job = NULL;

    if (runner == NULL || !runner->container_created ||
        binary_dir == NULL || binary_name == NULL || input_hex == NULL)
    {
        ERROR("tsapi_airbender_run: invalid arguments");
        return NULL;
    }

    rc = ts_container_share_folder(&runner->container,
                                   binary_dir, TSAPI_AIRBENDER_MOUNT_TARGET);
    if (rc != 0)
    {
        ERROR("Failed to share '%s' with the Airbender runner container: %r",
              binary_dir, rc);
        return NULL;
    }

    te_string_append(&bin_path, "%s/%s.bin",
                     TSAPI_AIRBENDER_MOUNT_TARGET, binary_name);
    te_string_append(&text_path, "%s/%s.text",
                     TSAPI_AIRBENDER_MOUNT_TARGET, binary_name);
    te_string_append(&input_path, "%s/%s",
                     TSAPI_AIRBENDER_MOUNT_TARGET, input_hex);

    /* The image has the cli as its ENTRYPOINT, so the arguments are its own. */
    argv[argc++] = "run";
    argv[argc++] = "--bin";
    argv[argc++] = bin_path.ptr;
    argv[argc++] = "--text";
    argv[argc++] = text_path.ptr;
    argv[argc++] = "--input-file";
    argv[argc++] = input_path.ptr;
    argv[argc++] = "--cycles";
    argv[argc++] = TSAPI_AIRBENDER_CYCLES;
    argv[argc++] = NULL;

    job = ts_container_run(&runner->container, argv);
    if (job == NULL)
        ERROR("Failed to create Airbender run job for guest '%s'",
              binary_name);

    te_string_free(&bin_path);
    te_string_free(&text_path);
    te_string_free(&input_path);

    return job;
}

/* See description in tsapi_airbender.h */
void
tsapi_airbender_runner_destroy(tsapi_airbender_runner *runner)
{
    if (runner == NULL)
        return;

    if (runner->container_created)
    {
        ts_container_destroy(&runner->container);
        runner->container_created = false;
    }
}
