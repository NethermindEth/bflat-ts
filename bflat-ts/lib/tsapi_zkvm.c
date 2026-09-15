/** @file
 * @brief Bflat Test Suite
 *
 * SP1 and OpenVM container runner implementation
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
#include "tsapi_zkvm.h"

/* See description in tsapi_zkvm.h */
bool
tsapi_zkvm_target_from_libc(const char *libc, tsapi_zkvm_target *target)
{
    if (libc == NULL || target == NULL)
        return false;

    if (strcmp(libc, "sp1") == 0)
    {
        *target = TSAPI_ZKVM_TARGET_SP1;
        return true;
    }

    if (strcmp(libc, "openvm") == 0)
    {
        *target = TSAPI_ZKVM_TARGET_OPENVM;
        return true;
    }

    return false;
}

/* See description in tsapi_zkvm.h */
te_errno
tsapi_zkvm_runner_create(rcf_rpc_server    *rpcs,
                         tsapi_zkvm_target  target,
                         const char        *image,
                         tsapi_zkvm_runner *runner)
{
    ts_container_params_docker dp = TS_CONTAINER_PARAMS_DOCKER;
    te_errno rc;

    if (runner == NULL)
        return TE_EINVAL;

    if (image == NULL)
    {
        image = (target == TSAPI_ZKVM_TARGET_SP1)
                    ? TSAPI_ZKVM_SP1_DEFAULT_IMAGE
                    : TSAPI_ZKVM_OPENVM_DEFAULT_IMAGE;
    }

    dp.params.name     = image;
    dp.params.prebuilt = TRUE;

    rc = ts_container_create(rpcs, TS_CONTAINER_TYPE_DOCKER,
                             &dp.params, &runner->container);
    if (rc != 0)
    {
        ERROR("Failed to create zkVM harness container '%s': %r", image, rc);
        return rc;
    }

    runner->target            = target;
    runner->container_created = true;
    return 0;
}

/* See description in tsapi_zkvm.h */
tapi_job_t *
tsapi_zkvm_run(tsapi_zkvm_runner *runner,
               const char        *binary_dir,
               const char        *binary_name,
               const char        *input_bin,
               const char        *openvm_config)
{
    te_errno    rc;
    te_string   bin_path    = TE_STRING_INIT;
    te_string   input_path  = TE_STRING_INIT;
    te_string   config_path = TE_STRING_INIT;
    const char *zkvm_argv[5];
    int         zkvm_argc = 0;
    tapi_job_t *job = NULL;

    if (runner == NULL || !runner->container_created ||
        binary_dir == NULL || binary_name == NULL || input_bin == NULL)
    {
        ERROR("tsapi_zkvm_run: invalid arguments");
        return NULL;
    }

    rc = ts_container_share_folder(&runner->container,
                                   binary_dir, TSAPI_ZKVM_MOUNT_TARGET);
    if (rc != 0)
    {
        ERROR("Failed to share '%s' with the zkVM harness container: %r",
              binary_dir, rc);
        return NULL;
    }

    te_string_append(&bin_path, "%s/%s",
                     TSAPI_ZKVM_MOUNT_TARGET, binary_name);
    te_string_append(&input_path, "%s/%s",
                     TSAPI_ZKVM_MOUNT_TARGET, input_bin);

    /*
     * Both images have the harness as their ENTRYPOINT, so the arguments are
     * the harness's own:
     *   sp1runner      <elf> <input> [<expected-hex>]
     *   openvm-runelf  <elf> <openvm.toml|-> <input>
     */
    zkvm_argv[zkvm_argc++] = bin_path.ptr;

    if (runner->target == TSAPI_ZKVM_TARGET_OPENVM)
    {
        if (openvm_config == NULL ||
            strcmp(openvm_config, TSAPI_ZKVM_OPENVM_BUILTIN_CONFIG) == 0)
        {
            zkvm_argv[zkvm_argc++] = TSAPI_ZKVM_OPENVM_BUILTIN_CONFIG;
        }
        else
        {
            te_string_append(&config_path, "%s/%s",
                             TSAPI_ZKVM_MOUNT_TARGET, openvm_config);
            zkvm_argv[zkvm_argc++] = config_path.ptr;
        }
    }

    zkvm_argv[zkvm_argc++] = input_path.ptr;
    zkvm_argv[zkvm_argc++] = NULL;

    job = ts_container_run(&runner->container, zkvm_argv);
    if (job == NULL)
        ERROR("Failed to create zkVM harness job for binary '%s'", binary_name);

    te_string_free(&bin_path);
    te_string_free(&input_path);
    te_string_free(&config_path);

    return job;
}

/* See description in tsapi_zkvm.h */
void
tsapi_zkvm_runner_destroy(tsapi_zkvm_runner *runner)
{
    if (runner == NULL)
        return;

    if (runner->container_created)
    {
        ts_container_destroy(&runner->container);
        runner->container_created = false;
    }
}
