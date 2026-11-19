// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// 64-bit arguments, return values and locals across calls on a target whose
// registers hold 32 bits (riscv32).
//
// A long, a ulong and a double (soft-float: a double is moved around as a
// long) take two registers there. The managed convention passes one in the
// next two argument registers a0..a7, wherever they start; with one register
// left the value goes wholly to the stack and that register stays unused,
// also for the arguments that follow; on the stack a 64-bit scalar is
// aligned to 8 bytes. A struct of two words is passed like that too, except
// that it is split between the last register and the stack. Larger structs
// go by reference. A long is returned in a0/a1, as is a two-word struct.
//
// What can go wrong: caller and callee disagreeing on any of this - which
// pair of registers, whether the last register is skipped, the padding in
// front of a stack long, the offset of whatever follows it; a callee that
// homes only one half of a parameter or homes the halves swapped; a hidden
// argument (this, a return buffer, a generic context, a delegate's target)
// that shifts the pairs; a thunk that moves arguments by one register and
// has to move a long from the stack into a pair or back; a tail call that
// writes its outgoing stack longs over incoming ones it still reads; a long
// live across a call with only one half saved; a long written in a try
// region and read in its handler from a stale half.
//
// Every callee reports the arguments it received to a log in the order of
// its signature and returns one of them; the caller compares the log and the
// returned value with what it passed, half by half through memory. The
// values are distinct in every half, so a swapped or shifted half shows.
// Signatures put a long after 0..10 ints, several longs in a row, longs and
// ints mixed, ulongs and doubles, structs of one long, two ints, two longs
// and int+long, and are called directly, on an instance, virtually, through
// an interface, through delegates on static and instance methods, through
// managed and unmanaged function pointers, as generic methods, recursively
// and as tail calls. libc is called with 64-bit arguments and results.
// Longs are kept live across calls in more locals than there are
// callee-saved registers, and are written and read across try, catch,
// finally and filter. The atomic helpers for long are used as well.
//
// Each group prints the number of checks; a failed check prints one line
// with the expected and the actual value. The run ends with ALL PASS or a
// FAIL count, and the guest returns 1 on failure.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

[assembly: DisableRuntimeMarshalling]

unsafe static class R
{
    const MethodImplOptions NI = MethodImplOptions.NoInlining;

    [MethodImpl(NI)] public static uint L(long x, int i) => ((ushort*)&x)[i];
    [MethodImpl(NI)] public static uint Lo(long x) => ((uint*)&x)[0];
    [MethodImpl(NI)] public static uint Hi(long x) => ((uint*)&x)[1];

    [MethodImpl(NI)]
    public static long C(uint hi, uint lo)
    {
        long r = 0;
        uint* p = (uint*)&r;
        p[0] = lo; p[1] = hi;
        return r;
    }

    public static long FromS(int x) => C(x < 0 ? 0xFFFFFFFFu : 0u, (uint)x);

    [MethodImpl(NI)] public static bool Same(long a, long b) => Lo(a) == Lo(b) && Hi(a) == Hi(b);

    public static string Hex(long x) => "0x" + Hi(x).ToString("x8") + Lo(x).ToString("x8");

    [MethodImpl(NI)]
    public static long Add(long a, long b)
    {
        uint lo = Lo(a) + Lo(b);
        uint hi = Hi(a) + Hi(b) + (lo < Lo(b) ? 1u : 0u);
        return C(hi, lo);
    }
}

struct W { public long V; }
struct P { public int A; public int B; }
struct Big { public long A; public long B; }
struct Mixed { public int A; public long B; }
struct Three { public int A; public int B; public int C; }

unsafe delegate long D8(int i0, int i1, int i2, int i3, int i4, int i5, int i6, long a);
unsafe delegate long DM(long a, int i, long b, int j, long c, int k, long d, int l, long e);
unsafe delegate long D1(long a);

class E : Exception { public long Code; public E(long c) : base("e") { Code = c; } }

interface ICalls
{
    long I8(int i0, int i1, int i2, int i3, int i4, int i5, int i6, long a);
    long IM(long a, int i, long b, int j, long c);
}

abstract class Base
{
    public abstract long V7(int i0, int i1, int i2, int i3, int i4, int i5, long a);
    public abstract long V8(int i0, int i1, int i2, int i3, int i4, int i5, int i6, long a);
    public abstract W VW(long a, W w, long b);
}

sealed class Inst : Base, ICalls
{
    public long K;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public long M1(long a) { Program.G(K); Program.G(a); return a; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public long M6(int i0, int i1, int i2, int i3, int i4, long a)
    { Program.G(K); Program.G(i0); Program.G(i1); Program.G(i2); Program.G(i3); Program.G(i4); Program.G(a); return a; }

    // this + six ints: one register left for the long.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public long M7(int i0, int i1, int i2, int i3, int i4, int i5, long a, int t)
    { Program.G(K); Program.G(i0); Program.G(i1); Program.G(i2); Program.G(i3); Program.G(i4); Program.G(i5); Program.G(a); Program.G(t); return a; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public long M4L(long a, long b, long c, long d)
    { Program.G(K); Program.G(a); Program.G(b); Program.G(c); Program.G(d); return d; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override long V7(int i0, int i1, int i2, int i3, int i4, int i5, long a)
    { Program.G(K); Program.G(i0); Program.G(i1); Program.G(i2); Program.G(i3); Program.G(i4); Program.G(i5); Program.G(a); return a; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override long V8(int i0, int i1, int i2, int i3, int i4, int i5, int i6, long a)
    { Program.G(K); Program.G(i0); Program.G(i1); Program.G(i2); Program.G(i3); Program.G(i4); Program.G(i5); Program.G(i6); Program.G(a); return a; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override W VW(long a, W w, long b) { Program.G(K); Program.G(a); Program.G(w.V); Program.G(b); return w; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public long I8(int i0, int i1, int i2, int i3, int i4, int i5, int i6, long a)
    { Program.G(K); Program.G(i0); Program.G(i1); Program.G(i2); Program.G(i3); Program.G(i4); Program.G(i5); Program.G(i6); Program.G(a); return a; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public long IM(long a, int i, long b, int j, long c)
    { Program.G(K); Program.G(a); Program.G(i); Program.G(b); Program.G(j); Program.G(c); return c; }
}

// A second implementation, so that the calls stay virtual.
sealed class Other : Base, ICalls
{
    public override long V7(int i0, int i1, int i2, int i3, int i4, int i5, long a) => 1;
    public override long V8(int i0, int i1, int i2, int i3, int i4, int i5, int i6, long a) => 2;
    public override W VW(long a, W w, long b) => default;
    public long I8(int i0, int i1, int i2, int i3, int i4, int i5, int i6, long a) => 3;
    public long IM(long a, int i, long b, int j, long c) => 4;
}

unsafe class Program
{
    const MethodImplOptions NI = MethodImplOptions.NoInlining;

    static int fails;
    static int checks;

    static readonly long[] s_got = new long[64];
    static int s_n;

    // The log of received arguments.
    [MethodImpl(NI)] public static void G(long x) { if (s_n < s_got.Length) s_got[s_n] = x; s_n++; }
    [MethodImpl(NI)] public static void G(int x) { G(R.FromS(x)); }

    static void Fail(string name, string exp, string got)
    {
        fails++;
        if (fails <= 60) Console.WriteLine("FAIL " + name + " expected " + exp + " got " + got);
    }

    static void Check(string name, long got, long exp)
    {
        checks++;
        if (!R.Same(got, exp)) Fail(name, R.Hex(exp), R.Hex(got));
    }

    static void CheckI(string name, int got, int exp)
    {
        checks++;
        if (got != exp) Fail(name, exp.ToString(), got.ToString());
    }

    // The log holds exactly `exp`.
    static void Log(string name, params long[] exp)
    {
        checks++;
        if (s_n != exp.Length)
        {
            Fail(name + " (count)", exp.Length.ToString(), s_n.ToString());
        }
        else
        {
            for (int i = 0; i < exp.Length; i++)
            {
                if (!R.Same(s_got[i], exp[i]))
                {
                    Fail(name + " (argument " + i.ToString() + ")", R.Hex(exp[i]), R.Hex(s_got[i]));
                    break;
                }
            }
        }
        s_n = 0;
    }

    static void Group(string name)
    {
        Console.WriteLine("ok   " + name + " " + checks.ToString());
    }

    // Distinct values: long k has four distinct 16-bit limbs, int k alternates in sign.
    static long X(int k) => R.C(0xA1B2C3D4u + (uint)k * 0x01010101u, 0x0E1F2031u + (uint)k * 0x10101010u);
    static int N(int k) => (k & 1) == 0 ? 0x11110000 + k * 0x111 : -(0x22220000 + k * 0x111);
    static long NL(int k) => R.FromS(N(k));
    static double Dbl(int k) => BitConverter.Int64BitsToDouble(X(k));
    static long Bits(double d) => BitConverter.DoubleToInt64Bits(d);

    // ---- a long after 0..10 ints ----

    [MethodImpl(NI)] static long S0(long a) { G(a); return a; }
    [MethodImpl(NI)] static long S1(int i0, long a) { G(i0); G(a); return a; }
    [MethodImpl(NI)] static long S2(int i0, int i1, long a) { G(i0); G(i1); G(a); return a; }
    [MethodImpl(NI)] static long S3(int i0, int i1, int i2, long a) { G(i0); G(i1); G(i2); G(a); return a; }
    [MethodImpl(NI)] static long S4(int i0, int i1, int i2, int i3, long a) { G(i0); G(i1); G(i2); G(i3); G(a); return a; }
    [MethodImpl(NI)] static long S5(int i0, int i1, int i2, int i3, int i4, long a) { G(i0); G(i1); G(i2); G(i3); G(i4); G(a); return a; }
    [MethodImpl(NI)] static long S6(int i0, int i1, int i2, int i3, int i4, int i5, long a) { G(i0); G(i1); G(i2); G(i3); G(i4); G(i5); G(a); return a; }
    [MethodImpl(NI)] static long S7(int i0, int i1, int i2, int i3, int i4, int i5, int i6, long a) { G(i0); G(i1); G(i2); G(i3); G(i4); G(i5); G(i6); G(a); return a; }
    [MethodImpl(NI)] static long S8(int i0, int i1, int i2, int i3, int i4, int i5, int i6, int i7, long a) { G(i0); G(i1); G(i2); G(i3); G(i4); G(i5); G(i6); G(i7); G(a); return a; }
    [MethodImpl(NI)] static long S9(int i0, int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, long a) { G(i0); G(i1); G(i2); G(i3); G(i4); G(i5); G(i6); G(i7); G(i8); G(a); return a; }
    [MethodImpl(NI)] static long S10(int i0, int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9, long a) { G(i0); G(i1); G(i2); G(i3); G(i4); G(i5); G(i6); G(i7); G(i8); G(i9); G(a); return a; }

    // What follows a long that left the last register unused.
    [MethodImpl(NI)] static long S7T(int i0, int i1, int i2, int i3, int i4, int i5, int i6, long a, int t) { G(i0); G(i1); G(i2); G(i3); G(i4); G(i5); G(i6); G(a); G(t); return a; }
    [MethodImpl(NI)] static long S7TT(int i0, int i1, int i2, int i3, int i4, int i5, int i6, long a, int t, long b, int u) { G(i0); G(i1); G(i2); G(i3); G(i4); G(i5); G(i6); G(a); G(t); G(b); G(u); return b; }
    [MethodImpl(NI)] static long S9T(int i0, int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, long a, int t, long b) { G(i0); G(i1); G(i2); G(i3); G(i4); G(i5); G(i6); G(i7); G(i8); G(a); G(t); G(b); return b; }
    [MethodImpl(NI)] static int S7I(int i0, int i1, int i2, int i3, int i4, int i5, int i6, long a, int t) { G(i0); G(i1); G(i2); G(i3); G(i4); G(i5); G(i6); G(a); G(t); return t; }

    // ---- longs in a row and mixed ----

    [MethodImpl(NI)] static long L2(long a, long b) { G(a); G(b); return b; }
    [MethodImpl(NI)] static long L4(long a, long b, long c, long d) { G(a); G(b); G(c); G(d); return d; }
    [MethodImpl(NI)] static long L5(long a, long b, long c, long d, long e) { G(a); G(b); G(c); G(d); G(e); return e; }
    [MethodImpl(NI)] static long L6(long a, long b, long c, long d, long e, long f) { G(a); G(b); G(c); G(d); G(e); G(f); return f; }
    [MethodImpl(NI)] static long IL4(int i, long a, long b, long c, long d) { G(i); G(a); G(b); G(c); G(d); return d; }
    [MethodImpl(NI)] static long IL4T(int i, long a, long b, long c, long d, int t) { G(i); G(a); G(b); G(c); G(d); G(t); return c; }
    [MethodImpl(NI)] static long Mix(long a, int i, long b, int j, long c, int k, long d, int l, long e) { G(a); G(i); G(b); G(j); G(c); G(k); G(d); G(l); G(e); return d; }
    [MethodImpl(NI)] static ulong U3(ulong a, int i, ulong b, int j, int k, int l, ulong c) { G((long)a); G(i); G((long)b); G(j); G(k); G(l); G((long)c); return c; }
    [MethodImpl(NI)] static long Small(byte b, long a, short s, long c, bool f, char ch, long d) { G(b); G(a); G(s); G(c); G(f ? 1 : 0); G(ch); G(d); return d; }

    // ---- doubles (two registers, soft-float) ----

    [MethodImpl(NI)] static double Dd(double a, int i, double b) { G(Bits(a)); G(i); G(Bits(b)); return b; }
    [MethodImpl(NI)] static double D7(int i0, int i1, int i2, int i3, int i4, int i5, int i6, double a, int t) { G(i0); G(i1); G(i2); G(i3); G(i4); G(i5); G(i6); G(Bits(a)); G(t); return a; }
    [MethodImpl(NI)] static double DL(long a, double b, long c, double d, double e) { G(a); G(Bits(b)); G(c); G(Bits(d)); G(Bits(e)); return e; }
    [MethodImpl(NI)] static float Ff(float a, long b, float c) { G(BitConverter.SingleToInt32Bits(a)); G(b); G(BitConverter.SingleToInt32Bits(c)); return c; }

    // ---- structs ----

    [MethodImpl(NI)] static W SW(W w) { G(w.V); return w; }
    [MethodImpl(NI)] static W SW7(int i0, int i1, int i2, int i3, int i4, int i5, int i6, W w, int t) { G(i0); G(i1); G(i2); G(i3); G(i4); G(i5); G(i6); G(w.V); G(t); return w; }
    [MethodImpl(NI)] static P SP7(int i0, int i1, int i2, int i3, int i4, int i5, int i6, P p, long a) { G(i0); G(i1); G(i2); G(i3); G(i4); G(i5); G(i6); G(p.A); G(p.B); G(a); return p; }
    [MethodImpl(NI)] static W SW6(int i0, int i1, int i2, int i3, int i4, int i5, W w, long a) { G(i0); G(i1); G(i2); G(i3); G(i4); G(i5); G(w.V); G(a); return w; }
    [MethodImpl(NI)] static Big SBig(long a, Big b, long c) { G(a); G(b.A); G(b.B); G(c); return b; }
    [MethodImpl(NI)] static Mixed SMixed(int i, Mixed m, long c) { G(i); G(m.A); G(m.B); G(c); return m; }
    [MethodImpl(NI)] static Three SThree(long a, Three t, long c) { G(a); G(t.A); G(t.B); G(t.C); G(c); return t; }
    [MethodImpl(NI)] static (long, long) Tuple2(long a, long b) { G(a); G(b); return (b, a); }
    [MethodImpl(NI)] static (int, long) TupleIL(int i, long b) { G(i); G(b); return (i, b); }
    [MethodImpl(NI)] static long ByRef(ref long a, in long b, out long c, long* d) { G(a); G(b); G(*d); c = a; a = b; return *d; }
    [MethodImpl(NI)] static long WSum(W a, W b, W c, W d, W e) { G(a.V); G(b.V); G(c.V); G(d.V); G(e.V); return e.V; }

    // ---- generics ----

    [MethodImpl(NI)] static T Gen7<T>(int i0, int i1, int i2, int i3, int i4, int i5, int i6, T a) { G(i0); G(i1); G(i2); G(i3); G(i4); G(i5); G(i6); return a; }
    [MethodImpl(NI)] static long GenShared<T>(T o, long a, int i, long b) where T : class { G(o == null ? 0 : 1); G(a); G(i); G(b); return b; }
    [MethodImpl(NI)] static T GenRet<T>(long a, T b, long c) { G(a); G(c); return b; }

    // ---- tail calls ----

    [MethodImpl(NI)] static long Tail7(int i0, int i1, int i2, int i3, int i4, int i5, int i6, long a) => S7(i6, i5, i4, i3, i2, i1, i0, a);
    [MethodImpl(NI)] static long Tail7T(int i0, int i1, int i2, int i3, int i4, int i5, int i6, long a, int t, long b, int u) => S7TT(i0, i1, i2, i3, i4, i5, i6, b, u, a, t);
    [MethodImpl(NI)] static long Tail5(long a, long b, long c, long d, long e) => L5(e, d, c, b, a);
    [MethodImpl(NI)] static long TailMix(long a, int i, long b, int j, long c, int k, long d, int l, long e) => Mix(e, l, d, k, c, j, b, i, a);
    [MethodImpl(NI)] static long TailFewer(int i0, int i1, int i2, int i3, int i4, int i5, int i6, long a, int t, long b, int u) => S7(i0, i1, i2, i3, i4, i5, i6, b);

    // ---- recursion with stack longs ----

    [MethodImpl(NI)]
    static long Rec(int depth, long a, long b, long c, int i, long d, long e)
    {
        if (depth == 0) { G(a); G(b); G(c); G(i); G(d); G(e); return e; }
        return Rec(depth - 1, b, c, d, i + 1, e, a);
    }

    // ---- unmanaged callers ----

    [UnmanagedCallersOnly] static long Um7(int i0, int i1, int i2, int i3, int i4, int i5, int i6, long a) { G(i0); G(i1); G(i2); G(i3); G(i4); G(i5); G(i6); G(a); return a; }
    [UnmanagedCallersOnly] static long Um3(long a, int i, long b) { G(a); G(i); G(b); return a; }

    [StructLayout(LayoutKind.Sequential)]
    struct LLDiv { public long Quot; public long Rem; }

    [DllImport("*")] static extern long llabs(long x);
    [DllImport("*")] static extern long strtoll(byte* s, byte** end, int radix);
    [DllImport("*")] static extern ulong strtoull(byte* s, byte** end, int radix);
    [DllImport("*")] static extern long atoll(byte* s);
    [DllImport("*")] static extern LLDiv lldiv(long a, long b);

    // ---- longs live across calls ----

    [MethodImpl(NI)] static void Opaque() { s_n = 0; }
    [MethodImpl(NI)] static long Id(long x) => x;

    [MethodImpl(NI)]
    static void LiveAcross(long a, long b, long c, long d)
    {
        long l0 = a, l1 = b, l2 = c, l3 = d;
        long l4 = Id(d), l5 = Id(c), l6 = Id(b), l7 = Id(a);
        Opaque();
        long l8 = Id(l0), l9 = Id(l1);
        Opaque();
        G(l0); G(l1); G(l2); G(l3); G(l4); G(l5); G(l6); G(l7); G(l8); G(l9);
    }

    [MethodImpl(NI)]
    static long LiveInLoop(long a, long b, int n)
    {
        long x = a, y = b;
        for (int i = 0; i < n; i++)
        {
            long t = Id(x);
            x = y;
            y = t;
            Opaque();
        }
        G(x); G(y);
        return y;
    }

    // ---- longs across exception handlers ----

    [MethodImpl(NI)] static void Throw(long code) { throw new E(code); }
    [MethodImpl(NI)] static void MayThrow(long code, bool really) { if (really) throw new E(code); }

    [MethodImpl(NI)]
    static long Catch(long a, long b, bool really)
    {
        long x = a;
        try
        {
            x = b;
            MayThrow(a, really);
            x = Id(a);
        }
        catch (E e)
        {
            G(e.Code);
            G(x);
            return x;
        }
        G(x);
        return x;
    }

    [MethodImpl(NI)]
    static long Finally(long a, long b, long c, bool really)
    {
        long x = a, y = c;
        try
        {
            try
            {
                x = b;
                MayThrow(c, really);
                y = a;
            }
            finally
            {
                G(x);
                G(y);
                x = c;
            }
        }
        catch (E e)
        {
            G(e.Code);
        }
        G(x);
        G(y);
        return x;
    }

    [MethodImpl(NI)]
    static long Filter(long a, long b)
    {
        long x = a;
        try
        {
            x = b;
            Throw(a);
        }
        catch (E e) when (R.Same(e.Code, a) && R.Same(x, b))
        {
            G(x);
            G(a);
            G(b);
            return e.Code;
        }
        return 0;
    }

    // Stack parameters read in a handler.
    [MethodImpl(NI)]
    static long CatchStackArgs(int i0, int i1, int i2, int i3, int i4, int i5, int i6, long a, int t, long b)
    {
        try
        {
            Throw(b);
        }
        catch (E e)
        {
            G(i0); G(i6); G(a); G(t); G(b); G(e.Code);
            return a;
        }
        return 0;
    }

    [MethodImpl(NI)]
    static long Rethrow(long a, long b)
    {
        long x = a;
        try
        {
            try { x = b; Throw(a); }
            catch (E) { x = Id(x); G(x); throw; }
        }
        catch (E e) { G(e.Code); G(x); }
        return x;
    }

    static long s_cell;

    static void TestPositions()
    {
        long a = X(1), b = X(2);
        int n0 = N(0), n1 = N(1), n2 = N(2), n3 = N(3), n4 = N(4), n5 = N(5), n6 = N(6), n7 = N(7), n8 = N(8), n9 = N(9);

        Check("S0 ret", S0(a), a); Log("S0", a);
        Check("S1 ret", S1(n0, a), a); Log("S1", NL(0), a);
        Check("S2 ret", S2(n0, n1, a), a); Log("S2", NL(0), NL(1), a);
        Check("S3 ret", S3(n0, n1, n2, a), a); Log("S3", NL(0), NL(1), NL(2), a);
        Check("S4 ret", S4(n0, n1, n2, n3, a), a); Log("S4", NL(0), NL(1), NL(2), NL(3), a);
        Check("S5 ret", S5(n0, n1, n2, n3, n4, a), a); Log("S5", NL(0), NL(1), NL(2), NL(3), NL(4), a);
        Check("S6 ret", S6(n0, n1, n2, n3, n4, n5, a), a); Log("S6", NL(0), NL(1), NL(2), NL(3), NL(4), NL(5), a);
        Check("S7 ret", S7(n0, n1, n2, n3, n4, n5, n6, a), a); Log("S7", NL(0), NL(1), NL(2), NL(3), NL(4), NL(5), NL(6), a);
        Check("S8 ret", S8(n0, n1, n2, n3, n4, n5, n6, n7, a), a); Log("S8", NL(0), NL(1), NL(2), NL(3), NL(4), NL(5), NL(6), NL(7), a);
        Check("S9 ret", S9(n0, n1, n2, n3, n4, n5, n6, n7, n8, a), a); Log("S9", NL(0), NL(1), NL(2), NL(3), NL(4), NL(5), NL(6), NL(7), NL(8), a);
        Check("S10 ret", S10(n0, n1, n2, n3, n4, n5, n6, n7, n8, n9, a), a); Log("S10", NL(0), NL(1), NL(2), NL(3), NL(4), NL(5), NL(6), NL(7), NL(8), NL(9), a);

        Check("S7T ret", S7T(n0, n1, n2, n3, n4, n5, n6, a, n7), a); Log("S7T", NL(0), NL(1), NL(2), NL(3), NL(4), NL(5), NL(6), a, NL(7));
        Check("S7TT ret", S7TT(n0, n1, n2, n3, n4, n5, n6, a, n7, b, n8), b); Log("S7TT", NL(0), NL(1), NL(2), NL(3), NL(4), NL(5), NL(6), a, NL(7), b, NL(8));
        Check("S9T ret", S9T(n0, n1, n2, n3, n4, n5, n6, n7, n8, a, n9, b), b); Log("S9T", NL(0), NL(1), NL(2), NL(3), NL(4), NL(5), NL(6), NL(7), NL(8), a, NL(9), b);
        CheckI("S7I ret", S7I(n0, n1, n2, n3, n4, n5, n6, a, n7), n7); Log("S7I", NL(0), NL(1), NL(2), NL(3), NL(4), NL(5), NL(6), a, NL(7));

        // Constants as arguments: the halves are materialized in the argument registers.
        Check("S7 const", S7(1, 2, 3, 4, 5, 6, 7, 0x1122334455667788L), R.C(0x11223344, 0x55667788));
        Log("S7 const", R.FromS(1), R.FromS(2), R.FromS(3), R.FromS(4), R.FromS(5), R.FromS(6), R.FromS(7), R.C(0x11223344, 0x55667788));
        Check("S6 const", S6(1, 2, 3, 4, 5, 6, -2L), R.C(0xFFFFFFFF, 0xFFFFFFFE));
        Log("S6 const", R.FromS(1), R.FromS(2), R.FromS(3), R.FromS(4), R.FromS(5), R.FromS(6), R.C(0xFFFFFFFF, 0xFFFFFFFE));
        Group("positions");
    }

    static void TestRows()
    {
        long a = X(1), b = X(2), c = X(3), d = X(4), e = X(5), f = X(6);
        Check("L2 ret", L2(a, b), b); Log("L2", a, b);
        Check("L4 ret", L4(a, b, c, d), d); Log("L4", a, b, c, d);
        Check("L5 ret", L5(a, b, c, d, e), e); Log("L5", a, b, c, d, e);
        Check("L6 ret", L6(a, b, c, d, e, f), f); Log("L6", a, b, c, d, e, f);
        Check("IL4 ret", IL4(N(0), a, b, c, d), d); Log("IL4", NL(0), a, b, c, d);
        Check("IL4T ret", IL4T(N(0), a, b, c, d, N(1)), c); Log("IL4T", NL(0), a, b, c, d, NL(1));
        Check("Mix ret", Mix(a, N(0), b, N(1), c, N(2), d, N(3), e), d); Log("Mix", a, NL(0), b, NL(1), c, NL(2), d, NL(3), e);
        Check("U3 ret", (long)U3((ulong)a, N(0), (ulong)b, N(1), N(2), N(3), (ulong)c), c); Log("U3", a, NL(0), b, NL(1), NL(2), NL(3), c);
        Check("Small ret", Small(0xFE, a, -2, b, true, '￾', c), c);
        Log("Small", R.FromS(0xFE), a, R.FromS(-2), b, R.FromS(1), R.FromS(0xFFFE), c);

        Check("Dd ret", Bits(Dd(Dbl(1), N(0), Dbl(2))), b); Log("Dd", a, NL(0), b);
        Check("D7 ret", Bits(D7(N(0), N(1), N(2), N(3), N(4), N(5), N(6), Dbl(1), N(7))), a);
        Log("D7", NL(0), NL(1), NL(2), NL(3), NL(4), NL(5), NL(6), a, NL(7));
        Check("DL ret", Bits(DL(a, Dbl(2), c, Dbl(4), Dbl(5))), e); Log("DL", a, b, c, d, e);
        float f1 = BitConverter.Int32BitsToSingle(0x40490FDB), f2 = BitConverter.Int32BitsToSingle(unchecked((int)0xC2F6E979));
        CheckI("Ff ret", BitConverter.SingleToInt32Bits(Ff(f1, a, f2)), unchecked((int)0xC2F6E979));
        Log("Ff", R.FromS(0x40490FDB), a, R.FromS(unchecked((int)0xC2F6E979)));
        Group("rows and doubles");
    }

    static void TestStructs()
    {
        long a = X(1), b = X(2), c = X(3);
        W w = new W { V = X(7) };
        P p = new P { A = N(10), B = N(11) };
        Big big = new Big { A = X(8), B = X(9) };
        Mixed m = new Mixed { A = N(12), B = X(10) };
        Three t = new Three { A = N(13), B = N(14), C = N(15) };

        Check("SW ret", SW(w).V, X(7)); Log("SW", X(7));
        Check("SW7 ret", SW7(N(0), N(1), N(2), N(3), N(4), N(5), N(6), w, N(7)).V, X(7));
        Log("SW7", NL(0), NL(1), NL(2), NL(3), NL(4), NL(5), NL(6), X(7), NL(7));
        P rp = SP7(N(0), N(1), N(2), N(3), N(4), N(5), N(6), p, a);
        CheckI("SP7 ret A", rp.A, N(10)); CheckI("SP7 ret B", rp.B, N(11));
        Log("SP7", NL(0), NL(1), NL(2), NL(3), NL(4), NL(5), NL(6), NL(10), NL(11), a);
        Check("SW6 ret", SW6(N(0), N(1), N(2), N(3), N(4), N(5), w, a).V, X(7));
        Log("SW6", NL(0), NL(1), NL(2), NL(3), NL(4), NL(5), X(7), a);
        Big rb = SBig(a, big, b);
        Check("SBig ret A", rb.A, X(8)); Check("SBig ret B", rb.B, X(9)); Log("SBig", a, X(8), X(9), b);
        Mixed rm = SMixed(N(0), m, c);
        CheckI("SMixed ret A", rm.A, N(12)); Check("SMixed ret B", rm.B, X(10)); Log("SMixed", NL(0), NL(12), X(10), c);
        Three rt = SThree(a, t, b);
        CheckI("SThree ret A", rt.A, N(13)); CheckI("SThree ret B", rt.B, N(14)); CheckI("SThree ret C", rt.C, N(15));
        Log("SThree", a, NL(13), NL(14), NL(15), b);
        (long t1, long t2) = Tuple2(a, b);
        Check("Tuple2 ret 1", t1, b); Check("Tuple2 ret 2", t2, a); Log("Tuple2", a, b);
        (int t3, long t4) = TupleIL(N(1), c);
        CheckI("TupleIL ret 1", t3, N(1)); Check("TupleIL ret 2", t4, c); Log("TupleIL", NL(1), c);

        long ra = a, rc, rd = c;
        Check("ByRef ret", ByRef(ref ra, in b, out rc, &rd), c); Log("ByRef", a, b, c);
        Check("ByRef a", ra, b); Check("ByRef c", rc, a);

        W w1 = new W { V = X(11) }, w2 = new W { V = X(12) }, w3 = new W { V = X(13) }, w4 = new W { V = X(14) };
        Check("WSum ret", WSum(w, w1, w2, w3, w4), X(14)); Log("WSum", X(7), X(11), X(12), X(13), X(14));
        Group("structs");
    }

    static void TestKinds()
    {
        long a = X(1), b = X(2), c = X(3), d = X(4), e = X(5);
        int n0 = N(0), n1 = N(1), n2 = N(2), n3 = N(3), n4 = N(4), n5 = N(5), n6 = N(6);
        Inst inst = new Inst { K = X(20) };
        Base bas = n0 != 0 ? inst : new Other();
        ICalls ic = n0 != 0 ? inst : new Other();

        Check("M1 ret", inst.M1(a), a); Log("M1", X(20), a);
        Check("M6 ret", inst.M6(n0, n1, n2, n3, n4, a), a); Log("M6", X(20), NL(0), NL(1), NL(2), NL(3), NL(4), a);
        Check("M7 ret", inst.M7(n0, n1, n2, n3, n4, n5, a, n6), a); Log("M7", X(20), NL(0), NL(1), NL(2), NL(3), NL(4), NL(5), a, NL(6));
        Check("M4L ret", inst.M4L(a, b, c, d), d); Log("M4L", X(20), a, b, c, d);
        Check("V7 ret", bas.V7(n0, n1, n2, n3, n4, n5, a), a); Log("V7", X(20), NL(0), NL(1), NL(2), NL(3), NL(4), NL(5), a);
        Check("V8 ret", bas.V8(n0, n1, n2, n3, n4, n5, n6, a), a); Log("V8", X(20), NL(0), NL(1), NL(2), NL(3), NL(4), NL(5), NL(6), a);
        Check("VW ret", bas.VW(a, new W { V = b }, c).V, b); Log("VW", X(20), a, b, c);
        Check("I8 ret", ic.I8(n0, n1, n2, n3, n4, n5, n6, a), a); Log("I8", X(20), NL(0), NL(1), NL(2), NL(3), NL(4), NL(5), NL(6), a);
        Check("IM ret", ic.IM(a, n0, b, n1, c), c); Log("IM", X(20), a, NL(0), b, NL(1), c);

        // Delegates: on a static method the target shifts every argument by one register.
        D8 d8s = S7, d8i = inst.I8;
        DM dms = Mix;
        D1 d1s = S0, d1i = inst.M1;
        Check("delegate static S7", d8s(n0, n1, n2, n3, n4, n5, n6, a), a); Log("delegate static S7", NL(0), NL(1), NL(2), NL(3), NL(4), NL(5), NL(6), a);
        Check("delegate instance I8", d8i(n0, n1, n2, n3, n4, n5, n6, a), a); Log("delegate instance I8", X(20), NL(0), NL(1), NL(2), NL(3), NL(4), NL(5), NL(6), a);
        Check("delegate static Mix", dms(a, n0, b, n1, c, n2, d, n3, e), d); Log("delegate static Mix", a, NL(0), b, NL(1), c, NL(2), d, NL(3), e);
        Check("delegate static S0", d1s(a), a); Log("delegate static S0", a);
        Check("delegate instance M1", d1i(b), b); Log("delegate instance M1", X(20), b);
        long captured = c;
        Func<long, int, long, long> lambda = (x, i, y) => { G(x); G(i); G(y); G(captured); return captured; };
        Check("lambda", lambda(a, n0, b), c); Log("lambda", a, NL(0), b, c);

        // Function pointers.
        delegate*<int, int, int, int, int, int, int, long, long> fp7 = &S7;
        delegate*<long, int, long, int, long, int, long, int, long, long> fpm = &Mix;
        delegate*<int, int, int, int, int, int, int, long, int, long> fp7t = &S7T;
        Check("fnptr S7", fp7(n0, n1, n2, n3, n4, n5, n6, a), a); Log("fnptr S7", NL(0), NL(1), NL(2), NL(3), NL(4), NL(5), NL(6), a);
        Check("fnptr Mix", fpm(a, n0, b, n1, c, n2, d, n3, e), d); Log("fnptr Mix", a, NL(0), b, NL(1), c, NL(2), d, NL(3), e);
        Check("fnptr S7T", fp7t(n0, n1, n2, n3, n4, n5, n6, a, n0), a); Log("fnptr S7T", NL(0), NL(1), NL(2), NL(3), NL(4), NL(5), NL(6), a, NL(0));
        delegate* unmanaged<int, int, int, int, int, int, int, long, long> um7 = &Um7;
        delegate* unmanaged<long, int, long, long> um3 = &Um3;
        Check("unmanaged Um7", um7(n0, n1, n2, n3, n4, n5, n6, a), a); Log("unmanaged Um7", NL(0), NL(1), NL(2), NL(3), NL(4), NL(5), NL(6), a);
        Check("unmanaged Um3", um3(a, n0, b), a); Log("unmanaged Um3", a, NL(0), b);

        // Generics.
        Check("Gen7<long>", Gen7<long>(n0, n1, n2, n3, n4, n5, n6, a), a); Log("Gen7<long>", NL(0), NL(1), NL(2), NL(3), NL(4), NL(5), NL(6));
        Check("Gen7<W>", Gen7<W>(n0, n1, n2, n3, n4, n5, n6, new W { V = b }).V, b); Log("Gen7<W>", NL(0), NL(1), NL(2), NL(3), NL(4), NL(5), NL(6));
        Check("Gen7<double>", Bits(Gen7<double>(n0, n1, n2, n3, n4, n5, n6, Dbl(3))), c); Log("Gen7<double>", NL(0), NL(1), NL(2), NL(3), NL(4), NL(5), NL(6));
        Check("GenShared<string>", GenShared<string>("s", a, n0, b), b); Log("GenShared<string>", R.FromS(1), a, NL(0), b);
        Check("GenShared<object>", GenShared<object>(null, a, n0, b), b); Log("GenShared<object>", R.FromS(0), a, NL(0), b);
        Check("GenRet<long>", GenRet<long>(a, b, c), b); Log("GenRet<long>", a, c);
        CheckI("GenRet<string>", GenRet<string>(a, "xyz", c).Length, 3); Log("GenRet<string>", a, c);

        // Tail calls.
        Check("Tail7", Tail7(n0, n1, n2, n3, n4, n5, n6, a), a); Log("Tail7", NL(6), NL(5), NL(4), NL(3), NL(2), NL(1), NL(0), a);
        Check("Tail7T", Tail7T(n0, n1, n2, n3, n4, n5, n6, a, n0, b, n1), a); Log("Tail7T", NL(0), NL(1), NL(2), NL(3), NL(4), NL(5), NL(6), b, NL(1), a, NL(0));
        Check("Tail5", Tail5(a, b, c, d, e), a); Log("Tail5", e, d, c, b, a);
        Check("TailMix", TailMix(a, n0, b, n1, c, n2, d, n3, e), b); Log("TailMix", e, NL(3), d, NL(2), c, NL(1), b, NL(0), a);
        Check("TailFewer", TailFewer(n0, n1, n2, n3, n4, n5, n6, a, n0, b, n1), b); Log("TailFewer", NL(0), NL(1), NL(2), NL(3), NL(4), NL(5), NL(6), b);

        // Recursion: the five longs rotate once per level.
        Check("Rec", Rec(7, a, b, c, n0, d, e), X(((4 + 7) % 5) + 1));
        Log("Rec", X(((0 + 7) % 5) + 1), X(((1 + 7) % 5) + 1), X(((2 + 7) % 5) + 1), R.FromS(n0 + 7), X(((3 + 7) % 5) + 1), X(((4 + 7) % 5) + 1));
        Group("call kinds");
    }

    static void TestNative()
    {
        Check("llabs +", llabs(0x123456789ABCDEFL), R.C(0x01234567, 0x89ABCDEF));
        Check("llabs -", llabs(-0x123456789ABCDEFL), R.C(0x01234567, 0x89ABCDEF));
        Check("llabs lo", llabs(-0x80000000L), R.C(0, 0x80000000));
        Check("llabs carry", llabs(-0x100000000L), R.C(1, 0));

        byte* text = stackalloc byte[32];
        string s = "-81985529216486895";
        for (int i = 0; i < s.Length; i++) text[i] = (byte)s[i];
        text[s.Length] = 0;
        byte* end = null;
        Check("strtoll", strtoll(text, &end, 10), R.C(0xFEDCBA98, 0x76543211));
        CheckI("strtoll end", (int)(end - text), s.Length);
        Check("atoll", atoll(text), R.C(0xFEDCBA98, 0x76543211));
        string h = "fedcba9876543210";
        for (int i = 0; i < h.Length; i++) text[i] = (byte)h[i];
        text[h.Length] = 0;
        Check("strtoull", (long)strtoull(text, null, 16), R.C(0xFEDCBA98, 0x76543210));

        LLDiv q = lldiv(0x123456789ABCDEFL, 0x100000000L);
        Check("lldiv quot", q.Quot, R.C(0, 0x01234567));
        Check("lldiv rem", q.Rem, R.C(0, 0x89ABCDEF));
        q = lldiv(-1000000000000007L, 1000000007L);
        Check("lldiv quot -", q.Quot, R.C(0xFFFFFFFF, 0xFFF0BDC1));
        Check("lldiv rem -", q.Rem, R.C(0xFFFFFFFF, 0xC4D005B2));
        Group("native");
    }

    static void TestLive()
    {
        long a = X(1), b = X(2), c = X(3), d = X(4);
        LiveAcross(a, b, c, d);
        Log("live across", a, b, c, d, d, c, b, a, a, b);
        Check("live in loop even", LiveInLoop(a, b, 6), b); Log("live in loop even", a, b);
        Check("live in loop odd", LiveInLoop(a, b, 7), a); Log("live in loop odd", b, a);
        Group("live across calls");
    }

    static void TestHandlers()
    {
        long a = X(1), b = X(2), c = X(3);
        Check("catch thrown", Catch(a, b, true), b); Log("catch thrown", a, b);
        Check("catch not thrown", Catch(a, b, false), a); Log("catch not thrown", a);
        Check("finally thrown", Finally(a, b, c, true), c); Log("finally thrown", b, c, c, c, c);
        Check("finally not thrown", Finally(a, b, c, false), c); Log("finally not thrown", b, a, c, a);
        Check("filter", Filter(a, b), a); Log("filter", b, a, b);
        Check("catch stack args", CatchStackArgs(N(0), N(1), N(2), N(3), N(4), N(5), N(6), a, N(7), b), a);
        Log("catch stack args", NL(0), NL(6), a, NL(7), b, b);
        Check("rethrow", Rethrow(a, b), b); Log("rethrow", b, a, b);
        Group("handlers");
    }

    static void TestAtomics()
    {
        long a = X(1), b = X(2);
        s_cell = a;
        Check("Interlocked.Read", Interlocked.Read(ref s_cell), a);
        Check("Interlocked.Exchange", Interlocked.Exchange(ref s_cell, b), a);
        Check("Interlocked.Exchange cell", s_cell, b);
        Check("Interlocked.CompareExchange miss", Interlocked.CompareExchange(ref s_cell, a, a), b);
        Check("Interlocked.CompareExchange miss cell", s_cell, b);
        Check("Interlocked.CompareExchange hit", Interlocked.CompareExchange(ref s_cell, a, b), b);
        Check("Interlocked.CompareExchange hit cell", s_cell, a);
        // A compare that differs in one half only must miss.
        Check("Interlocked.CompareExchange low differs", Interlocked.CompareExchange(ref s_cell, b, R.C(R.Hi(a), R.Lo(a) ^ 1)), a);
        Check("Interlocked.CompareExchange high differs", Interlocked.CompareExchange(ref s_cell, b, R.C(R.Hi(a) ^ 1, R.Lo(a))), a);
        Check("Interlocked cell kept", s_cell, a);
        s_cell = R.C(0, 0xFFFFFFFF);
        Check("Interlocked.Increment", Interlocked.Increment(ref s_cell), R.C(1, 0));
        Check("Interlocked.Decrement", Interlocked.Decrement(ref s_cell), R.C(0, 0xFFFFFFFF));
        Check("Interlocked.Add", Interlocked.Add(ref s_cell, a), R.Add(R.C(0, 0xFFFFFFFF), a));
        Volatile.Write(ref s_cell, b);
        Check("Volatile", Volatile.Read(ref s_cell), b);
        object boxed = a;
        Check("box", (long)boxed, a);
        Check("box equals", boxed.Equals((object)X(1)) ? 1 : 0, 1);
        Group("atomics and boxes");
    }

    static int Main()
    {
        TestPositions();
        TestRows();
        TestStructs();
        TestKinds();
        TestNative();
        TestLive();
        TestHandlers();
        TestAtomics();

        if (fails == 0)
        {
            Console.WriteLine("ALL PASS");
            return 0;
        }
        Console.WriteLine("FAIL " + fails.ToString());
        return 1;
    }
}
