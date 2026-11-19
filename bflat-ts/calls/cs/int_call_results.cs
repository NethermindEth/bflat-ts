// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// The 32-bit result of a call, in a 64-bit register.
//
// A managed method sign-extends an int it returns, and the compiler trusts
// that: the caller of a managed method does not extend the result again
// before it widens it, compares it or indexes with it, and a method that
// returns such a result moves it with mv. Native code, runtime helpers and
// calls through a pointer are not trusted, their result is extended by the
// caller. What can go wrong: a kind of call taken for a managed one that is
// not, a return path of a method that leaves the register as it was (a value
// from a 64-bit local, a struct that arrived in a register, a value kept in
// a 64-bit stack slot), and a result that is extended at the call but not
// after it was stored to a local or reloaded.
//
// The callees here return a 64-bit value where the caller's signature says
// int: libc labs through DllImport declared as int and as uint, a managed
// and an unmanaged method through a function pointer of another type. The
// result has ones or zeroes above bit 31 that are not its sign. Next to them
// are well-typed callees whose return statement gets its value from a place
// with such bits: a 64-bit parameter, a struct parameter, a P/Invoke, a
// local that crossed a call or an exception handler.
//
// Every kind of call is used in the same ways: widened, added, compared as
// int and as long, used as an index, kept across a call, returned again.
// The expected values are computed without optimization from 64-bit values.
// A failed check prints one line; the run ends with ALL PASS or a FAIL
// count, and the guest returns 1 when anything failed.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

interface IKind
{
    static abstract int Call(long c);
    // What the call returns, as a 64-bit value; only its low half counts.
    static abstract long Ref(long c);
}

struct I1 { public int A; }
struct S2 { public short A; public short B; }
struct I2 { public int A; public int B; }

interface ILow { int Low(long c); int LowG<T>(long c); }

abstract class Shape
{
    public abstract int Low(long c);
    public abstract int LowG<T>(long c);
}

sealed class ShapeA : Shape, ILow
{
    [MethodImpl(MethodImplOptions.NoInlining)] public override int Low(long c) => (int)c;
    [MethodImpl(MethodImplOptions.NoInlining)] public override int LowG<T>(long c) => (int)c;
}

sealed class ShapeB : Shape, ILow
{
    public override int Low(long c) => 1;
    public override int LowG<T>(long c) => 2;
}

class Bomb : Exception { }

unsafe class Program
{
    const MethodImplOptions AI = MethodImplOptions.AggressiveInlining;
    const MethodImplOptions NI = MethodImplOptions.NoInlining;
    const MethodImplOptions NoOpt = MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization;

    [DllImport("*", EntryPoint = "labs")] static extern int labs_int(long x);
    [DllImport("*", EntryPoint = "labs")] static extern uint labs_uint(long x);
    [DllImport("*", EntryPoint = "labs"), SuppressGCTransition] static extern int labs_fast(long x);
    [DllImport("*", EntryPoint = "labs")] static extern short labs_short(long x);
    [DllImport("*", EntryPoint = "labs")] static extern ushort labs_ushort(long x);
    [DllImport("*", EntryPoint = "labs")] static extern sbyte labs_sbyte(long x);
    [DllImport("*", EntryPoint = "labs")] static extern byte labs_byte(long x);

    static int fails;
    static int checks;
    static int cell;
    static long[] Tab;
    static Shape Virt;
    static ILow Iface;

    static delegate*<long, int> PtrWrong;               // points at a method that returns long
    static delegate*<long, int> PtrRight;
    static delegate* unmanaged<long, int> UPtrWrong;    // points at an unmanaged method that returns long
    static delegate*<long, long> EntryI1, EntryS2, EntryI1Pass, EntryS2Pass, EntryI1Field, EntryS2Field;

    [MethodImpl(NI)] static long Id(long x) => x;
    [MethodImpl(NI)] static int RetLow(long x) => (int)x;
    [UnmanagedCallersOnly] static long UId(long x) => x;
    [MethodImpl(NI)] static Shape Make(int k) => k == 0 ? new ShapeA() : new ShapeB();
    [MethodImpl(NI)] static void Opaque() { cell ^= 0; }
    [MethodImpl(NI)] static void Throw() => throw new Bomb();

    [MethodImpl(NoOpt)]
    static void Setup()
    {
        Tab = new long[16];
        for (int i = 0; i < 16; i++) Tab[i] = (i + 1) * 0x0101010101010101L;
        Virt = Make(0);
        Iface = (ILow)Virt;
        Shape other = Make(1);
        if (other.Low(0) != 1 || other.LowG<int>(0) != 2) fails++;
        PtrWrong = (delegate*<long, int>)(delegate*<long, long>)&Id;
        PtrRight = &RetLow;
        UPtrWrong = (delegate* unmanaged<long, int>)(delegate* unmanaged<long, long>)&UId;
        EntryI1 = (delegate*<long, long>)(delegate*<I1, long>)&WidenI1;
        EntryS2 = (delegate*<long, long>)(delegate*<S2, long>)&WidenS2;
        EntryI1Pass = (delegate*<long, long>)(delegate*<I1, long>)&WidenPassedI1;
        EntryS2Pass = (delegate*<long, long>)(delegate*<S2, long>)&WidenPassedS2;
        EntryI1Field = (delegate*<long, long>)(delegate*<I1, long>)&ReturnFieldI1;
        EntryS2Field = (delegate*<long, long>)(delegate*<S2, long>)&ReturnFieldS2;
    }

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

    [MethodImpl(NoOpt)] static long Abs(long c) => c < 0 ? -c : c;

    // ---- well-typed methods whose return value comes from a place with other upper bits ----

    // A struct of one int arrives in a register whose upper half the caller does not define.
    [MethodImpl(NI)] static int FieldI1(I1 s) => s.A;
    [MethodImpl(NI)] static I1 PassI1(I1 s) => s;
    [MethodImpl(NI)] static S2 PassS2(S2 s) => s;
    [MethodImpl(NI)] static long WidenI1(I1 s) => s.A;
    [MethodImpl(NI)] static long WidenS2(S2 s) => s.A * 65536L + s.B;
    [MethodImpl(NI)] static long WidenPassedI1(I1 s) => PassI1(s).A;
    [MethodImpl(NI)] static long WidenPassedS2(S2 s) { S2 t = PassS2(s); return t.A * 65536L + t.B; }
    [MethodImpl(NI)] static long ReturnFieldI1(I1 s) => FieldI1(s);
    [MethodImpl(NI)] static long ReturnFieldS2(S2 s) => (long)FieldA(s) * 65536L + FieldB(s);
    [MethodImpl(NI)] static short FieldA(S2 s) => s.A;
    [MethodImpl(NI)] static short FieldB(S2 s) => s.B;
    [MethodImpl(NI)] static I1 MakeI1(long c) { I1 s; s.A = (int)c; return s; }
    [MethodImpl(NI)] static I2 MakeI2(long c) { I2 s; s.A = (int)c; s.B = (int)(c >> 32); return s; }

    // The value returned is the low half of a 64-bit parameter, kept in a 32-bit local that is
    // changed on one path. Nothing on the other path writes the register again.
    [MethodImpl(NI)] static int Clamp(long c) { int x = (int)c; if (x == 7) x = 1; return x; }
    [MethodImpl(NI)] static int Fold(long c) { int h = (int)c; if (h < 0) h = ~h; return ~h; }
    [MethodImpl(NI)] static uint ClampU(long c) { uint x = (uint)c; if (x == 7) x = 1; return x; }

    [MethodImpl(NI)]
    static int Pick(long c, int sel)
    {
        if (sel == 0) return (int)c;
        if (sel == 1) return 1;
        if (sel == 2) return 2;
        if (sel == 3) return 3;
        if (sel == 4) return 4;
        return 5;
    }

    [MethodImpl(NI)]
    static int Loop(long c, int n)
    {
        int x = (int)c;
        for (int i = 0; i < n; i++) x = x * 31 + i;
        return x;
    }

    // Returns from several places.
    [MethodImpl(NI)]
    static int Several(long c, int sel)
    {
        if (sel == 0) return (int)c;
        if (sel == 1) return labs_int(c);
        if (sel == 2) return PtrWrong(c);
        if (sel == 3) return (int)labs_uint(c);
        if (sel == 4) return UPtrWrong(c);
        return (int)(c >> 1);
    }

    [MethodImpl(NI)]
    static int AfterFinally(long c)
    {
        try { return PtrWrong(c); }
        finally { Opaque(); }
    }

    [MethodImpl(NI)]
    static int FromCatch(long c)
    {
        int v = PtrWrong(c);
        try { Throw(); }
        catch (Bomb) { return v; }
        return 0;
    }

    [MethodImpl(NI)]
    static int NativeFromCatch(long c)
    {
        int v = labs_int(c);
        try { Throw(); }
        catch (Bomb) { return v; }
        return 0;
    }

    [MethodImpl(NI)]
    static int AcrossCall(long c)
    {
        int v = labs_int(c);
        Opaque();
        return v;
    }

    [MethodImpl(NI)] static int Direct(long c) => labs_int(c);
    [MethodImpl(NI)] static uint DirectU(long c) => labs_uint(c);
    [MethodImpl(NI)] static int DirectPtr(long c) => PtrWrong(c);
    [MethodImpl(NI)] static int DirectUPtr(long c) => UPtrWrong(c);
    [MethodImpl(NI)] static int Chain(long c) => RetLow(c);                // a tail call
    [MethodImpl(NI)] static int ChainVirtual(long c) => Virt.Low(c);
    [MethodImpl(NI)] static int Ternary(long c, bool f) => f ? labs_int(c) : PtrWrong(c);

    // ---- the kinds of call ----

    struct Native : IKind { [MethodImpl(AI)] public static int Call(long c) => labs_int(c); public static long Ref(long c) => Abs(c); }
    struct NativeUint : IKind { [MethodImpl(AI)] public static int Call(long c) => (int)labs_uint(c); public static long Ref(long c) => Abs(c); }
    struct NativeFast : IKind { [MethodImpl(AI)] public static int Call(long c) => labs_fast(c); public static long Ref(long c) => Abs(c); }
    struct NativeShort : IKind { [MethodImpl(AI)] public static int Call(long c) => labs_short(c); public static long Ref(long c) => S16(Abs(c)); }
    struct NativeUShort : IKind { [MethodImpl(AI)] public static int Call(long c) => labs_ushort(c); public static long Ref(long c) => Abs(c) & 0xFFFF; }
    struct NativeSByte : IKind { [MethodImpl(AI)] public static int Call(long c) => labs_sbyte(c); public static long Ref(long c) => S8(Abs(c)); }
    struct NativeByte : IKind { [MethodImpl(AI)] public static int Call(long c) => labs_byte(c); public static long Ref(long c) => Abs(c) & 0xFF; }
    struct WrongPointer : IKind { [MethodImpl(AI)] public static int Call(long c) => PtrWrong(c); public static long Ref(long c) => c; }
    struct RightPointer : IKind { [MethodImpl(AI)] public static int Call(long c) => PtrRight(c); public static long Ref(long c) => c; }
    struct WrongUnmanaged : IKind { [MethodImpl(AI)] public static int Call(long c) => UPtrWrong(c); public static long Ref(long c) => c; }
    struct Managed : IKind { [MethodImpl(AI)] public static int Call(long c) => RetLow(c); public static long Ref(long c) => c; }
    struct Virtual : IKind { [MethodImpl(AI)] public static int Call(long c) => Virt.Low(c); public static long Ref(long c) => c; }
    struct Interface : IKind { [MethodImpl(AI)] public static int Call(long c) => Iface.Low(c); public static long Ref(long c) => c; }
    struct GenericVirtual : IKind { [MethodImpl(AI)] public static int Call(long c) => Virt.LowG<string>(c); public static long Ref(long c) => c; }
    struct GenericInterface : IKind { [MethodImpl(AI)] public static int Call(long c) => Iface.LowG<object>(c); public static long Ref(long c) => c; }
    struct ViaSeveral0 : IKind { [MethodImpl(AI)] public static int Call(long c) => Several(c, 0); public static long Ref(long c) => c; }
    struct ViaSeveral1 : IKind { [MethodImpl(AI)] public static int Call(long c) => Several(c, 1); public static long Ref(long c) => Abs(c); }
    struct ViaSeveral2 : IKind { [MethodImpl(AI)] public static int Call(long c) => Several(c, 2); public static long Ref(long c) => c; }
    struct ViaSeveral3 : IKind { [MethodImpl(AI)] public static int Call(long c) => Several(c, 3); public static long Ref(long c) => Abs(c); }
    struct ViaSeveral4 : IKind { [MethodImpl(AI)] public static int Call(long c) => Several(c, 4); public static long Ref(long c) => c; }
    struct ViaSeveral5 : IKind { [MethodImpl(AI)] public static int Call(long c) => Several(c, 5); public static long Ref(long c) => c >> 1; }
    struct ViaFinally : IKind { [MethodImpl(AI)] public static int Call(long c) => AfterFinally(c); public static long Ref(long c) => c; }
    struct ViaCatch : IKind { [MethodImpl(AI)] public static int Call(long c) => FromCatch(c); public static long Ref(long c) => c; }
    struct ViaNativeCatch : IKind { [MethodImpl(AI)] public static int Call(long c) => NativeFromCatch(c); public static long Ref(long c) => Abs(c); }
    struct ViaAcrossCall : IKind { [MethodImpl(AI)] public static int Call(long c) => AcrossCall(c); public static long Ref(long c) => Abs(c); }
    struct ViaDirect : IKind { [MethodImpl(AI)] public static int Call(long c) => Direct(c); public static long Ref(long c) => Abs(c); }
    struct ViaDirectU : IKind { [MethodImpl(AI)] public static int Call(long c) => (int)DirectU(c); public static long Ref(long c) => Abs(c); }
    struct ViaDirectPtr : IKind { [MethodImpl(AI)] public static int Call(long c) => DirectPtr(c); public static long Ref(long c) => c; }
    struct ViaDirectUPtr : IKind { [MethodImpl(AI)] public static int Call(long c) => DirectUPtr(c); public static long Ref(long c) => c; }
    struct ViaChain : IKind { [MethodImpl(AI)] public static int Call(long c) => Chain(c); public static long Ref(long c) => c; }
    struct ViaChainVirtual : IKind { [MethodImpl(AI)] public static int Call(long c) => ChainVirtual(c); public static long Ref(long c) => c; }
    struct ViaTernary : IKind { [MethodImpl(AI)] public static int Call(long c) => Ternary(c, (c & 1) != 0); public static long Ref(long c) => (c & 1) != 0 ? Abs(c) : c; }
    struct ViaClamp : IKind { [MethodImpl(AI)] public static int Call(long c) => Clamp(c); public static long Ref(long c) => S32(c) == 7 ? 1 : c; }
    struct ViaFold : IKind { [MethodImpl(AI)] public static int Call(long c) => Fold(c); public static long Ref(long c) => S32(c) < 0 ? c : ~c; }
    struct ViaClampU : IKind { [MethodImpl(AI)] public static int Call(long c) => (int)ClampU(c); public static long Ref(long c) => S32(c) == 7 ? 1 : c; }
    struct ViaPick : IKind { [MethodImpl(AI)] public static int Call(long c) => Pick(c, 0); public static long Ref(long c) => c; }
    struct ViaLoop : IKind { [MethodImpl(AI)] public static int Call(long c) => Loop(c, 0); public static long Ref(long c) => c; }
    struct StructI1 : IKind { [MethodImpl(AI)] public static int Call(long c) => MakeI1(c).A; public static long Ref(long c) => c; }
    struct StructI2 : IKind { [MethodImpl(AI)] public static int Call(long c) => MakeI2(c).B ^ MakeI2(c).A; public static long Ref(long c) => (c >> 32) ^ c; }
    struct StructI1Field : IKind { [MethodImpl(AI)] public static int Call(long c) => FieldI1(MakeI1(c)); public static long Ref(long c) => c; }

    // ---- users ----

    [MethodImpl(NI)] static long UWiden<T>(long c, long k) where T : struct, IKind => T.Call(c);
    [MethodImpl(NI)] static long UZext<T>(long c, long k) where T : struct, IKind => (long)(uint)T.Call(c);
    [MethodImpl(NI)] static long UAdd<T>(long c, long k) where T : struct, IKind => (long)T.Call(c) + k;
    [MethodImpl(NI)] static long UMul<T>(long c, long k) where T : struct, IKind => (long)T.Call(c) * k;
    [MethodImpl(NI)] static bool UIntLt<T>(long c, long k) where T : struct, IKind => T.Call(c) < (int)k;
    [MethodImpl(NI)] static bool UUintLt<T>(long c, long k) where T : struct, IKind => (uint)T.Call(c) < (uint)k;
    [MethodImpl(NI)] static bool ULongEq<T>(long c, long k) where T : struct, IKind => (long)T.Call(c) == k;
    [MethodImpl(NI)] static bool ULongLt<T>(long c, long k) where T : struct, IKind => (long)T.Call(c) < k;
    [MethodImpl(NI)] static int UReturn<T>(long c, long k) where T : struct, IKind => T.Call(c);
    [MethodImpl(NI)] static uint UReturnU<T>(long c, long k) where T : struct, IKind => (uint)T.Call(c);

    [MethodImpl(NI)]
    static long UBranch<T>(long c, long k) where T : struct, IKind
    {
        int p = T.Call(c);
        long r = 0;
        if (p < (int)k) r |= 1;
        if ((uint)p < (uint)k) r |= 2;
        if (p == (int)k) r |= 4;
        if (p >= 0) r |= 8;
        if ((long)p == k) r |= 16;
        if ((long)p < k) r |= 32;
        if ((ulong)(uint)p == (ulong)k) r |= 64;
        if (p == -1) r |= 128;
        return r;
    }

    [MethodImpl(NoOpt)]
    static long RBranch(long p, long k)
    {
        long r = 0;
        if (p < S32(k)) r |= 1;
        if (Z32(p) < Z32(k)) r |= 2;
        if (p == S32(k)) r |= 4;
        if (p >= 0) r |= 8;
        if (p == k) r |= 16;
        if (p < k) r |= 32;
        if (Z32(p) == k) r |= 64;
        if (p == -1) r |= 128;
        return r;
    }

    [MethodImpl(NI)]
    static long UIndex<T>(long c, long k) where T : struct, IKind
    {
        long[] t = Tab;
        int p = T.Call(c);
        return ((uint)p < (uint)t.Length ? t[p] : -1) * 3 + t[p & 15];
    }

    [MethodImpl(NoOpt)] static long RIndex(long p, long k) => (p >= 0 && p < 16 ? Tab[p] : -1) * 3 + Tab[p & 15];

    [MethodImpl(NI)]
    static long UKept<T>(long c, long k) where T : struct, IKind
    {
        int v = T.Call(c);
        Opaque();
        return v + k;
    }

    [MethodImpl(NI)]
    static long UKeptTwo<T>(long c, long k) where T : struct, IKind
    {
        int v = T.Call(c);
        int w = T.Call(k);
        Opaque();
        return (long)v * 3 + w + (v < w ? 100 : 200);
    }

    [MethodImpl(NoOpt)] static long RKeptTwo(long p, long q) => p * 3 + q + (p < q ? 100 : 200);

    [MethodImpl(NI)]
    static long UPhi<T>(long c, long k) where T : struct, IKind
    {
        int v = (k & 1) != 0 ? T.Call(c) : (int)k;
        return (long)v * 3 + (uint)v;
    }

    [MethodImpl(NoOpt)] static long RPhi(long p, long k) => ((k & 1) != 0 ? p : S32(k)) * 3 + Z32((k & 1) != 0 ? p : k);

    [MethodImpl(NI)]
    static long ULoop<T>(long c, long k) where T : struct, IKind
    {
        long sum = 0;
        int acc = (int)k;
        for (int i = 0; i < 3; i++)
        {
            acc ^= T.Call(c);
            sum = sum * 3 + acc;
        }
        return sum;
    }

    [MethodImpl(NoOpt)] static long RLoop(long p, long k) => (S32(k ^ p) * 3 + S32(k)) * 3 + S32(k ^ p);

    [MethodImpl(NI)]
    static long UHandler<T>(long c, long k) where T : struct, IKind
    {
        int v = T.Call(c);
        try { Throw(); }
        catch (Bomb) { return v + k; }
        return 0;
    }

    [MethodImpl(NI)]
    static long UStore<T>(long c, long k) where T : struct, IKind
    {
        long r;
        int v = T.Call(c);
        long* p = &r;
        *p = v;
        Opaque();
        return r + (short)v * 3 + (byte)v * 5;
    }

    [MethodImpl(NoOpt)] static long RStore(long p, long k) => p + S16(p) * 3 + (p & 0xFF) * 5;

    // ---- driver ----

    static readonly long[] Cs =
    {
        0, 1, -1, 5, -7, 0x7FFFFFFF, 0x80000000L, 0xFFFFFFFFL, 0x100000000L, 0x100000005L, 0x17FFFFFFFL, 0x180000000L,
        0x1FFFFFFFFL, -0x80000000L, -0x80000001L, -0x100000000L, -0x100000003L, 0x123456789ABCDEF0L,
        unchecked((long)0xFEDCBA9876543210), 0x7FFFFFFF00000000L, 0x00007FFF80008000L, 0x5555555AAAAAAAA5L,
        0x0000000100008000L, 0x00000001000000FFL, 0x0000000100000080L,
    };

    static void Eq(string kind, string user, long c, long k, long want, long got)
    {
        checks++;
        if (want == got) return;
        fails++;
        // The first failure of every user of a kind is printed.
        for (int i = 0; i < seenCount; i++)
            if (seen[i] == user) return;
        if (seenCount < seen.Length) seen[seenCount++] = user;
        Console.WriteLine("FAIL " + kind + "/" + user + " c=0x" + c.ToString("x") + " k=0x" + k.ToString("x") + " expected 0x"
                          + want.ToString("x") + " got 0x" + got.ToString("x"));
    }

    static readonly string[] seen = new string[64];
    static int seenCount;

    [MethodImpl(NoOpt)] static long RefOf<T>(long c) where T : struct, IKind => S32(T.Ref(c));
    [MethodImpl(NoOpt)] static long B(bool v) => v ? 1 : 0;

    static void Run<T>() where T : struct, IKind
    {
        string n = typeof(T).Name;
        int before = fails;
        seenCount = 0;
        foreach (long c in Cs)
        {
            long p = RefOf<T>(c);
            foreach (long d in Cs)
            {
                long k = d;
                Eq(n, "widened", c, k, p, UWiden<T>(c, k));
                Eq(n, "zero-extended", c, k, Z32(p), UZext<T>(c, k));
                Eq(n, "added", c, k, p + k, UAdd<T>(c, k));
                Eq(n, "multiplied", c, k, p * k, UMul<T>(c, k));
                Eq(n, "int <", c, k, B(p < S32(k)), B(UIntLt<T>(c, k)));
                Eq(n, "uint <", c, k, B(Z32(p) < Z32(k)), B(UUintLt<T>(c, k)));
                Eq(n, "long ==", c, k, B(p == k), B(ULongEq<T>(c, k)));
                Eq(n, "long <", c, k, B(p < k), B(ULongLt<T>(c, k)));
                Eq(n, "branches", c, k, RBranch(p, k), UBranch<T>(c, k));
                Eq(n, "kept across a call", c, k, p + k, UKept<T>(c, k));
                Eq(n, "two kept across a call", c, k, RKeptTwo(p, RefOf<T>(k)), UKeptTwo<T>(c, k));
                Eq(n, "phi", c, k, RPhi(p, k), UPhi<T>(c, k));
                Eq(n, "loop", c, k, RLoop(p, k), ULoop<T>(c, k));
                Eq(n, "stored", c, k, RStore(p, k), UStore<T>(c, k));
                Eq(n, "returned", c, k, p, UReturn<T>(c, k));
                Eq(n, "returned as uint", c, k, Z32(p), UReturnU<T>(c, k));
                k = p + (d & 3) * 0x100000000L - 0x100000000L;
                Eq(n, "long == (2)", c, k, B(p == k), B(ULongEq<T>(c, k)));
                Eq(n, "branches (2)", c, k, RBranch(p, k), UBranch<T>(c, k));
            }
            Eq(n, "index", c, 0, RIndex(p, 0), UIndex<T>(c, 0));
            Eq(n, "live in a handler", c, 7, p + 7, UHandler<T>(c, 7));
        }
        if (fails != before) Console.WriteLine("FAIL " + n + ": " + (fails - before) + " checks");
    }

    // A struct of 4 bytes that arrives in a register with other upper bits.
    static void Structs()
    {
        int before = fails;
        seenCount = 0;
        foreach (long c in Cs)
        {
            Eq("struct of one int", "field widened", c, 0, S32(c), EntryI1(c));
            Eq("struct of one int", "passed on and returned", c, 0, S32(c), EntryI1Pass(c));
            Eq("struct of one int", "field returned as int", c, 0, S32(c), EntryI1Field(c));
            long want = S16(c) * 65536L + S16(c >> 16);
            Eq("struct of two shorts", "fields widened", c, 0, want, EntryS2(c));
            Eq("struct of two shorts", "passed on and returned", c, 0, want, EntryS2Pass(c));
            Eq("struct of two shorts", "fields returned as short", c, 0, want, EntryS2Field(c));
        }
        if (fails != before) Console.WriteLine("FAIL struct parameters: " + (fails - before) + " checks");
    }

    static int Main()
    {
        Setup();

        Run<Native>(); Run<NativeUint>(); Run<NativeFast>(); Run<NativeShort>(); Run<NativeUShort>(); Run<NativeSByte>(); Run<NativeByte>();
        Run<WrongPointer>(); Run<RightPointer>(); Run<WrongUnmanaged>();
        Run<Managed>(); Run<Virtual>(); Run<Interface>(); Run<GenericVirtual>(); Run<GenericInterface>();
        Run<ViaSeveral0>(); Run<ViaSeveral1>(); Run<ViaSeveral2>(); Run<ViaSeveral3>(); Run<ViaSeveral4>(); Run<ViaSeveral5>();
        Run<ViaFinally>(); Run<ViaCatch>(); Run<ViaNativeCatch>(); Run<ViaAcrossCall>();
        Run<ViaDirect>(); Run<ViaDirectU>(); Run<ViaDirectPtr>(); Run<ViaDirectUPtr>(); Run<ViaChain>(); Run<ViaChainVirtual>(); Run<ViaTernary>();
        Run<ViaClamp>(); Run<ViaFold>(); Run<ViaClampU>(); Run<ViaPick>(); Run<ViaLoop>();
        Run<StructI1>(); Run<StructI2>(); Run<StructI1Field>();
        Structs();

        Console.WriteLine(fails == 0 ? "ALL PASS (" + checks + " checks)" : "FAILED " + fails + " of " + checks);
        return fails == 0 ? 0 : 1;
    }
}
