// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// double and float on a soft-float target whose registers hold 32 bits
// (riscv32).
//
// There is no F or D extension, so every floating-point operation is a
// helper call, and what is left of a double in the JIT only moves 64 bits
// around: locals, loads, stores, constants, arguments and return values.
// A double does not fit a register, so before lowering every double that
// remains is retyped to long and from then on is two int halves like any
// other long; a float is 32 bits and stays in one register.
//
// What can go wrong: an operation that was not turned into a helper call
// before the retyping and now acts on the bits as an integer (negation,
// absolute value, a compare, a select, a conversion); a constant whose bits
// are split into the halves the wrong way round; a double argument or
// result of a helper passed in the wrong register pair; a double local,
// field, array element or static that keeps only one half; a float that is
// widened or narrowed as an integer; a method that is a leaf for the JIT
// but calls helpers, and returns through a lost return address.
//
// The values are chosen so that every result is exact or is a well-known
// rounding, and the expected results are the IEEE 754 bit patterns as
// literals; results are compared as bits, half by half through memory.
//
// Each group prints the number of checks; a failed check prints one line
// with the expected and the actual bits. The run ends with ALL PASS or a
// FAIL count, and the guest returns 1 on failure.
using System;
using System.Runtime.CompilerServices;
using System.Threading;

struct DPair { public double X; public double Y; }
struct Mixed { public int I; public double D; public float F; public long L; }
class DBox { public float F; public double D; public int I; public double E; }

unsafe class Program
{
    const MethodImplOptions NI = MethodImplOptions.NoInlining;

    static int fails;
    static int checks;

    [MethodImpl(NI)] static uint Lo(long x) => ((uint*)&x)[0];
    [MethodImpl(NI)] static uint Hi(long x) => ((uint*)&x)[1];
    static string Hex(long x) => "0x" + Hi(x).ToString("x8") + Lo(x).ToString("x8");

    static void CheckBits(string name, long got, uint hi, uint lo)
    {
        checks++;
        if (Hi(got) != hi || Lo(got) != lo)
        {
            fails++;
            if (fails <= 60)
                Console.WriteLine("FAIL " + name + " expected 0x" + hi.ToString("x8") + lo.ToString("x8") + " got " + Hex(got));
        }
    }

    static void CheckD(string name, double got, uint hi, uint lo) => CheckBits(name, *(long*)&got, hi, lo);
    static void CheckF(string name, float got, uint bits) => CheckBits(name, *(int*)&got, (*(int*)&got) < 0 ? 0xFFFFFFFFu : 0u, bits);
    static void CheckL(string name, long got, uint hi, uint lo) => CheckBits(name, got, hi, lo);

    static void Check(string name, int got, int exp)
    {
        checks++;
        if (got != exp)
        {
            fails++;
            if (fails <= 60) Console.WriteLine("FAIL " + name + " expected " + exp.ToString() + " got " + got.ToString());
        }
    }

    static void Group(string name)
    {
        Console.WriteLine("ok   " + name + " " + checks.ToString());
    }

    // Values the JIT cannot see through.
    static double s_d;
    static float s_f;
    [MethodImpl(NI)] static double D(uint hi, uint lo) { long b = 0; ((uint*)&b)[0] = lo; ((uint*)&b)[1] = hi; return *(double*)&b; }
    [MethodImpl(NI)] static float F(uint bits) => *(float*)&bits;

    // ---- arithmetic ----

    [MethodImpl(NI)] static double Add(double a, double b) => a + b;
    [MethodImpl(NI)] static double Sub(double a, double b) => a - b;
    [MethodImpl(NI)] static double Mul(double a, double b) => a * b;
    [MethodImpl(NI)] static double Div(double a, double b) => a / b;
    [MethodImpl(NI)] static double Rem(double a, double b) => a % b;
    [MethodImpl(NI)] static double Neg(double a) => -a;
    [MethodImpl(NI)] static double Abs(double a) => Math.Abs(a);
    [MethodImpl(NI)] static double Sqrt(double a) => Math.Sqrt(a);
    [MethodImpl(NI)] static double Floor(double a) => Math.Floor(a);
    [MethodImpl(NI)] static double Ceil(double a) => Math.Ceiling(a);
    [MethodImpl(NI)] static double Round(double a) => Math.Round(a);
    [MethodImpl(NI)] static double Trunc(double a) => Math.Truncate(a);
    [MethodImpl(NI)] static double Max(double a, double b) => Math.Max(a, b);
    [MethodImpl(NI)] static double Min(double a, double b) => Math.Min(a, b);
    [MethodImpl(NI)] static double Expr(double a, double b, double c) => (a + b) * c - a / b;
    [MethodImpl(NI)] static double AddConst(double a) => a + 0.5;
    [MethodImpl(NI)] static double MulConst(double a) => a * -2.0;
    [MethodImpl(NI)] static double Const() => 1234.5;
    [MethodImpl(NI)] static double ConstNeg() => -0.0;
    [MethodImpl(NI)] static double Sel(bool c, double a, double b) => c ? a : b;
    [MethodImpl(NI)] static double SelConst(int c) => c > 0 ? 1.0 : -1.0;
    [MethodImpl(NI)] static double SumLoop(double step, int n) { double s = 0; for (int i = 0; i < n; i++) s += step; return s; }
    [MethodImpl(NI)] static double Horner(double x) { double r = 1.0; for (int i = 0; i < 5; i++) r = r * x + 1.0; return r; }

    [MethodImpl(NI)] static float AddF(float a, float b) => a + b;
    [MethodImpl(NI)] static float MulF(float a, float b) => a * b;
    [MethodImpl(NI)] static float DivF(float a, float b) => a / b;
    [MethodImpl(NI)] static float NegF(float a) => -a;
    [MethodImpl(NI)] static float AbsF(float a) => MathF.Abs(a);
    [MethodImpl(NI)] static float ConstF() => 0.1f;
    [MethodImpl(NI)] static float SumLoopF(float step, int n) { float s = 0; for (int i = 0; i < n; i++) s += step; return s; }

    // ---- comparisons ----

    [MethodImpl(NI)] static bool Lt(double a, double b) => a < b;
    [MethodImpl(NI)] static bool Le(double a, double b) => a <= b;
    [MethodImpl(NI)] static bool Gt(double a, double b) => a > b;
    [MethodImpl(NI)] static bool Ge(double a, double b) => a >= b;
    [MethodImpl(NI)] static bool Eq(double a, double b) => a == b;
    [MethodImpl(NI)] static bool Ne(double a, double b) => a != b;
    [MethodImpl(NI)] static int BrLt(double a, double b) { if (a < b) return 11; return 22; }
    [MethodImpl(NI)] static int BrNotLt(double a, double b) { if (!(a < b)) return 11; return 22; }
    [MethodImpl(NI)] static bool IsNaN(double a) => double.IsNaN(a);
    [MethodImpl(NI)] static bool IsNeg(double a) => double.IsNegative(a);
    [MethodImpl(NI)] static bool Lt0(double a) => a < 0;
    [MethodImpl(NI)] static bool Eq0(double a) => a == 0;
    [MethodImpl(NI)] static bool LtF(float a, float b) => a < b;
    [MethodImpl(NI)] static bool EqF(float a, float b) => a == b;

    // ---- conversions ----

    [MethodImpl(NI)] static double FromInt(int x) => x;
    [MethodImpl(NI)] static double FromUInt(uint x) => x;
    [MethodImpl(NI)] static double FromLong(long x) => x;
    [MethodImpl(NI)] static double FromULong(ulong x) => x;
    [MethodImpl(NI)] static double FromFloat(float x) => x;
    [MethodImpl(NI)] static float ToFloat(double x) => (float)x;
    [MethodImpl(NI)] static int ToInt(double x) => (int)x;
    [MethodImpl(NI)] static uint ToUInt(double x) => (uint)x;
    [MethodImpl(NI)] static long ToLong(double x) => (long)x;
    [MethodImpl(NI)] static ulong ToULong(double x) => (ulong)x;
    [MethodImpl(NI)] static short ToShort(double x) => (short)x;
    [MethodImpl(NI)] static byte ToByte(double x) => (byte)x;
    [MethodImpl(NI)] static float FromIntF(int x) => x;
    [MethodImpl(NI)] static float FromLongF(long x) => x;
    [MethodImpl(NI)] static int ToIntF(float x) => (int)x;
    [MethodImpl(NI)] static long ToLongF(float x) => (long)x;
    [MethodImpl(NI)] static long Bits(double x) => BitConverter.DoubleToInt64Bits(x);
    [MethodImpl(NI)] static double FromBits(long x) => BitConverter.Int64BitsToDouble(x);
    [MethodImpl(NI)] static int BitsF(float x) => BitConverter.SingleToInt32Bits(x);

    // ---- moves ----

    [MethodImpl(NI)] static void Swap(ref double a, ref double b) { double t = a; a = b; b = t; }
    [MethodImpl(NI)] static double Field(DBox b) => b.D + b.E;
    [MethodImpl(NI)] static void SetFields(DBox b, double d, float f) { b.E = b.D; b.D = d; b.F = f; b.I = 7; }
    [MethodImpl(NI)] static DPair MakePair(double x, double y) { DPair p; p.X = y; p.Y = x; return p; }
    [MethodImpl(NI)] static double PairSum(DPair p) => p.X - p.Y;
    [MethodImpl(NI)] static Mixed MakeMixed(int i, double d, float f, long l) { Mixed m; m.I = i; m.D = d; m.F = f; m.L = l; return m; }
    [MethodImpl(NI)] static double ArrSum(double[] a) { double s = 0; for (int i = 0; i < a.Length; i++) s += a[i]; return s; }
    [MethodImpl(NI)] static void ArrScale(double[] a, double k) { for (int i = 0; i < a.Length; i++) a[i] *= k; }
    [MethodImpl(NI)] static T Pass<T>(int pad, T v) => v;
    [MethodImpl(NI)] static object Box(double d) => d;
    [MethodImpl(NI)] static double Unbox(object o) => (double)o;
    [MethodImpl(NI)] static double Many(double a, double b, double c, double d, double e, double f) => a - b + c - d + e - f;
    [MethodImpl(NI)] static double AcrossCalls(double a, double b) { double x = Add(a, b); double y = Mul(a, b); double z = Sub(x, y); return Add(Add(x, y), Add(z, a)); }

    [MethodImpl(NI)]
    static double InHandler(double a, double b, bool fail)
    {
        double r = a;
        try
        {
            r = Add(a, b);
            if (fail) throw new InvalidOperationException();
            r = Mul(r, b);
        }
        catch (InvalidOperationException)
        {
            r = Sub(r, 1.0);
        }
        finally
        {
            s_d = r;
        }
        return r;
    }

    static void TestArithmetic()
    {
        double a = D(0x3FF80000, 0), b = D(0x40020000, 0);      // 1.5, 2.25
        CheckD("1.5 + 2.25", Add(a, b), 0x400E0000, 0);         // 3.75
        CheckD("2.25 - 1.5", Sub(b, a), 0x3FE80000, 0);         // 0.75
        CheckD("1.5 - 2.25", Sub(a, b), 0xBFE80000, 0);         // -0.75
        CheckD("1.5 * 2.25", Mul(a, b), 0x400B0000, 0);         // 3.375
        CheckD("3.75 / 1.5", Div(D(0x400E0000, 0), a), 0x40040000, 0); // 2.5
        CheckD("1 / 3", Div(D(0x3FF00000, 0), D(0x40080000, 0)), 0x3FD55555, 0x55555555);
        CheckD("0.1 + 0.2", Add(D(0x3FB99999, 0x9999999A), D(0x3FC99999, 0x9999999A)), 0x3FD33333, 0x33333334);
        CheckD("5.5 % 2", Rem(D(0x40160000, 0), D(0x40000000, 0)), 0x3FF80000, 0);
        CheckD("-1.5", Neg(a), 0xBFF80000, 0);
        CheckD("-(-0.75)", Neg(D(0xBFE80000, 0)), 0x3FE80000, 0);
        CheckD("-0", Neg(D(0, 0)), 0x80000000, 0);
        CheckD("-(2^-1074)", Neg(D(0, 1)), 0x80000000, 1);
        CheckD("abs(-1.5)", Abs(D(0xBFF80000, 0)), 0x3FF80000, 0);
        CheckD("abs(-0)", Abs(D(0x80000000, 0)), 0, 0);
        CheckD("abs(2^-1074 negative)", Abs(D(0x80000000, 1)), 0, 1);
        CheckD("sqrt(2.25)", Sqrt(b), 0x3FF80000, 0);
        CheckD("sqrt(2)", Sqrt(D(0x40000000, 0)), 0x3FF6A09E, 0x667F3BCD);
        CheckD("floor(-1.5)", Floor(D(0xBFF80000, 0)), 0xC0000000, 0);
        CheckD("ceil(1.5)", Ceil(a), 0x40000000, 0);
        CheckD("round(2.5)", Round(D(0x40040000, 0)), 0x40000000, 0);
        CheckD("round(3.5)", Round(D(0x400C0000, 0)), 0x40100000, 0);
        CheckD("truncate(-1.5)", Trunc(D(0xBFF80000, 0)), 0xBFF00000, 0);
        CheckD("max", Max(a, b), 0x40020000, 0);
        CheckD("min", Min(a, b), 0x3FF80000, 0);
        // (1.5 + 2.25) * 2 - 1.5 / 2.25 = 7.5 - 0.6666666666666666 = 6.833333333333333
        CheckD("expression", Expr(a, b, D(0x40000000, 0)), 0x401B5555, 0x55555555);
        CheckD("1.5 + 0.5", AddConst(a), 0x40000000, 0);
        CheckD("1.5 * -2", MulConst(a), 0xC0080000, 0);
        CheckD("1234.5", Const(), 0x40934A00, 0);
        CheckD("-0.0", ConstNeg(), 0x80000000, 0);
        CheckD("select true", Sel(true, a, b), 0x3FF80000, 0);
        CheckD("select false", Sel(false, a, b), 0x40020000, 0);
        CheckD("select constant +", SelConst(1), 0x3FF00000, 0);
        CheckD("select constant -", SelConst(0), 0xBFF00000, 0);
        CheckD("0.1 ten times", SumLoop(D(0x3FB99999, 0x9999999A), 10), 0x3FEFFFFF, 0xFFFFFFFF);
        CheckD("0.25 eight times", SumLoop(D(0x3FD00000, 0), 8), 0x40000000, 0);
        // ((((1 * 2 + 1) * 2 + 1) * 2 + 1) * 2 + 1) * 2 + 1 = 63
        CheckD("horner", Horner(D(0x40000000, 0)), 0x404F8000, 0);
        CheckD("inf", Div(D(0x3FF00000, 0), D(0, 0)), 0x7FF00000, 0);
        CheckD("-inf", Div(D(0xBFF00000, 0), D(0, 0)), 0xFFF00000, 0);
        CheckD("2^1023 * 2", Mul(D(0x7FE00000, 0), D(0x40000000, 0)), 0x7FF00000, 0);

        CheckF("1.5f + 2.25f", AddF(F(0x3FC00000), F(0x40100000)), 0x40700000);
        CheckF("1.5f * 2.25f", MulF(F(0x3FC00000), F(0x40100000)), 0x40580000);
        CheckF("1f / 3f", DivF(F(0x3F800000), F(0x40400000)), 0x3EAAAAAB);
        CheckF("-1.5f", NegF(F(0x3FC00000)), 0xBFC00000);
        CheckF("-0f", NegF(F(0)), 0x80000000);
        CheckF("abs(-1.5f)", AbsF(F(0xBFC00000)), 0x3FC00000);
        CheckF("0.1f", ConstF(), 0x3DCCCCCD);
        CheckF("0.25f eight times", SumLoopF(F(0x3E800000), 8), 0x40000000);
        Group("arithmetic");
    }

    static void TestCompare()
    {
        double a = D(0x3FF80000, 0), b = D(0x40020000, 0), nan = D(0x7FF80000, 0), nz = D(0x80000000, 0), z = D(0, 0);
        double m = D(0xBFF80000, 0), big = D(0x7FE00000, 1), tiny = D(0, 1);
        Check("1.5 < 2.25", Lt(a, b) ? 1 : 0, 1);
        Check("2.25 < 1.5", Lt(b, a) ? 1 : 0, 0);
        Check("1.5 < 1.5", Lt(a, a) ? 1 : 0, 0);
        Check("1.5 <= 1.5", Le(a, a) ? 1 : 0, 1);
        Check("2.25 > 1.5", Gt(b, a) ? 1 : 0, 1);
        Check("1.5 >= 2.25", Ge(a, b) ? 1 : 0, 0);
        Check("1.5 == 1.5", Eq(a, a) ? 1 : 0, 1);
        Check("1.5 != 2.25", Ne(a, b) ? 1 : 0, 1);
        Check("-1.5 < 1.5", Lt(m, a) ? 1 : 0, 1);
        Check("1.5 < -1.5", Lt(a, m) ? 1 : 0, 0);
        Check("-1.5 < -0.0", Lt(m, nz) ? 1 : 0, 1);
        Check("-0.0 == 0.0", Eq(nz, z) ? 1 : 0, 1);
        Check("-0.0 < 0.0", Lt(nz, z) ? 1 : 0, 0);
        Check("0 < 2^-1074", Lt(z, tiny) ? 1 : 0, 1);
        // The values differ in one half only.
        Check("low half only <", Lt(D(0x7FE00000, 0), big) ? 1 : 0, 1);
        Check("low half only ==", Eq(D(0x7FE00000, 0), big) ? 1 : 0, 0);
        Check("high half only <", Lt(D(0x3FF00000, 5), D(0x3FF00001, 5)) ? 1 : 0, 1);
        Check("high half only ==", Eq(D(0x3FF00000, 5), D(0x3FF00001, 5)) ? 1 : 0, 0);
        Check("low half above 2^31 <", Lt(D(0x3FF00000, 0x80000000), D(0x3FF00000, 0x7FFFFFFF)) ? 1 : 0, 0);
        Check("nan == nan", Eq(nan, nan) ? 1 : 0, 0);
        Check("nan != nan", Ne(nan, nan) ? 1 : 0, 1);
        Check("nan < 1.5", Lt(nan, a) ? 1 : 0, 0);
        Check("1.5 < nan", Lt(a, nan) ? 1 : 0, 0);
        Check("nan >= 1.5", Ge(nan, a) ? 1 : 0, 0);
        Check("branch <", BrLt(a, b), 11);
        Check("branch not <", BrNotLt(a, b), 22);
        Check("branch < nan", BrLt(a, nan), 22);
        Check("branch not < nan", BrNotLt(a, nan), 11);
        Check("IsNaN(nan)", IsNaN(nan) ? 1 : 0, 1);
        Check("IsNaN(1.5)", IsNaN(a) ? 1 : 0, 0);
        Check("IsNaN(inf)", IsNaN(D(0x7FF00000, 0)) ? 1 : 0, 0);
        Check("IsNaN(low bits)", IsNaN(D(0x7FF00000, 1)) ? 1 : 0, 1);
        Check("IsNegative(-0.0)", IsNeg(nz) ? 1 : 0, 1);
        Check("IsNegative(1.5)", IsNeg(a) ? 1 : 0, 0);
        Check("-1.5 < 0", Lt0(m) ? 1 : 0, 1);
        Check("-0.0 < 0", Lt0(nz) ? 1 : 0, 0);
        Check("-0.0 == 0", Eq0(nz) ? 1 : 0, 1);
        Check("2^-1074 == 0", Eq0(tiny) ? 1 : 0, 0);
        Check("1.5f < 2.25f", LtF(F(0x3FC00000), F(0x40100000)) ? 1 : 0, 1);
        Check("-0f == 0f", EqF(F(0x80000000), F(0)) ? 1 : 0, 1);
        Check("nan f == nan f", EqF(F(0x7FC00000), F(0x7FC00000)) ? 1 : 0, 0);
        Group("comparisons");
    }

    static void TestConvert()
    {
        CheckD("(double)1", FromInt(1), 0x3FF00000, 0);
        CheckD("(double)-1", FromInt(-1), 0xBFF00000, 0);
        CheckD("(double)int.MinValue", FromInt(int.MinValue), 0xC1E00000, 0);
        CheckD("(double)int.MaxValue", FromInt(int.MaxValue), 0x41DFFFFF, 0xFFC00000);
        CheckD("(double)uint.MaxValue", FromUInt(uint.MaxValue), 0x41EFFFFF, 0xFFE00000);
        CheckD("(double)0x80000000u", FromUInt(0x80000000u), 0x41E00000, 0);
        CheckD("(double)2^32", FromLong(0x100000000L), 0x41F00000, 0);
        CheckD("(double)-1L", FromLong(-1L), 0xBFF00000, 0);
        CheckD("(double)long.MinValue", FromLong(long.MinValue), 0xC3E00000, 0);
        CheckD("(double)long.MaxValue", FromLong(long.MaxValue), 0x43E00000, 0);
        CheckD("(double)(2^53 + 1)", FromLong(0x20000000000001L), 0x43400000, 0);
        CheckD("(double)(2^53 + 3)", FromLong(0x20000000000003L), 0x43400000, 2);
        CheckD("(double)-(2^32 + 1)", FromLong(-0x100000001L), 0xC1F00000, 0x00100000);
        CheckD("(double)ulong.MaxValue", FromULong(ulong.MaxValue), 0x43F00000, 0);
        CheckD("(double)2^63 unsigned", FromULong(0x8000000000000000UL), 0x43E00000, 0);
        CheckD("(double)1.5f", FromFloat(F(0x3FC00000)), 0x3FF80000, 0);
        CheckD("(double)0.1f", FromFloat(F(0x3DCCCCCD)), 0x3FB99999, 0xA0000000);
        CheckF("(float)0.1", ToFloat(D(0x3FB99999, 0x9999999A)), 0x3DCCCCCD);
        CheckF("(float)1e40", ToFloat(D(0x483D6329, 0xF1C35CA5)), 0x7F800000);
        CheckF("(float)-1.5", ToFloat(D(0xBFF80000, 0)), 0xBFC00000);
        CheckF("(float)1", FromIntF(1), 0x3F800000);
        CheckF("(float)16777217", FromIntF(16777217), 0x4B800000);
        CheckF("(float)2^40", FromLongF(0x10000000000L), 0x53800000);

        Check("(int)3.99", ToInt(D(0x400FEB85, 0x1EB851EC)), 3);
        Check("(int)-3.99", ToInt(D(0xC00FEB85, 0x1EB851EC)), -3);
        Check("(int)2147483647.0", ToInt(D(0x41DFFFFF, 0xFFC00000)), int.MaxValue);
        Check("(int)-2147483648.0", ToInt(D(0xC1E00000, 0)), int.MinValue);
        Check("(uint)4294967295.0", (int)ToUInt(D(0x41EFFFFF, 0xFFE00000)), -1);
        Check("(uint)2147483648.0", (int)ToUInt(D(0x41E00000, 0)), int.MinValue);
        Check("(short)-1.5", ToShort(D(0xBFF80000, 0)), -1);
        Check("(byte)200.9", ToByte(D(0x40691CCC, 0xCCCCCCCD)), 200);
        CheckL("(long)3.99", ToLong(D(0x400FEB85, 0x1EB851EC)), 0, 3);
        CheckL("(long)-3.99", ToLong(D(0xC00FEB85, 0x1EB851EC)), 0xFFFFFFFF, 0xFFFFFFFD);
        CheckL("(long)1e18", ToLong(D(0x43ABC16D, 0x674EC800)), 0x0DE0B6B3, 0xA7640000);
        CheckL("(long)2^32", ToLong(D(0x41F00000, 0)), 1, 0);
        CheckL("(long)-(2^32 + 1)", ToLong(D(0xC1F00000, 0x00100000)), 0xFFFFFFFE, 0xFFFFFFFF);
        CheckL("(long)-2^63", ToLong(D(0xC3E00000, 0)), 0x80000000, 0);
        CheckL("(ulong)2^63", (long)ToULong(D(0x43E00000, 0)), 0x80000000, 0);
        CheckL("(ulong)(2^64 - 2048)", (long)ToULong(D(0x43EFFFFF, 0xFFFFFFFF)), 0xFFFFFFFF, 0xFFFFF800);
        CheckL("(ulong)1.5", (long)ToULong(D(0x3FF80000, 0)), 0, 1);
        Check("(int)1.5f", ToIntF(F(0x3FC00000)), 1);
        Check("(int)-2.5f", ToIntF(F(0xC0200000)), -2);
        CheckL("(long)2^40 f", ToLongF(F(0x53800000)), 0x100, 0);

        CheckL("bits of 1.5", Bits(D(0x3FF80000, 0)), 0x3FF80000, 0);
        CheckD("double of bits", FromBits(0x400E000000000000L), 0x400E0000, 0);
        Check("bits of 1.5f", BitsF(F(0x3FC00000)), 0x3FC00000);
        Group("conversions");
    }

    static void TestMoves()
    {
        double a = D(0x3FF80000, 0x11111111), b = D(0x40020000, 0x22222222);
        Swap(ref a, ref b);
        CheckD("swap a", a, 0x40020000, 0x22222222);
        CheckD("swap b", b, 0x3FF80000, 0x11111111);

        DBox box = new DBox { D = D(0x3FF80000, 0), E = D(0x40020000, 0), F = F(0x3FC00000), I = 5 };
        CheckD("fields sum", Field(box), 0x400E0000, 0);
        SetFields(box, D(0x40040000, 0x33333333), F(0x40100000));
        CheckD("field D", box.D, 0x40040000, 0x33333333);
        CheckD("field E", box.E, 0x3FF80000, 0);
        CheckF("field F", box.F, 0x40100000);
        Check("field I", box.I, 7);

        DPair p = MakePair(D(0x3FF80000, 1), D(0x40020000, 2));
        CheckD("pair X", p.X, 0x40020000, 2);
        CheckD("pair Y", p.Y, 0x3FF80000, 1);
        CheckD("pair difference", PairSum(MakePair(D(0x3FF80000, 0), D(0x40020000, 0))), 0x3FE80000, 0);
        Mixed m = MakeMixed(-3, D(0x3FF80000, 0x44444444), F(0x40100000), 0x1122334455667788L);
        Check("mixed I", m.I, -3);
        CheckD("mixed D", m.D, 0x3FF80000, 0x44444444);
        CheckF("mixed F", m.F, 0x40100000);
        CheckL("mixed L", m.L, 0x11223344, 0x55667788);

        double[] arr = new double[7];
        for (int i = 0; i < arr.Length; i++) arr[i] = D(0x3FD00000, 0);       // 0.25
        arr[3] = D(0x3FF80000, 0);
        CheckD("array sum", ArrSum(arr), 0x40080000, 0);                     // 6 * 0.25 + 1.5 = 3
        ArrScale(arr, D(0x40100000, 0));                                     // * 4
        CheckD("array scaled [0]", arr[0], 0x3FF00000, 0);
        CheckD("array scaled [3]", arr[3], 0x40180000, 0);
        CheckD("array scaled [6]", arr[6], 0x3FF00000, 0);

        s_d = D(0x3FF80000, 0x55555555);
        s_f = F(0x3FC00000);
        CheckD("static", s_d, 0x3FF80000, 0x55555555);
        CheckF("static float", s_f, 0x3FC00000);
        CheckD("Interlocked.Exchange", Interlocked.Exchange(ref s_d, D(0x40020000, 0x66666666)), 0x3FF80000, 0x55555555);
        CheckD("Interlocked.Exchange cell", s_d, 0x40020000, 0x66666666);
        CheckD("generic", Pass<double>(1, D(0x3FF80000, 0x77777777)), 0x3FF80000, 0x77777777);
        CheckF("generic float", Pass<float>(1, F(0x3FC00001)), 0x3FC00001);
        CheckD("box", Unbox(Box(D(0x3FF80000, 0x88888888))), 0x3FF80000, 0x88888888);
        // 1 - 2 + 4 - 8 + 16 - 32 = -21
        CheckD("six arguments", Many(D(0x3FF00000, 0), D(0x40000000, 0), D(0x40100000, 0), D(0x40200000, 0), D(0x40300000, 0), D(0x40400000, 0)), 0xC0350000, 0);
        // x = 3.75, y = 3.375, z = 0.375: (x + y) + (z + 1.5) = 7.125 + 1.875 = 9
        CheckD("across calls", AcrossCalls(D(0x3FF80000, 0), D(0x40020000, 0)), 0x40220000, 0);
        // (1.5 + 2.25) * 2.25 = 8.4375
        CheckD("handler, not thrown", InHandler(D(0x3FF80000, 0), D(0x40020000, 0), false), 0x4020E000, 0);
        CheckD("handler, not thrown: finally", s_d, 0x4020E000, 0);
        // 3.75 - 1 = 2.75
        CheckD("handler, thrown", InHandler(D(0x3FF80000, 0), D(0x40020000, 0), true), 0x40060000, 0);
        CheckD("handler, thrown: finally", s_d, 0x40060000, 0);

        string text = D(0x400E0000, 0).ToString();
        Check("ToString", text == "3.75" ? 1 : 0, 1);
        CheckD("Parse", double.Parse("3.75"), 0x400E0000, 0);
        CheckD("Parse 0.1", double.Parse("0.1"), 0x3FB99999, 0x9999999A);
        Group("moves");
    }

    static int Main()
    {
        TestArithmetic();
        TestCompare();
        TestConvert();
        TestMoves();

        if (fails == 0)
        {
            Console.WriteLine("ALL PASS");
            return 0;
        }
        Console.WriteLine("FAIL " + fails.ToString());
        return 1;
    }
}
