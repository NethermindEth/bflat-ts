// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// float and double in calls on a target without the F and D extensions: the
// lp64 soft-float calling convention.
//
// There are no floating-point registers. A float or a double is passed and
// returned as an integer of the same size: in a0-a7, then on the stack, and
// a float leaves the upper half of its register undefined. A struct is
// passed by its size alone, whatever its fields are: up to 8 bytes in one
// register, up to 16 in two (or one register and one stack slot when only
// a7 is left), larger by reference. The rules of the hardware convention,
// which send a struct of one or two floating-point fields to the fa
// registers field by field, must not apply. What can go wrong: a parameter
// or a field looked for in the wrong register or stack slot, a struct with
// float fields split by the hardware rule, a float taken together with the
// upper half of its register (which shows when it is widened, compared as
// bits, or packed into a struct with a second field), the two halves of a
// struct swapped, and a return value built in the wrong register.
//
// Three kinds of evidence, since a caller and a callee that are wrong in
// the same way agree with each other:
//
// Raw calls. A method with float, double and struct parameters is called
// through a function pointer whose signature has a long in place of every
// register the convention assigns, with the bit patterns as arguments and
// 0xDEADBEEF or all ones above every float. The method must see exactly the
// values. The same the other way round: a method that returns a float, a
// double or a struct is called as if it returned a long or a pair of longs.
//
// The C library. fmod, frexp, modf, ldexp, copysign, nextafter, scalbn,
// sqrt, hypot, fma, lrint and lround and their float forms take and return
// floating point; cabs, conj, creal and cimag and their float forms take
// and return a complex number, which C passes as a struct of two doubles or
// two floats; snprintf takes doubles as variable arguments in registers and
// on the stack. These were compiled by another compiler for the same
// convention.
//
// Typed calls of every kind with the values checked bit by bit in the
// callee: more than eight mixed parameters, structs by value and returned,
// ref and out, instance, virtual, interface and generic methods, a boxed
// struct called through an interface, delegates, lambdas that capture
// floats, function pointers, an UnmanagedCallersOnly method, parameter
// arrays, boxing, and tail calls.
//
// The raw calls and the snprintf check describe 64-bit registers. On a
// 32-bit target (ilp32: a float fills its register, a double takes two,
// variable arguments are aligned) they are skipped and one line says so;
// the C library and the typed calls run on both.
//
// No value here is computed with floating-point arithmetic except inside
// the C library; the checks compare bit patterns. Each failed check prints
// one line; the run ends with ALL PASS or a FAIL count, and the guest
// returns 0 or 1.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: DisableRuntimeMarshalling]

struct SF { public float A; }
struct SD { public double A; }
struct SFF { public float A, B; }
struct SDD { public double A, B; }
struct SFD { public float A; public double B; }
struct SDF { public double A; public float B; }
struct SFI { public float A; public int B; }
struct SIF { public int A; public float B; }
struct SFFF { public float A, B, C; }
struct SFFFF { public float A, B, C, D; }
struct SDDD { public double A, B, C; }
struct SNest { public SFF P; public SF Q; public int R; }
struct SLL { public long X, Y; }

unsafe interface IHash
{
    ulong Hash(float a, double b, SFF c, long d);
}

unsafe struct HashStruct : IHash
{
    public float F;
    public double D;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public ulong Hash(float a, double b, SFF c, long d)
        => Program.H(Program.H(Program.H(Program.H(Program.H(Program.H(Program.H(Program.Seed, Program.BF(F)),
           Program.BD(D)), Program.BF(a)), Program.BD(b)), Program.BF(c.A)), Program.BF(c.B)), (ulong)d);
}

unsafe abstract class Shape
{
    public float F;
    public double D;
    public abstract ulong Hash(float a, double b, SFF c, long d);
    public abstract SDD Pair(double a, float b);
}

unsafe sealed class ShapeA : Shape, IHash
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override ulong Hash(float a, double b, SFF c, long d)
        => Program.H(Program.H(Program.H(Program.H(Program.H(Program.H(Program.H(Program.Seed, Program.BF(F)),
           Program.BD(D)), Program.BF(a)), Program.BD(b)), Program.BF(c.A)), Program.BF(c.B)), (ulong)d);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override SDD Pair(double a, float b)
    {
        SDD r;
        r.A = a;
        *(ulong*)&r.B = Program.BF(b) | 0x7700000000000000UL;
        return r;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public ulong Instance(double a, float b, double c, float d, double e, float f, double g, float h, double i)
    {
        ulong x = Program.H(Program.H(Program.Seed, Program.BF(F)), Program.BD(D));
        x = Program.H(x, Program.BD(a)); x = Program.H(x, Program.BF(b)); x = Program.H(x, Program.BD(c));
        x = Program.H(x, Program.BF(d)); x = Program.H(x, Program.BD(e)); x = Program.H(x, Program.BF(f));
        x = Program.H(x, Program.BD(g)); x = Program.H(x, Program.BF(h)); x = Program.H(x, Program.BD(i));
        return x;
    }
}

// A second implementation, so that the calls above stay virtual.
unsafe sealed class ShapeB : Shape, IHash
{
    public override ulong Hash(float a, double b, SFF c, long d) => 1;
    public override SDD Pair(double a, float b) => default;
}

class Box<T> where T : unmanaged
{
    public T V;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public T Swap(T n) { T o = V; V = n; return o; }
}

unsafe delegate ulong DMix(float a, double b, SFF c, long d, double e, float f, SDD g, float h, double i);

unsafe class Program
{
    const MethodImplOptions NoInl = MethodImplOptions.NoInlining;
    const MethodImplOptions NoOpt = MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization;

    public const ulong Seed = 14695981039346656037UL;
    const ulong Junk = 0xDEADBEEF00000000UL;
    const ulong Ones = 0xFFFFFFFF00000000UL;

    // Registers are 64 bits wide: a long stands for a double or for a float with a dirty upper half.
    static readonly bool Wide = sizeof(nint) == 8;

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong H(ulong h, ulong v) => (h ^ v) * 0x100000001B3UL + (v >> 29);

    // The bits of a value, read through memory.
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static ulong BD(double d) => *(ulong*)&d;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static ulong BF(float f) => *(uint*)&f;

    // A value from its bits, written through memory, behind a call.
    [MethodImpl(NoInl)] static double D(ulong bits) => *(double*)&bits;
    [MethodImpl(NoInl)] static float F(uint bits) => *(float*)&bits;

    // Bit patterns: none is a round number, some are NaNs with a payload, -0, a subnormal.
    static readonly ulong[] PD =
    {
        0x3FF8000000000000UL, 0xC0091EB851EB851FUL, 0x7FF8DEADBEEF0001UL, 0x8000000000000000UL,
        0x0000000000000001UL, 0xFFEFFFFFFFFFFFFFUL, 0x400921FB54442D18UL, 0x123456789ABCDEF0UL,
        0xFEDCBA9876543210UL, 0x7FF0000000000000UL, 0x3FB999999999999AUL, 0xBFE0000000000001UL,
    };

    static readonly uint[] PF =
    {
        0x3FC00000U, 0xC0490FDBU, 0x7FC0BEEFU, 0x80000000U, 0x00000001U, 0xFF7FFFFFU,
        0x12345678U, 0xFEDCBA98U, 0x7F800000U, 0x3DCCCCCDU, 0xBF000001U, 0x80000001U,
    };

    // ================= raw calls: scalars =================

    [MethodImpl(NoInl)]
    static ulong Mix12(int a, double b, float c, long d, float e, double f, int g, float h, double i, float j,
                       long k, double l)
    {
        ulong x = H(Seed, (ulong)(long)a);
        x = H(x, BD(b)); x = H(x, BF(c)); x = H(x, (ulong)d); x = H(x, BF(e)); x = H(x, BD(f));
        x = H(x, (ulong)(long)g); x = H(x, BF(h)); x = H(x, BD(i)); x = H(x, BF(j)); x = H(x, (ulong)k);
        x = H(x, BD(l));
        return x;
    }

    [MethodImpl(NoOpt)]
    static ulong Mix12Ref(long a, ulong b, ulong c, long d, ulong e, ulong f, long g, ulong h, ulong i, ulong j,
                          long k, ulong l)
    {
        ulong x = H(Seed, (ulong)(long)(int)a);
        x = H(x, b); x = H(x, (uint)c); x = H(x, (ulong)d); x = H(x, (uint)e); x = H(x, f);
        x = H(x, (ulong)(long)(int)g); x = H(x, (uint)h); x = H(x, i); x = H(x, (uint)j); x = H(x, (ulong)k);
        x = H(x, l);
        return x;
    }

    // Twelve floats: four of them on the stack.
    [MethodImpl(NoInl)]
    static ulong Floats12(float a, float b, float c, float d, float e, float f, float g, float h, float i, float j,
                          float k, float l)
    {
        ulong x = H(Seed, BF(a));
        x = H(x, BF(b)); x = H(x, BF(c)); x = H(x, BF(d)); x = H(x, BF(e)); x = H(x, BF(f)); x = H(x, BF(g));
        x = H(x, BF(h)); x = H(x, BF(i)); x = H(x, BF(j)); x = H(x, BF(k)); x = H(x, BF(l));
        return x;
    }

    // Twelve doubles.
    [MethodImpl(NoInl)]
    static ulong Doubles12(double a, double b, double c, double d, double e, double f, double g, double h, double i,
                           double j, double k, double l)
    {
        ulong x = H(Seed, BD(a));
        x = H(x, BD(b)); x = H(x, BD(c)); x = H(x, BD(d)); x = H(x, BD(e)); x = H(x, BD(f)); x = H(x, BD(g));
        x = H(x, BD(h)); x = H(x, BD(i)); x = H(x, BD(j)); x = H(x, BD(k)); x = H(x, BD(l));
        return x;
    }

    [MethodImpl(NoOpt)]
    static ulong Hash12(ulong* v, ulong mask)
    {
        ulong x = H(Seed, v[0] & mask);
        for (int n = 1; n < 12; n++) x = H(x, v[n] & mask);
        return x;
    }

    // A float whose register has something above bit 31, in every use that
    // could take the whole register.
    [MethodImpl(NoInl)] static long FBitsWide(float f) => BitConverter.SingleToInt32Bits(f);
    [MethodImpl(NoInl)] static ulong FUBitsWide(float f) => BitConverter.SingleToUInt32Bits(f);
    [MethodImpl(NoInl)] static bool FBitsNeg(float f) => BitConverter.SingleToInt32Bits(f) < 0;
    [MethodImpl(NoInl)] static bool FBitsEq(float f, int k) => BitConverter.SingleToInt32Bits(f) == k;
    [MethodImpl(NoInl)] static bool FBitsEqU(float f, uint k) => BitConverter.SingleToUInt32Bits(f) == k;
    [MethodImpl(NoInl)] static ulong FBitsShift(float f) => (ulong)BitConverter.SingleToUInt32Bits(f) << 4;
    [MethodImpl(NoInl)] static long FBitsIndex(float f, long* tab) => tab[BitConverter.SingleToUInt32Bits(f) >> 24];
    [MethodImpl(NoInl)] static ulong FCast(float f) => Unsafe.BitCast<float, uint>(f);
    [MethodImpl(NoInl)] static long FCastS(float f) => Unsafe.BitCast<float, int>(f);
    [MethodImpl(NoInl)] static bool FEq(float a, float b) => a == b;
    [MethodImpl(NoInl)] static bool FLt(float a, float b) => a < b;
    [MethodImpl(NoInl)] static double FWiden(float f) => f;
    [MethodImpl(NoInl)] static object FBox(float f) => f;
    [MethodImpl(NoInl)] static SFF FPack(float a, float b) { SFF s; s.A = a; s.B = b; return s; }
    [MethodImpl(NoInl)] static SFI FPackI(float a, int b) { SFI s; s.A = a; s.B = b; return s; }
    [MethodImpl(NoInl)] static SIF IPackF(int a, float b) { SIF s; s.A = a; s.B = b; return s; }
    [MethodImpl(NoInl)] static SFD FPackD(float a, double b) { SFD s; s.A = a; s.B = b; return s; }

    [MethodImpl(NoInl)]
    static SFFF FPack3(float a, float b, float c) { SFFF s; s.A = a; s.B = b; s.C = c; return s; }

    static float sField;
    static float[] sArr = new float[4];

    [MethodImpl(NoInl)]
    static ulong FStore(float f)
    {
        sField = f;
        sArr[2] = f;
        float local = f;
        Touch(&local);
        fixed (float* p = &sField, q = sArr)
            return *(uint*)p ^ ((ulong)*(uint*)(q + 2) << 1) ^ ((ulong)*(uint*)&local << 2);
    }

    [MethodImpl(NoInl)] static void Touch(float* p) { }

    // Methods that return floating point, called as if they returned a long.
    [MethodImpl(NoInl)] static float RetF(ulong bits) => *(float*)&bits;
    [MethodImpl(NoInl)] static double RetD(ulong bits) => *(double*)&bits;
    [MethodImpl(NoInl)] static float RetFConst() => -1.5f;
    [MethodImpl(NoInl)] static double RetDConst() => -1.5;
    [MethodImpl(NoInl)] static float RetFFromInt(int bits) => BitConverter.Int32BitsToSingle(bits);
    [MethodImpl(NoInl)] static double RetDFromLong(long bits) => BitConverter.Int64BitsToDouble(bits);

    // Methods with integer parameters, called with floating point in their place.
    [MethodImpl(NoInl)] static ulong TakeLongs(long a, long b, long c) => H(H(H(Seed, (ulong)a), (uint)b), (ulong)c);

    // ================= raw calls: structs =================

    [MethodImpl(NoInl)]
    static ulong TakeF(long pre, SF s, long post) => H(H(H(Seed, (ulong)pre), BF(s.A)), (ulong)post);

    [MethodImpl(NoInl)]
    static ulong TakeD(long pre, SD s, long post) => H(H(H(Seed, (ulong)pre), BD(s.A)), (ulong)post);

    [MethodImpl(NoInl)]
    static ulong TakeFF(long pre, SFF s, long post) => H(H(H(H(Seed, (ulong)pre), BF(s.A)), BF(s.B)), (ulong)post);

    [MethodImpl(NoInl)]
    static ulong TakeDD(long pre, SDD s, long post) => H(H(H(H(Seed, (ulong)pre), BD(s.A)), BD(s.B)), (ulong)post);

    [MethodImpl(NoInl)]
    static ulong TakeFD(long pre, SFD s, long post) => H(H(H(H(Seed, (ulong)pre), BF(s.A)), BD(s.B)), (ulong)post);

    [MethodImpl(NoInl)]
    static ulong TakeDF(long pre, SDF s, long post) => H(H(H(H(Seed, (ulong)pre), BD(s.A)), BF(s.B)), (ulong)post);

    [MethodImpl(NoInl)]
    static ulong TakeFI(long pre, SFI s, long post)
        => H(H(H(H(Seed, (ulong)pre), BF(s.A)), (ulong)(long)s.B), (ulong)post);

    [MethodImpl(NoInl)]
    static ulong TakeFFF(long pre, SFFF s, long post)
        => H(H(H(H(H(Seed, (ulong)pre), BF(s.A)), BF(s.B)), BF(s.C)), (ulong)post);

    [MethodImpl(NoInl)]
    static ulong TakeDDD(long pre, SDDD s, long post)
        => H(H(H(H(H(Seed, (ulong)pre), BD(s.A)), BD(s.B)), BD(s.C)), (ulong)post);

    // The struct starts in the last register and ends on the stack.
    [MethodImpl(NoInl)]
    static ulong TakeSplit(long a, long b, long c, long d, long e, long f, long g, SDD s, long post)
    {
        ulong x = H(Seed, (ulong)a);
        x = H(x, (ulong)b); x = H(x, (ulong)c); x = H(x, (ulong)d); x = H(x, (ulong)e); x = H(x, (ulong)f);
        x = H(x, (ulong)g); x = H(x, BD(s.A)); x = H(x, BD(s.B)); x = H(x, (ulong)post);
        return x;
    }

    // Seven registers of structs, then one that is split, then the stack.
    [MethodImpl(NoInl)]
    static ulong TakeMany(SFF a, SDD b, SFD c, SF d, SD e, SFFF f, double g, float h)
    {
        ulong x = H(Seed, BF(a.A));
        x = H(x, BF(a.B)); x = H(x, BD(b.A)); x = H(x, BD(b.B)); x = H(x, BF(c.A)); x = H(x, BD(c.B));
        x = H(x, BF(d.A)); x = H(x, BD(e.A)); x = H(x, BF(f.A)); x = H(x, BF(f.B)); x = H(x, BF(f.C));
        x = H(x, BD(g)); x = H(x, BF(h));
        return x;
    }

    [MethodImpl(NoOpt)]
    static ulong HashList(ulong* v, int n)
    {
        ulong x = H(Seed, v[0]);
        for (int k = 1; k < n; k++) x = H(x, v[k]);
        return x;
    }

    [MethodImpl(NoInl)] static SF MakeF(ulong a) { SF s; *(uint*)&s.A = (uint)a; return s; }
    [MethodImpl(NoInl)] static SD MakeD(ulong a) { SD s; *(ulong*)&s.A = a; return s; }

    [MethodImpl(NoInl)]
    static SFF MakeFF(ulong a, ulong b) { SFF s; *(uint*)&s.A = (uint)a; *(uint*)&s.B = (uint)b; return s; }

    [MethodImpl(NoInl)]
    static SDD MakeDD(ulong a, ulong b) { SDD s; *(ulong*)&s.A = a; *(ulong*)&s.B = b; return s; }

    [MethodImpl(NoInl)]
    static SFD MakeFD(ulong a, ulong b) { SFD s; *(uint*)&s.A = (uint)a; *(ulong*)&s.B = b; return s; }

    [MethodImpl(NoInl)]
    static SDF MakeDF(ulong a, ulong b) { SDF s; *(ulong*)&s.A = a; *(uint*)&s.B = (uint)b; return s; }

    [MethodImpl(NoInl)]
    static SFFF MakeFFF(ulong a, ulong b, ulong c)
    {
        SFFF s;
        *(uint*)&s.A = (uint)a; *(uint*)&s.B = (uint)b; *(uint*)&s.C = (uint)c;
        return s;
    }

    [MethodImpl(NoInl)]
    static SDDD MakeDDD(ulong a, ulong b, ulong c)
    {
        SDDD s;
        *(ulong*)&s.A = a; *(ulong*)&s.B = b; *(ulong*)&s.C = c;
        return s;
    }

    // ================= the C library =================

    [DllImport("*")] static extern double fmod(double a, double b);
    [DllImport("*")] static extern float fmodf(float a, float b);
    [DllImport("*")] static extern double frexp(double a, int* e);
    [DllImport("*")] static extern double modf(double a, double* ip);
    [DllImport("*")] static extern float modff(float a, float* ip);
    [DllImport("*")] static extern double ldexp(double a, int e);
    [DllImport("*")] static extern float ldexpf(float a, int e);
    [DllImport("*")] static extern double copysign(double a, double b);
    [DllImport("*")] static extern float copysignf(float a, float b);
    [DllImport("*")] static extern double fabs(double a);
    [DllImport("*")] static extern float fabsf(float a);
    [DllImport("*")] static extern double nextafter(double a, double b);
    [DllImport("*")] static extern float nextafterf(float a, float b);
    [DllImport("*")] static extern double scalbn(double a, int e);
    [DllImport("*")] static extern double sqrt(double a);
    [DllImport("*")] static extern float sqrtf(float a);
    [DllImport("*")] static extern double hypot(double a, double b);
    [DllImport("*")] static extern double fma(double a, double b, double c);
    [DllImport("*")] static extern float fmaf(float a, float b, float c);
    [DllImport("*")] static extern nint lrint(double a);
    [DllImport("*")] static extern nint lround(double a);
    [DllImport("*")] static extern nint lroundf(float a);
    [DllImport("*")] static extern double fdim(double a, double b);
    [DllImport("*")] static extern double fmax(double a, double b);
    [DllImport("*")] static extern float fminf(float a, float b);

    // A complex number is a struct of two doubles or two floats.
    [DllImport("*")] static extern double cabs(SDD z);
    [DllImport("*")] static extern float cabsf(SFF z);
    [DllImport("*")] static extern SDD conj(SDD z);
    [DllImport("*")] static extern SFF conjf(SFF z);
    [DllImport("*")] static extern double creal(SDD z);
    [DllImport("*")] static extern double cimag(SDD z);
    [DllImport("*")] static extern float crealf(SFF z);
    [DllImport("*")] static extern float cimagf(SFF z);
    [DllImport("*")] static extern SDD cproj(SDD z);

    // Variable arguments are passed as integers, so a fixed signature
    // describes the same registers and stack slots.
    [DllImport("*", EntryPoint = "snprintf")]
    static extern int snprintf_d(byte* buf, nuint n, byte* fmt, double a, int b, double c, long d, double e,
                                 double f, int g, double h);

    [DllImport("*", EntryPoint = "snprintf")]
    static extern int snprintf_i(byte* buf, nuint n, byte* fmt, long a, long b, long c, long d, long e, long f,
                                 long g, long h);

    // ================= typed calls =================

    [MethodImpl(NoInl)]
    static ulong Mix9(float a, double b, SFF c, long d, double e, float f, SDD g, float h, double i)
    {
        ulong x = H(Seed, BF(a));
        x = H(x, BD(b)); x = H(x, BF(c.A)); x = H(x, BF(c.B)); x = H(x, (ulong)d); x = H(x, BD(e));
        x = H(x, BF(f)); x = H(x, BD(g.A)); x = H(x, BD(g.B)); x = H(x, BF(h)); x = H(x, BD(i));
        return x;
    }

    [UnmanagedCallersOnly]
    static ulong Mix9Unmanaged(float a, double b, SFF c, long d, double e, float f, SDD g, float h, double i)
        => Mix9(a, b, c, d, e, f, g, h, i);

    [MethodImpl(NoInl)]
    static ulong Mix9Tail(float a, double b, SFF c, long d, double e, float f, SDD g, float h, double i)
        => Mix9(h, i, c, d + 1, b, a, g, f, e);

    [MethodImpl(NoInl)]
    static void RefOut(ref float rf, ref double rd, out float of, out double od, in SFD s)
    {
        of = rf;
        od = rd;
        rf = s.A;
        rd = s.B;
    }

    [MethodImpl(NoInl)]
    static ulong Params(params double[] v)
    {
        ulong x = Seed;
        for (int n = 0; n < v.Length; n++) x = H(x, BD(v[n]));
        return x;
    }

    [MethodImpl(NoInl)]
    static T Pick<T>(bool first, T a, T b) where T : unmanaged => first ? a : b;

    [MethodImpl(NoInl)]
    static ulong Bits<T>(T v) where T : unmanaged
    {
        ulong r = 0;
        Buffer.MemoryCopy(&v, &r, 8, sizeof(T));
        return r;
    }

    [MethodImpl(NoInl)]
    static ulong ViaInterface<T>(T h, float a, double b, SFF c, long d) where T : IHash => h.Hash(a, b, c, d);

    [MethodImpl(NoInl)]
    static ulong ViaInterfaceBoxed(IHash h, float a, double b, SFF c, long d) => h.Hash(a, b, c, d);

    [MethodImpl(NoInl)]
    static SNest Nest(SNest s, float q)
    {
        SNest r;
        r.P.A = s.P.B;
        r.P.B = s.P.A;
        r.Q.A = q;
        r.R = s.R + 1;
        return r;
    }

    [MethodImpl(NoInl)]
    static SFFFF Rotate(SFFFF s)
    {
        SFFFF r;
        r.A = s.B; r.B = s.C; r.C = s.D; r.D = s.A;
        return r;
    }

    [MethodImpl(NoInl)]
    static SDDD Rotate3(SDDD s)
    {
        SDDD r;
        r.A = s.B; r.B = s.C; r.C = s.A;
        return r;
    }

    [MethodImpl(NoInl)]
    static Func<double, float, ulong> Capture(float cf, double cd, SFF cs)
        => (double a, float b) => H(H(H(H(H(H(Seed, BF(cf)), BD(cd)), BF(cs.A)), BF(cs.B)), BD(a)), BF(b));

    [MethodImpl(NoInl)]
    static double Depth(int n, double a, float b, double c)
    {
        if (n == 0) return a;
        return Depth(n - 1, c, b, a);
    }

    // ================= the runs =================

    static void RunRawScalars()
    {
        ulong* d = stackalloc ulong[12];
        ulong* f = stackalloc ulong[12];
        for (int r = 0; r < 12; r++)
        {
            for (int n = 0; n < 12; n++)
            {
                d[n] = PD[(n + r) % 12];
                f[n] = PF[(n * 5 + r) % 12];
            }
            long ia = (int)(uint)(d[0] >> 3), ig = -(int)(uint)(d[1] >> 7) - r;
            long ld = (long)d[2], lk = (long)~d[3];
            ulong want = Mix12Ref(ia, d[4], f[0], ld, f[1], d[5], ig, f[2], d[6], f[3], lk, d[7]);

            Check("twelve mixed parameters, typed, round " + r, want,
                  Mix12((int)ia, D(d[4]), F((uint)f[0]), ld, F((uint)f[1]), D(d[5]), (int)ig, F((uint)f[2]), D(d[6]),
                        F((uint)f[3]), lk, D(d[7])));

            Check("twelve floats, typed, round " + r, Hash12(f, 0xFFFFFFFFUL),
                  Floats12(F((uint)f[0]), F((uint)f[1]), F((uint)f[2]), F((uint)f[3]), F((uint)f[4]), F((uint)f[5]),
                           F((uint)f[6]), F((uint)f[7]), F((uint)f[8]), F((uint)f[9]), F((uint)f[10]),
                           F((uint)f[11])));
            Check("twelve doubles, typed, round " + r, Hash12(d, ~0UL),
                  Doubles12(D(d[0]), D(d[1]), D(d[2]), D(d[3]), D(d[4]), D(d[5]), D(d[6]), D(d[7]), D(d[8]), D(d[9]),
                            D(d[10]), D(d[11])));
            if (!Wide) continue;

            var raw = (delegate*<long, ulong, ulong, long, ulong, ulong, long, ulong, ulong, ulong, long, ulong, ulong>)
                      (delegate*<int, double, float, long, float, double, int, float, double, float, long, double, ulong>)
                      &Mix12;
            Check("twelve mixed parameters, raw, round " + r, want,
                  raw(ia, d[4], f[0] | Junk, ld, f[1] | Ones, d[5], ig, f[2] | Junk, d[6], f[3] | Ones, lk, d[7]));

            var rawF = (delegate*<ulong, ulong, ulong, ulong, ulong, ulong, ulong, ulong, ulong, ulong, ulong, ulong,
                                  ulong>)
                       (delegate*<float, float, float, float, float, float, float, float, float, float, float, float,
                                  ulong>)&Floats12;
            Check("twelve floats, raw, round " + r, Hash12(f, 0xFFFFFFFFUL),
                  rawF(f[0] | Junk, f[1] | Ones, f[2] | Junk, f[3] | Ones, f[4] | Junk, f[5] | Ones, f[6] | Junk,
                       f[7] | Ones, f[8] | Junk, f[9] | Ones, f[10] | Junk, f[11] | Ones));

            var rawD = (delegate*<ulong, ulong, ulong, ulong, ulong, ulong, ulong, ulong, ulong, ulong, ulong, ulong,
                                  ulong>)
                       (delegate*<double, double, double, double, double, double, double, double, double, double,
                                  double, double, ulong>)&Doubles12;
            Check("twelve doubles, raw, round " + r, Hash12(d, ~0UL),
                  rawD(d[0], d[1], d[2], d[3], d[4], d[5], d[6], d[7], d[8], d[9], d[10], d[11]));
        }
    }

    static void RunRawFloat()
    {
        long* tab = stackalloc long[256];
        for (int n = 0; n < 256; n++) tab[n] = (n + 1) * 0x0101010101010101L + n;

        for (int n = 0; n < PF.Length; n++)
        {
            ulong b = PF[n];
            ulong b2 = PF[(n + 5) % PF.Length];
            string r = "float " + n + " with a dirty register, ";
            for (int pass = 0; pass < 2; pass++)
            {
                ulong j = pass == 0 ? Junk : Ones;
                ulong j2 = pass == 0 ? Ones : Junk;
                ulong v = b | j;

                Check(r + "bits widened signed", (ulong)(long)(int)b, (ulong)((delegate*<ulong, long>)(delegate*<float, long>)&FBitsWide)(v));
                Check(r + "bits widened unsigned", b, ((delegate*<ulong, ulong>)(delegate*<float, ulong>)&FUBitsWide)(v));
                Check(r + "sign of the bits", (b >> 31) & 1, ((delegate*<ulong, bool>)(delegate*<float, bool>)&FBitsNeg)(v) ? 1UL : 0);
                Check(r + "bits equal to an int", 1, ((delegate*<ulong, int, bool>)(delegate*<float, int, bool>)&FBitsEq)(v, (int)b) ? 1UL : 0);
                Check(r + "bits equal to a uint", 1, ((delegate*<ulong, uint, bool>)(delegate*<float, uint, bool>)&FBitsEqU)(v, (uint)b) ? 1UL : 0);
                Check(r + "bits shifted", b << 4, ((delegate*<ulong, ulong>)(delegate*<float, ulong>)&FBitsShift)(v));
                Check(r + "bits as an index", (ulong)tab[b >> 24], (ulong)((delegate*<ulong, long*, long>)(delegate*<float, long*, long>)&FBitsIndex)(v, tab));
                Check(r + "reinterpreted unsigned", b, ((delegate*<ulong, ulong>)(delegate*<float, ulong>)&FCast)(v));
                Check(r + "reinterpreted signed", (ulong)(long)(int)b, (ulong)((delegate*<ulong, long>)(delegate*<float, long>)&FCastS)(v));
                Check(r + "stored", b ^ (b << 1) ^ (b << 2), ((delegate*<ulong, ulong>)(delegate*<float, ulong>)&FStore)(v));

                object o = ((delegate*<ulong, object>)(delegate*<float, object>)&FBox)(v);
                Check(r + "boxed", b, BF((float)o));

                bool nan = (b & 0x7FFFFFFF) > 0x7F800000;
                Check(r + "equal to itself with other upper bits", nan ? 0UL : 1UL,
                      ((delegate*<ulong, ulong, bool>)(delegate*<float, float, bool>)&FEq)(v, b | j2) ? 1UL : 0);
                Check(r + "less than itself with other upper bits", 0,
                      ((delegate*<ulong, ulong, bool>)(delegate*<float, float, bool>)&FLt)(v, b | j2) ? 1UL : 0);

                // The double of a float is exact; compare with the typed call.
                if (!nan)
                    Check(r + "widened to double", BD(FWiden(F((uint)b))),
                          ((delegate*<ulong, ulong>)(delegate*<float, double>)&FWiden)(v));

                Check(r + "packed with a float", b | (b2 << 32),
                      ((delegate*<ulong, ulong, ulong>)(delegate*<float, float, SFF>)&FPack)(v, b2 | j2));
                Check(r + "packed before an int", b | (0x89ABCDEFUL << 32),
                      ((delegate*<ulong, int, ulong>)(delegate*<float, int, SFI>)&FPackI)(v, unchecked((int)0x89ABCDEF)));
                Check(r + "packed after an int", 0x89ABCDEFUL | (b << 32),
                      ((delegate*<int, ulong, ulong>)(delegate*<int, float, SIF>)&IPackF)(unchecked((int)0x89ABCDEF), v));

                SLL fd = ((delegate*<ulong, ulong, SLL>)(delegate*<float, double, SFD>)&FPackD)(v, PD[n]);
                Check(r + "packed before a double, first register", b, (ulong)fd.X & 0xFFFFFFFFUL);
                Check(r + "packed before a double, second register", PD[n], (ulong)fd.Y);

                SLL f3 = ((delegate*<ulong, ulong, ulong, SLL>)(delegate*<float, float, float, SFFF>)&FPack3)(v, b2 | j2, ~b | j);
                Check(r + "three floats packed, first register", b | (b2 << 32), (ulong)f3.X);
                Check(r + "three floats packed, second register", ~b & 0xFFFFFFFFUL, (ulong)f3.Y & 0xFFFFFFFFUL);

                Check(r + "returned", b, ((delegate*<ulong, ulong>)(delegate*<ulong, float>)&RetF)(v) & 0xFFFFFFFFUL);
                Check(r + "returned from int bits", b,
                      ((delegate*<int, ulong>)(delegate*<int, float>)&RetFFromInt)((int)b) & 0xFFFFFFFFUL);

                // Floating-point arguments where the method has integers.
                Check(r + "passed where a long is declared", H(H(H(Seed, PD[n]), b), PD[(n + 1) % 12]),
                      ((delegate*<double, float, double, ulong>)(delegate*<long, long, long, ulong>)&TakeLongs)(
                          D(PD[n]), F((uint)b), D(PD[(n + 1) % 12])));
            }
        }

        for (int n = 0; n < PD.Length; n++)
        {
            ulong b = PD[n];
            Check("double " + n + " returned", b, ((delegate*<ulong, ulong>)(delegate*<ulong, double>)&RetD)(b));
            Check("double " + n + " returned from long bits", b,
                  ((delegate*<long, ulong>)(delegate*<long, double>)&RetDFromLong)((long)b));
        }
        Check("constant float returned", 0xBFC00000UL, ((delegate*<ulong>)(delegate*<float>)&RetFConst)() & 0xFFFFFFFFUL);
        Check("constant double returned", 0xBFF8000000000000UL, ((delegate*<ulong>)(delegate*<double>)&RetDConst)());
    }

    static void RunRawStructs()
    {
        ulong* v = stackalloc ulong[16];
        for (int n = 0; n < 12; n++)
        {
            ulong d0 = PD[n], d1 = PD[(n + 1) % 12], d2 = PD[(n + 2) % 12];
            ulong f0 = PF[n], f1 = PF[(n + 3) % 12], f2 = PF[(n + 7) % 12];
            long pre = (long)(d2 ^ 0x1111111111111111UL), post = (long)(d2 * 31);
            string r = "struct round " + n + ", ";

            v[0] = (ulong)pre; v[1] = f0; v[2] = (ulong)post;
            Check(r + "one float, typed", HashList(v, 3), TakeF(pre, MakeF(f0), post));
            if (Wide)
                Check(r + "one float, raw", HashList(v, 3),
                      ((delegate*<long, ulong, long, ulong>)(delegate*<long, SF, long, ulong>)&TakeF)(pre, f0 | Junk, post));

            v[1] = d0;
            Check(r + "one double, typed", HashList(v, 3), TakeD(pre, MakeD(d0), post));
            if (Wide)
                Check(r + "one double, raw", HashList(v, 3),
                      ((delegate*<long, ulong, long, ulong>)(delegate*<long, SD, long, ulong>)&TakeD)(pre, d0, post));

            v[1] = f0; v[2] = f1; v[3] = (ulong)post;
            Check(r + "two floats, typed", HashList(v, 4), TakeFF(pre, MakeFF(f0, f1), post));
            if (Wide)
                Check(r + "two floats, raw", HashList(v, 4),
                      ((delegate*<long, ulong, long, ulong>)(delegate*<long, SFF, long, ulong>)&TakeFF)(pre, f0 | (f1 << 32), post));

            v[1] = d0; v[2] = d1;
            Check(r + "two doubles, typed", HashList(v, 4), TakeDD(pre, MakeDD(d0, d1), post));
            if (Wide)
                Check(r + "two doubles, raw", HashList(v, 4),
                      ((delegate*<long, ulong, ulong, long, ulong>)(delegate*<long, SDD, long, ulong>)&TakeDD)(pre, d0, d1, post));

            v[1] = f0; v[2] = d1;
            Check(r + "float and double, typed", HashList(v, 4), TakeFD(pre, MakeFD(f0, d1), post));
            if (Wide)
                Check(r + "float and double, raw", HashList(v, 4),
                      ((delegate*<long, ulong, ulong, long, ulong>)(delegate*<long, SFD, long, ulong>)&TakeFD)(pre, f0 | Junk, d1, post));

            v[1] = d0; v[2] = f1;
            Check(r + "double and float, typed", HashList(v, 4), TakeDF(pre, MakeDF(d0, f1), post));
            if (Wide)
                Check(r + "double and float, raw", HashList(v, 4),
                      ((delegate*<long, ulong, ulong, long, ulong>)(delegate*<long, SDF, long, ulong>)&TakeDF)(pre, d0, f1 | Ones, post));

            v[1] = f0; v[2] = (ulong)(long)(int)f1;
            SFI fi; *(uint*)&fi.A = (uint)f0; fi.B = (int)f1;
            Check(r + "float and int, typed", HashList(v, 4), TakeFI(pre, fi, post));
            if (Wide)
                Check(r + "float and int, raw", HashList(v, 4),
                      ((delegate*<long, ulong, long, ulong>)(delegate*<long, SFI, long, ulong>)&TakeFI)(pre, f0 | (f1 << 32), post));

            v[1] = f0; v[2] = f1; v[3] = f2; v[4] = (ulong)post;
            Check(r + "three floats, typed", HashList(v, 5), TakeFFF(pre, MakeFFF(f0, f1, f2), post));
            if (Wide)
                Check(r + "three floats, raw", HashList(v, 5),
                      ((delegate*<long, ulong, ulong, long, ulong>)(delegate*<long, SFFF, long, ulong>)&TakeFFF)(pre, f0 | (f1 << 32), f2 | Junk, post));

            v[1] = d0; v[2] = d1; v[3] = d2;
            ulong* three = stackalloc ulong[3];
            three[0] = d0; three[1] = d1; three[2] = d2;
            Check(r + "three doubles, typed", HashList(v, 5), TakeDDD(pre, MakeDDD(d0, d1, d2), post));
            if (Wide)
                Check(r + "three doubles, raw", HashList(v, 5),
                      ((delegate*<long, ulong*, long, ulong>)(delegate*<long, SDDD, long, ulong>)&TakeDDD)(pre, three, post));

            for (int k = 0; k < 7; k++) v[k] = (ulong)(pre + k * 0x0101010101010101L);
            v[7] = d0; v[8] = d1; v[9] = (ulong)post;
            Check(r + "two doubles in the last register and on the stack, typed", HashList(v, 10),
                  TakeSplit((long)v[0], (long)v[1], (long)v[2], (long)v[3], (long)v[4], (long)v[5], (long)v[6],
                            MakeDD(d0, d1), post));
            if (Wide)
                Check(r + "two doubles in the last register and on the stack, raw", HashList(v, 10),
                      ((delegate*<long, long, long, long, long, long, long, ulong, ulong, long, ulong>)
                       (delegate*<long, long, long, long, long, long, long, SDD, long, ulong>)&TakeSplit)(
                          (long)v[0], (long)v[1], (long)v[2], (long)v[3], (long)v[4], (long)v[5], (long)v[6], d0, d1, post));

            v[0] = f0; v[1] = f1; v[2] = d0; v[3] = d1; v[4] = f2; v[5] = d2; v[6] = f1; v[7] = d1; v[8] = f2;
            v[9] = f0; v[10] = f1; v[11] = d0; v[12] = f2;
            Check(r + "six structs and two scalars, typed", HashList(v, 13),
                  TakeMany(MakeFF(f0, f1), MakeDD(d0, d1), MakeFD(f2, d2), MakeF(f1), MakeD(d1), MakeFFF(f2, f0, f1),
                           D(d0), F((uint)f2)));
            if (Wide)
                Check(r + "six structs and two scalars, raw", HashList(v, 13),
                      ((delegate*<ulong, ulong, ulong, ulong, ulong, ulong, ulong, ulong, ulong, ulong, ulong, ulong>)
                       (delegate*<SFF, SDD, SFD, SF, SD, SFFF, double, float, ulong>)&TakeMany)(
                          f0 | (f1 << 32), d0, d1, f2 | Junk, d2, f1 | Ones, d1, f2 | (f0 << 32), f1 | Junk, d0,
                          f2 | Ones));

            if (Wide)
            {
                // Returned structs, taken as integers.
                Check(r + "one float returned", f0,
                      ((delegate*<ulong, ulong>)(delegate*<ulong, SF>)&MakeF)(f0) & 0xFFFFFFFFUL);
                Check(r + "one double returned", d0, ((delegate*<ulong, ulong>)(delegate*<ulong, SD>)&MakeD)(d0));
                Check(r + "two floats returned", f0 | (f1 << 32),
                      ((delegate*<ulong, ulong, ulong>)(delegate*<ulong, ulong, SFF>)&MakeFF)(f0, f1));
                SLL p = ((delegate*<ulong, ulong, SLL>)(delegate*<ulong, ulong, SDD>)&MakeDD)(d0, d1);
                Check(r + "two doubles returned, first register", d0, (ulong)p.X);
                Check(r + "two doubles returned, second register", d1, (ulong)p.Y);
                p = ((delegate*<ulong, ulong, SLL>)(delegate*<ulong, ulong, SFD>)&MakeFD)(f0, d1);
                Check(r + "float and double returned, first register", f0, (ulong)p.X & 0xFFFFFFFFUL);
                Check(r + "float and double returned, second register", d1, (ulong)p.Y);
                p = ((delegate*<ulong, ulong, SLL>)(delegate*<ulong, ulong, SDF>)&MakeDF)(d0, f1);
                Check(r + "double and float returned, first register", d0, (ulong)p.X);
                Check(r + "double and float returned, second register", f1, (ulong)p.Y & 0xFFFFFFFFUL);
                p = ((delegate*<ulong, ulong, ulong, SLL>)(delegate*<ulong, ulong, ulong, SFFF>)&MakeFFF)(f0, f1, f2);
                Check(r + "three floats returned, first register", f0 | (f1 << 32), (ulong)p.X);
                Check(r + "three floats returned, second register", f2, (ulong)p.Y & 0xFFFFFFFFUL);
            }

            // Returned by reference: typed, read back through memory.
            SDDD t = MakeDDD(d0, d1, d2);
            Check(r + "three doubles returned", H(H(H(Seed, d0), d1), d2), H(H(H(Seed, BD(t.A)), BD(t.B)), BD(t.C)));
            SFF ff = MakeFF(f0, f1);
            Check(r + "two floats returned, typed", f0 | (f1 << 32), *(ulong*)&ff);
            SFD sfd = MakeFD(f0, d1);
            Check(r + "float and double returned, typed", H(H(Seed, f0), d1), H(H(Seed, BF(sfd.A)), BD(sfd.B)));
        }
    }

    static void RunLibc()
    {
        Check("fmod", 0x3FF8000000000000UL, BD(fmod(D(0x401E000000000000UL), D(0x4000000000000000UL))));
        Check("fmodf", 0xBFC00000UL, BF(fmodf(F(0xC0F00000U), F(0x40000000U))));
        int e = 0;
        Check("frexp", 0x3FE8000000000000UL, BD(frexp(D(0x4038000000000000UL), &e)));
        Check("frexp exponent", 5, (ulong)e);
        double ip = 0;
        Check("modf", 0xBFE8000000000000UL, BD(modf(D(0xC00E000000000000UL), &ip)));
        Check("modf integral part", 0xC008000000000000UL, BD(ip));
        float ipf = 0;
        Check("modff", 0x3F400000UL, BF(modff(F(0x40700000U), &ipf)));
        Check("modff integral part", 0x40400000UL, BF(ipf));
        Check("ldexp", 0x4038000000000000UL, BD(ldexp(D(0x3FF8000000000000UL), 4)));
        Check("ldexpf", 0x41C00000UL, BF(ldexpf(F(0x3FC00000U), 4)));
        Check("copysign", 0xC000000000000000UL, BD(copysign(D(0x4000000000000000UL), D(0x8000000000000000UL))));
        Check("copysignf", 0xC0000000UL, BF(copysignf(F(0x40000000U), F(0x80000000U))));
        Check("fabs", 0, BD(fabs(D(0x8000000000000000UL))));
        Check("fabsf", 0x7F800000UL, BF(fabsf(F(0xFF800000U))));
        Check("nextafter", 0x3FF0000000000001UL, BD(nextafter(D(0x3FF0000000000000UL), D(0x4000000000000000UL))));
        Check("nextafterf", 0x3F7FFFFFUL, BF(nextafterf(F(0x3F800000U), F(0x00000000U))));
        Check("scalbn", 1, BD(scalbn(D(0x3FF0000000000000UL), -1074)));
        Check("sqrt", 0x3FF6A09E667F3BCDUL, BD(sqrt(D(0x4000000000000000UL))));
        Check("sqrtf", 0x3FB504F3UL, BF(sqrtf(F(0x40000000U))));
        Check("hypot", 0x4014000000000000UL, BD(hypot(D(0x4008000000000000UL), D(0x4010000000000000UL))));
        // (1 + 2^-27)^2 - 1 and (1 + 2^-12)^2 - 1, exact only when fused
        Check("fma", 0x3E50000001000000UL, BD(fma(D(0x3FF0000002000000UL), D(0x3FF0000002000000UL), D(0xBFF0000000000000UL))));
        Check("fmaf", 0x3A000400UL, BF(fmaf(F(0x3F800800U), F(0x3F800800U), F(0xBF800000U))));
        Check("lrint", 2, (ulong)(long)lrint(D(0x4004000000000000UL)));
        Check("lround", 3, (ulong)(long)lround(D(0x4004000000000000UL)));
        Check("lround of a negative", unchecked((ulong)-3L), (ulong)(long)lround(D(0xC004000000000000UL)));
        Check("lroundf", unchecked((ulong)-8L), (ulong)(long)lroundf(F(0xC0F00000U)));
        Check("fdim", 0x4014000000000000UL, BD(fdim(D(0x401E000000000000UL), D(0x4004000000000000UL))));
        Check("fmax", 0x401E000000000000UL, BD(fmax(D(0x7FF8000000000000UL), D(0x401E000000000000UL))));
        Check("fminf", 0xC0F00000UL, BF(fminf(F(0x40000000U), F(0xC0F00000U))));

        SDD z = MakeDD(0x4008000000000000UL, 0xC010000000000000UL);      // 3 - 4i
        SFF zf = MakeFF(0xC0400000U, 0x40800000U);                       // -3 + 4i
        Check("cabs", 0x4014000000000000UL, BD(cabs(z)));
        Check("cabsf", 0x40A00000UL, BF(cabsf(zf)));
        Check("creal", 0x4008000000000000UL, BD(creal(z)));
        Check("cimag", 0xC010000000000000UL, BD(cimag(z)));
        Check("crealf", 0xC0400000UL, BF(crealf(zf)));
        Check("cimagf", 0x40800000UL, BF(cimagf(zf)));
        SDD c = conj(z);
        Check("conj, real part", 0x4008000000000000UL, BD(c.A));
        Check("conj, imaginary part", 0x4010000000000000UL, BD(c.B));
        SFF cf = conjf(zf);
        Check("conjf, real part", 0xC0400000UL, BF(cf.A));
        Check("conjf, imaginary part", 0xC0800000UL, BF(cf.B));
        c = cproj(MakeDD(0x3FF0000000000000UL, 0xFFF0000000000000UL));  // 1 - inf i
        Check("cproj, real part", 0x7FF0000000000000UL, BD(c.A));
        Check("cproj, imaginary part", 0x8000000000000000UL, BD(c.B));

        if (!Wide) return;

        // Eight variable arguments: five in registers, three on the stack.
        byte* buf = stackalloc byte[128];
        ReadOnlySpan<byte> fmtI = "%llx %llx %llx %llx %llx %llx %llx %llx"u8;
        ReadOnlySpan<byte> fmtD = "%llx %x %llx %llx %llx %llx %x %llx"u8;
        byte* want = stackalloc byte[128];
        byte* fmt = stackalloc byte[48];
        for (int n = 0; n < fmtI.Length; n++) fmt[n] = fmtI[n];
        fmt[fmtI.Length] = 0;
        int wl = snprintf_i(want, 128, fmt, (long)PD[0], 0x12345678, (long)PD[1], -5, (long)PD[2], (long)PD[3],
                            0x7654321, (long)PD[4]);
        for (int n = 0; n < fmtD.Length; n++) fmt[n] = fmtD[n];
        fmt[fmtD.Length] = 0;
        int gl = snprintf_d(buf, 128, fmt, D(PD[0]), 0x12345678, D(PD[1]), -5, D(PD[2]), D(PD[3]), 0x7654321,
                            D(PD[4]));
        Check("snprintf length", (ulong)wl, (ulong)gl);
        ulong hw = Seed, hg = Seed;
        for (int n = 0; n < wl && n < 128; n++) { hw = H(hw, want[n]); hg = H(hg, buf[n]); }
        Check("snprintf of doubles as variable arguments", hw, hg);
        Check("snprintf wrote the first argument", (ulong)'3', buf[0]);
    }

    static readonly Shape Other = new ShapeB();

    static void RunTyped()
    {
        Check("the other implementation", 1, Other.Hash(F(PF[0]), D(PD[0]), default, 0));
        ulong* v = stackalloc ulong[16];
        for (int n = 0; n < 12; n++)
        {
            ulong d0 = PD[n], d1 = PD[(n + 1) % 12], d2 = PD[(n + 2) % 12], d3 = PD[(n + 5) % 12];
            ulong f0 = PF[n], f1 = PF[(n + 3) % 12], f2 = PF[(n + 7) % 12], f3 = PF[(n + 8) % 12], f4 = PF[(n + 9) % 12];
            long l = (long)(d3 ^ d0);
            string r = "typed round " + n + ", ";
            float a = F((uint)f0), f = F((uint)f1), h = F((uint)f2);
            double b = D(d0), e = D(d1), i = D(d2);
            SFF c = MakeFF(f3, f4);
            SDD g = MakeDD(d3, d1 ^ 1);

            v[0] = f0; v[1] = d0; v[2] = f3; v[3] = f4; v[4] = (ulong)l; v[5] = d1; v[6] = f1; v[7] = d3;
            v[8] = d1 ^ 1; v[9] = f2; v[10] = d2;
            ulong want = HashList(v, 11);

            Check(r + "direct", want, Mix9(a, b, c, l, e, f, g, h, i));
            delegate*<float, double, SFF, long, double, float, SDD, float, double, ulong> fp = &Mix9;
            Check(r + "function pointer", want, fp(a, b, c, l, e, f, g, h, i));
            DMix del = Mix9;
            Check(r + "delegate", want, del(a, b, c, l, e, f, g, h, i));
            delegate* unmanaged<float, double, SFF, long, double, float, SDD, float, double, ulong> up = &Mix9Unmanaged;
            Check(r + "unmanaged function pointer", want, up(a, b, c, l, e, f, g, h, i));

            // Mix9Tail hands on (h, i, c, d + 1, b, a, g, f, e).
            v[0] = f2; v[1] = d2; v[4] = (ulong)(l + 1); v[5] = d0; v[6] = f0; v[9] = f1; v[10] = d1;
            Check(r + "tail call", HashList(v, 11), Mix9Tail(a, b, c, l, e, f, g, h, i));

            ShapeA sa = new ShapeA();
            sa.F = h;
            sa.D = i;
            Shape sb = n < 100 ? sa : Other;
            v[0] = f2; v[1] = d2; v[2] = f0; v[3] = d0; v[4] = f3; v[5] = f4; v[6] = (ulong)l;
            want = HashList(v, 7);
            Check(r + "virtual", want, sb.Hash(a, b, c, l));
            Check(r + "interface", want, ViaInterfaceBoxed(sa, a, b, c, l));
            HashStruct hs;
            hs.F = h;
            hs.D = i;
            Check(r + "struct through a generic interface", want, ViaInterface(hs, a, b, c, l));
            Check(r + "boxed struct through an interface", want, ViaInterfaceBoxed(hs, a, b, c, l));

            v[2] = d0; v[3] = f0; v[4] = d1; v[5] = f1; v[6] = d2; v[7] = f2; v[8] = d3; v[9] = f3; v[10] = d0 ^ 2;
            Check(r + "instance with nine parameters", HashList(v, 11),
                  sa.Instance(b, a, e, f, i, h, D(d3), F((uint)f3), D(d0 ^ 2)));

            SDD pr = sb.Pair(b, a);
            Check(r + "virtual returning a struct, first field", d0, BD(pr.A));
            Check(r + "virtual returning a struct, second field", f0 | 0x7700000000000000UL, BD(pr.B));

            float rf = a, of;
            double rd = b, od;
            SFD sfd = MakeFD(f1, d1);
            RefOut(ref rf, ref rd, out of, out od, in sfd);
            Check(r + "ref and out", H(H(H(H(Seed, f1), d1), f0), d0), H(H(H(H(Seed, BF(rf)), BD(rd)), BF(of)), BD(od)));

            v[0] = d0; v[1] = d1; v[2] = d2; v[3] = d3;
            Check(r + "parameter array", HashList(v, 4), Params(b, e, i, D(d3)));

            Check(r + "generic float", f1, BF(Pick(n % 2 == 0, f, h)) ^ (n % 2 == 0 ? 0 : f1 ^ f2));
            Check(r + "generic double", d1, BD(Pick(n % 2 != 0, i, e)) ^ (n % 2 != 0 ? d1 ^ d2 : 0));
            Check(r + "generic struct of two floats", f3 | (f4 << 32), Bits(Pick(true, c, default)));
            Check(r + "generic bits of a float", f0, Bits(a));
            Check(r + "generic bits of a double", d0, Bits(b));

            Box<float> bf = new Box<float>();
            bf.V = a;
            Check(r + "generic class of float, old value", f0, BF(bf.Swap(f)));
            Check(r + "generic class of float, new value", f1, BF(bf.V));
            Box<SFD> bs = new Box<SFD>();
            bs.V = sfd;
            SFD old = bs.Swap(MakeFD(f2, d2));
            Check(r + "generic class of a struct", H(H(H(H(Seed, f1), d1), f2), d2),
                  H(H(H(H(Seed, BF(old.A)), BD(old.B)), BF(bs.V.A)), BD(bs.V.B)));

            SNest ns;
            ns.P = c;
            ns.Q = MakeF(f0);
            ns.R = (int)n;
            SNest nr = Nest(ns, h);
            Check(r + "nested struct", H(H(H(H(Seed, f4), f3), f2), (ulong)(n + 1)),
                  H(H(H(H(Seed, BF(nr.P.A)), BF(nr.P.B)), BF(nr.Q.A)), (ulong)nr.R));

            SFFFF q;
            *(uint*)&q.A = (uint)f0; *(uint*)&q.B = (uint)f1; *(uint*)&q.C = (uint)f2; *(uint*)&q.D = (uint)f3;
            q = Rotate(q);
            Check(r + "four floats, first half", f1 | (f2 << 32), *(ulong*)&q);
            Check(r + "four floats, second half", f3 | (f0 << 32), *((ulong*)&q + 1));

            SDDD t3 = Rotate3(MakeDDD(d0, d1, d2));
            Check(r + "three doubles by reference", H(H(H(Seed, d1), d2), d0),
                  H(H(H(Seed, BD(t3.A)), BD(t3.B)), BD(t3.C)));

            Func<double, float, ulong> fn = Capture(a, b, c);
            Check(r + "lambda with captured values", H(H(H(H(H(H(Seed, f0), d0), f3), f4), d1), f1), fn(e, f));

            object ob = b, of2 = a;
            Check(r + "boxed double", d0, BD((double)ob));
            Check(r + "boxed float", f0, BF((float)of2));

            Check(r + "recursion, even depth", d0, BD(Depth(6, b, a, e)));
            Check(r + "recursion, odd depth", d1, BD(Depth(7, b, a, e)));
        }
    }

    static int Main()
    {
        if (!Wide) Console.WriteLine("32-bit registers: raw calls and snprintf skipped");
        RunRawScalars();
        if (Wide) RunRawFloat();
        RunRawStructs();
        RunLibc();
        RunTyped();

        Console.WriteLine("checks " + checks);
        Console.WriteLine(fails == 0 ? "ALL PASS" : "FAILED " + fails);
        return fails == 0 ? 0 : 1;
    }
}
