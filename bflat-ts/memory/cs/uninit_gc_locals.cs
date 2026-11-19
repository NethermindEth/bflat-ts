// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Object references in stack slots that the prolog does not zero, and
// exceptions through frames that have no GC info.
//
// The collector of a zkVM guest never scans a stack. With JitZkNoGcMustInit
// the prolog zeroes a slot that holds a reference only where it would zero
// any other slot, and every place that used to rely on the prolog (a struct
// set to default, a local of an inlined method, an object allocated on the
// stack, a local of a tail call turned into a loop, a local read by a
// finally) has to zero it itself. With IlcZkNoGcInfo the tables an exception
// reads end before the GC info of each method. What can go wrong: a
// reference that is null by the rules of the language reads as whatever the
// stack held, and an exception that is not dispatched to its handler.
//
// The stack is filled with a pattern that is not a valid reference before
// each case. The references are assigned on some paths only and the test
// checks which of them are null: in straight code, in loops where the slot
// was written on the turn before, in structs of three and of twenty-four
// references, in inlined methods, in objects and arrays that do not leave
// the method, across try/finally, a filter and a catch, in frames an
// exception passes through, in shared generic code, and in a method called
// from native code. Some cases are there twice: with the locals zeroed by
// the language and with [SkipLocalsInit].
//
// The heap is not collected, so the heap case checks only what a program
// can see: a new object is zeroed and an old one keeps its contents.
//
// Each case prints one line; the run ends with ALL PASS or a FAIL count, and
// the guest returns the number of failures.
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

class E : Exception { public int Code; public E(int c) : base("e") { Code = c; } }

class GE<T> : Exception { public T Val; public GE(T v) : base("ge") { Val = v; } }

class Node { public long V; public Node Next; public object Any; }

struct RS { public object A; public long N; public string S; }

// More fields than the JIT promotes.
struct Mixed { public Node A; public int X; public Node B; public long Y; public string S; public object O; }

struct Refs24
{
    public object R0, R1, R2, R3, R4, R5, R6, R7, R8, R9, R10, R11;
    public object R12, R13, R14, R15, R16, R17, R18, R19, R20, R21, R22, R23;
}

// A struct with references that a finally disposes.
struct Walker : IDisposable
{
    public Node Head, Cur;
    public int Steps;
    public static int Disposed;

    public bool MoveNext()
    {
        Cur = Steps == 0 ? Head : Cur.Next;
        Steps++;
        return Cur != null;
    }

    public void Dispose() { Disposed += Cur == null ? Steps : -Steps; }
}

static unsafe class Program
{
    static int fails;
    static long sink;

    static void Check(string name, int bad)
    {
        Console.WriteLine((bad == 0 ? "ok   " : "FAIL ") + name + (bad == 0 ? "" : " cases=0x" + bad.ToString("x")));
        if (bad != 0) fails++;
    }

    // Leaves a pattern that is not a reference in the stack below the caller.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Dirty()
    {
        Span<long> s = stackalloc long[1536];
        s.Fill(0x5A5A5A5A5A5A5A5B);
        sink += s[777];
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

    // Overwrites the registers, then throws for a positive code.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Thrower(int code)
    {
        Clobber(code);
        if (code > 0) throw new E(code);
        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Node Make(long v) => new Node { V = v };

    // Which of the four are not null. A reference is only compared here,
    // never followed, so a slot with garbage gives a wrong bit, not a crash.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Bits(object a, object b, object c, object d)
        => (a != null ? 1 : 0) | (b != null ? 2 : 0) | (c != null ? 4 : 0) | (d != null ? 8 : 0);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Count(ref Refs24 r)
    {
        int n = 0;
        for (int i = 0; i < 24; i++)
            if (Unsafe.Add(ref r.R0, i) != null) n++;
        return n;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Touch(ref Mixed m) { sink += m.X; }

    // ---- references assigned on some paths ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int CondAssign(Node src, int sel)
    {
        Node a = null, b = null;
        string s = null;
        object o = null;
        long[] arr = null;
        Node[] ns = null;
        if ((sel & 1) != 0) a = src;
        if ((sel & 2) != 0) { b = Make(sel); s = "x"; }
        Clobber(sel);
        if ((sel & 4) != 0) { o = src; arr = new long[2]; }
        if ((sel & 8) != 0) ns = new Node[1];
        int got = Bits(a, b, s, o) | Bits(arr, ns, null, null) << 4;
        int want = (sel & 1) | (sel & 2) | (sel & 2) << 1 | (sel & 4) << 1 | (sel & 4) << 2 | (sel & 8) << 2;
        return got ^ want;
    }

    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int CondAssignSkip(Node src, int sel)
    {
        Node a = null, b = null;
        string s = null;
        object o = null;
        long[] arr = null;
        Node[] ns = null;
        if ((sel & 1) != 0) a = src;
        if ((sel & 2) != 0) { b = Make(sel); s = "x"; }
        Clobber(sel);
        if ((sel & 4) != 0) { o = src; arr = new long[2]; }
        if ((sel & 8) != 0) ns = new Node[1];
        int got = Bits(a, b, s, o) | Bits(arr, ns, null, null) << 4;
        int want = (sel & 1) | (sel & 2) | (sel & 2) << 1 | (sel & 4) << 1 | (sel & 4) << 2 | (sel & 8) << 2;
        return got ^ want;
    }

    // ---- structs set to default on every turn of a loop ----

    // The turn before leaves references and numbers in both structs.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int StructLoop(Node src, int n)
    {
        int bad = 0;
        for (int i = 0; i < n; i++)
        {
            RS r = default;
            Mixed m = default;
            if ((i & 1) != 0) { r.A = src; m.B = src; }
            if ((i & 2) != 0) { r.S = "s"; m.O = r.S; m.X = i; }
            Clobber(i);
            Touch(ref m);
            int got = Bits(r.A, r.S, m.A, m.B) | Bits(m.S, m.O, null, null) << 4;
            int want = (i & 1) | (i & 2) | (i & 1) << 3 | (i & 2) << 4;
            if (got != want || r.N != 0 || m.Y != 0 || m.X != ((i & 2) != 0 ? i : 0)) bad |= 1 << (i & 7);
            r.N = i + 1;
            m.Y = i + 1;
            m.A = src;
            m.S = "t";
            Touch(ref m);
            sink += r.N;
        }
        return bad;
    }

    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int StructLoopSkip(Node src, int n)
    {
        int bad = 0;
        for (int i = 0; i < n; i++)
        {
            RS r = default;
            Mixed m = default;
            if ((i & 1) != 0) { r.A = src; m.B = src; }
            if ((i & 2) != 0) { r.S = "s"; m.O = r.S; m.X = i; }
            Clobber(i);
            Touch(ref m);
            int got = Bits(r.A, r.S, m.A, m.B) | Bits(m.S, m.O, null, null) << 4;
            int want = (i & 1) | (i & 2) | (i & 1) << 3 | (i & 2) << 4;
            if (got != want || r.N != 0 || m.Y != 0 || m.X != ((i & 2) != 0 ? i : 0)) bad |= 1 << (i & 7);
            r.N = i + 1;
            m.Y = i + 1;
            m.A = src;
            m.S = "t";
            Touch(ref m);
            sink += r.N;
        }
        return bad;
    }

    // ---- a frame with many reference slots ----

    // One slot of the twenty-four is set on a turn, two on every fourth.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ManyRefs(Node src, int n)
    {
        int bad = 0;
        for (int i = 0; i < n; i++)
        {
            Refs24 r = default;
            Unsafe.Add(ref r.R0, i % 24) = src;
            if ((i & 3) == 3) Unsafe.Add(ref r.R0, (i + 7) % 24) = "z";
            Clobber(i);
            if (Count(ref r) != ((i & 3) == 3 ? 2 : 1) || Unsafe.Add(ref r.R0, i % 24) != src) bad |= 1 << (i & 7);
        }
        Refs24 q = new Refs24();
        q.R23 = src;
        Refs24 copy = q;
        if (Count(ref copy) != 1 || copy.R0 != null || copy.R23 != src) bad |= 1 << 8;
        return bad;
    }

    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ManyRefsSkip(Node src, int n)
    {
        int bad = 0;
        for (int i = 0; i < n; i++)
        {
            Refs24 r = default;
            Unsafe.Add(ref r.R0, i % 24) = src;
            if ((i & 3) == 3) Unsafe.Add(ref r.R0, (i + 7) % 24) = "z";
            Clobber(i);
            if (Count(ref r) != ((i & 3) == 3 ? 2 : 1) || Unsafe.Add(ref r.R0, i % 24) != src) bad |= 1 << (i & 7);
        }
        Refs24 q = new Refs24();
        q.R23 = src;
        Refs24 copy = q;
        if (Count(ref copy) != 1 || copy.R0 != null || copy.R23 != src) bad |= 1 << 8;
        return bad;
    }

    // ---- locals of inlined methods ----

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int Pick(Node src, int i)
    {
        Node t = null;
        RS r = default;
        Mixed m = default;
        if ((i & 1) != 0) { t = src; r.S = "q"; m.A = src; }
        return Bits(t, r.S, r.A, m.A) | Bits(m.B, m.S, m.O, null) << 4;
    }

    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int PickSkip(Node src, int i)
    {
        Node t = null;
        RS r = default;
        Mixed m = default;
        if ((i & 1) != 0) { t = src; r.S = "q"; m.A = src; }
        return Bits(t, r.S, r.A, m.A) | Bits(m.B, m.S, m.O, null) << 4;
    }

    // The locals of the inlined method are locals of the loop body here.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Inlinees(Node src, int n)
    {
        int bad = 0;
        for (int i = 0; i < n; i++)
        {
            int want = (i & 1) != 0 ? 1 | 2 | 8 : 0;
            if (Pick(src, i) != want) bad |= 1;
            Clobber(i);
            if (PickSkip(src, i) != want) bad |= 2;
        }
        return bad;
    }

    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int InlineesSkip(Node src, int n)
    {
        int bad = 0;
        for (int i = 0; i < n; i++)
        {
            int want = (i & 1) != 0 ? 1 | 2 | 8 : 0;
            if (Pick(src, i) != want) bad |= 1;
            Clobber(i);
            if (PickSkip(src, i) != want) bad |= 2;
        }
        return bad;
    }

    // ---- objects that do not leave the method ----

    // The JIT may put the node and the array on the stack: a new one on
    // every turn has to be zeroed like one from the heap.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int StackObjects(Node src, int n)
    {
        int bad = 0;
        for (int i = 0; i < n; i++)
        {
            Node node = new Node();
            int[] t = new int[8];
            bool odd = (i & 1) != 0;
            if (Bits(node.Next, node.Any, null, null) != 0 || node.V != 0) bad |= 1;
            if (odd) { node.Next = src; node.Any = "a"; node.V = i; }
            t[i & 7] += 1;
            int sum = 0;
            for (int k = 0; k < t.Length; k++) sum += t[k];
            Clobber(i);
            if (Bits(node.Next, node.Any, null, null) != (odd ? 3 : 0) || node.V != (odd ? i : 0) || sum != 1) bad |= 2;
            node.V = -1;
            node.Any = src;
        }
        return bad;
    }

    // ---- a tail call that becomes a loop ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Tail(Node src, int n, int bad)
    {
        RS r = default;
        Node t = null;
        if ((n & 1) != 0) { r.A = src; t = src; }
        if (Bits(r.A, t, r.S, null) != ((n & 1) != 0 ? 3 : 0) || r.N != 0) bad |= 1;
        r.N = n + 1;
        r.S = "u";
        sink += r.N + r.S.Length;
        if (n == 0) return bad;
        return Tail(src, n - 1, bad);
    }

    // ---- handlers that read the references ----

    static int finSeen, finRuns, finBad;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Fin(Node src, int sel, int code)
    {
        Node a = null;
        string s = null;
        RS r = default;
        object o = null;
        try
        {
            if ((sel & 1) != 0) a = src;
            Thrower(code);
            if ((sel & 2) != 0) s = "t";
            if ((sel & 4) != 0) { r.A = src; o = s; }
        }
        finally
        {
            finSeen = Bits(a, s, r.A, r.S) | Bits(o, null, null, null) << 4;
        }
        return Bits(a, s, r.A, r.S) | Bits(o, null, null, null) << 4;
    }

    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int FinSkip(Node src, int sel, int code)
    {
        Node a = null;
        string s = null;
        RS r = default;
        object o = null;
        try
        {
            if ((sel & 1) != 0) a = src;
            Thrower(code);
            if ((sel & 2) != 0) s = "t";
            if ((sel & 4) != 0) { r.A = src; o = s; }
        }
        finally
        {
            finSeen = Bits(a, s, r.A, r.S) | Bits(o, null, null, null) << 4;
        }
        return Bits(a, s, r.A, r.S) | Bits(o, null, null, null) << 4;
    }

    static int FinCase(Node src, bool skip)
    {
        int bad = 0;
        for (int sel = 0; sel < 8; sel++)
        {
            int all = (sel & 1) | (sel & 2) | (sel & 4) | ((sel & 6) == 6 ? 16 : 0);
            Dirty();
            finSeen = -1;
            int got = skip ? FinSkip(src, sel, 0) : Fin(src, sel, 0);
            if (got != all || finSeen != all) bad |= 1;
            Dirty();
            finSeen = -1;
            got = -1;
            try { got = skip ? FinSkip(src, sel, 9) : Fin(src, sel, 9); } catch (E e) { got = -e.Code; }
            if (got != -9 || finSeen != (sel & 1)) bad |= 2;
        }
        return bad;
    }

    // The filter runs before the frame is unwound and reads its locals.
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Filter(Node src, int sel, int code)
    {
        Node a = null;
        object o = null;
        Mixed m = default;
        int got = 0;
        try
        {
            if ((sel & 1) != 0) a = src;
            Thrower(code);
            if ((sel & 2) != 0) { o = src; m.S = "m"; }
        }
        catch (E e) when (e.Code == code && Bits(a, o, m.S, m.A) == (sel & 1))
        {
            got = 0x100;
        }
        return got | Bits(a, o, m.S, m.A);
    }

    // ---- frames an exception passes through ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Through(Node src, int depth, int code)
    {
        Refs24 r = default;
        Node a = (depth & 1) != 0 ? src : null;
        RS s = default;
        Unsafe.Add(ref r.R0, depth % 24) = src;
        if ((depth & 2) != 0) s.S = "d";
        int below = depth == 0 ? Thrower(code) : Through(src, depth - 1, code);
        return below | (Count(ref r) == 1 && Bits(a, s.S, s.A, null) == ((depth & 1) | (depth & 2)) ? 0 : 1);
    }

    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ThroughFin(Node src, int depth, int code)
    {
        Refs24 r = default;
        Node a = null;
        try
        {
            if ((depth & 1) != 0) a = src;
            Unsafe.Add(ref r.R0, depth) = src;
            return depth == 0 ? Through(src, 5, code) : ThroughFin(src, depth - 1, code);
        }
        finally
        {
            finRuns++;
            if (Count(ref r) != 1 || (a != null) != ((depth & 1) != 0)) finBad++;
        }
    }

    // A catch that throws again, with references of its own.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Rethrow(Node src, int depth, int code)
    {
        Node a = null;
        Mixed m = default;
        try
        {
            if ((depth & 1) != 0) { a = src; m.O = src; }
            return depth == 0 ? ThroughFin(src, 4, code) : Rethrow(src, depth - 1, code);
        }
        catch (E e)
        {
            if (Bits(a, m.O, m.A, m.S) != ((depth & 1) != 0 ? 3 : 0)) finBad++;
            finRuns += 100;
            if ((depth & 1) != 0) throw;
            throw new E(e.Code + 1);
        }
    }

    // ---- structs that come back from a call ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static RS MakeRS(Node src, int i)
    {
        RS r = default;
        if ((i & 1) != 0) r.A = src;
        if ((i & 2) != 0) r.S = "r";
        r.N = i;
        return r;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void OutMixed(Node src, int i, out Mixed m, out Node n)
    {
        m = default;
        n = null;
        if ((i & 1) != 0) { m.B = src; n = src; }
    }

    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Returned(Node src, int n)
    {
        int bad = 0;
        for (int i = 0; i < n; i++)
        {
            RS r = MakeRS(src, i);
            OutMixed(src, i, out Mixed m, out Node nd);
            Clobber(i);
            if (Bits(r.A, r.S, m.A, m.B) != ((i & 1) | (i & 2) | (i & 1) << 3) || r.N != i) bad |= 1;
            if (Bits(nd, m.S, m.O, null) != (i & 1) || m.X != 0 || m.Y != 0) bad |= 2;
        }
        return bad;
    }

    // ---- shared generic code ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static T ThrowG<T>(T v, int code) where T : class
    {
        if (code > 0) throw new GE<T>(v);
        return null;
    }

    // The type of the catch depends on T.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Gen<T>(T src, int sel, int code) where T : class
    {
        T a = default;
        KeyValuePair<T, int> kv = default;
        T[] arr = null;
        T got = null;
        if ((sel & 1) != 0) a = src;
        if ((sel & 2) != 0) kv = new KeyValuePair<T, int>(src, 5);
        try
        {
            ThrowG<T>(src, code);
            if ((sel & 4) != 0) arr = new T[2];
        }
        catch (GE<T> e)
        {
            got = e.Val;
        }
        int want = (sel & 1) | (sel & 2) | (code > 0 ? 8 : sel & 4);
        if (Bits(a, kv.Key, arr, got) != want) return 1;
        if (got != null && (object)got != (object)src) return 2;
        return kv.Value == ((sel & 2) != 0 ? 5 : 0) ? 0 : 4;
    }

    // ---- spans and stackalloc ----

    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Spans(long[] arr, int sel)
    {
        Span<long> sp = default;
        ReadOnlySpan<char> rc = default;
        Span<long> st = (sel & 4) != 0 ? stackalloc long[16] : default;
        if ((sel & 1) != 0) sp = arr;
        if ((sel & 2) != 0) rc = "hello".AsSpan(1, 3);
        for (int i = 0; i < st.Length; i++) st[i] = i + sel;
        Clobber(sel);
        long sum = 0, arrSum = 0;
        for (int i = 0; i < st.Length; i++) sum += st[i];
        for (int i = 0; i < sp.Length; i++) sum += sp[i];
        for (int i = 0; i < arr.Length; i++) arrSum += arr[i];
        int bad = 0;
        if (sp.Length != ((sel & 1) != 0 ? arr.Length : 0)) bad |= 1;
        if (rc.Length != ((sel & 2) != 0 ? 3 : 0) || (rc.Length != 0 && (rc[0] != 'e' || rc[2] != 'l'))) bad |= 2;
        if (st.Length != ((sel & 4) != 0 ? 16 : 0)) bad |= 4;
        if (sum != ((sel & 4) != 0 ? 120 + 16 * sel : 0) + ((sel & 1) != 0 ? arrSum : 0)) bad |= 8;
        return bad;
    }

    // Without [SkipLocalsInit] a stackalloc block is zeroed.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int StackallocZero(int n)
    {
        Span<long> st = stackalloc long[n];
        Span<byte> sb = stackalloc byte[100];
        int bad = 0;
        for (int i = 0; i < st.Length; i++)
            if (st[i] != 0) bad |= 1;
        for (int i = 0; i < sb.Length; i++)
            if (sb[i] != 0) bad |= 2;
        st.Fill(-1);
        sb.Fill(0xFF);
        sink += st[n - 1] + sb[99];
        return bad;
    }

    // ---- a struct with references in a using ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Walk(Node head, int stop)
    {
        int sum = 0;
        using (Walker w = new Walker { Head = head })
        {
            while (w.MoveNext())
            {
                sum += (int)w.Cur.V;
                if (w.Steps == stop) Thrower(stop);
            }
        }
        return sum;
    }

    // ---- references in spill temps ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Join14(Node a0, string a1, long[] a2, Node a3, string a4, long[] a5, Node a6,
                       string a7, long[] a8, Node a9, string a10, long[] a11, Node a12, object a13)
        => a0.V + a1.Length * 2 + a2.Length * 3 + a3.V * 4 + a4.Length * 5 + a5.Length * 6 + a6.V * 7
           + a7.Length * 8 + a8.Length * 9 + a9.V * 10 + a10.Length * 11 + a11.Length * 12 + a12.V * 13
           + (a13 == null ? 14 : 0);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static string Str(int n) => new string('s', n);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long[] Arr(int n) => new long[n];

    [MethodImpl(MethodImplOptions.NoInlining)]
    static object Null(int n) => n > 100 ? "x" : null;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Sum2(Node a, string s, long rest) => a.V + s.Length + rest;

    // Results of calls wait for later calls: as arguments of one call, and
    // as the first arguments of fourteen nested calls, where each waits in
    // a register or a spill temp until the innermost call is back.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int SpillTemps(int k)
    {
        long got = Join14(Make(k), Str(k + 1), Arr(k + 2), Make(k + 3), Str(k + 4), Arr(k + 5), Make(k + 6),
                          Str(k + 7), Arr(k + 8), Make(k + 9), Str(k + 10), Arr(k + 11), Make(k + 12), Null(k));
        long want = 14;
        for (int i = 0; i < 13; i++)
            want += (long)(k + i) * (i + 1);
        int bad = got == want ? 0 : 1;

        got = Sum2(Make(k), Str(1), Sum2(Make(k + 1), Str(2), Sum2(Make(k + 2), Str(3), Sum2(Make(k + 3), Str(4),
              Sum2(Make(k + 4), Str(5), Sum2(Make(k + 5), Str(6), Sum2(Make(k + 6), Str(7), Sum2(Make(k + 7), Str(8),
              Sum2(Make(k + 8), Str(9), Sum2(Make(k + 9), Str(10), Sum2(Make(k + 10), Str(11),
              Sum2(Make(k + 11), Str(12), Sum2(Make(k + 12), Str(13), Sum2(Make(k + 13), Str(14), Clobber(k)))))))))))))));
        if (got != 14L * k + 91 + 105 + k + 1) bad |= 2;
        return bad;
    }

    // ---- a method called from native code ----

    // Such a method keeps its GC info: the unwinder reads its frame from it.
    [UnmanagedCallersOnly]
    static int FromNative(int code)
    {
        Node a = null;
        RS r = default;
        try
        {
            if (code > 0) { a = Make(code); r.S = "n"; }
            Thrower(code);
            return 1 + Bits(a, r.S, r.A, null);
        }
        catch (E e)
        {
            return e.Code + 1000 * Bits(a, r.S, r.A, null);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Reverse()
    {
        delegate* unmanaged<int, int> f = &FromNative;
        int bad = 0;
        if (f(0) != 1) bad |= 1;
        if (f(5) != 3005) bad |= 2;
        if (f(-1) != 1) bad |= 4;
        return bad;
    }

    // ---- the heap ----

    // Nothing is collected: a new object is zeroed, an old one is intact.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Heap()
    {
        int bad = 0;
        Node head = null;
        for (int i = 0; i < 20000; i++)
        {
            Node n = new Node();
            if (n.V != 0 || n.Next != null || n.Any != null) bad |= 1;
            n.V = i;
            n.Next = head;
            n.Any = n;
            head = n;
            if ((i & 4095) == 4095) GC.Collect();
        }
        int count = 0;
        for (Node n = head; n != null; n = n.Next, count++)
            if (n.V != 19999 - count || n.Any != n) bad |= 2;
        if (count != 20000) bad |= 4;

        // Blocks that are dropped at once, and a window of them that is kept.
        byte[][] keep = new byte[32][];
        for (int i = 0; i < 400; i++)
        {
            byte[] b = new byte[2048];
            int sum = 0;
            for (int k = 0; k < b.Length; k++) sum |= b[k];
            if (sum != 0) bad |= 8;
            b.AsSpan().Fill((byte)(i + 1));
            keep[i & 31] = b;
            if (i >= 31)
            {
                byte[] old = keep[(i + 1) & 31];
                if (old[0] != (byte)(i - 30) || old[2047] != (byte)(i - 30)) bad |= 16;
            }
        }
        GC.Collect();

        string s = "";
        for (int i = 0; i < 100; i++)
            s += (char)('a' + i % 26);
        if (s.Length != 100 || s[0] != 'a' || s[99] != 'v' || s[26] != 'a') bad |= 32;
        return bad;
    }

    // ---- driver ----

    static int Main()
    {
        Node src = new Node { V = 7 };
        int bad = 0, badSkip = 0;

        for (int sel = 0; sel < 16; sel++)
        {
            Dirty();
            bad |= CondAssign(src, sel);
            Dirty();
            badSkip |= CondAssignSkip(src, sel);
        }
        Check("assigned on some paths", bad);
        Check("assigned on some paths, skip init", badSkip);

        Dirty();
        Check("struct in a loop", StructLoop(src, 16));
        Dirty();
        Check("struct in a loop, skip init", StructLoopSkip(src, 16));

        Dirty();
        Check("many references", ManyRefs(src, 50));
        Dirty();
        Check("many references, skip init", ManyRefsSkip(src, 50));

        Dirty();
        Check("inlined locals", Inlinees(src, 12));
        Dirty();
        Check("inlined locals, skip init", InlineesSkip(src, 12));

        Dirty();
        Check("objects on the stack", StackObjects(src, 20));
        Dirty();
        Check("tail call", Tail(src, 21, 0));

        Check("finally", FinCase(src, false));
        Check("finally, skip init", FinCase(src, true));

        bad = 0;
        for (int sel = 0; sel < 4; sel++)
        {
            Dirty();
            if (Filter(src, sel, 0) != ((sel & 1) | (sel & 2) | (sel & 2) << 1)) bad |= 1;
            Dirty();
            if (Filter(src, sel, 6) != (0x100 | (sel & 1))) bad |= 2;
        }
        Check("filter", bad);

        bad = 0;
        Dirty();
        if (Through(src, 9, 0) != 0) bad |= 1;
        int code = 0;
        Dirty();
        try { Through(src, 9, 21); } catch (E e) { code = e.Code; }
        if (code != 21) bad |= 2;
        finRuns = 0; finBad = 0; code = 0;
        Dirty();
        if (ThroughFin(src, 6, 0) != 0 || finRuns != 7 || finBad != 0) bad |= 4;
        finRuns = 0;
        Dirty();
        try { ThroughFin(src, 6, 22); } catch (E e) { code = e.Code; }
        if (code != 22 || finRuns != 7 || finBad != 0) bad |= 8;
        // Four catches on the way up: two pass the exception on, two
        // replace it with one whose code is one more.
        finRuns = 0; code = 0;
        Dirty();
        try { Rethrow(src, 3, 30); } catch (E e) { code = e.Code; }
        if (code != 32 || finRuns != 405 || finBad != 0) bad |= 16;
        finRuns = 0;
        if (Rethrow(src, 3, 0) != 0 || finRuns != 5 || finBad != 0) bad |= 32;
        Check("exception through frames", bad);

        Dirty();
        Check("returned structs", Returned(src, 12));

        bad = 0;
        for (int sel = 0; sel < 8; sel++)
        {
            Dirty();
            bad |= Gen<Node>(src, sel, 0) | Gen<Node>(src, sel, 3) << 4;
            Dirty();
            bad |= Gen<string>("g", sel, 0) << 8 | Gen<string>("g", sel, 4) << 12;
        }
        Check("generic", bad);

        bad = 0;
        long[] arr = { 1, 2, 3, 4, 5 };
        for (int sel = 0; sel < 8; sel++)
        {
            Dirty();
            bad |= Spans(arr, sel);
        }
        Dirty();
        bad |= StackallocZero(64) << 4;
        Check("spans and stackalloc", bad);

        bad = 0;
        Node list = null;
        for (int i = 9; i >= 0; i--)
            list = new Node { V = i, Next = list };
        Walker.Disposed = 0;
        Dirty();
        if (Walk(list, 0) != 45 || Walker.Disposed != 11) bad |= 1;
        code = 0;
        Dirty();
        try { Walk(list, 4); } catch (E e) { code = e.Code; }
        if (code != 4 || Walker.Disposed != 11 - 4) bad |= 2;
        Check("using", bad);

        bad = 0;
        for (int k = 1; k < 6; k++)
        {
            Dirty();
            bad |= SpillTemps(k);
        }
        Check("references in spill temps", bad);

        Dirty();
        Check("called from native code", Reverse());

        Dirty();
        Check("heap", Heap());

        Console.WriteLine(fails == 0 ? "ALL PASS" : "FAILED " + fails);
        return fails;
    }
}
