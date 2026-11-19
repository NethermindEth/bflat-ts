// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Block copies and block fills of a constant size.
//
// A struct assignment, Unsafe.CopyBlock/InitBlock, a Span.CopyTo, Clear or
// Fill of a constant length and a struct passed or returned by value are
// expanded in line up to a size limit: straight loads and stores of eight,
// four, two and one bytes, for a Span.CopyTo all loads before the first
// store (the two spans may overlap), on ZisK one memcpy or memset precompile
// instruction pair, and one byte at a time where an unaligned access is not
// allowed. Above the limit the same operation is a call. What can go wrong:
// a tail of one to seven bytes that is skipped, copied twice from the wrong
// place or written past the end, a size right at a limit that takes neither
// form, a copy of overlapping spans that reads bytes it has already
// overwritten, a temporary register of the expansion that is also the
// source or destination address, an address that is formed from the wrong
// base (a local, a field at an offset, an array element), a fill value that
// is not replicated to all bytes, and a wide access through an address that
// is not a multiple of its width.
//
// Every operation runs for every size from 1 to 66 bytes and for sizes on
// both sides of 72, 80, 96, 120, 128, 136, 192, 256 and 264, with the
// source and the destination at different distances from an 8-byte boundary.
// Span copies also run with the destination before and after the source at
// distances smaller than the length. The second part repeats the copies and
// fills with every kind of address: a local, a field of an object, a static,
// an element of an array, a by-reference parameter, a value passed and
// returned, and structs that hold object references.
//
// The expected bytes come from loops that move one byte at a time and are
// compiled without optimization; a destination is compared together with 32
// bytes before and after it. Each group prints one line; the run ends with
// ALL PASS or a FAIL count.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

unsafe struct Z1 { public fixed byte B[1]; }
unsafe struct Z2 { public fixed byte B[2]; }
unsafe struct Z3 { public fixed byte B[3]; }
unsafe struct Z4 { public fixed byte B[4]; }
unsafe struct Z5 { public fixed byte B[5]; }
unsafe struct Z6 { public fixed byte B[6]; }
unsafe struct Z7 { public fixed byte B[7]; }
unsafe struct Z8 { public fixed byte B[8]; }
unsafe struct Z9 { public fixed byte B[9]; }
unsafe struct Z10 { public fixed byte B[10]; }
unsafe struct Z11 { public fixed byte B[11]; }
unsafe struct Z12 { public fixed byte B[12]; }
unsafe struct Z13 { public fixed byte B[13]; }
unsafe struct Z14 { public fixed byte B[14]; }
unsafe struct Z15 { public fixed byte B[15]; }
unsafe struct Z16 { public fixed byte B[16]; }
unsafe struct Z17 { public fixed byte B[17]; }
unsafe struct Z18 { public fixed byte B[18]; }
unsafe struct Z19 { public fixed byte B[19]; }
unsafe struct Z20 { public fixed byte B[20]; }
unsafe struct Z21 { public fixed byte B[21]; }
unsafe struct Z22 { public fixed byte B[22]; }
unsafe struct Z23 { public fixed byte B[23]; }
unsafe struct Z24 { public fixed byte B[24]; }
unsafe struct Z25 { public fixed byte B[25]; }
unsafe struct Z26 { public fixed byte B[26]; }
unsafe struct Z27 { public fixed byte B[27]; }
unsafe struct Z28 { public fixed byte B[28]; }
unsafe struct Z29 { public fixed byte B[29]; }
unsafe struct Z30 { public fixed byte B[30]; }
unsafe struct Z31 { public fixed byte B[31]; }
unsafe struct Z32 { public fixed byte B[32]; }
unsafe struct Z33 { public fixed byte B[33]; }
unsafe struct Z34 { public fixed byte B[34]; }
unsafe struct Z35 { public fixed byte B[35]; }
unsafe struct Z36 { public fixed byte B[36]; }
unsafe struct Z37 { public fixed byte B[37]; }
unsafe struct Z38 { public fixed byte B[38]; }
unsafe struct Z39 { public fixed byte B[39]; }
unsafe struct Z40 { public fixed byte B[40]; }
unsafe struct Z41 { public fixed byte B[41]; }
unsafe struct Z42 { public fixed byte B[42]; }
unsafe struct Z43 { public fixed byte B[43]; }
unsafe struct Z44 { public fixed byte B[44]; }
unsafe struct Z45 { public fixed byte B[45]; }
unsafe struct Z46 { public fixed byte B[46]; }
unsafe struct Z47 { public fixed byte B[47]; }
unsafe struct Z48 { public fixed byte B[48]; }
unsafe struct Z49 { public fixed byte B[49]; }
unsafe struct Z50 { public fixed byte B[50]; }
unsafe struct Z51 { public fixed byte B[51]; }
unsafe struct Z52 { public fixed byte B[52]; }
unsafe struct Z53 { public fixed byte B[53]; }
unsafe struct Z54 { public fixed byte B[54]; }
unsafe struct Z55 { public fixed byte B[55]; }
unsafe struct Z56 { public fixed byte B[56]; }
unsafe struct Z57 { public fixed byte B[57]; }
unsafe struct Z58 { public fixed byte B[58]; }
unsafe struct Z59 { public fixed byte B[59]; }
unsafe struct Z60 { public fixed byte B[60]; }
unsafe struct Z61 { public fixed byte B[61]; }
unsafe struct Z62 { public fixed byte B[62]; }
unsafe struct Z63 { public fixed byte B[63]; }
unsafe struct Z64 { public fixed byte B[64]; }
unsafe struct Z65 { public fixed byte B[65]; }
unsafe struct Z66 { public fixed byte B[66]; }
unsafe struct Z71 { public fixed byte B[71]; }
unsafe struct Z72 { public fixed byte B[72]; }
unsafe struct Z73 { public fixed byte B[73]; }
unsafe struct Z79 { public fixed byte B[79]; }
unsafe struct Z80 { public fixed byte B[80]; }
unsafe struct Z95 { public fixed byte B[95]; }
unsafe struct Z96 { public fixed byte B[96]; }
unsafe struct Z97 { public fixed byte B[97]; }
unsafe struct Z119 { public fixed byte B[119]; }
unsafe struct Z120 { public fixed byte B[120]; }
unsafe struct Z121 { public fixed byte B[121]; }
unsafe struct Z127 { public fixed byte B[127]; }
unsafe struct Z128 { public fixed byte B[128]; }
unsafe struct Z129 { public fixed byte B[129]; }
unsafe struct Z130 { public fixed byte B[130]; }
unsafe struct Z136 { public fixed byte B[136]; }
unsafe struct Z160 { public fixed byte B[160]; }
unsafe struct Z191 { public fixed byte B[191]; }
unsafe struct Z192 { public fixed byte B[192]; }
unsafe struct Z193 { public fixed byte B[193]; }
unsafe struct Z248 { public fixed byte B[248]; }
unsafe struct Z255 { public fixed byte B[255]; }
unsafe struct Z256 { public fixed byte B[256]; }
unsafe struct Z257 { public fixed byte B[257]; }
unsafe struct Z264 { public fixed byte B[264]; }
unsafe struct Z300 { public fixed byte B[300]; }

struct W3 { public long A, B, C; }
struct W5 { public long A, B, C, D, E; }
struct W17 { public long A0, A1, A2, A3, A4, A5, A6, A7, A8, A9, A10, A11, A12, A13, A14, A15, A16; }
struct W40 { public W5 P0, P1, P2, P3, P4, P5, P6, P7; }
struct G3 { public object O; public long L; public string S; }
struct G6 { public long L0; public object O0; public long L1; public string S0; public int[] A0; public long L2; }
struct G40 { public G6 P0, P1, P2, P3, P4, P5; public long T0, T1, T2; public object Last; }
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct Odd21 { public byte B; public long L0; public int I; public long L1; }

class Holder
{
    public long Before = 0x1111111111111111;
    public W3 F3;
    public long Mid0 = 0x2222222222222222;
    public W5 F5;
    public long Mid1 = 0x3333333333333333;
    public W17 F17;
    public long Mid2 = 0x4444444444444444;
    public W40 F40;
    public long Mid3 = 0x5555555555555555;
    public Odd21 FOdd;
    public byte Mid4 = 0x66;
    public G3 FG3;
    public G6 FG6;
    public G40 FG40;
    public long After = 0x7777777777777777;
}

// Byte loops the JIT must not turn into the operations under test.
static unsafe class Ref
{
    const MethodImplOptions Plain = MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization;

    [MethodImpl(Plain)]
    public static void Fill(byte* p, int n, int seed)
    {
        for (int i = 0; i < n; i++)
            p[i] = (byte)(seed * 29 + i * 7 + (i >> 4) + 1);
    }

    [MethodImpl(Plain)]
    public static void Copy(byte* d, byte* s, int n)
    {
        for (int i = 0; i < n; i++)
            d[i] = s[i];
    }

    [MethodImpl(Plain)]
    public static void Set(byte* d, byte v, int n)
    {
        for (int i = 0; i < n; i++)
            d[i] = v;
    }

    // The index of the first difference, -1 if there is none.
    [MethodImpl(Plain)]
    public static int Diff(byte* a, byte* b, int n)
    {
        for (int i = 0; i < n; i++)
        {
            if (a[i] != b[i])
                return i;
        }
        return -1;
    }

    [MethodImpl(Plain)]
    public static void Touch(void* p)
    {
    }
}

// The operations under test, one instantiation per size.
static unsafe class Ops<T> where T : unmanaged
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Assign(byte* d, byte* s) => *(T*)d = *(T*)s;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void CopyUnaligned(byte* d, byte* s) => Unsafe.CopyBlockUnaligned(d, s, (uint)sizeof(T));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void CopyAligned(byte* d, byte* s) => Unsafe.CopyBlock(d, s, (uint)sizeof(T));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SpanCopy(byte* d, byte* s) =>
        new ReadOnlySpan<byte>(s, sizeof(T)).CopyTo(new Span<byte>(d, sizeof(T)));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void BufferCopy(byte* d, byte* s) => Buffer.MemoryCopy(s, d, sizeof(T), sizeof(T));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ViaLocal(byte* d, byte* s)
    {
        T t = *(T*)s;
        Ref.Touch(&t);
        *(T*)d = t;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ViaTwoLocals(byte* d, byte* s)
    {
        T t = *(T*)s;
        Ref.Touch(&t);
        T u = t;
        Ref.Touch(&u);
        *(T*)d = u;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T Ret(byte* s) => *(T*)s;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Take(T v, byte* d) => *(T*)d = v;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ReadWrite(byte* d, byte* s) => Unsafe.WriteUnaligned(d, Unsafe.ReadUnaligned<T>(s));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Zero(byte* d) => *(T*)d = default;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void InitZeroUnaligned(byte* d) => Unsafe.InitBlockUnaligned(d, 0, (uint)sizeof(T));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void InitZeroAligned(byte* d) => Unsafe.InitBlock(d, 0, (uint)sizeof(T));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void InitConst(byte* d) => Unsafe.InitBlockUnaligned(d, 0xA5, (uint)sizeof(T));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void InitVar(byte* d, byte v) => Unsafe.InitBlockUnaligned(d, v, (uint)sizeof(T));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SpanClear(byte* d) => new Span<byte>(d, sizeof(T)).Clear();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SpanFill(byte* d) => new Span<byte>(d, sizeof(T)).Fill(0x5A);

    // Two copies in one method: the temporaries of the first must not survive into the second.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Swap(byte* a, byte* b)
    {
        T t = *(T*)a;
        *(T*)a = *(T*)b;
        *(T*)b = t;
    }
}

static unsafe class Program
{
    const int Guard = 32;
    const int OpCount = 18;

    static byte* s_src;
    static byte* s_dst;
    static byte* s_exp;
    static byte* s_tmp;
    static int s_fails;
    static int s_groupFails;

    static void Fail(string name, int size, int sm, int dm, int at)
    {
        s_groupFails++;
        if (s_groupFails <= 4)
        {
            Console.WriteLine("FAIL " + name + " size " + size + " src+" + sm + " dst+" + dm + " expected byte " +
                              s_exp[at - Guard] + " got " + s_dst[at - Guard] + " at offset " + (at - Guard - dm));
        }
    }

    static void EndGroup(string name)
    {
        if (s_groupFails == 0)
            Console.WriteLine("ok   " + name);
        else
            Console.WriteLine("FAIL " + name + " cases=" + s_groupFails);
        s_fails += s_groupFails;
        s_groupFails = 0;
    }

    static string OpName(int op) => op switch
    {
        0 => "Assign", 1 => "CopyUnaligned", 2 => "CopyAligned", 3 => "SpanCopy", 4 => "BufferCopy",
        5 => "ViaLocal", 6 => "ViaTwoLocals", 7 => "RetTake", 8 => "ReadWrite", 9 => "Zero",
        10 => "InitZeroUnaligned", 11 => "InitZeroAligned", 12 => "InitConst", 13 => "InitVar",
        14 => "SpanClear", 15 => "SpanFill", 16 => "Swap", _ => "AssignSelf",
    };

    static void One<T>(int op, int sm, int dm) where T : unmanaged
    {
        int n = sizeof(T);
        int span = n + 2 * Guard + 8;
        byte* s = s_src + sm;
        byte* d = s_dst + dm;
        byte* e = s_exp + dm;
        Ref.Fill(s_src - Guard, span, n + op);
        Ref.Fill(s_dst - Guard, span, n + op + 101);
        Ref.Copy(s_exp - Guard, s_dst - Guard, span);
        Ref.Copy(s_tmp - Guard, s_src - Guard, span);

        switch (op)
        {
            case 0: Ref.Copy(e, s, n); Ops<T>.Assign(d, s); break;
            case 1: Ref.Copy(e, s, n); Ops<T>.CopyUnaligned(d, s); break;
            case 2: Ref.Copy(e, s, n); Ops<T>.CopyAligned(d, s); break;
            case 3: Ref.Copy(e, s, n); Ops<T>.SpanCopy(d, s); break;
            case 4: Ref.Copy(e, s, n); Ops<T>.BufferCopy(d, s); break;
            case 5: Ref.Copy(e, s, n); Ops<T>.ViaLocal(d, s); break;
            case 6: Ref.Copy(e, s, n); Ops<T>.ViaTwoLocals(d, s); break;
            case 7: Ref.Copy(e, s, n); Ops<T>.Take(Ops<T>.Ret(s), d); break;
            case 8: Ref.Copy(e, s, n); Ops<T>.ReadWrite(d, s); break;
            case 9: Ref.Set(e, 0, n); Ops<T>.Zero(d); break;
            case 10: Ref.Set(e, 0, n); Ops<T>.InitZeroUnaligned(d); break;
            case 11: Ref.Set(e, 0, n); Ops<T>.InitZeroAligned(d); break;
            case 12: Ref.Set(e, 0xA5, n); Ops<T>.InitConst(d); break;
            case 13: Ref.Set(e, (byte)(0x80 + n), n); Ops<T>.InitVar(d, (byte)(0x80 + n)); break;
            case 14: Ref.Set(e, 0, n); Ops<T>.SpanClear(d); break;
            case 15: Ref.Set(e, 0x5A, n); Ops<T>.SpanFill(d); break;
            case 16:
                // The destination gets the source and the source the destination.
                Ref.Copy(e, s, n);
                Ref.Copy(s_tmp + sm, d, n);
                Ops<T>.Swap(d, s);
                break;
            default:
                // A struct assigned to itself.
                Ops<T>.Assign(d, d);
                break;
        }

        int at = Ref.Diff(s_exp - Guard, s_dst - Guard, span);
        if (at >= 0)
            Fail(OpName(op), n, sm, dm, at);
        at = Ref.Diff(s_tmp - Guard, s_src - Guard, span);
        if (at >= 0)
            Fail(OpName(op) + " (source changed)", n, sm, dm, Guard + dm);
    }

    // A copy between two spans of one buffer, `delta` bytes apart.
    static void Overlap<T>(bool buffer, int sm, int delta) where T : unmanaged
    {
        int n = sizeof(T);
        int span = 3 * n + 2 * Guard + 16;
        byte* s = s_dst + n + sm;
        byte* d = s + delta;
        Ref.Fill(s_dst - Guard, span, n + delta);
        Ref.Copy(s_exp - Guard, s_dst - Guard, span);
        Ref.Copy(s_tmp, s, n);
        Ref.Copy(s_exp + (d - s_dst), s_tmp, n);
        if (buffer)
            Ops<T>.BufferCopy(d, s);
        else
            Ops<T>.SpanCopy(d, s);
        int at = Ref.Diff(s_exp - Guard, s_dst - Guard, span);
        if (at >= 0)
        {
            s_groupFails++;
            if (s_groupFails <= 4)
            {
                Console.WriteLine("FAIL " + (buffer ? "BufferCopy" : "SpanCopy") + " overlapping size " + n + " src+" + sm +
                                  " distance " + delta + " expected byte " + s_exp[at - Guard] + " got " +
                                  s_dst[at - Guard] + " at offset " + (at - Guard - (int)(d - s_dst)));
            }
        }
    }

    static void Run<T>() where T : unmanaged
    {
        int n = sizeof(T);
        for (int op = 0; op < OpCount; op++)
        {
            for (int i = 0; i < 8; i++)
            {
                One<T>(op, i, i);
                One<T>(op, i, (i * 3 + 1) & 7);
                if (i != 0)
                {
                    One<T>(op, 0, i);
                    One<T>(op, i, 0);
                }
            }
        }

        for (int sm = 0; sm < 8; sm += (sm == 0) ? 1 : 4)
        {
            for (int delta = -n - 1; delta <= n + 1; delta++)
            {
                int a = (delta < 0) ? -delta : delta;
                if ((n > 20) && (a > 9) && (a < n - 9) && ((a & 15) != 0))
                    continue;
                Overlap<T>(false, sm, delta);
                Overlap<T>(true, sm, delta);
            }
        }
    }

    // ---- every kind of address ----

    static W3 s_w3;
    static W5 s_w5;
    static W17 s_w17;
    static W40 s_w40;
    static Odd21 s_odd;
    static G6 s_g6;
    static readonly object s_obj = new object();
    static readonly int[] s_ints = new int[3];

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    static void FillLongs(long* p, int count, long seed)
    {
        for (int i = 0; i < count; i++)
            p[i] = Pat(seed, i);
    }

    static long Pat(long seed, long i) => unchecked(seed * 0x0101010101010101 + i * 0x1000100010001 + 3);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    static long SumLongs(long* p, int count)
    {
        long sum = 0;
        for (int i = 0; i < count; i++)
            sum = sum * 31 + p[i];
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)] static void FieldFromPtr3(Holder h, W3* p) => h.F3 = *p;
    [MethodImpl(MethodImplOptions.NoInlining)] static void FieldFromPtr5(Holder h, W5* p) => h.F5 = *p;
    [MethodImpl(MethodImplOptions.NoInlining)] static void FieldFromPtr17(Holder h, W17* p) => h.F17 = *p;
    [MethodImpl(MethodImplOptions.NoInlining)] static void FieldFromPtr40(Holder h, W40* p) => h.F40 = *p;
    [MethodImpl(MethodImplOptions.NoInlining)] static void PtrFromField3(W3* p, Holder h) => *p = h.F3;
    [MethodImpl(MethodImplOptions.NoInlining)] static void PtrFromField5(W5* p, Holder h) => *p = h.F5;
    [MethodImpl(MethodImplOptions.NoInlining)] static void PtrFromField17(W17* p, Holder h) => *p = h.F17;
    [MethodImpl(MethodImplOptions.NoInlining)] static void PtrFromField40(W40* p, Holder h) => *p = h.F40;
    [MethodImpl(MethodImplOptions.NoInlining)] static void FieldFromField5(Holder a, Holder b) => a.F5 = b.F5;
    [MethodImpl(MethodImplOptions.NoInlining)] static void FieldFromField17(Holder a, Holder b) => a.F17 = b.F17;
    [MethodImpl(MethodImplOptions.NoInlining)] static void StaticFromPtr5(W5* p) => s_w5 = *p;
    [MethodImpl(MethodImplOptions.NoInlining)] static void PtrFromStatic5(W5* p) => *p = s_w5;
    [MethodImpl(MethodImplOptions.NoInlining)] static void StaticFromPtr17(W17* p) => s_w17 = *p;
    [MethodImpl(MethodImplOptions.NoInlining)] static void PtrFromStatic17(W17* p) => *p = s_w17;
    [MethodImpl(MethodImplOptions.NoInlining)] static void ElemFromPtr5(W5[] a, int i, W5* p) => a[i] = *p;
    [MethodImpl(MethodImplOptions.NoInlining)] static void PtrFromElem5(W5* p, W5[] a, int i) => *p = a[i];
    [MethodImpl(MethodImplOptions.NoInlining)] static void ElemFromElem5(W5[] a, int i, int j) => a[i] = a[j];
    [MethodImpl(MethodImplOptions.NoInlining)] static void ConstElemFromPtr5(W5[] a, W5* p) => a[2] = *p;
    [MethodImpl(MethodImplOptions.NoInlining)] static void RefFromRef5(ref W5 d, ref W5 s) => d = s;
    [MethodImpl(MethodImplOptions.NoInlining)] static void RefFromIn17(ref W17 d, in W17 s) => d = s;
    [MethodImpl(MethodImplOptions.NoInlining)] static void OutFromPtr5(out W5 d, W5* p) => d = *p;
    [MethodImpl(MethodImplOptions.NoInlining)] static void ZeroField3(Holder h) => h.F3 = default;
    [MethodImpl(MethodImplOptions.NoInlining)] static void ZeroField5(Holder h) => h.F5 = default;
    [MethodImpl(MethodImplOptions.NoInlining)] static void ZeroField17(Holder h) => h.F17 = default;
    [MethodImpl(MethodImplOptions.NoInlining)] static void ZeroField40(Holder h) => h.F40 = default;
    [MethodImpl(MethodImplOptions.NoInlining)] static void ZeroFieldOdd(Holder h) => h.FOdd = default;
    [MethodImpl(MethodImplOptions.NoInlining)] static void ZeroStatic17() => s_w17 = default;
    [MethodImpl(MethodImplOptions.NoInlining)] static void ZeroStatic40() => s_w40 = default;
    [MethodImpl(MethodImplOptions.NoInlining)] static void ZeroElem5(W5[] a, int i) => a[i] = default;
    [MethodImpl(MethodImplOptions.NoInlining)] static void ZeroRef17(ref W17 d) => d = default;
    [MethodImpl(MethodImplOptions.NoInlining)] static void ZeroOut40(out W40 d) => d = default;
    [MethodImpl(MethodImplOptions.NoInlining)] static void FieldOddFromPtr(Holder h, Odd21* p) => h.FOdd = *p;
    [MethodImpl(MethodImplOptions.NoInlining)] static void PtrFromFieldOdd(Odd21* p, Holder h) => *p = h.FOdd;
    [MethodImpl(MethodImplOptions.NoInlining)] static void StaticOddFromPtr(Odd21* p) => s_odd = *p;
    [MethodImpl(MethodImplOptions.NoInlining)] static W5 Return5(W5* p) => *p;
    [MethodImpl(MethodImplOptions.NoInlining)] static W17 Return17(Holder h) => h.F17;
    [MethodImpl(MethodImplOptions.NoInlining)] static long ByValue5(W5 v, long k) => v.A + v.B * 3 + v.C * 5 + v.D * 7 + v.E * 11 + k;
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long ByValue17(long k0, W17 v, long k1) => v.A0 + v.A8 * 3 + v.A16 * 5 + k0 * 7 + k1 * 11;

    // Many values live across a copy: its temporaries must come from free registers.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long LiveAcross(Holder h, W5* p, W17* q, long a, long b, long c, long d, long e)
    {
        long f = a * 3 + b, g = b * 5 + c, i = c * 7 + d, j = d * 11 + e, k = e * 13 + a;
        h.F5 = *p;
        long l = f ^ g, m = i ^ j;
        *q = h.F17;
        h.F3 = default;
        return a + b * 3 + c * 5 + d * 7 + e * 11 + f * 13 + g * 17 + i * 19 + j * 23 + k * 29 + l * 31 + m * 37;
    }

    // Structs with object references: copied one word at a time, with a write barrier on the heap.
    [MethodImpl(MethodImplOptions.NoInlining)] static void GcFieldFromRef3(Holder h, ref G3 s) => h.FG3 = s;
    [MethodImpl(MethodImplOptions.NoInlining)] static void GcFieldFromRef6(Holder h, ref G6 s) => h.FG6 = s;
    [MethodImpl(MethodImplOptions.NoInlining)] static void GcFieldFromRef40(Holder h, ref G40 s) => h.FG40 = s;
    [MethodImpl(MethodImplOptions.NoInlining)] static void GcRefFromField6(ref G6 d, Holder h) => d = h.FG6;
    [MethodImpl(MethodImplOptions.NoInlining)] static void GcRefFromField40(ref G40 d, Holder h) => d = h.FG40;
    [MethodImpl(MethodImplOptions.NoInlining)] static void GcElemFromElem(G6[] a, int i, int j) => a[i] = a[j];
    [MethodImpl(MethodImplOptions.NoInlining)] static void GcStaticFromRef(ref G6 s) => s_g6 = s;
    [MethodImpl(MethodImplOptions.NoInlining)] static void GcZeroField6(Holder h) => h.FG6 = default;
    [MethodImpl(MethodImplOptions.NoInlining)] static void GcZeroField40(Holder h) => h.FG40 = default;
    [MethodImpl(MethodImplOptions.NoInlining)] static void GcZeroRef40(ref G40 d) => d = default;
    [MethodImpl(MethodImplOptions.NoInlining)] static G6 GcReturn6(Holder h) => h.FG6;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long GcLocals(Holder h, long seed)
    {
        G6 a = MakeG6(seed);
        G6 b = a;
        Ref.Touch(&b.L0);
        G40 c = default;
        c.P3 = b;
        c.Last = s_obj;
        c.T1 = seed;
        G40 d = c;
        h.FG40 = d;
        return HashG6(ref b) * 3 + HashG40(ref d) + HashG40(ref h.FG40) * 7;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    static G6 MakeG6(long seed)
    {
        G6 g;
        g.L0 = seed * 3 + 1;
        g.O0 = ((seed & 1) == 0) ? s_obj : (object)s_ints;
        g.L1 = seed * 5 + 2;
        g.S0 = ((seed & 2) == 0) ? "even" : "odd";
        g.A0 = ((seed & 4) == 0) ? s_ints : null;
        g.L2 = seed * 7 + 3;
        return g;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    static long HashG6(ref G6 g)
    {
        long h = g.L0 * 31 + g.L1;
        h = h * 31 + g.L2;
        h = h * 31 + (ReferenceEquals(g.O0, s_obj) ? 1 : ReferenceEquals(g.O0, s_ints) ? 2 : (g.O0 == null) ? 3 : 4);
        h = h * 31 + (ReferenceEquals(g.S0, "even") ? 1 : ReferenceEquals(g.S0, "odd") ? 2 : (g.S0 == null) ? 3 : 4);
        h = h * 31 + (ReferenceEquals(g.A0, s_ints) ? 1 : (g.A0 == null) ? 3 : 4);
        return h;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    static long HashG40(ref G40 g)
    {
        long h = HashG6(ref g.P0);
        h = h * 37 + HashG6(ref g.P1);
        h = h * 37 + HashG6(ref g.P2);
        h = h * 37 + HashG6(ref g.P3);
        h = h * 37 + HashG6(ref g.P4);
        h = h * 37 + HashG6(ref g.P5);
        h = h * 37 + g.T0;
        h = h * 37 + g.T1;
        h = h * 37 + g.T2;
        h = h * 37 + (ReferenceEquals(g.Last, s_obj) ? 1 : (g.Last == null) ? 3 : 4);
        return h;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    static void MakeG40(ref G40 g, long seed)
    {
        g.P0 = MakeG6(seed);
        g.P1 = MakeG6(seed + 1);
        g.P2 = MakeG6(seed + 2);
        g.P3 = MakeG6(seed + 3);
        g.P4 = MakeG6(seed + 4);
        g.P5 = MakeG6(seed + 5);
        g.T0 = seed * 11;
        g.T1 = seed * 13;
        g.T2 = seed * 17;
        g.Last = ((seed & 1) == 0) ? s_obj : null;
    }

    static void Eq(string name, long expected, long got)
    {
        if (expected != got)
        {
            s_groupFails++;
            Console.WriteLine("FAIL " + name + " expected " + expected + " got " + got);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    static long HolderGuards(Holder h) =>
        (h.Before == 0x1111111111111111 ? 0 : 1) + (h.Mid0 == 0x2222222222222222 ? 0 : 2) +
        (h.Mid1 == 0x3333333333333333 ? 0 : 4) + (h.Mid2 == 0x4444444444444444 ? 0 : 8) +
        (h.Mid3 == 0x5555555555555555 ? 0 : 16) + (h.Mid4 == 0x66 ? 0 : 32) +
        (h.After == 0x7777777777777777 ? 0 : 64);

    static void Addresses()
    {
        Holder h = new Holder();
        Holder h2 = new Holder();
        long* buf = stackalloc long[48];
        long* buf2 = stackalloc long[48];
        W5[] arr = new W5[5];

        for (int round = 1; round <= 3; round++)
        {
            FillLongs(buf, 48, round);
            long s3 = SumLongs(buf, 3), s5 = SumLongs(buf, 5), s17 = SumLongs(buf, 17), s40 = SumLongs(buf, 40);

            FieldFromPtr3(h, (W3*)buf);
            fixed (W3* f = &h.F3) Eq("FieldFromPtr3", s3, SumLongs((long*)f, 3));
            FieldFromPtr5(h, (W5*)buf);
            fixed (W5* f = &h.F5) Eq("FieldFromPtr5", s5, SumLongs((long*)f, 5));
            FieldFromPtr17(h, (W17*)buf);
            fixed (W17* f = &h.F17) Eq("FieldFromPtr17", s17, SumLongs((long*)f, 17));
            FieldFromPtr40(h, (W40*)buf);
            fixed (W40* f = &h.F40) Eq("FieldFromPtr40", s40, SumLongs((long*)f, 40));
            Eq("field guards", 0, HolderGuards(h));

            FillLongs(buf2, 48, 77);
            PtrFromField3((W3*)buf2, h);
            Eq("PtrFromField3", s3, SumLongs(buf2, 3));
            Eq("PtrFromField3 end", Pat(77, 3), buf2[3]);
            PtrFromField5((W5*)(buf2 + 8), h);
            Eq("PtrFromField5", s5, SumLongs(buf2 + 8, 5));
            PtrFromField17((W17*)(buf2 + 16), h);
            Eq("PtrFromField17", s17, SumLongs(buf2 + 16, 17));
            Eq("PtrFromField17 end", Pat(77, 33), buf2[33]);
            FillLongs(buf2, 48, 78);
            PtrFromField40((W40*)buf2, h);
            Eq("PtrFromField40", s40, SumLongs(buf2, 40));
            Eq("PtrFromField40 end", Pat(78, 40), buf2[40]);

            FieldFromField5(h2, h);
            fixed (W5* f = &h2.F5) Eq("FieldFromField5", s5, SumLongs((long*)f, 5));
            FieldFromField17(h2, h);
            fixed (W17* f = &h2.F17) Eq("FieldFromField17", s17, SumLongs((long*)f, 17));
            Eq("field guards 2", 0, HolderGuards(h2));

            StaticFromPtr5((W5*)buf);
            FillLongs(buf2, 48, 79);
            PtrFromStatic5((W5*)buf2);
            Eq("Static5", s5, SumLongs(buf2, 5));
            StaticFromPtr17((W17*)buf);
            PtrFromStatic17((W17*)(buf2 + 8));
            Eq("Static17", s17, SumLongs(buf2 + 8, 17));
            Eq("Static17 end", Pat(79, 25), buf2[25]);

            for (int i = 0; i < 5; i++)
            {
                ElemFromPtr5(arr, i, (W5*)(buf + i));
                FillLongs(buf2, 8, 80);
                PtrFromElem5((W5*)buf2, arr, i);
                Eq("Elem5", SumLongs(buf + i, 5), SumLongs(buf2, 5));
                Eq("Elem5 end", Pat(80, 5), buf2[5]);
            }
            ElemFromElem5(arr, 0, 4);
            fixed (W5* f = &arr[0]) Eq("ElemFromElem5", SumLongs(buf + 4, 5), SumLongs((long*)f, 5));
            fixed (W5* f = &arr[1]) Eq("ElemFromElem5 next", SumLongs(buf + 1, 5), SumLongs((long*)f, 5));
            ConstElemFromPtr5(arr, (W5*)(buf + 20));
            fixed (W5* f = &arr[2]) Eq("ConstElemFromPtr5", SumLongs(buf + 20, 5), SumLongs((long*)f, 5));
            fixed (W5* f = &arr[3]) Eq("ConstElemFromPtr5 next", SumLongs(buf + 3, 5), SumLongs((long*)f, 5));

            FillLongs(buf2, 48, 81);
            RefFromRef5(ref *(W5*)buf2, ref h.F5);
            Eq("RefFromRef5", s5, SumLongs(buf2, 5));
            RefFromIn17(ref h2.F17, in *(W17*)(buf + 9));
            fixed (W17* f = &h2.F17) Eq("RefFromIn17", SumLongs(buf + 9, 17), SumLongs((long*)f, 17));
            OutFromPtr5(out arr[4], (W5*)(buf + 30));
            fixed (W5* f = &arr[4]) Eq("OutFromPtr5", SumLongs(buf + 30, 5), SumLongs((long*)f, 5));

            W5 r5 = Return5((W5*)(buf + 2));
            Eq("Return5", SumLongs(buf + 2, 5), SumLongs((long*)&r5, 5));
            W17 r17 = Return17(h);
            Eq("Return17", s17, SumLongs((long*)&r17, 17));
            Eq("ByValue5", buf[2] + buf[3] * 3 + buf[4] * 5 + buf[5] * 7 + buf[6] * 11 + round, ByValue5(r5, round));
            Eq("ByValue17", buf[0] + buf[8] * 3 + buf[16] * 5 + round * 7 + 99 * 11, ByValue17(round, r17, 99));
            Eq("ByValue17 field", buf[0] + buf[8] * 3 + buf[16] * 5 + 7 + 22, ByValue17(1, h.F17, 2));

            long a = round, b = round * 3 + 1, c = round * 5 + 2, d = round * 7 + 3, e = round * 9 + 4;
            long f1 = a * 3 + b, g1 = b * 5 + c, i1 = c * 7 + d, j1 = d * 11 + e, k1 = e * 13 + a;
            long live = a + b * 3 + c * 5 + d * 7 + e * 11 + f1 * 13 + g1 * 17 + i1 * 19 + j1 * 23 + k1 * 29 +
                        (f1 ^ g1) * 31 + (i1 ^ j1) * 37;
            FillLongs(buf2, 48, 82);
            Eq("LiveAcross", live, LiveAcross(h, (W5*)(buf + 11), (W17*)buf2, a, b, c, d, e));
            fixed (W5* f = &h.F5) Eq("LiveAcross F5", SumLongs(buf + 11, 5), SumLongs((long*)f, 5));
            Eq("LiveAcross F17", s17, SumLongs(buf2, 17));
            Eq("LiveAcross F3", 0, h.F3.A | h.F3.B | h.F3.C);

            Odd21* odd = (Odd21*)((byte*)buf + 3);
            FieldOddFromPtr(h, odd);
            fixed (Odd21* f = &h.FOdd) Eq("FieldOddFromPtr", -1, Ref.Diff((byte*)f, (byte*)odd, 21));
            Ref.Set((byte*)buf2, 0xEE, 64);
            PtrFromFieldOdd((Odd21*)((byte*)buf2 + 5), h);
            Eq("PtrFromFieldOdd", -1, Ref.Diff((byte*)buf2 + 5, (byte*)odd, 21));
            Eq("PtrFromFieldOdd guards", 0xEE + 0xEE, ((byte*)buf2)[4] + ((byte*)buf2)[26]);
            StaticOddFromPtr(odd);
            fixed (Odd21* f = &s_odd) Eq("StaticOddFromPtr", -1, Ref.Diff((byte*)f, (byte*)odd, 21));
            Eq("field guards 3", 0, HolderGuards(h));

            FieldFromPtr3(h, (W3*)buf);
            FieldFromPtr5(h, (W5*)buf);
            ZeroField3(h);
            Eq("ZeroField3", 0, h.F3.A | h.F3.B | h.F3.C);
            ZeroField5(h);
            Eq("ZeroField5", 0, h.F5.A | h.F5.B | h.F5.C | h.F5.D | h.F5.E);
            ZeroField17(h);
            fixed (W17* f = &h.F17) Eq("ZeroField17", 0, SumLongs((long*)f, 17));
            ZeroField40(h);
            fixed (W40* f = &h.F40) Eq("ZeroField40", 0, SumLongs((long*)f, 40));
            ZeroFieldOdd(h);
            Eq("ZeroFieldOdd", 0, h.FOdd.B | h.FOdd.L0 | (long)h.FOdd.I | h.FOdd.L1);
            Eq("field guards 4", 0, HolderGuards(h));
            ZeroStatic17();
            fixed (W17* f = &s_w17) Eq("ZeroStatic17", 0, SumLongs((long*)f, 17));
            s_w40.P7.E = 5;
            ZeroStatic40();
            fixed (W40* f = &s_w40) Eq("ZeroStatic40", 0, SumLongs((long*)f, 40));
            ZeroElem5(arr, 1);
            fixed (W5* f = &arr[1]) Eq("ZeroElem5", 0, SumLongs((long*)f, 5));
            fixed (W5* f = &arr[2]) Eq("ZeroElem5 next", SumLongs(buf + 20, 5), SumLongs((long*)f, 5));
            fixed (W5* f = &arr[0]) Eq("ZeroElem5 previous", SumLongs(buf + 4, 5), SumLongs((long*)f, 5));
            FillLongs(buf2, 48, 83);
            ZeroRef17(ref *(W17*)(buf2 + 1));
            Eq("ZeroRef17", 0, SumLongs(buf2 + 1, 17));
            Eq("ZeroRef17 guards", Pat(83, 0) + Pat(83, 18),
               buf2[0] + buf2[18]);
            ZeroOut40(out *(W40*)(buf2 + 2));
            Eq("ZeroOut40", 0, SumLongs(buf2 + 2, 40));
            Eq("ZeroOut40 guards", Pat(83, 0) + Pat(83, 42),
               buf2[0] + buf2[42]);
        }
        EndGroup("copies and fills through every kind of address");

        G6[] garr = new G6[4];
        for (int round = 1; round <= 9; round++)
        {
            G3 g3;
            g3.O = s_obj;
            g3.L = round * 1000003;
            g3.S = "odd";
            GcFieldFromRef3(h, ref g3);
            Eq("GcFieldFromRef3", round * 1000003 + 3,
               h.FG3.L + (ReferenceEquals(h.FG3.O, s_obj) ? 1 : 0) + (ReferenceEquals(h.FG3.S, "odd") ? 2 : 0));

            G6 g6 = MakeG6(round);
            long want6 = HashG6(ref g6);
            GcFieldFromRef6(h, ref g6);
            Eq("GcFieldFromRef6", want6, HashG6(ref h.FG6));
            G6 back = default;
            GcRefFromField6(ref back, h);
            Eq("GcRefFromField6", want6, HashG6(ref back));
            garr[3] = g6;
            GcElemFromElem(garr, round & 1, 3);
            Eq("GcElemFromElem", want6, HashG6(ref garr[round & 1]));
            GcStaticFromRef(ref garr[round & 1]);
            Eq("GcStaticFromRef", want6, HashG6(ref s_g6));
            G6 ret = GcReturn6(h);
            Eq("GcReturn6", want6, HashG6(ref ret));

            G40 g40 = default;
            MakeG40(ref g40, round);
            long want40 = HashG40(ref g40);
            GcFieldFromRef40(h, ref g40);
            Eq("GcFieldFromRef40", want40, HashG40(ref h.FG40));
            G40 back40 = default;
            GcRefFromField40(ref back40, h);
            Eq("GcRefFromField40", want40, HashG40(ref back40));
            Eq("gc field guards", 0, HolderGuards(h));

            G40 zero = default;
            long wantZero = HashG40(ref zero);
            GcZeroRef40(ref back40);
            Eq("GcZeroRef40", wantZero, HashG40(ref back40));
            GcZeroField40(h);
            Eq("GcZeroField40", wantZero, HashG40(ref h.FG40));
            G6 zero6 = default;
            GcZeroField6(h);
            Eq("GcZeroField6", HashG6(ref zero6), HashG6(ref h.FG6));
            Eq("gc field guards 2", 0, HolderGuards(h));

            G6 l6 = MakeG6(round);
            G40 l40 = default;
            l40.P3 = l6;
            l40.Last = s_obj;
            l40.T1 = round;
            long l40Hash = HashG40(ref l40);
            Eq("GcLocals", HashG6(ref l6) * 3 + l40Hash + l40Hash * 7, GcLocals(h, round));
        }
        EndGroup("structs with object references");
    }

    static int Main()
    {
        byte* mem = stackalloc byte[4 * 1100];
        s_src = mem + Guard;
        s_dst = mem + 1100 + Guard;
        s_exp = mem + 2200 + Guard;
        s_tmp = mem + 3300 + Guard;

        Run<Z1>(); Run<Z2>(); Run<Z3>(); Run<Z4>(); Run<Z5>(); Run<Z6>(); Run<Z7>(); Run<Z8>();
        Run<Z9>(); Run<Z10>(); Run<Z11>(); Run<Z12>(); Run<Z13>(); Run<Z14>(); Run<Z15>();
        Run<Z16>(); Run<Z17>(); Run<Z18>(); Run<Z19>(); Run<Z20>(); Run<Z21>(); Run<Z22>();
        Run<Z23>(); Run<Z24>(); Run<Z25>(); Run<Z26>(); Run<Z27>(); Run<Z28>(); Run<Z29>();
        Run<Z30>(); Run<Z31>(); Run<Z32>(); Run<Z33>(); Run<Z34>(); Run<Z35>(); Run<Z36>();
        Run<Z37>(); Run<Z38>(); Run<Z39>(); Run<Z40>(); Run<Z41>(); Run<Z42>(); Run<Z43>();
        Run<Z44>(); Run<Z45>(); Run<Z46>(); Run<Z47>(); Run<Z48>(); Run<Z49>(); Run<Z50>();
        Run<Z51>(); Run<Z52>(); Run<Z53>(); Run<Z54>(); Run<Z55>(); Run<Z56>(); Run<Z57>();
        Run<Z58>(); Run<Z59>(); Run<Z60>(); Run<Z61>(); Run<Z62>(); Run<Z63>(); Run<Z64>();
        Run<Z65>(); Run<Z66>(); Run<Z71>(); Run<Z72>(); Run<Z73>(); Run<Z79>(); Run<Z80>();
        Run<Z95>(); Run<Z96>(); Run<Z97>(); Run<Z119>(); Run<Z120>(); Run<Z121>(); Run<Z127>();
        Run<Z128>(); Run<Z129>(); Run<Z130>(); Run<Z136>(); Run<Z160>(); Run<Z191>(); Run<Z192>();
        Run<Z193>(); Run<Z248>(); Run<Z255>(); Run<Z256>(); Run<Z257>(); Run<Z264>(); Run<Z300>();
        EndGroup("every operation at every size and distance from an 8-byte boundary");

        Addresses();

        Console.WriteLine(s_fails == 0 ? "ALL PASS" : "FAILED " + s_fails);
        return s_fails == 0 ? 0 : 1;
    }
}
