/** @file
 * @brief Bflat Test Suite
 *
 * QEMU user-static runner API
 *
 * Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
 *
 * @author Maksim Menshikov <maksim.menshikov@nethermind.io>
 */

#ifndef __TSAPI_QEMU_H__
#define __TSAPI_QEMU_H__

#include "te_config.h"
#include "te_errno.h"
#include "tapi_job.h"
#include "rcf_rpc.h"

#ifdef __cplusplus
extern "C" {
#endif

/** Default path to qemu-riscv64-static on the agent */
#define TSAPI_QEMU_DEFAULT_PATH "/usr/bin/qemu-riscv64-static"

/** QEMU user-static runner context */
typedef struct tsapi_qemu_runner {
    rcf_rpc_server     *rpcs;       /**< RPC server on the host agent */
    tapi_job_factory_t *factory;    /**< TAPI job factory */
    char               *qemu_path;  /**< Path to the qemu binary on the agent */
} tsapi_qemu_runner;

/** Safe initialiser — all fields zeroed / NULL */
#define TSAPI_QEMU_RUNNER_INIT ((tsapi_qemu_runner){ \
        .rpcs       = NULL,                          \
        .factory    = NULL,                          \
        .qemu_path  = NULL,                          \
    })

/**
 * Create a QEMU runner.
 *
 * Allocates a job factory backed by @p rpcs and stores the path to the
 * QEMU binary that will be used for every subsequent tsapi_qemu_run() call.
 *
 * @param[in]  rpcs       RPC server on the agent where qemu is installed
 * @param[in]  qemu_path  Path to qemu-riscv64-static on the agent, or
 *                        @c NULL to use #TSAPI_QEMU_DEFAULT_PATH
 * @param[out] runner     Runner context to initialise
 *
 * @return Status code
 */
extern te_errno tsapi_qemu_runner_create(rcf_rpc_server    *rpcs,
                                         const char        *qemu_path,
                                         tsapi_qemu_runner *runner);

/**
 * Create a job that runs @p binary under QEMU.
 *
 * The job is created but not started; call tapi_job_start() on the returned
 * handle to actually launch it.
 *
 * @param runner        Initialised runner context
 * @param binary        Path to the riscv64 binary on the agent
 * @param extra_argv    Extra arguments to pass to @p binary, terminated by
 *                      @c NULL, or @c NULL if none
 *
 * @return Job handle, or @c NULL on failure
 */
extern tapi_job_t *tsapi_qemu_run(tsapi_qemu_runner *runner,
                                  const char        *binary,
                                  const char       **extra_argv);

/**
 * Destroy a QEMU runner and release all associated resources.
 *
 * Does not touch the RPC server — the caller is responsible for it.
 *
 * @param runner    Runner context to destroy
 */
extern void tsapi_qemu_runner_destroy(tsapi_qemu_runner *runner);

#ifdef __cplusplus
} /* extern "C" */
#endif

#endif /* !__TSAPI_QEMU_H__ */