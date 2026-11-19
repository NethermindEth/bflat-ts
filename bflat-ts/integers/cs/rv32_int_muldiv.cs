// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// 32-bit signed division, remainder and high multiplication built from the
// unsigned instructions, byte loads followed by a mask, and leaf methods
// with loops that do not save the return address (riscv32, Airbender).
//
// Airbender proves only the unsigned half of RV32M, so with
// JitRiscVUnsignedMulDiv the JIT emits no div, rem, mulh or mulhsu:
//
//   a / b, a % b:  divide the magnitudes with divu or remu, then give the
//                  quotient the sign of a ^ b and the remainder the sign of a
//   mulh(a, b):    mulhu(a, b) - (a < 0 ? b : 0) - (b < 0 ? a : 0)
//
// Both sequences use ra as a scratch register. A leaf method normally keeps
// its return address in ra without saving it - since patch 04 also when it
// has loops - so a method that contains one of these sequences must not be
// treated as such a leaf. Division by a constant becomes a multiplication
// by a magic number and a mulh, a checked multiplication compares the high
// product with the sign of the low one, and (long)a * b takes its high half
// from mulh.
//
// What can go wrong: the wrong sign for one of the four sign combinations,
// MinValue as either operand (its magnitude does not fit an int), a scratch
// register that is also an operand or the destination, the missing checks
// for a zero divisor and for MinValue / -1 (which .NET requires to throw),
// a leaf that returns through a clobbered ra, and a loop in a leaf that
// loses ra some other way.
//
// Patch 04 also drops `andi rd, rd, 0xff` right after `lbu rd, ...`. What
// can go wrong: the mask dropped after a load that sign-extends (lb), after
// a load of another register, or when another value was meant.
//
// Expected quotients and remainders are computed from the magnitudes with
// unsigned int arithmetic, expected products with 16-bit limbs. Each group
// prints the number of checks; a failed check prints one line with the
// operands, the expected and the actual value. The run ends with ALL PASS
// or a FAIL count, and the guest returns 1 on failure.
using System;
using System.Runtime.CompilerServices;

unsafe class Program
{
    const MethodImplOptions NI = MethodImplOptions.NoInlining;

    static int fails;
    static int checks;

    static void Check(string name, int a, int b, int got, int exp)
    {
        checks++;
        if (got != exp)
        {
            fails++;
            if (fails <= 60)
                Console.WriteLine("FAIL " + name + " a=" + a.ToString() + " b=" + b.ToString() + " expected " + exp.ToString() + " got " + got.ToString());
        }
    }

    static void Group(string name)
    {
        Console.WriteLine("ok   " + name + " " + checks.ToString());
    }

    static readonly int[] I =
    {
        0, 1, -1, 2, -2, 3, -3, 7, -7, 10, -10, 255, -255, 256, -256, 1000, -1000,
        0x7FFF, -0x8000, 0xFFFF, 0x10000, -0x10000, 46340, 46341, -46340, -46341, 65535, 65536,
        0x12345678, -0x12345678, 0x40000000, -0x40000000, 0x7FFFFFFE, 0x7FFFFFFF, -0x7FFFFFFF, unchecked((int)0x80000000),
        unchecked((int)0x80000001), 1000000007, -1000000007,
    };

    // ---- the reference ----

    static uint Mag(int x) => x < 0 ? (uint)0 - (uint)x : (uint)x;
    static int RefDiv(int a, int b) { uint q = Mag(a) / Mag(b); return (a < 0) != (b < 0) ? (int)((uint)0 - q) : (int)q; }
    static int RefRem(int a, int b) { uint r = Mag(a) % Mag(b); return a < 0 ? (int)((uint)0 - r) : (int)r; }

    // The 64-bit product of two unsigned ints by 16-bit limbs.
    static void MulU(uint a, uint b, out uint hi, out uint lo)
    {
        uint a0 = a & 0xFFFF, a1 = a >> 16, b0 = b & 0xFFFF, b1 = b >> 16;
        uint p00 = a0 * b0, p01 = a0 * b1, p10 = a1 * b0, p11 = a1 * b1;
        uint mid = (p00 >> 16) + (p01 & 0xFFFF) + (p10 & 0xFFFF);
        lo = (p00 & 0xFFFF) | (mid << 16);
        hi = p11 + (p01 >> 16) + (p10 >> 16) + (mid >> 16);
    }

    static void MulS(int a, int b, out int hi, out uint lo)
    {
        MulU((uint)a, (uint)b, out uint h, out lo);
        if (a < 0) h -= (uint)b;
        if (b < 0) h -= (uint)a;
        hi = (int)h;
    }

    // ---- the operations under test ----

    [MethodImpl(NI)] static int Div(int a, int b) => a / b;
    [MethodImpl(NI)] static int Rem(int a, int b) => a % b;
    [MethodImpl(NI)] static uint DivU(uint a, uint b) => a / b;
    [MethodImpl(NI)] static uint RemU(uint a, uint b) => a % b;
    [MethodImpl(NI)] static int DivRem(int a, int b) => a / b * 31 + a % b;
    [MethodImpl(NI)] static int DivSwapped(int b, int a) => a / b;
    [MethodImpl(NI)] static int DivSelf(int a) => a / a;
    [MethodImpl(NI)] static int RemSelf(int a) => a % a;
    [MethodImpl(NI)] static int DivInto(int a, int b) { a = a / b; b = a % b; return a * 7 + b; }
    [MethodImpl(NI)] static int DivShort(short a, sbyte b) => a / b;
    [MethodImpl(NI)] static int DivMem(int* p) => p[0] / p[1] + p[0] % p[1];
    [MethodImpl(NI)] static nint DivNative(nint a, nint b) => a / b;
    [MethodImpl(NI)] static nint RemNative(nint a, nint b) => a % b;

    [MethodImpl(NI)] static int Div3(int a) => a / 3;
    [MethodImpl(NI)] static int Div7(int a) => a / 7;
    [MethodImpl(NI)] static int DivM5(int a) => a / -5;
    [MethodImpl(NI)] static int Div10(int a) => a / 10;
    [MethodImpl(NI)] static int Div641(int a) => a / 641;
    [MethodImpl(NI)] static int DivBig(int a) => a / 1000000007;
    [MethodImpl(NI)] static int Div16(int a) => a / 16;
    [MethodImpl(NI)] static int DivM16(int a) => a / -16;
    [MethodImpl(NI)] static int DivM1(int a) => a / -1;
    [MethodImpl(NI)] static int Div1(int a) => a / 1;
    [MethodImpl(NI)] static int DivMin(int a) => a / int.MinValue;
    [MethodImpl(NI)] static int Rem3(int a) => a % 3;
    [MethodImpl(NI)] static int Rem7(int a) => a % 7;
    [MethodImpl(NI)] static int Rem10(int a) => a % 10;
    [MethodImpl(NI)] static int RemM10(int a) => a % -10;
    [MethodImpl(NI)] static int Rem16(int a) => a % 16;
    [MethodImpl(NI)] static int RemBig(int a) => a % 1000000007;
    [MethodImpl(NI)] static uint DivU3(uint a) => a / 3;
    [MethodImpl(NI)] static uint DivU7(uint a) => a / 7;
    [MethodImpl(NI)] static uint DivU10(uint a) => a / 10;
    [MethodImpl(NI)] static uint RemU10(uint a) => a % 10;
    [MethodImpl(NI)] static uint DivUBig(uint a) => a / 0x80000001u;
    [MethodImpl(NI)] static int ConstDivBy(int b) => 1000000 / b;
    [MethodImpl(NI)] static int ConstRemBy(int b) => -1000000 % b;

    [MethodImpl(NI)] static int MulHi(int a, int b) => (int)(((long)a * b) >> 32);
    [MethodImpl(NI)] static uint MulHiU(uint a, uint b) => (uint)(((ulong)a * b) >> 32);
    [MethodImpl(NI)] static int MulHiMath(int a, int b) => (int)(Math.BigMul(a, b) >> 32);
    [MethodImpl(NI)] static long MulWide(int a, int b) => (long)a * b;
    [MethodImpl(NI)] static int MulLo(int a, int b) => a * b;
    [MethodImpl(NI)] static int MulOvf(int a, int b) => checked(a * b);
    [MethodImpl(NI)] static uint MulOvfU(uint a, uint b) => checked(a * b);
    [MethodImpl(NI)] static int MulOvfConst(int a) => checked(a * 100003);
    [MethodImpl(NI)] static int AddOvf(int a, int b) => checked(a + b);
    [MethodImpl(NI)] static int SubOvf(int a, int b) => checked(a - b);
    [MethodImpl(NI)] static uint AddOvfU(uint a, uint b) => checked(a + b);
    [MethodImpl(NI)] static uint SubOvfU(uint a, uint b) => checked(a - b);

    // ---- leaves with loops: nothing here calls, so ra is not saved unless a sequence needs it ----

    [MethodImpl(NI)]
    static int LeafDivLoop(int a, int b, int n)
    {
        int s = 0;
        for (int i = 0; i < n; i++) { s += a / b; a -= 3; }
        return s;
    }

    [MethodImpl(NI)]
    static int LeafRemLoop(int a, int b, int n)
    {
        int s = 0;
        for (int i = 0; i < n; i++) { s += a % b; a += 7; }
        return s;
    }

    [MethodImpl(NI)]
    static int LeafConstDivLoop(int a, int n)
    {
        int s = 0;
        for (int i = 0; i < n; i++) { s += a / 7 + a % 10; a -= 1001; }
        return s;
    }

    [MethodImpl(NI)]
    static int LeafMulHiLoop(int a, int b, int n)
    {
        int s = 0;
        for (int i = 0; i < n; i++) { s += (int)(((long)a * b) >> 32); a += 0x01010101; b -= 0x00100010; }
        return s;
    }

    [MethodImpl(NI)]
    static long LeafMulWideLoop(int a, int b, int n)
    {
        long s = 0;
        for (int i = 0; i < n; i++) { s += (long)a * b; a = -a + 3; }
        return s;
    }

    [MethodImpl(NI)]
    static int LeafGcd(int a, int b)
    {
        while (b != 0) { int t = a % b; a = b; b = t; }
        return a;
    }

    [MethodImpl(NI)]
    static uint LeafUnsignedLoop(uint a, uint b, int n)
    {
        uint s = 0;
        for (int i = 0; i < n; i++) { s += a / b + a % b; a += 0x10000001; }
        return s;
    }

    [MethodImpl(NI)]
    static long LeafLongLoop(long a, long b, int n)
    {
        long s = 0;
        for (int i = 0; i < n; i++) { s += a * b; s ^= s << (i & 63); a += b; b = (b >> 1) - i; }
        return s;
    }

    [MethodImpl(NI)]
    static int LeafNestedLoops(int n)
    {
        int s = 0;
        for (int i = 1; i <= n; i++)
            for (int j = 1; j <= i; j++)
                s += (i * j) % (j + 1) - i / j;
        return s;
    }

    [MethodImpl(NI)]
    static int LeafDigits(int x, byte* buf)
    {
        int n = 0;
        bool neg = x < 0;
        do { int d = x % 10; buf[n++] = (byte)('0' + (d < 0 ? -d : d)); x /= 10; } while (x != 0);
        if (neg) buf[n++] = (byte)'-';
        return n;
    }

    // The same loop twice in a row: the second call returns through the first one's ra.
    [MethodImpl(NI)]
    static int CallsLeavesTwice(int a, int b)
    {
        int x = LeafDivLoop(a, b, 5);
        int y = LeafRemLoop(a, b, 5);
        int z = LeafGcd(a, b);
        return x * 31 + y * 7 + z;
    }

    // ---- byte loads and masks ----

    [MethodImpl(NI)] static int LoadByteMask(byte* p) => *p & 0xFF;
    [MethodImpl(NI)] static int LoadSByteMask(sbyte* p) => *p & 0xFF;
    [MethodImpl(NI)] static int LoadSByte(sbyte* p) => *p;
    [MethodImpl(NI)] static int LoadByteCast(int* p) => (byte)*p;
    [MethodImpl(NI)] static int LoadByteTwice(byte* p) => (byte)(p[0] + p[1]);
    [MethodImpl(NI)] static int LoadOtherMask(byte* p, int x) { int b = p[0]; return (x & 0xFF) + b; }
    [MethodImpl(NI)] static int LoadThenMaskOther(byte* p, int x) { int b = p[0]; x &= 0xFF; return x * 256 + b; }
    [MethodImpl(NI)] static int LoadBool(bool* p) => *p ? 10 : 20;
    [MethodImpl(NI)] static int LoadByteArr(byte[] a, int i) => a[i] & 0xFF;
    [MethodImpl(NI)] static int LoadSByteArrMask(sbyte[] a, int i) => a[i] & 0xFF;
    [MethodImpl(NI)] static int LoadByteField(ByteBox b) => (byte)b.B + ((byte)b.S << 8);
    [MethodImpl(NI)] static int LoadShortMask(short* p) => *p & 0xFF;
    [MethodImpl(NI)] static int LoadIntMask(int* p) => *p & 0xFF;
    [MethodImpl(NI)] static int LoadByteMask7F(byte* p) => *p & 0x7F;
    [MethodImpl(NI)] static uint LoadByteWiden(byte* p) => (uint)(byte)(*p);
    [MethodImpl(NI)] static int SumBytes(byte* p, int n) { int s = 0; for (int i = 0; i < n; i++) s += (byte)(p[i] ^ 0x80) & 0xFF; return s; }
    [MethodImpl(NI)] static int SumSBytes(sbyte* p, int n) { int s = 0; for (int i = 0; i < n; i++) s += p[i] & 0xFF; return s; }

    class ByteBox { public byte B; public sbyte S; }

    delegate int Op2(int a, int b);

    // 0 = no exception, 1 = OverflowException, 2 = DivideByZeroException, 3 = any other.
    static int Run(Op2 op, int a, int b, out int r)
    {
        r = 0;
        try { r = op(a, b); return 0; }
        catch (OverflowException) { return 1; }
        catch (DivideByZeroException) { return 2; }
        catch (Exception) { return 3; }
    }

    static void CheckThrows(string name, Op2 op, int a, int b, int expKind, int exp)
    {
        int kind = Run(op, a, b, out int r);
        Check(name + " (kind)", a, b, kind, expKind);
        if (kind == 0 && expKind == 0) Check(name, a, b, r, exp);
    }

    static void TestDivide()
    {
        int* pair = stackalloc int[2];
        foreach (int a in I)
        {
            foreach (int b in I)
            {
                if (b == 0)
                {
                    CheckThrows("div by zero", Div, a, b, 2, 0);
                    CheckThrows("rem by zero", Rem, a, b, 2, 0);
                    CheckThrows("divu by zero", (x, y) => (int)DivU((uint)x, (uint)y), a, b, 2, 0);
                    CheckThrows("remu by zero", (x, y) => (int)RemU((uint)x, (uint)y), a, b, 2, 0);
                    CheckThrows("const div by zero", (x, y) => ConstDivBy(y), a, b, 2, 0);
                    continue;
                }
                Check("divu", a, b, (int)DivU((uint)a, (uint)b), (int)((uint)a / (uint)b));
                Check("remu", a, b, (int)RemU((uint)a, (uint)b), (int)((uint)a - (uint)a / (uint)b * (uint)b));
                if (a == int.MinValue && b == -1)
                {
                    CheckThrows("min / -1", Div, a, b, 1, 0);
                    int kind = Run(Rem, a, b, out int r);
                    Check("min % -1", a, b, (kind == 1 || (kind == 0 && r == 0)) ? 1 : 0, 1);
                    continue;
                }
                int q = RefDiv(a, b), m = RefRem(a, b);
                Check("div", a, b, Div(a, b), q);
                Check("rem", a, b, Rem(a, b), m);
                Check("div and rem", a, b, DivRem(a, b), q * 31 + m);
                Check("div swapped", a, b, DivSwapped(b, a), q);
                // a = a / b; b = a % b: the quotient is MinValue only for MinValue / 1.
                Check("div into operands", a, b, DivInto(a, b), q * 7 + RefRem(q, b));
                pair[0] = a; pair[1] = b;
                Check("div from memory", a, b, DivMem(pair), q + m);
                Check("div native", a, b, (int)DivNative(a, b), q);
                Check("rem native", a, b, (int)RemNative(a, b), m);
                int sa = (short)a, sb = (sbyte)b;
                if (sb != 0) Check("div short by sbyte", a, b, DivShort((short)a, (sbyte)b), RefDiv(sa, sb));
            }

            if (a != 0)
            {
                Check("a / a", a, a, DivSelf(a), 1);
                Check("a % a", a, a, RemSelf(a), 0);
                Check("const / b", a, a, ConstDivBy(a), RefDiv(1000000, a));
                Check("const % b", a, a, ConstRemBy(a), RefRem(-1000000, a));
            }

            Check("/ 3", a, 3, Div3(a), RefDiv(a, 3));
            Check("/ 7", a, 7, Div7(a), RefDiv(a, 7));
            Check("/ -5", a, -5, DivM5(a), RefDiv(a, -5));
            Check("/ 10", a, 10, Div10(a), RefDiv(a, 10));
            Check("/ 641", a, 641, Div641(a), RefDiv(a, 641));
            Check("/ 1000000007", a, 1000000007, DivBig(a), RefDiv(a, 1000000007));
            Check("/ 16", a, 16, Div16(a), RefDiv(a, 16));
            Check("/ -16", a, -16, DivM16(a), RefDiv(a, -16));
            Check("/ 1", a, 1, Div1(a), a);
            Check("/ MinValue", a, int.MinValue, DivMin(a), a == int.MinValue ? 1 : 0);
            if (a != int.MinValue) Check("/ -1", a, -1, DivM1(a), -a);
            Check("% 3", a, 3, Rem3(a), RefRem(a, 3));
            Check("% 7", a, 7, Rem7(a), RefRem(a, 7));
            Check("% 10", a, 10, Rem10(a), RefRem(a, 10));
            Check("% -10", a, -10, RemM10(a), RefRem(a, -10));
            Check("% 16", a, 16, Rem16(a), RefRem(a, 16));
            Check("% 1000000007", a, 1000000007, RemBig(a), RefRem(a, 1000000007));
            // Unsigned division by a constant: checked by q * c + r == a and r < c.
            uint ua = (uint)a;
            uint q3 = DivU3(ua), q7 = DivU7(ua), q10 = DivU10(ua), r10 = RemU10(ua), qb = DivUBig(ua);
            Check("u / 3", a, 3, (ua - q3 * 3 < 3 && q3 <= 0x55555555u) ? 1 : 0, 1);
            Check("u / 7", a, 7, (ua - q7 * 7 < 7 && q7 <= 0x24924924u) ? 1 : 0, 1);
            Check("u / 10", a, 10, (ua - q10 * 10 < 10 && q10 <= 0x19999999u) ? 1 : 0, 1);
            Check("u % 10", a, 10, (int)r10, (int)(ua - q10 * 10));
            Check("u / 0x80000001", a, 0, (int)qb, ua >= 0x80000001u ? 1 : 0);
        }
        Group("divide");
    }

    static void TestMultiply()
    {
        foreach (int a in I)
        {
            foreach (int b in I)
            {
                MulS(a, b, out int shi, out uint slo);
                MulU((uint)a, (uint)b, out uint uhi, out uint ulo);
                Check("mul high", a, b, MulHi(a, b), shi);
                Check("mul high u", a, b, (int)MulHiU((uint)a, (uint)b), (int)uhi);
                Check("mul high BigMul", a, b, MulHiMath(a, b), shi);
                long w = MulWide(a, b);
                Check("mul wide low", a, b, (int)((uint*)&w)[0], (int)slo);
                Check("mul wide high", a, b, (int)((uint*)&w)[1], shi);
                Check("mul low", a, b, MulLo(a, b), (int)slo);

                bool sovf = shi != ((int)slo < 0 ? -1 : 0);
                CheckThrows("mul.ovf", MulOvf, a, b, sovf ? 1 : 0, (int)slo);
                CheckThrows("mul.ovf.un", (x, y) => (int)MulOvfU((uint)x, (uint)y), a, b, uhi != 0 ? 1 : 0, (int)ulo);

                int sum = (int)((uint)a + (uint)b), diff = (int)((uint)a - (uint)b);
                CheckThrows("add.ovf", AddOvf, a, b, ((a < 0) == (b < 0) && (sum < 0) != (a < 0)) ? 1 : 0, sum);
                CheckThrows("sub.ovf", SubOvf, a, b, ((a < 0) != (b < 0) && (diff < 0) != (a < 0)) ? 1 : 0, diff);
                CheckThrows("add.ovf.un", (x, y) => (int)AddOvfU((uint)x, (uint)y), a, b, (uint)sum < (uint)a ? 1 : 0, sum);
                CheckThrows("sub.ovf.un", (x, y) => (int)SubOvfU((uint)x, (uint)y), a, b, (uint)a < (uint)b ? 1 : 0, diff);
            }
            MulS(a, 100003, out int chi, out uint clo);
            CheckThrows("mul.ovf const", (x, y) => MulOvfConst(x), a, 0, chi != ((int)clo < 0 ? -1 : 0) ? 1 : 0, (int)clo);
        }
        Group("multiply");
    }

    static void TestLeaves()
    {
        byte* buf = stackalloc byte[16];
        foreach (int a in I)
        {
            foreach (int b in I)
            {
                if (b == 0 || b == -1) continue;

                int s = 0, x = a;
                for (int i = 0; i < 9; i++) { s += RefDiv(x, b); x -= 3; }
                Check("leaf div loop", a, b, LeafDivLoop(a, b, 9), s);

                s = 0; x = a;
                for (int i = 0; i < 9; i++) { s += RefRem(x, b); x += 7; }
                Check("leaf rem loop", a, b, LeafRemLoop(a, b, 9), s);

                s = 0; x = a; int y = b;
                for (int i = 0; i < 9; i++) { MulS(x, y, out int hi, out _); s += hi; x += 0x01010101; y -= 0x00100010; }
                Check("leaf mul high loop", a, b, LeafMulHiLoop(a, b, 9), s);

                uint lo = 0, hi2 = 0; x = a;
                for (int i = 0; i < 9; i++)
                {
                    MulS(x, b, out int h, out uint l);
                    uint nlo = lo + l;
                    hi2 = hi2 + (uint)h + (nlo < l ? 1u : 0u);
                    lo = nlo;
                    x = -x + 3;
                }
                long wide = LeafMulWideLoop(a, b, 9);
                Check("leaf mul wide loop low", a, b, (int)((uint*)&wide)[0], (int)lo);
                Check("leaf mul wide loop high", a, b, (int)((uint*)&wide)[1], (int)hi2);

                int ga = a, gb = b;
                while (gb != 0) { if (ga == int.MinValue && gb == -1) break; int t = RefRem(ga, gb); ga = gb; gb = t; }
                if (gb == 0) Check("leaf gcd", a, b, LeafGcd(a, b), ga);

                uint us = 0, ux = (uint)a;
                for (int i = 0; i < 9; i++) { us += ux / (uint)b + (ux - ux / (uint)b * (uint)b); ux += 0x10000001; }
                Check("leaf unsigned loop", a, b, (int)LeafUnsignedLoop((uint)a, (uint)b, 9), (int)us);

                if (a != int.MinValue)
                {
                    int e1 = 0, e2 = 0;
                    x = a; for (int i = 0; i < 5; i++) { e1 += RefDiv(x, b); x -= 3; }
                    x = a; for (int i = 0; i < 5; i++) { e2 += RefRem(x, b); x += 7; }
                    ga = a; gb = b;
                    bool ok = true;
                    while (gb != 0) { if (ga == int.MinValue && gb == -1) { ok = false; break; } int t = RefRem(ga, gb); ga = gb; gb = t; }
                    if (ok) Check("calls leaves twice", a, b, CallsLeavesTwice(a, b), e1 * 31 + e2 * 7 + ga);
                }
            }

            {
                int s = 0, x = a;
                for (int i = 0; i < 9; i++) { s += RefDiv(x, 7) + RefRem(x, 10); x -= 1001; }
                Check("leaf const div loop", a, 0, LeafConstDivLoop(a, 9), s);

                int n = LeafDigits(a, buf);
                string text = a.ToString();
                bool same = n == text.Length;
                for (int i = 0; same && i < n; i++) same = buf[n - 1 - i] == (byte)text[i];
                Check("leaf digits", a, 0, same ? 1 : 0, 1);
            }
        }

        {
            int s = 0;
            for (int i = 1; i <= 12; i++)
                for (int j = 1; j <= i; j++)
                    s += RefRem(i * j, j + 1) - RefDiv(i, j);
            Check("leaf nested loops", 12, 0, LeafNestedLoops(12), s);
        }

        // 2^32 + 3 times 5, the sum shifted and the operands stepped: literal results from the same
        // loop evaluated by hand for two rounds.
        //   i = 0: s = a * b = 0x5_0000000F; s ^= s << 0 -> 0; a = 0x1_00000008, b = 2
        //   i = 1: s = 0 + a * b = 0x2_00000010; s ^= s << 1 -> 0x2_00000010 ^ 0x4_00000020 = 0x6_00000030
        long ll = LeafLongLoop(0x100000003L, 5, 2);
        Check("leaf long loop low", 0, 0, (int)((uint*)&ll)[0], 0x30);
        Check("leaf long loop high", 0, 0, (int)((uint*)&ll)[1], 6);
        Group("leaves with loops");
    }

    static void TestBytes()
    {
        byte* p = stackalloc byte[16];
        byte[] arr = new byte[16];
        sbyte[] sarr = new sbyte[16];
        ByteBox box = new ByteBox();
        int sum = 0, ssum = 0;
        for (int i = 0; i < 16; i++)
        {
            byte v = (byte)(i * 37 + 0x79);
            p[i] = v; arr[i] = v; sarr[i] = (sbyte)v;
            sum += (v ^ 0x80) & 0xFF;
            ssum += v;
        }

        for (int i = 0; i < 15; i++)
        {
            int v = (i * 37 + 0x79) & 0xFF, v1 = ((i + 1) * 37 + 0x79) & 0xFF;
            int sv = (v << 24) >> 24;
            Check("byte & 0xFF", i, 0, LoadByteMask(p + i), v);
            Check("sbyte & 0xFF", i, 0, LoadSByteMask((sbyte*)p + i), v);
            Check("sbyte", i, 0, LoadSByte((sbyte*)p + i), sv);
            Check("(byte)two bytes", i, 0, LoadByteTwice(p + i), (v + v1) & 0xFF);
            Check("mask of another value", i, 0, LoadOtherMask(p + i, 0x12345), 0x45 + v);
            Check("mask of another value 2", i, 0, LoadThenMaskOther(p + i, 0x123FE), 0xFE * 256 + v);
            Check("byte array & 0xFF", i, 0, LoadByteArr(arr, i), v);
            Check("sbyte array & 0xFF", i, 0, LoadSByteArrMask(sarr, i), v);
            Check("byte & 0x7F", i, 0, LoadByteMask7F(p + i), v & 0x7F);
            Check("byte widened", i, 0, (int)LoadByteWiden(p + i), v);
            box.B = (byte)v; box.S = (sbyte)v;
            Check("byte fields", i, 0, LoadByteField(box), v + (v << 8));
            int word = v | (v1 << 8) | 0x7F5A0000;
            Check("(byte)int load", i, 0, LoadByteCast(&word), v);
            Check("int & 0xFF", i, 0, LoadIntMask(&word), v);
            Check("short & 0xFF", i, 0, LoadShortMask((short*)&word), v);
        }
        bool t = true, f = false;
        Check("bool true", 0, 0, LoadBool(&t), 10);
        Check("bool false", 0, 0, LoadBool(&f), 20);
        Check("sum bytes", 0, 0, SumBytes(p, 16), sum);
        Check("sum sbytes", 0, 0, SumSBytes((sbyte*)p, 16), ssum);
        Group("byte loads");
    }

    static int Main()
    {
        TestDivide();
        TestMultiply();
        TestLeaves();
        TestBytes();

        if (fails == 0)
        {
            Console.WriteLine("ALL PASS");
            return 0;
        }
        Console.WriteLine("FAIL " + fails.ToString());
        return 1;
    }
}
