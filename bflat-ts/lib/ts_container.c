/** @file
 * @brief Bflat Test Suite
 *
 * Container management functions
 *
 * Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
 *
 * @author Maksim Menshikov <maksim.menshikov@nethermind.io>
 */
#include "te_config.h"
#include "te_string.h"
#include "tapi_job.h"
#include "tapi_job_factory_rpc.h"
#include "tapi_test.h"
#include "ts_container.h"
#include "tapi_file.h"
#include "tapi_rpc_stdio.h"
#include "tapi_rpc_signal.h"
#include "tapi_rpc_unistd.h"

#define DOCKER_PATH                     "/usr/bin/docker"
#define DOCKER_DEFAULT_STOP_TIMEOUT_SEC (10)
#define DOCKER_STOP_TIMEOUT_MULTIPLIER  (3)

/* Count arguments in an array */
static size_t
args_in_array(const char **argv)
{
    size_t count = 0;

    if (argv == NULL)
        return 0;

    while (*argv != NULL)
    {
        argv++;
        count++;
    }

    return count;
}

/** Create a docker container */
static te_errno
ts_container_docker_create(rcf_rpc_server         *rpcs,
                           ts_container_params    *params,
                           ts_container           *container)
{
    te_errno    rc;

    container->name = strdup(params->name);
    if (container->name == NULL)
        return TE_ENOMEM;

    container->type = TS_CONTAINER_TYPE_DOCKER;
    container->rpcs = rpcs;
    container->status = params->prebuilt
        ? TS_CONTAINER_STATUS_BUILT
        : TS_CONTAINER_STATUS_CREATED;
    container->shared_folders = NULL;
    container->n_shared = 0;
    container->cidfile = tapi_file_make_name(NULL);

    rc = tapi_job_factory_rpc_create(rpcs, &container->factory);
    if (rc != 0)
    {
        ERROR("Failed to create job factory: %r", rc);
        container->factory = NULL;
    }

    return rc;
}

/* See description in ts_container.h */
static tapi_job_t *
ts_container_run_docker(ts_container *container,
                        const char  **argv)
{
    te_errno        rc;
    size_t          i;
    const char    **new_argv;
    const char     *def_argv[] =
        { "docker", "run", "--rm", "-t", "--cap-add=SYS_PTRACE",
        "--security-opt", "apparmor=unconfined",
        "--security-opt", "seccomp=unconfined",
        "--privileged",
        "--network", "host",
        "--group-add", "dialout", "--group-add", "tty",
        "--cidfile", container->cidfile,
        NULL};
    size_t          def_argv_count;
    size_t          argv_count;
    tapi_job_t     *job;
    size_t          cur_elem = 0;
    te_string      *paths;

    argv_count = args_in_array(argv);
    def_argv_count = args_in_array(def_argv);
    new_argv = malloc(sizeof(const char *) * (def_argv_count + 1 /* name */+
        container->n_shared * 2 /* shared */ + argv_count + 1));
    paths = malloc(sizeof(te_string) * container->n_shared);
    if (new_argv == NULL || paths == NULL)
    {
        free(new_argv);
        free(paths);
        ERROR("Failed to allocate memory for process arguments");
        return NULL;
    }

    /* Transfer docker & our arguments */
    for (i = 0; i < def_argv_count; ++i)
    {
        new_argv[cur_elem++] = def_argv[i];
    }

    /* Transfer shared folders */
    for (i = 0; i < container->n_shared; ++i)
    {
        paths[i] = (te_string)TE_STRING_INIT;

        rc = te_string_append(&paths[i], "%s:%s",
            container->shared_folders[i].host_path,
            container->shared_folders[i].container_path);
        if (rc != 0)
        {
            size_t j;

            for (j = 0; j < i; ++j)
            {
                te_string_free(&paths[j]);
            }
            free(new_argv);
            free(paths);

            return NULL;
        }

        new_argv[cur_elem++] = "-v";
        new_argv[cur_elem++] = paths[i].ptr;
    }

    /* Transfer container name */
    new_argv[cur_elem++] = (const char *)container->name;

    /* Transfer user arguments */
    for (i = 0; i < argv_count; ++i)
    {
        new_argv[cur_elem++] = argv[i];
    }

    new_argv[cur_elem++] = NULL;

    rc = tapi_job_create(container->factory, NULL, DOCKER_PATH,
                         (const char **)new_argv, NULL, &job);
    for (i = 0; i < container->n_shared; ++i)
    {
        te_string_free(&paths[i]);
    }
    free(new_argv);
    free(paths);

    if (rc != 0)
        return NULL;

    return job;
}

/* See description in ts_container.h */
static te_errno
ts_container_build_docker(ts_container                 *container,
                          ts_container_build_params    *params)
{
    ts_container_build_params_docker *p =
        (ts_container_build_params_docker *)params;
    te_errno        rc;
    size_t          i;
    tapi_job_t     *job;
    const char    **new_argv;
    const char     *def_argv[] =
        { "docker", "build",
        /* Tagging */
        "-t", (const char *)container->name,
        NULL};
    size_t          def_argv_count;
    size_t          argv_count;
    tapi_job_channel_t *out_channels[2];
    tapi_job_status_t   job_status;

    argv_count = args_in_array(p->argv);
    def_argv_count = args_in_array(def_argv);
    new_argv = malloc(sizeof(const char *) * (argv_count + def_argv_count + 2));
    if (new_argv == NULL)
    {
        ERROR("Failed to allocate memory for process arguments");
        return TE_ENOMEM;
    }

    /* Transfer docker & our arguments */
    for (i = 0; i < def_argv_count; ++i)
    {
        new_argv[i] = def_argv[i];
    }

    for (i = 0; i < argv_count; ++i)
    {
        new_argv[i + def_argv_count] = p->argv[i];
    }

    /* Transfer docker file path */
    new_argv[def_argv_count + argv_count] = p->dockerfile_path;
    new_argv[def_argv_count + argv_count + 1] = NULL;

    rc = tapi_job_create(container->factory, NULL, DOCKER_PATH,
                         (const char **)new_argv, NULL, &job);
    free(new_argv);
    if (rc != 0)
    {
        ERROR("Failed to create docker build job: %r", rc);
        return rc;
    }

    CHECK_RC(tapi_job_alloc_output_channels(job, 2, out_channels));
    CHECK_RC(tapi_job_attach_filter(TAPI_JOB_CHANNEL_SET(out_channels[0]),
                                    "Out filter", FALSE, TE_LL_RING, NULL));
    CHECK_RC(tapi_job_attach_filter(TAPI_JOB_CHANNEL_SET(out_channels[1]),
                                    "Error filter", FALSE, TE_LL_ERROR, NULL));
    rc = tapi_job_start(job);
    if (rc != 0)
    {
        ERROR("Failed to start docker build job: %r", rc);
        goto err;
    }

    rc = tapi_job_wait(job, params->timeout >= 0 ? params->timeout : -1,
                       &job_status);
    if (rc == 0 &&
        job_status.type == TAPI_JOB_STATUS_EXITED &&
        job_status.value == 0)
    {
        container->status = TS_CONTAINER_STATUS_BUILT;
        return 0;
    }

err:
    container->status = TS_CONTAINER_STATUS_FAILED;
    tapi_job_destroy(job, 10000);
    return rc;
}

/* See description in ts_container.h */
te_errno
ts_container_create(rcf_rpc_server         *rpcs,
                    ts_container_type       type,
                    ts_container_params    *params,
                    ts_container           *container)
{
    switch (type)
    {
        case TS_CONTAINER_TYPE_DOCKER:
            return ts_container_docker_create(rpcs, params, container);
        default:
            return TE_EINVAL;
    }
}

/* See description in ts_container.h */
te_errno
ts_container_build(ts_container                *container,
                   ts_container_build_params   *params)
{
    if (container->name == NULL || container->rpcs == NULL)
        return TE_EINVAL;

    switch (container->type)
    {
        case TS_CONTAINER_TYPE_DOCKER:
            return ts_container_build_docker(container, params);
        default:
            return TE_EINVAL;
    }
}

/* See description in ts_container.h */
tapi_job_t *
ts_container_run(ts_container *container,
                 const char  **argv)
{
    if (container->status != TS_CONTAINER_STATUS_BUILT)
    {
        ERROR("Failed to start process in container: status is not BUILT");
        return NULL;
    }

    switch (container->type)
    {
        case TS_CONTAINER_TYPE_DOCKER:
            return ts_container_run_docker(container, argv);
        default:
            return NULL;
    }
}

/* Get RPC server stop timeout */
static int
get_rpc_stop_timeout(int timeout_sec)
{
    if (timeout_sec <= 0)
        return TE_SEC2MS(DOCKER_DEFAULT_STOP_TIMEOUT_SEC);

    return TE_SEC2MS(timeout_sec) * DOCKER_STOP_TIMEOUT_MULTIPLIER;
}

/* See description in ts_container.h */
void
ts_container_stop(ts_container  *container, int timeout_sec)
{
    char *cid;

    if (container->cidfile == NULL)
        return;

    if (tapi_file_read_ta(container->rpcs->ta, container->cidfile,
                          &cid) == 0)
    {
        tarpc_pid_t pid;
        int         stop_timeout = get_rpc_stop_timeout(timeout_sec);

        container->rpcs->timeout = stop_timeout;
        RPC_AWAIT_ERROR(container->rpcs);
        pid = rpc_te_shell_cmd(container->rpcs,
                               "docker stop %s -t %d", -1,
                               NULL, NULL, NULL, cid, timeout_sec);

        container->rpcs->timeout = stop_timeout;
        RPC_AWAIT_ERROR(container->rpcs);
        rpc_waitpid(container->rpcs, pid, NULL, 0);

        container->rpcs->timeout = stop_timeout;
        RPC_AWAIT_ERROR(container->rpcs);
        rpc_unlink(container->rpcs, container->cidfile);
    }
}

/* See description in ts_container.h */
void
ts_container_destroy(ts_container  *container)
{
    if (container->factory != NULL)
        tapi_job_factory_destroy(container->factory);
    free(container->cidfile);
    if (container->shared_folders != NULL)
    {
        size_t i;

        for (i = 0; i < container->n_shared; ++i)
        {
            ts_container_shared_folder *folder = &container->shared_folders[i];
            free(folder->host_path);
            free(folder->container_path);
        }

        free(container->shared_folders);
    }
    free(container->name);
}

/* See description in ts_container.h */
te_errno
ts_container_share_folder(ts_container *container,
                          const char   *host_path,
                          const char   *container_path)
{
    ts_container_shared_folder *new_block;

    if (container == NULL || host_path == NULL || container_path == NULL)
        return TE_EINVAL;

    new_block = realloc(container->shared_folders,
                        sizeof(ts_container_shared_folder) *
                        (container->n_shared + 1 /* new object */));
    if (new_block == NULL)
        return TE_ENOMEM;

    container->n_shared++;
    container->shared_folders = new_block;
    container->shared_folders[container->n_shared - 1].host_path =
        strdup(host_path);
    container->shared_folders[container->n_shared - 1].container_path =
        strdup(container_path);

    return 0;
}
