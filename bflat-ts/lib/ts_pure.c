/** @file
 * @brief Pure string helpers shared by test binaries (ACSL-verified)
 *
 * Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
 *
 * @author Maksim Menshikov <maksim.menshikov@nethermind.io>
 */

#include <string.h>

#include "ts_pure.h"

/*@
  assigns \nothing;
  behavior upper:
    assumes 'A' <= c <= 'Z';
    ensures \result == c + 32;
  behavior other:
    assumes c < 'A' || c > 'Z';
    ensures \result == c;
  complete behaviors;
  disjoint behaviors;
*/
int
ts_ascii_lower(int c)
{
    if (c >= 'A' && c <= 'Z')
        return c + 32;
    return c;
}

/*@
  requires len >= 0;
  requires \valid_read(s + (0 .. len - 1));
  assigns \nothing;
  behavior has_prefix:
    assumes len >= 2 && s[0] == '0' && (s[1] == 'x' || s[1] == 'X');
    ensures \result == 2;
  behavior no_prefix:
    assumes !(len >= 2 && s[0] == '0' && (s[1] == 'x' || s[1] == 'X'));
    ensures \result == 0;
  complete behaviors;
  disjoint behaviors;
*/
long
ts_hex_prefix_len(const char *s, long len)
{
    if (len >= 2 && s[0] == '0' && (s[1] == 'x' || s[1] == 'X'))
        return 2;
    return 0;
}

/*@
  requires an >= 0 && bn >= 0;
  requires \valid_read(a + (0 .. an - 1));
  requires \valid_read(b + (0 .. bn - 1));
  assigns \nothing;
  ensures \result == 0 || \result == 1;
  ensures \result == 1 ==> an == bn;
*/
int
ts_hex_bytes_equal_ci(const char *a, long an, const char *b, long bn)
{
    long i;

    if (an != bn)
        return 0;

    /*@
      loop invariant 0 <= i <= an;
      loop assigns i;
      loop variant an - i;
    */
    for (i = 0; i < an; i++)
    {
        if (ts_ascii_lower((unsigned char)a[i]) !=
            ts_ascii_lower((unsigned char)b[i]))
            return 0;
    }

    return 1;
}

/*@
  requires valid_read_string(a);
  requires valid_read_string(b);
  requires strlen(a) <= 4096;
  requires strlen(b) <= 4096;
  assigns \nothing;
  ensures \result == 0 || \result == 1;
*/
int
ts_hash_str_equal(const char *a, const char *b)
{
    long an = (long)strlen(a);
    long bn = (long)strlen(b);
    long pa = ts_hex_prefix_len(a, an);
    long pb = ts_hex_prefix_len(b, bn);

    return ts_hex_bytes_equal_ci(a + pa, an - pa, b + pb, bn - pb);
}
