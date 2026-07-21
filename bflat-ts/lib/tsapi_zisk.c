/** @file
 * @brief Bflat Test Suite
 *
 * Zisk container runner implementation
 *
 * Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
 *
 * @author Maksim Menshikov <maksim.menshikov@nethermind.io>
 */
#include "te_config.h"
#include "te_errno.h"
#include "te_string.h"
#include "tapi_job.h"
#include "tapi_test.h"
#include "tsapi_zisk.h"

#include <stdlib.h>
#include <string.h>

/* See description in tsapi_zisk.h */
const char *
tsapi_zisk_resolve_image(const char *arg)
{
    const char *env;

    if (arg != NULL && strcmp(arg, "-") != 0)
        return arg;

    env = getenv("TS_ZISK_IMAGE");
    if (env != NULL && *env != '\0')
        return env;

    return TSAPI_ZISK_DEFAULT_IMAGE;
}

/* See description in tsapi_zisk.h */
te_errno
tsapi_zisk_runner_create(rcf_rpc_server    *rpcs,
                         const char        *zisk_image,
                         tsapi_zisk_runner *runner)
{
    ts_container_params_docker dp = TS_CONTAINER_PARAMS_DOCKER;
    te_errno rc;

    dp.params.name     = tsapi_zisk_resolve_image(zisk_image);
    dp.params.prebuilt = TRUE;

    rc = ts_container_create(rpcs, TS_CONTAINER_TYPE_DOCKER,
                             &dp.params, &runner->container);
    if (rc != 0)
    {
        ERROR("Failed to create Zisk container '%s': %r",
              dp.params.name, rc);
        return rc;
    }

    runner->container_created = true;
    return 0;
}

/* See description in tsapi_zisk.h */
tapi_job_t *
tsapi_zisk_run(tsapi_zisk_runner *runner,
               const char        *binary_dir,
               const char        *binary_name,
               const char        *input_bin,
               bool               verbose)
{
    te_errno    rc;
    te_string   bin_path   = TE_STRING_INIT;
    te_string   input_path = TE_STRING_INIT;
    const char *zisk_argv[8];
    int         zisk_argc = 0;
    tapi_job_t *job;

    if (runner == NULL || !runner->container_created ||
        binary_dir == NULL || binary_name == NULL)
    {
        ERROR("tsapi_zisk_run: invalid arguments");
        return NULL;
    }

    rc = ts_container_share_folder(&runner->container,
                                   binary_dir, TSAPI_ZISK_MOUNT_TARGET);
    if (rc != 0)
    {
        ERROR("Failed to share '%s' with Zisk container: %r",
              binary_dir, rc);
        return NULL;
    }

    rc = te_string_append(&bin_path, "%s/%s",
                          TSAPI_ZISK_MOUNT_TARGET, binary_name);
    if (rc != 0)
        goto out;

    zisk_argv[zisk_argc++] = TSAPI_ZISK_EMU_BIN;
    if (verbose)
        zisk_argv[zisk_argc++] = "-v";
    zisk_argv[zisk_argc++] = "-e";
    zisk_argv[zisk_argc++] = bin_path.ptr;

    if (input_bin != NULL)
    {
        rc = te_string_append(&input_path, "%s/%s",
                              TSAPI_ZISK_MOUNT_TARGET, input_bin);
        if (rc != 0)
            goto out;

        zisk_argv[zisk_argc++] = "-i";
        zisk_argv[zisk_argc++] = input_path.ptr;
    }

    zisk_argv[zisk_argc++] = NULL;

    job = ts_container_run(&runner->container, zisk_argv);
    if (job == NULL)
        ERROR("Failed to create Zisk job for binary '%s'", binary_name);

out:
    te_string_free(&bin_path);
    te_string_free(&input_path);

    if (rc != 0)
        return NULL;

    return job;
}

/* See description in tsapi_zisk.h */
void
tsapi_zisk_runner_destroy(tsapi_zisk_runner *runner)
{
    if (runner == NULL)
        return;

    if (runner->container_created)
    {
        ts_container_destroy(&runner->container);
        runner->container_created = false;
    }
}