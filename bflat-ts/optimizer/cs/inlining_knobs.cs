// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Results that must not depend on how much the JIT inlines.
//
// The size and depth limits of the inliner and an addition to its
// profitability multiplier can be set for a release build of a zkVM guest
// (JitInlineSize, JitInlineDepth, JitForceInlineDepth,
// JitInlineAdditionalMultiplier), so methods are inlined that the stock
// limits leave as calls: larger ones, deeper chains, methods with loops,
// handlers and many arguments. Inlining replaces a call by the body; what
// the call did implicitly has to be done explicitly. What can go wrong:
//
//   - arguments: evaluated once, left to right, also when the callee ignores
//     them; a by-value struct or a parameter assigned in the callee must not
//     change the caller's copy; two byrefs to one variable; a value read
//     before the callee changes the variable it came from;
//   - narrow types: a parameter or a result of type byte, sbyte, short,
//     ushort, bool or char is cut to its width by the call; inlined, the cut
//     has to stay;
//   - the receiver: a struct method called on a readonly field works on a
//     copy; an instance method that never uses `this`;
//   - handlers: a callee with try/catch/finally or a filter, a throw inside
//     an inlined callee caught by the caller, the order of finally blocks;
//   - static constructors: run once, before the first access and not
//     earlier than the program can tell;
//   - stack: a callee with stackalloc called in a loop must not grow the
//     caller's frame on every iteration (the guest stack is 64 KB);
//   - depth and size: chains of 30 calls, recursion, mutual recursion, big
//     switch bodies, nests of small calls.
//
// The helpers carry no inlining attribute: the knobs decide. Every case is a
// method that is not inlined and returns one number. The expected numbers
// are constants. They were taken from a build with -O0, where the JIT does
// not inline at all; arguments, byrefs, receivers, handlers, static
// constructors and stack were also worked out by hand (the comments in those
// cases give the terms). A build at any setting of the knobs has to print
// the same.
//
// A null receiver is a fault of the guest, not an exception, on a zkVM, so
// that case runs only when built with -d NULL_RECEIVERS.
//
// Each case prints one line on failure; the run ends with ALL PASS or a FAIL
// count, and the guest returns the number of failures.
using System;
using System.Runtime.CompilerServices;

struct Vec { public long X, Y, Z; }
struct Wide { public long A, B, C, D, E; }
struct Acc
{
    public int N;
    public int Bump() => ++N;
    public int BumpBy(int k) { N += k; return N; }
}
class HoldsAcc
{
    public readonly Acc Fixed = new Acc { N = 10 };
    public Acc Free = new Acc { N = 10 };
}

class Eager
{
    public static int Value;
    public static int Reads;
    static Eager() { Program.Log(1); Value = 7; }
    public static int Get() { Reads++; return Value; }
    public static int NoState(int x) => x + 1;
}
class Eager2
{
    public static readonly long Stamp;
    static Eager2() { Program.Log(2); Stamp = 0x1234; }
    public static long Read() => Stamp;
}

class Shape
{
    public int K;
    public int NoThis() => 99;
    public int UsesThis() => K;
    public virtual int Virt() => K + 1;
}
sealed class Round : Shape { public override int Virt() => K + 2; }

static unsafe class Program
{
    const MethodImplOptions NoInl = MethodImplOptions.NoInlining;
    static int fails;
    static long log;
    static int counter;

    public static void Log(int v) => log = log * 10 + v;

    static void Check(string name, long expected, long got)
    {
        if (expected != got)
        {
            fails++;
            Console.WriteLine("FAIL " + name + " expected " + expected + " got " + got);
        }
    }

    [MethodImpl(NoInl)] static int Opaque(int x) => x;
    [MethodImpl(NoInl)] static long OpaqueL(long x) => x;

    // ---- arguments -------------------------------------------------------------
    static int Next() => ++counter;
    static int NextLogged(int tag) { Log(tag); return ++counter; }
    static long Order(int a, int b, int c) => c * 100 + b * 10 + a;
    static long IgnoreSome(int a, int b, int c) => b;
    static long IgnoreAll(int a, long b, object c) => 5;
    static int Assigned(int x) { x = x * 2 + 1; return x; }
    static long Mutate(Vec v) { v.X += 100; v.Y = -v.Y; v.Z *= 3; return v.X + v.Y + v.Z; }
    static long MutateWide(Wide w) { w.A = w.E; w.E = 0; return w.A + w.B + w.C + w.D + w.E; }
    static void Swap(ref int a, ref int b) { int t = a; a = b; b = t; }
    static int AddTo(ref int x, int v) { x += 10; return x + v; }
    static void SetOut(out int x, int v) { x = 0; x = v + x; }
    static long TwoRefs(ref long a, ref long b) { a += 1; b += 10; return a * 1000 + b; }
    static long ReadThenWrite(ref Vec v, Vec copy) { v.X = 500; return copy.X + v.X; }
    static long InParam(in Vec v, ref Vec other) { other.X = 9; return v.X; }

    [MethodImpl(NoInl)]
    static long CaseArguments()
    {
        counter = 0; log = 0;
        long r = Order(Next(), Next(), Next());                       // 321
        r = r * 1000 + IgnoreSome(Next(), Next(), Next());            // 5, counter 6
        r = r * 10 + IgnoreAll(NextLogged(1), NextLogged(2), new object()) + counter;   // 5 + 8, carries into the digit above
        r = r * 100 + log;                                            // 12
        int x = 20;
        r = r * 100 + Assigned(x) + x;                                // 41 + 20
        var v = new Vec { X = 1, Y = 2, Z = 3 };
        r = r * 1000 + Mutate(v) + v.X + v.Y + v.Z;                   // 101 - 2 + 9 + 6
        var w = new Wide { A = 1, B = 2, C = 3, D = 4, E = 5 };
        r = r * 100 + MutateWide(w) + w.A + w.E;                      // 14 + 6
        return r;
    }

    [MethodImpl(NoInl)]
    static long CaseByrefs()
    {
        int a = 3, b = 4;
        Swap(ref a, ref b);
        long r = a * 10 + b;                                          // 43
        Swap(ref a, ref a);
        r = r * 10 + a;                                               // 4
        r = r * 100 + AddTo(ref a, a);                                // a = 14, 14 + 4
        SetOut(out b, b + 1);
        r = r * 10 + b;                                               // 4
        long l = 5;
        r = r * 100000 + TwoRefs(ref l, ref l);                       // l = 16: 16016
        var v = new Vec { X = 7 };
        r = r * 1000 + ReadThenWrite(ref v, v);                       // 507
        r = r * 10 + InParam(in v, ref v) % 10;                       // v.X = 9 seen through `in`
        return r;
    }

    // ---- narrow types --------------------------------------------------------------
    static byte LowByte(int x) => (byte)x;
    static sbyte LowSByte(int x) => (sbyte)x;
    static short LowShort(int x) => (short)x;
    static ushort LowUShort(int x) => (ushort)x;
    static char LowChar(int x) => (char)x;
    static bool IsOdd(int x) => (x & 1) != 0;
    static int FromByte(byte b) => b;
    static int FromSByte(sbyte b) => b;
    static int FromShort(short s) => s;
    static int FromUShort(ushort s) => s;
    static long FromUInt(uint u) => u;
    static long FromInt(int i) => i;
    static int AddBytes(byte a, byte b) => a + b;
    static byte AddBytesNarrow(byte a, byte b) => (byte)(a + b);
    static short Negate(short s) => (short)-s;

    [MethodImpl(NoInl)]
    static long CaseNarrow(int x, long big)
    {
        long r = LowByte(x) + 1;
        r = r * 1000 + LowSByte(x);
        r = r * 100000 + LowShort(x);
        r = r * 100000 + LowUShort(x);
        r = r * 100000 + LowChar(x);
        r = r * 2 + (IsOdd(x) ? 1 : 0);
        r += FromByte((byte)x) * 3L + FromSByte((sbyte)x) * 5L + FromShort((short)x) * 7L + FromUShort((ushort)x) * 11L;
        r += FromUInt((uint)big) * 13 + FromInt((int)big) * 17;
        r += AddBytes((byte)x, (byte)(x >> 8)) * 19L + AddBytesNarrow((byte)x, (byte)(x >> 8)) * 23L;
        r += Negate((short)x) * 29L + Negate(short.MinValue) * 31L;
        return r;
    }

    // ---- receivers ---------------------------------------------------------------------
    [MethodImpl(NoInl)]
    static long CaseReceivers()
    {
        var h = new HoldsAcc();
        long r = h.Fixed.Bump();                 // on a copy: 11
        r = r * 100 + h.Fixed.Bump();            // again 11
        r = r * 100 + h.Fixed.N;                 // still 10
        r = r * 100 + h.Free.Bump();             // 11
        r = r * 100 + h.Free.BumpBy(5);          // 16
        r = r * 100 + h.Free.N;                  // 16
        Acc local = h.Free;
        local.Bump();
        r = r * 100 + h.Free.N + local.N;        // 16 + 17
        Shape s = new Shape { K = 5 };
        Shape t = new Round { K = 5 };
        r = r * 1000 + s.NoThis() + s.UsesThis() + s.Virt() + t.Virt();   // 99 + 5 + 6 + 7
        return r;
    }

    // ---- handlers --------------------------------------------------------------------------
    static int Guarded(int x)
    {
        try
        {
            if (x < 0) throw new ArgumentException("neg");
            return x * 2;
        }
        catch (ArgumentException) { Log(3); return -1; }
        finally { Log(4); }
    }
    static int Throws(int x) => x > 2 ? throw new InvalidOperationException("big") : x;
    static int WithFinally(int x)
    {
        try { return Throws(x) + 1; }
        finally { Log(5); }
    }
    static int Filtered(int x)
    {
        try { return Throws(x); }
        catch (InvalidOperationException e) when (e.Message.Length == x) { Log(6); return -x; }
    }
    static int Checked(int a, int b) => checked(a * b);
    static int Divide(int a, int b) => a / b;
    static int Index(int[] a, int i) => a[i];

    [MethodImpl(NoInl)]
    static long CaseHandlers()
    {
        log = 0;
        long r = Guarded(5);                         // 10, log 4
        r = r * 10 + Guarded(-5) + 2;                // -1 + 2, log 34
        r = r * 10 + WithFinally(1);                 // 2, log 5
        try { r = r * 10 + WithFinally(7); } catch (InvalidOperationException) { Log(7); r = r * 10 + 9; }   // log 57
        r = r * 10 + Filtered(3) + 5;                // -3 + 5, log 6
        try { r += Filtered(4); } catch (InvalidOperationException) { Log(8); }
        try { r += Checked(0x10000, 0x10000); } catch (OverflowException) { Log(9); }
        try { r += Divide(1, Opaque(0)); } catch (DivideByZeroException) { Log(1); }
        try { r += Index(new int[2], Opaque(2)); } catch (IndexOutOfRangeException) { Log(2); }
        r = r * 100 + Checked(7, 6) + Divide(100, 7) + Index(new[] { 1, 2, 3 }, 2);   // 42 + 14 + 3
        return r * 10000000000L + log;
    }

    // ---- static constructors ---------------------------------------------------------------------
    [MethodImpl(NoInl)]
    static long CaseStatics()
    {
        log = 0;
        Log(5);
        long r = Eager.NoState(1);       // a static method runs the constructor first: log 51
        Log(6);
        r = r * 10 + Eager.Get();        // 7
        r = r * 10 + Eager.Get();        // 7, constructor not again
        Log(8);
        r = r * 100000 + Eager2.Read();  // 0x1234
        r = r * 10 + Eager.Reads;
        return r * 100000 + log;         // 51682
    }

    // ---- stack -----------------------------------------------------------------------------------
    static long Scratch(int i)
    {
        long* p = stackalloc long[32];
        for (int k = 0; k < 32; k++) p[k] = i + k;
        return p[i & 31] + p[31];
    }
    static long ScratchSpan(int i)
    {
        Span<byte> s = stackalloc byte[200];
        s[i % 200] = (byte)i;
        return s[i % 200] + s[(i + 1) % 200];
    }

    [MethodImpl(NoInl)]
    static long CaseStack()
    {
        long r = 0;
        for (int i = 0; i < 20000; i++) r += Scratch(i) + ScratchSpan(i);   // 5 MB if the frame grew each time
        return r;
    }

    // ---- depth -------------------------------------------------------------------------------------
    static long D30(long x) => x + 30;
    static long D29(long x) => D30(x * 2) + 29; static long D28(long x) => D29(x + 1) + 28;
    static long D27(long x) => D28(x ^ 5) + 27; static long D26(long x) => D27(x + 3) + 26;
    static long D25(long x) => D26(x * 3) + 25; static long D24(long x) => D25(x - 7) + 24;
    static long D23(long x) => D24(x + 11) + 23; static long D22(long x) => D23(x ^ 9) + 22;
    static long D21(long x) => D22(x + 2) + 21; static long D20(long x) => D21(x * 2) + 20;
    static long D19(long x) => D20(x - 1) + 19; static long D18(long x) => D19(x + 4) + 18;
    static long D17(long x) => D18(x ^ 3) + 17; static long D16(long x) => D17(x + 6) + 16;
    static long D15(long x) => D16(x * 5) + 15; static long D14(long x) => D15(x - 2) + 14;
    static long D13(long x) => D14(x + 8) + 13; static long D12(long x) => D13(x ^ 1) + 12;
    static long D11(long x) => D12(x + 5) + 11; static long D10(long x) => D11(x * 2) + 10;
    static long D9(long x) => D10(x - 3) + 9; static long D8(long x) => D9(x + 7) + 8;
    static long D7(long x) => D8(x ^ 6) + 7; static long D6(long x) => D7(x + 1) + 6;
    static long D5(long x) => D6(x * 3) + 5; static long D4(long x) => D5(x - 4) + 4;
    static long D3(long x) => D4(x + 9) + 3; static long D2(long x) => D3(x ^ 2) + 2;
    static long D1(long x) => D2(x + 1) + 1;

    static long Fib(int n) => n < 2 ? n : Fib(n - 1) + Fib(n - 2);
    static bool IsEven(int n) => n == 0 || IsOddR(n - 1);
    static bool IsOddR(int n) => n != 0 && IsEven(n - 1);
    static long Ack(long m, long n) => m == 0 ? n + 1 : n == 0 ? Ack(m - 1, 1) : Ack(m - 1, Ack(m, n - 1));
    static long Sq(long x) => x * x;
    static long Poly(long x) => Sq(Sq(x) + Sq(x + 1)) % 1000003 + Sq(Sq(Sq(x % 7) + 1) % 1009);

    [MethodImpl(NoInl)]
    static long CaseDepth(int x)
    {
        long r = D1(x) % 1000000007;
        r = r * 1000 + Fib(15);                              // 610
        r = r * 10 + (IsEven(41) ? 1 : 0) + (IsOddR(41) ? 2 : 0);
        r = r * 1000 + Ack(2, 3) + Ack(3, 3);                // 9 + 61
        long p = 0;
        for (int i = 0; i < 50; i++) p = (p * 31 + Poly(x + i)) % 1000000007;
        return r * 31 + p;
    }

    // ---- size ------------------------------------------------------------------------------------------
    static long BigSwitch(int op, long a, long b)
    {
        switch (op)
        {
            case 0: return a + b;
            case 1: return a - b;
            case 2: return a * b;
            case 3: return b == 0 ? -1 : a / b;
            case 4: return b == 0 ? -2 : a % b;
            case 5: return a & b;
            case 6: return a | b;
            case 7: return a ^ b;
            case 8: return a << (int)(b & 31);
            case 9: return a >> (int)(b & 31);
            case 10: return a < b ? a : b;
            case 11: return a > b ? a : b;
            case 12: { long s = 0; for (long i = 0; i < (b & 15); i++) s += a + i; return s; }
            case 13: { long s = a; while (s > 100) s /= 3; return s + b; }
            case 14: return ~a + b;
            default: return op * 1000 + a - b;
        }
    }
    static long Loopy(int[] a, int from, int to)
    {
        long s = 0;
        for (int i = from; i < to; i++)
        {
            if ((a[i] & 1) == 0) continue;
            if (a[i] > 1000) break;
            s += a[i];
        }
        return s;
    }
    static Vec MakeVec(long x) => new Vec { X = x, Y = x * 2, Z = x * 3 };
    static Wide MakeWide(long x) => new Wide { A = x, B = x + 1, C = x + 2, D = x + 3, E = x + 4 };
    static Vec AddVec(Vec a, Vec b) => new Vec { X = a.X + b.X, Y = a.Y + b.Y, Z = a.Z + b.Z };
    static long SumWide(Wide w, long k, Wide v) => (w.A + w.B + w.C + w.D + w.E) * k - (v.A ^ v.E);
    static long Many(long a, long b, long c, long d, long e, long f, long g, long h, long i, long j, long k, long l) =>
        a - 2 * b + 3 * c - 4 * d + 5 * e - 6 * f + 7 * g - 8 * h + 9 * i - 10 * j + 11 * k - 12 * l;
    static T Pick<T>(bool first, T a, T b) => first ? a : b;
    static int SizeOf<T>() => Unsafe.SizeOf<T>();
    static long ViaPointer(long* p, long v) { *p += v; return *p * 2; }

    [MethodImpl(NoInl)]
    static long CaseSize(int x)
    {
        long r = 0;
        for (int op = 0; op < 18; op++) r = r * 7 % 1000000007 + BigSwitch(op, x + op * 13, op * 5 - 20);
        int[] data = { 3, 8, 5, 12, 7, 2000, 9, 11 };
        r = r * 100 + Loopy(data, 0, 8) + Loopy(data, 6, 8);                  // 15 + 20
        Vec v = AddVec(MakeVec(x), AddVec(MakeVec(1), MakeVec(2)));
        r = r * 1000 + v.X + v.Y + v.Z - 6L * x;                              // 18
        r = r * 1000 + SumWide(MakeWide(1), 3, MakeWide(8));                  // 45 - (8 ^ 12)
        r = r * 1000 + Many(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12) + 100;     // -78 + 100
        r = r * 100 + Pick(x > 0, 11, 22) + Pick(x < 0, "a", "bcd").Length + SizeOf<Vec>() + SizeOf<byte>();
        long slot = 5;
        r = r * 100 + ViaPointer(&slot, 3) + slot;                            // 16 + 8
        return r;
    }

    [MethodImpl(NoInl)]
    static long CaseNull()
    {
#if NULL_RECEIVERS
        Shape none = Opaque(0) == 0 ? null : new Shape();
        try { return none.NoThis(); } catch (NullReferenceException) { return 1; }
#else
        return 1;
#endif
    }

    static int Main()
    {
        Check("arguments", 3210063126111420L, CaseArguments());
        Check("byrefs", 434184160165079L, CaseByrefs());
        Check("narrow types", 492250374932065520L, CaseNarrow(Opaque(0x12F4E7C1), OpaqueL(unchecked((long)0xFEDCBA98F6543210))));
        Check("narrow types, small", -6512834034926218010L, CaseNarrow(Opaque(-2), OpaqueL(-1)));
        Check("receivers", 11111011161633117L, CaseReceivers());
        Check("handlers", 101292633455768912L, CaseHandlers());
        Check("static constructors", 27704660251682L, CaseStatics());
        Check("stack", 403456416L, CaseStack());
        Check("depth", 1378961281774710L, CaseDepth(Opaque(12345)));
        Check("size", -4325800562837459660L, CaseSize(Opaque(77)));
        Check("null receiver", 1, CaseNull());

        Console.WriteLine(fails == 0 ? "ALL PASS" : "FAILED " + fails);
        return fails;
    }
}
