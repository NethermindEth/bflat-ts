// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Objects and boxes made by the allocation sequence the JIT emits in place
// of a call to the allocation helper.
//
// A zkVM guest takes all its memory from one pointer that moves down, kept
// in a cell at a fixed address and shared with the C allocator. For a class
// of fixed size the JIT emits the whole allocation at the `new`: load the
// pointer, subtract the size, compare with the heap floor kept in the cell
// below, store the pointer back, store the size in the word under the
// object and the MethodTable in the object. Nothing clears the memory: it
// is taken to be fresh. What can go wrong:
//
//   - the size: a class or a box whose size the JIT works out differently
//     from the runtime overlaps its neighbour (a box is sized from the value
//     type, without the MethodTable pointer); sizes at both sides of the
//     12-bit immediate limit; very large classes;
//   - the word under the object is also the object header: the size written
//     there must leave it empty, and the hash code or a lock put into the
//     header later must not touch a neighbour;
//   - memory that is not zero: a block the C allocator gave out and took
//     back, a block that was reallocated;
//   - the MethodTable: exact class in shared generic code, a class with a
//     finalizer (it has to take the helper), new T();
//   - several allocations in one method and in a loop, where the cell
//     address and the floor are shared, with helper and C allocations in
//     between that move the same pointer;
//   - the floor: the cell under the pointer has to hold the lowest address
//     of the heap, or the guard never fires and an exhausted heap runs into
//     the data below it. That is not tested here: the address of the cell
//     belongs to the guest's linker script, and running out of memory ends
//     the guest either way.
//
// Every size class is one generic method compiled per value type. Each
// object is checked for its type, alignment and zero contents, filled with
// a pattern, and checked again after the objects around it are made, hashed
// and locked. The address ranges of the objects, with the header word, must
// not overlap; the size of a range is worked out here from sizeof.
//
// The collector of a zkVM guest never collects, so nothing here can test
// what a collection would see.
//
// Each case prints one line on failure; the run ends with ALL PASS or a FAIL
// count, and the guest returns the number of failures.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Size = 3, Pack = 1)] struct S3 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 12)] struct S12 { public int B; }
[StructLayout(LayoutKind.Sequential, Size = 20)] struct S20 { public int B; }
[StructLayout(LayoutKind.Sequential, Size = 24)] struct S24 { public long B; }
[StructLayout(LayoutKind.Sequential, Size = 40)] struct S40 { public long B; }
[StructLayout(LayoutKind.Sequential, Size = 56)] struct S56 { public long B; }
[StructLayout(LayoutKind.Sequential, Size = 100)] struct S100 { public int B; }
[StructLayout(LayoutKind.Sequential, Size = 255, Pack = 1)] struct S255 { public byte B; }
[StructLayout(LayoutKind.Sequential, Size = 1000)] struct S1000 { public long B; }
[StructLayout(LayoutKind.Sequential, Size = 2024)] struct S2024 { public long B; }
[StructLayout(LayoutKind.Sequential, Size = 2032)] struct S2032 { public long B; }
[StructLayout(LayoutKind.Sequential, Size = 2040)] struct S2040 { public long B; }
[StructLayout(LayoutKind.Sequential, Size = 2048)] struct S2048 { public long B; }
[StructLayout(LayoutKind.Sequential, Size = 4096)] struct S4096 { public long B; }
[StructLayout(LayoutKind.Sequential, Size = 70000)] struct S70000 { public long B; }
[StructLayout(LayoutKind.Sequential, Size = 100000)] struct S100000 { public long B; }

class Empty { }
class Obj<T> where T : unmanaged { public T Body; }
class Derived<T> : Obj<T> where T : unmanaged { public long Tail; }
class RefObj<T> where T : class { public T R; public long X; }
class WithFinalizer { public long A, B, C; ~WithFinalizer() { } }
class Plain { public long A, B, C; public object R; }
struct WithRef { public object R; public int X; public long Y; }
enum Color : short { Red = 1, Blue = 0x1234 }
class Maker<T> where T : class, new() { [MethodImpl(MethodImplOptions.NoInlining)] public static T Make() => new T(); }

static unsafe class Program
{
    [DllImport("*")] static extern void* malloc(nuint n);
    [DllImport("*")] static extern void* calloc(nuint n, nuint m);
    [DllImport("*")] static extern void* realloc(void* p, nuint n);
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

    // ---- the allocation sites -----------------------------------------------
    [MethodImpl(MethodImplOptions.NoInlining)] static Obj<T> New<T>() where T : unmanaged => new Obj<T>();
    [MethodImpl(MethodImplOptions.NoInlining)] static Derived<T> NewDerived<T>() where T : unmanaged => new Derived<T>();
    [MethodImpl(MethodImplOptions.NoInlining)] static object Box<T>(T v) where T : struct => v;
    [MethodImpl(MethodImplOptions.NoInlining)] static object BoxNullable<T>(T? v) where T : struct => v;
    [MethodImpl(MethodImplOptions.NoInlining)] static RefObj<T> NewRef<T>() where T : class => new RefObj<T>();
    [MethodImpl(MethodImplOptions.NoInlining)] static Empty NewEmpty() => new Empty();
    [MethodImpl(MethodImplOptions.NoInlining)] static object NewObject() => new object();
    [MethodImpl(MethodImplOptions.NoInlining)] static WithFinalizer NewFinalizable() => new WithFinalizer();
    [MethodImpl(MethodImplOptions.NoInlining)] static Plain NewPlain() => new Plain();
    [MethodImpl(MethodImplOptions.NoInlining)] static object BoxWithRef(object r, int x) => new WithRef { R = r, X = x, Y = ~(long)x };

    // Dirty a block of the C heap and give it back.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void DirtyNative(int size)
    {
        byte* p = (byte*)malloc((nuint)size);
        new Span<byte>(p, size).Fill(0xA5);
        p = (byte*)realloc(p, (nuint)(size * 2));
        new Span<byte>(p, size * 2).Fill(0x5A);
        free(p);
        byte* q = (byte*)calloc(3, (nuint)size);
        long sum = 0;
        for (int i = 0; i < size * 3; i++) sum += q[i];
        Check("calloc is zero " + size, 0, sum);
        new Span<byte>(q, size * 3).Fill(0xC3);
        free(q);
    }

    static byte Pattern(int seed, int i) => (byte)(seed * 131 + i * 7 + (i >> 8) + 1);

    static long NonZero(ref byte b, int size)
    {
        long n = 0;
        for (int i = 0; i < size; i++) if (Unsafe.Add(ref b, i) != 0) n++;
        return n;
    }

    static void Fill(ref byte b, int size, int seed)
    {
        for (int i = 0; i < size; i++) Unsafe.Add(ref b, i) = Pattern(seed, i);
    }

    static long Wrong(ref byte b, int size, int seed)
    {
        long n = 0;
        for (int i = 0; i < size; i++) if (Unsafe.Add(ref b, i) != Pattern(seed, i)) n++;
        return n;
    }

    // Address ranges of everything made so far in a case: [start, end).
    static long[] rangeStart = new long[64];
    static long[] rangeEnd = new long[64];
    static int ranges;

    static void AddRange(string name, object o, int instanceBytes)
    {
        long a = Addr(o);
        Check(name + " alignment", 0, a & 7);
        long start = a - 8;                               // the header word
        long end = a + ((instanceBytes + 7) & ~7);
        for (int i = 0; i < ranges; i++)
            if (start < rangeEnd[i] && rangeStart[i] < end)
            {
                fails++;
                Console.WriteLine("FAIL " + name + " overlaps object " + i + ": [" + start.ToString("x") + "," + end.ToString("x") +
                                  ") and [" + rangeStart[i].ToString("x") + "," + rangeEnd[i].ToString("x") + ")");
            }
        if (ranges < rangeStart.Length) { rangeStart[ranges] = start; rangeEnd[ranges] = end; ranges++; }
    }

    static void SizeClass<T>(string name) where T : unmanaged
    {
        const int N = 6;
        int size = sizeof(T);
        ranges = 0;
        var keep = new Obj<T>[N];
        var keepDerived = new Derived<T>[N];
        int[] hash = new int[N];
        for (int i = 0; i < N; i++)
        {
            Obj<T> o = New<T>();
            Check(name + " type " + i, 1, o.GetType() == typeof(Obj<T>) ? 1 : 0);
            Check(name + " zero " + i, 0, NonZero(ref Unsafe.As<T, byte>(ref o.Body), size));
            AddRange(name + " " + i, o, 8 + size);
            Fill(ref Unsafe.As<T, byte>(ref o.Body), size, i);
            keep[i] = o;

            // Something else between two objects of the class: a native block
            // that is dirtied and freed, a string, an array, a derived object.
            switch (i % 4)
            {
                case 0: DirtyNative(size < 64 ? 64 : (size > 4096 ? 4096 : size)); break;
                case 1: s_sink = new string('q', Opaque(i + 5)); break;
                case 2: s_sink = new long[Opaque(i)]; break;
            }
            Derived<T> d = NewDerived<T>();
            Check(name + " derived type " + i, 1, d.GetType() == typeof(Derived<T>) ? 1 : 0);
            Check(name + " derived zero " + i, 0, NonZero(ref Unsafe.As<T, byte>(ref d.Body), size) + (d.Tail != 0 ? 1 : 0));
            AddRange(name + " derived " + i, d, 8 + ((size + 7) & ~7) + 8);
            Fill(ref Unsafe.As<T, byte>(ref d.Body), size, i + 100);
            d.Tail = 0x0102030405060708L * (i + 1);
            keepDerived[i] = d;
        }

        // The header word: a hash code and a lock on every second object.
        for (int i = 0; i < N; i++) hash[i] = RuntimeHelpers.GetHashCode(keep[i]);
        for (int i = 0; i < N; i += 2) lock (keepDerived[i]) { lock (keepDerived[i]) { s_sink = keepDerived[i]; } }
        for (int i = 0; i < N; i++)
        {
            Check(name + " kept " + i, 0, Wrong(ref Unsafe.As<T, byte>(ref keep[i].Body), size, i));
            Check(name + " derived kept " + i, 0, Wrong(ref Unsafe.As<T, byte>(ref keepDerived[i].Body), size, i + 100));
            Check(name + " derived tail " + i, 0x0102030405060708L * (i + 1), keepDerived[i].Tail);
            Check(name + " type after " + i, 1, keep[i].GetType() == typeof(Obj<T>) && keepDerived[i] is Obj<T> ? 1 : 0);
        }
        // Rewrite every byte and see that no header changed.
        for (int i = 0; i < N; i++)
        {
            Fill(ref Unsafe.As<T, byte>(ref keep[i].Body), size, i + 7);
            Fill(ref Unsafe.As<T, byte>(ref keepDerived[i].Body), size, i + 9);
            keepDerived[i].Tail = -1;
        }
        for (int i = 0; i < N; i++)
        {
            Check(name + " hash " + i, hash[i], RuntimeHelpers.GetHashCode(keep[i]));
            Check(name + " type last " + i, 1, keep[i].GetType() == typeof(Obj<T>) && keepDerived[i].GetType() == typeof(Derived<T>) ? 1 : 0);
        }
    }

    static void BoxClass<T>(string name, T a, T b) where T : unmanaged
    {
        const int N = 5;
        int size = sizeof(T);
        ranges = 0;
        object[] keep = new object[N * 2];
        int[] hash = new int[N * 2];
        for (int i = 0; i < N; i++)
        {
            object x = Box(a);
            if ((i & 1) == 0) DirtyNative(64);
            object y = Box(b);
            Check(name + " box type " + i, 1, x.GetType() == typeof(T) && y.GetType() == typeof(T) ? 1 : 0);
            AddRange(name + " box a " + i, x, 8 + size);
            AddRange(name + " box b " + i, y, 8 + size);
            keep[i * 2] = x;
            keep[i * 2 + 1] = y;
        }
        for (int i = 0; i < N * 2; i++) hash[i] = RuntimeHelpers.GetHashCode(keep[i]);
        for (int i = 0; i < N * 2; i++)
        {
            T v = (T)keep[i];
            T e = (i & 1) == 0 ? a : b;
            Check(name + " box value " + i, 1, new ReadOnlySpan<byte>(&v, size).SequenceEqual(new ReadOnlySpan<byte>(&e, size)) ? 1 : 0);
            Check(name + " box hash " + i, hash[i], RuntimeHelpers.GetHashCode(keep[i]));
        }
        object n1 = BoxNullable<T>(a), n0 = BoxNullable<T>(null);
        Check(name + " nullable box", 1, n1 != null && n1.GetType() == typeof(T) && n0 == null ? 1 : 0);
    }

    static void CaseSizes()
    {
        SizeClass<byte>("byte");
        SizeClass<short>("short");
        SizeClass<S3>("s3");
        SizeClass<int>("int");
        SizeClass<long>("long");
        SizeClass<S12>("s12");
        SizeClass<Guid>("guid");
        SizeClass<Int128>("int128");
        SizeClass<S20>("s20");
        SizeClass<S24>("s24");
        SizeClass<S40>("s40");
        SizeClass<S56>("s56");
        SizeClass<S100>("s100");
        SizeClass<S255>("s255");
        SizeClass<S1000>("s1000");
        SizeClass<S2024>("s2024");
        SizeClass<S2032>("s2032");
        SizeClass<S2040>("s2040");
        SizeClass<S2048>("s2048");
        SizeClass<S4096>("s4096");
        SizeClass<S70000>("s70000");
        SizeClass<S100000>("s100000");
    }

    static void CaseBoxes()
    {
        BoxClass<byte>("byte", 0x7F, 0x80);
        BoxClass<bool>("bool", true, false);
        BoxClass<char>("char", 'a', '￿');
        BoxClass<short>("short", -2, 0x7FFF);
        BoxClass<Color>("enum", Color.Blue, Color.Red);
        BoxClass<int>("int", int.MinValue, 0x12345678);
        BoxClass<long>("long", long.MinValue, 0x123456789ABCDEF0);
        BoxClass<S3>("s3", new S3 { B = 0xEE }, new S3 { B = 1 });
        BoxClass<S12>("s12", new S12 { B = -1 }, new S12 { B = 5 });
        BoxClass<Guid>("guid", new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new Guid(-1, -2, -3, 255, 254, 253, 252, 251, 250, 249, 248));
        BoxClass<Int128>("int128", Int128.MinValue, Int128.MaxValue);
        BoxClass<decimal>("decimal", 1.5m, decimal.MinValue);
        BoxClass<S24>("s24", new S24 { B = -1 }, new S24 { B = 2 });
        BoxClass<S40>("s40", new S40 { B = long.MaxValue }, new S40 { B = 3 });
        BoxClass<S255>("s255", new S255 { B = 0xFF }, new S255 { B = 7 });
        BoxClass<S2040>("s2040", new S2040 { B = -7 }, new S2040 { B = 7 });
        BoxClass<S4096>("s4096", new S4096 { B = -9 }, new S4096 { B = 9 });

        ranges = 0;
        object[] refs = new object[8];
        for (int i = 0; i < 8; i++)
        {
            refs[i] = BoxWithRef(i == 0 ? "first" : refs[i - 1], i * 1000 + 1);
            AddRange("box with ref " + i, refs[i], 8 + sizeof(WithRef));
        }
        for (int i = 0; i < 8; i++)
        {
            int h = RuntimeHelpers.GetHashCode(refs[i]);
            WithRef w = (WithRef)refs[i];
            Check("box with ref x " + i, i * 1000 + 1, w.X);
            Check("box with ref y " + i, ~(long)(i * 1000 + 1), w.Y);
            Check("box with ref r " + i, 1, ReferenceEquals(w.R, i == 0 ? "first" : refs[i - 1]) ? 1 : 0);
            Check("box with ref hash " + i, h, RuntimeHelpers.GetHashCode(refs[i]));
        }
    }

    static void CaseKinds()
    {
        ranges = 0;
        object[] keep = new object[40];
        int n = 0;
        for (int i = 0; i < 4; i++)
        {
            Empty e = NewEmpty();
            Check("empty type " + i, 1, e.GetType() == typeof(Empty) ? 1 : 0);
            AddRange("empty " + i, e, 8);
            keep[n++] = e;
            object o = NewObject();
            Check("object type " + i, 1, o.GetType() == typeof(object) ? 1 : 0);
            AddRange("object " + i, o, 8);
            keep[n++] = o;
            DirtyNative(128);
            WithFinalizer f = NewFinalizable();
            Check("finalizable type " + i, 1, f.GetType() == typeof(WithFinalizer) ? 1 : 0);
            Check("finalizable zero " + i, 0, f.A | f.B | f.C);
            AddRange("finalizable " + i, f, 32);
            f.A = f.B = f.C = -1;
            keep[n++] = f;
            Plain p = NewPlain();
            Check("plain zero " + i, 0, (p.A | p.B | p.C) + (p.R == null ? 0 : 1));
            AddRange("plain " + i, p, 40);
            p.A = p.B = p.C = -1; p.R = f;
            keep[n++] = p;

            RefObj<string> rs = NewRef<string>();
            RefObj<object> ro = NewRef<object>();
            RefObj<int[]> ra = NewRef<int[]>();
            Check("shared generic type " + i, 1, rs.GetType() == typeof(RefObj<string>) && ro.GetType() == typeof(RefObj<object>) &&
                                                  ra.GetType() == typeof(RefObj<int[]>) ? 1 : 0);
            Check("shared generic zero " + i, 0, rs.X | ro.X | ra.X | (rs.R == null && ro.R == null && ra.R == null ? 0 : 1));
            AddRange("shared generic string " + i, rs, 24);
            AddRange("shared generic object " + i, ro, 24);
            AddRange("shared generic array " + i, ra, 24);
            rs.X = ro.X = ra.X = -1; rs.R = "s"; ro.R = rs; ra.R = new int[1];
            keep[n++] = rs; keep[n++] = ro; keep[n++] = ra;

            Plain viaNewT = Maker<Plain>.Make();
            Check("new T type " + i, 1, viaNewT.GetType() == typeof(Plain) ? 1 : 0);
            Check("new T zero " + i, 0, (viaNewT.A | viaNewT.B | viaNewT.C) + (viaNewT.R == null ? 0 : 1));
            viaNewT.A = viaNewT.B = viaNewT.C = -1;
            keep[n++] = viaNewT;
        }
        for (int i = 0; i < n; i++) s_sink = RuntimeHelpers.GetHashCode(keep[i]);
        for (int i = 0; i < n; i++)
        {
            if (keep[i] is Plain p) Check("plain kept " + i, -1, p.A & p.B & p.C);
            if (keep[i] is WithFinalizer f) Check("finalizable kept " + i, -1, f.A & f.B & f.C);
        }
    }

    // Three allocation sites and a loop in one method: the cell address and
    // the floor are shared between the sites; calls in between move the same
    // pointer behind the JIT's back.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long ManyInOneMethod(int n, Plain[] outA, Obj<S40>[] outB)
    {
        long sum = 0;
        Plain first = new Plain();
        first.A = 1;
        for (int i = 0; i < n; i++)
        {
            Plain a = new Plain();
            sum += a.A | a.B | a.C;                   // must be zero
            a.A = i; a.B = ~i; a.C = i * 3L; a.R = first;
            if ((i & 3) == 1) DirtyNative(48);
            Obj<S40> b = new Obj<S40>();
            sum += b.Body.B;                          // must be zero
            b.Body.B = i * 5L;
            if ((i & 3) == 2) s_sink = new string('z', Opaque(i & 15));
            object boxed = (long)i;
            sum += (long)boxed - i;                   // zero
            outA[i] = a;
            outB[i] = b;
            long gap = Addr(a) - Addr(b);
            first.B += gap >= 56 || gap <= -48 ? 0 : 1; // 40 and 48 bytes, and the header words
        }
        return sum + first.B;
    }

    static void CaseOneMethod()
    {
        const int N = 300;
        var a = new Plain[N];
        var b = new Obj<S40>[N];
        Check("many in one method", 0, ManyInOneMethod(N, a, b));
        long bad = 0;
        for (int i = 0; i < N; i++)
        {
            if (a[i].A != i || a[i].B != ~i || a[i].C != i * 3L || a[i].R != a[0].R) bad++;
            if (b[i].Body.B != i * 5L) bad++;
            if (i > 0 && (Addr(a[i]) == Addr(a[i - 1]) || Addr(b[i]) == Addr(b[i - 1]))) bad++;
        }
        Check("many in one method kept", 0, bad);
    }

    // A few megabytes of small objects, linked, read back at the end.
    static void CaseVolume()
    {
        const int N = 120000;                       // about 6 MB with the header words
        Plain head = null;
        long dirty = 0;
        for (int i = 0; i < N; i++)
        {
            Plain p = NewPlain();
            dirty += (p.A | p.B | p.C) + (p.R == null ? 0 : 1);
            p.A = i; p.B = i * 3L; p.C = ~(long)i; p.R = head;
            head = p;
            if (i % 9973 == 0) DirtyNative(256);
        }
        Check("volume zero", 0, dirty);
        long bad = 0, count = 0;
        long prev = 0;
        for (Plain p = head; p != null; p = (Plain)p.R)
        {
            long i = N - 1 - count;
            if (p.A != i || p.B != i * 3L || p.C != ~i) bad++;
            long a = Addr(p);
            if (prev != 0 && (a - prev < (IntPtr.Size == 8 ? 48 : 32) && prev - a < (IntPtr.Size == 8 ? 48 : 32))) bad++;   // 40 bytes and the header word
            prev = a;
            count++;
        }
        Check("volume count", N, count);
        Check("volume kept", 0, bad);
    }

    static int Main()
    {
        CaseSizes();
        CaseBoxes();
        CaseKinds();
        CaseOneMethod();
        CaseVolume();

        Console.WriteLine(fails == 0 ? "ALL PASS" : "FAILED " + fails);
        return fails;
    }
}
