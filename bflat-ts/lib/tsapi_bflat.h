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
    "80f97933b93f86c20aad5a5edbfc08cb9cd9b2b4@sha256:" \
    "adee1249f66488ee6d3ed64e5a39d88fba343b6a8d38bc62436237f1daa80122"

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

/** Most @c --codegenopt values that @c TS_BFLAT_CODEGENOPT may give. */
#define TSAPI_BFLAT_CODEGENOPT_MAX 8

/**
 * Resolve the runtime package that bflat takes with @c --extlib for
 * @p libc, applying the precedence:
 *  1. the package of @p libc in the @c TS_BFLAT_EXTLIB environment variable
 *  2. @p arg
 *
 * @c TS_BFLAT_EXTLIB is a comma-separated list of @c platform=package
 * entries, where the platform is one of @c zisk, @c zisk_sim, @c sp1,
 * @c openvm and @c airbender, and the package is any value the
 * @c bflat_extlib test parameter takes. @c zisk_sim takes the package of
 * @c zisk when the list does not name it. For example,
 * @c zisk=<url>,airbender=<url>.tar.gz#<manifest>. A platform that the list
 * does not name keeps @p arg. A malformed list fails every test that builds,
 * so an override is never dropped without notice.
 *
 * @param arg     Value of the @c bflat_extlib test parameter, or @c NULL.
 * @param libc    Target libc, as passed to bflat @c --libc.
 * @param extlib  Location for the resolved package: @p arg, or a buffer that
 *                the next call overwrites.
 *
 * @return Status code; @c TE_EINVAL if the variable is malformed.
 */
extern te_errno tsapi_bflat_resolve_extlib(const char *arg, const char *libc,
                                           const char **extlib);

/**
 * The @c --codegenopt values in the @c TS_BFLAT_CODEGENOPT environment
 * variable: a comma-separated list of @c name=value entries, at most
 * #TSAPI_BFLAT_CODEGENOPT_MAX of them. For example,
 * @c JitMinOpts=1,JitStdOutFile=/src/jit.txt.
 *
 * @param opts  Array of #TSAPI_BFLAT_CODEGENOPT_MAX entries for the values,
 *              which point into a buffer that the next call overwrites.
 * @param n     Location for the number of values; @c 0 if the variable is
 *              not set or empty.
 *
 * @return Status code; @c TE_EINVAL if the variable is malformed.
 */
extern te_errno tsapi_bflat_env_codegenopt(const char **opts,
                                           unsigned int *n);

#ifdef __cplusplus
} /* extern "C" */
#endif

#endif /* !__TSAPI_BFLAT_H__ */