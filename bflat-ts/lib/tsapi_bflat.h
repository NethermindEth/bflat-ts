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
 * Compile-time default bflat Docker image used when neither the
 * @c bflat_image test parameter nor the @c TS_BFLAT_IMAGE environment
 * variable provides one.
 */
#define TSAPI_BFLAT_DEFAULT_IMAGE   "nethermindeth/bflat-riscv64:latest"

/**
 * Resolve the bflat Docker image to use, applying the precedence:
 *  1. @p arg, when not @c NULL and not equal to the placeholder @c "-"
 *  2. value of the @c TS_BFLAT_IMAGE environment variable, when non-empty
 *  3. #TSAPI_BFLAT_DEFAULT_IMAGE
 *
 * @param arg   Value of the @c bflat_image test parameter, or @c NULL.
 *
 * @return Pointer to a NUL-terminated string with the resolved image name.
 *         The pointer is valid for the lifetime of @p arg or the process
 *         environment, whichever is referenced.
 */
extern const char *tsapi_bflat_resolve_image(const char *arg);

#ifdef __cplusplus
} /* extern "C" */
#endif

#endif /* !__TSAPI_BFLAT_H__ */