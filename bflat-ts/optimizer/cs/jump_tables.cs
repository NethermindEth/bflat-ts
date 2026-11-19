// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Switch statements that compile to jump tables, and 64-bit constants.
//
// On RISC-V the tables and constants a method needs are not placed after
// its code. They go to a read-only data section of their own, and the code
// reaches them through a relocated auipc pair, because a zkVM transpiler
// reads the code section as instructions only. A wrong relocation, a table
// entry that is taken relative to the wrong address, or an index that is
// not checked or not widened correctly sends the jump to the wrong case.
//
// The switches differ in everything that changes the table or its use:
// four cases, the smallest that gets a table, up to 160 cases in a method of
// several kilobytes; a lowest case below zero; values of type long, ulong
// and uint that are far outside the table but have the low bits of a case;
// byte, sbyte, char and enum values; cases with gaps; two tables in one
// method and a switch inside a switch; an interpreter loop; switches inside
// try, catch, finally and a filter, which are separate code fragments; a
// shared generic method, two value-type instantiations, a lambda and a
// virtual method; a switch on strings and a switch expression with ranges.
// A method with many 64-bit constants loads them from the same section.
//
// Every switch is called with all its cases, the values next to the first
// and last case, and extreme values. The expected results are computed by
// code compiled without optimization that has no switch in it. Each check
// prints one line; the run ends with ALL PASS or a FAIL count, and the
// guest returns the number of failures.
using System;
using System.Runtime.CompilerServices;

class E : Exception { public int Code; public E(int c) : base("e") { Code = c; } }

enum Op : byte { Nop, Add, Sub, Mul, Xor, Shl, Shr, Neg, Not, Swap, Dup, Halt }

abstract class Shape
{
    public abstract int Area(int kind, int x);
}

class Table : Shape
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override int Area(int kind, int x)
    {
        switch (kind)
        {
            case 0: return x;
            case 1: return x * x;
            case 2: return x * 3 + 1;
            case 3: return x >> 1;
            case 4: return -x;
            case 5: return x ^ 0x5555;
            default: return 7;
        }
    }
}

class Flat : Shape
{
    public override int Area(int kind, int x) => 0;
}

class Program
{
    const MethodImplOptions NoOpt = MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization;

    static int fails;

    static void Check(string name, bool ok)
    {
        Console.WriteLine((ok ? "ok   " : "FAIL ") + name);
        if (!ok) fails++;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Thrower(int code)
    {
        if (code >= 0) throw new E(code);
        return 0;
    }

    // ---- tables of different sizes and bases ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Four(int x)
    {
        switch (x)
        {
            case 0: return 31;
            case 1: return 47;
            case 2: return 5;
            case 3: return 99;
            default: return -1;
        }
    }

    static readonly int[] fourWant = { 31, 47, 5, 99 };

    [MethodImpl(NoOpt)]
    static int FourRef(int x) => x >= 0 && x <= 3 ? fourWant[x] : -1;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Seventeen(int x, int y)
    {
        int r = y;
        switch (x)
        {
            case 0: r += 3; break;
            case 1: r ^= 0x111; break;
            case 2: r *= 5; break;
            case 3: r -= 77; break;
            case 4: r = r << 3; break;
            case 5: r = r >> 2; break;
            case 6: r = -r; break;
            case 7: r = ~r; break;
            case 8: r += x * y; break;
            case 9: r |= 0x8000; break;
            case 10: r &= 0x7F7F; break;
            case 11: r = r * r; break;
            case 12: r += 1000003; break;
            case 13: r ^= r >> 5; break;
            case 14: r -= y << 4; break;
            case 15: r = 15; break;
            case 16: r += 16 * 16; break;
            default: r = -r - 1; break;
        }
        return r + x;
    }

    [MethodImpl(NoOpt)]
    static int SeventeenRef(int x, int y)
    {
        int r = y;
        if (x == 0) r += 3;
        else if (x == 1) r ^= 0x111;
        else if (x == 2) r *= 5;
        else if (x == 3) r -= 77;
        else if (x == 4) r = r << 3;
        else if (x == 5) r = r >> 2;
        else if (x == 6) r = -r;
        else if (x == 7) r = ~r;
        else if (x == 8) r += x * y;
        else if (x == 9) r |= 0x8000;
        else if (x == 10) r &= 0x7F7F;
        else if (x == 11) r = r * r;
        else if (x == 12) r += 1000003;
        else if (x == 13) r ^= r >> 5;
        else if (x == 14) r -= y << 4;
        else if (x == 15) r = 15;
        else if (x == 16) r += 16 * 16;
        else r = -r - 1;
        return r + x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int BelowZero(int x)
    {
        switch (x)
        {
            case -7: return 70;
            case -6: return 61;
            case -5: return 52;
            case -4: return 43;
            case -3: return 34;
            case -2: return 25;
            case -1: return 16;
            case 0: return 7;
            case 1: return 18;
            case 2: return 29;
            default: return 1000;
        }
    }

    [MethodImpl(NoOpt)]
    static int BelowZeroRef(int x)
    {
        if (x < -7 || x > 2) return 1000;
        return x <= 0 ? 7 - x * 9 : 7 + x * 11;
    }

    // Cases with gaps: part table, part comparisons.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Gaps(int x)
    {
        switch (x)
        {
            case 1: return 11;
            case 3: return 13;
            case 5: return 15;
            case 6: return 16;
            case 100: return 200;
            case 101: return 201;
            case 102: return 202;
            case 103: return 203;
            case 104: return 204;
            case 1000: return 5;
            case int.MaxValue: return 6;
            case int.MinValue: return 7;
            default: return 0;
        }
    }

    [MethodImpl(NoOpt)]
    static int GapsRef(int x)
    {
        if (x == 1 || x == 3 || x == 5 || x == 6) return 10 + x;
        if (x >= 100 && x <= 104) return 100 + x;
        if (x == 1000) return 5;
        if (x == int.MaxValue) return 6;
        if (x == int.MinValue) return 7;
        return 0;
    }

    // ---- values wider or narrower than the table index ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Wide(long x, ulong y, uint z)
    {
        int r;
        switch (x)
        {
            case 0: r = 1; break;
            case 1: r = 2; break;
            case 2: r = 3; break;
            case 3: r = 4; break;
            case 4: r = 5; break;
            case 5: r = 6; break;
            default: r = 9; break;
        }
        switch (y)
        {
            case 0: r += 10; break;
            case 1: r += 20; break;
            case 2: r += 30; break;
            case 3: r += 40; break;
            case 4: r += 50; break;
            default: r += 90; break;
        }
        switch (z)
        {
            case 0: return r + 100;
            case 1: return r + 200;
            case 2: return r + 300;
            case 3: return r + 400;
            case 4: return r + 500;
            default: return r + 900;
        }
    }

    [MethodImpl(NoOpt)]
    static int WideRef(long x, ulong y, uint z)
    {
        int r = x >= 0 && x <= 5 ? (int)x + 1 : 9;
        r += y <= 4 ? ((int)y + 1) * 10 : 90;
        return r + (z <= 4 ? ((int)z + 1) * 100 : 900);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Narrow(byte b, sbyte sb, char c, Op op)
    {
        int r;
        switch (b)
        {
            case 250: r = 1; break;
            case 251: r = 2; break;
            case 252: r = 3; break;
            case 253: r = 4; break;
            case 254: r = 5; break;
            case 255: r = 6; break;
            default: r = 7; break;
        }
        switch (sb)
        {
            case -128: r += 10; break;
            case -127: r += 20; break;
            case -126: r += 30; break;
            case -125: r += 40; break;
            case -124: r += 50; break;
            default: r += 60; break;
        }
        switch (c)
        {
            case 'a': r += 100; break;
            case 'b': r += 200; break;
            case 'c': r += 300; break;
            case 'd': r += 400; break;
            case 'e': r += 500; break;
            case 'f': r += 600; break;
            default: r += 700; break;
        }
        switch (op)
        {
            case Op.Nop: return r;
            case Op.Add: return r + 1000;
            case Op.Sub: return r + 2000;
            case Op.Mul: return r + 3000;
            case Op.Xor: return r + 4000;
            case Op.Shl: return r + 5000;
            case Op.Shr: return r + 6000;
            case Op.Neg: return r + 7000;
            case Op.Not: return r + 8000;
            case Op.Swap: return r + 9000;
            case Op.Dup: return r + 10000;
            case Op.Halt: return r + 11000;
            default: return -r;
        }
    }

    [MethodImpl(NoOpt)]
    static int NarrowRef(int b, int sb, int c, int op)
    {
        int r = b >= 250 ? b - 249 : 7;
        r += sb <= -124 ? (sb + 129) * 10 : 60;
        r += c >= 'a' && c <= 'f' ? (c - 'a' + 1) * 100 : 700;
        return op <= 11 ? r + op * 1000 : -r;
    }

    // ---- two tables, and a switch inside a switch ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Nested(int x, int y)
    {
        switch (x)
        {
            case 0:
                switch (y)
                {
                    case 0: return 1;
                    case 1: return 2;
                    case 2: return 3;
                    case 3: return 4;
                    default: return 5;
                }
            case 1: return 10 + y;
            case 2:
                switch (y)
                {
                    case 10: return 21;
                    case 11: return 22;
                    case 12: return 23;
                    case 13: return 24;
                    case 14: return 25;
                    default: return 26;
                }
            case 3: return 30 - y;
            case 4: return 40;
            default: return y;
        }
    }

    [MethodImpl(NoOpt)]
    static int NestedRef(int x, int y)
    {
        if (x == 0) return y >= 0 && y <= 3 ? y + 1 : 5;
        if (x == 1) return 10 + y;
        if (x == 2) return y >= 10 && y <= 14 ? y + 11 : 26;
        if (x == 3) return 30 - y;
        if (x == 4) return 40;
        return y;
    }

    // ---- an interpreter loop ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Interpret(byte[] code, long a, long b)
    {
        for (int pc = 0; pc < code.Length; pc++)
        {
            switch ((Op)code[pc])
            {
                case Op.Nop: break;
                case Op.Add: a += b; break;
                case Op.Sub: a -= b; break;
                case Op.Mul: a *= b | 1; break;
                case Op.Xor: a ^= b; break;
                case Op.Shl: a <<= 3; break;
                case Op.Shr: a >>= 5; break;
                case Op.Neg: a = -a; break;
                case Op.Not: b = ~b; break;
                case Op.Swap: { long t = a; a = b; b = t; break; }
                case Op.Dup: b = a; break;
                case Op.Halt: return a ^ b;
                default: a += 1000; break;
            }
        }
        return a + b;
    }

    [MethodImpl(NoOpt)]
    static long InterpretRef(byte[] code, long a, long b)
    {
        for (int pc = 0; pc < code.Length; pc++)
        {
            int op = code[pc];
            if (op == 0) { }
            else if (op == 1) a += b;
            else if (op == 2) a -= b;
            else if (op == 3) a *= b | 1;
            else if (op == 4) a ^= b;
            else if (op == 5) a <<= 3;
            else if (op == 6) a >>= 5;
            else if (op == 7) a = -a;
            else if (op == 8) b = ~b;
            else if (op == 9) { long t = a; a = b; b = t; }
            else if (op == 10) b = a;
            else if (op == 11) return a ^ b;
            else a += 1000;
        }
        return a + b;
    }

    // ---- switches in try, catch, finally and a filter ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Pick(int x)
    {
        switch (x)
        {
            case 0: return 3;
            case 1: return 14;
            case 2: return 15;
            case 3: return 92;
            case 4: return 65;
            default: return 35;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Handlers(int x, int code)
    {
        int r = 0;
        try
        {
            try
            {
                switch (x)
                {
                    case 0: r = 1; break;
                    case 1: r = 2; Thrower(code); break;
                    case 2: r = 3; break;
                    case 3: r = 4; Thrower(code + 1); break;
                    case 4: r = 5; break;
                    default: r = 6; Thrower(code + 2); break;
                }
            }
            finally
            {
                switch (r)
                {
                    case 1: r += 10; break;
                    case 2: r += 20; break;
                    case 3: r += 30; break;
                    case 4: r += 40; break;
                    case 5: r += 50; break;
                    default: r += 60; break;
                }
            }
        }
        catch (E e) when (Pick(e.Code) != 35)
        {
            switch (e.Code)
            {
                case 0: r += 100; break;
                case 1: r += 200; break;
                case 2: r += 300; break;
                case 3: r += 400; break;
                case 4: r += 500; break;
                default: r += 600; break;
            }
        }
        catch (E)
        {
            r = -r;
        }
        return r;
    }

    [MethodImpl(NoOpt)]
    static int HandlersRef(int x, int code)
    {
        int r = x >= 0 && x <= 4 ? x + 1 : 6;
        int thrown = -1;
        if (x == 1) thrown = code;
        else if (x == 3) thrown = code + 1;
        else if (x < 0 || x > 4) thrown = code + 2;
        r += r * 10;
        if (thrown < 0) return r;
        return thrown <= 4 ? r + (thrown + 1) * 100 : -r;
    }

    // ---- generic methods, a lambda, a virtual method ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static T Choose<T>(T a, T b, T c, int op) where T : class
    {
        switch (op)
        {
            case 0: return a;
            case 1: return b;
            case 2: return c;
            case 3: return b;
            case 4: return a;
            default: return null;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Size<T>(T v, int op) where T : struct
    {
        switch (op)
        {
            case 0: return Unsafe.SizeOf<T>();
            case 1: return Unsafe.SizeOf<T>() * 3;
            case 2: return v.GetHashCode() & 0;
            case 3: return Unsafe.SizeOf<T>() + 100;
            case 4: return 4;
            default: return -Unsafe.SizeOf<T>();
        }
    }

    static readonly Func<int, int> lambda = x =>
    {
        switch (x)
        {
            case 0: return 100;
            case 1: return 81;
            case 2: return 64;
            case 3: return 49;
            case 4: return 36;
            case 5: return 25;
            default: return x;
        }
    };

    [MethodImpl(NoOpt)]
    static int LambdaRef(int x) => x >= 0 && x <= 5 ? (10 - x) * (10 - x) : x;

    [MethodImpl(NoOpt)]
    static int AreaRef(int kind, int x)
    {
        if (kind == 0) return x;
        if (kind == 1) return x * x;
        if (kind == 2) return x * 3 + 1;
        if (kind == 3) return x >> 1;
        if (kind == 4) return -x;
        if (kind == 5) return x ^ 0x5555;
        return 7;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Shape MakeShape(int kind) => kind == 0 ? new Table() : new Flat();

    // ---- strings and ranges ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Word(string s)
    {
        switch (s)
        {
            case "add": return 1;
            case "sub": return 2;
            case "mul": return 3;
            case "div": return 4;
            case "load": return 5;
            case "store": return 6;
            case "jump": return 7;
            case "branch": return 8;
            case "return": return 9;
            case "halt": return 10;
            default: return 0;
        }
    }

    static readonly string[] words = { "none", "add", "sub", "mul", "div", "load", "store", "jump", "branch", "return", "halt" };

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Ranges(int x) => x switch
    {
        < -100 => 1,
        >= -100 and < 0 => 2,
        0 => 3,
        1 or 2 or 3 => 4,
        4 => 5,
        5 => 6,
        6 => 7,
        7 => 8,
        >= 8 and <= 1000 => 9,
        _ => 10,
    };

    [MethodImpl(NoOpt)]
    static int RangesRef(int x)
    {
        if (x < -100) return 1;
        if (x < 0) return 2;
        if (x == 0) return 3;
        if (x <= 3) return 4;
        if (x <= 7) return x + 1;
        if (x <= 1000) return 9;
        return 10;
    }

    // ---- 160 cases ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Big(int x, long v)
    {
        long r;
        switch (x)
        {
            case 0: r = (v ^ 13) * 9 + 0; break; case 1: r = (v ^ 7932) * 811 + 1; break; case 2: r = (v ^ 5844) * 604 + 2; break;
            case 3: r = (v ^ 3756) * 397 + 3; break; case 4: r = (v ^ 1668) * 190 + 4; break; case 5: r = (v ^ 9587) * 992 + 5; break;
            case 6: r = (v ^ 7499) * 785 + 6; break; case 7: r = (v ^ 5411) * 578 + 7; break; case 8: r = (v ^ 3323) * 371 + 8; break;
            case 9: r = (v ^ 1235) * 164 + 9; break; case 10: r = (v ^ 9154) * 966 + 10; break; case 11: r = (v ^ 7066) * 759 + 11; break;
            case 12: r = (v ^ 4978) * 552 + 12; break; case 13: r = (v ^ 2890) * 345 + 13; break; case 14: r = (v ^ 802) * 138 + 14; break;
            case 15: r = (v ^ 8721) * 940 + 15; break; case 16: r = (v ^ 6633) * 733 + 16; break; case 17: r = (v ^ 4545) * 526 + 17; break;
            case 18: r = (v ^ 2457) * 319 + 18; break; case 19: r = (v ^ 369) * 112 + 19; break; case 20: r = (v ^ 8288) * 914 + 20; break;
            case 21: r = (v ^ 6200) * 707 + 21; break; case 22: r = (v ^ 4112) * 500 + 22; break; case 23: r = (v ^ 2024) * 293 + 23; break;
            case 24: r = (v ^ 9943) * 86 + 24; break; case 25: r = (v ^ 7855) * 888 + 25; break; case 26: r = (v ^ 5767) * 681 + 26; break;
            case 27: r = (v ^ 3679) * 474 + 27; break; case 28: r = (v ^ 1591) * 267 + 28; break; case 29: r = (v ^ 9510) * 60 + 29; break;
            case 30: r = (v ^ 7422) * 862 + 30; break; case 31: r = (v ^ 5334) * 655 + 31; break; case 32: r = (v ^ 3246) * 448 + 32; break;
            case 33: r = (v ^ 1158) * 241 + 33; break; case 34: r = (v ^ 9077) * 34 + 34; break; case 35: r = (v ^ 6989) * 836 + 35; break;
            case 36: r = (v ^ 4901) * 629 + 36; break; case 37: r = (v ^ 2813) * 422 + 37; break; case 38: r = (v ^ 725) * 215 + 38; break;
            case 39: r = (v ^ 8644) * 8 + 39; break; case 40: r = (v ^ 6556) * 810 + 40; break; case 41: r = (v ^ 4468) * 603 + 41; break;
            case 42: r = (v ^ 2380) * 396 + 42; break; case 43: r = (v ^ 292) * 189 + 43; break; case 44: r = (v ^ 8211) * 991 + 44; break;
            case 45: r = (v ^ 6123) * 784 + 45; break; case 46: r = (v ^ 4035) * 577 + 46; break; case 47: r = (v ^ 1947) * 370 + 47; break;
            case 48: r = (v ^ 9866) * 163 + 48; break; case 49: r = (v ^ 7778) * 965 + 49; break; case 50: r = (v ^ 5690) * 758 + 50; break;
            case 51: r = (v ^ 3602) * 551 + 51; break; case 52: r = (v ^ 1514) * 344 + 52; break; case 53: r = (v ^ 9433) * 137 + 53; break;
            case 54: r = (v ^ 7345) * 939 + 54; break; case 55: r = (v ^ 5257) * 732 + 55; break; case 56: r = (v ^ 3169) * 525 + 56; break;
            case 57: r = (v ^ 1081) * 318 + 57; break; case 58: r = (v ^ 9000) * 111 + 58; break; case 59: r = (v ^ 6912) * 913 + 59; break;
            case 60: r = (v ^ 4824) * 706 + 60; break; case 61: r = (v ^ 2736) * 499 + 61; break; case 62: r = (v ^ 648) * 292 + 62; break;
            case 63: r = (v ^ 8567) * 85 + 63; break; case 64: r = (v ^ 6479) * 887 + 64; break; case 65: r = (v ^ 4391) * 680 + 65; break;
            case 66: r = (v ^ 2303) * 473 + 66; break; case 67: r = (v ^ 215) * 266 + 67; break; case 68: r = (v ^ 8134) * 59 + 68; break;
            case 69: r = (v ^ 6046) * 861 + 69; break; case 70: r = (v ^ 3958) * 654 + 70; break; case 71: r = (v ^ 1870) * 447 + 71; break;
            case 72: r = (v ^ 9789) * 240 + 72; break; case 73: r = (v ^ 7701) * 33 + 73; break; case 74: r = (v ^ 5613) * 835 + 74; break;
            case 75: r = (v ^ 3525) * 628 + 75; break; case 76: r = (v ^ 1437) * 421 + 76; break; case 77: r = (v ^ 9356) * 214 + 77; break;
            case 78: r = (v ^ 7268) * 7 + 78; break; case 79: r = (v ^ 5180) * 809 + 79; break; case 80: r = (v ^ 3092) * 602 + 80; break;
            case 81: r = (v ^ 1004) * 395 + 81; break; case 82: r = (v ^ 8923) * 188 + 82; break; case 83: r = (v ^ 6835) * 990 + 83; break;
            case 84: r = (v ^ 4747) * 783 + 84; break; case 85: r = (v ^ 2659) * 576 + 85; break; case 86: r = (v ^ 571) * 369 + 86; break;
            case 87: r = (v ^ 8490) * 162 + 87; break; case 88: r = (v ^ 6402) * 964 + 88; break; case 89: r = (v ^ 4314) * 757 + 89; break;
            case 90: r = (v ^ 2226) * 550 + 90; break; case 91: r = (v ^ 138) * 343 + 91; break; case 92: r = (v ^ 8057) * 136 + 92; break;
            case 93: r = (v ^ 5969) * 938 + 93; break; case 94: r = (v ^ 3881) * 731 + 94; break; case 95: r = (v ^ 1793) * 524 + 95; break;
            case 96: r = (v ^ 9712) * 317 + 96; break; case 97: r = (v ^ 7624) * 110 + 97; break; case 98: r = (v ^ 5536) * 912 + 98; break;
            case 99: r = (v ^ 3448) * 705 + 99; break; case 100: r = (v ^ 1360) * 498 + 100; break; case 101: r = (v ^ 9279) * 291 + 101; break;
            case 102: r = (v ^ 7191) * 84 + 102; break; case 103: r = (v ^ 5103) * 886 + 103; break; case 104: r = (v ^ 3015) * 679 + 104; break;
            case 105: r = (v ^ 927) * 472 + 105; break; case 106: r = (v ^ 8846) * 265 + 106; break; case 107: r = (v ^ 6758) * 58 + 107; break;
            case 108: r = (v ^ 4670) * 860 + 108; break; case 109: r = (v ^ 2582) * 653 + 109; break; case 110: r = (v ^ 494) * 446 + 110; break;
            case 111: r = (v ^ 8413) * 239 + 111; break; case 112: r = (v ^ 6325) * 32 + 112; break; case 113: r = (v ^ 4237) * 834 + 113; break;
            case 114: r = (v ^ 2149) * 627 + 114; break; case 115: r = (v ^ 61) * 420 + 115; break; case 116: r = (v ^ 7980) * 213 + 116; break;
            case 117: r = (v ^ 5892) * 6 + 117; break; case 118: r = (v ^ 3804) * 808 + 118; break; case 119: r = (v ^ 1716) * 601 + 119; break;
            case 120: r = (v ^ 9635) * 394 + 120; break; case 121: r = (v ^ 7547) * 187 + 121; break; case 122: r = (v ^ 5459) * 989 + 122; break;
            case 123: r = (v ^ 3371) * 782 + 123; break; case 124: r = (v ^ 1283) * 575 + 124; break; case 125: r = (v ^ 9202) * 368 + 125; break;
            case 126: r = (v ^ 7114) * 161 + 126; break; case 127: r = (v ^ 5026) * 963 + 127; break; case 128: r = (v ^ 2938) * 756 + 128; break;
            case 129: r = (v ^ 850) * 549 + 129; break; case 130: r = (v ^ 8769) * 342 + 130; break; case 131: r = (v ^ 6681) * 135 + 131; break;
            case 132: r = (v ^ 4593) * 937 + 132; break; case 133: r = (v ^ 2505) * 730 + 133; break; case 134: r = (v ^ 417) * 523 + 134; break;
            case 135: r = (v ^ 8336) * 316 + 135; break; case 136: r = (v ^ 6248) * 109 + 136; break; case 137: r = (v ^ 4160) * 911 + 137; break;
            case 138: r = (v ^ 2072) * 704 + 138; break; case 139: r = (v ^ 9991) * 497 + 139; break; case 140: r = (v ^ 7903) * 290 + 140; break;
            case 141: r = (v ^ 5815) * 83 + 141; break; case 142: r = (v ^ 3727) * 885 + 142; break; case 143: r = (v ^ 1639) * 678 + 143; break;
            case 144: r = (v ^ 9558) * 471 + 144; break; case 145: r = (v ^ 7470) * 264 + 145; break; case 146: r = (v ^ 5382) * 57 + 146; break;
            case 147: r = (v ^ 3294) * 859 + 147; break; case 148: r = (v ^ 1206) * 652 + 148; break; case 149: r = (v ^ 9125) * 445 + 149; break;
            case 150: r = (v ^ 7037) * 238 + 150; break; case 151: r = (v ^ 4949) * 31 + 151; break; case 152: r = (v ^ 2861) * 833 + 152; break;
            case 153: r = (v ^ 773) * 626 + 153; break; case 154: r = (v ^ 8692) * 419 + 154; break; case 155: r = (v ^ 6604) * 212 + 155; break;
            case 156: r = (v ^ 4516) * 5 + 156; break; case 157: r = (v ^ 2428) * 807 + 157; break; case 158: r = (v ^ 340) * 600 + 158; break;
            case 159: r = (v ^ 8259) * 393 + 159; break;
            default: r = v - x; break;
        }
        return r ^ (v << 1);
    }

    [MethodImpl(NoOpt)]
    static long BigRef(int x, long v)
    {
        long r;
        if (x >= 0 && x < 160) r = (v ^ ((x * 7919 + 13) % 10007)) * ((x * 104729 + 7) % 1009 + 2) + x;
        else r = v - x;
        return r ^ (v << 1);
    }

    // ---- 64-bit constants ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ulong Constants(ulong x, int k)
    {
        ulong h = x ^ 0x9E3779B97F4A7C15UL;
        h = (h ^ (h >> 30)) * 0xBF58476D1CE4E5B9UL;
        h = (h ^ (h >> 27)) * 0x94D049BB133111EBUL;
        h ^= h >> 31;
        if ((k & 1) != 0) h += 0x0123456789ABCDEFUL;
        if ((k & 2) != 0) h ^= 0xFEDCBA9876543210UL;
        if ((k & 4) != 0) h -= 0x8000000000000001UL;
        if ((k & 8) != 0) h *= 0xD6E8FEB86659FD93UL;
        h = (h & 0x5555555555555555UL) | ((h >> 1) & 0x2AAAAAAAAAAAAAAAUL);
        return h + 0x7FFFFFFF80000001UL + (x & 0xFF00FF00FF00FF00UL);
    }

    static readonly ulong[] constants =
    {
        0x9E3779B97F4A7C15UL, 0xBF58476D1CE4E5B9UL, 0x94D049BB133111EBUL, 0x0123456789ABCDEFUL, 0xFEDCBA9876543210UL,
        0x8000000000000001UL, 0xD6E8FEB86659FD93UL, 0x5555555555555555UL, 0x2AAAAAAAAAAAAAAAUL, 0x7FFFFFFF80000001UL,
        0xFF00FF00FF00FF00UL,
    };

    [MethodImpl(NoOpt)]
    static ulong ConstantsRef(ulong x, int k)
    {
        ulong[] c = constants;
        ulong h = x ^ c[0];
        h = (h ^ (h >> 30)) * c[1];
        h = (h ^ (h >> 27)) * c[2];
        h ^= h >> 31;
        if ((k & 1) != 0) h += c[3];
        if ((k & 2) != 0) h ^= c[4];
        if ((k & 4) != 0) h -= c[5];
        if ((k & 8) != 0) h *= c[6];
        h = (h & c[7]) | ((h >> 1) & c[8]);
        return h + c[9] + (x & c[10]);
    }

    static readonly int[] Extremes =
    {
        int.MinValue, int.MinValue + 1, -65537, -65536, -32769, -32768, -257, -256, -129, -128, 255, 256, 257,
        32767, 32768, 65535, 65536, 0x10001, 0x10004, 0x7FFFFFFE, int.MaxValue,
    };

    static int Main()
    {
        bool four = true, seventeen = true, below = true, gaps = true, nested = true, big = true;
        bool ranges = true, lam = true, area = true, pick = true;
        Shape shape = MakeShape(0);
        if (MakeShape(1).Area(1, 5) != 0) fails++;

        for (int i = -40; i < 240; i++)
        {
            int x = i < 200 ? i : Extremes[i - 200 < Extremes.Length ? i - 200 : 0];
            four &= Four(x) == FourRef(x);
            seventeen &= Seventeen(x, i * 7919) == SeventeenRef(x, i * 7919);
            below &= BelowZero(x) == BelowZeroRef(x);
            gaps &= Gaps(x) == GapsRef(x) && Gaps(x + 900) == GapsRef(x + 900);
            ranges &= Ranges(x) == RangesRef(x) && Ranges(x * 9) == RangesRef(x * 9);
            lam &= lambda(x) == LambdaRef(x);
            area &= shape.Area(x, i) == AreaRef(x, i);
            big &= Big(x, i * 0x100000001B3L) == BigRef(x, i * 0x100000001B3L);
            for (int y = -2; y < 17; y++)
                nested &= Nested(x, y) == NestedRef(x, y);
        }
        Check("four cases", four);
        Check("seventeen cases", seventeen);
        Check("cases below zero", below);
        Check("cases with gaps", gaps);
        Check("nested", nested);
        Check("160 cases", big);
        Check("ranges", ranges);
        Check("lambda", lam);
        Check("virtual method", area);

        // Values with the low bits of a case and more above them.
        bool wide = true;
        for (int i = -2; i < 8; i++)
            for (int hi = 0; hi < 6; hi++)
            {
                long add = hi == 0 ? 0 : hi == 1 ? 0x100000000L : hi == 2 ? -0x100000000L : hi == 3 ? long.MinValue
                           : hi == 4 ? 0x80000000L : 0x7FFFFFFF00000000L;
                long x = i + add;
                ulong y = (ulong)(i + add);
                uint z = (uint)i + (hi == 4 ? 0x80000000u : 0);
                wide &= Wide(x, y, z) == WideRef(x, y, z);
            }
        Check("long, ulong, uint values", wide);

        bool narrow = true;
        for (int i = 0; i < 256; i++)
        {
            byte b = (byte)i;
            sbyte sb = (sbyte)(i - 128);
            char c = (char)('a' - 3 + (i & 15) + ((i & 16) != 0 ? 0x100 : 0));
            Op op = (Op)(i % 14);
            narrow &= Narrow(b, sb, c, op) == NarrowRef(b, sb, c, (int)op);
        }
        Check("byte, sbyte, char, enum values", narrow);

        bool interp = true;
        byte[] code = new byte[40];
        uint seed = 777;
        for (int run = 0; run < 30; run++)
        {
            for (int i = 0; i < code.Length; i++)
            {
                seed = seed * 1664525 + 1013904223;
                code[i] = (byte)((seed >> 16) % 13);
                if (code[i] == (byte)Op.Halt && i < run) code[i] = 0;
            }
            interp &= Interpret(code, run * 0x9E3779B1L, seed) == InterpretRef(code, run * 0x9E3779B1L, seed);
        }
        Check("interpreter loop", interp);

        bool handlers = true;
        for (int x = -1; x < 7; x++)
            for (int code2 = -3; code2 < 6; code2++)
                handlers &= Handlers(x, code2) == HandlersRef(x, code2);
        for (int x = -2; x < 8; x++) pick &= Pick(x) == (x == 0 ? 3 : x == 1 ? 14 : x == 2 ? 15 : x == 3 ? 92 : x == 4 ? 65 : 35);
        Check("try, catch, finally, filter", handlers && pick);

        bool generic = true;
        string sa = "a", sbb = "b", sc = "c";
        object oa = new object(), ob = new object(), oc = new object();
        for (int op = -1; op < 7; op++)
        {
            string ws = op == 0 || op == 4 ? sa : op == 1 || op == 3 ? sbb : op == 2 ? sc : null;
            object wo = op == 0 || op == 4 ? oa : op == 1 || op == 3 ? ob : op == 2 ? oc : null;
            generic &= ReferenceEquals(Choose(sa, sbb, sc, op), ws) && ReferenceEquals(Choose(oa, ob, oc, op), wo);
            int wi = op == 0 ? 4 : op == 1 ? 12 : op == 2 ? 0 : op == 3 ? 104 : op == 4 ? 4 : -4;
            int wl = op == 0 ? 8 : op == 1 ? 24 : op == 2 ? 0 : op == 3 ? 108 : op == 4 ? 4 : -8;
            generic &= Size(5, op) == wi && Size(5L, op) == wl;
        }
        Check("generic methods", generic);

        bool strings = true;
        for (int i = 0; i < words.Length; i++)
        {
            // Built at run time, so that the comparison is by content.
            string s = string.Concat(words[i].Substring(0, 1), words[i].Substring(1));
            strings &= Word(s) == i;
            strings &= Word(s + "x") == 0;
        }
        Check("strings", strings && Word("") == 0);

        bool consts = true;
        for (int k = 0; k < 16; k++)
            for (int i = 0; i < 8; i++)
            {
                ulong x = (ulong)i * 0x0101010101010101UL + (ulong)k;
                consts &= Constants(x, k) == ConstantsRef(x, k);
            }
        Check("64-bit constants", consts);

        Console.WriteLine(fails == 0 ? "ALL PASS" : "FAILED " + fails);
        return fails;
    }
}
