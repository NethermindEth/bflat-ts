/** @file
 * @brief Bflat Test Suite
 *
 * Container management functions
 *
 * Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
 *
 * @author Maksim Menshikov <maksim.menshikov@nethermind.io>
 */

#ifndef __TS_CONTAINER_H__
#define __TS_CONTAINER_H__

#include "te_config.h"
#include "te_string.h"
#include "tapi_job.h"
#include "tapi_rpc_stdio.h"
#include "rcf_rpc.h"

#ifdef __cplusplus
extern "C" {
#endif

/** Initialize container structure */
#define TS_CONTAINER_INIT ((ts_container) {         \
        .type = TS_CONTAINER_TYPE_DOCKER,           \
        .name = NULL,                               \
        .status = TS_CONTAINER_STATUS_FAILED,       \
        .rpcs = NULL,                               \
        .shared_folders = NULL,                     \
        .n_shared = 0,                              \
        .cidfile = NULL,                            \
        .factory = NULL,                            \
    })

/** Initial (safe but not totally correct) container params */
#define TS_CONTAINER_PARAMS_INIT ((ts_container_params) {                   \
        .name = NULL,                                                       \
        .prebuilt = FALSE,                                                  \
    })
/** Initial docker container params */
#define TS_CONTAINER_PARAMS_DOCKER ((ts_container_params_docker) {          \
        .params = TS_CONTAINER_PARAMS_INIT                                  \
    })
/** Initial container build params */
#define TS_CONTAINER_BUILD_PARAMS ((ts_container_build_params) {            \
        .timeout = -1                                                       \
    })
/** Initial docker build params */
#define TS_CONTAINER_BUILD_PARAMS_DOCKER ((ts_container_build_params_docker) { \
        .params = TS_CONTAINER_BUILD_PARAMS,                                   \
        .dockerfile_path = NULL,                                               \
        .argv = NULL,                                                          \
    })

/** Actual container status */
typedef enum ts_container_status {
    TS_CONTAINER_STATUS_CREATED,    /**< Container has just been created  */
    TS_CONTAINER_STATUS_BUILT,      /**< Container is ready to start */
    TS_CONTAINER_STATUS_FAILED,     /**< Container has failed */
} ts_container_status;

/** Supported container types */
typedef enum ts_container_type {
    TS_CONTAINER_TYPE_DOCKER,           /**< Docker-based container type */
} ts_container_type;

/** Shared folders for the container */
typedef struct ts_container_shared_folder {
    char *host_path;                /**< Path to folder on the host */
    char *container_path;           /**< Path to folder inside container */
} ts_container_shared_folder;

/** Container object */
typedef struct ts_container {
    ts_container_type   type;           /**< Container type */
    char               *name;           /**< Container name */
    ts_container_status status;         /**< Execution status */
    rcf_rpc_server     *rpcs;           /**< RPC server on a host holding a
                                             container */
    ts_container_shared_folder *shared_folders; /**< An array of shared folders
                                                     terminated by @c NULL,
                                                     or @c NULL */
    size_t              n_shared;       /**< Count of shared folders */
    char               *cidfile;        /**< CID file name */
    tapi_job_factory_t *factory;        /**< TAPI job factory */
} ts_container;

/** Common container parameters */
typedef struct ts_container_params {
    const char         *name;       /**< Container name */
    te_bool             prebuilt;   /**< Is container prebuilt? */
} ts_container_params;

/** Docker-based container parameters */
typedef struct ts_container_params_docker {
    ts_container_params params;     /**< Common parameters */
} ts_container_params_docker;

/** Common build parameters */
typedef struct ts_container_build_params {
    int             timeout;            /**< Build timeout */
} ts_container_build_params;

/** Docker-based container build parameters */
typedef struct ts_container_build_params_docker {
    ts_container_build_params params;   /**< Common build params */
    const char     *dockerfile_path;    /**< Path to folder containing
                                             Dockerfile */
    const char    **argv;               /**< Arguments */
} ts_container_build_params_docker;

/**
 * Create a container of a specified type with specified parameters
 *
 * @param[in]  type      Container type
 * @param[in]  params    Target container parameters, structure choice depends
 *                       on selected type
 * @param[out] container Container object to fill
 *
 * @return Status code
 */
extern te_errno ts_container_create(rcf_rpc_server         *rpcs,
                                    ts_container_type       type,
                                    ts_container_params    *params,
                                    ts_container           *container);

/**
 * Share the folder with container
 *
 * @param container         Container object
 * @param host_path         Path to directory on the host (on which container's
 *                          RPC server runs)
 * @param container_path    Path to directory inside container
 *
 * @return Status code
 */
extern te_errno ts_container_share_folder(ts_container *container,
                                          const char   *host_path,
                                          const char   *container_path);
/**
 * Build a container with specified arguments
 *
 * @param container     Container object
 * @param params        Build parameters
 *
 * @return Status code
 */
extern te_errno ts_container_build(ts_container *container,
                                   ts_container_build_params *params);

/**
 * Run a specific command in a container
 *
 * @param container     Container object
 * @param argv          An array of command arguments terminated by @c NULL.
 *
 * @return Job object, or @c NULL in case of failure
 */
extern tapi_job_t *ts_container_run(ts_container *container,
                                    const char  **argv);

/**
 * Stop a running container
 *
 * @param container     Container object
 * @param timeout_sec   Stop timeout (in seconds)
 */
extern void ts_container_stop(ts_container  *container, int timeout_sec);

/**
 * Destroy a created container
 *
 * @param container     Container object
 */
extern void ts_container_destroy(ts_container  *container);

#ifdef __cplusplus
} /* extern "C" */
#endif

#endif