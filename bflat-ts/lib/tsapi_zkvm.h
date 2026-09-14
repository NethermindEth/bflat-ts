/** @file
 * @brief Bflat Test Suite
 *
 * SP1 and OpenVM container runner API
 *
 * Copyright (C) 2026 Demerzel Solutions Limited (Nethermind)
 *
 * @author Maksim Menshikov <maksim.menshikov@nethermind.io>
 */

#ifndef __TSAPI_ZKVM_H__
#define __TSAPI_ZKVM_H__

#include "te_config.h"
#include "te_errno.h"
#include "tapi_job.h"
#include "rcf_rpc.h"
#include "ts_container.h"

#ifdef __cplusplus
extern "C" {
#endif

/**
 * SP1 execution harness, pinned by digest.
 *
 * The PORTABLE variant on purpose. SP1 picks its executor when the harness is
 * compiled: on x86_64 Linux it uses a native JIT unless the @c profiling
 * feature is set. The native one is an order of magnitude faster but runs the
 * guest in a child process whose stderr never reaches the caller, so the
 * guest's console output disappears - and it maps guest memory through
 * /dev/shm, which needs a @c --shm-size this suite's container wrapper does not
 * pass. Neither matters for the stateless guests nethermind proves; both matter
 * here, where the programs are tiny and their output is the thing under test.
 */
#define TSAPI_ZKVM_SP1_DEFAULT_IMAGE \
    "nethermindeth/sp1-runner:v6.5.0-portable"

/** OpenVM execution harness, pinned by digest. */
#define TSAPI_ZKVM_OPENVM_DEFAULT_IMAGE \
    "nethermindeth/openvm-runner:v2.1.0-rv64"

/** Mount point inside the harness container for host binaries */
#define TSAPI_ZKVM_MOUNT_TARGET  "/n"

/**
 * Tells the OpenVM harness to use the extension set shipped in its image
 * rather than a caller-supplied openvm.toml.
 */
#define TSAPI_ZKVM_OPENVM_BUILTIN_CONFIG "-"

/** Which zkVM the runner drives */
typedef enum tsapi_zkvm_target {
    TSAPI_ZKVM_TARGET_SP1,      /**< SP1 (bflat @c --libc sp1) */
    TSAPI_ZKVM_TARGET_OPENVM,   /**< OpenVM (bflat @c --libc openvm) */
} tsapi_zkvm_target;

/** zkVM harness runner context */
typedef struct tsapi_zkvm_runner {
    ts_container      container;         /**< Underlying Docker container */
    tsapi_zkvm_target target;            /**< Which harness this drives */
    bool              container_created; /**< Whether container was initialised */
} tsapi_zkvm_runner;

/** Safe zero initialiser */
#define TSAPI_ZKVM_RUNNER_INIT ((tsapi_zkvm_runner){    \
        .container         = TS_CONTAINER_INIT,         \
        .target            = TSAPI_ZKVM_TARGET_SP1,     \
        .container_created = false,                     \
    })

/**
 * Map a bflat @c --libc value to a harness target.
 *
 * @param[in]  libc     bflat libc name (@c sp1 or @c openvm)
 * @param[out] target   Resolved target
 *
 * @return @c true if @p libc is a zkVM this runner drives
 */
extern bool tsapi_zkvm_target_from_libc(const char        *libc,
                                        tsapi_zkvm_target *target);

/**
 * Create a harness runner backed by a Docker container.
 *
 * The container is initialised but not started; it is launched per-run by
 * tsapi_zkvm_run().
 *
 * @param[in]  rpcs     RPC server on the agent that has Docker
 * @param[in]  target   Which harness to run
 * @param[in]  image    Docker image to use, or @c NULL for the pinned default
 * @param[out] runner   Runner context to initialise
 *
 * @return Status code
 */
extern te_errno tsapi_zkvm_runner_create(rcf_rpc_server    *rpcs,
                                         tsapi_zkvm_target  target,
                                         const char        *image,
                                         tsapi_zkvm_runner *runner);

/**
 * Create a job that runs @p binary_name under the harness.
 *
 * @p binary_dir on the agent is bind-mounted as #TSAPI_ZKVM_MOUNT_TARGET, so
 * the binary and @p input_bin must both live there. Both harnesses take an
 * input path positionally and have no way to say "no input", so @p input_bin
 * is required - an empty file is the way to say the guest reads nothing.
 *
 * The returned job has not been started yet; call tapi_job_start() on it.
 *
 * @param runner        Initialised runner context
 * @param binary_dir    Absolute path on the agent holding the ELF
 * @param binary_name   Filename of the ELF inside @p binary_dir
 * @param input_bin     Filename of the input inside @p binary_dir
 * @param openvm_config For #TSAPI_ZKVM_TARGET_OPENVM, filename of an
 *                      openvm.toml inside @p binary_dir, or
 *                      #TSAPI_ZKVM_OPENVM_BUILTIN_CONFIG / @c NULL to use the
 *                      one shipped in the image. Ignored for SP1.
 *
 * @return Job handle, or @c NULL on failure
 */
extern tapi_job_t *tsapi_zkvm_run(tsapi_zkvm_runner *runner,
                                  const char        *binary_dir,
                                  const char        *binary_name,
                                  const char        *input_bin,
                                  const char        *openvm_config);

/**
 * Destroy a harness runner and release all associated resources.
 *
 * Does not touch the RPC server - the caller is responsible for it.
 *
 * @param runner    Runner context to destroy (may be @c NULL)
 */
extern void tsapi_zkvm_runner_destroy(tsapi_zkvm_runner *runner);

#ifdef __cplusplus
} /* extern "C" */
#endif

#endif /* !__TSAPI_ZKVM_H__ */
