/** @file
 * @brief Pure string helpers shared by test binaries (ACSL-verified)
 *
 * Self-contained helpers with no Test Environment dependencies. Every
 * function carries an ACSL contract and is verified with Frama-C/WP in CI
 * (scripts/frama_c_check.sh); keep this file free of TE headers so Frama-C
 * can parse it without the TE build tree.
 *
 * Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
 *
 * @author Maksim Menshikov <maksim.menshikov@nethermind.io>
 */

#ifndef __TS_PURE_H__
#define __TS_PURE_H__

#ifdef __cplusplus
extern "C" {
#endif

/**
 * Lower-case an ASCII character; anything outside 'A'..'Z' is returned
 * unchanged.
 *
 * @param c Character value.
 *
 * @return Lower-cased character value.
 */
extern int ts_ascii_lower(int c);

/**
 * Length of an optional "0x"/"0X" prefix of a length-delimited string.
 *
 * @param s   String start (need not be NUL-terminated).
 * @param len Number of readable characters at @p s.
 *
 * @return @c 2 when the prefix is present, @c 0 otherwise.
 */
extern long ts_hex_prefix_len(const char *s, long len);

/**
 * Case-insensitive comparison of two length-delimited character ranges.
 *
 * @param a  First range.
 * @param an Length of @p a.
 * @param b  Second range.
 * @param bn Length of @p b.
 *
 * @return @c 1 when both ranges have equal length and equal characters
 *         ignoring ASCII case, @c 0 otherwise.
 */
extern int ts_hex_bytes_equal_ci(const char *a, long an,
                                 const char *b, long bn);

/**
 * Compare two hash strings, ignoring ASCII case and an optional "0x"/"0X"
 * prefix on either side.
 *
 * @param a First NUL-terminated hash string.
 * @param b Second NUL-terminated hash string.
 *
 * @return @c 1 when the hashes match, @c 0 otherwise.
 */
extern int ts_hash_str_equal(const char *a, const char *b);

#ifdef __cplusplus
} /* extern "C" */
#endif

#endif /* __TS_PURE_H__ */
