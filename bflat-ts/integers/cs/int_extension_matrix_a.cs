// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// 32-bit values in 64-bit registers: every way to make one against every
// way to use one.
//
// A RISC-V register is 64 bits wide and a 32-bit value is kept in it
// sign-extended. The compiler knows which instructions leave a register
// that way (the W forms, lw/lh/lb, a narrowing cast, a compare, logic on
// two extended values, the result of a managed call) and drops the sext.w
// and the widening cast wherever the operand is one of those. It also knows
// which values are never negative, where a zero extension is free too, and
// which 64-bit values already are the sign extension of their low half, so
// that narrowing them is a copy. What can go wrong: a producer that is on
// one of those lists but leaves other bits in the upper half (a 64-bit
// instruction chosen for a 32-bit operation, a load that zero-extends), and
// a user that takes its width from the operand once the cast is gone (mulw
// for a 64-bit product, clzw for a 64-bit count, sw for a 64-bit stack
// argument, a compare of the low halves only).
//
// Here every producer is a small struct with a 32-bit expression P and the
// same value R computed by 64-bit arithmetic only. Every user is a generic
// method that inlines P and then widens it, compares it, indexes with it,
// returns it, keeps it across a call, and so on; its expected value is
// computed from R by code compiled without optimization. Producers and
// users are combined freely, on arguments that put ones and zeroes at bit
// 31 and above.
//
// A failed check prints one line with the producer, the user and the
// arguments (the first four of a producer only, all of them when built
// with -d VERBOSE); the run ends with ALL PASS or a FAIL
// count, and the guest returns 1 when anything failed.
using System;
using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

interface IProd
{
    static abstract int P(int a, int b, long c);
    // The same value from 64-bit arithmetic; only the low 32 bits count.
    static abstract long R(long a, long b, long c);
}

class Holder
{
    public long Pad;
    public int I;
    public uint U;
    public short S;
    public ushort C;
    public sbyte SB;
    public byte B;
}

struct Inner { public int X; public int Y; }

abstract class Shape { public abstract int Low(long c); }
sealed class ShapeA : Shape { [MethodImpl(MethodImplOptions.NoInlining)] public override int Low(long c) => (int)c; }
sealed class ShapeB : Shape { public override int Low(long c) => 1; }

unsafe class Program
{
    const MethodImplOptions AI = MethodImplOptions.AggressiveInlining;
    const MethodImplOptions NI = MethodImplOptions.NoInlining;
    const MethodImplOptions NoOpt = MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization;

    static int fails;
    static int checks;
    static int shown;      // failures printed for the current producer
#if VERBOSE
    const int MaxShown = 100000;
#else
    const int MaxShown = 4;
#endif

    // ---- data the producers load ----

    static readonly long[] Source =
    {
        0, 1, -1, 0x7FFFFFFF, -0x80000000L, 0x80, 0xFF, 0x7FFF, 0x8000, 0xFFFF, -0x8000, -0x81,
        0x12345678, -0x12345678, 0x40000000, -0x40000001,
    };

    static int[] ArrI;
    static uint[] ArrU;
    static short[] ArrS;
    static ushort[] ArrC;
    static sbyte[] ArrSB;
    static byte[] ArrB;
    static Inner[] ArrIn;
    static Holder Obj;
    static int Cell;
    static long LongCell;
    static Holder ProdObj;  // the producers' own state
    static int ProdCell;
    static long ProdLong;
    static byte* Raw;       // the Source values as ints at odd addresses
    static long[] Tab;
    static long* BaseL;
    static byte* BaseB;
    static Shape Virt;
    static Func<long, int> Dlg;
    static delegate*<long, int> Ptr;

    [MethodImpl(NoOpt)]
    static void Setup(byte* raw, long* mem)
    {
        ArrI = new int[16]; ArrU = new uint[16]; ArrS = new short[16]; ArrC = new ushort[16];
        ArrSB = new sbyte[16]; ArrB = new byte[16]; ArrIn = new Inner[16];
        Raw = raw;
        for (int i = 0; i < 16; i++)
        {
            long v = Source[i];
            ArrI[i] = (int)v; ArrU[i] = (uint)v; ArrS[i] = (short)v; ArrC[i] = (ushort)v;
            ArrSB[i] = (sbyte)v; ArrB[i] = (byte)v;
            ArrIn[i].X = (int)~v; ArrIn[i].Y = (int)v;
            for (int j = 0; j < 4; j++) raw[i * 8 + 1 + j] = (byte)(v >> (8 * j));
        }
        Obj = new Holder();
        ProdObj = new Holder();
        Tab = new long[16];
        for (int i = 0; i < 16; i++) Tab[i] = (i + 1) * 0x0101010101010101L;
        BaseL = mem; BaseB = (byte*)mem;
        Virt = MakeShape(0);
        if (MakeShape(1).Low(0) != 1) fails++;
        Dlg = RetLow;
        Ptr = &RetLow;
    }

    [MethodImpl(NI)] static Shape MakeShape(int k) => k == 0 ? new ShapeA() : new ShapeB();
    [MethodImpl(NI)] static int RetLow(long c) => (int)c;
    [MethodImpl(NI)] static void Opaque() { Cell ^= 0; }

    // ---- reference arithmetic, compiled without optimization ----

    [MethodImpl(NoOpt)]
    static long S32(long v)
    {
        v &= 0xFFFFFFFFL;
        if (v >= 0x80000000L) v -= 0x100000000L;
        return v;
    }

    [MethodImpl(NoOpt)] static long Z32(long v) => v & 0xFFFFFFFFL;

    [MethodImpl(NoOpt)]
    static long S16(long v)
    {
        v &= 0xFFFF;
        if (v >= 0x8000) v -= 0x10000;
        return v;
    }

    [MethodImpl(NoOpt)]
    static long S8(long v)
    {
        v &= 0xFF;
        if (v >= 0x80) v -= 0x100;
        return v;
    }

    [MethodImpl(NoOpt)]
    static long Pop(long v, int bits)
    {
        long n = 0;
        for (int i = 0; i < bits; i++) n += (v >> i) & 1;
        return n;
    }

    [MethodImpl(NoOpt)]
    static long Lzc(long v, int bits)
    {
        long n = 0;
        for (int i = bits - 1; i >= 0; i--)
        {
            if (((v >> i) & 1) != 0) break;
            n++;
        }
        return n;
    }

    [MethodImpl(NoOpt)]
    static long Tzc(long v, int bits)
    {
        long n = 0;
        for (int i = 0; i < bits; i++)
        {
            if (((v >> i) & 1) != 0) break;
            n++;
        }
        return n;
    }

    [MethodImpl(NoOpt)]
    static long Swap(long v, int bytes)
    {
        long r = 0;
        for (int i = 0; i < bytes; i++) r |= ((v >> (8 * i)) & 0xFF) << (8 * (bytes - 1 - i));
        return r;
    }

    // ---- producers ----

    struct Add : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a + b; public static long R(long a, long b, long c) => a + b; }
    struct Sub : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a - b; public static long R(long a, long b, long c) => a - b; }
    struct Mul : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a * b; public static long R(long a, long b, long c) => a * b; }
    struct Div : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a / ((b & 0xFF) + 1); public static long R(long a, long b, long c) => a / ((b & 0xFF) + 1); }
    struct DivNeg : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a / (-(b & 0xFF) - 2); public static long R(long a, long b, long c) => a / (-(b & 0xFF) - 2); }
    struct Mod : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a % ((b & 0xFF) + 2); public static long R(long a, long b, long c) => a % ((b & 0xFF) + 2); }
    struct DivU : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)((uint)a / (uint)((b & 0xFF) + 1)); public static long R(long a, long b, long c) => Z32(a) / ((b & 0xFF) + 1); }
    struct ModU : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)((uint)a % ((uint)b | 0x80000000u)); public static long R(long a, long b, long c) => Z32(a) % (Z32(b) | 0x80000000L); }
    struct Shl : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a << b; public static long R(long a, long b, long c) => a << (int)(b & 31); }
    struct Sar : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a >> b; public static long R(long a, long b, long c) => a >> (int)(b & 31); }
    struct Shr : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)((uint)a >> b); public static long R(long a, long b, long c) => Z32(a) >> (int)(b & 31); }
    struct Shl3 : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a << 3; public static long R(long a, long b, long c) => a << 3; }
    struct Shl1 : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a << 1; public static long R(long a, long b, long c) => a << 1; }
    struct Shl2 : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a << 2; public static long R(long a, long b, long c) => a << 2; }
    struct Mul2 : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a * 2; public static long R(long a, long b, long c) => a * 2; }
    struct Mul4 : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (a + b) * 4; public static long R(long a, long b, long c) => (a + b) * 4; }
    struct Mul8 : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a * 8; public static long R(long a, long b, long c) => a * 8; }
    struct UShl2 : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)((uint)a << 2); public static long R(long a, long b, long c) => a << 2; }
    struct Shl31 : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a << 31; public static long R(long a, long b, long c) => a << 31; }
    struct Sar7 : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a >> 7; public static long R(long a, long b, long c) => a >> 7; }
    struct Sar31 : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a >> 31; public static long R(long a, long b, long c) => a >> 31; }
    struct Shr1 : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)((uint)a >> 1); public static long R(long a, long b, long c) => Z32(a) >> 1; }
    struct Shr31 : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)((uint)a >> 31); public static long R(long a, long b, long c) => Z32(a) >> 31; }
    struct ShlShr : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)((uint)(a << 8) >> 8); public static long R(long a, long b, long c) => a & 0xFFFFFF; }
    struct ShlSar : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (a << 20) >> 20; public static long R(long a, long b, long c) => ((a & 0xFFF) ^ 0x800) - 0x800; }
    struct Neg : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => -a; public static long R(long a, long b, long c) => -a; }
    struct Not : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => ~a; public static long R(long a, long b, long c) => ~a; }
    struct And : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a & b; public static long R(long a, long b, long c) => a & b; }
    struct Or : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a | b; public static long R(long a, long b, long c) => a | b; }
    struct Xor : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a ^ b; public static long R(long a, long b, long c) => a ^ b; }
    struct AndNot : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a & ~b; public static long R(long a, long b, long c) => a & ~b; }
    struct OrNot : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a | ~b; public static long R(long a, long b, long c) => a | ~b; }
    struct XorNot : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => ~(a ^ b); public static long R(long a, long b, long c) => ~(a ^ b); }
    struct AndMax : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a & 0x7FFFFFFF; public static long R(long a, long b, long c) => a & 0x7FFFFFFF; }
    struct AndMin : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a & int.MinValue; public static long R(long a, long b, long c) => a & 0x80000000L; }
    struct OrMin : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a | int.MinValue; public static long R(long a, long b, long c) => a | 0x80000000L; }
    struct XorMin : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a ^ int.MinValue; public static long R(long a, long b, long c) => a ^ 0x80000000L; }
    struct XorM1 : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a ^ -1; public static long R(long a, long b, long c) => ~a; }
    struct AndFF : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a & 0xFF; public static long R(long a, long b, long c) => a & 0xFF; }
    struct AndFFFF : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a & 0xFFFF; public static long R(long a, long b, long c) => a & 0xFFFF; }
    struct AndHigh : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a & unchecked((int)0xFFFF0000); public static long R(long a, long b, long c) => a & 0xFFFF0000L; }
    struct AndUint : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)((uint)a & 0xFFFFFFF0u); public static long R(long a, long b, long c) => a & 0xFFFFFFF0L; }
    struct OrSmall : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a | 0x7FF; public static long R(long a, long b, long c) => a | 0x7FF; }
    struct XorBig : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a ^ 0x7FFFF800; public static long R(long a, long b, long c) => a ^ 0x7FFFF800; }
    struct Rol : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)BitOperations.RotateLeft((uint)a, b); public static long R(long a, long b, long c) => (Z32(a) << (int)(b & 31)) | (Z32(a) >> (int)(32 - (b & 31))); }
    struct Ror : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)BitOperations.RotateRight((uint)a, b); public static long R(long a, long b, long c) => (Z32(a) >> (int)(b & 31)) | (Z32(a) << (int)(32 - (b & 31))); }
    struct Rol1 : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)BitOperations.RotateLeft((uint)a, 1); public static long R(long a, long b, long c) => (Z32(a) << 1) | (Z32(a) >> 31); }
    struct Ror1 : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)BitOperations.RotateRight((uint)a, 1); public static long R(long a, long b, long c) => (Z32(a) >> 1) | (Z32(a) << 31); }
    struct RolHand : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (a << 5) | (int)((uint)a >> 27); public static long R(long a, long b, long c) => (Z32(a) << 5) | (Z32(a) >> 27); }
    struct Mul3 : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a * 3; public static long R(long a, long b, long c) => a * 3; }
    struct Mul5 : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a * 5; public static long R(long a, long b, long c) => a * 5; }
    struct Mul9 : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a * 9; public static long R(long a, long b, long c) => a * 9; }
    struct Mul7 : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a * 7; public static long R(long a, long b, long c) => a * 7; }
    struct Mul10 : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a * 10; public static long R(long a, long b, long c) => a * 10; }
    struct Mul16 : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a * 16; public static long R(long a, long b, long c) => a * 16; }
    struct MulBig : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a * 0x10001; public static long R(long a, long b, long c) => a * 0x10001; }
    struct MulNeg : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a * -3; public static long R(long a, long b, long c) => a * -3; }
    struct Mul3U : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)((uint)a * 3u); public static long R(long a, long b, long c) => a * 3; }
    struct AddSh1 : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a + b * 2; public static long R(long a, long b, long c) => a + b * 2; }
    struct AddSh2 : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a + (b << 2); public static long R(long a, long b, long c) => a + b * 4; }
    struct AddSh3 : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => b * 8 + a; public static long R(long a, long b, long c) => a + b * 8; }
    struct AddSh2U : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)((uint)a + ((uint)b << 2)); public static long R(long a, long b, long c) => a + b * 4; }
    struct AddC7FF : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a + 0x7FF; public static long R(long a, long b, long c) => a + 0x7FF; }
    struct AddC800 : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a + 0x800; public static long R(long a, long b, long c) => a + 0x800; }
    struct SubC801 : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a - 0x801; public static long R(long a, long b, long c) => a - 0x801; }
    struct AddCBig : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a + 0x7FFFF800; public static long R(long a, long b, long c) => a + 0x7FFFF800; }
    struct AddCMin : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a + int.MinValue; public static long R(long a, long b, long c) => a + 0x80000000L; }
    struct Add1 : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a + 1; public static long R(long a, long b, long c) => a + 1; }
    struct AddU : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)((uint)a + (uint)b); public static long R(long a, long b, long c) => a + b; }

    // 64-bit values narrowed.
    struct Low : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)c; public static long R(long a, long b, long c) => c; }
    struct High : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)(c >> 32); public static long R(long a, long b, long c) => c >> 32; }
    struct HighU : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)((ulong)c >> 32); public static long R(long a, long b, long c) => c >> 32; }
    struct HighUU : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)(uint)((ulong)c >> 32); public static long R(long a, long b, long c) => c >> 32; }
    struct Sar31L : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)(c >> 31); public static long R(long a, long b, long c) => c >> 31; }
    struct Shr31L : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)((ulong)c >> 31); public static long R(long a, long b, long c) => c >> 31; }
    struct Shr33L : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)((ulong)c >> 33); public static long R(long a, long b, long c) => (c >> 33) & 0x7FFFFFFF; }
    struct Sar40L : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)(c >> 40); public static long R(long a, long b, long c) => c >> 40; }
    struct Shr63L : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)((ulong)c >> 63); public static long R(long a, long b, long c) => (c >> 63) & 1; }
    struct ShrVarL : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)((ulong)c >> b); public static long R(long a, long b, long c) => (long)((ulong)c >> (int)(b & 63)); }
    struct SarVarL : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)(c >> b); public static long R(long a, long b, long c) => c >> (int)(b & 63); }
    struct MaskLow : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)(c & 0xFFFFFFFFL); public static long R(long a, long b, long c) => c; }
    struct MaskMax : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)(c & 0x7FFFFFFFL); public static long R(long a, long b, long c) => c & 0x7FFFFFFF; }
    struct MaskMin : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)(c & 0x80000000L); public static long R(long a, long b, long c) => c & 0x80000000L; }
    struct MaskNeg : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)(c & -0x80000000L); public static long R(long a, long b, long c) => c & 0x80000000L; }
    struct MaskVar : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)(c & (long)(a & 0x7FFFFFFF)); public static long R(long a, long b, long c) => c & a & 0x7FFFFFFF; }
    struct MaskInt : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)(c & (long)a); public static long R(long a, long b, long c) => c & a; }
    struct MaskUint : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)(c & (long)(uint)a); public static long R(long a, long b, long c) => c & a; }
    struct OrInts : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)((long)a | (long)b); public static long R(long a, long b, long c) => a | b; }
    struct XorInts : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)((long)a ^ (long)b); public static long R(long a, long b, long c) => a ^ b; }
    struct XorUints : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)((long)(uint)a ^ (long)(uint)b); public static long R(long a, long b, long c) => a ^ b; }
    struct OrIntUint : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)((long)a | (long)(uint)b); public static long R(long a, long b, long c) => a | b; }
    struct NotInt : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)~(long)a; public static long R(long a, long b, long c) => ~a; }
    struct NotUint : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)~(long)(uint)a; public static long R(long a, long b, long c) => ~a; }
    struct OrHighs : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)((c >> 32) | (long)a); public static long R(long a, long b, long c) => (c >> 32) | a; }
    struct XorShr33 : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)(((ulong)c >> 33) ^ (ulong)(uint)(a & 0xFFFF)); public static long R(long a, long b, long c) => ((c >> 33) & 0x7FFFFFFF) ^ (a & 0xFFFF); }
    struct OrConst : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)((long)a | 0x80000000L); public static long R(long a, long b, long c) => a | 0x80000000L; }
    struct OrConstNeg : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)((long)a | -0x80000000L); public static long R(long a, long b, long c) => a | 0x80000000L; }
    struct XorConstBig : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)((long)a ^ 0x100000000L); public static long R(long a, long b, long c) => a; }
    struct MinL : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)Math.Min((long)a, (long)b); public static long R(long a, long b, long c) => a < b ? a : b; }
    struct MaxL : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)Math.Max((long)a, (long)b); public static long R(long a, long b, long c) => a > b ? a : b; }
    struct MinUL : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)Math.Min((ulong)(long)a, (ulong)(long)b); public static long R(long a, long b, long c) => (ulong)a < (ulong)b ? a : b; }
    struct MaxUL : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)Math.Max((ulong)(uint)a, (ulong)(uint)b); public static long R(long a, long b, long c) => Z32(a) > Z32(b) ? a : b; }
    struct MinMixed : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)Math.Min((long)a, c); public static long R(long a, long b, long c) => a < c ? a : c; }
    struct MaxMixedU : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)Math.Max((ulong)(uint)a, (ulong)c >> 33); public static long R(long a, long b, long c) => Z32(a) > ((c >> 33) & 0x7FFFFFFF) ? a : (c >> 33) & 0x7FFFFFFF; }
    struct PopL : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => BitOperations.PopCount((ulong)c); public static long R(long a, long b, long c) => Pop(c, 64); }
    struct LzcL : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => BitOperations.LeadingZeroCount((ulong)c); public static long R(long a, long b, long c) => Lzc(c, 64); }
    struct TzcL : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => BitOperations.TrailingZeroCount(c); public static long R(long a, long b, long c) => Tzc(c, 64); }
    struct SumL : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)(c + a); public static long R(long a, long b, long c) => c + a; }
    struct ProdL : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)(c * b); public static long R(long a, long b, long c) => c * b; }
    struct LowU : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)(uint)c; public static long R(long a, long b, long c) => c; }
    struct LowZext : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)(ulong)(uint)c; public static long R(long a, long b, long c) => c; }
    struct LowNint : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)(nint)c; public static long R(long a, long b, long c) => c; }
    struct ShlL : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)(c << 31); public static long R(long a, long b, long c) => c << 31; }
    struct WidenBack : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)((long)a + (long)b); public static long R(long a, long b, long c) => a + b; }
    struct WidenBackU : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)((long)(uint)a + (long)(uint)b); public static long R(long a, long b, long c) => a + b; }
    struct MulL : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)((long)a * (long)b); public static long R(long a, long b, long c) => a * b; }
    struct MulHighL : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)(((long)a * (long)b) >> 32); public static long R(long a, long b, long c) => (a * b) >> 32; }
    struct MulHighUL : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)(((ulong)(uint)a * (ulong)(uint)b) >> 32); public static long R(long a, long b, long c) => (long)((ulong)(Z32(a) * Z32(b)) >> 32); }

    // Small types.
    struct ToShort : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (short)a; public static long R(long a, long b, long c) => S16(a); }
    struct ToUShort : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (ushort)a; public static long R(long a, long b, long c) => a & 0xFFFF; }
    struct ToSByte : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (sbyte)a; public static long R(long a, long b, long c) => S8(a); }
    struct ToByte : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (byte)a; public static long R(long a, long b, long c) => a & 0xFF; }
    struct LongToShort : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (short)c; public static long R(long a, long b, long c) => S16(c); }
    struct LongToByte : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (byte)(c >> 28); public static long R(long a, long b, long c) => (c >> 28) & 0xFF; }
    struct ShortSum : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (short)(a + b); public static long R(long a, long b, long c) => S16(a + b); }
    struct UShortMul : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (ushort)a * (ushort)b; public static long R(long a, long b, long c) => (a & 0xFFFF) * (b & 0xFFFF); }
    struct ByteShl : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (byte)a << 24; public static long R(long a, long b, long c) => (a & 0xFF) << 24; }
    struct UShortShl : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (ushort)a << 16; public static long R(long a, long b, long c) => (a & 0xFFFF) << 16; }

    // Compares and selects.
    struct Lt : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a < b ? 1 : 0; public static long R(long a, long b, long c) => a < b ? 1 : 0; }
    struct LtNeg : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => -(a < b ? 1 : 0); public static long R(long a, long b, long c) => a < b ? -1 : 0; }
    struct Select : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a < b ? a : b; public static long R(long a, long b, long c) => a < b ? a : b; }
    struct SelectU : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (uint)a < (uint)b ? b : (int)c; public static long R(long a, long b, long c) => Z32(a) < Z32(b) ? b : c; }
    struct MinI : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => Math.Min(a, b); public static long R(long a, long b, long c) => a < b ? a : b; }
    struct MaxI : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => Math.Max(a, b); public static long R(long a, long b, long c) => a > b ? a : b; }
    struct MaxU : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)Math.Max((uint)a, (uint)b); public static long R(long a, long b, long c) => Z32(a) > Z32(b) ? a : b; }
    struct MinU : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)Math.Min((uint)a, (uint)b); public static long R(long a, long b, long c) => Z32(a) < Z32(b) ? a : b; }
    struct MaxLowC : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => Math.Max(a, (int)c); public static long R(long a, long b, long c) => a > S32(c) ? a : c; }
    struct AbsI : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a == int.MinValue ? a : Math.Abs(a); public static long R(long a, long b, long c) => a < 0 ? -a : a; }

    // Bit counts and byte swaps.
    struct PopI : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => BitOperations.PopCount((uint)a); public static long R(long a, long b, long c) => Pop(a, 32); }
    struct LzcI : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => BitOperations.LeadingZeroCount((uint)a); public static long R(long a, long b, long c) => Lzc(a, 32); }
    struct TzcI : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => BitOperations.TrailingZeroCount(a); public static long R(long a, long b, long c) => Tzc(a, 32); }
    struct Log2I : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => BitOperations.Log2((uint)a); public static long R(long a, long b, long c) => 31 - Lzc(a | 1, 32); }
    struct Log2L : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => BitOperations.Log2((ulong)c); public static long R(long a, long b, long c) => 63 - Lzc(c | 1, 64); }
    struct LzcWide : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => BitOperations.LeadingZeroCount((ulong)(long)a); public static long R(long a, long b, long c) => Lzc(a, 64); }
    struct LzcWideU : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => BitOperations.LeadingZeroCount((ulong)(uint)a); public static long R(long a, long b, long c) => Lzc(Z32(a), 64); }
    struct PopWide : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => BitOperations.PopCount((ulong)(long)(a + b)); public static long R(long a, long b, long c) => Pop(S32(a + b), 64); }
    struct TzcWide : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => BitOperations.TrailingZeroCount((long)(a & b)); public static long R(long a, long b, long c) => Tzc(a & b, 64); }
    struct SwapI : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => BinaryPrimitives.ReverseEndianness(a); public static long R(long a, long b, long c) => Swap(a, 4); }
    struct SwapU : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)BinaryPrimitives.ReverseEndianness((uint)a); public static long R(long a, long b, long c) => Swap(a, 4); }
    struct SwapS : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => BinaryPrimitives.ReverseEndianness((short)a); public static long R(long a, long b, long c) => S16(Swap(a, 2)); }
    struct SwapC : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => BinaryPrimitives.ReverseEndianness((ushort)a); public static long R(long a, long b, long c) => Swap(a, 2); }
    struct SwapLLow : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)BinaryPrimitives.ReverseEndianness(c); public static long R(long a, long b, long c) => Swap(c, 8); }
    struct SwapWide : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)(BinaryPrimitives.ReverseEndianness((long)(a + b)) >> 32); public static long R(long a, long b, long c) => Swap(S32(a + b), 8) >> 32; }
    struct SwapSum : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => BinaryPrimitives.ReverseEndianness(a + b); public static long R(long a, long b, long c) => Swap(a + b, 4); }

    // Single bits.
    struct BitSet : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a | (1 << b); public static long R(long a, long b, long c) => a | (1L << (int)(b & 31)); }
    struct BitFlip : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a ^ (1 << b); public static long R(long a, long b, long c) => a ^ (1L << (int)(b & 31)); }
    struct BitClear : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a & ~(1 << b); public static long R(long a, long b, long c) => a & ~(1L << (int)(b & 31)); }
    struct BitGet : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (a >> b) & 1; public static long R(long a, long b, long c) => (a >> (int)(b & 31)) & 1; }
    struct BitSet31 : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a | (1 << 31); public static long R(long a, long b, long c) => a | 0x80000000L; }
    struct BitFlip31 : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a ^ (1 << 31); public static long R(long a, long b, long c) => a ^ 0x80000000L; }
    struct BitClear31 : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a & ~(1 << 31); public static long R(long a, long b, long c) => a & 0x7FFFFFFF; }
    struct BitSet7 : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => a | (1 << 20); public static long R(long a, long b, long c) => a | (1 << 20); }
    struct BitSetL : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)((c | (1L << b)) >> 31); public static long R(long a, long b, long c) => (c | (1L << (int)(b & 63))) >> 31; }
    struct BitFlipL : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)((c ^ (1L << b)) >> 30); public static long R(long a, long b, long c) => (c ^ (1L << (int)(b & 63))) >> 30; }
    struct BitClearL : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)((c & ~(1L << b)) >> 32); public static long R(long a, long b, long c) => (c & ~(1L << (int)(b & 63))) >> 32; }
    struct BitGetL : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)((c >> b) & 1); public static long R(long a, long b, long c) => (c >> (int)(b & 63)) & 1; }
    struct BitSetWide : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)(((long)(a + 1) | (1L << b)) >> 28); public static long R(long a, long b, long c) => (S32(a + 1) | (1L << (int)(b & 63))) >> 28; }

    // Loads.
    struct LoadI : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => ArrI[a & 15]; public static long R(long a, long b, long c) => Source[a & 15]; }
    struct LoadU : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)ArrU[a & 15]; public static long R(long a, long b, long c) => Source[a & 15]; }
    struct LoadS : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => ArrS[a & 15]; public static long R(long a, long b, long c) => S16(Source[a & 15]); }
    struct LoadC : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => ArrC[a & 15]; public static long R(long a, long b, long c) => Source[a & 15] & 0xFFFF; }
    struct LoadSB : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => ArrSB[a & 15]; public static long R(long a, long b, long c) => S8(Source[a & 15]); }
    struct LoadB : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => ArrB[a & 15]; public static long R(long a, long b, long c) => Source[a & 15] & 0xFF; }
    struct LoadIn : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => ArrIn[a & 15].Y; public static long R(long a, long b, long c) => Source[a & 15]; }
    struct LoadInX : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => ArrIn[a & 15].X; public static long R(long a, long b, long c) => ~Source[a & 15]; }
    struct LoadInCopy : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) { Inner s = ArrIn[a & 15]; return s.X ^ s.Y ^ s.Y; } public static long R(long a, long b, long c) => ~Source[a & 15]; }
    struct LoadRaw : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => Unsafe.ReadUnaligned<int>(Raw + (a & 15) * 8 + 1); public static long R(long a, long b, long c) => Source[a & 15]; }
    struct LoadRawU : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => (int)Unsafe.ReadUnaligned<uint>(Raw + (a & 15) * 8 + 1); public static long R(long a, long b, long c) => Source[a & 15]; }
    struct LoadPtr : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) { fixed (int* p = ArrI) return p[a & 15]; } public static long R(long a, long b, long c) => Source[a & 15]; }
    struct LoadPtrU : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) { fixed (uint* p = ArrU) return (int)p[a & 15]; } public static long R(long a, long b, long c) => Source[a & 15]; }
    struct LoadField : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) { Holder h = ProdObj; h.I = a; Opaque(); return h.I; } public static long R(long a, long b, long c) => a; }
    struct LoadFieldU : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) { Holder h = ProdObj; h.U = (uint)a; Opaque(); return (int)h.U; } public static long R(long a, long b, long c) => a; }
    struct LoadFieldS : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) { Holder h = ProdObj; h.S = (short)a; Opaque(); return h.S; } public static long R(long a, long b, long c) => S16(a); }
    struct LoadFieldC : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) { Holder h = ProdObj; h.C = (ushort)a; Opaque(); return h.C; } public static long R(long a, long b, long c) => a & 0xFFFF; }
    struct LoadFieldB : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) { Holder h = ProdObj; h.B = (byte)a; h.SB = (sbyte)b; Opaque(); return h.B * 256 + h.SB; } public static long R(long a, long b, long c) => (a & 0xFF) * 256 + S8(b); }
    struct LoadStatic : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) { ProdCell = a; Opaque(); return ProdCell; } public static long R(long a, long b, long c) => a; }
    struct LoadLowOfLong : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) { ProdLong = c; Opaque(); return (int)ProdLong; } public static long R(long a, long b, long c) => c; }
    struct LoadHighOfLong : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) { ProdLong = c; Opaque(); fixed (long* p = &ProdLong) return ((int*)p)[1]; } public static long R(long a, long b, long c) => c >> 32; }
    struct LoadLocal : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) { int v = a; Touch(&v); return v; } public static long R(long a, long b, long c) => a + 1; }
    struct LoadLocalU : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) { uint v = (uint)a; Touch((int*)&v); return (int)v; } public static long R(long a, long b, long c) => a + 1; }
    struct LoadLocalLong : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) { long v = c; TouchL(&v); return (int)v; } public static long R(long a, long b, long c) => c + 0x100000001L; }
    struct LoadLocalFld : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) { Inner v; v.X = a; v.Y = b; Touch(&v.Y); return v.Y; } public static long R(long a, long b, long c) => b + 1; }
    struct LoadArrLen : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => ArrI.Length - (a & 31); public static long R(long a, long b, long c) => 16 - (a & 31); }
    struct LoadTwice : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) { int v = ArrI[a & 15]; return v ^ (v >> 31); } public static long R(long a, long b, long c) => Source[a & 15] ^ (Source[a & 15] >> 63); }

    [MethodImpl(NI)] static void Touch(int* p) { *p += 1; }
    [MethodImpl(NI)] static void TouchL(long* p) { *p += 0x100000001L; }

    // Interlocked operations.
    struct Xchg : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) { ProdCell = b; return Interlocked.Exchange(ref ProdCell, a); } public static long R(long a, long b, long c) => b; }
    struct Xadd : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) { ProdCell = b; return Interlocked.Add(ref ProdCell, a); } public static long R(long a, long b, long c) => a + b; }
    struct Cas : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) { ProdCell = b; return Interlocked.CompareExchange(ref ProdCell, a, b); } public static long R(long a, long b, long c) => b; }
    struct Inc : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) { ProdCell = a; return Interlocked.Increment(ref ProdCell); } public static long R(long a, long b, long c) => a + 1; }

    // Managed calls: the method called returns the low half of c.
    struct CallDirect : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => RetLow(c); public static long R(long a, long b, long c) => c; }
    struct CallVirtual : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => Virt.Low(c); public static long R(long a, long b, long c) => c; }
    struct CallDelegate : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => Dlg(c); public static long R(long a, long b, long c) => c; }
    struct CallPointer : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => Ptr(c); public static long R(long a, long b, long c) => c; }
    struct CallTwice : IProd { [MethodImpl(AI)] public static int P(int a, int b, long c) => RetLow(c) ^ RetLow(a); public static long R(long a, long b, long c) => c ^ a; }

    // ---- users ----

    [MethodImpl(NI)] static int AsInt<T>(int a, int b, long c) where T : struct, IProd => T.P(a, b, c);
    [MethodImpl(NI)] static uint AsUint<T>(int a, int b, long c) where T : struct, IProd => (uint)T.P(a, b, c);
    [MethodImpl(NI)] static short AsShort<T>(int a, int b, long c) where T : struct, IProd => (short)T.P(a, b, c);
    [MethodImpl(NI)] static ushort AsUShort<T>(int a, int b, long c) where T : struct, IProd => (ushort)T.P(a, b, c);
    [MethodImpl(NI)] static bool AsBool<T>(int a, int b, long c) where T : struct, IProd => T.P(a, b, c) < b;

    [MethodImpl(NI)] static long URet<T>(int a, int b, long c, long k) where T : struct, IProd => T.P(a, b, c);
    [MethodImpl(NoOpt)] static long RRet(long p, long a, long b, long c, long k) => p;

    [MethodImpl(NI)] static long UZext<T>(int a, int b, long c, long k) where T : struct, IProd => (long)(uint)T.P(a, b, c);
    [MethodImpl(NoOpt)] static long RZext(long p, long a, long b, long c, long k) => Z32(p);

    [MethodImpl(NI)] static long UAdd<T>(int a, int b, long c, long k) where T : struct, IProd => (long)T.P(a, b, c) + k;
    [MethodImpl(NoOpt)] static long RAdd(long p, long a, long b, long c, long k) => p + k;

    [MethodImpl(NI)] static long USub<T>(int a, int b, long c, long k) where T : struct, IProd => k - (long)T.P(a, b, c);
    [MethodImpl(NoOpt)] static long RSub(long p, long a, long b, long c, long k) => k - p;

    [MethodImpl(NI)] static long UMul<T>(int a, int b, long c, long k) where T : struct, IProd => (long)T.P(a, b, c) * k;
    [MethodImpl(NoOpt)] static long RMul(long p, long a, long b, long c, long k) => p * k;

    [MethodImpl(NI)] static long USquare<T>(int a, int b, long c, long k) where T : struct, IProd => (long)T.P(a, b, c) * (long)T.P(b, a, c);
    [MethodImpl(NoOpt)] static long RSquare(long p, long q, long b, long c, long k) => p * q;

    [MethodImpl(NI)] static long UZextOps<T>(int a, int b, long c, long k) where T : struct, IProd
        => (k + (long)(uint)T.P(a, b, c)) ^ ((k + ((long)(uint)T.P(a, b, c) << 2)) * 3) ^ (long)((ulong)(uint)T.P(a, b, c) << 5) ^ ((long)(uint)T.P(a, b, c) * k);
    [MethodImpl(NoOpt)] static long RZextOps(long p, long a, long b, long c, long k)
        => (k + Z32(p)) ^ ((k + (Z32(p) << 2)) * 3) ^ (Z32(p) << 5) ^ (Z32(p) * k);

    [MethodImpl(NI)] static long USextOps<T>(int a, int b, long c, long k) where T : struct, IProd
        => (k + ((long)T.P(a, b, c) << 3)) ^ (((long)T.P(a, b, c) << 1) + k) * 5 ^ ((long)T.P(a, b, c) >> 3) ^ (long)((ulong)(long)T.P(a, b, c) >> 7) ^ ((long)T.P(a, b, c) << (b & 63));
    [MethodImpl(NoOpt)] static long RSextOps(long p, long a, long b, long c, long k)
        => (k + (p << 3)) ^ ((p << 1) + k) * 5 ^ (p >> 3) ^ (long)((ulong)p >> 7) ^ (p << (int)(b & 63));

    [MethodImpl(NI)] static bool UEq<T>(int a, int b, long c, long k) where T : struct, IProd => (long)T.P(a, b, c) == k;
    [MethodImpl(NI)] static bool UNe<T>(int a, int b, long c, long k) where T : struct, IProd => k != (long)T.P(a, b, c);
    [MethodImpl(NI)] static bool ULt<T>(int a, int b, long c, long k) where T : struct, IProd => (long)T.P(a, b, c) < k;
    [MethodImpl(NI)] static bool UUlt<T>(int a, int b, long c, long k) where T : struct, IProd => (ulong)(long)T.P(a, b, c) < (ulong)k;
    [MethodImpl(NI)] static bool UZeq<T>(int a, int b, long c, long k) where T : struct, IProd => (long)(uint)T.P(a, b, c) == k;
    [MethodImpl(NI)] static bool UZlt<T>(int a, int b, long c, long k) where T : struct, IProd => (ulong)(uint)T.P(a, b, c) < (ulong)k;

    [MethodImpl(NI)]
    static long UBranch<T>(int a, int b, long c, long k) where T : struct, IProd
    {
        long r = 0;
        if ((long)T.P(a, b, c) < k) r |= 1;
        if ((ulong)(long)T.P(a, b, c) < (ulong)k) r |= 2;
        if ((long)T.P(a, b, c) == k) r |= 4;
        if (k <= (long)T.P(a, b, c)) r |= 8;
        if ((long)(uint)T.P(a, b, c) == k) r |= 16;
        if ((ulong)(uint)T.P(a, b, c) > (ulong)k) r |= 32;
        if ((long)T.P(a, b, c) > 0x7FFFFFFFL + (k & 1)) r |= 64;
        if ((long)T.P(a, b, c) < -0x80000000L - (k & 1)) r |= 128;
        return r;
    }

    [MethodImpl(NoOpt)]
    static long RBranch(long p, long a, long b, long c, long k)
    {
        long r = 0;
        if (p < k) r |= 1;
        if ((ulong)p < (ulong)k) r |= 2;
        if (p == k) r |= 4;
        if (k <= p) r |= 8;
        if (Z32(p) == k) r |= 16;
        if ((ulong)Z32(p) > (ulong)k) r |= 32;
        return r;
    }

    [MethodImpl(NI)] static bool UILt<T>(int a, int b, long c, long k) where T : struct, IProd => T.P(a, b, c) < b;
    [MethodImpl(NI)] static bool UIUlt<T>(int a, int b, long c, long k) where T : struct, IProd => (uint)T.P(a, b, c) < (uint)b;
    [MethodImpl(NI)] static bool UIEq<T>(int a, int b, long c, long k) where T : struct, IProd => T.P(a, b, c) == (int)k;
    [MethodImpl(NI)] static bool UIGe0<T>(int a, int b, long c, long k) where T : struct, IProd => T.P(a, b, c) >= 0;

    [MethodImpl(NI)]
    static long UIBranch<T>(int a, int b, long c, long k) where T : struct, IProd
    {
        int p = T.P(a, b, c);
        long r = 0;
        if (p < b) r |= 1;
        if ((uint)p < (uint)b) r |= 2;
        if (p == b) r |= 4;
        if (p >= 0) r |= 8;
        if (p == -1) r |= 16;
        if (p > a) r |= 32;
        if ((uint)p >= (uint)a) r |= 64;
        if (p == (int)k) r |= 128;
        if ((uint)p > 0x7FFFFFFFu) r |= 256;
        if (p < 100) r |= 512;
        if ((uint)p < 100u) r |= 1024;
        if (p != int.MinValue) r |= 2048;
        return r;
    }

    [MethodImpl(NoOpt)]
    static long RIBranch(long p, long a, long b, long c, long k)
    {
        long r = 0;
        if (p < b) r |= 1;
        if (Z32(p) < Z32(b)) r |= 2;
        if (p == b) r |= 4;
        if (p >= 0) r |= 8;
        if (p == -1) r |= 16;
        if (p > a) r |= 32;
        if (Z32(p) >= Z32(a)) r |= 64;
        if (p == S32(k)) r |= 128;
        if (Z32(p) > 0x7FFFFFFFL) r |= 256;
        if (p < 100) r |= 512;
        if (Z32(p) < 100) r |= 1024;
        if (p != -0x80000000L) r |= 2048;
        return r;
    }

    [MethodImpl(NI)]
    static long UIndex<T>(int a, int b, long c, long k) where T : struct, IProd
    {
        long[] t = Tab;
        int p = T.P(a, b, c);
        long r = (uint)p < (uint)t.Length ? t[p] : -1;
        return r * 3 + t[p & 15] + t[(uint)p % 16] + t[(int)((uint)p >> 28)];
    }

    [MethodImpl(NoOpt)]
    static long RIndex(long p, long a, long b, long c, long k)
    {
        long r = p >= 0 && p < 16 ? Tab[p] : -1;
        return r * 3 + Tab[p & 15] + Tab[Z32(p) % 16] + Tab[Z32(p) >> 28];
    }

    [MethodImpl(NI)]
    static long UIndexThrow<T>(int a, int b, long c, long k) where T : struct, IProd
    {
        try { return Tab[T.P(a, b, c)]; }
        catch (IndexOutOfRangeException) { return -2; }
    }

    [MethodImpl(NoOpt)] static long RIndexThrow(long p, long a, long b, long c, long k) => p >= 0 && p < 16 ? Tab[p] : -2;

    [MethodImpl(NI)]
    static long UChecked<T>(int a, int b, long c, long k) where T : struct, IProd
    {
        long r = 0;
        try { r += checked((uint)T.P(a, b, c)); } catch (OverflowException) { r += 0x1000000000L; }
        try { r += (long)checked((ulong)T.P(a, b, c)) * 3; } catch (OverflowException) { r += 0x2000000000L; }
        try { r += checked((int)(uint)T.P(a, b, c)) * 5L; } catch (OverflowException) { r += 0x4000000000L; }
        try { r += checked((short)T.P(a, b, c)) * 7L; } catch (OverflowException) { r += 0x8000000000L; }
        try { r += checked(T.P(a, b, c) + b) * 11L; } catch (OverflowException) { r += 0x10000000000L; }
        try { r += k + checked(unchecked((long)(uint)T.P(a, b, c)) * 4); } catch (OverflowException) { r += 0x20000000000L; }
        return r;
    }

    [MethodImpl(NoOpt)]
    static long RChecked(long p, long a, long b, long c, long k)
    {
        long r = 0;
        if (p >= 0) r += p; else r += 0x1000000000L;
        if (p >= 0) r += p * 3; else r += 0x2000000000L;
        if (p >= 0) r += p * 5; else r += 0x4000000000L;
        if (p >= -0x8000 && p <= 0x7FFF) r += p * 7; else r += 0x8000000000L;
        if (p + b >= -0x80000000L && p + b <= 0x7FFFFFFFL) r += (p + b) * 11; else r += 0x10000000000L;
        r += k + Z32(p) * 4;
        return r;
    }

    // 64-bit checked arithmetic on the widened value: the widening itself cannot overflow, and the
    // checked operation is its direct user. An overflow leaves the method.
    const long Overflowed = 0x5EED0FF10BADL;

    [MethodImpl(NI)]
    static long UCheckedWide<T>(int op, int a, int b, long c, long k) where T : struct, IProd
    {
        ulong uk = unchecked((ulong)k);
        switch (op)
        {
            case 0: return checked((long)T.P(a, b, c) + k);
            case 1: return checked(k - (long)T.P(a, b, c));
            case 2: return (long)checked(unchecked((ulong)(long)T.P(a, b, c)) + uk);
            case 3: return (long)checked(uk - unchecked((ulong)(long)T.P(a, b, c)));
            case 4: return (long)checked(unchecked((ulong)(long)T.P(a, b, c)) - uk);
            case 5: return checked((long)T.P(a, b, c) * k);
            case 6: return (long)checked(unchecked((ulong)(uint)T.P(a, b, c)) + uk);
            case 7: return (long)checked(unchecked((ulong)(long)T.P(a, b, c)) * uk);
            case 8: return (long)checked(uk - unchecked((ulong)(uint)T.P(a, b, c)));
            default: return checked((long)T.P(a, b, c) + (long)T.P(b, a, c));
        }
    }

    static long CatchCheckedWide<T>(int op, int a, int b, long c, long k) where T : struct, IProd
    {
        try { return UCheckedWide<T>(op, a, b, c, k); }
        catch (OverflowException) { return Overflowed; }
    }

    [MethodImpl(NoOpt)]
    static long RCheckedWide(int op, long p, long q, long k)
    {
        ulong u = (ulong)p, uk = (ulong)k, z = (ulong)Z32(p);
        long s = p + k, d = k - p;
        switch (op)
        {
            case 0: return ((p ^ s) & (k ^ s)) < 0 ? Overflowed : s;
            case 1: return ((k ^ p) & (k ^ d)) < 0 ? Overflowed : d;
            case 2: return u + uk < u ? Overflowed : (long)(u + uk);
            case 3: return uk < u ? Overflowed : (long)(uk - u);
            case 4: return u < uk ? Overflowed : (long)(u - uk);
            case 5: return MulOverflows(p, k) ? Overflowed : p * k;
            case 6: return z + uk < z ? Overflowed : (long)(z + uk);
            case 7: return UMulOverflows(u, uk) ? Overflowed : (long)(u * uk);
            case 8: return uk < z ? Overflowed : (long)(uk - z);
            default: return p + q;
        }
    }

    [MethodImpl(NoOpt)]
    static bool UMulOverflows(ulong x, ulong y)
    {
        if (x == 0 || y == 0) return false;
        return (x * y) / y != x;
    }

    [MethodImpl(NoOpt)]
    static bool MulOverflows(long x, long y)
    {
        if (x == 0 || y == 0) return false;
        if ((x == -1 && y == long.MinValue) || (y == -1 && x == long.MinValue)) return true;
        return (x * y) / y != x;
    }

    [MethodImpl(NI)]
    static long USpill<T>(int a, int b, long c, long k) where T : struct, IProd
    {
        int v = T.P(a, b, c);
        Opaque();
        return v + k;
    }

    [MethodImpl(NI)]
    static long USpillMany<T>(int a, int b, long c, long k) where T : struct, IProd
    {
        // More values than registers that survive the call.
        int v0 = T.P(a, b, c), v1 = v0 ^ a, v2 = v0 + b, v3 = v0 - a, v4 = ~v0, v5 = v0 * 3, v6 = v0 >> 1;
        int v7 = v0 | b, v8 = v0 & a, v9 = -v0, v10 = v0 << 1, v11 = v0 + 0x7FFFFFFF, v12 = (int)((uint)v0 >> 1);
        Opaque();
        long r = v0;
        r = r * 31 + v1; r = r * 31 + v2; r = r * 31 + v3; r = r * 31 + v4; r = r * 31 + v5; r = r * 31 + v6;
        r = r * 31 + v7; r = r * 31 + v8; r = r * 31 + v9; r = r * 31 + v10; r = r * 31 + v11; r = r * 31 + (uint)v12;
        return r + k;
    }

    [MethodImpl(NoOpt)]
    static long RSpillMany(long p, long a, long b, long c, long k)
    {
        long r = p;
        r = r * 31 + S32(p ^ a); r = r * 31 + S32(p + b); r = r * 31 + S32(p - a); r = r * 31 + S32(~p);
        r = r * 31 + S32(p * 3); r = r * 31 + (p >> 1); r = r * 31 + S32(p | b); r = r * 31 + S32(p & a);
        r = r * 31 + S32(-p); r = r * 31 + S32(p << 1); r = r * 31 + S32(p + 0x7FFFFFFF); r = r * 31 + (Z32(p) >> 1);
        return r + k;
    }

    [MethodImpl(NI)] static long UCall<T>(int a, int b, long c, long k) where T : struct, IProd => (long)AsInt<T>(a, b, c) + k;

    [MethodImpl(NI)]
    static long UCallKinds<T>(int a, int b, long c, long k) where T : struct, IProd
        => (long)AsUint<T>(a, b, c) + (long)(int)AsUint<T>(a, b, c) * 3 + (long)AsShort<T>(a, b, c) * 5 + (long)AsUShort<T>(a, b, c) * 7 + (AsBool<T>(a, b, c) ? k : 11);

    [MethodImpl(NoOpt)]
    static long RCallKinds(long p, long a, long b, long c, long k)
        => Z32(p) + p * 3 + S16(p) * 5 + (p & 0xFFFF) * 7 + (p < b ? k : 11);

    [MethodImpl(NI)]
    static long UCallCompare<T>(int a, int b, long c, long k) where T : struct, IProd
    {
        long r = 0;
        if (AsInt<T>(a, b, c) < b) r |= 1;
        if ((uint)AsInt<T>(a, b, c) < (uint)b) r |= 2;
        if (AsUint<T>(a, b, c) > 0x7FFFFFFFu) r |= 4;
        if (AsInt<T>(a, b, c) == (int)k) r |= 8;
        if (AsShort<T>(a, b, c) < 0) r |= 16;
        if (AsUShort<T>(a, b, c) > 0x7FFF) r |= 32;
        return r;
    }

    [MethodImpl(NoOpt)]
    static long RCallCompare(long p, long a, long b, long c, long k)
    {
        long r = 0;
        if (p < b) r |= 1;
        if (Z32(p) < Z32(b)) r |= 2;
        if (Z32(p) > 0x7FFFFFFFL) r |= 4;
        if (p == S32(k)) r |= 8;
        if (S16(p) < 0) r |= 16;
        if ((p & 0xFFFF) > 0x7FFF) r |= 32;
        return r;
    }

    [MethodImpl(NI)]
    static long UBits<T>(int a, int b, long c, long k) where T : struct, IProd
        => BitOperations.LeadingZeroCount((ulong)(long)T.P(a, b, c)) + BitOperations.TrailingZeroCount((long)T.P(a, b, c)) * 100
           + BitOperations.PopCount((ulong)(long)T.P(a, b, c)) * 10000 + BitOperations.LeadingZeroCount((ulong)(uint)T.P(a, b, c)) * 1000000L
           + BitOperations.Log2((ulong)(long)T.P(a, b, c)) * 100000000L;

    [MethodImpl(NoOpt)]
    static long RBits(long p, long a, long b, long c, long k)
        => Lzc(p, 64) + Tzc(p, 64) * 100 + Pop(p, 64) * 10000 + Lzc(Z32(p), 64) * 1000000L + (63 - Lzc(p | 1, 64)) * 100000000L;

    [MethodImpl(NI)]
    static long Last(long a0, long a1, long a2, long a3, long a4, long a5, long a6, long a7, long s0, long s1, ulong s2) => s0 * 3 + s1 + (long)s2 * 7;

    [MethodImpl(NI)]
    static long UStackArg<T>(int a, int b, long c, long k) where T : struct, IProd
        => Last(-1, -1, -1, -1, -1, -1, -1, -1, T.P(a, b, c), k, (uint)T.P(a, b, c));

    [MethodImpl(NoOpt)] static long RStackArg(long p, long a, long b, long c, long k) => p * 3 + k + Z32(p) * 7;

    [MethodImpl(NI)] static long UNotNeg<T>(int a, int b, long c, long k) where T : struct, IProd => ~(long)T.P(a, b, c) ^ (-(long)T.P(a, b, c) * 3);
    [MethodImpl(NoOpt)] static long RNotNeg(long p, long a, long b, long c, long k) => ~p ^ (-p * 3);

    [MethodImpl(NI)]
    static long UMinMax<T>(int a, int b, long c, long k) where T : struct, IProd
        => Math.Max((long)T.P(a, b, c), k) * 3 + Math.Min((long)T.P(a, b, c), k) + (long)Math.Max((ulong)(long)T.P(a, b, c), (ulong)k) * 5
           + (long)Math.Min((ulong)(uint)T.P(a, b, c), (ulong)k) * 7;

    [MethodImpl(NoOpt)]
    static long RMinMax(long p, long a, long b, long c, long k)
        => (p > k ? p : k) * 3 + (p < k ? p : k) + ((ulong)p > (ulong)k ? p : k) * 5 + ((ulong)Z32(p) < (ulong)k ? Z32(p) : k) * 7;

    [MethodImpl(NI)]
    static long ULogic<T>(int a, int b, long c, long k) where T : struct, IProd
        => ((long)T.P(a, b, c) & k) + ((long)T.P(a, b, c) | k) * 3 + ((long)T.P(a, b, c) ^ k) * 5 + ((long)(uint)T.P(a, b, c) | k) * 7
           + (k & ~(long)T.P(a, b, c)) * 11 + (k ^ (long)(uint)T.P(a, b, c)) * 13;

    [MethodImpl(NoOpt)]
    static long RLogic(long p, long a, long b, long c, long k)
        => (p & k) + (p | k) * 3 + (p ^ k) * 5 + (Z32(p) | k) * 7 + (k & ~p) * 11 + (k ^ Z32(p)) * 13;

    [MethodImpl(NI)]
    static long UDiv64<T>(int a, int b, long c, long k) where T : struct, IProd
    {
        long d = T.P(a, b, c);
        if (d == 0 || d == -1) return 5;
        return k / d + k % d * 3 + (long)((ulong)k / (ulong)(uint)T.P(a, b, c)) * 5 + (long)((ulong)k % (ulong)d) * 7;
    }

    [MethodImpl(NoOpt)]
    static long RDiv64(long p, long a, long b, long c, long k)
    {
        if (p == 0 || p == -1) return 5;
        return k / p + k % p * 3 + (long)((ulong)k / (ulong)Z32(p)) * 5 + (long)((ulong)k % (ulong)p) * 7;
    }

    [MethodImpl(NI)]
    static long UPhi<T>(int a, int b, long c, long k) where T : struct, IProd
    {
        int v = (k & 1) != 0 ? T.P(a, b, c) : b;
        return (long)v * 3 + (uint)v;
    }

    [MethodImpl(NoOpt)] static long RPhi(long p, long a, long b, long c, long k) => ((k & 1) != 0 ? p : b) * 3 + Z32((k & 1) != 0 ? p : b);

    [MethodImpl(NI)]
    static long ULoop<T>(int a, int b, long c, long k) where T : struct, IProd
    {
        long sum = 0;
        int v = T.P(a, b, c);
        for (int i = 0; i < 3; i++)
        {
            sum = sum * 3 + v;
            v = ~v;
        }
        return sum + (uint)v;
    }

    [MethodImpl(NoOpt)] static long RLoop(long p, long a, long b, long c, long k) => ((p * 3 + ~p) * 3 + p) + Z32(~p);

    [MethodImpl(NI)]
    static long UPointer<T>(int a, int b, long c, long k) where T : struct, IProd
        => (long)((nint)(BaseL + T.P(a, b, c)) - (nint)BaseL) ^ ((long)((nint)(BaseB + (uint)T.P(a, b, c)) - (nint)BaseB) * 3) ^ ((long)((nint)(BaseL - T.P(a, b, c)) - (nint)BaseL) * 5);

    [MethodImpl(NoOpt)] static long RPointer(long p, long a, long b, long c, long k) => (long)(nint)(p * 8) ^ ((long)(nint)Z32(p) * 3) ^ ((long)(nint)(-p * 8) * 5);

    [MethodImpl(NI)]
    static long UStore<T>(int a, int b, long c, long k) where T : struct, IProd
    {
        Holder h = Obj;
        h.Pad = T.P(a, b, c);
        h.I = T.P(a, b, c);
        h.S = (short)T.P(a, b, c);
        h.B = (byte)T.P(a, b, c);
        LongCell = (uint)T.P(a, b, c);
        Opaque();
        return h.Pad + (long)h.I * 3 + h.S * 5 + h.B * 7 + LongCell * 11;
    }

    [MethodImpl(NoOpt)] static long RStore(long p, long a, long b, long c, long k) => p + p * 3 + S16(p) * 5 + (p & 0xFF) * 7 + Z32(p) * 11;

    [MethodImpl(NI)]
    static long UAtomic<T>(int a, int b, long c, long k) where T : struct, IProd
    {
        LongCell = k;
        long old = Interlocked.Exchange(ref LongCell, T.P(a, b, c));
        long sum = Interlocked.Add(ref LongCell, T.P(a, b, c));
        long seen = Interlocked.CompareExchange(ref LongCell, k, (long)T.P(a, b, c) * 2);
        return old ^ sum * 3 ^ seen * 5 ^ LongCell * 7;
    }

    [MethodImpl(NoOpt)] static long RAtomic(long p, long a, long b, long c, long k) => k ^ (p * 2) * 3 ^ (p * 2) * 5 ^ k * 7;

    [MethodImpl(NI)]
    static long USwap64<T>(int a, int b, long c, long k) where T : struct, IProd
        => BinaryPrimitives.ReverseEndianness((long)T.P(a, b, c)) ^ (long)BinaryPrimitives.ReverseEndianness((ulong)(uint)T.P(a, b, c)) * 3
           ^ (long)BitOperations.RotateLeft((ulong)(long)T.P(a, b, c), b) * 5 ^ (long)BitOperations.RotateRight((ulong)(long)T.P(a, b, c), 7) * 7;

    [MethodImpl(NoOpt)]
    static long RSwap64(long p, long a, long b, long c, long k)
        => Swap(p, 8) ^ Swap(Z32(p), 8) * 3 ^ (long)(((ulong)p << (int)(b & 63)) | ((b & 63) == 0 ? 0 : (ulong)p >> (int)(64 - (b & 63)))) * 5
           ^ (long)(((ulong)p >> 7) | ((ulong)p << 57)) * 7;

    [MethodImpl(NI)]
    static long UBit64<T>(int a, int b, long c, long k) where T : struct, IProd
        => ((long)T.P(a, b, c) | (1L << b)) ^ (((long)T.P(a, b, c) ^ (1L << b)) * 3) ^ (((long)T.P(a, b, c) & ~(1L << b)) * 5) ^ ((((long)T.P(a, b, c) >> b) & 1) * 7)
           ^ ((k | (1L << T.P(a, b, c))) * 11) ^ ((k & ~(1L << T.P(a, b, c))) * 13) ^ (((k >> T.P(a, b, c)) & 1) * 17);

    [MethodImpl(NoOpt)]
    static long RBit64(long p, long a, long b, long c, long k)
    {
        int n = (int)(b & 63), m = (int)(p & 63);
        return (p | (1L << n)) ^ ((p ^ (1L << n)) * 3) ^ ((p & ~(1L << n)) * 5) ^ (((p >> n) & 1) * 7)
               ^ ((k | (1L << m)) * 11) ^ ((k & ~(1L << m)) * 13) ^ (((k >> m) & 1) * 17);
    }

    [MethodImpl(NI)]
    static long UShiftCount<T>(int a, int b, long c, long k) where T : struct, IProd
        => (k << T.P(a, b, c)) ^ ((k >> T.P(a, b, c)) * 3) ^ ((long)((ulong)k >> T.P(a, b, c)) * 5) ^ ((long)(b << T.P(a, b, c)) * 7) ^ ((long)(b >> T.P(a, b, c)) * 11);

    [MethodImpl(NoOpt)]
    static long RShiftCount(long p, long a, long b, long c, long k)
        => (k << (int)(p & 63)) ^ ((k >> (int)(p & 63)) * 3) ^ ((long)((ulong)k >> (int)(p & 63)) * 5) ^ (S32(b << (int)(p & 31)) * 7) ^ ((b >> (int)(p & 31)) * 11);

    [MethodImpl(NI)]
    static long USwitch<T>(int a, int b, long c, long k) where T : struct, IProd
    {
        switch (T.P(a, b, c))
        {
            case -2: return 11;
            case -1: return 12;
            case 0: return 13;
            case 1: return 14;
            case 2: return 15;
            case 3: return 16;
            case 4: return 17;
            default: return 18;
        }
    }

    [MethodImpl(NoOpt)] static long RSwitch(long p, long a, long b, long c, long k) => p >= -2 && p <= 4 ? 13 + p : 18;

    [MethodImpl(NI)]
    static long USmall<T>(int a, int b, long c, long k) where T : struct, IProd
        => (long)(short)T.P(a, b, c) + (long)(byte)T.P(a, b, c) * 3 + (long)(ushort)T.P(a, b, c) * 5 + (long)(sbyte)T.P(a, b, c) * 7;

    [MethodImpl(NoOpt)] static long RSmall(long p, long a, long b, long c, long k) => S16(p) + (p & 0xFF) * 3 + (p & 0xFFFF) * 5 + S8(p) * 7;

    [MethodImpl(NI)]
    static long UInt32Ops<T>(int a, int b, long c, long k) where T : struct, IProd
    {
        // 32-bit users on top of the producer, widened in the end.
        int p = T.P(a, b, c);
        long r = (long)(p + b);
        r = r * 31 + (long)(p & b);
        r = r * 31 + (long)(p | b);
        r = r * 31 + (long)(p ^ b);
        r = r * 31 + (long)(uint)(p & b);
        r = r * 31 + (long)(uint)(p | a);
        r = r * 31 + (long)(~p);
        r = r * 31 + (long)(p & ~b);
        r = r * 31 + (long)(p >> 1);
        r = r * 31 + (long)(p * b);
        r = r * 31 + (long)(-p);
        return r;
    }

    [MethodImpl(NoOpt)]
    static long RInt32Ops(long p, long a, long b, long c, long k)
    {
        long r = S32(p + b);
        r = r * 31 + (p & b);
        r = r * 31 + (p | b);
        r = r * 31 + (p ^ b);
        r = r * 31 + Z32(p & b);
        r = r * 31 + Z32(p | a);
        r = r * 31 + ~p;
        r = r * 31 + (p & ~b);
        r = r * 31 + (p >> 1);
        r = r * 31 + S32(p * b);
        r = r * 31 + S32(-p);
        return r;
    }

    // ---- driver ----

    static readonly int[] As =
    {
        0, 1, -1, 2, int.MaxValue, int.MinValue, 0x40000000, -0x40000001, 0x55555555, unchecked((int)0xAAAAAAAA),
        0xFF, 0x8000, -0x8000, 0x12345678, unchecked((int)0x87654321), 15,
    };

    static readonly int[] Bs = { 0, 1, 2, 3, 31, 32, 33, -1, int.MinValue, int.MaxValue, 16, 0x1234567 };

    static readonly long[] Cs =
    {
        0, -1, 0x7FFFFFFF, 0x80000000L, 0xFFFFFFFFL, 0x100000001L, unchecked((long)0xFEDCBA9876543210),
        0x7FFFFFFF80000000L, long.MinValue, 0x00000001_7FFFFFFFL,
    };

    static readonly long[] Deltas = { 0, 1, -1, 0x100000000L, -0x100000000L, 0x80000000L };

    static void Eq(string prod, string user, int a, int b, long c, long k, long want, long got)
    {
        checks++;
        if (want == got) return;
        fails++;
        if (++shown <= MaxShown)
            Console.WriteLine("FAIL " + prod + "/" + user + " a=0x" + a.ToString("x") + " b=0x" + b.ToString("x") + " c=0x" + c.ToString("x")
                              + " k=0x" + k.ToString("x") + " expected 0x" + want.ToString("x") + " got 0x" + got.ToString("x"));
    }

    [MethodImpl(NoOpt)] static long Ref<T>(int a, int b, long c) where T : struct, IProd => S32(T.R(a, b, c));

    [MethodImpl(NoOpt)] static long B(bool v) => v ? 1 : 0;

    static void Run<T>() where T : struct, IProd
    {
        string n = typeof(T).Name;
        int before = fails;
        shown = 0;
        for (int ia = 0; ia < As.Length; ia++)
        for (int ib = 0; ib < Bs.Length; ib++)
        for (int ic = 0; ic < Cs.Length; ic++)
        {
            int a = As[ia], b = Bs[ib];
            long c = Cs[ic];
            long p = Ref<T>(a, b, c);
            long k = c ^ a;

            Eq(n, "ret", a, b, c, k, RRet(p, a, b, c, k), URet<T>(a, b, c, k));
            Eq(n, "zext", a, b, c, k, RZext(p, a, b, c, k), UZext<T>(a, b, c, k));
            Eq(n, "add", a, b, c, k, RAdd(p, a, b, c, k), UAdd<T>(a, b, c, k));
            Eq(n, "sub", a, b, c, k, RSub(p, a, b, c, k), USub<T>(a, b, c, k));
            Eq(n, "mul", a, b, c, k, RMul(p, a, b, c, k), UMul<T>(a, b, c, k));
            Eq(n, "square", a, b, c, k, RSquare(p, Ref<T>(b, a, c), b, c, k), USquare<T>(a, b, c, k));
            Eq(n, "zext ops", a, b, c, k, RZextOps(p, a, b, c, k), UZextOps<T>(a, b, c, k));
            Eq(n, "sext ops", a, b, c, k, RSextOps(p, a, b, c, k), USextOps<T>(a, b, c, k));
            Eq(n, "int compare", a, b, c, k, RIBranch(p, a, b, c, k), UIBranch<T>(a, b, c, k));
            Eq(n, "int <", a, b, c, k, B(p < b), B(UILt<T>(a, b, c, k)));
            Eq(n, "uint <", a, b, c, k, B(Z32(p) < Z32(b)), B(UIUlt<T>(a, b, c, k)));
            Eq(n, "int >= 0", a, b, c, k, B(p >= 0), B(UIGe0<T>(a, b, c, k)));
            Eq(n, "index", a, b, c, k, RIndex(p, a, b, c, k), UIndex<T>(a, b, c, k));
            Eq(n, "kept across a call", a, b, c, k, RAdd(p, a, b, c, k), USpill<T>(a, b, c, k));
            Eq(n, "many kept across a call", a, b, c, k, RSpillMany(p, a, b, c, k), USpillMany<T>(a, b, c, k));
            Eq(n, "returned", a, b, c, k, RAdd(p, a, b, c, k), UCall<T>(a, b, c, k));
            Eq(n, "returned kinds", a, b, c, k, RCallKinds(p, a, b, c, k), UCallKinds<T>(a, b, c, k));
            Eq(n, "returned compare", a, b, c, k, RCallCompare(p, a, b, c, k), UCallCompare<T>(a, b, c, k));
            Eq(n, "bit counts", a, b, c, k, RBits(p, a, b, c, k), UBits<T>(a, b, c, k));
            Eq(n, "stack argument", a, b, c, k, RStackArg(p, a, b, c, k), UStackArg<T>(a, b, c, k));
            Eq(n, "not neg", a, b, c, k, RNotNeg(p, a, b, c, k), UNotNeg<T>(a, b, c, k));
            Eq(n, "min max", a, b, c, k, RMinMax(p, a, b, c, k), UMinMax<T>(a, b, c, k));
            Eq(n, "logic", a, b, c, k, RLogic(p, a, b, c, k), ULogic<T>(a, b, c, k));
            Eq(n, "div", a, b, c, k, RDiv64(p, a, b, c, k), UDiv64<T>(a, b, c, k));
            Eq(n, "phi", a, b, c, k, RPhi(p, a, b, c, k), UPhi<T>(a, b, c, k));
            Eq(n, "loop", a, b, c, k, RLoop(p, a, b, c, k), ULoop<T>(a, b, c, k));
            Eq(n, "pointer", a, b, c, k, RPointer(p, a, b, c, k), UPointer<T>(a, b, c, k));
            Eq(n, "store", a, b, c, k, RStore(p, a, b, c, k), UStore<T>(a, b, c, k));
            Eq(n, "atomic", a, b, c, k, RAtomic(p, a, b, c, k), UAtomic<T>(a, b, c, k));
            Eq(n, "swap rotate", a, b, c, k, RSwap64(p, a, b, c, k), USwap64<T>(a, b, c, k));
            Eq(n, "single bits", a, b, c, k, RBit64(p, a, b, c, k), UBit64<T>(a, b, c, k));
            Eq(n, "shift count", a, b, c, k, RShiftCount(p, a, b, c, k), UShiftCount<T>(a, b, c, k));
            Eq(n, "switch", a, b, c, k, RSwitch(p, a, b, c, k), USwitch<T>(a, b, c, k));
            Eq(n, "small casts", a, b, c, k, RSmall(p, a, b, c, k), USmall<T>(a, b, c, k));
            Eq(n, "int ops", a, b, c, k, RInt32Ops(p, a, b, c, k), UInt32Ops<T>(a, b, c, k));

            // Throwing is slow: a part of the arguments only.
            if ((ia + ib + ic) % 7 == 0)
            {
                Eq(n, "index throw", a, b, c, k, RIndexThrow(p, a, b, c, k), UIndexThrow<T>(a, b, c, k));
                Eq(n, "checked", a, b, c, k, RChecked(p, a, b, c, k), UChecked<T>(a, b, c, k));
                long q = Ref<T>(b, a, c);
                for (int op = 0; op < 10; op++)
                    Eq(n, "checked 64-bit, operation " + op, a, b, c, k, RCheckedWide(op, p, q, k), CatchCheckedWide<T>(op, a, b, c, k));
            }

            // 64-bit compares against values around the producer's.
            if (ic < 3)
            {
                for (int id = 0; id < Deltas.Length; id++)
                {
                    k = p + Deltas[id];
                    Eq(n, "long ==", a, b, c, k, B(p == k), B(UEq<T>(a, b, c, k)));
                    Eq(n, "long !=", a, b, c, k, B(p != k), B(UNe<T>(a, b, c, k)));
                    Eq(n, "long <", a, b, c, k, B(p < k), B(ULt<T>(a, b, c, k)));
                    Eq(n, "ulong <", a, b, c, k, B((ulong)p < (ulong)k), B(UUlt<T>(a, b, c, k)));
                    Eq(n, "zext ==", a, b, c, k, B(Z32(p) == k), B(UZeq<T>(a, b, c, k)));
                    Eq(n, "zext <", a, b, c, k, B((ulong)Z32(p) < (ulong)k), B(UZlt<T>(a, b, c, k)));
                    Eq(n, "long branches", a, b, c, k, RBranch(p, a, b, c, k), UBranch<T>(a, b, c, k));
                    Eq(n, "int ==", a, b, c, k, B(p == S32(k)), B(UIEq<T>(a, b, c, k)));
                    k = Z32(p) + Deltas[id];
                    Eq(n, "zext == (2)", a, b, c, k, B(Z32(p) == k), B(UZeq<T>(a, b, c, k)));
                    Eq(n, "zext < (2)", a, b, c, k, B((ulong)Z32(p) < (ulong)k), B(UZlt<T>(a, b, c, k)));
                    Eq(n, "long branches (2)", a, b, c, k, RBranch(p, a, b, c, k), UBranch<T>(a, b, c, k));
                }
            }
        }
        if (fails != before) Console.WriteLine("FAIL " + n + ": " + (fails - before) + " checks");
    }

    static int Main()
    {
        byte* raw = stackalloc byte[16 * 8 + 8];
        long* mem = stackalloc long[4];
        Setup(raw, mem);

        Run<Add>(); Run<Sub>(); Run<Mul>(); Run<Div>(); Run<DivNeg>(); Run<Mod>(); Run<DivU>(); Run<ModU>();
        Run<Shl>(); Run<Sar>(); Run<Shr>(); Run<Shl1>(); Run<Shl2>(); Run<Shl3>(); Run<Mul2>(); Run<Mul4>(); Run<Mul8>(); Run<UShl2>(); Run<Shl31>(); Run<Sar7>(); Run<Sar31>(); Run<Shr1>(); Run<Shr31>();
        Run<ShlShr>(); Run<ShlSar>(); Run<Neg>(); Run<Not>();
        Run<And>(); Run<Or>(); Run<Xor>(); Run<AndNot>(); Run<OrNot>(); Run<XorNot>();
        Run<AndMax>(); Run<AndMin>(); Run<OrMin>(); Run<XorMin>(); Run<XorM1>(); Run<AndFF>(); Run<AndFFFF>(); Run<AndHigh>();
        Run<AndUint>(); Run<OrSmall>(); Run<XorBig>();
        Run<Rol>(); Run<Ror>(); Run<Rol1>(); Run<Ror1>(); Run<RolHand>();
        Run<Mul3>(); Run<Mul5>(); Run<Mul9>(); Run<Mul7>(); Run<Mul10>(); Run<Mul16>(); Run<MulBig>(); Run<MulNeg>(); Run<Mul3U>();
        Run<AddSh1>(); Run<AddSh2>(); Run<AddSh3>(); Run<AddSh2U>();
        Run<AddC7FF>(); Run<AddC800>(); Run<SubC801>(); Run<AddCBig>(); Run<AddCMin>(); Run<Add1>(); Run<AddU>();


        Console.WriteLine(fails == 0 ? "ALL PASS (" + checks + " checks)" : "FAILED " + fails + " of " + checks);
        return fails == 0 ? 0 : 1;
    }
}
