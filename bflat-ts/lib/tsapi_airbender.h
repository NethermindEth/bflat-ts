/** @file
 * @brief Bflat Test Suite
 *
 * Airbender simulator runner API
 *
 * Copyright (C) 2026 Demerzel Solutions Limited (Nethermind)
 *
 * @author Maksim Menshikov <maksim.menshikov@nethermind.io>
 */

#ifndef __TSAPI_AIRBENDER_H__
#define __TSAPI_AIRBENDER_H__

#include "te_config.h"
#include "te_errno.h"
#include "tapi_job.h"
#include "rcf_rpc.h"
#include "ts_container.h"

#ifdef __cplusplus
extern "C" {
#endif

/**
 * The cli of matter-labs/zksync-airbender (v3) as its ENTRYPOINT, pinned by
 * digest: the image that bflat-riscv64 runs its Airbender guests with.
 */
#define TSAPI_AIRBENDER_DEFAULT_IMAGE \
    "nethermindeth/airbender-runner:" \
    "7a147da32e0c74308891a3fbd03193e724115266@sha256:" \
    "024fe8c5bd052865e02da23d3a24bc17955c4543e64d15aef1f94cfc2a08eb62"

/** Mount point inside the runner container for host binaries */
#define TSAPI_AIRBENDER_MOUNT_TARGET  "/n"

/**
 * The cycle limit of a run. The simulator reports the limit only in its
 * output ("did not finish"), not in its exit status.
 */
#define TSAPI_AIRBENDER_CYCLES  "2000000000"

/** The input of a guest that reads nothing: a byte length of zero. */
#define TSAPI_AIRBENDER_EMPTY_INPUT  "00000000"

/** Airbender runner context */
typedef struct tsapi_airbender_runner {
    ts_container container;         /**< Underlying Docker container */
    bool         container_created; /**< Whether container was initialised */
} tsapi_airbender_runner;

/** Safe zero initialiser */
#define TSAPI_AIRBENDER_RUNNER_INIT ((tsapi_airbender_runner){  \
        .container         = TS_CONTAINER_INIT,                 \
        .container_created = false,                             \
    })

/**
 * Create an Airbender runner backed by a Docker container.
 *
 * The container is initialised but not started; it is launched per-run by
 * tsapi_airbender_run().
 *
 * @param[in]  rpcs     RPC server on the agent that has Docker
 * @param[in]  image    Docker image to use, or @c NULL for
 *                      #TSAPI_AIRBENDER_DEFAULT_IMAGE
 * @param[out] runner   Runner context to initialise
 *
 * @return Status code
 */
extern te_errno tsapi_airbender_runner_create(rcf_rpc_server         *rpcs,
                                              const char             *image,
                                              tsapi_airbender_runner *runner);

/**
 * Create a job that runs the guest @p binary_name on the simulator.
 *
 * bflat writes the guest as the ELF and the images Airbender loads,
 * @c <binary_name>.bin and @c <binary_name>.text; the simulator runs the
 * images. @p binary_dir on the agent is bind-mounted as
 * #TSAPI_AIRBENDER_MOUNT_TARGET, so the images and @p input_hex must live
 * there. The input is in the form the simulator reads: the byte length, then
 * the bytes as little-endian words, each word as 8 hex digits of its value
 * (#TSAPI_AIRBENDER_EMPTY_INPUT for no input).
 *
 * The guest's console goes to the simulator's output as @c UART: lines, and
 * a run that ends prints @c "Result: ". The returned job has not been
 * started yet; call tapi_job_start() on it.
 *
 * @param runner        Initialised runner context
 * @param binary_dir    Absolute path on the agent holding the images
 * @param binary_name   Name of the guest inside @p binary_dir
 * @param input_hex     Filename of the input inside @p binary_dir
 *
 * @return Job handle, or @c NULL on failure
 */
extern tapi_job_t *tsapi_airbender_run(tsapi_airbender_runner *runner,
                                       const char             *binary_dir,
                                       const char             *binary_name,
                                       const char             *input_hex);

/**
 * Destroy an Airbender runner and release all associated resources.
 *
 * Does not touch the RPC server - the caller is responsible for it.
 *
 * @param runner    Runner context to destroy (may be @c NULL)
 */
extern void tsapi_airbender_runner_destroy(tsapi_airbender_runner *runner);

#ifdef __cplusplus
} /* extern "C" */
#endif

#endif /* !__TSAPI_AIRBENDER_H__ */
