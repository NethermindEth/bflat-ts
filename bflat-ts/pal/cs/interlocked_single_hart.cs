// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Interlocked, Volatile and locks on a target with one hart, no atomic
// instructions and no memory barriers.
//
// A zkVM guest is built without the A extension, so the JIT cannot emit
// amoadd, amoswap or lr/sc. It lowers an exchange to a load and a store, an
// add (and, or) to load, operate, store, and a compare-exchange to load,
// branch, store. It also emits no `fence` for volatile accesses and memory
// barriers. With one hart this is all the ordering there is; what is left to
// get wrong is the plain sequence itself:
//
//   - the registers: the sequence uses the address and the value after the
//     result is written, so the result must not share a register with
//     either, nor with the comparand; the add needs a scratch register; a
//     result that is not used has no register at all;
//   - the width: a 32-bit location is loaded sign-extended and compared
//     with the whole comparand register, so a comparand that is zero-extended
//     or has stale upper bits (an unsigned value, a narrowed long, a value
//     loaded with a zero-extending load) fails to match when bit 31 is set;
//     the old value handed back must be extended as its type says;
//   - the result of an add is the new value, of an exchange, and and or the
//     old one; at the ends of the range the add wraps;
//   - a value and an address that are the same, a location that is its own
//     comparand;
//   - volatile reads and writes of every width, with the right extension,
//     and the check of a class with a static constructor, which is a
//     volatile read.
//
// Every operation is in its own method that is not inlined, and is also
// used inside loops with many values alive. The expected values come from
// plain arithmetic on a copy of the location kept by the test.
//
// One hart only: nothing here can test atomicity.
//
// Each case prints one line on failure; the run ends with ALL PASS or a FAIL
// count, and the guest returns the number of failures.
using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;

class Cell
{
    public long G0 = 0x1111111111111111;
    public int I;
    public int GI = 0x22222222;
    public uint U;
    public uint GU = 0x33333333;
    public long L;
    public long G1 = 0x4444444444444444;
    public ulong UL;
    public long G2 = 0x5555555555555555;
    public object O;
    public long G3 = 0x6666666666666666;
    public byte B; public byte GB = 0x77;
    public sbyte SB; public sbyte GSB = 0x78;
    public short S; public short GS = 0x7979;
    public ushort US; public ushort GUS = 0x7A7A;
    public nint N;
    public long G4 = 0x7B7B7B7B7B7B7B7B;
    public volatile int VI;
    public volatile uint VU;
    public volatile byte VB;
    public volatile sbyte VSB;
    public volatile short VS;
    public volatile ushort VUS;
    public volatile bool VBool;
    public volatile object VO;
    public long G5 = 0x7C7C7C7C7C7C7C7C;

    public long Guards() =>
        (G0 == 0x1111111111111111 ? 0 : 1) + (GI == 0x22222222 ? 0 : 2) + (GU == 0x33333333 ? 0 : 4) + (G1 == 0x4444444444444444 ? 0 : 8) +
        (G2 == 0x5555555555555555 ? 0 : 16) + (G3 == 0x6666666666666666 ? 0 : 32) + (GB == 0x77 ? 0 : 64) + (GSB == 0x78 ? 0 : 128) +
        (GS == 0x7979 ? 0 : 256) + (GUS == 0x7A7A ? 0 : 512) + (G4 == 0x7B7B7B7B7B7B7B7B ? 0 : 1024) + (G5 == 0x7C7C7C7C7C7C7C7C ? 0 : 2048);
}

class WithCctor
{
    public static int Count;
    public static volatile int Shared;
    public static readonly long Stamp;
    static WithCctor() { Count++; Stamp = 0x0123456789ABCDEF; }
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Read(int i) => Stamp + i + Shared;
}

class Link { public Link Next; public int V; }

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
    [MethodImpl(NoInl)] static long OpaqueL(long x) => x;

    // ---- compare-exchange --------------------------------------------------------
    [MethodImpl(NoInl)] static int Cas(ref int loc, int v, int c) => Interlocked.CompareExchange(ref loc, v, c);
    [MethodImpl(NoInl)] static uint Cas(ref uint loc, uint v, uint c) => Interlocked.CompareExchange(ref loc, v, c);
    [MethodImpl(NoInl)] static long Cas(ref long loc, long v, long c) => Interlocked.CompareExchange(ref loc, v, c);
    [MethodImpl(NoInl)] static ulong Cas(ref ulong loc, ulong v, ulong c) => Interlocked.CompareExchange(ref loc, v, c);
    [MethodImpl(NoInl)] static nint Cas(ref nint loc, nint v, nint c) => Interlocked.CompareExchange(ref loc, v, c);
    [MethodImpl(NoInl)] static object Cas(ref object loc, object v, object c) => Interlocked.CompareExchange(ref loc, v, c);
    [MethodImpl(NoInl)] static Link CasT(ref Link loc, Link v, Link c) => Interlocked.CompareExchange(ref loc, v, c);
    [MethodImpl(NoInl)] static byte Cas(ref byte loc, byte v, byte c) => Interlocked.CompareExchange(ref loc, v, c);
    [MethodImpl(NoInl)] static sbyte Cas(ref sbyte loc, sbyte v, sbyte c) => Interlocked.CompareExchange(ref loc, v, c);
    [MethodImpl(NoInl)] static short Cas(ref short loc, short v, short c) => Interlocked.CompareExchange(ref loc, v, c);
    [MethodImpl(NoInl)] static ushort Cas(ref ushort loc, ushort v, ushort c) => Interlocked.CompareExchange(ref loc, v, c);

    // The comparand in other shapes: each must still be compared as 32 bits.
    [MethodImpl(NoInl)] static int CasNarrowed(ref int loc, int v, long c) => Interlocked.CompareExchange(ref loc, v, (int)c);
    [MethodImpl(NoInl)] static int CasFromUInt(ref int loc, int v, uint c) => Interlocked.CompareExchange(ref loc, v, (int)c);
    [MethodImpl(NoInl)] static int CasFromArray(ref int loc, int v, int[] c, int i) => Interlocked.CompareExchange(ref loc, v, c[i]);
    [MethodImpl(NoInl)] static int CasFromUArray(ref int loc, int v, uint[] c, int i) => Interlocked.CompareExchange(ref loc, v, (int)c[i]);
    [MethodImpl(NoInl)] static int CasSum(ref int loc, int v, int a, int b) => Interlocked.CompareExchange(ref loc, v, a + b);
    [MethodImpl(NoInl)] static int CasShifted(ref int loc, int v, ushort hi) => Interlocked.CompareExchange(ref loc, v, hi << 16);
    [MethodImpl(NoInl)] static int CasSelf(ref int loc, int v) => Interlocked.CompareExchange(ref loc, v, loc);
    [MethodImpl(NoInl)] static uint CasUNarrowed(ref uint loc, uint v, ulong c) => Interlocked.CompareExchange(ref loc, v, (uint)c);
    [MethodImpl(NoInl)] static uint CasUFromArray(ref uint loc, uint v, uint[] c, int i) => Interlocked.CompareExchange(ref loc, v, c[i]);
    [MethodImpl(NoInl)] static uint CasUFromInt(ref uint loc, uint v, int c) => Interlocked.CompareExchange(ref loc, v, (uint)c);
    [MethodImpl(NoInl)] static uint CasUSum(ref uint loc, uint v, uint a, uint b) => Interlocked.CompareExchange(ref loc, v, a + b);
    [MethodImpl(NoInl)] static uint CasUShifted(ref uint loc, uint v, ulong c) => Interlocked.CompareExchange(ref loc, v, (uint)(c >> 32));
    [MethodImpl(NoInl)] static bool CasMatched(ref int loc, int v, int c) => Interlocked.CompareExchange(ref loc, v, c) == c;
    [MethodImpl(NoInl)] static bool CasUMatched(ref uint loc, uint v, uint c) => Interlocked.CompareExchange(ref loc, v, c) == c;
    [MethodImpl(NoInl)] static int CasConst0(ref int loc, int v) => Interlocked.CompareExchange(ref loc, v, 0);
    [MethodImpl(NoInl)] static int CasConstMin(ref int loc) => Interlocked.CompareExchange(ref loc, 0x7FFFFFFF, int.MinValue);
    [MethodImpl(NoInl)] static uint CasConstTop(ref uint loc) => Interlocked.CompareExchange(ref loc, 1u, 0x80000000u);
    [MethodImpl(NoInl)] static void CasUnused(ref int loc, int v, int c) { Interlocked.CompareExchange(ref loc, v, c); }
    [MethodImpl(NoInl)] static void CasUnusedL(ref long loc, long v, long c) { Interlocked.CompareExchange(ref loc, v, c); }

    static readonly int[] IntEdges = { 0, 1, -1, 2, 0x7FFFFFFF, int.MinValue, int.MinValue + 1, 0x12345678, unchecked((int)0x87654321), 0x00008000, unchecked((int)0xFFFF8000), 0x40000000, -2 };
    static readonly long[] LongEdges = { 0, 1, -1, int.MaxValue, int.MinValue, 0x80000000L, 0xFFFFFFFFL, 0x100000000L, long.MaxValue, long.MinValue, 0x123456789ABCDEF0, unchecked((long)0xFEDCBA9876543210) };

    static void CaseCompareExchangeInt()
    {
        var cell = new Cell();
        long bad = 0;
        int n = IntEdges.Length;
        int[] asInts = IntEdges;
        uint[] asUInts = new uint[n];
        for (int i = 0; i < n; i++) asUInts[i] = (uint)IntEdges[i];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                for (int k = 0; k < n; k++)
                {
                    int start = IntEdges[i], v = IntEdges[j], c = IntEdges[k];
                    int after = start == c ? v : start;

                    cell.I = start;
                    if (Cas(ref cell.I, v, c) != start || cell.I != after) bad++;
                    cell.I = start;
                    if (CasMatched(ref cell.I, v, c) != (start == c) || cell.I != after) bad++;
                    cell.I = start;
                    CasUnused(ref cell.I, v, c);
                    if (cell.I != after) bad++;
                    // Stale bits above bit 31 of the comparand.
                    cell.I = start;
                    if (CasNarrowed(ref cell.I, v, 0x5A5A5A5A00000000L | (uint)c) != start || cell.I != after) bad++;
                    cell.I = start;
                    if (CasFromUInt(ref cell.I, v, (uint)c) != start || cell.I != after) bad++;
                    cell.I = start;
                    if (CasFromArray(ref cell.I, v, asInts, k) != start || cell.I != after) bad++;
                    cell.I = start;
                    if (CasFromUArray(ref cell.I, v, asUInts, k) != start || cell.I != after) bad++;
                    cell.I = start;
                    if (CasSum(ref cell.I, v, c - 0x7FFFFFF0, 0x7FFFFFF0) != start || cell.I != after) bad++;

                    uint us = (uint)start, uv = (uint)v, uc = (uint)c;
                    uint uafter = us == uc ? uv : us;
                    cell.U = us;
                    ulong old = Cas(ref cell.U, uv, uc);
                    if (old != us || cell.U != uafter) bad++;
                    cell.U = us;
                    if (CasUMatched(ref cell.U, uv, uc) != (us == uc) || cell.U != uafter) bad++;
                    cell.U = us;
                    old = CasUNarrowed(ref cell.U, uv, 0xA5A5A5A500000000UL | uc);
                    if (old != us || cell.U != uafter) bad++;
                    cell.U = us;
                    old = CasUFromArray(ref cell.U, uv, asUInts, k);
                    if (old != us || cell.U != uafter) bad++;
                    cell.U = us;
                    old = CasUFromInt(ref cell.U, uv, c);
                    if (old != us || cell.U != uafter) bad++;
                    cell.U = us;
                    old = CasUSum(ref cell.U, uv, uc - 0xFFFFFFF0u, 0xFFFFFFF0u);
                    if (old != us || cell.U != uafter) bad++;
                    cell.U = us;
                    old = CasUShifted(ref cell.U, uv, ((ulong)uc << 32) | 0xDEADBEEF);
                    if (old != us || cell.U != uafter) bad++;
                }
        Check("compare-exchange int and uint", 0, bad);

        bad = 0;
        for (int i = 0; i < n; i++)
        {
            int start = IntEdges[i];
            cell.I = start;
            if (CasSelf(ref cell.I, 99) != start || cell.I != 99) bad++;
            cell.I = start;
            if (CasConst0(ref cell.I, 7) != start || cell.I != (start == 0 ? 7 : start)) bad++;
            cell.I = start;
            if (CasConstMin(ref cell.I) != start || cell.I != (start == int.MinValue ? 0x7FFFFFFF : start)) bad++;
            cell.U = (uint)start;
            if (CasConstTop(ref cell.U) != (uint)start || cell.U != ((uint)start == 0x80000000u ? 1u : (uint)start)) bad++;
            ushort hi = (ushort)((uint)start >> 16);
            cell.I = start;
            int exp = start == (hi << 16) ? 5 : start;
            if (CasShifted(ref cell.I, 5, hi) != start || cell.I != exp) bad++;
        }
        Check("compare-exchange int, special comparands", 0, bad);
        Check("compare-exchange int guards", 0, cell.Guards());
    }

    static void CaseCompareExchangeOther()
    {
        var cell = new Cell();
        long bad = 0;
        int n = LongEdges.Length;
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                for (int k = 0; k < n; k++)
                {
                    long start = LongEdges[i], v = LongEdges[j], c = LongEdges[k];
                    long after = start == c ? v : start;
                    cell.L = start;
                    if (Cas(ref cell.L, v, c) != start || cell.L != after) bad++;
                    cell.L = start;
                    CasUnusedL(ref cell.L, v, c);
                    if (cell.L != after) bad++;
                    cell.UL = (ulong)start;
                    if (Cas(ref cell.UL, (ulong)v, (ulong)c) != (ulong)start || cell.UL != (ulong)after) bad++;
                    cell.N = (nint)start;
                    if (Cas(ref cell.N, (nint)v, (nint)c) != (nint)start || cell.N != ((nint)start == (nint)c ? (nint)v : (nint)start)) bad++;
                }
        Check("compare-exchange long, ulong, nint", 0, bad);

        bad = 0;
        for (int start = -3; start < 260; start += 1)
            for (int c = start - 1; c <= start + 1; c++)
            {
                byte bs = (byte)start, bc = (byte)c;
                cell.B = bs;
                if (Cas(ref cell.B, 0xA5, bc) != bs || cell.B != (bs == bc ? 0xA5 : bs)) bad++;
                sbyte ss = (sbyte)start, sc = (sbyte)c;
                cell.SB = ss;
                if (Cas(ref cell.SB, -91, sc) != ss || cell.SB != (ss == sc ? -91 : ss)) bad++;
                short hs = (short)(start * 127), hc = (short)(c * 127);
                cell.S = hs;
                if (Cas(ref cell.S, -23131, hc) != hs || cell.S != (hs == hc ? -23131 : hs)) bad++;
                ushort ws = (ushort)(start * 255), wc = (ushort)(c * 255);
                cell.US = ws;
                if (Cas(ref cell.US, 0xA5A5, wc) != ws || cell.US != (ws == wc ? 0xA5A5 : ws)) bad++;
            }
        Check("compare-exchange small types", 0, bad);
        Check("compare-exchange small types guards", 0, cell.Guards());

        object a = new object(), b = "b";
        Check("compare-exchange object miss", 1, Cas(ref cell.O, a, b) == null && cell.O == null ? 1 : 0);
        Check("compare-exchange object hit", 1, Cas(ref cell.O, a, null) == null && ReferenceEquals(cell.O, a) ? 1 : 0);
        Check("compare-exchange object hit 2", 1, ReferenceEquals(Cas(ref cell.O, b, a), a) && ReferenceEquals(cell.O, b) ? 1 : 0);
        Check("compare-exchange object miss 2", 1, ReferenceEquals(Cas(ref cell.O, null, a), b) && ReferenceEquals(cell.O, b) ? 1 : 0);

        // A stack of links pushed and popped with compare-exchange.
        Link head = null;
        for (int i = 1; i <= 50; i++)
        {
            Link node = new Link { V = i };
            Link seen;
            do { seen = head; node.Next = seen; } while (!ReferenceEquals(CasT(ref head, node, seen), seen));
        }
        long sum = 0;
        int pops = 0;
        while (head != null)
        {
            Link seen = head;
            if (ReferenceEquals(CasT(ref head, seen.Next, seen), seen)) { sum = sum * 3 % 1000003 + seen.V; pops++; }
        }
        long e = 0;
        for (int i = 50; i >= 1; i--) e = e * 3 % 1000003 + i;
        Check("stack of links", e * 100 + 50, sum * 100 + pops);
        Check("compare-exchange other guards", 0, cell.Guards());
    }

    // ---- exchange, add, and, or -------------------------------------------------------
    [MethodImpl(NoInl)] static int Xchg(ref int loc, int v) => Interlocked.Exchange(ref loc, v);
    [MethodImpl(NoInl)] static uint Xchg(ref uint loc, uint v) => Interlocked.Exchange(ref loc, v);
    [MethodImpl(NoInl)] static long Xchg(ref long loc, long v) => Interlocked.Exchange(ref loc, v);
    [MethodImpl(NoInl)] static ulong Xchg(ref ulong loc, ulong v) => Interlocked.Exchange(ref loc, v);
    [MethodImpl(NoInl)] static nint Xchg(ref nint loc, nint v) => Interlocked.Exchange(ref loc, v);
    [MethodImpl(NoInl)] static byte Xchg(ref byte loc, byte v) => Interlocked.Exchange(ref loc, v);
    [MethodImpl(NoInl)] static short Xchg(ref short loc, short v) => Interlocked.Exchange(ref loc, v);
    [MethodImpl(NoInl)] static object Xchg(ref object loc, object v) => Interlocked.Exchange(ref loc, v);
    [MethodImpl(NoInl)] static void XchgUnused(ref int loc, int v) { Interlocked.Exchange(ref loc, v); }
    [MethodImpl(NoInl)] static void XchgUnusedL(ref long loc, long v) { Interlocked.Exchange(ref loc, v); }
    [MethodImpl(NoInl)] static int XchgZero(ref int loc) => Interlocked.Exchange(ref loc, 0);
    [MethodImpl(NoInl)] static long XchgZeroL(ref long loc) => Interlocked.Exchange(ref loc, 0);
    [MethodImpl(NoInl)] static int XchgSelf(ref int loc) => Interlocked.Exchange(ref loc, loc);
    [MethodImpl(NoInl)] static long XchgAddress(long* p) => Interlocked.Exchange(ref *p, (long)p);
    [MethodImpl(NoInl)] static long CasAddress(long* p) => Interlocked.CompareExchange(ref *p, (long)p + 1, (long)p);

    [MethodImpl(NoInl)] static int Add(ref int loc, int v) => Interlocked.Add(ref loc, v);
    [MethodImpl(NoInl)] static uint Add(ref uint loc, uint v) => Interlocked.Add(ref loc, v);
    [MethodImpl(NoInl)] static long Add(ref long loc, long v) => Interlocked.Add(ref loc, v);
    [MethodImpl(NoInl)] static ulong Add(ref ulong loc, ulong v) => Interlocked.Add(ref loc, v);
    [MethodImpl(NoInl)] static int Inc(ref int loc) => Interlocked.Increment(ref loc);
    [MethodImpl(NoInl)] static int Dec(ref int loc) => Interlocked.Decrement(ref loc);
    [MethodImpl(NoInl)] static long Inc(ref long loc) => Interlocked.Increment(ref loc);
    [MethodImpl(NoInl)] static long Dec(ref long loc) => Interlocked.Decrement(ref loc);
    [MethodImpl(NoInl)] static uint Inc(ref uint loc) => Interlocked.Increment(ref loc);
    [MethodImpl(NoInl)] static ulong Dec(ref ulong loc) => Interlocked.Decrement(ref loc);
    [MethodImpl(NoInl)] static void AddUnused(ref int loc, int v) { Interlocked.Add(ref loc, v); }
    [MethodImpl(NoInl)] static void AddUnusedL(ref long loc, long v) { Interlocked.Add(ref loc, v); }
    [MethodImpl(NoInl)] static void IncUnused(ref int loc) { Interlocked.Increment(ref loc); }
    [MethodImpl(NoInl)] static void DecUnusedL(ref long loc) { Interlocked.Decrement(ref loc); }
    [MethodImpl(NoInl)] static int AddSelf(int[] a, int i) => Interlocked.Add(ref a[i], a[i]);
    [MethodImpl(NoInl)] static long AddSelfL(long[] a, int i) => Interlocked.Add(ref a[i], a[i]);
    [MethodImpl(NoInl)] static long AddAddress(long* p) => Interlocked.Add(ref *p, (long)p);

    [MethodImpl(NoInl)] static int And(ref int loc, int v) => Interlocked.And(ref loc, v);
    [MethodImpl(NoInl)] static uint And(ref uint loc, uint v) => Interlocked.And(ref loc, v);
    [MethodImpl(NoInl)] static long And(ref long loc, long v) => Interlocked.And(ref loc, v);
    [MethodImpl(NoInl)] static ulong And(ref ulong loc, ulong v) => Interlocked.And(ref loc, v);
    [MethodImpl(NoInl)] static int Or(ref int loc, int v) => Interlocked.Or(ref loc, v);
    [MethodImpl(NoInl)] static uint Or(ref uint loc, uint v) => Interlocked.Or(ref loc, v);
    [MethodImpl(NoInl)] static long Or(ref long loc, long v) => Interlocked.Or(ref loc, v);
    [MethodImpl(NoInl)] static ulong Or(ref ulong loc, ulong v) => Interlocked.Or(ref loc, v);
    [MethodImpl(NoInl)] static void AndUnused(ref int loc, int v) { Interlocked.And(ref loc, v); }
    [MethodImpl(NoInl)] static void OrUnusedL(ref long loc, long v) { Interlocked.Or(ref loc, v); }
    [MethodImpl(NoInl)] static long ReadLong(ref long loc) => Interlocked.Read(ref loc);

    static void CaseExchangeAndArithmetic()
    {
        var cell = new Cell();
        long bad = 0;
        int n = IntEdges.Length;
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                int s = IntEdges[i], v = IntEdges[j];
                uint us = (uint)s, uv = (uint)v;
                cell.I = s; if (Xchg(ref cell.I, v) != s || cell.I != v) bad++;
                cell.I = s; XchgUnused(ref cell.I, v); if (cell.I != v) bad++;
                cell.U = us; ulong old = Xchg(ref cell.U, uv); if (old != us || cell.U != uv) bad++;
                cell.I = s; if (Add(ref cell.I, v) != unchecked(s + v) || cell.I != unchecked(s + v)) bad++;
                cell.I = s; AddUnused(ref cell.I, v); if (cell.I != unchecked(s + v)) bad++;
                cell.U = us; old = Add(ref cell.U, uv); if (old != unchecked(us + uv) || cell.U != unchecked(us + uv)) bad++;
                cell.I = s; if (And(ref cell.I, v) != s || cell.I != (s & v)) bad++;
                cell.I = s; AndUnused(ref cell.I, v); if (cell.I != (s & v)) bad++;
                cell.I = s; if (Or(ref cell.I, v) != s || cell.I != (s | v)) bad++;
                cell.U = us; old = And(ref cell.U, uv); if (old != us || cell.U != (us & uv)) bad++;
                cell.U = us; old = Or(ref cell.U, uv); if (old != us || cell.U != (us | uv)) bad++;
            }
        for (int i = 0; i < n; i++)
        {
            int s = IntEdges[i];
            cell.I = s; if (Inc(ref cell.I) != unchecked(s + 1) || cell.I != unchecked(s + 1)) bad++;
            cell.I = s; if (Dec(ref cell.I) != unchecked(s - 1) || cell.I != unchecked(s - 1)) bad++;
            cell.I = s; IncUnused(ref cell.I); if (cell.I != unchecked(s + 1)) bad++;
            cell.U = (uint)s; ulong r = Inc(ref cell.U); if (r != unchecked((uint)s + 1) || cell.U != unchecked((uint)s + 1)) bad++;
            cell.I = s; if (XchgZero(ref cell.I) != s || cell.I != 0) bad++;
            cell.I = s; if (XchgSelf(ref cell.I) != s || cell.I != s) bad++;
        }
        Check("exchange and arithmetic int", 0, bad);

        bad = 0;
        n = LongEdges.Length;
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                long s = LongEdges[i], v = LongEdges[j];
                ulong us = (ulong)s, uv = (ulong)v;
                cell.L = s; if (Xchg(ref cell.L, v) != s || cell.L != v) bad++;
                cell.L = s; XchgUnusedL(ref cell.L, v); if (cell.L != v) bad++;
                cell.UL = us; if (Xchg(ref cell.UL, uv) != us || cell.UL != uv) bad++;
                cell.N = (nint)s; if (Xchg(ref cell.N, (nint)v) != (nint)s || cell.N != (nint)v) bad++;
                cell.L = s; if (Add(ref cell.L, v) != unchecked(s + v) || cell.L != unchecked(s + v)) bad++;
                cell.L = s; AddUnusedL(ref cell.L, v); if (cell.L != unchecked(s + v)) bad++;
                cell.UL = us; if (Add(ref cell.UL, uv) != unchecked(us + uv) || cell.UL != unchecked(us + uv)) bad++;
                cell.L = s; if (And(ref cell.L, v) != s || cell.L != (s & v)) bad++;
                cell.L = s; if (Or(ref cell.L, v) != s || cell.L != (s | v)) bad++;
                cell.L = s; OrUnusedL(ref cell.L, v); if (cell.L != (s | v)) bad++;
                cell.UL = us; if (And(ref cell.UL, uv) != us || cell.UL != (us & uv)) bad++;
                cell.UL = us; if (Or(ref cell.UL, uv) != us || cell.UL != (us | uv)) bad++;
            }
        for (int i = 0; i < n; i++)
        {
            long s = LongEdges[i];
            cell.L = s; if (Inc(ref cell.L) != unchecked(s + 1) || cell.L != unchecked(s + 1)) bad++;
            cell.L = s; if (Dec(ref cell.L) != unchecked(s - 1) || cell.L != unchecked(s - 1)) bad++;
            cell.L = s; DecUnusedL(ref cell.L); if (cell.L != unchecked(s - 1)) bad++;
            cell.UL = (ulong)s; if (Dec(ref cell.UL) != unchecked((ulong)s - 1) || cell.UL != unchecked((ulong)s - 1)) bad++;
            cell.L = s; if (XchgZeroL(ref cell.L) != s || cell.L != 0) bad++;
            cell.L = s; if (ReadLong(ref cell.L) != s) bad++;
        }
        Check("exchange and arithmetic long", 0, bad);

        bad = 0;
        for (int v = 0; v < 256; v += 5)
        {
            cell.B = (byte)v; if (Xchg(ref cell.B, (byte)(v ^ 0xFF)) != (byte)v || cell.B != (byte)(v ^ 0xFF)) bad++;
            cell.S = (short)(v * 257 - 30000); if (Xchg(ref cell.S, (short)-v) != (short)(v * 257 - 30000) || cell.S != (short)-v) bad++;
        }
        object a = new object();
        if (Xchg(ref cell.O, a) != null || !ReferenceEquals(Xchg(ref cell.O, null), a) || cell.O != null) bad++;
        Check("exchange small types and object", 0, bad);

        int[] ints = { 5, int.MaxValue, int.MinValue, -1 };
        long[] longs = { 5, long.MaxValue, long.MinValue, -1 };
        bad = 0;
        for (int i = 0; i < 4; i++)
        {
            int s = ints[i];
            if (AddSelf(ints, i) != unchecked(s + s) || ints[i] != unchecked(s + s)) bad++;
            long l = longs[i];
            if (AddSelfL(longs, i) != unchecked(l + l) || longs[i] != unchecked(l + l)) bad++;
        }
        long slot = 77;
        if (XchgAddress(&slot) != 77 || slot != (long)&slot) bad++;
        if (CasAddress(&slot) != (long)&slot || slot != (long)&slot + 1) bad++;
        slot = 3;
        if (AddAddress(&slot) != (long)&slot + 3 || slot != (long)&slot + 3) bad++;
        Check("value and location that are the same", 0, bad);
        Check("exchange guards", 0, cell.Guards());
    }

    // ---- many values alive around the operations --------------------------------------------
    [MethodImpl(NoInl)]
    static long Busy(Cell c, int[] a, long[] b, int n)
    {
        long s0 = 1, s1 = 2, s2 = 3, s3 = 4, s4 = 5, s5 = 6, s6 = 7, s7 = 8;
        for (int i = 0; i < n; i++)
        {
            int k = i & 7;
            s0 += Interlocked.Increment(ref c.I);
            s1 ^= Interlocked.Add(ref c.L, s0 + i);
            s2 += Interlocked.Exchange(ref a[k], (int)s1);
            s3 += Interlocked.CompareExchange(ref b[k], s2, b[k]);
            s4 ^= Interlocked.CompareExchange(ref a[(k + 1) & 7], i, i - 8);
            s5 += Interlocked.Or(ref c.UL, (ulong)(1L << (i & 63))) > 0 ? 1 : 0;
            s6 += Interlocked.And(ref c.I, ~(1 << (i & 15)));
            Interlocked.Add(ref b[(k + 3) & 7], s6);
            Interlocked.Decrement(ref c.L);
            s7 = s7 * 31 + (uint)Interlocked.Exchange(ref c.U, (uint)(s7 >> 3));
        }
        return s0 + s1 * 3 + s2 * 5 + s3 * 7 + s4 * 11 + s5 * 13 + s6 * 17 + s7 * 19 + c.I + c.L + (long)c.UL + c.U;
    }

    // The same with plain operations, compiled without optimization.
    [MethodImpl(NoInl | MethodImplOptions.NoOptimization)]
    static long BusyModel(Cell c, int[] a, long[] b, int n)
    {
        long s0 = 1, s1 = 2, s2 = 3, s3 = 4, s4 = 5, s5 = 6, s6 = 7, s7 = 8;
        for (int i = 0; i < n; i++)
        {
            int k = i & 7;
            c.I = c.I + 1; s0 += c.I;
            c.L = c.L + s0 + i; s1 ^= c.L;
            int oa = a[k]; a[k] = (int)s1; s2 += oa;
            long ob = b[k]; b[k] = s2; s3 += ob;
            int oc = a[(k + 1) & 7]; if (oc == i - 8) a[(k + 1) & 7] = i; s4 ^= oc;
            ulong ou = c.UL; c.UL = ou | (ulong)(1L << (i & 63)); s5 += ou > 0 ? 1 : 0;
            int oi = c.I; c.I = oi & ~(1 << (i & 15)); s6 += oi;
            b[(k + 3) & 7] = b[(k + 3) & 7] + s6;
            c.L = c.L - 1;
            uint ox = c.U; c.U = (uint)(s7 >> 3); s7 = s7 * 31 + ox;
        }
        return s0 + s1 * 3 + s2 * 5 + s3 * 7 + s4 * 11 + s5 * 13 + s6 * 17 + s7 * 19 + c.I + c.L + (long)c.UL + c.U;
    }

    static void CaseBusy()
    {
        var c1 = new Cell { I = int.MaxValue - 5, L = long.MaxValue - 100, U = 0xFFFFFFF0 };
        var c2 = new Cell { I = int.MaxValue - 5, L = long.MaxValue - 100, U = 0xFFFFFFF0 };
        int[] a1 = { 1, -8, 3, -6, 5, -4, 7, -2 }, a2 = (int[])a1.Clone();
        long[] b1 = { -1, 2, -3, 4, -5, 6, -7, 8 }, b2 = (long[])b1.Clone();
        long expected = BusyModel(c2, a2, b2, 500);
        Check("many values alive", expected, Busy(c1, a1, b1, 500));
        long bad = 0;
        for (int i = 0; i < 8; i++) if (a1[i] != a2[i] || b1[i] != b2[i]) bad++;
        Check("many values alive, arrays", 0, bad);
        Check("many values alive, guards", 0, c1.Guards());
    }

    // ---- volatile ----------------------------------------------------------------------------------
    [MethodImpl(NoInl)] static long VRead(ref byte x) => Volatile.Read(ref x);
    [MethodImpl(NoInl)] static long VRead(ref sbyte x) => Volatile.Read(ref x);
    [MethodImpl(NoInl)] static long VRead(ref short x) => Volatile.Read(ref x);
    [MethodImpl(NoInl)] static long VRead(ref ushort x) => Volatile.Read(ref x);
    [MethodImpl(NoInl)] static long VRead(ref int x) => Volatile.Read(ref x);
    [MethodImpl(NoInl)] static long VRead(ref uint x) => Volatile.Read(ref x);
    [MethodImpl(NoInl)] static long VRead(ref long x) => Volatile.Read(ref x);
    [MethodImpl(NoInl)] static ulong VRead(ref ulong x) => Volatile.Read(ref x);
    [MethodImpl(NoInl)] static object VRead(ref object x) => Volatile.Read(ref x);
    [MethodImpl(NoInl)] static void VWrite(ref byte x, byte v) => Volatile.Write(ref x, v);
    [MethodImpl(NoInl)] static void VWrite(ref sbyte x, sbyte v) => Volatile.Write(ref x, v);
    [MethodImpl(NoInl)] static void VWrite(ref short x, short v) => Volatile.Write(ref x, v);
    [MethodImpl(NoInl)] static void VWrite(ref ushort x, ushort v) => Volatile.Write(ref x, v);
    [MethodImpl(NoInl)] static void VWrite(ref int x, int v) => Volatile.Write(ref x, v);
    [MethodImpl(NoInl)] static void VWrite(ref uint x, uint v) => Volatile.Write(ref x, v);
    [MethodImpl(NoInl)] static void VWrite(ref long x, long v) => Volatile.Write(ref x, v);
    [MethodImpl(NoInl)] static void VWrite(ref ulong x, ulong v) => Volatile.Write(ref x, v);
    [MethodImpl(NoInl)] static long VolatileFields(Cell c, int v)
    {
        c.VI = v; c.VU = (uint)v; c.VB = (byte)v; c.VSB = (sbyte)v; c.VS = (short)v; c.VUS = (ushort)v; c.VBool = (v & 1) != 0; c.VO = c;
        Interlocked.MemoryBarrier();
        long r = c.VI;
        r = r * 3 + c.VU;
        r = r * 3 + c.VB;
        r = r * 3 + c.VSB;
        Thread.MemoryBarrier();
        r = r * 3 + c.VS;
        r = r * 3 + c.VUS;
        r = r * 3 + (c.VBool ? 1 : 0);
        return r * 3 + (ReferenceEquals(c.VO, c) ? 1 : 0);
    }
    [MethodImpl(NoInl)] static long VolatileLoop(Cell c, int n)
    {
        long sum = 0;
        for (int i = 0; i < n; i++) { c.VI += i; c.VU -= (uint)i; sum += c.VI + (long)c.VU + WithCctor.Read(i); WithCctor.Shared = i; }
        return sum;
    }

    static void CaseVolatile()
    {
        var cell = new Cell();
        long bad = 0;
        for (int i = 0; i < IntEdges.Length; i++)
        {
            int v = IntEdges[i];
            VWrite(ref cell.B, (byte)v); if (VRead(ref cell.B) != (byte)v || cell.B != (byte)v) bad++;
            VWrite(ref cell.SB, (sbyte)v); if (VRead(ref cell.SB) != (sbyte)v || cell.SB != (sbyte)v) bad++;
            VWrite(ref cell.S, (short)v); if (VRead(ref cell.S) != (short)v || cell.S != (short)v) bad++;
            VWrite(ref cell.US, (ushort)v); if (VRead(ref cell.US) != (ushort)v || cell.US != (ushort)v) bad++;
            VWrite(ref cell.I, v); if (VRead(ref cell.I) != v || cell.I != v) bad++;
            VWrite(ref cell.U, (uint)v); if (VRead(ref cell.U) != (uint)v || cell.U != (uint)v) bad++;
            long l = LongEdges[i % LongEdges.Length];
            VWrite(ref cell.L, l); if (VRead(ref cell.L) != l || cell.L != l) bad++;
            VWrite(ref cell.UL, (ulong)l); if (VRead(ref cell.UL) != (ulong)l || cell.UL != (ulong)l) bad++;

            long e = v;
            e = e * 3 + (uint)v; e = e * 3 + (byte)v; e = e * 3 + (sbyte)v; e = e * 3 + (short)v; e = e * 3 + (ushort)v; e = e * 3 + (v & 1); e = e * 3 + 1;
            if (VolatileFields(cell, v) != e) bad++;
        }
        cell.O = cell;
        if (!ReferenceEquals(VRead(ref cell.O), cell)) bad++;
        Check("volatile reads and writes", 0, bad);
        Check("volatile guards", 0, cell.Guards());

        cell.VI = int.MaxValue - 10; cell.VU = 5;
        long sum = 0;
        int mi = int.MaxValue - 10; uint mu = 5; int shared = 0;
        for (int i = 0; i < 40; i++) { mi = unchecked(mi + i); mu = unchecked(mu - (uint)i); sum += mi + (long)mu + 0x0123456789ABCDEF + i + shared; shared = i; }
        Check("volatile fields in a loop", sum, VolatileLoop(cell, 40));
        Check("static constructor ran once", 1, WithCctor.Count);
    }

    // ---- locks and the library types built on these operations -----------------------------------------
    static readonly object gate = new object();
    static int lockDepth;

    [MethodImpl(NoInl)]
    static int Locked(int depth)
    {
        lock (gate)
        {
            lockDepth++;
            if (depth > 0) return Locked(depth - 1) + 1;
            if (!Monitor.IsEntered(gate)) return -1000;
            return 0;
        }
    }

    [MethodImpl(NoInl)]
    static int LockedThrow(object o, int x)
    {
        lock (o)
        {
            if (x < 0) throw new ArgumentException("x");
            return x + 1;
        }
    }

    static void CaseLocks()
    {
        Check("nested lock", 5 * 10 + 6, Locked(5) * 10 + lockDepth);
        Check("lock released", 0, Monitor.IsEntered(gate) ? 1 : 0);
        object o = new object();
        string got = "nothing";
        try { LockedThrow(o, -1); } catch (ArgumentException) { got = "ArgumentException"; }
        Check("lock released by an exception", 1, got == "ArgumentException" && !Monitor.IsEntered(o) ? 1 : 0);
        Check("lock taken again", 8, LockedThrow(o, 7));
        bool taken = false;
        Monitor.TryEnter(o, ref taken);
        Check("try enter", 1, taken && Monitor.IsEntered(o) ? 1 : 0);
        Monitor.Exit(o);
        long sum = 0;
        for (int i = 0; i < 2000; i++) lock (o) { sum += i; }
        Check("lock in a loop", 1999 * 2000 / 2, sum);
        var modern = new Lock();
        using (modern.EnterScope()) { using (modern.EnterScope()) { sum++; } }
        Check("Lock", 1, modern.IsHeldByCurrentThread ? 0 : 1);

        var spin = new SpinLock(false);
        bool held = false;
        spin.Enter(ref held);
        Check("spin lock", 1, held && spin.IsHeld ? 1 : 0);
        spin.Exit();
        Check("spin lock released", 0, spin.IsHeld ? 1 : 0);

        int made = 0;
        var lazy = new Lazy<string>(() => { made++; return "lazy"; });
        Check("lazy", 4 * 10 + 1, (lazy.Value + lazy.Value).Length / 2 * 10 + made);
        string ensured = null;
        LazyInitializer.EnsureInitialized(ref ensured, () => "once");
        LazyInitializer.EnsureInitialized(ref ensured, () => "twice");
        Check("ensure initialized", 1, ensured == "once" ? 1 : 0);

        var dict = new ConcurrentDictionary<int, long>();
        for (int i = 0; i < 300; i++) dict.AddOrUpdate(i % 50, i, (k, old) => old + i);
        long total = 0;
        for (int i = 0; i < 50; i++) total += dict[i];
        Check("concurrent dictionary", 299 * 300 / 2, total);
        var queue = new ConcurrentQueue<int>();
        for (int i = 0; i < 200; i++) queue.Enqueue(i);
        long order = 0;
        while (queue.TryDequeue(out int x)) order = order * 3 % 1000003 + x;
        long eo = 0;
        for (int i = 0; i < 200; i++) eo = eo * 3 % 1000003 + i;
        Check("concurrent queue", eo, order);
    }

    static int Main()
    {
        CaseCompareExchangeInt();
        CaseCompareExchangeOther();
        CaseExchangeAndArithmetic();
        CaseBusy();
        CaseVolatile();
        CaseLocks();

        Console.WriteLine(fails == 0 ? "ALL PASS" : "FAILED " + fails);
        return fails;
    }
}
