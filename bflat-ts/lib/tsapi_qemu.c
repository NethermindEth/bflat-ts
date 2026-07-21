/** @file
 * @brief Bflat Test Suite
 *
 * QEMU user-static runner implementation
 *
 * Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
 *
 * @author Maksim Menshikov <maksim.menshikov@nethermind.io>
 */
#include "te_config.h"
#include "te_errno.h"
#include "tapi_job.h"
#include "tapi_job_factory_rpc.h"
#include "tapi_test.h"
#include "tsapi_qemu.h"

/* Count arguments in a NULL-terminated argv array */
static size_t
args_count(const char **argv)
{
    size_t n = 0;

    if (argv == NULL)
        return 0;

    while (argv[n] != NULL)
        n++;

    return n;
}

/* See description in tsapi_qemu.h */
te_errno
tsapi_qemu_runner_create(rcf_rpc_server    *rpcs,
                         const char        *qemu_path,
                         tsapi_qemu_runner *runner)
{
    te_errno rc;

    runner->rpcs      = rpcs;
    runner->factory   = NULL;
    runner->qemu_path = strdup(qemu_path != NULL
                               ? qemu_path
                               : TSAPI_QEMU_DEFAULT_PATH);
    if (runner->qemu_path == NULL)
        return TE_ENOMEM;

    rc = tapi_job_factory_rpc_create(rpcs, &runner->factory);
    if (rc != 0)
    {
        ERROR("Failed to create job factory: %r", rc);
        free(runner->qemu_path);
        runner->qemu_path = NULL;
        runner->factory   = NULL;
    }

    return rc;
}

/* See description in tsapi_qemu.h */
tapi_job_t *
tsapi_qemu_run(tsapi_qemu_runner *runner,
               const char        *binary,
               const char       **extra_argv)
{
    te_errno     rc;
    size_t       extra_argc;
    const char **argv;
    size_t       i;
    tapi_job_t  *job;

    if (runner == NULL || runner->factory == NULL || binary == NULL)
    {
        ERROR("tsapi_qemu_run: invalid arguments");
        return NULL;
    }

    extra_argc = args_count(extra_argv);

    /* qemu_path + binary + extra_argv + NULL */
    argv = malloc(sizeof(const char *) * (2 + extra_argc + 1));
    if (argv == NULL)
    {
        ERROR("Failed to allocate argv for QEMU job");
        return NULL;
    }

    argv[0] = runner->qemu_path;
    argv[1] = binary;
    for (i = 0; i < extra_argc; i++)
        argv[2 + i] = extra_argv[i];
    argv[2 + extra_argc] = NULL;

    rc = tapi_job_create(runner->factory, NULL, runner->qemu_path,
                         argv, NULL, &job);
    free(argv);

    if (rc != 0)
    {
        ERROR("Failed to create QEMU job: %r", rc);
        return NULL;
    }

    return job;
}

/* See description in tsapi_qemu.h */
void
tsapi_qemu_runner_destroy(tsapi_qemu_runner *runner)
{
    if (runner == NULL)
        return;

    if (runner->factory != NULL)
    {
        tapi_job_factory_destroy(runner->factory);
        runner->factory = NULL;
    }

    free(runner->qemu_path);
    runner->qemu_path = NULL;
    runner->rpcs      = NULL;
}