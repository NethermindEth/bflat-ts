/** @file
 * @brief Bflat Test Suite API
 *
 * Stub implementation of Bflat Test Suite library.
 *
 * Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
 *
 * @author Maksim Menshikov <maksim.menshikov@nethermind.io>
 */

#include "tsapi_bflat.h"

#include <stdlib.h>
#include <string.h>

const char *
tsapi_bflat_resolve_image(const char *arg)
{
    const char *env;

    if (arg != NULL && strcmp(arg, "-") != 0)
        return arg;

    env = getenv("TS_BFLAT_IMAGE");
    if (env != NULL && *env != '\0')
        return env;

    return TSAPI_BFLAT_DEFAULT_IMAGE;
}
