// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Interface calls that go through the dispatch helper and its caches.
//
// An interface call that is not resolved at compile time loads the address
// of a dispatch cell and jumps to a helper written in assembly. The helper
// compares the receiver's type with the one type the cell remembers, then
// probes a global hash table keyed by (cell, type), and only then calls the
// resolver. The resolver finds what the cell stands for (interface and
// slot) from a table the compiler wrote, where only every second cell of a
// run has its own entry and the others are found by stepping back; the
// tables of a module are looked up once and kept. What can go wrong:
//
//   - the helper: a hash or a probe sequence that differs from the one the
//     table is filled with (every call misses - slow but right), a compare
//     of half a key (a hit for another cell or another type - wrong
//     target), the probe count kept in the upper bits of the index, an
//     argument register or the stack disturbed on the way to the target;
//   - the cell that remembers one type: a second type at the same site, and
//     the first type again;
//   - the table of cells: a cell resolved as its neighbour (off by one in
//     the step back, at the start and the end of a run, in runs of odd and
//     even length), cells of generic virtual methods, cells in shared
//     generic code;
//   - the kinds of target: a method of a generic class, a default interface
//     method, a method of a boxed struct (through an unboxing stub), a
//     variant interface, an array as IList<T>, a generic virtual method.
//
// The receivers are 256 classes made from one generic class, so no site
// here can be resolved at compile time or guarded with a type test; every
// method returns a value built from the identity of its class, its own
// number and its object. The expected value is written when the object is
// made and does not go through an interface.
//
// A null receiver faults in the first instruction of the helper. A zkVM
// guest has no signals to turn that into a NullReferenceException, so the
// null receivers run only when built with -d NULL_RECEIVERS.
//
// Each case prints one line on failure; the run ends with ALL PASS or a FAIL
// count, and the guest returns the number of failures.
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

static class TypeTag<T> { public static int Tag; }

interface IVal { long Val(); long Val2(long x); }
interface IOther { int Other(); }

class G<A, B> : IVal, IOther where A : unmanaged where B : unmanaged
{
    public int Id;
    public long Val() => Id * 10000L + TypeTag<A>.Tag * 100 + TypeTag<B>.Tag;
    public long Val2(long x) => x ^ Val();
    public int Other() => TypeTag<B>.Tag * 100 + TypeTag<A>.Tag;
}

// One interface with many methods, called from many sites.
interface IMany
{
    int M0(); int M1(); int M2(); int M3(); int M4(); int M5(); int M6(); int M7();
    int M8(); int M9(); int M10(); int M11(); int M12(); int M13(); int M14(); int M15();
    int M16(); int M17(); int M18(); int M19(); int M20(); int M21(); int M22(); int M23();
}

class Many<T> : IMany where T : unmanaged
{
    public int Tag;
    public int M0() => Tag + 0; public int M1() => Tag + 1; public int M2() => Tag + 2; public int M3() => Tag + 3;
    public int M4() => Tag + 4; public int M5() => Tag + 5; public int M6() => Tag + 6; public int M7() => Tag + 7;
    public int M8() => Tag + 8; public int M9() => Tag + 9; public int M10() => Tag + 10; public int M11() => Tag + 11;
    public int M12() => Tag + 12; public int M13() => Tag + 13; public int M14() => Tag + 14; public int M15() => Tag + 15;
    public int M16() => Tag + 16; public int M17() => Tag + 17; public int M18() => Tag + 18; public int M19() => Tag + 19;
    public int M20() => Tag + 20; public int M21() => Tag + 21; public int M22() => Tag + 22; public int M23() => Tag + 23;
}

// 24 sites per instantiation. A cell belongs to a pair of target and calling
// method, so six instantiations give a run of six cells per method.
static class Sites<T> where T : unmanaged
{
    const MethodImplOptions NoInl = MethodImplOptions.NoInlining;
    [MethodImpl(NoInl)] public static int C0(IMany x) => x.M0(); [MethodImpl(NoInl)] public static int C1(IMany x) => x.M1();
    [MethodImpl(NoInl)] public static int C2(IMany x) => x.M2(); [MethodImpl(NoInl)] public static int C3(IMany x) => x.M3();
    [MethodImpl(NoInl)] public static int C4(IMany x) => x.M4(); [MethodImpl(NoInl)] public static int C5(IMany x) => x.M5();
    [MethodImpl(NoInl)] public static int C6(IMany x) => x.M6(); [MethodImpl(NoInl)] public static int C7(IMany x) => x.M7();
    [MethodImpl(NoInl)] public static int C8(IMany x) => x.M8(); [MethodImpl(NoInl)] public static int C9(IMany x) => x.M9();
    [MethodImpl(NoInl)] public static int C10(IMany x) => x.M10(); [MethodImpl(NoInl)] public static int C11(IMany x) => x.M11();
    [MethodImpl(NoInl)] public static int C12(IMany x) => x.M12(); [MethodImpl(NoInl)] public static int C13(IMany x) => x.M13();
    [MethodImpl(NoInl)] public static int C14(IMany x) => x.M14(); [MethodImpl(NoInl)] public static int C15(IMany x) => x.M15();
    [MethodImpl(NoInl)] public static int C16(IMany x) => x.M16(); [MethodImpl(NoInl)] public static int C17(IMany x) => x.M17();
    [MethodImpl(NoInl)] public static int C18(IMany x) => x.M18(); [MethodImpl(NoInl)] public static int C19(IMany x) => x.M19();
    [MethodImpl(NoInl)] public static int C20(IMany x) => x.M20(); [MethodImpl(NoInl)] public static int C21(IMany x) => x.M21();
    [MethodImpl(NoInl)] public static int C22(IMany x) => x.M22(); [MethodImpl(NoInl)] public static int C23(IMany x) => x.M23();

    public static int Call(int k, IMany x)
    {
        switch (k)
        {
            case 0: return C0(x); case 1: return C1(x); case 2: return C2(x); case 3: return C3(x);
            case 4: return C4(x); case 5: return C5(x); case 6: return C6(x); case 7: return C7(x);
            case 8: return C8(x); case 9: return C9(x); case 10: return C10(x); case 11: return C11(x);
            case 12: return C12(x); case 13: return C13(x); case 14: return C14(x); case 15: return C15(x);
            case 16: return C16(x); case 17: return C17(x); case 18: return C18(x); case 19: return C19(x);
            case 20: return C20(x); case 21: return C21(x); case 22: return C22(x); default: return C23(x);
        }
    }

    [MethodImpl(NoInl)] public static long Mega(IVal v) => v.Val();
}

// ---- other kinds of target ---------------------------------------------------
interface IGet<T> { T Get(); int Width(); }
class GetImpl<T> : IGet<T> { public T V; public T Get() => V; public int Width() => Unsafe.SizeOf<T>(); }
class GetInt : IGet<int> { public int Get() => 41; public int Width() => -4; }
class GetString : IGet<string> { public string Get() => "fixed"; public int Width() => -8; }
class GetBoth : IGet<int>, IGet<string>, IGet<long>
{
    int IGet<int>.Get() => 1; int IGet<int>.Width() => 101;
    string IGet<string>.Get() => "both"; int IGet<string>.Width() => 102;
    long IGet<long>.Get() => 3; int IGet<long>.Width() => 103;
}

class Animal { public virtual int Legs => 0; }
class Dog : Animal { public override int Legs => 4; }
class Bird : Animal { public override int Legs => 2; }
interface IOut<out T> { T Make(); }
interface IIn<in T> { int Take(T t); }
class MakeAnimal : IOut<Animal> { public Animal Make() => new Animal(); }
class MakeDog : IOut<Dog> { public Dog Make() => new Dog(); }
class MakeBird : IOut<Bird> { public Bird Make() => new Bird(); }
class MakeAny<T> : IOut<T> where T : new() { public T Make() => new T(); }
class TakeAnimal : IIn<Animal> { public int Take(Animal a) => 100 + a.Legs; }
class TakeDog : IIn<Dog> { public int Take(Dog d) => 200 + d.Legs; }
class TakeObject : IIn<object> { public int Take(object o) => 300 + (o is Animal a ? a.Legs : 9); }
class TakeAny<T> : IIn<T> { public int Take(T t) => 400 + (t is Animal a ? a.Legs : 9); }

interface IDefault { int Plain(); int WithDefault() => 1000 + Plain(); int Other() => 5; }
interface IDefaultLeft : IDefault { int IDefault.WithDefault() => 2000 + Plain(); }
interface IDefaultRight : IDefault { int IDefault.Other() => 6; }
class Def1 : IDefault { public int Plain() => 1; }
class Def2 : IDefault { public int Plain() => 2; public int WithDefault() => 20; }
class Def3 : IDefaultLeft { public int Plain() => 3; }
class Def4 : IDefaultLeft, IDefaultRight { public int Plain() => 4; }
class Def5 : Def1 { }
class Def6 : Def3, IDefault { int IDefault.WithDefault() => 60; }
struct DefStruct : IDefault { public int P; public int Plain() => P; }
class Def7<T> : IDefault { public int Plain() => Unsafe.SizeOf<T>(); }

interface IGvm { T Echo<T>(T x); int Width<T>(); string Name<T>(T x); }
class Gvm1 : IGvm { public T Echo<T>(T x) => x; public int Width<T>() => Unsafe.SizeOf<T>(); public string Name<T>(T x) => "1:" + typeof(T).Name; }
class Gvm2 : IGvm { public T Echo<T>(T x) => default; public int Width<T>() => -Unsafe.SizeOf<T>(); public string Name<T>(T x) => "2:" + typeof(T).Name; }
class Gvm3<U> : IGvm { public T Echo<T>(T x) => x; public int Width<T>() => Unsafe.SizeOf<T>() * 100 + Unsafe.SizeOf<U>(); public string Name<T>(T x) => "3:" + typeof(U).Name + typeof(T).Name; }
abstract class GvmBase { public abstract int Twice<T>(T x) where T : struct; }
class GvmA : GvmBase { public override int Twice<T>(T x) => Unsafe.SizeOf<T>() * 2; }
class GvmB : GvmBase { public override int Twice<T>(T x) => Unsafe.SizeOf<T>() * 3; }

interface ICount { int Inc(); int Read(); }
struct Counter : ICount { public int N; public int Inc() => ++N; public int Read() => N; }
struct WideCounter : ICount { public long Pad0, Pad1; public int N; public int Inc() => N += 10; public int Read() => N; }
class ClassCounter : ICount { public int N; public int Inc() => N += 100; public int Read() => N; }
struct GenCounter<T> : ICount { public T Pad; public int N; public int Inc() => N += 1000; public int Read() => N; }

[StructLayout(LayoutKind.Sequential)] struct Big { public long A, B, C, D, E; }
interface IBig
{
    long Sum12(long a0, long a1, long a2, long a3, long a4, long a5, long a6, long a7, long a8, long a9, long a10, long a11);
    Big Build(long seed, in Big other, ref long acc);
    Guid MakeGuid(int a, short b);
    long Mixed(byte a, short b, int c, long d, bool e, char f, sbyte g, ushort h, uint i, ulong j);
}
class Big<T> : IBig where T : unmanaged
{
    public long K;
    public long Sum12(long a0, long a1, long a2, long a3, long a4, long a5, long a6, long a7, long a8, long a9, long a10, long a11) =>
        K + a0 + 2 * a1 + 3 * a2 + 4 * a3 + 5 * a4 + 6 * a5 + 7 * a6 + 8 * a7 + 9 * a8 + 10 * a9 + 11 * a10 + 12 * a11;
    public Big Build(long seed, in Big other, ref long acc)
    {
        acc += K + seed;
        return new Big { A = seed + K, B = other.B * 2, C = other.C + other.A, D = ~seed, E = other.E - K };
    }
    public Guid MakeGuid(int a, short b) => new Guid(a, b, (short)K, 1, 2, 3, 4, 5, 6, 7, 8);
    public long Mixed(byte a, short b, int c, long d, bool e, char f, sbyte g, ushort h, uint i, ulong j) =>
        K + a + b * 3L + c * 5L + d * 7 + (e ? 11 : 13) + f * 17L + g * 19L + h * 23L + i * 29L + (long)j * 31;
}

interface IThrow { int MayThrow(int x); }
class Throw<T> : IThrow { public int MayThrow(int x) => x < 0 ? throw new InvalidOperationException("t" + Unsafe.SizeOf<T>()) : x + Unsafe.SizeOf<T>(); }

interface IVisit { long Visit(IVisit next, int depth); }
class Visitor<T> : IVisit where T : unmanaged
{
    public long Visit(IVisit next, int depth) => depth == 0 ? sizeof_() : sizeof_() + next.Visit(this, depth - 1) * 3 % 1000003;
    static int sizeof_() => Unsafe.SizeOf<T>();
}

interface IStatic<TSelf> where TSelf : IStatic<TSelf> { static abstract int Id(); static virtual int Twice() => TSelf.Id() * 2; }
struct StaticA : IStatic<StaticA> { public static int Id() => 21; }
struct StaticB : IStatic<StaticB> { public static int Id() => 34; public static int Twice() => -1; }

static class Program
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

    static void CheckText(string name, string expected, string got)
    {
        if (expected != got)
        {
            fails++;
            Console.WriteLine("FAIL " + name + " expected " + expected + " got " + got);
        }
    }

    [MethodImpl(NoInl)] static T Opaque<T>(T x) => x;

    // ---- the 256 receivers ------------------------------------------------------
    static readonly object[] items = new object[256];
    static readonly long[] expVal = new long[256];
    static readonly int[] expOther = new int[256];
    static int count;

    static void Add<A, B>(int ta, int tb) where A : unmanaged where B : unmanaged
    {
        TypeTag<A>.Tag = ta;
        TypeTag<B>.Tag = tb;
        int id = count + 1;
        items[count] = new G<A, B> { Id = id };
        expVal[count] = id * 10000L + ta * 100 + tb;
        expOther[count] = tb * 100 + ta;
        count++;
    }

    static void Row<A>(int ta) where A : unmanaged
    {
        Add<A, byte>(ta, 1); Add<A, sbyte>(ta, 2); Add<A, short>(ta, 3); Add<A, ushort>(ta, 4);
        Add<A, int>(ta, 5); Add<A, uint>(ta, 6); Add<A, long>(ta, 7); Add<A, ulong>(ta, 8);
        Add<A, char>(ta, 9); Add<A, bool>(ta, 10); Add<A, nint>(ta, 11); Add<A, nuint>(ta, 12);
        Add<A, Guid>(ta, 13); Add<A, decimal>(ta, 14); Add<A, Int128>(ta, 15); Add<A, DateTime>(ta, 16);
    }

    static void MakeReceivers()
    {
        Row<byte>(1); Row<sbyte>(2); Row<short>(3); Row<ushort>(4);
        Row<int>(5); Row<uint>(6); Row<long>(7); Row<ulong>(8);
        Row<char>(9); Row<bool>(10); Row<nint>(11); Row<nuint>(12);
        Row<Guid>(13); Row<decimal>(14); Row<Int128>(15); Row<DateTime>(16);
    }

    // ---- call sites ----------------------------------------------------------------
    [MethodImpl(NoInl)] static long SiteMono(IVal v) => v.Val();
    [MethodImpl(NoInl)] static long SitePoly(IVal v) => v.Val();
    [MethodImpl(NoInl)] static long SiteMega(IVal v) => v.Val();
    [MethodImpl(NoInl)] static long SiteMega2(IVal v, long x) => v.Val2(x);
    [MethodImpl(NoInl)] static int SiteOther(IOther o) => o.Other();
    [MethodImpl(NoInl)] static long SiteTwo(IVal v, IOther o) => v.Val() * 100000 + o.Other();
    [MethodImpl(NoInl)] static long SiteCast(object o) => ((IVal)o).Val() + ((IOther)o).Other();
    [MethodImpl(NoInl)] static long SiteIs(object o) => o is IVal v ? v.Val() : -1;
    [MethodImpl(NoInl)]
    static long SiteLoop(object[] all, int from, int step, int n)
    {
        long sum = 0;
        for (int i = 0, k = from; i < n; i++, k = (k + step) & 255)
        {
            IVal v = (IVal)all[k];
            sum = sum * 31 + v.Val() + v.Val2(i) + ((IOther)all[k]).Other();
        }
        return sum;
    }

    static long Mismatches(string name, long[] expected, Func<int, long> call, int from, int step, int n)
    {
        long bad = 0;
        for (int i = 0, k = from; i < n; i++, k = (k + step) & 255)
        {
            long got = call(k);
            if (got != expected[k])
            {
                if (bad == 0) Console.WriteLine("  " + name + ": receiver " + k + " expected " + expected[k] + " got " + got);
                bad++;
            }
        }
        return bad;
    }

    static void CaseOneType()
    {
        IVal one = (IVal)items[77];
        long bad = 0;
        for (int i = 0; i < 1000; i++) if (SiteMono(one) != expVal[77]) bad++;
        Check("one type at a site", 0, bad);
    }

    static void CaseFewTypes()
    {
        // One type, then a second, then the first again; then four in turn.
        long bad = 0;
        int[] order = { 3, 3, 3, 200, 3, 200, 200, 3, 3, 17, 90, 3, 200, 17, 90, 90, 17, 200, 3 };
        for (int round = 0; round < 20; round++)
            for (int i = 0; i < order.Length; i++)
                if (SitePoly((IVal)items[order[i]]) != expVal[order[i]]) bad++;
        Check("few types at a site", 0, bad);
    }

    static void CaseManyTypes()
    {
        Check("many types, first calls", 0, Mismatches("first", expVal, k => SiteMega((IVal)items[k]), 0, 1, 256));
        Check("many types, again", 0, Mismatches("again", expVal, k => SiteMega((IVal)items[k]), 0, 1, 256));
        Check("many types, backwards", 0, Mismatches("backwards", expVal, k => SiteMega((IVal)items[k]), 255, 255, 256));
        Check("many types, stride 37", 0, Mismatches("stride", expVal, k => SiteMega((IVal)items[k]), 5, 37, 256 * 3));
        long[] expX = new long[256];
        for (int i = 0; i < 256; i++) expX[i] = 0x5555AAAA5555AAAAL ^ expVal[i];
        Check("many types, second method", 0, Mismatches("second", expX, k => SiteMega2((IVal)items[k], 0x5555AAAA5555AAAAL), 9, 101, 256 * 2));
        long[] expO = new long[256];
        for (int i = 0; i < 256; i++) expO[i] = expOther[i];
        Check("many types, second interface", 0, Mismatches("other", expO, k => SiteOther((IOther)items[k]), 0, 3, 256 * 2));
        long[] expTwo = new long[256];
        for (int i = 0; i < 256; i++) expTwo[i] = expVal[i] * 100000 + expOther[255 - i];
        Check("many types, two sites in a method", 0, Mismatches("two", expTwo, k => SiteTwo((IVal)items[k], (IOther)items[255 - k]), 0, 1, 256));
        long[] expCast = new long[256];
        for (int i = 0; i < 256; i++) expCast[i] = expVal[i] + expOther[i];
        Check("many types, after a cast", 0, Mismatches("cast", expCast, k => SiteCast(items[k]), 0, 7, 256));
        Check("many types, after a type test", 0, Mismatches("is", expVal, k => SiteIs(items[k]), 0, 11, 256));
        Check("type test fails", -1, SiteIs("text"));

        long sum = 0;
        for (int i = 0, k = 13; i < 2000; i++, k = (k + 59) & 255) sum = sum * 31 + expVal[k] + (i ^ expVal[k]) + expOther[k];
        Check("many types in a loop", sum, SiteLoop(items, 13, 59, 2000));

        // Six more sites, each with all 256 types: more keys than the table starts with.
        long bad = 0;
        for (int round = 0; round < 2; round++)
            for (int k = 0; k < 256; k++)
            {
                IVal v = (IVal)items[(k * 77 + round) & 255];
                long e = expVal[(k * 77 + round) & 255];
                if (Sites<byte>.Mega(v) != e) bad++;
                if (Sites<short>.Mega(v) != e) bad++;
                if (Sites<int>.Mega(v) != e) bad++;
                if (Sites<long>.Mega(v) != e) bad++;
                if (Sites<Guid>.Mega(v) != e) bad++;
                if (Sites<decimal>.Mega(v) != e) bad++;
            }
        Check("many types at many sites", 0, bad);
        Check("many types, after the table grew", 0, Mismatches("grown", expVal, k => SiteMega((IVal)items[k]), 0, 1, 256));
    }

    // ---- many cells ------------------------------------------------------------------
    [MethodImpl(NoInl)]
    static long ExtraSites(IMany x)
    {
        // One more cell for M0, M3, M7 and M23: their runs have seven cells.
        long r = x.M0();
        r = r * 7 + x.M3();
        r = r * 7 + x.M7();
        r = r * 7 + x.M3();
        r = r * 7 + x.M23();
        r = r * 7 + x.M7();
        r = r * 7 + x.M7();
        return r;
    }

    static void CaseManyCells()
    {
        IMany[] impl =
        {
            new Many<byte> { Tag = 1000 }, new Many<short> { Tag = 2000 }, new Many<int> { Tag = 3000 },
            new Many<long> { Tag = 4000 }, new Many<Guid> { Tag = 5000 }, new Many<decimal> { Tag = 6000 },
            new Many<char> { Tag = 7000 },
        };
        long bad = 0;
        // Every cell is resolved for the first time with a different receiver,
        // in an order that is neither the order of the methods nor of the sites.
        for (int step = 0; step < 24 * 6; step++)
        {
            int k = (step * 7) % 24, site = (step * 5 + k) % 6;
            IMany x = impl[(step * 3) % impl.Length];
            int tag = ((step * 3) % impl.Length + 1) * 1000;
            int got = site == 0 ? Sites<byte>.Call(k, x) : site == 1 ? Sites<short>.Call(k, x) : site == 2 ? Sites<int>.Call(k, x)
                    : site == 3 ? Sites<long>.Call(k, x) : site == 4 ? Sites<Guid>.Call(k, x) : Sites<decimal>.Call(k, x);
            if (got != tag + k)
            {
                if (bad == 0) Console.WriteLine("  many cells: site " + site + " method " + k + " expected " + (tag + k) + " got " + got);
                bad++;
            }
        }
        Check("many cells, first calls", 0, bad);
        bad = 0;
        for (int k = 0; k < 24; k++)
            for (int t = 0; t < impl.Length; t++)
            {
                int e = (t + 1) * 1000 + k;
                if (Sites<byte>.Call(k, impl[t]) != e) bad++;
                if (Sites<short>.Call(k, impl[t]) != e) bad++;
                if (Sites<int>.Call(k, impl[t]) != e) bad++;
                if (Sites<long>.Call(k, impl[t]) != e) bad++;
                if (Sites<Guid>.Call(k, impl[t]) != e) bad++;
                if (Sites<decimal>.Call(k, impl[t]) != e) bad++;
            }
        Check("many cells, every site and type", 0, bad);
        for (int t = 0; t < impl.Length; t++)
        {
            long tag = (t + 1) * 1000, e = tag;
            e = e * 7 + tag + 3; e = e * 7 + tag + 7; e = e * 7 + tag + 3; e = e * 7 + tag + 23; e = e * 7 + tag + 7; e = e * 7 + tag + 7;
            Check("many cells, extra sites " + t, e, ExtraSites(impl[t]));
        }
    }

    // ---- generic interfaces --------------------------------------------------------------
    [MethodImpl(NoInl)] static int GetInt(IGet<int> g) => g.Get() * 1000 + g.Width();
    [MethodImpl(NoInl)] static long GetLong(IGet<long> g) => g.Get() * 1000 + g.Width();
    [MethodImpl(NoInl)] static string GetString(IGet<string> g) => g.Get() + g.Width();
    [MethodImpl(NoInl)] static T GetShared<T>(IGet<T> g) => g.Get();
    [MethodImpl(NoInl)] static int WidthShared<T>(IGet<T> g) => g.Width();
    [MethodImpl(NoInl)] static int Legs(IOut<Animal> o) => o.Make().Legs;
    [MethodImpl(NoInl)] static int Feed(IIn<Dog> i, Dog d) => i.Take(d);
    [MethodImpl(NoInl)] static int CountOf(IReadOnlyCollection<object> c) => c.Count;
    [MethodImpl(NoInl)] static object ItemOf(IReadOnlyList<object> c, int i) => c[i];
    [MethodImpl(NoInl)] static int SumOf(IEnumerable<int> c) { int s = 0; foreach (int x in c) s = s * 3 + x; return s; }
    [MethodImpl(NoInl)] static int IndexOf(IList<int> c, int x) => c.IndexOf(x);
    [MethodImpl(NoInl)] static int Compare<T>(IComparable<T> a, T b) => a.CompareTo(b);

    static void CaseGenericInterfaces()
    {
        IGet<int>[] ints = { new GetInt(), new GetImpl<int> { V = 7 }, new GetBoth(), new GetImpl<int> { V = -7 }, new GetInt() };
        int[] ei = { 41000 - 4, 7004, 1101, -6996, 41000 - 4 };
        for (int round = 0; round < 3; round++)
            for (int i = 0; i < ints.Length; i++) Check("generic interface int " + i, ei[i], GetInt(ints[i]));
        IGet<long>[] longs = { new GetImpl<long> { V = 1L << 40 }, new GetBoth() };
        Check("generic interface long 0", (1L << 40) * 1000 + 8, GetLong(longs[0]));
        Check("generic interface long 1", 3103, GetLong(longs[1]));
        IGet<string>[] strs = { new GetString(), new GetImpl<string> { V = "impl" }, new GetBoth() };
        string[] es = { "fixed-8", "impl" + IntPtr.Size, "both102" };
        for (int round = 0; round < 3; round++)
            for (int i = 0; i < strs.Length; i++)
            {
                CheckText("generic interface string " + i, es[i], GetString(strs[i]));
                CheckText("generic interface shared string " + i, es[i], GetShared(strs[i]) + WidthShared(strs[i]));
            }
        Check("generic interface shared int", 7, GetShared(ints[1]));
        Check("generic interface shared object", 5, ((int[])GetShared<object>(new GetImpl<object> { V = new int[5] })).Length);
        Check("generic interface shared array", 6, GetShared(new GetImpl<int[]> { V = new int[6] }).Length + WidthShared(new GetImpl<long[]>()) - IntPtr.Size);

        IOut<Animal>[] outs = { new MakeAnimal(), new MakeDog(), new MakeBird(), new MakeAny<Dog>(), new MakeAny<Animal>(), new MakeAny<Bird>() };
        int[] eo = { 0, 4, 2, 4, 0, 2 };
        for (int round = 0; round < 3; round++)
            for (int i = 0; i < outs.Length; i++) Check("covariant " + i, eo[i], Legs(outs[i]));
        IIn<Dog>[] ins = { new TakeAnimal(), new TakeDog(), new TakeObject(), new TakeAny<Dog>(), new TakeAny<Animal>(), new TakeAny<object>() };
        int[] en = { 104, 204, 304, 404, 404, 404 };
        Dog d = new Dog();
        for (int round = 0; round < 3; round++)
            for (int i = 0; i < ins.Length; i++) Check("contravariant " + i, en[i], Feed(ins[i], d));

        string[] words = { "a", "bb", "ccc" };
        var list = new List<string> { "x", "yy" };
        Check("array as collection of object", 3, CountOf(words));
        Check("list as collection of object", 2, CountOf(list));
        CheckText("array as list of object", "ccc", (string)ItemOf(words, 2));
        CheckText("list as list of object", "yy", (string)ItemOf(list, 1));
        Check("array as enumerable", (1 * 3 + 2) * 3 + 3, SumOf(new[] { 1, 2, 3 }));
        Check("list as enumerable", (4 * 3 + 5) * 3 + 6, SumOf(new List<int> { 4, 5, 6 }));
        Check("array as list", 2, IndexOf(new[] { 7, 8, 9 }, 9));
        Check("list as list", 0, IndexOf(new List<int> { 9, 8 }, 9));
        Check("comparable int", 1, Compare<int>(5, 3) > 0 ? 1 : 0);
        Check("comparable string", 1, Compare<string>("a", "b") < 0 ? 1 : 0);
        Check("comparable long", 0, Compare<long>(long.MinValue, long.MinValue));
    }

    // ---- default interface methods ----------------------------------------------------------
    [MethodImpl(NoInl)] static int DefWith(IDefault d) => d.WithDefault();
    [MethodImpl(NoInl)] static int DefOther(IDefault d) => d.Other();
    [MethodImpl(NoInl)] static int DefPlain(IDefault d) => d.Plain();

    static void CaseDefaultMethods()
    {
        IDefault[] ds = { new Def1(), new Def2(), new Def3(), new Def4(), new Def5(), new Def6(), new DefStruct { P = 8 }, new Def7<long>(), new Def7<byte>() };
        int[] with = { 1001, 20, 2003, 2004, 1001, 60, 1008, 1008, 1001 };
        int[] other = { 5, 5, 5, 6, 5, 5, 5, 5, 5 };
        int[] plain = { 1, 2, 3, 4, 1, 3, 8, 8, 1 };
        for (int round = 0; round < 3; round++)
            for (int i = 0; i < ds.Length; i++)
            {
                Check("default method " + i, with[i], DefWith(ds[i]));
                Check("default method other " + i, other[i], DefOther(ds[i]));
                Check("default method plain " + i, plain[i], DefPlain(ds[i]));
            }
    }

    // ---- generic virtual methods ----------------------------------------------------------------
    [MethodImpl(NoInl)] static T Echo<T>(IGvm g, T x) => g.Echo(x);
    [MethodImpl(NoInl)] static int WidthInt(IGvm g) => g.Width<int>();
    [MethodImpl(NoInl)] static int WidthGuid(IGvm g) => g.Width<Guid>();
    [MethodImpl(NoInl)] static int WidthString(IGvm g) => g.Width<string>();
    [MethodImpl(NoInl)] static string NameOf<T>(IGvm g, T x) => g.Name(x);
    [MethodImpl(NoInl)] static int Twice<T>(GvmBase g, T x) where T : struct => g.Twice(x);

    static void CaseGenericVirtual()
    {
        IGvm[] gs = { new Gvm1(), new Gvm2(), new Gvm3<long>(), new Gvm3<byte>(), new Gvm3<string>() };
        int[] wi = { 4, -4, 408, 401, 400 + IntPtr.Size };
        int[] wg = { 16, -16, 1608, 1601, 1600 + IntPtr.Size };
        int[] ws = { IntPtr.Size, -IntPtr.Size, IntPtr.Size * 100 + 8, IntPtr.Size * 100 + 1, IntPtr.Size * 101 };
        string[] ni = { "1:Int32", "2:Int32", "3:Int64Int32", "3:ByteInt32", "3:StringInt32" };
        string[] ns = { "1:String", "2:String", "3:Int64String", "3:ByteString", "3:StringString" };
        for (int round = 0; round < 3; round++)
            for (int i = 0; i < gs.Length; i++)
            {
                Check("generic virtual echo int " + i, i == 1 ? 0 : 12345, Echo(gs[i], 12345));
                Check("generic virtual echo long " + i, i == 1 ? 0 : long.MinValue + 9, Echo(gs[i], long.MinValue + 9));
                CheckText("generic virtual echo string " + i, i == 1 ? null : "s", Echo(gs[i], "s"));
                Check("generic virtual echo guid " + i, i == 1 ? 0 : 1, Echo(gs[i], new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11)) == new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11) ? 1 : 0);
                Check("generic virtual width int " + i, wi[i], WidthInt(gs[i]));
                Check("generic virtual width guid " + i, wg[i], WidthGuid(gs[i]));
                Check("generic virtual width string " + i, ws[i], WidthString(gs[i]));
                CheckText("generic virtual name int " + i, ni[i], NameOf(gs[i], 1));
                CheckText("generic virtual name string " + i, ns[i], NameOf(gs[i], "x"));
            }
        GvmBase a = new GvmA(), b = new GvmB();
        Check("generic virtual class", 8 * 1000 + 24 * 10 + 48, Twice(a, 1) * 1000 + Twice(b, 1L) * 10 + Twice(b, Guid.Empty));
    }

    // ---- boxed structs --------------------------------------------------------------------------------
    [MethodImpl(NoInl)] static int Inc(ICount c) => c.Inc();
    [MethodImpl(NoInl)] static int Read(ICount c) => c.Read();
    [MethodImpl(NoInl)] static int IncTwice<T>(ref T c) where T : ICount { c.Inc(); return c.Inc(); }

    static void CaseStructs()
    {
        ICount[] cs = { new Counter(), new WideCounter(), new ClassCounter(), new GenCounter<long>(), new GenCounter<Guid>(), new GenCounter<string>(), new Counter { N = 5 } };
        int[] step = { 1, 10, 100, 1000, 1000, 1000, 1 };
        int[] start = { 0, 0, 0, 0, 0, 0, 5 };
        for (int round = 1; round <= 4; round++)
            for (int i = 0; i < cs.Length; i++)
            {
                Check("boxed struct inc " + i, start[i] + step[i] * round, Inc(cs[i]));
                Check("boxed struct read " + i, start[i] + step[i] * round, Read(cs[i]));
            }
        var c = new Counter { N = 40 };
        Check("struct by constraint", 42, IncTwice(ref c));
        Check("struct by constraint kept", 42, c.N);
        var w = new WideCounter { Pad0 = -1, Pad1 = -1 };
        Check("wide struct by constraint", 20, IncTwice(ref w) + (w.Pad0 == -1 && w.Pad1 == -1 ? 0 : 1));
        ICount boxed = c;
        Check("box is a copy", 43 * 100 + 42, Inc(boxed) * 100 + c.N);
        Check("static abstract", 21 * 1000 + 42, StaticId<StaticA>() * 1000 + StaticTwice<StaticA>());
        Check("static virtual replaced", 34 * 1000 - 1, StaticId<StaticB>() * 1000 + StaticTwice<StaticB>());
    }

    [MethodImpl(NoInl)] static int StaticId<T>() where T : IStatic<T> => T.Id();
    [MethodImpl(NoInl)] static int StaticTwice<T>() where T : IStatic<T> => T.Twice();

    // ---- arguments and results across the helper ---------------------------------------------------------
    [MethodImpl(NoInl)] static long CallSum12(IBig b, long x) => b.Sum12(x, x + 1, x + 2, x + 3, x + 4, x + 5, x + 6, x + 7, x + 8, x + 9, x + 10, x + 11);
    [MethodImpl(NoInl)] static Big CallBuild(IBig b, long seed, in Big other, ref long acc) => b.Build(seed, in other, ref acc);
    [MethodImpl(NoInl)] static Guid CallGuid(IBig b, int a, short s) => b.MakeGuid(a, s);
    [MethodImpl(NoInl)] static long CallMixed(IBig b, int x) => b.Mixed((byte)x, (short)-x, x * 3, (long)x << 33, (x & 1) != 0, (char)(x + 60000), (sbyte)-x, (ushort)(x + 65000), 0xFFFFFFF0u + (uint)x, 0x8000000000000000UL + (ulong)x);

    static void CaseArguments()
    {
        IBig[] bs = { new Big<byte> { K = 1 }, new Big<short> { K = 2 }, new Big<int> { K = 3 }, new Big<long> { K = 4 }, new Big<Guid> { K = 5 }, new Big<decimal> { K = 6 } };
        // The first call of every site with every type goes through the resolver;
        // the checks below see those first calls.
        for (int round = 0; round < 3; round++)
            for (int i = 0; i < bs.Length; i++)
            {
                long k = i + 1, x = 1000 + i * 17 + round;
                long e = k;
                for (int j = 0; j < 12; j++) e += (j + 1) * (x + j);
                Check("twelve arguments " + i, e, CallSum12(bs[i], x));

                var other = new Big { A = 10 + i, B = 20, C = 30, D = 40, E = 50 };
                long acc = 7;
                Big r = CallBuild(bs[i], x, in other, ref acc);
                Check("struct result " + i, 1, r.A == x + k && r.B == 40 && r.C == 40 + i && r.D == ~x && r.E == 50 - k ? 1 : 0);
                Check("byref argument " + i, 7 + k + x, acc);
                Check("struct argument kept " + i, 10 + i + 20 + 30 + 40 + 50, other.A + other.B + other.C + other.D + other.E);
                Check("guid result " + i, 1, CallGuid(bs[i], 0x12345678 + round, -2) == new Guid(0x12345678 + round, -2, (short)k, 1, 2, 3, 4, 5, 6, 7, 8) ? 1 : 0);

                int m = 100 + i + round * 50;
                long em = k + (byte)m + (short)-m * 3L + m * 3 * 5L + ((long)m << 33) * 7 + ((m & 1) != 0 ? 11 : 13) + (char)(m + 60000) * 17L +
                          (sbyte)-m * 19L + (ushort)(m + 65000) * 23L + (0xFFFFFFF0u + (uint)m) * 29L + (long)(0x8000000000000000UL + (ulong)m) * 31;
                Check("mixed arguments " + i, em, CallMixed(bs[i], m));
            }
    }

    // ---- exceptions, recursion, delegates --------------------------------------------------------------------
    [MethodImpl(NoInl)] static int CallThrow(IThrow t, int x) => t.MayThrow(x);
    [MethodImpl(NoInl)] static long CallVisit(IVisit a, IVisit b, int depth) => a.Visit(b, depth);
    [MethodImpl(NoInl)] static Func<long> Bind(IVal v) => v.Val;
    [MethodImpl(NoInl)] static Func<long, long> Bind2(IVal v) => v.Val2;

    static void CaseOther()
    {
        IThrow[] ts = { new Throw<byte>(), new Throw<long>(), new Throw<Guid>(), new Throw<string>(), new Throw<int>(), new Throw<short>() };
        int[] sz = { 1, 8, 16, IntPtr.Size, 4, 2 };
        for (int round = 0; round < 2; round++)
            for (int i = 0; i < ts.Length; i++)
            {
                string got = "nothing";
                try { CallThrow(ts[i], -1); } catch (InvalidOperationException e) { got = e.Message; }
                CheckText("exception through a call " + i, "t" + sz[i], got);
                Check("call after an exception " + i, 10 + sz[i], CallThrow(ts[i], 10));
            }

        IVisit[] vs = { new Visitor<byte>(), new Visitor<short>(), new Visitor<int>(), new Visitor<long>(), new Visitor<Guid>(), new Visitor<decimal>() };
        for (int i = 0; i < vs.Length; i++)
        {
            int j = (i + 1) % vs.Length;
            long sa = sz2(i), sb = sz2(j), e = 0;
            for (int depth = 0; depth <= 24; depth++)
            {
                // The receiver at the bottom is a when the depth is even.
                long bottom = (depth & 1) == 0 ? sa : sb, cur = bottom;
                for (int up = 1; up <= depth; up++) cur = ((depth - up) % 2 == 0 ? sa : sb) + cur * 3 % 1000003;
                e = cur;
            }
            Check("recursion through a call " + i, e, CallVisit(vs[i], vs[j], 24));
        }

        // Binding a delegate resolves the method without calling it.
        long bad = 0;
        for (int round = 0; round < 3; round++)
            for (int k = 0; k < 256; k += 5)
            {
                if (Bind((IVal)items[k])() != expVal[k]) bad++;
                if (Bind2((IVal)items[k])(0xF0F0) != (0xF0F0 ^ expVal[k])) bad++;
            }
        Check("delegates to interface methods", 0, bad);
    }

    static long sz2(int i) => i == 0 ? 1 : i == 1 ? 2 : i == 2 ? 4 : i == 3 ? 8 : 16;

    static void CaseNull()
    {
#if NULL_RECEIVERS
        string got = "nothing";
        try { SiteMega(Opaque<IVal>(null)); } catch (Exception e) { got = e.GetType().Name; }
        CheckText("null receiver", "NullReferenceException", got);
        got = "nothing";
        try { Echo(Opaque<IGvm>(null), 1); } catch (Exception e) { got = e.GetType().Name; }
        CheckText("null receiver, generic virtual", "NullReferenceException", got);
        got = "nothing";
        try { DefWith(Opaque<IDefault>(null)); } catch (Exception e) { got = e.GetType().Name; }
        CheckText("null receiver, default method", "NullReferenceException", got);
        Check("call after null", expVal[4], SiteMega((IVal)items[4]));
#endif
    }

    static int Main()
    {
        MakeReceivers();
        CaseOneType();
        CaseFewTypes();
        CaseManyTypes();
        CaseManyCells();
        CaseGenericInterfaces();
        CaseDefaultMethods();
        CaseGenericVirtual();
        CaseStructs();
        CaseArguments();
        CaseOther();
        CaseNull();

        Console.WriteLine(fails == 0 ? "ALL PASS" : "FAILED " + fails);
        return fails;
    }
}
