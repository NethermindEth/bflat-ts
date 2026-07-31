/** @file
 * @brief Build prologue: compile Nethermind ZiskGuest ELF with bflat
 *
 * Prologue script for the @c zisk_guest test package.  Always updates the
 * Nethermind source tree before building so that the freshest code is
 * compiled.  The build itself is fully delegated to the @c Makefile shipped
 * with @c Nethermind.Stateless.ZiskGuest, so this suite does not duplicate
 * any of the Nethermind build logic (reference lists, bflat flags, etc.).
 * Steps:
 *
 *  -# Reads @c ${TS_TOPDIR}/.nethermind_path to locate the Nethermind source
 *     tree and resolves it to an absolute path via @c realpath(3).  If no
 *     checkout exists there (fresh CI workspace), shallow-clones the
 *     repository first (@c TS_NETHERMIND_REPO env var overrides the URL,
 *     default #NETHERMIND_REPO_DEFAULT).
 *  -# Updates the repository: @c git @c fetch @c origin followed by
 *     @c git @c reset @c --hard @c FETCH_HEAD.
 *  -# Removes any stale @c nethermind binary so the build always produces a
 *     fresh artefact from the updated sources.
 *  -# Runs @c make @c build @c BFLAT_IMAGE=<image> in
 *     @c src/Nethermind/Nethermind.Stateless.ZiskGuest, which performs the
 *     @c dotnet build and the bflat (riscv64/zisk) compilation inside a
 *     Docker container.
 *  -# Copies the resulting @c bin/nethermind ELF to
 *     @c ${TS_TOPDIR}/bflat-ts/zisk_guest/bin/nethermind.
 *  -# Verifies that the binary is present and logs its size.
 *
 * @param ta          Test Agent name (must have Docker and dotnet available)
 * @param bflat_image bflat Docker image; @c - or omitted picks up
 *                    @c TS_BFLAT_IMAGE from the environment, falling back to
 *                    #TSAPI_BFLAT_DEFAULT_IMAGE
 *
 * Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
 *
 * @author Maksim Menshikov <maksim.menshikov@nethermind.io>
 */

#ifndef TE_TEST_NAME
#define TE_TEST_NAME "build_guest"
#endif

#include "te_config.h"
#include "tapi_test.h"
#include "tapi_job.h"
#include "tapi_job_factory_rpc.h"
#include "tapi_file.h"
#include "te_file.h"
#include "te_string.h"
#include "rcf_rpc.h"
#include "tapi_rpc_unistd.h"
#include "tapi_rpc_stdio.h"
#include "tapi_rpc_signal.h"
#include "tsapi_bflat.h"
#include <stdio.h>
#include <stdlib.h>
#include <sys/stat.h>
#include <unistd.h>
#include <limits.h>
#include <string.h>

/** Whole-build (dotnet build + bflat) timeout: 20 minutes */
#define BUILD_TIMEOUT_MS    (20 * 60 * 1000)

/** Location of the ZiskGuest project inside the Nethermind tree */
#define GUEST_SUBDIR        "src/Nethermind/Nethermind.Stateless.ZiskGuest"

/** Repository cloned when .nethermind_path points at a missing directory */
#define NETHERMIND_REPO_DEFAULT "https://github.com/NethermindEth/nethermind.git"


int
main(int argc, char **argv)
{
    const char         *ta;
    const char         *bflat_image;

    rcf_rpc_server     *rpcs              = NULL;
    tapi_job_factory_t *factory           = NULL;
    tapi_job_t         *build_job         = NULL;
    tapi_job_channel_t *build_ch[2];
    tapi_job_status_t   build_status;

    te_string   ts_topdir_s  = TE_STRING_INIT;
    te_string   nm_dir_s     = TE_STRING_INIT;
    te_string   bin_path_s   = TE_STRING_INIT;
    te_string   guest_s      = TE_STRING_INIT;
    te_string   image_var_s  = TE_STRING_INIT;
    te_string   cmd_s        = TE_STRING_INIT;

    TEST_START;
    TEST_GET_STRING_PARAM(ta);
    TEST_GET_OPT_STRING_PARAM(bflat_image);

    bflat_image = tsapi_bflat_resolve_image(bflat_image);

    /* ------------------------------------------------------------------ */
    TEST_STEP("Read TS_TOPDIR environment variable");
    {
        const char *env = getenv("TS_TOPDIR");

        if (env == NULL)
            TEST_FAIL("TS_TOPDIR environment variable is not set");
        CHECK_RC(te_string_append(&ts_topdir_s, "%s", env));
        RING("TS_TOPDIR = %s", ts_topdir_s.ptr);
    }

    /* ------------------------------------------------------------------ */
    TEST_STEP("Read .nethermind_path and resolve Nethermind directory");
    {
        char  nm_path_file[PATH_MAX];
        char  nm_path_raw[PATH_MAX];
        char  nm_path_combined[PATH_MAX * 2 + 2];
        char  nm_path_abs[PATH_MAX];
        FILE *f;
        char *p;
        const char *nm_target = NULL;

        snprintf(nm_path_file, sizeof(nm_path_file),
                 "%s/.nethermind_path", ts_topdir_s.ptr);

        f = fopen(nm_path_file, "r");
        if (f == NULL)
            TEST_FAIL("Cannot open '%s'", nm_path_file);

        if (fgets(nm_path_raw, sizeof(nm_path_raw), f) == NULL)
        {
            fclose(f);
            TEST_FAIL("Cannot read nethermind path from '%s'", nm_path_file);
        }
        fclose(f);

        p = strchr(nm_path_raw, '\n');
        if (p != NULL)
            *p = '\0';
        p = strchr(nm_path_raw, '\r');
        if (p != NULL)
            *p = '\0';

        if (nm_path_raw[0] == '/')
        {
            nm_target = nm_path_raw;
        }
        else
        {
            snprintf(nm_path_combined, sizeof(nm_path_combined),
                     "%s/%s", ts_topdir_s.ptr, nm_path_raw);
            nm_target = nm_path_combined;
        }

        /*
         * Fresh CI workspaces contain no Nethermind checkout next to the
         * suite: shallow-clone one so the prologue is self-sufficient.
         * --depth 1 implies --single-branch, so the later plain
         * 'git fetch origin' still resolves FETCH_HEAD to the default
         * branch tip, and a rev-specific fetch works as before.
         */
        if (access(nm_target, F_OK) != 0)
        {
            const char *repo = getenv("TS_NETHERMIND_REPO");
            char        clone_cmd[PATH_MAX * 3 + 256];
            int         clone_rc;

            if (repo == NULL || *repo == '\0')
                repo = NETHERMIND_REPO_DEFAULT;

            TEST_STEP("Clone Nethermind repository (missing checkout)");
            RING("No checkout at '%s'; cloning from '%s'", nm_target, repo);
            snprintf(clone_cmd, sizeof(clone_cmd),
                     "git clone --depth 1 '%s' '%s' 2>&1", repo, nm_target);
            RING("Running: %s", clone_cmd);
            clone_rc = system(clone_cmd);
            if (clone_rc != 0)
                TEST_FAIL("'git clone %s' into '%s' failed (exit code %d)",
                          repo, nm_target, clone_rc);
        }

        if (realpath(nm_target, nm_path_abs) == NULL)
            TEST_FAIL("realpath('%s') failed", nm_target);

        CHECK_RC(te_string_append(&nm_dir_s, "%s", nm_path_abs));
        RING("Nethermind directory: %s", nm_dir_s.ptr);
    }

    /* ------------------------------------------------------------------ */
    TEST_STEP("Update Nethermind repository (git pull + git reset --hard)");
    {
        char        git_cmd[PATH_MAX + 256];
        int         git_rc;
        const char *rev = getenv("TS_NETHERMIND_REV");

        if (rev != NULL && *rev == '\0')
            rev = NULL;

        /*
         * git fetch origin [<rev>] — update remote-tracking refs without
         * touching the working tree and set FETCH_HEAD to the fetched tip.
         * When TS_NETHERMIND_REV is set we fetch that specific revision
         * (branch/tag/SHA); otherwise we fetch the default ref.  Both work
         * regardless of the local branch name (main, master, etc.).
         */
        if (rev != NULL)
        {
            RING("Building Nethermind at revision '%s'", rev);
            snprintf(git_cmd, sizeof(git_cmd),
                     "git -C '%s' fetch origin '%s' 2>&1",
                     nm_dir_s.ptr, rev);
        }
        else
        {
            snprintf(git_cmd, sizeof(git_cmd),
                     "git -C '%s' fetch origin 2>&1", nm_dir_s.ptr);
        }
        RING("Running: %s", git_cmd);
        git_rc = system(git_cmd);
        if (git_rc != 0)
            TEST_FAIL("'git fetch origin' failed in '%s' (exit code %d)",
                      nm_dir_s.ptr, git_rc);

        /*
         * git reset --hard FETCH_HEAD — reset the working tree to whatever
         * was just fetched, discarding any local modifications.  We always
         * reset to FETCH_HEAD (not to <rev> by name): after a rev-specific
         * fetch FETCH_HEAD points at exactly the requested commit, whereas a
         * bare branch name would resolve to the (possibly stale) local branch
         * and a fetched tag/SHA may not have a local ref at all.
         */
        snprintf(git_cmd, sizeof(git_cmd),
                 "git -C '%s' reset --hard FETCH_HEAD 2>&1", nm_dir_s.ptr);
        RING("Running: %s", git_cmd);
        git_rc = system(git_cmd);
        if (git_rc != 0)
            TEST_FAIL("'git reset --hard' failed in '%s' (exit code %d)",
                      nm_dir_s.ptr, git_rc);

        RING("Nethermind repository updated successfully");
    }

    /* ------------------------------------------------------------------ */
    TEST_STEP("Remove stale binary so a fresh build is always performed");
    CHECK_RC(te_string_append(&bin_path_s,
                              "%s/bflat-ts/zisk_guest/bin/nethermind",
                              ts_topdir_s.ptr));

    if (access(bin_path_s.ptr, F_OK) == 0)
    {
        if (unlink(bin_path_s.ptr) != 0)
            TEST_FAIL("Failed to remove stale binary '%s'", bin_path_s.ptr);
        RING("Removed stale binary: %s", bin_path_s.ptr);
    }
    else
    {
        RING("No stale binary found at '%s'", bin_path_s.ptr);
    }

    /* ------------------------------------------------------------------ */
    TEST_STEP("Create RPC server on agent '%s'", ta);
    CHECK_RC(rcf_rpc_server_create(ta, "rpcs_build_guest", &rpcs));

    CHECK_RC(te_string_append(&guest_s, "%s/" GUEST_SUBDIR, nm_dir_s.ptr));

    /* ------------------------------------------------------------------ */
    TEST_STEP("Build ZiskGuest via Nethermind's Makefile (image='%s')",
              bflat_image);
    {
        const char *make_argv[6];

        CHECK_RC(te_string_append(&image_var_s,
                                  "BFLAT_IMAGE=%s", bflat_image));

        make_argv[0] = "make";
        make_argv[1] = "-C";
        make_argv[2] = guest_s.ptr;
        make_argv[3] = "build";
        make_argv[4] = image_var_s.ptr;
        make_argv[5] = NULL;

        CHECK_RC(tapi_job_factory_rpc_create(rpcs, &factory));
        CHECK_RC(tapi_job_create(factory, NULL, "make", make_argv,
                                 NULL, &build_job));
    }

    CHECK_RC(tapi_job_alloc_output_channels(build_job, 2, build_ch));
    CHECK_RC(tapi_job_attach_filter(TAPI_JOB_CHANNEL_SET(build_ch[0]),
                                    "make stdout", false, TE_LL_RING, NULL));
    CHECK_RC(tapi_job_attach_filter(TAPI_JOB_CHANNEL_SET(build_ch[1]),
                                    "make stderr", false, TE_LL_ERROR, NULL));

    CHECK_RC(tapi_job_start(build_job));

    TEST_STEP("Wait for the build to finish (timeout %d ms)",
              BUILD_TIMEOUT_MS);
    CHECK_RC(tapi_job_wait(build_job, BUILD_TIMEOUT_MS, &build_status));

    if (build_status.type != TAPI_JOB_STATUS_EXITED)
        TEST_FAIL("make was killed by a signal (signo=%d)",
                  build_status.value);
    if (build_status.value != 0)
        TEST_FAIL("make exited with non-zero status %d",
                  build_status.value);

    RING("Nethermind Makefile built ZiskGuest successfully");

    /* ------------------------------------------------------------------ */
    TEST_STEP("Copy compiled binary to '%s'", bin_path_s.ptr);
    {
        tarpc_pid_t     cp_pid;
        rpc_wait_status cp_ws;

        CHECK_RC(te_string_append(&cmd_s,
            "mkdir -p '%s/bflat-ts/zisk_guest/bin'"
            " && cp -f '%s/bin/nethermind' '%s'",
            ts_topdir_s.ptr, guest_s.ptr, bin_path_s.ptr));

        RPC_AWAIT_ERROR(rpcs);
        cp_pid = rpc_te_shell_cmd(rpcs, "%s", -1, NULL, NULL, NULL,
                                  cmd_s.ptr);
        if (cp_pid < 0)
            TEST_FAIL("Failed to start copy command");

        RPC_AWAIT_ERROR(rpcs);
        rpc_waitpid(rpcs, cp_pid, &cp_ws, 0);
        if (cp_ws.flag != RPC_WAIT_STATUS_EXITED || cp_ws.value != 0)
            TEST_FAIL("Copy command failed for '%s'", bin_path_s.ptr);
    }

    /* ------------------------------------------------------------------ */
    TEST_STEP("Verify binary at '%s'", bin_path_s.ptr);
    if (access(bin_path_s.ptr, F_OK) != 0)
        TEST_FAIL("Binary not found at '%s' after build", bin_path_s.ptr);

    {
        struct stat st;

        if (stat(bin_path_s.ptr, &st) == 0)
            RING("ZiskGuest binary ready: %s (%lld bytes)",
                 bin_path_s.ptr, (long long)st.st_size);
    }

    TEST_SUCCESS;

cleanup:
    if (build_job != NULL)
        CLEANUP_CHECK_RC(tapi_job_destroy(build_job, -1));

    if (factory != NULL)
        tapi_job_factory_destroy(factory);

    te_string_free(&ts_topdir_s);
    te_string_free(&nm_dir_s);
    te_string_free(&bin_path_s);
    te_string_free(&guest_s);
    te_string_free(&image_var_s);
    te_string_free(&cmd_s);

    if (rpcs != NULL)
        CLEANUP_CHECK_RC(rcf_rpc_server_destroy(rpcs));

    TEST_END;
}
