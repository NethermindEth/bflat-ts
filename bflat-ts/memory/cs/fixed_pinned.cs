// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// fixed statements whose pinned local is an ordinary local.
//
// The collector of a zkVM guest never moves an object, so with
// JitZkNoPinning the JIT drops the pinned attribute of a local: the local
// may live in a register, it is not zeroed in the prolog and the store that
// clears it at the end of the fixed block is dead. What can go wrong: the
// pointer made from the local is wrong after the local moved to a register
// or was spilled around a call, the null and the empty case give a pointer
// that is not null, and a local of an inlined method or of a loop keeps the
// value of the turn before.
//
// The cases: an array, a string, a span, an element, a field of an object,
// a fixed-size buffer and a field of a struct in an array; a null and an
// empty array, string and span; two pinned locals in one statement and
// nested ones; a fixed block in a loop and in an inlined method; pointer
// arithmetic in both directions and views of other widths; calls that
// allocate inside the block; more live values than registers around the
// block; a throw inside the block that is caught in the same method and
// one that passes through it; and a pinned GC handle next to a fixed block.
//
// Each case prints one line; the run ends with ALL PASS or a FAIL count, and
// the guest returns the number of failures.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

class E : Exception { public int Code; public E(int c) : base("e") { Code = c; } }

struct Pair { public long A; public long B; }

unsafe struct Buf8 { public fixed int Data[8]; }

class Holder
{
    public int Pad;
    public long Field;
    public int Tail;
    public Buf8 Fb;
    public Pair P;
}

static unsafe class Program
{
    static int fails;
    static long sink;
    static object keep;

    static void Check(string name, int bad)
    {
        Console.WriteLine((bad == 0 ? "ok   " : "FAIL ") + name + (bad == 0 ? "" : " cases=0x" + bad.ToString("x")));
        if (bad != 0) fails++;
    }

    // Overwrites every caller-saved register with values made from x.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Clobber(long x)
    {
        long a = x ^ 0x5555555555555555, b = a * 3 + 1, c = b ^ (a >> 3), d = c + b * 7;
        long e = d ^ (c << 5), f = e + d * 11, g = f ^ (e >> 7), h = g + f * 13;
        long i = h ^ (g << 9), j = i + h * 17, k = j ^ (i >> 11), l = k + j * 19;
        long m = l ^ (k << 13), n = m + l * 23, o = n ^ (m >> 2), p = o + n * 29;
        sink += a ^ b ^ c ^ d ^ e ^ f ^ g ^ h ^ i ^ j ^ k ^ l ^ m ^ n ^ o ^ p;
        return x + 1;
    }

    // Allocates and overwrites the registers.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Alloc(int n)
    {
        keep = new long[(n & 15) + 1];
        Clobber(n);
    }

    // Overwrites the registers, then throws for a positive code.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Thrower(int code)
    {
        long r = Clobber(code);
        if (code > 0) throw new E(code);
        return r;
    }

    // ---- an array, with pointer arithmetic and wider views ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Bytes(byte[] a, int n)
    {
        int bad = 0;
        long sum = 0, want = 0;
        fixed (byte* p = a)
        {
            for (int i = 0; i < n; i++)
                p[i] = (byte)(i * 7);
            Alloc(n);
            byte* q = p + n;
            while (q > p)
            {
                q--;
                sum += *q;
            }
            if (q != p || (nint)(p + n - q) != n) bad |= 1;
            if (n >= 16)
            {
                uint* w = (uint*)p;
                ulong* l = (ulong*)(p + 8);
                if (w[1] != (28u | 35u << 8 | 42u << 16 | 49u << 24)) bad |= 2;
                if ((byte)*l != 56 || (byte)(*l >> 56) != 105) bad |= 4;
                Alloc(n + 1);
                w[3] = 0xA1B2C3D4;
                if (p[12] != 0xD4 || p[15] != 0xA1 || p[11] != 77 || p[16 % n] != (byte)(16 % n * 7)) bad |= 8;
                w[3] = 84u | 91u << 8 | 98u << 16 | 105u << 24;
            }
        }
        for (int i = 0; i < n; i++)
        {
            if (a[i] != (byte)(i * 7)) bad |= 16;
            want += (byte)(i * 7);
        }
        if (sum != want) bad |= 32;
        for (int i = n; i < a.Length; i++)
            if (a[i] != 0) bad |= 64;
        return bad;
    }

    // ---- null and empty ----

    // Bit 0: the array gives a pointer, bit 1: the string, bit 2: the span.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int NullEmpty(int[] a, string s, Span<long> sp)
    {
        int r = 0;
        fixed (int* p = a)
        {
            if (p != null) r |= 1;
            if (p != null && *p != 11) r |= 64;
        }
        fixed (char* c = s)
        {
            if (c != null) r |= 2;
            if (c != null && c[s.Length] != 0) r |= 128;
        }
        fixed (long* l = sp)
        {
            if (l != null) r |= 4;
            if (l != null && *l != 22) r |= 256;
        }
        return r;
    }

    // ---- a string ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Str(string s)
    {
        int n, hash = 0, want = 0, bad = 0;
        fixed (char* p = s)
        {
            char* q = p;
            while (*q != 0)
            {
                hash = hash * 31 + *q;
                q++;
            }
            n = (int)(q - p);
            Alloc(n);
            if (p[n] != 0 || (n > 0 && q[-1] != s[n - 1])) bad |= 1;
        }
        for (int i = 0; i < s.Length; i++)
            want = want * 31 + s[i];
        if (n != s.Length) bad |= 2;
        if (hash != want) bad |= 4;
        return bad;
    }

    // ---- two pinned locals in one statement, and a nested one ----

    // dst gets src times three in reverse order; src[1] gets 1000 more.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Copy(long[] src, long[] dst)
    {
        int bad = 0, n = src.Length;
        fixed (long* s = src, d = dst)
        {
            long* e = s + n;
            long* o = d + n;
            for (long* x = s; x < e; x++)
                *--o = *x * 3;
            if (o != d) bad |= 1;
            Alloc(n);
            fixed (long* s2 = &src[1])
            {
                if (s2 != s + 1 || s2 - s != 1) bad |= 2;
                *s2 += 1000;
                fixed (long* d2 = &dst[n - 1])
                {
                    if (d2 - d != n - 1 || *d2 != s[0] * 3) bad |= 4;
                }
            }
            if (s[1] != 1001) bad |= 8;
        }
        for (int i = 0; i < n; i++)
            if (dst[n - 1 - i] != i * 3 || src[i] != (i == 1 ? 1001 : i)) bad |= 16;
        return bad;
    }

    // ---- an element, a field, a fixed-size buffer ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Interior(long[] arr, Holder h, Pair[] ps)
    {
        int bad = 0;
        fixed (long* p = &arr[3])
        {
            p[0] = 30;
            p[1] = 31;
            p[-1] = 29;
            Alloc(3);
            p[2] = p[0] + p[1] + p[-1];
        }
        fixed (long* f = &h.Field)
        {
            *f = 0x1122334455667788;
            Alloc(4);
            *f += 1;
        }
        fixed (long* b = &ps[2].B)
        {
            *b = 77;
            b[-1] = 76;
            b[1] = 78;
        }
        fixed (int* d = h.Fb.Data)
        {
            for (int i = 0; i < 8; i++)
                d[i] = i * i;
            Alloc(5);
            d[7] += d[0] + d[1];
        }
        fixed (Pair* pp = &h.P)
        {
            pp->A = 5;
            pp->B = pp->A + 1;
        }
        if (arr[2] != 29 || arr[3] != 30 || arr[4] != 31 || arr[5] != 90 || arr[1] != 0 || arr[6] != 0) bad |= 1;
        if (h.Field != 0x1122334455667789 || h.Pad != 0x7EADBEEF || h.Tail != 0x12345678) bad |= 2;
        if (ps[2].B != 77 || ps[2].A != 76 || ps[3].A != 78 || ps[1].B != 0 || ps[3].B != 0) bad |= 4;
        int sum = 0;
        for (int i = 0; i < 8; i++)
            sum += h.Fb.Data[i];
        if (sum != 141 || h.Fb.Data[7] != 50) bad |= 8;
        if (h.P.A != 5 || h.P.B != 6) bad |= 16;
        return bad;
    }

    // ---- a fixed block in a loop and in an inlined method ----

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static long SumInlined(int[] a)
    {
        long s = 0;
        fixed (int* p = a)
        {
            for (int i = 0; i < a.Length; i++)
                s += p[i];
        }
        return s;
    }

    // Array k has k elements, so the first one is empty. Element i of array
    // k goes from i to i + k.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Loop(int[][] arrs)
    {
        long total = 0, want = 0;
        int bad = 0;
        for (int k = 0; k < arrs.Length; k++)
        {
            fixed (int* p = arrs[k])
            {
                int n = arrs[k].Length;
                if ((p == null) != (n == 0)) bad |= 1;
                for (int i = 0; i < n; i++)
                    p[i] += k;
                if ((k & 1) != 0) Alloc(k);
            }
            total += SumInlined(arrs[k]);
            want += (long)k * (k - 1) / 2 + (long)k * k;
        }
        if (total != want) bad |= 2;
        return bad;
    }

    // ---- more live values than registers around the block ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int V14(long b, long v0, long v1, long v2, long v3, long v4, long v5, long v6,
                   long v7, long v8, long v9, long v10, long v11, long v12, long v13)
    {
        int bad = 0;
        if (v0 != b) bad |= 1 << 0;
        if (v1 != b + 1) bad |= 1 << 1;
        if (v2 != b + 2) bad |= 1 << 2;
        if (v3 != b + 3) bad |= 1 << 3;
        if (v4 != b + 4) bad |= 1 << 4;
        if (v5 != b + 5) bad |= 1 << 5;
        if (v6 != b + 6) bad |= 1 << 6;
        if (v7 != b + 7) bad |= 1 << 7;
        if (v8 != b + 8) bad |= 1 << 8;
        if (v9 != b + 9) bad |= 1 << 9;
        if (v10 != b + 10) bad |= 1 << 10;
        if (v11 != b + 11) bad |= 1 << 11;
        if (v12 != b + 12) bad |= 1 << 12;
        if (v13 != b + 13) bad |= 1 << 13;
        return bad;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Pressure(long[] s, long[] dst, bool c)
    {
        long v0 = s[0], v1 = s[1], v2 = s[2], v3 = s[3], v4 = s[4], v5 = s[5], v6 = s[6];
        long v7 = s[7], v8 = s[8], v9 = s[9], v10 = s[10], v11 = s[11], v12 = s[12], v13 = s[13];
        fixed (long* p = dst, q = s)
        {
            p[0] = v0;
            if (c)
                v1 = Clobber(v1) - 1;
            p[1] = v1 + q[2];
            Alloc(2);
            p[2] = v13;
            for (int i = 0; i < 14; i++)
                p[3] += q[i];
            if (c)
                Alloc(3);
            p[4] = (nint)(q + 13 - q) + (p == q ? 100 : 0);
        }
        int bad = V14(100, v0, v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13);
        if (dst[0] != 100 || dst[1] != 203 || dst[2] != 113 || dst[3] != 1491 || dst[4] != 13) bad |= 1 << 14;
        return bad;
    }

    // ---- exceptions ----

    // The throw is inside the block and the handler pins the array again.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Caught(long[] a, int code)
    {
        int stage = 0;
        try
        {
            fixed (long* p = a)
            {
                p[0] = 1;
                stage = 1;
                Thrower(code);
                p[1] = 2;
                stage = 2;
            }
        }
        catch (E e)
        {
            stage += 10 * e.Code;
            fixed (long* p = a)
            {
                p[2] = 3;
            }
        }
        finally
        {
            fixed (long* p = &a[3])
            {
                *p = stage;
            }
        }
        return stage;
    }

    // The throw passes through a frame that is inside a fixed block.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Passing(long[] a, int code)
    {
        fixed (long* p = a)
        {
            p[4] = 44;
            long r = Thrower(code);
            p[5] = 55;
            return r + p[4];
        }
    }

    // ---- a span and a stackalloc block ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Spans(int[] a)
    {
        int bad = 0;
        Span<int> sp = a.AsSpan(2, 5);
        Span<int> st = stackalloc int[8];
        st.Clear();
        fixed (int* p = sp)
        fixed (int* q = st)
        fixed (int* r = &MemoryMarshal.GetReference(sp))
        {
            if (p != r) bad |= 1;
            for (int i = 0; i < 5; i++)
                q[i] = p[i] * 2;
            q[5] = (int)(p + 4 - r);
            Alloc(6);
            p[4] = q[0] + q[1];
            p[-1] = q[5];
        }
        if (st[0] != 4 || st[4] != 12 || st[5] != 4 || st[6] != 0) bad |= 2;
        if (a[6] != 10 || a[1] != 4 || a[2] != 2 || a[5] != 5 || a[7] != 7 || a[0] != 0) bad |= 4;
        return bad;
    }

    // ---- a pinned handle ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Handle()
    {
        int bad = 0;
        byte[] a = new byte[64];
        GCHandle h = GCHandle.Alloc(a, GCHandleType.Pinned);
        byte* p = (byte*)h.AddrOfPinnedObject();
        fixed (byte* f = a)
        {
            if (f != p) bad |= 1;
            f[6] = 8;
        }
        p[5] = 9;
        Alloc(7);
        p[7] = (byte)(p[5] + p[6]);
        h.Free();
        if (a[5] != 9 || a[6] != 8 || a[7] != 17 || a[4] != 0 || a[8] != 0) bad |= 2;
        return bad;
    }

    // ---- driver ----

    static int Main()
    {
        int bad = 0;
        for (int n = 0; n <= 40; n += 8)
            bad |= Bytes(new byte[48], n == 0 ? 0 : n + (n == 40 ? 0 : 1));
        bad |= Bytes(new byte[0], 0) << 8;
        Check("array", bad);

        bad = 0;
        if (NullEmpty(null, null, default) != 0) bad |= 1;
        if (NullEmpty(new int[0], "", new long[0]) != 2) bad |= 2;
        if (NullEmpty(new int[] { 11 }, "q", new long[] { 22 }) != 7) bad |= 4;
        if (NullEmpty(new int[] { 11, 12 }, null, new long[] { 0, 22 }.AsSpan(1)) != 5) bad |= 8;
        if (NullEmpty(null, "qq", default) != 2) bad |= 16;
        Check("null and empty", bad);

        Check("string", Str("") | Str("a") | Str("hello, pinned world") | Str(new string('z', 300)));

        bad = 0;
        for (int n = 2; n < 20; n += 5)
        {
            long[] src = new long[n];
            for (int i = 0; i < n; i++) src[i] = i;
            bad |= Copy(src, new long[n]);
        }
        Check("two arrays", bad);

        Holder h = new Holder { Pad = 0x7EADBEEF, Tail = 0x12345678 };
        Check("interior", Interior(new long[8], h, new Pair[4]));

        int[][] arrs = new int[12][];
        for (int k = 0; k < arrs.Length; k++)
        {
            arrs[k] = new int[k];
            for (int i = 0; i < k; i++) arrs[k][i] = i;
        }
        Check("loop and inlined", Loop(arrs));

        long[] s = new long[16];
        for (int i = 0; i < 16; i++) s[i] = 100 + i;
        Check("many live values", Pressure(s, new long[8], false) | Pressure(s, new long[8], true));

        bad = 0;
        long[] a = new long[8];
        if (Caught(a, 0) != 2 || a[0] != 1 || a[1] != 2 || a[2] != 0 || a[3] != 2) bad |= 1;
        a = new long[8];
        if (Caught(a, 7) != 71 || a[0] != 1 || a[1] != 0 || a[2] != 3 || a[3] != 71) bad |= 2;
        a = new long[8];
        if (Passing(a, 0) != 45 || a[4] != 44 || a[5] != 55) bad |= 4;
        a = new long[8];
        int code = 0;
        try { Passing(a, 8); } catch (E e) { code = e.Code; }
        if (code != 8 || a[4] != 44 || a[5] != 0) bad |= 8;
        for (int i = 0; i < 50; i++)
        {
            code = 0;
            try { Passing(a, 100 + i); } catch (E e) { code = e.Code; }
            if (code != 100 + i || Caught(a, i + 1) != 10 * (i + 1) + 1) bad |= 16;
        }
        Check("exceptions", bad);

        Check("span and stackalloc", Spans(new int[] { 0, 1, 2, 3, 4, 5, 6, 7 }));
        Check("pinned handle", Handle());

        Console.WriteLine(fails == 0 ? "ALL PASS" : "FAILED " + fails);
        return fails;
    }
}
