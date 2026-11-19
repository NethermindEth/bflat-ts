// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Null checks and stack probes that load into the zero register.
//
// A null check that needs no value is a load whose result is discarded.
// The compiler now probes an object reference with an aligned ld of its
// first word (the method table pointer) where it used lb or lw, probes
// "this" of an instance call the same way, touches the stack pages of a
// stackalloc with ld, and uses the zero-extending lbu, lhu and lwu for the
// narrow probes that are left (a field at an offset, an interior pointer,
// a struct "this"). What can go wrong: a probe that reads past the end of
// what the address points to (8 bytes through an interior pointer to the
// last byte of an array), a probe that is dropped or reads another address
// so that a null reference no longer faults, a probe that writes a real
// register, and a stack probe that misses a page or touches below the
// allocation.
//
// The first part runs every kind of check on valid references, among them
// interior pointers to the last bytes of objects, with live values in
// registers around the probe, and stackallocs of many sizes whose memory is
// then written and read back. It ends with ALL PASS or a FAIL count.
//
// The second part needs one build per case: with -d NULL_THIS, NULL_FIELD,
// NULL_ARRAY, NULL_POINTER, NULL_STRUCT_THIS, NULL_LENGTH or NULL_FIXED the
// program runs the same check on a null reference. A zkVM guest has no
// handler for the fault, so that build must die (SIGSEGV under qemu) before
// it prints "SURVIVED".
using System;
using System.Runtime.CompilerServices;

class Leaf
{
    public long A;
    public int I;
    public short S;
    public byte B;
    public byte Last;

    [MethodImpl(MethodImplOptions.NoInlining)] public long Twice(long x) => x * 2;   // does not touch this
    [MethodImpl(MethodImplOptions.NoInlining)] public long Add(long x) => A + x;
}

struct Pod
{
    public int X;
    public byte Y;

    [MethodImpl(MethodImplOptions.NoInlining)] public long Triple(long x) => x * 3;   // does not touch this
}

sealed class Holder { public Pod P; public byte Tail; }

unsafe class Program
{
    const MethodImplOptions NI = MethodImplOptions.NoInlining;

    static int fails;
    static int checks;
    static long sink;

    static void Eq(string name, long want, long got)
    {
        checks++;
        if (want == got) return;
        fails++;
        Console.WriteLine("FAIL " + name + " expected 0x" + want.ToString("x") + " got 0x" + got.ToString("x"));
    }

    [MethodImpl(NI)] static void Use(ref byte r) { sink += 1; }
    [MethodImpl(NI)] static void UseInt(ref int r) { sink += 1; }
    [MethodImpl(NI)] static void UseLong(ref long r) { sink += 1; }
    [MethodImpl(NI)] static void UsePod(ref Pod r) { sink += 1; }

    // ---- "this" of a call that does not use it ----

    [MethodImpl(NI)] static long ThisObject(Leaf o, long a, long b, long c) => o.Twice(a) + b * 3 + c * 5;
    [MethodImpl(NI)] static long ThisStruct(ref Pod p, long a, long b, long c) => p.Triple(a) + b * 3 + c * 5;
    [MethodImpl(NI)] static long ThisStructInObject(Holder h, long a, long b) => h.P.Triple(a) + b * 3;
    [MethodImpl(NI)] static long ThisStructInArray(Pod[] v, int i, long a) => v[i].Triple(a) + i;

    // ---- a field or an element whose value is not needed ----

    [MethodImpl(NI)] static long FieldLong(Leaf o, long a, long b) { _ = o.A; return a * 3 + b; }
    [MethodImpl(NI)] static long FieldInt(Leaf o, long a, long b) { _ = o.I; return a * 3 + b; }
    [MethodImpl(NI)] static long FieldShort(Leaf o, long a, long b) { _ = o.S; return a * 3 + b; }
    [MethodImpl(NI)] static long FieldByte(Leaf o, long a, long b) { _ = o.B; return a * 3 + b; }
    [MethodImpl(NI)] static long FieldLast(Leaf o, long a, long b) { _ = o.Last; return a * 3 + b; }
    [MethodImpl(NI)] static long FieldAddress(Leaf o, long a, long b) { Use(ref o.Last); return a * 3 + b; }
    [MethodImpl(NI)] static long FieldAddressInt(Leaf o, long a, long b) { UseInt(ref o.I); return a * 3 + b; }
    [MethodImpl(NI)] static long FieldAddressPod(Holder h, long a, long b) { UsePod(ref h.P); Use(ref h.Tail); return a * 3 + b; }
    [MethodImpl(NI)] static long ArrayLength(byte[] v, long a, long b) { _ = v.Length; return a * 3 + b; }
    [MethodImpl(NI)] static long StringLength(string s, long a, long b) { _ = s.Length; return a * 3 + b; }
    [MethodImpl(NI)] static long ArrayData(byte[] v, long a, long b) { fixed (byte* p = v) sink += (long)p & 0; return a * 3 + b; }
    [MethodImpl(NI)] static long ArrayFirst(byte[] v, long a, long b) { fixed (byte* p = &v[0]) sink += (long)p & 0; return a * 3 + b; }
    [MethodImpl(NI)] static long ArrayLast(byte[] v, long a, long b) { Use(ref v[v.Length - 1]); return a * 3 + b; }
    [MethodImpl(NI)] static long ArrayLastUnused(byte[] v, long a, long b) { _ = v[v.Length - 1]; return a * 3 + b; }
    [MethodImpl(NI)] static long ArrayLongLast(long[] v, long a, long b) { UseLong(ref v[v.Length - 1]); return a * 3 + b; }
    [MethodImpl(NI)] static long SpanOf(byte[] v, long a, long b) { Span<byte> s = v; return a * 3 + b + s.Length; }
    [MethodImpl(NI)] static long ObjectType(object o, long a, long b) { _ = o.GetType(); return a * 3 + b; }

    // ---- an interior pointer whose value is not needed ----
    // (C# drops the unused read through a ref parameter, so only the pointer forms are probes.)

    [MethodImpl(NI)] static long ByrefByte(ref byte r, long a, long b) { _ = r; return a * 3 + b; }
    [MethodImpl(NI)] static long ByrefShort(ref short r, long a, long b) { _ = r; return a * 3 + b; }
    [MethodImpl(NI)] static long ByrefInt(ref int r, long a, long b) { _ = r; return a * 3 + b; }
    [MethodImpl(NI)] static long ByrefLong(ref long r, long a, long b) { _ = r; return a * 3 + b; }
    [MethodImpl(NI)] static long PointerByte(byte* p, long a, long b) { _ = *p; return a * 3 + b; }
    [MethodImpl(NI)] static long PointerInt(int* p, long a, long b) { _ = *p; return a * 3 + b; }

    // Many values live across a probe.
    [MethodImpl(NI)]
    static long Crowded(Leaf o, byte[] v, ref byte r, long a, long b, long c, long d, long e)
    {
        long f = a * 3, g = b * 5, h = c * 7, i = d * 11, j = e * 13, k = a ^ b, l = c ^ d;
        _ = o.Twice(0);
        _ = v.Length;
        _ = r;
        _ = o.Last;
        Use(ref v[v.Length - 1]);
        return f + g + h + i + j + k + l + a + b + c + d + e;
    }

    // ---- stackalloc: every page is touched, the block is usable ----

    [MethodImpl(NI)]
    static long Alloc(int size, long seed)
    {
        byte* p = stackalloc byte[size];
        for (int i = 0; i < size; i += 61) p[i] = (byte)(seed + i);
        if (size > 0) p[size - 1] = (byte)(seed * 3);
        long s = 0;
        for (int i = 0; i < size; i += 61) s = s * 31 + p[i];
        return size > 0 ? s + p[size - 1] : s;
    }

    [MethodImpl(NoOptAlloc)]
    static long AllocRef(int size, long seed)
    {
        byte[] p = new byte[size];
        for (int i = 0; i < size; i += 61) p[i] = (byte)(seed + i);
        if (size > 0) p[size - 1] = (byte)(seed * 3);
        long s = 0;
        for (int i = 0; i < size; i += 61) s = s * 31 + p[i];
        return size > 0 ? s + p[size - 1] : s;
    }

    const MethodImplOptions NoOptAlloc = MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization;

    // Without zero initialization the pages of the block are only touched by the probe.
    [MethodImpl(NI), SkipLocalsInit]
    static long AllocRaw(int size, long seed)
    {
        byte* p = stackalloc byte[size];
        for (int i = 0; i < size; i += 61) p[i] = (byte)(seed + i);
        if (size > 0) p[size - 1] = (byte)(seed * 3);
        long s = 0;
        for (int i = 0; i < size; i += 61) s = s * 31 + p[i];
        return size > 0 ? s + p[size - 1] : s;
    }

    [MethodImpl(NI), SkipLocalsInit] static long AllocRaw16() { long* p = stackalloc long[2]; p[0] = 3; p[1] = 5; return p[0] * p[1]; }
    [MethodImpl(NI), SkipLocalsInit] static long AllocRaw4096() { byte* p = stackalloc byte[4096]; p[0] = 3; p[4095] = 5; return p[0] * p[4095]; }
    [MethodImpl(NI), SkipLocalsInit] static long AllocRaw9000() { byte* p = stackalloc byte[9000]; p[0] = 3; p[4096] = 7; p[8999] = 5; return p[0] * p[8999] * p[4096]; }
    [MethodImpl(NI), SkipLocalsInit] static long AllocRaw20000() { byte* p = stackalloc byte[20000]; p[0] = 3; p[10000] = 7; p[19999] = 5; return p[0] * p[19999] * p[10000]; }

    [MethodImpl(NI), SkipLocalsInit]
    static long AllocRawTwice(int n, int m, long seed)
    {
        long* p = stackalloc long[n];
        long* q = stackalloc long[m];
        for (int i = 0; i < n; i++) p[i] = seed + i;
        for (int i = 0; i < m; i++) q[i] = seed * 3 + i;
        long s = 0;
        for (int i = 0; i < n; i++) s += p[i];
        for (int i = 0; i < m; i++) s ^= q[i];
        return s;
    }

    [MethodImpl(NI)] static long AllocConst16() { long* p = stackalloc long[2]; p[0] = 3; p[1] = 5; return p[0] * p[1]; }
    [MethodImpl(NI)] static long AllocConst4096() { byte* p = stackalloc byte[4096]; p[0] = 3; p[4095] = 5; return p[0] * p[4095]; }
    [MethodImpl(NI)] static long AllocConst9000() { byte* p = stackalloc byte[9000]; p[0] = 3; p[4096] = 7; p[8999] = 5; return p[0] * p[8999] * p[4096]; }

    [MethodImpl(NI)]
    static long AllocTwice(int n, int m, long seed)
    {
        long* p = stackalloc long[n];
        long* q = stackalloc long[m];
        for (int i = 0; i < n; i++) p[i] = seed + i;
        for (int i = 0; i < m; i++) q[i] = seed * 3 + i;
        long s = 0;
        for (int i = 0; i < n; i++) s += p[i];
        for (int i = 0; i < m; i++) s ^= q[i];
        return s;
    }

    [MethodImpl(NoOptAlloc)]
    static long AllocTwiceRef(int n, int m, long seed)
    {
        long s = 0;
        for (int i = 0; i < n; i++) s += seed + i;
        for (int i = 0; i < m; i++) s ^= seed * 3 + i;
        return s;
    }

    // ---- driver ----

    [MethodImpl(NI)] static T Null<T>() where T : class => null;
    [MethodImpl(NI)] static int* NullPointer() => null;

    static int Main()
    {
#if NULL_THIS
        Console.WriteLine(ThisObject(Null<Leaf>(), 1, 2, 3));
        Console.WriteLine("SURVIVED");
        return 1;
#elif NULL_FIELD
        Console.WriteLine(FieldLast(Null<Leaf>(), 1, 2));
        Console.WriteLine("SURVIVED");
        return 1;
#elif NULL_ARRAY
        Console.WriteLine(ArrayData(Null<byte[]>(), 1, 2) + ArrayLast(Null<byte[]>(), 1, 2));
        Console.WriteLine("SURVIVED");
        return 1;
#elif NULL_POINTER
        Console.WriteLine(PointerInt(NullPointer(), 1, 2));
        Console.WriteLine("SURVIVED");
        return 1;
#elif NULL_STRUCT_THIS
        Console.WriteLine(ThisStructInObject(Null<Holder>(), 1, 2));
        Console.WriteLine("SURVIVED");
        return 1;
#elif NULL_LENGTH
        Console.WriteLine(ArrayLength(Null<byte[]>(), 1, 2) + StringLength(Null<string>(), 1, 2));
        Console.WriteLine("SURVIVED");
        return 1;
#elif NULL_FIXED
        Console.WriteLine(FieldAddress(Null<Leaf>(), 1, 2));
        Console.WriteLine("SURVIVED");
        return 1;
#else
        Leaf leaf = new Leaf { A = 7 };
        Holder holder = new Holder();
        Pod pod = default;
        Pod[] pods = new Pod[3];
        long x = 0x1111111111111111L, y = 0x2222222222222222L, z = 0x4444444444444444L;
        long want = x * 3 + y;

        Eq("this, object", x * 2 + y * 3 + z * 5, ThisObject(leaf, x, y, z));
        Eq("this, struct", x * 3 + y * 3 + z * 5, ThisStruct(ref pod, x, y, z));
        Eq("this, struct in an object", x * 3 + y * 3, ThisStructInObject(holder, x, y));
        Eq("this, struct in an array", x * 3 + 2, ThisStructInArray(pods, 2, x));
        Eq("field, long", want, FieldLong(leaf, x, y));
        Eq("field, int", want, FieldInt(leaf, x, y));
        Eq("field, short", want, FieldShort(leaf, x, y));
        Eq("field, byte", want, FieldByte(leaf, x, y));
        Eq("field, last byte", want, FieldLast(leaf, x, y));
        Eq("field address, last byte", want, FieldAddress(leaf, x, y));
        Eq("field address, int", want, FieldAddressInt(leaf, x, y));
        Eq("field address, struct", want, FieldAddressPod(holder, x, y));
        Eq("object type", want, ObjectType(leaf, x, y));
        Eq("string length", want, StringLength("abc", x, y));
        Eq("string length, empty", want, StringLength("", x, y));

        // Arrays of every length from 0 to 17: the last element is the last byte of the object.
        for (int n = 0; n <= 17; n++)
        {
            byte[] v = new byte[n];
            long[] w = new long[n];
            Eq("array length", want, ArrayLength(v, x, y));
            Eq("array data", want, ArrayData(v, x, y));
            Eq("span of an array", want + n, SpanOf(v, x, y));
            if (n == 0) continue;
            Eq("array first", want, ArrayFirst(v, x, y));
            Eq("array last", want, ArrayLast(v, x, y));
            Eq("array last, unused", want, ArrayLastUnused(v, x, y));
            Eq("long array last", want, ArrayLongLast(w, x, y));
            Eq("byref byte, last", want, ByrefByte(ref v[n - 1], x, y));
            Eq("byref long, last", want, ByrefLong(ref w[n - 1], x, y));
            fixed (byte* p = v)
                Eq("pointer byte, last", want, PointerByte(p + n - 1, x, y));
            Eq("crowded", x * 3 + y * 5 + z * 7 + x * 11 + y * 13 + (x ^ y) + (z ^ x) + x + y + z + x + y,
               Crowded(leaf, v, ref v[n - 1], x, y, z, x, y));
        }

        short[] shorts = new short[3];
        int[] ints = new int[3];
        Eq("byref short, last", want, ByrefShort(ref shorts[2], x, y));
        Eq("byref int, last", want, ByrefInt(ref ints[2], x, y));
        Eq("byref int, field", want, ByrefInt(ref leaf.I, x, y));
        Eq("byref byte, last field", want, ByrefByte(ref leaf.Last, x, y));
        fixed (int* p = ints)
            Eq("pointer int, last", want, PointerInt(p + 2, x, y));

        int[] sizes = { 0, 1, 7, 8, 9, 15, 16, 17, 31, 32, 33, 100, 1000, 2047, 2048, 2049, 4080, 4095, 4096, 4097, 4112, 8191, 8192, 8193, 12288, 16000 };
        foreach (int size in sizes)
            for (long seed = 1; seed <= 3; seed++)
            {
                Eq("stackalloc " + size, AllocRef(size, seed), Alloc(size, seed));
                Eq("stackalloc, not zeroed " + size, AllocRef(size, seed), AllocRaw(size, seed));
            }
        Eq("stackalloc 16", 15, AllocConst16());
        Eq("stackalloc 4096", 15, AllocConst4096());
        Eq("stackalloc 9000", 105, AllocConst9000());
        for (int n = 0; n <= 600; n += 150)
            for (int m = 1; m <= 700; m += 233)
            {
                Eq("two stackallocs", AllocTwiceRef(n, m, 77), AllocTwice(n, m, 77));
                Eq("two stackallocs, not zeroed", AllocTwiceRef(n, m, 77), AllocRawTwice(n, m, 77));
            }
        Eq("stackalloc 16, not zeroed", 15, AllocRaw16());
        Eq("stackalloc 4096, not zeroed", 15, AllocRaw4096());
        Eq("stackalloc 9000, not zeroed", 105, AllocRaw9000());
        Eq("stackalloc 20000, not zeroed", 105, AllocRaw20000());

        Console.WriteLine(fails == 0 ? "ALL PASS (" + checks + " checks)" : "FAILED " + fails + " of " + checks);
        return fails == 0 ? 0 : 1;
#endif
    }
}
