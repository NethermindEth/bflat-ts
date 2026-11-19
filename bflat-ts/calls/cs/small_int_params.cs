// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Parameters and return values smaller than 32 bits: bool, byte, sbyte,
// short, ushort and char.
//
// A caller may leave more than the declared bits in the register, so a
// method has to narrow such a parameter itself. On RISC-V a parameter that
// arrives in a register and is never assigned or address-taken is narrowed
// once on entry, in the register it lives in, and its uses rely on that; one
// that lives on the frame is loaded with a narrow load. What can go wrong: a
// use that sees the raw register, a parameter that is narrowed in the wrong
// register or not at all because it is first used late, in a loop, after a
// call or in a handler, and a parameter that does not qualify (assigned,
// address-taken, passed on the stack) but is treated as if it did.
//
// Every method here is called in two ways. Typed: the arguments are narrowed
// by the caller. Raw: through a function pointer whose signature has int in
// place of each small type, so the register holds a whole int with anything
// above the declared bits; the method must give the same result. A raw bool
// has 0 or 1 in its low byte. The raw values sit on both sides of every
// 8-bit and 16-bit limit.
//
// Methods: all six types used in comparisons, widening, shifts, arithmetic,
// conversions between each other and as indices; a bool and a byte tested in
// a loop; parameters on the stack; parameters that are assigned and
// address-taken; parameters first used after calls; parameters used in
// finally and catch blocks and in a filter; switches; parameters handed on
// to another method and through recursion; unused parameters; an instance
// method, a virtual, an interface and a shared generic method, a delegate,
// and an UnmanagedCallersOnly method called typed and raw. Return values of
// each type are widened by the caller.
//
// The expected values come from twins that take int parameters, are
// compiled without optimization and get values narrowed through memory.
// Each check prints one line; the run ends with ALL PASS or a FAIL count,
// and the guest returns the number of failures.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: DisableRuntimeMarshalling]

class E : Exception { public int Code; public E(int c) : base("e") { Code = c; } }

unsafe delegate long DSix(bool f, byte b, sbyte sb, short s, ushort us, char c, long* tab);

unsafe abstract class Base
{
    public abstract long VSix(bool f, byte b, sbyte sb, short s, ushort us, char c, long* tab);
}

unsafe interface ISix
{
    long ISixCall(bool f, byte b, sbyte sb, short s, ushort us, char c, long* tab);
}

unsafe sealed class Holder : Base, ISix
{
    public long K;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public long Six(bool f, byte b, sbyte sb, short s, ushort us, char c, long* tab)
        => Program.SixBody(f, b, sb, s, us, c, tab) + K;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override long VSix(bool f, byte b, sbyte sb, short s, ushort us, char c, long* tab)
        => Program.SixBody(f, b, sb, s, us, c, tab) + K;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public long ISixCall(bool f, byte b, sbyte sb, short s, ushort us, char c, long* tab)
        => Program.SixBody(f, b, sb, s, us, c, tab) + K;
}

// A second implementation, so that the calls above stay virtual.
unsafe sealed class Other : Base, ISix
{
    public override long VSix(bool f, byte b, sbyte sb, short s, ushort us, char c, long* tab) => 1;
    public long ISixCall(bool f, byte b, sbyte sb, short s, ushort us, char c, long* tab) => 2;
}

unsafe class Program
{
    const MethodImplOptions NoOpt = MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization;

    static int fails;

    static void Check(string name, bool ok)
    {
        Console.WriteLine((ok ? "ok   " : "FAIL ") + name);
        if (!ok) fails++;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Opaque(long x) => x * 0x100000001B3L + 7;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Thrower(int code)
    {
        if (code >= 0) throw new E(code);
        return 0;
    }

    // A raw value narrowed through memory.
    [MethodImpl(NoOpt)] static int AsByte(int raw) => *(byte*)&raw;
    [MethodImpl(NoOpt)] static int AsSByte(int raw) => *(sbyte*)&raw;
    [MethodImpl(NoOpt)] static int AsShort(int raw) => *(short*)&raw;
    [MethodImpl(NoOpt)] static int AsUShort(int raw) => *(ushort*)&raw;

    // ---- all six types, every kind of use ----

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long SixBody(bool f, byte b, sbyte sb, short s, ushort us, char c, long* tab)
    {
        long h = f ? 3 : 5;
        h = h * 31 + b;
        h = h * 31 + sb;
        h = h * 31 + s;
        h = h * 31 + us;
        h = h * 31 + c;
        h = h * 31 + (b < 0x80 ? 1 : 2) + (sb < 0 ? 4 : 8) + (s < -1 ? 16 : 32) + (us > 0x7FFF ? 64 : 128)
            + (c == 'A' ? 256 : 512);
        h = h * 31 + tab[b] + tab[us >> 8] + tab[c & 0xFF] + tab[sb + 128] + tab[(s >> 8) + 128];
        h = h * 31 + ((long)b << 40) + ((long)sb << 20) + ((long)s << 24) + ((long)us << 44) + ((long)c << 3);
        h = h * 31 + (1L << (b & 63)) + (tab[1] >> (us & 31)) + (tab[2] << (sb & 15));
        h = h * 31 + b * sb + s * us + c;
        h = h * 31 + (sbyte)b + (byte)sb + (ushort)s + (short)us + (byte)c + (short)c;
        h = h * 31 + (b == sb ? 1 : 0) + (s == us ? 2 : 0) + (c == us ? 4 : 0) + (b == c ? 8 : 0);
        h = h * 31 + (long)(ulong)b + (long)(ulong)sb + (long)(ulong)s + (long)(ulong)us;
        h = h * 31 + s / (b + 1) + us % (b + 1) + sb / (b + 1);
        h = h * 31 + (uint)sb + (uint)s + (f ? (int)b : sb);
        return h;
    }

    [MethodImpl(NoOpt)]
    static long SixRef(int f, int b, int sb, int s, int us, int c, long* tab)
    {
        long h = f != 0 ? 3 : 5;
        h = h * 31 + b;
        h = h * 31 + sb;
        h = h * 31 + s;
        h = h * 31 + us;
        h = h * 31 + c;
        h = h * 31 + (b < 0x80 ? 1 : 2) + (sb < 0 ? 4 : 8) + (s < -1 ? 16 : 32) + (us > 0x7FFF ? 64 : 128)
            + (c == 'A' ? 256 : 512);
        h = h * 31 + tab[b] + tab[us >> 8] + tab[c & 0xFF] + tab[sb + 128] + tab[(s >> 8) + 128];
        h = h * 31 + ((long)b << 40) + ((long)sb << 20) + ((long)s << 24) + ((long)us << 44) + ((long)c << 3);
        h = h * 31 + (1L << (b & 63)) + (tab[1] >> (us & 31)) + (tab[2] << (sb & 15));
        h = h * 31 + b * sb + s * us + c;
        h = h * 31 + (sbyte)b + (byte)sb + (ushort)s + (short)us + (byte)c + (short)c;
        h = h * 31 + (b == sb ? 1 : 0) + (s == us ? 2 : 0) + (c == us ? 4 : 0) + (b == c ? 8 : 0);
        h = h * 31 + (long)(ulong)b + (long)(ulong)sb + (long)(ulong)s + (long)(ulong)us;
        h = h * 31 + s / (b + 1) + us % (b + 1) + sb / (b + 1);
        h = h * 31 + (uint)sb + (uint)s + (f != 0 ? b : sb);
        return h;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Six(bool f, byte b, sbyte sb, short s, ushort us, char c, long* tab)
        => SixBody(f, b, sb, s, us, c, tab);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long GSix<T>(T tag, bool f, byte b, sbyte sb, short s, ushort us, char c, long* tab) where T : class
        => SixBody(f, b, sb, s, us, c, tab);

    [UnmanagedCallersOnly]
    static long USix(bool f, byte b, sbyte sb, short s, ushort us, char c, long* tab)
        => SixBody(f, b, sb, s, us, c, tab);

    // ---- a bool and a byte tested in a loop ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Loop(bool f, byte b, sbyte sb, int n)
    {
        long h = 0;
        for (int i = 0; i < n; i++)
        {
            if (f) h += i;
            if (b == i) h ^= 0x55;
            if (sb < i - 100) h += 3;
            h = h * 3 + b;
        }
        return h;
    }

    [MethodImpl(NoOpt)]
    static long LoopRef(int f, int b, int sb, int n)
    {
        long h = 0;
        for (int i = 0; i < n; i++)
        {
            if (f != 0) h += i;
            if (b == i) h ^= 0x55;
            if (sb < i - 100) h += 3;
            h = h * 3 + b;
        }
        return h;
    }

    // The loop is the first thing in the method.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Head(byte b, ushort us, short s, int n)
    {
        long h = 0;
        do
        {
            h = h * 31 + b + us + s;
            n--;
        }
        while (n > 0);
        return h;
    }

    [MethodImpl(NoOpt)]
    static long HeadRef(int b, int us, int s, int n)
    {
        long h = 0;
        do
        {
            h = h * 31 + b + us + s;
            n--;
        }
        while (n > 0);
        return h;
    }

    // ---- parameters on the stack ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Stack(long r0, long r1, long r2, long r3, long r4, long r5, byte b6, sbyte sb7,
                      bool f, byte b, sbyte sb, short s, ushort us, char c)
    {
        long h = r0 ^ r1 ^ r2 ^ r3 ^ r4 ^ r5;
        h = h * 31 + b6 + sb7 * 3;
        h = h * 31 + (f ? 7 : 9) + b + sb * 5 + s * 7 + us * 11 + c * 13;
        h = h * 31 + (b6 == b ? 1 : 2) + (sb7 < sb ? 3 : 4) + (s < us ? 5 : 6) + (c > us ? 7 : 8);
        return h * 31 + ((long)sb << 33) + ((long)us << 40) + ((long)sb7 << 36);
    }

    [MethodImpl(NoOpt)]
    static long StackRef(long r0, long r1, long r2, long r3, long r4, long r5, int b6, int sb7,
                         int f, int b, int sb, int s, int us, int c)
    {
        long h = r0 ^ r1 ^ r2 ^ r3 ^ r4 ^ r5;
        h = h * 31 + b6 + sb7 * 3;
        h = h * 31 + (f != 0 ? 7 : 9) + b + sb * 5 + s * 7 + us * 11 + c * 13;
        h = h * 31 + (b6 == b ? 1 : 2) + (sb7 < sb ? 3 : 4) + (s < us ? 5 : 6) + (c > us ? 7 : 8);
        return h * 31 + ((long)sb << 33) + ((long)us << 40) + ((long)sb7 << 36);
    }

    // ---- parameters that are assigned or address-taken ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Stored(byte b, sbyte sb, short s, ushort us, bool f, int n)
    {
        long h = b + sb + s + us;
        for (int i = 0; i < n; i++)
        {
            b = (byte)(b + 1);
            sb = (sbyte)(sb - 1);
            s = (short)(s + 300);
            us = (ushort)(us - 500);
            f = !f;
            h = h * 31 + b + sb + s + us + (f ? 1 : 0);
        }
        return h;
    }

    [MethodImpl(NoOpt)]
    static long StoredRef(int b, int sb, int s, int us, int f, int n)
    {
        long h = b + sb + s + us;
        for (int i = 0; i < n; i++)
        {
            b = (byte)(b + 1);
            sb = (sbyte)(sb - 1);
            s = (short)(s + 300);
            us = (ushort)(us - 500);
            f = f != 0 ? 0 : 1;
            h = h * 31 + b + sb + s + us + (f != 0 ? 1 : 0);
        }
        return h;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Bump(ref byte b, ref short s) { b += 7; s -= 9; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Read(sbyte* p) => *p * 3;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Exposed(byte b, short s, sbyte sb, char c)
    {
        long h = b + s * 3 + sb * 5 + c;
        Bump(ref b, ref s);
        h = h * 31 + Read(&sb);
        return h * 31 + b + s * 3 + sb * 5 + c;
    }

    [MethodImpl(NoOpt)]
    static long ExposedRef(int b, int s, int sb, int c)
    {
        long h = b + s * 3 + sb * 5 + c;
        b = (byte)(b + 7);
        s = (short)(s - 9);
        h = h * 31 + sb * 3;
        return h * 31 + b + s * 3 + sb * 5 + c;
    }

    // ---- parameters first used after calls ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Across(byte b, sbyte sb, short s, ushort us, char c, bool f, long* tab)
    {
        long h = Opaque(tab[3]);
        for (int i = 0; i < 4; i++)
        {
            h = h * 31 + Opaque(i) + b + sb;
            if (f) h += s;
            h ^= us + c;
        }
        return h + tab[b] + Opaque(us) + (sb < s ? 1 : 2);
    }

    [MethodImpl(NoOpt)]
    static long AcrossRef(int b, int sb, int s, int us, int c, int f, long* tab)
    {
        long h = Opaque(tab[3]);
        for (int i = 0; i < 4; i++)
        {
            h = h * 31 + Opaque(i) + b + sb;
            if (f != 0) h += s;
            h ^= us + c;
        }
        return h + tab[b] + Opaque(us) + (sb < s ? 1 : 2);
    }

    // ---- parameters used in handlers ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Handlers(byte b, sbyte sb, short s, ushort us, char c, bool f, int code)
    {
        long h = 0;
        try
        {
            h += b;
            try
            {
                Thrower(code);
                h += sb;
            }
            finally
            {
                h = h * 31 + s + (f ? 1 : 2);
            }
        }
        catch (E e) when (e.Code != us + 100000)
        {
            h = h * 31 + c + us + b;
        }
        return h * 31 + sb + c;
    }

    [MethodImpl(NoOpt)]
    static long HandlersRef(int b, int sb, int s, int us, int c, int f, int code)
    {
        long h = b;
        if (code < 0) h += sb;
        h = h * 31 + s + (f != 0 ? 1 : 2);
        if (code >= 0) h = h * 31 + c + us + b;
        return h * 31 + sb + c;
    }

    // ---- switches ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Switches(byte b, sbyte sb, char c, short s)
    {
        int r;
        switch (b)
        {
            case 0: r = 10; break;
            case 1: r = 20; break;
            case 2: r = 30; break;
            case 3: r = 40; break;
            case 4: r = 50; break;
            case 5: r = 60; break;
            default: r = 70; break;
        }
        switch (sb)
        {
            case -3: r += 1; break;
            case -2: r += 2; break;
            case -1: r += 3; break;
            case 0: r += 4; break;
            case 1: r += 5; break;
            case 2: r += 6; break;
            default: r += 7; break;
        }
        switch (c)
        {
            case 'a': r += 100; break;
            case 'b': r += 200; break;
            case 'c': r += 300; break;
            case 'd': r += 400; break;
            case 'e': r += 500; break;
            default: r += 600; break;
        }
        switch (s)
        {
            case -32768: return r + 1000;
            case -32767: return r + 2000;
            case -32766: return r + 3000;
            case -32765: return r + 4000;
            case -32764: return r + 5000;
            default: return r + 6000;
        }
    }

    [MethodImpl(NoOpt)]
    static int SwitchesRef(int b, int sb, int c, int s)
    {
        int r = b <= 5 ? (b + 1) * 10 : 70;
        r += sb >= -3 && sb <= 2 ? sb + 4 : 7;
        r += c >= 'a' && c <= 'e' ? (c - 'a' + 1) * 100 : 600;
        return r + (s <= -32764 ? (s + 32769) * 1000 : 6000);
    }

    // ---- parameters handed on ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Forward(bool f, byte b, sbyte sb, short s, ushort us, char c, long* tab)
        => Six(f, b, sb, s, us, c, tab) * 3 + Six(!f, (byte)sb, (sbyte)b, (short)us, (ushort)s, c, tab) + b + sb;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Recurse(byte b, short s, sbyte sb, int depth)
        => depth == 0 ? b + s * 3 + sb * 5 : Recurse(b, s, sb, depth - 1) * 7 + b + sb;

    [MethodImpl(NoOpt)]
    static long RecurseRef(int b, int s, int sb, int depth)
        => depth == 0 ? b + s * 3 + sb * 5 : RecurseRef(b, s, sb, depth - 1) * 7 + b + sb;

    // ---- parameters that are not used, or only on one path ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Partly(byte b, short s, bool f, sbyte unused, long x)
    {
        if (x > 0) return x + (f ? b : s);
        return x - 1;
    }

    [MethodImpl(NoOpt)]
    static long PartlyRef(int b, int s, int f, long x)
    {
        if (x > 0) return x + (f != 0 ? b : s);
        return x - 1;
    }

    // ---- return values ----

    [MethodImpl(MethodImplOptions.NoInlining)] static byte IdByte(byte v) => v;
    [MethodImpl(MethodImplOptions.NoInlining)] static sbyte IdSByte(sbyte v) => v;
    [MethodImpl(MethodImplOptions.NoInlining)] static short IdShort(short v) => v;
    [MethodImpl(MethodImplOptions.NoInlining)] static ushort IdUShort(ushort v) => v;
    [MethodImpl(MethodImplOptions.NoInlining)] static char IdChar(char v) => v;
    [MethodImpl(MethodImplOptions.NoInlining)] static bool IdBool(bool v) => v;
    [MethodImpl(MethodImplOptions.NoInlining)] static byte CutByte(int v) => (byte)v;
    [MethodImpl(MethodImplOptions.NoInlining)] static sbyte CutSByte(int v) => (sbyte)v;
    [MethodImpl(MethodImplOptions.NoInlining)] static short CutShort(int v) => (short)v;
    [MethodImpl(MethodImplOptions.NoInlining)] static ushort CutUShort(int v) => (ushort)v;
    [MethodImpl(MethodImplOptions.NoInlining)] static char CutChar(long v) => (char)v;

    // ---- the calls ----

    // Function pointers with the real signature and with int for each small
    // type, kept in fields so that the calls stay indirect.
    static delegate*<bool, byte, sbyte, short, ushort, char, long*, long> typedSix, typedForward;
    static delegate*<int, int, int, int, int, int, long*, long> rawSix, rawForward, rawAcross;
    static delegate* unmanaged<bool, byte, sbyte, short, ushort, char, long*, long> typedUSix;
    static delegate* unmanaged<int, int, int, int, int, int, long*, long> rawUSix;
    static delegate*<int, int, int, int, long> rawLoop, rawHead, rawExposed, rawRecurse;
    static delegate*<long, long, long, long, long, long, int, int, int, int, int, int, int, int, long> rawStack;
    static delegate*<int, int, int, int, int, int, long> rawStored;
    static delegate*<int, int, int, int, int, int, int, long> rawHandlers;
    static delegate*<int, int, int, int, int> rawSwitches;
    static delegate*<int, int, int, int, long, long> rawPartly;
    static delegate*<int, byte> rawIdByte;
    static delegate*<int, sbyte> rawIdSByte;
    static delegate*<int, short> rawIdShort;
    static delegate*<int, ushort> rawIdUShort;
    static delegate*<int, char> rawIdChar;
    static delegate*<int, bool> rawIdBool;
    static Holder holder;
    static Base viaBase;
    static ISix viaInterface;
    static DSix closed, open;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Base Make(int kind) => kind == 0 ? new Holder() : new Other();

    static void Setup()
    {
        typedSix = &Six;
        typedForward = &Forward;
        rawSix = (delegate*<int, int, int, int, int, int, long*, long>)typedSix;
        rawForward = (delegate*<int, int, int, int, int, int, long*, long>)typedForward;
        rawAcross = (delegate*<int, int, int, int, int, int, long*, long>)(delegate*<byte, sbyte, short, ushort, char, bool, long*, long>)&Across;
        typedUSix = &USix;
        rawUSix = (delegate* unmanaged<int, int, int, int, int, int, long*, long>)typedUSix;
        rawLoop = (delegate*<int, int, int, int, long>)(delegate*<bool, byte, sbyte, int, long>)&Loop;
        rawHead = (delegate*<int, int, int, int, long>)(delegate*<byte, ushort, short, int, long>)&Head;
        rawExposed = (delegate*<int, int, int, int, long>)(delegate*<byte, short, sbyte, char, long>)&Exposed;
        rawRecurse = (delegate*<int, int, int, int, long>)(delegate*<byte, short, sbyte, int, long>)&Recurse;
        rawStack = (delegate*<long, long, long, long, long, long, int, int, int, int, int, int, int, int, long>)
            (delegate*<long, long, long, long, long, long, byte, sbyte, bool, byte, sbyte, short, ushort, char, long>)&Stack;
        rawStored = (delegate*<int, int, int, int, int, int, long>)(delegate*<byte, sbyte, short, ushort, bool, int, long>)&Stored;
        rawHandlers = (delegate*<int, int, int, int, int, int, int, long>)
            (delegate*<byte, sbyte, short, ushort, char, bool, int, long>)&Handlers;
        rawSwitches = (delegate*<int, int, int, int, int>)(delegate*<byte, sbyte, char, short, int>)&Switches;
        rawPartly = (delegate*<int, int, int, int, long, long>)(delegate*<byte, short, bool, sbyte, long, long>)&Partly;
        rawIdByte = (delegate*<int, byte>)(delegate*<byte, byte>)&IdByte;
        rawIdSByte = (delegate*<int, sbyte>)(delegate*<sbyte, sbyte>)&IdSByte;
        rawIdShort = (delegate*<int, short>)(delegate*<short, short>)&IdShort;
        rawIdUShort = (delegate*<int, ushort>)(delegate*<ushort, ushort>)&IdUShort;
        rawIdChar = (delegate*<int, char>)(delegate*<char, char>)&IdChar;
        rawIdBool = (delegate*<int, bool>)(delegate*<bool, bool>)&IdBool;

        holder = (Holder)Make(0);
        viaBase = holder;
        viaInterface = holder;
        Base other = Make(1);
        if (other.VSix(false, 0, 0, 0, 0, 'x', null) != 1 || ((ISix)other).ISixCall(false, 0, 0, 0, 0, 'x', null) != 2) fails++;
        closed = holder.Six;
        open = Six;
    }

    static readonly int[] Raws =
    {
        0, 1, 2, 5, 0x41, 0x61, 0x7F, 0x80, 0xFF, 0x100, 0x101, 0x17F, 0x180, 0x1FF, 0x7FFF, 0x8000, 0x8001,
        0xFFFF, 0x10000, 0x10001, 0x18000, 0x1FFFF, -1, -2, -3, -128, -129, -256, -257, -32768, -32769,
        -65536, -65537, int.MinValue, int.MaxValue, 0x12345678, unchecked((int)0x87654321), 0x00FF00FF,
        unchecked((int)0xFFFF8003), unchecked((int)0x7FFF8000),
    };

    static bool okSix = true, okKinds = true, okLoop = true, okStack = true, okStored = true, okAcross = true;
    static bool okHandlers = true, okSwitch = true, okForward = true, okPartly = true, okReturn = true;

    // One set of raw values through every method, typed and raw.
    static void Run(int rf, int rb, int rsb, int rs, int rus, int rc, long* tab, bool all)
    {
        // A bool keeps 0 or 1 in its low byte.
        rf &= unchecked((int)0xFFFFFF01);
        int nf = AsByte(rf), nb = AsByte(rb), nsb = AsSByte(rsb), ns = AsShort(rs), nus = AsUShort(rus), nc = AsUShort(rc);
        bool f = nf != 0;

        long want = SixRef(nf, nb, nsb, ns, nus, nc, tab);
        okSix &= Six(f, (byte)rb, (sbyte)rsb, (short)rs, (ushort)rus, (char)rc, tab) == want;
        okSix &= rawSix(rf, rb, rsb, rs, rus, rc, tab) == want;

        okLoop &= Loop(f, (byte)rb, (sbyte)rsb, 300) == LoopRef(nf, nb, nsb, 300);
        okLoop &= rawLoop(rf, rb, rsb, 300) == LoopRef(nf, nb, nsb, 300);
        okLoop &= rawHead(rb, rus, rs, 5) == HeadRef(nb, nus, ns, 5);

        okSwitch &= Switches((byte)rb, (sbyte)rsb, (char)rc, (short)rs) == SwitchesRef(nb, nsb, nc, ns);
        okSwitch &= rawSwitches(rb, rsb, rc, rs) == SwitchesRef(nb, nsb, nc, ns);

        okReturn &= rawIdByte(rb) == nb && rawIdSByte(rsb) == nsb && rawIdShort(rs) == ns;
        okReturn &= rawIdUShort(rus) == nus && rawIdChar(rc) == nc && rawIdBool(rf) == f;
        okReturn &= CutByte(rb) == nb && CutSByte(rsb) == nsb && CutShort(rs) == ns && CutUShort(rus) == nus;
        okReturn &= CutChar(((long)rs << 32) | (uint)rc) == nc;
        long widened = (long)rawIdByte(rb) * 3 + (long)rawIdSByte(rsb) * 5 + (long)rawIdShort(rs) * 7
                       + (long)rawIdUShort(rus) * 11 + (long)rawIdChar(rc) * 13;
        okReturn &= widened == (long)nb * 3 + (long)nsb * 5 + (long)ns * 7 + (long)nus * 11 + (long)nc * 13;
        okReturn &= tab[rawIdByte(rb)] == tab[nb] && tab[CutByte(rs)] == tab[AsByte(rs)];

        okPartly &= rawPartly(rb, rs, rf, rsb, rus) == PartlyRef(nb, ns, nf, rus);
        okPartly &= Partly((byte)rb, (short)rs, f, (sbyte)rsb, -rus) == PartlyRef(nb, ns, nf, -rus);

        if (!all) return;

        okKinds &= holder.Six(f, (byte)rb, (sbyte)rsb, (short)rs, (ushort)rus, (char)rc, tab) == want;
        okKinds &= viaBase.VSix(f, (byte)rb, (sbyte)rsb, (short)rs, (ushort)rus, (char)rc, tab) == want;
        okKinds &= viaInterface.ISixCall(f, (byte)rb, (sbyte)rsb, (short)rs, (ushort)rus, (char)rc, tab) == want;
        okKinds &= closed(f, (byte)rb, (sbyte)rsb, (short)rs, (ushort)rus, (char)rc, tab) == want;
        okKinds &= open(f, (byte)rb, (sbyte)rsb, (short)rs, (ushort)rus, (char)rc, tab) == want;
        okKinds &= GSix<string>(null, f, (byte)rb, (sbyte)rsb, (short)rs, (ushort)rus, (char)rc, tab) == want;
        okKinds &= typedSix(f, (byte)rb, (sbyte)rsb, (short)rs, (ushort)rus, (char)rc, tab) == want;
        okKinds &= typedUSix(f, (byte)rb, (sbyte)rsb, (short)rs, (ushort)rus, (char)rc, tab) == want;
        okKinds &= rawUSix(rf, rb, rsb, rs, rus, rc, tab) == want;

        long wantStack = StackRef(rb, rs, 3, 4, 5, 6, nb, nsb, nf, AsByte(rs), AsSByte(rb), ns, nus, nc);
        okStack &= Stack(rb, rs, 3, 4, 5, 6, (byte)rb, (sbyte)rsb, f, (byte)rs, (sbyte)rb, (short)rs, (ushort)rus, (char)rc)
                   == wantStack;
        okStack &= rawStack(rb, rs, 3, 4, 5, 6, rb, rsb, rf, rs, rb, rs, rus, rc) == wantStack;

        okStored &= Stored((byte)rb, (sbyte)rsb, (short)rs, (ushort)rus, f, 9) == StoredRef(nb, nsb, ns, nus, nf, 9);
        okStored &= rawStored(rb, rsb, rs, rus, rf, 9) == StoredRef(nb, nsb, ns, nus, nf, 9);
        okStored &= rawExposed(rb, rs, rsb, rc) == ExposedRef(nb, ns, nsb, nc);

        okAcross &= Across((byte)rb, (sbyte)rsb, (short)rs, (ushort)rus, (char)rc, f, tab)
                    == AcrossRef(nb, nsb, ns, nus, nc, nf, tab);
        okAcross &= rawAcross(rb, rsb, rs, rus, rc, rf, tab) == AcrossRef(nb, nsb, ns, nus, nc, nf, tab);

        long wantForward = want * 3 + SixRef(nf ^ 1, AsByte(nsb), AsSByte(nb), AsShort(nus), AsUShort(ns), nc, tab) + nb + nsb;
        okForward &= Forward(f, (byte)rb, (sbyte)rsb, (short)rs, (ushort)rus, (char)rc, tab) == wantForward;
        okForward &= rawForward(rf, rb, rsb, rs, rus, rc, tab) == wantForward;
        okForward &= rawRecurse(rb, rs, rsb, 3) == RecurseRef(nb, ns, nsb, 3);
    }

    static void RunHandlers(int rf, int rb, int rsb, int rs, int rus, int rc)
    {
        rf &= unchecked((int)0xFFFFFF01);
        int nf = AsByte(rf), nb = AsByte(rb), nsb = AsSByte(rsb), ns = AsShort(rs), nus = AsUShort(rus), nc = AsUShort(rc);
        okHandlers &= rawHandlers(rb, rsb, rs, rus, rc, rf, -1) == HandlersRef(nb, nsb, ns, nus, nc, nf, -1);
        okHandlers &= rawHandlers(rb, rsb, rs, rus, rc, rf, 5) == HandlersRef(nb, nsb, ns, nus, nc, nf, 5);
        okHandlers &= Handlers((byte)rb, (sbyte)rsb, (short)rs, (ushort)rus, (char)rc, nf != 0, 6)
                      == HandlersRef(nb, nsb, ns, nus, nc, nf, 6);
    }

    static int Main()
    {
        Setup();
        long* tab = stackalloc long[256];
        for (int i = 0; i < 256; i++) tab[i] = (i + 1) * 0x0101010101010101L + i;

        // The same raw value in every position, then mixed ones.
        int n = Raws.Length;
        for (int i = 0; i < n; i++)
        {
            Run(Raws[i], Raws[i], Raws[i], Raws[i], Raws[i], Raws[i], tab, true);
            RunHandlers(Raws[i], Raws[(i + 1) % n], Raws[(i + 2) % n], Raws[(i + 3) % n], Raws[(i + 4) % n], Raws[(i + 5) % n]);
        }
        uint seed = 12345;
        int* r = stackalloc int[6];
        for (int k = 0; k < 1500; k++)
        {
            for (int j = 0; j < 6; j++)
            {
                seed = seed * 1664525 + 1013904223;
                r[j] = Raws[(int)((seed >> 16) % (uint)n)];
            }
            Run(r[0], r[1], r[2], r[3], r[4], r[5], tab, k < 300);
        }

        Check("six types", okSix);
        Check("call kinds", okKinds);
        Check("loops", okLoop);
        Check("stack parameters", okStack);
        Check("assigned and address-taken", okStored);
        Check("after calls", okAcross);
        Check("handlers", okHandlers);
        Check("switches", okSwitch);
        Check("handed on", okForward);
        Check("partly used", okPartly);
        Check("return values", okReturn);

        Console.WriteLine(fails == 0 ? "ALL PASS" : "FAILED " + fails);
        return fails;
    }
}
