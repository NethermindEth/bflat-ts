/** @file
 * @brief Bflat Test Suite API
 *
 * Stub implementation of Bflat Test Suite library.
 *
 * Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
 *
 * @author Maksim Menshikov <maksim.menshikov@nethermind.io>
 */

#include "te_config.h"
#include "tapi_test.h"
#include "tsapi_bflat.h"

#include <stdbool.h>
#include <stdlib.h>
#include <string.h>

/** Longest image name an entry of TS_BFLAT_IMAGE may give. */
#define TSAPI_BFLAT_IMAGE_MAX 1024

/**
 * Find the image of @p arch in the list @p list (see
 * tsapi_bflat_resolve_image()) and copy it to @p buf.
 *
 * @return @c true if the list names an image for @p arch
 */
static bool
tsapi_bflat_find_image(const char *list, const char *arch,
                       char *buf, size_t size)
{
    const char *entry = list;

    while (*entry != '\0')
    {
        const char *end = strchr(entry, ',');
        const char *eq;
        size_t      len;

        if (end == NULL)
            end = entry + strlen(entry);

        eq = memchr(entry, '=', end - entry);
        if (eq != NULL
                ? (strlen(arch) == (size_t)(eq - entry) &&
                   strncmp(entry, arch, eq - entry) == 0)
                : strcmp(arch, "riscv64") == 0)
        {
            const char *image = eq != NULL ? eq + 1 : entry;

            len = end - image;
            if (len == 0)
                return false;
            if (len >= size)
            {
                ERROR("TS_BFLAT_IMAGE: the image of '%s' is too long", arch);
                return false;
            }
            memcpy(buf, image, len);
            buf[len] = '\0';
            return true;
        }

        entry = *end == ',' ? end + 1 : end;
    }

    return false;
}

/* See description in tsapi_bflat.h */
const char *
tsapi_bflat_env_image(const char *arch)
{
    static char image[TSAPI_BFLAT_IMAGE_MAX];
    const char *env = getenv("TS_BFLAT_IMAGE");

    if (env == NULL ||
        !tsapi_bflat_find_image(env, arch != NULL ? arch : "riscv64",
                                image, sizeof(image)))
        return NULL;

    return image;
}

/* See description in tsapi_bflat.h */
const char *
tsapi_bflat_resolve_image(const char *arg, const char *arch)
{
    const char *image;

    if (arg != NULL && strcmp(arg, "-") != 0)
        return arg;

    image = tsapi_bflat_env_image(arch);
    if (image != NULL)
        return image;

    return arch != NULL && strcmp(arch, "riscv32") == 0
               ? TSAPI_BFLAT_RISCV32_DEFAULT_IMAGE
               : TSAPI_BFLAT_DEFAULT_IMAGE;
}
