// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// The zkVM code generation options that the Airbender platform turns on
// (riscv32, and each one is available on riscv64 too): the EVM handler
// calling convention, cheap inline arguments cloned at their uses, cheap
// values forward-substituted into their uses, struct constructors run in
// place, locals not zeroed, null checks not emitted.
//
// JitZkHandlerConv. A static method (ref T, nuint, ref U, ref byte, nint,
// nint*, ref byte, ref byte) -> int takes its arguments in s1-s8 and saves
// none of them; every call of that signature, direct or through a function
// pointer, passes them there and counts s1-s8 as changed. What can go
// wrong: a caller that is not a handler puts arguments in s1-s8 without
// saving them for its own caller, whose values in callee-saved registers
// are then lost; a handler that calls another one keeps its own arguments
// in s1-s8 across the call; a tail call from a method that is not a handler
// restores s1-s8 over the arguments; an exception unwinds through a handler
// that changed s1-s8 and the frames above get them back wrong; a method of
// the same shape that is not static, or a delegate on one, is called with
// the other convention.
//
// JitZkInlineCloneCheapArgs and JitZkForwardSubCheap. An argument of an
// inlined call, or a value the importer spilled to a temp, that is a local
// plus or minus a constant is recomputed at each use instead of being held
// in a temp. What can go wrong: the local is written between the point
// where the value was taken and a use (a later argument assigns it, the
// callee assigns its parameter, the statement itself stores to it), so the
// clone reads the new value.
//
// JitZkNewObjInPlace. `local = new S(...)` runs the constructor on the local
// itself rather than on a temp that is copied. What can go wrong: the
// constructor reads the old value of the local through an argument that
// aliases it (a struct passed by reference to a copy that was elided, an
// `in` argument) while it overwrites it; a
// constructor that throws leaves the local half written where C# says the
// assignment did not happen.
//
// JitZkInitLocallocOnly. Only stackalloc memory is zeroed in the prolog.
// What can go wrong: stackalloc memory, or a struct the IL initializes
// explicitly (default, new S()), that is left dirty.
//
// JitZkNoNullChecks. An explicit null check emits nothing. What can go
// wrong: the operand of the check has a side effect and is dropped with it.
// (That a null dereference no longer throws is the documented effect of the
// option, so nothing here dereferences null.)
//
// Each check prints one line on failure with the expected and the actual
// value; each group prints the number of checks. The run ends with ALL PASS
// or a FAIL count, and the guest returns 1 on failure.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

struct St { public int Top; public int Depth; }
struct Disp { public long Gas; public int Pc; public int Steps; }

unsafe delegate int HandlerDelegate(ref St s, nuint a, ref Disp d, ref byte b, nint c, nint* p, ref byte e, ref byte f);

class E : Exception { public int Code; public E(int c) : base("e") { Code = c; } }

struct Pair
{
    public long A;
    public long B;

    public Pair(long a, long b) { A = a; B = b; }

    // Reads its argument after the first field is written.
    public Pair(Pair o) { A = o.B; B = o.A; }
    public Pair(in Pair o, int k) { A = o.B + k; B = o.A; }
    public Pair(long a, bool fail) { A = a; if (fail) throw new E(7); B = a; }
    public Pair(long a) { A = a; B = A + 1; }
}

struct Wide
{
    public int A, B, C, D, E, F;

    public Wide(int a) { A = a; B = a + 1; C = a + 2; D = a + 3; E = a + 4; F = a + 5; }
    public Wide(Wide o, int k) { A = o.F + k; B = o.E + k; C = o.D + k; D = o.C + k; E = o.B + k; F = o.A + k; }
}

struct Padded
{
    public byte Tag;
    public long Value;
    public short Tail;
}

unsafe class Shape
{
    public int K;

    // The handler signature on an instance method: the ordinary convention.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int Inst(ref St s, nuint a, ref Disp d, ref byte b, nint c, nint* p, ref byte e, ref byte f)
    {
        return Program.Mix(K, s.Top, (int)a, d.Pc, b, (int)c, (int)p[0], e, f);
    }
}

unsafe class Program
{
    const MethodImplOptions NI = MethodImplOptions.NoInlining;
    const MethodImplOptions AI = MethodImplOptions.AggressiveInlining;

    static int fails;
    static int checks;

    static void Check(string name, long got, long exp)
    {
        checks++;
        if (got != exp)
        {
            fails++;
            if (fails <= 60)
                Console.WriteLine("FAIL " + name + " expected " + exp.ToString() + " got " + got.ToString());
        }
    }

    static void Group(string name)
    {
        Console.WriteLine("ok   " + name + " " + checks.ToString());
    }

    [MethodImpl(NI)] static int Id(int x) => x;
    [MethodImpl(NI)] static long IdL(long x) => x;
    [MethodImpl(NI)] static void Nop() { }

    [MethodImpl(NI)]
    public static int Mix(int a, int b, int c, int d, int e, int f, int g, int h, int i)
    {
        return ((((((((a * 31 + b) * 31 + c) * 31 + d) * 31 + e) * 31 + f) * 31 + g) * 31 + h) * 31) + i;
    }

    // ================= the handler convention =================

    static int s_seen;

    // Reports its arguments and returns.
    [MethodImpl(NI)]
    static int HLeaf(ref St s, nuint a, ref Disp d, ref byte b, nint c, nint* p, ref byte e, ref byte f)
    {
        return Mix(1, s.Top, (int)a, d.Pc, b, (int)c, (int)p[0], e, f);
    }

    // Changes every argument register before it returns.
    [MethodImpl(NI)]
    static int HClobber(ref St s, nuint a, ref Disp d, ref byte b, nint c, nint* p, ref byte e, ref byte f)
    {
        int r = Mix(2, s.Top, (int)a, d.Pc, b, (int)c, (int)p[0], e, f);
        for (int i = 0; i < 3; i++)
        {
            a = a * 3 + 1;
            c = c ^ (nint)a;
            p++;
            s = ref Unsafe.Add(ref s, 1);
            d = ref Unsafe.Add(ref d, 1);
            b = ref Unsafe.Add(ref b, 1);
            e = ref Unsafe.Add(ref e, 2);
            f = ref Unsafe.Add(ref f, 3);
        }
        s_seen = (int)a + (int)c + (int)(nint)p + (int)(nint)Unsafe.AsPointer(ref s) + (int)(nint)Unsafe.AsPointer(ref d) +
                 (int)(nint)Unsafe.AsPointer(ref b) + (int)(nint)Unsafe.AsPointer(ref e) + (int)(nint)Unsafe.AsPointer(ref f);
        return r;
    }

    // Calls ordinary methods and still has its arguments afterwards.
    [MethodImpl(NI)]
    static int HCallsOut(ref St s, nuint a, ref Disp d, ref byte b, nint c, nint* p, ref byte e, ref byte f)
    {
        int x = Id(s.Top);
        long g = IdL(d.Gas);
        Nop();
        d.Gas = g - 3;
        d.Steps++;
        return Mix(3, x, (int)a, d.Pc, b, (int)c, (int)p[0], e, f);
    }

    // Calls another handler and uses its own arguments afterwards.
    [MethodImpl(NI)]
    static int HCallsHandler(ref St s, nuint a, ref Disp d, ref byte b, nint c, nint* p, ref byte e, ref byte f)
    {
        int inner = HClobber(ref s, a + 1, ref d, ref b, c + 1, p, ref e, ref f);
        d.Steps++;
        return inner ^ Mix(4, s.Top, (int)a, d.Pc, b, (int)c, (int)p[0], e, f);
    }

    // Tail calls: the next handler through a table, with changed arguments.
    static delegate*<ref St, nuint, ref Disp, ref byte, nint, nint*, ref byte, ref byte, int>[] s_table;

    [MethodImpl(NI)]
    static int HNext(ref St s, nuint a, ref Disp d, ref byte b, nint c, nint* p, ref byte e, ref byte f)
    {
        d.Steps++;
        d.Pc += b;
        s.Top += (int)a;
        if (d.Steps >= 6) return HLeaf(ref s, a, ref d, ref b, c, p, ref e, ref f);
        return s_table[d.Steps & 1](ref s, a + 1, ref d, ref Unsafe.Add(ref b, 1), c - 1, p, ref e, ref f);
    }

    [MethodImpl(NI)]
    static int HNext2(ref St s, nuint a, ref Disp d, ref byte b, nint c, nint* p, ref byte e, ref byte f)
    {
        d.Steps++;
        d.Gas -= e;
        return s_table[d.Steps & 1](ref s, a, ref d, ref b, c, p + 1, ref Unsafe.Add(ref e, 1), ref f);
    }

    [MethodImpl(NI)] static void ThrowE(int code) { throw new E(code); }

    // Throws from under a handler that changed its registers.
    [MethodImpl(NI)]
    static int HThrows(ref St s, nuint a, ref Disp d, ref byte b, nint c, nint* p, ref byte e, ref byte f)
    {
        a = a * 5;
        c = c * 7;
        s = ref Unsafe.Add(ref s, 1);
        b = ref Unsafe.Add(ref b, 1);
        e = ref Unsafe.Add(ref e, 1);
        f = ref Unsafe.Add(ref f, 1);
        s_seen = (int)a + (int)c + b + e + f + s.Top;
        ThrowE((int)p[0]);
        return 0;
    }

    // Catches, then reads its arguments.
    [MethodImpl(NI)]
    static int HCatches(ref St s, nuint a, ref Disp d, ref byte b, nint c, nint* p, ref byte e, ref byte f)
    {
        int code = 0;
        try
        {
            HThrows(ref s, a, ref d, ref b, c, p, ref e, ref f);
        }
        catch (E ex)
        {
            code = ex.Code + s.Top + (int)a + d.Pc + b + (int)c + (int)p[0] + e + f;
        }
        return code ^ Mix(5, s.Top, (int)a, d.Pc, b, (int)c, (int)p[0], e, f);
    }

    // A method that is not a handler and passes the arguments on. It has nothing live across
    // the call, so whatever it saves it saves for its caller.
    [MethodImpl(NI)]
    static int Mid(int which, ref St s, ref Disp d, byte* bytes, nint* p)
    {
        switch (which)
        {
            case 0: return HLeaf(ref s, 0x1234, ref d, ref bytes[0], -77, p, ref bytes[1], ref bytes[2]);
            case 1: return HClobber(ref s, 0x1234, ref d, ref bytes[0], -77, p, ref bytes[1], ref bytes[2]);
            case 2: return HCallsOut(ref s, 0x1234, ref d, ref bytes[0], -77, p, ref bytes[1], ref bytes[2]);
            case 3: return HCallsHandler(ref s, 0x1234, ref d, ref bytes[0], -77, p, ref bytes[1], ref bytes[2]);
            case 4: return HNext(ref s, 2, ref d, ref bytes[0], 100, p, ref bytes[1], ref bytes[2]);
            case 5: return HCatches(ref s, 0x1234, ref d, ref bytes[0], -77, p, ref bytes[1], ref bytes[2]);
            default: return HThrows(ref s, 0x1234, ref d, ref bytes[0], -77, p, ref bytes[1], ref bytes[2]);
        }
    }

    // Keeps more values live across the call than there are callee-saved registers.
    [MethodImpl(NI)]
    static int Outer(int which, int seed, ref St s, ref Disp d, byte* bytes, nint* p, out int result)
    {
        int v0 = Id(seed + 1), v1 = Id(seed + 2), v2 = Id(seed + 3), v3 = Id(seed + 4), v4 = Id(seed + 5),
            v5 = Id(seed + 6), v6 = Id(seed + 7), v7 = Id(seed + 8), v8 = Id(seed + 9), v9 = Id(seed + 10),
            v10 = Id(seed + 11), v11 = Id(seed + 12);
        result = Mid(which, ref s, ref d, bytes, p);
        return Mix(v0, v1, v2, v3, v4, v5, v6, v7, v8) + Mix(v9, v10, v11, v0, v1, v2, v3, v4, v5);
    }

    // The same with the exception caught here.
    [MethodImpl(NI)]
    static int OuterCatch(int seed, ref St s, ref Disp d, byte* bytes, nint* p, out int result)
    {
        int v0 = Id(seed + 1), v1 = Id(seed + 2), v2 = Id(seed + 3), v3 = Id(seed + 4), v4 = Id(seed + 5),
            v5 = Id(seed + 6), v6 = Id(seed + 7), v7 = Id(seed + 8), v8 = Id(seed + 9), v9 = Id(seed + 10),
            v10 = Id(seed + 11), v11 = Id(seed + 12);
        result = 0;
        try
        {
            result = Mid(6, ref s, ref d, bytes, p);
        }
        catch (E ex)
        {
            result = ex.Code;
        }
        return Mix(v0, v1, v2, v3, v4, v5, v6, v7, v8) + Mix(v9, v10, v11, v0, v1, v2, v3, v4, v5);
    }

    static int Live(int seed) =>
        Mix(seed + 1, seed + 2, seed + 3, seed + 4, seed + 5, seed + 6, seed + 7, seed + 8, seed + 9) +
        Mix(seed + 10, seed + 11, seed + 12, seed + 1, seed + 2, seed + 3, seed + 4, seed + 5, seed + 6);

    static void Reset(St* s, Disp* d, byte* bytes, nint* p)
    {
        for (int i = 0; i < 4; i++) { s[i].Top = 40 + i; s[i].Depth = 9; d[i].Gas = 1000000; d[i].Pc = 7 + i; d[i].Steps = 0; }
        for (int i = 0; i < 32; i++) bytes[i] = (byte)(200 + i);
        for (int i = 0; i < 8; i++) p[i] = 5000 + i;
    }

    static void TestHandlers()
    {
        St* s = stackalloc St[4];
        Disp* d = stackalloc Disp[4];
        byte* bytes = stackalloc byte[32];
        nint* p = stackalloc nint[8];
        s_table = new delegate*<ref St, nuint, ref Disp, ref byte, nint, nint*, ref byte, ref byte, int>[] { &HNext, &HNext2 };
        int r;

        int leaf = Mix(1, 40, 0x1234, 7, 200, -77, 5000, 201, 202);
        int clob = Mix(2, 40, 0x1234, 7, 200, -77, 5000, 201, 202);

        Reset(s, d, bytes, p);
        Check("leaf: caller's values", Outer(0, 100, ref s[0], ref d[0], bytes, p, out r), Live(100));
        Check("leaf: result", r, leaf);

        Reset(s, d, bytes, p);
        Check("clobber: caller's values", Outer(1, 200, ref s[0], ref d[0], bytes, p, out r), Live(200));
        Check("clobber: result", r, clob);

        Reset(s, d, bytes, p);
        Check("calls out: caller's values", Outer(2, 300, ref s[0], ref d[0], bytes, p, out r), Live(300));
        Check("calls out: result", r, Mix(3, 40, 0x1234, 7, 200, -77, 5000, 201, 202));
        Check("calls out: gas", d[0].Gas, 999997);
        Check("calls out: steps", d[0].Steps, 1);

        Reset(s, d, bytes, p);
        Check("calls handler: caller's values", Outer(3, 400, ref s[0], ref d[0], bytes, p, out r), Live(400));
        Check("calls handler: result", r,
              Mix(2, 40, 0x1235, 7, 200, -76, 5000, 201, 202) ^ Mix(4, 40, 0x1234, 7, 200, -77, 5000, 201, 202));
        Check("calls handler: steps", d[0].Steps, 1);

        // The chain: HNext, HNext2, HNext, HNext2, HNext, then HNext at step 6 ends in HLeaf.
        //   step 1 (HNext):  pc 7+200, top 40+2 -> table[1] with a 3, b at 1, c 99
        //   step 2 (HNext2): gas -201 -> table[0] with p at 1, e at 2
        //   step 3 (HNext):  pc +201, top +3 -> table[1] with a 4, b at 2, c 98
        //   step 4 (HNext2): gas -202 -> table[0] with p at 2, e at 3
        //   step 5 (HNext):  pc +202, top +4 -> table[1] with a 5, b at 3, c 97
        //   step 6 (HNext2): gas -203 -> table[0] with p at 3, e at 4
        //   step 7 (HNext):  pc +203, top +5 -> HLeaf(a 5, b at 3, c 97, p at 3, e at 4, f at 2)
        Reset(s, d, bytes, p);
        Check("tail chain: caller's values", Outer(4, 500, ref s[0], ref d[0], bytes, p, out r), Live(500));
        Check("tail chain: result", r, Mix(1, 40 + 2 + 3 + 4 + 5, 5, 7 + 200 + 201 + 202 + 203, 203, 97, 5003, 204, 202));
        Check("tail chain: steps", d[0].Steps, 7);
        Check("tail chain: gas", d[0].Gas, 1000000 - 201 - 202 - 203);

        Reset(s, d, bytes, p);
        Check("handler catches: caller's values", Outer(5, 600, ref s[0], ref d[0], bytes, p, out r), Live(600));
        Check("handler catches: result", r,
              (5000 + 40 + 0x1234 + 7 + 200 - 77 + 5000 + 201 + 202) ^ Mix(5, 40, 0x1234, 7, 200, -77, 5000, 201, 202));

        Reset(s, d, bytes, p);
        Check("thrown through: caller's values", OuterCatch(700, ref s[0], ref d[0], bytes, p, out r), Live(700));
        Check("thrown through: code", r, 5000);

        // Other ways to reach a handler, and the same shape without the convention.
        Reset(s, d, bytes, p);
        delegate*<ref St, nuint, ref Disp, ref byte, nint, nint*, ref byte, ref byte, int> fp = &HClobber;
        int a0 = Id(11), a1 = Id(12), a2 = Id(13), a3 = Id(14), a4 = Id(15), a5 = Id(16), a6 = Id(17), a7 = Id(18), a8 = Id(19), a9 = Id(20);
        r = fp(ref s[0], 0x1234, ref d[0], ref bytes[0], -77, p, ref bytes[1], ref bytes[2]);
        Check("function pointer: result", r, clob);
        Check("function pointer: caller's values", Mix(a0, a1, a2, a3, a4, a5, a6, a7, a8) + a9, Mix(11, 12, 13, 14, 15, 16, 17, 18, 19) + 20);

        HandlerDelegate del = HClobber;
        r = del(ref s[0], 0x1234, ref d[0], ref bytes[0], -77, p, ref bytes[1], ref bytes[2]);
        Check("delegate: result", r, clob);
        Check("delegate: caller's values", Mix(a0, a1, a2, a3, a4, a5, a6, a7, a8) + a9, Mix(11, 12, 13, 14, 15, 16, 17, 18, 19) + 20);

        Shape shape = new Shape { K = 6 };
        r = shape.Inst(ref s[0], 0x1234, ref d[0], ref bytes[0], -77, p, ref bytes[1], ref bytes[2]);
        Check("instance method: result", r, Mix(6, 40, 0x1234, 7, 200, -77, 5000, 201, 202));
        HandlerDelegate del2 = shape.Inst;
        r = del2(ref s[0], 0x1234, ref d[0], ref bytes[0], -77, p, ref bytes[1], ref bytes[2]);
        Check("delegate on instance method: result", r, Mix(6, 40, 0x1234, 7, 200, -77, 5000, 201, 202));
        Check("instance: caller's values", Mix(a0, a1, a2, a3, a4, a5, a6, a7, a8) + a9, Mix(11, 12, 13, 14, 15, 16, 17, 18, 19) + 20);

        // In a loop, with the loop's own values live across each call.
        Reset(s, d, bytes, p);
        int acc = 0;
        for (int i = 0; i < 5; i++)
        {
            acc = acc * 3 + HCallsOut(ref s[0], (nuint)i, ref d[0], ref bytes[i], i, p + i, ref bytes[i + 1], ref bytes[i + 2]) + i;
        }
        int expAcc = 0;
        for (int i = 0; i < 5; i++)
            expAcc = expAcc * 3 + Mix(3, 40, i, 7, 200 + i, i, 5000 + i, 201 + i, 202 + i) + i;
        Check("loop: result", acc, expAcc);
        Check("loop: gas", d[0].Gas, 1000000 - 15);
        Group("handler convention");
    }

    // ================= cloned arguments and forward substitution =================

    [MethodImpl(AI)] static int Twice(int a, int b) => a * 1000 + b * 10 + a;
    [MethodImpl(AI)] static int Thrice(int a, int b, int c) => a * 10000 + b * 100 + c + a;
    [MethodImpl(AI)] static int AssignsParam(int a, int b) { a += b; return a * 2 + b; }
    [MethodImpl(AI)] static int WritesThroughRef(int a, ref int x) { x = 50; return a + x + a; }
    [MethodImpl(AI)] static int Late(int a, int b) { int t = b * 3; Nop(); return t + a + a; }
    [MethodImpl(AI)] static long TwiceL(long a, long b) => a * 1000 + b * 10 + a;
    [MethodImpl(AI)] static int Deref(byte* p, int n) => p[0] * 100 + n + p[1];
    [MethodImpl(AI)] static int Inner(int a) => a + 2;
    [MethodImpl(AI)] static int Wraps(int a, int b) => Twice(Inner(a), b);
    [MethodImpl(AI)] static int LoopUse(int a, int n) { int s = 0; for (int i = 0; i < n; i++) s += a; return s + a; }

    [MethodImpl(NI)] static int ArgThenIncrement(int x) => Twice(x + 1, x++);
    [MethodImpl(NI)] static int ArgThenAssign(int x) { int r = Twice(x + 1, x = 7); return r + x; }
    [MethodImpl(NI)] static int ArgThenPreIncrement(int x) { int r = Twice(x - 1, ++x); return r * 2 + x; }
    [MethodImpl(NI)] static int TwoArgsThenAssign(int x, int y) { int r = Thrice(x + 1, y - 1, x = y = 9); return r + x + y; }
    [MethodImpl(NI)] static int ArgCalleeAssigns(int x) { int r = AssignsParam(x + 1, 5); return r + x; }
    [MethodImpl(NI)] static int ArgWithRef(int x) { int r = WritesThroughRef(x + 1, ref x); return r * 2 + x; }
    [MethodImpl(NI)] static int ArgUsedLate(int x) { int r = Late(x + 3, x); x = 100; return r + x; }
    [MethodImpl(NI)] static int ResultIntoArg(int x) { x = Twice(x + 1, x) + x; return x; }
    [MethodImpl(NI)] static int ArgNested(int x) { int r = Wraps(x + 1, x++); return r + x; }
    [MethodImpl(NI)] static int ArgInLoop(int x) { int s = 0; for (int i = 0; i < 4; i++) s += Twice(x + 1, x++); return s + x; }
    [MethodImpl(NI)] static int ArgLoopUse(int x) { int r = LoopUse(x + 1, 3); x += r; return x; }
    [MethodImpl(NI)] static long ArgLong(long x) { long r = TwiceL(x + 1, x++); return r + x; }
    [MethodImpl(NI)] static int ArgPointer(byte* p) { int r = Deref(p + 4, (int)(p++)[0]); return r + p[0]; }
    [MethodImpl(NI)] static int ArgPointerAssign(byte* p, byte* q) { int r = Deref(p + 4, (p = q)[0]); return r + p[0]; }

    [MethodImpl(NI)] static int ValueThenWrite(int x) { int r = x + 1; x = 100; return r + x; }
    [MethodImpl(NI)] static int ValueThenWrites(int x) { int r = x + 4; x <<= 1; int s = r + x; x += r; return s * 3 + x + r; }
    [MethodImpl(NI)] static int ValueOfCall(int x) { int r = Inner(x) + 1; x = Id(50); return r * r + x; }
    [MethodImpl(NI)] static int ValueSwap(int x, int y) { int t = x + 1; x = y + 1; y = t; return x * 100 + y; }
    [MethodImpl(NI)] static int ValueInLoop(int n) { int sum = 0; for (int i = 0; i < n;) { int t = i + 1; i += 2; sum += t * t; } return sum; }
    [MethodImpl(NI)] static long ValueLong(long x) { long r = x - 1; x = 100; return r * 2 + x; }
    [MethodImpl(NI)] static int ValuePointer(byte* p) { byte* r = p + 4; p += 8; return r[0] * 100 + p[0] + r[1]; }
    [MethodImpl(NI)] static int ValueRef(ref byte b) { ref byte r = ref Unsafe.Add(ref b, 4); b = ref Unsafe.Add(ref b, 8); return r * 100 + b + Unsafe.Add(ref r, 1); }
    [MethodImpl(NI)] static int ValueTwoUses(int x, int[] a) { int t = x + 1; x = a[t]; return a[t] + x + a[x]; }
    [MethodImpl(NI)] static int ValueAcrossCall(int x) { int t = x + 2; x = Id(x) * 3; Nop(); return t + x + t; }
    [MethodImpl(NI)] static int ValueChain(int x) { int a = x + 1; int b = a + 1; x = 0; int c = b + 1; a = 50; return a + b + c + x; }
    [MethodImpl(NI)] static int ValueStackSwap(int x, int y) { (x, y) = (y + 1, x + 1); return x * 1000 + y; }
    [MethodImpl(NI)] static int ValueCompound(int x) { x = (x + 1) * (x = 10) + (x + 2); return x; }

    static void TestCloning()
    {
        byte* bytes = stackalloc byte[32];
        for (int i = 0; i < 32; i++) bytes[i] = (byte)(i + 1);
        int[] arr = new int[16];
        for (int i = 0; i < 16; i++) arr[i] = (i * 5 + 3) & 15;

        // Twice(a, b) = a * 1001 + b * 10.
        Check("argument then x++", ArgThenIncrement(5), 6 * 1001 + 5 * 10);
        Check("argument then x = 7", ArgThenAssign(5), 6 * 1001 + 7 * 10 + 7);
        Check("argument then ++x", ArgThenPreIncrement(5), (4 * 1001 + 6 * 10) * 2 + 6);
        Check("two arguments then x = y = 9", TwoArgsThenAssign(5, 20), 6 * 10000 + 19 * 100 + 9 + 6 + 9 + 9);
        Check("callee assigns the parameter", ArgCalleeAssigns(5), (6 + 5) * 2 + 5 + 5);
        Check("argument with ref x", ArgWithRef(5), (6 + 50 + 6) * 2 + 50);
        Check("argument used late", ArgUsedLate(5), 5 * 3 + 8 + 8 + 100);
        Check("result into the argument's local", ResultIntoArg(5), 6 * 1001 + 5 * 10 + 5);
        Check("nested inline", ArgNested(5), (8 * 1001 + 5 * 10) + 6);
        Check("argument in a loop", ArgInLoop(5),
              (6 * 1001 + 50) + (7 * 1001 + 60) + (8 * 1001 + 70) + (9 * 1001 + 80) + 9);
        Check("argument used in the callee's loop", ArgLoopUse(5), 5 + 6 * 4);
        Check("long argument then x++", ArgLong(0x100000005L), 0x100000006L * 1001 + 0x100000005L * 10 + 0x100000006L);
        Check("pointer argument then p++", ArgPointer(bytes), 5 * 100 + 1 + 6 + 2);
        Check("pointer argument then p = q", ArgPointerAssign(bytes, bytes + 16), 5 * 100 + 17 + 6 + 17);

        Check("value then write", ValueThenWrite(5), 6 + 100);
        Check("value then writes", ValueThenWrites(5), (9 + 10) * 3 + 19 + 9);
        Check("value of a call", ValueOfCall(5), 8 * 8 + 50);
        Check("value swap", ValueSwap(5, 20), 21 * 100 + 6);
        Check("value in a loop", ValueInLoop(8), 1 + 9 + 25 + 49);
        Check("long value then write", ValueLong(0x100000000L), 0xFFFFFFFFL * 2 + 100);
        Check("pointer value then write", ValuePointer(bytes), 5 * 100 + 9 + 6);
        Check("ref value then write", ValueRef(ref bytes[0]), 5 * 100 + 9 + 6);
        // t = 3, x = a[3] = 2, then a[3] + 2 + a[2] = 2 + 2 + 13.
        Check("value with two uses", ValueTwoUses(2, arr), 2 + 2 + 13);
        Check("value across a call", ValueAcrossCall(5), 7 + 15 + 7);
        Check("value chain", ValueChain(5), 50 + 7 + 8 + 0);
        Check("tuple swap", ValueStackSwap(5, 20), 21 * 1000 + 6);
        Check("compound", ValueCompound(5), 6 * 10 + 12);
        Group("cloned values");
    }

    // ================= constructors in place =================

    [MethodImpl(NI)] static void Touch(void* p) { }

    [MethodImpl(NI)]
    static long NewFromOwnFields(long a, long b)
    {
        Pair p = new Pair(a, b);
        p = new Pair(p.B, p.A);
        Touch(&p);
        return p.A * 3 + p.B;
    }

    [MethodImpl(NI)]
    static long NewFromSelfByValue(long a, long b)
    {
        Pair p = new Pair(a, b);
        p = new Pair(p);
        Touch(&p);
        return p.A * 3 + p.B;
    }

    [MethodImpl(NI)]
    static long NewFromSelfIn(long a, long b)
    {
        Pair p = new Pair(a, b);
        p = new Pair(in p, 1);
        Touch(&p);
        return p.A * 3 + p.B;
    }

    [MethodImpl(NI)]
    static long NewReadsThis(long a)
    {
        Pair p = new Pair(a);
        Touch(&p);
        return p.A * 3 + p.B;
    }

    static Pair s_pair;

    // A constructor that throws: the caller catches, the local died with this frame, but the
    // static it would have been copied to must be untouched.
    [MethodImpl(NI)]
    static void NewThrowsInner(long a, bool fail)
    {
        Pair p = new Pair(a, fail);
        Touch(&p);
        s_pair = p;
    }

    [MethodImpl(NI)]
    static long NewThrowsInTry(long a, bool fail)
    {
        Pair p = new Pair(1, 2);
        try
        {
            p = new Pair(a, fail);
        }
        catch (E)
        {
        }
        Touch(&p);
        return p.A * 3 + p.B;
    }

    [MethodImpl(NI)]
    static int NewWideFromSelf(int a)
    {
        Wide w = new Wide(a);
        w = new Wide(w, 100);
        Touch(&w);
        return Mix(w.A, w.B, w.C, w.D, w.E, w.F, 0, 0, 0);
    }

    [MethodImpl(NI)]
    static long NewInLoop(int n)
    {
        long sum = 0;
        Pair p = new Pair(1, 2);
        for (int i = 0; i < n; i++)
        {
            p = new Pair(p);
            Touch(&p);
            sum = sum * 3 + p.A;
        }
        return sum + p.B;
    }

    // ---- locals that are not zeroed ----

    [MethodImpl(NI)]
    static int Dirty(int n)
    {
        byte* p = stackalloc byte[256];
        for (int i = 0; i < 256; i++) p[i] = (byte)(0xA5 ^ i);
        int s = 0;
        for (int i = 0; i < 256; i++) s += p[i] * n;
        return s;
    }

    [MethodImpl(NI)]
    static int StackallocIsZero(int n)
    {
        int* p = stackalloc int[n];
        int s = 0;
        for (int i = 0; i < n; i++) s |= p[i];
        return s;
    }

    [MethodImpl(NI)]
    static long StackallocSpanIsZero()
    {
        Span<long> s = stackalloc long[8];
        long r = 0;
        for (int i = 0; i < s.Length; i++) r |= s[i];
        return r;
    }

    [MethodImpl(NI)]
    static long DefaultIsZero()
    {
        Padded pd = default;
        Wide w = new Wide();
        Pair p = default;
        Touch(&pd); Touch(&w); Touch(&p);
        long r = 0;
        byte* raw = (byte*)&pd;
        for (int i = 0; i < sizeof(Padded); i++) r |= raw[i];
        return r | (uint)w.A | (uint)w.B | (uint)w.C | (uint)w.D | (uint)w.E | (uint)w.F | p.A | p.B;
    }

    [MethodImpl(NI)]
    static long OutIsZero()
    {
        MakeDefault(out Padded pd);
        return pd.Tag | pd.Value | (long)pd.Tail;
    }

    [MethodImpl(NI)] static void MakeDefault(out Padded pd) { pd = default; }

    // ---- null checks ----

    sealed class Node
    {
        public int V;
        [MethodImpl(AI)] public int Five() => 5;
    }

    static int s_nodeCalls;
    static readonly Node s_node = new Node { V = 3 };

    [MethodImpl(NI)] static Node NextNode() { s_nodeCalls++; return s_node; }
    [MethodImpl(NI)] static int CallOnResult() => NextNode().Five();
    [MethodImpl(NI)] static void DiscardField() { _ = NextNode().V; }
    [MethodImpl(NI)] static int LengthOfResult(int[][] a, ref int i) => a[i++].Length;

    static void TestNewObj()
    {
        Check("new from own fields", NewFromOwnFields(10, 20), 20 * 3 + 10);
        Check("new from itself by value", NewFromSelfByValue(10, 20), 20 * 3 + 10);
        Check("new from itself by in", NewFromSelfIn(10, 20), 21 * 3 + 10);
        Check("constructor reads this", NewReadsThis(10), 10 * 3 + 11);

        s_pair = new Pair(1, 2);
        int code = 0;
        try { NewThrowsInner(50, true); } catch (E ex) { code = ex.Code; }
        Check("constructor throws: code", code, 7);
        Check("constructor throws: static", s_pair.A * 3 + s_pair.B, 1 * 3 + 2);
        NewThrowsInner(50, false);
        Check("constructor does not throw: static", s_pair.A * 3 + s_pair.B, 50 * 3 + 50);
        Check("constructor throws in try", NewThrowsInTry(50, true), 1 * 3 + 2);
        Check("constructor does not throw in try", NewThrowsInTry(50, false), 50 * 3 + 50);

        Check("wide new from itself", NewWideFromSelf(1), Mix(106, 105, 104, 103, 102, 101, 0, 0, 0));
        // p alternates (2,1), (1,2), ...: A is 2, 1, 2, 1, 2.
        Check("new in a loop", NewInLoop(5), ((((2 * 3 + 1) * 3 + 2) * 3 + 1) * 3 + 2) + 1);

        Check("dirty", Dirty(1) != 0 ? 1 : 0, 1);
        Check("stackalloc is zero", StackallocIsZero(48), 0);
        Dirty(2);
        Check("stackalloc span is zero", StackallocSpanIsZero(), 0);
        Dirty(3);
        Check("default is zero", DefaultIsZero(), 0);
        Dirty(4);
        Check("out default is zero", OutIsZero(), 0);

        s_nodeCalls = 0;
        Check("call on a result", CallOnResult(), 5);
        DiscardField();
        Check("operands of null checks are evaluated", s_nodeCalls, 2);
        int[][] jag = { new int[3], new int[7] };
        int idx = 0;
        Check("length of a[i++]", LengthOfResult(jag, ref idx) * 10 + LengthOfResult(jag, ref idx), 37);
        Check("i after a[i++]", idx, 2);
        Group("constructors, locals, null checks");
    }

    static int Main()
    {
        TestHandlers();
        TestCloning();
        TestNewObj();

        if (fails == 0)
        {
            Console.WriteLine("ALL PASS");
            return 0;
        }
        Console.WriteLine("FAIL " + fails.ToString());
        return 1;
    }
}
