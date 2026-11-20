// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Moves of a constant number of bytes between ranges that may overlap, and
// integer Math.Min / Math.Max.
//
// A move whose length the JIT knows is expanded in place: every byte of the
// source is loaded into temporary registers, then all of them are stored, so
// the ranges may overlap either way. A length that is not a multiple of
// eight uses a last load and store that overlap the one before. The
// temporaries are internal registers of the node; the two addresses stay in
// use until the last store. What can go wrong:
//
//   - a temporary in the register of one of the addresses: the loads destroy
//     the address the stores need (the registers were once kept apart by
//     delaying the release of the temporaries; that was dropped because it
//     trips an assertion of a Checked JIT, on the grounds that the allocator
//     already keeps them apart);
//   - a store before the last load, which is wrong when the ranges overlap;
//   - the tail of a length that is not a multiple of eight; lengths under
//     eight, which use two overlapping narrow accesses;
//   - the same for a struct assignment of a constant size.
//
// Math.Min and Math.Max on integers have one instruction each with Zbb, which
// works on whole registers; the result of the 32-bit forms is still an INT,
// which the code after it extends or wraps. ZisK guests are built with Zbb;
// on the other targets these are branches.
//
// One generic method per length is compiled, with the length taken from the
// size of a struct; it moves inside one buffer at every distance from -9 to
// 9 and at distances larger than the length. The expected bytes come from a
// loop over a copy of the buffer, compiled without optimization.
//
// Each case prints one line on failure; the run ends with ALL PASS or a FAIL
// count, and the guest returns the number of failures.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Size = 1, Pack = 1)] struct N1 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 2, Pack = 1)] struct N2 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 3, Pack = 1)] struct N3 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 4, Pack = 1)] struct N4 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 5, Pack = 1)] struct N5 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 6, Pack = 1)] struct N6 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 7, Pack = 1)] struct N7 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 8, Pack = 1)] struct N8 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 9, Pack = 1)] struct N9 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 10, Pack = 1)] struct N10 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 11, Pack = 1)] struct N11 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 12, Pack = 1)] struct N12 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 13, Pack = 1)] struct N13 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 14, Pack = 1)] struct N14 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 15, Pack = 1)] struct N15 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 16, Pack = 1)] struct N16 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 17, Pack = 1)] struct N17 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 18, Pack = 1)] struct N18 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 19, Pack = 1)] struct N19 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 20, Pack = 1)] struct N20 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 21, Pack = 1)] struct N21 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 22, Pack = 1)] struct N22 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 23, Pack = 1)] struct N23 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 24, Pack = 1)] struct N24 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 25, Pack = 1)] struct N25 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 26, Pack = 1)] struct N26 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 27, Pack = 1)] struct N27 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 28, Pack = 1)] struct N28 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 29, Pack = 1)] struct N29 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 30, Pack = 1)] struct N30 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 31, Pack = 1)] struct N31 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 32, Pack = 1)] struct N32 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 33, Pack = 1)] struct N33 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 34, Pack = 1)] struct N34 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 35, Pack = 1)] struct N35 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 36, Pack = 1)] struct N36 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 37, Pack = 1)] struct N37 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 38, Pack = 1)] struct N38 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 39, Pack = 1)] struct N39 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 40, Pack = 1)] struct N40 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 47, Pack = 1)] struct N47 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 48, Pack = 1)] struct N48 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 49, Pack = 1)] struct N49 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 56, Pack = 1)] struct N56 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 63, Pack = 1)] struct N63 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 64, Pack = 1)] struct N64 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 65, Pack = 1)] struct N65 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 72, Pack = 1)] struct N72 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 100, Pack = 1)] struct N100 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 128, Pack = 1)] struct N128 { public byte B; }

static unsafe class Program
{
    const MethodImplOptions NoInl = MethodImplOptions.NoInlining;
    static int fails;

    static void Check(string name, long expected, long got)
    {
        if (expected != got)
        {
            fails++;
            Console.WriteLine("FAIL " + name + " expected " + expected + " got " + got);
        }
    }

    [MethodImpl(NoInl)] static int Opaque(int x) => x;

    // ---- the moves --------------------------------------------------------------
    // Span.CopyTo is a move: the ranges may overlap.
    [MethodImpl(NoInl)]
    static void Move<T>(byte[] buf, int dst, int src) where T : unmanaged =>
        new Span<byte>(buf, src, sizeof(T)).CopyTo(new Span<byte>(buf, dst, sizeof(T)));

    // The addresses are computed just before the move and die in it.
    [MethodImpl(NoInl)]
    static void MoveRaw<T>(byte* p, long dst, long src) where T : unmanaged =>
        Buffer.MemoryCopy(p + src, p + dst, sizeof(T), sizeof(T));

    // Other values are alive around the move and used after it.
    [MethodImpl(NoInl)]
    static long MoveBusy<T>(byte[] buf, int dst, int src, long a, long b, long c, long d) where T : unmanaged
    {
        long e = a * 3 + b, f = c * 5 + d, g = a ^ d, h = b - c;
        new Span<byte>(buf, src, sizeof(T)).CopyTo(new Span<byte>(buf, dst, sizeof(T)));
        long r = e + f * 7 + g * 11 + h * 13 + buf[dst] + a + b + c + d;
        new Span<byte>(buf, dst, sizeof(T)).CopyTo(new Span<byte>(buf, src, sizeof(T)));
        return r + e + f + g + h;
    }

    // A struct assignment: the ranges do not overlap.
    [MethodImpl(NoInl)]
    static void Assign<T>(byte* p, long dst, long src) where T : unmanaged => *(T*)(p + dst) = *(T*)(p + src);

    [MethodImpl(NoInl)]
    static T Load<T>(byte* p, long src) where T : unmanaged => *(T*)(p + src);

    [MethodImpl(NoInl | MethodImplOptions.NoOptimization)]
    static void Model(byte[] buf, int dst, int src, int n)
    {
        byte[] t = new byte[n];
        for (int i = 0; i < n; i++) t[i] = buf[src + i];
        for (int i = 0; i < n; i++) buf[dst + i] = t[i];
    }

    const int Len = 512, Base = 200;

    static void Reset(byte[] a, int seed)
    {
        for (int i = 0; i < a.Length; i++) a[i] = (byte)(i * 7 + seed + (i >> 5));
    }

    static long Diff(byte[] a, byte[] b)
    {
        long n = 0;
        for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) n++;
        return n;
    }

    static void Size<T>() where T : unmanaged
    {
        int n = sizeof(T);
        byte[] got = new byte[Len], exp = new byte[Len];
        long bad = 0, badRaw = 0, badBusy = 0, badAssign = 0;
        int[] far = { -(n + 1), n + 1, -(n + 8), n + 8, -n, n, -150, 150 };
        for (int k = -9; k <= 9 + far.Length; k++)
        {
            int dist = k <= 9 ? k : far[k - 10];
            for (int align = 0; align < 8; align++)
            {
                int src = Base + align, dst = src + dist;
                Reset(got, n + k); Reset(exp, n + k);
                Move<T>(got, dst, src);
                Model(exp, dst, src, n);
                bad += Diff(got, exp);

                Reset(got, n - k); Reset(exp, n - k);
                fixed (byte* p = got) MoveRaw<T>(p, dst, src);
                Model(exp, dst, src, n);
                badRaw += Diff(got, exp);

                Reset(got, k); Reset(exp, k);
                long a = n * 1000003L + k, b = ~a, c = a << 20, d = a ^ 0x5555;
                long e = a * 3 + b, f = c * 5 + d, g = a ^ d, h = b - c;
                long r = MoveBusy<T>(got, dst, src, a, b, c, d);
                Model(exp, dst, src, n);
                long er = e + f * 7 + g * 11 + h * 13 + exp[dst] + a + b + c + d + e + f + g + h;
                Model(exp, src, dst, n);
                badBusy += Diff(got, exp) + (r == er ? 0 : 1);
            }
        }
        for (int align = 0; align < 8; align++)
        {
            Reset(got, n); Reset(exp, n);
            fixed (byte* p = got)
            {
                Assign<T>(p, Base + 160 + align, Base + align);
                T v = Load<T>(p, Base + 3 + align);
                Assign<T>(p, 8 + align, Base + 160 + align);
                *(T*)(p + 330 + align) = v;
            }
            Model(exp, Base + 160 + align, Base + align, n);
            byte[] t = new byte[n];
            for (int i = 0; i < n; i++) t[i] = exp[Base + 3 + align + i];
            Model(exp, 8 + align, Base + 160 + align, n);
            for (int i = 0; i < n; i++) exp[330 + align + i] = t[i];
            badAssign += Diff(got, exp);
        }
        Check("move of " + n + " bytes", 0, bad);
        Check("raw move of " + n + " bytes", 0, badRaw);
        Check("move of " + n + " bytes with values alive", 0, badBusy);
        Check("assignment of " + n + " bytes", 0, badAssign);
    }

    // ---- Math.Min and Math.Max ---------------------------------------------------------
    [MethodImpl(NoInl)] static int MinI(int a, int b) => Math.Min(a, b);
    [MethodImpl(NoInl)] static int MaxI(int a, int b) => Math.Max(a, b);
    [MethodImpl(NoInl)] static uint MinU(uint a, uint b) => Math.Min(a, b);
    [MethodImpl(NoInl)] static uint MaxU(uint a, uint b) => Math.Max(a, b);
    [MethodImpl(NoInl)] static long MinL(long a, long b) => Math.Min(a, b);
    [MethodImpl(NoInl)] static long MaxL(long a, long b) => Math.Max(a, b);
    [MethodImpl(NoInl)] static ulong MinUL(ulong a, ulong b) => Math.Min(a, b);
    [MethodImpl(NoInl)] static ulong MaxUL(ulong a, ulong b) => Math.Max(a, b);
    // An operand that is widened inside the call.
    [MethodImpl(NoInl)] static long MinWidened(int a, long b) => Math.Min(a, b);
    [MethodImpl(NoInl)] static long MaxWidened(long a, int b) => Math.Max(a, b);
    [MethodImpl(NoInl)] static ulong MinWidenedU(uint a, ulong b) => Math.Min(a, b);
    [MethodImpl(NoInl)] static ulong MaxWidenedU(ulong a, uint b) => Math.Max(a, b);
    [MethodImpl(NoInl)] static int Clamp(int x, int lo, int hi) => Math.Max(lo, Math.Min(x, hi));
    [MethodImpl(NoInl)] static long MinOfLoads(int[] a, int i, int j) => Math.Min(a[i], a[j]) + (long)Math.Max((uint)a[i], (uint)a[j]);
    [MethodImpl(NoInl)] static int MinConst(int a) => Math.Min(a, 100) + Math.Max(a, -100);
    // The result is an INT whatever the width of the instruction: what follows extends it or wraps at
    // 32 bits.
    [MethodImpl(NoInl)] static uint MaxShifted(uint a, uint b) => Math.Max(a, b) >> 4;
    [MethodImpl(NoInl)] static bool MaxPlusOneIsNegative(int a, int b) => Math.Max(a, b) + 1 < 0;
    [MethodImpl(NoInl)] static ulong MinWidenedAfter(uint a, uint b) => (ulong)Math.Min(a, b) * 3;
    [MethodImpl(NoInl)] static long MaxAdded(uint a, uint b, long c) => c + Math.Max(a, b);
    [MethodImpl(NoInl)] static nint MinN(nint a, nint b) => Math.Min(a, b);
    [MethodImpl(NoInl)] static byte MinB(byte a, byte b) => Math.Min(a, b);
    [MethodImpl(NoInl)] static short MaxS(short a, short b) => Math.Max(a, b);

    static void CaseMinMax()
    {
        long[] edges = { 0, 1, -1, 2, 100, -100, 101, -101, int.MaxValue, int.MinValue, 0x80000000L, 0xFFFFFFFFL, 0x100000000L, long.MaxValue, long.MinValue, -0x80000001L };
        int[] ints = new int[edges.Length];
        for (int i = 0; i < edges.Length; i++) ints[i] = (int)edges[i];
        long bad = 0;
        for (int i = 0; i < edges.Length; i++)
            for (int j = 0; j < edges.Length; j++)
            {
                long a = edges[i], b = edges[j];
                int ia = (int)a, ib = (int)b;
                uint ua = (uint)a, ub = (uint)b;
                ulong la = (ulong)a, lb = (ulong)b;
                if (MinI(ia, ib) != (ia < ib ? ia : ib)) bad++;
                if (MaxI(ia, ib) != (ia > ib ? ia : ib)) bad++;
                if ((ulong)MinU(ua, ub) != (ua < ub ? ua : ub)) bad++;
                if ((ulong)MaxU(ua, ub) != (ua > ub ? ua : ub)) bad++;
                if (MinL(a, b) != (a < b ? a : b)) bad++;
                if (MaxL(a, b) != (a > b ? a : b)) bad++;
                if (MinUL(la, lb) != (la < lb ? la : lb)) bad++;
                if (MaxUL(la, lb) != (la > lb ? la : lb)) bad++;
                if (MinWidened(ia, b) != (ia < b ? ia : b)) bad++;
                if (MaxWidened(a, ib) != (a > ib ? a : ib)) bad++;
                if (MinWidenedU(ua, lb) != (ua < lb ? ua : lb)) bad++;
                if (MaxWidenedU(la, ub) != (la > ub ? la : ub)) bad++;
                if (MinN((nint)a, (nint)b) != ((nint)a < (nint)b ? (nint)a : (nint)b)) bad++;
                if (MinB((byte)a, (byte)b) != ((byte)a < (byte)b ? (byte)a : (byte)b)) bad++;
                if (MaxS((short)a, (short)b) != ((short)a > (short)b ? (short)a : (short)b)) bad++;
                long ml = (ia < ib ? ia : ib) + (long)(ua > ub ? ua : ub);
                if (MinOfLoads(ints, i, j) != ml) bad++;
                if (MaxShifted(ua, ub) != (ua > ub ? ua : ub) >> 4) bad++;
                if (MaxPlusOneIsNegative(ia, ib) != (unchecked((ia > ib ? ia : ib) + 1) < 0)) bad++;
                if (MinWidenedAfter(ua, ub) != (ulong)(ua < ub ? ua : ub) * 3) bad++;
                if (MaxAdded(ua, ub, a) != a + (long)(ua > ub ? ua : ub)) bad++;
                int lo = ia < ib ? ia : ib, hi = ia < ib ? ib : ia;
                for (int k = 0; k < edges.Length; k += 3)
                {
                    int x = (int)edges[k];
                    if (Clamp(x, lo, hi) != (x < lo ? lo : (x > hi ? hi : x))) bad++;
                }
            }
        for (int i = 0; i < edges.Length; i++)
        {
            int a = (int)edges[i];
            if (MinConst(a) != unchecked((a < 100 ? a : 100) + (a > -100 ? a : -100))) bad++;
        }
        Check("min and max", 0, bad);
    }

    static int Main()
    {
        Size<N1>();
        Size<N2>();
        Size<N3>();
        Size<N4>();
        Size<N5>();
        Size<N6>();
        Size<N7>();
        Size<N8>();
        Size<N9>();
        Size<N10>();
        Size<N11>();
        Size<N12>();
        Size<N13>();
        Size<N14>();
        Size<N15>();
        Size<N16>();
        Size<N17>();
        Size<N18>();
        Size<N19>();
        Size<N20>();
        Size<N21>();
        Size<N22>();
        Size<N23>();
        Size<N24>();
        Size<N25>();
        Size<N26>();
        Size<N27>();
        Size<N28>();
        Size<N29>();
        Size<N30>();
        Size<N31>();
        Size<N32>();
        Size<N33>();
        Size<N34>();
        Size<N35>();
        Size<N36>();
        Size<N37>();
        Size<N38>();
        Size<N39>();
        Size<N40>();
        Size<N47>();
        Size<N48>();
        Size<N49>();
        Size<N56>();
        Size<N63>();
        Size<N64>();
        Size<N65>();
        Size<N72>();
        Size<N100>();
        Size<N128>();
        CaseMinMax();

        Console.WriteLine(fails == 0 ? "ALL PASS" : "FAILED " + fails);
        return fails;
    }
}
