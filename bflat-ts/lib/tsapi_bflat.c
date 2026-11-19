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
#include "tapi_job.h"
#include "tapi_job_factory_rpc.h"
#include "te_string.h"
#include "tsapi_bflat.h"

#include <stdbool.h>
#include <stdlib.h>
#include <string.h>

/** Longest image name an entry of TS_BFLAT_IMAGE may give. */
#define TSAPI_BFLAT_IMAGE_MAX 1024

/** Longest value TS_BFLAT_EXTLIB or TS_BFLAT_CODEGENOPT may have. */
#define TSAPI_BFLAT_ENV_MAX 4096

/*
 * Characters a package of TS_BFLAT_EXTLIB may have besides letters and
 * digits. build.c puts a release archive URL in single quotes on a shell
 * command line, so a quote, a space or a backslash must never get there.
 */
#define TSAPI_BFLAT_EXTLIB_CHARS "._~:/?#@+=%&-"

/** Characters a value of TS_BFLAT_CODEGENOPT may have besides alphanumerics. */
#define TSAPI_BFLAT_CODEGENOPT_CHARS "._:/+-"

/** Platforms TS_BFLAT_EXTLIB may name, as passed to bflat --libc. */
static const char *const tsapi_bflat_platforms[] = {
    "zisk", "zisk_sim", "sp1", "openvm", "airbender",
};

#define TSAPI_BFLAT_PLATFORMS \
    (sizeof(tsapi_bflat_platforms) / sizeof(tsapi_bflat_platforms[0]))

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

/**
 * Check that @p len characters at @p s are letters, digits or in @p extra.
 */
static bool
tsapi_bflat_chars_ok(const char *s, size_t len, const char *extra)
{
    size_t i;

    for (i = 0; i < len; i++)
    {
        unsigned char c = (unsigned char)s[i];

        if (!(c >= 'a' && c <= 'z') && !(c >= 'A' && c <= 'Z') &&
            !(c >= '0' && c <= '9') && (c == '\0' || strchr(extra, c) == NULL))
            return false;
    }

    return true;
}

/**
 * Find the platform of @p len characters at @p name in
 * tsapi_bflat_platforms.
 *
 * @return Index of the platform, or #TSAPI_BFLAT_PLATFORMS if it is unknown
 */
static size_t
tsapi_bflat_platform(const char *name, size_t len)
{
    size_t i;

    for (i = 0; i < TSAPI_BFLAT_PLATFORMS; i++)
    {
        if (strlen(tsapi_bflat_platforms[i]) == len &&
            strncmp(tsapi_bflat_platforms[i], name, len) == 0)
            break;
    }

    return i;
}

/* See description in tsapi_bflat.h */
te_errno
tsapi_bflat_resolve_extlib(const char *arg, const char *libc,
                           const char **extlib)
{
    static char value[TSAPI_BFLAT_ENV_MAX];
    const char *env = getenv("TS_BFLAT_EXTLIB");
    const char *entry;
    const char *found     = NULL;
    size_t      found_len = 0;
    bool        exact     = false;
    bool        seen[TSAPI_BFLAT_PLATFORMS] = { false };

    *extlib = arg;
    if (env == NULL || *env == '\0')
        return 0;

    if (strlen(env) >= sizeof(value))
    {
        ERROR("TS_BFLAT_EXTLIB: the list is too long");
        return TE_EINVAL;
    }

    for (entry = env; ; entry++)
    {
        const char *end = strchr(entry, ',');
        const char *eq;
        size_t      platform;

        if (end == NULL)
            end = entry + strlen(entry);

        eq = memchr(entry, '=', end - entry);
        platform = eq != NULL ? tsapi_bflat_platform(entry, eq - entry)
                              : TSAPI_BFLAT_PLATFORMS;
        if (platform == TSAPI_BFLAT_PLATFORMS || eq + 1 == end ||
            !tsapi_bflat_chars_ok(eq + 1, end - eq - 1,
                                  TSAPI_BFLAT_EXTLIB_CHARS))
        {
            ERROR("TS_BFLAT_EXTLIB: malformed entry '%.*s', expected "
                  "platform=package with a platform of zisk, zisk_sim, sp1, "
                  "openvm or airbender", (int)(end - entry), entry);
            return TE_EINVAL;
        }
        if (seen[platform])
        {
            ERROR("TS_BFLAT_EXTLIB: the platform '%s' is named twice",
                  tsapi_bflat_platforms[platform]);
            return TE_EINVAL;
        }
        seen[platform] = true;

        if (libc != NULL)
        {
            if (strcmp(libc, tsapi_bflat_platforms[platform]) == 0)
            {
                found = eq + 1;
                found_len = end - found;
                exact = true;
            }
            else if (!exact && strcmp(libc, "zisk_sim") == 0 &&
                     strcmp(tsapi_bflat_platforms[platform], "zisk") == 0)
            {
                found = eq + 1;
                found_len = end - found;
            }
        }

        entry = end;
        if (*entry == '\0')
            break;
    }

    if (found != NULL)
    {
        memcpy(value, found, found_len);
        value[found_len] = '\0';
        *extlib = value;
    }

    return 0;
}

/* See description in tsapi_bflat.h */
te_errno
tsapi_bflat_env_codegenopt(const char **opts, unsigned int *n)
{
    static char  value[TSAPI_BFLAT_ENV_MAX];
    const char  *env = getenv("TS_BFLAT_CODEGENOPT");
    char        *entry;

    *n = 0;
    if (env == NULL || *env == '\0')
        return 0;

    if (strlen(env) >= sizeof(value))
    {
        ERROR("TS_BFLAT_CODEGENOPT: the list is too long");
        return TE_EINVAL;
    }
    strncpy(value, env, sizeof(value) - 1);
    value[sizeof(value) - 1] = '\0';

    for (entry = value; ; entry++)
    {
        char *end = strchr(entry, ',');
        char *eq;
        bool  last = end == NULL;

        if (last)
            end = entry + strlen(entry);

        eq = memchr(entry, '=', end - entry);
        if (eq == NULL || eq == entry || eq + 1 == end ||
            (!(*entry >= 'a' && *entry <= 'z') &&
             !(*entry >= 'A' && *entry <= 'Z')) ||
            !tsapi_bflat_chars_ok(entry, eq - entry, "_") ||
            !tsapi_bflat_chars_ok(eq + 1, end - eq - 1,
                                  TSAPI_BFLAT_CODEGENOPT_CHARS))
        {
            ERROR("TS_BFLAT_CODEGENOPT: malformed entry '%.*s', expected "
                  "name=value", (int)(end - entry), entry);
            *n = 0;
            return TE_EINVAL;
        }
        if (*n == TSAPI_BFLAT_CODEGENOPT_MAX)
        {
            ERROR("TS_BFLAT_CODEGENOPT: more than %d values",
                  TSAPI_BFLAT_CODEGENOPT_MAX);
            *n = 0;
            return TE_EINVAL;
        }

        *end = '\0';
        opts[(*n)++] = entry;

        entry = end;
        if (last)
            break;
    }

    return 0;
}

/** Time for eh_frame_check.py to read and check one binary. */
#define TSAPI_BFLAT_UNWIND_TIMEOUT_MS 300000

/* See description in tsapi_bflat.h */
te_errno
tsapi_bflat_check_unwind(rcf_rpc_server *rpcs, const char *script,
                         const char *elf, int min_split, int *status)
{
    tapi_job_factory_t *factory = NULL;
    tapi_job_t         *job = NULL;
    tapi_job_channel_t *channels[2];
    tapi_job_status_t   job_status;
    te_string           min_split_str = TE_STRING_INIT;
    const char         *job_argv[7];
    te_errno            rc;

    te_string_append(&min_split_str, "%d", min_split);

    job_argv[0] = "/usr/bin/env";
    job_argv[1] = "python3";
    job_argv[2] = script;
    job_argv[3] = elf;
    job_argv[4] = "--min-split";
    job_argv[5] = min_split_str.ptr;
    job_argv[6] = NULL;

    rc = tapi_job_factory_rpc_create(rpcs, &factory);
    if (rc == 0)
        rc = tapi_job_create(factory, NULL, job_argv[0], job_argv, NULL,
                             &job);
    if (rc == 0)
        rc = tapi_job_alloc_output_channels(job, 2, channels);
    if (rc == 0)
        rc = tapi_job_attach_filter(TAPI_JOB_CHANNEL_SET(channels[0]),
                                    "eh_frame_check stdout", false,
                                    TE_LL_RING, NULL);
    if (rc == 0)
        rc = tapi_job_attach_filter(TAPI_JOB_CHANNEL_SET(channels[1]),
                                    "eh_frame_check stderr", false,
                                    TE_LL_ERROR, NULL);
    if (rc == 0)
        rc = tapi_job_start(job);
    if (rc == 0)
        rc = tapi_job_wait(job, TSAPI_BFLAT_UNWIND_TIMEOUT_MS, &job_status);
    if (rc == 0)
    {
        if (job_status.type != TAPI_JOB_STATUS_EXITED)
        {
            ERROR("eh_frame_check.py was killed by a signal (signo=%d)",
                  job_status.value);
            rc = TE_RC(TE_TAPI, TE_ESHCMD);
        }
        else
        {
            *status = job_status.value;
        }
    }

    if (job != NULL)
        (void)tapi_job_destroy(job, -1);
    tapi_job_factory_destroy(factory);
    te_string_free(&min_split_str);

    return rc;
}
