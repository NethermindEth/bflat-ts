// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// 32-bit integer parameters and return values.
//
// The RISC-V calling convention passes an int or a uint sign-extended to
// the full register. The compiler trusts that: a method does not extend a
// 32-bit parameter again before it widens it, compares it or indexes with
// it. So every caller has to extend, also when the value is the low half of
// a 64-bit value that already sits in the argument register, or that is
// reloaded into it from the stack. Only a struct field that arrives in
// part of a register is still extended by the method itself.
//
// Callers. The arguments are 64-bit values narrowed without a check, with
// anything in the upper half: a plain narrowing, both halves taken by
// shifts, a mask of the low half, a sum and a product narrowed, a
// zero-extended value. The methods called return what is in the register
// widened to 64 bits, so an argument that was not extended shows. They are
// reached by a direct call, an instance, a virtual and an interface call,
// a closed and a static delegate, a shared generic method, a managed
// function pointer and an unmanaged one to an UnmanagedCallersOnly method,
// with one argument and with twelve, four of them on the stack after a call
// that left all ones there. The same is done for an int return value, and
// for libc: labs sees the whole register of an int argument, and qsort
// looks at the int a managed comparer returns.
//
// Methods. Comparisons, widening, shifts, division, multiplication, array
// and pointer indexing and a switch on int and uint parameters at their
// extreme values; struct parameters with 32-bit and smaller fields.
//
// The expected values are computed by code compiled without optimization
// from values that went through memory. Each check prints one line; the
// run ends with ALL PASS or a FAIL count, and the guest returns the number
// of failures.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

interface ICall
{
    static abstract long Call1(int a);
    static abstract long Call12(int a0, int a1, int a2, int a3, uint a4, uint a5,
                                int a6, uint a7, int a8, uint a9, int a10, uint a11);
    static abstract int Ret(long l);
}

delegate long D1(int a);
delegate long D12(int a0, int a1, int a2, int a3, uint a4, uint a5, int a6, uint a7, int a8, uint a9, int a10, uint a11);
delegate int DRet(long l);

abstract class Base
{
    public abstract long VSink1(int a);
    public abstract long VSink12(int a0, int a1, int a2, int a3, uint a4, uint a5,
                                 int a6, uint a7, int a8, uint a9, int a10, uint a11);
    public abstract int VRet(long l);
}

interface ITarget
{
    long ISink1(int a);
    long ISink12(int a0, int a1, int a2, int a3, uint a4, uint a5, int a6, uint a7, int a8, uint a9, int a10, uint a11);
    int IRet(long l);
}

sealed class Target : Base, ITarget
{
    public long Bias;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public long Sink1(int a) => a + Bias;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public long Sink12(int a0, int a1, int a2, int a3, uint a4, uint a5, int a6, uint a7, int a8, uint a9, int a10, uint a11)
        => Program.Hash12(a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11) + Bias;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int Ret(long l) => (int)l;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override long VSink1(int a) => a + Bias;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override long VSink12(int a0, int a1, int a2, int a3, uint a4, uint a5, int a6, uint a7, int a8, uint a9, int a10, uint a11)
        => Program.Hash12(a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11) + Bias;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override int VRet(long l) => (int)l;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public long ISink1(int a) => a + Bias;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public long ISink12(int a0, int a1, int a2, int a3, uint a4, uint a5, int a6, uint a7, int a8, uint a9, int a10, uint a11)
        => Program.Hash12(a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11) + Bias;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int IRet(long l) => (int)l;
}

// A second implementation, so that the calls above stay virtual.
sealed class Other : Base, ITarget
{
    public override long VSink1(int a) => 1;
    public override long VSink12(int a0, int a1, int a2, int a3, uint a4, uint a5, int a6, uint a7, int a8, uint a9, int a10, uint a11) => 2;
    public override int VRet(long l) => 3;
    public long ISink1(int a) => 4;
    public long ISink12(int a0, int a1, int a2, int a3, uint a4, uint a5, int a6, uint a7, int a8, uint a9, int a10, uint a11) => 5;
    public int IRet(long l) => 6;
}

struct I1 { public int A; }
struct I2 { public int A, B; }
struct U2 { public uint A, B; }
struct S4 { public short A; public ushort B; public int C; }

unsafe class Program
{
    [DllImport("*", EntryPoint = "labs")] static extern nint labs_int(int x);
    [DllImport("*", EntryPoint = "labs")] static extern nint labs_uint(uint x);
    [DllImport("*")] static extern int abs(int x);
    [DllImport("*")] static extern void qsort(void* b, nuint n, nuint size, delegate* unmanaged<void*, void*, int> cmp);

    const MethodImplOptions NoOpt = MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization;

    static int fails;

    static void Check(string name, bool ok)
    {
        Console.WriteLine((ok ? "ok   " : "FAIL ") + name);
        if (!ok) fails++;
    }

    // ---- the methods called: they widen what arrives ----

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Hash12(int a0, int a1, int a2, int a3, uint a4, uint a5, int a6, uint a7, int a8, uint a9, int a10, uint a11)
    {
        long h = a0;
        h = h * 31 + a1;
        h = h * 31 + a2;
        h = h * 31 + a3;
        h = h * 31 + (int)a4;
        h = h * 31 + (int)a5;
        h = h * 31 + a6;
        h = h * 31 + (int)a7;
        h = h * 31 + a8;
        h = h * 31 + (int)a9;
        h = h * 31 + a10;
        h = h * 31 + (int)a11;
        return h;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Sink1(int a) => a;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Sink12(int a0, int a1, int a2, int a3, uint a4, uint a5, int a6, uint a7, int a8, uint a9, int a10, uint a11)
        => Hash12(a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int RetLow(long l) => (int)l;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long GSink1<T>(T tag, int a) where T : class => a;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long GSink12<T>(T tag, int a0, int a1, int a2, int a3, uint a4, uint a5, int a6, uint a7, int a8, uint a9, int a10, uint a11)
        where T : class
        => Hash12(a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int GRet<T>(T tag, long l) where T : class => (int)l;

    [UnmanagedCallersOnly]
    static long USink1(int a) => a;

    [UnmanagedCallersOnly]
    static long USink12(int a0, int a1, int a2, int a3, uint a4, uint a5, int a6, uint a7, int a8, uint a9, int a10, uint a11)
        => Hash12(a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11);

    [UnmanagedCallersOnly]
    static int URet(long l) => (int)l;

    // Targets are read from fields so that the calls stay indirect.
    static Target target;
    static Base viaBase;
    static ITarget viaInterface;
    static D1 closed1, static1;
    static D12 closed12, static12;
    static DRet closedRet, staticRet;
    static delegate*<int, long> ptr1;
    static delegate*<int, int, int, int, uint, uint, int, uint, int, uint, int, uint, long> ptr12;
    static delegate*<long, int> ptrRet;
    static delegate* unmanaged<int, long> uptr1;
    static delegate* unmanaged<int, int, int, int, uint, uint, int, uint, int, uint, int, uint, long> uptr12;
    static delegate* unmanaged<long, int> uptrRet;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Base Make(int kind) => kind == 0 ? new Target() : new Other();

    static void Setup()
    {
        target = (Target)Make(0);
        viaBase = target;
        viaInterface = target;
        Base other = Make(1);
        if (other.VSink1(0) != 1 || ((ITarget)other).ISink1(0) != 4) fails++;
        closed1 = target.Sink1; closed12 = target.Sink12; closedRet = target.Ret;
        static1 = Sink1; static12 = Sink12; staticRet = RetLow;
        ptr1 = &Sink1; ptr12 = &Sink12; ptrRet = &RetLow;
        uptr1 = &USink1; uptr12 = &USink12; uptrRet = &URet;
    }

    // ---- the ways to call them ----

    struct Direct : ICall
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Call1(int a) => Sink1(a);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Call12(int a0, int a1, int a2, int a3, uint a4, uint a5, int a6, uint a7, int a8, uint a9, int a10, uint a11)
            => Sink12(a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Ret(long l) => RetLow(l);
    }

    struct Instance : ICall
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Call1(int a) => target.Sink1(a);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Call12(int a0, int a1, int a2, int a3, uint a4, uint a5, int a6, uint a7, int a8, uint a9, int a10, uint a11)
            => target.Sink12(a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Ret(long l) => target.Ret(l);
    }

    struct Virtual : ICall
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Call1(int a) => viaBase.VSink1(a);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Call12(int a0, int a1, int a2, int a3, uint a4, uint a5, int a6, uint a7, int a8, uint a9, int a10, uint a11)
            => viaBase.VSink12(a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Ret(long l) => viaBase.VRet(l);
    }

    struct Interface : ICall
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Call1(int a) => viaInterface.ISink1(a);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Call12(int a0, int a1, int a2, int a3, uint a4, uint a5, int a6, uint a7, int a8, uint a9, int a10, uint a11)
            => viaInterface.ISink12(a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Ret(long l) => viaInterface.IRet(l);
    }

    struct ClosedDelegate : ICall
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Call1(int a) => closed1(a);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Call12(int a0, int a1, int a2, int a3, uint a4, uint a5, int a6, uint a7, int a8, uint a9, int a10, uint a11)
            => closed12(a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Ret(long l) => closedRet(l);
    }

    struct StaticDelegate : ICall
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Call1(int a) => static1(a);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Call12(int a0, int a1, int a2, int a3, uint a4, uint a5, int a6, uint a7, int a8, uint a9, int a10, uint a11)
            => static12(a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Ret(long l) => staticRet(l);
    }

    struct SharedGeneric : ICall
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Call1(int a) => GSink1<string>(null, a);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Call12(int a0, int a1, int a2, int a3, uint a4, uint a5, int a6, uint a7, int a8, uint a9, int a10, uint a11)
            => GSink12<string>(null, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Ret(long l) => GRet<string>(null, l);
    }

    struct Pointer : ICall
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Call1(int a) => ptr1(a);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Call12(int a0, int a1, int a2, int a3, uint a4, uint a5, int a6, uint a7, int a8, uint a9, int a10, uint a11)
            => ptr12(a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Ret(long l) => ptrRet(l);
    }

    struct Unmanaged : ICall
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Call1(int a) => uptr1(a);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Call12(int a0, int a1, int a2, int a3, uint a4, uint a5, int a6, uint a7, int a8, uint a9, int a10, uint a11)
            => uptr12(a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Ret(long l) => uptrRet(l);
    }

    // ---- the callers ----

    // The low half of a 64-bit value, read from memory.
    [MethodImpl(NoOpt)]
    static long Low(long v)
    {
        int* p = (int*)&v;
        return *p;
    }

    [MethodImpl(NoOpt)]
    static long RefHash12(int* buf)
    {
        long h = buf[0];
        for (int i = 1; i < 12; i++) h = h * 31 + buf[i];
        return h;
    }

    // The value is in the first argument register, then in the second.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long One<T>(long l) where T : struct, ICall => T.Call1((int)l);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long OneSecond<T>(long pad, long l) where T : struct, ICall => T.Call1((int)l);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long OneUint<T>(long l) where T : struct, ICall => T.Call1((int)(uint)l);

    // The values live across calls first, so they come from other registers
    // or from the stack.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Three<T>(long l, long m) where T : struct, ICall
    {
        long r = T.Call1((int)l);
        r = r * 31 + T.Call1((int)m);
        r = r * 31 + T.Call1((int)(l + m));
        return r * 31 + T.Call1((int)l);
    }

    [MethodImpl(NoOpt)]
    static long ThreeRef(long l, long m)
    {
        long r = Low(l);
        r = r * 31 + Low(m);
        r = r * 31 + Low(l + m);
        return r * 31 + Low(l);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Dirty12(long a0, long a1, long a2, long a3, long a4, long a5,
                        long a6, long a7, long a8, long a9, long a10, long a11)
        => a0 ^ a8 ^ a9 ^ a10 ^ a11;

    // Twelve narrowed values; buf gets the same values through memory.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Twelve<T>(long l, long m, int* buf) where T : struct, ICall
    {
        int e0 = (int)l;
        int e1 = (int)(l >> 32);
        int e2 = (int)((ulong)l >> 32);
        int e3 = (int)(l & 0xFFFFFFFFL);
        uint e4 = (uint)m;
        uint e5 = (uint)(l + m);
        int e6 = (int)(l * m);
        uint e7 = (uint)(ulong)(uint)l;
        int e8 = (int)m;
        uint e9 = (uint)(l ^ m);
        int e10 = (int)(m >> 31);
        uint e11 = (uint)((ulong)m >> 33);
        buf[0] = e0; buf[1] = e1; buf[2] = e2; buf[3] = e3;
        buf[4] = (int)e4; buf[5] = (int)e5; buf[6] = e6; buf[7] = (int)e7;
        buf[8] = e8; buf[9] = (int)e9; buf[10] = e10; buf[11] = (int)e11;
        long d = Dirty12(l, m, l, m, l, m, l, m, -1, -1, -1, -1);
        return T.Call12(e0, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11) + (d - l);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Returned<T>(long l) where T : struct, ICall
    {
        long w = T.Ret(l);
        return w;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long ReturnedTwice<T>(long l, long m) where T : struct, ICall
    {
        long w = T.Ret(l + m);
        long v = T.Ret(m);
        return w * 31 + v;
    }

    static readonly long[] Longs =
    {
        0, 1, -1, 0x7FFFFFFF, 0x80000000, 0xFFFFFFFF, 0x100000000, 0x17FFFFFFF, 0x180000000,
        -0x80000000L, -0x80000001L, long.MaxValue, long.MinValue, 0x123456789ABCDEF0,
        unchecked((long)0xFEDCBA9876543210), 0x5555555AAAAAAAA5,
    };

    static void Callers<T>(string name) where T : struct, ICall
    {
        int* buf = stackalloc int[12];
        bool one = true, three = true, twelve = true, ret = true;
        foreach (long l in Longs)
        {
            one &= One<T>(l) == Low(l);
            one &= OneSecond<T>(~l, l) == Low(l);
            one &= OneUint<T>(l) == Low(l);
            ret &= Returned<T>(l) == Low(l);
            foreach (long m in Longs)
            {
                three &= Three<T>(l, m) == ThreeRef(l, m);
                twelve &= Twelve<T>(l, m, buf) == RefHash12(buf);
                ret &= ReturnedTwice<T>(l, m) == Low(l + m) * 31 + Low(m);
            }
        }
        Check(name + ": one argument", one);
        Check(name + ": after calls", three);
        Check(name + ": twelve arguments", twelve);
        Check(name + ": return value", ret);
    }

    // ---- libc sees the register ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static nint NativeInt(long l) => labs_int((int)l);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static nint NativeUint(long pad, long l) => labs_uint((uint)l) + labs_uint((uint)(pad & 0xFFFFFFFFL));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long NativeAbs(long l) => abs((int)l);

    [MethodImpl(NoOpt)]
    static nint RefLabs(long l)
    {
        nint v = (nint)Low(l);
        return v < 0 ? -v : v;
    }

    // The difference is computed in 64 bits; only its low half is the result.
    [UnmanagedCallersOnly]
    static int CmpWide(void* a, void* b)
    {
        ulong d = (ulong)(uint)(*(int*)a - *(int*)b);
        return (int)d;
    }

    [UnmanagedCallersOnly]
    static int CmpLow(void* a, void* b)
    {
        long d = ((long)*(int*)a << 32 | 5) - ((long)*(int*)b << 32 | 5);
        return (int)(d >> 32);
    }

    static bool Sorted(int seed, delegate* unmanaged<void*, void*, int> cmp)
    {
        int* data = stackalloc int[24];
        uint x = (uint)seed * 2654435761u + 1;
        for (int i = 0; i < 24; i++)
        {
            x = x * 1664525 + 1013904223;
            data[i] = (int)(x >> 12) - 500000;
        }
        qsort(data, 24, 4, cmp);
        for (int i = 1; i < 24; i++)
            if (data[i - 1] > data[i]) return false;
        return true;
    }

    static void Native()
    {
        bool ok = true, okAbs = true;
        foreach (long l in Longs)
        {
            ok &= NativeInt(l) == RefLabs(l);
            ok &= NativeUint(~l, l) == RefLabs(l) + RefLabs(~l);
            long want = Low(l);
            if (want < 0) want = Low(-want);
            okAbs &= NativeAbs(l) == want;
        }
        Check("libc: int and uint arguments", ok);
        Check("libc: int return value", okAbs);

        ok = true;
        for (int seed = 0; seed < 6; seed++)
            ok &= Sorted(seed, &CmpWide) && Sorted(seed, &CmpLow);
        Check("libc: int returned by a comparer", ok);
    }

    // ---- methods that use their parameters ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Uses(int a, uint b, int c, long[] tab)
    {
        long h = 0;
        h = h * 31 + (a < c ? 1 : 2);
        h = h * 31 + (b < (uint)c ? 3 : 4);
        h = h * 31 + (a == c ? 5 : 6);
        h = h * 31 + ((uint)a >= b ? 7 : 8);
        h = h * 31 + a;
        h = h * 31 + b;
        h = h * 31 + (long)(ulong)a;
        h = h * 31 + (a >> (c & 31));
        h = h * 31 + (int)(b >> (c & 31));
        h = h * 31 + ((long)a << 7);
        h = h * 31 + ((long)b << 33);
        h = h * 31 + a / ((c & 0xff) + 1);
        h = h * 31 + (int)(b / (uint)((c & 0xff) + 1));
        h = h * 31 + a % ((c & 0xff) + 1);
        h = h * 31 + (long)a * c;
        h = h * 31 + (long)((ulong)b * b);
        h = h * 31 + tab[a & 15] + tab[(int)(b & 15)];
        h = h * 31 + ((uint)a < (uint)tab.Length ? tab[a] : -1);
        h = h * 31 + (long)(nint)a + (long)(nuint)b;
        h = h * 31 + (a + c) + (long)(b + (uint)c);
        h = h * 31 + (a * c) + (long)(b * 3u);
        h = h * 31 + -a + ~b;
        h = h * 31 + (a > 0 ? a : -a);
        h = h * 31 + (a < 0 ? 9 : 10) + (b > 0x7FFFFFFFu ? 11 : 12) + (a <= -1 ? 13 : 14);
        return h;
    }

    [MethodImpl(NoOpt)]
    static long UsesRef(int a, uint b, int c, long[] tab)
    {
        long h = 0;
        h = h * 31 + (a < c ? 1 : 2);
        h = h * 31 + (b < (uint)c ? 3 : 4);
        h = h * 31 + (a == c ? 5 : 6);
        h = h * 31 + ((uint)a >= b ? 7 : 8);
        h = h * 31 + a;
        h = h * 31 + b;
        h = h * 31 + (long)(ulong)a;
        h = h * 31 + (a >> (c & 31));
        h = h * 31 + (int)(b >> (c & 31));
        h = h * 31 + ((long)a << 7);
        h = h * 31 + ((long)b << 33);
        h = h * 31 + a / ((c & 0xff) + 1);
        h = h * 31 + (int)(b / (uint)((c & 0xff) + 1));
        h = h * 31 + a % ((c & 0xff) + 1);
        h = h * 31 + (long)a * c;
        h = h * 31 + (long)((ulong)b * b);
        h = h * 31 + tab[a & 15] + tab[(int)(b & 15)];
        h = h * 31 + ((uint)a < (uint)tab.Length ? tab[a] : -1);
        h = h * 31 + (long)(nint)a + (long)(nuint)b;
        h = h * 31 + (a + c) + (long)(b + (uint)c);
        h = h * 31 + (a * c) + (long)(b * 3u);
        h = h * 31 + -a + ~b;
        h = h * 31 + (a > 0 ? a : -a);
        h = h * 31 + (a < 0 ? 9 : 10) + (b > 0x7FFFFFFFu ? 11 : 12) + (a <= -1 ? 13 : 14);
        return h;
    }

    // An index that is out of range must throw, whatever its sign.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Index(long[] arr, int i, uint u)
    {
        long r = 0;
        try { r += arr[i]; } catch (IndexOutOfRangeException) { r += 1000; }
        try { r += arr[u] * 3; } catch (IndexOutOfRangeException) { r += 3000; }
        return r;
    }

    [MethodImpl(NoOpt)]
    static long IndexRef(long[] arr, int i, uint u)
    {
        long r = 0;
        r += i >= 0 && i < arr.Length ? arr[i] : 1000;
        r += u < (uint)arr.Length ? arr[u] * 3 : 3000;
        return r;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long PointerIndex(long* mid, int i, uint u) => mid[i] * 3 + mid[u] + *(mid + i) + *(mid - i);

    [MethodImpl(NoOpt)]
    static long PointerIndexRef(long* mid, int i, uint u) => mid[i] * 3 + mid[u] + *(mid + i) + *(mid - i);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Switch(int a, uint b)
    {
        int r;
        switch (a)
        {
            case -2: r = 11; break;
            case -1: r = 12; break;
            case 0: r = 13; break;
            case 1: r = 14; break;
            case 2: r = 15; break;
            case 3: r = 16; break;
            default: r = 17; break;
        }
        switch (b)
        {
            case 0: return r + 100;
            case 1: return r + 200;
            case 2: return r + 300;
            case 3: return r + 400;
            case 4: return r + 500;
            default: return r + 600;
        }
    }

    [MethodImpl(NoOpt)]
    static int SwitchRef(int a, uint b)
    {
        int r = a >= -2 && a <= 3 ? 13 + a : 17;
        return r + (b <= 4 ? (int)(b + 1) * 100 : 600);
    }

    static readonly int[] Ints =
    {
        0, 1, 2, 3, -1, -2, 15, 16, 0x7F, 0x80, 0xFF, 0x100, 0x7FFF, 0x8000, 0xFFFF, 0x10000,
        -128, -129, -32768, -32769, int.MaxValue, int.MinValue, int.MaxValue - 1, int.MinValue + 1,
        0x12345678, unchecked((int)0x87654321),
    };

    static void Methods()
    {
        long[] tab = new long[16];
        for (int i = 0; i < tab.Length; i++) tab[i] = (i + 1) * 0x0101010101010101L;
        long* mem = stackalloc long[9];
        for (int i = 0; i < 9; i++) mem[i] = (i + 3) * 0x1000000010001L;

        bool uses = true, index = true, sw = true, ptr = true;
        foreach (int a in Ints)
            foreach (int b in Ints)
            {
                uses &= Uses(a, (uint)b, b, tab) == UsesRef(a, (uint)b, b, tab);
                uses &= Uses(b, (uint)a, 7, tab) == UsesRef(b, (uint)a, 7, tab);
                sw &= Switch(a, (uint)b) == SwitchRef(a, (uint)b);
            }
        foreach (int a in Ints)
        {
            index &= Index(tab, a, (uint)a) == IndexRef(tab, a, (uint)a);
            index &= Index(tab, -a, (uint)a + 14) == IndexRef(tab, -a, (uint)a + 14);
        }
        for (int i = -4; i <= 4; i++)
            ptr &= PointerIndex(mem + 4, i, (uint)(i + 4) / 2) == PointerIndexRef(mem + 4, i, (uint)(i + 4) / 2);
        Check("methods: operations", uses);
        Check("methods: array index", index);
        Check("methods: pointer index", ptr);
        Check("methods: switch", sw);
    }

    // ---- struct parameters: fields share a register ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Fields(I1 s, I2 t, U2 u, S4 v, int x)
        => s.A + (long)t.A * 3 + (long)t.B * 5 + (long)u.A * 7 + (long)u.B * 11 + (long)(int)u.A * 13
           + v.A * 17L + v.B * 19L + (long)v.C * 23 + x + (t.A < t.B ? 29 : 31) + (u.A < u.B ? 37 : 41);

    [MethodImpl(NoOpt)]
    static long FieldsRef(I1 s, I2 t, U2 u, S4 v, int x)
        => s.A + (long)t.A * 3 + (long)t.B * 5 + (long)u.A * 7 + (long)u.B * 11 + (long)(int)u.A * 13
           + v.A * 17L + v.B * 19L + (long)v.C * 23 + x + (t.A < t.B ? 29 : 31) + (u.A < u.B ? 37 : 41);

    static void Structs()
    {
        bool ok = true;
        foreach (int a in Ints)
            foreach (int b in Ints)
            {
                I1 s = new I1 { A = a };
                I2 t = new I2 { A = a, B = b };
                U2 u = new U2 { A = (uint)b, B = (uint)a };
                S4 v = new S4 { A = (short)a, B = (ushort)b, C = a ^ b };
                ok &= Fields(s, t, u, v, b) == FieldsRef(s, t, u, v, b);
            }
        Check("struct fields", ok);
    }

    static int Main()
    {
        Setup();
        Callers<Direct>("direct");
        Callers<Instance>("instance");
        Callers<Virtual>("virtual");
        Callers<Interface>("interface");
        Callers<ClosedDelegate>("closed delegate");
        Callers<StaticDelegate>("static delegate");
        Callers<SharedGeneric>("shared generic");
        Callers<Pointer>("function pointer");
        Callers<Unmanaged>("unmanaged pointer");
        Native();
        Methods();
        Structs();

        Console.WriteLine(fails == 0 ? "ALL PASS" : "FAILED " + fails);
        return fails;
    }
}
