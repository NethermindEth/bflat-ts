/** @file
 * @brief Bflat Test Suite API
 *
 * Main header file for Bflat Test Suite library.
 *
 * Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
 *
 * @author Maksim Menshikov <maksim.menshikov@nethermind.io>
 */

#ifndef __TSAPI_BFLAT_H__
#define __TSAPI_BFLAT_H__

#include "te_config.h"
#include "te_defs.h"
#include "te_errno.h"

#ifdef __cplusplus
extern "C" {
#endif

/**
 * @defgroup bflat_ts Bflat Test Suite
 * @{
 *
 * @brief Test suite for bflat compiler.
 *
 * @}
 */

/**
 * Compile-time default bflat Docker image for @c --arch @c riscv64, used when
 * neither the @c bflat_image test parameter nor the @c TS_BFLAT_IMAGE
 * environment variable provides one. Pinned by digest, as :latest moves with
 * every CI run of bflat-riscv64; the tag is the bflat-riscv64 commit. This
 * bflat takes each zkVM platform from its runtime package (@c --extlib) and
 * checks the signature of the package.
 */
#define TSAPI_BFLAT_DEFAULT_IMAGE \
    "nethermindeth/bflat-riscv64-11:" \
    "dd05dd8af0a14065a9974492007dd5e568acfecc@sha256:" \
    "34235a4a8050b171dd03562a875752c08db5b6fdb33bb505be1eeeb7c4ea19be"

/**
 * Compile-time default bflat Docker image for @c --arch @c riscv32: the image
 * of the same bflat with the riscv32 target too, which the default image does
 * not have. The tag is the bflat-riscv64 commit, the one Nethermind pins for
 * its Airbender guest.
 */
#define TSAPI_BFLAT_RISCV32_DEFAULT_IMAGE \
    "nethermindeth/bflat-riscv64-11-riscv32:" \
    "80f97933b93f86c20aad5a5edbfc08cb9cd9b2b4@sha256:" \
    "1631e9d15ed9dc446e514c498e17c1fc9546cec36a9ab2c1baec2300f1d1f378"

/**
 * Resolve the bflat Docker image that builds for @p arch, applying the
 * precedence:
 *  1. @p arg, when not @c NULL and not equal to the placeholder @c "-"
 *  2. the image of @p arch in the @c TS_BFLAT_IMAGE environment variable
 *  3. #TSAPI_BFLAT_RISCV32_DEFAULT_IMAGE for @c riscv32,
 *     #TSAPI_BFLAT_DEFAULT_IMAGE for any other architecture
 *
 * @c TS_BFLAT_IMAGE is a comma-separated list. An entry @c arch=image names
 * the image of that architecture; an entry without @c = names the image of
 * @c riscv64, so a single image keeps its old meaning. For example,
 * @c img64,riscv32=img32 or @c riscv64=img64,riscv32=img32.
 *
 * @param arg   Value of the @c bflat_image test parameter, or @c NULL.
 * @param arch  Target architecture, as passed to bflat @c --arch.
 *
 * @return Pointer to a NUL-terminated string with the resolved image name:
 *         @p arg, a compile-time default, or a buffer that the next call
 *         overwrites.
 */
extern const char *tsapi_bflat_resolve_image(const char *arg,
                                             const char *arch);

/**
 * The image of @p arch in the @c TS_BFLAT_IMAGE environment variable, for a
 * test that has a default of its own instead of the ones of
 * tsapi_bflat_resolve_image().
 *
 * @param arch  Target architecture, as passed to bflat @c --arch.
 *
 * @return The image, in a buffer that the next call overwrites, or @c NULL if
 *         the variable names none for @p arch.
 */
extern const char *tsapi_bflat_env_image(const char *arch);

#ifdef __cplusplus
} /* extern "C" */
#endif

#endif /* !__TSAPI_BFLAT_H__ */