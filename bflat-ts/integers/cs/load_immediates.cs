// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Constants loaded into a register.
//
// A constant that does not fit an addi is built from lui and addiw for its
// upper 32 bits and from slli and addi steps for the rest. The compiler now
// uses addi instead of addiw wherever the 32-bit sum does not wrap, loads
// one that fits 12 bits with a single addi, and builds a lui immediate
// shifted left with lui and slli alone, also when the lui part is negative.
// What can go wrong: addi where the sum does wrap (0x7FFFF800 to 0x7FFFFFFF,
// where lui gives -2^31 and the low part is negative), a low part taken
// unsigned or a lui part taken unsigned or one too large, and a shifted
// form chosen for a value it does not produce.
//
// Every constant below is returned by a method of its own as a long, used
// as the right operand of an addition, of a 32-bit operation and of a
// compare, and stored to memory, and each result is compared with the value
// parsed at run time from the hexadecimal text of the constant. The
// constants sit around the boundaries of the 12-bit, 20-bit and 32-bit
// fields, alone and shifted; the rest are random. The methods are the same
// three or six lines for every constant.
//
// A failed check prints one line; the run ends with ALL PASS or a FAIL
// count, and the guest returns 1 when anything failed.
using System;
using System.Runtime.CompilerServices;

unsafe class Program
{
    const MethodImplOptions NI = MethodImplOptions.NoInlining;
    const MethodImplOptions NoOpt = MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization;

    static int fails;
    static int checks;
    static long[] cellL = new long[2];
    static int[] cellI = new int[2];

    [MethodImpl(NoOpt)]
    static long Parse(string hex)
    {
        ulong v = 0;
        for (int i = 0; i < hex.Length; i++)
        {
            char c = hex[i];
            ulong d = c <= '9' ? (ulong)(c - '0') : (ulong)(c - 'A' + 10);
            v = v * 16 + d;
        }
        return (long)v;
    }

    [MethodImpl(NoOpt)]
    static long S32(long v)
    {
        v &= 0xFFFFFFFFL;
        if (v >= 0x80000000L) v -= 0x100000000L;
        return v;
    }

    static void Eq(string hex, string what, long want, long got)
    {
        checks++;
        if (want == got) return;
        fails++;
        if (fails <= 60)
            Console.WriteLine("FAIL 0x" + hex + " " + what + " expected 0x" + want.ToString("x") + " got 0x" + got.ToString("x"));
    }

    // x is 0x0123456789ABCDEF, y is 0x13579BDF, read from memory.
    [MethodImpl(NoOpt)]
    static void Check(string hex, long loaded, long sum, long mixed, long x, int y)
    {
        long k = Parse(hex);
        Eq(hex, "loaded", k, loaded);
        Eq(hex, "x + k", x + k, sum);
        long want = (x < k ? 1 : 0) + ((ulong)x < (ulong)k ? 2 : 0) + (x == k ? 4 : 0);
        want = want * 31 + (x & k);
        want = want * 31 + (x ^ k);
        Eq(hex, "compares and logic", want, mixed);
    }

    [MethodImpl(NoOpt)]
    static void CheckInt(string hex, long loaded, long mixed, long stored, int y)
    {
        long k = S32(Parse(hex));
        Eq(hex, "loaded as int", k, loaded);
        long want = S32(y + k);
        want = want * 31 + S32(y * k);
        want = want * 31 + (y & k);
        want = want * 31 + (y < k ? 1 : 0) + ((y & 0xFFFFFFFFL) < (k & 0xFFFFFFFFL) ? 2 : 0) + (y == k ? 4 : 0);
        Eq(hex, "int operations", want, mixed);
        Eq(hex, "stored", k * 3 + k, stored);
    }

    [MethodImpl(NI)] static long K0() => unchecked((long)0x0000000000000000UL);
    [MethodImpl(NI)] static long A0(long x) => x + unchecked((long)0x0000000000000000UL);
    [MethodImpl(NI)] static long C0(long x) { long r = (x < unchecked((long)0x0000000000000000UL) ? 1 : 0) + ((ulong)x < 0x0000000000000000UL ? 2 : 0) + (x == unchecked((long)0x0000000000000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000000000000UL)); return r * 31 + (x ^ unchecked((long)0x0000000000000000UL)); }
    [MethodImpl(NI)] static long K1() => unchecked((long)0x0000000000000001UL);
    [MethodImpl(NI)] static long A1(long x) => x + unchecked((long)0x0000000000000001UL);
    [MethodImpl(NI)] static long C1(long x) { long r = (x < unchecked((long)0x0000000000000001UL) ? 1 : 0) + ((ulong)x < 0x0000000000000001UL ? 2 : 0) + (x == unchecked((long)0x0000000000000001UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000000000001UL)); return r * 31 + (x ^ unchecked((long)0x0000000000000001UL)); }
    [MethodImpl(NI)] static long K2() => unchecked((long)0x0000000000000002UL);
    [MethodImpl(NI)] static long A2(long x) => x + unchecked((long)0x0000000000000002UL);
    [MethodImpl(NI)] static long C2(long x) { long r = (x < unchecked((long)0x0000000000000002UL) ? 1 : 0) + ((ulong)x < 0x0000000000000002UL ? 2 : 0) + (x == unchecked((long)0x0000000000000002UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000000000002UL)); return r * 31 + (x ^ unchecked((long)0x0000000000000002UL)); }
    [MethodImpl(NI)] static long K3() => unchecked((long)0x0000000000000152UL);
    [MethodImpl(NI)] static long A3(long x) => x + unchecked((long)0x0000000000000152UL);
    [MethodImpl(NI)] static long C3(long x) { long r = (x < unchecked((long)0x0000000000000152UL) ? 1 : 0) + ((ulong)x < 0x0000000000000152UL ? 2 : 0) + (x == unchecked((long)0x0000000000000152UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000000000152UL)); return r * 31 + (x ^ unchecked((long)0x0000000000000152UL)); }
    [MethodImpl(NI)] static long K4() => unchecked((long)0x00000000000001AEUL);
    [MethodImpl(NI)] static long A4(long x) => x + unchecked((long)0x00000000000001AEUL);
    [MethodImpl(NI)] static long C4(long x) { long r = (x < unchecked((long)0x00000000000001AEUL) ? 1 : 0) + ((ulong)x < 0x00000000000001AEUL ? 2 : 0) + (x == unchecked((long)0x00000000000001AEUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000000001AEUL)); return r * 31 + (x ^ unchecked((long)0x00000000000001AEUL)); }
    [MethodImpl(NI)] static long K5() => unchecked((long)0x000000000000025DUL);
    [MethodImpl(NI)] static long A5(long x) => x + unchecked((long)0x000000000000025DUL);
    [MethodImpl(NI)] static long C5(long x) { long r = (x < unchecked((long)0x000000000000025DUL) ? 1 : 0) + ((ulong)x < 0x000000000000025DUL ? 2 : 0) + (x == unchecked((long)0x000000000000025DUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000000000000025DUL)); return r * 31 + (x ^ unchecked((long)0x000000000000025DUL)); }
    [MethodImpl(NI)] static long K6() => unchecked((long)0x00000000000007FEUL);
    [MethodImpl(NI)] static long A6(long x) => x + unchecked((long)0x00000000000007FEUL);
    [MethodImpl(NI)] static long C6(long x) { long r = (x < unchecked((long)0x00000000000007FEUL) ? 1 : 0) + ((ulong)x < 0x00000000000007FEUL ? 2 : 0) + (x == unchecked((long)0x00000000000007FEUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000000007FEUL)); return r * 31 + (x ^ unchecked((long)0x00000000000007FEUL)); }
    [MethodImpl(NI)] static long K7() => unchecked((long)0x00000000000007FFUL);
    [MethodImpl(NI)] static long A7(long x) => x + unchecked((long)0x00000000000007FFUL);
    [MethodImpl(NI)] static long C7(long x) { long r = (x < unchecked((long)0x00000000000007FFUL) ? 1 : 0) + ((ulong)x < 0x00000000000007FFUL ? 2 : 0) + (x == unchecked((long)0x00000000000007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000000007FFUL)); return r * 31 + (x ^ unchecked((long)0x00000000000007FFUL)); }
    [MethodImpl(NI)] static long K8() => unchecked((long)0x0000000000000800UL);
    [MethodImpl(NI)] static long A8(long x) => x + unchecked((long)0x0000000000000800UL);
    [MethodImpl(NI)] static long C8(long x) { long r = (x < unchecked((long)0x0000000000000800UL) ? 1 : 0) + ((ulong)x < 0x0000000000000800UL ? 2 : 0) + (x == unchecked((long)0x0000000000000800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000000000800UL)); return r * 31 + (x ^ unchecked((long)0x0000000000000800UL)); }
    [MethodImpl(NI)] static long K9() => unchecked((long)0x0000000000000801UL);
    [MethodImpl(NI)] static long A9(long x) => x + unchecked((long)0x0000000000000801UL);
    [MethodImpl(NI)] static long C9(long x) { long r = (x < unchecked((long)0x0000000000000801UL) ? 1 : 0) + ((ulong)x < 0x0000000000000801UL ? 2 : 0) + (x == unchecked((long)0x0000000000000801UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000000000801UL)); return r * 31 + (x ^ unchecked((long)0x0000000000000801UL)); }
    [MethodImpl(NI)] static long K10() => unchecked((long)0x0000000000000802UL);
    [MethodImpl(NI)] static long A10(long x) => x + unchecked((long)0x0000000000000802UL);
    [MethodImpl(NI)] static long C10(long x) { long r = (x < unchecked((long)0x0000000000000802UL) ? 1 : 0) + ((ulong)x < 0x0000000000000802UL ? 2 : 0) + (x == unchecked((long)0x0000000000000802UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000000000802UL)); return r * 31 + (x ^ unchecked((long)0x0000000000000802UL)); }
    [MethodImpl(NI)] static long K11() => unchecked((long)0x0000000000000CFCUL);
    [MethodImpl(NI)] static long A11(long x) => x + unchecked((long)0x0000000000000CFCUL);
    [MethodImpl(NI)] static long C11(long x) { long r = (x < unchecked((long)0x0000000000000CFCUL) ? 1 : 0) + ((ulong)x < 0x0000000000000CFCUL ? 2 : 0) + (x == unchecked((long)0x0000000000000CFCUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000000000CFCUL)); return r * 31 + (x ^ unchecked((long)0x0000000000000CFCUL)); }
    [MethodImpl(NI)] static long K12() => unchecked((long)0x0000000000000FFEUL);
    [MethodImpl(NI)] static long A12(long x) => x + unchecked((long)0x0000000000000FFEUL);
    [MethodImpl(NI)] static long C12(long x) { long r = (x < unchecked((long)0x0000000000000FFEUL) ? 1 : 0) + ((ulong)x < 0x0000000000000FFEUL ? 2 : 0) + (x == unchecked((long)0x0000000000000FFEUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000000000FFEUL)); return r * 31 + (x ^ unchecked((long)0x0000000000000FFEUL)); }
    [MethodImpl(NI)] static long K13() => unchecked((long)0x0000000000000FFFUL);
    [MethodImpl(NI)] static long A13(long x) => x + unchecked((long)0x0000000000000FFFUL);
    [MethodImpl(NI)] static long C13(long x) { long r = (x < unchecked((long)0x0000000000000FFFUL) ? 1 : 0) + ((ulong)x < 0x0000000000000FFFUL ? 2 : 0) + (x == unchecked((long)0x0000000000000FFFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000000000FFFUL)); return r * 31 + (x ^ unchecked((long)0x0000000000000FFFUL)); }
    [MethodImpl(NI)] static long K14() => unchecked((long)0x0000000000001000UL);
    [MethodImpl(NI)] static long A14(long x) => x + unchecked((long)0x0000000000001000UL);
    [MethodImpl(NI)] static long C14(long x) { long r = (x < unchecked((long)0x0000000000001000UL) ? 1 : 0) + ((ulong)x < 0x0000000000001000UL ? 2 : 0) + (x == unchecked((long)0x0000000000001000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000000001000UL)); return r * 31 + (x ^ unchecked((long)0x0000000000001000UL)); }
    [MethodImpl(NI)] static long K15() => unchecked((long)0x0000000000001002UL);
    [MethodImpl(NI)] static long A15(long x) => x + unchecked((long)0x0000000000001002UL);
    [MethodImpl(NI)] static long C15(long x) { long r = (x < unchecked((long)0x0000000000001002UL) ? 1 : 0) + ((ulong)x < 0x0000000000001002UL ? 2 : 0) + (x == unchecked((long)0x0000000000001002UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000000001002UL)); return r * 31 + (x ^ unchecked((long)0x0000000000001002UL)); }
    [MethodImpl(NI)] static long K16() => unchecked((long)0x00000000000017FDUL);
    [MethodImpl(NI)] static long A16(long x) => x + unchecked((long)0x00000000000017FDUL);
    [MethodImpl(NI)] static long C16(long x) { long r = (x < unchecked((long)0x00000000000017FDUL) ? 1 : 0) + ((ulong)x < 0x00000000000017FDUL ? 2 : 0) + (x == unchecked((long)0x00000000000017FDUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000000017FDUL)); return r * 31 + (x ^ unchecked((long)0x00000000000017FDUL)); }
    [MethodImpl(NI)] static long K17() => unchecked((long)0x00000000000017FEUL);
    [MethodImpl(NI)] static long A17(long x) => x + unchecked((long)0x00000000000017FEUL);
    [MethodImpl(NI)] static long C17(long x) { long r = (x < unchecked((long)0x00000000000017FEUL) ? 1 : 0) + ((ulong)x < 0x00000000000017FEUL ? 2 : 0) + (x == unchecked((long)0x00000000000017FEUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000000017FEUL)); return r * 31 + (x ^ unchecked((long)0x00000000000017FEUL)); }
    [MethodImpl(NI)] static long K18() => unchecked((long)0x00000000000017FFUL);
    [MethodImpl(NI)] static long A18(long x) => x + unchecked((long)0x00000000000017FFUL);
    [MethodImpl(NI)] static long C18(long x) { long r = (x < unchecked((long)0x00000000000017FFUL) ? 1 : 0) + ((ulong)x < 0x00000000000017FFUL ? 2 : 0) + (x == unchecked((long)0x00000000000017FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000000017FFUL)); return r * 31 + (x ^ unchecked((long)0x00000000000017FFUL)); }
    [MethodImpl(NI)] static long K19() => unchecked((long)0x0000000000001800UL);
    [MethodImpl(NI)] static long A19(long x) => x + unchecked((long)0x0000000000001800UL);
    [MethodImpl(NI)] static long C19(long x) { long r = (x < unchecked((long)0x0000000000001800UL) ? 1 : 0) + ((ulong)x < 0x0000000000001800UL ? 2 : 0) + (x == unchecked((long)0x0000000000001800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000000001800UL)); return r * 31 + (x ^ unchecked((long)0x0000000000001800UL)); }
    [MethodImpl(NI)] static long K20() => unchecked((long)0x0000000000001801UL);
    [MethodImpl(NI)] static long A20(long x) => x + unchecked((long)0x0000000000001801UL);
    [MethodImpl(NI)] static long C20(long x) { long r = (x < unchecked((long)0x0000000000001801UL) ? 1 : 0) + ((ulong)x < 0x0000000000001801UL ? 2 : 0) + (x == unchecked((long)0x0000000000001801UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000000001801UL)); return r * 31 + (x ^ unchecked((long)0x0000000000001801UL)); }
    [MethodImpl(NI)] static long K21() => unchecked((long)0x0000000000001FFEUL);
    [MethodImpl(NI)] static long A21(long x) => x + unchecked((long)0x0000000000001FFEUL);
    [MethodImpl(NI)] static long C21(long x) { long r = (x < unchecked((long)0x0000000000001FFEUL) ? 1 : 0) + ((ulong)x < 0x0000000000001FFEUL ? 2 : 0) + (x == unchecked((long)0x0000000000001FFEUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000000001FFEUL)); return r * 31 + (x ^ unchecked((long)0x0000000000001FFEUL)); }
    [MethodImpl(NI)] static long K22() => unchecked((long)0x0000000000002000UL);
    [MethodImpl(NI)] static long A22(long x) => x + unchecked((long)0x0000000000002000UL);
    [MethodImpl(NI)] static long C22(long x) { long r = (x < unchecked((long)0x0000000000002000UL) ? 1 : 0) + ((ulong)x < 0x0000000000002000UL ? 2 : 0) + (x == unchecked((long)0x0000000000002000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000000002000UL)); return r * 31 + (x ^ unchecked((long)0x0000000000002000UL)); }
    [MethodImpl(NI)] static long K23() => unchecked((long)0x00000000000027FDUL);
    [MethodImpl(NI)] static long A23(long x) => x + unchecked((long)0x00000000000027FDUL);
    [MethodImpl(NI)] static long C23(long x) { long r = (x < unchecked((long)0x00000000000027FDUL) ? 1 : 0) + ((ulong)x < 0x00000000000027FDUL ? 2 : 0) + (x == unchecked((long)0x00000000000027FDUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000000027FDUL)); return r * 31 + (x ^ unchecked((long)0x00000000000027FDUL)); }
    [MethodImpl(NI)] static long K24() => unchecked((long)0x00000000000027FEUL);
    [MethodImpl(NI)] static long A24(long x) => x + unchecked((long)0x00000000000027FEUL);
    [MethodImpl(NI)] static long C24(long x) { long r = (x < unchecked((long)0x00000000000027FEUL) ? 1 : 0) + ((ulong)x < 0x00000000000027FEUL ? 2 : 0) + (x == unchecked((long)0x00000000000027FEUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000000027FEUL)); return r * 31 + (x ^ unchecked((long)0x00000000000027FEUL)); }
    [MethodImpl(NI)] static long K25() => unchecked((long)0x00000000000027FFUL);
    [MethodImpl(NI)] static long A25(long x) => x + unchecked((long)0x00000000000027FFUL);
    [MethodImpl(NI)] static long C25(long x) { long r = (x < unchecked((long)0x00000000000027FFUL) ? 1 : 0) + ((ulong)x < 0x00000000000027FFUL ? 2 : 0) + (x == unchecked((long)0x00000000000027FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000000027FFUL)); return r * 31 + (x ^ unchecked((long)0x00000000000027FFUL)); }
    [MethodImpl(NI)] static long K26() => unchecked((long)0x0000000000002800UL);
    [MethodImpl(NI)] static long A26(long x) => x + unchecked((long)0x0000000000002800UL);
    [MethodImpl(NI)] static long C26(long x) { long r = (x < unchecked((long)0x0000000000002800UL) ? 1 : 0) + ((ulong)x < 0x0000000000002800UL ? 2 : 0) + (x == unchecked((long)0x0000000000002800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000000002800UL)); return r * 31 + (x ^ unchecked((long)0x0000000000002800UL)); }
    [MethodImpl(NI)] static long K27() => unchecked((long)0x0000000000002FFEUL);
    [MethodImpl(NI)] static long A27(long x) => x + unchecked((long)0x0000000000002FFEUL);
    [MethodImpl(NI)] static long C27(long x) { long r = (x < unchecked((long)0x0000000000002FFEUL) ? 1 : 0) + ((ulong)x < 0x0000000000002FFEUL ? 2 : 0) + (x == unchecked((long)0x0000000000002FFEUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000000002FFEUL)); return r * 31 + (x ^ unchecked((long)0x0000000000002FFEUL)); }
    [MethodImpl(NI)] static long K28() => unchecked((long)0x0000000000003000UL);
    [MethodImpl(NI)] static long A28(long x) => x + unchecked((long)0x0000000000003000UL);
    [MethodImpl(NI)] static long C28(long x) { long r = (x < unchecked((long)0x0000000000003000UL) ? 1 : 0) + ((ulong)x < 0x0000000000003000UL ? 2 : 0) + (x == unchecked((long)0x0000000000003000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000000003000UL)); return r * 31 + (x ^ unchecked((long)0x0000000000003000UL)); }
    [MethodImpl(NI)] static long K29() => unchecked((long)0x00000000000037FDUL);
    [MethodImpl(NI)] static long A29(long x) => x + unchecked((long)0x00000000000037FDUL);
    [MethodImpl(NI)] static long C29(long x) { long r = (x < unchecked((long)0x00000000000037FDUL) ? 1 : 0) + ((ulong)x < 0x00000000000037FDUL ? 2 : 0) + (x == unchecked((long)0x00000000000037FDUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000000037FDUL)); return r * 31 + (x ^ unchecked((long)0x00000000000037FDUL)); }
    [MethodImpl(NI)] static long K30() => unchecked((long)0x00000000000037FFUL);
    [MethodImpl(NI)] static long A30(long x) => x + unchecked((long)0x00000000000037FFUL);
    [MethodImpl(NI)] static long C30(long x) { long r = (x < unchecked((long)0x00000000000037FFUL) ? 1 : 0) + ((ulong)x < 0x00000000000037FFUL ? 2 : 0) + (x == unchecked((long)0x00000000000037FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000000037FFUL)); return r * 31 + (x ^ unchecked((long)0x00000000000037FFUL)); }
    [MethodImpl(NI)] static long K31() => unchecked((long)0x0000000000004000UL);
    [MethodImpl(NI)] static long A31(long x) => x + unchecked((long)0x0000000000004000UL);
    [MethodImpl(NI)] static long C31(long x) { long r = (x < unchecked((long)0x0000000000004000UL) ? 1 : 0) + ((ulong)x < 0x0000000000004000UL ? 2 : 0) + (x == unchecked((long)0x0000000000004000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000000004000UL)); return r * 31 + (x ^ unchecked((long)0x0000000000004000UL)); }
    [MethodImpl(NI)] static long K32() => unchecked((long)0x0000000000008000UL);
    [MethodImpl(NI)] static long A32(long x) => x + unchecked((long)0x0000000000008000UL);
    [MethodImpl(NI)] static long C32(long x) { long r = (x < unchecked((long)0x0000000000008000UL) ? 1 : 0) + ((ulong)x < 0x0000000000008000UL ? 2 : 0) + (x == unchecked((long)0x0000000000008000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000000008000UL)); return r * 31 + (x ^ unchecked((long)0x0000000000008000UL)); }
    [MethodImpl(NI)] static long K33() => unchecked((long)0x0000000000010000UL);
    [MethodImpl(NI)] static long A33(long x) => x + unchecked((long)0x0000000000010000UL);
    [MethodImpl(NI)] static long C33(long x) { long r = (x < unchecked((long)0x0000000000010000UL) ? 1 : 0) + ((ulong)x < 0x0000000000010000UL ? 2 : 0) + (x == unchecked((long)0x0000000000010000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000000010000UL)); return r * 31 + (x ^ unchecked((long)0x0000000000010000UL)); }
    [MethodImpl(NI)] static long K34() => unchecked((long)0x00000000000CD7FFUL);
    [MethodImpl(NI)] static long A34(long x) => x + unchecked((long)0x00000000000CD7FFUL);
    [MethodImpl(NI)] static long C34(long x) { long r = (x < unchecked((long)0x00000000000CD7FFUL) ? 1 : 0) + ((ulong)x < 0x00000000000CD7FFUL ? 2 : 0) + (x == unchecked((long)0x00000000000CD7FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000000CD7FFUL)); return r * 31 + (x ^ unchecked((long)0x00000000000CD7FFUL)); }
    [MethodImpl(NI)] static long K35() => unchecked((long)0x00000000000CD97BUL);
    [MethodImpl(NI)] static long A35(long x) => x + unchecked((long)0x00000000000CD97BUL);
    [MethodImpl(NI)] static long C35(long x) { long r = (x < unchecked((long)0x00000000000CD97BUL) ? 1 : 0) + ((ulong)x < 0x00000000000CD97BUL ? 2 : 0) + (x == unchecked((long)0x00000000000CD97BUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000000CD97BUL)); return r * 31 + (x ^ unchecked((long)0x00000000000CD97BUL)); }
    [MethodImpl(NI)] static long K36() => unchecked((long)0x0000000000100000UL);
    [MethodImpl(NI)] static long A36(long x) => x + unchecked((long)0x0000000000100000UL);
    [MethodImpl(NI)] static long C36(long x) { long r = (x < unchecked((long)0x0000000000100000UL) ? 1 : 0) + ((ulong)x < 0x0000000000100000UL ? 2 : 0) + (x == unchecked((long)0x0000000000100000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000000100000UL)); return r * 31 + (x ^ unchecked((long)0x0000000000100000UL)); }
    [MethodImpl(NI)] static long K37() => unchecked((long)0x00000000007FE800UL);
    [MethodImpl(NI)] static long A37(long x) => x + unchecked((long)0x00000000007FE800UL);
    [MethodImpl(NI)] static long C37(long x) { long r = (x < unchecked((long)0x00000000007FE800UL) ? 1 : 0) + ((ulong)x < 0x00000000007FE800UL ? 2 : 0) + (x == unchecked((long)0x00000000007FE800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000007FE800UL)); return r * 31 + (x ^ unchecked((long)0x00000000007FE800UL)); }
    [MethodImpl(NI)] static long K38() => unchecked((long)0x00000000007FF000UL);
    [MethodImpl(NI)] static long A38(long x) => x + unchecked((long)0x00000000007FF000UL);
    [MethodImpl(NI)] static long C38(long x) { long r = (x < unchecked((long)0x00000000007FF000UL) ? 1 : 0) + ((ulong)x < 0x00000000007FF000UL ? 2 : 0) + (x == unchecked((long)0x00000000007FF000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000007FF000UL)); return r * 31 + (x ^ unchecked((long)0x00000000007FF000UL)); }
    [MethodImpl(NI)] static long K39() => unchecked((long)0x00000000007FF7FFUL);
    [MethodImpl(NI)] static long A39(long x) => x + unchecked((long)0x00000000007FF7FFUL);
    [MethodImpl(NI)] static long C39(long x) { long r = (x < unchecked((long)0x00000000007FF7FFUL) ? 1 : 0) + ((ulong)x < 0x00000000007FF7FFUL ? 2 : 0) + (x == unchecked((long)0x00000000007FF7FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000007FF7FFUL)); return r * 31 + (x ^ unchecked((long)0x00000000007FF7FFUL)); }
    [MethodImpl(NI)] static long K40() => unchecked((long)0x00000000007FF800UL);
    [MethodImpl(NI)] static long A40(long x) => x + unchecked((long)0x00000000007FF800UL);
    [MethodImpl(NI)] static long C40(long x) { long r = (x < unchecked((long)0x00000000007FF800UL) ? 1 : 0) + ((ulong)x < 0x00000000007FF800UL ? 2 : 0) + (x == unchecked((long)0x00000000007FF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000007FF800UL)); return r * 31 + (x ^ unchecked((long)0x00000000007FF800UL)); }
    [MethodImpl(NI)] static long K41() => unchecked((long)0x0000000000800000UL);
    [MethodImpl(NI)] static long A41(long x) => x + unchecked((long)0x0000000000800000UL);
    [MethodImpl(NI)] static long C41(long x) { long r = (x < unchecked((long)0x0000000000800000UL) ? 1 : 0) + ((ulong)x < 0x0000000000800000UL ? 2 : 0) + (x == unchecked((long)0x0000000000800000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000000800000UL)); return r * 31 + (x ^ unchecked((long)0x0000000000800000UL)); }
    [MethodImpl(NI)] static long K42() => unchecked((long)0x00000000008007FFUL);
    [MethodImpl(NI)] static long A42(long x) => x + unchecked((long)0x00000000008007FFUL);
    [MethodImpl(NI)] static long C42(long x) { long r = (x < unchecked((long)0x00000000008007FFUL) ? 1 : 0) + ((ulong)x < 0x00000000008007FFUL ? 2 : 0) + (x == unchecked((long)0x00000000008007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000008007FFUL)); return r * 31 + (x ^ unchecked((long)0x00000000008007FFUL)); }
    [MethodImpl(NI)] static long K43() => unchecked((long)0x0000000000800800UL);
    [MethodImpl(NI)] static long A43(long x) => x + unchecked((long)0x0000000000800800UL);
    [MethodImpl(NI)] static long C43(long x) { long r = (x < unchecked((long)0x0000000000800800UL) ? 1 : 0) + ((ulong)x < 0x0000000000800800UL ? 2 : 0) + (x == unchecked((long)0x0000000000800800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000000800800UL)); return r * 31 + (x ^ unchecked((long)0x0000000000800800UL)); }
    [MethodImpl(NI)] static long K44() => unchecked((long)0x0000000000801000UL);
    [MethodImpl(NI)] static long A44(long x) => x + unchecked((long)0x0000000000801000UL);
    [MethodImpl(NI)] static long C44(long x) { long r = (x < unchecked((long)0x0000000000801000UL) ? 1 : 0) + ((ulong)x < 0x0000000000801000UL ? 2 : 0) + (x == unchecked((long)0x0000000000801000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000000801000UL)); return r * 31 + (x ^ unchecked((long)0x0000000000801000UL)); }
    [MethodImpl(NI)] static long K45() => unchecked((long)0x00000000008017FFUL);
    [MethodImpl(NI)] static long A45(long x) => x + unchecked((long)0x00000000008017FFUL);
    [MethodImpl(NI)] static long C45(long x) { long r = (x < unchecked((long)0x00000000008017FFUL) ? 1 : 0) + ((ulong)x < 0x00000000008017FFUL ? 2 : 0) + (x == unchecked((long)0x00000000008017FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000008017FFUL)); return r * 31 + (x ^ unchecked((long)0x00000000008017FFUL)); }
    [MethodImpl(NI)] static long K46() => unchecked((long)0x0000000000FFE800UL);
    [MethodImpl(NI)] static long A46(long x) => x + unchecked((long)0x0000000000FFE800UL);
    [MethodImpl(NI)] static long C46(long x) { long r = (x < unchecked((long)0x0000000000FFE800UL) ? 1 : 0) + ((ulong)x < 0x0000000000FFE800UL ? 2 : 0) + (x == unchecked((long)0x0000000000FFE800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000000FFE800UL)); return r * 31 + (x ^ unchecked((long)0x0000000000FFE800UL)); }
    [MethodImpl(NI)] static long K47() => unchecked((long)0x0000000000FFF000UL);
    [MethodImpl(NI)] static long A47(long x) => x + unchecked((long)0x0000000000FFF000UL);
    [MethodImpl(NI)] static long C47(long x) { long r = (x < unchecked((long)0x0000000000FFF000UL) ? 1 : 0) + ((ulong)x < 0x0000000000FFF000UL ? 2 : 0) + (x == unchecked((long)0x0000000000FFF000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000000FFF000UL)); return r * 31 + (x ^ unchecked((long)0x0000000000FFF000UL)); }
    [MethodImpl(NI)] static long K48() => unchecked((long)0x0000000000FFF7FFUL);
    [MethodImpl(NI)] static long A48(long x) => x + unchecked((long)0x0000000000FFF7FFUL);
    [MethodImpl(NI)] static long C48(long x) { long r = (x < unchecked((long)0x0000000000FFF7FFUL) ? 1 : 0) + ((ulong)x < 0x0000000000FFF7FFUL ? 2 : 0) + (x == unchecked((long)0x0000000000FFF7FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000000FFF7FFUL)); return r * 31 + (x ^ unchecked((long)0x0000000000FFF7FFUL)); }
    [MethodImpl(NI)] static long K49() => unchecked((long)0x0000000000FFF800UL);
    [MethodImpl(NI)] static long A49(long x) => x + unchecked((long)0x0000000000FFF800UL);
    [MethodImpl(NI)] static long C49(long x) { long r = (x < unchecked((long)0x0000000000FFF800UL) ? 1 : 0) + ((ulong)x < 0x0000000000FFF800UL ? 2 : 0) + (x == unchecked((long)0x0000000000FFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000000FFF800UL)); return r * 31 + (x ^ unchecked((long)0x0000000000FFF800UL)); }
    [MethodImpl(NI)] static long K50() => unchecked((long)0x0000000001000000UL);
    [MethodImpl(NI)] static long A50(long x) => x + unchecked((long)0x0000000001000000UL);
    [MethodImpl(NI)] static long C50(long x) { long r = (x < unchecked((long)0x0000000001000000UL) ? 1 : 0) + ((ulong)x < 0x0000000001000000UL ? 2 : 0) + (x == unchecked((long)0x0000000001000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000001000000UL)); return r * 31 + (x ^ unchecked((long)0x0000000001000000UL)); }
    [MethodImpl(NI)] static long K51() => unchecked((long)0x00000000010007FFUL);
    [MethodImpl(NI)] static long A51(long x) => x + unchecked((long)0x00000000010007FFUL);
    [MethodImpl(NI)] static long C51(long x) { long r = (x < unchecked((long)0x00000000010007FFUL) ? 1 : 0) + ((ulong)x < 0x00000000010007FFUL ? 2 : 0) + (x == unchecked((long)0x00000000010007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000010007FFUL)); return r * 31 + (x ^ unchecked((long)0x00000000010007FFUL)); }
    [MethodImpl(NI)] static long K52() => unchecked((long)0x00000000017FE800UL);
    [MethodImpl(NI)] static long A52(long x) => x + unchecked((long)0x00000000017FE800UL);
    [MethodImpl(NI)] static long C52(long x) { long r = (x < unchecked((long)0x00000000017FE800UL) ? 1 : 0) + ((ulong)x < 0x00000000017FE800UL ? 2 : 0) + (x == unchecked((long)0x00000000017FE800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000017FE800UL)); return r * 31 + (x ^ unchecked((long)0x00000000017FE800UL)); }
    [MethodImpl(NI)] static long K53() => unchecked((long)0x00000000017FF000UL);
    [MethodImpl(NI)] static long A53(long x) => x + unchecked((long)0x00000000017FF000UL);
    [MethodImpl(NI)] static long C53(long x) { long r = (x < unchecked((long)0x00000000017FF000UL) ? 1 : 0) + ((ulong)x < 0x00000000017FF000UL ? 2 : 0) + (x == unchecked((long)0x00000000017FF000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000017FF000UL)); return r * 31 + (x ^ unchecked((long)0x00000000017FF000UL)); }
    [MethodImpl(NI)] static long K54() => unchecked((long)0x00000000017FF7FFUL);
    [MethodImpl(NI)] static long A54(long x) => x + unchecked((long)0x00000000017FF7FFUL);
    [MethodImpl(NI)] static long C54(long x) { long r = (x < unchecked((long)0x00000000017FF7FFUL) ? 1 : 0) + ((ulong)x < 0x00000000017FF7FFUL ? 2 : 0) + (x == unchecked((long)0x00000000017FF7FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000017FF7FFUL)); return r * 31 + (x ^ unchecked((long)0x00000000017FF7FFUL)); }
    [MethodImpl(NI)] static long K55() => unchecked((long)0x00000000017FF800UL);
    [MethodImpl(NI)] static long A55(long x) => x + unchecked((long)0x00000000017FF800UL);
    [MethodImpl(NI)] static long C55(long x) { long r = (x < unchecked((long)0x00000000017FF800UL) ? 1 : 0) + ((ulong)x < 0x00000000017FF800UL ? 2 : 0) + (x == unchecked((long)0x00000000017FF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000017FF800UL)); return r * 31 + (x ^ unchecked((long)0x00000000017FF800UL)); }
    [MethodImpl(NI)] static long K56() => unchecked((long)0x0000000001800000UL);
    [MethodImpl(NI)] static long A56(long x) => x + unchecked((long)0x0000000001800000UL);
    [MethodImpl(NI)] static long C56(long x) { long r = (x < unchecked((long)0x0000000001800000UL) ? 1 : 0) + ((ulong)x < 0x0000000001800000UL ? 2 : 0) + (x == unchecked((long)0x0000000001800000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000001800000UL)); return r * 31 + (x ^ unchecked((long)0x0000000001800000UL)); }
    [MethodImpl(NI)] static long K57() => unchecked((long)0x00000000018007FFUL);
    [MethodImpl(NI)] static long A57(long x) => x + unchecked((long)0x00000000018007FFUL);
    [MethodImpl(NI)] static long C57(long x) { long r = (x < unchecked((long)0x00000000018007FFUL) ? 1 : 0) + ((ulong)x < 0x00000000018007FFUL ? 2 : 0) + (x == unchecked((long)0x00000000018007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000018007FFUL)); return r * 31 + (x ^ unchecked((long)0x00000000018007FFUL)); }
    [MethodImpl(NI)] static long K58() => unchecked((long)0x0000000010000000UL);
    [MethodImpl(NI)] static long A58(long x) => x + unchecked((long)0x0000000010000000UL);
    [MethodImpl(NI)] static long C58(long x) { long r = (x < unchecked((long)0x0000000010000000UL) ? 1 : 0) + ((ulong)x < 0x0000000010000000UL ? 2 : 0) + (x == unchecked((long)0x0000000010000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000010000000UL)); return r * 31 + (x ^ unchecked((long)0x0000000010000000UL)); }
    [MethodImpl(NI)] static long K59() => unchecked((long)0x0000000012345000UL);
    [MethodImpl(NI)] static long A59(long x) => x + unchecked((long)0x0000000012345000UL);
    [MethodImpl(NI)] static long C59(long x) { long r = (x < unchecked((long)0x0000000012345000UL) ? 1 : 0) + ((ulong)x < 0x0000000012345000UL ? 2 : 0) + (x == unchecked((long)0x0000000012345000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000012345000UL)); return r * 31 + (x ^ unchecked((long)0x0000000012345000UL)); }
    [MethodImpl(NI)] static long K60() => unchecked((long)0x0000000012345678UL);
    [MethodImpl(NI)] static long A60(long x) => x + unchecked((long)0x0000000012345678UL);
    [MethodImpl(NI)] static long C60(long x) { long r = (x < unchecked((long)0x0000000012345678UL) ? 1 : 0) + ((ulong)x < 0x0000000012345678UL ? 2 : 0) + (x == unchecked((long)0x0000000012345678UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000012345678UL)); return r * 31 + (x ^ unchecked((long)0x0000000012345678UL)); }
    [MethodImpl(NI)] static long K61() => unchecked((long)0x000000001BD16000UL);
    [MethodImpl(NI)] static long A61(long x) => x + unchecked((long)0x000000001BD16000UL);
    [MethodImpl(NI)] static long C61(long x) { long r = (x < unchecked((long)0x000000001BD16000UL) ? 1 : 0) + ((ulong)x < 0x000000001BD16000UL ? 2 : 0) + (x == unchecked((long)0x000000001BD16000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000000001BD16000UL)); return r * 31 + (x ^ unchecked((long)0x000000001BD16000UL)); }
    [MethodImpl(NI)] static long K62() => unchecked((long)0x000000001BD16135UL);
    [MethodImpl(NI)] static long A62(long x) => x + unchecked((long)0x000000001BD16135UL);
    [MethodImpl(NI)] static long C62(long x) { long r = (x < unchecked((long)0x000000001BD16135UL) ? 1 : 0) + ((ulong)x < 0x000000001BD16135UL ? 2 : 0) + (x == unchecked((long)0x000000001BD16135UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000000001BD16135UL)); return r * 31 + (x ^ unchecked((long)0x000000001BD16135UL)); }
    [MethodImpl(NI)] static long K63() => unchecked((long)0x000000002468A000UL);
    [MethodImpl(NI)] static long A63(long x) => x + unchecked((long)0x000000002468A000UL);
    [MethodImpl(NI)] static long C63(long x) { long r = (x < unchecked((long)0x000000002468A000UL) ? 1 : 0) + ((ulong)x < 0x000000002468A000UL ? 2 : 0) + (x == unchecked((long)0x000000002468A000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000000002468A000UL)); return r * 31 + (x ^ unchecked((long)0x000000002468A000UL)); }
    [MethodImpl(NI)] static long K64() => unchecked((long)0x000000002468A4F0UL);
    [MethodImpl(NI)] static long A64(long x) => x + unchecked((long)0x000000002468A4F0UL);
    [MethodImpl(NI)] static long C64(long x) { long r = (x < unchecked((long)0x000000002468A4F0UL) ? 1 : 0) + ((ulong)x < 0x000000002468A4F0UL ? 2 : 0) + (x == unchecked((long)0x000000002468A4F0UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000000002468A4F0UL)); return r * 31 + (x ^ unchecked((long)0x000000002468A4F0UL)); }
    [MethodImpl(NI)] static long K65() => unchecked((long)0x000000002468ACF0UL);
    [MethodImpl(NI)] static long A65(long x) => x + unchecked((long)0x000000002468ACF0UL);
    [MethodImpl(NI)] static long C65(long x) { long r = (x < unchecked((long)0x000000002468ACF0UL) ? 1 : 0) + ((ulong)x < 0x000000002468ACF0UL ? 2 : 0) + (x == unchecked((long)0x000000002468ACF0UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000000002468ACF0UL)); return r * 31 + (x ^ unchecked((long)0x000000002468ACF0UL)); }
    [MethodImpl(NI)] static long K66() => unchecked((long)0x000000002468B4EFUL);
    [MethodImpl(NI)] static long A66(long x) => x + unchecked((long)0x000000002468B4EFUL);
    [MethodImpl(NI)] static long C66(long x) { long r = (x < unchecked((long)0x000000002468B4EFUL) ? 1 : 0) + ((ulong)x < 0x000000002468B4EFUL ? 2 : 0) + (x == unchecked((long)0x000000002468B4EFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000000002468B4EFUL)); return r * 31 + (x ^ unchecked((long)0x000000002468B4EFUL)); }
    [MethodImpl(NI)] static long K67() => unchecked((long)0x000000002FFFC000UL);
    [MethodImpl(NI)] static long A67(long x) => x + unchecked((long)0x000000002FFFC000UL);
    [MethodImpl(NI)] static long C67(long x) { long r = (x < unchecked((long)0x000000002FFFC000UL) ? 1 : 0) + ((ulong)x < 0x000000002FFFC000UL ? 2 : 0) + (x == unchecked((long)0x000000002FFFC000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000000002FFFC000UL)); return r * 31 + (x ^ unchecked((long)0x000000002FFFC000UL)); }
    [MethodImpl(NI)] static long K68() => unchecked((long)0x000000003154A800UL);
    [MethodImpl(NI)] static long A68(long x) => x + unchecked((long)0x000000003154A800UL);
    [MethodImpl(NI)] static long C68(long x) { long r = (x < unchecked((long)0x000000003154A800UL) ? 1 : 0) + ((ulong)x < 0x000000003154A800UL ? 2 : 0) + (x == unchecked((long)0x000000003154A800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000000003154A800UL)); return r * 31 + (x ^ unchecked((long)0x000000003154A800UL)); }
    [MethodImpl(NI)] static long K69() => unchecked((long)0x000000003154AD39UL);
    [MethodImpl(NI)] static long A69(long x) => x + unchecked((long)0x000000003154AD39UL);
    [MethodImpl(NI)] static long C69(long x) { long r = (x < unchecked((long)0x000000003154AD39UL) ? 1 : 0) + ((ulong)x < 0x000000003154AD39UL ? 2 : 0) + (x == unchecked((long)0x000000003154AD39UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000000003154AD39UL)); return r * 31 + (x ^ unchecked((long)0x000000003154AD39UL)); }
    [MethodImpl(NI)] static long K70() => unchecked((long)0x0000000032FD8338UL);
    [MethodImpl(NI)] static long A70(long x) => x + unchecked((long)0x0000000032FD8338UL);
    [MethodImpl(NI)] static long C70(long x) { long r = (x < unchecked((long)0x0000000032FD8338UL) ? 1 : 0) + ((ulong)x < 0x0000000032FD8338UL ? 2 : 0) + (x == unchecked((long)0x0000000032FD8338UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000032FD8338UL)); return r * 31 + (x ^ unchecked((long)0x0000000032FD8338UL)); }
    [MethodImpl(NI)] static long K71() => unchecked((long)0x0000000032FD8800UL);
    [MethodImpl(NI)] static long A71(long x) => x + unchecked((long)0x0000000032FD8800UL);
    [MethodImpl(NI)] static long C71(long x) { long r = (x < unchecked((long)0x0000000032FD8800UL) ? 1 : 0) + ((ulong)x < 0x0000000032FD8800UL ? 2 : 0) + (x == unchecked((long)0x0000000032FD8800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000032FD8800UL)); return r * 31 + (x ^ unchecked((long)0x0000000032FD8800UL)); }
    [MethodImpl(NI)] static long K72() => unchecked((long)0x000000003FFFF800UL);
    [MethodImpl(NI)] static long A72(long x) => x + unchecked((long)0x000000003FFFF800UL);
    [MethodImpl(NI)] static long C72(long x) { long r = (x < unchecked((long)0x000000003FFFF800UL) ? 1 : 0) + ((ulong)x < 0x000000003FFFF800UL ? 2 : 0) + (x == unchecked((long)0x000000003FFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000000003FFFF800UL)); return r * 31 + (x ^ unchecked((long)0x000000003FFFF800UL)); }
    [MethodImpl(NI)] static long K73() => unchecked((long)0x0000000040000000UL);
    [MethodImpl(NI)] static long A73(long x) => x + unchecked((long)0x0000000040000000UL);
    [MethodImpl(NI)] static long C73(long x) { long r = (x < unchecked((long)0x0000000040000000UL) ? 1 : 0) + ((ulong)x < 0x0000000040000000UL ? 2 : 0) + (x == unchecked((long)0x0000000040000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000040000000UL)); return r * 31 + (x ^ unchecked((long)0x0000000040000000UL)); }
    [MethodImpl(NI)] static long K74() => unchecked((long)0x0000000048D14000UL);
    [MethodImpl(NI)] static long A74(long x) => x + unchecked((long)0x0000000048D14000UL);
    [MethodImpl(NI)] static long C74(long x) { long r = (x < unchecked((long)0x0000000048D14000UL) ? 1 : 0) + ((ulong)x < 0x0000000048D14000UL ? 2 : 0) + (x == unchecked((long)0x0000000048D14000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000048D14000UL)); return r * 31 + (x ^ unchecked((long)0x0000000048D14000UL)); }
    [MethodImpl(NI)] static long K75() => unchecked((long)0x000000005FFF7800UL);
    [MethodImpl(NI)] static long A75(long x) => x + unchecked((long)0x000000005FFF7800UL);
    [MethodImpl(NI)] static long C75(long x) { long r = (x < unchecked((long)0x000000005FFF7800UL) ? 1 : 0) + ((ulong)x < 0x000000005FFF7800UL ? 2 : 0) + (x == unchecked((long)0x000000005FFF7800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000000005FFF7800UL)); return r * 31 + (x ^ unchecked((long)0x000000005FFF7800UL)); }
    [MethodImpl(NI)] static long K76() => unchecked((long)0x000000005FFF8000UL);
    [MethodImpl(NI)] static long A76(long x) => x + unchecked((long)0x000000005FFF8000UL);
    [MethodImpl(NI)] static long C76(long x) { long r = (x < unchecked((long)0x000000005FFF8000UL) ? 1 : 0) + ((ulong)x < 0x000000005FFF8000UL ? 2 : 0) + (x == unchecked((long)0x000000005FFF8000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000000005FFF8000UL)); return r * 31 + (x ^ unchecked((long)0x000000005FFF8000UL)); }
    [MethodImpl(NI)] static long K77() => unchecked((long)0x000000005FFF87FFUL);
    [MethodImpl(NI)] static long A77(long x) => x + unchecked((long)0x000000005FFF87FFUL);
    [MethodImpl(NI)] static long C77(long x) { long r = (x < unchecked((long)0x000000005FFF87FFUL) ? 1 : 0) + ((ulong)x < 0x000000005FFF87FFUL ? 2 : 0) + (x == unchecked((long)0x000000005FFF87FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000000005FFF87FFUL)); return r * 31 + (x ^ unchecked((long)0x000000005FFF87FFUL)); }
    [MethodImpl(NI)] static long K78() => unchecked((long)0x0000000075833652UL);
    [MethodImpl(NI)] static long A78(long x) => x + unchecked((long)0x0000000075833652UL);
    [MethodImpl(NI)] static long C78(long x) { long r = (x < unchecked((long)0x0000000075833652UL) ? 1 : 0) + ((ulong)x < 0x0000000075833652UL ? 2 : 0) + (x == unchecked((long)0x0000000075833652UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000075833652UL)); return r * 31 + (x ^ unchecked((long)0x0000000075833652UL)); }
    [MethodImpl(NI)] static long K79() => unchecked((long)0x00000000758337FFUL);
    [MethodImpl(NI)] static long A79(long x) => x + unchecked((long)0x00000000758337FFUL);
    [MethodImpl(NI)] static long C79(long x) { long r = (x < unchecked((long)0x00000000758337FFUL) ? 1 : 0) + ((ulong)x < 0x00000000758337FFUL ? 2 : 0) + (x == unchecked((long)0x00000000758337FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000758337FFUL)); return r * 31 + (x ^ unchecked((long)0x00000000758337FFUL)); }
    [MethodImpl(NI)] static long K80() => unchecked((long)0x000000007FF007FFUL);
    [MethodImpl(NI)] static long A80(long x) => x + unchecked((long)0x000000007FF007FFUL);
    [MethodImpl(NI)] static long C80(long x) { long r = (x < unchecked((long)0x000000007FF007FFUL) ? 1 : 0) + ((ulong)x < 0x000000007FF007FFUL ? 2 : 0) + (x == unchecked((long)0x000000007FF007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000000007FF007FFUL)); return r * 31 + (x ^ unchecked((long)0x000000007FF007FFUL)); }
    [MethodImpl(NI)] static long K81() => unchecked((long)0x000000007FFFE800UL);
    [MethodImpl(NI)] static long A81(long x) => x + unchecked((long)0x000000007FFFE800UL);
    [MethodImpl(NI)] static long C81(long x) { long r = (x < unchecked((long)0x000000007FFFE800UL) ? 1 : 0) + ((ulong)x < 0x000000007FFFE800UL ? 2 : 0) + (x == unchecked((long)0x000000007FFFE800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000000007FFFE800UL)); return r * 31 + (x ^ unchecked((long)0x000000007FFFE800UL)); }
    [MethodImpl(NI)] static long K82() => unchecked((long)0x000000007FFFF000UL);
    [MethodImpl(NI)] static long A82(long x) => x + unchecked((long)0x000000007FFFF000UL);
    [MethodImpl(NI)] static long C82(long x) { long r = (x < unchecked((long)0x000000007FFFF000UL) ? 1 : 0) + ((ulong)x < 0x000000007FFFF000UL ? 2 : 0) + (x == unchecked((long)0x000000007FFFF000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000000007FFFF000UL)); return r * 31 + (x ^ unchecked((long)0x000000007FFFF000UL)); }
    [MethodImpl(NI)] static long K83() => unchecked((long)0x000000007FFFF7FFUL);
    [MethodImpl(NI)] static long A83(long x) => x + unchecked((long)0x000000007FFFF7FFUL);
    [MethodImpl(NI)] static long C83(long x) { long r = (x < unchecked((long)0x000000007FFFF7FFUL) ? 1 : 0) + ((ulong)x < 0x000000007FFFF7FFUL ? 2 : 0) + (x == unchecked((long)0x000000007FFFF7FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000000007FFFF7FFUL)); return r * 31 + (x ^ unchecked((long)0x000000007FFFF7FFUL)); }
    [MethodImpl(NI)] static long K84() => unchecked((long)0x000000007FFFF800UL);
    [MethodImpl(NI)] static long A84(long x) => x + unchecked((long)0x000000007FFFF800UL);
    [MethodImpl(NI)] static long C84(long x) { long r = (x < unchecked((long)0x000000007FFFF800UL) ? 1 : 0) + ((ulong)x < 0x000000007FFFF800UL ? 2 : 0) + (x == unchecked((long)0x000000007FFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000000007FFFF800UL)); return r * 31 + (x ^ unchecked((long)0x000000007FFFF800UL)); }
    [MethodImpl(NI)] static long K85() => unchecked((long)0x000000007FFFF801UL);
    [MethodImpl(NI)] static long A85(long x) => x + unchecked((long)0x000000007FFFF801UL);
    [MethodImpl(NI)] static long C85(long x) { long r = (x < unchecked((long)0x000000007FFFF801UL) ? 1 : 0) + ((ulong)x < 0x000000007FFFF801UL ? 2 : 0) + (x == unchecked((long)0x000000007FFFF801UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000000007FFFF801UL)); return r * 31 + (x ^ unchecked((long)0x000000007FFFF801UL)); }
    [MethodImpl(NI)] static long K86() => unchecked((long)0x000000007FFFFFFFUL);
    [MethodImpl(NI)] static long A86(long x) => x + unchecked((long)0x000000007FFFFFFFUL);
    [MethodImpl(NI)] static long C86(long x) { long r = (x < unchecked((long)0x000000007FFFFFFFUL) ? 1 : 0) + ((ulong)x < 0x000000007FFFFFFFUL ? 2 : 0) + (x == unchecked((long)0x000000007FFFFFFFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000000007FFFFFFFUL)); return r * 31 + (x ^ unchecked((long)0x000000007FFFFFFFUL)); }
    [MethodImpl(NI)] static long K87() => unchecked((long)0x0000000080000000UL);
    [MethodImpl(NI)] static long A87(long x) => x + unchecked((long)0x0000000080000000UL);
    [MethodImpl(NI)] static long C87(long x) { long r = (x < unchecked((long)0x0000000080000000UL) ? 1 : 0) + ((ulong)x < 0x0000000080000000UL ? 2 : 0) + (x == unchecked((long)0x0000000080000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000080000000UL)); return r * 31 + (x ^ unchecked((long)0x0000000080000000UL)); }
    [MethodImpl(NI)] static long K88() => unchecked((long)0x0000000080000001UL);
    [MethodImpl(NI)] static long A88(long x) => x + unchecked((long)0x0000000080000001UL);
    [MethodImpl(NI)] static long C88(long x) { long r = (x < unchecked((long)0x0000000080000001UL) ? 1 : 0) + ((ulong)x < 0x0000000080000001UL ? 2 : 0) + (x == unchecked((long)0x0000000080000001UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000080000001UL)); return r * 31 + (x ^ unchecked((long)0x0000000080000001UL)); }
    [MethodImpl(NI)] static long K89() => unchecked((long)0x00000000800007FFUL);
    [MethodImpl(NI)] static long A89(long x) => x + unchecked((long)0x00000000800007FFUL);
    [MethodImpl(NI)] static long C89(long x) { long r = (x < unchecked((long)0x00000000800007FFUL) ? 1 : 0) + ((ulong)x < 0x00000000800007FFUL ? 2 : 0) + (x == unchecked((long)0x00000000800007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000800007FFUL)); return r * 31 + (x ^ unchecked((long)0x00000000800007FFUL)); }
    [MethodImpl(NI)] static long K90() => unchecked((long)0x0000000080000800UL);
    [MethodImpl(NI)] static long A90(long x) => x + unchecked((long)0x0000000080000800UL);
    [MethodImpl(NI)] static long C90(long x) { long r = (x < unchecked((long)0x0000000080000800UL) ? 1 : 0) + ((ulong)x < 0x0000000080000800UL ? 2 : 0) + (x == unchecked((long)0x0000000080000800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000080000800UL)); return r * 31 + (x ^ unchecked((long)0x0000000080000800UL)); }
    [MethodImpl(NI)] static long K91() => unchecked((long)0x0000000087654321UL);
    [MethodImpl(NI)] static long A91(long x) => x + unchecked((long)0x0000000087654321UL);
    [MethodImpl(NI)] static long C91(long x) { long r = (x < unchecked((long)0x0000000087654321UL) ? 1 : 0) + ((ulong)x < 0x0000000087654321UL ? 2 : 0) + (x == unchecked((long)0x0000000087654321UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000087654321UL)); return r * 31 + (x ^ unchecked((long)0x0000000087654321UL)); }
    [MethodImpl(NI)] static long K92() => unchecked((long)0x0000000091A28000UL);
    [MethodImpl(NI)] static long A92(long x) => x + unchecked((long)0x0000000091A28000UL);
    [MethodImpl(NI)] static long C92(long x) { long r = (x < unchecked((long)0x0000000091A28000UL) ? 1 : 0) + ((ulong)x < 0x0000000091A28000UL ? 2 : 0) + (x == unchecked((long)0x0000000091A28000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000091A28000UL)); return r * 31 + (x ^ unchecked((long)0x0000000091A28000UL)); }
    [MethodImpl(NI)] static long K93() => unchecked((long)0x00000000BFFEFFF8UL);
    [MethodImpl(NI)] static long A93(long x) => x + unchecked((long)0x00000000BFFEFFF8UL);
    [MethodImpl(NI)] static long C93(long x) { long r = (x < unchecked((long)0x00000000BFFEFFF8UL) ? 1 : 0) + ((ulong)x < 0x00000000BFFEFFF8UL ? 2 : 0) + (x == unchecked((long)0x00000000BFFEFFF8UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000BFFEFFF8UL)); return r * 31 + (x ^ unchecked((long)0x00000000BFFEFFF8UL)); }
    [MethodImpl(NI)] static long K94() => unchecked((long)0x00000000BFFF0000UL);
    [MethodImpl(NI)] static long A94(long x) => x + unchecked((long)0x00000000BFFF0000UL);
    [MethodImpl(NI)] static long C94(long x) { long r = (x < unchecked((long)0x00000000BFFF0000UL) ? 1 : 0) + ((ulong)x < 0x00000000BFFF0000UL ? 2 : 0) + (x == unchecked((long)0x00000000BFFF0000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000BFFF0000UL)); return r * 31 + (x ^ unchecked((long)0x00000000BFFF0000UL)); }
    [MethodImpl(NI)] static long K95() => unchecked((long)0x00000000C0000800UL);
    [MethodImpl(NI)] static long A95(long x) => x + unchecked((long)0x00000000C0000800UL);
    [MethodImpl(NI)] static long C95(long x) { long r = (x < unchecked((long)0x00000000C0000800UL) ? 1 : 0) + ((ulong)x < 0x00000000C0000800UL ? 2 : 0) + (x == unchecked((long)0x00000000C0000800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000C0000800UL)); return r * 31 + (x ^ unchecked((long)0x00000000C0000800UL)); }
    [MethodImpl(NI)] static long K96() => unchecked((long)0x00000000FDB6C553UL);
    [MethodImpl(NI)] static long A96(long x) => x + unchecked((long)0x00000000FDB6C553UL);
    [MethodImpl(NI)] static long C96(long x) { long r = (x < unchecked((long)0x00000000FDB6C553UL) ? 1 : 0) + ((ulong)x < 0x00000000FDB6C553UL ? 2 : 0) + (x == unchecked((long)0x00000000FDB6C553UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000FDB6C553UL)); return r * 31 + (x ^ unchecked((long)0x00000000FDB6C553UL)); }
    [MethodImpl(NI)] static long K97() => unchecked((long)0x00000000FDB6C800UL);
    [MethodImpl(NI)] static long A97(long x) => x + unchecked((long)0x00000000FDB6C800UL);
    [MethodImpl(NI)] static long C97(long x) { long r = (x < unchecked((long)0x00000000FDB6C800UL) ? 1 : 0) + ((ulong)x < 0x00000000FDB6C800UL ? 2 : 0) + (x == unchecked((long)0x00000000FDB6C800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000FDB6C800UL)); return r * 31 + (x ^ unchecked((long)0x00000000FDB6C800UL)); }
    [MethodImpl(NI)] static long K98() => unchecked((long)0x00000000FFE007FEUL);
    [MethodImpl(NI)] static long A98(long x) => x + unchecked((long)0x00000000FFE007FEUL);
    [MethodImpl(NI)] static long C98(long x) { long r = (x < unchecked((long)0x00000000FFE007FEUL) ? 1 : 0) + ((ulong)x < 0x00000000FFE007FEUL ? 2 : 0) + (x == unchecked((long)0x00000000FFE007FEUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000FFE007FEUL)); return r * 31 + (x ^ unchecked((long)0x00000000FFE007FEUL)); }
    [MethodImpl(NI)] static long K99() => unchecked((long)0x00000000FFE00FFEUL);
    [MethodImpl(NI)] static long A99(long x) => x + unchecked((long)0x00000000FFE00FFEUL);
    [MethodImpl(NI)] static long C99(long x) { long r = (x < unchecked((long)0x00000000FFE00FFEUL) ? 1 : 0) + ((ulong)x < 0x00000000FFE00FFEUL ? 2 : 0) + (x == unchecked((long)0x00000000FFE00FFEUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000FFE00FFEUL)); return r * 31 + (x ^ unchecked((long)0x00000000FFE00FFEUL)); }
    [MethodImpl(NI)] static long K100() => unchecked((long)0x00000000FFE017FDUL);
    [MethodImpl(NI)] static long A100(long x) => x + unchecked((long)0x00000000FFE017FDUL);
    [MethodImpl(NI)] static long C100(long x) { long r = (x < unchecked((long)0x00000000FFE017FDUL) ? 1 : 0) + ((ulong)x < 0x00000000FFE017FDUL ? 2 : 0) + (x == unchecked((long)0x00000000FFE017FDUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000FFE017FDUL)); return r * 31 + (x ^ unchecked((long)0x00000000FFE017FDUL)); }
    [MethodImpl(NI)] static long K101() => unchecked((long)0x00000000FFFFD800UL);
    [MethodImpl(NI)] static long A101(long x) => x + unchecked((long)0x00000000FFFFD800UL);
    [MethodImpl(NI)] static long C101(long x) { long r = (x < unchecked((long)0x00000000FFFFD800UL) ? 1 : 0) + ((ulong)x < 0x00000000FFFFD800UL ? 2 : 0) + (x == unchecked((long)0x00000000FFFFD800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000FFFFD800UL)); return r * 31 + (x ^ unchecked((long)0x00000000FFFFD800UL)); }
    [MethodImpl(NI)] static long K102() => unchecked((long)0x00000000FFFFE000UL);
    [MethodImpl(NI)] static long A102(long x) => x + unchecked((long)0x00000000FFFFE000UL);
    [MethodImpl(NI)] static long C102(long x) { long r = (x < unchecked((long)0x00000000FFFFE000UL) ? 1 : 0) + ((ulong)x < 0x00000000FFFFE000UL ? 2 : 0) + (x == unchecked((long)0x00000000FFFFE000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000FFFFE000UL)); return r * 31 + (x ^ unchecked((long)0x00000000FFFFE000UL)); }
    [MethodImpl(NI)] static long K103() => unchecked((long)0x00000000FFFFE7FEUL);
    [MethodImpl(NI)] static long A103(long x) => x + unchecked((long)0x00000000FFFFE7FEUL);
    [MethodImpl(NI)] static long C103(long x) { long r = (x < unchecked((long)0x00000000FFFFE7FEUL) ? 1 : 0) + ((ulong)x < 0x00000000FFFFE7FEUL ? 2 : 0) + (x == unchecked((long)0x00000000FFFFE7FEUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000FFFFE7FEUL)); return r * 31 + (x ^ unchecked((long)0x00000000FFFFE7FEUL)); }
    [MethodImpl(NI)] static long K104() => unchecked((long)0x00000000FFFFE7FFUL);
    [MethodImpl(NI)] static long A104(long x) => x + unchecked((long)0x00000000FFFFE7FFUL);
    [MethodImpl(NI)] static long C104(long x) { long r = (x < unchecked((long)0x00000000FFFFE7FFUL) ? 1 : 0) + ((ulong)x < 0x00000000FFFFE7FFUL ? 2 : 0) + (x == unchecked((long)0x00000000FFFFE7FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000FFFFE7FFUL)); return r * 31 + (x ^ unchecked((long)0x00000000FFFFE7FFUL)); }
    [MethodImpl(NI)] static long K105() => unchecked((long)0x00000000FFFFE800UL);
    [MethodImpl(NI)] static long A105(long x) => x + unchecked((long)0x00000000FFFFE800UL);
    [MethodImpl(NI)] static long C105(long x) { long r = (x < unchecked((long)0x00000000FFFFE800UL) ? 1 : 0) + ((ulong)x < 0x00000000FFFFE800UL ? 2 : 0) + (x == unchecked((long)0x00000000FFFFE800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000FFFFE800UL)); return r * 31 + (x ^ unchecked((long)0x00000000FFFFE800UL)); }
    [MethodImpl(NI)] static long K106() => unchecked((long)0x00000000FFFFE802UL);
    [MethodImpl(NI)] static long A106(long x) => x + unchecked((long)0x00000000FFFFE802UL);
    [MethodImpl(NI)] static long C106(long x) { long r = (x < unchecked((long)0x00000000FFFFE802UL) ? 1 : 0) + ((ulong)x < 0x00000000FFFFE802UL ? 2 : 0) + (x == unchecked((long)0x00000000FFFFE802UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000FFFFE802UL)); return r * 31 + (x ^ unchecked((long)0x00000000FFFFE802UL)); }
    [MethodImpl(NI)] static long K107() => unchecked((long)0x00000000FFFFEFFEUL);
    [MethodImpl(NI)] static long A107(long x) => x + unchecked((long)0x00000000FFFFEFFEUL);
    [MethodImpl(NI)] static long C107(long x) { long r = (x < unchecked((long)0x00000000FFFFEFFEUL) ? 1 : 0) + ((ulong)x < 0x00000000FFFFEFFEUL ? 2 : 0) + (x == unchecked((long)0x00000000FFFFEFFEUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000FFFFEFFEUL)); return r * 31 + (x ^ unchecked((long)0x00000000FFFFEFFEUL)); }
    [MethodImpl(NI)] static long K108() => unchecked((long)0x00000000FFFFF000UL);
    [MethodImpl(NI)] static long A108(long x) => x + unchecked((long)0x00000000FFFFF000UL);
    [MethodImpl(NI)] static long C108(long x) { long r = (x < unchecked((long)0x00000000FFFFF000UL) ? 1 : 0) + ((ulong)x < 0x00000000FFFFF000UL ? 2 : 0) + (x == unchecked((long)0x00000000FFFFF000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000FFFFF000UL)); return r * 31 + (x ^ unchecked((long)0x00000000FFFFF000UL)); }
    [MethodImpl(NI)] static long K109() => unchecked((long)0x00000000FFFFF002UL);
    [MethodImpl(NI)] static long A109(long x) => x + unchecked((long)0x00000000FFFFF002UL);
    [MethodImpl(NI)] static long C109(long x) { long r = (x < unchecked((long)0x00000000FFFFF002UL) ? 1 : 0) + ((ulong)x < 0x00000000FFFFF002UL ? 2 : 0) + (x == unchecked((long)0x00000000FFFFF002UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000FFFFF002UL)); return r * 31 + (x ^ unchecked((long)0x00000000FFFFF002UL)); }
    [MethodImpl(NI)] static long K110() => unchecked((long)0x00000000FFFFF7FDUL);
    [MethodImpl(NI)] static long A110(long x) => x + unchecked((long)0x00000000FFFFF7FDUL);
    [MethodImpl(NI)] static long C110(long x) { long r = (x < unchecked((long)0x00000000FFFFF7FDUL) ? 1 : 0) + ((ulong)x < 0x00000000FFFFF7FDUL ? 2 : 0) + (x == unchecked((long)0x00000000FFFFF7FDUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000FFFFF7FDUL)); return r * 31 + (x ^ unchecked((long)0x00000000FFFFF7FDUL)); }
    [MethodImpl(NI)] static long K111() => unchecked((long)0x00000000FFFFF7FEUL);
    [MethodImpl(NI)] static long A111(long x) => x + unchecked((long)0x00000000FFFFF7FEUL);
    [MethodImpl(NI)] static long C111(long x) { long r = (x < unchecked((long)0x00000000FFFFF7FEUL) ? 1 : 0) + ((ulong)x < 0x00000000FFFFF7FEUL ? 2 : 0) + (x == unchecked((long)0x00000000FFFFF7FEUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000FFFFF7FEUL)); return r * 31 + (x ^ unchecked((long)0x00000000FFFFF7FEUL)); }
    [MethodImpl(NI)] static long K112() => unchecked((long)0x00000000FFFFF7FFUL);
    [MethodImpl(NI)] static long A112(long x) => x + unchecked((long)0x00000000FFFFF7FFUL);
    [MethodImpl(NI)] static long C112(long x) { long r = (x < unchecked((long)0x00000000FFFFF7FFUL) ? 1 : 0) + ((ulong)x < 0x00000000FFFFF7FFUL ? 2 : 0) + (x == unchecked((long)0x00000000FFFFF7FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000FFFFF7FFUL)); return r * 31 + (x ^ unchecked((long)0x00000000FFFFF7FFUL)); }
    [MethodImpl(NI)] static long K113() => unchecked((long)0x00000000FFFFF800UL);
    [MethodImpl(NI)] static long A113(long x) => x + unchecked((long)0x00000000FFFFF800UL);
    [MethodImpl(NI)] static long C113(long x) { long r = (x < unchecked((long)0x00000000FFFFF800UL) ? 1 : 0) + ((ulong)x < 0x00000000FFFFF800UL ? 2 : 0) + (x == unchecked((long)0x00000000FFFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000FFFFF800UL)); return r * 31 + (x ^ unchecked((long)0x00000000FFFFF800UL)); }
    [MethodImpl(NI)] static long K114() => unchecked((long)0x00000000FFFFF801UL);
    [MethodImpl(NI)] static long A114(long x) => x + unchecked((long)0x00000000FFFFF801UL);
    [MethodImpl(NI)] static long C114(long x) { long r = (x < unchecked((long)0x00000000FFFFF801UL) ? 1 : 0) + ((ulong)x < 0x00000000FFFFF801UL ? 2 : 0) + (x == unchecked((long)0x00000000FFFFF801UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000FFFFF801UL)); return r * 31 + (x ^ unchecked((long)0x00000000FFFFF801UL)); }
    [MethodImpl(NI)] static long K115() => unchecked((long)0x00000000FFFFF802UL);
    [MethodImpl(NI)] static long A115(long x) => x + unchecked((long)0x00000000FFFFF802UL);
    [MethodImpl(NI)] static long C115(long x) { long r = (x < unchecked((long)0x00000000FFFFF802UL) ? 1 : 0) + ((ulong)x < 0x00000000FFFFF802UL ? 2 : 0) + (x == unchecked((long)0x00000000FFFFF802UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000FFFFF802UL)); return r * 31 + (x ^ unchecked((long)0x00000000FFFFF802UL)); }
    [MethodImpl(NI)] static long K116() => unchecked((long)0x00000000FFFFFFFEUL);
    [MethodImpl(NI)] static long A116(long x) => x + unchecked((long)0x00000000FFFFFFFEUL);
    [MethodImpl(NI)] static long C116(long x) { long r = (x < unchecked((long)0x00000000FFFFFFFEUL) ? 1 : 0) + ((ulong)x < 0x00000000FFFFFFFEUL ? 2 : 0) + (x == unchecked((long)0x00000000FFFFFFFEUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000FFFFFFFEUL)); return r * 31 + (x ^ unchecked((long)0x00000000FFFFFFFEUL)); }
    [MethodImpl(NI)] static long K117() => unchecked((long)0x00000000FFFFFFFFUL);
    [MethodImpl(NI)] static long A117(long x) => x + unchecked((long)0x00000000FFFFFFFFUL);
    [MethodImpl(NI)] static long C117(long x) { long r = (x < unchecked((long)0x00000000FFFFFFFFUL) ? 1 : 0) + ((ulong)x < 0x00000000FFFFFFFFUL ? 2 : 0) + (x == unchecked((long)0x00000000FFFFFFFFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000000FFFFFFFFUL)); return r * 31 + (x ^ unchecked((long)0x00000000FFFFFFFFUL)); }
    [MethodImpl(NI)] static long K118() => unchecked((long)0x0000000100000000UL);
    [MethodImpl(NI)] static long A118(long x) => x + unchecked((long)0x0000000100000000UL);
    [MethodImpl(NI)] static long C118(long x) { long r = (x < unchecked((long)0x0000000100000000UL) ? 1 : 0) + ((ulong)x < 0x0000000100000000UL ? 2 : 0) + (x == unchecked((long)0x0000000100000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000100000000UL)); return r * 31 + (x ^ unchecked((long)0x0000000100000000UL)); }
    [MethodImpl(NI)] static long K119() => unchecked((long)0x0000000100000001UL);
    [MethodImpl(NI)] static long A119(long x) => x + unchecked((long)0x0000000100000001UL);
    [MethodImpl(NI)] static long C119(long x) { long r = (x < unchecked((long)0x0000000100000001UL) ? 1 : 0) + ((ulong)x < 0x0000000100000001UL ? 2 : 0) + (x == unchecked((long)0x0000000100000001UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000100000001UL)); return r * 31 + (x ^ unchecked((long)0x0000000100000001UL)); }
    [MethodImpl(NI)] static long K120() => unchecked((long)0x0000000100000002UL);
    [MethodImpl(NI)] static long A120(long x) => x + unchecked((long)0x0000000100000002UL);
    [MethodImpl(NI)] static long C120(long x) { long r = (x < unchecked((long)0x0000000100000002UL) ? 1 : 0) + ((ulong)x < 0x0000000100000002UL ? 2 : 0) + (x == unchecked((long)0x0000000100000002UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000100000002UL)); return r * 31 + (x ^ unchecked((long)0x0000000100000002UL)); }
    [MethodImpl(NI)] static long K121() => unchecked((long)0x00000001000007FDUL);
    [MethodImpl(NI)] static long A121(long x) => x + unchecked((long)0x00000001000007FDUL);
    [MethodImpl(NI)] static long C121(long x) { long r = (x < unchecked((long)0x00000001000007FDUL) ? 1 : 0) + ((ulong)x < 0x00000001000007FDUL ? 2 : 0) + (x == unchecked((long)0x00000001000007FDUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000001000007FDUL)); return r * 31 + (x ^ unchecked((long)0x00000001000007FDUL)); }
    [MethodImpl(NI)] static long K122() => unchecked((long)0x00000001000007FEUL);
    [MethodImpl(NI)] static long A122(long x) => x + unchecked((long)0x00000001000007FEUL);
    [MethodImpl(NI)] static long C122(long x) { long r = (x < unchecked((long)0x00000001000007FEUL) ? 1 : 0) + ((ulong)x < 0x00000001000007FEUL ? 2 : 0) + (x == unchecked((long)0x00000001000007FEUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000001000007FEUL)); return r * 31 + (x ^ unchecked((long)0x00000001000007FEUL)); }
    [MethodImpl(NI)] static long K123() => unchecked((long)0x00000001000007FFUL);
    [MethodImpl(NI)] static long A123(long x) => x + unchecked((long)0x00000001000007FFUL);
    [MethodImpl(NI)] static long C123(long x) { long r = (x < unchecked((long)0x00000001000007FFUL) ? 1 : 0) + ((ulong)x < 0x00000001000007FFUL ? 2 : 0) + (x == unchecked((long)0x00000001000007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000001000007FFUL)); return r * 31 + (x ^ unchecked((long)0x00000001000007FFUL)); }
    [MethodImpl(NI)] static long K124() => unchecked((long)0x0000000100000800UL);
    [MethodImpl(NI)] static long A124(long x) => x + unchecked((long)0x0000000100000800UL);
    [MethodImpl(NI)] static long C124(long x) { long r = (x < unchecked((long)0x0000000100000800UL) ? 1 : 0) + ((ulong)x < 0x0000000100000800UL ? 2 : 0) + (x == unchecked((long)0x0000000100000800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000100000800UL)); return r * 31 + (x ^ unchecked((long)0x0000000100000800UL)); }
    [MethodImpl(NI)] static long K125() => unchecked((long)0x0000000100000801UL);
    [MethodImpl(NI)] static long A125(long x) => x + unchecked((long)0x0000000100000801UL);
    [MethodImpl(NI)] static long C125(long x) { long r = (x < unchecked((long)0x0000000100000801UL) ? 1 : 0) + ((ulong)x < 0x0000000100000801UL ? 2 : 0) + (x == unchecked((long)0x0000000100000801UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000100000801UL)); return r * 31 + (x ^ unchecked((long)0x0000000100000801UL)); }
    [MethodImpl(NI)] static long K126() => unchecked((long)0x0000000100000FFEUL);
    [MethodImpl(NI)] static long A126(long x) => x + unchecked((long)0x0000000100000FFEUL);
    [MethodImpl(NI)] static long C126(long x) { long r = (x < unchecked((long)0x0000000100000FFEUL) ? 1 : 0) + ((ulong)x < 0x0000000100000FFEUL ? 2 : 0) + (x == unchecked((long)0x0000000100000FFEUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000100000FFEUL)); return r * 31 + (x ^ unchecked((long)0x0000000100000FFEUL)); }
    [MethodImpl(NI)] static long K127() => unchecked((long)0x0000000100000FFFUL);
    [MethodImpl(NI)] static long A127(long x) => x + unchecked((long)0x0000000100000FFFUL);
    [MethodImpl(NI)] static long C127(long x) { long r = (x < unchecked((long)0x0000000100000FFFUL) ? 1 : 0) + ((ulong)x < 0x0000000100000FFFUL ? 2 : 0) + (x == unchecked((long)0x0000000100000FFFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000100000FFFUL)); return r * 31 + (x ^ unchecked((long)0x0000000100000FFFUL)); }
    [MethodImpl(NI)] static long K128() => unchecked((long)0x0000000100001000UL);
    [MethodImpl(NI)] static long A128(long x) => x + unchecked((long)0x0000000100001000UL);
    [MethodImpl(NI)] static long C128(long x) { long r = (x < unchecked((long)0x0000000100001000UL) ? 1 : 0) + ((ulong)x < 0x0000000100001000UL ? 2 : 0) + (x == unchecked((long)0x0000000100001000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000100001000UL)); return r * 31 + (x ^ unchecked((long)0x0000000100001000UL)); }
    [MethodImpl(NI)] static long K129() => unchecked((long)0x00000001000017FDUL);
    [MethodImpl(NI)] static long A129(long x) => x + unchecked((long)0x00000001000017FDUL);
    [MethodImpl(NI)] static long C129(long x) { long r = (x < unchecked((long)0x00000001000017FDUL) ? 1 : 0) + ((ulong)x < 0x00000001000017FDUL ? 2 : 0) + (x == unchecked((long)0x00000001000017FDUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000001000017FDUL)); return r * 31 + (x ^ unchecked((long)0x00000001000017FDUL)); }
    [MethodImpl(NI)] static long K130() => unchecked((long)0x00000001000017FFUL);
    [MethodImpl(NI)] static long A130(long x) => x + unchecked((long)0x00000001000017FFUL);
    [MethodImpl(NI)] static long C130(long x) { long r = (x < unchecked((long)0x00000001000017FFUL) ? 1 : 0) + ((ulong)x < 0x00000001000017FFUL ? 2 : 0) + (x == unchecked((long)0x00000001000017FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000001000017FFUL)); return r * 31 + (x ^ unchecked((long)0x00000001000017FFUL)); }
    [MethodImpl(NI)] static long K131() => unchecked((long)0x0000000100001800UL);
    [MethodImpl(NI)] static long A131(long x) => x + unchecked((long)0x0000000100001800UL);
    [MethodImpl(NI)] static long C131(long x) { long r = (x < unchecked((long)0x0000000100001800UL) ? 1 : 0) + ((ulong)x < 0x0000000100001800UL ? 2 : 0) + (x == unchecked((long)0x0000000100001800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000100001800UL)); return r * 31 + (x ^ unchecked((long)0x0000000100001800UL)); }
    [MethodImpl(NI)] static long K132() => unchecked((long)0x000000010ECA7E42UL);
    [MethodImpl(NI)] static long A132(long x) => x + unchecked((long)0x000000010ECA7E42UL);
    [MethodImpl(NI)] static long C132(long x) { long r = (x < unchecked((long)0x000000010ECA7E42UL) ? 1 : 0) + ((ulong)x < 0x000000010ECA7E42UL ? 2 : 0) + (x == unchecked((long)0x000000010ECA7E42UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000000010ECA7E42UL)); return r * 31 + (x ^ unchecked((long)0x000000010ECA7E42UL)); }
    [MethodImpl(NI)] static long K133() => unchecked((long)0x000000010ECA8642UL);
    [MethodImpl(NI)] static long A133(long x) => x + unchecked((long)0x000000010ECA8642UL);
    [MethodImpl(NI)] static long C133(long x) { long r = (x < unchecked((long)0x000000010ECA8642UL) ? 1 : 0) + ((ulong)x < 0x000000010ECA8642UL ? 2 : 0) + (x == unchecked((long)0x000000010ECA8642UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000000010ECA8642UL)); return r * 31 + (x ^ unchecked((long)0x000000010ECA8642UL)); }
    [MethodImpl(NI)] static long K134() => unchecked((long)0x000000010ECA8E41UL);
    [MethodImpl(NI)] static long A134(long x) => x + unchecked((long)0x000000010ECA8E41UL);
    [MethodImpl(NI)] static long C134(long x) { long r = (x < unchecked((long)0x000000010ECA8E41UL) ? 1 : 0) + ((ulong)x < 0x000000010ECA8E41UL ? 2 : 0) + (x == unchecked((long)0x000000010ECA8E41UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000000010ECA8E41UL)); return r * 31 + (x ^ unchecked((long)0x000000010ECA8E41UL)); }
    [MethodImpl(NI)] static long K135() => unchecked((long)0x0000000112345678UL);
    [MethodImpl(NI)] static long A135(long x) => x + unchecked((long)0x0000000112345678UL);
    [MethodImpl(NI)] static long C135(long x) { long r = (x < unchecked((long)0x0000000112345678UL) ? 1 : 0) + ((ulong)x < 0x0000000112345678UL ? 2 : 0) + (x == unchecked((long)0x0000000112345678UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000112345678UL)); return r * 31 + (x ^ unchecked((long)0x0000000112345678UL)); }
    [MethodImpl(NI)] static long K136() => unchecked((long)0x0000000123450000UL);
    [MethodImpl(NI)] static long A136(long x) => x + unchecked((long)0x0000000123450000UL);
    [MethodImpl(NI)] static long C136(long x) { long r = (x < unchecked((long)0x0000000123450000UL) ? 1 : 0) + ((ulong)x < 0x0000000123450000UL ? 2 : 0) + (x == unchecked((long)0x0000000123450000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000123450000UL)); return r * 31 + (x ^ unchecked((long)0x0000000123450000UL)); }
    [MethodImpl(NI)] static long K137() => unchecked((long)0x000000012FFFC000UL);
    [MethodImpl(NI)] static long A137(long x) => x + unchecked((long)0x000000012FFFC000UL);
    [MethodImpl(NI)] static long C137(long x) { long r = (x < unchecked((long)0x000000012FFFC000UL) ? 1 : 0) + ((ulong)x < 0x000000012FFFC000UL ? 2 : 0) + (x == unchecked((long)0x000000012FFFC000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000000012FFFC000UL)); return r * 31 + (x ^ unchecked((long)0x000000012FFFC000UL)); }
    [MethodImpl(NI)] static long K138() => unchecked((long)0x000000013FFFF800UL);
    [MethodImpl(NI)] static long A138(long x) => x + unchecked((long)0x000000013FFFF800UL);
    [MethodImpl(NI)] static long C138(long x) { long r = (x < unchecked((long)0x000000013FFFF800UL) ? 1 : 0) + ((ulong)x < 0x000000013FFFF800UL ? 2 : 0) + (x == unchecked((long)0x000000013FFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000000013FFFF800UL)); return r * 31 + (x ^ unchecked((long)0x000000013FFFF800UL)); }
    [MethodImpl(NI)] static long K139() => unchecked((long)0x000000017FF007FFUL);
    [MethodImpl(NI)] static long A139(long x) => x + unchecked((long)0x000000017FF007FFUL);
    [MethodImpl(NI)] static long C139(long x) { long r = (x < unchecked((long)0x000000017FF007FFUL) ? 1 : 0) + ((ulong)x < 0x000000017FF007FFUL ? 2 : 0) + (x == unchecked((long)0x000000017FF007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000000017FF007FFUL)); return r * 31 + (x ^ unchecked((long)0x000000017FF007FFUL)); }
    [MethodImpl(NI)] static long K140() => unchecked((long)0x000000017FFDF7F0UL);
    [MethodImpl(NI)] static long A140(long x) => x + unchecked((long)0x000000017FFDF7F0UL);
    [MethodImpl(NI)] static long C140(long x) { long r = (x < unchecked((long)0x000000017FFDF7F0UL) ? 1 : 0) + ((ulong)x < 0x000000017FFDF7F0UL ? 2 : 0) + (x == unchecked((long)0x000000017FFDF7F0UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000000017FFDF7F0UL)); return r * 31 + (x ^ unchecked((long)0x000000017FFDF7F0UL)); }
    [MethodImpl(NI)] static long K141() => unchecked((long)0x000000017FFDF800UL);
    [MethodImpl(NI)] static long A141(long x) => x + unchecked((long)0x000000017FFDF800UL);
    [MethodImpl(NI)] static long C141(long x) { long r = (x < unchecked((long)0x000000017FFDF800UL) ? 1 : 0) + ((ulong)x < 0x000000017FFDF800UL ? 2 : 0) + (x == unchecked((long)0x000000017FFDF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000000017FFDF800UL)); return r * 31 + (x ^ unchecked((long)0x000000017FFDF800UL)); }
    [MethodImpl(NI)] static long K142() => unchecked((long)0x000000017FFDFFF0UL);
    [MethodImpl(NI)] static long A142(long x) => x + unchecked((long)0x000000017FFDFFF0UL);
    [MethodImpl(NI)] static long C142(long x) { long r = (x < unchecked((long)0x000000017FFDFFF0UL) ? 1 : 0) + ((ulong)x < 0x000000017FFDFFF0UL ? 2 : 0) + (x == unchecked((long)0x000000017FFDFFF0UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000000017FFDFFF0UL)); return r * 31 + (x ^ unchecked((long)0x000000017FFDFFF0UL)); }
    [MethodImpl(NI)] static long K143() => unchecked((long)0x000000017FFE0000UL);
    [MethodImpl(NI)] static long A143(long x) => x + unchecked((long)0x000000017FFE0000UL);
    [MethodImpl(NI)] static long C143(long x) { long r = (x < unchecked((long)0x000000017FFE0000UL) ? 1 : 0) + ((ulong)x < 0x000000017FFE0000UL ? 2 : 0) + (x == unchecked((long)0x000000017FFE0000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000000017FFE0000UL)); return r * 31 + (x ^ unchecked((long)0x000000017FFE0000UL)); }
    [MethodImpl(NI)] static long K144() => unchecked((long)0x000000017FFE07EFUL);
    [MethodImpl(NI)] static long A144(long x) => x + unchecked((long)0x000000017FFE07EFUL);
    [MethodImpl(NI)] static long C144(long x) { long r = (x < unchecked((long)0x000000017FFE07EFUL) ? 1 : 0) + ((ulong)x < 0x000000017FFE07EFUL ? 2 : 0) + (x == unchecked((long)0x000000017FFE07EFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000000017FFE07EFUL)); return r * 31 + (x ^ unchecked((long)0x000000017FFE07EFUL)); }
    [MethodImpl(NI)] static long K145() => unchecked((long)0x000000017FFE07FFUL);
    [MethodImpl(NI)] static long A145(long x) => x + unchecked((long)0x000000017FFE07FFUL);
    [MethodImpl(NI)] static long C145(long x) { long r = (x < unchecked((long)0x000000017FFE07FFUL) ? 1 : 0) + ((ulong)x < 0x000000017FFE07FFUL ? 2 : 0) + (x == unchecked((long)0x000000017FFE07FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000000017FFE07FFUL)); return r * 31 + (x ^ unchecked((long)0x000000017FFE07FFUL)); }
    [MethodImpl(NI)] static long K146() => unchecked((long)0x000000017FFFF000UL);
    [MethodImpl(NI)] static long A146(long x) => x + unchecked((long)0x000000017FFFF000UL);
    [MethodImpl(NI)] static long C146(long x) { long r = (x < unchecked((long)0x000000017FFFF000UL) ? 1 : 0) + ((ulong)x < 0x000000017FFFF000UL ? 2 : 0) + (x == unchecked((long)0x000000017FFFF000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000000017FFFF000UL)); return r * 31 + (x ^ unchecked((long)0x000000017FFFF000UL)); }
    [MethodImpl(NI)] static long K147() => unchecked((long)0x000000017FFFF7FFUL);
    [MethodImpl(NI)] static long A147(long x) => x + unchecked((long)0x000000017FFFF7FFUL);
    [MethodImpl(NI)] static long C147(long x) { long r = (x < unchecked((long)0x000000017FFFF7FFUL) ? 1 : 0) + ((ulong)x < 0x000000017FFFF7FFUL ? 2 : 0) + (x == unchecked((long)0x000000017FFFF7FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000000017FFFF7FFUL)); return r * 31 + (x ^ unchecked((long)0x000000017FFFF7FFUL)); }
    [MethodImpl(NI)] static long K148() => unchecked((long)0x000000017FFFF800UL);
    [MethodImpl(NI)] static long A148(long x) => x + unchecked((long)0x000000017FFFF800UL);
    [MethodImpl(NI)] static long C148(long x) { long r = (x < unchecked((long)0x000000017FFFF800UL) ? 1 : 0) + ((ulong)x < 0x000000017FFFF800UL ? 2 : 0) + (x == unchecked((long)0x000000017FFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000000017FFFF800UL)); return r * 31 + (x ^ unchecked((long)0x000000017FFFF800UL)); }
    [MethodImpl(NI)] static long K149() => unchecked((long)0x000000017FFFF801UL);
    [MethodImpl(NI)] static long A149(long x) => x + unchecked((long)0x000000017FFFF801UL);
    [MethodImpl(NI)] static long C149(long x) { long r = (x < unchecked((long)0x000000017FFFF801UL) ? 1 : 0) + ((ulong)x < 0x000000017FFFF801UL ? 2 : 0) + (x == unchecked((long)0x000000017FFFF801UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000000017FFFF801UL)); return r * 31 + (x ^ unchecked((long)0x000000017FFFF801UL)); }
    [MethodImpl(NI)] static long K150() => unchecked((long)0x000000017FFFFFFFUL);
    [MethodImpl(NI)] static long A150(long x) => x + unchecked((long)0x000000017FFFFFFFUL);
    [MethodImpl(NI)] static long C150(long x) { long r = (x < unchecked((long)0x000000017FFFFFFFUL) ? 1 : 0) + ((ulong)x < 0x000000017FFFFFFFUL ? 2 : 0) + (x == unchecked((long)0x000000017FFFFFFFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000000017FFFFFFFUL)); return r * 31 + (x ^ unchecked((long)0x000000017FFFFFFFUL)); }
    [MethodImpl(NI)] static long K151() => unchecked((long)0x0000000180000000UL);
    [MethodImpl(NI)] static long A151(long x) => x + unchecked((long)0x0000000180000000UL);
    [MethodImpl(NI)] static long C151(long x) { long r = (x < unchecked((long)0x0000000180000000UL) ? 1 : 0) + ((ulong)x < 0x0000000180000000UL ? 2 : 0) + (x == unchecked((long)0x0000000180000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000180000000UL)); return r * 31 + (x ^ unchecked((long)0x0000000180000000UL)); }
    [MethodImpl(NI)] static long K152() => unchecked((long)0x0000000180000001UL);
    [MethodImpl(NI)] static long A152(long x) => x + unchecked((long)0x0000000180000001UL);
    [MethodImpl(NI)] static long C152(long x) { long r = (x < unchecked((long)0x0000000180000001UL) ? 1 : 0) + ((ulong)x < 0x0000000180000001UL ? 2 : 0) + (x == unchecked((long)0x0000000180000001UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000180000001UL)); return r * 31 + (x ^ unchecked((long)0x0000000180000001UL)); }
    [MethodImpl(NI)] static long K153() => unchecked((long)0x00000001800007FFUL);
    [MethodImpl(NI)] static long A153(long x) => x + unchecked((long)0x00000001800007FFUL);
    [MethodImpl(NI)] static long C153(long x) { long r = (x < unchecked((long)0x00000001800007FFUL) ? 1 : 0) + ((ulong)x < 0x00000001800007FFUL ? 2 : 0) + (x == unchecked((long)0x00000001800007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000001800007FFUL)); return r * 31 + (x ^ unchecked((long)0x00000001800007FFUL)); }
    [MethodImpl(NI)] static long K154() => unchecked((long)0x0000000180000800UL);
    [MethodImpl(NI)] static long A154(long x) => x + unchecked((long)0x0000000180000800UL);
    [MethodImpl(NI)] static long C154(long x) { long r = (x < unchecked((long)0x0000000180000800UL) ? 1 : 0) + ((ulong)x < 0x0000000180000800UL ? 2 : 0) + (x == unchecked((long)0x0000000180000800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000180000800UL)); return r * 31 + (x ^ unchecked((long)0x0000000180000800UL)); }
    [MethodImpl(NI)] static long K155() => unchecked((long)0x0000000180001000UL);
    [MethodImpl(NI)] static long A155(long x) => x + unchecked((long)0x0000000180001000UL);
    [MethodImpl(NI)] static long C155(long x) { long r = (x < unchecked((long)0x0000000180001000UL) ? 1 : 0) + ((ulong)x < 0x0000000180001000UL ? 2 : 0) + (x == unchecked((long)0x0000000180001000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000180001000UL)); return r * 31 + (x ^ unchecked((long)0x0000000180001000UL)); }
    [MethodImpl(NI)] static long K156() => unchecked((long)0x00000001800017FFUL);
    [MethodImpl(NI)] static long A156(long x) => x + unchecked((long)0x00000001800017FFUL);
    [MethodImpl(NI)] static long C156(long x) { long r = (x < unchecked((long)0x00000001800017FFUL) ? 1 : 0) + ((ulong)x < 0x00000001800017FFUL ? 2 : 0) + (x == unchecked((long)0x00000001800017FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000001800017FFUL)); return r * 31 + (x ^ unchecked((long)0x00000001800017FFUL)); }
    [MethodImpl(NI)] static long K157() => unchecked((long)0x0000000187654321UL);
    [MethodImpl(NI)] static long A157(long x) => x + unchecked((long)0x0000000187654321UL);
    [MethodImpl(NI)] static long C157(long x) { long r = (x < unchecked((long)0x0000000187654321UL) ? 1 : 0) + ((ulong)x < 0x0000000187654321UL ? 2 : 0) + (x == unchecked((long)0x0000000187654321UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000187654321UL)); return r * 31 + (x ^ unchecked((long)0x0000000187654321UL)); }
    [MethodImpl(NI)] static long K158() => unchecked((long)0x00000001B2C827FFUL);
    [MethodImpl(NI)] static long A158(long x) => x + unchecked((long)0x00000001B2C827FFUL);
    [MethodImpl(NI)] static long C158(long x) { long r = (x < unchecked((long)0x00000001B2C827FFUL) ? 1 : 0) + ((ulong)x < 0x00000001B2C827FFUL ? 2 : 0) + (x == unchecked((long)0x00000001B2C827FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000001B2C827FFUL)); return r * 31 + (x ^ unchecked((long)0x00000001B2C827FFUL)); }
    [MethodImpl(NI)] static long K159() => unchecked((long)0x00000001B2C828CAUL);
    [MethodImpl(NI)] static long A159(long x) => x + unchecked((long)0x00000001B2C828CAUL);
    [MethodImpl(NI)] static long C159(long x) { long r = (x < unchecked((long)0x00000001B2C828CAUL) ? 1 : 0) + ((ulong)x < 0x00000001B2C828CAUL ? 2 : 0) + (x == unchecked((long)0x00000001B2C828CAUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000001B2C828CAUL)); return r * 31 + (x ^ unchecked((long)0x00000001B2C828CAUL)); }
    [MethodImpl(NI)] static long K160() => unchecked((long)0x00000001BFFEFFF8UL);
    [MethodImpl(NI)] static long A160(long x) => x + unchecked((long)0x00000001BFFEFFF8UL);
    [MethodImpl(NI)] static long C160(long x) { long r = (x < unchecked((long)0x00000001BFFEFFF8UL) ? 1 : 0) + ((ulong)x < 0x00000001BFFEFFF8UL ? 2 : 0) + (x == unchecked((long)0x00000001BFFEFFF8UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000001BFFEFFF8UL)); return r * 31 + (x ^ unchecked((long)0x00000001BFFEFFF8UL)); }
    [MethodImpl(NI)] static long K161() => unchecked((long)0x00000001BFFF0000UL);
    [MethodImpl(NI)] static long A161(long x) => x + unchecked((long)0x00000001BFFF0000UL);
    [MethodImpl(NI)] static long C161(long x) { long r = (x < unchecked((long)0x00000001BFFF0000UL) ? 1 : 0) + ((ulong)x < 0x00000001BFFF0000UL ? 2 : 0) + (x == unchecked((long)0x00000001BFFF0000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000001BFFF0000UL)); return r * 31 + (x ^ unchecked((long)0x00000001BFFF0000UL)); }
    [MethodImpl(NI)] static long K162() => unchecked((long)0x00000001C0000800UL);
    [MethodImpl(NI)] static long A162(long x) => x + unchecked((long)0x00000001C0000800UL);
    [MethodImpl(NI)] static long C162(long x) { long r = (x < unchecked((long)0x00000001C0000800UL) ? 1 : 0) + ((ulong)x < 0x00000001C0000800UL ? 2 : 0) + (x == unchecked((long)0x00000001C0000800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000001C0000800UL)); return r * 31 + (x ^ unchecked((long)0x00000001C0000800UL)); }
    [MethodImpl(NI)] static long K163() => unchecked((long)0x00000001FFFFC000UL);
    [MethodImpl(NI)] static long A163(long x) => x + unchecked((long)0x00000001FFFFC000UL);
    [MethodImpl(NI)] static long C163(long x) { long r = (x < unchecked((long)0x00000001FFFFC000UL) ? 1 : 0) + ((ulong)x < 0x00000001FFFFC000UL ? 2 : 0) + (x == unchecked((long)0x00000001FFFFC000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000001FFFFC000UL)); return r * 31 + (x ^ unchecked((long)0x00000001FFFFC000UL)); }
    [MethodImpl(NI)] static long K164() => unchecked((long)0x00000001FFFFD800UL);
    [MethodImpl(NI)] static long A164(long x) => x + unchecked((long)0x00000001FFFFD800UL);
    [MethodImpl(NI)] static long C164(long x) { long r = (x < unchecked((long)0x00000001FFFFD800UL) ? 1 : 0) + ((ulong)x < 0x00000001FFFFD800UL ? 2 : 0) + (x == unchecked((long)0x00000001FFFFD800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000001FFFFD800UL)); return r * 31 + (x ^ unchecked((long)0x00000001FFFFD800UL)); }
    [MethodImpl(NI)] static long K165() => unchecked((long)0x00000001FFFFE000UL);
    [MethodImpl(NI)] static long A165(long x) => x + unchecked((long)0x00000001FFFFE000UL);
    [MethodImpl(NI)] static long C165(long x) { long r = (x < unchecked((long)0x00000001FFFFE000UL) ? 1 : 0) + ((ulong)x < 0x00000001FFFFE000UL ? 2 : 0) + (x == unchecked((long)0x00000001FFFFE000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000001FFFFE000UL)); return r * 31 + (x ^ unchecked((long)0x00000001FFFFE000UL)); }
    [MethodImpl(NI)] static long K166() => unchecked((long)0x00000001FFFFE7FEUL);
    [MethodImpl(NI)] static long A166(long x) => x + unchecked((long)0x00000001FFFFE7FEUL);
    [MethodImpl(NI)] static long C166(long x) { long r = (x < unchecked((long)0x00000001FFFFE7FEUL) ? 1 : 0) + ((ulong)x < 0x00000001FFFFE7FEUL ? 2 : 0) + (x == unchecked((long)0x00000001FFFFE7FEUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000001FFFFE7FEUL)); return r * 31 + (x ^ unchecked((long)0x00000001FFFFE7FEUL)); }
    [MethodImpl(NI)] static long K167() => unchecked((long)0x00000001FFFFE7FFUL);
    [MethodImpl(NI)] static long A167(long x) => x + unchecked((long)0x00000001FFFFE7FFUL);
    [MethodImpl(NI)] static long C167(long x) { long r = (x < unchecked((long)0x00000001FFFFE7FFUL) ? 1 : 0) + ((ulong)x < 0x00000001FFFFE7FFUL ? 2 : 0) + (x == unchecked((long)0x00000001FFFFE7FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000001FFFFE7FFUL)); return r * 31 + (x ^ unchecked((long)0x00000001FFFFE7FFUL)); }
    [MethodImpl(NI)] static long K168() => unchecked((long)0x00000001FFFFE800UL);
    [MethodImpl(NI)] static long A168(long x) => x + unchecked((long)0x00000001FFFFE800UL);
    [MethodImpl(NI)] static long C168(long x) { long r = (x < unchecked((long)0x00000001FFFFE800UL) ? 1 : 0) + ((ulong)x < 0x00000001FFFFE800UL ? 2 : 0) + (x == unchecked((long)0x00000001FFFFE800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000001FFFFE800UL)); return r * 31 + (x ^ unchecked((long)0x00000001FFFFE800UL)); }
    [MethodImpl(NI)] static long K169() => unchecked((long)0x00000001FFFFEFFEUL);
    [MethodImpl(NI)] static long A169(long x) => x + unchecked((long)0x00000001FFFFEFFEUL);
    [MethodImpl(NI)] static long C169(long x) { long r = (x < unchecked((long)0x00000001FFFFEFFEUL) ? 1 : 0) + ((ulong)x < 0x00000001FFFFEFFEUL ? 2 : 0) + (x == unchecked((long)0x00000001FFFFEFFEUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000001FFFFEFFEUL)); return r * 31 + (x ^ unchecked((long)0x00000001FFFFEFFEUL)); }
    [MethodImpl(NI)] static long K170() => unchecked((long)0x00000001FFFFF000UL);
    [MethodImpl(NI)] static long A170(long x) => x + unchecked((long)0x00000001FFFFF000UL);
    [MethodImpl(NI)] static long C170(long x) { long r = (x < unchecked((long)0x00000001FFFFF000UL) ? 1 : 0) + ((ulong)x < 0x00000001FFFFF000UL ? 2 : 0) + (x == unchecked((long)0x00000001FFFFF000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000001FFFFF000UL)); return r * 31 + (x ^ unchecked((long)0x00000001FFFFF000UL)); }
    [MethodImpl(NI)] static long K171() => unchecked((long)0x00000001FFFFF7FDUL);
    [MethodImpl(NI)] static long A171(long x) => x + unchecked((long)0x00000001FFFFF7FDUL);
    [MethodImpl(NI)] static long C171(long x) { long r = (x < unchecked((long)0x00000001FFFFF7FDUL) ? 1 : 0) + ((ulong)x < 0x00000001FFFFF7FDUL ? 2 : 0) + (x == unchecked((long)0x00000001FFFFF7FDUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000001FFFFF7FDUL)); return r * 31 + (x ^ unchecked((long)0x00000001FFFFF7FDUL)); }
    [MethodImpl(NI)] static long K172() => unchecked((long)0x00000001FFFFF7FEUL);
    [MethodImpl(NI)] static long A172(long x) => x + unchecked((long)0x00000001FFFFF7FEUL);
    [MethodImpl(NI)] static long C172(long x) { long r = (x < unchecked((long)0x00000001FFFFF7FEUL) ? 1 : 0) + ((ulong)x < 0x00000001FFFFF7FEUL ? 2 : 0) + (x == unchecked((long)0x00000001FFFFF7FEUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000001FFFFF7FEUL)); return r * 31 + (x ^ unchecked((long)0x00000001FFFFF7FEUL)); }
    [MethodImpl(NI)] static long K173() => unchecked((long)0x00000001FFFFF7FFUL);
    [MethodImpl(NI)] static long A173(long x) => x + unchecked((long)0x00000001FFFFF7FFUL);
    [MethodImpl(NI)] static long C173(long x) { long r = (x < unchecked((long)0x00000001FFFFF7FFUL) ? 1 : 0) + ((ulong)x < 0x00000001FFFFF7FFUL ? 2 : 0) + (x == unchecked((long)0x00000001FFFFF7FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000001FFFFF7FFUL)); return r * 31 + (x ^ unchecked((long)0x00000001FFFFF7FFUL)); }
    [MethodImpl(NI)] static long K174() => unchecked((long)0x00000001FFFFF800UL);
    [MethodImpl(NI)] static long A174(long x) => x + unchecked((long)0x00000001FFFFF800UL);
    [MethodImpl(NI)] static long C174(long x) { long r = (x < unchecked((long)0x00000001FFFFF800UL) ? 1 : 0) + ((ulong)x < 0x00000001FFFFF800UL ? 2 : 0) + (x == unchecked((long)0x00000001FFFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000001FFFFF800UL)); return r * 31 + (x ^ unchecked((long)0x00000001FFFFF800UL)); }
    [MethodImpl(NI)] static long K175() => unchecked((long)0x00000001FFFFFFFEUL);
    [MethodImpl(NI)] static long A175(long x) => x + unchecked((long)0x00000001FFFFFFFEUL);
    [MethodImpl(NI)] static long C175(long x) { long r = (x < unchecked((long)0x00000001FFFFFFFEUL) ? 1 : 0) + ((ulong)x < 0x00000001FFFFFFFEUL ? 2 : 0) + (x == unchecked((long)0x00000001FFFFFFFEUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000001FFFFFFFEUL)); return r * 31 + (x ^ unchecked((long)0x00000001FFFFFFFEUL)); }
    [MethodImpl(NI)] static long K176() => unchecked((long)0x00000001FFFFFFFFUL);
    [MethodImpl(NI)] static long A176(long x) => x + unchecked((long)0x00000001FFFFFFFFUL);
    [MethodImpl(NI)] static long C176(long x) { long r = (x < unchecked((long)0x00000001FFFFFFFFUL) ? 1 : 0) + ((ulong)x < 0x00000001FFFFFFFFUL ? 2 : 0) + (x == unchecked((long)0x00000001FFFFFFFFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000001FFFFFFFFUL)); return r * 31 + (x ^ unchecked((long)0x00000001FFFFFFFFUL)); }
    [MethodImpl(NI)] static long K177() => unchecked((long)0x0000000200000000UL);
    [MethodImpl(NI)] static long A177(long x) => x + unchecked((long)0x0000000200000000UL);
    [MethodImpl(NI)] static long C177(long x) { long r = (x < unchecked((long)0x0000000200000000UL) ? 1 : 0) + ((ulong)x < 0x0000000200000000UL ? 2 : 0) + (x == unchecked((long)0x0000000200000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000200000000UL)); return r * 31 + (x ^ unchecked((long)0x0000000200000000UL)); }
    [MethodImpl(NI)] static long K178() => unchecked((long)0x00000002000007FDUL);
    [MethodImpl(NI)] static long A178(long x) => x + unchecked((long)0x00000002000007FDUL);
    [MethodImpl(NI)] static long C178(long x) { long r = (x < unchecked((long)0x00000002000007FDUL) ? 1 : 0) + ((ulong)x < 0x00000002000007FDUL ? 2 : 0) + (x == unchecked((long)0x00000002000007FDUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000002000007FDUL)); return r * 31 + (x ^ unchecked((long)0x00000002000007FDUL)); }
    [MethodImpl(NI)] static long K179() => unchecked((long)0x00000003FFFF8000UL);
    [MethodImpl(NI)] static long A179(long x) => x + unchecked((long)0x00000003FFFF8000UL);
    [MethodImpl(NI)] static long C179(long x) { long r = (x < unchecked((long)0x00000003FFFF8000UL) ? 1 : 0) + ((ulong)x < 0x00000003FFFF8000UL ? 2 : 0) + (x == unchecked((long)0x00000003FFFF8000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000003FFFF8000UL)); return r * 31 + (x ^ unchecked((long)0x00000003FFFF8000UL)); }
    [MethodImpl(NI)] static long K180() => unchecked((long)0x0000000400000000UL);
    [MethodImpl(NI)] static long A180(long x) => x + unchecked((long)0x0000000400000000UL);
    [MethodImpl(NI)] static long C180(long x) { long r = (x < unchecked((long)0x0000000400000000UL) ? 1 : 0) + ((ulong)x < 0x0000000400000000UL ? 2 : 0) + (x == unchecked((long)0x0000000400000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000400000000UL)); return r * 31 + (x ^ unchecked((long)0x0000000400000000UL)); }
    [MethodImpl(NI)] static long K181() => unchecked((long)0x00000007FFFF0000UL);
    [MethodImpl(NI)] static long A181(long x) => x + unchecked((long)0x00000007FFFF0000UL);
    [MethodImpl(NI)] static long C181(long x) { long r = (x < unchecked((long)0x00000007FFFF0000UL) ? 1 : 0) + ((ulong)x < 0x00000007FFFF0000UL ? 2 : 0) + (x == unchecked((long)0x00000007FFFF0000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000007FFFF0000UL)); return r * 31 + (x ^ unchecked((long)0x00000007FFFF0000UL)); }
    [MethodImpl(NI)] static long K182() => unchecked((long)0x00000007FFFFF7FFUL);
    [MethodImpl(NI)] static long A182(long x) => x + unchecked((long)0x00000007FFFFF7FFUL);
    [MethodImpl(NI)] static long C182(long x) { long r = (x < unchecked((long)0x00000007FFFFF7FFUL) ? 1 : 0) + ((ulong)x < 0x00000007FFFFF7FFUL ? 2 : 0) + (x == unchecked((long)0x00000007FFFFF7FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000007FFFFF7FFUL)); return r * 31 + (x ^ unchecked((long)0x00000007FFFFF7FFUL)); }
    [MethodImpl(NI)] static long K183() => unchecked((long)0x00000007FFFFF800UL);
    [MethodImpl(NI)] static long A183(long x) => x + unchecked((long)0x00000007FFFFF800UL);
    [MethodImpl(NI)] static long C183(long x) { long r = (x < unchecked((long)0x00000007FFFFF800UL) ? 1 : 0) + ((ulong)x < 0x00000007FFFFF800UL ? 2 : 0) + (x == unchecked((long)0x00000007FFFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000007FFFFF800UL)); return r * 31 + (x ^ unchecked((long)0x00000007FFFFF800UL)); }
    [MethodImpl(NI)] static long K184() => unchecked((long)0x00000007FFFFF801UL);
    [MethodImpl(NI)] static long A184(long x) => x + unchecked((long)0x00000007FFFFF801UL);
    [MethodImpl(NI)] static long C184(long x) { long r = (x < unchecked((long)0x00000007FFFFF801UL) ? 1 : 0) + ((ulong)x < 0x00000007FFFFF801UL ? 2 : 0) + (x == unchecked((long)0x00000007FFFFF801UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000007FFFFF801UL)); return r * 31 + (x ^ unchecked((long)0x00000007FFFFF801UL)); }
    [MethodImpl(NI)] static long K185() => unchecked((long)0x00000007FFFFFFFEUL);
    [MethodImpl(NI)] static long A185(long x) => x + unchecked((long)0x00000007FFFFFFFEUL);
    [MethodImpl(NI)] static long C185(long x) { long r = (x < unchecked((long)0x00000007FFFFFFFEUL) ? 1 : 0) + ((ulong)x < 0x00000007FFFFFFFEUL ? 2 : 0) + (x == unchecked((long)0x00000007FFFFFFFEUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000007FFFFFFFEUL)); return r * 31 + (x ^ unchecked((long)0x00000007FFFFFFFEUL)); }
    [MethodImpl(NI)] static long K186() => unchecked((long)0x00000007FFFFFFFFUL);
    [MethodImpl(NI)] static long A186(long x) => x + unchecked((long)0x00000007FFFFFFFFUL);
    [MethodImpl(NI)] static long C186(long x) { long r = (x < unchecked((long)0x00000007FFFFFFFFUL) ? 1 : 0) + ((ulong)x < 0x00000007FFFFFFFFUL ? 2 : 0) + (x == unchecked((long)0x00000007FFFFFFFFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000007FFFFFFFFUL)); return r * 31 + (x ^ unchecked((long)0x00000007FFFFFFFFUL)); }
    [MethodImpl(NI)] static long K187() => unchecked((long)0x0000000800000000UL);
    [MethodImpl(NI)] static long A187(long x) => x + unchecked((long)0x0000000800000000UL);
    [MethodImpl(NI)] static long C187(long x) { long r = (x < unchecked((long)0x0000000800000000UL) ? 1 : 0) + ((ulong)x < 0x0000000800000000UL ? 2 : 0) + (x == unchecked((long)0x0000000800000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000000800000000UL)); return r * 31 + (x ^ unchecked((long)0x0000000800000000UL)); }
    [MethodImpl(NI)] static long K188() => unchecked((long)0x0000001234500000UL);
    [MethodImpl(NI)] static long A188(long x) => x + unchecked((long)0x0000001234500000UL);
    [MethodImpl(NI)] static long C188(long x) { long r = (x < unchecked((long)0x0000001234500000UL) ? 1 : 0) + ((ulong)x < 0x0000001234500000UL ? 2 : 0) + (x == unchecked((long)0x0000001234500000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000001234500000UL)); return r * 31 + (x ^ unchecked((long)0x0000001234500000UL)); }
    [MethodImpl(NI)] static long K189() => unchecked((long)0x0000004000000000UL);
    [MethodImpl(NI)] static long A189(long x) => x + unchecked((long)0x0000004000000000UL);
    [MethodImpl(NI)] static long C189(long x) { long r = (x < unchecked((long)0x0000004000000000UL) ? 1 : 0) + ((ulong)x < 0x0000004000000000UL ? 2 : 0) + (x == unchecked((long)0x0000004000000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000004000000000UL)); return r * 31 + (x ^ unchecked((long)0x0000004000000000UL)); }
    [MethodImpl(NI)] static long K190() => unchecked((long)0x0000007FFFF00000UL);
    [MethodImpl(NI)] static long A190(long x) => x + unchecked((long)0x0000007FFFF00000UL);
    [MethodImpl(NI)] static long C190(long x) { long r = (x < unchecked((long)0x0000007FFFF00000UL) ? 1 : 0) + ((ulong)x < 0x0000007FFFF00000UL ? 2 : 0) + (x == unchecked((long)0x0000007FFFF00000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000007FFFF00000UL)); return r * 31 + (x ^ unchecked((long)0x0000007FFFF00000UL)); }
    [MethodImpl(NI)] static long K191() => unchecked((long)0x000001020EB3F40CUL);
    [MethodImpl(NI)] static long A191(long x) => x + unchecked((long)0x000001020EB3F40CUL);
    [MethodImpl(NI)] static long C191(long x) { long r = (x < unchecked((long)0x000001020EB3F40CUL) ? 1 : 0) + ((ulong)x < 0x000001020EB3F40CUL ? 2 : 0) + (x == unchecked((long)0x000001020EB3F40CUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000001020EB3F40CUL)); return r * 31 + (x ^ unchecked((long)0x000001020EB3F40CUL)); }
    [MethodImpl(NI)] static long K192() => unchecked((long)0x000001020EB3F800UL);
    [MethodImpl(NI)] static long A192(long x) => x + unchecked((long)0x000001020EB3F800UL);
    [MethodImpl(NI)] static long C192(long x) { long r = (x < unchecked((long)0x000001020EB3F800UL) ? 1 : 0) + ((ulong)x < 0x000001020EB3F800UL ? 2 : 0) + (x == unchecked((long)0x000001020EB3F800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000001020EB3F800UL)); return r * 31 + (x ^ unchecked((long)0x000001020EB3F800UL)); }
    [MethodImpl(NI)] static long K193() => unchecked((long)0x0000012345000000UL);
    [MethodImpl(NI)] static long A193(long x) => x + unchecked((long)0x0000012345000000UL);
    [MethodImpl(NI)] static long C193(long x) { long r = (x < unchecked((long)0x0000012345000000UL) ? 1 : 0) + ((ulong)x < 0x0000012345000000UL ? 2 : 0) + (x == unchecked((long)0x0000012345000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000012345000000UL)); return r * 31 + (x ^ unchecked((long)0x0000012345000000UL)); }
    [MethodImpl(NI)] static long K194() => unchecked((long)0x0000012345677800UL);
    [MethodImpl(NI)] static long A194(long x) => x + unchecked((long)0x0000012345677800UL);
    [MethodImpl(NI)] static long C194(long x) { long r = (x < unchecked((long)0x0000012345677800UL) ? 1 : 0) + ((ulong)x < 0x0000012345677800UL ? 2 : 0) + (x == unchecked((long)0x0000012345677800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000012345677800UL)); return r * 31 + (x ^ unchecked((long)0x0000012345677800UL)); }
    [MethodImpl(NI)] static long K195() => unchecked((long)0x0000012345678000UL);
    [MethodImpl(NI)] static long A195(long x) => x + unchecked((long)0x0000012345678000UL);
    [MethodImpl(NI)] static long C195(long x) { long r = (x < unchecked((long)0x0000012345678000UL) ? 1 : 0) + ((ulong)x < 0x0000012345678000UL ? 2 : 0) + (x == unchecked((long)0x0000012345678000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000012345678000UL)); return r * 31 + (x ^ unchecked((long)0x0000012345678000UL)); }
    [MethodImpl(NI)] static long K196() => unchecked((long)0x00000123456787FFUL);
    [MethodImpl(NI)] static long A196(long x) => x + unchecked((long)0x00000123456787FFUL);
    [MethodImpl(NI)] static long C196(long x) { long r = (x < unchecked((long)0x00000123456787FFUL) ? 1 : 0) + ((ulong)x < 0x00000123456787FFUL ? 2 : 0) + (x == unchecked((long)0x00000123456787FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000123456787FFUL)); return r * 31 + (x ^ unchecked((long)0x00000123456787FFUL)); }
    [MethodImpl(NI)] static long K197() => unchecked((long)0x000002FFFBFFF800UL);
    [MethodImpl(NI)] static long A197(long x) => x + unchecked((long)0x000002FFFBFFF800UL);
    [MethodImpl(NI)] static long C197(long x) { long r = (x < unchecked((long)0x000002FFFBFFF800UL) ? 1 : 0) + ((ulong)x < 0x000002FFFBFFF800UL ? 2 : 0) + (x == unchecked((long)0x000002FFFBFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000002FFFBFFF800UL)); return r * 31 + (x ^ unchecked((long)0x000002FFFBFFF800UL)); }
    [MethodImpl(NI)] static long K198() => unchecked((long)0x000002FFFC000000UL);
    [MethodImpl(NI)] static long A198(long x) => x + unchecked((long)0x000002FFFC000000UL);
    [MethodImpl(NI)] static long C198(long x) { long r = (x < unchecked((long)0x000002FFFC000000UL) ? 1 : 0) + ((ulong)x < 0x000002FFFC000000UL ? 2 : 0) + (x == unchecked((long)0x000002FFFC000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000002FFFC000000UL)); return r * 31 + (x ^ unchecked((long)0x000002FFFC000000UL)); }
    [MethodImpl(NI)] static long K199() => unchecked((long)0x000002FFFC0007FFUL);
    [MethodImpl(NI)] static long A199(long x) => x + unchecked((long)0x000002FFFC0007FFUL);
    [MethodImpl(NI)] static long C199(long x) { long r = (x < unchecked((long)0x000002FFFC0007FFUL) ? 1 : 0) + ((ulong)x < 0x000002FFFC0007FFUL ? 2 : 0) + (x == unchecked((long)0x000002FFFC0007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000002FFFC0007FFUL)); return r * 31 + (x ^ unchecked((long)0x000002FFFC0007FFUL)); }
    [MethodImpl(NI)] static long K200() => unchecked((long)0x000003FFFF7FF800UL);
    [MethodImpl(NI)] static long A200(long x) => x + unchecked((long)0x000003FFFF7FF800UL);
    [MethodImpl(NI)] static long C200(long x) { long r = (x < unchecked((long)0x000003FFFF7FF800UL) ? 1 : 0) + ((ulong)x < 0x000003FFFF7FF800UL ? 2 : 0) + (x == unchecked((long)0x000003FFFF7FF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000003FFFF7FF800UL)); return r * 31 + (x ^ unchecked((long)0x000003FFFF7FF800UL)); }
    [MethodImpl(NI)] static long K201() => unchecked((long)0x000003FFFF800000UL);
    [MethodImpl(NI)] static long A201(long x) => x + unchecked((long)0x000003FFFF800000UL);
    [MethodImpl(NI)] static long C201(long x) { long r = (x < unchecked((long)0x000003FFFF800000UL) ? 1 : 0) + ((ulong)x < 0x000003FFFF800000UL ? 2 : 0) + (x == unchecked((long)0x000003FFFF800000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000003FFFF800000UL)); return r * 31 + (x ^ unchecked((long)0x000003FFFF800000UL)); }
    [MethodImpl(NI)] static long K202() => unchecked((long)0x000003FFFF8007FFUL);
    [MethodImpl(NI)] static long A202(long x) => x + unchecked((long)0x000003FFFF8007FFUL);
    [MethodImpl(NI)] static long C202(long x) { long r = (x < unchecked((long)0x000003FFFF8007FFUL) ? 1 : 0) + ((ulong)x < 0x000003FFFF8007FFUL ? 2 : 0) + (x == unchecked((long)0x000003FFFF8007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000003FFFF8007FFUL)); return r * 31 + (x ^ unchecked((long)0x000003FFFF8007FFUL)); }
    [MethodImpl(NI)] static long K203() => unchecked((long)0x0000040000000000UL);
    [MethodImpl(NI)] static long A203(long x) => x + unchecked((long)0x0000040000000000UL);
    [MethodImpl(NI)] static long C203(long x) { long r = (x < unchecked((long)0x0000040000000000UL) ? 1 : 0) + ((ulong)x < 0x0000040000000000UL ? 2 : 0) + (x == unchecked((long)0x0000040000000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000040000000000UL)); return r * 31 + (x ^ unchecked((long)0x0000040000000000UL)); }
    [MethodImpl(NI)] static long K204() => unchecked((long)0x000005311D5B0502UL);
    [MethodImpl(NI)] static long A204(long x) => x + unchecked((long)0x000005311D5B0502UL);
    [MethodImpl(NI)] static long C204(long x) { long r = (x < unchecked((long)0x000005311D5B0502UL) ? 1 : 0) + ((ulong)x < 0x000005311D5B0502UL ? 2 : 0) + (x == unchecked((long)0x000005311D5B0502UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000005311D5B0502UL)); return r * 31 + (x ^ unchecked((long)0x000005311D5B0502UL)); }
    [MethodImpl(NI)] static long K205() => unchecked((long)0x000005311D5B07FFUL);
    [MethodImpl(NI)] static long A205(long x) => x + unchecked((long)0x000005311D5B07FFUL);
    [MethodImpl(NI)] static long C205(long x) { long r = (x < unchecked((long)0x000005311D5B07FFUL) ? 1 : 0) + ((ulong)x < 0x000005311D5B07FFUL ? 2 : 0) + (x == unchecked((long)0x000005311D5B07FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000005311D5B07FFUL)); return r * 31 + (x ^ unchecked((long)0x000005311D5B07FFUL)); }
    [MethodImpl(NI)] static long K206() => unchecked((long)0x000007FEFFFFF800UL);
    [MethodImpl(NI)] static long A206(long x) => x + unchecked((long)0x000007FEFFFFF800UL);
    [MethodImpl(NI)] static long C206(long x) { long r = (x < unchecked((long)0x000007FEFFFFF800UL) ? 1 : 0) + ((ulong)x < 0x000007FEFFFFF800UL ? 2 : 0) + (x == unchecked((long)0x000007FEFFFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000007FEFFFFF800UL)); return r * 31 + (x ^ unchecked((long)0x000007FEFFFFF800UL)); }
    [MethodImpl(NI)] static long K207() => unchecked((long)0x000007FF00000000UL);
    [MethodImpl(NI)] static long A207(long x) => x + unchecked((long)0x000007FF00000000UL);
    [MethodImpl(NI)] static long C207(long x) { long r = (x < unchecked((long)0x000007FF00000000UL) ? 1 : 0) + ((ulong)x < 0x000007FF00000000UL ? 2 : 0) + (x == unchecked((long)0x000007FF00000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000007FF00000000UL)); return r * 31 + (x ^ unchecked((long)0x000007FF00000000UL)); }
    [MethodImpl(NI)] static long K208() => unchecked((long)0x000007FF000007FFUL);
    [MethodImpl(NI)] static long A208(long x) => x + unchecked((long)0x000007FF000007FFUL);
    [MethodImpl(NI)] static long C208(long x) { long r = (x < unchecked((long)0x000007FF000007FFUL) ? 1 : 0) + ((ulong)x < 0x000007FF000007FFUL ? 2 : 0) + (x == unchecked((long)0x000007FF000007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000007FF000007FFUL)); return r * 31 + (x ^ unchecked((long)0x000007FF000007FFUL)); }
    [MethodImpl(NI)] static long K209() => unchecked((long)0x000007FF007FE800UL);
    [MethodImpl(NI)] static long A209(long x) => x + unchecked((long)0x000007FF007FE800UL);
    [MethodImpl(NI)] static long C209(long x) { long r = (x < unchecked((long)0x000007FF007FE800UL) ? 1 : 0) + ((ulong)x < 0x000007FF007FE800UL ? 2 : 0) + (x == unchecked((long)0x000007FF007FE800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000007FF007FE800UL)); return r * 31 + (x ^ unchecked((long)0x000007FF007FE800UL)); }
    [MethodImpl(NI)] static long K210() => unchecked((long)0x000007FF007FF000UL);
    [MethodImpl(NI)] static long A210(long x) => x + unchecked((long)0x000007FF007FF000UL);
    [MethodImpl(NI)] static long C210(long x) { long r = (x < unchecked((long)0x000007FF007FF000UL) ? 1 : 0) + ((ulong)x < 0x000007FF007FF000UL ? 2 : 0) + (x == unchecked((long)0x000007FF007FF000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000007FF007FF000UL)); return r * 31 + (x ^ unchecked((long)0x000007FF007FF000UL)); }
    [MethodImpl(NI)] static long K211() => unchecked((long)0x000007FF007FF7FFUL);
    [MethodImpl(NI)] static long A211(long x) => x + unchecked((long)0x000007FF007FF7FFUL);
    [MethodImpl(NI)] static long C211(long x) { long r = (x < unchecked((long)0x000007FF007FF7FFUL) ? 1 : 0) + ((ulong)x < 0x000007FF007FF7FFUL ? 2 : 0) + (x == unchecked((long)0x000007FF007FF7FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000007FF007FF7FFUL)); return r * 31 + (x ^ unchecked((long)0x000007FF007FF7FFUL)); }
    [MethodImpl(NI)] static long K212() => unchecked((long)0x000007FFFEFFF800UL);
    [MethodImpl(NI)] static long A212(long x) => x + unchecked((long)0x000007FFFEFFF800UL);
    [MethodImpl(NI)] static long C212(long x) { long r = (x < unchecked((long)0x000007FFFEFFF800UL) ? 1 : 0) + ((ulong)x < 0x000007FFFEFFF800UL ? 2 : 0) + (x == unchecked((long)0x000007FFFEFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000007FFFEFFF800UL)); return r * 31 + (x ^ unchecked((long)0x000007FFFEFFF800UL)); }
    [MethodImpl(NI)] static long K213() => unchecked((long)0x000007FFFF000000UL);
    [MethodImpl(NI)] static long A213(long x) => x + unchecked((long)0x000007FFFF000000UL);
    [MethodImpl(NI)] static long C213(long x) { long r = (x < unchecked((long)0x000007FFFF000000UL) ? 1 : 0) + ((ulong)x < 0x000007FFFF000000UL ? 2 : 0) + (x == unchecked((long)0x000007FFFF000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000007FFFF000000UL)); return r * 31 + (x ^ unchecked((long)0x000007FFFF000000UL)); }
    [MethodImpl(NI)] static long K214() => unchecked((long)0x000007FFFF0007FFUL);
    [MethodImpl(NI)] static long A214(long x) => x + unchecked((long)0x000007FFFF0007FFUL);
    [MethodImpl(NI)] static long C214(long x) { long r = (x < unchecked((long)0x000007FFFF0007FFUL) ? 1 : 0) + ((ulong)x < 0x000007FFFF0007FFUL ? 2 : 0) + (x == unchecked((long)0x000007FFFF0007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000007FFFF0007FFUL)); return r * 31 + (x ^ unchecked((long)0x000007FFFF0007FFUL)); }
    [MethodImpl(NI)] static long K215() => unchecked((long)0x000007FFFF7FE800UL);
    [MethodImpl(NI)] static long A215(long x) => x + unchecked((long)0x000007FFFF7FE800UL);
    [MethodImpl(NI)] static long C215(long x) { long r = (x < unchecked((long)0x000007FFFF7FE800UL) ? 1 : 0) + ((ulong)x < 0x000007FFFF7FE800UL ? 2 : 0) + (x == unchecked((long)0x000007FFFF7FE800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000007FFFF7FE800UL)); return r * 31 + (x ^ unchecked((long)0x000007FFFF7FE800UL)); }
    [MethodImpl(NI)] static long K216() => unchecked((long)0x000007FFFF7FF000UL);
    [MethodImpl(NI)] static long A216(long x) => x + unchecked((long)0x000007FFFF7FF000UL);
    [MethodImpl(NI)] static long C216(long x) { long r = (x < unchecked((long)0x000007FFFF7FF000UL) ? 1 : 0) + ((ulong)x < 0x000007FFFF7FF000UL ? 2 : 0) + (x == unchecked((long)0x000007FFFF7FF000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000007FFFF7FF000UL)); return r * 31 + (x ^ unchecked((long)0x000007FFFF7FF000UL)); }
    [MethodImpl(NI)] static long K217() => unchecked((long)0x000007FFFF7FF7FFUL);
    [MethodImpl(NI)] static long A217(long x) => x + unchecked((long)0x000007FFFF7FF7FFUL);
    [MethodImpl(NI)] static long C217(long x) { long r = (x < unchecked((long)0x000007FFFF7FF7FFUL) ? 1 : 0) + ((ulong)x < 0x000007FFFF7FF7FFUL ? 2 : 0) + (x == unchecked((long)0x000007FFFF7FF7FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000007FFFF7FF7FFUL)); return r * 31 + (x ^ unchecked((long)0x000007FFFF7FF7FFUL)); }
    [MethodImpl(NI)] static long K218() => unchecked((long)0x000007FFFF7FF800UL);
    [MethodImpl(NI)] static long A218(long x) => x + unchecked((long)0x000007FFFF7FF800UL);
    [MethodImpl(NI)] static long C218(long x) { long r = (x < unchecked((long)0x000007FFFF7FF800UL) ? 1 : 0) + ((ulong)x < 0x000007FFFF7FF800UL ? 2 : 0) + (x == unchecked((long)0x000007FFFF7FF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000007FFFF7FF800UL)); return r * 31 + (x ^ unchecked((long)0x000007FFFF7FF800UL)); }
    [MethodImpl(NI)] static long K219() => unchecked((long)0x000007FFFF800000UL);
    [MethodImpl(NI)] static long A219(long x) => x + unchecked((long)0x000007FFFF800000UL);
    [MethodImpl(NI)] static long C219(long x) { long r = (x < unchecked((long)0x000007FFFF800000UL) ? 1 : 0) + ((ulong)x < 0x000007FFFF800000UL ? 2 : 0) + (x == unchecked((long)0x000007FFFF800000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000007FFFF800000UL)); return r * 31 + (x ^ unchecked((long)0x000007FFFF800000UL)); }
    [MethodImpl(NI)] static long K220() => unchecked((long)0x000007FFFF8007FFUL);
    [MethodImpl(NI)] static long A220(long x) => x + unchecked((long)0x000007FFFF8007FFUL);
    [MethodImpl(NI)] static long C220(long x) { long r = (x < unchecked((long)0x000007FFFF8007FFUL) ? 1 : 0) + ((ulong)x < 0x000007FFFF8007FFUL ? 2 : 0) + (x == unchecked((long)0x000007FFFF8007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000007FFFF8007FFUL)); return r * 31 + (x ^ unchecked((long)0x000007FFFF8007FFUL)); }
    [MethodImpl(NI)] static long K221() => unchecked((long)0x000007FFFF800800UL);
    [MethodImpl(NI)] static long A221(long x) => x + unchecked((long)0x000007FFFF800800UL);
    [MethodImpl(NI)] static long C221(long x) { long r = (x < unchecked((long)0x000007FFFF800800UL) ? 1 : 0) + ((ulong)x < 0x000007FFFF800800UL ? 2 : 0) + (x == unchecked((long)0x000007FFFF800800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000007FFFF800800UL)); return r * 31 + (x ^ unchecked((long)0x000007FFFF800800UL)); }
    [MethodImpl(NI)] static long K222() => unchecked((long)0x000007FFFF801000UL);
    [MethodImpl(NI)] static long A222(long x) => x + unchecked((long)0x000007FFFF801000UL);
    [MethodImpl(NI)] static long C222(long x) { long r = (x < unchecked((long)0x000007FFFF801000UL) ? 1 : 0) + ((ulong)x < 0x000007FFFF801000UL ? 2 : 0) + (x == unchecked((long)0x000007FFFF801000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000007FFFF801000UL)); return r * 31 + (x ^ unchecked((long)0x000007FFFF801000UL)); }
    [MethodImpl(NI)] static long K223() => unchecked((long)0x000007FFFF8017FFUL);
    [MethodImpl(NI)] static long A223(long x) => x + unchecked((long)0x000007FFFF8017FFUL);
    [MethodImpl(NI)] static long C223(long x) { long r = (x < unchecked((long)0x000007FFFF8017FFUL) ? 1 : 0) + ((ulong)x < 0x000007FFFF8017FFUL ? 2 : 0) + (x == unchecked((long)0x000007FFFF8017FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000007FFFF8017FFUL)); return r * 31 + (x ^ unchecked((long)0x000007FFFF8017FFUL)); }
    [MethodImpl(NI)] static long K224() => unchecked((long)0x000007FFFFFFE800UL);
    [MethodImpl(NI)] static long A224(long x) => x + unchecked((long)0x000007FFFFFFE800UL);
    [MethodImpl(NI)] static long C224(long x) { long r = (x < unchecked((long)0x000007FFFFFFE800UL) ? 1 : 0) + ((ulong)x < 0x000007FFFFFFE800UL ? 2 : 0) + (x == unchecked((long)0x000007FFFFFFE800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000007FFFFFFE800UL)); return r * 31 + (x ^ unchecked((long)0x000007FFFFFFE800UL)); }
    [MethodImpl(NI)] static long K225() => unchecked((long)0x000007FFFFFFF000UL);
    [MethodImpl(NI)] static long A225(long x) => x + unchecked((long)0x000007FFFFFFF000UL);
    [MethodImpl(NI)] static long C225(long x) { long r = (x < unchecked((long)0x000007FFFFFFF000UL) ? 1 : 0) + ((ulong)x < 0x000007FFFFFFF000UL ? 2 : 0) + (x == unchecked((long)0x000007FFFFFFF000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000007FFFFFFF000UL)); return r * 31 + (x ^ unchecked((long)0x000007FFFFFFF000UL)); }
    [MethodImpl(NI)] static long K226() => unchecked((long)0x000007FFFFFFF7FFUL);
    [MethodImpl(NI)] static long A226(long x) => x + unchecked((long)0x000007FFFFFFF7FFUL);
    [MethodImpl(NI)] static long C226(long x) { long r = (x < unchecked((long)0x000007FFFFFFF7FFUL) ? 1 : 0) + ((ulong)x < 0x000007FFFFFFF7FFUL ? 2 : 0) + (x == unchecked((long)0x000007FFFFFFF7FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000007FFFFFFF7FFUL)); return r * 31 + (x ^ unchecked((long)0x000007FFFFFFF7FFUL)); }
    [MethodImpl(NI)] static long K227() => unchecked((long)0x000007FFFFFFF800UL);
    [MethodImpl(NI)] static long A227(long x) => x + unchecked((long)0x000007FFFFFFF800UL);
    [MethodImpl(NI)] static long C227(long x) { long r = (x < unchecked((long)0x000007FFFFFFF800UL) ? 1 : 0) + ((ulong)x < 0x000007FFFFFFF800UL ? 2 : 0) + (x == unchecked((long)0x000007FFFFFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000007FFFFFFF800UL)); return r * 31 + (x ^ unchecked((long)0x000007FFFFFFF800UL)); }
    [MethodImpl(NI)] static long K228() => unchecked((long)0x0000080000000000UL);
    [MethodImpl(NI)] static long A228(long x) => x + unchecked((long)0x0000080000000000UL);
    [MethodImpl(NI)] static long C228(long x) { long r = (x < unchecked((long)0x0000080000000000UL) ? 1 : 0) + ((ulong)x < 0x0000080000000000UL ? 2 : 0) + (x == unchecked((long)0x0000080000000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000080000000000UL)); return r * 31 + (x ^ unchecked((long)0x0000080000000000UL)); }
    [MethodImpl(NI)] static long K229() => unchecked((long)0x00000800000007FFUL);
    [MethodImpl(NI)] static long A229(long x) => x + unchecked((long)0x00000800000007FFUL);
    [MethodImpl(NI)] static long C229(long x) { long r = (x < unchecked((long)0x00000800000007FFUL) ? 1 : 0) + ((ulong)x < 0x00000800000007FFUL ? 2 : 0) + (x == unchecked((long)0x00000800000007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000800000007FFUL)); return r * 31 + (x ^ unchecked((long)0x00000800000007FFUL)); }
    [MethodImpl(NI)] static long K230() => unchecked((long)0x0000080000000800UL);
    [MethodImpl(NI)] static long A230(long x) => x + unchecked((long)0x0000080000000800UL);
    [MethodImpl(NI)] static long C230(long x) { long r = (x < unchecked((long)0x0000080000000800UL) ? 1 : 0) + ((ulong)x < 0x0000080000000800UL ? 2 : 0) + (x == unchecked((long)0x0000080000000800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000080000000800UL)); return r * 31 + (x ^ unchecked((long)0x0000080000000800UL)); }
    [MethodImpl(NI)] static long K231() => unchecked((long)0x0000080000001000UL);
    [MethodImpl(NI)] static long A231(long x) => x + unchecked((long)0x0000080000001000UL);
    [MethodImpl(NI)] static long C231(long x) { long r = (x < unchecked((long)0x0000080000001000UL) ? 1 : 0) + ((ulong)x < 0x0000080000001000UL ? 2 : 0) + (x == unchecked((long)0x0000080000001000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000080000001000UL)); return r * 31 + (x ^ unchecked((long)0x0000080000001000UL)); }
    [MethodImpl(NI)] static long K232() => unchecked((long)0x00000800000017FFUL);
    [MethodImpl(NI)] static long A232(long x) => x + unchecked((long)0x00000800000017FFUL);
    [MethodImpl(NI)] static long C232(long x) { long r = (x < unchecked((long)0x00000800000017FFUL) ? 1 : 0) + ((ulong)x < 0x00000800000017FFUL ? 2 : 0) + (x == unchecked((long)0x00000800000017FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000800000017FFUL)); return r * 31 + (x ^ unchecked((long)0x00000800000017FFUL)); }
    [MethodImpl(NI)] static long K233() => unchecked((long)0x00000800007FE800UL);
    [MethodImpl(NI)] static long A233(long x) => x + unchecked((long)0x00000800007FE800UL);
    [MethodImpl(NI)] static long C233(long x) { long r = (x < unchecked((long)0x00000800007FE800UL) ? 1 : 0) + ((ulong)x < 0x00000800007FE800UL ? 2 : 0) + (x == unchecked((long)0x00000800007FE800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000800007FE800UL)); return r * 31 + (x ^ unchecked((long)0x00000800007FE800UL)); }
    [MethodImpl(NI)] static long K234() => unchecked((long)0x00000800007FF000UL);
    [MethodImpl(NI)] static long A234(long x) => x + unchecked((long)0x00000800007FF000UL);
    [MethodImpl(NI)] static long C234(long x) { long r = (x < unchecked((long)0x00000800007FF000UL) ? 1 : 0) + ((ulong)x < 0x00000800007FF000UL ? 2 : 0) + (x == unchecked((long)0x00000800007FF000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000800007FF000UL)); return r * 31 + (x ^ unchecked((long)0x00000800007FF000UL)); }
    [MethodImpl(NI)] static long K235() => unchecked((long)0x00000800007FF7FFUL);
    [MethodImpl(NI)] static long A235(long x) => x + unchecked((long)0x00000800007FF7FFUL);
    [MethodImpl(NI)] static long C235(long x) { long r = (x < unchecked((long)0x00000800007FF7FFUL) ? 1 : 0) + ((ulong)x < 0x00000800007FF7FFUL ? 2 : 0) + (x == unchecked((long)0x00000800007FF7FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000800007FF7FFUL)); return r * 31 + (x ^ unchecked((long)0x00000800007FF7FFUL)); }
    [MethodImpl(NI)] static long K236() => unchecked((long)0x00000800007FF800UL);
    [MethodImpl(NI)] static long A236(long x) => x + unchecked((long)0x00000800007FF800UL);
    [MethodImpl(NI)] static long C236(long x) { long r = (x < unchecked((long)0x00000800007FF800UL) ? 1 : 0) + ((ulong)x < 0x00000800007FF800UL ? 2 : 0) + (x == unchecked((long)0x00000800007FF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000800007FF800UL)); return r * 31 + (x ^ unchecked((long)0x00000800007FF800UL)); }
    [MethodImpl(NI)] static long K237() => unchecked((long)0x0000080000800000UL);
    [MethodImpl(NI)] static long A237(long x) => x + unchecked((long)0x0000080000800000UL);
    [MethodImpl(NI)] static long C237(long x) { long r = (x < unchecked((long)0x0000080000800000UL) ? 1 : 0) + ((ulong)x < 0x0000080000800000UL ? 2 : 0) + (x == unchecked((long)0x0000080000800000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000080000800000UL)); return r * 31 + (x ^ unchecked((long)0x0000080000800000UL)); }
    [MethodImpl(NI)] static long K238() => unchecked((long)0x00000800008007FFUL);
    [MethodImpl(NI)] static long A238(long x) => x + unchecked((long)0x00000800008007FFUL);
    [MethodImpl(NI)] static long C238(long x) { long r = (x < unchecked((long)0x00000800008007FFUL) ? 1 : 0) + ((ulong)x < 0x00000800008007FFUL ? 2 : 0) + (x == unchecked((long)0x00000800008007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000800008007FFUL)); return r * 31 + (x ^ unchecked((long)0x00000800008007FFUL)); }
    [MethodImpl(NI)] static long K239() => unchecked((long)0x00000800FFFFF800UL);
    [MethodImpl(NI)] static long A239(long x) => x + unchecked((long)0x00000800FFFFF800UL);
    [MethodImpl(NI)] static long C239(long x) { long r = (x < unchecked((long)0x00000800FFFFF800UL) ? 1 : 0) + ((ulong)x < 0x00000800FFFFF800UL ? 2 : 0) + (x == unchecked((long)0x00000800FFFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000800FFFFF800UL)); return r * 31 + (x ^ unchecked((long)0x00000800FFFFF800UL)); }
    [MethodImpl(NI)] static long K240() => unchecked((long)0x0000080100000000UL);
    [MethodImpl(NI)] static long A240(long x) => x + unchecked((long)0x0000080100000000UL);
    [MethodImpl(NI)] static long C240(long x) { long r = (x < unchecked((long)0x0000080100000000UL) ? 1 : 0) + ((ulong)x < 0x0000080100000000UL ? 2 : 0) + (x == unchecked((long)0x0000080100000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000080100000000UL)); return r * 31 + (x ^ unchecked((long)0x0000080100000000UL)); }
    [MethodImpl(NI)] static long K241() => unchecked((long)0x00000801000007FFUL);
    [MethodImpl(NI)] static long A241(long x) => x + unchecked((long)0x00000801000007FFUL);
    [MethodImpl(NI)] static long C241(long x) { long r = (x < unchecked((long)0x00000801000007FFUL) ? 1 : 0) + ((ulong)x < 0x00000801000007FFUL ? 2 : 0) + (x == unchecked((long)0x00000801000007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000801000007FFUL)); return r * 31 + (x ^ unchecked((long)0x00000801000007FFUL)); }
    [MethodImpl(NI)] static long K242() => unchecked((long)0x0000080100000801UL);
    [MethodImpl(NI)] static long A242(long x) => x + unchecked((long)0x0000080100000801UL);
    [MethodImpl(NI)] static long C242(long x) { long r = (x < unchecked((long)0x0000080100000801UL) ? 1 : 0) + ((ulong)x < 0x0000080100000801UL ? 2 : 0) + (x == unchecked((long)0x0000080100000801UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000080100000801UL)); return r * 31 + (x ^ unchecked((long)0x0000080100000801UL)); }
    [MethodImpl(NI)] static long K243() => unchecked((long)0x0000087654320800UL);
    [MethodImpl(NI)] static long A243(long x) => x + unchecked((long)0x0000087654320800UL);
    [MethodImpl(NI)] static long C243(long x) { long r = (x < unchecked((long)0x0000087654320800UL) ? 1 : 0) + ((ulong)x < 0x0000087654320800UL ? 2 : 0) + (x == unchecked((long)0x0000087654320800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000087654320800UL)); return r * 31 + (x ^ unchecked((long)0x0000087654320800UL)); }
    [MethodImpl(NI)] static long K244() => unchecked((long)0x0000087654321000UL);
    [MethodImpl(NI)] static long A244(long x) => x + unchecked((long)0x0000087654321000UL);
    [MethodImpl(NI)] static long C244(long x) { long r = (x < unchecked((long)0x0000087654321000UL) ? 1 : 0) + ((ulong)x < 0x0000087654321000UL ? 2 : 0) + (x == unchecked((long)0x0000087654321000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000087654321000UL)); return r * 31 + (x ^ unchecked((long)0x0000087654321000UL)); }
    [MethodImpl(NI)] static long K245() => unchecked((long)0x00000876543217FFUL);
    [MethodImpl(NI)] static long A245(long x) => x + unchecked((long)0x00000876543217FFUL);
    [MethodImpl(NI)] static long C245(long x) { long r = (x < unchecked((long)0x00000876543217FFUL) ? 1 : 0) + ((ulong)x < 0x00000876543217FFUL ? 2 : 0) + (x == unchecked((long)0x00000876543217FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000876543217FFUL)); return r * 31 + (x ^ unchecked((long)0x00000876543217FFUL)); }
    [MethodImpl(NI)] static long K246() => unchecked((long)0x00000947F3D9FCDAUL);
    [MethodImpl(NI)] static long A246(long x) => x + unchecked((long)0x00000947F3D9FCDAUL);
    [MethodImpl(NI)] static long C246(long x) { long r = (x < unchecked((long)0x00000947F3D9FCDAUL) ? 1 : 0) + ((ulong)x < 0x00000947F3D9FCDAUL ? 2 : 0) + (x == unchecked((long)0x00000947F3D9FCDAUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000947F3D9FCDAUL)); return r * 31 + (x ^ unchecked((long)0x00000947F3D9FCDAUL)); }
    [MethodImpl(NI)] static long K247() => unchecked((long)0x00000947F3D9FFFFUL);
    [MethodImpl(NI)] static long A247(long x) => x + unchecked((long)0x00000947F3D9FFFFUL);
    [MethodImpl(NI)] static long C247(long x) { long r = (x < unchecked((long)0x00000947F3D9FFFFUL) ? 1 : 0) + ((ulong)x < 0x00000947F3D9FFFFUL ? 2 : 0) + (x == unchecked((long)0x00000947F3D9FFFFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000947F3D9FFFFUL)); return r * 31 + (x ^ unchecked((long)0x00000947F3D9FFFFUL)); }
    [MethodImpl(NI)] static long K248() => unchecked((long)0x00000B5A81B54800UL);
    [MethodImpl(NI)] static long A248(long x) => x + unchecked((long)0x00000B5A81B54800UL);
    [MethodImpl(NI)] static long C248(long x) { long r = (x < unchecked((long)0x00000B5A81B54800UL) ? 1 : 0) + ((ulong)x < 0x00000B5A81B54800UL ? 2 : 0) + (x == unchecked((long)0x00000B5A81B54800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000B5A81B54800UL)); return r * 31 + (x ^ unchecked((long)0x00000B5A81B54800UL)); }
    [MethodImpl(NI)] static long K249() => unchecked((long)0x00000B5A81B54DABUL);
    [MethodImpl(NI)] static long A249(long x) => x + unchecked((long)0x00000B5A81B54DABUL);
    [MethodImpl(NI)] static long C249(long x) { long r = (x < unchecked((long)0x00000B5A81B54DABUL) ? 1 : 0) + ((ulong)x < 0x00000B5A81B54DABUL ? 2 : 0) + (x == unchecked((long)0x00000B5A81B54DABUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000B5A81B54DABUL)); return r * 31 + (x ^ unchecked((long)0x00000B5A81B54DABUL)); }
    [MethodImpl(NI)] static long K250() => unchecked((long)0x00000BFFEFFF7800UL);
    [MethodImpl(NI)] static long A250(long x) => x + unchecked((long)0x00000BFFEFFF7800UL);
    [MethodImpl(NI)] static long C250(long x) { long r = (x < unchecked((long)0x00000BFFEFFF7800UL) ? 1 : 0) + ((ulong)x < 0x00000BFFEFFF7800UL ? 2 : 0) + (x == unchecked((long)0x00000BFFEFFF7800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000BFFEFFF7800UL)); return r * 31 + (x ^ unchecked((long)0x00000BFFEFFF7800UL)); }
    [MethodImpl(NI)] static long K251() => unchecked((long)0x00000BFFEFFF8000UL);
    [MethodImpl(NI)] static long A251(long x) => x + unchecked((long)0x00000BFFEFFF8000UL);
    [MethodImpl(NI)] static long C251(long x) { long r = (x < unchecked((long)0x00000BFFEFFF8000UL) ? 1 : 0) + ((ulong)x < 0x00000BFFEFFF8000UL ? 2 : 0) + (x == unchecked((long)0x00000BFFEFFF8000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000BFFEFFF8000UL)); return r * 31 + (x ^ unchecked((long)0x00000BFFEFFF8000UL)); }
    [MethodImpl(NI)] static long K252() => unchecked((long)0x00000BFFEFFF87FFUL);
    [MethodImpl(NI)] static long A252(long x) => x + unchecked((long)0x00000BFFEFFF87FFUL);
    [MethodImpl(NI)] static long C252(long x) { long r = (x < unchecked((long)0x00000BFFEFFF87FFUL) ? 1 : 0) + ((ulong)x < 0x00000BFFEFFF87FFUL ? 2 : 0) + (x == unchecked((long)0x00000BFFEFFF87FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000BFFEFFF87FFUL)); return r * 31 + (x ^ unchecked((long)0x00000BFFEFFF87FFUL)); }
    [MethodImpl(NI)] static long K253() => unchecked((long)0x00000BFFEFFFF800UL);
    [MethodImpl(NI)] static long A253(long x) => x + unchecked((long)0x00000BFFEFFFF800UL);
    [MethodImpl(NI)] static long C253(long x) { long r = (x < unchecked((long)0x00000BFFEFFFF800UL) ? 1 : 0) + ((ulong)x < 0x00000BFFEFFFF800UL ? 2 : 0) + (x == unchecked((long)0x00000BFFEFFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000BFFEFFFF800UL)); return r * 31 + (x ^ unchecked((long)0x00000BFFEFFFF800UL)); }
    [MethodImpl(NI)] static long K254() => unchecked((long)0x00000BFFF0000000UL);
    [MethodImpl(NI)] static long A254(long x) => x + unchecked((long)0x00000BFFF0000000UL);
    [MethodImpl(NI)] static long C254(long x) { long r = (x < unchecked((long)0x00000BFFF0000000UL) ? 1 : 0) + ((ulong)x < 0x00000BFFF0000000UL ? 2 : 0) + (x == unchecked((long)0x00000BFFF0000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000BFFF0000000UL)); return r * 31 + (x ^ unchecked((long)0x00000BFFF0000000UL)); }
    [MethodImpl(NI)] static long K255() => unchecked((long)0x00000BFFF00007FFUL);
    [MethodImpl(NI)] static long A255(long x) => x + unchecked((long)0x00000BFFF00007FFUL);
    [MethodImpl(NI)] static long C255(long x) { long r = (x < unchecked((long)0x00000BFFF00007FFUL) ? 1 : 0) + ((ulong)x < 0x00000BFFF00007FFUL ? 2 : 0) + (x == unchecked((long)0x00000BFFF00007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000BFFF00007FFUL)); return r * 31 + (x ^ unchecked((long)0x00000BFFF00007FFUL)); }
    [MethodImpl(NI)] static long K256() => unchecked((long)0x00000C00007FF800UL);
    [MethodImpl(NI)] static long A256(long x) => x + unchecked((long)0x00000C00007FF800UL);
    [MethodImpl(NI)] static long C256(long x) { long r = (x < unchecked((long)0x00000C00007FF800UL) ? 1 : 0) + ((ulong)x < 0x00000C00007FF800UL ? 2 : 0) + (x == unchecked((long)0x00000C00007FF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000C00007FF800UL)); return r * 31 + (x ^ unchecked((long)0x00000C00007FF800UL)); }
    [MethodImpl(NI)] static long K257() => unchecked((long)0x00000C0000800000UL);
    [MethodImpl(NI)] static long A257(long x) => x + unchecked((long)0x00000C0000800000UL);
    [MethodImpl(NI)] static long C257(long x) { long r = (x < unchecked((long)0x00000C0000800000UL) ? 1 : 0) + ((ulong)x < 0x00000C0000800000UL ? 2 : 0) + (x == unchecked((long)0x00000C0000800000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000C0000800000UL)); return r * 31 + (x ^ unchecked((long)0x00000C0000800000UL)); }
    [MethodImpl(NI)] static long K258() => unchecked((long)0x00000C00008007FFUL);
    [MethodImpl(NI)] static long A258(long x) => x + unchecked((long)0x00000C00008007FFUL);
    [MethodImpl(NI)] static long C258(long x) { long r = (x < unchecked((long)0x00000C00008007FFUL) ? 1 : 0) + ((ulong)x < 0x00000C00008007FFUL ? 2 : 0) + (x == unchecked((long)0x00000C00008007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000C00008007FFUL)); return r * 31 + (x ^ unchecked((long)0x00000C00008007FFUL)); }
    [MethodImpl(NI)] static long K259() => unchecked((long)0x00000D3D8F1017FFUL);
    [MethodImpl(NI)] static long A259(long x) => x + unchecked((long)0x00000D3D8F1017FFUL);
    [MethodImpl(NI)] static long C259(long x) { long r = (x < unchecked((long)0x00000D3D8F1017FFUL) ? 1 : 0) + ((ulong)x < 0x00000D3D8F1017FFUL ? 2 : 0) + (x == unchecked((long)0x00000D3D8F1017FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000D3D8F1017FFUL)); return r * 31 + (x ^ unchecked((long)0x00000D3D8F1017FFUL)); }
    [MethodImpl(NI)] static long K260() => unchecked((long)0x00000D3D8F101F4EUL);
    [MethodImpl(NI)] static long A260(long x) => x + unchecked((long)0x00000D3D8F101F4EUL);
    [MethodImpl(NI)] static long C260(long x) { long r = (x < unchecked((long)0x00000D3D8F101F4EUL) ? 1 : 0) + ((ulong)x < 0x00000D3D8F101F4EUL ? 2 : 0) + (x == unchecked((long)0x00000D3D8F101F4EUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000D3D8F101F4EUL)); return r * 31 + (x ^ unchecked((long)0x00000D3D8F101F4EUL)); }
    [MethodImpl(NI)] static long K261() => unchecked((long)0x00000E1B98B5F13AUL);
    [MethodImpl(NI)] static long A261(long x) => x + unchecked((long)0x00000E1B98B5F13AUL);
    [MethodImpl(NI)] static long C261(long x) { long r = (x < unchecked((long)0x00000E1B98B5F13AUL) ? 1 : 0) + ((ulong)x < 0x00000E1B98B5F13AUL ? 2 : 0) + (x == unchecked((long)0x00000E1B98B5F13AUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000E1B98B5F13AUL)); return r * 31 + (x ^ unchecked((long)0x00000E1B98B5F13AUL)); }
    [MethodImpl(NI)] static long K262() => unchecked((long)0x00000E1B98B5FFFFUL);
    [MethodImpl(NI)] static long A262(long x) => x + unchecked((long)0x00000E1B98B5FFFFUL);
    [MethodImpl(NI)] static long C262(long x) { long r = (x < unchecked((long)0x00000E1B98B5FFFFUL) ? 1 : 0) + ((ulong)x < 0x00000E1B98B5FFFFUL ? 2 : 0) + (x == unchecked((long)0x00000E1B98B5FFFFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000E1B98B5FFFFUL)); return r * 31 + (x ^ unchecked((long)0x00000E1B98B5FFFFUL)); }
    [MethodImpl(NI)] static long K263() => unchecked((long)0x00000FFEFFFFF800UL);
    [MethodImpl(NI)] static long A263(long x) => x + unchecked((long)0x00000FFEFFFFF800UL);
    [MethodImpl(NI)] static long C263(long x) { long r = (x < unchecked((long)0x00000FFEFFFFF800UL) ? 1 : 0) + ((ulong)x < 0x00000FFEFFFFF800UL ? 2 : 0) + (x == unchecked((long)0x00000FFEFFFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000FFEFFFFF800UL)); return r * 31 + (x ^ unchecked((long)0x00000FFEFFFFF800UL)); }
    [MethodImpl(NI)] static long K264() => unchecked((long)0x00000FFF00000000UL);
    [MethodImpl(NI)] static long A264(long x) => x + unchecked((long)0x00000FFF00000000UL);
    [MethodImpl(NI)] static long C264(long x) { long r = (x < unchecked((long)0x00000FFF00000000UL) ? 1 : 0) + ((ulong)x < 0x00000FFF00000000UL ? 2 : 0) + (x == unchecked((long)0x00000FFF00000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000FFF00000000UL)); return r * 31 + (x ^ unchecked((long)0x00000FFF00000000UL)); }
    [MethodImpl(NI)] static long K265() => unchecked((long)0x00000FFF000007FFUL);
    [MethodImpl(NI)] static long A265(long x) => x + unchecked((long)0x00000FFF000007FFUL);
    [MethodImpl(NI)] static long C265(long x) { long r = (x < unchecked((long)0x00000FFF000007FFUL) ? 1 : 0) + ((ulong)x < 0x00000FFF000007FFUL ? 2 : 0) + (x == unchecked((long)0x00000FFF000007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000FFF000007FFUL)); return r * 31 + (x ^ unchecked((long)0x00000FFF000007FFUL)); }
    [MethodImpl(NI)] static long K266() => unchecked((long)0x00000FFF00000FFFUL);
    [MethodImpl(NI)] static long A266(long x) => x + unchecked((long)0x00000FFF00000FFFUL);
    [MethodImpl(NI)] static long C266(long x) { long r = (x < unchecked((long)0x00000FFF00000FFFUL) ? 1 : 0) + ((ulong)x < 0x00000FFF00000FFFUL ? 2 : 0) + (x == unchecked((long)0x00000FFF00000FFFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000FFF00000FFFUL)); return r * 31 + (x ^ unchecked((long)0x00000FFF00000FFFUL)); }
    [MethodImpl(NI)] static long K267() => unchecked((long)0x00000FFFFEFFF800UL);
    [MethodImpl(NI)] static long A267(long x) => x + unchecked((long)0x00000FFFFEFFF800UL);
    [MethodImpl(NI)] static long C267(long x) { long r = (x < unchecked((long)0x00000FFFFEFFF800UL) ? 1 : 0) + ((ulong)x < 0x00000FFFFEFFF800UL ? 2 : 0) + (x == unchecked((long)0x00000FFFFEFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000FFFFEFFF800UL)); return r * 31 + (x ^ unchecked((long)0x00000FFFFEFFF800UL)); }
    [MethodImpl(NI)] static long K268() => unchecked((long)0x00000FFFFF000000UL);
    [MethodImpl(NI)] static long A268(long x) => x + unchecked((long)0x00000FFFFF000000UL);
    [MethodImpl(NI)] static long C268(long x) { long r = (x < unchecked((long)0x00000FFFFF000000UL) ? 1 : 0) + ((ulong)x < 0x00000FFFFF000000UL ? 2 : 0) + (x == unchecked((long)0x00000FFFFF000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000FFFFF000000UL)); return r * 31 + (x ^ unchecked((long)0x00000FFFFF000000UL)); }
    [MethodImpl(NI)] static long K269() => unchecked((long)0x00000FFFFF0007FFUL);
    [MethodImpl(NI)] static long A269(long x) => x + unchecked((long)0x00000FFFFF0007FFUL);
    [MethodImpl(NI)] static long C269(long x) { long r = (x < unchecked((long)0x00000FFFFF0007FFUL) ? 1 : 0) + ((ulong)x < 0x00000FFFFF0007FFUL ? 2 : 0) + (x == unchecked((long)0x00000FFFFF0007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000FFFFF0007FFUL)); return r * 31 + (x ^ unchecked((long)0x00000FFFFF0007FFUL)); }
    [MethodImpl(NI)] static long K270() => unchecked((long)0x00000FFFFF7FE800UL);
    [MethodImpl(NI)] static long A270(long x) => x + unchecked((long)0x00000FFFFF7FE800UL);
    [MethodImpl(NI)] static long C270(long x) { long r = (x < unchecked((long)0x00000FFFFF7FE800UL) ? 1 : 0) + ((ulong)x < 0x00000FFFFF7FE800UL ? 2 : 0) + (x == unchecked((long)0x00000FFFFF7FE800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000FFFFF7FE800UL)); return r * 31 + (x ^ unchecked((long)0x00000FFFFF7FE800UL)); }
    [MethodImpl(NI)] static long K271() => unchecked((long)0x00000FFFFF7FF000UL);
    [MethodImpl(NI)] static long A271(long x) => x + unchecked((long)0x00000FFFFF7FF000UL);
    [MethodImpl(NI)] static long C271(long x) { long r = (x < unchecked((long)0x00000FFFFF7FF000UL) ? 1 : 0) + ((ulong)x < 0x00000FFFFF7FF000UL ? 2 : 0) + (x == unchecked((long)0x00000FFFFF7FF000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000FFFFF7FF000UL)); return r * 31 + (x ^ unchecked((long)0x00000FFFFF7FF000UL)); }
    [MethodImpl(NI)] static long K272() => unchecked((long)0x00000FFFFF7FF7FFUL);
    [MethodImpl(NI)] static long A272(long x) => x + unchecked((long)0x00000FFFFF7FF7FFUL);
    [MethodImpl(NI)] static long C272(long x) { long r = (x < unchecked((long)0x00000FFFFF7FF7FFUL) ? 1 : 0) + ((ulong)x < 0x00000FFFFF7FF7FFUL ? 2 : 0) + (x == unchecked((long)0x00000FFFFF7FF7FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000FFFFF7FF7FFUL)); return r * 31 + (x ^ unchecked((long)0x00000FFFFF7FF7FFUL)); }
    [MethodImpl(NI)] static long K273() => unchecked((long)0x00000FFFFF7FF800UL);
    [MethodImpl(NI)] static long A273(long x) => x + unchecked((long)0x00000FFFFF7FF800UL);
    [MethodImpl(NI)] static long C273(long x) { long r = (x < unchecked((long)0x00000FFFFF7FF800UL) ? 1 : 0) + ((ulong)x < 0x00000FFFFF7FF800UL ? 2 : 0) + (x == unchecked((long)0x00000FFFFF7FF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000FFFFF7FF800UL)); return r * 31 + (x ^ unchecked((long)0x00000FFFFF7FF800UL)); }
    [MethodImpl(NI)] static long K274() => unchecked((long)0x00000FFFFF800000UL);
    [MethodImpl(NI)] static long A274(long x) => x + unchecked((long)0x00000FFFFF800000UL);
    [MethodImpl(NI)] static long C274(long x) { long r = (x < unchecked((long)0x00000FFFFF800000UL) ? 1 : 0) + ((ulong)x < 0x00000FFFFF800000UL ? 2 : 0) + (x == unchecked((long)0x00000FFFFF800000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000FFFFF800000UL)); return r * 31 + (x ^ unchecked((long)0x00000FFFFF800000UL)); }
    [MethodImpl(NI)] static long K275() => unchecked((long)0x00000FFFFF8007FFUL);
    [MethodImpl(NI)] static long A275(long x) => x + unchecked((long)0x00000FFFFF8007FFUL);
    [MethodImpl(NI)] static long C275(long x) { long r = (x < unchecked((long)0x00000FFFFF8007FFUL) ? 1 : 0) + ((ulong)x < 0x00000FFFFF8007FFUL ? 2 : 0) + (x == unchecked((long)0x00000FFFFF8007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000FFFFF8007FFUL)); return r * 31 + (x ^ unchecked((long)0x00000FFFFF8007FFUL)); }
    [MethodImpl(NI)] static long K276() => unchecked((long)0x00000FFFFFFFE800UL);
    [MethodImpl(NI)] static long A276(long x) => x + unchecked((long)0x00000FFFFFFFE800UL);
    [MethodImpl(NI)] static long C276(long x) { long r = (x < unchecked((long)0x00000FFFFFFFE800UL) ? 1 : 0) + ((ulong)x < 0x00000FFFFFFFE800UL ? 2 : 0) + (x == unchecked((long)0x00000FFFFFFFE800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000FFFFFFFE800UL)); return r * 31 + (x ^ unchecked((long)0x00000FFFFFFFE800UL)); }
    [MethodImpl(NI)] static long K277() => unchecked((long)0x00000FFFFFFFF000UL);
    [MethodImpl(NI)] static long A277(long x) => x + unchecked((long)0x00000FFFFFFFF000UL);
    [MethodImpl(NI)] static long C277(long x) { long r = (x < unchecked((long)0x00000FFFFFFFF000UL) ? 1 : 0) + ((ulong)x < 0x00000FFFFFFFF000UL ? 2 : 0) + (x == unchecked((long)0x00000FFFFFFFF000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000FFFFFFFF000UL)); return r * 31 + (x ^ unchecked((long)0x00000FFFFFFFF000UL)); }
    [MethodImpl(NI)] static long K278() => unchecked((long)0x00000FFFFFFFF7FFUL);
    [MethodImpl(NI)] static long A278(long x) => x + unchecked((long)0x00000FFFFFFFF7FFUL);
    [MethodImpl(NI)] static long C278(long x) { long r = (x < unchecked((long)0x00000FFFFFFFF7FFUL) ? 1 : 0) + ((ulong)x < 0x00000FFFFFFFF7FFUL ? 2 : 0) + (x == unchecked((long)0x00000FFFFFFFF7FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000FFFFFFFF7FFUL)); return r * 31 + (x ^ unchecked((long)0x00000FFFFFFFF7FFUL)); }
    [MethodImpl(NI)] static long K279() => unchecked((long)0x00000FFFFFFFF800UL);
    [MethodImpl(NI)] static long A279(long x) => x + unchecked((long)0x00000FFFFFFFF800UL);
    [MethodImpl(NI)] static long C279(long x) { long r = (x < unchecked((long)0x00000FFFFFFFF800UL) ? 1 : 0) + ((ulong)x < 0x00000FFFFFFFF800UL ? 2 : 0) + (x == unchecked((long)0x00000FFFFFFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00000FFFFFFFF800UL)); return r * 31 + (x ^ unchecked((long)0x00000FFFFFFFF800UL)); }
    [MethodImpl(NI)] static long K280() => unchecked((long)0x0000100000000000UL);
    [MethodImpl(NI)] static long A280(long x) => x + unchecked((long)0x0000100000000000UL);
    [MethodImpl(NI)] static long C280(long x) { long r = (x < unchecked((long)0x0000100000000000UL) ? 1 : 0) + ((ulong)x < 0x0000100000000000UL ? 2 : 0) + (x == unchecked((long)0x0000100000000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000100000000000UL)); return r * 31 + (x ^ unchecked((long)0x0000100000000000UL)); }
    [MethodImpl(NI)] static long K281() => unchecked((long)0x00001000000007FFUL);
    [MethodImpl(NI)] static long A281(long x) => x + unchecked((long)0x00001000000007FFUL);
    [MethodImpl(NI)] static long C281(long x) { long r = (x < unchecked((long)0x00001000000007FFUL) ? 1 : 0) + ((ulong)x < 0x00001000000007FFUL ? 2 : 0) + (x == unchecked((long)0x00001000000007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00001000000007FFUL)); return r * 31 + (x ^ unchecked((long)0x00001000000007FFUL)); }
    [MethodImpl(NI)] static long K282() => unchecked((long)0x0000100000001000UL);
    [MethodImpl(NI)] static long A282(long x) => x + unchecked((long)0x0000100000001000UL);
    [MethodImpl(NI)] static long C282(long x) { long r = (x < unchecked((long)0x0000100000001000UL) ? 1 : 0) + ((ulong)x < 0x0000100000001000UL ? 2 : 0) + (x == unchecked((long)0x0000100000001000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000100000001000UL)); return r * 31 + (x ^ unchecked((long)0x0000100000001000UL)); }
    [MethodImpl(NI)] static long K283() => unchecked((long)0x0000123450000000UL);
    [MethodImpl(NI)] static long A283(long x) => x + unchecked((long)0x0000123450000000UL);
    [MethodImpl(NI)] static long C283(long x) { long r = (x < unchecked((long)0x0000123450000000UL) ? 1 : 0) + ((ulong)x < 0x0000123450000000UL ? 2 : 0) + (x == unchecked((long)0x0000123450000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000123450000000UL)); return r * 31 + (x ^ unchecked((long)0x0000123450000000UL)); }
    [MethodImpl(NI)] static long K284() => unchecked((long)0x000017FEFFFFF800UL);
    [MethodImpl(NI)] static long A284(long x) => x + unchecked((long)0x000017FEFFFFF800UL);
    [MethodImpl(NI)] static long C284(long x) { long r = (x < unchecked((long)0x000017FEFFFFF800UL) ? 1 : 0) + ((ulong)x < 0x000017FEFFFFF800UL ? 2 : 0) + (x == unchecked((long)0x000017FEFFFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000017FEFFFFF800UL)); return r * 31 + (x ^ unchecked((long)0x000017FEFFFFF800UL)); }
    [MethodImpl(NI)] static long K285() => unchecked((long)0x000017FF00000000UL);
    [MethodImpl(NI)] static long A285(long x) => x + unchecked((long)0x000017FF00000000UL);
    [MethodImpl(NI)] static long C285(long x) { long r = (x < unchecked((long)0x000017FF00000000UL) ? 1 : 0) + ((ulong)x < 0x000017FF00000000UL ? 2 : 0) + (x == unchecked((long)0x000017FF00000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000017FF00000000UL)); return r * 31 + (x ^ unchecked((long)0x000017FF00000000UL)); }
    [MethodImpl(NI)] static long K286() => unchecked((long)0x000017FF000007FFUL);
    [MethodImpl(NI)] static long A286(long x) => x + unchecked((long)0x000017FF000007FFUL);
    [MethodImpl(NI)] static long C286(long x) { long r = (x < unchecked((long)0x000017FF000007FFUL) ? 1 : 0) + ((ulong)x < 0x000017FF000007FFUL ? 2 : 0) + (x == unchecked((long)0x000017FF000007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000017FF000007FFUL)); return r * 31 + (x ^ unchecked((long)0x000017FF000007FFUL)); }
    [MethodImpl(NI)] static long K287() => unchecked((long)0x000017FF000017FFUL);
    [MethodImpl(NI)] static long A287(long x) => x + unchecked((long)0x000017FF000017FFUL);
    [MethodImpl(NI)] static long C287(long x) { long r = (x < unchecked((long)0x000017FF000017FFUL) ? 1 : 0) + ((ulong)x < 0x000017FF000017FFUL ? 2 : 0) + (x == unchecked((long)0x000017FF000017FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000017FF000017FFUL)); return r * 31 + (x ^ unchecked((long)0x000017FF000017FFUL)); }
    [MethodImpl(NI)] static long K288() => unchecked((long)0x000017FFFFFFF800UL);
    [MethodImpl(NI)] static long A288(long x) => x + unchecked((long)0x000017FFFFFFF800UL);
    [MethodImpl(NI)] static long C288(long x) { long r = (x < unchecked((long)0x000017FFFFFFF800UL) ? 1 : 0) + ((ulong)x < 0x000017FFFFFFF800UL ? 2 : 0) + (x == unchecked((long)0x000017FFFFFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000017FFFFFFF800UL)); return r * 31 + (x ^ unchecked((long)0x000017FFFFFFF800UL)); }
    [MethodImpl(NI)] static long K289() => unchecked((long)0x0000180000000000UL);
    [MethodImpl(NI)] static long A289(long x) => x + unchecked((long)0x0000180000000000UL);
    [MethodImpl(NI)] static long C289(long x) { long r = (x < unchecked((long)0x0000180000000000UL) ? 1 : 0) + ((ulong)x < 0x0000180000000000UL ? 2 : 0) + (x == unchecked((long)0x0000180000000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000180000000000UL)); return r * 31 + (x ^ unchecked((long)0x0000180000000000UL)); }
    [MethodImpl(NI)] static long K290() => unchecked((long)0x00001800000007FFUL);
    [MethodImpl(NI)] static long A290(long x) => x + unchecked((long)0x00001800000007FFUL);
    [MethodImpl(NI)] static long C290(long x) { long r = (x < unchecked((long)0x00001800000007FFUL) ? 1 : 0) + ((ulong)x < 0x00001800000007FFUL ? 2 : 0) + (x == unchecked((long)0x00001800000007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00001800000007FFUL)); return r * 31 + (x ^ unchecked((long)0x00001800000007FFUL)); }
    [MethodImpl(NI)] static long K291() => unchecked((long)0x0000180000001800UL);
    [MethodImpl(NI)] static long A291(long x) => x + unchecked((long)0x0000180000001800UL);
    [MethodImpl(NI)] static long C291(long x) { long r = (x < unchecked((long)0x0000180000001800UL) ? 1 : 0) + ((ulong)x < 0x0000180000001800UL ? 2 : 0) + (x == unchecked((long)0x0000180000001800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000180000001800UL)); return r * 31 + (x ^ unchecked((long)0x0000180000001800UL)); }
    [MethodImpl(NI)] static long K292() => unchecked((long)0x0000400000000000UL);
    [MethodImpl(NI)] static long A292(long x) => x + unchecked((long)0x0000400000000000UL);
    [MethodImpl(NI)] static long C292(long x) { long r = (x < unchecked((long)0x0000400000000000UL) ? 1 : 0) + ((ulong)x < 0x0000400000000000UL ? 2 : 0) + (x == unchecked((long)0x0000400000000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000400000000000UL)); return r * 31 + (x ^ unchecked((long)0x0000400000000000UL)); }
    [MethodImpl(NI)] static long K293() => unchecked((long)0x00007FFFF0000000UL);
    [MethodImpl(NI)] static long A293(long x) => x + unchecked((long)0x00007FFFF0000000UL);
    [MethodImpl(NI)] static long C293(long x) { long r = (x < unchecked((long)0x00007FFFF0000000UL) ? 1 : 0) + ((ulong)x < 0x00007FFFF0000000UL ? 2 : 0) + (x == unchecked((long)0x00007FFFF0000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00007FFFF0000000UL)); return r * 31 + (x ^ unchecked((long)0x00007FFFF0000000UL)); }
    [MethodImpl(NI)] static long K294() => unchecked((long)0x00007FFFF7FFFFFFUL);
    [MethodImpl(NI)] static long A294(long x) => x + unchecked((long)0x00007FFFF7FFFFFFUL);
    [MethodImpl(NI)] static long C294(long x) { long r = (x < unchecked((long)0x00007FFFF7FFFFFFUL) ? 1 : 0) + ((ulong)x < 0x00007FFFF7FFFFFFUL ? 2 : 0) + (x == unchecked((long)0x00007FFFF7FFFFFFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00007FFFF7FFFFFFUL)); return r * 31 + (x ^ unchecked((long)0x00007FFFF7FFFFFFUL)); }
    [MethodImpl(NI)] static long K295() => unchecked((long)0x00007FFFF8000000UL);
    [MethodImpl(NI)] static long A295(long x) => x + unchecked((long)0x00007FFFF8000000UL);
    [MethodImpl(NI)] static long C295(long x) { long r = (x < unchecked((long)0x00007FFFF8000000UL) ? 1 : 0) + ((ulong)x < 0x00007FFFF8000000UL ? 2 : 0) + (x == unchecked((long)0x00007FFFF8000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00007FFFF8000000UL)); return r * 31 + (x ^ unchecked((long)0x00007FFFF8000000UL)); }
    [MethodImpl(NI)] static long K296() => unchecked((long)0x00007FFFF8000001UL);
    [MethodImpl(NI)] static long A296(long x) => x + unchecked((long)0x00007FFFF8000001UL);
    [MethodImpl(NI)] static long C296(long x) { long r = (x < unchecked((long)0x00007FFFF8000001UL) ? 1 : 0) + ((ulong)x < 0x00007FFFF8000001UL ? 2 : 0) + (x == unchecked((long)0x00007FFFF8000001UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00007FFFF8000001UL)); return r * 31 + (x ^ unchecked((long)0x00007FFFF8000001UL)); }
    [MethodImpl(NI)] static long K297() => unchecked((long)0x00007FFFFFFFF7FFUL);
    [MethodImpl(NI)] static long A297(long x) => x + unchecked((long)0x00007FFFFFFFF7FFUL);
    [MethodImpl(NI)] static long C297(long x) { long r = (x < unchecked((long)0x00007FFFFFFFF7FFUL) ? 1 : 0) + ((ulong)x < 0x00007FFFFFFFF7FFUL ? 2 : 0) + (x == unchecked((long)0x00007FFFFFFFF7FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00007FFFFFFFF7FFUL)); return r * 31 + (x ^ unchecked((long)0x00007FFFFFFFF7FFUL)); }
    [MethodImpl(NI)] static long K298() => unchecked((long)0x00007FFFFFFFF800UL);
    [MethodImpl(NI)] static long A298(long x) => x + unchecked((long)0x00007FFFFFFFF800UL);
    [MethodImpl(NI)] static long C298(long x) { long r = (x < unchecked((long)0x00007FFFFFFFF800UL) ? 1 : 0) + ((ulong)x < 0x00007FFFFFFFF800UL ? 2 : 0) + (x == unchecked((long)0x00007FFFFFFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00007FFFFFFFF800UL)); return r * 31 + (x ^ unchecked((long)0x00007FFFFFFFF800UL)); }
    [MethodImpl(NI)] static long K299() => unchecked((long)0x00007FFFFFFFF801UL);
    [MethodImpl(NI)] static long A299(long x) => x + unchecked((long)0x00007FFFFFFFF801UL);
    [MethodImpl(NI)] static long C299(long x) { long r = (x < unchecked((long)0x00007FFFFFFFF801UL) ? 1 : 0) + ((ulong)x < 0x00007FFFFFFFF801UL ? 2 : 0) + (x == unchecked((long)0x00007FFFFFFFF801UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00007FFFFFFFF801UL)); return r * 31 + (x ^ unchecked((long)0x00007FFFFFFFF801UL)); }
    [MethodImpl(NI)] static long K300() => unchecked((long)0x00007FFFFFFFFFFFUL);
    [MethodImpl(NI)] static long A300(long x) => x + unchecked((long)0x00007FFFFFFFFFFFUL);
    [MethodImpl(NI)] static long C300(long x) { long r = (x < unchecked((long)0x00007FFFFFFFFFFFUL) ? 1 : 0) + ((ulong)x < 0x00007FFFFFFFFFFFUL ? 2 : 0) + (x == unchecked((long)0x00007FFFFFFFFFFFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00007FFFFFFFFFFFUL)); return r * 31 + (x ^ unchecked((long)0x00007FFFFFFFFFFFUL)); }
    [MethodImpl(NI)] static long K301() => unchecked((long)0x0000800000000000UL);
    [MethodImpl(NI)] static long A301(long x) => x + unchecked((long)0x0000800000000000UL);
    [MethodImpl(NI)] static long C301(long x) { long r = (x < unchecked((long)0x0000800000000000UL) ? 1 : 0) + ((ulong)x < 0x0000800000000000UL ? 2 : 0) + (x == unchecked((long)0x0000800000000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000800000000000UL)); return r * 31 + (x ^ unchecked((long)0x0000800000000000UL)); }
    [MethodImpl(NI)] static long K302() => unchecked((long)0x0000800000000001UL);
    [MethodImpl(NI)] static long A302(long x) => x + unchecked((long)0x0000800000000001UL);
    [MethodImpl(NI)] static long C302(long x) { long r = (x < unchecked((long)0x0000800000000001UL) ? 1 : 0) + ((ulong)x < 0x0000800000000001UL ? 2 : 0) + (x == unchecked((long)0x0000800000000001UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0000800000000001UL)); return r * 31 + (x ^ unchecked((long)0x0000800000000001UL)); }
    [MethodImpl(NI)] static long K303() => unchecked((long)0x0001234500000000UL);
    [MethodImpl(NI)] static long A303(long x) => x + unchecked((long)0x0001234500000000UL);
    [MethodImpl(NI)] static long C303(long x) { long r = (x < unchecked((long)0x0001234500000000UL) ? 1 : 0) + ((ulong)x < 0x0001234500000000UL ? 2 : 0) + (x == unchecked((long)0x0001234500000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0001234500000000UL)); return r * 31 + (x ^ unchecked((long)0x0001234500000000UL)); }
    [MethodImpl(NI)] static long K304() => unchecked((long)0x0003EC2689D3380DUL);
    [MethodImpl(NI)] static long A304(long x) => x + unchecked((long)0x0003EC2689D3380DUL);
    [MethodImpl(NI)] static long C304(long x) { long r = (x < unchecked((long)0x0003EC2689D3380DUL) ? 1 : 0) + ((ulong)x < 0x0003EC2689D3380DUL ? 2 : 0) + (x == unchecked((long)0x0003EC2689D3380DUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0003EC2689D3380DUL)); return r * 31 + (x ^ unchecked((long)0x0003EC2689D3380DUL)); }
    [MethodImpl(NI)] static long K305() => unchecked((long)0x0003EC2689D33FFFUL);
    [MethodImpl(NI)] static long A305(long x) => x + unchecked((long)0x0003EC2689D33FFFUL);
    [MethodImpl(NI)] static long C305(long x) { long r = (x < unchecked((long)0x0003EC2689D33FFFUL) ? 1 : 0) + ((ulong)x < 0x0003EC2689D33FFFUL ? 2 : 0) + (x == unchecked((long)0x0003EC2689D33FFFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0003EC2689D33FFFUL)); return r * 31 + (x ^ unchecked((long)0x0003EC2689D33FFFUL)); }
    [MethodImpl(NI)] static long K306() => unchecked((long)0x0004000000000000UL);
    [MethodImpl(NI)] static long A306(long x) => x + unchecked((long)0x0004000000000000UL);
    [MethodImpl(NI)] static long C306(long x) { long r = (x < unchecked((long)0x0004000000000000UL) ? 1 : 0) + ((ulong)x < 0x0004000000000000UL ? 2 : 0) + (x == unchecked((long)0x0004000000000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0004000000000000UL)); return r * 31 + (x ^ unchecked((long)0x0004000000000000UL)); }
    [MethodImpl(NI)] static long K307() => unchecked((long)0x000411F744FB97FFUL);
    [MethodImpl(NI)] static long A307(long x) => x + unchecked((long)0x000411F744FB97FFUL);
    [MethodImpl(NI)] static long C307(long x) { long r = (x < unchecked((long)0x000411F744FB97FFUL) ? 1 : 0) + ((ulong)x < 0x000411F744FB97FFUL ? 2 : 0) + (x == unchecked((long)0x000411F744FB97FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000411F744FB97FFUL)); return r * 31 + (x ^ unchecked((long)0x000411F744FB97FFUL)); }
    [MethodImpl(NI)] static long K308() => unchecked((long)0x000411F744FB9EC2UL);
    [MethodImpl(NI)] static long A308(long x) => x + unchecked((long)0x000411F744FB9EC2UL);
    [MethodImpl(NI)] static long C308(long x) { long r = (x < unchecked((long)0x000411F744FB9EC2UL) ? 1 : 0) + ((ulong)x < 0x000411F744FB9EC2UL ? 2 : 0) + (x == unchecked((long)0x000411F744FB9EC2UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000411F744FB9EC2UL)); return r * 31 + (x ^ unchecked((long)0x000411F744FB9EC2UL)); }
    [MethodImpl(NI)] static long K309() => unchecked((long)0x0004F680C8DEB800UL);
    [MethodImpl(NI)] static long A309(long x) => x + unchecked((long)0x0004F680C8DEB800UL);
    [MethodImpl(NI)] static long C309(long x) { long r = (x < unchecked((long)0x0004F680C8DEB800UL) ? 1 : 0) + ((ulong)x < 0x0004F680C8DEB800UL ? 2 : 0) + (x == unchecked((long)0x0004F680C8DEB800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0004F680C8DEB800UL)); return r * 31 + (x ^ unchecked((long)0x0004F680C8DEB800UL)); }
    [MethodImpl(NI)] static long K310() => unchecked((long)0x0004F680C8DEBDA2UL);
    [MethodImpl(NI)] static long A310(long x) => x + unchecked((long)0x0004F680C8DEBDA2UL);
    [MethodImpl(NI)] static long C310(long x) { long r = (x < unchecked((long)0x0004F680C8DEBDA2UL) ? 1 : 0) + ((ulong)x < 0x0004F680C8DEBDA2UL ? 2 : 0) + (x == unchecked((long)0x0004F680C8DEBDA2UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0004F680C8DEBDA2UL)); return r * 31 + (x ^ unchecked((long)0x0004F680C8DEBDA2UL)); }
    [MethodImpl(NI)] static long K311() => unchecked((long)0x00069FAF7762A46EUL);
    [MethodImpl(NI)] static long A311(long x) => x + unchecked((long)0x00069FAF7762A46EUL);
    [MethodImpl(NI)] static long C311(long x) { long r = (x < unchecked((long)0x00069FAF7762A46EUL) ? 1 : 0) + ((ulong)x < 0x00069FAF7762A46EUL ? 2 : 0) + (x == unchecked((long)0x00069FAF7762A46EUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00069FAF7762A46EUL)); return r * 31 + (x ^ unchecked((long)0x00069FAF7762A46EUL)); }
    [MethodImpl(NI)] static long K312() => unchecked((long)0x00069FAF7762A800UL);
    [MethodImpl(NI)] static long A312(long x) => x + unchecked((long)0x00069FAF7762A800UL);
    [MethodImpl(NI)] static long C312(long x) { long r = (x < unchecked((long)0x00069FAF7762A800UL) ? 1 : 0) + ((ulong)x < 0x00069FAF7762A800UL ? 2 : 0) + (x == unchecked((long)0x00069FAF7762A800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x00069FAF7762A800UL)); return r * 31 + (x ^ unchecked((long)0x00069FAF7762A800UL)); }
    [MethodImpl(NI)] static long K313() => unchecked((long)0x0007FFFF00000000UL);
    [MethodImpl(NI)] static long A313(long x) => x + unchecked((long)0x0007FFFF00000000UL);
    [MethodImpl(NI)] static long C313(long x) { long r = (x < unchecked((long)0x0007FFFF00000000UL) ? 1 : 0) + ((ulong)x < 0x0007FFFF00000000UL ? 2 : 0) + (x == unchecked((long)0x0007FFFF00000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0007FFFF00000000UL)); return r * 31 + (x ^ unchecked((long)0x0007FFFF00000000UL)); }
    [MethodImpl(NI)] static long K314() => unchecked((long)0x000E0F3B78B9D7FFUL);
    [MethodImpl(NI)] static long A314(long x) => x + unchecked((long)0x000E0F3B78B9D7FFUL);
    [MethodImpl(NI)] static long C314(long x) { long r = (x < unchecked((long)0x000E0F3B78B9D7FFUL) ? 1 : 0) + ((ulong)x < 0x000E0F3B78B9D7FFUL ? 2 : 0) + (x == unchecked((long)0x000E0F3B78B9D7FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000E0F3B78B9D7FFUL)); return r * 31 + (x ^ unchecked((long)0x000E0F3B78B9D7FFUL)); }
    [MethodImpl(NI)] static long K315() => unchecked((long)0x000E0F3B78B9D9DAUL);
    [MethodImpl(NI)] static long A315(long x) => x + unchecked((long)0x000E0F3B78B9D9DAUL);
    [MethodImpl(NI)] static long C315(long x) { long r = (x < unchecked((long)0x000E0F3B78B9D9DAUL) ? 1 : 0) + ((ulong)x < 0x000E0F3B78B9D9DAUL ? 2 : 0) + (x == unchecked((long)0x000E0F3B78B9D9DAUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x000E0F3B78B9D9DAUL)); return r * 31 + (x ^ unchecked((long)0x000E0F3B78B9D9DAUL)); }
    [MethodImpl(NI)] static long K316() => unchecked((long)0x0123456789ABCDEEUL);
    [MethodImpl(NI)] static long A316(long x) => x + unchecked((long)0x0123456789ABCDEEUL);
    [MethodImpl(NI)] static long C316(long x) { long r = (x < unchecked((long)0x0123456789ABCDEEUL) ? 1 : 0) + ((ulong)x < 0x0123456789ABCDEEUL ? 2 : 0) + (x == unchecked((long)0x0123456789ABCDEEUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0123456789ABCDEEUL)); return r * 31 + (x ^ unchecked((long)0x0123456789ABCDEEUL)); }
    [MethodImpl(NI)] static long K317() => unchecked((long)0x0123456789ABCDEFUL);
    [MethodImpl(NI)] static long A317(long x) => x + unchecked((long)0x0123456789ABCDEFUL);
    [MethodImpl(NI)] static long C317(long x) { long r = (x < unchecked((long)0x0123456789ABCDEFUL) ? 1 : 0) + ((ulong)x < 0x0123456789ABCDEFUL ? 2 : 0) + (x == unchecked((long)0x0123456789ABCDEFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0123456789ABCDEFUL)); return r * 31 + (x ^ unchecked((long)0x0123456789ABCDEFUL)); }
    [MethodImpl(NI)] static long K318() => unchecked((long)0x0123456789ABCDF0UL);
    [MethodImpl(NI)] static long A318(long x) => x + unchecked((long)0x0123456789ABCDF0UL);
    [MethodImpl(NI)] static long C318(long x) { long r = (x < unchecked((long)0x0123456789ABCDF0UL) ? 1 : 0) + ((ulong)x < 0x0123456789ABCDF0UL ? 2 : 0) + (x == unchecked((long)0x0123456789ABCDF0UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0123456789ABCDF0UL)); return r * 31 + (x ^ unchecked((long)0x0123456789ABCDF0UL)); }
    [MethodImpl(NI)] static long K319() => unchecked((long)0x091A280000000000UL);
    [MethodImpl(NI)] static long A319(long x) => x + unchecked((long)0x091A280000000000UL);
    [MethodImpl(NI)] static long C319(long x) { long r = (x < unchecked((long)0x091A280000000000UL) ? 1 : 0) + ((ulong)x < 0x091A280000000000UL ? 2 : 0) + (x == unchecked((long)0x091A280000000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x091A280000000000UL)); return r * 31 + (x ^ unchecked((long)0x091A280000000000UL)); }
    [MethodImpl(NI)] static long K320() => unchecked((long)0x0FFFFFFFEFFFFFFFUL);
    [MethodImpl(NI)] static long A320(long x) => x + unchecked((long)0x0FFFFFFFEFFFFFFFUL);
    [MethodImpl(NI)] static long C320(long x) { long r = (x < unchecked((long)0x0FFFFFFFEFFFFFFFUL) ? 1 : 0) + ((ulong)x < 0x0FFFFFFFEFFFFFFFUL ? 2 : 0) + (x == unchecked((long)0x0FFFFFFFEFFFFFFFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0FFFFFFFEFFFFFFFUL)); return r * 31 + (x ^ unchecked((long)0x0FFFFFFFEFFFFFFFUL)); }
    [MethodImpl(NI)] static long K321() => unchecked((long)0x0FFFFFFFF0000000UL);
    [MethodImpl(NI)] static long A321(long x) => x + unchecked((long)0x0FFFFFFFF0000000UL);
    [MethodImpl(NI)] static long C321(long x) { long r = (x < unchecked((long)0x0FFFFFFFF0000000UL) ? 1 : 0) + ((ulong)x < 0x0FFFFFFFF0000000UL ? 2 : 0) + (x == unchecked((long)0x0FFFFFFFF0000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0FFFFFFFF0000000UL)); return r * 31 + (x ^ unchecked((long)0x0FFFFFFFF0000000UL)); }
    [MethodImpl(NI)] static long K322() => unchecked((long)0x0FFFFFFFF0000001UL);
    [MethodImpl(NI)] static long A322(long x) => x + unchecked((long)0x0FFFFFFFF0000001UL);
    [MethodImpl(NI)] static long C322(long x) { long r = (x < unchecked((long)0x0FFFFFFFF0000001UL) ? 1 : 0) + ((ulong)x < 0x0FFFFFFFF0000001UL ? 2 : 0) + (x == unchecked((long)0x0FFFFFFFF0000001UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x0FFFFFFFF0000001UL)); return r * 31 + (x ^ unchecked((long)0x0FFFFFFFF0000001UL)); }
    [MethodImpl(NI)] static long K323() => unchecked((long)0x10D7FA4C26B687FFUL);
    [MethodImpl(NI)] static long A323(long x) => x + unchecked((long)0x10D7FA4C26B687FFUL);
    [MethodImpl(NI)] static long C323(long x) { long r = (x < unchecked((long)0x10D7FA4C26B687FFUL) ? 1 : 0) + ((ulong)x < 0x10D7FA4C26B687FFUL ? 2 : 0) + (x == unchecked((long)0x10D7FA4C26B687FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x10D7FA4C26B687FFUL)); return r * 31 + (x ^ unchecked((long)0x10D7FA4C26B687FFUL)); }
    [MethodImpl(NI)] static long K324() => unchecked((long)0x10D7FA4C26B68939UL);
    [MethodImpl(NI)] static long A324(long x) => x + unchecked((long)0x10D7FA4C26B68939UL);
    [MethodImpl(NI)] static long C324(long x) { long r = (x < unchecked((long)0x10D7FA4C26B68939UL) ? 1 : 0) + ((ulong)x < 0x10D7FA4C26B68939UL ? 2 : 0) + (x == unchecked((long)0x10D7FA4C26B68939UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x10D7FA4C26B68939UL)); return r * 31 + (x ^ unchecked((long)0x10D7FA4C26B68939UL)); }
    [MethodImpl(NI)] static long K325() => unchecked((long)0x1234500000000000UL);
    [MethodImpl(NI)] static long A325(long x) => x + unchecked((long)0x1234500000000000UL);
    [MethodImpl(NI)] static long C325(long x) { long r = (x < unchecked((long)0x1234500000000000UL) ? 1 : 0) + ((ulong)x < 0x1234500000000000UL ? 2 : 0) + (x == unchecked((long)0x1234500000000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x1234500000000000UL)); return r * 31 + (x ^ unchecked((long)0x1234500000000000UL)); }
    [MethodImpl(NI)] static long K326() => unchecked((long)0x12345677FFFFF800UL);
    [MethodImpl(NI)] static long A326(long x) => x + unchecked((long)0x12345677FFFFF800UL);
    [MethodImpl(NI)] static long C326(long x) { long r = (x < unchecked((long)0x12345677FFFFF800UL) ? 1 : 0) + ((ulong)x < 0x12345677FFFFF800UL ? 2 : 0) + (x == unchecked((long)0x12345677FFFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x12345677FFFFF800UL)); return r * 31 + (x ^ unchecked((long)0x12345677FFFFF800UL)); }
    [MethodImpl(NI)] static long K327() => unchecked((long)0x1234567800000000UL);
    [MethodImpl(NI)] static long A327(long x) => x + unchecked((long)0x1234567800000000UL);
    [MethodImpl(NI)] static long C327(long x) { long r = (x < unchecked((long)0x1234567800000000UL) ? 1 : 0) + ((ulong)x < 0x1234567800000000UL ? 2 : 0) + (x == unchecked((long)0x1234567800000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x1234567800000000UL)); return r * 31 + (x ^ unchecked((long)0x1234567800000000UL)); }
    [MethodImpl(NI)] static long K328() => unchecked((long)0x12345678000007FFUL);
    [MethodImpl(NI)] static long A328(long x) => x + unchecked((long)0x12345678000007FFUL);
    [MethodImpl(NI)] static long C328(long x) { long r = (x < unchecked((long)0x12345678000007FFUL) ? 1 : 0) + ((ulong)x < 0x12345678000007FFUL ? 2 : 0) + (x == unchecked((long)0x12345678000007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x12345678000007FFUL)); return r * 31 + (x ^ unchecked((long)0x12345678000007FFUL)); }
    [MethodImpl(NI)] static long K329() => unchecked((long)0x1234567812345678UL);
    [MethodImpl(NI)] static long A329(long x) => x + unchecked((long)0x1234567812345678UL);
    [MethodImpl(NI)] static long C329(long x) { long r = (x < unchecked((long)0x1234567812345678UL) ? 1 : 0) + ((ulong)x < 0x1234567812345678UL ? 2 : 0) + (x == unchecked((long)0x1234567812345678UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x1234567812345678UL)); return r * 31 + (x ^ unchecked((long)0x1234567812345678UL)); }
    [MethodImpl(NI)] static long K330() => unchecked((long)0x1F02501339E97642UL);
    [MethodImpl(NI)] static long A330(long x) => x + unchecked((long)0x1F02501339E97642UL);
    [MethodImpl(NI)] static long C330(long x) { long r = (x < unchecked((long)0x1F02501339E97642UL) ? 1 : 0) + ((ulong)x < 0x1F02501339E97642UL ? 2 : 0) + (x == unchecked((long)0x1F02501339E97642UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x1F02501339E97642UL)); return r * 31 + (x ^ unchecked((long)0x1F02501339E97642UL)); }
    [MethodImpl(NI)] static long K331() => unchecked((long)0x1F02501339E97FFFUL);
    [MethodImpl(NI)] static long A331(long x) => x + unchecked((long)0x1F02501339E97FFFUL);
    [MethodImpl(NI)] static long C331(long x) { long r = (x < unchecked((long)0x1F02501339E97FFFUL) ? 1 : 0) + ((ulong)x < 0x1F02501339E97FFFUL ? 2 : 0) + (x == unchecked((long)0x1F02501339E97FFFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x1F02501339E97FFFUL)); return r * 31 + (x ^ unchecked((long)0x1F02501339E97FFFUL)); }
    [MethodImpl(NI)] static long K332() => unchecked((long)0x2000000000000000UL);
    [MethodImpl(NI)] static long A332(long x) => x + unchecked((long)0x2000000000000000UL);
    [MethodImpl(NI)] static long C332(long x) { long r = (x < unchecked((long)0x2000000000000000UL) ? 1 : 0) + ((ulong)x < 0x2000000000000000UL ? 2 : 0) + (x == unchecked((long)0x2000000000000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x2000000000000000UL)); return r * 31 + (x ^ unchecked((long)0x2000000000000000UL)); }
    [MethodImpl(NI)] static long K333() => unchecked((long)0x2FFFBFFFFFFFF800UL);
    [MethodImpl(NI)] static long A333(long x) => x + unchecked((long)0x2FFFBFFFFFFFF800UL);
    [MethodImpl(NI)] static long C333(long x) { long r = (x < unchecked((long)0x2FFFBFFFFFFFF800UL) ? 1 : 0) + ((ulong)x < 0x2FFFBFFFFFFFF800UL ? 2 : 0) + (x == unchecked((long)0x2FFFBFFFFFFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x2FFFBFFFFFFFF800UL)); return r * 31 + (x ^ unchecked((long)0x2FFFBFFFFFFFF800UL)); }
    [MethodImpl(NI)] static long K334() => unchecked((long)0x2FFFC00000000000UL);
    [MethodImpl(NI)] static long A334(long x) => x + unchecked((long)0x2FFFC00000000000UL);
    [MethodImpl(NI)] static long C334(long x) { long r = (x < unchecked((long)0x2FFFC00000000000UL) ? 1 : 0) + ((ulong)x < 0x2FFFC00000000000UL ? 2 : 0) + (x == unchecked((long)0x2FFFC00000000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x2FFFC00000000000UL)); return r * 31 + (x ^ unchecked((long)0x2FFFC00000000000UL)); }
    [MethodImpl(NI)] static long K335() => unchecked((long)0x2FFFC000000007FFUL);
    [MethodImpl(NI)] static long A335(long x) => x + unchecked((long)0x2FFFC000000007FFUL);
    [MethodImpl(NI)] static long C335(long x) { long r = (x < unchecked((long)0x2FFFC000000007FFUL) ? 1 : 0) + ((ulong)x < 0x2FFFC000000007FFUL ? 2 : 0) + (x == unchecked((long)0x2FFFC000000007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x2FFFC000000007FFUL)); return r * 31 + (x ^ unchecked((long)0x2FFFC000000007FFUL)); }
    [MethodImpl(NI)] static long K336() => unchecked((long)0x2FFFC0002FFFC000UL);
    [MethodImpl(NI)] static long A336(long x) => x + unchecked((long)0x2FFFC0002FFFC000UL);
    [MethodImpl(NI)] static long C336(long x) { long r = (x < unchecked((long)0x2FFFC0002FFFC000UL) ? 1 : 0) + ((ulong)x < 0x2FFFC0002FFFC000UL ? 2 : 0) + (x == unchecked((long)0x2FFFC0002FFFC000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x2FFFC0002FFFC000UL)); return r * 31 + (x ^ unchecked((long)0x2FFFC0002FFFC000UL)); }
    [MethodImpl(NI)] static long K337() => unchecked((long)0x3FFFF7FFFFFFF800UL);
    [MethodImpl(NI)] static long A337(long x) => x + unchecked((long)0x3FFFF7FFFFFFF800UL);
    [MethodImpl(NI)] static long C337(long x) { long r = (x < unchecked((long)0x3FFFF7FFFFFFF800UL) ? 1 : 0) + ((ulong)x < 0x3FFFF7FFFFFFF800UL ? 2 : 0) + (x == unchecked((long)0x3FFFF7FFFFFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x3FFFF7FFFFFFF800UL)); return r * 31 + (x ^ unchecked((long)0x3FFFF7FFFFFFF800UL)); }
    [MethodImpl(NI)] static long K338() => unchecked((long)0x3FFFF80000000000UL);
    [MethodImpl(NI)] static long A338(long x) => x + unchecked((long)0x3FFFF80000000000UL);
    [MethodImpl(NI)] static long C338(long x) { long r = (x < unchecked((long)0x3FFFF80000000000UL) ? 1 : 0) + ((ulong)x < 0x3FFFF80000000000UL ? 2 : 0) + (x == unchecked((long)0x3FFFF80000000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x3FFFF80000000000UL)); return r * 31 + (x ^ unchecked((long)0x3FFFF80000000000UL)); }
    [MethodImpl(NI)] static long K339() => unchecked((long)0x3FFFF800000007FFUL);
    [MethodImpl(NI)] static long A339(long x) => x + unchecked((long)0x3FFFF800000007FFUL);
    [MethodImpl(NI)] static long C339(long x) { long r = (x < unchecked((long)0x3FFFF800000007FFUL) ? 1 : 0) + ((ulong)x < 0x3FFFF800000007FFUL ? 2 : 0) + (x == unchecked((long)0x3FFFF800000007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x3FFFF800000007FFUL)); return r * 31 + (x ^ unchecked((long)0x3FFFF800000007FFUL)); }
    [MethodImpl(NI)] static long K340() => unchecked((long)0x3FFFF8003FFFF800UL);
    [MethodImpl(NI)] static long A340(long x) => x + unchecked((long)0x3FFFF8003FFFF800UL);
    [MethodImpl(NI)] static long C340(long x) { long r = (x < unchecked((long)0x3FFFF8003FFFF800UL) ? 1 : 0) + ((ulong)x < 0x3FFFF8003FFFF800UL ? 2 : 0) + (x == unchecked((long)0x3FFFF8003FFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x3FFFF8003FFFF800UL)); return r * 31 + (x ^ unchecked((long)0x3FFFF8003FFFF800UL)); }
    [MethodImpl(NI)] static long K341() => unchecked((long)0x4000000000000000UL);
    [MethodImpl(NI)] static long A341(long x) => x + unchecked((long)0x4000000000000000UL);
    [MethodImpl(NI)] static long C341(long x) { long r = (x < unchecked((long)0x4000000000000000UL) ? 1 : 0) + ((ulong)x < 0x4000000000000000UL ? 2 : 0) + (x == unchecked((long)0x4000000000000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x4000000000000000UL)); return r * 31 + (x ^ unchecked((long)0x4000000000000000UL)); }
    [MethodImpl(NI)] static long K342() => unchecked((long)0x5555555555555554UL);
    [MethodImpl(NI)] static long A342(long x) => x + unchecked((long)0x5555555555555554UL);
    [MethodImpl(NI)] static long C342(long x) { long r = (x < unchecked((long)0x5555555555555554UL) ? 1 : 0) + ((ulong)x < 0x5555555555555554UL ? 2 : 0) + (x == unchecked((long)0x5555555555555554UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x5555555555555554UL)); return r * 31 + (x ^ unchecked((long)0x5555555555555554UL)); }
    [MethodImpl(NI)] static long K343() => unchecked((long)0x5555555555555555UL);
    [MethodImpl(NI)] static long A343(long x) => x + unchecked((long)0x5555555555555555UL);
    [MethodImpl(NI)] static long C343(long x) { long r = (x < unchecked((long)0x5555555555555555UL) ? 1 : 0) + ((ulong)x < 0x5555555555555555UL ? 2 : 0) + (x == unchecked((long)0x5555555555555555UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x5555555555555555UL)); return r * 31 + (x ^ unchecked((long)0x5555555555555555UL)); }
    [MethodImpl(NI)] static long K344() => unchecked((long)0x5555555555555556UL);
    [MethodImpl(NI)] static long A344(long x) => x + unchecked((long)0x5555555555555556UL);
    [MethodImpl(NI)] static long C344(long x) { long r = (x < unchecked((long)0x5555555555555556UL) ? 1 : 0) + ((ulong)x < 0x5555555555555556UL ? 2 : 0) + (x == unchecked((long)0x5555555555555556UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x5555555555555556UL)); return r * 31 + (x ^ unchecked((long)0x5555555555555556UL)); }
    [MethodImpl(NI)] static long K345() => unchecked((long)0x6206E2DB648AC7FFUL);
    [MethodImpl(NI)] static long A345(long x) => x + unchecked((long)0x6206E2DB648AC7FFUL);
    [MethodImpl(NI)] static long C345(long x) { long r = (x < unchecked((long)0x6206E2DB648AC7FFUL) ? 1 : 0) + ((ulong)x < 0x6206E2DB648AC7FFUL ? 2 : 0) + (x == unchecked((long)0x6206E2DB648AC7FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x6206E2DB648AC7FFUL)); return r * 31 + (x ^ unchecked((long)0x6206E2DB648AC7FFUL)); }
    [MethodImpl(NI)] static long K346() => unchecked((long)0x6206E2DB648ACA5FUL);
    [MethodImpl(NI)] static long A346(long x) => x + unchecked((long)0x6206E2DB648ACA5FUL);
    [MethodImpl(NI)] static long C346(long x) { long r = (x < unchecked((long)0x6206E2DB648ACA5FUL) ? 1 : 0) + ((ulong)x < 0x6206E2DB648ACA5FUL ? 2 : 0) + (x == unchecked((long)0x6206E2DB648ACA5FUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x6206E2DB648ACA5FUL)); return r * 31 + (x ^ unchecked((long)0x6206E2DB648ACA5FUL)); }
    [MethodImpl(NI)] static long K347() => unchecked((long)0x6A0A5C690030C46CUL);
    [MethodImpl(NI)] static long A347(long x) => x + unchecked((long)0x6A0A5C690030C46CUL);
    [MethodImpl(NI)] static long C347(long x) { long r = (x < unchecked((long)0x6A0A5C690030C46CUL) ? 1 : 0) + ((ulong)x < 0x6A0A5C690030C46CUL ? 2 : 0) + (x == unchecked((long)0x6A0A5C690030C46CUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x6A0A5C690030C46CUL)); return r * 31 + (x ^ unchecked((long)0x6A0A5C690030C46CUL)); }
    [MethodImpl(NI)] static long K348() => unchecked((long)0x6A0A5C690030C7FFUL);
    [MethodImpl(NI)] static long A348(long x) => x + unchecked((long)0x6A0A5C690030C7FFUL);
    [MethodImpl(NI)] static long C348(long x) { long r = (x < unchecked((long)0x6A0A5C690030C7FFUL) ? 1 : 0) + ((ulong)x < 0x6A0A5C690030C7FFUL ? 2 : 0) + (x == unchecked((long)0x6A0A5C690030C7FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x6A0A5C690030C7FFUL)); return r * 31 + (x ^ unchecked((long)0x6A0A5C690030C7FFUL)); }
    [MethodImpl(NI)] static long K349() => unchecked((long)0x72E0DA84AC51D587UL);
    [MethodImpl(NI)] static long A349(long x) => x + unchecked((long)0x72E0DA84AC51D587UL);
    [MethodImpl(NI)] static long C349(long x) { long r = (x < unchecked((long)0x72E0DA84AC51D587UL) ? 1 : 0) + ((ulong)x < 0x72E0DA84AC51D587UL ? 2 : 0) + (x == unchecked((long)0x72E0DA84AC51D587UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x72E0DA84AC51D587UL)); return r * 31 + (x ^ unchecked((long)0x72E0DA84AC51D587UL)); }
    [MethodImpl(NI)] static long K350() => unchecked((long)0x72E0DA84AC51D7FFUL);
    [MethodImpl(NI)] static long A350(long x) => x + unchecked((long)0x72E0DA84AC51D7FFUL);
    [MethodImpl(NI)] static long C350(long x) { long r = (x < unchecked((long)0x72E0DA84AC51D7FFUL) ? 1 : 0) + ((ulong)x < 0x72E0DA84AC51D7FFUL ? 2 : 0) + (x == unchecked((long)0x72E0DA84AC51D7FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x72E0DA84AC51D7FFUL)); return r * 31 + (x ^ unchecked((long)0x72E0DA84AC51D7FFUL)); }
    [MethodImpl(NI)] static long K351() => unchecked((long)0x739B8000FC8005F3UL);
    [MethodImpl(NI)] static long A351(long x) => x + unchecked((long)0x739B8000FC8005F3UL);
    [MethodImpl(NI)] static long C351(long x) { long r = (x < unchecked((long)0x739B8000FC8005F3UL) ? 1 : 0) + ((ulong)x < 0x739B8000FC8005F3UL ? 2 : 0) + (x == unchecked((long)0x739B8000FC8005F3UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x739B8000FC8005F3UL)); return r * 31 + (x ^ unchecked((long)0x739B8000FC8005F3UL)); }
    [MethodImpl(NI)] static long K352() => unchecked((long)0x739B8000FC8005F4UL);
    [MethodImpl(NI)] static long A352(long x) => x + unchecked((long)0x739B8000FC8005F4UL);
    [MethodImpl(NI)] static long C352(long x) { long r = (x < unchecked((long)0x739B8000FC8005F4UL) ? 1 : 0) + ((ulong)x < 0x739B8000FC8005F4UL ? 2 : 0) + (x == unchecked((long)0x739B8000FC8005F4UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x739B8000FC8005F4UL)); return r * 31 + (x ^ unchecked((long)0x739B8000FC8005F4UL)); }
    [MethodImpl(NI)] static long K353() => unchecked((long)0x739B8000FC8005F5UL);
    [MethodImpl(NI)] static long A353(long x) => x + unchecked((long)0x739B8000FC8005F5UL);
    [MethodImpl(NI)] static long C353(long x) { long r = (x < unchecked((long)0x739B8000FC8005F5UL) ? 1 : 0) + ((ulong)x < 0x739B8000FC8005F5UL ? 2 : 0) + (x == unchecked((long)0x739B8000FC8005F5UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x739B8000FC8005F5UL)); return r * 31 + (x ^ unchecked((long)0x739B8000FC8005F5UL)); }
    [MethodImpl(NI)] static long K354() => unchecked((long)0x7D4CDD8A2E36C590UL);
    [MethodImpl(NI)] static long A354(long x) => x + unchecked((long)0x7D4CDD8A2E36C590UL);
    [MethodImpl(NI)] static long C354(long x) { long r = (x < unchecked((long)0x7D4CDD8A2E36C590UL) ? 1 : 0) + ((ulong)x < 0x7D4CDD8A2E36C590UL ? 2 : 0) + (x == unchecked((long)0x7D4CDD8A2E36C590UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7D4CDD8A2E36C590UL)); return r * 31 + (x ^ unchecked((long)0x7D4CDD8A2E36C590UL)); }
    [MethodImpl(NI)] static long K355() => unchecked((long)0x7D4CDD8A2E36C800UL);
    [MethodImpl(NI)] static long A355(long x) => x + unchecked((long)0x7D4CDD8A2E36C800UL);
    [MethodImpl(NI)] static long C355(long x) { long r = (x < unchecked((long)0x7D4CDD8A2E36C800UL) ? 1 : 0) + ((ulong)x < 0x7D4CDD8A2E36C800UL ? 2 : 0) + (x == unchecked((long)0x7D4CDD8A2E36C800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7D4CDD8A2E36C800UL)); return r * 31 + (x ^ unchecked((long)0x7D4CDD8A2E36C800UL)); }
    [MethodImpl(NI)] static long K356() => unchecked((long)0x7FF007FEFFFFF800UL);
    [MethodImpl(NI)] static long A356(long x) => x + unchecked((long)0x7FF007FEFFFFF800UL);
    [MethodImpl(NI)] static long C356(long x) { long r = (x < unchecked((long)0x7FF007FEFFFFF800UL) ? 1 : 0) + ((ulong)x < 0x7FF007FEFFFFF800UL ? 2 : 0) + (x == unchecked((long)0x7FF007FEFFFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7FF007FEFFFFF800UL)); return r * 31 + (x ^ unchecked((long)0x7FF007FEFFFFF800UL)); }
    [MethodImpl(NI)] static long K357() => unchecked((long)0x7FF007FF00000000UL);
    [MethodImpl(NI)] static long A357(long x) => x + unchecked((long)0x7FF007FF00000000UL);
    [MethodImpl(NI)] static long C357(long x) { long r = (x < unchecked((long)0x7FF007FF00000000UL) ? 1 : 0) + ((ulong)x < 0x7FF007FF00000000UL ? 2 : 0) + (x == unchecked((long)0x7FF007FF00000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7FF007FF00000000UL)); return r * 31 + (x ^ unchecked((long)0x7FF007FF00000000UL)); }
    [MethodImpl(NI)] static long K358() => unchecked((long)0x7FF007FF000007FFUL);
    [MethodImpl(NI)] static long A358(long x) => x + unchecked((long)0x7FF007FF000007FFUL);
    [MethodImpl(NI)] static long C358(long x) { long r = (x < unchecked((long)0x7FF007FF000007FFUL) ? 1 : 0) + ((ulong)x < 0x7FF007FF000007FFUL ? 2 : 0) + (x == unchecked((long)0x7FF007FF000007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7FF007FF000007FFUL)); return r * 31 + (x ^ unchecked((long)0x7FF007FF000007FFUL)); }
    [MethodImpl(NI)] static long K359() => unchecked((long)0x7FF007FF7FF007FFUL);
    [MethodImpl(NI)] static long A359(long x) => x + unchecked((long)0x7FF007FF7FF007FFUL);
    [MethodImpl(NI)] static long C359(long x) { long r = (x < unchecked((long)0x7FF007FF7FF007FFUL) ? 1 : 0) + ((ulong)x < 0x7FF007FF7FF007FFUL ? 2 : 0) + (x == unchecked((long)0x7FF007FF7FF007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7FF007FF7FF007FFUL)); return r * 31 + (x ^ unchecked((long)0x7FF007FF7FF007FFUL)); }
    [MethodImpl(NI)] static long K360() => unchecked((long)0x7FFFEFFFFFFFF800UL);
    [MethodImpl(NI)] static long A360(long x) => x + unchecked((long)0x7FFFEFFFFFFFF800UL);
    [MethodImpl(NI)] static long C360(long x) { long r = (x < unchecked((long)0x7FFFEFFFFFFFF800UL) ? 1 : 0) + ((ulong)x < 0x7FFFEFFFFFFFF800UL ? 2 : 0) + (x == unchecked((long)0x7FFFEFFFFFFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7FFFEFFFFFFFF800UL)); return r * 31 + (x ^ unchecked((long)0x7FFFEFFFFFFFF800UL)); }
    [MethodImpl(NI)] static long K361() => unchecked((long)0x7FFFF00000000000UL);
    [MethodImpl(NI)] static long A361(long x) => x + unchecked((long)0x7FFFF00000000000UL);
    [MethodImpl(NI)] static long C361(long x) { long r = (x < unchecked((long)0x7FFFF00000000000UL) ? 1 : 0) + ((ulong)x < 0x7FFFF00000000000UL ? 2 : 0) + (x == unchecked((long)0x7FFFF00000000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7FFFF00000000000UL)); return r * 31 + (x ^ unchecked((long)0x7FFFF00000000000UL)); }
    [MethodImpl(NI)] static long K362() => unchecked((long)0x7FFFF000000007FFUL);
    [MethodImpl(NI)] static long A362(long x) => x + unchecked((long)0x7FFFF000000007FFUL);
    [MethodImpl(NI)] static long C362(long x) { long r = (x < unchecked((long)0x7FFFF000000007FFUL) ? 1 : 0) + ((ulong)x < 0x7FFFF000000007FFUL ? 2 : 0) + (x == unchecked((long)0x7FFFF000000007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7FFFF000000007FFUL)); return r * 31 + (x ^ unchecked((long)0x7FFFF000000007FFUL)); }
    [MethodImpl(NI)] static long K363() => unchecked((long)0x7FFFF0007FFFF000UL);
    [MethodImpl(NI)] static long A363(long x) => x + unchecked((long)0x7FFFF0007FFFF000UL);
    [MethodImpl(NI)] static long C363(long x) { long r = (x < unchecked((long)0x7FFFF0007FFFF000UL) ? 1 : 0) + ((ulong)x < 0x7FFFF0007FFFF000UL ? 2 : 0) + (x == unchecked((long)0x7FFFF0007FFFF000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7FFFF0007FFFF000UL)); return r * 31 + (x ^ unchecked((long)0x7FFFF0007FFFF000UL)); }
    [MethodImpl(NI)] static long K364() => unchecked((long)0x7FFFF7FEFFFFF800UL);
    [MethodImpl(NI)] static long A364(long x) => x + unchecked((long)0x7FFFF7FEFFFFF800UL);
    [MethodImpl(NI)] static long C364(long x) { long r = (x < unchecked((long)0x7FFFF7FEFFFFF800UL) ? 1 : 0) + ((ulong)x < 0x7FFFF7FEFFFFF800UL ? 2 : 0) + (x == unchecked((long)0x7FFFF7FEFFFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7FFFF7FEFFFFF800UL)); return r * 31 + (x ^ unchecked((long)0x7FFFF7FEFFFFF800UL)); }
    [MethodImpl(NI)] static long K365() => unchecked((long)0x7FFFF7FF00000000UL);
    [MethodImpl(NI)] static long A365(long x) => x + unchecked((long)0x7FFFF7FF00000000UL);
    [MethodImpl(NI)] static long C365(long x) { long r = (x < unchecked((long)0x7FFFF7FF00000000UL) ? 1 : 0) + ((ulong)x < 0x7FFFF7FF00000000UL ? 2 : 0) + (x == unchecked((long)0x7FFFF7FF00000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7FFFF7FF00000000UL)); return r * 31 + (x ^ unchecked((long)0x7FFFF7FF00000000UL)); }
    [MethodImpl(NI)] static long K366() => unchecked((long)0x7FFFF7FF000007FFUL);
    [MethodImpl(NI)] static long A366(long x) => x + unchecked((long)0x7FFFF7FF000007FFUL);
    [MethodImpl(NI)] static long C366(long x) { long r = (x < unchecked((long)0x7FFFF7FF000007FFUL) ? 1 : 0) + ((ulong)x < 0x7FFFF7FF000007FFUL ? 2 : 0) + (x == unchecked((long)0x7FFFF7FF000007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7FFFF7FF000007FFUL)); return r * 31 + (x ^ unchecked((long)0x7FFFF7FF000007FFUL)); }
    [MethodImpl(NI)] static long K367() => unchecked((long)0x7FFFF7FF7FFFF7FFUL);
    [MethodImpl(NI)] static long A367(long x) => x + unchecked((long)0x7FFFF7FF7FFFF7FFUL);
    [MethodImpl(NI)] static long C367(long x) { long r = (x < unchecked((long)0x7FFFF7FF7FFFF7FFUL) ? 1 : 0) + ((ulong)x < 0x7FFFF7FF7FFFF7FFUL ? 2 : 0) + (x == unchecked((long)0x7FFFF7FF7FFFF7FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7FFFF7FF7FFFF7FFUL)); return r * 31 + (x ^ unchecked((long)0x7FFFF7FF7FFFF7FFUL)); }
    [MethodImpl(NI)] static long K368() => unchecked((long)0x7FFFF7FFFFFFF800UL);
    [MethodImpl(NI)] static long A368(long x) => x + unchecked((long)0x7FFFF7FFFFFFF800UL);
    [MethodImpl(NI)] static long C368(long x) { long r = (x < unchecked((long)0x7FFFF7FFFFFFF800UL) ? 1 : 0) + ((ulong)x < 0x7FFFF7FFFFFFF800UL ? 2 : 0) + (x == unchecked((long)0x7FFFF7FFFFFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7FFFF7FFFFFFF800UL)); return r * 31 + (x ^ unchecked((long)0x7FFFF7FFFFFFF800UL)); }
    [MethodImpl(NI)] static long K369() => unchecked((long)0x7FFFF7FFFFFFFFFEUL);
    [MethodImpl(NI)] static long A369(long x) => x + unchecked((long)0x7FFFF7FFFFFFFFFEUL);
    [MethodImpl(NI)] static long C369(long x) { long r = (x < unchecked((long)0x7FFFF7FFFFFFFFFEUL) ? 1 : 0) + ((ulong)x < 0x7FFFF7FFFFFFFFFEUL ? 2 : 0) + (x == unchecked((long)0x7FFFF7FFFFFFFFFEUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7FFFF7FFFFFFFFFEUL)); return r * 31 + (x ^ unchecked((long)0x7FFFF7FFFFFFFFFEUL)); }
    [MethodImpl(NI)] static long K370() => unchecked((long)0x7FFFF7FFFFFFFFFFUL);
    [MethodImpl(NI)] static long A370(long x) => x + unchecked((long)0x7FFFF7FFFFFFFFFFUL);
    [MethodImpl(NI)] static long C370(long x) { long r = (x < unchecked((long)0x7FFFF7FFFFFFFFFFUL) ? 1 : 0) + ((ulong)x < 0x7FFFF7FFFFFFFFFFUL ? 2 : 0) + (x == unchecked((long)0x7FFFF7FFFFFFFFFFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7FFFF7FFFFFFFFFFUL)); return r * 31 + (x ^ unchecked((long)0x7FFFF7FFFFFFFFFFUL)); }
    [MethodImpl(NI)] static long K371() => unchecked((long)0x7FFFF80000000000UL);
    [MethodImpl(NI)] static long A371(long x) => x + unchecked((long)0x7FFFF80000000000UL);
    [MethodImpl(NI)] static long C371(long x) { long r = (x < unchecked((long)0x7FFFF80000000000UL) ? 1 : 0) + ((ulong)x < 0x7FFFF80000000000UL ? 2 : 0) + (x == unchecked((long)0x7FFFF80000000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7FFFF80000000000UL)); return r * 31 + (x ^ unchecked((long)0x7FFFF80000000000UL)); }
    [MethodImpl(NI)] static long K372() => unchecked((long)0x7FFFF80000000001UL);
    [MethodImpl(NI)] static long A372(long x) => x + unchecked((long)0x7FFFF80000000001UL);
    [MethodImpl(NI)] static long C372(long x) { long r = (x < unchecked((long)0x7FFFF80000000001UL) ? 1 : 0) + ((ulong)x < 0x7FFFF80000000001UL ? 2 : 0) + (x == unchecked((long)0x7FFFF80000000001UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7FFFF80000000001UL)); return r * 31 + (x ^ unchecked((long)0x7FFFF80000000001UL)); }
    [MethodImpl(NI)] static long K373() => unchecked((long)0x7FFFF800000007FFUL);
    [MethodImpl(NI)] static long A373(long x) => x + unchecked((long)0x7FFFF800000007FFUL);
    [MethodImpl(NI)] static long C373(long x) { long r = (x < unchecked((long)0x7FFFF800000007FFUL) ? 1 : 0) + ((ulong)x < 0x7FFFF800000007FFUL ? 2 : 0) + (x == unchecked((long)0x7FFFF800000007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7FFFF800000007FFUL)); return r * 31 + (x ^ unchecked((long)0x7FFFF800000007FFUL)); }
    [MethodImpl(NI)] static long K374() => unchecked((long)0x7FFFF8007FFFF7FFUL);
    [MethodImpl(NI)] static long A374(long x) => x + unchecked((long)0x7FFFF8007FFFF7FFUL);
    [MethodImpl(NI)] static long C374(long x) { long r = (x < unchecked((long)0x7FFFF8007FFFF7FFUL) ? 1 : 0) + ((ulong)x < 0x7FFFF8007FFFF7FFUL ? 2 : 0) + (x == unchecked((long)0x7FFFF8007FFFF7FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7FFFF8007FFFF7FFUL)); return r * 31 + (x ^ unchecked((long)0x7FFFF8007FFFF7FFUL)); }
    [MethodImpl(NI)] static long K375() => unchecked((long)0x7FFFF8007FFFF800UL);
    [MethodImpl(NI)] static long A375(long x) => x + unchecked((long)0x7FFFF8007FFFF800UL);
    [MethodImpl(NI)] static long C375(long x) { long r = (x < unchecked((long)0x7FFFF8007FFFF800UL) ? 1 : 0) + ((ulong)x < 0x7FFFF8007FFFF800UL ? 2 : 0) + (x == unchecked((long)0x7FFFF8007FFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7FFFF8007FFFF800UL)); return r * 31 + (x ^ unchecked((long)0x7FFFF8007FFFF800UL)); }
    [MethodImpl(NI)] static long K376() => unchecked((long)0x7FFFF8007FFFF801UL);
    [MethodImpl(NI)] static long A376(long x) => x + unchecked((long)0x7FFFF8007FFFF801UL);
    [MethodImpl(NI)] static long C376(long x) { long r = (x < unchecked((long)0x7FFFF8007FFFF801UL) ? 1 : 0) + ((ulong)x < 0x7FFFF8007FFFF801UL ? 2 : 0) + (x == unchecked((long)0x7FFFF8007FFFF801UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7FFFF8007FFFF801UL)); return r * 31 + (x ^ unchecked((long)0x7FFFF8007FFFF801UL)); }
    [MethodImpl(NI)] static long K377() => unchecked((long)0x7FFFF800FFFFF800UL);
    [MethodImpl(NI)] static long A377(long x) => x + unchecked((long)0x7FFFF800FFFFF800UL);
    [MethodImpl(NI)] static long C377(long x) { long r = (x < unchecked((long)0x7FFFF800FFFFF800UL) ? 1 : 0) + ((ulong)x < 0x7FFFF800FFFFF800UL ? 2 : 0) + (x == unchecked((long)0x7FFFF800FFFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7FFFF800FFFFF800UL)); return r * 31 + (x ^ unchecked((long)0x7FFFF800FFFFF800UL)); }
    [MethodImpl(NI)] static long K378() => unchecked((long)0x7FFFF80100000000UL);
    [MethodImpl(NI)] static long A378(long x) => x + unchecked((long)0x7FFFF80100000000UL);
    [MethodImpl(NI)] static long C378(long x) { long r = (x < unchecked((long)0x7FFFF80100000000UL) ? 1 : 0) + ((ulong)x < 0x7FFFF80100000000UL ? 2 : 0) + (x == unchecked((long)0x7FFFF80100000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7FFFF80100000000UL)); return r * 31 + (x ^ unchecked((long)0x7FFFF80100000000UL)); }
    [MethodImpl(NI)] static long K379() => unchecked((long)0x7FFFF801000007FFUL);
    [MethodImpl(NI)] static long A379(long x) => x + unchecked((long)0x7FFFF801000007FFUL);
    [MethodImpl(NI)] static long C379(long x) { long r = (x < unchecked((long)0x7FFFF801000007FFUL) ? 1 : 0) + ((ulong)x < 0x7FFFF801000007FFUL ? 2 : 0) + (x == unchecked((long)0x7FFFF801000007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7FFFF801000007FFUL)); return r * 31 + (x ^ unchecked((long)0x7FFFF801000007FFUL)); }
    [MethodImpl(NI)] static long K380() => unchecked((long)0x7FFFF8017FFFF801UL);
    [MethodImpl(NI)] static long A380(long x) => x + unchecked((long)0x7FFFF8017FFFF801UL);
    [MethodImpl(NI)] static long C380(long x) { long r = (x < unchecked((long)0x7FFFF8017FFFF801UL) ? 1 : 0) + ((ulong)x < 0x7FFFF8017FFFF801UL ? 2 : 0) + (x == unchecked((long)0x7FFFF8017FFFF801UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7FFFF8017FFFF801UL)); return r * 31 + (x ^ unchecked((long)0x7FFFF8017FFFF801UL)); }
    [MethodImpl(NI)] static long K381() => unchecked((long)0x7FFFFFFEFFFFF800UL);
    [MethodImpl(NI)] static long A381(long x) => x + unchecked((long)0x7FFFFFFEFFFFF800UL);
    [MethodImpl(NI)] static long C381(long x) { long r = (x < unchecked((long)0x7FFFFFFEFFFFF800UL) ? 1 : 0) + ((ulong)x < 0x7FFFFFFEFFFFF800UL ? 2 : 0) + (x == unchecked((long)0x7FFFFFFEFFFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7FFFFFFEFFFFF800UL)); return r * 31 + (x ^ unchecked((long)0x7FFFFFFEFFFFF800UL)); }
    [MethodImpl(NI)] static long K382() => unchecked((long)0x7FFFFFFEFFFFFFFFUL);
    [MethodImpl(NI)] static long A382(long x) => x + unchecked((long)0x7FFFFFFEFFFFFFFFUL);
    [MethodImpl(NI)] static long C382(long x) { long r = (x < unchecked((long)0x7FFFFFFEFFFFFFFFUL) ? 1 : 0) + ((ulong)x < 0x7FFFFFFEFFFFFFFFUL ? 2 : 0) + (x == unchecked((long)0x7FFFFFFEFFFFFFFFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7FFFFFFEFFFFFFFFUL)); return r * 31 + (x ^ unchecked((long)0x7FFFFFFEFFFFFFFFUL)); }
    [MethodImpl(NI)] static long K383() => unchecked((long)0x7FFFFFFF00000000UL);
    [MethodImpl(NI)] static long A383(long x) => x + unchecked((long)0x7FFFFFFF00000000UL);
    [MethodImpl(NI)] static long C383(long x) { long r = (x < unchecked((long)0x7FFFFFFF00000000UL) ? 1 : 0) + ((ulong)x < 0x7FFFFFFF00000000UL ? 2 : 0) + (x == unchecked((long)0x7FFFFFFF00000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7FFFFFFF00000000UL)); return r * 31 + (x ^ unchecked((long)0x7FFFFFFF00000000UL)); }
    [MethodImpl(NI)] static long K384() => unchecked((long)0x7FFFFFFF00000001UL);
    [MethodImpl(NI)] static long A384(long x) => x + unchecked((long)0x7FFFFFFF00000001UL);
    [MethodImpl(NI)] static long C384(long x) { long r = (x < unchecked((long)0x7FFFFFFF00000001UL) ? 1 : 0) + ((ulong)x < 0x7FFFFFFF00000001UL ? 2 : 0) + (x == unchecked((long)0x7FFFFFFF00000001UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7FFFFFFF00000001UL)); return r * 31 + (x ^ unchecked((long)0x7FFFFFFF00000001UL)); }
    [MethodImpl(NI)] static long K385() => unchecked((long)0x7FFFFFFF000007FFUL);
    [MethodImpl(NI)] static long A385(long x) => x + unchecked((long)0x7FFFFFFF000007FFUL);
    [MethodImpl(NI)] static long C385(long x) { long r = (x < unchecked((long)0x7FFFFFFF000007FFUL) ? 1 : 0) + ((ulong)x < 0x7FFFFFFF000007FFUL ? 2 : 0) + (x == unchecked((long)0x7FFFFFFF000007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7FFFFFFF000007FFUL)); return r * 31 + (x ^ unchecked((long)0x7FFFFFFF000007FFUL)); }
    [MethodImpl(NI)] static long K386() => unchecked((long)0x7FFFFFFF7FFFFFFEUL);
    [MethodImpl(NI)] static long A386(long x) => x + unchecked((long)0x7FFFFFFF7FFFFFFEUL);
    [MethodImpl(NI)] static long C386(long x) { long r = (x < unchecked((long)0x7FFFFFFF7FFFFFFEUL) ? 1 : 0) + ((ulong)x < 0x7FFFFFFF7FFFFFFEUL ? 2 : 0) + (x == unchecked((long)0x7FFFFFFF7FFFFFFEUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7FFFFFFF7FFFFFFEUL)); return r * 31 + (x ^ unchecked((long)0x7FFFFFFF7FFFFFFEUL)); }
    [MethodImpl(NI)] static long K387() => unchecked((long)0x7FFFFFFF7FFFFFFFUL);
    [MethodImpl(NI)] static long A387(long x) => x + unchecked((long)0x7FFFFFFF7FFFFFFFUL);
    [MethodImpl(NI)] static long C387(long x) { long r = (x < unchecked((long)0x7FFFFFFF7FFFFFFFUL) ? 1 : 0) + ((ulong)x < 0x7FFFFFFF7FFFFFFFUL ? 2 : 0) + (x == unchecked((long)0x7FFFFFFF7FFFFFFFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7FFFFFFF7FFFFFFFUL)); return r * 31 + (x ^ unchecked((long)0x7FFFFFFF7FFFFFFFUL)); }
    [MethodImpl(NI)] static long K388() => unchecked((long)0x7FFFFFFF80000000UL);
    [MethodImpl(NI)] static long A388(long x) => x + unchecked((long)0x7FFFFFFF80000000UL);
    [MethodImpl(NI)] static long C388(long x) { long r = (x < unchecked((long)0x7FFFFFFF80000000UL) ? 1 : 0) + ((ulong)x < 0x7FFFFFFF80000000UL ? 2 : 0) + (x == unchecked((long)0x7FFFFFFF80000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7FFFFFFF80000000UL)); return r * 31 + (x ^ unchecked((long)0x7FFFFFFF80000000UL)); }
    [MethodImpl(NI)] static long K389() => unchecked((long)0x7FFFFFFFFFFFF7FFUL);
    [MethodImpl(NI)] static long A389(long x) => x + unchecked((long)0x7FFFFFFFFFFFF7FFUL);
    [MethodImpl(NI)] static long C389(long x) { long r = (x < unchecked((long)0x7FFFFFFFFFFFF7FFUL) ? 1 : 0) + ((ulong)x < 0x7FFFFFFFFFFFF7FFUL ? 2 : 0) + (x == unchecked((long)0x7FFFFFFFFFFFF7FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7FFFFFFFFFFFF7FFUL)); return r * 31 + (x ^ unchecked((long)0x7FFFFFFFFFFFF7FFUL)); }
    [MethodImpl(NI)] static long K390() => unchecked((long)0x7FFFFFFFFFFFF800UL);
    [MethodImpl(NI)] static long A390(long x) => x + unchecked((long)0x7FFFFFFFFFFFF800UL);
    [MethodImpl(NI)] static long C390(long x) { long r = (x < unchecked((long)0x7FFFFFFFFFFFF800UL) ? 1 : 0) + ((ulong)x < 0x7FFFFFFFFFFFF800UL ? 2 : 0) + (x == unchecked((long)0x7FFFFFFFFFFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7FFFFFFFFFFFF800UL)); return r * 31 + (x ^ unchecked((long)0x7FFFFFFFFFFFF800UL)); }
    [MethodImpl(NI)] static long K391() => unchecked((long)0x7FFFFFFFFFFFF801UL);
    [MethodImpl(NI)] static long A391(long x) => x + unchecked((long)0x7FFFFFFFFFFFF801UL);
    [MethodImpl(NI)] static long C391(long x) { long r = (x < unchecked((long)0x7FFFFFFFFFFFF801UL) ? 1 : 0) + ((ulong)x < 0x7FFFFFFFFFFFF801UL ? 2 : 0) + (x == unchecked((long)0x7FFFFFFFFFFFF801UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7FFFFFFFFFFFF801UL)); return r * 31 + (x ^ unchecked((long)0x7FFFFFFFFFFFF801UL)); }
    [MethodImpl(NI)] static long K392() => unchecked((long)0x7FFFFFFFFFFFFFFEUL);
    [MethodImpl(NI)] static long A392(long x) => x + unchecked((long)0x7FFFFFFFFFFFFFFEUL);
    [MethodImpl(NI)] static long C392(long x) { long r = (x < unchecked((long)0x7FFFFFFFFFFFFFFEUL) ? 1 : 0) + ((ulong)x < 0x7FFFFFFFFFFFFFFEUL ? 2 : 0) + (x == unchecked((long)0x7FFFFFFFFFFFFFFEUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7FFFFFFFFFFFFFFEUL)); return r * 31 + (x ^ unchecked((long)0x7FFFFFFFFFFFFFFEUL)); }
    [MethodImpl(NI)] static long K393() => unchecked((long)0x7FFFFFFFFFFFFFFFUL);
    [MethodImpl(NI)] static long A393(long x) => x + unchecked((long)0x7FFFFFFFFFFFFFFFUL);
    [MethodImpl(NI)] static long C393(long x) { long r = (x < unchecked((long)0x7FFFFFFFFFFFFFFFUL) ? 1 : 0) + ((ulong)x < 0x7FFFFFFFFFFFFFFFUL ? 2 : 0) + (x == unchecked((long)0x7FFFFFFFFFFFFFFFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x7FFFFFFFFFFFFFFFUL)); return r * 31 + (x ^ unchecked((long)0x7FFFFFFFFFFFFFFFUL)); }
    [MethodImpl(NI)] static long K394() => unchecked((long)0x8000000000000000UL);
    [MethodImpl(NI)] static long A394(long x) => x + unchecked((long)0x8000000000000000UL);
    [MethodImpl(NI)] static long C394(long x) { long r = (x < unchecked((long)0x8000000000000000UL) ? 1 : 0) + ((ulong)x < 0x8000000000000000UL ? 2 : 0) + (x == unchecked((long)0x8000000000000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x8000000000000000UL)); return r * 31 + (x ^ unchecked((long)0x8000000000000000UL)); }
    [MethodImpl(NI)] static long K395() => unchecked((long)0x8000000000000001UL);
    [MethodImpl(NI)] static long A395(long x) => x + unchecked((long)0x8000000000000001UL);
    [MethodImpl(NI)] static long C395(long x) { long r = (x < unchecked((long)0x8000000000000001UL) ? 1 : 0) + ((ulong)x < 0x8000000000000001UL ? 2 : 0) + (x == unchecked((long)0x8000000000000001UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x8000000000000001UL)); return r * 31 + (x ^ unchecked((long)0x8000000000000001UL)); }
    [MethodImpl(NI)] static long K396() => unchecked((long)0x80000000000007FFUL);
    [MethodImpl(NI)] static long A396(long x) => x + unchecked((long)0x80000000000007FFUL);
    [MethodImpl(NI)] static long C396(long x) { long r = (x < unchecked((long)0x80000000000007FFUL) ? 1 : 0) + ((ulong)x < 0x80000000000007FFUL ? 2 : 0) + (x == unchecked((long)0x80000000000007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x80000000000007FFUL)); return r * 31 + (x ^ unchecked((long)0x80000000000007FFUL)); }
    [MethodImpl(NI)] static long K397() => unchecked((long)0x800000007FFFFFFEUL);
    [MethodImpl(NI)] static long A397(long x) => x + unchecked((long)0x800000007FFFFFFEUL);
    [MethodImpl(NI)] static long C397(long x) { long r = (x < unchecked((long)0x800000007FFFFFFEUL) ? 1 : 0) + ((ulong)x < 0x800000007FFFFFFEUL ? 2 : 0) + (x == unchecked((long)0x800000007FFFFFFEUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x800000007FFFFFFEUL)); return r * 31 + (x ^ unchecked((long)0x800000007FFFFFFEUL)); }
    [MethodImpl(NI)] static long K398() => unchecked((long)0x800000007FFFFFFFUL);
    [MethodImpl(NI)] static long A398(long x) => x + unchecked((long)0x800000007FFFFFFFUL);
    [MethodImpl(NI)] static long C398(long x) { long r = (x < unchecked((long)0x800000007FFFFFFFUL) ? 1 : 0) + ((ulong)x < 0x800000007FFFFFFFUL ? 2 : 0) + (x == unchecked((long)0x800000007FFFFFFFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x800000007FFFFFFFUL)); return r * 31 + (x ^ unchecked((long)0x800000007FFFFFFFUL)); }
    [MethodImpl(NI)] static long K399() => unchecked((long)0x8000000080000000UL);
    [MethodImpl(NI)] static long A399(long x) => x + unchecked((long)0x8000000080000000UL);
    [MethodImpl(NI)] static long C399(long x) { long r = (x < unchecked((long)0x8000000080000000UL) ? 1 : 0) + ((ulong)x < 0x8000000080000000UL ? 2 : 0) + (x == unchecked((long)0x8000000080000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x8000000080000000UL)); return r * 31 + (x ^ unchecked((long)0x8000000080000000UL)); }
    [MethodImpl(NI)] static long K400() => unchecked((long)0x80000000FFFFF800UL);
    [MethodImpl(NI)] static long A400(long x) => x + unchecked((long)0x80000000FFFFF800UL);
    [MethodImpl(NI)] static long C400(long x) { long r = (x < unchecked((long)0x80000000FFFFF800UL) ? 1 : 0) + ((ulong)x < 0x80000000FFFFF800UL ? 2 : 0) + (x == unchecked((long)0x80000000FFFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x80000000FFFFF800UL)); return r * 31 + (x ^ unchecked((long)0x80000000FFFFF800UL)); }
    [MethodImpl(NI)] static long K401() => unchecked((long)0x80000000FFFFFFFEUL);
    [MethodImpl(NI)] static long A401(long x) => x + unchecked((long)0x80000000FFFFFFFEUL);
    [MethodImpl(NI)] static long C401(long x) { long r = (x < unchecked((long)0x80000000FFFFFFFEUL) ? 1 : 0) + ((ulong)x < 0x80000000FFFFFFFEUL ? 2 : 0) + (x == unchecked((long)0x80000000FFFFFFFEUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x80000000FFFFFFFEUL)); return r * 31 + (x ^ unchecked((long)0x80000000FFFFFFFEUL)); }
    [MethodImpl(NI)] static long K402() => unchecked((long)0x80000000FFFFFFFFUL);
    [MethodImpl(NI)] static long A402(long x) => x + unchecked((long)0x80000000FFFFFFFFUL);
    [MethodImpl(NI)] static long C402(long x) { long r = (x < unchecked((long)0x80000000FFFFFFFFUL) ? 1 : 0) + ((ulong)x < 0x80000000FFFFFFFFUL ? 2 : 0) + (x == unchecked((long)0x80000000FFFFFFFFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x80000000FFFFFFFFUL)); return r * 31 + (x ^ unchecked((long)0x80000000FFFFFFFFUL)); }
    [MethodImpl(NI)] static long K403() => unchecked((long)0x8000000100000000UL);
    [MethodImpl(NI)] static long A403(long x) => x + unchecked((long)0x8000000100000000UL);
    [MethodImpl(NI)] static long C403(long x) { long r = (x < unchecked((long)0x8000000100000000UL) ? 1 : 0) + ((ulong)x < 0x8000000100000000UL ? 2 : 0) + (x == unchecked((long)0x8000000100000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x8000000100000000UL)); return r * 31 + (x ^ unchecked((long)0x8000000100000000UL)); }
    [MethodImpl(NI)] static long K404() => unchecked((long)0x80000001000007FFUL);
    [MethodImpl(NI)] static long A404(long x) => x + unchecked((long)0x80000001000007FFUL);
    [MethodImpl(NI)] static long C404(long x) { long r = (x < unchecked((long)0x80000001000007FFUL) ? 1 : 0) + ((ulong)x < 0x80000001000007FFUL ? 2 : 0) + (x == unchecked((long)0x80000001000007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x80000001000007FFUL)); return r * 31 + (x ^ unchecked((long)0x80000001000007FFUL)); }
    [MethodImpl(NI)] static long K405() => unchecked((long)0x8000000180000001UL);
    [MethodImpl(NI)] static long A405(long x) => x + unchecked((long)0x8000000180000001UL);
    [MethodImpl(NI)] static long C405(long x) { long r = (x < unchecked((long)0x8000000180000001UL) ? 1 : 0) + ((ulong)x < 0x8000000180000001UL ? 2 : 0) + (x == unchecked((long)0x8000000180000001UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x8000000180000001UL)); return r * 31 + (x ^ unchecked((long)0x8000000180000001UL)); }
    [MethodImpl(NI)] static long K406() => unchecked((long)0x800007FEFFFFF800UL);
    [MethodImpl(NI)] static long A406(long x) => x + unchecked((long)0x800007FEFFFFF800UL);
    [MethodImpl(NI)] static long C406(long x) { long r = (x < unchecked((long)0x800007FEFFFFF800UL) ? 1 : 0) + ((ulong)x < 0x800007FEFFFFF800UL ? 2 : 0) + (x == unchecked((long)0x800007FEFFFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x800007FEFFFFF800UL)); return r * 31 + (x ^ unchecked((long)0x800007FEFFFFF800UL)); }
    [MethodImpl(NI)] static long K407() => unchecked((long)0x800007FF00000000UL);
    [MethodImpl(NI)] static long A407(long x) => x + unchecked((long)0x800007FF00000000UL);
    [MethodImpl(NI)] static long C407(long x) { long r = (x < unchecked((long)0x800007FF00000000UL) ? 1 : 0) + ((ulong)x < 0x800007FF00000000UL ? 2 : 0) + (x == unchecked((long)0x800007FF00000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x800007FF00000000UL)); return r * 31 + (x ^ unchecked((long)0x800007FF00000000UL)); }
    [MethodImpl(NI)] static long K408() => unchecked((long)0x800007FF000007FFUL);
    [MethodImpl(NI)] static long A408(long x) => x + unchecked((long)0x800007FF000007FFUL);
    [MethodImpl(NI)] static long C408(long x) { long r = (x < unchecked((long)0x800007FF000007FFUL) ? 1 : 0) + ((ulong)x < 0x800007FF000007FFUL ? 2 : 0) + (x == unchecked((long)0x800007FF000007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x800007FF000007FFUL)); return r * 31 + (x ^ unchecked((long)0x800007FF000007FFUL)); }
    [MethodImpl(NI)] static long K409() => unchecked((long)0x800007FF800007FFUL);
    [MethodImpl(NI)] static long A409(long x) => x + unchecked((long)0x800007FF800007FFUL);
    [MethodImpl(NI)] static long C409(long x) { long r = (x < unchecked((long)0x800007FF800007FFUL) ? 1 : 0) + ((ulong)x < 0x800007FF800007FFUL ? 2 : 0) + (x == unchecked((long)0x800007FF800007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x800007FF800007FFUL)); return r * 31 + (x ^ unchecked((long)0x800007FF800007FFUL)); }
    [MethodImpl(NI)] static long K410() => unchecked((long)0x800007FFFFFFF800UL);
    [MethodImpl(NI)] static long A410(long x) => x + unchecked((long)0x800007FFFFFFF800UL);
    [MethodImpl(NI)] static long C410(long x) { long r = (x < unchecked((long)0x800007FFFFFFF800UL) ? 1 : 0) + ((ulong)x < 0x800007FFFFFFF800UL ? 2 : 0) + (x == unchecked((long)0x800007FFFFFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x800007FFFFFFF800UL)); return r * 31 + (x ^ unchecked((long)0x800007FFFFFFF800UL)); }
    [MethodImpl(NI)] static long K411() => unchecked((long)0x800007FFFFFFFFFFUL);
    [MethodImpl(NI)] static long A411(long x) => x + unchecked((long)0x800007FFFFFFFFFFUL);
    [MethodImpl(NI)] static long C411(long x) { long r = (x < unchecked((long)0x800007FFFFFFFFFFUL) ? 1 : 0) + ((ulong)x < 0x800007FFFFFFFFFFUL ? 2 : 0) + (x == unchecked((long)0x800007FFFFFFFFFFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x800007FFFFFFFFFFUL)); return r * 31 + (x ^ unchecked((long)0x800007FFFFFFFFFFUL)); }
    [MethodImpl(NI)] static long K412() => unchecked((long)0x8000080000000000UL);
    [MethodImpl(NI)] static long A412(long x) => x + unchecked((long)0x8000080000000000UL);
    [MethodImpl(NI)] static long C412(long x) { long r = (x < unchecked((long)0x8000080000000000UL) ? 1 : 0) + ((ulong)x < 0x8000080000000000UL ? 2 : 0) + (x == unchecked((long)0x8000080000000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x8000080000000000UL)); return r * 31 + (x ^ unchecked((long)0x8000080000000000UL)); }
    [MethodImpl(NI)] static long K413() => unchecked((long)0x80000800000007FFUL);
    [MethodImpl(NI)] static long A413(long x) => x + unchecked((long)0x80000800000007FFUL);
    [MethodImpl(NI)] static long C413(long x) { long r = (x < unchecked((long)0x80000800000007FFUL) ? 1 : 0) + ((ulong)x < 0x80000800000007FFUL ? 2 : 0) + (x == unchecked((long)0x80000800000007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x80000800000007FFUL)); return r * 31 + (x ^ unchecked((long)0x80000800000007FFUL)); }
    [MethodImpl(NI)] static long K414() => unchecked((long)0x8000080080000800UL);
    [MethodImpl(NI)] static long A414(long x) => x + unchecked((long)0x8000080080000800UL);
    [MethodImpl(NI)] static long C414(long x) { long r = (x < unchecked((long)0x8000080080000800UL) ? 1 : 0) + ((ulong)x < 0x8000080080000800UL ? 2 : 0) + (x == unchecked((long)0x8000080080000800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x8000080080000800UL)); return r * 31 + (x ^ unchecked((long)0x8000080080000800UL)); }
    [MethodImpl(NI)] static long K415() => unchecked((long)0x8000100000000000UL);
    [MethodImpl(NI)] static long A415(long x) => x + unchecked((long)0x8000100000000000UL);
    [MethodImpl(NI)] static long C415(long x) { long r = (x < unchecked((long)0x8000100000000000UL) ? 1 : 0) + ((ulong)x < 0x8000100000000000UL ? 2 : 0) + (x == unchecked((long)0x8000100000000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x8000100000000000UL)); return r * 31 + (x ^ unchecked((long)0x8000100000000000UL)); }
    [MethodImpl(NI)] static long K416() => unchecked((long)0x87654320FFFFF800UL);
    [MethodImpl(NI)] static long A416(long x) => x + unchecked((long)0x87654320FFFFF800UL);
    [MethodImpl(NI)] static long C416(long x) { long r = (x < unchecked((long)0x87654320FFFFF800UL) ? 1 : 0) + ((ulong)x < 0x87654320FFFFF800UL ? 2 : 0) + (x == unchecked((long)0x87654320FFFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x87654320FFFFF800UL)); return r * 31 + (x ^ unchecked((long)0x87654320FFFFF800UL)); }
    [MethodImpl(NI)] static long K417() => unchecked((long)0x8765432100000000UL);
    [MethodImpl(NI)] static long A417(long x) => x + unchecked((long)0x8765432100000000UL);
    [MethodImpl(NI)] static long C417(long x) { long r = (x < unchecked((long)0x8765432100000000UL) ? 1 : 0) + ((ulong)x < 0x8765432100000000UL ? 2 : 0) + (x == unchecked((long)0x8765432100000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x8765432100000000UL)); return r * 31 + (x ^ unchecked((long)0x8765432100000000UL)); }
    [MethodImpl(NI)] static long K418() => unchecked((long)0x87654321000007FFUL);
    [MethodImpl(NI)] static long A418(long x) => x + unchecked((long)0x87654321000007FFUL);
    [MethodImpl(NI)] static long C418(long x) { long r = (x < unchecked((long)0x87654321000007FFUL) ? 1 : 0) + ((ulong)x < 0x87654321000007FFUL ? 2 : 0) + (x == unchecked((long)0x87654321000007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x87654321000007FFUL)); return r * 31 + (x ^ unchecked((long)0x87654321000007FFUL)); }
    [MethodImpl(NI)] static long K419() => unchecked((long)0x8765432187654321UL);
    [MethodImpl(NI)] static long A419(long x) => x + unchecked((long)0x8765432187654321UL);
    [MethodImpl(NI)] static long C419(long x) { long r = (x < unchecked((long)0x8765432187654321UL) ? 1 : 0) + ((ulong)x < 0x8765432187654321UL ? 2 : 0) + (x == unchecked((long)0x8765432187654321UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x8765432187654321UL)); return r * 31 + (x ^ unchecked((long)0x8765432187654321UL)); }
    [MethodImpl(NI)] static long K420() => unchecked((long)0x8C647FFF037FFA0BUL);
    [MethodImpl(NI)] static long A420(long x) => x + unchecked((long)0x8C647FFF037FFA0BUL);
    [MethodImpl(NI)] static long C420(long x) { long r = (x < unchecked((long)0x8C647FFF037FFA0BUL) ? 1 : 0) + ((ulong)x < 0x8C647FFF037FFA0BUL ? 2 : 0) + (x == unchecked((long)0x8C647FFF037FFA0BUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x8C647FFF037FFA0BUL)); return r * 31 + (x ^ unchecked((long)0x8C647FFF037FFA0BUL)); }
    [MethodImpl(NI)] static long K421() => unchecked((long)0x9C18A51ECAA547FFUL);
    [MethodImpl(NI)] static long A421(long x) => x + unchecked((long)0x9C18A51ECAA547FFUL);
    [MethodImpl(NI)] static long C421(long x) { long r = (x < unchecked((long)0x9C18A51ECAA547FFUL) ? 1 : 0) + ((ulong)x < 0x9C18A51ECAA547FFUL ? 2 : 0) + (x == unchecked((long)0x9C18A51ECAA547FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x9C18A51ECAA547FFUL)); return r * 31 + (x ^ unchecked((long)0x9C18A51ECAA547FFUL)); }
    [MethodImpl(NI)] static long K422() => unchecked((long)0x9C18A51ECAA54BFEUL);
    [MethodImpl(NI)] static long A422(long x) => x + unchecked((long)0x9C18A51ECAA54BFEUL);
    [MethodImpl(NI)] static long C422(long x) { long r = (x < unchecked((long)0x9C18A51ECAA54BFEUL) ? 1 : 0) + ((ulong)x < 0x9C18A51ECAA54BFEUL ? 2 : 0) + (x == unchecked((long)0x9C18A51ECAA54BFEUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x9C18A51ECAA54BFEUL)); return r * 31 + (x ^ unchecked((long)0x9C18A51ECAA54BFEUL)); }
    [MethodImpl(NI)] static long K423() => unchecked((long)0x9E391671AAC2E7FFUL);
    [MethodImpl(NI)] static long A423(long x) => x + unchecked((long)0x9E391671AAC2E7FFUL);
    [MethodImpl(NI)] static long C423(long x) { long r = (x < unchecked((long)0x9E391671AAC2E7FFUL) ? 1 : 0) + ((ulong)x < 0x9E391671AAC2E7FFUL ? 2 : 0) + (x == unchecked((long)0x9E391671AAC2E7FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x9E391671AAC2E7FFUL)); return r * 31 + (x ^ unchecked((long)0x9E391671AAC2E7FFUL)); }
    [MethodImpl(NI)] static long K424() => unchecked((long)0x9E391671AAC2EBCEUL);
    [MethodImpl(NI)] static long A424(long x) => x + unchecked((long)0x9E391671AAC2EBCEUL);
    [MethodImpl(NI)] static long C424(long x) { long r = (x < unchecked((long)0x9E391671AAC2EBCEUL) ? 1 : 0) + ((ulong)x < 0x9E391671AAC2EBCEUL ? 2 : 0) + (x == unchecked((long)0x9E391671AAC2EBCEUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0x9E391671AAC2EBCEUL)); return r * 31 + (x ^ unchecked((long)0x9E391671AAC2EBCEUL)); }
    [MethodImpl(NI)] static long K425() => unchecked((long)0xAAAAAAAAAAAAAAA9UL);
    [MethodImpl(NI)] static long A425(long x) => x + unchecked((long)0xAAAAAAAAAAAAAAA9UL);
    [MethodImpl(NI)] static long C425(long x) { long r = (x < unchecked((long)0xAAAAAAAAAAAAAAA9UL) ? 1 : 0) + ((ulong)x < 0xAAAAAAAAAAAAAAA9UL ? 2 : 0) + (x == unchecked((long)0xAAAAAAAAAAAAAAA9UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xAAAAAAAAAAAAAAA9UL)); return r * 31 + (x ^ unchecked((long)0xAAAAAAAAAAAAAAA9UL)); }
    [MethodImpl(NI)] static long K426() => unchecked((long)0xAAAAAAAAAAAAAAAAUL);
    [MethodImpl(NI)] static long A426(long x) => x + unchecked((long)0xAAAAAAAAAAAAAAAAUL);
    [MethodImpl(NI)] static long C426(long x) { long r = (x < unchecked((long)0xAAAAAAAAAAAAAAAAUL) ? 1 : 0) + ((ulong)x < 0xAAAAAAAAAAAAAAAAUL ? 2 : 0) + (x == unchecked((long)0xAAAAAAAAAAAAAAAAUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xAAAAAAAAAAAAAAAAUL)); return r * 31 + (x ^ unchecked((long)0xAAAAAAAAAAAAAAAAUL)); }
    [MethodImpl(NI)] static long K427() => unchecked((long)0xAAAAAAAAAAAAAAABUL);
    [MethodImpl(NI)] static long A427(long x) => x + unchecked((long)0xAAAAAAAAAAAAAAABUL);
    [MethodImpl(NI)] static long C427(long x) { long r = (x < unchecked((long)0xAAAAAAAAAAAAAAABUL) ? 1 : 0) + ((ulong)x < 0xAAAAAAAAAAAAAAABUL ? 2 : 0) + (x == unchecked((long)0xAAAAAAAAAAAAAAABUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xAAAAAAAAAAAAAAABUL)); return r * 31 + (x ^ unchecked((long)0xAAAAAAAAAAAAAAABUL)); }
    [MethodImpl(NI)] static long K428() => unchecked((long)0xBF9AE54DB6DB570DUL);
    [MethodImpl(NI)] static long A428(long x) => x + unchecked((long)0xBF9AE54DB6DB570DUL);
    [MethodImpl(NI)] static long C428(long x) { long r = (x < unchecked((long)0xBF9AE54DB6DB570DUL) ? 1 : 0) + ((ulong)x < 0xBF9AE54DB6DB570DUL ? 2 : 0) + (x == unchecked((long)0xBF9AE54DB6DB570DUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xBF9AE54DB6DB570DUL)); return r * 31 + (x ^ unchecked((long)0xBF9AE54DB6DB570DUL)); }
    [MethodImpl(NI)] static long K429() => unchecked((long)0xBF9AE54DB6DB5800UL);
    [MethodImpl(NI)] static long A429(long x) => x + unchecked((long)0xBF9AE54DB6DB5800UL);
    [MethodImpl(NI)] static long C429(long x) { long r = (x < unchecked((long)0xBF9AE54DB6DB5800UL) ? 1 : 0) + ((ulong)x < 0xBF9AE54DB6DB5800UL ? 2 : 0) + (x == unchecked((long)0xBF9AE54DB6DB5800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xBF9AE54DB6DB5800UL)); return r * 31 + (x ^ unchecked((long)0xBF9AE54DB6DB5800UL)); }
    [MethodImpl(NI)] static long K430() => unchecked((long)0xBFFEFFF7FFFFF800UL);
    [MethodImpl(NI)] static long A430(long x) => x + unchecked((long)0xBFFEFFF7FFFFF800UL);
    [MethodImpl(NI)] static long C430(long x) { long r = (x < unchecked((long)0xBFFEFFF7FFFFF800UL) ? 1 : 0) + ((ulong)x < 0xBFFEFFF7FFFFF800UL ? 2 : 0) + (x == unchecked((long)0xBFFEFFF7FFFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xBFFEFFF7FFFFF800UL)); return r * 31 + (x ^ unchecked((long)0xBFFEFFF7FFFFF800UL)); }
    [MethodImpl(NI)] static long K431() => unchecked((long)0xBFFEFFF800000000UL);
    [MethodImpl(NI)] static long A431(long x) => x + unchecked((long)0xBFFEFFF800000000UL);
    [MethodImpl(NI)] static long C431(long x) { long r = (x < unchecked((long)0xBFFEFFF800000000UL) ? 1 : 0) + ((ulong)x < 0xBFFEFFF800000000UL ? 2 : 0) + (x == unchecked((long)0xBFFEFFF800000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xBFFEFFF800000000UL)); return r * 31 + (x ^ unchecked((long)0xBFFEFFF800000000UL)); }
    [MethodImpl(NI)] static long K432() => unchecked((long)0xBFFEFFF8000007FFUL);
    [MethodImpl(NI)] static long A432(long x) => x + unchecked((long)0xBFFEFFF8000007FFUL);
    [MethodImpl(NI)] static long C432(long x) { long r = (x < unchecked((long)0xBFFEFFF8000007FFUL) ? 1 : 0) + ((ulong)x < 0xBFFEFFF8000007FFUL ? 2 : 0) + (x == unchecked((long)0xBFFEFFF8000007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xBFFEFFF8000007FFUL)); return r * 31 + (x ^ unchecked((long)0xBFFEFFF8000007FFUL)); }
    [MethodImpl(NI)] static long K433() => unchecked((long)0xBFFEFFF8BFFEFFF8UL);
    [MethodImpl(NI)] static long A433(long x) => x + unchecked((long)0xBFFEFFF8BFFEFFF8UL);
    [MethodImpl(NI)] static long C433(long x) { long r = (x < unchecked((long)0xBFFEFFF8BFFEFFF8UL) ? 1 : 0) + ((ulong)x < 0xBFFEFFF8BFFEFFF8UL ? 2 : 0) + (x == unchecked((long)0xBFFEFFF8BFFEFFF8UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xBFFEFFF8BFFEFFF8UL)); return r * 31 + (x ^ unchecked((long)0xBFFEFFF8BFFEFFF8UL)); }
    [MethodImpl(NI)] static long K434() => unchecked((long)0xBFFEFFFFFFFFF800UL);
    [MethodImpl(NI)] static long A434(long x) => x + unchecked((long)0xBFFEFFFFFFFFF800UL);
    [MethodImpl(NI)] static long C434(long x) { long r = (x < unchecked((long)0xBFFEFFFFFFFFF800UL) ? 1 : 0) + ((ulong)x < 0xBFFEFFFFFFFFF800UL ? 2 : 0) + (x == unchecked((long)0xBFFEFFFFFFFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xBFFEFFFFFFFFF800UL)); return r * 31 + (x ^ unchecked((long)0xBFFEFFFFFFFFF800UL)); }
    [MethodImpl(NI)] static long K435() => unchecked((long)0xBFFF000000000000UL);
    [MethodImpl(NI)] static long A435(long x) => x + unchecked((long)0xBFFF000000000000UL);
    [MethodImpl(NI)] static long C435(long x) { long r = (x < unchecked((long)0xBFFF000000000000UL) ? 1 : 0) + ((ulong)x < 0xBFFF000000000000UL ? 2 : 0) + (x == unchecked((long)0xBFFF000000000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xBFFF000000000000UL)); return r * 31 + (x ^ unchecked((long)0xBFFF000000000000UL)); }
    [MethodImpl(NI)] static long K436() => unchecked((long)0xBFFF0000000007FFUL);
    [MethodImpl(NI)] static long A436(long x) => x + unchecked((long)0xBFFF0000000007FFUL);
    [MethodImpl(NI)] static long C436(long x) { long r = (x < unchecked((long)0xBFFF0000000007FFUL) ? 1 : 0) + ((ulong)x < 0xBFFF0000000007FFUL ? 2 : 0) + (x == unchecked((long)0xBFFF0000000007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xBFFF0000000007FFUL)); return r * 31 + (x ^ unchecked((long)0xBFFF0000000007FFUL)); }
    [MethodImpl(NI)] static long K437() => unchecked((long)0xBFFF0000BFFF0000UL);
    [MethodImpl(NI)] static long A437(long x) => x + unchecked((long)0xBFFF0000BFFF0000UL);
    [MethodImpl(NI)] static long C437(long x) { long r = (x < unchecked((long)0xBFFF0000BFFF0000UL) ? 1 : 0) + ((ulong)x < 0xBFFF0000BFFF0000UL ? 2 : 0) + (x == unchecked((long)0xBFFF0000BFFF0000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xBFFF0000BFFF0000UL)); return r * 31 + (x ^ unchecked((long)0xBFFF0000BFFF0000UL)); }
    [MethodImpl(NI)] static long K438() => unchecked((long)0xC000000000000000UL);
    [MethodImpl(NI)] static long A438(long x) => x + unchecked((long)0xC000000000000000UL);
    [MethodImpl(NI)] static long C438(long x) { long r = (x < unchecked((long)0xC000000000000000UL) ? 1 : 0) + ((ulong)x < 0xC000000000000000UL ? 2 : 0) + (x == unchecked((long)0xC000000000000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xC000000000000000UL)); return r * 31 + (x ^ unchecked((long)0xC000000000000000UL)); }
    [MethodImpl(NI)] static long K439() => unchecked((long)0xC00007FFFFFFF800UL);
    [MethodImpl(NI)] static long A439(long x) => x + unchecked((long)0xC00007FFFFFFF800UL);
    [MethodImpl(NI)] static long C439(long x) { long r = (x < unchecked((long)0xC00007FFFFFFF800UL) ? 1 : 0) + ((ulong)x < 0xC00007FFFFFFF800UL ? 2 : 0) + (x == unchecked((long)0xC00007FFFFFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xC00007FFFFFFF800UL)); return r * 31 + (x ^ unchecked((long)0xC00007FFFFFFF800UL)); }
    [MethodImpl(NI)] static long K440() => unchecked((long)0xC000080000000000UL);
    [MethodImpl(NI)] static long A440(long x) => x + unchecked((long)0xC000080000000000UL);
    [MethodImpl(NI)] static long C440(long x) { long r = (x < unchecked((long)0xC000080000000000UL) ? 1 : 0) + ((ulong)x < 0xC000080000000000UL ? 2 : 0) + (x == unchecked((long)0xC000080000000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xC000080000000000UL)); return r * 31 + (x ^ unchecked((long)0xC000080000000000UL)); }
    [MethodImpl(NI)] static long K441() => unchecked((long)0xC0000800000007FFUL);
    [MethodImpl(NI)] static long A441(long x) => x + unchecked((long)0xC0000800000007FFUL);
    [MethodImpl(NI)] static long C441(long x) { long r = (x < unchecked((long)0xC0000800000007FFUL) ? 1 : 0) + ((ulong)x < 0xC0000800000007FFUL ? 2 : 0) + (x == unchecked((long)0xC0000800000007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xC0000800000007FFUL)); return r * 31 + (x ^ unchecked((long)0xC0000800000007FFUL)); }
    [MethodImpl(NI)] static long K442() => unchecked((long)0xC0000800C0000800UL);
    [MethodImpl(NI)] static long A442(long x) => x + unchecked((long)0xC0000800C0000800UL);
    [MethodImpl(NI)] static long C442(long x) { long r = (x < unchecked((long)0xC0000800C0000800UL) ? 1 : 0) + ((ulong)x < 0xC0000800C0000800UL ? 2 : 0) + (x == unchecked((long)0xC0000800C0000800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xC0000800C0000800UL)); return r * 31 + (x ^ unchecked((long)0xC0000800C0000800UL)); }
    [MethodImpl(NI)] static long K443() => unchecked((long)0xDFFF800000000000UL);
    [MethodImpl(NI)] static long A443(long x) => x + unchecked((long)0xDFFF800000000000UL);
    [MethodImpl(NI)] static long C443(long x) { long r = (x < unchecked((long)0xDFFF800000000000UL) ? 1 : 0) + ((ulong)x < 0xDFFF800000000000UL ? 2 : 0) + (x == unchecked((long)0xDFFF800000000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xDFFF800000000000UL)); return r * 31 + (x ^ unchecked((long)0xDFFF800000000000UL)); }
    [MethodImpl(NI)] static long K444() => unchecked((long)0xED75DEA57A0D88B5UL);
    [MethodImpl(NI)] static long A444(long x) => x + unchecked((long)0xED75DEA57A0D88B5UL);
    [MethodImpl(NI)] static long C444(long x) { long r = (x < unchecked((long)0xED75DEA57A0D88B5UL) ? 1 : 0) + ((ulong)x < 0xED75DEA57A0D88B5UL ? 2 : 0) + (x == unchecked((long)0xED75DEA57A0D88B5UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xED75DEA57A0D88B5UL)); return r * 31 + (x ^ unchecked((long)0xED75DEA57A0D88B5UL)); }
    [MethodImpl(NI)] static long K445() => unchecked((long)0xED75DEA57A0D8FFFUL);
    [MethodImpl(NI)] static long A445(long x) => x + unchecked((long)0xED75DEA57A0D8FFFUL);
    [MethodImpl(NI)] static long C445(long x) { long r = (x < unchecked((long)0xED75DEA57A0D8FFFUL) ? 1 : 0) + ((ulong)x < 0xED75DEA57A0D8FFFUL ? 2 : 0) + (x == unchecked((long)0xED75DEA57A0D8FFFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xED75DEA57A0D8FFFUL)); return r * 31 + (x ^ unchecked((long)0xED75DEA57A0D8FFFUL)); }
    [MethodImpl(NI)] static long K446() => unchecked((long)0xF00000000FFFFFFFUL);
    [MethodImpl(NI)] static long A446(long x) => x + unchecked((long)0xF00000000FFFFFFFUL);
    [MethodImpl(NI)] static long C446(long x) { long r = (x < unchecked((long)0xF00000000FFFFFFFUL) ? 1 : 0) + ((ulong)x < 0xF00000000FFFFFFFUL ? 2 : 0) + (x == unchecked((long)0xF00000000FFFFFFFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xF00000000FFFFFFFUL)); return r * 31 + (x ^ unchecked((long)0xF00000000FFFFFFFUL)); }
    [MethodImpl(NI)] static long K447() => unchecked((long)0xFEDCB00000000000UL);
    [MethodImpl(NI)] static long A447(long x) => x + unchecked((long)0xFEDCB00000000000UL);
    [MethodImpl(NI)] static long C447(long x) { long r = (x < unchecked((long)0xFEDCB00000000000UL) ? 1 : 0) + ((ulong)x < 0xFEDCB00000000000UL ? 2 : 0) + (x == unchecked((long)0xFEDCB00000000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFEDCB00000000000UL)); return r * 31 + (x ^ unchecked((long)0xFEDCB00000000000UL)); }
    [MethodImpl(NI)] static long K448() => unchecked((long)0xFEDCBA987654320FUL);
    [MethodImpl(NI)] static long A448(long x) => x + unchecked((long)0xFEDCBA987654320FUL);
    [MethodImpl(NI)] static long C448(long x) { long r = (x < unchecked((long)0xFEDCBA987654320FUL) ? 1 : 0) + ((ulong)x < 0xFEDCBA987654320FUL ? 2 : 0) + (x == unchecked((long)0xFEDCBA987654320FUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFEDCBA987654320FUL)); return r * 31 + (x ^ unchecked((long)0xFEDCBA987654320FUL)); }
    [MethodImpl(NI)] static long K449() => unchecked((long)0xFEDCBA9876543210UL);
    [MethodImpl(NI)] static long A449(long x) => x + unchecked((long)0xFEDCBA9876543210UL);
    [MethodImpl(NI)] static long C449(long x) { long r = (x < unchecked((long)0xFEDCBA9876543210UL) ? 1 : 0) + ((ulong)x < 0xFEDCBA9876543210UL ? 2 : 0) + (x == unchecked((long)0xFEDCBA9876543210UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFEDCBA9876543210UL)); return r * 31 + (x ^ unchecked((long)0xFEDCBA9876543210UL)); }
    [MethodImpl(NI)] static long K450() => unchecked((long)0xFEDCBA9876543211UL);
    [MethodImpl(NI)] static long A450(long x) => x + unchecked((long)0xFEDCBA9876543211UL);
    [MethodImpl(NI)] static long C450(long x) { long r = (x < unchecked((long)0xFEDCBA9876543211UL) ? 1 : 0) + ((ulong)x < 0xFEDCBA9876543211UL ? 2 : 0) + (x == unchecked((long)0xFEDCBA9876543211UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFEDCBA9876543211UL)); return r * 31 + (x ^ unchecked((long)0xFEDCBA9876543211UL)); }
    [MethodImpl(NI)] static long K451() => unchecked((long)0xFF6E580000000000UL);
    [MethodImpl(NI)] static long A451(long x) => x + unchecked((long)0xFF6E580000000000UL);
    [MethodImpl(NI)] static long C451(long x) { long r = (x < unchecked((long)0xFF6E580000000000UL) ? 1 : 0) + ((ulong)x < 0xFF6E580000000000UL ? 2 : 0) + (x == unchecked((long)0xFF6E580000000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFF6E580000000000UL)); return r * 31 + (x ^ unchecked((long)0xFF6E580000000000UL)); }
    [MethodImpl(NI)] static long K452() => unchecked((long)0xFFF562EEE508C000UL);
    [MethodImpl(NI)] static long A452(long x) => x + unchecked((long)0xFFF562EEE508C000UL);
    [MethodImpl(NI)] static long C452(long x) { long r = (x < unchecked((long)0xFFF562EEE508C000UL) ? 1 : 0) + ((ulong)x < 0xFFF562EEE508C000UL ? 2 : 0) + (x == unchecked((long)0xFFF562EEE508C000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFF562EEE508C000UL)); return r * 31 + (x ^ unchecked((long)0xFFF562EEE508C000UL)); }
    [MethodImpl(NI)] static long K453() => unchecked((long)0xFFF562EEE508C978UL);
    [MethodImpl(NI)] static long A453(long x) => x + unchecked((long)0xFFF562EEE508C978UL);
    [MethodImpl(NI)] static long C453(long x) { long r = (x < unchecked((long)0xFFF562EEE508C978UL) ? 1 : 0) + ((ulong)x < 0xFFF562EEE508C978UL ? 2 : 0) + (x == unchecked((long)0xFFF562EEE508C978UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFF562EEE508C978UL)); return r * 31 + (x ^ unchecked((long)0xFFF562EEE508C978UL)); }
    [MethodImpl(NI)] static long K454() => unchecked((long)0xFFF7D08BE832D000UL);
    [MethodImpl(NI)] static long A454(long x) => x + unchecked((long)0xFFF7D08BE832D000UL);
    [MethodImpl(NI)] static long C454(long x) { long r = (x < unchecked((long)0xFFF7D08BE832D000UL) ? 1 : 0) + ((ulong)x < 0xFFF7D08BE832D000UL ? 2 : 0) + (x == unchecked((long)0xFFF7D08BE832D000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFF7D08BE832D000UL)); return r * 31 + (x ^ unchecked((long)0xFFF7D08BE832D000UL)); }
    [MethodImpl(NI)] static long K455() => unchecked((long)0xFFF7D08BE832D72AUL);
    [MethodImpl(NI)] static long A455(long x) => x + unchecked((long)0xFFF7D08BE832D72AUL);
    [MethodImpl(NI)] static long C455(long x) { long r = (x < unchecked((long)0xFFF7D08BE832D72AUL) ? 1 : 0) + ((ulong)x < 0xFFF7D08BE832D72AUL ? 2 : 0) + (x == unchecked((long)0xFFF7D08BE832D72AUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFF7D08BE832D72AUL)); return r * 31 + (x ^ unchecked((long)0xFFF7D08BE832D72AUL)); }
    [MethodImpl(NI)] static long K456() => unchecked((long)0xFFF8000000000000UL);
    [MethodImpl(NI)] static long A456(long x) => x + unchecked((long)0xFFF8000000000000UL);
    [MethodImpl(NI)] static long C456(long x) { long r = (x < unchecked((long)0xFFF8000000000000UL) ? 1 : 0) + ((ulong)x < 0xFFF8000000000000UL ? 2 : 0) + (x == unchecked((long)0xFFF8000000000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFF8000000000000UL)); return r * 31 + (x ^ unchecked((long)0xFFF8000000000000UL)); }
    [MethodImpl(NI)] static long K457() => unchecked((long)0xFFF8000100000000UL);
    [MethodImpl(NI)] static long A457(long x) => x + unchecked((long)0xFFF8000100000000UL);
    [MethodImpl(NI)] static long C457(long x) { long r = (x < unchecked((long)0xFFF8000100000000UL) ? 1 : 0) + ((ulong)x < 0xFFF8000100000000UL ? 2 : 0) + (x == unchecked((long)0xFFF8000100000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFF8000100000000UL)); return r * 31 + (x ^ unchecked((long)0xFFF8000100000000UL)); }
    [MethodImpl(NI)] static long K458() => unchecked((long)0xFFFBFFF000000000UL);
    [MethodImpl(NI)] static long A458(long x) => x + unchecked((long)0xFFFBFFF000000000UL);
    [MethodImpl(NI)] static long C458(long x) { long r = (x < unchecked((long)0xFFFBFFF000000000UL) ? 1 : 0) + ((ulong)x < 0xFFFBFFF000000000UL ? 2 : 0) + (x == unchecked((long)0xFFFBFFF000000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFBFFF000000000UL)); return r * 31 + (x ^ unchecked((long)0xFFFBFFF000000000UL)); }
    [MethodImpl(NI)] static long K459() => unchecked((long)0xFFFF7FFFFFFFFFFFUL);
    [MethodImpl(NI)] static long A459(long x) => x + unchecked((long)0xFFFF7FFFFFFFFFFFUL);
    [MethodImpl(NI)] static long C459(long x) { long r = (x < unchecked((long)0xFFFF7FFFFFFFFFFFUL) ? 1 : 0) + ((ulong)x < 0xFFFF7FFFFFFFFFFFUL ? 2 : 0) + (x == unchecked((long)0xFFFF7FFFFFFFFFFFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFF7FFFFFFFFFFFUL)); return r * 31 + (x ^ unchecked((long)0xFFFF7FFFFFFFFFFFUL)); }
    [MethodImpl(NI)] static long K460() => unchecked((long)0xFFFF800000000000UL);
    [MethodImpl(NI)] static long A460(long x) => x + unchecked((long)0xFFFF800000000000UL);
    [MethodImpl(NI)] static long C460(long x) { long r = (x < unchecked((long)0xFFFF800000000000UL) ? 1 : 0) + ((ulong)x < 0xFFFF800000000000UL ? 2 : 0) + (x == unchecked((long)0xFFFF800000000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFF800000000000UL)); return r * 31 + (x ^ unchecked((long)0xFFFF800000000000UL)); }
    [MethodImpl(NI)] static long K461() => unchecked((long)0xFFFF8000000007FFUL);
    [MethodImpl(NI)] static long A461(long x) => x + unchecked((long)0xFFFF8000000007FFUL);
    [MethodImpl(NI)] static long C461(long x) { long r = (x < unchecked((long)0xFFFF8000000007FFUL) ? 1 : 0) + ((ulong)x < 0xFFFF8000000007FFUL ? 2 : 0) + (x == unchecked((long)0xFFFF8000000007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFF8000000007FFUL)); return r * 31 + (x ^ unchecked((long)0xFFFF8000000007FFUL)); }
    [MethodImpl(NI)] static long K462() => unchecked((long)0xFFFF800007FFFFFFUL);
    [MethodImpl(NI)] static long A462(long x) => x + unchecked((long)0xFFFF800007FFFFFFUL);
    [MethodImpl(NI)] static long C462(long x) { long r = (x < unchecked((long)0xFFFF800007FFFFFFUL) ? 1 : 0) + ((ulong)x < 0xFFFF800007FFFFFFUL ? 2 : 0) + (x == unchecked((long)0xFFFF800007FFFFFFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFF800007FFFFFFUL)); return r * 31 + (x ^ unchecked((long)0xFFFF800007FFFFFFUL)); }
    [MethodImpl(NI)] static long K463() => unchecked((long)0xFFFF800010000000UL);
    [MethodImpl(NI)] static long A463(long x) => x + unchecked((long)0xFFFF800010000000UL);
    [MethodImpl(NI)] static long C463(long x) { long r = (x < unchecked((long)0xFFFF800010000000UL) ? 1 : 0) + ((ulong)x < 0xFFFF800010000000UL ? 2 : 0) + (x == unchecked((long)0xFFFF800010000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFF800010000000UL)); return r * 31 + (x ^ unchecked((long)0xFFFF800010000000UL)); }
    [MethodImpl(NI)] static long K464() => unchecked((long)0xFFFFBFFF00000000UL);
    [MethodImpl(NI)] static long A464(long x) => x + unchecked((long)0xFFFFBFFF00000000UL);
    [MethodImpl(NI)] static long C464(long x) { long r = (x < unchecked((long)0xFFFFBFFF00000000UL) ? 1 : 0) + ((ulong)x < 0xFFFFBFFF00000000UL ? 2 : 0) + (x == unchecked((long)0xFFFFBFFF00000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFBFFF00000000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFBFFF00000000UL)); }
    [MethodImpl(NI)] static long K465() => unchecked((long)0xFFFFEDCB00000000UL);
    [MethodImpl(NI)] static long A465(long x) => x + unchecked((long)0xFFFFEDCB00000000UL);
    [MethodImpl(NI)] static long C465(long x) { long r = (x < unchecked((long)0xFFFFEDCB00000000UL) ? 1 : 0) + ((ulong)x < 0xFFFFEDCB00000000UL ? 2 : 0) + (x == unchecked((long)0xFFFFEDCB00000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFEDCB00000000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFEDCB00000000UL)); }
    [MethodImpl(NI)] static long K466() => unchecked((long)0xFFFFEFFFFFFFF800UL);
    [MethodImpl(NI)] static long A466(long x) => x + unchecked((long)0xFFFFEFFFFFFFF800UL);
    [MethodImpl(NI)] static long C466(long x) { long r = (x < unchecked((long)0xFFFFEFFFFFFFF800UL) ? 1 : 0) + ((ulong)x < 0xFFFFEFFFFFFFF800UL ? 2 : 0) + (x == unchecked((long)0xFFFFEFFFFFFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFEFFFFFFFF800UL)); return r * 31 + (x ^ unchecked((long)0xFFFFEFFFFFFFF800UL)); }
    [MethodImpl(NI)] static long K467() => unchecked((long)0xFFFFF00000000000UL);
    [MethodImpl(NI)] static long A467(long x) => x + unchecked((long)0xFFFFF00000000000UL);
    [MethodImpl(NI)] static long C467(long x) { long r = (x < unchecked((long)0xFFFFF00000000000UL) ? 1 : 0) + ((ulong)x < 0xFFFFF00000000000UL ? 2 : 0) + (x == unchecked((long)0xFFFFF00000000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFF00000000000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFF00000000000UL)); }
    [MethodImpl(NI)] static long K468() => unchecked((long)0xFFFFF000000007FFUL);
    [MethodImpl(NI)] static long A468(long x) => x + unchecked((long)0xFFFFF000000007FFUL);
    [MethodImpl(NI)] static long C468(long x) { long r = (x < unchecked((long)0xFFFFF000000007FFUL) ? 1 : 0) + ((ulong)x < 0xFFFFF000000007FFUL ? 2 : 0) + (x == unchecked((long)0xFFFFF000000007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFF000000007FFUL)); return r * 31 + (x ^ unchecked((long)0xFFFFF000000007FFUL)); }
    [MethodImpl(NI)] static long K469() => unchecked((long)0xFFFFF000FFFFF000UL);
    [MethodImpl(NI)] static long A469(long x) => x + unchecked((long)0xFFFFF000FFFFF000UL);
    [MethodImpl(NI)] static long C469(long x) { long r = (x < unchecked((long)0xFFFFF000FFFFF000UL) ? 1 : 0) + ((ulong)x < 0xFFFFF000FFFFF000UL ? 2 : 0) + (x == unchecked((long)0xFFFFF000FFFFF000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFF000FFFFF000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFF000FFFFF000UL)); }
    [MethodImpl(NI)] static long K470() => unchecked((long)0xFFFFF004F522820EUL);
    [MethodImpl(NI)] static long A470(long x) => x + unchecked((long)0xFFFFF004F522820EUL);
    [MethodImpl(NI)] static long C470(long x) { long r = (x < unchecked((long)0xFFFFF004F522820EUL) ? 1 : 0) + ((ulong)x < 0xFFFFF004F522820EUL ? 2 : 0) + (x == unchecked((long)0xFFFFF004F522820EUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFF004F522820EUL)); return r * 31 + (x ^ unchecked((long)0xFFFFF004F522820EUL)); }
    [MethodImpl(NI)] static long K471() => unchecked((long)0xFFFFF004F5228800UL);
    [MethodImpl(NI)] static long A471(long x) => x + unchecked((long)0xFFFFF004F5228800UL);
    [MethodImpl(NI)] static long C471(long x) { long r = (x < unchecked((long)0xFFFFF004F5228800UL) ? 1 : 0) + ((ulong)x < 0xFFFFF004F5228800UL ? 2 : 0) + (x == unchecked((long)0xFFFFF004F5228800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFF004F5228800UL)); return r * 31 + (x ^ unchecked((long)0xFFFFF004F5228800UL)); }
    [MethodImpl(NI)] static long K472() => unchecked((long)0xFFFFF2D34F020000UL);
    [MethodImpl(NI)] static long A472(long x) => x + unchecked((long)0xFFFFF2D34F020000UL);
    [MethodImpl(NI)] static long C472(long x) { long r = (x < unchecked((long)0xFFFFF2D34F020000UL) ? 1 : 0) + ((ulong)x < 0xFFFFF2D34F020000UL ? 2 : 0) + (x == unchecked((long)0xFFFFF2D34F020000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFF2D34F020000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFF2D34F020000UL)); }
    [MethodImpl(NI)] static long K473() => unchecked((long)0xFFFFF2D34F02053FUL);
    [MethodImpl(NI)] static long A473(long x) => x + unchecked((long)0xFFFFF2D34F02053FUL);
    [MethodImpl(NI)] static long C473(long x) { long r = (x < unchecked((long)0xFFFFF2D34F02053FUL) ? 1 : 0) + ((ulong)x < 0xFFFFF2D34F02053FUL ? 2 : 0) + (x == unchecked((long)0xFFFFF2D34F02053FUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFF2D34F02053FUL)); return r * 31 + (x ^ unchecked((long)0xFFFFF2D34F02053FUL)); }
    [MethodImpl(NI)] static long K474() => unchecked((long)0xFFFFF7FEFFFFF800UL);
    [MethodImpl(NI)] static long A474(long x) => x + unchecked((long)0xFFFFF7FEFFFFF800UL);
    [MethodImpl(NI)] static long C474(long x) { long r = (x < unchecked((long)0xFFFFF7FEFFFFF800UL) ? 1 : 0) + ((ulong)x < 0xFFFFF7FEFFFFF800UL ? 2 : 0) + (x == unchecked((long)0xFFFFF7FEFFFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFF7FEFFFFF800UL)); return r * 31 + (x ^ unchecked((long)0xFFFFF7FEFFFFF800UL)); }
    [MethodImpl(NI)] static long K475() => unchecked((long)0xFFFFF7FF00000000UL);
    [MethodImpl(NI)] static long A475(long x) => x + unchecked((long)0xFFFFF7FF00000000UL);
    [MethodImpl(NI)] static long C475(long x) { long r = (x < unchecked((long)0xFFFFF7FF00000000UL) ? 1 : 0) + ((ulong)x < 0xFFFFF7FF00000000UL ? 2 : 0) + (x == unchecked((long)0xFFFFF7FF00000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFF7FF00000000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFF7FF00000000UL)); }
    [MethodImpl(NI)] static long K476() => unchecked((long)0xFFFFF7FF000007FFUL);
    [MethodImpl(NI)] static long A476(long x) => x + unchecked((long)0xFFFFF7FF000007FFUL);
    [MethodImpl(NI)] static long C476(long x) { long r = (x < unchecked((long)0xFFFFF7FF000007FFUL) ? 1 : 0) + ((ulong)x < 0xFFFFF7FF000007FFUL ? 2 : 0) + (x == unchecked((long)0xFFFFF7FF000007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFF7FF000007FFUL)); return r * 31 + (x ^ unchecked((long)0xFFFFF7FF000007FFUL)); }
    [MethodImpl(NI)] static long K477() => unchecked((long)0xFFFFF7FFFFFFF7FFUL);
    [MethodImpl(NI)] static long A477(long x) => x + unchecked((long)0xFFFFF7FFFFFFF7FFUL);
    [MethodImpl(NI)] static long C477(long x) { long r = (x < unchecked((long)0xFFFFF7FFFFFFF7FFUL) ? 1 : 0) + ((ulong)x < 0xFFFFF7FFFFFFF7FFUL ? 2 : 0) + (x == unchecked((long)0xFFFFF7FFFFFFF7FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFF7FFFFFFF7FFUL)); return r * 31 + (x ^ unchecked((long)0xFFFFF7FFFFFFF7FFUL)); }
    [MethodImpl(NI)] static long K478() => unchecked((long)0xFFFFF7FFFFFFF800UL);
    [MethodImpl(NI)] static long A478(long x) => x + unchecked((long)0xFFFFF7FFFFFFF800UL);
    [MethodImpl(NI)] static long C478(long x) { long r = (x < unchecked((long)0xFFFFF7FFFFFFF800UL) ? 1 : 0) + ((ulong)x < 0xFFFFF7FFFFFFF800UL ? 2 : 0) + (x == unchecked((long)0xFFFFF7FFFFFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFF7FFFFFFF800UL)); return r * 31 + (x ^ unchecked((long)0xFFFFF7FFFFFFF800UL)); }
    [MethodImpl(NI)] static long K479() => unchecked((long)0xFFFFF80000000000UL);
    [MethodImpl(NI)] static long A479(long x) => x + unchecked((long)0xFFFFF80000000000UL);
    [MethodImpl(NI)] static long C479(long x) { long r = (x < unchecked((long)0xFFFFF80000000000UL) ? 1 : 0) + ((ulong)x < 0xFFFFF80000000000UL ? 2 : 0) + (x == unchecked((long)0xFFFFF80000000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFF80000000000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFF80000000000UL)); }
    [MethodImpl(NI)] static long K480() => unchecked((long)0xFFFFF800000007FFUL);
    [MethodImpl(NI)] static long A480(long x) => x + unchecked((long)0xFFFFF800000007FFUL);
    [MethodImpl(NI)] static long C480(long x) { long r = (x < unchecked((long)0xFFFFF800000007FFUL) ? 1 : 0) + ((ulong)x < 0xFFFFF800000007FFUL ? 2 : 0) + (x == unchecked((long)0xFFFFF800000007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFF800000007FFUL)); return r * 31 + (x ^ unchecked((long)0xFFFFF800000007FFUL)); }
    [MethodImpl(NI)] static long K481() => unchecked((long)0xFFFFF80001000000UL);
    [MethodImpl(NI)] static long A481(long x) => x + unchecked((long)0xFFFFF80001000000UL);
    [MethodImpl(NI)] static long C481(long x) { long r = (x < unchecked((long)0xFFFFF80001000000UL) ? 1 : 0) + ((ulong)x < 0xFFFFF80001000000UL ? 2 : 0) + (x == unchecked((long)0xFFFFF80001000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFF80001000000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFF80001000000UL)); }
    [MethodImpl(NI)] static long K482() => unchecked((long)0xFFFFF800FFFFF800UL);
    [MethodImpl(NI)] static long A482(long x) => x + unchecked((long)0xFFFFF800FFFFF800UL);
    [MethodImpl(NI)] static long C482(long x) { long r = (x < unchecked((long)0xFFFFF800FFFFF800UL) ? 1 : 0) + ((ulong)x < 0xFFFFF800FFFFF800UL ? 2 : 0) + (x == unchecked((long)0xFFFFF800FFFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFF800FFFFF800UL)); return r * 31 + (x ^ unchecked((long)0xFFFFF800FFFFF800UL)); }
    [MethodImpl(NI)] static long K483() => unchecked((long)0xFFFFFBFFF0000000UL);
    [MethodImpl(NI)] static long A483(long x) => x + unchecked((long)0xFFFFFBFFF0000000UL);
    [MethodImpl(NI)] static long C483(long x) { long r = (x < unchecked((long)0xFFFFFBFFF0000000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFBFFF0000000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFBFFF0000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFBFFF0000000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFBFFF0000000UL)); }
    [MethodImpl(NI)] static long K484() => unchecked((long)0xFFFFFEDCB0000000UL);
    [MethodImpl(NI)] static long A484(long x) => x + unchecked((long)0xFFFFFEDCB0000000UL);
    [MethodImpl(NI)] static long C484(long x) { long r = (x < unchecked((long)0xFFFFFEDCB0000000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFEDCB0000000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFEDCB0000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFEDCB0000000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFEDCB0000000UL)); }
    [MethodImpl(NI)] static long K485() => unchecked((long)0xFFFFFF8000000000UL);
    [MethodImpl(NI)] static long A485(long x) => x + unchecked((long)0xFFFFFF8000000000UL);
    [MethodImpl(NI)] static long C485(long x) { long r = (x < unchecked((long)0xFFFFFF8000000000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFF8000000000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFF8000000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFF8000000000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFF8000000000UL)); }
    [MethodImpl(NI)] static long K486() => unchecked((long)0xFFFFFF8000100000UL);
    [MethodImpl(NI)] static long A486(long x) => x + unchecked((long)0xFFFFFF8000100000UL);
    [MethodImpl(NI)] static long C486(long x) { long r = (x < unchecked((long)0xFFFFFF8000100000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFF8000100000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFF8000100000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFF8000100000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFF8000100000UL)); }
    [MethodImpl(NI)] static long K487() => unchecked((long)0xFFFFFFBFFF000000UL);
    [MethodImpl(NI)] static long A487(long x) => x + unchecked((long)0xFFFFFFBFFF000000UL);
    [MethodImpl(NI)] static long C487(long x) { long r = (x < unchecked((long)0xFFFFFFBFFF000000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFBFFF000000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFBFFF000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFBFFF000000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFBFFF000000UL)); }
    [MethodImpl(NI)] static long K488() => unchecked((long)0xFFFFFFEDCB000000UL);
    [MethodImpl(NI)] static long A488(long x) => x + unchecked((long)0xFFFFFFEDCB000000UL);
    [MethodImpl(NI)] static long C488(long x) { long r = (x < unchecked((long)0xFFFFFFEDCB000000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFEDCB000000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFEDCB000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFEDCB000000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFEDCB000000UL)); }
    [MethodImpl(NI)] static long K489() => unchecked((long)0xFFFFFFF800000000UL);
    [MethodImpl(NI)] static long A489(long x) => x + unchecked((long)0xFFFFFFF800000000UL);
    [MethodImpl(NI)] static long C489(long x) { long r = (x < unchecked((long)0xFFFFFFF800000000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFF800000000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFF800000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFF800000000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFF800000000UL)); }
    [MethodImpl(NI)] static long K490() => unchecked((long)0xFFFFFFF8000007FFUL);
    [MethodImpl(NI)] static long A490(long x) => x + unchecked((long)0xFFFFFFF8000007FFUL);
    [MethodImpl(NI)] static long C490(long x) { long r = (x < unchecked((long)0xFFFFFFF8000007FFUL) ? 1 : 0) + ((ulong)x < 0xFFFFFFF8000007FFUL ? 2 : 0) + (x == unchecked((long)0xFFFFFFF8000007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFF8000007FFUL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFF8000007FFUL)); }
    [MethodImpl(NI)] static long K491() => unchecked((long)0xFFFFFFF800010000UL);
    [MethodImpl(NI)] static long A491(long x) => x + unchecked((long)0xFFFFFFF800010000UL);
    [MethodImpl(NI)] static long C491(long x) { long r = (x < unchecked((long)0xFFFFFFF800010000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFF800010000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFF800010000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFF800010000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFF800010000UL)); }
    [MethodImpl(NI)] static long K492() => unchecked((long)0xFFFFFFFBFFF00000UL);
    [MethodImpl(NI)] static long A492(long x) => x + unchecked((long)0xFFFFFFFBFFF00000UL);
    [MethodImpl(NI)] static long C492(long x) { long r = (x < unchecked((long)0xFFFFFFFBFFF00000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFBFFF00000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFBFFF00000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFBFFF00000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFBFFF00000UL)); }
    [MethodImpl(NI)] static long K493() => unchecked((long)0xFFFFFFFC00000000UL);
    [MethodImpl(NI)] static long A493(long x) => x + unchecked((long)0xFFFFFFFC00000000UL);
    [MethodImpl(NI)] static long C493(long x) { long r = (x < unchecked((long)0xFFFFFFFC00000000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFC00000000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFC00000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFC00000000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFC00000000UL)); }
    [MethodImpl(NI)] static long K494() => unchecked((long)0xFFFFFFFC00008000UL);
    [MethodImpl(NI)] static long A494(long x) => x + unchecked((long)0xFFFFFFFC00008000UL);
    [MethodImpl(NI)] static long C494(long x) { long r = (x < unchecked((long)0xFFFFFFFC00008000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFC00008000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFC00008000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFC00008000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFC00008000UL)); }
    [MethodImpl(NI)] static long K495() => unchecked((long)0xFFFFFFFDFFF80000UL);
    [MethodImpl(NI)] static long A495(long x) => x + unchecked((long)0xFFFFFFFDFFF80000UL);
    [MethodImpl(NI)] static long C495(long x) { long r = (x < unchecked((long)0xFFFFFFFDFFF80000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFDFFF80000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFDFFF80000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFDFFF80000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFDFFF80000UL)); }
    [MethodImpl(NI)] static long K496() => unchecked((long)0xFFFFFFFE00000000UL);
    [MethodImpl(NI)] static long A496(long x) => x + unchecked((long)0xFFFFFFFE00000000UL);
    [MethodImpl(NI)] static long C496(long x) { long r = (x < unchecked((long)0xFFFFFFFE00000000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFE00000000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFE00000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFE00000000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFE00000000UL)); }
    [MethodImpl(NI)] static long K497() => unchecked((long)0xFFFFFFFE00004000UL);
    [MethodImpl(NI)] static long A497(long x) => x + unchecked((long)0xFFFFFFFE00004000UL);
    [MethodImpl(NI)] static long C497(long x) { long r = (x < unchecked((long)0xFFFFFFFE00004000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFE00004000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFE00004000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFE00004000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFE00004000UL)); }
    [MethodImpl(NI)] static long K498() => unchecked((long)0xFFFFFFFE16E456A0UL);
    [MethodImpl(NI)] static long A498(long x) => x + unchecked((long)0xFFFFFFFE16E456A0UL);
    [MethodImpl(NI)] static long C498(long x) { long r = (x < unchecked((long)0xFFFFFFFE16E456A0UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFE16E456A0UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFE16E456A0UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFE16E456A0UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFE16E456A0UL)); }
    [MethodImpl(NI)] static long K499() => unchecked((long)0xFFFFFFFE16E45FFFUL);
    [MethodImpl(NI)] static long A499(long x) => x + unchecked((long)0xFFFFFFFE16E45FFFUL);
    [MethodImpl(NI)] static long C499(long x) { long r = (x < unchecked((long)0xFFFFFFFE16E45FFFUL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFE16E45FFFUL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFE16E45FFFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFE16E45FFFUL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFE16E45FFFUL)); }
    [MethodImpl(NI)] static long K500() => unchecked((long)0xFFFFFFFEDCB00000UL);
    [MethodImpl(NI)] static long A500(long x) => x + unchecked((long)0xFFFFFFFEDCB00000UL);
    [MethodImpl(NI)] static long C500(long x) { long r = (x < unchecked((long)0xFFFFFFFEDCB00000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFEDCB00000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFEDCB00000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFEDCB00000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFEDCB00000UL)); }
    [MethodImpl(NI)] static long K501() => unchecked((long)0xFFFFFFFEFFFC0000UL);
    [MethodImpl(NI)] static long A501(long x) => x + unchecked((long)0xFFFFFFFEFFFC0000UL);
    [MethodImpl(NI)] static long C501(long x) { long r = (x < unchecked((long)0xFFFFFFFEFFFC0000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFEFFFC0000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFEFFFC0000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFEFFFC0000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFEFFFC0000UL)); }
    [MethodImpl(NI)] static long K502() => unchecked((long)0xFFFFFFFEFFFFF800UL);
    [MethodImpl(NI)] static long A502(long x) => x + unchecked((long)0xFFFFFFFEFFFFF800UL);
    [MethodImpl(NI)] static long C502(long x) { long r = (x < unchecked((long)0xFFFFFFFEFFFFF800UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFEFFFFF800UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFEFFFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFEFFFFF800UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFEFFFFF800UL)); }
    [MethodImpl(NI)] static long K503() => unchecked((long)0xFFFFFFFF00000000UL);
    [MethodImpl(NI)] static long A503(long x) => x + unchecked((long)0xFFFFFFFF00000000UL);
    [MethodImpl(NI)] static long C503(long x) { long r = (x < unchecked((long)0xFFFFFFFF00000000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF00000000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF00000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF00000000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF00000000UL)); }
    [MethodImpl(NI)] static long K504() => unchecked((long)0xFFFFFFFF00000001UL);
    [MethodImpl(NI)] static long A504(long x) => x + unchecked((long)0xFFFFFFFF00000001UL);
    [MethodImpl(NI)] static long C504(long x) { long r = (x < unchecked((long)0xFFFFFFFF00000001UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF00000001UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF00000001UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF00000001UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF00000001UL)); }
    [MethodImpl(NI)] static long K505() => unchecked((long)0xFFFFFFFF000007FFUL);
    [MethodImpl(NI)] static long A505(long x) => x + unchecked((long)0xFFFFFFFF000007FFUL);
    [MethodImpl(NI)] static long C505(long x) { long r = (x < unchecked((long)0xFFFFFFFF000007FFUL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF000007FFUL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF000007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF000007FFUL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF000007FFUL)); }
    [MethodImpl(NI)] static long K506() => unchecked((long)0xFFFFFFFF00000800UL);
    [MethodImpl(NI)] static long A506(long x) => x + unchecked((long)0xFFFFFFFF00000800UL);
    [MethodImpl(NI)] static long C506(long x) { long r = (x < unchecked((long)0xFFFFFFFF00000800UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF00000800UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF00000800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF00000800UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF00000800UL)); }
    [MethodImpl(NI)] static long K507() => unchecked((long)0xFFFFFFFF00000801UL);
    [MethodImpl(NI)] static long A507(long x) => x + unchecked((long)0xFFFFFFFF00000801UL);
    [MethodImpl(NI)] static long C507(long x) { long r = (x < unchecked((long)0xFFFFFFFF00000801UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF00000801UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF00000801UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF00000801UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF00000801UL)); }
    [MethodImpl(NI)] static long K508() => unchecked((long)0xFFFFFFFF00000FFFUL);
    [MethodImpl(NI)] static long A508(long x) => x + unchecked((long)0xFFFFFFFF00000FFFUL);
    [MethodImpl(NI)] static long C508(long x) { long r = (x < unchecked((long)0xFFFFFFFF00000FFFUL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF00000FFFUL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF00000FFFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF00000FFFUL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF00000FFFUL)); }
    [MethodImpl(NI)] static long K509() => unchecked((long)0xFFFFFFFF00001000UL);
    [MethodImpl(NI)] static long A509(long x) => x + unchecked((long)0xFFFFFFFF00001000UL);
    [MethodImpl(NI)] static long C509(long x) { long r = (x < unchecked((long)0xFFFFFFFF00001000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF00001000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF00001000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF00001000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF00001000UL)); }
    [MethodImpl(NI)] static long K510() => unchecked((long)0xFFFFFFFF000017FFUL);
    [MethodImpl(NI)] static long A510(long x) => x + unchecked((long)0xFFFFFFFF000017FFUL);
    [MethodImpl(NI)] static long C510(long x) { long r = (x < unchecked((long)0xFFFFFFFF000017FFUL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF000017FFUL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF000017FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF000017FFUL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF000017FFUL)); }
    [MethodImpl(NI)] static long K511() => unchecked((long)0xFFFFFFFF00001800UL);
    [MethodImpl(NI)] static long A511(long x) => x + unchecked((long)0xFFFFFFFF00001800UL);
    [MethodImpl(NI)] static long C511(long x) { long r = (x < unchecked((long)0xFFFFFFFF00001800UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF00001800UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF00001800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF00001800UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF00001800UL)); }
    [MethodImpl(NI)] static long K512() => unchecked((long)0xFFFFFFFF00002000UL);
    [MethodImpl(NI)] static long A512(long x) => x + unchecked((long)0xFFFFFFFF00002000UL);
    [MethodImpl(NI)] static long C512(long x) { long r = (x < unchecked((long)0xFFFFFFFF00002000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF00002000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF00002000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF00002000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF00002000UL)); }
    [MethodImpl(NI)] static long K513() => unchecked((long)0xFFFFFFFF12345678UL);
    [MethodImpl(NI)] static long A513(long x) => x + unchecked((long)0xFFFFFFFF12345678UL);
    [MethodImpl(NI)] static long C513(long x) { long r = (x < unchecked((long)0xFFFFFFFF12345678UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF12345678UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF12345678UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF12345678UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF12345678UL)); }
    [MethodImpl(NI)] static long K514() => unchecked((long)0xFFFFFFFF2FFFC000UL);
    [MethodImpl(NI)] static long A514(long x) => x + unchecked((long)0xFFFFFFFF2FFFC000UL);
    [MethodImpl(NI)] static long C514(long x) { long r = (x < unchecked((long)0xFFFFFFFF2FFFC000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF2FFFC000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF2FFFC000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF2FFFC000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF2FFFC000UL)); }
    [MethodImpl(NI)] static long K515() => unchecked((long)0xFFFFFFFF3FFFF7FFUL);
    [MethodImpl(NI)] static long A515(long x) => x + unchecked((long)0xFFFFFFFF3FFFF7FFUL);
    [MethodImpl(NI)] static long C515(long x) { long r = (x < unchecked((long)0xFFFFFFFF3FFFF7FFUL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF3FFFF7FFUL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF3FFFF7FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF3FFFF7FFUL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF3FFFF7FFUL)); }
    [MethodImpl(NI)] static long K516() => unchecked((long)0xFFFFFFFF3FFFF800UL);
    [MethodImpl(NI)] static long A516(long x) => x + unchecked((long)0xFFFFFFFF3FFFF800UL);
    [MethodImpl(NI)] static long C516(long x) { long r = (x < unchecked((long)0xFFFFFFFF3FFFF800UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF3FFFF800UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF3FFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF3FFFF800UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF3FFFF800UL)); }
    [MethodImpl(NI)] static long K517() => unchecked((long)0xFFFFFFFF4000FFFFUL);
    [MethodImpl(NI)] static long A517(long x) => x + unchecked((long)0xFFFFFFFF4000FFFFUL);
    [MethodImpl(NI)] static long C517(long x) { long r = (x < unchecked((long)0xFFFFFFFF4000FFFFUL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF4000FFFFUL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF4000FFFFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF4000FFFFUL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF4000FFFFUL)); }
    [MethodImpl(NI)] static long K518() => unchecked((long)0xFFFFFFFF40010000UL);
    [MethodImpl(NI)] static long A518(long x) => x + unchecked((long)0xFFFFFFFF40010000UL);
    [MethodImpl(NI)] static long C518(long x) { long r = (x < unchecked((long)0xFFFFFFFF40010000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF40010000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF40010000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF40010000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF40010000UL)); }
    [MethodImpl(NI)] static long K519() => unchecked((long)0xFFFFFFFF40010007UL);
    [MethodImpl(NI)] static long A519(long x) => x + unchecked((long)0xFFFFFFFF40010007UL);
    [MethodImpl(NI)] static long C519(long x) { long r = (x < unchecked((long)0xFFFFFFFF40010007UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF40010007UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF40010007UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF40010007UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF40010007UL)); }
    [MethodImpl(NI)] static long K520() => unchecked((long)0xFFFFFFFF40010008UL);
    [MethodImpl(NI)] static long A520(long x) => x + unchecked((long)0xFFFFFFFF40010008UL);
    [MethodImpl(NI)] static long C520(long x) { long r = (x < unchecked((long)0xFFFFFFFF40010008UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF40010008UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF40010008UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF40010008UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF40010008UL)); }
    [MethodImpl(NI)] static long K521() => unchecked((long)0xFFFFFFFF4B1237FFUL);
    [MethodImpl(NI)] static long A521(long x) => x + unchecked((long)0xFFFFFFFF4B1237FFUL);
    [MethodImpl(NI)] static long C521(long x) { long r = (x < unchecked((long)0xFFFFFFFF4B1237FFUL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF4B1237FFUL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF4B1237FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF4B1237FFUL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF4B1237FFUL)); }
    [MethodImpl(NI)] static long K522() => unchecked((long)0xFFFFFFFF4B123C37UL);
    [MethodImpl(NI)] static long A522(long x) => x + unchecked((long)0xFFFFFFFF4B123C37UL);
    [MethodImpl(NI)] static long C522(long x) { long r = (x < unchecked((long)0xFFFFFFFF4B123C37UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF4B123C37UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF4B123C37UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF4B123C37UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF4B123C37UL)); }
    [MethodImpl(NI)] static long K523() => unchecked((long)0xFFFFFFFF789ABCDEUL);
    [MethodImpl(NI)] static long A523(long x) => x + unchecked((long)0xFFFFFFFF789ABCDEUL);
    [MethodImpl(NI)] static long C523(long x) { long r = (x < unchecked((long)0xFFFFFFFF789ABCDEUL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF789ABCDEUL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF789ABCDEUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF789ABCDEUL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF789ABCDEUL)); }
    [MethodImpl(NI)] static long K524() => unchecked((long)0xFFFFFFFF789ABCDFUL);
    [MethodImpl(NI)] static long A524(long x) => x + unchecked((long)0xFFFFFFFF789ABCDFUL);
    [MethodImpl(NI)] static long C524(long x) { long r = (x < unchecked((long)0xFFFFFFFF789ABCDFUL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF789ABCDFUL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF789ABCDFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF789ABCDFUL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF789ABCDFUL)); }
    [MethodImpl(NI)] static long K525() => unchecked((long)0xFFFFFFFF7FF007FFUL);
    [MethodImpl(NI)] static long A525(long x) => x + unchecked((long)0xFFFFFFFF7FF007FFUL);
    [MethodImpl(NI)] static long C525(long x) { long r = (x < unchecked((long)0xFFFFFFFF7FF007FFUL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF7FF007FFUL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF7FF007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF7FF007FFUL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF7FF007FFUL)); }
    [MethodImpl(NI)] static long K526() => unchecked((long)0xFFFFFFFF7FFE0000UL);
    [MethodImpl(NI)] static long A526(long x) => x + unchecked((long)0xFFFFFFFF7FFE0000UL);
    [MethodImpl(NI)] static long C526(long x) { long r = (x < unchecked((long)0xFFFFFFFF7FFE0000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF7FFE0000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF7FFE0000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF7FFE0000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF7FFE0000UL)); }
    [MethodImpl(NI)] static long K527() => unchecked((long)0xFFFFFFFF7FFFF000UL);
    [MethodImpl(NI)] static long A527(long x) => x + unchecked((long)0xFFFFFFFF7FFFF000UL);
    [MethodImpl(NI)] static long C527(long x) { long r = (x < unchecked((long)0xFFFFFFFF7FFFF000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF7FFFF000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF7FFFF000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF7FFFF000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF7FFFF000UL)); }
    [MethodImpl(NI)] static long K528() => unchecked((long)0xFFFFFFFF7FFFF7FEUL);
    [MethodImpl(NI)] static long A528(long x) => x + unchecked((long)0xFFFFFFFF7FFFF7FEUL);
    [MethodImpl(NI)] static long C528(long x) { long r = (x < unchecked((long)0xFFFFFFFF7FFFF7FEUL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF7FFFF7FEUL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF7FFFF7FEUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF7FFFF7FEUL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF7FFFF7FEUL)); }
    [MethodImpl(NI)] static long K529() => unchecked((long)0xFFFFFFFF7FFFF7FFUL);
    [MethodImpl(NI)] static long A529(long x) => x + unchecked((long)0xFFFFFFFF7FFFF7FFUL);
    [MethodImpl(NI)] static long C529(long x) { long r = (x < unchecked((long)0xFFFFFFFF7FFFF7FFUL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF7FFFF7FFUL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF7FFFF7FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF7FFFF7FFUL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF7FFFF7FFUL)); }
    [MethodImpl(NI)] static long K530() => unchecked((long)0xFFFFFFFF7FFFF800UL);
    [MethodImpl(NI)] static long A530(long x) => x + unchecked((long)0xFFFFFFFF7FFFF800UL);
    [MethodImpl(NI)] static long C530(long x) { long r = (x < unchecked((long)0xFFFFFFFF7FFFF800UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF7FFFF800UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF7FFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF7FFFF800UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF7FFFF800UL)); }
    [MethodImpl(NI)] static long K531() => unchecked((long)0xFFFFFFFF7FFFF801UL);
    [MethodImpl(NI)] static long A531(long x) => x + unchecked((long)0xFFFFFFFF7FFFF801UL);
    [MethodImpl(NI)] static long C531(long x) { long r = (x < unchecked((long)0xFFFFFFFF7FFFF801UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF7FFFF801UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF7FFFF801UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF7FFFF801UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF7FFFF801UL)); }
    [MethodImpl(NI)] static long K532() => unchecked((long)0xFFFFFFFF7FFFFFFEUL);
    [MethodImpl(NI)] static long A532(long x) => x + unchecked((long)0xFFFFFFFF7FFFFFFEUL);
    [MethodImpl(NI)] static long C532(long x) { long r = (x < unchecked((long)0xFFFFFFFF7FFFFFFEUL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF7FFFFFFEUL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF7FFFFFFEUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF7FFFFFFEUL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF7FFFFFFEUL)); }
    [MethodImpl(NI)] static long K533() => unchecked((long)0xFFFFFFFF7FFFFFFFUL);
    [MethodImpl(NI)] static long A533(long x) => x + unchecked((long)0xFFFFFFFF7FFFFFFFUL);
    [MethodImpl(NI)] static long C533(long x) { long r = (x < unchecked((long)0xFFFFFFFF7FFFFFFFUL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF7FFFFFFFUL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF7FFFFFFFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF7FFFFFFFUL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF7FFFFFFFUL)); }
    [MethodImpl(NI)] static long K534() => unchecked((long)0xFFFFFFFF80000000UL);
    [MethodImpl(NI)] static long A534(long x) => x + unchecked((long)0xFFFFFFFF80000000UL);
    [MethodImpl(NI)] static long C534(long x) { long r = (x < unchecked((long)0xFFFFFFFF80000000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF80000000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF80000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF80000000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF80000000UL)); }
    [MethodImpl(NI)] static long K535() => unchecked((long)0xFFFFFFFF80000001UL);
    [MethodImpl(NI)] static long A535(long x) => x + unchecked((long)0xFFFFFFFF80000001UL);
    [MethodImpl(NI)] static long C535(long x) { long r = (x < unchecked((long)0xFFFFFFFF80000001UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF80000001UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF80000001UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF80000001UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF80000001UL)); }
    [MethodImpl(NI)] static long K536() => unchecked((long)0xFFFFFFFF800007FEUL);
    [MethodImpl(NI)] static long A536(long x) => x + unchecked((long)0xFFFFFFFF800007FEUL);
    [MethodImpl(NI)] static long C536(long x) { long r = (x < unchecked((long)0xFFFFFFFF800007FEUL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF800007FEUL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF800007FEUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF800007FEUL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF800007FEUL)); }
    [MethodImpl(NI)] static long K537() => unchecked((long)0xFFFFFFFF800007FFUL);
    [MethodImpl(NI)] static long A537(long x) => x + unchecked((long)0xFFFFFFFF800007FFUL);
    [MethodImpl(NI)] static long C537(long x) { long r = (x < unchecked((long)0xFFFFFFFF800007FFUL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF800007FFUL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF800007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF800007FFUL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF800007FFUL)); }
    [MethodImpl(NI)] static long K538() => unchecked((long)0xFFFFFFFF80000800UL);
    [MethodImpl(NI)] static long A538(long x) => x + unchecked((long)0xFFFFFFFF80000800UL);
    [MethodImpl(NI)] static long C538(long x) { long r = (x < unchecked((long)0xFFFFFFFF80000800UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF80000800UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF80000800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF80000800UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF80000800UL)); }
    [MethodImpl(NI)] static long K539() => unchecked((long)0xFFFFFFFF80000801UL);
    [MethodImpl(NI)] static long A539(long x) => x + unchecked((long)0xFFFFFFFF80000801UL);
    [MethodImpl(NI)] static long C539(long x) { long r = (x < unchecked((long)0xFFFFFFFF80000801UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF80000801UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF80000801UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF80000801UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF80000801UL)); }
    [MethodImpl(NI)] static long K540() => unchecked((long)0xFFFFFFFF80000FFFUL);
    [MethodImpl(NI)] static long A540(long x) => x + unchecked((long)0xFFFFFFFF80000FFFUL);
    [MethodImpl(NI)] static long C540(long x) { long r = (x < unchecked((long)0xFFFFFFFF80000FFFUL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF80000FFFUL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF80000FFFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF80000FFFUL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF80000FFFUL)); }
    [MethodImpl(NI)] static long K541() => unchecked((long)0xFFFFFFFF80001000UL);
    [MethodImpl(NI)] static long A541(long x) => x + unchecked((long)0xFFFFFFFF80001000UL);
    [MethodImpl(NI)] static long C541(long x) { long r = (x < unchecked((long)0xFFFFFFFF80001000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF80001000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF80001000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF80001000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF80001000UL)); }
    [MethodImpl(NI)] static long K542() => unchecked((long)0xFFFFFFFF800FF800UL);
    [MethodImpl(NI)] static long A542(long x) => x + unchecked((long)0xFFFFFFFF800FF800UL);
    [MethodImpl(NI)] static long C542(long x) { long r = (x < unchecked((long)0xFFFFFFFF800FF800UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF800FF800UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF800FF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF800FF800UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF800FF800UL)); }
    [MethodImpl(NI)] static long K543() => unchecked((long)0xFFFFFFFF800FF801UL);
    [MethodImpl(NI)] static long A543(long x) => x + unchecked((long)0xFFFFFFFF800FF801UL);
    [MethodImpl(NI)] static long C543(long x) { long r = (x < unchecked((long)0xFFFFFFFF800FF801UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF800FF801UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF800FF801UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF800FF801UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF800FF801UL)); }
    [MethodImpl(NI)] static long K544() => unchecked((long)0xFFFFFFFF87654321UL);
    [MethodImpl(NI)] static long A544(long x) => x + unchecked((long)0xFFFFFFFF87654321UL);
    [MethodImpl(NI)] static long C544(long x) { long r = (x < unchecked((long)0xFFFFFFFF87654321UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF87654321UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF87654321UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF87654321UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF87654321UL)); }
    [MethodImpl(NI)] static long K545() => unchecked((long)0xFFFFFFFF8C5C0507UL);
    [MethodImpl(NI)] static long A545(long x) => x + unchecked((long)0xFFFFFFFF8C5C0507UL);
    [MethodImpl(NI)] static long C545(long x) { long r = (x < unchecked((long)0xFFFFFFFF8C5C0507UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF8C5C0507UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF8C5C0507UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF8C5C0507UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF8C5C0507UL)); }
    [MethodImpl(NI)] static long K546() => unchecked((long)0xFFFFFFFF8C5C07FFUL);
    [MethodImpl(NI)] static long A546(long x) => x + unchecked((long)0xFFFFFFFF8C5C07FFUL);
    [MethodImpl(NI)] static long C546(long x) { long r = (x < unchecked((long)0xFFFFFFFF8C5C07FFUL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF8C5C07FFUL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF8C5C07FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF8C5C07FFUL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF8C5C07FFUL)); }
    [MethodImpl(NI)] static long K547() => unchecked((long)0xFFFFFFFF94AA5FFBUL);
    [MethodImpl(NI)] static long A547(long x) => x + unchecked((long)0xFFFFFFFF94AA5FFBUL);
    [MethodImpl(NI)] static long C547(long x) { long r = (x < unchecked((long)0xFFFFFFFF94AA5FFBUL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF94AA5FFBUL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF94AA5FFBUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF94AA5FFBUL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF94AA5FFBUL)); }
    [MethodImpl(NI)] static long K548() => unchecked((long)0xFFFFFFFF94AA5FFFUL);
    [MethodImpl(NI)] static long A548(long x) => x + unchecked((long)0xFFFFFFFF94AA5FFFUL);
    [MethodImpl(NI)] static long C548(long x) { long r = (x < unchecked((long)0xFFFFFFFF94AA5FFFUL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF94AA5FFFUL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF94AA5FFFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF94AA5FFFUL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF94AA5FFFUL)); }
    [MethodImpl(NI)] static long K549() => unchecked((long)0xFFFFFFFF9E632CE3UL);
    [MethodImpl(NI)] static long A549(long x) => x + unchecked((long)0xFFFFFFFF9E632CE3UL);
    [MethodImpl(NI)] static long C549(long x) { long r = (x < unchecked((long)0xFFFFFFFF9E632CE3UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF9E632CE3UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF9E632CE3UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF9E632CE3UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF9E632CE3UL)); }
    [MethodImpl(NI)] static long K550() => unchecked((long)0xFFFFFFFF9E632FFFUL);
    [MethodImpl(NI)] static long A550(long x) => x + unchecked((long)0xFFFFFFFF9E632FFFUL);
    [MethodImpl(NI)] static long C550(long x) { long r = (x < unchecked((long)0xFFFFFFFF9E632FFFUL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFF9E632FFFUL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFF9E632FFFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFF9E632FFFUL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFF9E632FFFUL)); }
    [MethodImpl(NI)] static long K551() => unchecked((long)0xFFFFFFFFA838D000UL);
    [MethodImpl(NI)] static long A551(long x) => x + unchecked((long)0xFFFFFFFFA838D000UL);
    [MethodImpl(NI)] static long C551(long x) { long r = (x < unchecked((long)0xFFFFFFFFA838D000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFA838D000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFA838D000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFA838D000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFA838D000UL)); }
    [MethodImpl(NI)] static long K552() => unchecked((long)0xFFFFFFFFA838DC54UL);
    [MethodImpl(NI)] static long A552(long x) => x + unchecked((long)0xFFFFFFFFA838DC54UL);
    [MethodImpl(NI)] static long C552(long x) { long r = (x < unchecked((long)0xFFFFFFFFA838DC54UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFA838DC54UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFA838DC54UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFA838DC54UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFA838DC54UL)); }
    [MethodImpl(NI)] static long K553() => unchecked((long)0xFFFFFFFFBFFEFFF8UL);
    [MethodImpl(NI)] static long A553(long x) => x + unchecked((long)0xFFFFFFFFBFFEFFF8UL);
    [MethodImpl(NI)] static long C553(long x) { long r = (x < unchecked((long)0xFFFFFFFFBFFEFFF8UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFBFFEFFF8UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFBFFEFFF8UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFBFFEFFF8UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFBFFEFFF8UL)); }
    [MethodImpl(NI)] static long K554() => unchecked((long)0xFFFFFFFFBFFF0000UL);
    [MethodImpl(NI)] static long A554(long x) => x + unchecked((long)0xFFFFFFFFBFFF0000UL);
    [MethodImpl(NI)] static long C554(long x) { long r = (x < unchecked((long)0xFFFFFFFFBFFF0000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFBFFF0000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFBFFF0000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFBFFF0000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFBFFF0000UL)); }
    [MethodImpl(NI)] static long K555() => unchecked((long)0xFFFFFFFFC00007FFUL);
    [MethodImpl(NI)] static long A555(long x) => x + unchecked((long)0xFFFFFFFFC00007FFUL);
    [MethodImpl(NI)] static long C555(long x) { long r = (x < unchecked((long)0xFFFFFFFFC00007FFUL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFC00007FFUL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFC00007FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFC00007FFUL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFC00007FFUL)); }
    [MethodImpl(NI)] static long K556() => unchecked((long)0xFFFFFFFFC0000800UL);
    [MethodImpl(NI)] static long A556(long x) => x + unchecked((long)0xFFFFFFFFC0000800UL);
    [MethodImpl(NI)] static long C556(long x) { long r = (x < unchecked((long)0xFFFFFFFFC0000800UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFC0000800UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFC0000800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFC0000800UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFC0000800UL)); }
    [MethodImpl(NI)] static long K557() => unchecked((long)0xFFFFFFFFD0003FFFUL);
    [MethodImpl(NI)] static long A557(long x) => x + unchecked((long)0xFFFFFFFFD0003FFFUL);
    [MethodImpl(NI)] static long C557(long x) { long r = (x < unchecked((long)0xFFFFFFFFD0003FFFUL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFD0003FFFUL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFD0003FFFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFD0003FFFUL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFD0003FFFUL)); }
    [MethodImpl(NI)] static long K558() => unchecked((long)0xFFFFFFFFD0004000UL);
    [MethodImpl(NI)] static long A558(long x) => x + unchecked((long)0xFFFFFFFFD0004000UL);
    [MethodImpl(NI)] static long C558(long x) { long r = (x < unchecked((long)0xFFFFFFFFD0004000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFD0004000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFD0004000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFD0004000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFD0004000UL)); }
    [MethodImpl(NI)] static long K559() => unchecked((long)0xFFFFFFFFED5F23ADUL);
    [MethodImpl(NI)] static long A559(long x) => x + unchecked((long)0xFFFFFFFFED5F23ADUL);
    [MethodImpl(NI)] static long C559(long x) { long r = (x < unchecked((long)0xFFFFFFFFED5F23ADUL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFED5F23ADUL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFED5F23ADUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFED5F23ADUL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFED5F23ADUL)); }
    [MethodImpl(NI)] static long K560() => unchecked((long)0xFFFFFFFFED5F2FFFUL);
    [MethodImpl(NI)] static long A560(long x) => x + unchecked((long)0xFFFFFFFFED5F2FFFUL);
    [MethodImpl(NI)] static long C560(long x) { long r = (x < unchecked((long)0xFFFFFFFFED5F2FFFUL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFED5F2FFFUL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFED5F2FFFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFED5F2FFFUL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFED5F2FFFUL)); }
    [MethodImpl(NI)] static long K561() => unchecked((long)0xFFFFFFFFEDCB0000UL);
    [MethodImpl(NI)] static long A561(long x) => x + unchecked((long)0xFFFFFFFFEDCB0000UL);
    [MethodImpl(NI)] static long C561(long x) { long r = (x < unchecked((long)0xFFFFFFFFEDCB0000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFEDCB0000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFEDCB0000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFEDCB0000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFEDCB0000UL)); }
    [MethodImpl(NI)] static long K562() => unchecked((long)0xFFFFFFFFEDCBA987UL);
    [MethodImpl(NI)] static long A562(long x) => x + unchecked((long)0xFFFFFFFFEDCBA987UL);
    [MethodImpl(NI)] static long C562(long x) { long r = (x < unchecked((long)0xFFFFFFFFEDCBA987UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFEDCBA987UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFEDCBA987UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFEDCBA987UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFEDCBA987UL)); }
    [MethodImpl(NI)] static long K563() => unchecked((long)0xFFFFFFFFEDCBA988UL);
    [MethodImpl(NI)] static long A563(long x) => x + unchecked((long)0xFFFFFFFFEDCBA988UL);
    [MethodImpl(NI)] static long C563(long x) { long r = (x < unchecked((long)0xFFFFFFFFEDCBA988UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFEDCBA988UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFEDCBA988UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFEDCBA988UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFEDCBA988UL)); }
    [MethodImpl(NI)] static long K564() => unchecked((long)0xFFFFFFFFEDD2F7FFUL);
    [MethodImpl(NI)] static long A564(long x) => x + unchecked((long)0xFFFFFFFFEDD2F7FFUL);
    [MethodImpl(NI)] static long C564(long x) { long r = (x < unchecked((long)0xFFFFFFFFEDD2F7FFUL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFEDD2F7FFUL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFEDD2F7FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFEDD2F7FFUL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFEDD2F7FFUL)); }
    [MethodImpl(NI)] static long K565() => unchecked((long)0xFFFFFFFFEDD2FA80UL);
    [MethodImpl(NI)] static long A565(long x) => x + unchecked((long)0xFFFFFFFFEDD2FA80UL);
    [MethodImpl(NI)] static long C565(long x) { long r = (x < unchecked((long)0xFFFFFFFFEDD2FA80UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFEDD2FA80UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFEDD2FA80UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFEDD2FA80UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFEDD2FA80UL)); }
    [MethodImpl(NI)] static long K566() => unchecked((long)0xFFFFFFFFF0000000UL);
    [MethodImpl(NI)] static long A566(long x) => x + unchecked((long)0xFFFFFFFFF0000000UL);
    [MethodImpl(NI)] static long C566(long x) { long r = (x < unchecked((long)0xFFFFFFFFF0000000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFF0000000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFF0000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFF0000000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFF0000000UL)); }
    [MethodImpl(NI)] static long K567() => unchecked((long)0xFFFFFFFFF6E58000UL);
    [MethodImpl(NI)] static long A567(long x) => x + unchecked((long)0xFFFFFFFFF6E58000UL);
    [MethodImpl(NI)] static long C567(long x) { long r = (x < unchecked((long)0xFFFFFFFFF6E58000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFF6E58000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFF6E58000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFF6E58000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFF6E58000UL)); }
    [MethodImpl(NI)] static long K568() => unchecked((long)0xFFFFFFFFFB72C000UL);
    [MethodImpl(NI)] static long A568(long x) => x + unchecked((long)0xFFFFFFFFFB72C000UL);
    [MethodImpl(NI)] static long C568(long x) { long r = (x < unchecked((long)0xFFFFFFFFFB72C000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFFB72C000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFFB72C000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFFB72C000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFFB72C000UL)); }
    [MethodImpl(NI)] static long K569() => unchecked((long)0xFFFFFFFFFDB96000UL);
    [MethodImpl(NI)] static long A569(long x) => x + unchecked((long)0xFFFFFFFFFDB96000UL);
    [MethodImpl(NI)] static long C569(long x) { long r = (x < unchecked((long)0xFFFFFFFFFDB96000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFFDB96000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFFDB96000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFFDB96000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFFDB96000UL)); }
    [MethodImpl(NI)] static long K570() => unchecked((long)0xFFFFFFFFFEDCB000UL);
    [MethodImpl(NI)] static long A570(long x) => x + unchecked((long)0xFFFFFFFFFEDCB000UL);
    [MethodImpl(NI)] static long C570(long x) { long r = (x < unchecked((long)0xFFFFFFFFFEDCB000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFFEDCB000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFFEDCB000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFFEDCB000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFFEDCB000UL)); }
    [MethodImpl(NI)] static long K571() => unchecked((long)0xFFFFFFFFFF000000UL);
    [MethodImpl(NI)] static long A571(long x) => x + unchecked((long)0xFFFFFFFFFF000000UL);
    [MethodImpl(NI)] static long C571(long x) { long r = (x < unchecked((long)0xFFFFFFFFFF000000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFFF000000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFFF000000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFFF000000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFFF000000UL)); }
    [MethodImpl(NI)] static long K572() => unchecked((long)0xFFFFFFFFFFF00000UL);
    [MethodImpl(NI)] static long A572(long x) => x + unchecked((long)0xFFFFFFFFFFF00000UL);
    [MethodImpl(NI)] static long C572(long x) { long r = (x < unchecked((long)0xFFFFFFFFFFF00000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFFFF00000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFFFF00000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFFFF00000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFFFF00000UL)); }
    [MethodImpl(NI)] static long K573() => unchecked((long)0xFFFFFFFFFFF43800UL);
    [MethodImpl(NI)] static long A573(long x) => x + unchecked((long)0xFFFFFFFFFFF43800UL);
    [MethodImpl(NI)] static long C573(long x) { long r = (x < unchecked((long)0xFFFFFFFFFFF43800UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFFFF43800UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFFFF43800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFFFF43800UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFFFF43800UL)); }
    [MethodImpl(NI)] static long K574() => unchecked((long)0xFFFFFFFFFFF439C7UL);
    [MethodImpl(NI)] static long A574(long x) => x + unchecked((long)0xFFFFFFFFFFF439C7UL);
    [MethodImpl(NI)] static long C574(long x) { long r = (x < unchecked((long)0xFFFFFFFFFFF439C7UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFFFF439C7UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFFFF439C7UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFFFF439C7UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFFFF439C7UL)); }
    [MethodImpl(NI)] static long K575() => unchecked((long)0xFFFFFFFFFFF626EEUL);
    [MethodImpl(NI)] static long A575(long x) => x + unchecked((long)0xFFFFFFFFFFF626EEUL);
    [MethodImpl(NI)] static long C575(long x) { long r = (x < unchecked((long)0xFFFFFFFFFFF626EEUL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFFFF626EEUL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFFFF626EEUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFFFF626EEUL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFFFF626EEUL)); }
    [MethodImpl(NI)] static long K576() => unchecked((long)0xFFFFFFFFFFF62FFFUL);
    [MethodImpl(NI)] static long A576(long x) => x + unchecked((long)0xFFFFFFFFFFF62FFFUL);
    [MethodImpl(NI)] static long C576(long x) { long r = (x < unchecked((long)0xFFFFFFFFFFF62FFFUL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFFFF62FFFUL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFFFF62FFFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFFFF62FFFUL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFFFF62FFFUL)); }
    [MethodImpl(NI)] static long K577() => unchecked((long)0xFFFFFFFFFFF81000UL);
    [MethodImpl(NI)] static long A577(long x) => x + unchecked((long)0xFFFFFFFFFFF81000UL);
    [MethodImpl(NI)] static long C577(long x) { long r = (x < unchecked((long)0xFFFFFFFFFFF81000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFFFF81000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFFFF81000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFFFF81000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFFFF81000UL)); }
    [MethodImpl(NI)] static long K578() => unchecked((long)0xFFFFFFFFFFF81812UL);
    [MethodImpl(NI)] static long A578(long x) => x + unchecked((long)0xFFFFFFFFFFF81812UL);
    [MethodImpl(NI)] static long C578(long x) { long r = (x < unchecked((long)0xFFFFFFFFFFF81812UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFFFF81812UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFFFF81812UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFFFF81812UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFFFF81812UL)); }
    [MethodImpl(NI)] static long K579() => unchecked((long)0xFFFFFFFFFFFF0000UL);
    [MethodImpl(NI)] static long A579(long x) => x + unchecked((long)0xFFFFFFFFFFFF0000UL);
    [MethodImpl(NI)] static long C579(long x) { long r = (x < unchecked((long)0xFFFFFFFFFFFF0000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFFFFF0000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFFFFF0000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFFFFF0000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFFFFF0000UL)); }
    [MethodImpl(NI)] static long K580() => unchecked((long)0xFFFFFFFFFFFF8000UL);
    [MethodImpl(NI)] static long A580(long x) => x + unchecked((long)0xFFFFFFFFFFFF8000UL);
    [MethodImpl(NI)] static long C580(long x) { long r = (x < unchecked((long)0xFFFFFFFFFFFF8000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFFFFF8000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFFFFF8000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFFFFF8000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFFFFF8000UL)); }
    [MethodImpl(NI)] static long K581() => unchecked((long)0xFFFFFFFFFFFFC000UL);
    [MethodImpl(NI)] static long A581(long x) => x + unchecked((long)0xFFFFFFFFFFFFC000UL);
    [MethodImpl(NI)] static long C581(long x) { long r = (x < unchecked((long)0xFFFFFFFFFFFFC000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFFFFFC000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFFFFFC000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFFFFFC000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFFFFFC000UL)); }
    [MethodImpl(NI)] static long K582() => unchecked((long)0xFFFFFFFFFFFFE000UL);
    [MethodImpl(NI)] static long A582(long x) => x + unchecked((long)0xFFFFFFFFFFFFE000UL);
    [MethodImpl(NI)] static long C582(long x) { long r = (x < unchecked((long)0xFFFFFFFFFFFFE000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFFFFFE000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFFFFFE000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFFFFFE000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFFFFFE000UL)); }
    [MethodImpl(NI)] static long K583() => unchecked((long)0xFFFFFFFFFFFFE7FFUL);
    [MethodImpl(NI)] static long A583(long x) => x + unchecked((long)0xFFFFFFFFFFFFE7FFUL);
    [MethodImpl(NI)] static long C583(long x) { long r = (x < unchecked((long)0xFFFFFFFFFFFFE7FFUL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFFFFFE7FFUL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFFFFFE7FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFFFFFE7FFUL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFFFFFE7FFUL)); }
    [MethodImpl(NI)] static long K584() => unchecked((long)0xFFFFFFFFFFFFE800UL);
    [MethodImpl(NI)] static long A584(long x) => x + unchecked((long)0xFFFFFFFFFFFFE800UL);
    [MethodImpl(NI)] static long C584(long x) { long r = (x < unchecked((long)0xFFFFFFFFFFFFE800UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFFFFFE800UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFFFFFE800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFFFFFE800UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFFFFFE800UL)); }
    [MethodImpl(NI)] static long K585() => unchecked((long)0xFFFFFFFFFFFFE801UL);
    [MethodImpl(NI)] static long A585(long x) => x + unchecked((long)0xFFFFFFFFFFFFE801UL);
    [MethodImpl(NI)] static long C585(long x) { long r = (x < unchecked((long)0xFFFFFFFFFFFFE801UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFFFFFE801UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFFFFFE801UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFFFFFE801UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFFFFFE801UL)); }
    [MethodImpl(NI)] static long K586() => unchecked((long)0xFFFFFFFFFFFFEFFFUL);
    [MethodImpl(NI)] static long A586(long x) => x + unchecked((long)0xFFFFFFFFFFFFEFFFUL);
    [MethodImpl(NI)] static long C586(long x) { long r = (x < unchecked((long)0xFFFFFFFFFFFFEFFFUL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFFFFFEFFFUL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFFFFFEFFFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFFFFFEFFFUL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFFFFFEFFFUL)); }
    [MethodImpl(NI)] static long K587() => unchecked((long)0xFFFFFFFFFFFFF000UL);
    [MethodImpl(NI)] static long A587(long x) => x + unchecked((long)0xFFFFFFFFFFFFF000UL);
    [MethodImpl(NI)] static long C587(long x) { long r = (x < unchecked((long)0xFFFFFFFFFFFFF000UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFFFFFF000UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFFFFFF000UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFFFFFF000UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFFFFFF000UL)); }
    [MethodImpl(NI)] static long K588() => unchecked((long)0xFFFFFFFFFFFFF001UL);
    [MethodImpl(NI)] static long A588(long x) => x + unchecked((long)0xFFFFFFFFFFFFF001UL);
    [MethodImpl(NI)] static long C588(long x) { long r = (x < unchecked((long)0xFFFFFFFFFFFFF001UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFFFFFF001UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFFFFFF001UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFFFFFF001UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFFFFFF001UL)); }
    [MethodImpl(NI)] static long K589() => unchecked((long)0xFFFFFFFFFFFFF1C8UL);
    [MethodImpl(NI)] static long A589(long x) => x + unchecked((long)0xFFFFFFFFFFFFF1C8UL);
    [MethodImpl(NI)] static long C589(long x) { long r = (x < unchecked((long)0xFFFFFFFFFFFFF1C8UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFFFFFF1C8UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFFFFFF1C8UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFFFFFF1C8UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFFFFFF1C8UL)); }
    [MethodImpl(NI)] static long K590() => unchecked((long)0xFFFFFFFFFFFFF7FEUL);
    [MethodImpl(NI)] static long A590(long x) => x + unchecked((long)0xFFFFFFFFFFFFF7FEUL);
    [MethodImpl(NI)] static long C590(long x) { long r = (x < unchecked((long)0xFFFFFFFFFFFFF7FEUL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFFFFFF7FEUL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFFFFFF7FEUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFFFFFF7FEUL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFFFFFF7FEUL)); }
    [MethodImpl(NI)] static long K591() => unchecked((long)0xFFFFFFFFFFFFF7FFUL);
    [MethodImpl(NI)] static long A591(long x) => x + unchecked((long)0xFFFFFFFFFFFFF7FFUL);
    [MethodImpl(NI)] static long C591(long x) { long r = (x < unchecked((long)0xFFFFFFFFFFFFF7FFUL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFFFFFF7FFUL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFFFFFF7FFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFFFFFF7FFUL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFFFFFF7FFUL)); }
    [MethodImpl(NI)] static long K592() => unchecked((long)0xFFFFFFFFFFFFF800UL);
    [MethodImpl(NI)] static long A592(long x) => x + unchecked((long)0xFFFFFFFFFFFFF800UL);
    [MethodImpl(NI)] static long C592(long x) { long r = (x < unchecked((long)0xFFFFFFFFFFFFF800UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFFFFFF800UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFFFFFF800UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFFFFFF800UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFFFFFF800UL)); }
    [MethodImpl(NI)] static long K593() => unchecked((long)0xFFFFFFFFFFFFF801UL);
    [MethodImpl(NI)] static long A593(long x) => x + unchecked((long)0xFFFFFFFFFFFFF801UL);
    [MethodImpl(NI)] static long C593(long x) { long r = (x < unchecked((long)0xFFFFFFFFFFFFF801UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFFFFFF801UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFFFFFF801UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFFFFFF801UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFFFFFF801UL)); }
    [MethodImpl(NI)] static long K594() => unchecked((long)0xFFFFFFFFFFFFF802UL);
    [MethodImpl(NI)] static long A594(long x) => x + unchecked((long)0xFFFFFFFFFFFFF802UL);
    [MethodImpl(NI)] static long C594(long x) { long r = (x < unchecked((long)0xFFFFFFFFFFFFF802UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFFFFFF802UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFFFFFF802UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFFFFFF802UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFFFFFF802UL)); }
    [MethodImpl(NI)] static long K595() => unchecked((long)0xFFFFFFFFFFFFFCABUL);
    [MethodImpl(NI)] static long A595(long x) => x + unchecked((long)0xFFFFFFFFFFFFFCABUL);
    [MethodImpl(NI)] static long C595(long x) { long r = (x < unchecked((long)0xFFFFFFFFFFFFFCABUL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFFFFFFCABUL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFFFFFFCABUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFFFFFFCABUL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFFFFFFCABUL)); }
    [MethodImpl(NI)] static long K596() => unchecked((long)0xFFFFFFFFFFFFFF71UL);
    [MethodImpl(NI)] static long A596(long x) => x + unchecked((long)0xFFFFFFFFFFFFFF71UL);
    [MethodImpl(NI)] static long C596(long x) { long r = (x < unchecked((long)0xFFFFFFFFFFFFFF71UL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFFFFFFF71UL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFFFFFFF71UL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFFFFFFF71UL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFFFFFFF71UL)); }
    [MethodImpl(NI)] static long K597() => unchecked((long)0xFFFFFFFFFFFFFFFEUL);
    [MethodImpl(NI)] static long A597(long x) => x + unchecked((long)0xFFFFFFFFFFFFFFFEUL);
    [MethodImpl(NI)] static long C597(long x) { long r = (x < unchecked((long)0xFFFFFFFFFFFFFFFEUL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFFFFFFFFEUL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFFFFFFFFEUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFFFFFFFFEUL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFFFFFFFFEUL)); }
    [MethodImpl(NI)] static long K598() => unchecked((long)0xFFFFFFFFFFFFFFFFUL);
    [MethodImpl(NI)] static long A598(long x) => x + unchecked((long)0xFFFFFFFFFFFFFFFFUL);
    [MethodImpl(NI)] static long C598(long x) { long r = (x < unchecked((long)0xFFFFFFFFFFFFFFFFUL) ? 1 : 0) + ((ulong)x < 0xFFFFFFFFFFFFFFFFUL ? 2 : 0) + (x == unchecked((long)0xFFFFFFFFFFFFFFFFUL) ? 4 : 0); r = r * 31 + (x & unchecked((long)0xFFFFFFFFFFFFFFFFUL)); return r * 31 + (x ^ unchecked((long)0xFFFFFFFFFFFFFFFFUL)); }
    [MethodImpl(NI)] static int IK0() => unchecked((int)0x00000000);
    [MethodImpl(NI)] static long IM0(int y) { long r = (long)(y + unchecked((int)0x00000000)); r = r * 31 + (long)(y * unchecked((int)0x00000000)); r = r * 31 + (long)(y & unchecked((int)0x00000000)); return r * 31 + (y < unchecked((int)0x00000000) ? 1 : 0) + ((uint)y < 0x00000000u ? 2 : 0) + (y == unchecked((int)0x00000000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS0(long[] l, int[] m) { l[1] = unchecked((int)0x00000000); m[1] = unchecked((int)0x00000000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK1() => unchecked((int)0x00000001);
    [MethodImpl(NI)] static long IM1(int y) { long r = (long)(y + unchecked((int)0x00000001)); r = r * 31 + (long)(y * unchecked((int)0x00000001)); r = r * 31 + (long)(y & unchecked((int)0x00000001)); return r * 31 + (y < unchecked((int)0x00000001) ? 1 : 0) + ((uint)y < 0x00000001u ? 2 : 0) + (y == unchecked((int)0x00000001) ? 4 : 0); }
    [MethodImpl(NI)] static long IS1(long[] l, int[] m) { l[1] = unchecked((int)0x00000001); m[1] = unchecked((int)0x00000001); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK2() => unchecked((int)0x00000002);
    [MethodImpl(NI)] static long IM2(int y) { long r = (long)(y + unchecked((int)0x00000002)); r = r * 31 + (long)(y * unchecked((int)0x00000002)); r = r * 31 + (long)(y & unchecked((int)0x00000002)); return r * 31 + (y < unchecked((int)0x00000002) ? 1 : 0) + ((uint)y < 0x00000002u ? 2 : 0) + (y == unchecked((int)0x00000002) ? 4 : 0); }
    [MethodImpl(NI)] static long IS2(long[] l, int[] m) { l[1] = unchecked((int)0x00000002); m[1] = unchecked((int)0x00000002); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK3() => unchecked((int)0x00000152);
    [MethodImpl(NI)] static long IM3(int y) { long r = (long)(y + unchecked((int)0x00000152)); r = r * 31 + (long)(y * unchecked((int)0x00000152)); r = r * 31 + (long)(y & unchecked((int)0x00000152)); return r * 31 + (y < unchecked((int)0x00000152) ? 1 : 0) + ((uint)y < 0x00000152u ? 2 : 0) + (y == unchecked((int)0x00000152) ? 4 : 0); }
    [MethodImpl(NI)] static long IS3(long[] l, int[] m) { l[1] = unchecked((int)0x00000152); m[1] = unchecked((int)0x00000152); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK4() => unchecked((int)0x000001AE);
    [MethodImpl(NI)] static long IM4(int y) { long r = (long)(y + unchecked((int)0x000001AE)); r = r * 31 + (long)(y * unchecked((int)0x000001AE)); r = r * 31 + (long)(y & unchecked((int)0x000001AE)); return r * 31 + (y < unchecked((int)0x000001AE) ? 1 : 0) + ((uint)y < 0x000001AEu ? 2 : 0) + (y == unchecked((int)0x000001AE) ? 4 : 0); }
    [MethodImpl(NI)] static long IS4(long[] l, int[] m) { l[1] = unchecked((int)0x000001AE); m[1] = unchecked((int)0x000001AE); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK5() => unchecked((int)0x0000025D);
    [MethodImpl(NI)] static long IM5(int y) { long r = (long)(y + unchecked((int)0x0000025D)); r = r * 31 + (long)(y * unchecked((int)0x0000025D)); r = r * 31 + (long)(y & unchecked((int)0x0000025D)); return r * 31 + (y < unchecked((int)0x0000025D) ? 1 : 0) + ((uint)y < 0x0000025Du ? 2 : 0) + (y == unchecked((int)0x0000025D) ? 4 : 0); }
    [MethodImpl(NI)] static long IS5(long[] l, int[] m) { l[1] = unchecked((int)0x0000025D); m[1] = unchecked((int)0x0000025D); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK6() => unchecked((int)0x000007FE);
    [MethodImpl(NI)] static long IM6(int y) { long r = (long)(y + unchecked((int)0x000007FE)); r = r * 31 + (long)(y * unchecked((int)0x000007FE)); r = r * 31 + (long)(y & unchecked((int)0x000007FE)); return r * 31 + (y < unchecked((int)0x000007FE) ? 1 : 0) + ((uint)y < 0x000007FEu ? 2 : 0) + (y == unchecked((int)0x000007FE) ? 4 : 0); }
    [MethodImpl(NI)] static long IS6(long[] l, int[] m) { l[1] = unchecked((int)0x000007FE); m[1] = unchecked((int)0x000007FE); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK7() => unchecked((int)0x000007FF);
    [MethodImpl(NI)] static long IM7(int y) { long r = (long)(y + unchecked((int)0x000007FF)); r = r * 31 + (long)(y * unchecked((int)0x000007FF)); r = r * 31 + (long)(y & unchecked((int)0x000007FF)); return r * 31 + (y < unchecked((int)0x000007FF) ? 1 : 0) + ((uint)y < 0x000007FFu ? 2 : 0) + (y == unchecked((int)0x000007FF) ? 4 : 0); }
    [MethodImpl(NI)] static long IS7(long[] l, int[] m) { l[1] = unchecked((int)0x000007FF); m[1] = unchecked((int)0x000007FF); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK8() => unchecked((int)0x00000800);
    [MethodImpl(NI)] static long IM8(int y) { long r = (long)(y + unchecked((int)0x00000800)); r = r * 31 + (long)(y * unchecked((int)0x00000800)); r = r * 31 + (long)(y & unchecked((int)0x00000800)); return r * 31 + (y < unchecked((int)0x00000800) ? 1 : 0) + ((uint)y < 0x00000800u ? 2 : 0) + (y == unchecked((int)0x00000800) ? 4 : 0); }
    [MethodImpl(NI)] static long IS8(long[] l, int[] m) { l[1] = unchecked((int)0x00000800); m[1] = unchecked((int)0x00000800); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK9() => unchecked((int)0x00000801);
    [MethodImpl(NI)] static long IM9(int y) { long r = (long)(y + unchecked((int)0x00000801)); r = r * 31 + (long)(y * unchecked((int)0x00000801)); r = r * 31 + (long)(y & unchecked((int)0x00000801)); return r * 31 + (y < unchecked((int)0x00000801) ? 1 : 0) + ((uint)y < 0x00000801u ? 2 : 0) + (y == unchecked((int)0x00000801) ? 4 : 0); }
    [MethodImpl(NI)] static long IS9(long[] l, int[] m) { l[1] = unchecked((int)0x00000801); m[1] = unchecked((int)0x00000801); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK10() => unchecked((int)0x00000802);
    [MethodImpl(NI)] static long IM10(int y) { long r = (long)(y + unchecked((int)0x00000802)); r = r * 31 + (long)(y * unchecked((int)0x00000802)); r = r * 31 + (long)(y & unchecked((int)0x00000802)); return r * 31 + (y < unchecked((int)0x00000802) ? 1 : 0) + ((uint)y < 0x00000802u ? 2 : 0) + (y == unchecked((int)0x00000802) ? 4 : 0); }
    [MethodImpl(NI)] static long IS10(long[] l, int[] m) { l[1] = unchecked((int)0x00000802); m[1] = unchecked((int)0x00000802); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK11() => unchecked((int)0x00000CFC);
    [MethodImpl(NI)] static long IM11(int y) { long r = (long)(y + unchecked((int)0x00000CFC)); r = r * 31 + (long)(y * unchecked((int)0x00000CFC)); r = r * 31 + (long)(y & unchecked((int)0x00000CFC)); return r * 31 + (y < unchecked((int)0x00000CFC) ? 1 : 0) + ((uint)y < 0x00000CFCu ? 2 : 0) + (y == unchecked((int)0x00000CFC) ? 4 : 0); }
    [MethodImpl(NI)] static long IS11(long[] l, int[] m) { l[1] = unchecked((int)0x00000CFC); m[1] = unchecked((int)0x00000CFC); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK12() => unchecked((int)0x00000FFE);
    [MethodImpl(NI)] static long IM12(int y) { long r = (long)(y + unchecked((int)0x00000FFE)); r = r * 31 + (long)(y * unchecked((int)0x00000FFE)); r = r * 31 + (long)(y & unchecked((int)0x00000FFE)); return r * 31 + (y < unchecked((int)0x00000FFE) ? 1 : 0) + ((uint)y < 0x00000FFEu ? 2 : 0) + (y == unchecked((int)0x00000FFE) ? 4 : 0); }
    [MethodImpl(NI)] static long IS12(long[] l, int[] m) { l[1] = unchecked((int)0x00000FFE); m[1] = unchecked((int)0x00000FFE); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK13() => unchecked((int)0x00000FFF);
    [MethodImpl(NI)] static long IM13(int y) { long r = (long)(y + unchecked((int)0x00000FFF)); r = r * 31 + (long)(y * unchecked((int)0x00000FFF)); r = r * 31 + (long)(y & unchecked((int)0x00000FFF)); return r * 31 + (y < unchecked((int)0x00000FFF) ? 1 : 0) + ((uint)y < 0x00000FFFu ? 2 : 0) + (y == unchecked((int)0x00000FFF) ? 4 : 0); }
    [MethodImpl(NI)] static long IS13(long[] l, int[] m) { l[1] = unchecked((int)0x00000FFF); m[1] = unchecked((int)0x00000FFF); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK14() => unchecked((int)0x00001000);
    [MethodImpl(NI)] static long IM14(int y) { long r = (long)(y + unchecked((int)0x00001000)); r = r * 31 + (long)(y * unchecked((int)0x00001000)); r = r * 31 + (long)(y & unchecked((int)0x00001000)); return r * 31 + (y < unchecked((int)0x00001000) ? 1 : 0) + ((uint)y < 0x00001000u ? 2 : 0) + (y == unchecked((int)0x00001000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS14(long[] l, int[] m) { l[1] = unchecked((int)0x00001000); m[1] = unchecked((int)0x00001000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK15() => unchecked((int)0x00001002);
    [MethodImpl(NI)] static long IM15(int y) { long r = (long)(y + unchecked((int)0x00001002)); r = r * 31 + (long)(y * unchecked((int)0x00001002)); r = r * 31 + (long)(y & unchecked((int)0x00001002)); return r * 31 + (y < unchecked((int)0x00001002) ? 1 : 0) + ((uint)y < 0x00001002u ? 2 : 0) + (y == unchecked((int)0x00001002) ? 4 : 0); }
    [MethodImpl(NI)] static long IS15(long[] l, int[] m) { l[1] = unchecked((int)0x00001002); m[1] = unchecked((int)0x00001002); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK16() => unchecked((int)0x000017FD);
    [MethodImpl(NI)] static long IM16(int y) { long r = (long)(y + unchecked((int)0x000017FD)); r = r * 31 + (long)(y * unchecked((int)0x000017FD)); r = r * 31 + (long)(y & unchecked((int)0x000017FD)); return r * 31 + (y < unchecked((int)0x000017FD) ? 1 : 0) + ((uint)y < 0x000017FDu ? 2 : 0) + (y == unchecked((int)0x000017FD) ? 4 : 0); }
    [MethodImpl(NI)] static long IS16(long[] l, int[] m) { l[1] = unchecked((int)0x000017FD); m[1] = unchecked((int)0x000017FD); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK17() => unchecked((int)0x000017FE);
    [MethodImpl(NI)] static long IM17(int y) { long r = (long)(y + unchecked((int)0x000017FE)); r = r * 31 + (long)(y * unchecked((int)0x000017FE)); r = r * 31 + (long)(y & unchecked((int)0x000017FE)); return r * 31 + (y < unchecked((int)0x000017FE) ? 1 : 0) + ((uint)y < 0x000017FEu ? 2 : 0) + (y == unchecked((int)0x000017FE) ? 4 : 0); }
    [MethodImpl(NI)] static long IS17(long[] l, int[] m) { l[1] = unchecked((int)0x000017FE); m[1] = unchecked((int)0x000017FE); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK18() => unchecked((int)0x000017FF);
    [MethodImpl(NI)] static long IM18(int y) { long r = (long)(y + unchecked((int)0x000017FF)); r = r * 31 + (long)(y * unchecked((int)0x000017FF)); r = r * 31 + (long)(y & unchecked((int)0x000017FF)); return r * 31 + (y < unchecked((int)0x000017FF) ? 1 : 0) + ((uint)y < 0x000017FFu ? 2 : 0) + (y == unchecked((int)0x000017FF) ? 4 : 0); }
    [MethodImpl(NI)] static long IS18(long[] l, int[] m) { l[1] = unchecked((int)0x000017FF); m[1] = unchecked((int)0x000017FF); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK19() => unchecked((int)0x00001800);
    [MethodImpl(NI)] static long IM19(int y) { long r = (long)(y + unchecked((int)0x00001800)); r = r * 31 + (long)(y * unchecked((int)0x00001800)); r = r * 31 + (long)(y & unchecked((int)0x00001800)); return r * 31 + (y < unchecked((int)0x00001800) ? 1 : 0) + ((uint)y < 0x00001800u ? 2 : 0) + (y == unchecked((int)0x00001800) ? 4 : 0); }
    [MethodImpl(NI)] static long IS19(long[] l, int[] m) { l[1] = unchecked((int)0x00001800); m[1] = unchecked((int)0x00001800); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK20() => unchecked((int)0x00001801);
    [MethodImpl(NI)] static long IM20(int y) { long r = (long)(y + unchecked((int)0x00001801)); r = r * 31 + (long)(y * unchecked((int)0x00001801)); r = r * 31 + (long)(y & unchecked((int)0x00001801)); return r * 31 + (y < unchecked((int)0x00001801) ? 1 : 0) + ((uint)y < 0x00001801u ? 2 : 0) + (y == unchecked((int)0x00001801) ? 4 : 0); }
    [MethodImpl(NI)] static long IS20(long[] l, int[] m) { l[1] = unchecked((int)0x00001801); m[1] = unchecked((int)0x00001801); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK21() => unchecked((int)0x00001FFE);
    [MethodImpl(NI)] static long IM21(int y) { long r = (long)(y + unchecked((int)0x00001FFE)); r = r * 31 + (long)(y * unchecked((int)0x00001FFE)); r = r * 31 + (long)(y & unchecked((int)0x00001FFE)); return r * 31 + (y < unchecked((int)0x00001FFE) ? 1 : 0) + ((uint)y < 0x00001FFEu ? 2 : 0) + (y == unchecked((int)0x00001FFE) ? 4 : 0); }
    [MethodImpl(NI)] static long IS21(long[] l, int[] m) { l[1] = unchecked((int)0x00001FFE); m[1] = unchecked((int)0x00001FFE); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK22() => unchecked((int)0x00002000);
    [MethodImpl(NI)] static long IM22(int y) { long r = (long)(y + unchecked((int)0x00002000)); r = r * 31 + (long)(y * unchecked((int)0x00002000)); r = r * 31 + (long)(y & unchecked((int)0x00002000)); return r * 31 + (y < unchecked((int)0x00002000) ? 1 : 0) + ((uint)y < 0x00002000u ? 2 : 0) + (y == unchecked((int)0x00002000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS22(long[] l, int[] m) { l[1] = unchecked((int)0x00002000); m[1] = unchecked((int)0x00002000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK23() => unchecked((int)0x000027FD);
    [MethodImpl(NI)] static long IM23(int y) { long r = (long)(y + unchecked((int)0x000027FD)); r = r * 31 + (long)(y * unchecked((int)0x000027FD)); r = r * 31 + (long)(y & unchecked((int)0x000027FD)); return r * 31 + (y < unchecked((int)0x000027FD) ? 1 : 0) + ((uint)y < 0x000027FDu ? 2 : 0) + (y == unchecked((int)0x000027FD) ? 4 : 0); }
    [MethodImpl(NI)] static long IS23(long[] l, int[] m) { l[1] = unchecked((int)0x000027FD); m[1] = unchecked((int)0x000027FD); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK24() => unchecked((int)0x000027FE);
    [MethodImpl(NI)] static long IM24(int y) { long r = (long)(y + unchecked((int)0x000027FE)); r = r * 31 + (long)(y * unchecked((int)0x000027FE)); r = r * 31 + (long)(y & unchecked((int)0x000027FE)); return r * 31 + (y < unchecked((int)0x000027FE) ? 1 : 0) + ((uint)y < 0x000027FEu ? 2 : 0) + (y == unchecked((int)0x000027FE) ? 4 : 0); }
    [MethodImpl(NI)] static long IS24(long[] l, int[] m) { l[1] = unchecked((int)0x000027FE); m[1] = unchecked((int)0x000027FE); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK25() => unchecked((int)0x000027FF);
    [MethodImpl(NI)] static long IM25(int y) { long r = (long)(y + unchecked((int)0x000027FF)); r = r * 31 + (long)(y * unchecked((int)0x000027FF)); r = r * 31 + (long)(y & unchecked((int)0x000027FF)); return r * 31 + (y < unchecked((int)0x000027FF) ? 1 : 0) + ((uint)y < 0x000027FFu ? 2 : 0) + (y == unchecked((int)0x000027FF) ? 4 : 0); }
    [MethodImpl(NI)] static long IS25(long[] l, int[] m) { l[1] = unchecked((int)0x000027FF); m[1] = unchecked((int)0x000027FF); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK26() => unchecked((int)0x00002800);
    [MethodImpl(NI)] static long IM26(int y) { long r = (long)(y + unchecked((int)0x00002800)); r = r * 31 + (long)(y * unchecked((int)0x00002800)); r = r * 31 + (long)(y & unchecked((int)0x00002800)); return r * 31 + (y < unchecked((int)0x00002800) ? 1 : 0) + ((uint)y < 0x00002800u ? 2 : 0) + (y == unchecked((int)0x00002800) ? 4 : 0); }
    [MethodImpl(NI)] static long IS26(long[] l, int[] m) { l[1] = unchecked((int)0x00002800); m[1] = unchecked((int)0x00002800); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK27() => unchecked((int)0x00002FFE);
    [MethodImpl(NI)] static long IM27(int y) { long r = (long)(y + unchecked((int)0x00002FFE)); r = r * 31 + (long)(y * unchecked((int)0x00002FFE)); r = r * 31 + (long)(y & unchecked((int)0x00002FFE)); return r * 31 + (y < unchecked((int)0x00002FFE) ? 1 : 0) + ((uint)y < 0x00002FFEu ? 2 : 0) + (y == unchecked((int)0x00002FFE) ? 4 : 0); }
    [MethodImpl(NI)] static long IS27(long[] l, int[] m) { l[1] = unchecked((int)0x00002FFE); m[1] = unchecked((int)0x00002FFE); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK28() => unchecked((int)0x00003000);
    [MethodImpl(NI)] static long IM28(int y) { long r = (long)(y + unchecked((int)0x00003000)); r = r * 31 + (long)(y * unchecked((int)0x00003000)); r = r * 31 + (long)(y & unchecked((int)0x00003000)); return r * 31 + (y < unchecked((int)0x00003000) ? 1 : 0) + ((uint)y < 0x00003000u ? 2 : 0) + (y == unchecked((int)0x00003000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS28(long[] l, int[] m) { l[1] = unchecked((int)0x00003000); m[1] = unchecked((int)0x00003000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK29() => unchecked((int)0x000037FD);
    [MethodImpl(NI)] static long IM29(int y) { long r = (long)(y + unchecked((int)0x000037FD)); r = r * 31 + (long)(y * unchecked((int)0x000037FD)); r = r * 31 + (long)(y & unchecked((int)0x000037FD)); return r * 31 + (y < unchecked((int)0x000037FD) ? 1 : 0) + ((uint)y < 0x000037FDu ? 2 : 0) + (y == unchecked((int)0x000037FD) ? 4 : 0); }
    [MethodImpl(NI)] static long IS29(long[] l, int[] m) { l[1] = unchecked((int)0x000037FD); m[1] = unchecked((int)0x000037FD); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK30() => unchecked((int)0x000037FF);
    [MethodImpl(NI)] static long IM30(int y) { long r = (long)(y + unchecked((int)0x000037FF)); r = r * 31 + (long)(y * unchecked((int)0x000037FF)); r = r * 31 + (long)(y & unchecked((int)0x000037FF)); return r * 31 + (y < unchecked((int)0x000037FF) ? 1 : 0) + ((uint)y < 0x000037FFu ? 2 : 0) + (y == unchecked((int)0x000037FF) ? 4 : 0); }
    [MethodImpl(NI)] static long IS30(long[] l, int[] m) { l[1] = unchecked((int)0x000037FF); m[1] = unchecked((int)0x000037FF); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK31() => unchecked((int)0x00004000);
    [MethodImpl(NI)] static long IM31(int y) { long r = (long)(y + unchecked((int)0x00004000)); r = r * 31 + (long)(y * unchecked((int)0x00004000)); r = r * 31 + (long)(y & unchecked((int)0x00004000)); return r * 31 + (y < unchecked((int)0x00004000) ? 1 : 0) + ((uint)y < 0x00004000u ? 2 : 0) + (y == unchecked((int)0x00004000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS31(long[] l, int[] m) { l[1] = unchecked((int)0x00004000); m[1] = unchecked((int)0x00004000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK32() => unchecked((int)0x00008000);
    [MethodImpl(NI)] static long IM32(int y) { long r = (long)(y + unchecked((int)0x00008000)); r = r * 31 + (long)(y * unchecked((int)0x00008000)); r = r * 31 + (long)(y & unchecked((int)0x00008000)); return r * 31 + (y < unchecked((int)0x00008000) ? 1 : 0) + ((uint)y < 0x00008000u ? 2 : 0) + (y == unchecked((int)0x00008000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS32(long[] l, int[] m) { l[1] = unchecked((int)0x00008000); m[1] = unchecked((int)0x00008000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK33() => unchecked((int)0x00010000);
    [MethodImpl(NI)] static long IM33(int y) { long r = (long)(y + unchecked((int)0x00010000)); r = r * 31 + (long)(y * unchecked((int)0x00010000)); r = r * 31 + (long)(y & unchecked((int)0x00010000)); return r * 31 + (y < unchecked((int)0x00010000) ? 1 : 0) + ((uint)y < 0x00010000u ? 2 : 0) + (y == unchecked((int)0x00010000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS33(long[] l, int[] m) { l[1] = unchecked((int)0x00010000); m[1] = unchecked((int)0x00010000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK34() => unchecked((int)0x000CD7FF);
    [MethodImpl(NI)] static long IM34(int y) { long r = (long)(y + unchecked((int)0x000CD7FF)); r = r * 31 + (long)(y * unchecked((int)0x000CD7FF)); r = r * 31 + (long)(y & unchecked((int)0x000CD7FF)); return r * 31 + (y < unchecked((int)0x000CD7FF) ? 1 : 0) + ((uint)y < 0x000CD7FFu ? 2 : 0) + (y == unchecked((int)0x000CD7FF) ? 4 : 0); }
    [MethodImpl(NI)] static long IS34(long[] l, int[] m) { l[1] = unchecked((int)0x000CD7FF); m[1] = unchecked((int)0x000CD7FF); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK35() => unchecked((int)0x000CD97B);
    [MethodImpl(NI)] static long IM35(int y) { long r = (long)(y + unchecked((int)0x000CD97B)); r = r * 31 + (long)(y * unchecked((int)0x000CD97B)); r = r * 31 + (long)(y & unchecked((int)0x000CD97B)); return r * 31 + (y < unchecked((int)0x000CD97B) ? 1 : 0) + ((uint)y < 0x000CD97Bu ? 2 : 0) + (y == unchecked((int)0x000CD97B) ? 4 : 0); }
    [MethodImpl(NI)] static long IS35(long[] l, int[] m) { l[1] = unchecked((int)0x000CD97B); m[1] = unchecked((int)0x000CD97B); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK36() => unchecked((int)0x00100000);
    [MethodImpl(NI)] static long IM36(int y) { long r = (long)(y + unchecked((int)0x00100000)); r = r * 31 + (long)(y * unchecked((int)0x00100000)); r = r * 31 + (long)(y & unchecked((int)0x00100000)); return r * 31 + (y < unchecked((int)0x00100000) ? 1 : 0) + ((uint)y < 0x00100000u ? 2 : 0) + (y == unchecked((int)0x00100000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS36(long[] l, int[] m) { l[1] = unchecked((int)0x00100000); m[1] = unchecked((int)0x00100000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK37() => unchecked((int)0x007FE800);
    [MethodImpl(NI)] static long IM37(int y) { long r = (long)(y + unchecked((int)0x007FE800)); r = r * 31 + (long)(y * unchecked((int)0x007FE800)); r = r * 31 + (long)(y & unchecked((int)0x007FE800)); return r * 31 + (y < unchecked((int)0x007FE800) ? 1 : 0) + ((uint)y < 0x007FE800u ? 2 : 0) + (y == unchecked((int)0x007FE800) ? 4 : 0); }
    [MethodImpl(NI)] static long IS37(long[] l, int[] m) { l[1] = unchecked((int)0x007FE800); m[1] = unchecked((int)0x007FE800); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK38() => unchecked((int)0x007FF000);
    [MethodImpl(NI)] static long IM38(int y) { long r = (long)(y + unchecked((int)0x007FF000)); r = r * 31 + (long)(y * unchecked((int)0x007FF000)); r = r * 31 + (long)(y & unchecked((int)0x007FF000)); return r * 31 + (y < unchecked((int)0x007FF000) ? 1 : 0) + ((uint)y < 0x007FF000u ? 2 : 0) + (y == unchecked((int)0x007FF000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS38(long[] l, int[] m) { l[1] = unchecked((int)0x007FF000); m[1] = unchecked((int)0x007FF000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK39() => unchecked((int)0x007FF7FF);
    [MethodImpl(NI)] static long IM39(int y) { long r = (long)(y + unchecked((int)0x007FF7FF)); r = r * 31 + (long)(y * unchecked((int)0x007FF7FF)); r = r * 31 + (long)(y & unchecked((int)0x007FF7FF)); return r * 31 + (y < unchecked((int)0x007FF7FF) ? 1 : 0) + ((uint)y < 0x007FF7FFu ? 2 : 0) + (y == unchecked((int)0x007FF7FF) ? 4 : 0); }
    [MethodImpl(NI)] static long IS39(long[] l, int[] m) { l[1] = unchecked((int)0x007FF7FF); m[1] = unchecked((int)0x007FF7FF); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK40() => unchecked((int)0x007FF800);
    [MethodImpl(NI)] static long IM40(int y) { long r = (long)(y + unchecked((int)0x007FF800)); r = r * 31 + (long)(y * unchecked((int)0x007FF800)); r = r * 31 + (long)(y & unchecked((int)0x007FF800)); return r * 31 + (y < unchecked((int)0x007FF800) ? 1 : 0) + ((uint)y < 0x007FF800u ? 2 : 0) + (y == unchecked((int)0x007FF800) ? 4 : 0); }
    [MethodImpl(NI)] static long IS40(long[] l, int[] m) { l[1] = unchecked((int)0x007FF800); m[1] = unchecked((int)0x007FF800); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK41() => unchecked((int)0x00800000);
    [MethodImpl(NI)] static long IM41(int y) { long r = (long)(y + unchecked((int)0x00800000)); r = r * 31 + (long)(y * unchecked((int)0x00800000)); r = r * 31 + (long)(y & unchecked((int)0x00800000)); return r * 31 + (y < unchecked((int)0x00800000) ? 1 : 0) + ((uint)y < 0x00800000u ? 2 : 0) + (y == unchecked((int)0x00800000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS41(long[] l, int[] m) { l[1] = unchecked((int)0x00800000); m[1] = unchecked((int)0x00800000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK42() => unchecked((int)0x008007FF);
    [MethodImpl(NI)] static long IM42(int y) { long r = (long)(y + unchecked((int)0x008007FF)); r = r * 31 + (long)(y * unchecked((int)0x008007FF)); r = r * 31 + (long)(y & unchecked((int)0x008007FF)); return r * 31 + (y < unchecked((int)0x008007FF) ? 1 : 0) + ((uint)y < 0x008007FFu ? 2 : 0) + (y == unchecked((int)0x008007FF) ? 4 : 0); }
    [MethodImpl(NI)] static long IS42(long[] l, int[] m) { l[1] = unchecked((int)0x008007FF); m[1] = unchecked((int)0x008007FF); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK43() => unchecked((int)0x00800800);
    [MethodImpl(NI)] static long IM43(int y) { long r = (long)(y + unchecked((int)0x00800800)); r = r * 31 + (long)(y * unchecked((int)0x00800800)); r = r * 31 + (long)(y & unchecked((int)0x00800800)); return r * 31 + (y < unchecked((int)0x00800800) ? 1 : 0) + ((uint)y < 0x00800800u ? 2 : 0) + (y == unchecked((int)0x00800800) ? 4 : 0); }
    [MethodImpl(NI)] static long IS43(long[] l, int[] m) { l[1] = unchecked((int)0x00800800); m[1] = unchecked((int)0x00800800); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK44() => unchecked((int)0x00801000);
    [MethodImpl(NI)] static long IM44(int y) { long r = (long)(y + unchecked((int)0x00801000)); r = r * 31 + (long)(y * unchecked((int)0x00801000)); r = r * 31 + (long)(y & unchecked((int)0x00801000)); return r * 31 + (y < unchecked((int)0x00801000) ? 1 : 0) + ((uint)y < 0x00801000u ? 2 : 0) + (y == unchecked((int)0x00801000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS44(long[] l, int[] m) { l[1] = unchecked((int)0x00801000); m[1] = unchecked((int)0x00801000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK45() => unchecked((int)0x008017FF);
    [MethodImpl(NI)] static long IM45(int y) { long r = (long)(y + unchecked((int)0x008017FF)); r = r * 31 + (long)(y * unchecked((int)0x008017FF)); r = r * 31 + (long)(y & unchecked((int)0x008017FF)); return r * 31 + (y < unchecked((int)0x008017FF) ? 1 : 0) + ((uint)y < 0x008017FFu ? 2 : 0) + (y == unchecked((int)0x008017FF) ? 4 : 0); }
    [MethodImpl(NI)] static long IS45(long[] l, int[] m) { l[1] = unchecked((int)0x008017FF); m[1] = unchecked((int)0x008017FF); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK46() => unchecked((int)0x00FFE800);
    [MethodImpl(NI)] static long IM46(int y) { long r = (long)(y + unchecked((int)0x00FFE800)); r = r * 31 + (long)(y * unchecked((int)0x00FFE800)); r = r * 31 + (long)(y & unchecked((int)0x00FFE800)); return r * 31 + (y < unchecked((int)0x00FFE800) ? 1 : 0) + ((uint)y < 0x00FFE800u ? 2 : 0) + (y == unchecked((int)0x00FFE800) ? 4 : 0); }
    [MethodImpl(NI)] static long IS46(long[] l, int[] m) { l[1] = unchecked((int)0x00FFE800); m[1] = unchecked((int)0x00FFE800); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK47() => unchecked((int)0x00FFF000);
    [MethodImpl(NI)] static long IM47(int y) { long r = (long)(y + unchecked((int)0x00FFF000)); r = r * 31 + (long)(y * unchecked((int)0x00FFF000)); r = r * 31 + (long)(y & unchecked((int)0x00FFF000)); return r * 31 + (y < unchecked((int)0x00FFF000) ? 1 : 0) + ((uint)y < 0x00FFF000u ? 2 : 0) + (y == unchecked((int)0x00FFF000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS47(long[] l, int[] m) { l[1] = unchecked((int)0x00FFF000); m[1] = unchecked((int)0x00FFF000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK48() => unchecked((int)0x00FFF7FF);
    [MethodImpl(NI)] static long IM48(int y) { long r = (long)(y + unchecked((int)0x00FFF7FF)); r = r * 31 + (long)(y * unchecked((int)0x00FFF7FF)); r = r * 31 + (long)(y & unchecked((int)0x00FFF7FF)); return r * 31 + (y < unchecked((int)0x00FFF7FF) ? 1 : 0) + ((uint)y < 0x00FFF7FFu ? 2 : 0) + (y == unchecked((int)0x00FFF7FF) ? 4 : 0); }
    [MethodImpl(NI)] static long IS48(long[] l, int[] m) { l[1] = unchecked((int)0x00FFF7FF); m[1] = unchecked((int)0x00FFF7FF); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK49() => unchecked((int)0x00FFF800);
    [MethodImpl(NI)] static long IM49(int y) { long r = (long)(y + unchecked((int)0x00FFF800)); r = r * 31 + (long)(y * unchecked((int)0x00FFF800)); r = r * 31 + (long)(y & unchecked((int)0x00FFF800)); return r * 31 + (y < unchecked((int)0x00FFF800) ? 1 : 0) + ((uint)y < 0x00FFF800u ? 2 : 0) + (y == unchecked((int)0x00FFF800) ? 4 : 0); }
    [MethodImpl(NI)] static long IS49(long[] l, int[] m) { l[1] = unchecked((int)0x00FFF800); m[1] = unchecked((int)0x00FFF800); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK50() => unchecked((int)0x01000000);
    [MethodImpl(NI)] static long IM50(int y) { long r = (long)(y + unchecked((int)0x01000000)); r = r * 31 + (long)(y * unchecked((int)0x01000000)); r = r * 31 + (long)(y & unchecked((int)0x01000000)); return r * 31 + (y < unchecked((int)0x01000000) ? 1 : 0) + ((uint)y < 0x01000000u ? 2 : 0) + (y == unchecked((int)0x01000000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS50(long[] l, int[] m) { l[1] = unchecked((int)0x01000000); m[1] = unchecked((int)0x01000000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK51() => unchecked((int)0x010007FF);
    [MethodImpl(NI)] static long IM51(int y) { long r = (long)(y + unchecked((int)0x010007FF)); r = r * 31 + (long)(y * unchecked((int)0x010007FF)); r = r * 31 + (long)(y & unchecked((int)0x010007FF)); return r * 31 + (y < unchecked((int)0x010007FF) ? 1 : 0) + ((uint)y < 0x010007FFu ? 2 : 0) + (y == unchecked((int)0x010007FF) ? 4 : 0); }
    [MethodImpl(NI)] static long IS51(long[] l, int[] m) { l[1] = unchecked((int)0x010007FF); m[1] = unchecked((int)0x010007FF); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK52() => unchecked((int)0x017FE800);
    [MethodImpl(NI)] static long IM52(int y) { long r = (long)(y + unchecked((int)0x017FE800)); r = r * 31 + (long)(y * unchecked((int)0x017FE800)); r = r * 31 + (long)(y & unchecked((int)0x017FE800)); return r * 31 + (y < unchecked((int)0x017FE800) ? 1 : 0) + ((uint)y < 0x017FE800u ? 2 : 0) + (y == unchecked((int)0x017FE800) ? 4 : 0); }
    [MethodImpl(NI)] static long IS52(long[] l, int[] m) { l[1] = unchecked((int)0x017FE800); m[1] = unchecked((int)0x017FE800); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK53() => unchecked((int)0x017FF000);
    [MethodImpl(NI)] static long IM53(int y) { long r = (long)(y + unchecked((int)0x017FF000)); r = r * 31 + (long)(y * unchecked((int)0x017FF000)); r = r * 31 + (long)(y & unchecked((int)0x017FF000)); return r * 31 + (y < unchecked((int)0x017FF000) ? 1 : 0) + ((uint)y < 0x017FF000u ? 2 : 0) + (y == unchecked((int)0x017FF000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS53(long[] l, int[] m) { l[1] = unchecked((int)0x017FF000); m[1] = unchecked((int)0x017FF000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK54() => unchecked((int)0x017FF7FF);
    [MethodImpl(NI)] static long IM54(int y) { long r = (long)(y + unchecked((int)0x017FF7FF)); r = r * 31 + (long)(y * unchecked((int)0x017FF7FF)); r = r * 31 + (long)(y & unchecked((int)0x017FF7FF)); return r * 31 + (y < unchecked((int)0x017FF7FF) ? 1 : 0) + ((uint)y < 0x017FF7FFu ? 2 : 0) + (y == unchecked((int)0x017FF7FF) ? 4 : 0); }
    [MethodImpl(NI)] static long IS54(long[] l, int[] m) { l[1] = unchecked((int)0x017FF7FF); m[1] = unchecked((int)0x017FF7FF); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK55() => unchecked((int)0x017FF800);
    [MethodImpl(NI)] static long IM55(int y) { long r = (long)(y + unchecked((int)0x017FF800)); r = r * 31 + (long)(y * unchecked((int)0x017FF800)); r = r * 31 + (long)(y & unchecked((int)0x017FF800)); return r * 31 + (y < unchecked((int)0x017FF800) ? 1 : 0) + ((uint)y < 0x017FF800u ? 2 : 0) + (y == unchecked((int)0x017FF800) ? 4 : 0); }
    [MethodImpl(NI)] static long IS55(long[] l, int[] m) { l[1] = unchecked((int)0x017FF800); m[1] = unchecked((int)0x017FF800); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK56() => unchecked((int)0x01800000);
    [MethodImpl(NI)] static long IM56(int y) { long r = (long)(y + unchecked((int)0x01800000)); r = r * 31 + (long)(y * unchecked((int)0x01800000)); r = r * 31 + (long)(y & unchecked((int)0x01800000)); return r * 31 + (y < unchecked((int)0x01800000) ? 1 : 0) + ((uint)y < 0x01800000u ? 2 : 0) + (y == unchecked((int)0x01800000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS56(long[] l, int[] m) { l[1] = unchecked((int)0x01800000); m[1] = unchecked((int)0x01800000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK57() => unchecked((int)0x018007FF);
    [MethodImpl(NI)] static long IM57(int y) { long r = (long)(y + unchecked((int)0x018007FF)); r = r * 31 + (long)(y * unchecked((int)0x018007FF)); r = r * 31 + (long)(y & unchecked((int)0x018007FF)); return r * 31 + (y < unchecked((int)0x018007FF) ? 1 : 0) + ((uint)y < 0x018007FFu ? 2 : 0) + (y == unchecked((int)0x018007FF) ? 4 : 0); }
    [MethodImpl(NI)] static long IS57(long[] l, int[] m) { l[1] = unchecked((int)0x018007FF); m[1] = unchecked((int)0x018007FF); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK58() => unchecked((int)0x10000000);
    [MethodImpl(NI)] static long IM58(int y) { long r = (long)(y + unchecked((int)0x10000000)); r = r * 31 + (long)(y * unchecked((int)0x10000000)); r = r * 31 + (long)(y & unchecked((int)0x10000000)); return r * 31 + (y < unchecked((int)0x10000000) ? 1 : 0) + ((uint)y < 0x10000000u ? 2 : 0) + (y == unchecked((int)0x10000000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS58(long[] l, int[] m) { l[1] = unchecked((int)0x10000000); m[1] = unchecked((int)0x10000000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK59() => unchecked((int)0x12345000);
    [MethodImpl(NI)] static long IM59(int y) { long r = (long)(y + unchecked((int)0x12345000)); r = r * 31 + (long)(y * unchecked((int)0x12345000)); r = r * 31 + (long)(y & unchecked((int)0x12345000)); return r * 31 + (y < unchecked((int)0x12345000) ? 1 : 0) + ((uint)y < 0x12345000u ? 2 : 0) + (y == unchecked((int)0x12345000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS59(long[] l, int[] m) { l[1] = unchecked((int)0x12345000); m[1] = unchecked((int)0x12345000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK60() => unchecked((int)0x12345678);
    [MethodImpl(NI)] static long IM60(int y) { long r = (long)(y + unchecked((int)0x12345678)); r = r * 31 + (long)(y * unchecked((int)0x12345678)); r = r * 31 + (long)(y & unchecked((int)0x12345678)); return r * 31 + (y < unchecked((int)0x12345678) ? 1 : 0) + ((uint)y < 0x12345678u ? 2 : 0) + (y == unchecked((int)0x12345678) ? 4 : 0); }
    [MethodImpl(NI)] static long IS60(long[] l, int[] m) { l[1] = unchecked((int)0x12345678); m[1] = unchecked((int)0x12345678); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK61() => unchecked((int)0x1BD16000);
    [MethodImpl(NI)] static long IM61(int y) { long r = (long)(y + unchecked((int)0x1BD16000)); r = r * 31 + (long)(y * unchecked((int)0x1BD16000)); r = r * 31 + (long)(y & unchecked((int)0x1BD16000)); return r * 31 + (y < unchecked((int)0x1BD16000) ? 1 : 0) + ((uint)y < 0x1BD16000u ? 2 : 0) + (y == unchecked((int)0x1BD16000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS61(long[] l, int[] m) { l[1] = unchecked((int)0x1BD16000); m[1] = unchecked((int)0x1BD16000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK62() => unchecked((int)0x1BD16135);
    [MethodImpl(NI)] static long IM62(int y) { long r = (long)(y + unchecked((int)0x1BD16135)); r = r * 31 + (long)(y * unchecked((int)0x1BD16135)); r = r * 31 + (long)(y & unchecked((int)0x1BD16135)); return r * 31 + (y < unchecked((int)0x1BD16135) ? 1 : 0) + ((uint)y < 0x1BD16135u ? 2 : 0) + (y == unchecked((int)0x1BD16135) ? 4 : 0); }
    [MethodImpl(NI)] static long IS62(long[] l, int[] m) { l[1] = unchecked((int)0x1BD16135); m[1] = unchecked((int)0x1BD16135); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK63() => unchecked((int)0x2468A000);
    [MethodImpl(NI)] static long IM63(int y) { long r = (long)(y + unchecked((int)0x2468A000)); r = r * 31 + (long)(y * unchecked((int)0x2468A000)); r = r * 31 + (long)(y & unchecked((int)0x2468A000)); return r * 31 + (y < unchecked((int)0x2468A000) ? 1 : 0) + ((uint)y < 0x2468A000u ? 2 : 0) + (y == unchecked((int)0x2468A000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS63(long[] l, int[] m) { l[1] = unchecked((int)0x2468A000); m[1] = unchecked((int)0x2468A000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK64() => unchecked((int)0x2468A4F0);
    [MethodImpl(NI)] static long IM64(int y) { long r = (long)(y + unchecked((int)0x2468A4F0)); r = r * 31 + (long)(y * unchecked((int)0x2468A4F0)); r = r * 31 + (long)(y & unchecked((int)0x2468A4F0)); return r * 31 + (y < unchecked((int)0x2468A4F0) ? 1 : 0) + ((uint)y < 0x2468A4F0u ? 2 : 0) + (y == unchecked((int)0x2468A4F0) ? 4 : 0); }
    [MethodImpl(NI)] static long IS64(long[] l, int[] m) { l[1] = unchecked((int)0x2468A4F0); m[1] = unchecked((int)0x2468A4F0); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK65() => unchecked((int)0x2468ACF0);
    [MethodImpl(NI)] static long IM65(int y) { long r = (long)(y + unchecked((int)0x2468ACF0)); r = r * 31 + (long)(y * unchecked((int)0x2468ACF0)); r = r * 31 + (long)(y & unchecked((int)0x2468ACF0)); return r * 31 + (y < unchecked((int)0x2468ACF0) ? 1 : 0) + ((uint)y < 0x2468ACF0u ? 2 : 0) + (y == unchecked((int)0x2468ACF0) ? 4 : 0); }
    [MethodImpl(NI)] static long IS65(long[] l, int[] m) { l[1] = unchecked((int)0x2468ACF0); m[1] = unchecked((int)0x2468ACF0); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK66() => unchecked((int)0x2468B4EF);
    [MethodImpl(NI)] static long IM66(int y) { long r = (long)(y + unchecked((int)0x2468B4EF)); r = r * 31 + (long)(y * unchecked((int)0x2468B4EF)); r = r * 31 + (long)(y & unchecked((int)0x2468B4EF)); return r * 31 + (y < unchecked((int)0x2468B4EF) ? 1 : 0) + ((uint)y < 0x2468B4EFu ? 2 : 0) + (y == unchecked((int)0x2468B4EF) ? 4 : 0); }
    [MethodImpl(NI)] static long IS66(long[] l, int[] m) { l[1] = unchecked((int)0x2468B4EF); m[1] = unchecked((int)0x2468B4EF); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK67() => unchecked((int)0x2FFFC000);
    [MethodImpl(NI)] static long IM67(int y) { long r = (long)(y + unchecked((int)0x2FFFC000)); r = r * 31 + (long)(y * unchecked((int)0x2FFFC000)); r = r * 31 + (long)(y & unchecked((int)0x2FFFC000)); return r * 31 + (y < unchecked((int)0x2FFFC000) ? 1 : 0) + ((uint)y < 0x2FFFC000u ? 2 : 0) + (y == unchecked((int)0x2FFFC000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS67(long[] l, int[] m) { l[1] = unchecked((int)0x2FFFC000); m[1] = unchecked((int)0x2FFFC000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK68() => unchecked((int)0x3154A800);
    [MethodImpl(NI)] static long IM68(int y) { long r = (long)(y + unchecked((int)0x3154A800)); r = r * 31 + (long)(y * unchecked((int)0x3154A800)); r = r * 31 + (long)(y & unchecked((int)0x3154A800)); return r * 31 + (y < unchecked((int)0x3154A800) ? 1 : 0) + ((uint)y < 0x3154A800u ? 2 : 0) + (y == unchecked((int)0x3154A800) ? 4 : 0); }
    [MethodImpl(NI)] static long IS68(long[] l, int[] m) { l[1] = unchecked((int)0x3154A800); m[1] = unchecked((int)0x3154A800); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK69() => unchecked((int)0x3154AD39);
    [MethodImpl(NI)] static long IM69(int y) { long r = (long)(y + unchecked((int)0x3154AD39)); r = r * 31 + (long)(y * unchecked((int)0x3154AD39)); r = r * 31 + (long)(y & unchecked((int)0x3154AD39)); return r * 31 + (y < unchecked((int)0x3154AD39) ? 1 : 0) + ((uint)y < 0x3154AD39u ? 2 : 0) + (y == unchecked((int)0x3154AD39) ? 4 : 0); }
    [MethodImpl(NI)] static long IS69(long[] l, int[] m) { l[1] = unchecked((int)0x3154AD39); m[1] = unchecked((int)0x3154AD39); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK70() => unchecked((int)0x32FD8338);
    [MethodImpl(NI)] static long IM70(int y) { long r = (long)(y + unchecked((int)0x32FD8338)); r = r * 31 + (long)(y * unchecked((int)0x32FD8338)); r = r * 31 + (long)(y & unchecked((int)0x32FD8338)); return r * 31 + (y < unchecked((int)0x32FD8338) ? 1 : 0) + ((uint)y < 0x32FD8338u ? 2 : 0) + (y == unchecked((int)0x32FD8338) ? 4 : 0); }
    [MethodImpl(NI)] static long IS70(long[] l, int[] m) { l[1] = unchecked((int)0x32FD8338); m[1] = unchecked((int)0x32FD8338); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK71() => unchecked((int)0x32FD8800);
    [MethodImpl(NI)] static long IM71(int y) { long r = (long)(y + unchecked((int)0x32FD8800)); r = r * 31 + (long)(y * unchecked((int)0x32FD8800)); r = r * 31 + (long)(y & unchecked((int)0x32FD8800)); return r * 31 + (y < unchecked((int)0x32FD8800) ? 1 : 0) + ((uint)y < 0x32FD8800u ? 2 : 0) + (y == unchecked((int)0x32FD8800) ? 4 : 0); }
    [MethodImpl(NI)] static long IS71(long[] l, int[] m) { l[1] = unchecked((int)0x32FD8800); m[1] = unchecked((int)0x32FD8800); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK72() => unchecked((int)0x3FFFF800);
    [MethodImpl(NI)] static long IM72(int y) { long r = (long)(y + unchecked((int)0x3FFFF800)); r = r * 31 + (long)(y * unchecked((int)0x3FFFF800)); r = r * 31 + (long)(y & unchecked((int)0x3FFFF800)); return r * 31 + (y < unchecked((int)0x3FFFF800) ? 1 : 0) + ((uint)y < 0x3FFFF800u ? 2 : 0) + (y == unchecked((int)0x3FFFF800) ? 4 : 0); }
    [MethodImpl(NI)] static long IS72(long[] l, int[] m) { l[1] = unchecked((int)0x3FFFF800); m[1] = unchecked((int)0x3FFFF800); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK73() => unchecked((int)0x40000000);
    [MethodImpl(NI)] static long IM73(int y) { long r = (long)(y + unchecked((int)0x40000000)); r = r * 31 + (long)(y * unchecked((int)0x40000000)); r = r * 31 + (long)(y & unchecked((int)0x40000000)); return r * 31 + (y < unchecked((int)0x40000000) ? 1 : 0) + ((uint)y < 0x40000000u ? 2 : 0) + (y == unchecked((int)0x40000000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS73(long[] l, int[] m) { l[1] = unchecked((int)0x40000000); m[1] = unchecked((int)0x40000000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK74() => unchecked((int)0x48D14000);
    [MethodImpl(NI)] static long IM74(int y) { long r = (long)(y + unchecked((int)0x48D14000)); r = r * 31 + (long)(y * unchecked((int)0x48D14000)); r = r * 31 + (long)(y & unchecked((int)0x48D14000)); return r * 31 + (y < unchecked((int)0x48D14000) ? 1 : 0) + ((uint)y < 0x48D14000u ? 2 : 0) + (y == unchecked((int)0x48D14000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS74(long[] l, int[] m) { l[1] = unchecked((int)0x48D14000); m[1] = unchecked((int)0x48D14000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK75() => unchecked((int)0x5FFF7800);
    [MethodImpl(NI)] static long IM75(int y) { long r = (long)(y + unchecked((int)0x5FFF7800)); r = r * 31 + (long)(y * unchecked((int)0x5FFF7800)); r = r * 31 + (long)(y & unchecked((int)0x5FFF7800)); return r * 31 + (y < unchecked((int)0x5FFF7800) ? 1 : 0) + ((uint)y < 0x5FFF7800u ? 2 : 0) + (y == unchecked((int)0x5FFF7800) ? 4 : 0); }
    [MethodImpl(NI)] static long IS75(long[] l, int[] m) { l[1] = unchecked((int)0x5FFF7800); m[1] = unchecked((int)0x5FFF7800); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK76() => unchecked((int)0x5FFF8000);
    [MethodImpl(NI)] static long IM76(int y) { long r = (long)(y + unchecked((int)0x5FFF8000)); r = r * 31 + (long)(y * unchecked((int)0x5FFF8000)); r = r * 31 + (long)(y & unchecked((int)0x5FFF8000)); return r * 31 + (y < unchecked((int)0x5FFF8000) ? 1 : 0) + ((uint)y < 0x5FFF8000u ? 2 : 0) + (y == unchecked((int)0x5FFF8000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS76(long[] l, int[] m) { l[1] = unchecked((int)0x5FFF8000); m[1] = unchecked((int)0x5FFF8000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK77() => unchecked((int)0x5FFF87FF);
    [MethodImpl(NI)] static long IM77(int y) { long r = (long)(y + unchecked((int)0x5FFF87FF)); r = r * 31 + (long)(y * unchecked((int)0x5FFF87FF)); r = r * 31 + (long)(y & unchecked((int)0x5FFF87FF)); return r * 31 + (y < unchecked((int)0x5FFF87FF) ? 1 : 0) + ((uint)y < 0x5FFF87FFu ? 2 : 0) + (y == unchecked((int)0x5FFF87FF) ? 4 : 0); }
    [MethodImpl(NI)] static long IS77(long[] l, int[] m) { l[1] = unchecked((int)0x5FFF87FF); m[1] = unchecked((int)0x5FFF87FF); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK78() => unchecked((int)0x75833652);
    [MethodImpl(NI)] static long IM78(int y) { long r = (long)(y + unchecked((int)0x75833652)); r = r * 31 + (long)(y * unchecked((int)0x75833652)); r = r * 31 + (long)(y & unchecked((int)0x75833652)); return r * 31 + (y < unchecked((int)0x75833652) ? 1 : 0) + ((uint)y < 0x75833652u ? 2 : 0) + (y == unchecked((int)0x75833652) ? 4 : 0); }
    [MethodImpl(NI)] static long IS78(long[] l, int[] m) { l[1] = unchecked((int)0x75833652); m[1] = unchecked((int)0x75833652); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK79() => unchecked((int)0x758337FF);
    [MethodImpl(NI)] static long IM79(int y) { long r = (long)(y + unchecked((int)0x758337FF)); r = r * 31 + (long)(y * unchecked((int)0x758337FF)); r = r * 31 + (long)(y & unchecked((int)0x758337FF)); return r * 31 + (y < unchecked((int)0x758337FF) ? 1 : 0) + ((uint)y < 0x758337FFu ? 2 : 0) + (y == unchecked((int)0x758337FF) ? 4 : 0); }
    [MethodImpl(NI)] static long IS79(long[] l, int[] m) { l[1] = unchecked((int)0x758337FF); m[1] = unchecked((int)0x758337FF); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK80() => unchecked((int)0x7FF007FF);
    [MethodImpl(NI)] static long IM80(int y) { long r = (long)(y + unchecked((int)0x7FF007FF)); r = r * 31 + (long)(y * unchecked((int)0x7FF007FF)); r = r * 31 + (long)(y & unchecked((int)0x7FF007FF)); return r * 31 + (y < unchecked((int)0x7FF007FF) ? 1 : 0) + ((uint)y < 0x7FF007FFu ? 2 : 0) + (y == unchecked((int)0x7FF007FF) ? 4 : 0); }
    [MethodImpl(NI)] static long IS80(long[] l, int[] m) { l[1] = unchecked((int)0x7FF007FF); m[1] = unchecked((int)0x7FF007FF); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK81() => unchecked((int)0x7FFFE800);
    [MethodImpl(NI)] static long IM81(int y) { long r = (long)(y + unchecked((int)0x7FFFE800)); r = r * 31 + (long)(y * unchecked((int)0x7FFFE800)); r = r * 31 + (long)(y & unchecked((int)0x7FFFE800)); return r * 31 + (y < unchecked((int)0x7FFFE800) ? 1 : 0) + ((uint)y < 0x7FFFE800u ? 2 : 0) + (y == unchecked((int)0x7FFFE800) ? 4 : 0); }
    [MethodImpl(NI)] static long IS81(long[] l, int[] m) { l[1] = unchecked((int)0x7FFFE800); m[1] = unchecked((int)0x7FFFE800); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK82() => unchecked((int)0x7FFFF000);
    [MethodImpl(NI)] static long IM82(int y) { long r = (long)(y + unchecked((int)0x7FFFF000)); r = r * 31 + (long)(y * unchecked((int)0x7FFFF000)); r = r * 31 + (long)(y & unchecked((int)0x7FFFF000)); return r * 31 + (y < unchecked((int)0x7FFFF000) ? 1 : 0) + ((uint)y < 0x7FFFF000u ? 2 : 0) + (y == unchecked((int)0x7FFFF000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS82(long[] l, int[] m) { l[1] = unchecked((int)0x7FFFF000); m[1] = unchecked((int)0x7FFFF000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK83() => unchecked((int)0x7FFFF7FF);
    [MethodImpl(NI)] static long IM83(int y) { long r = (long)(y + unchecked((int)0x7FFFF7FF)); r = r * 31 + (long)(y * unchecked((int)0x7FFFF7FF)); r = r * 31 + (long)(y & unchecked((int)0x7FFFF7FF)); return r * 31 + (y < unchecked((int)0x7FFFF7FF) ? 1 : 0) + ((uint)y < 0x7FFFF7FFu ? 2 : 0) + (y == unchecked((int)0x7FFFF7FF) ? 4 : 0); }
    [MethodImpl(NI)] static long IS83(long[] l, int[] m) { l[1] = unchecked((int)0x7FFFF7FF); m[1] = unchecked((int)0x7FFFF7FF); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK84() => unchecked((int)0x7FFFF800);
    [MethodImpl(NI)] static long IM84(int y) { long r = (long)(y + unchecked((int)0x7FFFF800)); r = r * 31 + (long)(y * unchecked((int)0x7FFFF800)); r = r * 31 + (long)(y & unchecked((int)0x7FFFF800)); return r * 31 + (y < unchecked((int)0x7FFFF800) ? 1 : 0) + ((uint)y < 0x7FFFF800u ? 2 : 0) + (y == unchecked((int)0x7FFFF800) ? 4 : 0); }
    [MethodImpl(NI)] static long IS84(long[] l, int[] m) { l[1] = unchecked((int)0x7FFFF800); m[1] = unchecked((int)0x7FFFF800); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK85() => unchecked((int)0x7FFFF801);
    [MethodImpl(NI)] static long IM85(int y) { long r = (long)(y + unchecked((int)0x7FFFF801)); r = r * 31 + (long)(y * unchecked((int)0x7FFFF801)); r = r * 31 + (long)(y & unchecked((int)0x7FFFF801)); return r * 31 + (y < unchecked((int)0x7FFFF801) ? 1 : 0) + ((uint)y < 0x7FFFF801u ? 2 : 0) + (y == unchecked((int)0x7FFFF801) ? 4 : 0); }
    [MethodImpl(NI)] static long IS85(long[] l, int[] m) { l[1] = unchecked((int)0x7FFFF801); m[1] = unchecked((int)0x7FFFF801); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK86() => unchecked((int)0x7FFFFFFF);
    [MethodImpl(NI)] static long IM86(int y) { long r = (long)(y + unchecked((int)0x7FFFFFFF)); r = r * 31 + (long)(y * unchecked((int)0x7FFFFFFF)); r = r * 31 + (long)(y & unchecked((int)0x7FFFFFFF)); return r * 31 + (y < unchecked((int)0x7FFFFFFF) ? 1 : 0) + ((uint)y < 0x7FFFFFFFu ? 2 : 0) + (y == unchecked((int)0x7FFFFFFF) ? 4 : 0); }
    [MethodImpl(NI)] static long IS86(long[] l, int[] m) { l[1] = unchecked((int)0x7FFFFFFF); m[1] = unchecked((int)0x7FFFFFFF); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK87() => unchecked((int)0x80000000);
    [MethodImpl(NI)] static long IM87(int y) { long r = (long)(y + unchecked((int)0x80000000)); r = r * 31 + (long)(y * unchecked((int)0x80000000)); r = r * 31 + (long)(y & unchecked((int)0x80000000)); return r * 31 + (y < unchecked((int)0x80000000) ? 1 : 0) + ((uint)y < 0x80000000u ? 2 : 0) + (y == unchecked((int)0x80000000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS87(long[] l, int[] m) { l[1] = unchecked((int)0x80000000); m[1] = unchecked((int)0x80000000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK88() => unchecked((int)0x80000001);
    [MethodImpl(NI)] static long IM88(int y) { long r = (long)(y + unchecked((int)0x80000001)); r = r * 31 + (long)(y * unchecked((int)0x80000001)); r = r * 31 + (long)(y & unchecked((int)0x80000001)); return r * 31 + (y < unchecked((int)0x80000001) ? 1 : 0) + ((uint)y < 0x80000001u ? 2 : 0) + (y == unchecked((int)0x80000001) ? 4 : 0); }
    [MethodImpl(NI)] static long IS88(long[] l, int[] m) { l[1] = unchecked((int)0x80000001); m[1] = unchecked((int)0x80000001); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK89() => unchecked((int)0x800007FF);
    [MethodImpl(NI)] static long IM89(int y) { long r = (long)(y + unchecked((int)0x800007FF)); r = r * 31 + (long)(y * unchecked((int)0x800007FF)); r = r * 31 + (long)(y & unchecked((int)0x800007FF)); return r * 31 + (y < unchecked((int)0x800007FF) ? 1 : 0) + ((uint)y < 0x800007FFu ? 2 : 0) + (y == unchecked((int)0x800007FF) ? 4 : 0); }
    [MethodImpl(NI)] static long IS89(long[] l, int[] m) { l[1] = unchecked((int)0x800007FF); m[1] = unchecked((int)0x800007FF); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK90() => unchecked((int)0x80000800);
    [MethodImpl(NI)] static long IM90(int y) { long r = (long)(y + unchecked((int)0x80000800)); r = r * 31 + (long)(y * unchecked((int)0x80000800)); r = r * 31 + (long)(y & unchecked((int)0x80000800)); return r * 31 + (y < unchecked((int)0x80000800) ? 1 : 0) + ((uint)y < 0x80000800u ? 2 : 0) + (y == unchecked((int)0x80000800) ? 4 : 0); }
    [MethodImpl(NI)] static long IS90(long[] l, int[] m) { l[1] = unchecked((int)0x80000800); m[1] = unchecked((int)0x80000800); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK91() => unchecked((int)0x87654321);
    [MethodImpl(NI)] static long IM91(int y) { long r = (long)(y + unchecked((int)0x87654321)); r = r * 31 + (long)(y * unchecked((int)0x87654321)); r = r * 31 + (long)(y & unchecked((int)0x87654321)); return r * 31 + (y < unchecked((int)0x87654321) ? 1 : 0) + ((uint)y < 0x87654321u ? 2 : 0) + (y == unchecked((int)0x87654321) ? 4 : 0); }
    [MethodImpl(NI)] static long IS91(long[] l, int[] m) { l[1] = unchecked((int)0x87654321); m[1] = unchecked((int)0x87654321); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK92() => unchecked((int)0x91A28000);
    [MethodImpl(NI)] static long IM92(int y) { long r = (long)(y + unchecked((int)0x91A28000)); r = r * 31 + (long)(y * unchecked((int)0x91A28000)); r = r * 31 + (long)(y & unchecked((int)0x91A28000)); return r * 31 + (y < unchecked((int)0x91A28000) ? 1 : 0) + ((uint)y < 0x91A28000u ? 2 : 0) + (y == unchecked((int)0x91A28000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS92(long[] l, int[] m) { l[1] = unchecked((int)0x91A28000); m[1] = unchecked((int)0x91A28000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK93() => unchecked((int)0xBFFEFFF8);
    [MethodImpl(NI)] static long IM93(int y) { long r = (long)(y + unchecked((int)0xBFFEFFF8)); r = r * 31 + (long)(y * unchecked((int)0xBFFEFFF8)); r = r * 31 + (long)(y & unchecked((int)0xBFFEFFF8)); return r * 31 + (y < unchecked((int)0xBFFEFFF8) ? 1 : 0) + ((uint)y < 0xBFFEFFF8u ? 2 : 0) + (y == unchecked((int)0xBFFEFFF8) ? 4 : 0); }
    [MethodImpl(NI)] static long IS93(long[] l, int[] m) { l[1] = unchecked((int)0xBFFEFFF8); m[1] = unchecked((int)0xBFFEFFF8); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK94() => unchecked((int)0xBFFF0000);
    [MethodImpl(NI)] static long IM94(int y) { long r = (long)(y + unchecked((int)0xBFFF0000)); r = r * 31 + (long)(y * unchecked((int)0xBFFF0000)); r = r * 31 + (long)(y & unchecked((int)0xBFFF0000)); return r * 31 + (y < unchecked((int)0xBFFF0000) ? 1 : 0) + ((uint)y < 0xBFFF0000u ? 2 : 0) + (y == unchecked((int)0xBFFF0000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS94(long[] l, int[] m) { l[1] = unchecked((int)0xBFFF0000); m[1] = unchecked((int)0xBFFF0000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK95() => unchecked((int)0xC0000800);
    [MethodImpl(NI)] static long IM95(int y) { long r = (long)(y + unchecked((int)0xC0000800)); r = r * 31 + (long)(y * unchecked((int)0xC0000800)); r = r * 31 + (long)(y & unchecked((int)0xC0000800)); return r * 31 + (y < unchecked((int)0xC0000800) ? 1 : 0) + ((uint)y < 0xC0000800u ? 2 : 0) + (y == unchecked((int)0xC0000800) ? 4 : 0); }
    [MethodImpl(NI)] static long IS95(long[] l, int[] m) { l[1] = unchecked((int)0xC0000800); m[1] = unchecked((int)0xC0000800); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK96() => unchecked((int)0xFDB6C553);
    [MethodImpl(NI)] static long IM96(int y) { long r = (long)(y + unchecked((int)0xFDB6C553)); r = r * 31 + (long)(y * unchecked((int)0xFDB6C553)); r = r * 31 + (long)(y & unchecked((int)0xFDB6C553)); return r * 31 + (y < unchecked((int)0xFDB6C553) ? 1 : 0) + ((uint)y < 0xFDB6C553u ? 2 : 0) + (y == unchecked((int)0xFDB6C553) ? 4 : 0); }
    [MethodImpl(NI)] static long IS96(long[] l, int[] m) { l[1] = unchecked((int)0xFDB6C553); m[1] = unchecked((int)0xFDB6C553); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK97() => unchecked((int)0xFDB6C800);
    [MethodImpl(NI)] static long IM97(int y) { long r = (long)(y + unchecked((int)0xFDB6C800)); r = r * 31 + (long)(y * unchecked((int)0xFDB6C800)); r = r * 31 + (long)(y & unchecked((int)0xFDB6C800)); return r * 31 + (y < unchecked((int)0xFDB6C800) ? 1 : 0) + ((uint)y < 0xFDB6C800u ? 2 : 0) + (y == unchecked((int)0xFDB6C800) ? 4 : 0); }
    [MethodImpl(NI)] static long IS97(long[] l, int[] m) { l[1] = unchecked((int)0xFDB6C800); m[1] = unchecked((int)0xFDB6C800); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK98() => unchecked((int)0xFFE007FE);
    [MethodImpl(NI)] static long IM98(int y) { long r = (long)(y + unchecked((int)0xFFE007FE)); r = r * 31 + (long)(y * unchecked((int)0xFFE007FE)); r = r * 31 + (long)(y & unchecked((int)0xFFE007FE)); return r * 31 + (y < unchecked((int)0xFFE007FE) ? 1 : 0) + ((uint)y < 0xFFE007FEu ? 2 : 0) + (y == unchecked((int)0xFFE007FE) ? 4 : 0); }
    [MethodImpl(NI)] static long IS98(long[] l, int[] m) { l[1] = unchecked((int)0xFFE007FE); m[1] = unchecked((int)0xFFE007FE); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK99() => unchecked((int)0xFFE00FFE);
    [MethodImpl(NI)] static long IM99(int y) { long r = (long)(y + unchecked((int)0xFFE00FFE)); r = r * 31 + (long)(y * unchecked((int)0xFFE00FFE)); r = r * 31 + (long)(y & unchecked((int)0xFFE00FFE)); return r * 31 + (y < unchecked((int)0xFFE00FFE) ? 1 : 0) + ((uint)y < 0xFFE00FFEu ? 2 : 0) + (y == unchecked((int)0xFFE00FFE) ? 4 : 0); }
    [MethodImpl(NI)] static long IS99(long[] l, int[] m) { l[1] = unchecked((int)0xFFE00FFE); m[1] = unchecked((int)0xFFE00FFE); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK100() => unchecked((int)0xFFE017FD);
    [MethodImpl(NI)] static long IM100(int y) { long r = (long)(y + unchecked((int)0xFFE017FD)); r = r * 31 + (long)(y * unchecked((int)0xFFE017FD)); r = r * 31 + (long)(y & unchecked((int)0xFFE017FD)); return r * 31 + (y < unchecked((int)0xFFE017FD) ? 1 : 0) + ((uint)y < 0xFFE017FDu ? 2 : 0) + (y == unchecked((int)0xFFE017FD) ? 4 : 0); }
    [MethodImpl(NI)] static long IS100(long[] l, int[] m) { l[1] = unchecked((int)0xFFE017FD); m[1] = unchecked((int)0xFFE017FD); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK101() => unchecked((int)0xFFFFD800);
    [MethodImpl(NI)] static long IM101(int y) { long r = (long)(y + unchecked((int)0xFFFFD800)); r = r * 31 + (long)(y * unchecked((int)0xFFFFD800)); r = r * 31 + (long)(y & unchecked((int)0xFFFFD800)); return r * 31 + (y < unchecked((int)0xFFFFD800) ? 1 : 0) + ((uint)y < 0xFFFFD800u ? 2 : 0) + (y == unchecked((int)0xFFFFD800) ? 4 : 0); }
    [MethodImpl(NI)] static long IS101(long[] l, int[] m) { l[1] = unchecked((int)0xFFFFD800); m[1] = unchecked((int)0xFFFFD800); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK102() => unchecked((int)0xFFFFE000);
    [MethodImpl(NI)] static long IM102(int y) { long r = (long)(y + unchecked((int)0xFFFFE000)); r = r * 31 + (long)(y * unchecked((int)0xFFFFE000)); r = r * 31 + (long)(y & unchecked((int)0xFFFFE000)); return r * 31 + (y < unchecked((int)0xFFFFE000) ? 1 : 0) + ((uint)y < 0xFFFFE000u ? 2 : 0) + (y == unchecked((int)0xFFFFE000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS102(long[] l, int[] m) { l[1] = unchecked((int)0xFFFFE000); m[1] = unchecked((int)0xFFFFE000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK103() => unchecked((int)0xFFFFE7FE);
    [MethodImpl(NI)] static long IM103(int y) { long r = (long)(y + unchecked((int)0xFFFFE7FE)); r = r * 31 + (long)(y * unchecked((int)0xFFFFE7FE)); r = r * 31 + (long)(y & unchecked((int)0xFFFFE7FE)); return r * 31 + (y < unchecked((int)0xFFFFE7FE) ? 1 : 0) + ((uint)y < 0xFFFFE7FEu ? 2 : 0) + (y == unchecked((int)0xFFFFE7FE) ? 4 : 0); }
    [MethodImpl(NI)] static long IS103(long[] l, int[] m) { l[1] = unchecked((int)0xFFFFE7FE); m[1] = unchecked((int)0xFFFFE7FE); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK104() => unchecked((int)0xFFFFE7FF);
    [MethodImpl(NI)] static long IM104(int y) { long r = (long)(y + unchecked((int)0xFFFFE7FF)); r = r * 31 + (long)(y * unchecked((int)0xFFFFE7FF)); r = r * 31 + (long)(y & unchecked((int)0xFFFFE7FF)); return r * 31 + (y < unchecked((int)0xFFFFE7FF) ? 1 : 0) + ((uint)y < 0xFFFFE7FFu ? 2 : 0) + (y == unchecked((int)0xFFFFE7FF) ? 4 : 0); }
    [MethodImpl(NI)] static long IS104(long[] l, int[] m) { l[1] = unchecked((int)0xFFFFE7FF); m[1] = unchecked((int)0xFFFFE7FF); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK105() => unchecked((int)0xFFFFE800);
    [MethodImpl(NI)] static long IM105(int y) { long r = (long)(y + unchecked((int)0xFFFFE800)); r = r * 31 + (long)(y * unchecked((int)0xFFFFE800)); r = r * 31 + (long)(y & unchecked((int)0xFFFFE800)); return r * 31 + (y < unchecked((int)0xFFFFE800) ? 1 : 0) + ((uint)y < 0xFFFFE800u ? 2 : 0) + (y == unchecked((int)0xFFFFE800) ? 4 : 0); }
    [MethodImpl(NI)] static long IS105(long[] l, int[] m) { l[1] = unchecked((int)0xFFFFE800); m[1] = unchecked((int)0xFFFFE800); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK106() => unchecked((int)0xFFFFE802);
    [MethodImpl(NI)] static long IM106(int y) { long r = (long)(y + unchecked((int)0xFFFFE802)); r = r * 31 + (long)(y * unchecked((int)0xFFFFE802)); r = r * 31 + (long)(y & unchecked((int)0xFFFFE802)); return r * 31 + (y < unchecked((int)0xFFFFE802) ? 1 : 0) + ((uint)y < 0xFFFFE802u ? 2 : 0) + (y == unchecked((int)0xFFFFE802) ? 4 : 0); }
    [MethodImpl(NI)] static long IS106(long[] l, int[] m) { l[1] = unchecked((int)0xFFFFE802); m[1] = unchecked((int)0xFFFFE802); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK107() => unchecked((int)0xFFFFEFFE);
    [MethodImpl(NI)] static long IM107(int y) { long r = (long)(y + unchecked((int)0xFFFFEFFE)); r = r * 31 + (long)(y * unchecked((int)0xFFFFEFFE)); r = r * 31 + (long)(y & unchecked((int)0xFFFFEFFE)); return r * 31 + (y < unchecked((int)0xFFFFEFFE) ? 1 : 0) + ((uint)y < 0xFFFFEFFEu ? 2 : 0) + (y == unchecked((int)0xFFFFEFFE) ? 4 : 0); }
    [MethodImpl(NI)] static long IS107(long[] l, int[] m) { l[1] = unchecked((int)0xFFFFEFFE); m[1] = unchecked((int)0xFFFFEFFE); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK108() => unchecked((int)0xFFFFF000);
    [MethodImpl(NI)] static long IM108(int y) { long r = (long)(y + unchecked((int)0xFFFFF000)); r = r * 31 + (long)(y * unchecked((int)0xFFFFF000)); r = r * 31 + (long)(y & unchecked((int)0xFFFFF000)); return r * 31 + (y < unchecked((int)0xFFFFF000) ? 1 : 0) + ((uint)y < 0xFFFFF000u ? 2 : 0) + (y == unchecked((int)0xFFFFF000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS108(long[] l, int[] m) { l[1] = unchecked((int)0xFFFFF000); m[1] = unchecked((int)0xFFFFF000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK109() => unchecked((int)0xFFFFF002);
    [MethodImpl(NI)] static long IM109(int y) { long r = (long)(y + unchecked((int)0xFFFFF002)); r = r * 31 + (long)(y * unchecked((int)0xFFFFF002)); r = r * 31 + (long)(y & unchecked((int)0xFFFFF002)); return r * 31 + (y < unchecked((int)0xFFFFF002) ? 1 : 0) + ((uint)y < 0xFFFFF002u ? 2 : 0) + (y == unchecked((int)0xFFFFF002) ? 4 : 0); }
    [MethodImpl(NI)] static long IS109(long[] l, int[] m) { l[1] = unchecked((int)0xFFFFF002); m[1] = unchecked((int)0xFFFFF002); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK110() => unchecked((int)0xFFFFF7FD);
    [MethodImpl(NI)] static long IM110(int y) { long r = (long)(y + unchecked((int)0xFFFFF7FD)); r = r * 31 + (long)(y * unchecked((int)0xFFFFF7FD)); r = r * 31 + (long)(y & unchecked((int)0xFFFFF7FD)); return r * 31 + (y < unchecked((int)0xFFFFF7FD) ? 1 : 0) + ((uint)y < 0xFFFFF7FDu ? 2 : 0) + (y == unchecked((int)0xFFFFF7FD) ? 4 : 0); }
    [MethodImpl(NI)] static long IS110(long[] l, int[] m) { l[1] = unchecked((int)0xFFFFF7FD); m[1] = unchecked((int)0xFFFFF7FD); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK111() => unchecked((int)0xFFFFF7FE);
    [MethodImpl(NI)] static long IM111(int y) { long r = (long)(y + unchecked((int)0xFFFFF7FE)); r = r * 31 + (long)(y * unchecked((int)0xFFFFF7FE)); r = r * 31 + (long)(y & unchecked((int)0xFFFFF7FE)); return r * 31 + (y < unchecked((int)0xFFFFF7FE) ? 1 : 0) + ((uint)y < 0xFFFFF7FEu ? 2 : 0) + (y == unchecked((int)0xFFFFF7FE) ? 4 : 0); }
    [MethodImpl(NI)] static long IS111(long[] l, int[] m) { l[1] = unchecked((int)0xFFFFF7FE); m[1] = unchecked((int)0xFFFFF7FE); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK112() => unchecked((int)0xFFFFF7FF);
    [MethodImpl(NI)] static long IM112(int y) { long r = (long)(y + unchecked((int)0xFFFFF7FF)); r = r * 31 + (long)(y * unchecked((int)0xFFFFF7FF)); r = r * 31 + (long)(y & unchecked((int)0xFFFFF7FF)); return r * 31 + (y < unchecked((int)0xFFFFF7FF) ? 1 : 0) + ((uint)y < 0xFFFFF7FFu ? 2 : 0) + (y == unchecked((int)0xFFFFF7FF) ? 4 : 0); }
    [MethodImpl(NI)] static long IS112(long[] l, int[] m) { l[1] = unchecked((int)0xFFFFF7FF); m[1] = unchecked((int)0xFFFFF7FF); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK113() => unchecked((int)0xFFFFF800);
    [MethodImpl(NI)] static long IM113(int y) { long r = (long)(y + unchecked((int)0xFFFFF800)); r = r * 31 + (long)(y * unchecked((int)0xFFFFF800)); r = r * 31 + (long)(y & unchecked((int)0xFFFFF800)); return r * 31 + (y < unchecked((int)0xFFFFF800) ? 1 : 0) + ((uint)y < 0xFFFFF800u ? 2 : 0) + (y == unchecked((int)0xFFFFF800) ? 4 : 0); }
    [MethodImpl(NI)] static long IS113(long[] l, int[] m) { l[1] = unchecked((int)0xFFFFF800); m[1] = unchecked((int)0xFFFFF800); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK114() => unchecked((int)0xFFFFF801);
    [MethodImpl(NI)] static long IM114(int y) { long r = (long)(y + unchecked((int)0xFFFFF801)); r = r * 31 + (long)(y * unchecked((int)0xFFFFF801)); r = r * 31 + (long)(y & unchecked((int)0xFFFFF801)); return r * 31 + (y < unchecked((int)0xFFFFF801) ? 1 : 0) + ((uint)y < 0xFFFFF801u ? 2 : 0) + (y == unchecked((int)0xFFFFF801) ? 4 : 0); }
    [MethodImpl(NI)] static long IS114(long[] l, int[] m) { l[1] = unchecked((int)0xFFFFF801); m[1] = unchecked((int)0xFFFFF801); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK115() => unchecked((int)0xFFFFF802);
    [MethodImpl(NI)] static long IM115(int y) { long r = (long)(y + unchecked((int)0xFFFFF802)); r = r * 31 + (long)(y * unchecked((int)0xFFFFF802)); r = r * 31 + (long)(y & unchecked((int)0xFFFFF802)); return r * 31 + (y < unchecked((int)0xFFFFF802) ? 1 : 0) + ((uint)y < 0xFFFFF802u ? 2 : 0) + (y == unchecked((int)0xFFFFF802) ? 4 : 0); }
    [MethodImpl(NI)] static long IS115(long[] l, int[] m) { l[1] = unchecked((int)0xFFFFF802); m[1] = unchecked((int)0xFFFFF802); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK116() => unchecked((int)0xFFFFFFFE);
    [MethodImpl(NI)] static long IM116(int y) { long r = (long)(y + unchecked((int)0xFFFFFFFE)); r = r * 31 + (long)(y * unchecked((int)0xFFFFFFFE)); r = r * 31 + (long)(y & unchecked((int)0xFFFFFFFE)); return r * 31 + (y < unchecked((int)0xFFFFFFFE) ? 1 : 0) + ((uint)y < 0xFFFFFFFEu ? 2 : 0) + (y == unchecked((int)0xFFFFFFFE) ? 4 : 0); }
    [MethodImpl(NI)] static long IS116(long[] l, int[] m) { l[1] = unchecked((int)0xFFFFFFFE); m[1] = unchecked((int)0xFFFFFFFE); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK117() => unchecked((int)0xFFFFFFFF);
    [MethodImpl(NI)] static long IM117(int y) { long r = (long)(y + unchecked((int)0xFFFFFFFF)); r = r * 31 + (long)(y * unchecked((int)0xFFFFFFFF)); r = r * 31 + (long)(y & unchecked((int)0xFFFFFFFF)); return r * 31 + (y < unchecked((int)0xFFFFFFFF) ? 1 : 0) + ((uint)y < 0xFFFFFFFFu ? 2 : 0) + (y == unchecked((int)0xFFFFFFFF) ? 4 : 0); }
    [MethodImpl(NI)] static long IS117(long[] l, int[] m) { l[1] = unchecked((int)0xFFFFFFFF); m[1] = unchecked((int)0xFFFFFFFF); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK118() => unchecked((int)0x80000000);
    [MethodImpl(NI)] static long IM118(int y) { long r = (long)(y + unchecked((int)0x80000000)); r = r * 31 + (long)(y * unchecked((int)0x80000000)); r = r * 31 + (long)(y & unchecked((int)0x80000000)); return r * 31 + (y < unchecked((int)0x80000000) ? 1 : 0) + ((uint)y < 0x80000000u ? 2 : 0) + (y == unchecked((int)0x80000000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS118(long[] l, int[] m) { l[1] = unchecked((int)0x80000000); m[1] = unchecked((int)0x80000000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK119() => unchecked((int)0x80000001);
    [MethodImpl(NI)] static long IM119(int y) { long r = (long)(y + unchecked((int)0x80000001)); r = r * 31 + (long)(y * unchecked((int)0x80000001)); r = r * 31 + (long)(y & unchecked((int)0x80000001)); return r * 31 + (y < unchecked((int)0x80000001) ? 1 : 0) + ((uint)y < 0x80000001u ? 2 : 0) + (y == unchecked((int)0x80000001) ? 4 : 0); }
    [MethodImpl(NI)] static long IS119(long[] l, int[] m) { l[1] = unchecked((int)0x80000001); m[1] = unchecked((int)0x80000001); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK120() => unchecked((int)0x800007FE);
    [MethodImpl(NI)] static long IM120(int y) { long r = (long)(y + unchecked((int)0x800007FE)); r = r * 31 + (long)(y * unchecked((int)0x800007FE)); r = r * 31 + (long)(y & unchecked((int)0x800007FE)); return r * 31 + (y < unchecked((int)0x800007FE) ? 1 : 0) + ((uint)y < 0x800007FEu ? 2 : 0) + (y == unchecked((int)0x800007FE) ? 4 : 0); }
    [MethodImpl(NI)] static long IS120(long[] l, int[] m) { l[1] = unchecked((int)0x800007FE); m[1] = unchecked((int)0x800007FE); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK121() => unchecked((int)0x800007FF);
    [MethodImpl(NI)] static long IM121(int y) { long r = (long)(y + unchecked((int)0x800007FF)); r = r * 31 + (long)(y * unchecked((int)0x800007FF)); r = r * 31 + (long)(y & unchecked((int)0x800007FF)); return r * 31 + (y < unchecked((int)0x800007FF) ? 1 : 0) + ((uint)y < 0x800007FFu ? 2 : 0) + (y == unchecked((int)0x800007FF) ? 4 : 0); }
    [MethodImpl(NI)] static long IS121(long[] l, int[] m) { l[1] = unchecked((int)0x800007FF); m[1] = unchecked((int)0x800007FF); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK122() => unchecked((int)0x80000800);
    [MethodImpl(NI)] static long IM122(int y) { long r = (long)(y + unchecked((int)0x80000800)); r = r * 31 + (long)(y * unchecked((int)0x80000800)); r = r * 31 + (long)(y & unchecked((int)0x80000800)); return r * 31 + (y < unchecked((int)0x80000800) ? 1 : 0) + ((uint)y < 0x80000800u ? 2 : 0) + (y == unchecked((int)0x80000800) ? 4 : 0); }
    [MethodImpl(NI)] static long IS122(long[] l, int[] m) { l[1] = unchecked((int)0x80000800); m[1] = unchecked((int)0x80000800); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK123() => unchecked((int)0x80000801);
    [MethodImpl(NI)] static long IM123(int y) { long r = (long)(y + unchecked((int)0x80000801)); r = r * 31 + (long)(y * unchecked((int)0x80000801)); r = r * 31 + (long)(y & unchecked((int)0x80000801)); return r * 31 + (y < unchecked((int)0x80000801) ? 1 : 0) + ((uint)y < 0x80000801u ? 2 : 0) + (y == unchecked((int)0x80000801) ? 4 : 0); }
    [MethodImpl(NI)] static long IS123(long[] l, int[] m) { l[1] = unchecked((int)0x80000801); m[1] = unchecked((int)0x80000801); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK124() => unchecked((int)0x80000FFF);
    [MethodImpl(NI)] static long IM124(int y) { long r = (long)(y + unchecked((int)0x80000FFF)); r = r * 31 + (long)(y * unchecked((int)0x80000FFF)); r = r * 31 + (long)(y & unchecked((int)0x80000FFF)); return r * 31 + (y < unchecked((int)0x80000FFF) ? 1 : 0) + ((uint)y < 0x80000FFFu ? 2 : 0) + (y == unchecked((int)0x80000FFF) ? 4 : 0); }
    [MethodImpl(NI)] static long IS124(long[] l, int[] m) { l[1] = unchecked((int)0x80000FFF); m[1] = unchecked((int)0x80000FFF); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK125() => unchecked((int)0x80001000);
    [MethodImpl(NI)] static long IM125(int y) { long r = (long)(y + unchecked((int)0x80001000)); r = r * 31 + (long)(y * unchecked((int)0x80001000)); r = r * 31 + (long)(y & unchecked((int)0x80001000)); return r * 31 + (y < unchecked((int)0x80001000) ? 1 : 0) + ((uint)y < 0x80001000u ? 2 : 0) + (y == unchecked((int)0x80001000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS125(long[] l, int[] m) { l[1] = unchecked((int)0x80001000); m[1] = unchecked((int)0x80001000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK126() => unchecked((int)0x800FF800);
    [MethodImpl(NI)] static long IM126(int y) { long r = (long)(y + unchecked((int)0x800FF800)); r = r * 31 + (long)(y * unchecked((int)0x800FF800)); r = r * 31 + (long)(y & unchecked((int)0x800FF800)); return r * 31 + (y < unchecked((int)0x800FF800) ? 1 : 0) + ((uint)y < 0x800FF800u ? 2 : 0) + (y == unchecked((int)0x800FF800) ? 4 : 0); }
    [MethodImpl(NI)] static long IS126(long[] l, int[] m) { l[1] = unchecked((int)0x800FF800); m[1] = unchecked((int)0x800FF800); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK127() => unchecked((int)0x800FF801);
    [MethodImpl(NI)] static long IM127(int y) { long r = (long)(y + unchecked((int)0x800FF801)); r = r * 31 + (long)(y * unchecked((int)0x800FF801)); r = r * 31 + (long)(y & unchecked((int)0x800FF801)); return r * 31 + (y < unchecked((int)0x800FF801) ? 1 : 0) + ((uint)y < 0x800FF801u ? 2 : 0) + (y == unchecked((int)0x800FF801) ? 4 : 0); }
    [MethodImpl(NI)] static long IS127(long[] l, int[] m) { l[1] = unchecked((int)0x800FF801); m[1] = unchecked((int)0x800FF801); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK128() => unchecked((int)0x87654321);
    [MethodImpl(NI)] static long IM128(int y) { long r = (long)(y + unchecked((int)0x87654321)); r = r * 31 + (long)(y * unchecked((int)0x87654321)); r = r * 31 + (long)(y & unchecked((int)0x87654321)); return r * 31 + (y < unchecked((int)0x87654321) ? 1 : 0) + ((uint)y < 0x87654321u ? 2 : 0) + (y == unchecked((int)0x87654321) ? 4 : 0); }
    [MethodImpl(NI)] static long IS128(long[] l, int[] m) { l[1] = unchecked((int)0x87654321); m[1] = unchecked((int)0x87654321); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK129() => unchecked((int)0x8C5C0507);
    [MethodImpl(NI)] static long IM129(int y) { long r = (long)(y + unchecked((int)0x8C5C0507)); r = r * 31 + (long)(y * unchecked((int)0x8C5C0507)); r = r * 31 + (long)(y & unchecked((int)0x8C5C0507)); return r * 31 + (y < unchecked((int)0x8C5C0507) ? 1 : 0) + ((uint)y < 0x8C5C0507u ? 2 : 0) + (y == unchecked((int)0x8C5C0507) ? 4 : 0); }
    [MethodImpl(NI)] static long IS129(long[] l, int[] m) { l[1] = unchecked((int)0x8C5C0507); m[1] = unchecked((int)0x8C5C0507); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK130() => unchecked((int)0x8C5C07FF);
    [MethodImpl(NI)] static long IM130(int y) { long r = (long)(y + unchecked((int)0x8C5C07FF)); r = r * 31 + (long)(y * unchecked((int)0x8C5C07FF)); r = r * 31 + (long)(y & unchecked((int)0x8C5C07FF)); return r * 31 + (y < unchecked((int)0x8C5C07FF) ? 1 : 0) + ((uint)y < 0x8C5C07FFu ? 2 : 0) + (y == unchecked((int)0x8C5C07FF) ? 4 : 0); }
    [MethodImpl(NI)] static long IS130(long[] l, int[] m) { l[1] = unchecked((int)0x8C5C07FF); m[1] = unchecked((int)0x8C5C07FF); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK131() => unchecked((int)0x94AA5FFB);
    [MethodImpl(NI)] static long IM131(int y) { long r = (long)(y + unchecked((int)0x94AA5FFB)); r = r * 31 + (long)(y * unchecked((int)0x94AA5FFB)); r = r * 31 + (long)(y & unchecked((int)0x94AA5FFB)); return r * 31 + (y < unchecked((int)0x94AA5FFB) ? 1 : 0) + ((uint)y < 0x94AA5FFBu ? 2 : 0) + (y == unchecked((int)0x94AA5FFB) ? 4 : 0); }
    [MethodImpl(NI)] static long IS131(long[] l, int[] m) { l[1] = unchecked((int)0x94AA5FFB); m[1] = unchecked((int)0x94AA5FFB); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK132() => unchecked((int)0x94AA5FFF);
    [MethodImpl(NI)] static long IM132(int y) { long r = (long)(y + unchecked((int)0x94AA5FFF)); r = r * 31 + (long)(y * unchecked((int)0x94AA5FFF)); r = r * 31 + (long)(y & unchecked((int)0x94AA5FFF)); return r * 31 + (y < unchecked((int)0x94AA5FFF) ? 1 : 0) + ((uint)y < 0x94AA5FFFu ? 2 : 0) + (y == unchecked((int)0x94AA5FFF) ? 4 : 0); }
    [MethodImpl(NI)] static long IS132(long[] l, int[] m) { l[1] = unchecked((int)0x94AA5FFF); m[1] = unchecked((int)0x94AA5FFF); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK133() => unchecked((int)0x9E632CE3);
    [MethodImpl(NI)] static long IM133(int y) { long r = (long)(y + unchecked((int)0x9E632CE3)); r = r * 31 + (long)(y * unchecked((int)0x9E632CE3)); r = r * 31 + (long)(y & unchecked((int)0x9E632CE3)); return r * 31 + (y < unchecked((int)0x9E632CE3) ? 1 : 0) + ((uint)y < 0x9E632CE3u ? 2 : 0) + (y == unchecked((int)0x9E632CE3) ? 4 : 0); }
    [MethodImpl(NI)] static long IS133(long[] l, int[] m) { l[1] = unchecked((int)0x9E632CE3); m[1] = unchecked((int)0x9E632CE3); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK134() => unchecked((int)0x9E632FFF);
    [MethodImpl(NI)] static long IM134(int y) { long r = (long)(y + unchecked((int)0x9E632FFF)); r = r * 31 + (long)(y * unchecked((int)0x9E632FFF)); r = r * 31 + (long)(y & unchecked((int)0x9E632FFF)); return r * 31 + (y < unchecked((int)0x9E632FFF) ? 1 : 0) + ((uint)y < 0x9E632FFFu ? 2 : 0) + (y == unchecked((int)0x9E632FFF) ? 4 : 0); }
    [MethodImpl(NI)] static long IS134(long[] l, int[] m) { l[1] = unchecked((int)0x9E632FFF); m[1] = unchecked((int)0x9E632FFF); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK135() => unchecked((int)0xA838D000);
    [MethodImpl(NI)] static long IM135(int y) { long r = (long)(y + unchecked((int)0xA838D000)); r = r * 31 + (long)(y * unchecked((int)0xA838D000)); r = r * 31 + (long)(y & unchecked((int)0xA838D000)); return r * 31 + (y < unchecked((int)0xA838D000) ? 1 : 0) + ((uint)y < 0xA838D000u ? 2 : 0) + (y == unchecked((int)0xA838D000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS135(long[] l, int[] m) { l[1] = unchecked((int)0xA838D000); m[1] = unchecked((int)0xA838D000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK136() => unchecked((int)0xA838DC54);
    [MethodImpl(NI)] static long IM136(int y) { long r = (long)(y + unchecked((int)0xA838DC54)); r = r * 31 + (long)(y * unchecked((int)0xA838DC54)); r = r * 31 + (long)(y & unchecked((int)0xA838DC54)); return r * 31 + (y < unchecked((int)0xA838DC54) ? 1 : 0) + ((uint)y < 0xA838DC54u ? 2 : 0) + (y == unchecked((int)0xA838DC54) ? 4 : 0); }
    [MethodImpl(NI)] static long IS136(long[] l, int[] m) { l[1] = unchecked((int)0xA838DC54); m[1] = unchecked((int)0xA838DC54); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK137() => unchecked((int)0xBFFEFFF8);
    [MethodImpl(NI)] static long IM137(int y) { long r = (long)(y + unchecked((int)0xBFFEFFF8)); r = r * 31 + (long)(y * unchecked((int)0xBFFEFFF8)); r = r * 31 + (long)(y & unchecked((int)0xBFFEFFF8)); return r * 31 + (y < unchecked((int)0xBFFEFFF8) ? 1 : 0) + ((uint)y < 0xBFFEFFF8u ? 2 : 0) + (y == unchecked((int)0xBFFEFFF8) ? 4 : 0); }
    [MethodImpl(NI)] static long IS137(long[] l, int[] m) { l[1] = unchecked((int)0xBFFEFFF8); m[1] = unchecked((int)0xBFFEFFF8); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK138() => unchecked((int)0xBFFF0000);
    [MethodImpl(NI)] static long IM138(int y) { long r = (long)(y + unchecked((int)0xBFFF0000)); r = r * 31 + (long)(y * unchecked((int)0xBFFF0000)); r = r * 31 + (long)(y & unchecked((int)0xBFFF0000)); return r * 31 + (y < unchecked((int)0xBFFF0000) ? 1 : 0) + ((uint)y < 0xBFFF0000u ? 2 : 0) + (y == unchecked((int)0xBFFF0000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS138(long[] l, int[] m) { l[1] = unchecked((int)0xBFFF0000); m[1] = unchecked((int)0xBFFF0000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK139() => unchecked((int)0xC00007FF);
    [MethodImpl(NI)] static long IM139(int y) { long r = (long)(y + unchecked((int)0xC00007FF)); r = r * 31 + (long)(y * unchecked((int)0xC00007FF)); r = r * 31 + (long)(y & unchecked((int)0xC00007FF)); return r * 31 + (y < unchecked((int)0xC00007FF) ? 1 : 0) + ((uint)y < 0xC00007FFu ? 2 : 0) + (y == unchecked((int)0xC00007FF) ? 4 : 0); }
    [MethodImpl(NI)] static long IS139(long[] l, int[] m) { l[1] = unchecked((int)0xC00007FF); m[1] = unchecked((int)0xC00007FF); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK140() => unchecked((int)0xC0000800);
    [MethodImpl(NI)] static long IM140(int y) { long r = (long)(y + unchecked((int)0xC0000800)); r = r * 31 + (long)(y * unchecked((int)0xC0000800)); r = r * 31 + (long)(y & unchecked((int)0xC0000800)); return r * 31 + (y < unchecked((int)0xC0000800) ? 1 : 0) + ((uint)y < 0xC0000800u ? 2 : 0) + (y == unchecked((int)0xC0000800) ? 4 : 0); }
    [MethodImpl(NI)] static long IS140(long[] l, int[] m) { l[1] = unchecked((int)0xC0000800); m[1] = unchecked((int)0xC0000800); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK141() => unchecked((int)0xD0003FFF);
    [MethodImpl(NI)] static long IM141(int y) { long r = (long)(y + unchecked((int)0xD0003FFF)); r = r * 31 + (long)(y * unchecked((int)0xD0003FFF)); r = r * 31 + (long)(y & unchecked((int)0xD0003FFF)); return r * 31 + (y < unchecked((int)0xD0003FFF) ? 1 : 0) + ((uint)y < 0xD0003FFFu ? 2 : 0) + (y == unchecked((int)0xD0003FFF) ? 4 : 0); }
    [MethodImpl(NI)] static long IS141(long[] l, int[] m) { l[1] = unchecked((int)0xD0003FFF); m[1] = unchecked((int)0xD0003FFF); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK142() => unchecked((int)0xD0004000);
    [MethodImpl(NI)] static long IM142(int y) { long r = (long)(y + unchecked((int)0xD0004000)); r = r * 31 + (long)(y * unchecked((int)0xD0004000)); r = r * 31 + (long)(y & unchecked((int)0xD0004000)); return r * 31 + (y < unchecked((int)0xD0004000) ? 1 : 0) + ((uint)y < 0xD0004000u ? 2 : 0) + (y == unchecked((int)0xD0004000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS142(long[] l, int[] m) { l[1] = unchecked((int)0xD0004000); m[1] = unchecked((int)0xD0004000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK143() => unchecked((int)0xED5F23AD);
    [MethodImpl(NI)] static long IM143(int y) { long r = (long)(y + unchecked((int)0xED5F23AD)); r = r * 31 + (long)(y * unchecked((int)0xED5F23AD)); r = r * 31 + (long)(y & unchecked((int)0xED5F23AD)); return r * 31 + (y < unchecked((int)0xED5F23AD) ? 1 : 0) + ((uint)y < 0xED5F23ADu ? 2 : 0) + (y == unchecked((int)0xED5F23AD) ? 4 : 0); }
    [MethodImpl(NI)] static long IS143(long[] l, int[] m) { l[1] = unchecked((int)0xED5F23AD); m[1] = unchecked((int)0xED5F23AD); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK144() => unchecked((int)0xED5F2FFF);
    [MethodImpl(NI)] static long IM144(int y) { long r = (long)(y + unchecked((int)0xED5F2FFF)); r = r * 31 + (long)(y * unchecked((int)0xED5F2FFF)); r = r * 31 + (long)(y & unchecked((int)0xED5F2FFF)); return r * 31 + (y < unchecked((int)0xED5F2FFF) ? 1 : 0) + ((uint)y < 0xED5F2FFFu ? 2 : 0) + (y == unchecked((int)0xED5F2FFF) ? 4 : 0); }
    [MethodImpl(NI)] static long IS144(long[] l, int[] m) { l[1] = unchecked((int)0xED5F2FFF); m[1] = unchecked((int)0xED5F2FFF); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK145() => unchecked((int)0xEDCB0000);
    [MethodImpl(NI)] static long IM145(int y) { long r = (long)(y + unchecked((int)0xEDCB0000)); r = r * 31 + (long)(y * unchecked((int)0xEDCB0000)); r = r * 31 + (long)(y & unchecked((int)0xEDCB0000)); return r * 31 + (y < unchecked((int)0xEDCB0000) ? 1 : 0) + ((uint)y < 0xEDCB0000u ? 2 : 0) + (y == unchecked((int)0xEDCB0000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS145(long[] l, int[] m) { l[1] = unchecked((int)0xEDCB0000); m[1] = unchecked((int)0xEDCB0000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK146() => unchecked((int)0xEDCBA987);
    [MethodImpl(NI)] static long IM146(int y) { long r = (long)(y + unchecked((int)0xEDCBA987)); r = r * 31 + (long)(y * unchecked((int)0xEDCBA987)); r = r * 31 + (long)(y & unchecked((int)0xEDCBA987)); return r * 31 + (y < unchecked((int)0xEDCBA987) ? 1 : 0) + ((uint)y < 0xEDCBA987u ? 2 : 0) + (y == unchecked((int)0xEDCBA987) ? 4 : 0); }
    [MethodImpl(NI)] static long IS146(long[] l, int[] m) { l[1] = unchecked((int)0xEDCBA987); m[1] = unchecked((int)0xEDCBA987); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK147() => unchecked((int)0xEDCBA988);
    [MethodImpl(NI)] static long IM147(int y) { long r = (long)(y + unchecked((int)0xEDCBA988)); r = r * 31 + (long)(y * unchecked((int)0xEDCBA988)); r = r * 31 + (long)(y & unchecked((int)0xEDCBA988)); return r * 31 + (y < unchecked((int)0xEDCBA988) ? 1 : 0) + ((uint)y < 0xEDCBA988u ? 2 : 0) + (y == unchecked((int)0xEDCBA988) ? 4 : 0); }
    [MethodImpl(NI)] static long IS147(long[] l, int[] m) { l[1] = unchecked((int)0xEDCBA988); m[1] = unchecked((int)0xEDCBA988); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK148() => unchecked((int)0xEDD2F7FF);
    [MethodImpl(NI)] static long IM148(int y) { long r = (long)(y + unchecked((int)0xEDD2F7FF)); r = r * 31 + (long)(y * unchecked((int)0xEDD2F7FF)); r = r * 31 + (long)(y & unchecked((int)0xEDD2F7FF)); return r * 31 + (y < unchecked((int)0xEDD2F7FF) ? 1 : 0) + ((uint)y < 0xEDD2F7FFu ? 2 : 0) + (y == unchecked((int)0xEDD2F7FF) ? 4 : 0); }
    [MethodImpl(NI)] static long IS148(long[] l, int[] m) { l[1] = unchecked((int)0xEDD2F7FF); m[1] = unchecked((int)0xEDD2F7FF); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK149() => unchecked((int)0xEDD2FA80);
    [MethodImpl(NI)] static long IM149(int y) { long r = (long)(y + unchecked((int)0xEDD2FA80)); r = r * 31 + (long)(y * unchecked((int)0xEDD2FA80)); r = r * 31 + (long)(y & unchecked((int)0xEDD2FA80)); return r * 31 + (y < unchecked((int)0xEDD2FA80) ? 1 : 0) + ((uint)y < 0xEDD2FA80u ? 2 : 0) + (y == unchecked((int)0xEDD2FA80) ? 4 : 0); }
    [MethodImpl(NI)] static long IS149(long[] l, int[] m) { l[1] = unchecked((int)0xEDD2FA80); m[1] = unchecked((int)0xEDD2FA80); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK150() => unchecked((int)0xF0000000);
    [MethodImpl(NI)] static long IM150(int y) { long r = (long)(y + unchecked((int)0xF0000000)); r = r * 31 + (long)(y * unchecked((int)0xF0000000)); r = r * 31 + (long)(y & unchecked((int)0xF0000000)); return r * 31 + (y < unchecked((int)0xF0000000) ? 1 : 0) + ((uint)y < 0xF0000000u ? 2 : 0) + (y == unchecked((int)0xF0000000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS150(long[] l, int[] m) { l[1] = unchecked((int)0xF0000000); m[1] = unchecked((int)0xF0000000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK151() => unchecked((int)0xF6E58000);
    [MethodImpl(NI)] static long IM151(int y) { long r = (long)(y + unchecked((int)0xF6E58000)); r = r * 31 + (long)(y * unchecked((int)0xF6E58000)); r = r * 31 + (long)(y & unchecked((int)0xF6E58000)); return r * 31 + (y < unchecked((int)0xF6E58000) ? 1 : 0) + ((uint)y < 0xF6E58000u ? 2 : 0) + (y == unchecked((int)0xF6E58000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS151(long[] l, int[] m) { l[1] = unchecked((int)0xF6E58000); m[1] = unchecked((int)0xF6E58000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK152() => unchecked((int)0xFB72C000);
    [MethodImpl(NI)] static long IM152(int y) { long r = (long)(y + unchecked((int)0xFB72C000)); r = r * 31 + (long)(y * unchecked((int)0xFB72C000)); r = r * 31 + (long)(y & unchecked((int)0xFB72C000)); return r * 31 + (y < unchecked((int)0xFB72C000) ? 1 : 0) + ((uint)y < 0xFB72C000u ? 2 : 0) + (y == unchecked((int)0xFB72C000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS152(long[] l, int[] m) { l[1] = unchecked((int)0xFB72C000); m[1] = unchecked((int)0xFB72C000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK153() => unchecked((int)0xFDB96000);
    [MethodImpl(NI)] static long IM153(int y) { long r = (long)(y + unchecked((int)0xFDB96000)); r = r * 31 + (long)(y * unchecked((int)0xFDB96000)); r = r * 31 + (long)(y & unchecked((int)0xFDB96000)); return r * 31 + (y < unchecked((int)0xFDB96000) ? 1 : 0) + ((uint)y < 0xFDB96000u ? 2 : 0) + (y == unchecked((int)0xFDB96000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS153(long[] l, int[] m) { l[1] = unchecked((int)0xFDB96000); m[1] = unchecked((int)0xFDB96000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK154() => unchecked((int)0xFEDCB000);
    [MethodImpl(NI)] static long IM154(int y) { long r = (long)(y + unchecked((int)0xFEDCB000)); r = r * 31 + (long)(y * unchecked((int)0xFEDCB000)); r = r * 31 + (long)(y & unchecked((int)0xFEDCB000)); return r * 31 + (y < unchecked((int)0xFEDCB000) ? 1 : 0) + ((uint)y < 0xFEDCB000u ? 2 : 0) + (y == unchecked((int)0xFEDCB000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS154(long[] l, int[] m) { l[1] = unchecked((int)0xFEDCB000); m[1] = unchecked((int)0xFEDCB000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK155() => unchecked((int)0xFF000000);
    [MethodImpl(NI)] static long IM155(int y) { long r = (long)(y + unchecked((int)0xFF000000)); r = r * 31 + (long)(y * unchecked((int)0xFF000000)); r = r * 31 + (long)(y & unchecked((int)0xFF000000)); return r * 31 + (y < unchecked((int)0xFF000000) ? 1 : 0) + ((uint)y < 0xFF000000u ? 2 : 0) + (y == unchecked((int)0xFF000000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS155(long[] l, int[] m) { l[1] = unchecked((int)0xFF000000); m[1] = unchecked((int)0xFF000000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK156() => unchecked((int)0xFFF00000);
    [MethodImpl(NI)] static long IM156(int y) { long r = (long)(y + unchecked((int)0xFFF00000)); r = r * 31 + (long)(y * unchecked((int)0xFFF00000)); r = r * 31 + (long)(y & unchecked((int)0xFFF00000)); return r * 31 + (y < unchecked((int)0xFFF00000) ? 1 : 0) + ((uint)y < 0xFFF00000u ? 2 : 0) + (y == unchecked((int)0xFFF00000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS156(long[] l, int[] m) { l[1] = unchecked((int)0xFFF00000); m[1] = unchecked((int)0xFFF00000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK157() => unchecked((int)0xFFF43800);
    [MethodImpl(NI)] static long IM157(int y) { long r = (long)(y + unchecked((int)0xFFF43800)); r = r * 31 + (long)(y * unchecked((int)0xFFF43800)); r = r * 31 + (long)(y & unchecked((int)0xFFF43800)); return r * 31 + (y < unchecked((int)0xFFF43800) ? 1 : 0) + ((uint)y < 0xFFF43800u ? 2 : 0) + (y == unchecked((int)0xFFF43800) ? 4 : 0); }
    [MethodImpl(NI)] static long IS157(long[] l, int[] m) { l[1] = unchecked((int)0xFFF43800); m[1] = unchecked((int)0xFFF43800); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK158() => unchecked((int)0xFFF439C7);
    [MethodImpl(NI)] static long IM158(int y) { long r = (long)(y + unchecked((int)0xFFF439C7)); r = r * 31 + (long)(y * unchecked((int)0xFFF439C7)); r = r * 31 + (long)(y & unchecked((int)0xFFF439C7)); return r * 31 + (y < unchecked((int)0xFFF439C7) ? 1 : 0) + ((uint)y < 0xFFF439C7u ? 2 : 0) + (y == unchecked((int)0xFFF439C7) ? 4 : 0); }
    [MethodImpl(NI)] static long IS158(long[] l, int[] m) { l[1] = unchecked((int)0xFFF439C7); m[1] = unchecked((int)0xFFF439C7); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK159() => unchecked((int)0xFFF626EE);
    [MethodImpl(NI)] static long IM159(int y) { long r = (long)(y + unchecked((int)0xFFF626EE)); r = r * 31 + (long)(y * unchecked((int)0xFFF626EE)); r = r * 31 + (long)(y & unchecked((int)0xFFF626EE)); return r * 31 + (y < unchecked((int)0xFFF626EE) ? 1 : 0) + ((uint)y < 0xFFF626EEu ? 2 : 0) + (y == unchecked((int)0xFFF626EE) ? 4 : 0); }
    [MethodImpl(NI)] static long IS159(long[] l, int[] m) { l[1] = unchecked((int)0xFFF626EE); m[1] = unchecked((int)0xFFF626EE); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK160() => unchecked((int)0xFFF62FFF);
    [MethodImpl(NI)] static long IM160(int y) { long r = (long)(y + unchecked((int)0xFFF62FFF)); r = r * 31 + (long)(y * unchecked((int)0xFFF62FFF)); r = r * 31 + (long)(y & unchecked((int)0xFFF62FFF)); return r * 31 + (y < unchecked((int)0xFFF62FFF) ? 1 : 0) + ((uint)y < 0xFFF62FFFu ? 2 : 0) + (y == unchecked((int)0xFFF62FFF) ? 4 : 0); }
    [MethodImpl(NI)] static long IS160(long[] l, int[] m) { l[1] = unchecked((int)0xFFF62FFF); m[1] = unchecked((int)0xFFF62FFF); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK161() => unchecked((int)0xFFF81000);
    [MethodImpl(NI)] static long IM161(int y) { long r = (long)(y + unchecked((int)0xFFF81000)); r = r * 31 + (long)(y * unchecked((int)0xFFF81000)); r = r * 31 + (long)(y & unchecked((int)0xFFF81000)); return r * 31 + (y < unchecked((int)0xFFF81000) ? 1 : 0) + ((uint)y < 0xFFF81000u ? 2 : 0) + (y == unchecked((int)0xFFF81000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS161(long[] l, int[] m) { l[1] = unchecked((int)0xFFF81000); m[1] = unchecked((int)0xFFF81000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK162() => unchecked((int)0xFFF81812);
    [MethodImpl(NI)] static long IM162(int y) { long r = (long)(y + unchecked((int)0xFFF81812)); r = r * 31 + (long)(y * unchecked((int)0xFFF81812)); r = r * 31 + (long)(y & unchecked((int)0xFFF81812)); return r * 31 + (y < unchecked((int)0xFFF81812) ? 1 : 0) + ((uint)y < 0xFFF81812u ? 2 : 0) + (y == unchecked((int)0xFFF81812) ? 4 : 0); }
    [MethodImpl(NI)] static long IS162(long[] l, int[] m) { l[1] = unchecked((int)0xFFF81812); m[1] = unchecked((int)0xFFF81812); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK163() => unchecked((int)0xFFFF0000);
    [MethodImpl(NI)] static long IM163(int y) { long r = (long)(y + unchecked((int)0xFFFF0000)); r = r * 31 + (long)(y * unchecked((int)0xFFFF0000)); r = r * 31 + (long)(y & unchecked((int)0xFFFF0000)); return r * 31 + (y < unchecked((int)0xFFFF0000) ? 1 : 0) + ((uint)y < 0xFFFF0000u ? 2 : 0) + (y == unchecked((int)0xFFFF0000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS163(long[] l, int[] m) { l[1] = unchecked((int)0xFFFF0000); m[1] = unchecked((int)0xFFFF0000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK164() => unchecked((int)0xFFFF8000);
    [MethodImpl(NI)] static long IM164(int y) { long r = (long)(y + unchecked((int)0xFFFF8000)); r = r * 31 + (long)(y * unchecked((int)0xFFFF8000)); r = r * 31 + (long)(y & unchecked((int)0xFFFF8000)); return r * 31 + (y < unchecked((int)0xFFFF8000) ? 1 : 0) + ((uint)y < 0xFFFF8000u ? 2 : 0) + (y == unchecked((int)0xFFFF8000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS164(long[] l, int[] m) { l[1] = unchecked((int)0xFFFF8000); m[1] = unchecked((int)0xFFFF8000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK165() => unchecked((int)0xFFFFC000);
    [MethodImpl(NI)] static long IM165(int y) { long r = (long)(y + unchecked((int)0xFFFFC000)); r = r * 31 + (long)(y * unchecked((int)0xFFFFC000)); r = r * 31 + (long)(y & unchecked((int)0xFFFFC000)); return r * 31 + (y < unchecked((int)0xFFFFC000) ? 1 : 0) + ((uint)y < 0xFFFFC000u ? 2 : 0) + (y == unchecked((int)0xFFFFC000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS165(long[] l, int[] m) { l[1] = unchecked((int)0xFFFFC000); m[1] = unchecked((int)0xFFFFC000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK166() => unchecked((int)0xFFFFE000);
    [MethodImpl(NI)] static long IM166(int y) { long r = (long)(y + unchecked((int)0xFFFFE000)); r = r * 31 + (long)(y * unchecked((int)0xFFFFE000)); r = r * 31 + (long)(y & unchecked((int)0xFFFFE000)); return r * 31 + (y < unchecked((int)0xFFFFE000) ? 1 : 0) + ((uint)y < 0xFFFFE000u ? 2 : 0) + (y == unchecked((int)0xFFFFE000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS166(long[] l, int[] m) { l[1] = unchecked((int)0xFFFFE000); m[1] = unchecked((int)0xFFFFE000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK167() => unchecked((int)0xFFFFE7FF);
    [MethodImpl(NI)] static long IM167(int y) { long r = (long)(y + unchecked((int)0xFFFFE7FF)); r = r * 31 + (long)(y * unchecked((int)0xFFFFE7FF)); r = r * 31 + (long)(y & unchecked((int)0xFFFFE7FF)); return r * 31 + (y < unchecked((int)0xFFFFE7FF) ? 1 : 0) + ((uint)y < 0xFFFFE7FFu ? 2 : 0) + (y == unchecked((int)0xFFFFE7FF) ? 4 : 0); }
    [MethodImpl(NI)] static long IS167(long[] l, int[] m) { l[1] = unchecked((int)0xFFFFE7FF); m[1] = unchecked((int)0xFFFFE7FF); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK168() => unchecked((int)0xFFFFE800);
    [MethodImpl(NI)] static long IM168(int y) { long r = (long)(y + unchecked((int)0xFFFFE800)); r = r * 31 + (long)(y * unchecked((int)0xFFFFE800)); r = r * 31 + (long)(y & unchecked((int)0xFFFFE800)); return r * 31 + (y < unchecked((int)0xFFFFE800) ? 1 : 0) + ((uint)y < 0xFFFFE800u ? 2 : 0) + (y == unchecked((int)0xFFFFE800) ? 4 : 0); }
    [MethodImpl(NI)] static long IS168(long[] l, int[] m) { l[1] = unchecked((int)0xFFFFE800); m[1] = unchecked((int)0xFFFFE800); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK169() => unchecked((int)0xFFFFE801);
    [MethodImpl(NI)] static long IM169(int y) { long r = (long)(y + unchecked((int)0xFFFFE801)); r = r * 31 + (long)(y * unchecked((int)0xFFFFE801)); r = r * 31 + (long)(y & unchecked((int)0xFFFFE801)); return r * 31 + (y < unchecked((int)0xFFFFE801) ? 1 : 0) + ((uint)y < 0xFFFFE801u ? 2 : 0) + (y == unchecked((int)0xFFFFE801) ? 4 : 0); }
    [MethodImpl(NI)] static long IS169(long[] l, int[] m) { l[1] = unchecked((int)0xFFFFE801); m[1] = unchecked((int)0xFFFFE801); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK170() => unchecked((int)0xFFFFEFFF);
    [MethodImpl(NI)] static long IM170(int y) { long r = (long)(y + unchecked((int)0xFFFFEFFF)); r = r * 31 + (long)(y * unchecked((int)0xFFFFEFFF)); r = r * 31 + (long)(y & unchecked((int)0xFFFFEFFF)); return r * 31 + (y < unchecked((int)0xFFFFEFFF) ? 1 : 0) + ((uint)y < 0xFFFFEFFFu ? 2 : 0) + (y == unchecked((int)0xFFFFEFFF) ? 4 : 0); }
    [MethodImpl(NI)] static long IS170(long[] l, int[] m) { l[1] = unchecked((int)0xFFFFEFFF); m[1] = unchecked((int)0xFFFFEFFF); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK171() => unchecked((int)0xFFFFF000);
    [MethodImpl(NI)] static long IM171(int y) { long r = (long)(y + unchecked((int)0xFFFFF000)); r = r * 31 + (long)(y * unchecked((int)0xFFFFF000)); r = r * 31 + (long)(y & unchecked((int)0xFFFFF000)); return r * 31 + (y < unchecked((int)0xFFFFF000) ? 1 : 0) + ((uint)y < 0xFFFFF000u ? 2 : 0) + (y == unchecked((int)0xFFFFF000) ? 4 : 0); }
    [MethodImpl(NI)] static long IS171(long[] l, int[] m) { l[1] = unchecked((int)0xFFFFF000); m[1] = unchecked((int)0xFFFFF000); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK172() => unchecked((int)0xFFFFF001);
    [MethodImpl(NI)] static long IM172(int y) { long r = (long)(y + unchecked((int)0xFFFFF001)); r = r * 31 + (long)(y * unchecked((int)0xFFFFF001)); r = r * 31 + (long)(y & unchecked((int)0xFFFFF001)); return r * 31 + (y < unchecked((int)0xFFFFF001) ? 1 : 0) + ((uint)y < 0xFFFFF001u ? 2 : 0) + (y == unchecked((int)0xFFFFF001) ? 4 : 0); }
    [MethodImpl(NI)] static long IS172(long[] l, int[] m) { l[1] = unchecked((int)0xFFFFF001); m[1] = unchecked((int)0xFFFFF001); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK173() => unchecked((int)0xFFFFF1C8);
    [MethodImpl(NI)] static long IM173(int y) { long r = (long)(y + unchecked((int)0xFFFFF1C8)); r = r * 31 + (long)(y * unchecked((int)0xFFFFF1C8)); r = r * 31 + (long)(y & unchecked((int)0xFFFFF1C8)); return r * 31 + (y < unchecked((int)0xFFFFF1C8) ? 1 : 0) + ((uint)y < 0xFFFFF1C8u ? 2 : 0) + (y == unchecked((int)0xFFFFF1C8) ? 4 : 0); }
    [MethodImpl(NI)] static long IS173(long[] l, int[] m) { l[1] = unchecked((int)0xFFFFF1C8); m[1] = unchecked((int)0xFFFFF1C8); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK174() => unchecked((int)0xFFFFF7FE);
    [MethodImpl(NI)] static long IM174(int y) { long r = (long)(y + unchecked((int)0xFFFFF7FE)); r = r * 31 + (long)(y * unchecked((int)0xFFFFF7FE)); r = r * 31 + (long)(y & unchecked((int)0xFFFFF7FE)); return r * 31 + (y < unchecked((int)0xFFFFF7FE) ? 1 : 0) + ((uint)y < 0xFFFFF7FEu ? 2 : 0) + (y == unchecked((int)0xFFFFF7FE) ? 4 : 0); }
    [MethodImpl(NI)] static long IS174(long[] l, int[] m) { l[1] = unchecked((int)0xFFFFF7FE); m[1] = unchecked((int)0xFFFFF7FE); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK175() => unchecked((int)0xFFFFF7FF);
    [MethodImpl(NI)] static long IM175(int y) { long r = (long)(y + unchecked((int)0xFFFFF7FF)); r = r * 31 + (long)(y * unchecked((int)0xFFFFF7FF)); r = r * 31 + (long)(y & unchecked((int)0xFFFFF7FF)); return r * 31 + (y < unchecked((int)0xFFFFF7FF) ? 1 : 0) + ((uint)y < 0xFFFFF7FFu ? 2 : 0) + (y == unchecked((int)0xFFFFF7FF) ? 4 : 0); }
    [MethodImpl(NI)] static long IS175(long[] l, int[] m) { l[1] = unchecked((int)0xFFFFF7FF); m[1] = unchecked((int)0xFFFFF7FF); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK176() => unchecked((int)0xFFFFF800);
    [MethodImpl(NI)] static long IM176(int y) { long r = (long)(y + unchecked((int)0xFFFFF800)); r = r * 31 + (long)(y * unchecked((int)0xFFFFF800)); r = r * 31 + (long)(y & unchecked((int)0xFFFFF800)); return r * 31 + (y < unchecked((int)0xFFFFF800) ? 1 : 0) + ((uint)y < 0xFFFFF800u ? 2 : 0) + (y == unchecked((int)0xFFFFF800) ? 4 : 0); }
    [MethodImpl(NI)] static long IS176(long[] l, int[] m) { l[1] = unchecked((int)0xFFFFF800); m[1] = unchecked((int)0xFFFFF800); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK177() => unchecked((int)0xFFFFF801);
    [MethodImpl(NI)] static long IM177(int y) { long r = (long)(y + unchecked((int)0xFFFFF801)); r = r * 31 + (long)(y * unchecked((int)0xFFFFF801)); r = r * 31 + (long)(y & unchecked((int)0xFFFFF801)); return r * 31 + (y < unchecked((int)0xFFFFF801) ? 1 : 0) + ((uint)y < 0xFFFFF801u ? 2 : 0) + (y == unchecked((int)0xFFFFF801) ? 4 : 0); }
    [MethodImpl(NI)] static long IS177(long[] l, int[] m) { l[1] = unchecked((int)0xFFFFF801); m[1] = unchecked((int)0xFFFFF801); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK178() => unchecked((int)0xFFFFF802);
    [MethodImpl(NI)] static long IM178(int y) { long r = (long)(y + unchecked((int)0xFFFFF802)); r = r * 31 + (long)(y * unchecked((int)0xFFFFF802)); r = r * 31 + (long)(y & unchecked((int)0xFFFFF802)); return r * 31 + (y < unchecked((int)0xFFFFF802) ? 1 : 0) + ((uint)y < 0xFFFFF802u ? 2 : 0) + (y == unchecked((int)0xFFFFF802) ? 4 : 0); }
    [MethodImpl(NI)] static long IS178(long[] l, int[] m) { l[1] = unchecked((int)0xFFFFF802); m[1] = unchecked((int)0xFFFFF802); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK179() => unchecked((int)0xFFFFFCAB);
    [MethodImpl(NI)] static long IM179(int y) { long r = (long)(y + unchecked((int)0xFFFFFCAB)); r = r * 31 + (long)(y * unchecked((int)0xFFFFFCAB)); r = r * 31 + (long)(y & unchecked((int)0xFFFFFCAB)); return r * 31 + (y < unchecked((int)0xFFFFFCAB) ? 1 : 0) + ((uint)y < 0xFFFFFCABu ? 2 : 0) + (y == unchecked((int)0xFFFFFCAB) ? 4 : 0); }
    [MethodImpl(NI)] static long IS179(long[] l, int[] m) { l[1] = unchecked((int)0xFFFFFCAB); m[1] = unchecked((int)0xFFFFFCAB); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK180() => unchecked((int)0xFFFFFF71);
    [MethodImpl(NI)] static long IM180(int y) { long r = (long)(y + unchecked((int)0xFFFFFF71)); r = r * 31 + (long)(y * unchecked((int)0xFFFFFF71)); r = r * 31 + (long)(y & unchecked((int)0xFFFFFF71)); return r * 31 + (y < unchecked((int)0xFFFFFF71) ? 1 : 0) + ((uint)y < 0xFFFFFF71u ? 2 : 0) + (y == unchecked((int)0xFFFFFF71) ? 4 : 0); }
    [MethodImpl(NI)] static long IS180(long[] l, int[] m) { l[1] = unchecked((int)0xFFFFFF71); m[1] = unchecked((int)0xFFFFFF71); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK181() => unchecked((int)0xFFFFFFFE);
    [MethodImpl(NI)] static long IM181(int y) { long r = (long)(y + unchecked((int)0xFFFFFFFE)); r = r * 31 + (long)(y * unchecked((int)0xFFFFFFFE)); r = r * 31 + (long)(y & unchecked((int)0xFFFFFFFE)); return r * 31 + (y < unchecked((int)0xFFFFFFFE) ? 1 : 0) + ((uint)y < 0xFFFFFFFEu ? 2 : 0) + (y == unchecked((int)0xFFFFFFFE) ? 4 : 0); }
    [MethodImpl(NI)] static long IS181(long[] l, int[] m) { l[1] = unchecked((int)0xFFFFFFFE); m[1] = unchecked((int)0xFFFFFFFE); return l[1] * 3 + m[1]; }
    [MethodImpl(NI)] static int IK182() => unchecked((int)0xFFFFFFFF);
    [MethodImpl(NI)] static long IM182(int y) { long r = (long)(y + unchecked((int)0xFFFFFFFF)); r = r * 31 + (long)(y * unchecked((int)0xFFFFFFFF)); r = r * 31 + (long)(y & unchecked((int)0xFFFFFFFF)); return r * 31 + (y < unchecked((int)0xFFFFFFFF) ? 1 : 0) + ((uint)y < 0xFFFFFFFFu ? 2 : 0) + (y == unchecked((int)0xFFFFFFFF) ? 4 : 0); }
    [MethodImpl(NI)] static long IS182(long[] l, int[] m) { l[1] = unchecked((int)0xFFFFFFFF); m[1] = unchecked((int)0xFFFFFFFF); return l[1] * 3 + m[1]; }

    static int Main()
    {
        long x = Parse("0123456789ABCDEF");
        int y = (int)Parse("13579BDF");
        Part0(x, y);
        Part1(x, y);
        Part2(x, y);
        Part3(x, y);
        Part4(x, y);
        Part5(x, y);
        Part6(x, y);
        Part7(x, y);
        Console.WriteLine(fails == 0 ? "ALL PASS (" + checks + " checks)" : "FAILED " + fails + " of " + checks);
        return fails == 0 ? 0 : 1;
    }

    static void Part0(long x, int y)
    {
        Check("0000000000000000", K0(), A0(x), C0(x), x, y);
        Check("0000000000000001", K1(), A1(x), C1(x), x, y);
        Check("0000000000000002", K2(), A2(x), C2(x), x, y);
        Check("0000000000000152", K3(), A3(x), C3(x), x, y);
        Check("00000000000001AE", K4(), A4(x), C4(x), x, y);
        Check("000000000000025D", K5(), A5(x), C5(x), x, y);
        Check("00000000000007FE", K6(), A6(x), C6(x), x, y);
        Check("00000000000007FF", K7(), A7(x), C7(x), x, y);
        Check("0000000000000800", K8(), A8(x), C8(x), x, y);
        Check("0000000000000801", K9(), A9(x), C9(x), x, y);
        Check("0000000000000802", K10(), A10(x), C10(x), x, y);
        Check("0000000000000CFC", K11(), A11(x), C11(x), x, y);
        Check("0000000000000FFE", K12(), A12(x), C12(x), x, y);
        Check("0000000000000FFF", K13(), A13(x), C13(x), x, y);
        Check("0000000000001000", K14(), A14(x), C14(x), x, y);
        Check("0000000000001002", K15(), A15(x), C15(x), x, y);
        Check("00000000000017FD", K16(), A16(x), C16(x), x, y);
        Check("00000000000017FE", K17(), A17(x), C17(x), x, y);
        Check("00000000000017FF", K18(), A18(x), C18(x), x, y);
        Check("0000000000001800", K19(), A19(x), C19(x), x, y);
        Check("0000000000001801", K20(), A20(x), C20(x), x, y);
        Check("0000000000001FFE", K21(), A21(x), C21(x), x, y);
        Check("0000000000002000", K22(), A22(x), C22(x), x, y);
        Check("00000000000027FD", K23(), A23(x), C23(x), x, y);
        Check("00000000000027FE", K24(), A24(x), C24(x), x, y);
        Check("00000000000027FF", K25(), A25(x), C25(x), x, y);
        Check("0000000000002800", K26(), A26(x), C26(x), x, y);
        Check("0000000000002FFE", K27(), A27(x), C27(x), x, y);
        Check("0000000000003000", K28(), A28(x), C28(x), x, y);
        Check("00000000000037FD", K29(), A29(x), C29(x), x, y);
        Check("00000000000037FF", K30(), A30(x), C30(x), x, y);
        Check("0000000000004000", K31(), A31(x), C31(x), x, y);
        Check("0000000000008000", K32(), A32(x), C32(x), x, y);
        Check("0000000000010000", K33(), A33(x), C33(x), x, y);
        Check("00000000000CD7FF", K34(), A34(x), C34(x), x, y);
        Check("00000000000CD97B", K35(), A35(x), C35(x), x, y);
        Check("0000000000100000", K36(), A36(x), C36(x), x, y);
        Check("00000000007FE800", K37(), A37(x), C37(x), x, y);
        Check("00000000007FF000", K38(), A38(x), C38(x), x, y);
        Check("00000000007FF7FF", K39(), A39(x), C39(x), x, y);
        Check("00000000007FF800", K40(), A40(x), C40(x), x, y);
        Check("0000000000800000", K41(), A41(x), C41(x), x, y);
        Check("00000000008007FF", K42(), A42(x), C42(x), x, y);
        Check("0000000000800800", K43(), A43(x), C43(x), x, y);
        Check("0000000000801000", K44(), A44(x), C44(x), x, y);
        Check("00000000008017FF", K45(), A45(x), C45(x), x, y);
        Check("0000000000FFE800", K46(), A46(x), C46(x), x, y);
        Check("0000000000FFF000", K47(), A47(x), C47(x), x, y);
        Check("0000000000FFF7FF", K48(), A48(x), C48(x), x, y);
        Check("0000000000FFF800", K49(), A49(x), C49(x), x, y);
        Check("0000000001000000", K50(), A50(x), C50(x), x, y);
        Check("00000000010007FF", K51(), A51(x), C51(x), x, y);
        Check("00000000017FE800", K52(), A52(x), C52(x), x, y);
        Check("00000000017FF000", K53(), A53(x), C53(x), x, y);
        Check("00000000017FF7FF", K54(), A54(x), C54(x), x, y);
        Check("00000000017FF800", K55(), A55(x), C55(x), x, y);
        Check("0000000001800000", K56(), A56(x), C56(x), x, y);
        Check("00000000018007FF", K57(), A57(x), C57(x), x, y);
        Check("0000000010000000", K58(), A58(x), C58(x), x, y);
        Check("0000000012345000", K59(), A59(x), C59(x), x, y);
        Check("0000000012345678", K60(), A60(x), C60(x), x, y);
        Check("000000001BD16000", K61(), A61(x), C61(x), x, y);
        Check("000000001BD16135", K62(), A62(x), C62(x), x, y);
        Check("000000002468A000", K63(), A63(x), C63(x), x, y);
        Check("000000002468A4F0", K64(), A64(x), C64(x), x, y);
        Check("000000002468ACF0", K65(), A65(x), C65(x), x, y);
        Check("000000002468B4EF", K66(), A66(x), C66(x), x, y);
        Check("000000002FFFC000", K67(), A67(x), C67(x), x, y);
        Check("000000003154A800", K68(), A68(x), C68(x), x, y);
        Check("000000003154AD39", K69(), A69(x), C69(x), x, y);
        Check("0000000032FD8338", K70(), A70(x), C70(x), x, y);
        Check("0000000032FD8800", K71(), A71(x), C71(x), x, y);
        Check("000000003FFFF800", K72(), A72(x), C72(x), x, y);
        Check("0000000040000000", K73(), A73(x), C73(x), x, y);
        Check("0000000048D14000", K74(), A74(x), C74(x), x, y);
        Check("000000005FFF7800", K75(), A75(x), C75(x), x, y);
        Check("000000005FFF8000", K76(), A76(x), C76(x), x, y);
        Check("000000005FFF87FF", K77(), A77(x), C77(x), x, y);
        Check("0000000075833652", K78(), A78(x), C78(x), x, y);
        Check("00000000758337FF", K79(), A79(x), C79(x), x, y);
        Check("000000007FF007FF", K80(), A80(x), C80(x), x, y);
        Check("000000007FFFE800", K81(), A81(x), C81(x), x, y);
        Check("000000007FFFF000", K82(), A82(x), C82(x), x, y);
        Check("000000007FFFF7FF", K83(), A83(x), C83(x), x, y);
        Check("000000007FFFF800", K84(), A84(x), C84(x), x, y);
        Check("000000007FFFF801", K85(), A85(x), C85(x), x, y);
        Check("000000007FFFFFFF", K86(), A86(x), C86(x), x, y);
        Check("0000000080000000", K87(), A87(x), C87(x), x, y);
        Check("0000000080000001", K88(), A88(x), C88(x), x, y);
        Check("00000000800007FF", K89(), A89(x), C89(x), x, y);
        Check("0000000080000800", K90(), A90(x), C90(x), x, y);
        Check("0000000087654321", K91(), A91(x), C91(x), x, y);
        Check("0000000091A28000", K92(), A92(x), C92(x), x, y);
        Check("00000000BFFEFFF8", K93(), A93(x), C93(x), x, y);
        Check("00000000BFFF0000", K94(), A94(x), C94(x), x, y);
        Check("00000000C0000800", K95(), A95(x), C95(x), x, y);
        Check("00000000FDB6C553", K96(), A96(x), C96(x), x, y);
        Check("00000000FDB6C800", K97(), A97(x), C97(x), x, y);
        Check("00000000FFE007FE", K98(), A98(x), C98(x), x, y);
        Check("00000000FFE00FFE", K99(), A99(x), C99(x), x, y);
    }

    static void Part1(long x, int y)
    {
        Check("00000000FFE017FD", K100(), A100(x), C100(x), x, y);
        Check("00000000FFFFD800", K101(), A101(x), C101(x), x, y);
        Check("00000000FFFFE000", K102(), A102(x), C102(x), x, y);
        Check("00000000FFFFE7FE", K103(), A103(x), C103(x), x, y);
        Check("00000000FFFFE7FF", K104(), A104(x), C104(x), x, y);
        Check("00000000FFFFE800", K105(), A105(x), C105(x), x, y);
        Check("00000000FFFFE802", K106(), A106(x), C106(x), x, y);
        Check("00000000FFFFEFFE", K107(), A107(x), C107(x), x, y);
        Check("00000000FFFFF000", K108(), A108(x), C108(x), x, y);
        Check("00000000FFFFF002", K109(), A109(x), C109(x), x, y);
        Check("00000000FFFFF7FD", K110(), A110(x), C110(x), x, y);
        Check("00000000FFFFF7FE", K111(), A111(x), C111(x), x, y);
        Check("00000000FFFFF7FF", K112(), A112(x), C112(x), x, y);
        Check("00000000FFFFF800", K113(), A113(x), C113(x), x, y);
        Check("00000000FFFFF801", K114(), A114(x), C114(x), x, y);
        Check("00000000FFFFF802", K115(), A115(x), C115(x), x, y);
        Check("00000000FFFFFFFE", K116(), A116(x), C116(x), x, y);
        Check("00000000FFFFFFFF", K117(), A117(x), C117(x), x, y);
        Check("0000000100000000", K118(), A118(x), C118(x), x, y);
        Check("0000000100000001", K119(), A119(x), C119(x), x, y);
        Check("0000000100000002", K120(), A120(x), C120(x), x, y);
        Check("00000001000007FD", K121(), A121(x), C121(x), x, y);
        Check("00000001000007FE", K122(), A122(x), C122(x), x, y);
        Check("00000001000007FF", K123(), A123(x), C123(x), x, y);
        Check("0000000100000800", K124(), A124(x), C124(x), x, y);
        Check("0000000100000801", K125(), A125(x), C125(x), x, y);
        Check("0000000100000FFE", K126(), A126(x), C126(x), x, y);
        Check("0000000100000FFF", K127(), A127(x), C127(x), x, y);
        Check("0000000100001000", K128(), A128(x), C128(x), x, y);
        Check("00000001000017FD", K129(), A129(x), C129(x), x, y);
        Check("00000001000017FF", K130(), A130(x), C130(x), x, y);
        Check("0000000100001800", K131(), A131(x), C131(x), x, y);
        Check("000000010ECA7E42", K132(), A132(x), C132(x), x, y);
        Check("000000010ECA8642", K133(), A133(x), C133(x), x, y);
        Check("000000010ECA8E41", K134(), A134(x), C134(x), x, y);
        Check("0000000112345678", K135(), A135(x), C135(x), x, y);
        Check("0000000123450000", K136(), A136(x), C136(x), x, y);
        Check("000000012FFFC000", K137(), A137(x), C137(x), x, y);
        Check("000000013FFFF800", K138(), A138(x), C138(x), x, y);
        Check("000000017FF007FF", K139(), A139(x), C139(x), x, y);
        Check("000000017FFDF7F0", K140(), A140(x), C140(x), x, y);
        Check("000000017FFDF800", K141(), A141(x), C141(x), x, y);
        Check("000000017FFDFFF0", K142(), A142(x), C142(x), x, y);
        Check("000000017FFE0000", K143(), A143(x), C143(x), x, y);
        Check("000000017FFE07EF", K144(), A144(x), C144(x), x, y);
        Check("000000017FFE07FF", K145(), A145(x), C145(x), x, y);
        Check("000000017FFFF000", K146(), A146(x), C146(x), x, y);
        Check("000000017FFFF7FF", K147(), A147(x), C147(x), x, y);
        Check("000000017FFFF800", K148(), A148(x), C148(x), x, y);
        Check("000000017FFFF801", K149(), A149(x), C149(x), x, y);
        Check("000000017FFFFFFF", K150(), A150(x), C150(x), x, y);
        Check("0000000180000000", K151(), A151(x), C151(x), x, y);
        Check("0000000180000001", K152(), A152(x), C152(x), x, y);
        Check("00000001800007FF", K153(), A153(x), C153(x), x, y);
        Check("0000000180000800", K154(), A154(x), C154(x), x, y);
        Check("0000000180001000", K155(), A155(x), C155(x), x, y);
        Check("00000001800017FF", K156(), A156(x), C156(x), x, y);
        Check("0000000187654321", K157(), A157(x), C157(x), x, y);
        Check("00000001B2C827FF", K158(), A158(x), C158(x), x, y);
        Check("00000001B2C828CA", K159(), A159(x), C159(x), x, y);
        Check("00000001BFFEFFF8", K160(), A160(x), C160(x), x, y);
        Check("00000001BFFF0000", K161(), A161(x), C161(x), x, y);
        Check("00000001C0000800", K162(), A162(x), C162(x), x, y);
        Check("00000001FFFFC000", K163(), A163(x), C163(x), x, y);
        Check("00000001FFFFD800", K164(), A164(x), C164(x), x, y);
        Check("00000001FFFFE000", K165(), A165(x), C165(x), x, y);
        Check("00000001FFFFE7FE", K166(), A166(x), C166(x), x, y);
        Check("00000001FFFFE7FF", K167(), A167(x), C167(x), x, y);
        Check("00000001FFFFE800", K168(), A168(x), C168(x), x, y);
        Check("00000001FFFFEFFE", K169(), A169(x), C169(x), x, y);
        Check("00000001FFFFF000", K170(), A170(x), C170(x), x, y);
        Check("00000001FFFFF7FD", K171(), A171(x), C171(x), x, y);
        Check("00000001FFFFF7FE", K172(), A172(x), C172(x), x, y);
        Check("00000001FFFFF7FF", K173(), A173(x), C173(x), x, y);
        Check("00000001FFFFF800", K174(), A174(x), C174(x), x, y);
        Check("00000001FFFFFFFE", K175(), A175(x), C175(x), x, y);
        Check("00000001FFFFFFFF", K176(), A176(x), C176(x), x, y);
        Check("0000000200000000", K177(), A177(x), C177(x), x, y);
        Check("00000002000007FD", K178(), A178(x), C178(x), x, y);
        Check("00000003FFFF8000", K179(), A179(x), C179(x), x, y);
        Check("0000000400000000", K180(), A180(x), C180(x), x, y);
        Check("00000007FFFF0000", K181(), A181(x), C181(x), x, y);
        Check("00000007FFFFF7FF", K182(), A182(x), C182(x), x, y);
        Check("00000007FFFFF800", K183(), A183(x), C183(x), x, y);
        Check("00000007FFFFF801", K184(), A184(x), C184(x), x, y);
        Check("00000007FFFFFFFE", K185(), A185(x), C185(x), x, y);
        Check("00000007FFFFFFFF", K186(), A186(x), C186(x), x, y);
        Check("0000000800000000", K187(), A187(x), C187(x), x, y);
        Check("0000001234500000", K188(), A188(x), C188(x), x, y);
        Check("0000004000000000", K189(), A189(x), C189(x), x, y);
        Check("0000007FFFF00000", K190(), A190(x), C190(x), x, y);
        Check("000001020EB3F40C", K191(), A191(x), C191(x), x, y);
        Check("000001020EB3F800", K192(), A192(x), C192(x), x, y);
        Check("0000012345000000", K193(), A193(x), C193(x), x, y);
        Check("0000012345677800", K194(), A194(x), C194(x), x, y);
        Check("0000012345678000", K195(), A195(x), C195(x), x, y);
        Check("00000123456787FF", K196(), A196(x), C196(x), x, y);
        Check("000002FFFBFFF800", K197(), A197(x), C197(x), x, y);
        Check("000002FFFC000000", K198(), A198(x), C198(x), x, y);
        Check("000002FFFC0007FF", K199(), A199(x), C199(x), x, y);
    }

    static void Part2(long x, int y)
    {
        Check("000003FFFF7FF800", K200(), A200(x), C200(x), x, y);
        Check("000003FFFF800000", K201(), A201(x), C201(x), x, y);
        Check("000003FFFF8007FF", K202(), A202(x), C202(x), x, y);
        Check("0000040000000000", K203(), A203(x), C203(x), x, y);
        Check("000005311D5B0502", K204(), A204(x), C204(x), x, y);
        Check("000005311D5B07FF", K205(), A205(x), C205(x), x, y);
        Check("000007FEFFFFF800", K206(), A206(x), C206(x), x, y);
        Check("000007FF00000000", K207(), A207(x), C207(x), x, y);
        Check("000007FF000007FF", K208(), A208(x), C208(x), x, y);
        Check("000007FF007FE800", K209(), A209(x), C209(x), x, y);
        Check("000007FF007FF000", K210(), A210(x), C210(x), x, y);
        Check("000007FF007FF7FF", K211(), A211(x), C211(x), x, y);
        Check("000007FFFEFFF800", K212(), A212(x), C212(x), x, y);
        Check("000007FFFF000000", K213(), A213(x), C213(x), x, y);
        Check("000007FFFF0007FF", K214(), A214(x), C214(x), x, y);
        Check("000007FFFF7FE800", K215(), A215(x), C215(x), x, y);
        Check("000007FFFF7FF000", K216(), A216(x), C216(x), x, y);
        Check("000007FFFF7FF7FF", K217(), A217(x), C217(x), x, y);
        Check("000007FFFF7FF800", K218(), A218(x), C218(x), x, y);
        Check("000007FFFF800000", K219(), A219(x), C219(x), x, y);
        Check("000007FFFF8007FF", K220(), A220(x), C220(x), x, y);
        Check("000007FFFF800800", K221(), A221(x), C221(x), x, y);
        Check("000007FFFF801000", K222(), A222(x), C222(x), x, y);
        Check("000007FFFF8017FF", K223(), A223(x), C223(x), x, y);
        Check("000007FFFFFFE800", K224(), A224(x), C224(x), x, y);
        Check("000007FFFFFFF000", K225(), A225(x), C225(x), x, y);
        Check("000007FFFFFFF7FF", K226(), A226(x), C226(x), x, y);
        Check("000007FFFFFFF800", K227(), A227(x), C227(x), x, y);
        Check("0000080000000000", K228(), A228(x), C228(x), x, y);
        Check("00000800000007FF", K229(), A229(x), C229(x), x, y);
        Check("0000080000000800", K230(), A230(x), C230(x), x, y);
        Check("0000080000001000", K231(), A231(x), C231(x), x, y);
        Check("00000800000017FF", K232(), A232(x), C232(x), x, y);
        Check("00000800007FE800", K233(), A233(x), C233(x), x, y);
        Check("00000800007FF000", K234(), A234(x), C234(x), x, y);
        Check("00000800007FF7FF", K235(), A235(x), C235(x), x, y);
        Check("00000800007FF800", K236(), A236(x), C236(x), x, y);
        Check("0000080000800000", K237(), A237(x), C237(x), x, y);
        Check("00000800008007FF", K238(), A238(x), C238(x), x, y);
        Check("00000800FFFFF800", K239(), A239(x), C239(x), x, y);
        Check("0000080100000000", K240(), A240(x), C240(x), x, y);
        Check("00000801000007FF", K241(), A241(x), C241(x), x, y);
        Check("0000080100000801", K242(), A242(x), C242(x), x, y);
        Check("0000087654320800", K243(), A243(x), C243(x), x, y);
        Check("0000087654321000", K244(), A244(x), C244(x), x, y);
        Check("00000876543217FF", K245(), A245(x), C245(x), x, y);
        Check("00000947F3D9FCDA", K246(), A246(x), C246(x), x, y);
        Check("00000947F3D9FFFF", K247(), A247(x), C247(x), x, y);
        Check("00000B5A81B54800", K248(), A248(x), C248(x), x, y);
        Check("00000B5A81B54DAB", K249(), A249(x), C249(x), x, y);
        Check("00000BFFEFFF7800", K250(), A250(x), C250(x), x, y);
        Check("00000BFFEFFF8000", K251(), A251(x), C251(x), x, y);
        Check("00000BFFEFFF87FF", K252(), A252(x), C252(x), x, y);
        Check("00000BFFEFFFF800", K253(), A253(x), C253(x), x, y);
        Check("00000BFFF0000000", K254(), A254(x), C254(x), x, y);
        Check("00000BFFF00007FF", K255(), A255(x), C255(x), x, y);
        Check("00000C00007FF800", K256(), A256(x), C256(x), x, y);
        Check("00000C0000800000", K257(), A257(x), C257(x), x, y);
        Check("00000C00008007FF", K258(), A258(x), C258(x), x, y);
        Check("00000D3D8F1017FF", K259(), A259(x), C259(x), x, y);
        Check("00000D3D8F101F4E", K260(), A260(x), C260(x), x, y);
        Check("00000E1B98B5F13A", K261(), A261(x), C261(x), x, y);
        Check("00000E1B98B5FFFF", K262(), A262(x), C262(x), x, y);
        Check("00000FFEFFFFF800", K263(), A263(x), C263(x), x, y);
        Check("00000FFF00000000", K264(), A264(x), C264(x), x, y);
        Check("00000FFF000007FF", K265(), A265(x), C265(x), x, y);
        Check("00000FFF00000FFF", K266(), A266(x), C266(x), x, y);
        Check("00000FFFFEFFF800", K267(), A267(x), C267(x), x, y);
        Check("00000FFFFF000000", K268(), A268(x), C268(x), x, y);
        Check("00000FFFFF0007FF", K269(), A269(x), C269(x), x, y);
        Check("00000FFFFF7FE800", K270(), A270(x), C270(x), x, y);
        Check("00000FFFFF7FF000", K271(), A271(x), C271(x), x, y);
        Check("00000FFFFF7FF7FF", K272(), A272(x), C272(x), x, y);
        Check("00000FFFFF7FF800", K273(), A273(x), C273(x), x, y);
        Check("00000FFFFF800000", K274(), A274(x), C274(x), x, y);
        Check("00000FFFFF8007FF", K275(), A275(x), C275(x), x, y);
        Check("00000FFFFFFFE800", K276(), A276(x), C276(x), x, y);
        Check("00000FFFFFFFF000", K277(), A277(x), C277(x), x, y);
        Check("00000FFFFFFFF7FF", K278(), A278(x), C278(x), x, y);
        Check("00000FFFFFFFF800", K279(), A279(x), C279(x), x, y);
        Check("0000100000000000", K280(), A280(x), C280(x), x, y);
        Check("00001000000007FF", K281(), A281(x), C281(x), x, y);
        Check("0000100000001000", K282(), A282(x), C282(x), x, y);
        Check("0000123450000000", K283(), A283(x), C283(x), x, y);
        Check("000017FEFFFFF800", K284(), A284(x), C284(x), x, y);
        Check("000017FF00000000", K285(), A285(x), C285(x), x, y);
        Check("000017FF000007FF", K286(), A286(x), C286(x), x, y);
        Check("000017FF000017FF", K287(), A287(x), C287(x), x, y);
        Check("000017FFFFFFF800", K288(), A288(x), C288(x), x, y);
        Check("0000180000000000", K289(), A289(x), C289(x), x, y);
        Check("00001800000007FF", K290(), A290(x), C290(x), x, y);
        Check("0000180000001800", K291(), A291(x), C291(x), x, y);
        Check("0000400000000000", K292(), A292(x), C292(x), x, y);
        Check("00007FFFF0000000", K293(), A293(x), C293(x), x, y);
        Check("00007FFFF7FFFFFF", K294(), A294(x), C294(x), x, y);
        Check("00007FFFF8000000", K295(), A295(x), C295(x), x, y);
        Check("00007FFFF8000001", K296(), A296(x), C296(x), x, y);
        Check("00007FFFFFFFF7FF", K297(), A297(x), C297(x), x, y);
        Check("00007FFFFFFFF800", K298(), A298(x), C298(x), x, y);
        Check("00007FFFFFFFF801", K299(), A299(x), C299(x), x, y);
    }

    static void Part3(long x, int y)
    {
        Check("00007FFFFFFFFFFF", K300(), A300(x), C300(x), x, y);
        Check("0000800000000000", K301(), A301(x), C301(x), x, y);
        Check("0000800000000001", K302(), A302(x), C302(x), x, y);
        Check("0001234500000000", K303(), A303(x), C303(x), x, y);
        Check("0003EC2689D3380D", K304(), A304(x), C304(x), x, y);
        Check("0003EC2689D33FFF", K305(), A305(x), C305(x), x, y);
        Check("0004000000000000", K306(), A306(x), C306(x), x, y);
        Check("000411F744FB97FF", K307(), A307(x), C307(x), x, y);
        Check("000411F744FB9EC2", K308(), A308(x), C308(x), x, y);
        Check("0004F680C8DEB800", K309(), A309(x), C309(x), x, y);
        Check("0004F680C8DEBDA2", K310(), A310(x), C310(x), x, y);
        Check("00069FAF7762A46E", K311(), A311(x), C311(x), x, y);
        Check("00069FAF7762A800", K312(), A312(x), C312(x), x, y);
        Check("0007FFFF00000000", K313(), A313(x), C313(x), x, y);
        Check("000E0F3B78B9D7FF", K314(), A314(x), C314(x), x, y);
        Check("000E0F3B78B9D9DA", K315(), A315(x), C315(x), x, y);
        Check("0123456789ABCDEE", K316(), A316(x), C316(x), x, y);
        Check("0123456789ABCDEF", K317(), A317(x), C317(x), x, y);
        Check("0123456789ABCDF0", K318(), A318(x), C318(x), x, y);
        Check("091A280000000000", K319(), A319(x), C319(x), x, y);
        Check("0FFFFFFFEFFFFFFF", K320(), A320(x), C320(x), x, y);
        Check("0FFFFFFFF0000000", K321(), A321(x), C321(x), x, y);
        Check("0FFFFFFFF0000001", K322(), A322(x), C322(x), x, y);
        Check("10D7FA4C26B687FF", K323(), A323(x), C323(x), x, y);
        Check("10D7FA4C26B68939", K324(), A324(x), C324(x), x, y);
        Check("1234500000000000", K325(), A325(x), C325(x), x, y);
        Check("12345677FFFFF800", K326(), A326(x), C326(x), x, y);
        Check("1234567800000000", K327(), A327(x), C327(x), x, y);
        Check("12345678000007FF", K328(), A328(x), C328(x), x, y);
        Check("1234567812345678", K329(), A329(x), C329(x), x, y);
        Check("1F02501339E97642", K330(), A330(x), C330(x), x, y);
        Check("1F02501339E97FFF", K331(), A331(x), C331(x), x, y);
        Check("2000000000000000", K332(), A332(x), C332(x), x, y);
        Check("2FFFBFFFFFFFF800", K333(), A333(x), C333(x), x, y);
        Check("2FFFC00000000000", K334(), A334(x), C334(x), x, y);
        Check("2FFFC000000007FF", K335(), A335(x), C335(x), x, y);
        Check("2FFFC0002FFFC000", K336(), A336(x), C336(x), x, y);
        Check("3FFFF7FFFFFFF800", K337(), A337(x), C337(x), x, y);
        Check("3FFFF80000000000", K338(), A338(x), C338(x), x, y);
        Check("3FFFF800000007FF", K339(), A339(x), C339(x), x, y);
        Check("3FFFF8003FFFF800", K340(), A340(x), C340(x), x, y);
        Check("4000000000000000", K341(), A341(x), C341(x), x, y);
        Check("5555555555555554", K342(), A342(x), C342(x), x, y);
        Check("5555555555555555", K343(), A343(x), C343(x), x, y);
        Check("5555555555555556", K344(), A344(x), C344(x), x, y);
        Check("6206E2DB648AC7FF", K345(), A345(x), C345(x), x, y);
        Check("6206E2DB648ACA5F", K346(), A346(x), C346(x), x, y);
        Check("6A0A5C690030C46C", K347(), A347(x), C347(x), x, y);
        Check("6A0A5C690030C7FF", K348(), A348(x), C348(x), x, y);
        Check("72E0DA84AC51D587", K349(), A349(x), C349(x), x, y);
        Check("72E0DA84AC51D7FF", K350(), A350(x), C350(x), x, y);
        Check("739B8000FC8005F3", K351(), A351(x), C351(x), x, y);
        Check("739B8000FC8005F4", K352(), A352(x), C352(x), x, y);
        Check("739B8000FC8005F5", K353(), A353(x), C353(x), x, y);
        Check("7D4CDD8A2E36C590", K354(), A354(x), C354(x), x, y);
        Check("7D4CDD8A2E36C800", K355(), A355(x), C355(x), x, y);
        Check("7FF007FEFFFFF800", K356(), A356(x), C356(x), x, y);
        Check("7FF007FF00000000", K357(), A357(x), C357(x), x, y);
        Check("7FF007FF000007FF", K358(), A358(x), C358(x), x, y);
        Check("7FF007FF7FF007FF", K359(), A359(x), C359(x), x, y);
        Check("7FFFEFFFFFFFF800", K360(), A360(x), C360(x), x, y);
        Check("7FFFF00000000000", K361(), A361(x), C361(x), x, y);
        Check("7FFFF000000007FF", K362(), A362(x), C362(x), x, y);
        Check("7FFFF0007FFFF000", K363(), A363(x), C363(x), x, y);
        Check("7FFFF7FEFFFFF800", K364(), A364(x), C364(x), x, y);
        Check("7FFFF7FF00000000", K365(), A365(x), C365(x), x, y);
        Check("7FFFF7FF000007FF", K366(), A366(x), C366(x), x, y);
        Check("7FFFF7FF7FFFF7FF", K367(), A367(x), C367(x), x, y);
        Check("7FFFF7FFFFFFF800", K368(), A368(x), C368(x), x, y);
        Check("7FFFF7FFFFFFFFFE", K369(), A369(x), C369(x), x, y);
        Check("7FFFF7FFFFFFFFFF", K370(), A370(x), C370(x), x, y);
        Check("7FFFF80000000000", K371(), A371(x), C371(x), x, y);
        Check("7FFFF80000000001", K372(), A372(x), C372(x), x, y);
        Check("7FFFF800000007FF", K373(), A373(x), C373(x), x, y);
        Check("7FFFF8007FFFF7FF", K374(), A374(x), C374(x), x, y);
        Check("7FFFF8007FFFF800", K375(), A375(x), C375(x), x, y);
        Check("7FFFF8007FFFF801", K376(), A376(x), C376(x), x, y);
        Check("7FFFF800FFFFF800", K377(), A377(x), C377(x), x, y);
        Check("7FFFF80100000000", K378(), A378(x), C378(x), x, y);
        Check("7FFFF801000007FF", K379(), A379(x), C379(x), x, y);
        Check("7FFFF8017FFFF801", K380(), A380(x), C380(x), x, y);
        Check("7FFFFFFEFFFFF800", K381(), A381(x), C381(x), x, y);
        Check("7FFFFFFEFFFFFFFF", K382(), A382(x), C382(x), x, y);
        Check("7FFFFFFF00000000", K383(), A383(x), C383(x), x, y);
        Check("7FFFFFFF00000001", K384(), A384(x), C384(x), x, y);
        Check("7FFFFFFF000007FF", K385(), A385(x), C385(x), x, y);
        Check("7FFFFFFF7FFFFFFE", K386(), A386(x), C386(x), x, y);
        Check("7FFFFFFF7FFFFFFF", K387(), A387(x), C387(x), x, y);
        Check("7FFFFFFF80000000", K388(), A388(x), C388(x), x, y);
        Check("7FFFFFFFFFFFF7FF", K389(), A389(x), C389(x), x, y);
        Check("7FFFFFFFFFFFF800", K390(), A390(x), C390(x), x, y);
        Check("7FFFFFFFFFFFF801", K391(), A391(x), C391(x), x, y);
        Check("7FFFFFFFFFFFFFFE", K392(), A392(x), C392(x), x, y);
        Check("7FFFFFFFFFFFFFFF", K393(), A393(x), C393(x), x, y);
        Check("8000000000000000", K394(), A394(x), C394(x), x, y);
        Check("8000000000000001", K395(), A395(x), C395(x), x, y);
        Check("80000000000007FF", K396(), A396(x), C396(x), x, y);
        Check("800000007FFFFFFE", K397(), A397(x), C397(x), x, y);
        Check("800000007FFFFFFF", K398(), A398(x), C398(x), x, y);
        Check("8000000080000000", K399(), A399(x), C399(x), x, y);
    }

    static void Part4(long x, int y)
    {
        Check("80000000FFFFF800", K400(), A400(x), C400(x), x, y);
        Check("80000000FFFFFFFE", K401(), A401(x), C401(x), x, y);
        Check("80000000FFFFFFFF", K402(), A402(x), C402(x), x, y);
        Check("8000000100000000", K403(), A403(x), C403(x), x, y);
        Check("80000001000007FF", K404(), A404(x), C404(x), x, y);
        Check("8000000180000001", K405(), A405(x), C405(x), x, y);
        Check("800007FEFFFFF800", K406(), A406(x), C406(x), x, y);
        Check("800007FF00000000", K407(), A407(x), C407(x), x, y);
        Check("800007FF000007FF", K408(), A408(x), C408(x), x, y);
        Check("800007FF800007FF", K409(), A409(x), C409(x), x, y);
        Check("800007FFFFFFF800", K410(), A410(x), C410(x), x, y);
        Check("800007FFFFFFFFFF", K411(), A411(x), C411(x), x, y);
        Check("8000080000000000", K412(), A412(x), C412(x), x, y);
        Check("80000800000007FF", K413(), A413(x), C413(x), x, y);
        Check("8000080080000800", K414(), A414(x), C414(x), x, y);
        Check("8000100000000000", K415(), A415(x), C415(x), x, y);
        Check("87654320FFFFF800", K416(), A416(x), C416(x), x, y);
        Check("8765432100000000", K417(), A417(x), C417(x), x, y);
        Check("87654321000007FF", K418(), A418(x), C418(x), x, y);
        Check("8765432187654321", K419(), A419(x), C419(x), x, y);
        Check("8C647FFF037FFA0B", K420(), A420(x), C420(x), x, y);
        Check("9C18A51ECAA547FF", K421(), A421(x), C421(x), x, y);
        Check("9C18A51ECAA54BFE", K422(), A422(x), C422(x), x, y);
        Check("9E391671AAC2E7FF", K423(), A423(x), C423(x), x, y);
        Check("9E391671AAC2EBCE", K424(), A424(x), C424(x), x, y);
        Check("AAAAAAAAAAAAAAA9", K425(), A425(x), C425(x), x, y);
        Check("AAAAAAAAAAAAAAAA", K426(), A426(x), C426(x), x, y);
        Check("AAAAAAAAAAAAAAAB", K427(), A427(x), C427(x), x, y);
        Check("BF9AE54DB6DB570D", K428(), A428(x), C428(x), x, y);
        Check("BF9AE54DB6DB5800", K429(), A429(x), C429(x), x, y);
        Check("BFFEFFF7FFFFF800", K430(), A430(x), C430(x), x, y);
        Check("BFFEFFF800000000", K431(), A431(x), C431(x), x, y);
        Check("BFFEFFF8000007FF", K432(), A432(x), C432(x), x, y);
        Check("BFFEFFF8BFFEFFF8", K433(), A433(x), C433(x), x, y);
        Check("BFFEFFFFFFFFF800", K434(), A434(x), C434(x), x, y);
        Check("BFFF000000000000", K435(), A435(x), C435(x), x, y);
        Check("BFFF0000000007FF", K436(), A436(x), C436(x), x, y);
        Check("BFFF0000BFFF0000", K437(), A437(x), C437(x), x, y);
        Check("C000000000000000", K438(), A438(x), C438(x), x, y);
        Check("C00007FFFFFFF800", K439(), A439(x), C439(x), x, y);
        Check("C000080000000000", K440(), A440(x), C440(x), x, y);
        Check("C0000800000007FF", K441(), A441(x), C441(x), x, y);
        Check("C0000800C0000800", K442(), A442(x), C442(x), x, y);
        Check("DFFF800000000000", K443(), A443(x), C443(x), x, y);
        Check("ED75DEA57A0D88B5", K444(), A444(x), C444(x), x, y);
        Check("ED75DEA57A0D8FFF", K445(), A445(x), C445(x), x, y);
        Check("F00000000FFFFFFF", K446(), A446(x), C446(x), x, y);
        Check("FEDCB00000000000", K447(), A447(x), C447(x), x, y);
        Check("FEDCBA987654320F", K448(), A448(x), C448(x), x, y);
        Check("FEDCBA9876543210", K449(), A449(x), C449(x), x, y);
        Check("FEDCBA9876543211", K450(), A450(x), C450(x), x, y);
        Check("FF6E580000000000", K451(), A451(x), C451(x), x, y);
        Check("FFF562EEE508C000", K452(), A452(x), C452(x), x, y);
        Check("FFF562EEE508C978", K453(), A453(x), C453(x), x, y);
        Check("FFF7D08BE832D000", K454(), A454(x), C454(x), x, y);
        Check("FFF7D08BE832D72A", K455(), A455(x), C455(x), x, y);
        Check("FFF8000000000000", K456(), A456(x), C456(x), x, y);
        Check("FFF8000100000000", K457(), A457(x), C457(x), x, y);
        Check("FFFBFFF000000000", K458(), A458(x), C458(x), x, y);
        Check("FFFF7FFFFFFFFFFF", K459(), A459(x), C459(x), x, y);
        Check("FFFF800000000000", K460(), A460(x), C460(x), x, y);
        Check("FFFF8000000007FF", K461(), A461(x), C461(x), x, y);
        Check("FFFF800007FFFFFF", K462(), A462(x), C462(x), x, y);
        Check("FFFF800010000000", K463(), A463(x), C463(x), x, y);
        Check("FFFFBFFF00000000", K464(), A464(x), C464(x), x, y);
        Check("FFFFEDCB00000000", K465(), A465(x), C465(x), x, y);
        Check("FFFFEFFFFFFFF800", K466(), A466(x), C466(x), x, y);
        Check("FFFFF00000000000", K467(), A467(x), C467(x), x, y);
        Check("FFFFF000000007FF", K468(), A468(x), C468(x), x, y);
        Check("FFFFF000FFFFF000", K469(), A469(x), C469(x), x, y);
        Check("FFFFF004F522820E", K470(), A470(x), C470(x), x, y);
        Check("FFFFF004F5228800", K471(), A471(x), C471(x), x, y);
        Check("FFFFF2D34F020000", K472(), A472(x), C472(x), x, y);
        Check("FFFFF2D34F02053F", K473(), A473(x), C473(x), x, y);
        Check("FFFFF7FEFFFFF800", K474(), A474(x), C474(x), x, y);
        Check("FFFFF7FF00000000", K475(), A475(x), C475(x), x, y);
        Check("FFFFF7FF000007FF", K476(), A476(x), C476(x), x, y);
        Check("FFFFF7FFFFFFF7FF", K477(), A477(x), C477(x), x, y);
        Check("FFFFF7FFFFFFF800", K478(), A478(x), C478(x), x, y);
        Check("FFFFF80000000000", K479(), A479(x), C479(x), x, y);
        Check("FFFFF800000007FF", K480(), A480(x), C480(x), x, y);
        Check("FFFFF80001000000", K481(), A481(x), C481(x), x, y);
        Check("FFFFF800FFFFF800", K482(), A482(x), C482(x), x, y);
        Check("FFFFFBFFF0000000", K483(), A483(x), C483(x), x, y);
        Check("FFFFFEDCB0000000", K484(), A484(x), C484(x), x, y);
        Check("FFFFFF8000000000", K485(), A485(x), C485(x), x, y);
        Check("FFFFFF8000100000", K486(), A486(x), C486(x), x, y);
        Check("FFFFFFBFFF000000", K487(), A487(x), C487(x), x, y);
        Check("FFFFFFEDCB000000", K488(), A488(x), C488(x), x, y);
        Check("FFFFFFF800000000", K489(), A489(x), C489(x), x, y);
        Check("FFFFFFF8000007FF", K490(), A490(x), C490(x), x, y);
        Check("FFFFFFF800010000", K491(), A491(x), C491(x), x, y);
        Check("FFFFFFFBFFF00000", K492(), A492(x), C492(x), x, y);
        Check("FFFFFFFC00000000", K493(), A493(x), C493(x), x, y);
        Check("FFFFFFFC00008000", K494(), A494(x), C494(x), x, y);
        Check("FFFFFFFDFFF80000", K495(), A495(x), C495(x), x, y);
        Check("FFFFFFFE00000000", K496(), A496(x), C496(x), x, y);
        Check("FFFFFFFE00004000", K497(), A497(x), C497(x), x, y);
        Check("FFFFFFFE16E456A0", K498(), A498(x), C498(x), x, y);
        Check("FFFFFFFE16E45FFF", K499(), A499(x), C499(x), x, y);
    }

    static void Part5(long x, int y)
    {
        Check("FFFFFFFEDCB00000", K500(), A500(x), C500(x), x, y);
        Check("FFFFFFFEFFFC0000", K501(), A501(x), C501(x), x, y);
        Check("FFFFFFFEFFFFF800", K502(), A502(x), C502(x), x, y);
        Check("FFFFFFFF00000000", K503(), A503(x), C503(x), x, y);
        Check("FFFFFFFF00000001", K504(), A504(x), C504(x), x, y);
        Check("FFFFFFFF000007FF", K505(), A505(x), C505(x), x, y);
        Check("FFFFFFFF00000800", K506(), A506(x), C506(x), x, y);
        Check("FFFFFFFF00000801", K507(), A507(x), C507(x), x, y);
        Check("FFFFFFFF00000FFF", K508(), A508(x), C508(x), x, y);
        Check("FFFFFFFF00001000", K509(), A509(x), C509(x), x, y);
        Check("FFFFFFFF000017FF", K510(), A510(x), C510(x), x, y);
        Check("FFFFFFFF00001800", K511(), A511(x), C511(x), x, y);
        Check("FFFFFFFF00002000", K512(), A512(x), C512(x), x, y);
        Check("FFFFFFFF12345678", K513(), A513(x), C513(x), x, y);
        Check("FFFFFFFF2FFFC000", K514(), A514(x), C514(x), x, y);
        Check("FFFFFFFF3FFFF7FF", K515(), A515(x), C515(x), x, y);
        Check("FFFFFFFF3FFFF800", K516(), A516(x), C516(x), x, y);
        Check("FFFFFFFF4000FFFF", K517(), A517(x), C517(x), x, y);
        Check("FFFFFFFF40010000", K518(), A518(x), C518(x), x, y);
        Check("FFFFFFFF40010007", K519(), A519(x), C519(x), x, y);
        Check("FFFFFFFF40010008", K520(), A520(x), C520(x), x, y);
        Check("FFFFFFFF4B1237FF", K521(), A521(x), C521(x), x, y);
        Check("FFFFFFFF4B123C37", K522(), A522(x), C522(x), x, y);
        Check("FFFFFFFF789ABCDE", K523(), A523(x), C523(x), x, y);
        Check("FFFFFFFF789ABCDF", K524(), A524(x), C524(x), x, y);
        Check("FFFFFFFF7FF007FF", K525(), A525(x), C525(x), x, y);
        Check("FFFFFFFF7FFE0000", K526(), A526(x), C526(x), x, y);
        Check("FFFFFFFF7FFFF000", K527(), A527(x), C527(x), x, y);
        Check("FFFFFFFF7FFFF7FE", K528(), A528(x), C528(x), x, y);
        Check("FFFFFFFF7FFFF7FF", K529(), A529(x), C529(x), x, y);
        Check("FFFFFFFF7FFFF800", K530(), A530(x), C530(x), x, y);
        Check("FFFFFFFF7FFFF801", K531(), A531(x), C531(x), x, y);
        Check("FFFFFFFF7FFFFFFE", K532(), A532(x), C532(x), x, y);
        Check("FFFFFFFF7FFFFFFF", K533(), A533(x), C533(x), x, y);
        Check("FFFFFFFF80000000", K534(), A534(x), C534(x), x, y);
        Check("FFFFFFFF80000001", K535(), A535(x), C535(x), x, y);
        Check("FFFFFFFF800007FE", K536(), A536(x), C536(x), x, y);
        Check("FFFFFFFF800007FF", K537(), A537(x), C537(x), x, y);
        Check("FFFFFFFF80000800", K538(), A538(x), C538(x), x, y);
        Check("FFFFFFFF80000801", K539(), A539(x), C539(x), x, y);
        Check("FFFFFFFF80000FFF", K540(), A540(x), C540(x), x, y);
        Check("FFFFFFFF80001000", K541(), A541(x), C541(x), x, y);
        Check("FFFFFFFF800FF800", K542(), A542(x), C542(x), x, y);
        Check("FFFFFFFF800FF801", K543(), A543(x), C543(x), x, y);
        Check("FFFFFFFF87654321", K544(), A544(x), C544(x), x, y);
        Check("FFFFFFFF8C5C0507", K545(), A545(x), C545(x), x, y);
        Check("FFFFFFFF8C5C07FF", K546(), A546(x), C546(x), x, y);
        Check("FFFFFFFF94AA5FFB", K547(), A547(x), C547(x), x, y);
        Check("FFFFFFFF94AA5FFF", K548(), A548(x), C548(x), x, y);
        Check("FFFFFFFF9E632CE3", K549(), A549(x), C549(x), x, y);
        Check("FFFFFFFF9E632FFF", K550(), A550(x), C550(x), x, y);
        Check("FFFFFFFFA838D000", K551(), A551(x), C551(x), x, y);
        Check("FFFFFFFFA838DC54", K552(), A552(x), C552(x), x, y);
        Check("FFFFFFFFBFFEFFF8", K553(), A553(x), C553(x), x, y);
        Check("FFFFFFFFBFFF0000", K554(), A554(x), C554(x), x, y);
        Check("FFFFFFFFC00007FF", K555(), A555(x), C555(x), x, y);
        Check("FFFFFFFFC0000800", K556(), A556(x), C556(x), x, y);
        Check("FFFFFFFFD0003FFF", K557(), A557(x), C557(x), x, y);
        Check("FFFFFFFFD0004000", K558(), A558(x), C558(x), x, y);
        Check("FFFFFFFFED5F23AD", K559(), A559(x), C559(x), x, y);
        Check("FFFFFFFFED5F2FFF", K560(), A560(x), C560(x), x, y);
        Check("FFFFFFFFEDCB0000", K561(), A561(x), C561(x), x, y);
        Check("FFFFFFFFEDCBA987", K562(), A562(x), C562(x), x, y);
        Check("FFFFFFFFEDCBA988", K563(), A563(x), C563(x), x, y);
        Check("FFFFFFFFEDD2F7FF", K564(), A564(x), C564(x), x, y);
        Check("FFFFFFFFEDD2FA80", K565(), A565(x), C565(x), x, y);
        Check("FFFFFFFFF0000000", K566(), A566(x), C566(x), x, y);
        Check("FFFFFFFFF6E58000", K567(), A567(x), C567(x), x, y);
        Check("FFFFFFFFFB72C000", K568(), A568(x), C568(x), x, y);
        Check("FFFFFFFFFDB96000", K569(), A569(x), C569(x), x, y);
        Check("FFFFFFFFFEDCB000", K570(), A570(x), C570(x), x, y);
        Check("FFFFFFFFFF000000", K571(), A571(x), C571(x), x, y);
        Check("FFFFFFFFFFF00000", K572(), A572(x), C572(x), x, y);
        Check("FFFFFFFFFFF43800", K573(), A573(x), C573(x), x, y);
        Check("FFFFFFFFFFF439C7", K574(), A574(x), C574(x), x, y);
        Check("FFFFFFFFFFF626EE", K575(), A575(x), C575(x), x, y);
        Check("FFFFFFFFFFF62FFF", K576(), A576(x), C576(x), x, y);
        Check("FFFFFFFFFFF81000", K577(), A577(x), C577(x), x, y);
        Check("FFFFFFFFFFF81812", K578(), A578(x), C578(x), x, y);
        Check("FFFFFFFFFFFF0000", K579(), A579(x), C579(x), x, y);
        Check("FFFFFFFFFFFF8000", K580(), A580(x), C580(x), x, y);
        Check("FFFFFFFFFFFFC000", K581(), A581(x), C581(x), x, y);
        Check("FFFFFFFFFFFFE000", K582(), A582(x), C582(x), x, y);
        Check("FFFFFFFFFFFFE7FF", K583(), A583(x), C583(x), x, y);
        Check("FFFFFFFFFFFFE800", K584(), A584(x), C584(x), x, y);
        Check("FFFFFFFFFFFFE801", K585(), A585(x), C585(x), x, y);
        Check("FFFFFFFFFFFFEFFF", K586(), A586(x), C586(x), x, y);
        Check("FFFFFFFFFFFFF000", K587(), A587(x), C587(x), x, y);
        Check("FFFFFFFFFFFFF001", K588(), A588(x), C588(x), x, y);
        Check("FFFFFFFFFFFFF1C8", K589(), A589(x), C589(x), x, y);
        Check("FFFFFFFFFFFFF7FE", K590(), A590(x), C590(x), x, y);
        Check("FFFFFFFFFFFFF7FF", K591(), A591(x), C591(x), x, y);
        Check("FFFFFFFFFFFFF800", K592(), A592(x), C592(x), x, y);
        Check("FFFFFFFFFFFFF801", K593(), A593(x), C593(x), x, y);
        Check("FFFFFFFFFFFFF802", K594(), A594(x), C594(x), x, y);
        Check("FFFFFFFFFFFFFCAB", K595(), A595(x), C595(x), x, y);
        Check("FFFFFFFFFFFFFF71", K596(), A596(x), C596(x), x, y);
        Check("FFFFFFFFFFFFFFFE", K597(), A597(x), C597(x), x, y);
        Check("FFFFFFFFFFFFFFFF", K598(), A598(x), C598(x), x, y);
        CheckInt("0000000000000000", IK0(), IM0(y), IS0(cellL, cellI), y);
    }

    static void Part6(long x, int y)
    {
        CheckInt("0000000000000001", IK1(), IM1(y), IS1(cellL, cellI), y);
        CheckInt("0000000000000002", IK2(), IM2(y), IS2(cellL, cellI), y);
        CheckInt("0000000000000152", IK3(), IM3(y), IS3(cellL, cellI), y);
        CheckInt("00000000000001AE", IK4(), IM4(y), IS4(cellL, cellI), y);
        CheckInt("000000000000025D", IK5(), IM5(y), IS5(cellL, cellI), y);
        CheckInt("00000000000007FE", IK6(), IM6(y), IS6(cellL, cellI), y);
        CheckInt("00000000000007FF", IK7(), IM7(y), IS7(cellL, cellI), y);
        CheckInt("0000000000000800", IK8(), IM8(y), IS8(cellL, cellI), y);
        CheckInt("0000000000000801", IK9(), IM9(y), IS9(cellL, cellI), y);
        CheckInt("0000000000000802", IK10(), IM10(y), IS10(cellL, cellI), y);
        CheckInt("0000000000000CFC", IK11(), IM11(y), IS11(cellL, cellI), y);
        CheckInt("0000000000000FFE", IK12(), IM12(y), IS12(cellL, cellI), y);
        CheckInt("0000000000000FFF", IK13(), IM13(y), IS13(cellL, cellI), y);
        CheckInt("0000000000001000", IK14(), IM14(y), IS14(cellL, cellI), y);
        CheckInt("0000000000001002", IK15(), IM15(y), IS15(cellL, cellI), y);
        CheckInt("00000000000017FD", IK16(), IM16(y), IS16(cellL, cellI), y);
        CheckInt("00000000000017FE", IK17(), IM17(y), IS17(cellL, cellI), y);
        CheckInt("00000000000017FF", IK18(), IM18(y), IS18(cellL, cellI), y);
        CheckInt("0000000000001800", IK19(), IM19(y), IS19(cellL, cellI), y);
        CheckInt("0000000000001801", IK20(), IM20(y), IS20(cellL, cellI), y);
        CheckInt("0000000000001FFE", IK21(), IM21(y), IS21(cellL, cellI), y);
        CheckInt("0000000000002000", IK22(), IM22(y), IS22(cellL, cellI), y);
        CheckInt("00000000000027FD", IK23(), IM23(y), IS23(cellL, cellI), y);
        CheckInt("00000000000027FE", IK24(), IM24(y), IS24(cellL, cellI), y);
        CheckInt("00000000000027FF", IK25(), IM25(y), IS25(cellL, cellI), y);
        CheckInt("0000000000002800", IK26(), IM26(y), IS26(cellL, cellI), y);
        CheckInt("0000000000002FFE", IK27(), IM27(y), IS27(cellL, cellI), y);
        CheckInt("0000000000003000", IK28(), IM28(y), IS28(cellL, cellI), y);
        CheckInt("00000000000037FD", IK29(), IM29(y), IS29(cellL, cellI), y);
        CheckInt("00000000000037FF", IK30(), IM30(y), IS30(cellL, cellI), y);
        CheckInt("0000000000004000", IK31(), IM31(y), IS31(cellL, cellI), y);
        CheckInt("0000000000008000", IK32(), IM32(y), IS32(cellL, cellI), y);
        CheckInt("0000000000010000", IK33(), IM33(y), IS33(cellL, cellI), y);
        CheckInt("00000000000CD7FF", IK34(), IM34(y), IS34(cellL, cellI), y);
        CheckInt("00000000000CD97B", IK35(), IM35(y), IS35(cellL, cellI), y);
        CheckInt("0000000000100000", IK36(), IM36(y), IS36(cellL, cellI), y);
        CheckInt("00000000007FE800", IK37(), IM37(y), IS37(cellL, cellI), y);
        CheckInt("00000000007FF000", IK38(), IM38(y), IS38(cellL, cellI), y);
        CheckInt("00000000007FF7FF", IK39(), IM39(y), IS39(cellL, cellI), y);
        CheckInt("00000000007FF800", IK40(), IM40(y), IS40(cellL, cellI), y);
        CheckInt("0000000000800000", IK41(), IM41(y), IS41(cellL, cellI), y);
        CheckInt("00000000008007FF", IK42(), IM42(y), IS42(cellL, cellI), y);
        CheckInt("0000000000800800", IK43(), IM43(y), IS43(cellL, cellI), y);
        CheckInt("0000000000801000", IK44(), IM44(y), IS44(cellL, cellI), y);
        CheckInt("00000000008017FF", IK45(), IM45(y), IS45(cellL, cellI), y);
        CheckInt("0000000000FFE800", IK46(), IM46(y), IS46(cellL, cellI), y);
        CheckInt("0000000000FFF000", IK47(), IM47(y), IS47(cellL, cellI), y);
        CheckInt("0000000000FFF7FF", IK48(), IM48(y), IS48(cellL, cellI), y);
        CheckInt("0000000000FFF800", IK49(), IM49(y), IS49(cellL, cellI), y);
        CheckInt("0000000001000000", IK50(), IM50(y), IS50(cellL, cellI), y);
        CheckInt("00000000010007FF", IK51(), IM51(y), IS51(cellL, cellI), y);
        CheckInt("00000000017FE800", IK52(), IM52(y), IS52(cellL, cellI), y);
        CheckInt("00000000017FF000", IK53(), IM53(y), IS53(cellL, cellI), y);
        CheckInt("00000000017FF7FF", IK54(), IM54(y), IS54(cellL, cellI), y);
        CheckInt("00000000017FF800", IK55(), IM55(y), IS55(cellL, cellI), y);
        CheckInt("0000000001800000", IK56(), IM56(y), IS56(cellL, cellI), y);
        CheckInt("00000000018007FF", IK57(), IM57(y), IS57(cellL, cellI), y);
        CheckInt("0000000010000000", IK58(), IM58(y), IS58(cellL, cellI), y);
        CheckInt("0000000012345000", IK59(), IM59(y), IS59(cellL, cellI), y);
        CheckInt("0000000012345678", IK60(), IM60(y), IS60(cellL, cellI), y);
        CheckInt("000000001BD16000", IK61(), IM61(y), IS61(cellL, cellI), y);
        CheckInt("000000001BD16135", IK62(), IM62(y), IS62(cellL, cellI), y);
        CheckInt("000000002468A000", IK63(), IM63(y), IS63(cellL, cellI), y);
        CheckInt("000000002468A4F0", IK64(), IM64(y), IS64(cellL, cellI), y);
        CheckInt("000000002468ACF0", IK65(), IM65(y), IS65(cellL, cellI), y);
        CheckInt("000000002468B4EF", IK66(), IM66(y), IS66(cellL, cellI), y);
        CheckInt("000000002FFFC000", IK67(), IM67(y), IS67(cellL, cellI), y);
        CheckInt("000000003154A800", IK68(), IM68(y), IS68(cellL, cellI), y);
        CheckInt("000000003154AD39", IK69(), IM69(y), IS69(cellL, cellI), y);
        CheckInt("0000000032FD8338", IK70(), IM70(y), IS70(cellL, cellI), y);
        CheckInt("0000000032FD8800", IK71(), IM71(y), IS71(cellL, cellI), y);
        CheckInt("000000003FFFF800", IK72(), IM72(y), IS72(cellL, cellI), y);
        CheckInt("0000000040000000", IK73(), IM73(y), IS73(cellL, cellI), y);
        CheckInt("0000000048D14000", IK74(), IM74(y), IS74(cellL, cellI), y);
        CheckInt("000000005FFF7800", IK75(), IM75(y), IS75(cellL, cellI), y);
        CheckInt("000000005FFF8000", IK76(), IM76(y), IS76(cellL, cellI), y);
        CheckInt("000000005FFF87FF", IK77(), IM77(y), IS77(cellL, cellI), y);
        CheckInt("0000000075833652", IK78(), IM78(y), IS78(cellL, cellI), y);
        CheckInt("00000000758337FF", IK79(), IM79(y), IS79(cellL, cellI), y);
        CheckInt("000000007FF007FF", IK80(), IM80(y), IS80(cellL, cellI), y);
        CheckInt("000000007FFFE800", IK81(), IM81(y), IS81(cellL, cellI), y);
        CheckInt("000000007FFFF000", IK82(), IM82(y), IS82(cellL, cellI), y);
        CheckInt("000000007FFFF7FF", IK83(), IM83(y), IS83(cellL, cellI), y);
        CheckInt("000000007FFFF800", IK84(), IM84(y), IS84(cellL, cellI), y);
        CheckInt("000000007FFFF801", IK85(), IM85(y), IS85(cellL, cellI), y);
        CheckInt("000000007FFFFFFF", IK86(), IM86(y), IS86(cellL, cellI), y);
        CheckInt("0000000080000000", IK87(), IM87(y), IS87(cellL, cellI), y);
        CheckInt("0000000080000001", IK88(), IM88(y), IS88(cellL, cellI), y);
        CheckInt("00000000800007FF", IK89(), IM89(y), IS89(cellL, cellI), y);
        CheckInt("0000000080000800", IK90(), IM90(y), IS90(cellL, cellI), y);
        CheckInt("0000000087654321", IK91(), IM91(y), IS91(cellL, cellI), y);
        CheckInt("0000000091A28000", IK92(), IM92(y), IS92(cellL, cellI), y);
        CheckInt("00000000BFFEFFF8", IK93(), IM93(y), IS93(cellL, cellI), y);
        CheckInt("00000000BFFF0000", IK94(), IM94(y), IS94(cellL, cellI), y);
        CheckInt("00000000C0000800", IK95(), IM95(y), IS95(cellL, cellI), y);
        CheckInt("00000000FDB6C553", IK96(), IM96(y), IS96(cellL, cellI), y);
        CheckInt("00000000FDB6C800", IK97(), IM97(y), IS97(cellL, cellI), y);
        CheckInt("00000000FFE007FE", IK98(), IM98(y), IS98(cellL, cellI), y);
        CheckInt("00000000FFE00FFE", IK99(), IM99(y), IS99(cellL, cellI), y);
        CheckInt("00000000FFE017FD", IK100(), IM100(y), IS100(cellL, cellI), y);
    }

    static void Part7(long x, int y)
    {
        CheckInt("00000000FFFFD800", IK101(), IM101(y), IS101(cellL, cellI), y);
        CheckInt("00000000FFFFE000", IK102(), IM102(y), IS102(cellL, cellI), y);
        CheckInt("00000000FFFFE7FE", IK103(), IM103(y), IS103(cellL, cellI), y);
        CheckInt("00000000FFFFE7FF", IK104(), IM104(y), IS104(cellL, cellI), y);
        CheckInt("00000000FFFFE800", IK105(), IM105(y), IS105(cellL, cellI), y);
        CheckInt("00000000FFFFE802", IK106(), IM106(y), IS106(cellL, cellI), y);
        CheckInt("00000000FFFFEFFE", IK107(), IM107(y), IS107(cellL, cellI), y);
        CheckInt("00000000FFFFF000", IK108(), IM108(y), IS108(cellL, cellI), y);
        CheckInt("00000000FFFFF002", IK109(), IM109(y), IS109(cellL, cellI), y);
        CheckInt("00000000FFFFF7FD", IK110(), IM110(y), IS110(cellL, cellI), y);
        CheckInt("00000000FFFFF7FE", IK111(), IM111(y), IS111(cellL, cellI), y);
        CheckInt("00000000FFFFF7FF", IK112(), IM112(y), IS112(cellL, cellI), y);
        CheckInt("00000000FFFFF800", IK113(), IM113(y), IS113(cellL, cellI), y);
        CheckInt("00000000FFFFF801", IK114(), IM114(y), IS114(cellL, cellI), y);
        CheckInt("00000000FFFFF802", IK115(), IM115(y), IS115(cellL, cellI), y);
        CheckInt("00000000FFFFFFFE", IK116(), IM116(y), IS116(cellL, cellI), y);
        CheckInt("00000000FFFFFFFF", IK117(), IM117(y), IS117(cellL, cellI), y);
        CheckInt("FFFFFFFF80000000", IK118(), IM118(y), IS118(cellL, cellI), y);
        CheckInt("FFFFFFFF80000001", IK119(), IM119(y), IS119(cellL, cellI), y);
        CheckInt("FFFFFFFF800007FE", IK120(), IM120(y), IS120(cellL, cellI), y);
        CheckInt("FFFFFFFF800007FF", IK121(), IM121(y), IS121(cellL, cellI), y);
        CheckInt("FFFFFFFF80000800", IK122(), IM122(y), IS122(cellL, cellI), y);
        CheckInt("FFFFFFFF80000801", IK123(), IM123(y), IS123(cellL, cellI), y);
        CheckInt("FFFFFFFF80000FFF", IK124(), IM124(y), IS124(cellL, cellI), y);
        CheckInt("FFFFFFFF80001000", IK125(), IM125(y), IS125(cellL, cellI), y);
        CheckInt("FFFFFFFF800FF800", IK126(), IM126(y), IS126(cellL, cellI), y);
        CheckInt("FFFFFFFF800FF801", IK127(), IM127(y), IS127(cellL, cellI), y);
        CheckInt("FFFFFFFF87654321", IK128(), IM128(y), IS128(cellL, cellI), y);
        CheckInt("FFFFFFFF8C5C0507", IK129(), IM129(y), IS129(cellL, cellI), y);
        CheckInt("FFFFFFFF8C5C07FF", IK130(), IM130(y), IS130(cellL, cellI), y);
        CheckInt("FFFFFFFF94AA5FFB", IK131(), IM131(y), IS131(cellL, cellI), y);
        CheckInt("FFFFFFFF94AA5FFF", IK132(), IM132(y), IS132(cellL, cellI), y);
        CheckInt("FFFFFFFF9E632CE3", IK133(), IM133(y), IS133(cellL, cellI), y);
        CheckInt("FFFFFFFF9E632FFF", IK134(), IM134(y), IS134(cellL, cellI), y);
        CheckInt("FFFFFFFFA838D000", IK135(), IM135(y), IS135(cellL, cellI), y);
        CheckInt("FFFFFFFFA838DC54", IK136(), IM136(y), IS136(cellL, cellI), y);
        CheckInt("FFFFFFFFBFFEFFF8", IK137(), IM137(y), IS137(cellL, cellI), y);
        CheckInt("FFFFFFFFBFFF0000", IK138(), IM138(y), IS138(cellL, cellI), y);
        CheckInt("FFFFFFFFC00007FF", IK139(), IM139(y), IS139(cellL, cellI), y);
        CheckInt("FFFFFFFFC0000800", IK140(), IM140(y), IS140(cellL, cellI), y);
        CheckInt("FFFFFFFFD0003FFF", IK141(), IM141(y), IS141(cellL, cellI), y);
        CheckInt("FFFFFFFFD0004000", IK142(), IM142(y), IS142(cellL, cellI), y);
        CheckInt("FFFFFFFFED5F23AD", IK143(), IM143(y), IS143(cellL, cellI), y);
        CheckInt("FFFFFFFFED5F2FFF", IK144(), IM144(y), IS144(cellL, cellI), y);
        CheckInt("FFFFFFFFEDCB0000", IK145(), IM145(y), IS145(cellL, cellI), y);
        CheckInt("FFFFFFFFEDCBA987", IK146(), IM146(y), IS146(cellL, cellI), y);
        CheckInt("FFFFFFFFEDCBA988", IK147(), IM147(y), IS147(cellL, cellI), y);
        CheckInt("FFFFFFFFEDD2F7FF", IK148(), IM148(y), IS148(cellL, cellI), y);
        CheckInt("FFFFFFFFEDD2FA80", IK149(), IM149(y), IS149(cellL, cellI), y);
        CheckInt("FFFFFFFFF0000000", IK150(), IM150(y), IS150(cellL, cellI), y);
        CheckInt("FFFFFFFFF6E58000", IK151(), IM151(y), IS151(cellL, cellI), y);
        CheckInt("FFFFFFFFFB72C000", IK152(), IM152(y), IS152(cellL, cellI), y);
        CheckInt("FFFFFFFFFDB96000", IK153(), IM153(y), IS153(cellL, cellI), y);
        CheckInt("FFFFFFFFFEDCB000", IK154(), IM154(y), IS154(cellL, cellI), y);
        CheckInt("FFFFFFFFFF000000", IK155(), IM155(y), IS155(cellL, cellI), y);
        CheckInt("FFFFFFFFFFF00000", IK156(), IM156(y), IS156(cellL, cellI), y);
        CheckInt("FFFFFFFFFFF43800", IK157(), IM157(y), IS157(cellL, cellI), y);
        CheckInt("FFFFFFFFFFF439C7", IK158(), IM158(y), IS158(cellL, cellI), y);
        CheckInt("FFFFFFFFFFF626EE", IK159(), IM159(y), IS159(cellL, cellI), y);
        CheckInt("FFFFFFFFFFF62FFF", IK160(), IM160(y), IS160(cellL, cellI), y);
        CheckInt("FFFFFFFFFFF81000", IK161(), IM161(y), IS161(cellL, cellI), y);
        CheckInt("FFFFFFFFFFF81812", IK162(), IM162(y), IS162(cellL, cellI), y);
        CheckInt("FFFFFFFFFFFF0000", IK163(), IM163(y), IS163(cellL, cellI), y);
        CheckInt("FFFFFFFFFFFF8000", IK164(), IM164(y), IS164(cellL, cellI), y);
        CheckInt("FFFFFFFFFFFFC000", IK165(), IM165(y), IS165(cellL, cellI), y);
        CheckInt("FFFFFFFFFFFFE000", IK166(), IM166(y), IS166(cellL, cellI), y);
        CheckInt("FFFFFFFFFFFFE7FF", IK167(), IM167(y), IS167(cellL, cellI), y);
        CheckInt("FFFFFFFFFFFFE800", IK168(), IM168(y), IS168(cellL, cellI), y);
        CheckInt("FFFFFFFFFFFFE801", IK169(), IM169(y), IS169(cellL, cellI), y);
        CheckInt("FFFFFFFFFFFFEFFF", IK170(), IM170(y), IS170(cellL, cellI), y);
        CheckInt("FFFFFFFFFFFFF000", IK171(), IM171(y), IS171(cellL, cellI), y);
        CheckInt("FFFFFFFFFFFFF001", IK172(), IM172(y), IS172(cellL, cellI), y);
        CheckInt("FFFFFFFFFFFFF1C8", IK173(), IM173(y), IS173(cellL, cellI), y);
        CheckInt("FFFFFFFFFFFFF7FE", IK174(), IM174(y), IS174(cellL, cellI), y);
        CheckInt("FFFFFFFFFFFFF7FF", IK175(), IM175(y), IS175(cellL, cellI), y);
        CheckInt("FFFFFFFFFFFFF800", IK176(), IM176(y), IS176(cellL, cellI), y);
        CheckInt("FFFFFFFFFFFFF801", IK177(), IM177(y), IS177(cellL, cellI), y);
        CheckInt("FFFFFFFFFFFFF802", IK178(), IM178(y), IS178(cellL, cellI), y);
        CheckInt("FFFFFFFFFFFFFCAB", IK179(), IM179(y), IS179(cellL, cellI), y);
        CheckInt("FFFFFFFFFFFFFF71", IK180(), IM180(y), IS180(cellL, cellI), y);
        CheckInt("FFFFFFFFFFFFFFFE", IK181(), IM181(y), IS181(cellL, cellI), y);
        CheckInt("FFFFFFFFFFFFFFFF", IK182(), IM182(y), IS182(cellL, cellI), y);
    }
}
