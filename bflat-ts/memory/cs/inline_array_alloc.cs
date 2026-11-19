// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Arrays made by the allocation sequence the JIT emits in place of a call
// to the array allocation helper, and strings, which keep the helper.
//
// For `new T[n]` with a known element type the JIT emits the allocation at
// the site: widen the length, compare it with the largest array length,
// size = (n * element size + 16 + 7) & ~7, move the heap pointer down by
// it, compare with the heap floor, store the size under the array, the
// MethodTable and the length in it. A constant length is folded: no length
// check and no arithmetic. Nothing clears the memory. What can go wrong:
//
//   - the size: the element size of a struct, an odd element size, the
//     rounding, a size at either side of the 12-bit immediate limit; an
//     array that is too short overlaps its neighbour;
//   - the length: a length register with more than 32 bits of meaning (a
//     wrapped sum, an unsigned value, a narrowed long), a length that comes
//     from a byte, a char or another array's length; the length field and
//     the four bytes after it, which a 64-bit load of the length reads;
//   - the MethodTable: object[] against string[] (a store of the wrong
//     type must still throw), jagged arrays, shared generic code (keeps the
//     helper);
//   - memory that is not zero: blocks the C allocator had and gave back;
//   - a constant length the helper rejects must keep the helper, and its
//     exception;
//   - the helper for strings computes the size, calls for the thread's
//     allocation context and then uses the size: it must survive the call.
//
// One generic method per element type is compiled for a variable length;
// constant lengths are written out. Every array is checked for its type,
// its length (three ways), alignment and zero contents, filled with a
// pattern, and checked again after later arrays are made. The address
// ranges, with the word under the array, must not overlap; the size of a
// range is worked out here from sizeof.
//
// A length that is negative or too large for the heap is not here, apart
// from the constants the helper rejects: the sequence ends the guest for it
// instead of throwing, see array_length_checks.cs.
// The collector of a zkVM guest never collects, so nothing here can test
// what a collection would see.
//
// Each case prints one line on failure; the run ends with ALL PASS or a FAIL
// count, and the guest returns the number of failures.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Size = 3, Pack = 1)] struct E3 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 5, Pack = 1)] struct E5 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 6, Pack = 2)] struct E6 { public short B; }
[StructLayout(LayoutKind.Sequential, Size = 12)] struct E12 { public int B; }
[StructLayout(LayoutKind.Sequential, Size = 20)] struct E20 { public int B; }
[StructLayout(LayoutKind.Sequential, Size = 24)] struct E24 { public long B; }
[StructLayout(LayoutKind.Sequential, Size = 40)] struct E40 { public long B; }
[StructLayout(LayoutKind.Sequential, Size = 257, Pack = 1)] struct E257 { public byte B; }
struct ERef { public object R; public long X; }
enum Small : byte { A, B }
class Thing { public int V; }
class SubThing : Thing { }

static unsafe class Program
{
    [DllImport("*")] static extern void* malloc(nuint n);
    [DllImport("*")] static extern void free(void* p);

    static int fails;
    static object s_sink;

    static void Check(string name, long expected, long got)
    {
        if (expected != got)
        {
            fails++;
            Console.WriteLine("FAIL " + name + " expected " + expected + " got " + got);
        }
    }

    static long Addr(object o) => Unsafe.As<object, long>(ref o);

    [MethodImpl(MethodImplOptions.NoInlining)] static int Opaque(int x) => x;
    [MethodImpl(MethodImplOptions.NoInlining)] static long OpaqueL(long x) => x;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void DirtyNative(int size)
    {
        byte* p = (byte*)malloc((nuint)size);
        new Span<byte>(p, size).Fill(0xA5);
        free(p);
    }

    static byte Pattern(int seed, long i) => (byte)(seed * 131 + i * 7 + (i >> 8) + 1);

    // The words of an array as the runtime lays it out: the MethodTable, then
    // the length in a 64-bit word, then the elements.
    static long RawLengthWord(Array a) => *(long*)(Addr(a) + 8);
    static byte* Data(Array a) => (byte*)(Addr(a) + 16);

    static long NonZero(byte* p, long size)
    {
        long n = 0;
        for (long i = 0; i < size; i++) if (p[i] != 0) n++;
        return n;
    }

    static void Fill(byte* p, long size, int seed)
    {
        for (long i = 0; i < size; i++) p[i] = Pattern(seed, i);
    }

    static long Wrong(byte* p, long size, int seed)
    {
        // The whole of a small array; both ends of a large one.
        long n = 0;
        if (size <= 16384)
        {
            for (long i = 0; i < size; i++) if (p[i] != Pattern(seed, i)) n++;
        }
        else
        {
            for (long i = 0; i < 4096; i++) if (p[i] != Pattern(seed, i)) n++;
            for (long i = size - 4096; i < size; i++) if (p[i] != Pattern(seed, i)) n++;
        }
        return n;
    }

    // The last few arrays, kept to be read again after later allocations.
    const int Keep = 6;
    static Array[] kept = new Array[Keep];
    static long[] keptBytes = new long[Keep];
    static int[] keptSeed = new int[Keep];
    static int keptNext, seedNext;

    static void Verify(string name, Array a, long length, int elemSize, bool exactType)
    {
        long bytes = length * elemSize;
        Check(name + " Length", length, a.Length);
        Check(name + " LongLength", length, a.LongLength);
        Check(name + " length word", length, RawLengthWord(a));
        Check(name + " alignment", 0, Addr(a) & 7);
        Check(name + " zero", 0, NonZero(Data(a), bytes));
        Check(name + " type", 1, exactType ? 1 : 0);

        long start = Addr(a) - 8, end = Addr(a) + 16 + ((bytes + 7) & ~7L);
        for (int i = 0; i < Keep; i++)
        {
            if (kept[i] == null) continue;
            long s = Addr(kept[i]) - 8, e = Addr(kept[i]) + 16 + ((keptBytes[i] + 7) & ~7L);
            if (start < e && s < end)
            {
                fails++;
                Console.WriteLine("FAIL " + name + " overlaps an earlier array: [" + start.ToString("x") + "," + end.ToString("x") +
                                  ") and [" + s.ToString("x") + "," + e.ToString("x") + ")");
            }
        }

        int seed = ++seedNext;
        Fill(Data(a), bytes, seed);
        Check(name + " length after fill", length, a.Length);
        kept[keptNext] = a; keptBytes[keptNext] = bytes; keptSeed[keptNext] = seed;
        keptNext = (keptNext + 1) % Keep;
        long wrong = 0;
        for (int i = 0; i < Keep; i++)
            if (kept[i] != null)
            {
                wrong += Wrong(Data(kept[i]), keptBytes[i], keptSeed[i]);
                if (RawLengthWord(kept[i]) * 1 != kept[i].Length) wrong++;
            }
        Check(name + " earlier arrays", 0, wrong);
    }

    static void Forget()
    {
        for (int i = 0; i < Keep; i++) kept[i] = null;
    }

    // ---- variable length, one method per element type ------------------------
    [MethodImpl(MethodImplOptions.NoInlining)] static T[] New<T>(int n) where T : unmanaged => new T[n];
    [MethodImpl(MethodImplOptions.NoInlining)] static T[] NewLong<T>(long n) where T : unmanaged => new T[n];
    [MethodImpl(MethodImplOptions.NoInlining)] static T[] NewUInt<T>(uint n) where T : unmanaged => new T[n];
    [MethodImpl(MethodImplOptions.NoInlining)] static T[] NewByte<T>(byte n) where T : unmanaged => new T[n];
    [MethodImpl(MethodImplOptions.NoInlining)] static T[] NewChar<T>(char n) where T : unmanaged => new T[n];
    [MethodImpl(MethodImplOptions.NoInlining)] static T[] NewSum<T>(int a, int b) where T : unmanaged => new T[a + b];
    [MethodImpl(MethodImplOptions.NoInlining)] static T[] NewNarrowed<T>(long n) where T : unmanaged => new T[(int)n];
    [MethodImpl(MethodImplOptions.NoInlining)] static T[] NewMasked<T>(uint u) where T : unmanaged => new T[(int)(u >> 28)];
    [MethodImpl(MethodImplOptions.NoInlining)] static T[] NewLike<T>(Array other) where T : unmanaged => new T[other.Length];
    [MethodImpl(MethodImplOptions.NoInlining)] static T[] NewShared<T>(int n) where T : class => new T[n];
    [MethodImpl(MethodImplOptions.NoInlining)] static T[] NewFromField<T>(int[] box) where T : unmanaged => new T[box[0]];

    static readonly int[] Lengths =
    {
        0, 1, 2, 3, 4, 5, 7, 8, 9, 15, 16, 17, 31, 32, 33, 63, 64, 65, 127, 255, 256, 257, 1000,
        2023, 2024, 2025, 2032, 2033, 4095, 4096, 4097, 65535, 65536, 65537, 300000,
    };

    static void ElementType<T>(string name) where T : unmanaged
    {
        int size = sizeof(T);
        Forget();
        for (int k = 0; k < Lengths.Length; k++)
        {
            int n = Lengths[k];
            if ((long)n * size > (1 << 21)) continue;
            if (k % 3 == 0) DirtyNative(n * size < 64 ? 64 : (n * size > 8192 ? 8192 : n * size));
            T[] a = New<T>(n);
            Verify(name + "[" + n + "]", a, n, size, a.GetType() == typeof(T[]));
            if (n > 0)
            {
                // Typed access to the last element agrees with the bytes.
                T last = a[n - 1];
                Check(name + "[" + n + "] last element", 1,
                      new ReadOnlySpan<byte>(&last, size).SequenceEqual(new ReadOnlySpan<byte>(Data(a) + (long)(n - 1) * size, size)) ? 1 : 0);
            }
        }

        // The same length arriving in other shapes.
        int[] few = { 0, 1, 5, 9, 200 };
        for (int k = 0; k < few.Length; k++)
        {
            int n = few[k];
            string nm = name + "[" + n + "]";
            T[] a = NewLong<T>(n); Verify(nm + " long", a, n, size, a.GetType() == typeof(T[]));
            a = NewUInt<T>((uint)n); Verify(nm + " uint", a, n, size, a.GetType() == typeof(T[]));
            a = NewByte<T>((byte)n); Verify(nm + " byte", a, n, size, a.GetType() == typeof(T[]));
            a = NewChar<T>((char)n); Verify(nm + " char", a, n, size, a.GetType() == typeof(T[]));
            // The sum wraps to n in 32 bits.
            a = NewSum<T>(0x7FFFFFFF, unchecked((int)0x80000001) + n); Verify(nm + " wrapped sum", a, n, size, a.GetType() == typeof(T[]));
            a = NewSum<T>(-5, n + 5); Verify(nm + " sum", a, n, size, a.GetType() == typeof(T[]));
            a = NewNarrowed<T>(OpaqueL(0x7654321000000000L + n)); Verify(nm + " narrowed", a, n, size, a.GetType() == typeof(T[]));
            if (n < 16) { a = NewMasked<T>(((uint)n << 28) | 0x0ABCDEF1); Verify(nm + " shifted uint", a, n, size, a.GetType() == typeof(T[])); }
            a = NewLike<T>(new byte[n]); Verify(nm + " like", a, n, size, a.GetType() == typeof(T[]));
            a = NewFromField<T>(new int[] { n, -1 }); Verify(nm + " from element", a, n, size, a.GetType() == typeof(T[]));
        }
    }

    // ---- constant lengths ------------------------------------------------------
    [MethodImpl(MethodImplOptions.AggressiveInlining)] static int Five() => 5;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] static int MinusOne() => -1;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] static T[] Inl<T>(int n) => new T[n];
    static readonly int s_seven = 7;

    [MethodImpl(MethodImplOptions.NoInlining)] static byte[] B0() => new byte[0];
    [MethodImpl(MethodImplOptions.NoInlining)] static byte[] B1() => new byte[1];
    [MethodImpl(MethodImplOptions.NoInlining)] static byte[] B7() => new byte[7];
    [MethodImpl(MethodImplOptions.NoInlining)] static byte[] B8() => new byte[8];
    [MethodImpl(MethodImplOptions.NoInlining)] static byte[] B9() => new byte[9];
    [MethodImpl(MethodImplOptions.NoInlining)] static byte[] B2024() => new byte[2024];
    [MethodImpl(MethodImplOptions.NoInlining)] static byte[] B2025() => new byte[2025];
    [MethodImpl(MethodImplOptions.NoInlining)] static byte[] B2032() => new byte[2032];
    [MethodImpl(MethodImplOptions.NoInlining)] static byte[] B2033() => new byte[2033];
    [MethodImpl(MethodImplOptions.NoInlining)] static byte[] B100000() => new byte[100000];
    [MethodImpl(MethodImplOptions.NoInlining)] static char[] C3() => new char[3];
    [MethodImpl(MethodImplOptions.NoInlining)] static int[] I5() => new int[Five()];
    [MethodImpl(MethodImplOptions.NoInlining)] static int[] I7() => new int[s_seven];
    [MethodImpl(MethodImplOptions.NoInlining)] static int[] I100000() => Inl<int>(100000);
    [MethodImpl(MethodImplOptions.NoInlining)] static long[] L253() => new long[253];
    [MethodImpl(MethodImplOptions.NoInlining)] static long[] L254() => new long[254];
    [MethodImpl(MethodImplOptions.NoInlining)] static long[] L255() => new long[255];
    [MethodImpl(MethodImplOptions.NoInlining)] static E3[] T3() => new E3[5];
    [MethodImpl(MethodImplOptions.NoInlining)] static E12[] T12() => new E12[5];
    [MethodImpl(MethodImplOptions.NoInlining)] static E257[] T257() => new E257[9];
    [MethodImpl(MethodImplOptions.NoInlining)] static Int128[] W4() => new Int128[4];
    [MethodImpl(MethodImplOptions.NoInlining)] static int[] Init3() => new int[] { 0x11111111, 0x22222222, 0x33333333 };
    [MethodImpl(MethodImplOptions.NoInlining)] static long[] Init9(long x) => new long[] { 1, 2, 3, 4, x, 6, 7, 8, 9 };
    [MethodImpl(MethodImplOptions.NoInlining)] static int[] NegativeConstant() => new int[MinusOne()];
    [MethodImpl(MethodImplOptions.NoInlining)] static byte[] TooLongConstant() => Inl<byte>(int.MaxValue);

    static void Constant<T>(string name, T[] a, int n) where T : unmanaged
    {
        Verify(name, a, n, sizeof(T), a.GetType() == typeof(T[]));
    }

    static void CaseConstants()
    {
        Forget();
        for (int round = 0; round < 2; round++)
        {
            Constant("const byte[0]", B0(), 0);
            Constant("const byte[1]", B1(), 1);
            DirtyNative(100);
            Constant("const byte[7]", B7(), 7);
            Constant("const byte[8]", B8(), 8);
            Constant("const byte[9]", B9(), 9);
            Constant("const byte[2024]", B2024(), 2024);
            Constant("const byte[2025]", B2025(), 2025);
            DirtyNative(4000);
            Constant("const byte[2032]", B2032(), 2032);
            Constant("const byte[2033]", B2033(), 2033);
            Constant("const byte[100000]", B100000(), 100000);
            Constant("const char[3]", C3(), 3);
            Constant("const int[5]", I5(), 5);
            Constant("const int[7]", I7(), 7);
            Constant("const int[100000]", I100000(), 100000);
            Constant("const long[253]", L253(), 253);
            Constant("const long[254]", L254(), 254);
            Constant("const long[255]", L255(), 255);
            Constant("const E3[5]", T3(), 5);
            Constant("const E12[5]", T12(), 5);
            Constant("const E257[9]", T257(), 9);
            Constant("const Int128[4]", W4(), 4);
        }
        int[] i3 = Init3();
        Check("initialized int[3]", 0x11111111L + 0x22222222 + 0x33333333 + 3, (long)i3[0] + i3[1] + i3[2] + i3.Length);
        long[] l9 = Init9(-5);
        long sum = 0;
        for (int i = 0; i < l9.Length; i++) sum = sum * 10 + l9[i];
        Check("initialized long[9]", 123356789, sum);

        string got = "nothing";
        try { s_sink = NegativeConstant(); } catch (Exception e) { got = e.GetType().Name; }
        if (got != "OverflowException") { fails++; Console.WriteLine("FAIL constant length -1 expected OverflowException got " + got); }
        got = "nothing";
        try { s_sink = TooLongConstant(); } catch (Exception e) { got = e.GetType().Name; }
        if (got != "OutOfMemoryException" && got != "OverflowException")
        {
            fails++; Console.WriteLine("FAIL constant length int.MaxValue expected OutOfMemoryException or OverflowException got " + got);
        }
    }

    // ---- arrays of references ----------------------------------------------------
    [MethodImpl(MethodImplOptions.NoInlining)] static object[] NewObjects(int n) => new object[n];
    [MethodImpl(MethodImplOptions.NoInlining)] static string[] NewStrings(int n) => new string[n];
    [MethodImpl(MethodImplOptions.NoInlining)] static Thing[] NewThings(int n) => new Thing[n];
    [MethodImpl(MethodImplOptions.NoInlining)] static SubThing[] NewSubThings(int n) => new SubThing[n];
    [MethodImpl(MethodImplOptions.NoInlining)] static int[][] NewJagged(int n) => new int[n][];
    [MethodImpl(MethodImplOptions.NoInlining)] static ERef[] NewWithRefs(int n) => new ERef[n];
    [MethodImpl(MethodImplOptions.NoInlining)] static object[] NewObjects3() => new object[3];
    [MethodImpl(MethodImplOptions.NoInlining)] static void StoreObject(object[] a, int i, object o) => a[i] = o;

    static long NullCount(object[] a)
    {
        long n = 0;
        for (int i = 0; i < a.Length; i++) if (a[i] == null) n++;
        return n;
    }

    static void CaseReferences()
    {
        Forget();
        int[] lens = { 0, 1, 2, 3, 8, 100, 253, 254, 255, 5000 };
        for (int k = 0; k < lens.Length; k++)
        {
            int n = lens[k];
            if ((k & 1) == 0) DirtyNative(256);
            object[] o = NewObjects(n);
            Check("object[" + n + "] nulls", n, NullCount(o));
            Verify("object[" + n + "]", o, n, 8, o.GetType() == typeof(object[]));
            string[] s = NewStrings(n);
            Check("string[" + n + "] nulls", n, NullCount(s));
            Verify("string[" + n + "]", s, n, 8, s.GetType() == typeof(string[]));
            Thing[] t = NewSubThings(n);
            Verify("SubThing[" + n + "]", t, n, 8, t.GetType() == typeof(SubThing[]) && t is Thing[] && !(t is object[] and string[]));
            int[][] j = NewJagged(n);
            Verify("int[" + n + "][]", j, n, 8, j.GetType() == typeof(int[][]));
            ERef[] r = NewWithRefs(n);
            Verify("ERef[" + n + "]", r, n, 16, r.GetType() == typeof(ERef[]));
            string[] sh = NewShared<string>(n);
            Verify("shared string[" + n + "]", sh, n, 8, sh.GetType() == typeof(string[]));
            Thing[] sht = NewShared<Thing>(n);
            Verify("shared Thing[" + n + "]", sht, n, 8, sht.GetType() == typeof(Thing[]));
        }
        Forget();

        // The MethodTable decides which stores are allowed.
        object[] asObjects = NewStrings(4);
        StoreObject(asObjects, 1, "ok");
        string got = "nothing";
        try { StoreObject(asObjects, 2, new Thing()); } catch (Exception e) { got = e.GetType().Name; }
        if (got != "ArrayTypeMismatchException") { fails++; Console.WriteLine("FAIL store Thing into string[] expected ArrayTypeMismatchException got " + got); }
        Thing[] things = NewSubThings(2);
        got = "nothing";
        try { StoreObject(things, 0, new Thing()); } catch (Exception e) { got = e.GetType().Name; }
        if (got != "ArrayTypeMismatchException") { fails++; Console.WriteLine("FAIL store Thing into SubThing[] expected ArrayTypeMismatchException got " + got); }
        StoreObject(things, 1, new SubThing { V = 9 });
        Check("store SubThing", 9, things[1].V);
        object[] three = NewObjects3();
        StoreObject(three, 0, three); StoreObject(three, 2, "x");
        Check("object[3]", 1, ReferenceEquals(three[0], three) && three[1] == null && (string)three[2] == "x" ? 1 : 0);

        int[][] jag = NewJagged(5);
        for (int i = 0; i < 5; i++) jag[i] = New<int>(i);
        long total = 0;
        for (int i = 0; i < 5; i++) total = total * 10 + jag[i].Length;
        Check("jagged", 1234, total);
        int[,] grid = new int[Opaque(3), Opaque(4)];
        grid[2, 3] = 7;
        Check("rectangular", 12 * 100 + 7, grid.Length * 100 + grid[2, 3]);
    }

    // ---- strings and the other users of the helper -------------------------------
    [MethodImpl(MethodImplOptions.NoInlining)] static string NewString(char c, int n) => new string(c, n);

    static void CaseStrings()
    {
        const int N = 40;
        string[] keep = new string[N];
        long[] ends = new long[N];
        int[] lens = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 31, 32, 33, 100, 1000, 1001, 4095, 4096, 65535, 100000 };
        for (int round = 0; round < 3; round++)
            for (int k = 0; k < lens.Length; k++)
            {
                int n = lens[k];
                char c = (char)('a' + (k + round) % 26);
                if (k % 5 == 0) DirtyNative(512);
                string s = NewString(c, n);
                Check("string " + n + " length", n, s.Length);
                long bad = 0;
                for (int i = 0; i < n; i++) if (s[i] != c) bad++;
                Check("string " + n + " content", 0, bad);
                if (n == 0) continue;
                Check("string " + n + " terminator", 0, *(char*)(Addr(s) + 12 + 2L * n));
                long start = Addr(s) - 8, end = Addr(s) + ((12 + 2L * n + 2 + 7) & ~7L);
                int slot = (round * lens.Length + k) % N;
                for (int i = 0; i < N; i++)
                {
                    if (keep[i] == null) continue;
                    if (start < ends[i] && Addr(keep[i]) - 8 < end)
                    {
                        fails++;
                        Console.WriteLine("FAIL string " + n + " overlaps an earlier string");
                    }
                }
                keep[slot] = s; ends[slot] = end;
            }
        long wrong = 0;
        for (int i = 0; i < N; i++)
            if (keep[i] != null)
            {
                char c = keep[i][0];
                for (int j = 0; j < keep[i].Length; j++) if (keep[i][j] != c) wrong++;
            }
        Check("strings kept", 0, wrong);

        string cat = "";
        for (int i = 0; i < 200; i++) cat += (char)('0' + i % 10);
        long digits = 0;
        for (int i = 0; i < cat.Length; i++) digits += cat[i] - '0';
        Check("concatenation", 200 * 1000 + 900, cat.Length * 1000 + digits);

        // Arrays that do not come from `new T[n]`.
        long[] u = GC.AllocateUninitializedArray<long>(Opaque(3000));
        Check("uninitialized array", 3000, u.Length);
        byte[] z = GC.AllocateArray<byte>(Opaque(777), pinned: true);
        Check("pinned array", 777, z.Length + NonZero(Data(z), 777));
        Array viaType = Array.CreateInstance(typeof(short), Opaque(33));
        Check("created array", 33 * 10 + 1, viaType.Length * 10 + (viaType.GetType() == typeof(short[]) ? 1 : 0));
        int[] resized = New<int>(10);
        for (int i = 0; i < 10; i++) resized[i] = i + 1;
        Array.Resize(ref resized, Opaque(25));
        long rs = 0;
        for (int i = 0; i < resized.Length; i++) rs += resized[i];
        Check("resized array", 25 * 1000 + 55, resized.Length * 1000 + rs);
        long[] clone = (long[])Init9(5).Clone();
        Check("cloned array", 9 * 100 + 45, clone.Length * 100 + clone[0] + clone[1] + clone[2] + clone[3] + clone[4] + clone[5] + clone[6] + clone[7] + clone[8]);
    }

    // ---- several megabytes ---------------------------------------------------------
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Volume(int rounds, byte[][] keepB, long[][] keepL)
    {
        long dirty = 0;
        for (int i = 0; i < rounds; i++)
        {
            int n = (i * 37) % 600;
            byte[] b = new byte[n];
            long[] l = new long[n & 63];
            char[] c = new char[17];
            for (int k = 0; k < b.Length; k++) dirty += b[k];
            for (int k = 0; k < l.Length; k++) dirty += l[k];
            dirty += c[0] + c[16];
            for (int k = 0; k < b.Length; k++) b[k] = (byte)(i + k);
            for (int k = 0; k < l.Length; k++) l[k] = ((long)i << 20) + k;
            c[0] = c[16] = 'x';
            if (i % 997 == 0) DirtyNative(1024);
            if (i % 16 == 0) { keepB[i / 16] = b; keepL[i / 16] = l; }
        }
        return dirty;
    }

    static void CaseVolume()
    {
        const int Rounds = 24000;                    // about 12 MB
        var keepB = new byte[Rounds / 16][];
        var keepL = new long[Rounds / 16][];
        Check("volume zero", 0, Volume(Rounds, keepB, keepL));
        long bad = 0;
        for (int j = 0; j < keepB.Length; j++)
        {
            int i = j * 16, n = (i * 37) % 600;
            if (keepB[j].Length != n || keepL[j].Length != (n & 63)) bad++;
            for (int k = 0; k < keepB[j].Length; k++) if (keepB[j][k] != (byte)(i + k)) bad++;
            for (int k = 0; k < keepL[j].Length; k++) if (keepL[j][k] != ((long)i << 20) + k) bad++;
        }
        Check("volume kept", 0, bad);
    }

    static int Main()
    {
        ElementType<byte>("byte");
        ElementType<bool>("bool");
        ElementType<Small>("enum");
        ElementType<short>("short");
        ElementType<char>("char");
        ElementType<E3>("e3");
        ElementType<int>("int");
        ElementType<E5>("e5");
        ElementType<E6>("e6");
        ElementType<long>("long");
        ElementType<nint>("nint");
        ElementType<E12>("e12");
        ElementType<Guid>("guid");
        ElementType<Int128>("int128");
        ElementType<decimal>("decimal");
        ElementType<E20>("e20");
        ElementType<E24>("e24");
        ElementType<E40>("e40");
        ElementType<E257>("e257");
        CaseConstants();
        CaseReferences();
        CaseStrings();
        CaseVolume();

        Console.WriteLine(fails == 0 ? "ALL PASS" : "FAILED " + fails);
        return fails;
    }
}
