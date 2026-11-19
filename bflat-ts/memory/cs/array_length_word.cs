// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// The length of an array read and written as a whole 8-byte word.
//
// An array keeps its 32-bit length at offset 8 and a 32-bit padding word at
// offset 12, before the first element at 16. On a zkVM target (JitZkBumpAddr
// set) the JIT reads the length with one 8-byte load, which is only the
// length if the padding word is zero, and an array allocated in line gets
// its length stored with one 8-byte store. What can go wrong: an array whose
// padding word is not zero (allocated on a stack that an earlier call left
// dirty, allocated by a path that does not clear it, copied from somewhere),
// so that the length has bits above bit 31; an object that is not an array
// (a string, whose first characters follow its length) read the same way; an
// 8-byte length store that overwrites what follows or leaves a wrong upper
// half; and a use that takes the loaded word where it needed a 32-bit value.
// (A length that is negative or too large is not tried: with allocation in
// line it ends the guest instead of throwing.)
//
// The lengths are used where the upper half of the register matters:
// widened to long and ulong, shifted, multiplied and divided as 64-bit
// values, compared with a long, as an index, as a loop limit, negated,
// narrowed. The arrays come from every allocation path: new with a constant
// and a variable length (from an int, a long, a byte, a ushort, an
// expression), every element type, initializers, static readonly data,
// Array.Empty, GC.AllocateArray and AllocateUninitializedArray,
// Array.CreateInstance, Clone, Resize, ToArray, ToCharArray, Split, params,
// jagged and multi-dimensional arrays, arrays in fields of objects and
// arrays the JIT allocates on the stack, after the stack has been filled
// with a non-zero pattern.
//
// The expected length is the one the array was created with; it is also read
// with a 4-byte load through a reference to the first element, and the
// padding word is read the same way and must be zero. Each group prints one
// line; the run ends with ALL PASS or a FAIL count.
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

struct S12 { public int A, B, C; }
struct SRef { public object O; public long L; }
class Node { public int[] Ints; public byte[] Bytes; public long Tag; }

static unsafe class Len
{
    // ---- uses of a length that read the whole register ----

    [MethodImpl(MethodImplOptions.NoInlining)] public static long AsLong(int[] a) => a.Length;
    [MethodImpl(MethodImplOptions.NoInlining)] public static ulong AsULong(int[] a) => (ulong)a.Length;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long LongLength(int[] a) => a.LongLength;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Shifted(int[] a) => (long)a.Length << 31;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Times(int[] a, long k) => a.Length * k;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Divides(int[] a, long d) => d / (a.Length + 1L);
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Negated(int[] a) => -(long)a.Length;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Plus(int[] a, long k) => k + a.Length;
    [MethodImpl(MethodImplOptions.NoInlining)] public static int Below(int[] a, long x) => (x < a.Length) ? 1 : 0;
    [MethodImpl(MethodImplOptions.NoInlining)] public static int Equal(int[] a, long x) => (a.Length == x) ? 1 : 0;
    [MethodImpl(MethodImplOptions.NoInlining)] public static int IsEmpty(int[] a) => (a.Length == 0) ? 1 : 0;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Indexes(int[] a, long[] table) => table[a.Length];
    [MethodImpl(MethodImplOptions.NoInlining)] public static int Narrowed(int[] a) => (byte)a.Length + (short)a.Length * 3;
    [MethodImpl(MethodImplOptions.NoInlining)] public static int IntSum(int[] a, int k) => a.Length + k;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Unsigned(int[] a) => (uint)a.Length;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Passed(int[] a) => Callee(a.Length, 7);
    [MethodImpl(MethodImplOptions.NoInlining)] static long Callee(long n, long k) => n * 1000 + k;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long Loop(int[] a)
    {
        long count = 0;
        for (int i = 0; i < a.Length; i++)
            count += 1 + (a[i] & 0);
        return count;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long BackLoop(int[] a)
    {
        long count = 0;
        for (long i = a.Length - 1L; i >= 0; i--)
            count++;
        return count;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long Stored(int[] a, long* slot)
    {
        *slot = a.Length;
        return *slot;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long Two(int[] a, int[] b) => (long)a.Length * 100000 + b.Length;

    [MethodImpl(MethodImplOptions.NoInlining)] public static long Of<T>(T[] a) => a.Length;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long OfShifted<T>(T[] a) => ((long)a.Length << 31) + a.LongLength;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long OfArray(Array a) => (long)a.Length * 3 + a.LongLength;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long OfObject(object o) => ((int[])o).Length + 0L;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long OfField(Node n) => (long)n.Ints.Length * 100000 + n.Bytes.Length;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long OfSpan(int[] a) => a.AsSpan().Length + 0L;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long OfJagged(int[][] a, int i) => (long)a.Length * 100000 + a[i].Length;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long OfMd(int[,] a) => a.Length + a.GetLength(0) * 1000L + a.GetLength(1) * 1000000L + a.LongLength * 1000000000L;

    // A string keeps its first characters where an array keeps its padding word.
    [MethodImpl(MethodImplOptions.NoInlining)] public static long OfString(string s) => s.Length;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long OfStringShifted(string s) => ((long)s.Length << 31) + (uint)s.Length;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long OfStringTimes(string s, long k) => s.Length * k;
    [MethodImpl(MethodImplOptions.NoInlining)] public static int OfStringBelow(string s, long x) => (x < s.Length) ? 1 : 0;

    // ---- independent readers: 4-byte loads through a reference to the first element ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Raw<T>(T[] a) => Unsafe.Add(ref Unsafe.As<T, int>(ref MemoryMarshal.GetArrayDataReference(a)), IntPtr.Size == 8 ? -2 : -1);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Padding<T>(T[] a) => IntPtr.Size != 8 ? 0 : Unsafe.Add(ref Unsafe.As<T, int>(ref MemoryMarshal.GetArrayDataReference(a)), -1);

    // ---- allocation ----

    [MethodImpl(MethodImplOptions.NoInlining)] public static T[] New<T>(int n) => new T[n];
    [MethodImpl(MethodImplOptions.NoInlining)] public static int[] NewInt(int n) => new int[n];
    [MethodImpl(MethodImplOptions.NoInlining)] public static byte[] NewByte(int n) => new byte[n];
    [MethodImpl(MethodImplOptions.NoInlining)] public static long[] NewLong(int n) => new long[n];
    [MethodImpl(MethodImplOptions.NoInlining)] public static object[] NewObject(int n) => new object[n];
    [MethodImpl(MethodImplOptions.NoInlining)] public static int[] NewFromLong(long n) => new int[n];
    [MethodImpl(MethodImplOptions.NoInlining)] public static int[] NewFromLongCast(long n) => new int[(int)n];
    [MethodImpl(MethodImplOptions.NoInlining)] public static int[] NewFromByte(byte n) => new int[n];
    [MethodImpl(MethodImplOptions.NoInlining)] public static int[] NewFromUShort(ushort n) => new int[n];
    [MethodImpl(MethodImplOptions.NoInlining)] public static int[] NewFromUInt(uint n) => new int[n];
    [MethodImpl(MethodImplOptions.NoInlining)] public static int[] NewFromSum(int a, int b) => new int[a + b];
    [MethodImpl(MethodImplOptions.NoInlining)] public static int[] NewFromLength(byte[] other) => new int[other.Length];
    [MethodImpl(MethodImplOptions.NoInlining)] public static int[] NewFromMasked(long n) => new int[(int)(n & 0x3F)];
    [MethodImpl(MethodImplOptions.NoInlining)] public static int[] NewConst7() => new int[7];
    [MethodImpl(MethodImplOptions.NoInlining)] public static byte[] NewConst0() => new byte[0];
    [MethodImpl(MethodImplOptions.NoInlining)] public static long[] NewConst100() => new long[100];
    [MethodImpl(MethodImplOptions.NoInlining)] public static int[] Initializer(int x) => new[] { x, x + 1, x + 2, x + 3, x + 4 };
    [MethodImpl(MethodImplOptions.NoInlining)] public static int[] Params(params int[] a) => a;

    // Two arrays in a row: the length store of the second must leave the first intact.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long Adjacent(int n, int m)
    {
        byte[] first = new byte[n];
        for (int i = 0; i < first.Length; i++)
            first[i] = 0xFF;
        long[] second = new long[m];
        byte[] third = new byte[n];
        long sum = 0;
        for (int i = 0; i < first.Length; i++)
            sum += first[i];
        for (int i = 0; i < second.Length; i++)
            sum += second[i] * 1000;
        for (int i = 0; i < third.Length; i++)
            sum += third[i] * 1000000;
        return sum + (long)first.Length * 1000000000L + (long)second.Length * 100000000000L + third.LongLength * 10000000000000L;
    }

    // ---- arrays the JIT can allocate on the stack ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long StackConst(int k)
    {
        int[] a = new int[6];
        a[0] = k;
        a[5] = k * 2;
        long sum = 0;
        for (int i = 0; i < a.Length; i++)
            sum += a[i];
        return sum + ((long)a.Length << 32);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long StackEither(int k, bool wide)
    {
        int[] a = wide ? new int[9] : new int[6];
        a[1] = k;
        long sum = 0;
        for (int i = 0; i < a.Length; i++)
            sum += a[i];
        return sum + ((long)a.Length << 32) + a.LongLength * 1000000;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long StackInLoop(int rounds)
    {
        long sum = 0;
        for (int r = 0; r < rounds; r++)
        {
            byte[] a = ((r & 1) == 0) ? new byte[5] : new byte[13];
            a[r % 5] = (byte)(r + 1);
            for (int i = 0; i < a.Length; i++)
                sum += a[i];
            sum += (long)a.Length << 32;
            long[] b = new long[3];
            b[r % 3] = -1;
            sum += b.LongLength * 1000000 + (b[0] & 1) + (b[1] & 2) + (b[2] & 4);
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long StackSpan(int k)
    {
        Span<long> s = new long[4];
        s[3] = k;
        long sum = 0;
        for (int i = 0; i < s.Length; i++)
            sum += s[i];
        return sum + ((long)s.Length << 32);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long StackObjects(object o)
    {
        object[] a = { o, null, o };
        long count = 0;
        foreach (object x in a)
            count += (x != null) ? 1 : 0;
        return count + ((long)a.Length << 32);
    }
}

static unsafe class Program
{
    static int s_fails;
    static int s_groupFails;
    static readonly int[] s_frozen = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
    static readonly byte[] s_frozenBytes = { 1, 2, 3 };
    static readonly string[] s_frozenStrings = { "a", "b" };
    static readonly long[] s_table = new long[70];

    static void EndGroup(string name)
    {
        if (s_groupFails == 0)
            Console.WriteLine("ok   " + name);
        else
            Console.WriteLine("FAIL " + name + " cases=" + s_groupFails);
        s_fails += s_groupFails;
        s_groupFails = 0;
    }

    static void Check(string name, long expected, long got)
    {
        if (expected == got)
            return;
        s_groupFails++;
        if (s_groupFails <= 12)
            Console.WriteLine("FAIL " + name + " expected " + expected + " got " + got);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    static int Dirty(int bytes)
    {
        byte* p = stackalloc byte[bytes];
        for (int i = 0; i < bytes; i++)
            p[i] = (byte)(0xC1 + i * 3 | 1);
        return p[bytes - 1];
    }

    // An array of any element type: the length through generic code and the raw words.
    static void Any<T>(string name, T[] a, int n)
    {
        Check(name + " raw length", n, Len.Raw(a));
        Check(name + " padding word", 0, Len.Padding(a));
        Check(name + " Of", n, Len.Of(a));
        Check(name + " OfShifted", ((long)n << 31) + n, Len.OfShifted(a));
        Check(name + " OfArray", (long)n * 4, Len.OfArray(a));
    }

    static void Ints(string name, int[] a, int n)
    {
        Any(name, a, n);
        Check(name + " AsLong", n, Len.AsLong(a));
        Check(name + " AsULong", n, (long)Len.AsULong(a));
        Check(name + " LongLength", n, Len.LongLength(a));
        Check(name + " Shifted", (long)n << 31, Len.Shifted(a));
        Check(name + " Times", n * 0x100000001L, Len.Times(a, 0x100000001L));
        Check(name + " Divides", 0x7000000000000000L / (n + 1L), Len.Divides(a, 0x7000000000000000L));
        Check(name + " Negated", -(long)n, Len.Negated(a));
        Check(name + " Plus", 0x100000000L + n, Len.Plus(a, 0x100000000L));
        Check(name + " Below", 1, Len.Below(a, n - 1L));
        Check(name + " Below equal", 0, Len.Below(a, n));
        Check(name + " Below big", 0, Len.Below(a, n + 0x100000000L));
        Check(name + " Equal", 1, Len.Equal(a, n));
        Check(name + " Equal big", 0, Len.Equal(a, n + 0x100000000L));
        Check(name + " IsEmpty", (n == 0) ? 1 : 0, Len.IsEmpty(a));
        if (n < s_table.Length)
            Check(name + " Indexes", n * 7 + 1, Len.Indexes(a, s_table));
        Check(name + " Narrowed", (byte)n + (short)n * 3, Len.Narrowed(a));
        Check(name + " IntSum", n - 5, Len.IntSum(a, -5));
        Check(name + " Unsigned", n, Len.Unsigned(a));
        Check(name + " Passed", n * 1000L + 7, Len.Passed(a));
        Check(name + " Loop", n, Len.Loop(a));
        Check(name + " BackLoop", n, Len.BackLoop(a));
        long slot = -1;
        Check(name + " Stored", n, Len.Stored(a, &slot));
        Check(name + " Stored slot", n, slot);
        Check(name + " Two", n * 100000L + 11, Len.Two(a, s_frozen));
        Check(name + " OfObject", n, Len.OfObject(a));
        Check(name + " OfSpan", n, Len.OfSpan(a));
    }

    static void Allocations()
    {
        for (int i = 0; i < s_table.Length; i++)
            s_table[i] = i * 7 + 1;

        for (int n = 0; n <= 66; n++)
        {
            Ints("new int[n]", Len.NewInt(n), n);
            Ints("new int[long]", Len.NewFromLong(n), n);
            Ints("new int[(int)long]", Len.NewFromLongCast(n), n);
            Ints("new int[byte]", Len.NewFromByte((byte)n), n);
            Ints("new int[ushort]", Len.NewFromUShort((ushort)n), n);
            Ints("new int[uint]", Len.NewFromUInt((uint)n), n);
            Ints("new int[a + b]", Len.NewFromSum(n + 5, -5), n);
            Ints("new int[other.Length]", Len.NewFromLength(Len.NewByte(n)), n);
            Ints("new int[n & 63]", Len.NewFromMasked(n | 0x7FFFFFFF00000040L), n & 63);
            Any("new byte[n]", Len.NewByte(n), n);
            Any("new long[n]", Len.NewLong(n), n);
            Any("new object[n]", Len.NewObject(n), n);
            Any("new short[n]", Len.New<short>(n), n);
            Any("new char[n]", Len.New<char>(n), n);
            Any("new string[n]", Len.New<string>(n), n);
            Any("new S12[n]", Len.New<S12>(n), n);
            Any("new SRef[n]", Len.New<SRef>(n), n);
            Any("new int[n][]", Len.New<int[]>(n), n);
        }

        int[] sizes = { 127, 128, 255, 256, 257, 1000, 4095, 4096, 65535, 65536, 100000 };
        foreach (int n in sizes)
        {
            Ints("new int[big]", Len.NewInt(n), n);
            Any("new byte[big]", Len.NewByte(n), n);
            Any("new object[big]", Len.NewObject(n), n);
            Any("GC.AllocateUninitializedArray", GC.AllocateUninitializedArray<byte>(n), n);
            Any("GC.AllocateArray pinned", GC.AllocateArray<long>(n, pinned: true), n);
        }

        Ints("new int[7]", Len.NewConst7(), 7);
        Any("new byte[0]", Len.NewConst0(), 0);
        Any("new long[100]", Len.NewConst100(), 100);
        Ints("initializer", Len.Initializer(3), 5);
        Ints("params", Len.Params(1, 2, 3, 4), 4);
        Ints("params none", Len.Params(), 0);
        Ints("static readonly", s_frozen, 11);
        Any("static readonly bytes", s_frozenBytes, 3);
        Any("static readonly strings", s_frozenStrings, 2);
        Ints("Array.Empty", Array.Empty<int>(), 0);
        Any("Array.Empty strings", Array.Empty<string>(), 0);

        for (int n = 0; n <= 20; n++)
        {
            Any("GC.AllocateUninitializedArray small", GC.AllocateUninitializedArray<int>(n), n);
            Ints("Array.CreateInstance", (int[])Array.CreateInstance(typeof(int), n), n);
            int[] source = Len.NewInt(n);
            Ints("Clone", (int[])source.Clone(), n);
            int[] resized = Len.NewInt(3);
            Array.Resize(ref resized, n);
            Ints("Resize", resized, n);
            var list = new List<int>();
            for (int i = 0; i < n; i++)
                list.Add(i);
            Ints("List.ToArray", list.ToArray(), n);
            Ints("Span.ToArray", source.AsSpan().ToArray(), n);
            string text = new string('x', n);
            Any("ToCharArray", text.ToCharArray(), n);
            Any("Encoding.GetBytes", Encoding.UTF8.GetBytes(text), n);
            Any("Split", new string(',', n).Split(','), n + 1);
            int[][] jagged = new int[n + 1][];
            for (int i = 0; i <= n; i++)
                jagged[i] = new int[i * 3];
            Check("jagged", (n + 1) * 100000L + n * 3, Len.OfJagged(jagged, n));
            Check("jagged first", (n + 1) * 100000L, Len.OfJagged(jagged, 0));
            int[,] md = new int[n, 3];
            Check("int[n, 3]", n * 3 + n * 1000L + 3 * 1000000L + n * 3 * 1000000000L, Len.OfMd(md));
            Node node = new Node { Ints = Len.NewInt(n), Bytes = Len.NewByte(n * 2), Tag = -1 };
            Check("fields", n * 100000L + n * 2, Len.OfField(node));
        }

        Any("BitConverter.GetBytes", BitConverter.GetBytes(0x0102030405060708L), 8);
        EndGroup("lengths of arrays from every allocation path");

        for (int n = 0; n <= 40; n++)
        {
            for (int m = 0; m <= 5; m++)
            {
                Check("Adjacent", n * 255L + n * 1000000000L + m * 100000000000L + n * 10000000000000L, Len.Adjacent(n, m));
            }
        }
        EndGroup("arrays allocated in a row");

        string[] strings = { "", "a", "ab", "abc", "abcd", "abcde", "abcdefgh", "￿￿￿￿￿￿￿" };
        foreach (string s in strings)
        {
            string t = new string(s.AsSpan());
            int n = s.Length;
            Check("string length", n, Len.OfString(t));
            Check("string length shifted", ((long)n << 31) + n, Len.OfStringShifted(t));
            Check("string length times", n * 0x100000001L, Len.OfStringTimes(t, 0x100000001L));
            Check("string length below", 0, Len.OfStringBelow(t, n));
            Check("string length below big", 0, Len.OfStringBelow(t, n + 0x100000000L));
            Check("string length literal", n, Len.OfString(s));
        }
        EndGroup("lengths of strings");
    }

    static void Stack()
    {
        for (int round = 1; round <= 6; round++)
        {
            Dirty(1024);
            Check("StackConst", round * 3 + (6L << 32), Len.StackConst(round));
            Dirty(1024);
            Check("StackEither narrow", round + (6L << 32) + 6 * 1000000, Len.StackEither(round, false));
            Dirty(1024);
            Check("StackEither wide", round + (9L << 32) + 9 * 1000000, Len.StackEither(round, true));
            Dirty(1024);
            long want = 0;
            for (int r = 0; r < round + 3; r++)
            {
                want += (r + 1) + ((((r & 1) == 0) ? 5L : 13L) << 32);
                want += 3 * 1000000 + ((r % 3 == 0) ? 1 : 0) + ((r % 3 == 1) ? 2 : 0) + ((r % 3 == 2) ? 4 : 0);
            }
            Check("StackInLoop", want, Len.StackInLoop(round + 3));
            Dirty(1024);
            Check("StackSpan", round + (4L << 32), Len.StackSpan(round));
            Dirty(1024);
            Check("StackObjects", 2 + (3L << 32), Len.StackObjects("x"));
        }
        EndGroup("arrays on a stack that an earlier call left dirty");
    }

    static int Main()
    {
        Allocations();
        Stack();

        Console.WriteLine(s_fails == 0 ? "ALL PASS" : "FAILED " + s_fails);
        return s_fails == 0 ? 0 : 1;
    }
}
