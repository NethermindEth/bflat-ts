// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// float and double through the runtime's own stubs and through library code
// on a target without the F and D extensions.
//
// The runtime's hand-written assembly was written for a machine with
// floating-point registers: the stub that resolves an interface call the
// first time it is made saves and restores the eight floating-point argument
// registers around the lookup, and the interlocked helpers and write
// barriers sit in the same sources. On this target those saves are compiled
// out, and floating-point arguments travel in a0-a7, which the stub saves as
// integers. The library is the same managed code as everywhere, compiled
// with every floating-point operation turned into a call. What can go wrong:
// an argument register or a stack argument lost by the first call through
// an interface or to a generic virtual method, a stub frame whose layout
// changed with the saves, an interlocked exchange that returns or stores the
// wrong value, and library code that depends on what the hardware would
// have done: the software path of the vector types, which pass and return
// structs of two, three and four floats, equality and ordering of NaNs and
// zeros in the comparers behind Equals, CompareTo, Dictionary, Sort and
// IndexOf, and conversions inside Convert and TimeSpan.
//
// Checks: interface calls with seven floating-point arguments in registers
// and three on the stack, at call sites that see one type and at one that
// sees four; generic virtual methods over float, double, a struct of two
// floats and long; Interlocked and Volatile on object references, double
// and float; arrays, spans, two-dimensional arrays and static data of float
// and double; Equals, CompareTo, GetHashCode, Dictionary, Sort,
// BinarySearch and IndexOf with NaNs and both zeros; Vector2, Vector3,
// Vector4 and Vector128 by value and in arithmetic; an iterator; Convert
// and TimeSpan.
//
// The expected values are bit patterns: copies must be exact, and the
// arithmetic uses operands whose results are exact or were computed outside
// the test. Each failed check prints one line; the run ends with ALL PASS
// or a FAIL count, and the guest returns 0 or 1.
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Threading;

struct SFF { public float A, B; }

interface IMix
{
    ulong Mix(double a, float b, double c, float d, double e, float f, double g, float h, double i, float j);
}

abstract class Gen
{
    public abstract ulong Bits<T>(T x, double a, float b) where T : unmanaged;
}

unsafe sealed class GenA : Gen
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override ulong Bits<T>(T x, double a, float b)
    {
        ulong r = 0;
        Buffer.MemoryCopy(&x, &r, 8, sizeof(T));
        return Program.H(Program.H(Program.H(Program.Seed + 1, r), Program.BD(a)), Program.BF(b));
    }
}

unsafe sealed class GenB : Gen
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override ulong Bits<T>(T x, double a, float b)
    {
        ulong r = 0;
        Buffer.MemoryCopy(&x, &r, 8, sizeof(T));
        return Program.H(Program.H(Program.H(Program.Seed + 2, Program.BF(b)), r), Program.BD(a));
    }
}

class MixBase : IMix
{
    public ulong Salt;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public ulong Mix(double a, float b, double c, float d, double e, float f, double g, float h, double i, float j)
    {
        ulong x = Program.H(Program.Seed, Salt);
        x = Program.H(x, Program.BD(a)); x = Program.H(x, Program.BF(b)); x = Program.H(x, Program.BD(c));
        x = Program.H(x, Program.BF(d)); x = Program.H(x, Program.BD(e)); x = Program.H(x, Program.BF(f));
        x = Program.H(x, Program.BD(g)); x = Program.H(x, Program.BF(h)); x = Program.H(x, Program.BD(i));
        x = Program.H(x, Program.BF(j));
        return x;
    }
}

sealed class Mix1 : MixBase { public Mix1() { Salt = 1; } }
sealed class Mix2 : MixBase { public Mix2() { Salt = 2; } }
sealed class Mix3 : MixBase { public Mix3() { Salt = 3; } }

struct Mix4 : IMix
{
    public ulong Salt;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public ulong Mix(double a, float b, double c, float d, double e, float f, double g, float h, double i, float j)
    {
        ulong x = Program.H(Program.Seed, Salt);
        x = Program.H(x, Program.BD(a)); x = Program.H(x, Program.BF(b)); x = Program.H(x, Program.BD(c));
        x = Program.H(x, Program.BF(d)); x = Program.H(x, Program.BD(e)); x = Program.H(x, Program.BF(f));
        x = Program.H(x, Program.BD(g)); x = Program.H(x, Program.BF(h)); x = Program.H(x, Program.BD(i));
        x = Program.H(x, Program.BF(j));
        return x;
    }
}

class Cell
{
    public object Ref;
    public double D;
    public float F;
}

unsafe class Program
{
    const MethodImplOptions NoInl = MethodImplOptions.NoInlining;
    public const ulong Seed = 14695981039346656037UL;

    static int fails;
    static int checks;

    [MethodImpl(NoInl)]
    static void Check(string name, ulong expected, ulong got)
    {
        checks++;
        if (got == expected) return;
        fails++;
        Console.WriteLine("FAIL " + name + " expected 0x" + expected.ToString("x16") + " got 0x" + got.ToString("x16"));
    }

    static void Check(string name, bool expected, bool got) => Check(name, expected ? 1UL : 0, got ? 1UL : 0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong H(ulong h, ulong v) => (h ^ v) * 0x100000001B3UL + (v >> 29);

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static ulong BD(double d) => *(ulong*)&d;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static ulong BF(float f) => *(uint*)&f;
    [MethodImpl(NoInl)] static double D(ulong bits) => *(double*)&bits;
    [MethodImpl(NoInl)] static float F(uint bits) => *(float*)&bits;

    static readonly ulong[] PD =
    {
        0x3FF8000000000000UL, 0xC0091EB851EB851FUL, 0x7FF8DEADBEEF0001UL, 0x8000000000000000UL,
        0x0000000000000001UL, 0xFFEFFFFFFFFFFFFFUL, 0x400921FB54442D18UL, 0x123456789ABCDEF0UL,
    };

    static readonly uint[] PF =
    {
        0x3FC00000U, 0xC0490FDBU, 0x7FC0BEEFU, 0x80000000U, 0x00000001U, 0xFF7FFFFFU, 0x12345678U, 0xFEDCBA98U,
    };

    // ---- interface calls and generic virtual methods ----

    [MethodImpl(NoInl)]
    static ulong Want(ulong salt, int r)
    {
        ulong x = H(Seed, salt);
        for (int n = 0; n < 5; n++)
        {
            x = H(x, PD[(n + r) % 8]);
            x = H(x, PF[(n * 3 + r) % 8]);
        }
        return x;
    }

    // One call site that sees every type.
    [MethodImpl(NoInl)]
    static ulong CallAny(IMix m, int r)
        => m.Mix(D(PD[r % 8]), F(PF[r % 8]), D(PD[(1 + r) % 8]), F(PF[(3 + r) % 8]), D(PD[(2 + r) % 8]),
                 F(PF[(6 + r) % 8]), D(PD[(3 + r) % 8]), F(PF[(9 + r) % 8]), D(PD[(4 + r) % 8]), F(PF[(12 + r) % 8]));

    // Call sites that each see one type; their first call resolves the method.
    [MethodImpl(NoInl)]
    static ulong CallFirst1(IMix m)
        => m.Mix(D(PD[0]), F(PF[0]), D(PD[1]), F(PF[3]), D(PD[2]), F(PF[6]), D(PD[3]), F(PF[1]), D(PD[4]), F(PF[4]));

    [MethodImpl(NoInl)]
    static ulong CallFirst2(IMix m)
        => m.Mix(D(PD[0]), F(PF[0]), D(PD[1]), F(PF[3]), D(PD[2]), F(PF[6]), D(PD[3]), F(PF[1]), D(PD[4]), F(PF[4]));

    [MethodImpl(NoInl)]
    static ulong CallFirst3(IMix m)
        => m.Mix(D(PD[0]), F(PF[0]), D(PD[1]), F(PF[3]), D(PD[2]), F(PF[6]), D(PD[3]), F(PF[1]), D(PD[4]), F(PF[4]));

    [MethodImpl(NoInl)]
    static ulong CallFirst4(IMix m)
        => m.Mix(D(PD[0]), F(PF[0]), D(PD[1]), F(PF[3]), D(PD[2]), F(PF[6]), D(PD[3]), F(PF[1]), D(PD[4]), F(PF[4]));

    [MethodImpl(NoInl)]
    static ulong CallGen<T>(Gen g, T x, double a, float b) where T : unmanaged => g.Bits(x, a, b);

    static void RunDispatch()
    {
        Mix4 m4;
        m4.Salt = 4;
        IMix[] all = { new Mix1(), new Mix2(), new Mix3(), m4 };

        Check("first interface call, class 1", Want(1, 0), CallFirst1(all[0]));
        Check("first interface call, class 2", Want(2, 0), CallFirst2(all[1]));
        Check("first interface call, class 3", Want(3, 0), CallFirst3(all[2]));
        Check("first interface call, boxed struct", Want(4, 0), CallFirst4(all[3]));
        Check("second interface call, class 1", Want(1, 0), CallFirst1(all[0]));
        for (int r = 0; r < 24; r++)
            Check("interface call at a shared site, round " + r, Want((ulong)(r % 4) + 1, r), CallAny(all[r % 4], r));

        Gen[] gens = { new GenA(), new GenB() };
        for (int r = 0; r < 8; r++)
        {
            ulong a = PD[r], bits;
            uint b = PF[(r + 3) % 8];
            Gen g = gens[r % 2];
            bool first = r % 2 == 0;

            bits = PF[r];
            Check("generic virtual over float, round " + r,
                  first ? H(H(H(Seed + 1, bits), a), b) : H(H(H(Seed + 2, b), bits), a), CallGen(g, F((uint)bits), D(a), F(b)));
            bits = PD[(r + 1) % 8];
            Check("generic virtual over double, round " + r,
                  first ? H(H(H(Seed + 1, bits), a), b) : H(H(H(Seed + 2, b), bits), a), CallGen(g, D(bits), D(a), F(b)));
            SFF s;
            *(uint*)&s.A = PF[r];
            *(uint*)&s.B = PF[(r + 5) % 8];
            bits = PF[r] | ((ulong)PF[(r + 5) % 8] << 32);
            Check("generic virtual over a struct of two floats, round " + r,
                  first ? H(H(H(Seed + 1, bits), a), b) : H(H(H(Seed + 2, b), bits), a), CallGen(g, s, D(a), F(b)));
            bits = ~PD[r];
            Check("generic virtual over long, round " + r,
                  first ? H(H(H(Seed + 1, bits), a), b) : H(H(H(Seed + 2, b), bits), a), CallGen(g, (long)bits, D(a), F(b)));
        }
    }

    // ---- Interlocked and Volatile ----

    static object sRef;
    static double sD;
    static float sF;

    [MethodImpl(NoInl)] static object Xchg(ref object loc, object v) => Interlocked.Exchange(ref loc, v);
    [MethodImpl(NoInl)] static object CmpXchg(ref object loc, object v, object c) => Interlocked.CompareExchange(ref loc, v, c);
    [MethodImpl(NoInl)] static double XchgD(ref double loc, double v) => Interlocked.Exchange(ref loc, v);
    [MethodImpl(NoInl)] static double CmpXchgD(ref double loc, double v, double c) => Interlocked.CompareExchange(ref loc, v, c);
    [MethodImpl(NoInl)] static float XchgF(ref float loc, float v) => Interlocked.Exchange(ref loc, v);
    [MethodImpl(NoInl)] static float CmpXchgF(ref float loc, float v, float c) => Interlocked.CompareExchange(ref loc, v, c);

    static void RunInterlocked()
    {
        object a = new object(), b = "b", c = new Cell();
        Cell cell = new Cell();

        cell.Ref = a;
        Check("Exchange of a reference in an object returns the old one", true, ReferenceEquals(Xchg(ref cell.Ref, b), a));
        Check("Exchange of a reference in an object stores the new one", true, ReferenceEquals(cell.Ref, b));
        Check("failed CompareExchange returns the current reference", true, ReferenceEquals(CmpXchg(ref cell.Ref, c, a), b));
        Check("failed CompareExchange leaves the reference", true, ReferenceEquals(cell.Ref, b));
        Check("CompareExchange returns the old reference", true, ReferenceEquals(CmpXchg(ref cell.Ref, c, b), b));
        Check("CompareExchange stores the new reference", true, ReferenceEquals(cell.Ref, c));
        Check("CompareExchange with null as the new value", true, ReferenceEquals(CmpXchg(ref cell.Ref, null, c), c));
        Check("null stored", true, cell.Ref == null);
        Check("CompareExchange with null as the comparand", true, CmpXchg(ref cell.Ref, a, null) == null);
        Check("stored over null", true, ReferenceEquals(cell.Ref, a));

        sRef = a;
        Check("Exchange of a static reference", true, ReferenceEquals(Xchg(ref sRef, c), a) && ReferenceEquals(sRef, c));
        object local = b;
        Check("Exchange of a local reference", true, ReferenceEquals(Xchg(ref local, a), b) && ReferenceEquals(local, a));
        Check("CompareExchange of a local reference", true,
              ReferenceEquals(CmpXchg(ref local, c, a), a) && ReferenceEquals(local, c));

        for (int n = 0; n < 8; n++)
        {
            ulong d0 = PD[n], d1 = PD[(n + 1) % 8], d2 = PD[(n + 2) % 8];
            uint f0 = PF[n], f1 = PF[(n + 1) % 8], f2 = PF[(n + 2) % 8];
            string r = "round " + n + ", ";

            cell.D = D(d0);
            Check(r + "Exchange of a double returns the old value", d0, BD(XchgD(ref cell.D, D(d1))));
            Check(r + "Exchange of a double stores the new value", d1, BD(cell.D));
            // The comparison is of the bits: a NaN matches itself, -0 does not match +0.
            Check(r + "failed CompareExchange of a double returns the current value", d1,
                  BD(CmpXchgD(ref cell.D, D(d2), D(d1 ^ 0x8000000000000000UL))));
            Check(r + "failed CompareExchange of a double leaves the value", d1, BD(cell.D));
            Check(r + "CompareExchange of a double returns the old value", d1, BD(CmpXchgD(ref cell.D, D(d2), D(d1))));
            Check(r + "CompareExchange of a double stores the new value", d2, BD(cell.D));

            sF = F(f0);
            Check(r + "Exchange of a float returns the old value", f0, BF(XchgF(ref sF, F(f1))));
            Check(r + "Exchange of a float stores the new value", f1, BF(sF));
            Check(r + "failed CompareExchange of a float returns the current value", f1,
                  BF(CmpXchgF(ref sF, F(f2), F(f1 ^ 0x80000000U))));
            Check(r + "failed CompareExchange of a float leaves the value", f1, BF(sF));
            Check(r + "CompareExchange of a float returns the old value", f1, BF(CmpXchgF(ref sF, F(f2), F(f1))));
            Check(r + "CompareExchange of a float stores the new value", f2, BF(sF));

            Volatile.Write(ref sD, D(d0));
            Check(r + "Volatile double", d0, BD(Volatile.Read(ref sD)));
            Volatile.Write(ref cell.F, F(f0));
            Check(r + "Volatile float", f0, BF(Volatile.Read(ref cell.F)));
        }
    }

    // ---- arrays, spans and static data ----

    static readonly double Two = 2.0;
    static readonly double Tau = Two * Math.PI;
    static readonly float[] StaticF = { 0.1f, -2.5f, 3.0e38f, 1.0e-45f };
    static readonly double[] StaticD = { 0.1, -2.5, 1.7976931348623157e308, 5e-324 };

    static void RunArrays()
    {
        Check("static readonly double computed at start", 0x401921FB54442D18UL, BD(Tau));
        Check("static float data 0", 0x3DCCCCCDUL, BF(StaticF[0]));
        Check("static float data 1", 0xC0200000UL, BF(StaticF[1]));
        Check("static float data 2", 0x7F61B1E6UL, BF(StaticF[2]));
        Check("static float data 3", 0x00000001UL, BF(StaticF[3]));
        Check("static double data 0", 0x3FB999999999999AUL, BD(StaticD[0]));
        Check("static double data 1", 0xC004000000000000UL, BD(StaticD[1]));
        Check("static double data 2", 0x7FEFFFFFFFFFFFFFUL, BD(StaticD[2]));
        Check("static double data 3", 0x0000000000000001UL, BD(StaticD[3]));

        double[] d = new double[8];
        float[] f = new float[8];
        for (int n = 0; n < 8; n++) { d[n] = D(PD[n]); f[n] = F(PF[n]); }

        double[] d2 = new double[8];
        float[] f2 = new float[8];
        Array.Copy(d, d2, 8);
        Array.Copy(f, f2, 8);
        Array.Reverse(d2);
        Array.Reverse(f2);
        ulong x = Seed, y = Seed, wx = Seed, wy = Seed;
        for (int n = 0; n < 8; n++)
        {
            x = H(x, BD(d2[n])); y = H(y, BF(f2[n]));
            wx = H(wx, PD[7 - n]); wy = H(wy, PF[7 - n]);
        }
        Check("double array copied and reversed", wx, x);
        Check("float array copied and reversed", wy, y);

        Span<double> sd = stackalloc double[8];
        Span<float> sf = stackalloc float[8];
        d.AsSpan().CopyTo(sd);
        f.AsSpan().CopyTo(sf);
        sd.Slice(2, 3).Fill(D(PD[5]));
        sf.Slice(1, 2).Fill(F(PF[7]));
        x = y = wx = wy = Seed;
        for (int n = 0; n < 8; n++)
        {
            x = H(x, BD(sd[n])); y = H(y, BF(sf[n]));
            wx = H(wx, n >= 2 && n < 5 ? PD[5] : PD[n]); wy = H(wy, n >= 1 && n < 3 ? PF[7] : PF[n]);
        }
        Check("double span copied and filled", wx, x);
        Check("float span copied and filled", wy, y);

        double[,] dd = new double[3, 4];
        float[,] ff = new float[4, 3];
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 4; j++) { dd[i, j] = D(PD[(i * 4 + j) % 8] ^ (ulong)i); ff[j, i] = F(PF[(i * 4 + j) % 8] ^ (uint)j); }
        x = y = wx = wy = Seed;
        for (int i = 2; i >= 0; i--)
            for (int j = 3; j >= 0; j--)
            {
                x = H(x, BD(dd[i, j])); y = H(y, BF(ff[j, i]));
                wx = H(wx, PD[(i * 4 + j) % 8] ^ (ulong)i); wy = H(wy, PF[(i * 4 + j) % 8] ^ (uint)j);
            }
        Check("two-dimensional double array", wx, x);
        Check("two-dimensional float array", wy, y);

        SFF[] sa = new SFF[4];
        for (int n = 0; n < 4; n++) { sa[n].A = F(PF[n]); sa[n].B = F(PF[7 - n]); }
        SFF[] sb = (SFF[])sa.Clone();
        x = wx = Seed;
        for (int n = 0; n < 4; n++) { x = H(H(x, BF(sb[n].A)), BF(sb[n].B)); wx = H(H(wx, PF[n]), PF[7 - n]); }
        Check("array of structs of two floats", wx, x);
    }

    // ---- comparers and collections ----

    [MethodImpl(NoInl)] static bool Eq<T>(T a, T b) => EqualityComparer<T>.Default.Equals(a, b);
    [MethodImpl(NoInl)] static int Cmp<T>(T a, T b) => Comparer<T>.Default.Compare(a, b);
    [MethodImpl(NoInl)] static int Sign(int v) => v < 0 ? -1 : v > 0 ? 1 : 0;

    static void RunCollections()
    {
        double nan = D(0x7FF8000000000000UL), nan2 = D(0xFFF8000000000123UL), pz = D(0), nz = D(0x8000000000000000UL);
        double one = D(0x3FF0000000000000UL), inf = D(0x7FF0000000000000UL), ninf = D(0xFFF0000000000000UL);
        float fnan = F(0x7FC00000U), fpz = F(0), fnz = F(0x80000000U), fone = F(0x3F800000U);

        Check("NaN Equals NaN", true, nan.Equals(nan2));
        Check("+0 Equals -0", true, pz.Equals(nz));
        Check("1 Equals NaN", false, one.Equals(nan));
        Check("float NaN Equals NaN", true, fnan.Equals(fnan));
        Check("float +0 Equals -0", true, fpz.Equals(fnz));
        Check("comparer: NaN equals NaN", true, Eq(nan, nan2));
        Check("comparer: float 1 equals NaN", false, Eq(fone, fnan));
        Check("boxed double equals boxed double", true, ((object)one).Equals((object)D(0x3FF0000000000000UL)));
        Check("boxed double equals boxed float", false, ((object)one).Equals((object)fone));
        Check("hash codes of +0 and -0", true, pz.GetHashCode() == nz.GetHashCode());
        Check("hash codes of two NaNs", true, nan.GetHashCode() == nan2.GetHashCode());
        Check("float hash codes of +0 and -0", true, fpz.GetHashCode() == fnz.GetHashCode());

        Check("NaN CompareTo 1", unchecked((ulong)-1L), (ulong)(long)Sign(nan.CompareTo(one)));
        Check("1 CompareTo NaN", 1, (ulong)(long)Sign(one.CompareTo(nan)));
        Check("NaN CompareTo NaN", 0, (ulong)(long)Sign(nan.CompareTo(nan2)));
        Check("NaN CompareTo -infinity", unchecked((ulong)-1L), (ulong)(long)Sign(nan.CompareTo(ninf)));
        Check("-0 CompareTo +0", 0, (ulong)(long)Sign(nz.CompareTo(pz)));
        Check("1 CompareTo infinity", unchecked((ulong)-1L), (ulong)(long)Sign(one.CompareTo(inf)));
        Check("comparer: float NaN with 1", unchecked((ulong)-1L), (ulong)(long)Sign(Cmp(fnan, fone)));
        Check("comparer: float 1 with -0", 1, (ulong)(long)Sign(Cmp(fone, fnz)));

        Dictionary<double, int> dict = new Dictionary<double, int>();
        dict[pz] = 1;
        dict[nz] = 2;
        dict[nan] = 3;
        dict[nan2] = 4;
        dict[one] = 5;
        for (int n = 2; n < 200; n++) dict[(double)n * 0.5] = n * 10;
        Check("dictionary: +0 and -0 are one key", 2, (ulong)dict[pz]);
        Check("dictionary: two NaNs are one key", 4, (ulong)dict[nan]);
        Check("dictionary: count", 200, (ulong)dict.Count);
        long sum = 0;
        for (int n = 2; n < 200; n++) sum += dict[(double)n / 2.0];
        Check("dictionary: sum of the looked-up values", 198990, (ulong)sum);
        Check("dictionary: 1 was overwritten by 2 * 0.5", 20, (ulong)dict[one]);

        double[] d = { D(0x4008000000000000UL), nan, D(0xBFF0000000000000UL), inf, D(0x3FE0000000000000UL), ninf,
                       D(0x0000000000000001UL), nan2, D(0x7FEFFFFFFFFFFFFFUL), D(0xC008000000000000UL) };
        Array.Sort(d);
        // NaNs first, then ascending.
        Check("sorted double 0 is a NaN", true, d[0] != d[0]);
        Check("sorted double 1 is a NaN", true, d[1] != d[1]);
        Check("sorted double 2", 0xFFF0000000000000UL, BD(d[2]));
        Check("sorted double 3", 0xC008000000000000UL, BD(d[3]));
        Check("sorted double 4", 0xBFF0000000000000UL, BD(d[4]));
        Check("sorted double 5", 0x0000000000000001UL, BD(d[5]));
        Check("sorted double 6", 0x3FE0000000000000UL, BD(d[6]));
        Check("sorted double 7", 0x4008000000000000UL, BD(d[7]));
        Check("sorted double 8", 0x7FEFFFFFFFFFFFFFUL, BD(d[8]));
        Check("sorted double 9", 0x7FF0000000000000UL, BD(d[9]));
        Check("binary search", 6, (ulong)Array.BinarySearch(d, D(0x3FE0000000000000UL)));
        Check("binary search of a missing value", unchecked((ulong)~7L), (ulong)(long)Array.BinarySearch(d, one));
        Check("IndexOf a NaN", 0, (ulong)Array.IndexOf(d, nan));
        Check("IndexOf infinity", 9, (ulong)Array.IndexOf(d, inf));

        float[] f = new float[40];
        for (int n = 0; n < 40; n++) f[n] = (float)((n * 17) % 40) * 0.25f - 3.0f;
        Array.Sort(f);
        bool ordered = true;
        for (int n = 0; n < 40; n++) ordered &= BF(f[n]) == BF((float)n * 0.25f - 3.0f);
        Check("forty floats sorted", true, ordered);
        Check("first sorted float", 0xC0400000UL, BF(f[0]));
        Check("last sorted float", 0x40D80000UL, BF(f[39]));

        List<float> list = new List<float>();
        for (int n = 0; n < 10; n++) list.Add((float)n * 0.5f);
        list.Add(fnan);
        Check("list contains a NaN", true, list.Contains(fnan));
        Check("list index of 2.5", 5, (ulong)list.IndexOf(F(0x40200000U)));
        Check("list index of -0", 0, (ulong)list.IndexOf(fnz));
        Check("spans of +0, NaN and -0, NaN are equal", true,
              new double[] { pz, nan }.AsSpan().SequenceEqual(new double[] { nz, nan2 }));
    }

    // ---- the vector types ----

    [MethodImpl(NoInl)] static Vector2 Swap2(Vector2 v) => new Vector2(v.Y, v.X);
    [MethodImpl(NoInl)] static Vector3 Rot3(Vector3 v) => new Vector3(v.Y, v.Z, v.X);
    [MethodImpl(NoInl)] static Vector4 Rev4(Vector4 v) => new Vector4(v.W, v.Z, v.Y, v.X);
    [MethodImpl(NoInl)] static Vector2 Add2(Vector2 a, Vector2 b) => a + b;
    [MethodImpl(NoInl)] static Vector2 Mul2(Vector2 a, Vector2 b) => a * b;
    [MethodImpl(NoInl)] static Vector2 Div2(Vector2 a, Vector2 b) => a / b;
    [MethodImpl(NoInl)] static float Dot2(Vector2 a, Vector2 b) => Vector2.Dot(a, b);
    [MethodImpl(NoInl)] static float Len2(Vector2 a) => a.Length();
    [MethodImpl(NoInl)] static Vector2 Norm2(Vector2 a) => Vector2.Normalize(a);
    [MethodImpl(NoInl)] static Vector3 Cross3(Vector3 a, Vector3 b) => Vector3.Cross(a, b);
    [MethodImpl(NoInl)] static float Dot3(Vector3 a, Vector3 b) => Vector3.Dot(a, b);
    [MethodImpl(NoInl)] static Vector4 Scale4(Vector4 a, float k) => a * k;
    [MethodImpl(NoInl)] static float Dot4(Vector4 a, Vector4 b) => Vector4.Dot(a, b);
    [MethodImpl(NoInl)] static Vector128<float> Add128(Vector128<float> a, Vector128<float> b) => a + b;
    [MethodImpl(NoInl)] static Vector128<double> Mul128(Vector128<double> a, Vector128<double> b) => a * b;
    [MethodImpl(NoInl)] static Vector128<float> Sqrt128(Vector128<float> a) => Vector128.Sqrt(a);
    [MethodImpl(NoInl)] static Vector128<int> ToInt128(Vector128<float> a) => Vector128.ConvertToInt32(a);
    [MethodImpl(NoInl)] static float Sum128(Vector128<float> a) => Vector128.Sum(a);

    static void RunVectors()
    {
        Vector2 v2 = Swap2(new Vector2(F(PF[1]), F(PF[2])));
        Check("two floats by value, first", PF[2], BF(v2.X));
        Check("two floats by value, second", PF[1], BF(v2.Y));
        Vector3 v3 = Rot3(new Vector3(F(PF[1]), F(PF[2]), F(PF[5])));
        Check("three floats by value", H(H(H(Seed, PF[2]), PF[5]), PF[1]), H(H(H(Seed, BF(v3.X)), BF(v3.Y)), BF(v3.Z)));
        Vector4 v4 = Rev4(new Vector4(F(PF[1]), F(PF[2]), F(PF[5]), F(PF[6])));
        Check("four floats by value", H(H(H(H(Seed, PF[6]), PF[5]), PF[2]), PF[1]),
              H(H(H(H(Seed, BF(v4.X)), BF(v4.Y)), BF(v4.Z)), BF(v4.W)));

        Vector2 a = new Vector2(F(0x3FC00000U), F(0xC0000000U));    // 1.5, -2
        Vector2 b = new Vector2(F(0x40800000U), F(0x3F000000U));    // 4, 0.5
        v2 = Add2(a, b);
        Check("Vector2 sum, X", 0x40B00000UL, BF(v2.X));           // 5.5
        Check("Vector2 sum, Y", 0xBFC00000UL, BF(v2.Y));           // -1.5
        v2 = Mul2(a, b);
        Check("Vector2 product, X", 0x40C00000UL, BF(v2.X));       // 6
        Check("Vector2 product, Y", 0xBF800000UL, BF(v2.Y));       // -1
        v2 = Div2(a, b);
        Check("Vector2 quotient, X", 0x3EC00000UL, BF(v2.X));      // 0.375
        Check("Vector2 quotient, Y", 0xC0800000UL, BF(v2.Y));      // -4
        Check("Vector2 dot product", 0x40A00000UL, BF(Dot2(a, b))); // 5
        Vector2 c = new Vector2(F(0x40400000U), F(0x40800000U));    // 3, 4
        Check("Vector2 length", 0x40A00000UL, BF(Len2(c)));         // 5
        v2 = Norm2(c);
        Check("Vector2 normalized, X", 0x3F19999AUL, BF(v2.X));    // 0.6
        Check("Vector2 normalized, Y", 0x3F4CCCCDUL, BF(v2.Y));    // 0.8

        Vector3 p = new Vector3(F(0x3F800000U), F(0x40000000U), F(0x40400000U));    // 1, 2, 3
        Vector3 q = new Vector3(F(0x40800000U), F(0x40A00000U), F(0x40C00000U));    // 4, 5, 6
        v3 = Cross3(p, q);
        Check("Vector3 cross product, X", 0xC0400000UL, BF(v3.X)); // -3
        Check("Vector3 cross product, Y", 0x40C00000UL, BF(v3.Y)); // 6
        Check("Vector3 cross product, Z", 0xC0400000UL, BF(v3.Z)); // -3
        Check("Vector3 dot product", 0x42000000UL, BF(Dot3(p, q))); // 32

        Vector4 w = new Vector4(F(0x3F800000U), F(0x40000000U), F(0x40400000U), F(0x40800000U));   // 1, 2, 3, 4
        v4 = Scale4(w, F(0x40000000U));
        Check("Vector4 scaled", H(H(H(H(Seed, 0x40000000UL), 0x40800000UL), 0x40C00000UL), 0x41000000UL),
              H(H(H(H(Seed, BF(v4.X)), BF(v4.Y)), BF(v4.Z)), BF(v4.W)));
        Check("Vector4 dot product", 0x41200000UL, BF(Dot4(w, new Vector4(F(0x3F800000U)))));        // 10

        Vector128<float> s = Add128(Vector128.Create(F(0x3F800000U), F(0x40000000U), F(0x40400000U), F(0x40800000U)),
                                    Vector128.Create(F(0x3F000000U)));
        Check("Vector128 of float sum", H(H(H(H(Seed, 0x3FC00000UL), 0x40200000UL), 0x40600000UL), 0x40900000UL),
              H(H(H(H(Seed, BF(s.GetElement(0))), BF(s.GetElement(1))), BF(s.GetElement(2))), BF(s.GetElement(3))));
        Check("Vector128 of float total", 0x41400000UL, BF(Sum128(s)));                              // 12
        Vector128<double> m = Mul128(Vector128.Create(D(0x3FF8000000000000UL), D(0xC000000000000000UL)),
                                     Vector128.Create(D(0x4000000000000000UL), D(0x3FD0000000000000UL)));
        Check("Vector128 of double product, 0", 0x4008000000000000UL, BD(m.GetElement(0)));          // 3
        Check("Vector128 of double product, 1", 0xBFE0000000000000UL, BD(m.GetElement(1)));          // -0.5
        s = Sqrt128(Vector128.Create(F(0x40800000U), F(0x41100000U), F(0x41800000U), F(0x41C80000U)));  // 4, 9, 16, 25
        Check("Vector128 square roots", H(H(H(H(Seed, 0x40000000UL), 0x40400000UL), 0x40800000UL), 0x40A00000UL),
              H(H(H(H(Seed, BF(s.GetElement(0))), BF(s.GetElement(1))), BF(s.GetElement(2))), BF(s.GetElement(3))));
        Vector128<int> t = ToInt128(Vector128.Create(F(0x3FC00000U), F(0xC0200000U), F(0x4F32D05EU), F(0x7FC00000U)));
        Check("Vector128 to int, 1.5", 1, (ulong)(long)t.GetElement(0));
        Check("Vector128 to int, -2.5", unchecked((ulong)-2L), (ulong)(long)t.GetElement(1));
        Check("Vector128 to int, 3e9", 0x7FFFFFFFUL, (ulong)(long)t.GetElement(2));
        Check("Vector128 to int, NaN", 0, (ulong)(long)t.GetElement(3));
    }

    // ---- an iterator, Convert and TimeSpan ----

    static IEnumerable<double> Steps(double start, float step, int n)
    {
        double v = start;
        for (int k = 0; k < n; k++)
        {
            yield return v;
            v += step;
        }
    }

    [MethodImpl(NoInl)]
    static int ConvertOrThrow(double v)
    {
        try { return Convert.ToInt32(v); }
        catch (OverflowException) { return -99; }
    }

    static void RunOther()
    {
        double sum = 0.0;
        foreach (double v in Steps(D(0x3FE0000000000000UL), F(0x3F000000U), 8)) sum += v;   // 0.5, 1, ... 4
        Check("iterator with a double and a float in its state", 0x4032000000000000UL, BD(sum));  // 18

        Check("Convert.ToInt32 of 2.5", 2, (ulong)(long)ConvertOrThrow(D(0x4004000000000000UL)));
        Check("Convert.ToInt32 of 3.5", 4, (ulong)(long)ConvertOrThrow(D(0x400C000000000000UL)));
        Check("Convert.ToInt32 of -2.5", unchecked((ulong)-2L), (ulong)(long)ConvertOrThrow(D(0xC004000000000000UL)));
        Check("Convert.ToInt32 of 2147483647.4", 0x7FFFFFFFUL, (ulong)(long)ConvertOrThrow(D(0x41DFFFFFFFD9999AUL)));
        Check("Convert.ToInt32 of 2147483647.5 throws", unchecked((ulong)-99L), (ulong)(long)ConvertOrThrow(D(0x41DFFFFFFFE00000UL)));
        Check("Convert.ToInt32 of a NaN throws", unchecked((ulong)-99L), (ulong)(long)ConvertOrThrow(D(0x7FF8000000000000UL)));
        Check("Convert.ToSingle of a double", 0x3DCCCCCDUL, BF(Convert.ToSingle(D(0x3FB999999999999AUL))));
        Check("Convert.ToDouble of a long", 0x43E0000000000000UL, BD(Convert.ToDouble(long.MaxValue)));

        Check("TimeSpan.FromSeconds", 15000000, (ulong)TimeSpan.FromSeconds(D(0x3FF8000000000000UL)).Ticks);
        Check("TimeSpan.TotalMilliseconds", 0x4097700000000000UL, BD(new TimeSpan(15000000).TotalMilliseconds));  // 1500
        Check("TimeSpan.TotalDays", 0x3FE0000000000000UL, BD(new TimeSpan(12, 0, 0).TotalDays));                  // 0.5
    }

    static int Main()
    {
        RunDispatch();
        RunInterlocked();
        RunArrays();
        RunCollections();
        RunVectors();
        RunOther();

        Console.WriteLine("checks " + checks);
        Console.WriteLine(fails == 0 ? "ALL PASS" : "FAILED " + fails);
        return fails == 0 ? 0 : 1;
    }
}
