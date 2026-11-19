// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// 64-bit integer arithmetic and comparisons on a target whose registers hold
// 32 bits (riscv32).
//
// There a long is two int halves and every operation on it is rewritten into
// operations on the halves before register allocation. RISC-V has no carry
// or condition flags, so the rewrite computes them as values:
//
//   add:      lo = lo1 + lo2, hi = hi1 + hi2 + (lo <u lo2)
//   sub:      lo = lo1 - lo2, hi = hi1 - hi2 - (lo1 <u lo2)
//   neg:      lo = -lo, hi = -hi - (lo != 0)
//   mul:      lo = lo1 * lo2, hi = mulhu(lo1, lo2) + lo1 * hi2 + hi1 * lo2
//   eq, ne:   ((lo1 ^ lo2) | (hi1 ^ hi2)) against zero
//   lt, ge:   (hi1 < hi2) | ((hi2 < hi1) <u (lo1 <u lo2)); le and gt swap
//   select:   both halves select on one condition
//   checked:  the overflow bit is computed from the high halves
//
// with shortcuts when an operand is a constant: a constant below 2^32 leaves
// only the carry for the high half, x - c becomes x + (-c) when -c fits an
// add immediate, a signed test against zero reads the high half alone, an
// unsigned compare with a constant below 2^32 is hi == 0 && lo <u c.
// Division, remainder and checked multiplication are helper calls.
//
// What can go wrong: a carry or borrow taken from the wrong operand or after
// the operand was overwritten, a lost carry when the low halves are equal or
// zero, a half read from the wrong register, a signed compare where an
// unsigned one is needed (and the other way round) on either half, a shortcut
// applied to a constant it does not hold for (0x80000000, 0xFFFFFFFF, values
// on both sides of the 12-bit immediate range), an overflow check that
// misses a wrap in the low or in the high half.
//
// Every operation is a method that is not inlined and takes its operands as
// parameters, so nothing is folded. The operands are all pairs of values
// that sit on the limits of each half. The expected value comes from the
// class R, which does the same arithmetic on 16-bit limbs read from memory
// with int operations only, and results are compared half by half through
// memory, so neither the expected value nor the comparison uses the long
// operation under test. Division is checked by q * b + r == a and the range
// and sign of r, in R's arithmetic.
//
// Each group prints the number of checks; a failed check prints one line
// with the operands, the expected and the actual value. The run ends with
// ALL PASS or a FAIL count, and the guest returns 1 on failure.
using System;
using System.Runtime.CompilerServices;

// Reference 64-bit arithmetic on 16-bit limbs, int operations only.
unsafe static class R
{
    const MethodImplOptions NI = MethodImplOptions.NoInlining;

    [MethodImpl(NI)] public static uint L(long x, int i) => ((ushort*)&x)[i];
    [MethodImpl(NI)] public static uint Lo(long x) => ((uint*)&x)[0];
    [MethodImpl(NI)] public static uint Hi(long x) => ((uint*)&x)[1];

    [MethodImpl(NI)]
    public static long Make(uint l0, uint l1, uint l2, uint l3)
    {
        long r = 0;
        ushort* p = (ushort*)&r;
        p[0] = (ushort)l0; p[1] = (ushort)l1; p[2] = (ushort)l2; p[3] = (ushort)l3;
        return r;
    }

    [MethodImpl(NI)] public static bool Same(long a, long b) => Lo(a) == Lo(b) && Hi(a) == Hi(b);

    public static string Hex(long x) => "0x" + Hi(x).ToString("x8") + Lo(x).ToString("x8");

    [MethodImpl(NI)]
    public static long AddC(long a, long b, uint cin, out uint cout)
    {
        uint* r = stackalloc uint[4];
        uint c = cin;
        for (int i = 0; i < 4; i++)
        {
            uint s = L(a, i) + L(b, i) + c;
            r[i] = s & 0xFFFF;
            c = s >> 16;
        }
        cout = c;
        return Make(r[0], r[1], r[2], r[3]);
    }

    public static long Add(long a, long b) => AddC(a, b, 0, out _);
    public static long Not(long a) => Make(L(a, 0) ^ 0xFFFF, L(a, 1) ^ 0xFFFF, L(a, 2) ^ 0xFFFF, L(a, 3) ^ 0xFFFF);
    public static long Sub(long a, long b) => AddC(a, Not(b), 1, out _);
    public static long Neg(long a) => AddC(Make(0, 0, 0, 0), Not(a), 1, out _);
    public static long And(long a, long b) => Make(L(a, 0) & L(b, 0), L(a, 1) & L(b, 1), L(a, 2) & L(b, 2), L(a, 3) & L(b, 3));
    public static long Or(long a, long b) => Make(L(a, 0) | L(b, 0), L(a, 1) | L(b, 1), L(a, 2) | L(b, 2), L(a, 3) | L(b, 3));
    public static long Xor(long a, long b) => Make(L(a, 0) ^ L(b, 0), L(a, 1) ^ L(b, 1), L(a, 2) ^ L(b, 2), L(a, 3) ^ L(b, 3));

    public static bool IsNeg(long a) => (L(a, 3) & 0x8000) != 0;
    public static bool IsZero(long a) => (L(a, 0) | L(a, 1) | L(a, 2) | L(a, 3)) == 0;
    public static long Abs(long a) => IsNeg(a) ? Neg(a) : a;

    // -1, 0 or 1, unsigned.
    [MethodImpl(NI)]
    public static int CmpU(long a, long b)
    {
        for (int i = 3; i >= 0; i--)
        {
            uint x = L(a, i), y = L(b, i);
            if (x != y) return x < y ? -1 : 1;
        }
        return 0;
    }

    // -1, 0 or 1, signed.
    public static int CmpS(long a, long b)
    {
        bool na = IsNeg(a), nb = IsNeg(b);
        if (na != nb) return na ? -1 : 1;
        return CmpU(a, b);
    }

    // The 128-bit unsigned product, 8 limbs.
    [MethodImpl(NI)]
    public static void Mul128(long a, long b, uint* p)
    {
        uint* col = stackalloc uint[9];
        for (int i = 0; i < 9; i++) col[i] = 0;
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                uint m = L(a, i) * L(b, j);
                col[i + j] += m & 0xFFFF;
                col[i + j + 1] += m >> 16;
            }
        }
        uint c = 0;
        for (int k = 0; k < 8; k++)
        {
            uint s = col[k] + c;
            p[k] = s & 0xFFFF;
            c = s >> 16;
        }
    }

    public static long Mul(long a, long b)
    {
        uint* p = stackalloc uint[8];
        Mul128(a, b, p);
        return Make(p[0], p[1], p[2], p[3]);
    }

    // Does the unsigned product need more than 64 bits?
    public static bool MulOvfU(long a, long b)
    {
        uint* p = stackalloc uint[8];
        Mul128(a, b, p);
        return (p[4] | p[5] | p[6] | p[7]) != 0;
    }

    // Does the signed product need more than 64 bits?
    public static bool MulOvfS(long a, long b)
    {
        uint* p = stackalloc uint[8];
        Mul128(Abs(a), Abs(b), p);
        if ((p[4] | p[5] | p[6] | p[7]) != 0) return true;
        bool neg = IsNeg(a) != IsNeg(b);
        if ((p[3] & 0x8000) == 0) return false;
        // The magnitude has bit 63 set: only -2^63 fits.
        return !(neg && p[3] == 0x8000 && (p[0] | p[1] | p[2]) == 0);
    }
}

unsafe class Program
{
    const MethodImplOptions NI = MethodImplOptions.NoInlining;

    static int fails;
    static int checks;

    static void Fail(string name, long a, long b, string exp, string got)
    {
        fails++;
        if (fails <= 60)
            Console.WriteLine("FAIL " + name + " a=" + R.Hex(a) + " b=" + R.Hex(b) + " expected " + exp + " got " + got);
    }

    static void Check(string name, long a, long b, long got, long exp)
    {
        checks++;
        if (!R.Same(got, exp)) Fail(name, a, b, R.Hex(exp), R.Hex(got));
    }

    static void CheckB(string name, long a, long b, bool got, bool exp)
    {
        checks++;
        if (got != exp) Fail(name, a, b, exp ? "true" : "false", got ? "true" : "false");
    }

    static void CheckI(string name, long a, long b, int got, int exp)
    {
        checks++;
        if (got != exp) Fail(name, a, b, exp.ToString(), got.ToString());
    }

    static void Group(string name)
    {
        Console.WriteLine("ok   " + name + " " + checks.ToString());
    }

    static readonly long[] V =
    {
        0L, 1L, 2L, -1L, -2L,
        0x7FFL, 0x800L, 0x801L, -0x800L, -0x801L,
        0x7FFFFFFFL, 0x80000000L, 0x80000001L, 0xFFFFFFFFL, 0x100000000L, 0x100000001L,
        -0x80000000L, -0x80000001L, -0x100000000L, -0x100000001L,
        unchecked((long)0xFFFFFFFF00000001UL), 0x7FFFFFFF80000000L, unchecked((long)0x800000007FFFFFFFUL),
        0x7FFFFFFFFFFFFFFFL, unchecked((long)0x8000000000000000UL), unchecked((long)0x8000000000000001UL),
        0x0123456789ABCDEFL, unchecked((long)0xFEDCBA9876543210UL), 0x00000001FFFFFFFFL, 0x0000FFFF0000FFFFL,
        0x00010000_00010000L, 0x3FFFFFFFC0000000L,
    };

    // ---- the operations under test ----

    [MethodImpl(NI)] static long Add(long a, long b) => a + b;
    [MethodImpl(NI)] static long Sub(long a, long b) => a - b;
    [MethodImpl(NI)] static long Mul(long a, long b) => a * b;
    [MethodImpl(NI)] static long And(long a, long b) => a & b;
    [MethodImpl(NI)] static long Or(long a, long b) => a | b;
    [MethodImpl(NI)] static long Xor(long a, long b) => a ^ b;
    [MethodImpl(NI)] static long AndNot(long a, long b) => a & ~b;
    [MethodImpl(NI)] static long Neg(long a) => -a;
    [MethodImpl(NI)] static long Not(long a) => ~a;
    [MethodImpl(NI)] static ulong AddU(ulong a, ulong b) => a + b;
    [MethodImpl(NI)] static ulong SubU(ulong a, ulong b) => a - b;
    [MethodImpl(NI)] static ulong MulU(ulong a, ulong b) => a * b;

    // The result reuses an operand register pair.
    [MethodImpl(NI)] static long AddSelf(long a) => a + a;
    [MethodImpl(NI)] static long SubSelf(long a) => a - a;
    [MethodImpl(NI)] static long MulSelf(long a) => a * a;
    [MethodImpl(NI)] static long AddSwap(long a, long b) => b + a;
    [MethodImpl(NI)] static long SubSwap(long a, long b) => b - a;

    // Chains: the carry of one operation must not be taken from the next.
    [MethodImpl(NI)] static long Chain1(long a, long b) => (a + b) - (b - a) + a;
    [MethodImpl(NI)] static long Chain2(long a, long b) => (a - b) * (a + b) - a * a + b * b;
    [MethodImpl(NI)] static long Chain3(long a, long b) => -(a + b) + ~(a - b) + (a ^ b);

    [MethodImpl(NI)] static bool Eq(long a, long b) => a == b;
    [MethodImpl(NI)] static bool Ne(long a, long b) => a != b;
    [MethodImpl(NI)] static bool Lt(long a, long b) => a < b;
    [MethodImpl(NI)] static bool Le(long a, long b) => a <= b;
    [MethodImpl(NI)] static bool Gt(long a, long b) => a > b;
    [MethodImpl(NI)] static bool Ge(long a, long b) => a >= b;
    [MethodImpl(NI)] static bool LtU(ulong a, ulong b) => a < b;
    [MethodImpl(NI)] static bool LeU(ulong a, ulong b) => a <= b;
    [MethodImpl(NI)] static bool GtU(ulong a, ulong b) => a > b;
    [MethodImpl(NI)] static bool GeU(ulong a, ulong b) => a >= b;

    // The same compares as branches.
    [MethodImpl(NI)] static int BrEq(long a, long b) { if (a == b) return 11; return 22; }
    [MethodImpl(NI)] static int BrNe(long a, long b) { if (a != b) return 11; return 22; }
    [MethodImpl(NI)] static int BrLt(long a, long b) { if (a < b) return 11; return 22; }
    [MethodImpl(NI)] static int BrLe(long a, long b) { if (a <= b) return 11; return 22; }
    [MethodImpl(NI)] static int BrGt(long a, long b) { if (a > b) return 11; return 22; }
    [MethodImpl(NI)] static int BrGe(long a, long b) { if (a >= b) return 11; return 22; }
    [MethodImpl(NI)] static int BrLtU(ulong a, ulong b) { if (a < b) return 11; return 22; }
    [MethodImpl(NI)] static int BrLeU(ulong a, ulong b) { if (a <= b) return 11; return 22; }
    [MethodImpl(NI)] static int BrGtU(ulong a, ulong b) { if (a > b) return 11; return 22; }
    [MethodImpl(NI)] static int BrGeU(ulong a, ulong b) { if (a >= b) return 11; return 22; }

    // Selects of longs, on an int and on a long condition.
    [MethodImpl(NI)] static long Sel(int c, long a, long b) => c != 0 ? a : b;
    [MethodImpl(NI)] static long Min(long a, long b) => a < b ? a : b;
    [MethodImpl(NI)] static long Max(long a, long b) => a > b ? a : b;
    [MethodImpl(NI)] static ulong MinU(ulong a, ulong b) => a < b ? a : b;
    [MethodImpl(NI)] static ulong MaxU(ulong a, ulong b) => a > b ? a : b;
    [MethodImpl(NI)] static long AbsSel(long a) => a < 0 ? -a : a;
    [MethodImpl(NI)] static long SelConst(long a) => a == 0 ? 0x100000000L : -1L;
    [MethodImpl(NI)] static long SelAdd(int c, long a, long b) { long r = a; if (c != 0) r = a + b; return r; }

    // Compares with zero and with constants.
    [MethodImpl(NI)] static bool Lt0(long a) => a < 0;
    [MethodImpl(NI)] static bool Ge0(long a) => a >= 0;
    [MethodImpl(NI)] static bool Gt0(long a) => a > 0;
    [MethodImpl(NI)] static bool Le0(long a) => a <= 0;
    [MethodImpl(NI)] static bool Eq0(long a) => a == 0;
    [MethodImpl(NI)] static bool Ne0(long a) => a != 0;
    [MethodImpl(NI)] static bool ZeroLt(long a) => 0 < a;
    [MethodImpl(NI)] static bool ZeroGe(long a) => 0 >= a;
    [MethodImpl(NI)] static int BrLt0(long a) { if (a < 0) return 11; return 22; }
    [MethodImpl(NI)] static int BrGe0(long a) { if (a >= 0) return 11; return 22; }
    [MethodImpl(NI)] static bool LtC(long a) => a < 0x1234L;
    [MethodImpl(NI)] static bool GeC(long a) => a >= 0x1234L;
    [MethodImpl(NI)] static bool GtC(long a) => a > 0x1234L;
    [MethodImpl(NI)] static bool LeC(long a) => a <= 0x1234L;
    [MethodImpl(NI)] static bool LtNegC(long a) => a < -5L;
    [MethodImpl(NI)] static bool LtBig(long a) => a < 0x100000000L;
    [MethodImpl(NI)] static bool GeBig(long a) => a >= 0x7FFFFFFF80000000L;
    [MethodImpl(NI)] static bool LtUC(ulong a) => a < 0x1234UL;
    [MethodImpl(NI)] static bool GeUC(ulong a) => a >= 0x1234UL;
    [MethodImpl(NI)] static bool GtUC(ulong a) => a > 0x1234UL;
    [MethodImpl(NI)] static bool LeUC(ulong a) => a <= 0x1234UL;
    [MethodImpl(NI)] static bool LtU1(ulong a) => a < 1UL;
    [MethodImpl(NI)] static bool LtUMax32(ulong a) => a < 0xFFFFFFFFUL;
    [MethodImpl(NI)] static bool LeUMax32(ulong a) => a <= 0xFFFFFFFFUL;
    [MethodImpl(NI)] static bool LtU80(ulong a) => a < 0x80000000UL;
    [MethodImpl(NI)] static bool LtUBig(ulong a) => a < 0x100000000UL;
    [MethodImpl(NI)] static bool GeUBig(ulong a) => a >= 0x8000000000000000UL;
    [MethodImpl(NI)] static int BrLtUC(ulong a) { if (a < 0x1234UL) return 11; return 22; }
    [MethodImpl(NI)] static bool EqC(long a) => a == 0x100000000L;
    [MethodImpl(NI)] static bool EqC2(long a) => a == 0xFFFFFFFFL;
    [MethodImpl(NI)] static bool EqM1(long a) => a == -1L;

    // Arithmetic with constants on both sides of each shortcut.
    [MethodImpl(NI)] static long Add1(long a) => a + 1;
    [MethodImpl(NI)] static long Add7FF(long a) => a + 0x7FF;
    [MethodImpl(NI)] static long Add800(long a) => a + 0x800;
    [MethodImpl(NI)] static long AddM1(long a) => a + (-1L);
    [MethodImpl(NI)] static long Add7FFFFFFF(long a) => a + 0x7FFFFFFFL;
    [MethodImpl(NI)] static long Add80000000(long a) => a + 0x80000000L;
    [MethodImpl(NI)] static long AddFFFFFFFF(long a) => a + 0xFFFFFFFFL;
    [MethodImpl(NI)] static long Add100000000(long a) => a + 0x100000000L;
    [MethodImpl(NI)] static long AddBig(long a) => a + 0x0123456789ABCDEFL;
    [MethodImpl(NI)] static long AddHiOnly(long a) => a + 0x7FFFFFFF00000000L;
    [MethodImpl(NI)] static long Sub1(long a) => a - 1;
    [MethodImpl(NI)] static long Sub7FF(long a) => a - 0x7FF;
    [MethodImpl(NI)] static long Sub800(long a) => a - 0x800;
    [MethodImpl(NI)] static long Sub801(long a) => a - 0x801;
    [MethodImpl(NI)] static long SubM800(long a) => a - (-0x800L);
    [MethodImpl(NI)] static long SubM801(long a) => a - (-0x801L);
    [MethodImpl(NI)] static long Sub7FFFFFFF(long a) => a - 0x7FFFFFFFL;
    [MethodImpl(NI)] static long Sub80000000(long a) => a - 0x80000000L;
    [MethodImpl(NI)] static long SubFFFFFFFF(long a) => a - 0xFFFFFFFFL;
    [MethodImpl(NI)] static long Sub100000000(long a) => a - 0x100000000L;
    [MethodImpl(NI)] static long Sub180000000(long a) => a - 0x180000000L;
    [MethodImpl(NI)] static long SubBig(long a) => a - 0x0123456789ABCDEFL;
    [MethodImpl(NI)] static long RSub0(long a) => 0 - a;
    [MethodImpl(NI)] static long RSub5(long a) => 5 - a;
    [MethodImpl(NI)] static long RSubBig(long a) => 0x100000000L - a;
    [MethodImpl(NI)] static ulong SubU1(ulong a) => a - 1;
    [MethodImpl(NI)] static ulong SubU800(ulong a) => a - 0x800;
    [MethodImpl(NI)] static long Mul3(long a) => a * 3;
    [MethodImpl(NI)] static long Mul10(long a) => a * 10;
    [MethodImpl(NI)] static long MulM1(long a) => a * -1;
    [MethodImpl(NI)] static long MulBig(long a) => a * 0x100000001B3L;
    [MethodImpl(NI)] static long MulHiOnly(long a) => a * 0x100000000L;
    [MethodImpl(NI)] static long AndLo(long a) => a & 0xFFFFFFFFL;
    [MethodImpl(NI)] static long AndHi(long a) => a & unchecked((long)0xFFFFFFFF00000000UL);
    [MethodImpl(NI)] static long XorC(long a) => a ^ 0x5555555555555555L;

    // Division and remainder: helper calls on riscv32.
    [MethodImpl(NI)] static long Div(long a, long b) => a / b;
    [MethodImpl(NI)] static long Rem(long a, long b) => a % b;
    [MethodImpl(NI)] static ulong DivU(ulong a, ulong b) => a / b;
    [MethodImpl(NI)] static ulong RemU(ulong a, ulong b) => a % b;
    [MethodImpl(NI)] static long Div10(long a) => a / 10;
    [MethodImpl(NI)] static long Rem10(long a) => a % 10;
    [MethodImpl(NI)] static ulong DivU10(ulong a) => a / 10;
    [MethodImpl(NI)] static ulong RemU10(ulong a) => a % 10;
    [MethodImpl(NI)] static long Div8(long a) => a / 8;
    [MethodImpl(NI)] static long Rem8(long a) => a % 8;
    [MethodImpl(NI)] static ulong DivU16(ulong a) => a / 16;
    [MethodImpl(NI)] static ulong RemU16(ulong a) => a % 16;
    [MethodImpl(NI)] static long DivBig(long a) => a / 0x100000000L;
    [MethodImpl(NI)] static long RemBig(long a) => a % 0x100000000L;

    // Checked arithmetic.
    [MethodImpl(NI)] static long AddOvf(long a, long b) => checked(a + b);
    [MethodImpl(NI)] static long SubOvf(long a, long b) => checked(a - b);
    [MethodImpl(NI)] static long MulOvf(long a, long b) => checked(a * b);
    [MethodImpl(NI)] static ulong AddOvfU(ulong a, ulong b) => checked(a + b);
    [MethodImpl(NI)] static ulong SubOvfU(ulong a, ulong b) => checked(a - b);
    [MethodImpl(NI)] static ulong MulOvfU(ulong a, ulong b) => checked(a * b);
    [MethodImpl(NI)] static long AddOvf1(long a) => checked(a + 1);
    [MethodImpl(NI)] static long SubOvf1(long a) => checked(a - 1);
    [MethodImpl(NI)] static ulong AddOvfU1(ulong a) => checked(a + 1);
    [MethodImpl(NI)] static ulong SubOvfU1(ulong a) => checked(a - 1);
    [MethodImpl(NI)] static long NegOvf(long a) => checked(-a);

    delegate long Op2(long a, long b);
    delegate ulong Op2U(ulong a, ulong b);
    delegate long Op1(long a);

    // 0 = no exception, 1 = OverflowException, 2 = DivideByZeroException, 3 = any other.
    static int Run2(Op2 op, long a, long b, out long r)
    {
        r = 0;
        try { r = op(a, b); return 0; }
        catch (OverflowException) { return 1; }
        catch (DivideByZeroException) { return 2; }
        catch (Exception) { return 3; }
    }

    static int Run2U(Op2U op, long a, long b, out long r)
    {
        r = 0;
        try { r = (long)op((ulong)a, (ulong)b); return 0; }
        catch (OverflowException) { return 1; }
        catch (DivideByZeroException) { return 2; }
        catch (Exception) { return 3; }
    }

    static void CheckOvf(string name, long a, long b, int kind, long got, bool expOvf, long exp)
    {
        checks++;
        if (kind != (expOvf ? 1 : 0))
        {
            Fail(name, a, b, expOvf ? "OverflowException" : "no exception", "kind " + kind.ToString());
            return;
        }
        if (!expOvf && !R.Same(got, exp)) Fail(name, a, b, R.Hex(exp), R.Hex(got));
    }

    static void TestBinary()
    {
        foreach (long a in V)
        {
            foreach (long b in V)
            {
                Check("add", a, b, Add(a, b), R.Add(a, b));
                Check("sub", a, b, Sub(a, b), R.Sub(a, b));
                Check("mul", a, b, Mul(a, b), R.Mul(a, b));
                Check("and", a, b, And(a, b), R.And(a, b));
                Check("or", a, b, Or(a, b), R.Or(a, b));
                Check("xor", a, b, Xor(a, b), R.Xor(a, b));
                Check("andnot", a, b, AndNot(a, b), R.And(a, R.Not(b)));
                Check("addu", a, b, (long)AddU((ulong)a, (ulong)b), R.Add(a, b));
                Check("subu", a, b, (long)SubU((ulong)a, (ulong)b), R.Sub(a, b));
                Check("mulu", a, b, (long)MulU((ulong)a, (ulong)b), R.Mul(a, b));
                Check("addswap", a, b, AddSwap(a, b), R.Add(b, a));
                Check("subswap", a, b, SubSwap(a, b), R.Sub(b, a));
                Check("chain1", a, b, Chain1(a, b), R.Add(R.Sub(R.Add(a, b), R.Sub(b, a)), a));
                Check("chain2", a, b, Chain2(a, b),
                      R.Add(R.Sub(R.Mul(R.Sub(a, b), R.Add(a, b)), R.Mul(a, a)), R.Mul(b, b)));
                Check("chain3", a, b, Chain3(a, b),
                      R.Add(R.Add(R.Neg(R.Add(a, b)), R.Not(R.Sub(a, b))), R.Xor(a, b)));
            }
        }
        Group("binary");
    }

    static void TestCompare()
    {
        foreach (long a in V)
        {
            foreach (long b in V)
            {
                int s = R.CmpS(a, b);
                int u = R.CmpU(a, b);
                CheckB("eq", a, b, Eq(a, b), u == 0);
                CheckB("ne", a, b, Ne(a, b), u != 0);
                CheckB("lt", a, b, Lt(a, b), s < 0);
                CheckB("le", a, b, Le(a, b), s <= 0);
                CheckB("gt", a, b, Gt(a, b), s > 0);
                CheckB("ge", a, b, Ge(a, b), s >= 0);
                CheckB("ltu", a, b, LtU((ulong)a, (ulong)b), u < 0);
                CheckB("leu", a, b, LeU((ulong)a, (ulong)b), u <= 0);
                CheckB("gtu", a, b, GtU((ulong)a, (ulong)b), u > 0);
                CheckB("geu", a, b, GeU((ulong)a, (ulong)b), u >= 0);
                CheckI("breq", a, b, BrEq(a, b), u == 0 ? 11 : 22);
                CheckI("brne", a, b, BrNe(a, b), u != 0 ? 11 : 22);
                CheckI("brlt", a, b, BrLt(a, b), s < 0 ? 11 : 22);
                CheckI("brle", a, b, BrLe(a, b), s <= 0 ? 11 : 22);
                CheckI("brgt", a, b, BrGt(a, b), s > 0 ? 11 : 22);
                CheckI("brge", a, b, BrGe(a, b), s >= 0 ? 11 : 22);
                CheckI("brltu", a, b, BrLtU((ulong)a, (ulong)b), u < 0 ? 11 : 22);
                CheckI("brleu", a, b, BrLeU((ulong)a, (ulong)b), u <= 0 ? 11 : 22);
                CheckI("brgtu", a, b, BrGtU((ulong)a, (ulong)b), u > 0 ? 11 : 22);
                CheckI("brgeu", a, b, BrGeU((ulong)a, (ulong)b), u >= 0 ? 11 : 22);
                Check("sel1", a, b, Sel(1, a, b), a);
                Check("sel0", a, b, Sel(0, a, b), b);
                Check("selm", a, b, Sel(-0x80000000, a, b), a);
                Check("min", a, b, Min(a, b), s < 0 ? a : b);
                Check("max", a, b, Max(a, b), s > 0 ? a : b);
                Check("minu", a, b, (long)MinU((ulong)a, (ulong)b), u < 0 ? a : b);
                Check("maxu", a, b, (long)MaxU((ulong)a, (ulong)b), u > 0 ? a : b);
                Check("seladd1", a, b, SelAdd(1, a, b), R.Add(a, b));
                Check("seladd0", a, b, SelAdd(0, a, b), a);
            }
        }
        Group("compare");
    }

    static long C(uint hi, uint lo) => R.Make(lo & 0xFFFF, lo >> 16, hi & 0xFFFF, hi >> 16);

    static void TestUnary()
    {
        long zero = C(0, 0);
        foreach (long a in V)
        {
            Check("neg", a, 0, Neg(a), R.Neg(a));
            Check("not", a, 0, Not(a), R.Not(a));
            Check("addself", a, 0, AddSelf(a), R.Add(a, a));
            Check("subself", a, 0, SubSelf(a), zero);
            Check("mulself", a, 0, MulSelf(a), R.Mul(a, a));
            Check("abssel", a, 0, AbsSel(a), R.Abs(a));
            Check("selconst", a, 0, SelConst(a), R.IsZero(a) ? C(1, 0) : C(0xFFFFFFFF, 0xFFFFFFFF));

            int s0 = R.CmpS(a, zero);
            CheckB("lt0", a, 0, Lt0(a), s0 < 0);
            CheckB("ge0", a, 0, Ge0(a), s0 >= 0);
            CheckB("gt0", a, 0, Gt0(a), s0 > 0);
            CheckB("le0", a, 0, Le0(a), s0 <= 0);
            CheckB("eq0", a, 0, Eq0(a), s0 == 0);
            CheckB("ne0", a, 0, Ne0(a), s0 != 0);
            CheckB("zerolt", a, 0, ZeroLt(a), s0 > 0);
            CheckB("zeroge", a, 0, ZeroGe(a), s0 <= 0);
            CheckI("brlt0", a, 0, BrLt0(a), s0 < 0 ? 11 : 22);
            CheckI("brge0", a, 0, BrGe0(a), s0 >= 0 ? 11 : 22);

            long c1234 = C(0, 0x1234);
            CheckB("ltc", a, c1234, LtC(a), R.CmpS(a, c1234) < 0);
            CheckB("gec", a, c1234, GeC(a), R.CmpS(a, c1234) >= 0);
            CheckB("gtc", a, c1234, GtC(a), R.CmpS(a, c1234) > 0);
            CheckB("lec", a, c1234, LeC(a), R.CmpS(a, c1234) <= 0);
            CheckB("ltnegc", a, -5, LtNegC(a), R.CmpS(a, C(0xFFFFFFFF, 0xFFFFFFFB)) < 0);
            CheckB("ltbig", a, 0, LtBig(a), R.CmpS(a, C(1, 0)) < 0);
            CheckB("gebig", a, 0, GeBig(a), R.CmpS(a, C(0x7FFFFFFF, 0x80000000)) >= 0);
            CheckB("ltuc", a, c1234, LtUC((ulong)a), R.CmpU(a, c1234) < 0);
            CheckB("geuc", a, c1234, GeUC((ulong)a), R.CmpU(a, c1234) >= 0);
            CheckB("gtuc", a, c1234, GtUC((ulong)a), R.CmpU(a, c1234) > 0);
            CheckB("leuc", a, c1234, LeUC((ulong)a), R.CmpU(a, c1234) <= 0);
            CheckB("ltu1", a, 1, LtU1((ulong)a), R.IsZero(a));
            CheckB("ltumax32", a, 0, LtUMax32((ulong)a), R.CmpU(a, C(0, 0xFFFFFFFF)) < 0);
            CheckB("leumax32", a, 0, LeUMax32((ulong)a), R.CmpU(a, C(0, 0xFFFFFFFF)) <= 0);
            CheckB("ltu80", a, 0, LtU80((ulong)a), R.CmpU(a, C(0, 0x80000000)) < 0);
            CheckB("ltubig", a, 0, LtUBig((ulong)a), R.CmpU(a, C(1, 0)) < 0);
            CheckB("geubig", a, 0, GeUBig((ulong)a), R.CmpU(a, C(0x80000000, 0)) >= 0);
            CheckI("brltuc", a, c1234, BrLtUC((ulong)a), R.CmpU(a, c1234) < 0 ? 11 : 22);
            CheckB("eqc", a, 0, EqC(a), R.CmpU(a, C(1, 0)) == 0);
            CheckB("eqc2", a, 0, EqC2(a), R.CmpU(a, C(0, 0xFFFFFFFF)) == 0);
            CheckB("eqm1", a, 0, EqM1(a), R.CmpU(a, C(0xFFFFFFFF, 0xFFFFFFFF)) == 0);

            Check("add1", a, 0, Add1(a), R.Add(a, C(0, 1)));
            Check("add7ff", a, 0, Add7FF(a), R.Add(a, C(0, 0x7FF)));
            Check("add800", a, 0, Add800(a), R.Add(a, C(0, 0x800)));
            Check("addm1", a, 0, AddM1(a), R.Add(a, C(0xFFFFFFFF, 0xFFFFFFFF)));
            Check("add7fffffff", a, 0, Add7FFFFFFF(a), R.Add(a, C(0, 0x7FFFFFFF)));
            Check("add80000000", a, 0, Add80000000(a), R.Add(a, C(0, 0x80000000)));
            Check("addffffffff", a, 0, AddFFFFFFFF(a), R.Add(a, C(0, 0xFFFFFFFF)));
            Check("add100000000", a, 0, Add100000000(a), R.Add(a, C(1, 0)));
            Check("addbig", a, 0, AddBig(a), R.Add(a, C(0x01234567, 0x89ABCDEF)));
            Check("addhionly", a, 0, AddHiOnly(a), R.Add(a, C(0x7FFFFFFF, 0)));
            Check("sub1", a, 0, Sub1(a), R.Sub(a, C(0, 1)));
            Check("sub7ff", a, 0, Sub7FF(a), R.Sub(a, C(0, 0x7FF)));
            Check("sub800", a, 0, Sub800(a), R.Sub(a, C(0, 0x800)));
            Check("sub801", a, 0, Sub801(a), R.Sub(a, C(0, 0x801)));
            Check("subm800", a, 0, SubM800(a), R.Add(a, C(0, 0x800)));
            Check("subm801", a, 0, SubM801(a), R.Add(a, C(0, 0x801)));
            Check("sub7fffffff", a, 0, Sub7FFFFFFF(a), R.Sub(a, C(0, 0x7FFFFFFF)));
            Check("sub80000000", a, 0, Sub80000000(a), R.Sub(a, C(0, 0x80000000)));
            Check("subffffffff", a, 0, SubFFFFFFFF(a), R.Sub(a, C(0, 0xFFFFFFFF)));
            Check("sub100000000", a, 0, Sub100000000(a), R.Sub(a, C(1, 0)));
            Check("sub180000000", a, 0, Sub180000000(a), R.Sub(a, C(1, 0x80000000)));
            Check("subbig", a, 0, SubBig(a), R.Sub(a, C(0x01234567, 0x89ABCDEF)));
            Check("rsub0", a, 0, RSub0(a), R.Neg(a));
            Check("rsub5", a, 0, RSub5(a), R.Sub(C(0, 5), a));
            Check("rsubbig", a, 0, RSubBig(a), R.Sub(C(1, 0), a));
            Check("subu1", a, 0, (long)SubU1((ulong)a), R.Sub(a, C(0, 1)));
            Check("subu800", a, 0, (long)SubU800((ulong)a), R.Sub(a, C(0, 0x800)));
            Check("mul3", a, 0, Mul3(a), R.Mul(a, C(0, 3)));
            Check("mul10", a, 0, Mul10(a), R.Mul(a, C(0, 10)));
            Check("mulm1", a, 0, MulM1(a), R.Neg(a));
            Check("mulbig", a, 0, MulBig(a), R.Mul(a, C(0x100, 0x000001B3)));
            Check("mulhionly", a, 0, MulHiOnly(a), R.Mul(a, C(1, 0)));
            Check("andlo", a, 0, AndLo(a), R.And(a, C(0, 0xFFFFFFFF)));
            Check("andhi", a, 0, AndHi(a), R.And(a, C(0xFFFFFFFF, 0)));
            Check("xorc", a, 0, XorC(a), R.Xor(a, C(0x55555555, 0x55555555)));
        }
        Group("unary and constants");
    }

    // q and r are the quotient and remainder of a by b iff q * b + r == a, |r| < |b| and
    // r is zero or has the sign of a (unsigned: r < b).
    static void CheckDivS(string name, long a, long b, long q, long r)
    {
        checks++;
        bool ok = R.Same(R.Add(R.Mul(q, b), r), a) && R.CmpU(R.Abs(r), R.Abs(b)) < 0 &&
                  (R.IsZero(r) || R.IsNeg(r) == R.IsNeg(a));
        if (!ok) Fail(name, a, b, "q*b+r==a", "q=" + R.Hex(q) + " r=" + R.Hex(r));
    }

    static void CheckDivU(string name, long a, long b, long q, long r)
    {
        checks++;
        bool ok = !R.MulOvfU(q, b) && R.Same(R.Add(R.Mul(q, b), r), a) && R.CmpU(r, b) < 0;
        if (!ok) Fail(name, a, b, "q*b+r==a", "q=" + R.Hex(q) + " r=" + R.Hex(r));
    }

    static void TestDivide()
    {
        long min = C(0x80000000, 0);
        long m1 = C(0xFFFFFFFF, 0xFFFFFFFF);
        foreach (long a in V)
        {
            foreach (long b in V)
            {
                long q, r;
                if (R.IsZero(b))
                {
                    CheckI("div by zero", a, b, Run2(Div, a, b, out q), 2);
                    CheckI("rem by zero", a, b, Run2(Rem, a, b, out q), 2);
                    CheckI("divu by zero", a, b, Run2U(DivU, a, b, out q), 2);
                    CheckI("remu by zero", a, b, Run2U(RemU, a, b, out q), 2);
                    continue;
                }
                CheckDivU("divu/remu", a, b, (long)DivU((ulong)a, (ulong)b), (long)RemU((ulong)a, (ulong)b));
                if (R.Same(a, min) && R.Same(b, m1))
                {
                    CheckI("min / -1", a, b, Run2(Div, a, b, out q), 1);
                    // The remainder is 0 in arithmetic; .NET throws as it does for the quotient.
                    int kind = Run2(Rem, a, b, out r);
                    checks++;
                    if (!(kind == 1 || (kind == 0 && R.IsZero(r))))
                        Fail("min % -1", a, b, "OverflowException or 0", "kind " + kind.ToString() + " " + R.Hex(r));
                    continue;
                }
                CheckDivS("div/rem", a, b, Div(a, b), Rem(a, b));
            }
            long c10 = C(0, 10);
            CheckDivS("div10/rem10", a, c10, Div10(a), Rem10(a));
            CheckDivU("divu10/remu10", a, c10, (long)DivU10((ulong)a), (long)RemU10((ulong)a));
            CheckDivS("div8/rem8", a, C(0, 8), Div8(a), Rem8(a));
            CheckDivU("divu16/remu16", a, C(0, 16), (long)DivU16((ulong)a), (long)RemU16((ulong)a));
            CheckDivS("divbig/rembig", a, C(1, 0), DivBig(a), RemBig(a));
        }
        Group("divide");
    }

    static void TestChecked()
    {
        long one = C(0, 1);
        foreach (long a in V)
        {
            foreach (long b in V)
            {
                long r;
                int kind;
                long sum = R.AddC(a, b, 0, out uint carry);
                long diff = R.Sub(a, b);

                kind = Run2(AddOvf, a, b, out r);
                CheckOvf("add.ovf", a, b, kind, r, R.IsNeg(a) == R.IsNeg(b) && R.IsNeg(sum) != R.IsNeg(a), sum);
                kind = Run2(SubOvf, a, b, out r);
                CheckOvf("sub.ovf", a, b, kind, r, R.IsNeg(a) != R.IsNeg(b) && R.IsNeg(diff) != R.IsNeg(a), diff);
                kind = Run2(MulOvf, a, b, out r);
                CheckOvf("mul.ovf", a, b, kind, r, R.MulOvfS(a, b), R.Mul(a, b));
                kind = Run2U(AddOvfU, a, b, out r);
                CheckOvf("add.ovf.un", a, b, kind, r, carry != 0, sum);
                kind = Run2U(SubOvfU, a, b, out r);
                CheckOvf("sub.ovf.un", a, b, kind, r, R.CmpU(a, b) < 0, diff);
                kind = Run2U(MulOvfU, a, b, out r);
                CheckOvf("mul.ovf.un", a, b, kind, r, R.MulOvfU(a, b), R.Mul(a, b));
            }

            {
                long r;
                int kind;
                kind = Run2((x, y) => AddOvf1(x), a, 0, out r);
                CheckOvf("add.ovf 1", a, 1, kind, r, R.Same(a, C(0x7FFFFFFF, 0xFFFFFFFF)), R.Add(a, one));
                kind = Run2((x, y) => SubOvf1(x), a, 0, out r);
                CheckOvf("sub.ovf 1", a, 1, kind, r, R.Same(a, C(0x80000000, 0)), R.Sub(a, one));
                kind = Run2U((x, y) => AddOvfU1(x), a, 0, out r);
                CheckOvf("add.ovf.un 1", a, 1, kind, r, R.Same(a, C(0xFFFFFFFF, 0xFFFFFFFF)), R.Add(a, one));
                kind = Run2U((x, y) => SubOvfU1(x), a, 0, out r);
                CheckOvf("sub.ovf.un 1", a, 1, kind, r, R.IsZero(a), R.Sub(a, one));
                kind = Run2((x, y) => NegOvf(x), a, 0, out r);
                CheckOvf("neg.ovf", a, 0, kind, r, R.Same(a, C(0x80000000, 0)), R.Neg(a));
            }
        }
        Group("checked");
    }

    // The same operations with every operand and result in memory: array elements, fields of a
    // class and of a struct behind a pointer, statics. The halves are loaded and stored at the
    // address and at the address plus four.
    class Box { public int Pad; public long A; public long B; public long C; }
    struct Trio { public long A; public int Pad; public long B; public long C; }
    static long s_a, s_b, s_c;

    [MethodImpl(NI)] static void ArrOps(long[] v, int i, int j, int k) { v[k] = v[i] + v[j]; v[k + 1] = v[i] - v[j]; v[k + 2] = v[i] * v[j]; }
    [MethodImpl(NI)] static void BoxOps(Box x) { x.C = x.A + x.B; x.A -= x.B; x.B = -x.B; }
    [MethodImpl(NI)] static void PtrOps(Trio* p) { p->C = p->A - p->B; p->A += 0x100000000L; p->B++; }
    [MethodImpl(NI)] static void StaticOps() { s_c = s_a + s_b; s_a = s_a - s_b; s_b = ~s_b + 1; }
    [MethodImpl(NI)] static bool ArrLt(long[] v, int i, int j) => v[i] < v[j];
    [MethodImpl(NI)] static void RefOps(ref long a, ref long b, out long c) { c = a * b; a += b; b -= 1; }
    [MethodImpl(NI)] static long SumArr(long[] v) { long s = 0; for (int i = 0; i < v.Length; i++) s += v[i]; return s; }
    [MethodImpl(NI)] static ulong SumArrU(ulong[] v) { ulong s = 0; foreach (ulong x in v) s += x; return s; }
    [MethodImpl(NI)] static long CountDown(long from, long step) { long n = 0; for (long i = from; i > 0; i -= step) n++; return n; }

    static void TestMemory()
    {
        long[] arr = new long[5];
        Box box = new Box();
        Trio trio = default;
        foreach (long a in V)
        {
            foreach (long b in V)
            {
                arr[0] = a; arr[1] = b;
                ArrOps(arr, 0, 1, 2);
                Check("arr add", a, b, arr[2], R.Add(a, b));
                Check("arr sub", a, b, arr[3], R.Sub(a, b));
                Check("arr mul", a, b, arr[4], R.Mul(a, b));
                Check("arr keep a", a, b, arr[0], a);
                Check("arr keep b", a, b, arr[1], b);
                CheckB("arr lt", a, b, ArrLt(arr, 0, 1), R.CmpS(a, b) < 0);

                box.A = a; box.B = b; box.Pad = 0x5A5A5A5A;
                BoxOps(box);
                Check("box add", a, b, box.C, R.Add(a, b));
                Check("box sub", a, b, box.A, R.Sub(a, b));
                Check("box neg", a, b, box.B, R.Neg(b));
                CheckI("box pad", a, b, box.Pad, 0x5A5A5A5A);

                trio.A = a; trio.B = b; trio.Pad = 0x3C3C3C3C;
                PtrOps(&trio);
                Check("ptr sub", a, b, trio.C, R.Sub(a, b));
                Check("ptr add", a, b, trio.A, R.Add(a, C(1, 0)));
                Check("ptr inc", a, b, trio.B, R.Add(b, C(0, 1)));
                CheckI("ptr pad", a, b, trio.Pad, 0x3C3C3C3C);

                s_a = a; s_b = b;
                StaticOps();
                Check("static add", a, b, s_c, R.Add(a, b));
                Check("static sub", a, b, s_a, R.Sub(a, b));
                Check("static neg", a, b, s_b, R.Neg(b));

                long x = a, y = b, z;
                RefOps(ref x, ref y, out z);
                Check("ref mul", a, b, z, R.Mul(a, b));
                Check("ref add", a, b, x, R.Add(a, b));
                Check("ref dec", a, b, y, R.Sub(b, C(0, 1)));
            }
        }

        long sum = C(0, 0);
        foreach (long a in V) sum = R.Add(sum, a);
        Check("sum array", 0, 0, SumArr(V), sum);
        ulong[] vu = new ulong[V.Length];
        for (int i = 0; i < V.Length; i++) vu[i] = (ulong)V[i];
        Check("sum array u", 0, 0, (long)SumArrU(vu), sum);
        Check("count down", 0, 0, CountDown(0x300000000L, 0x10000000L), C(0, 48));
        Check("count down 2", 0, 0, CountDown(0x100000005L, 0x80000000L), C(0, 3));
        Group("memory");
    }

    static int Main()
    {
        TestBinary();
        TestCompare();
        TestUnary();
        TestDivide();
        TestChecked();
        TestMemory();

        if (fails == 0)
        {
            Console.WriteLine("ALL PASS");
            return 0;
        }
        Console.WriteLine("FAIL " + fails.ToString());
        return 1;
    }
}
