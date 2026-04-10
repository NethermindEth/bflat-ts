/** @file
 * @brief Bflat Test Suite
 *
 * Zisk container runner API
 *
 * Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
 *
 * @author Maksim Menshikov <maksim.menshikov@nethermind.io>
 */

#ifndef __TSAPI_ZISK_H__
#define __TSAPI_ZISK_H__

#include "te_config.h"
#include "te_errno.h"
#include "tapi_job.h"
#include "rcf_rpc.h"
#include "ts_container.h"

#ifdef __cplusplus
extern "C" {
#endif

/**
 * Default Zisk Docker image (pinned by digest for reproducibility).
 * Override via the @c zisk_image test parameter.
 */
#define TSAPI_ZISK_DEFAULT_IMAGE \
    "nethermindeth/zisk@sha256:" \
    "1b38e92264c30a4109fdb47f3b175324544085e36b9dde0abf7570c2b1e3312b"

/** Mount point inside the Zisk container for host binaries */
#define TSAPI_ZISK_MOUNT_TARGET  "/n"

/** Zisk emulator binary name inside the container */
#define TSAPI_ZISK_EMU_BIN       "ziskemu"

/** Zisk container runner context */
typedef struct tsapi_zisk_runner {
    ts_container    container;          /**< Underlying Docker container */
    bool            container_created;  /**< Whether container was initialised */
} tsapi_zisk_runner;

/** Safe zero initialiser */
#define TSAPI_ZISK_RUNNER_INIT ((tsapi_zisk_runner){    \
        .container         = TS_CONTAINER_INIT,         \
        .container_created = false,                     \
    })

/**
 * Create a Zisk runner backed by a Docker container.
 *
 * The container is initialised with @p zisk_image (prebuilt) but not started;
 * it will be launched per-run by tsapi_zisk_run().
 *
 * @param[in]  rpcs        RPC server on the agent that has Docker
 * @param[in]  zisk_image  Docker image to use, or @c NULL for
 *                         #TSAPI_ZISK_DEFAULT_IMAGE
 * @param[out] runner      Runner context to initialise
 *
 * @return Status code
 */
extern te_errno tsapi_zisk_runner_create(rcf_rpc_server    *rpcs,
                                         const char        *zisk_image,
                                         tsapi_zisk_runner *runner);

/**
 * Create a job that runs @p binary_name under @c ziskemu inside the Zisk
 * container.
 *
 * The directory @p binary_dir on the agent is bind-mounted as
 * #TSAPI_ZISK_MOUNT_TARGET inside the container, so both the binary and
 * (optionally) @p input_bin must reside there.
 *
 * The returned job has not been started yet; call tapi_job_start() on it.
 *
 * @param runner        Initialised runner context
 * @param binary_dir    Absolute path to the directory on the agent that
 *                      contains the compiled riscv64 ELF
 * @param binary_name   Filename of the ELF binary inside @p binary_dir
 * @param input_bin     Filename of the input binary inside @p binary_dir,
 *                      passed as @c -i to ziskemu, or @c NULL to omit
 *
 * @return Job handle, or @c NULL on failure
 */
extern tapi_job_t *tsapi_zisk_run(tsapi_zisk_runner *runner,
                                   const char        *binary_dir,
                                   const char        *binary_name,
                                   const char        *input_bin);

/**
 * Destroy a Zisk runner and release all associated resources.
 *
 * Does not touch the RPC server — the caller is responsible for it.
 *
 * @param runner    Runner context to destroy (may be @c NULL)
 */
extern void tsapi_zisk_runner_destroy(tsapi_zisk_runner *runner);

#ifdef __cplusplus
} /* extern "C" */
#endif

#endif /* !__TSAPI_ZISK_H__ */