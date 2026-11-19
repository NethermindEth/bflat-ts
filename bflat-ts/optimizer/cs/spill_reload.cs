// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Locals that stay in a caller-saved register up to a call that kills it.
//
// With JitZkSpillKeepReg the register allocator leaves such a local in its
// register until the call, the code generator stores it to its stack home
// right before the call, and the reload is placed on the edges that leave the
// block of the call. With JitZkColdCallSpill a local that only crosses calls
// in cold blocks does not get a callee-saved register at all, so every one of
// those calls is such a spill. What can go wrong: a store that is missing
// before one of the calls, a reload that is missing on one of the edges, a
// stale home after the local was changed between two calls, and a register
// that the collector or the unwinder still takes for the local.
//
// Every method here loads fourteen values from an array (a load is not a
// call, so the values do not cross a call in a hot block), runs a call on a
// path that is taken or not, and then checks every value against a literal.
// Fourteen values are more than the callee-saved registers, so some of them
// are spilled without the cold call policy too. The call overwrites every
// caller-saved register. The cases: a call in one arm, in both arms, in a
// switch, two calls in a row with a change between them, calls in loops,
// object references, interior pointers and spans, promoted struct fields,
// parameters, runtime helper calls, nested call arguments, and calls that
// throw through or into the frame.
//
// Each case prints one line; the run ends with ALL PASS or a FAIL count, and
// the guest returns the number of failures.
using System;
using System.Runtime.CompilerServices;

class E : Exception { public int Code; public E(int c) : base("e") { Code = c; } }

class Node { public long V; public Node Next; public object Any; }

struct Pair { public long A; public long B; }

static class Program
{
    static int fails;
    static long sink;
    static object staticRef;

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

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ClobberI(int x)
    {
        int a = x ^ 0x55555555, b = a * 3 + 1, c = b ^ (a >> 3), d = c + b * 7;
        int e = d ^ (c << 5), f = e + d * 11, g = f ^ (e >> 7), h = g + f * 13;
        int i = h ^ (g << 9), j = i + h * 17, k = j ^ (i >> 11), l = k + j * 19;
        int m = l ^ (k << 13), n = m + l * 23, o = n ^ (m >> 2), p = o + n * 29;
        sink += a ^ b ^ c ^ d ^ e ^ f ^ g ^ h ^ i ^ j ^ k ^ l ^ m ^ n ^ o ^ p;
        return x + 1;
    }

    // Allocates, so the registers are overwritten by the allocator as well.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static Node Alloc(long v)
    {
        Node n = new Node { V = Clobber(v) };
        n.Any = new long[4];
        return n;
    }

    // Overwrites the registers, then throws for a positive code.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Thrower(int code)
    {
        long r = Clobber(code);
        if (code > 0) throw new E(code);
        return r;
    }

    // Bit i is set when v[i] is not b + i.
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
    static int V14I(int b, int v0, int v1, int v2, int v3, int v4, int v5, int v6,
                    int v7, int v8, int v9, int v10, int v11, int v12, int v13)
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

    // ---- a call in one arm ----

    // The call changes v0, the other thirteen values only cross it.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int OneArm(long[] s, bool c)
    {
        long v0 = s[0], v1 = s[1], v2 = s[2], v3 = s[3], v4 = s[4], v5 = s[5], v6 = s[6];
        long v7 = s[7], v8 = s[8], v9 = s[9], v10 = s[10], v11 = s[11], v12 = s[12], v13 = s[13];
        if (c)
            v0 += Clobber(v1);
        return V14(100, c ? v0 - 102 : v0, v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13);
    }

    // The same with native ints: on a 32-bit target a long is two registers.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int OneArmInt(int[] s, bool c)
    {
        int v0 = s[0], v1 = s[1], v2 = s[2], v3 = s[3], v4 = s[4], v5 = s[5], v6 = s[6];
        int v7 = s[7], v8 = s[8], v9 = s[9], v10 = s[10], v11 = s[11], v12 = s[12], v13 = s[13];
        if (c)
            v0 += ClobberI(v1);
        return V14I(-7, c ? v0 + 5 : v0, v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13);
    }

    // ---- different calls in the arms ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Arms(long[] s, int sel)
    {
        long v0 = s[0], v1 = s[1], v2 = s[2], v3 = s[3], v4 = s[4], v5 = s[5], v6 = s[6];
        long v7 = s[7], v8 = s[8], v9 = s[9], v10 = s[10], v11 = s[11], v12 = s[12], v13 = s[13];
        long d2 = 0, d5 = 0, d9 = 0;
        if (sel == 0)
        {
            v2 = Clobber(v3);
            d2 = 2;
        }
        else if (sel == 1)
        {
            v5 += Thrower(-3);
            d5 = -2;
        }
        else if (sel == 2)
        {
            Node n = Alloc(v9);
            v9 = n.V;
            d9 = 1;
        }
        return V14(100, v0, v1, v2 - d2, v3, v4, v5 - d5, v6, v7, v8, v9 - d9, v10, v11, v12, v13);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Switch(int[] s, int sel)
    {
        int v0 = s[0], v1 = s[1], v2 = s[2], v3 = s[3], v4 = s[4], v5 = s[5], v6 = s[6];
        int v7 = s[7], v8 = s[8], v9 = s[9], v10 = s[10], v11 = s[11], v12 = s[12], v13 = s[13];
        int d = 0;
        switch (sel)
        {
            case 0: v0 = ClobberI(v0); d = 1; break;
            case 1: v1 += 1; d = 1; break;
            case 2: v2 = ClobberI(ClobberI(v2)) - 1; d = 1; break;
            case 3: break;
            case 4: v4 = (int)Clobber(v4); v13 -= 1; d = 1; break;
            default: v5 = v5 + v6 - v6; break;
        }
        return V14I(-7, sel == 0 ? v0 - d : v0, sel == 1 ? v1 - d : v1, sel == 2 ? v2 - d : v2, v3,
                    sel == 4 ? v4 - d : v4, v5, v6, v7, v8, v9, v10, v11, v12, sel == 4 ? v13 + 1 : v13);
    }

    // ---- two calls with a change between them ----

    // v1 is reloaded after the first call, changed, and has to be stored
    // again before the second one. v2 is not changed: its home is still good.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int TwoCalls(long[] s, bool c1, bool c2)
    {
        long v0 = s[0], v1 = s[1], v2 = s[2], v3 = s[3], v4 = s[4], v5 = s[5], v6 = s[6];
        long v7 = s[7], v8 = s[8], v9 = s[9], v10 = s[10], v11 = s[11], v12 = s[12], v13 = s[13];
        if (c1)
            v0 = Clobber(v0) - 1;
        v1 += 1000;
        v3 ^= v1;
        if (c2)
            v4 = Clobber(v4) - 1;
        v3 ^= v1;
        v1 -= 1000;
        return V14(100, v0, v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13);
    }

    // The value is changed in one arm only, after the call of that arm.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ChangeAfter(int[] s, bool c1, bool c2)
    {
        int v0 = s[0], v1 = s[1], v2 = s[2], v3 = s[3], v4 = s[4], v5 = s[5], v6 = s[6];
        int v7 = s[7], v8 = s[8], v9 = s[9], v10 = s[10], v11 = s[11], v12 = s[12], v13 = s[13];
        int d6 = 0, d7 = 0;
        if (c1)
        {
            ClobberI(v6);
            v6 += 50;
            d6 = 50;
            if (c2)
            {
                v7 = ClobberI(v6);
                d7 = 50;
            }
        }
        else if (c2)
        {
            v7 -= 9;
            d7 = -9;
            ClobberI(v7);
        }
        return V14I(-7, v0, v1, v2, v3, v4, v5, v6 - d6, v7 - d7, v8, v9, v10, v11, v12, v13);
    }

    // ---- loops ----

    // v0 and v1 change on every turn, the call runs on some turns and
    // changes v2. After n turns: v0 = 100 + n, v1 = 101 + 2n, v2 = 102 + calls.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Loop(long[] s, int n, int mask)
    {
        long v0 = s[0], v1 = s[1], v2 = s[2], v3 = s[3], v4 = s[4], v5 = s[5], v6 = s[6];
        long v7 = s[7], v8 = s[8], v9 = s[9], v10 = s[10], v11 = s[11], v12 = s[12], v13 = s[13];
        int calls = 0;
        for (int i = 0; i < n; i++)
        {
            v0 += 1;
            v1 += 2;
            if ((i & mask) == mask)
            {
                v2 += Clobber(v0) - v0;
                calls++;
            }
        }
        int want = 0;
        for (int i = 0; i < n; i++)
            if ((i & mask) == mask) want++;
        int bad = V14(100, v0 - n, v1 - 2 * n, v2 - calls, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13);
        return bad | (calls == want ? 0 : 1 << 14);
    }

    // A hot inner loop without a call and a call on some turns of the outer
    // one. v0 = 100 + 8 * n * 101, v2 = 102 + calls.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Nested(long[] s, int n)
    {
        long v0 = s[0], v1 = s[1], v2 = s[2], v3 = s[3], v4 = s[4], v5 = s[5], v6 = s[6];
        long v7 = s[7], v8 = s[8], v9 = s[9], v10 = s[10], v11 = s[11], v12 = s[12], v13 = s[13];
        int calls = 0;
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < 8; j++)
                v0 += v1;
            if ((i & 3) == 3)
            {
                v2 += Clobber(v0) - v0;
                calls++;
            }
        }
        return V14(100, v0 - 8L * n * 101, v1, v2 - calls, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13)
               | (calls == n / 4 ? 0 : 1 << 14);
    }

    // The call is on the way out of the loop.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int LoopExit(int[] s, int n, int stop)
    {
        int v0 = s[0], v1 = s[1], v2 = s[2], v3 = s[3], v4 = s[4], v5 = s[5], v6 = s[6];
        int v7 = s[7], v8 = s[8], v9 = s[9], v10 = s[10], v11 = s[11], v12 = s[12], v13 = s[13];
        int i = 0, d = 0;
        while (i < n)
        {
            v3 += v4;
            if (i == stop)
            {
                v5 = ClobberI(v5);
                d = 1;
                break;
            }
            i++;
        }
        int turns = stop >= 0 && stop < n ? stop + 1 : n;
        return V14I(-7, v0, v1, v2, v3 - turns * -3, v4, v5 - d, v6, v7, v8, v9, v10, v11, v12, v13);
    }

    // ---- object references ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Refs(Node[] ns, string[] strs, bool c)
    {
        Node n0 = ns[0], n1 = ns[1], n2 = ns[2], n3 = ns[3], n4 = ns[4], n5 = ns[5], n6 = ns[6];
        Node n7 = ns[7], n8 = ns[8], n9 = ns[9];
        string s0 = strs[0], s1 = strs[1], s2 = strs[2];
        long[] arr = (long[])n0.Any;
        if (c)
        {
            Node made = Alloc(n3.V);
            made.Next = n4;
            n5 = made;
        }
        int bad = 0;
        if (n0 != ns[0] || n0.V != 500) bad |= 1 << 0;
        if (n1 != ns[1] || n1.V != 501) bad |= 1 << 1;
        if (n2 != ns[2] || n2.V != 502) bad |= 1 << 2;
        if (n3 != ns[3] || n3.V != 503) bad |= 1 << 3;
        if (n4 != ns[4] || n4.V != 504) bad |= 1 << 4;
        if (c ? (n5.V != 504 || n5.Next != ns[4]) : (n5 != ns[5] || n5.V != 505)) bad |= 1 << 5;
        if (n6 != ns[6] || n6.V != 506) bad |= 1 << 6;
        if (n7 != ns[7] || n7.V != 507) bad |= 1 << 7;
        if (n8 != ns[8] || n8.V != 508) bad |= 1 << 8;
        if (n9 != ns[9] || n9.V != 509) bad |= 1 << 9;
        if ((object)s0 != strs[0] || s0.Length != 1 || s0[0] != 'a') bad |= 1 << 10;
        if ((object)s1 != strs[1] || s1.Length != 2 || s1[1] != 'c') bad |= 1 << 11;
        if ((object)s2 != strs[2] || s2.Length != 3 || s2[2] != 'f') bad |= 1 << 12;
        if (arr != ns[0].Any || arr.Length != 4 || arr[3] != 33) bad |= 1 << 13;
        return bad;
    }

    // References that change on every turn of a loop with a call on some
    // turns: six lists are walked side by side, with a new node now and then.
    // List k holds k * 1000 + i.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int RefLoop(Node[] heads, int n)
    {
        Node c0 = heads[0], c1 = heads[1], c2 = heads[2], c3 = heads[3], c4 = heads[4], c5 = heads[5];
        Node l0 = null, l1 = null, l2 = null, l3 = null, l4 = null, l5 = null, made = null;
        long sum = 0;
        int count = 0, allocs = 0;
        while (c0 != null)
        {
            sum += c0.V + c1.V + c2.V + c3.V + c4.V + c5.V;
            count++;
            l0 = c0; c0 = c0.Next;
            l1 = c1; c1 = c1.Next;
            l2 = c2; c2 = c2.Next;
            l3 = c3; c3 = c3.Next;
            l4 = c4; c4 = c4.Next;
            l5 = c5; c5 = c5.Next;
            if ((count & 7) == 0)
            {
                made = Alloc(l3.V);
                made.Next = l4;
                allocs++;
            }
        }
        int bad = 0, m = n / 8 * 8;
        if (count != n) bad |= 1;
        if (sum != 6L * n * (n - 1) / 2 + 15000L * n) bad |= 2;
        if (l0.V != n - 1 || l1.V != 1000 + n - 1 || l2.V != 2000 + n - 1) bad |= 4;
        if (l3.V != 3000 + n - 1 || l4.V != 4000 + n - 1 || l5.V != 5000 + n - 1) bad |= 8;
        if (c1 != null || c2 != null || c3 != null || c4 != null || c5 != null) bad |= 16;
        if (allocs != n / 8) bad |= 32;
        if (m > 0 && (made == null || made.V != 3000 + m || made.Next == null || made.Next.V != 4000 + m - 1))
            bad |= 64;
        if (m == 0 && made != null) bad |= 128;
        return bad;
    }

    // ---- interior pointers ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Interior(long[] s, long[] arr, Pair[] pairs, bool c)
    {
        long v0 = s[0], v1 = s[1], v2 = s[2], v3 = s[3], v4 = s[4], v5 = s[5], v6 = s[6];
        long v7 = s[7], v8 = s[8], v9 = s[9], v10 = s[10], v11 = s[11], v12 = s[12], v13 = s[13];
        ref long r0 = ref arr[3];
        ref long r1 = ref arr[5];
        ref long rb = ref pairs[1].B;
        Span<long> sp = arr.AsSpan(8, 6);
        ReadOnlySpan<long> ro = s;
        r0 = 30;
        if (c)
        {
            r1 = Clobber(v2);
            Alloc(v3);
        }
        r0 += 1;
        rb += r0;
        sp[1] = sp.Length + ro[13];
        sp[5] = ro.Length;
        int bad = V14(100, v0, v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13);
        if (arr[3] != 31) bad |= 1 << 14;
        if (arr[5] != (c ? 103 : 0)) bad |= 1 << 15;
        if (pairs[1].B != 31 || pairs[1].A != 0 || pairs[0].B != 0 || pairs[2].A != 0) bad |= 1 << 16;
        if (arr[9] != 6 + 113 || arr[13] != 16 || arr[8] != 0 || arr[12] != 0) bad |= 1 << 17;
        return bad;
    }

    // References that move along eight arrays, with a call on some turns.
    // Array k gets i * 3 + k.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int RefWalk(int[][] a, int n)
    {
        ref int p0 = ref a[0][0];
        ref int p1 = ref a[1][0];
        ref int p2 = ref a[2][0];
        ref int p3 = ref a[3][0];
        ref int p4 = ref a[4][0];
        ref int p5 = ref a[5][0];
        ref int p6 = ref a[6][0];
        ref int p7 = ref a[7][0];
        int sum = 0, calls = 0;
        for (int i = 0; i < n; i++)
        {
            p0 = i * 3; p1 = i * 3 + 1; p2 = i * 3 + 2; p3 = i * 3 + 3;
            p4 = i * 3 + 4; p5 = i * 3 + 5; p6 = i * 3 + 6; p7 = i * 3 + 7;
            if ((i & 15) == 9)
            {
                sum += ClobberI(p2) - p2;
                calls++;
            }
            sum += p0 + p1 + p2 + p3 + p4 + p5 + p6 + p7;
            if (i + 1 < n)
            {
                p0 = ref Unsafe.Add(ref p0, 1); p1 = ref Unsafe.Add(ref p1, 1);
                p2 = ref Unsafe.Add(ref p2, 1); p3 = ref Unsafe.Add(ref p3, 1);
                p4 = ref Unsafe.Add(ref p4, 1); p5 = ref Unsafe.Add(ref p5, 1);
                p6 = ref Unsafe.Add(ref p6, 1); p7 = ref Unsafe.Add(ref p7, 1);
            }
        }
        int bad = 0;
        if (sum != 8 * 3 * n * (n - 1) / 2 + 28 * n + calls || calls != (n + 6) / 16) bad |= 1;
        for (int k = 0; k < 8; k++)
            for (int i = 0; i < n; i++)
                if (a[k][i] != i * 3 + k) bad |= 2;
        if (!Unsafe.AreSame(ref p0, ref a[0][n - 1]) || !Unsafe.AreSame(ref p1, ref a[1][n - 1])) bad |= 4;
        if (!Unsafe.AreSame(ref p2, ref a[2][n - 1]) || !Unsafe.AreSame(ref p3, ref a[3][n - 1])) bad |= 8;
        if (!Unsafe.AreSame(ref p4, ref a[4][n - 1]) || !Unsafe.AreSame(ref p5, ref a[5][n - 1])) bad |= 16;
        if (!Unsafe.AreSame(ref p6, ref a[6][n - 1]) || !Unsafe.AreSame(ref p7, ref a[7][n - 1])) bad |= 32;
        return bad;
    }

    // ---- promoted struct fields ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Structs(Pair[] ps, bool c)
    {
        Pair p0 = ps[0], p1 = ps[1], p2 = ps[2], p3 = ps[3], p4 = ps[4], p5 = ps[5], p6 = ps[6];
        if (c)
        {
            p0.A = Clobber(p0.B);
            p3.B += 1;
            p3 = new Pair { A = Clobber(p3.B), B = p3.A };
        }
        return V14(700, c ? p0.A - 2 : p0.A, p0.B, p1.A, p1.B, p2.A, p2.B, c ? p3.A - 3 : p3.A,
                   c ? p3.B + 1 : p3.B, p4.A, p4.B, p5.A, p5.B, p6.A, p6.B);
    }

    // ---- parameters ----

    // Parameters stay in their argument registers up to the call.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Params(long a0, long a1, long a2, long a3, long a4, long a5, long a6, int sel)
    {
        long d1 = 0, d5 = 0;
        if (sel == 1)
        {
            a1 = Clobber(a2);
            d1 = 3;
        }
        else if (sel == 2)
        {
            a5 += 40;
            d5 = 40;
            a6 = Clobber(a6) - 1;
            a5 += 2;
            d5 += 2;
        }
        return V14(100, a0, a1 - d1 + 1, a2 + 0, a3, a4, a5 - d5, a6, 107, 108, 109, 110, 111, 112, 113)
               | (a2 == 102 ? 0 : 1 << 14);
    }

    // Parameters on the stack, and more locals than registers.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int StackParams(int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7,
                           int a8, int a9, int a10, int a11, int a12, int a13, bool c)
    {
        int x0 = a0 + a13, x1 = a1 + a12, x2 = a2 + a11;
        if (c)
        {
            a9 = ClobberI(a9) - 1;
            a8 += ClobberI(a10) - a10 - 1;
        }
        int bad = V14I(-7, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13);
        if (x0 != -1 || x1 != -1 || x2 != -1) bad |= 1 << 14;
        return bad;
    }

    // ---- runtime helpers ----

    // A helper call has its own set of registers it changes.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Helpers(long[] s, object o, object[] store, Node holder, int sel)
    {
        long v0 = s[0], v1 = s[1], v2 = s[2], v3 = s[3], v4 = s[4], v5 = s[5], v6 = s[6];
        long v7 = s[7], v8 = s[8], v9 = s[9], v10 = s[10], v11 = s[11], v12 = s[12], v13 = s[13];
        long extra = 0, wantExtra = 0;
        switch (sel)
        {
            case 1:
                extra = ((string)o).Length;
                wantExtra = 2;
                break;
            case 2:
                store[0] = o;
                break;
            case 3:
                holder.Any = o;
                break;
            case 4:
                extra = new long[(int)v0 - 96].Length;
                wantExtra = 4;
                break;
            case 5:
                staticRef = o;
                break;
            case 6:
                store[1] = v3;
                extra = (long)store[1];
                wantExtra = 103;
                break;
            case 7:
                extra = v5 / (v7 - 106) + v6 % (v8 - 100);
                wantExtra = 105 + 106 % 8;
                break;
            case 8:
                extra = (v9 * v10) >> (int)(v11 & 3);
                wantExtra = 109L * 110 >> 3;
                break;
            case 9:
                extra = o is string str ? str.Length : -1;
                wantExtra = 2;
                break;
        }
        return V14(100, v0, v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13)
               | (extra == wantExtra ? 0 : 1 << 14);
    }

    // ---- calls in the arguments of a call ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Mix(long a, long b, long c, long d, long e) => a + b * 3 + c * 5 + d * 7 + e * 11;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int NestedArgs(long[] s, bool c)
    {
        long v0 = s[0], v1 = s[1], v2 = s[2], v3 = s[3], v4 = s[4], v5 = s[5], v6 = s[6];
        long v7 = s[7], v8 = s[8], v9 = s[9], v10 = s[10], v11 = s[11], v12 = s[12], v13 = s[13];
        long r = 0;
        if (c)
            r = Mix(v0, Clobber(v1), v2, Clobber(v3 + v4), v5 + Clobber(v6));
        int bad = V14(100, v0, v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13);
        long want = c ? 100 + 102 * 3 + 102 * 5 + 208 * 7 + (105 + 107) * 11 : 0;
        return bad | (r == want ? 0 : 1 << 14);
    }

    // ---- exceptions ----

    // The call throws through this frame: nothing to check here, the caller
    // checks its own values.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ThrowThrough(long[] s, int code)
    {
        long v0 = s[0], v1 = s[1], v2 = s[2], v3 = s[3], v4 = s[4], v5 = s[5], v6 = s[6];
        long v7 = s[7], v8 = s[8], v9 = s[9], v10 = s[10], v11 = s[11], v12 = s[12], v13 = s[13];
        if (code != 0)
            v0 += Thrower(code) - code - 1;
        return V14(100, v0, v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Catcher(long[] s, int code)
    {
        try
        {
            return ThrowThrough(s, code);
        }
        catch (E e)
        {
            return e.Code == code ? 0 : 1 << 20;
        }
    }

    // The frame that keeps values in registers calls a frame that catches.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int AboveCatch(long[] s, int code)
    {
        long v0 = s[0], v1 = s[1], v2 = s[2], v3 = s[3], v4 = s[4], v5 = s[5], v6 = s[6];
        long v7 = s[7], v8 = s[8], v9 = s[9], v10 = s[10], v11 = s[11], v12 = s[12], v13 = s[13];
        int bad = 0;
        if (code != 0)
            bad = Catcher(s, code);
        return bad | V14(100, v0, v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13);
    }

    // The handler is in the frame itself. The values the handler reads are
    // not kept in registers; the test is that the rest still works.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int TryCatch(long[] s, int code)
    {
        long v0 = s[0], v1 = s[1], v2 = s[2], v3 = s[3], v4 = s[4], v5 = s[5], v6 = s[6];
        long v7 = s[7], v8 = s[8], v9 = s[9], v10 = s[10], v11 = s[11], v12 = s[12], v13 = s[13];
        long got = 0, d0 = 0, d1 = 0;
        if (code < 0)
            v2 = Clobber(v2) - 1;
        try
        {
            v0 += 5;
            d0 = 5;
            if (code != 0)
                Thrower(code);
            v0 += 6;
            d0 = 11;
        }
        catch (E e)
        {
            got = e.Code;
            v1 += Clobber(v3) - 4;
            d1 = 100;
        }
        if (code > 0)
            v4 = Clobber(v4) - 1;
        int bad = V14(100, v0 - d0, v1 - d1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13);
        if (got != (code > 0 ? code : 0)) bad |= 1 << 14;
        if (d0 != (code > 0 ? 5 : 11)) bad |= 1 << 15;
        return bad;
    }

    static int finMask;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int TryFinally(int[] s, int code)
    {
        int v0 = s[0], v1 = s[1], v2 = s[2], v3 = s[3], v4 = s[4], v5 = s[5], v6 = s[6];
        int v7 = s[7], v8 = s[8], v9 = s[9], v10 = s[10], v11 = s[11], v12 = s[12], v13 = s[13];
        if (code == 0)
            v6 = ClobberI(v6) - 1;
        try
        {
            v0 = ClobberI(v0);
            if (code != 0)
                Thrower(code);
            v0 -= 1;
        }
        finally
        {
            v1 = ClobberI(v1) - 1;
            finMask = V14I(-7, code > 0 ? v0 - 1 : v0, v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13);
        }
        return V14I(-7, v0, v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int AboveFinally(int[] s, int code)
    {
        int v0 = s[0], v1 = s[1], v2 = s[2], v3 = s[3], v4 = s[4], v5 = s[5], v6 = s[6];
        int v7 = s[7], v8 = s[8], v9 = s[9], v10 = s[10], v11 = s[11], v12 = s[12], v13 = s[13];
        int bad = 0, got = 0;
        finMask = -1;
        try
        {
            bad = TryFinally(s, code);
        }
        catch (E e)
        {
            got = e.Code;
        }
        if (code != 0)
            v8 = ClobberI(v8) - 1;
        bad |= finMask;
        if (got != (code > 0 ? code : 0)) bad |= 1 << 14;
        return bad | V14I(-7, v0, v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13);
    }

    // ---- driver ----

    static Node[] Lists(int n)
    {
        Node[] heads = new Node[6];
        for (int k = 0; k < heads.Length; k++)
            for (int i = n - 1; i >= 0; i--)
                heads[k] = new Node { V = k * 1000 + i, Next = heads[k] };
        return heads;
    }

    static int[][] Arrays(int n)
    {
        int[][] a = new int[8][];
        for (int k = 0; k < a.Length; k++)
            a[k] = new int[n];
        return a;
    }

    static int Main()
    {
        long[] s = new long[16];
        int[] si = new int[16];
        for (int i = 0; i < 16; i++)
        {
            s[i] = 100 + i;
            si[i] = i - 7;
        }

        int bad = 0;
        for (int rep = 0; rep < 3; rep++)
        {
            bad |= OneArm(s, false) | OneArm(s, true) << 14;
        }
        Check("one arm", bad);
        Check("one arm int", OneArmInt(si, false) | OneArmInt(si, true) << 14);

        bad = 0;
        for (int sel = 0; sel < 4; sel++)
            bad |= Arms(s, sel);
        Check("arms", bad);

        bad = 0;
        for (int sel = 0; sel < 7; sel++)
            bad |= Switch(si, sel);
        Check("switch", bad);

        bad = 0;
        for (int m = 0; m < 4; m++)
        {
            bad |= TwoCalls(s, (m & 1) != 0, (m & 2) != 0);
            bad |= ChangeAfter(si, (m & 1) != 0, (m & 2) != 0) << 14;
        }
        Check("two calls", bad);

        bad = Loop(s, 0, 0) | Loop(s, 1, 0) | Loop(s, 100, 7) | Loop(s, 100, 0) | Loop(s, 33, 1) | Loop(s, 64, 63)
              | Loop(s, 50, 255);
        Check("loop", bad);
        Check("nested loop", Nested(s, 0) | Nested(s, 3) | Nested(s, 4) | Nested(s, 41));
        Check("loop exit", LoopExit(si, 10, 4) | LoopExit(si, 10, -1) | LoopExit(si, 10, 0) | LoopExit(si, 10, 9)
                           | LoopExit(si, 0, 0));

        Node[] ns = new Node[10];
        for (int i = 0; i < ns.Length; i++)
            ns[i] = new Node { V = 500 + i };
        ns[0].Any = new long[] { 0, 11, 22, 33 };
        string[] strs = { "a", "bc", "def" };
        Check("references", Refs(ns, strs, false) | Refs(ns, strs, true) | Refs(ns, strs, false));

        Check("reference loop", RefLoop(Lists(100), 100) | RefLoop(Lists(5), 5) << 8 | RefLoop(Lists(64), 64) << 16);

        bad = 0;
        for (int m = 0; m < 2; m++)
            bad |= Interior(s, new long[16], new Pair[3], m != 0);
        Check("interior pointers", bad);
        Check("moving references", RefWalk(Arrays(100), 100) | RefWalk(Arrays(1), 1) | RefWalk(Arrays(10), 10));

        Pair[] ps = new Pair[7];
        for (int i = 0; i < ps.Length; i++)
            ps[i] = new Pair { A = 700 + 2 * i, B = 701 + 2 * i };
        Check("struct fields", Structs(ps, false) | Structs(ps, true));

        bad = 0;
        for (int sel = 0; sel < 3; sel++)
            bad |= Params(100, 100, 102, 103, 104, 105, 106, sel);
        Check("parameters", bad);
        Check("stack parameters", StackParams(-7, -6, -5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, false)
                                  | StackParams(-7, -6, -5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, true));

        bad = 0;
        object[] store = new object[2];
        Node holder = new Node();
        for (int sel = 0; sel < 10; sel++)
            bad |= Helpers(s, "zz", store, holder, sel);
        if (store[0] != (object)"zz" || holder.Any != (object)"zz" || staticRef != (object)"zz") bad |= 1 << 15;
        Check("helpers", bad);

        Check("nested arguments", NestedArgs(s, false) | NestedArgs(s, true));

        bad = 0;
        for (int rep = 0; rep < 20; rep++)
            bad |= AboveCatch(s, rep % 4 - 1);
        Check("throw through", bad);
        Check("try catch", TryCatch(s, 0) | TryCatch(s, -1) | TryCatch(s, 9) | TryCatch(s, 0) | TryCatch(s, 77));
        Check("try finally", AboveFinally(si, 0) | AboveFinally(si, -1) | AboveFinally(si, 12) | AboveFinally(si, 0));

        Console.WriteLine(fails == 0 ? "ALL PASS" : "FAILED " + fails);
        return fails;
    }
}
