// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Strict alignment: loads and stores the program does not mark as unaligned.
//
// With JitRiscV64StrictAlign the JIT may emit a wide load or store only
// where the address is a multiple of the width. It proves that for what it
// can (an object reference plus an aligned offset, a stack local, stackalloc
// space, a local whose every value is aligned, arithmetic that keeps low
// zero bits: add, subtract, or, and, multiply, shift left, casts); every
// other access tests its address and takes a byte-wise path when the test
// fails. Runs of accesses through one base in a block, in a group of blocks
// and in loops share one test and get a second, tested copy of the code.
// What can go wrong: a proof that claims more than is true (a mask that
// aligns to four bytes taken for eight, a step of four, an index scaled by
// twelve, a value that only sometimes is aligned, a local with a second
// definition, a span that was reassigned), a shared test that no longer
// holds because the base moved or because only one of two bases was tested,
// a copy of the code that computes something else than the original (stores
// that alias loads, values carried from one copy into the rest of the
// method), a byte-wise path with the wrong byte order, extension or offset,
// and a struct taken apart into fields that are loaded wide.
//
// Every method runs with its pointer at all eight distances from an 8-byte
// boundary, so each test passes in one run and fails in others. Where a
// proof would be wrong the inputs make the address misaligned. Accesses
// through pointers, references, spans, `in` structs, fields of packed
// structs in objects, arrays, statics and locals, and structs passed and
// returned by value are covered.
//
// The expected values are put together from single bytes by methods compiled
// without optimization. A run under an emulator that accepts misaligned
// accesses checks the values (both paths of every test) only; that no wide
// access is misaligned needs a target that rejects them or a trace of the
// accesses. Each group prints one line; the run ends with ALL PASS or a FAIL
// count.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

struct T3 { public byte A, B, C; }
struct T6 { public short A, B, C; }
struct T8 { public int A, B; }
struct T12 { public int A, B, C; }
struct T16 { public long A, B; }
struct T24 { public long A, B, C; }
struct Limbs { public ulong U0, U1, U2, U3; }

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct Packed { public byte B0; public long L; public short S; public int I; public byte B1; public ulong UL; }

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct Packed13 { public byte B; public long L; public int I; }

[StructLayout(LayoutKind.Sequential, Pack = 4)]
struct Pack4 { public int I; public long L; }

[StructLayout(LayoutKind.Explicit)]
class OddFields
{
    [FieldOffset(0)] public byte B;
    [FieldOffset(1)] public long L1;
    [FieldOffset(9)] public int I9;
    [FieldOffset(14)] public short S14;
    [FieldOffset(16)] public long L16;
    [FieldOffset(28)] public long L28;
    [FieldOffset(36)] public int I36;
}

class Holder { public long Before; public Packed P; public Pack4 Q; public long After; }

ref struct TwoSpans { public Span<byte> First; public Span<byte> Second; }

static unsafe class Ref
{
    const MethodImplOptions Plain = MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization;

    [MethodImpl(Plain)]
    public static ulong Get(byte* p, int size)
    {
        ulong v = 0;
        for (int i = size - 1; i >= 0; i--)
            v = (v << 8) | p[i];
        return v;
    }

    [MethodImpl(Plain)]
    public static long GetSigned(byte* p, int size)
    {
        ulong v = Get(p, size);
        int shift = 64 - 8 * size;
        return (long)(v << shift) >> shift;
    }

    [MethodImpl(Plain)]
    public static void Put(byte* p, int size, ulong v)
    {
        for (int i = 0; i < size; i++)
        {
            p[i] = (byte)v;
            v >>= 8;
        }
    }

    [MethodImpl(Plain)]
    public static void Fill(byte* p, int n, int seed)
    {
        for (int i = 0; i < n; i++)
            p[i] = (byte)(seed * 37 + i * 11 + (i >> 3) + 0x81);
    }

    [MethodImpl(Plain)]
    public static void Copy(byte* d, byte* s, int n)
    {
        for (int i = 0; i < n; i++)
            d[i] = s[i];
    }

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

    [MethodImpl(Plain)] public static byte* Opaque(byte* p) => p;
    [MethodImpl(Plain)] public static void Touch(void* p) { }
}

// One instantiation per type.
static unsafe class Acc<T> where T : unmanaged
{
    [MethodImpl(MethodImplOptions.NoInlining)] public static T Read(byte* p) => *(T*)p;
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Write(byte* p, T v) => *(T*)p = v;
    [MethodImpl(MethodImplOptions.NoInlining)] public static T ReadAs(ref byte p) => Unsafe.As<byte, T>(ref p);
    [MethodImpl(MethodImplOptions.NoInlining)] public static void WriteAs(ref byte p, T v) => Unsafe.As<byte, T>(ref p) = v;
    [MethodImpl(MethodImplOptions.NoInlining)] public static T ReadRef(ref T r) => r;
    [MethodImpl(MethodImplOptions.NoInlining)] public static void WriteRef(ref T r, T v) => r = v;
    [MethodImpl(MethodImplOptions.NoInlining)] public static T ReadIn(in T r) => r;
    [MethodImpl(MethodImplOptions.NoInlining)] public static void WriteOut(out T r, T v) => r = v;
    [MethodImpl(MethodImplOptions.NoInlining)] public static T ReadIndexed(byte* p, nint i) => ((T*)p)[i];
    [MethodImpl(MethodImplOptions.NoInlining)] public static void WriteIndexed(byte* p, int i, T v) => ((T*)p)[i] = v;
    [MethodImpl(MethodImplOptions.NoInlining)] public static T ReadCast(Span<byte> s, int i) => MemoryMarshal.Cast<byte, T>(s)[i];
    [MethodImpl(MethodImplOptions.NoInlining)] public static void WriteCast(Span<byte> s, int i, T v) => MemoryMarshal.Cast<byte, T>(s)[i] = v;
    [MethodImpl(MethodImplOptions.NoInlining)] public static T ReadSpan(Span<T> s, int i) => s[i];
    [MethodImpl(MethodImplOptions.NoInlining)] public static T Read8(byte* p) => *(T*)(p + 8);
    [MethodImpl(MethodImplOptions.NoInlining)] public static T Read3(byte* p) => *(T*)(p + 3);
    [MethodImpl(MethodImplOptions.NoInlining)] public static T ReadBack16(byte* p) => *(T*)(p - 16);
    [MethodImpl(MethodImplOptions.NoInlining)] public static T Read2040(byte* p) => *(T*)(p + 2040);
    [MethodImpl(MethodImplOptions.NoInlining)] public static T Read2044(byte* p) => *(T*)(p + 2044);
    [MethodImpl(MethodImplOptions.NoInlining)] public static T Read2046(byte* p) => *(T*)(p + 2046);
    [MethodImpl(MethodImplOptions.NoInlining)] public static T Read2048(byte* p) => *(T*)(p + 2048);
    [MethodImpl(MethodImplOptions.NoInlining)] public static T ReadBack2048(byte* p) => *(T*)(p - 2048);
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Write8(byte* p, T v) => *(T*)(p + 8) = v;
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Write3(byte* p, T v) => *(T*)(p + 3) = v;
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Write2040(byte* p, T v) => *(T*)(p + 2040) = v;
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Write2044(byte* p, T v) => *(T*)(p + 2044) = v;
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Write2048(byte* p, T v) => *(T*)(p + 2048) = v;
    [MethodImpl(MethodImplOptions.NoInlining)] public static void WriteBack2048(byte* p, T v) => *(T*)(p - 2048) = v;
    [MethodImpl(MethodImplOptions.NoInlining)] public static void WriteDefault(byte* p) => *(T*)p = default;
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Move(byte* d, byte* s) => *(T*)d = *(T*)s;
    [MethodImpl(MethodImplOptions.NoInlining)] public static T Pass(byte* p) => Take(*(T*)p, 5);
    [MethodImpl(MethodImplOptions.NoInlining)] static T Take(T v, long k) => v;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Swap(byte* a, byte* b)
    {
        T x = *(T*)a;
        T y = *(T*)b;
        *(T*)a = y;
        *(T*)b = x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ViaLocal(byte* d, byte* s)
    {
        T t = *(T*)s;
        Ref.Touch(&t);
        *(T*)d = t;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long Live(byte* p, byte* q, long a, long b, long c, long d)
    {
        long e = a * 3 + b, f = b * 5 + c, g = c * 7 + d, h = d * 11 + a;
        T v = *(T*)p;
        long i = e ^ f, j = g ^ h;
        *(T*)q = v;
        return a + b * 3 + c * 5 + d * 7 + e * 11 + f * 13 + g * 17 + h * 19 + i * 23 + j * 29;
    }
}

static unsafe class Ops
{
    // ---- extension ----

    [MethodImpl(MethodImplOptions.NoInlining)] public static long I16(byte* p) => *(short*)p;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long U16(byte* p) => *(ushort*)p;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long I32(byte* p) => *(int*)p;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long U32(byte* p) => *(uint*)p;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long I32Div(byte* p) => *(int*)p / 3;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long I32Compare(byte* p, long x) => (*(int*)p == x) ? 1 : 2;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long I32Add(byte* p, int x) => *(int*)p + x;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long I32Index(byte* p, long[] t) => t[*(int*)p & 15];
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Increment(byte* p) => (*(long*)p)++;
    [MethodImpl(MethodImplOptions.NoInlining)] public static void AddInt(byte* p, int k) => *(int*)p += k;
    [MethodImpl(MethodImplOptions.NoInlining)] public static void StoreSelf(byte* p) => *(long*)p = (long)p;
    [MethodImpl(MethodImplOptions.NoInlining)] public static void StoreZero(byte* p) => *(long*)p = 0;
    [MethodImpl(MethodImplOptions.NoInlining)] public static void StoreZeroInt(byte* p) => *(int*)p = 0;
    [MethodImpl(MethodImplOptions.NoInlining)] public static void StoreConst(byte* p) => *(long*)p = 0x1122334455667788;
    [MethodImpl(MethodImplOptions.NoInlining)] public static byte* Follow(byte* p) => *(byte**)p;

    // ---- arithmetic that does or does not keep an address aligned ----
    // Each reads eight bytes at `Where(...)`; the caller computes the same address.

    [MethodImpl(MethodImplOptions.NoInlining)] public static long Down8(byte* p) => *(long*)((nuint)p & ~7UL);
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Down4(byte* p) => *(long*)((nuint)p & ~3UL);
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Down16Plus8(byte* p) => *(long*)(((nuint)p & ~15UL) + 8);
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Down8Plus4(byte* p) => *(long*)(((nuint)p & ~7UL) + 4);
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Up8(byte* p) => *(long*)(((nuint)p + 7) & ~7UL);
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Down8Or(byte* p, nuint bit) => *(long*)(((nuint)p & ~7UL) | bit);
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Down8Or4(byte* p) => *(long*)(((nuint)p & ~7UL) | 4);
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Down8Xor(byte* p, nuint bit) => *(long*)(((nuint)p & ~7UL) ^ bit);
    [MethodImpl(MethodImplOptions.NoInlining)] public static long MaskVar(byte* p, nuint mask) => *(long*)((nuint)p & mask);
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Times8(byte* p, int i) => *(long*)(((nuint)p & ~7UL) + (nuint)(i * 8));
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Times4(byte* p, int i) => *(long*)(((nuint)p & ~7UL) + (nuint)(i * 4));
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Times12(byte* p, int i) => *(long*)(((nuint)p & ~7UL) + (nuint)(i * 12));
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Times24(byte* p, int i) => *(long*)(((nuint)p & ~7UL) + (nuint)(i * 24));
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Shift3(byte* p, long i) => *(long*)(((nuint)p & ~7UL) + (nuint)(i << 3));
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Shift2(byte* p, long i) => *(long*)(((nuint)p & ~7UL) + (nuint)(i << 2));
    [MethodImpl(MethodImplOptions.NoInlining)] public static long ShiftVar(byte* p, long i, int n) => *(long*)(((nuint)p & ~7UL) + (nuint)(i << n));
    [MethodImpl(MethodImplOptions.NoInlining)] public static long ShiftInt35(byte* p, int i) => *(long*)(((nuint)p & ~7UL) + (nuint)(i << 35));
    [MethodImpl(MethodImplOptions.NoInlining)] public static long ShiftBack(byte* p, long i) => *(long*)(((nuint)p & ~7UL) + (nuint)((i << 4) >> 2));
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Product(byte* p, int i, int j) => *(long*)(((nuint)p & ~7UL) + (nuint)((i * 2) * (j * 4)));
    [MethodImpl(MethodImplOptions.NoInlining)] public static long ProductOdd(byte* p, int i, int j) => *(long*)(((nuint)p & ~7UL) + (nuint)((i * 2) * (j * 2)));
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Difference(byte* p, int i, int j) => *(long*)(((nuint)p & ~7UL) + (nuint)(i * 16 - j * 8));
    [MethodImpl(MethodImplOptions.NoInlining)] public static long DifferenceOdd(byte* p, int i, int j) => *(long*)(((nuint)p & ~7UL) + (nuint)(i * 16 - j * 4));
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Negated(byte* p, int i) => *(long*)(((nuint)p & ~7UL) - (nuint)(-(i * 8)));
    [MethodImpl(MethodImplOptions.NoInlining)] public static long MaskedIndex(byte* p, int i) => *(long*)(((nuint)p & ~7UL) + (nuint)(i & 0x78));
    [MethodImpl(MethodImplOptions.NoInlining)] public static long MaskedIndexOdd(byte* p, int i) => *(long*)(((nuint)p & ~7UL) + (nuint)(i & 0x7C));
    [MethodImpl(MethodImplOptions.NoInlining)] public static long ByteIndex(byte* p, int i) => *(long*)(((nuint)p & ~7UL) + (byte)(i * 8));
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Divided(byte* p, int i) => *(long*)(((nuint)p & ~7UL) + (nuint)(i * 16 / 2));
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Remainder(byte* p, int i) => *(long*)(((nuint)p & ~7UL) + (nuint)(i * 8 % 20));
    [MethodImpl(MethodImplOptions.NoInlining)] public static long PointerDifference(byte* p, byte* q) => *(long*)(((nuint)p & ~7UL) + (nuint)(q - p));
    [MethodImpl(MethodImplOptions.NoInlining)] public static int Down8Int(byte* p) => *(int*)((nuint)p & ~7UL);
    [MethodImpl(MethodImplOptions.NoInlining)] public static int Down2Int(byte* p) => *(int*)((nuint)p & ~1UL);
    [MethodImpl(MethodImplOptions.NoInlining)] public static int Down4Plus2Short(byte* p) => *(short*)(((nuint)p & ~3UL) + 2);
    [MethodImpl(MethodImplOptions.NoInlining)] public static int Down4Plus1Short(byte* p) => *(short*)(((nuint)p & ~3UL) + 1);

    // ---- locals: every value stored to the local decides ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long LocalStep(byte* p, int count, int step)
    {
        byte* q = (byte*)((nuint)p & ~7UL);
        long sum = 0;
        for (int i = 0; i < count; i++)
        {
            sum = sum * 3 + *(long*)q;
            q += step;
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long LocalStep8(byte* p, int count)
    {
        byte* q = (byte*)((nuint)p & ~7UL);
        long sum = 0;
        for (int i = 0; i < count; i++)
        {
            sum = sum * 3 + *(long*)q;
            q += 8;
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long LocalStep4(byte* p, int count)
    {
        byte* q = (byte*)((nuint)p & ~7UL);
        long sum = 0;
        for (int i = 0; i < count; i++)
        {
            sum = sum * 3 + *(long*)q + *(int*)q;
            q += 4;
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long LocalEither(byte* p, bool odd)
    {
        byte* q = (byte*)((nuint)p & ~7UL);
        if (odd)
            q = p;
        return *(long*)q + *(long*)(q + 8);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long LocalLate(byte* p, int k)
    {
        byte* q = (byte*)((nuint)p & ~7UL);
        long sum = *(long*)q;
        for (int i = 0; i < 3; i++)
        {
            sum += *(long*)(q + 16);
            if (i == k)
                q += 3;
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long LocalFromCall(byte* p) { byte* q = Ref.Opaque(p); return *(long*)q + *(long*)(q + 8); }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long LocalFromMemory(byte** pp) { byte* q = *pp; return *(long*)q + *(int*)(q + 8); }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long IndexLocal(byte* p, int count, int scale)
    {
        byte* q = (byte*)((nuint)p & ~7UL);
        long sum = 0;
        int index = 0;
        for (int i = 0; i < count; i++)
        {
            sum = sum * 3 + *(long*)(q + index);
            index += scale;
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long IndexLocal8(byte* p, int count)
    {
        byte* q = (byte*)((nuint)p & ~7UL);
        long sum = 0;
        for (int index = 0; index < count * 8; index += 8)
            sum = sum * 3 + *(long*)(q + index);
        return sum;
    }

    // ---- stackalloc ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long StackAlloc(int at, long v)
    {
        byte* b = stackalloc byte[96];
        for (int i = 0; i < 96; i++)
            b[i] = (byte)(i * 7 + 1);
        *(long*)(b + 16) = v;
        *(long*)(b + at) = ~v;
        *(int*)(b + 44) = (int)v;
        *(long*)(b + 49) = v * 3;
        long sum = 0;
        for (int i = 0; i < 96; i++)
            sum = sum * 31 + b[i];
        return sum + *(long*)(b + 8) + *(long*)(b + at + 3) + *(short*)(b + 81);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long StackSpan(int at, long v)
    {
        Span<byte> s = stackalloc byte[64];
        for (int i = 0; i < 64; i++)
            s[i] = (byte)(i * 5 + 3);
        Unsafe.As<byte, long>(ref s[8]) = v;
        Unsafe.As<byte, long>(ref s[at]) = ~v;
        Span<long> longs = MemoryMarshal.Cast<byte, long>(s.Slice(at));
        long sum = longs[1];
        for (int i = 0; i < 64; i++)
            sum = sum * 31 + s[i];
        return sum + Unsafe.As<byte, int>(ref s[at + 1]) + Unsafe.As<byte, long>(ref s[16]);
    }

    // ---- spans over arrays ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long SpanAt8(byte[] a) { Span<byte> s = a.AsSpan(8, 32); return Unsafe.As<byte, long>(ref s[0]) + Unsafe.As<byte, long>(ref s[8]); }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long SpanAt3(byte[] a) { Span<byte> s = a.AsSpan(3, 32); return Unsafe.As<byte, long>(ref s[0]) + Unsafe.As<byte, long>(ref s[8]); }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long SpanAt(byte[] a, int at) { Span<byte> s = a.AsSpan(at, 32); return Unsafe.As<byte, long>(ref s[0]) + Unsafe.As<byte, long>(ref s[8]); }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long SpanAt8Index(byte[] a, int i) { Span<byte> s = a.AsSpan(8); return Unsafe.As<byte, long>(ref s[i]) + Unsafe.As<byte, int>(ref s[i + 8]); }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long SpanTwice(byte[] a, int at, bool again)
    {
        Span<byte> s = a.AsSpan(8, 40);
        long sum = Unsafe.As<byte, long>(ref s[0]);
        if (again)
            s = a.AsSpan(at, 40);
        return sum + Unsafe.As<byte, long>(ref s[0]) + Unsafe.As<byte, long>(ref s[16]);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long SpanSliced(byte[] a, int step, int count)
    {
        Span<byte> s = a.AsSpan(8);
        long sum = 0;
        for (int i = 0; i < count; i++)
        {
            sum = sum * 3 + Unsafe.As<byte, long>(ref s[0]) + Unsafe.As<byte, int>(ref s[8]);
            s = s.Slice(step);
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long SpanSliced8(byte[] a, int count)
    {
        Span<byte> s = a.AsSpan(8);
        long sum = 0;
        for (int i = 0; i < count; i++)
        {
            sum = sum * 3 + Unsafe.As<byte, long>(ref s[0]);
            s = s.Slice(8);
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)] static void Replace(ref Span<byte> s, byte[] a, int at) => s = a.AsSpan(at);
    [MethodImpl(MethodImplOptions.NoInlining)] static Span<byte> Make(byte[] a, int at) => a.AsSpan(at);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long SpanReplaced(byte[] a, int at)
    {
        Span<byte> s = a.AsSpan(8);
        long sum = Unsafe.As<byte, long>(ref s[0]);
        Replace(ref s, a, at);
        return sum + Unsafe.As<byte, long>(ref s[0]) + Unsafe.As<byte, long>(ref s[8]);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long SpanReturned(byte[] a, int at) { Span<byte> s = Make(a, at); return Unsafe.As<byte, long>(ref s[0]) + Unsafe.As<byte, long>(ref s[8]); }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long SpanPair(byte[] a, int at)
    {
        TwoSpans pair;
        pair.First = a.AsSpan(8);
        pair.Second = a.AsSpan(at);
        TwoSpans copy = pair;
        copy.First = copy.Second;
        return Unsafe.As<byte, long>(ref pair.First[0]) + Unsafe.As<byte, long>(ref copy.First[0]) * 3 + Unsafe.As<byte, long>(ref copy.Second[8]) * 5;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long LongsOverBytes(byte[] a, int at, int i) { Span<long> s = MemoryMarshal.Cast<byte, long>(a.AsSpan(at)); return s[i] + s[i + 1]; }

    // ---- runs and regions that share one test ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long Run(byte* p) => *(long*)p + *(long*)(p + 8) * 3 + *(long*)(p + 4) * 5 + *(long*)(p + 16) * 7 + *(int*)(p + 2) * 11L + *(short*)(p + 24) * 13L;

    // A store through one base between loads through another: the two may be the same memory.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long Aliased(byte* p, byte* q, long v)
    {
        long a = *(long*)p;
        *(long*)q = v;
        long b = *(long*)p;
        *(long*)(q + 8) = a;
        long c = *(long*)(p + 8);
        *(int*)(p + 4) = (int)b;
        return a + b * 3 + c * 5 + *(long*)q * 7 + *(long*)(q + 8) * 11;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long MovedInBlock(byte* p, int step)
    {
        long a = *(long*)p + *(long*)(p + 8);
        p += step;
        return a + (*(long*)p + *(long*)(p + 8)) * 3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long TwoBases(byte* p, byte* q, int exit)
    {
        long sum = *(long*)p;
        if (exit == 0)
            return sum;
        sum += *(long*)q * 3 + *(long*)(p + 8);
        if (exit == 1)
            return sum;
        sum += *(long*)(q + 8) * 5;
        if (exit == 2)
            return sum + *(int*)(p + 16);
        return sum + *(long*)(q + 16) * 7 + *(long*)(p + 24);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long MovedBetweenBlocks(byte* p, byte* q, int step, int exit)
    {
        long sum = *(long*)p + *(long*)q;
        if (exit == 0)
            return sum;
        q += step;
        sum += *(long*)(p + 8) * 3 + *(long*)q * 5;
        if (exit == 1)
            return sum;
        p += step;
        return sum + *(long*)p * 7 + *(long*)(q + 8) * 11 + *(long*)(p + 8) * 13;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Compare(in Limbs a, in Limbs b)
    {
        if (a.U3 != b.U3) return (a.U3 < b.U3) ? -1 : 1;
        if (a.U2 != b.U2) return (a.U2 < b.U2) ? -1 : 1;
        if (a.U1 != b.U1) return (a.U1 < b.U1) ? -1 : 1;
        if (a.U0 != b.U0) return (a.U0 < b.U0) ? -1 : 1;
        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ulong AddWithCarry(in Limbs a, in Limbs b, out Limbs r)
    {
        ulong s0 = a.U0 + b.U0;
        ulong c = (s0 < a.U0) ? 1UL : 0;
        ulong s1 = a.U1 + b.U1;
        ulong c1 = (s1 < a.U1) ? 1UL : 0;
        s1 += c;
        if (s1 < c) c1 = 1;
        ulong s2 = a.U2 + b.U2;
        ulong c2 = (s2 < a.U2) ? 1UL : 0;
        s2 += c1;
        if (s2 < c1) c2 = 1;
        ulong s3 = a.U3 + b.U3;
        ulong c3 = (s3 < a.U3) ? 1UL : 0;
        s3 += c2;
        if (s3 < c2) c3 = 1;
        r.U0 = s0;
        r.U1 = s1;
        r.U2 = s2;
        r.U3 = s3;
        return c3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long LoopRegion(byte* p, byte* q, int count, int moveAt)
    {
        long sum = *(long*)p;
        for (int i = 0; i < count; i++)
        {
            sum = sum * 3 + *(long*)(p + 8) + *(long*)q;
            if (i == moveAt)
                q += 1;
            *(long*)(p + 16) = sum;
        }
        return sum + *(long*)(p + 24) + *(long*)(q + 8);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long NestedLoops(byte* p, int outer, int inner)
    {
        long sum = 0;
        for (int i = 0; i < outer; i++)
        {
            sum += *(long*)p;
            for (int j = 0; j < inner; j++)
            {
                sum = sum * 5 + *(long*)(p + 8) + *(int*)(p + 16);
                if (sum == 12345)
                    return *(long*)(p + 24);
            }
            sum ^= *(long*)(p + 32);
        }
        return sum + *(short*)(p + 40);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long CallInside(byte* p, int k)
    {
        long sum = *(long*)p + *(long*)(p + 8);
        byte* q = Ref.Opaque(p + k);
        sum += *(long*)q * 3 + *(long*)(p + 16);
        return sum + *(long*)(q + 8) * 5;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void StoreRun(byte* p, long v, int exit)
    {
        *(long*)p = v;
        *(int*)(p + 8) = (int)(v * 3);
        if (exit == 0)
            return;
        *(long*)(p + 16) = v * 5;
        *(short*)(p + 12) = (short)(v * 7);
        if (exit == 1)
            return;
        *(long*)(p + 24) = *(long*)p + *(long*)(p + 16);
    }

    // ---- fields at odd offsets: objects, arrays, statics, locals, parameters ----

    public static Packed s_packed;
    public static Packed13 s_packed13;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long OddSum(OddFields o) => o.L1 + o.I9 * 3L + o.S14 * 5L + o.L16 * 7 + o.L28 * 11 + o.I36 * 13L;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void OddSet(OddFields o, long v)
    {
        o.L1 = v;
        o.I9 = (int)(v * 3);
        o.S14 = (short)(v * 5);
        o.L16 = v * 7;
        o.L28 = v * 11;
        o.I36 = (int)(v * 13);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long HolderSum(Holder h) => h.P.L + h.P.S * 3L + h.P.I * 5L + (long)h.P.UL * 7 + h.Q.I * 11L + h.Q.L * 13;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void HolderSet(Holder h, long v)
    {
        h.P.L = v;
        h.P.S = (short)(v * 3);
        h.P.I = (int)(v * 5);
        h.P.UL = (ulong)(v * 7);
        h.Q.I = (int)(v * 11);
        h.Q.L = v * 13;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long ElementSum(Packed13[] a, int i) => a[i].L + a[i].I * 3L + a[i].B * 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ElementSet(Packed13[] a, int i, long v) { a[i].L = v; a[i].I = (int)(v * 3); a[i].B = (byte)(v * 5); }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long Pack4Sum(Pack4[] a, int i) => a[i].L + a[i].I * 3L;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Pack4Set(Pack4[] a, int i, long v) { a[i].L = v; a[i].I = (int)(v * 3); }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long StaticSum() => s_packed.L + s_packed.S * 3L + s_packed.I * 5L + (long)s_packed.UL * 7 + s_packed13.L * 11 + s_packed13.I * 13L;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void StaticSet(long v)
    {
        s_packed.L = v;
        s_packed.S = (short)(v * 3);
        s_packed.I = (int)(v * 5);
        s_packed.UL = (ulong)(v * 7);
        s_packed13.L = v * 11;
        s_packed13.I = (int)(v * 13);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long LocalPacked(long v)
    {
        Packed p = default;
        p.L = v;
        p.S = (short)(v * 3);
        p.I = (int)(v * 5);
        p.UL = (ulong)(v * 7);
        Ref.Touch(&p);
        return p.L + p.S * 3L + p.I * 5L + (long)p.UL * 7;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long LocalPackedPlain(long v, long w)
    {
        Packed13 p;
        p.B = (byte)v;
        p.L = v ^ w;
        p.I = (int)(v + w);
        Packed13 q = p;
        q.L += 5;
        return p.L + p.I * 3L + p.B * 5 + q.L * 7 + q.I * 11L;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long ByValue(Packed13 a, Packed b, long k) => a.L + a.I * 3L + b.L * 5 + b.I * 7L + (long)b.UL * 11 + b.S * 13L + k;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Packed13 Returned(long v) { Packed13 p; p.B = 1; p.L = v; p.I = (int)(v * 3); return p; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long PointerFields(Packed* p) => p->L + p->S * 3L + p->I * 5L + (long)p->UL * 7;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long LimbsPointer(Limbs* p) => (long)(p->U0 + p->U1 * 3 + p->U2 * 5 + p->U3 * 7);
}

static unsafe class Program
{
    const int Half = 2400;

    static byte* s_buf;
    static byte* s_exp;
    static int s_fails;
    static int s_groupFails;
    static readonly long[] s_table = new long[16];

    static void EndGroup(string name)
    {
        if (s_groupFails == 0)
            Console.WriteLine("ok   " + name);
        else
            Console.WriteLine("FAIL " + name + " cases=" + s_groupFails);
        s_fails += s_groupFails;
        s_groupFails = 0;
    }

    static void Check(string name, int m, long expected, long got)
    {
        if (expected == got)
            return;
        s_groupFails++;
        if (s_groupFails <= 12)
            Console.WriteLine("FAIL " + name + " at +" + m + " expected 0x" + expected.ToString("x") + " got 0x" + got.ToString("x"));
    }

    static long Bytes<T>(T v) where T : unmanaged => (long)Ref.Get((byte*)&v, (sizeof(T) > 8) ? 8 : sizeof(T));
    static long L(byte* p) => (long)Ref.Get(p, 8);
    static long I(byte* p) => Ref.GetSigned(p, 4);
    static long S(byte* p) => Ref.GetSigned(p, 2);

    static void Refill(int seed)
    {
        Ref.Fill(s_buf - Half, 2 * Half, seed);
        Ref.Copy(s_exp - Half, s_buf - Half, 2 * Half);
    }

    static void Same(string name, int m)
    {
        int at = Ref.Diff(s_exp - Half, s_buf - Half, 2 * Half);
        if (at >= 0)
            Check(name + " (memory at offset " + (at - Half - m) + ")", m, s_exp[at - Half], s_buf[at - Half]);
    }

    static T Make<T>(int seed) where T : unmanaged
    {
        T v;
        Ref.Fill((byte*)&v, sizeof(T), seed);
        return v;
    }

    static void Type<T>(string type) where T : unmanaged
    {
        int n = sizeof(T), first = (n > 8) ? 8 : n;
        for (int m = 0; m < 8; m++)
        {
            byte* p = s_buf + m;
            byte* e = s_exp + m;
            Refill(m + n);

            Check(type + " Read", m, (long)Ref.Get(p, first), Bytes(Acc<T>.Read(p)));
            Check(type + " ReadAs", m, (long)Ref.Get(p, first), Bytes(Acc<T>.ReadAs(ref *p)));
            Check(type + " ReadRef", m, (long)Ref.Get(p, first), Bytes(Acc<T>.ReadRef(ref *(T*)p)));
            Check(type + " ReadIn", m, (long)Ref.Get(p, first), Bytes(Acc<T>.ReadIn(in *(T*)p)));
            Check(type + " ReadIndexed", m, (long)Ref.Get(p + 5 * n, first), Bytes(Acc<T>.ReadIndexed(p, 5)));
            Check(type + " ReadIndexed back", m, (long)Ref.Get(p - 3 * n, first), Bytes(Acc<T>.ReadIndexed(p, -3)));
            Check(type + " ReadCast", m, (long)Ref.Get(p + 2 * n, first), Bytes(Acc<T>.ReadCast(new Span<byte>(p, 4 * n), 2)));
            Check(type + " ReadSpan", m, (long)Ref.Get(p + 3 * n, first), Bytes(Acc<T>.ReadSpan(new Span<T>(p, 4), 3)));
            Check(type + " Read8", m, (long)Ref.Get(p + 8, first), Bytes(Acc<T>.Read8(p)));
            Check(type + " Read3", m, (long)Ref.Get(p + 3, first), Bytes(Acc<T>.Read3(p)));
            Check(type + " ReadBack16", m, (long)Ref.Get(p - 16, first), Bytes(Acc<T>.ReadBack16(p)));
            Check(type + " Read2040", m, (long)Ref.Get(p + 2040, first), Bytes(Acc<T>.Read2040(p)));
            Check(type + " Read2044", m, (long)Ref.Get(p + 2044, first), Bytes(Acc<T>.Read2044(p)));
            Check(type + " Read2046", m, (long)Ref.Get(p + 2046, first), Bytes(Acc<T>.Read2046(p)));
            Check(type + " Read2048", m, (long)Ref.Get(p + 2048, first), Bytes(Acc<T>.Read2048(p)));
            Check(type + " ReadBack2048", m, (long)Ref.Get(p - 2048, first), Bytes(Acc<T>.ReadBack2048(p)));
            Check(type + " Pass", m, (long)Ref.Get(p + 9, first), Bytes(Acc<T>.Pass(p + 9)));
            Same(type + " reads", m);

            T v = Make<T>(m * 7 + n);
            Ref.Copy(e, (byte*)&v, n); Acc<T>.Write(p, v); Same(type + " Write", m);
            v = Make<T>(m * 7 + n + 1);
            Ref.Copy(e + 32, (byte*)&v, n); Acc<T>.WriteAs(ref p[32], v); Same(type + " WriteAs", m);
            Ref.Copy(e + 64, (byte*)&v, n); Acc<T>.WriteRef(ref *(T*)(p + 64), v); Same(type + " WriteRef", m);
            Ref.Copy(e + 96, (byte*)&v, n); Acc<T>.WriteOut(out *(T*)(p + 96), v); Same(type + " WriteOut", m);
            Ref.Copy(e + 128 + 2 * n, (byte*)&v, n); Acc<T>.WriteIndexed(p + 128, 2, v); Same(type + " WriteIndexed", m);
            Ref.Copy(e + 200 + n, (byte*)&v, n); Acc<T>.WriteCast(new Span<byte>(p + 200, 3 * n), 1, v); Same(type + " WriteCast", m);
            v = Make<T>(m * 7 + n + 2);
            Ref.Copy(e + 8, (byte*)&v, n); Acc<T>.Write8(p, v); Same(type + " Write8", m);
            Ref.Copy(e + 3, (byte*)&v, n); Acc<T>.Write3(p, v); Same(type + " Write3", m);
            Ref.Copy(e + 2040, (byte*)&v, n); Acc<T>.Write2040(p, v); Same(type + " Write2040", m);
            Ref.Copy(e + 2044, (byte*)&v, n); Acc<T>.Write2044(p, v); Same(type + " Write2044", m);
            Ref.Copy(e + 2048, (byte*)&v, n); Acc<T>.Write2048(p, v); Same(type + " Write2048", m);
            Ref.Copy(e - 2048, (byte*)&v, n); Acc<T>.WriteBack2048(p, v); Same(type + " WriteBack2048", m);
            for (int i = 0; i < n; i++) e[300 + i] = 0;
            Acc<T>.WriteDefault(p + 300); Same(type + " WriteDefault", m);

            for (int m2 = 0; m2 < 8; m2 += 3)
            {
                Ref.Copy(e + 400 + m2, e + 500, n); Acc<T>.Move(p + 400 + m2, p + 500); Same(type + " Move", m);
                Ref.Copy(e + 440 + m2, e + 540, n); Acc<T>.ViaLocal(p + 440 + m2, p + 540); Same(type + " ViaLocal", m);
                Ref.Copy((byte*)&v, e + 600 + m2, n); Ref.Copy(e + 600 + m2, e + 700, n); Ref.Copy(e + 700, (byte*)&v, n);
                Acc<T>.Swap(p + 600 + m2, p + 700); Same(type + " Swap", m);
                long a = m + 1, b = m2 + 2, c = n, d = 77;
                long f1 = a * 3 + b, f2 = b * 5 + c, f3 = c * 7 + d, f4 = d * 11 + a;
                long want = a + b * 3 + c * 5 + d * 7 + f1 * 11 + f2 * 13 + f3 * 17 + f4 * 19 + (f1 ^ f2) * 23 + (f3 ^ f4) * 29;
                Ref.Copy(e + 800 + m2, e + 900, n);
                Check(type + " Live", m, want, Acc<T>.Live(p + 900, p + 800 + m2, a, b, c, d));
                Same(type + " Live", m);
            }
        }
    }

    static void Scalars()
    {
        for (int m = 0; m < 8; m++)
        {
            byte* p = s_buf + m;
            byte* e = s_exp + m;
            for (int round = 0; round < 4; round++)
            {
                Refill(m * 9 + round);
                if ((round & 1) == 0) { p[1] &= 0x7F; p[3] &= 0x7F; }
                else { p[1] |= 0x80; p[3] |= 0x80; }
                Ref.Copy(s_exp - Half, s_buf - Half, 2 * Half);

                Check("I16", m, S(p), Ops.I16(p));
                Check("U16", m, (long)Ref.Get(p, 2), Ops.U16(p));
                Check("I32", m, I(p), Ops.I32(p));
                Check("U32", m, (long)Ref.Get(p, 4), Ops.U32(p));
                Check("I32Div", m, I(p) / 3, Ops.I32Div(p));
                Check("I32Compare", m, 1, Ops.I32Compare(p, I(p)));
                Check("I32Compare unsigned", m, (I(p) < 0) ? 2 : 1, Ops.I32Compare(p, (long)Ref.Get(p, 4)));
                Check("I32Add", m, (int)(I(p) + 1000), Ops.I32Add(p, 1000));
                Check("I32Index", m, s_table[I(p) & 15], Ops.I32Index(p, s_table));
                Same("scalar reads", m);

                Ref.Put(e, 8, Ref.Get(p, 8) + 1); Ops.Increment(p); Same("Increment", m);
                Ref.Put(e + 16, 4, Ref.Get(p + 16, 4) + 0x7FFFFFFF); Ops.AddInt(p + 16, 0x7FFFFFFF); Same("AddInt", m);
                Ref.Put(e + 24, 8, (ulong)(p + 24)); Ops.StoreSelf(p + 24); Same("StoreSelf", m);
                Check("Follow", m, 24, Ops.Follow(p + 24) - p);
                Ref.Put(e + 32, 8, 0); Ops.StoreZero(p + 32); Same("StoreZero", m);
                Ref.Put(e + 41, 4, 0); Ops.StoreZeroInt(p + 41); Same("StoreZeroInt", m);
                Ref.Put(e + 48, 8, 0x1122334455667788); Ops.StoreConst(p + 48); Same("StoreConst", m);
            }
        }
    }

    static byte* Down(byte* p, int align) => (byte*)((nuint)p & ~((nuint)align - 1));

    static void Arithmetic()
    {
        for (int m = 0; m < 16; m++)
        {
            byte* p = s_buf + 64 + m;
            byte* d8 = Down(p, 8);
            Refill(m + 70);
            Check("Down8", m, L(d8), Ops.Down8(p));
            Check("Down4", m, L(Down(p, 4)), Ops.Down4(p));
            Check("Down16Plus8", m, L(Down(p, 16) + 8), Ops.Down16Plus8(p));
            Check("Down8Plus4", m, L(d8 + 4), Ops.Down8Plus4(p));
            Check("Up8", m, L(Down(p + 7, 8)), Ops.Up8(p));
            Check("Down8Or4", m, L(d8 + 4), Ops.Down8Or4(p));
            Check("Down8Int", m, I(d8), Ops.Down8Int(p));
            Check("Down2Int", m, I(Down(p, 2)), Ops.Down2Int(p));
            Check("Down4Plus2Short", m, S(Down(p, 4) + 2), Ops.Down4Plus2Short(p));
            Check("Down4Plus1Short", m, S(Down(p, 4) + 1), Ops.Down4Plus1Short(p));
            for (int i = 0; i < 8; i++)
            {
                Check("Down8Or", m, L(d8 + i), Ops.Down8Or(p, (nuint)i));
                Check("Down8Xor", m, L(d8 + i), Ops.Down8Xor(p, (nuint)i));
                Check("MaskVar", m, L(Down(p, 1 << (i & 3))), Ops.MaskVar(p, ~(((nuint)1 << (i & 3)) - 1)));
                Check("Times8", m, L(d8 + i * 8), Ops.Times8(p, i));
                Check("Times4", m, L(d8 + i * 4), Ops.Times4(p, i));
                Check("Times12", m, L(d8 + i * 12), Ops.Times12(p, i));
                Check("Times24", m, L(d8 + i * 24), Ops.Times24(p, i));
                Check("Shift3", m, L(d8 + i * 8), Ops.Shift3(p, i));
                Check("Shift2", m, L(d8 + i * 4), Ops.Shift2(p, i));
                Check("ShiftVar", m, L(d8 + (i << (i & 3))), Ops.ShiftVar(p, i, i & 3));
                Check("ShiftInt35", m, L(d8 + i * 8), Ops.ShiftInt35(p, i));
                Check("ShiftBack", m, L(d8 + i * 4), Ops.ShiftBack(p, i));
                Check("Product", m, L(d8 + i * 8), Ops.Product(p, i, 1));
                Check("Product by 3", m, L(d8 + i * 24), Ops.Product(p, i, 3));
                Check("ProductOdd", m, L(d8 + i * 4), Ops.ProductOdd(p, i, 1));
                Check("Difference", m, L(d8 + i * 16 - 24), Ops.Difference(p, i, 3));
                Check("DifferenceOdd", m, L(d8 + i * 16 - 12), Ops.DifferenceOdd(p, i, 3));
                Check("Negated", m, L(d8 + i * 8), Ops.Negated(p, i));
                Check("MaskedIndex", m, L(d8 + ((i * 13) & 0x78)), Ops.MaskedIndex(p, i * 13));
                Check("MaskedIndexOdd", m, L(d8 + ((i * 13) & 0x7C)), Ops.MaskedIndexOdd(p, i * 13));
                Check("ByteIndex", m, L(d8 + i * 8), Ops.ByteIndex(p, i));
                Check("Divided", m, L(d8 + i * 8), Ops.Divided(p, i));
                Check("Remainder", m, L(d8 + i * 8 % 20), Ops.Remainder(p, i));
                Check("PointerDifference", m, L(d8 + i * 3), Ops.PointerDifference(p, p + i * 3));

                long step = 0, step8 = 0, step4 = 0, index = 0;
                for (int k = 0; k < 6; k++) step = step * 3 + L(d8 + k * i);
                for (int k = 0; k < 6; k++) step8 = step8 * 3 + L(d8 + k * 8);
                for (int k = 0; k < 6; k++) step4 = step4 * 3 + L(d8 + k * 4) + I(d8 + k * 4);
                Check("LocalStep", m, step, Ops.LocalStep(p, 6, i));
                Check("LocalStep8", m, step8, Ops.LocalStep8(p, 6));
                Check("LocalStep4", m, step4, Ops.LocalStep4(p, 6));
                Check("IndexLocal", m, step, Ops.IndexLocal(p, 6, i));
                Check("IndexLocal8", m, step8, Ops.IndexLocal8(p, 6));
                Check("LocalLate", m, L(d8) + L(d8 + 16) * ((i < 3) ? i + 1 : 3) + L(d8 + 19) * ((i < 3) ? 2 - i : 0), Ops.LocalLate(p, i));
            }
            Check("LocalEither aligned", m, L(d8) + L(d8 + 8), Ops.LocalEither(p, false));
            Check("LocalEither", m, L(p) + L(p + 8), Ops.LocalEither(p, true));
            Check("LocalFromCall", m, L(p) + L(p + 8), Ops.LocalFromCall(p));
            byte* q = p + 3;
            Check("LocalFromMemory", m, L(q) + I(q + 8), Ops.LocalFromMemory(&q));
            Same("address arithmetic", m);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    static long StackAllocExpected(int at, long v)
    {
        byte* b = stackalloc byte[96];
        for (int i = 0; i < 96; i++) b[i] = (byte)(i * 7 + 1);
        Ref.Put(b + 16, 8, (ulong)v); Ref.Put(b + at, 8, (ulong)~v); Ref.Put(b + 44, 4, (ulong)v); Ref.Put(b + 49, 8, (ulong)(v * 3));
        long sum = 0;
        for (int i = 0; i < 96; i++) sum = sum * 31 + b[i];
        return sum + L(b + 8) + L(b + at + 3) + S(b + 81);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    static long StackSpanExpected(int at, long v)
    {
        byte* b = stackalloc byte[64];
        for (int i = 0; i < 64; i++) b[i] = (byte)(i * 5 + 3);
        Ref.Put(b + 8, 8, (ulong)v); Ref.Put(b + at, 8, (ulong)~v);
        long sum = L(b + at + 8);
        for (int i = 0; i < 64; i++) sum = sum * 31 + b[i];
        return sum + I(b + at + 1) + L(b + 16);
    }

    static void Spans()
    {
        byte[] arr = new byte[200];
        for (int i = 0; i < arr.Length; i++)
            arr[i] = (byte)(i * 23 + 0x83);
        fixed (byte* a = arr)
        {
            Check("SpanAt8", 8, L(a + 8) + L(a + 16), Ops.SpanAt8(arr));
            Check("SpanAt3", 3, L(a + 3) + L(a + 11), Ops.SpanAt3(arr));
            for (int at = 0; at < 24; at++)
            {
                Check("StackAlloc", at, StackAllocExpected(at, 0x0123456789ABCDEF * (at + 1)), Ops.StackAlloc(at, 0x0123456789ABCDEF * (at + 1)));
                Check("StackSpan", at, StackSpanExpected(at, 0x0FEDCBA987654321 * (at + 1)), Ops.StackSpan(at, 0x0FEDCBA987654321 * (at + 1)));
                Check("SpanAt", at, L(a + at) + L(a + at + 8), Ops.SpanAt(arr, at));
                Check("SpanAt8Index", at, L(a + 8 + at) + I(a + 16 + at), Ops.SpanAt8Index(arr, at));
                Check("SpanTwice", at, L(a + 8) + L(a + at) + L(a + at + 16), Ops.SpanTwice(arr, at, true));
                Check("SpanTwice once", at, L(a + 8) + L(a + 8) + L(a + 24), Ops.SpanTwice(arr, at, false));
                Check("SpanReplaced", at, L(a + 8) + L(a + at) + L(a + at + 8), Ops.SpanReplaced(arr, at));
                Check("SpanReturned", at, L(a + at) + L(a + at + 8), Ops.SpanReturned(arr, at));
                Check("SpanPair", at, L(a + 8) + L(a + at) * 3 + L(a + at + 8) * 5, Ops.SpanPair(arr, at));
                Check("LongsOverBytes", at, L(a + at + 16) + L(a + at + 24), Ops.LongsOverBytes(arr, at, 2));
                long sliced = 0, sliced8 = 0;
                for (int k = 0; k < 7; k++) sliced = sliced * 3 + L(a + 8 + k * at) + I(a + 16 + k * at);
                for (int k = 0; k < 7; k++) sliced8 = sliced8 * 3 + L(a + 8 + k * 8);
                if (8 + 6 * at + 12 <= arr.Length)
                    Check("SpanSliced", at, sliced, Ops.SpanSliced(arr, at, 7));
                Check("SpanSliced8", at, sliced8, Ops.SpanSliced8(arr, 7));
            }
        }
    }

    static long Limb(byte* p, int i) => L(p + 8 * i);

    static void Regions()
    {
        for (int m = 0; m < 8; m++)
        {
            for (int m2 = 0; m2 < 8; m2++)
            {
                byte* p = s_buf + m;
                byte* q = s_buf + 512 + m2;
                byte* e = s_exp + m;
                Refill(m * 8 + m2);

                if (m2 == 0)
                {
                    Check("Run", m, L(p) + L(p + 8) * 3 + L(p + 4) * 5 + L(p + 16) * 7 + I(p + 2) * 11 + S(p + 24) * 13, Ops.Run(p));
                    Check("LimbsPointer", m, L(p) + L(p + 8) * 3 + L(p + 16) * 5 + L(p + 24) * 7, Ops.LimbsPointer((Limbs*)p));
                    for (int k = 0; k < 4; k++)
                        Check("CallInside", m, L(p) + L(p + 8) + L(p + k) * 3 + L(p + 16) + L(p + k + 8) * 5, Ops.CallInside(p, k));
                    Check("NestedLoops", m, NestedExpected(p, 3, 4), Ops.NestedLoops(p, 3, 4));
                    Check("NestedLoops none", m, S(p + 40), Ops.NestedLoops(p, 0, 4));
                    Same("region reads", m);
                    for (int exit = 0; exit < 3; exit++)
                    {
                        long v = L(p + 300) + exit;
                        Ref.Put(e + 100, 8, (ulong)v); Ref.Put(e + 108, 4, (ulong)(v * 3));
                        if (exit > 0) { Ref.Put(e + 116, 8, (ulong)(v * 5)); Ref.Put(e + 112, 2, (ulong)(v * 7)); }
                        if (exit > 1) Ref.Put(e + 124, 8, (ulong)(v + v * 5));
                        Ops.StoreRun(p + 100, v, exit);
                        Same("StoreRun", m);
                    }
                }

                for (int step = 0; step < 4; step++)
                {
                    Check("MovedInBlock", m, L(p) + L(p + 8) + (L(p + step) + L(p + step + 8)) * 3, Ops.MovedInBlock(p, step));
                    for (int exit = 0; exit < 4; exit++)
                    {
                        long two = L(p);
                        if (exit >= 1) two += L(q) * 3 + L(p + 8);
                        if (exit >= 2) two += L(q + 8) * 5;
                        if (exit == 2) two += I(p + 16);
                        if (exit >= 3) two += L(q + 16) * 7 + L(p + 24);
                        if (step == 0)
                            Check("TwoBases", m * 8 + m2, two, Ops.TwoBases(p, q, exit));
                        long moved = L(p) + L(q);
                        if (exit >= 1) moved += L(p + 8) * 3 + L(q + step) * 5;
                        if (exit >= 2) moved += L(p + step) * 7 + L(q + step + 8) * 11 + L(p + step + 8) * 13;
                        if (exit < 3)
                            Check("MovedBetweenBlocks", m * 8 + m2, moved, Ops.MovedBetweenBlocks(p, q, step, exit));
                    }
                }

                // Two numbers of four limbs at independent distances from a boundary.
                for (int k = 0; k < 4; k++)
                {
                    Ref.Copy(q, p, 32);
                    if (k < 3) q[8 * k + 7] ^= 0x40;
                    Ref.Copy(s_exp - Half, s_buf - Half, 2 * Half);
                    int want = 0;
                    for (int i = 3; (i >= 0) && (want == 0); i--)
                        want = ((ulong)Limb(p, i) < (ulong)Limb(q, i)) ? -1 : ((ulong)Limb(p, i) > (ulong)Limb(q, i)) ? 1 : 0;
                    Check("Compare", m * 8 + m2, want, Ops.Compare(in *(Limbs*)p, in *(Limbs*)q));
                    Check("Compare swapped", m * 8 + m2, -want, Ops.Compare(in *(Limbs*)q, in *(Limbs*)p));
                }
                ulong carry = 0;
                byte* r = s_buf + 1024 + ((m + m2 * 3) & 7);
                byte* er = s_exp + (r - s_buf);
                if ((m2 & 1) != 0) { Ref.Put(p, 8, ulong.MaxValue); Ref.Put(q + 8, 8, ulong.MaxValue); Ref.Put(p + 24, 8, ulong.MaxValue); }
                Ref.Copy(s_exp - Half, s_buf - Half, 2 * Half);
                for (int i = 0; i < 4; i++)
                {
                    ulong x = (ulong)Limb(p, i), y = (ulong)Limb(q, i), s = x + y, c = (s < x) ? 1UL : 0;
                    s += carry;
                    if (s < carry) c = 1;
                    carry = c;
                    Ref.Put(er + 8 * i, 8, s);
                }
                Check("AddWithCarry", m * 8 + m2, (long)carry, (long)Ops.AddWithCarry(in *(Limbs*)p, in *(Limbs*)q, out *(Limbs*)r));
                Same("AddWithCarry", m * 8 + m2);

                // Loads and stores through two pointers to the same or overlapping memory.
                for (int over = -8; over <= 8; over += 4)
                {
                    Refill(m * 8 + m2 + over + 300);
                    byte* x = s_buf + 1500 + m, y = x + over, ex = s_exp + 1500 + m, ey = ex + over;
                    long v = 0x0102030405060708 * (m2 + 1);
                    long a = L(ex);
                    Ref.Put(ey, 8, (ulong)v);
                    long b = L(ex);
                    Ref.Put(ey + 8, 8, (ulong)a);
                    long c = L(ex + 8);
                    Ref.Put(ex + 4, 4, (ulong)b);
                    Check("Aliased", m * 100 + over, a + b * 3 + c * 5 + L(ey) * 7 + L(ey + 8) * 11, Ops.Aliased(x, y, v));
                    Same("Aliased", m * 100 + over);
                }

                for (int moveAt = -1; moveAt < 3; moveAt++)
                {
                    Refill(m * 8 + m2 + moveAt + 500);
                    byte* x = s_buf + 1700 + m, y = s_buf + 1800 + m2, ex = s_exp + 1700 + m, ey = s_exp + 1800 + m2;
                    long sum = L(ex);
                    for (int i = 0; i < 4; i++)
                    {
                        sum = sum * 3 + L(ex + 8) + L(ey);
                        if (i == moveAt) ey += 1;
                        Ref.Put(ex + 16, 8, (ulong)sum);
                    }
                    Check("LoopRegion", m * 100 + moveAt, sum + L(ex + 24) + L(ey + 8), Ops.LoopRegion(x, y, 4, moveAt));
                    Same("LoopRegion", m * 100 + moveAt);
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    static long NestedExpected(byte* p, int outer, int inner)
    {
        long sum = 0;
        for (int i = 0; i < outer; i++)
        {
            sum += L(p);
            for (int j = 0; j < inner; j++)
                sum = sum * 5 + L(p + 8) + I(p + 16);
            sum ^= L(p + 32);
        }
        return sum + S(p + 40);
    }

    static void OddOffsets()
    {
        OddFields o = new OddFields();
        Holder h = new Holder { Before = -1, After = -1 };
        Packed13[] a13 = new Packed13[6];
        Pack4[] a4 = new Pack4[6];
        for (int round = 1; round <= 6; round++)
        {
            long v = 0x0123456789ABCDEF * round;
            Ops.OddSet(o, v);
            Check("OddFields", round, v + (int)(v * 3) * 3L + (short)(v * 5) * 5L + v * 7 * 7 + v * 11 * 11 + (int)(v * 13) * 13L, Ops.OddSum(o));
            Check("OddFields byte", round, 0, o.B);
            Ops.HolderSet(h, v);
            Check("Holder", round, v + (short)(v * 3) * 3L + (int)(v * 5) * 5L + v * 7 * 7 + (int)(v * 11) * 11L + v * 13 * 13, Ops.HolderSum(h));
            Check("Holder guards", round, -2, h.Before + h.After);
            for (int i = 0; i < 6; i++)
            {
                Ops.ElementSet(a13, i, v + i);
                Ops.Pack4Set(a4, i, v - i);
            }
            for (int i = 0; i < 6; i++)
            {
                Check("Packed13[]", round, (v + i) + (int)((v + i) * 3) * 3L + (byte)((v + i) * 5) * 5, Ops.ElementSum(a13, i));
                Check("Pack4[]", round, (v - i) + (int)((v - i) * 3) * 3L, Ops.Pack4Sum(a4, i));
            }
            Ops.StaticSet(v);
            Check("statics", round, v + (short)(v * 3) * 3L + (int)(v * 5) * 5L + v * 7 * 7 + v * 11 * 11 + (int)(v * 13) * 13L, Ops.StaticSum());
            Check("LocalPacked", round, v + (short)(v * 3) * 3L + (int)(v * 5) * 5L + v * 7 * 7, Ops.LocalPacked(v));
            long w = ~v * 3;
            Check("LocalPackedPlain", round, (v ^ w) + (int)(v + w) * 3L + (byte)v * 5 + ((v ^ w) + 5) * 7 + (int)(v + w) * 11L,
                  Ops.LocalPackedPlain(v, w));
            Packed13 r = Ops.Returned(v);
            Check("Returned", round, v + (int)(v * 3) * 3L + 1, r.L + r.I * 3L + r.B);
            Packed big = default;
            big.L = w; big.I = (int)v; big.UL = (ulong)(v + w); big.S = (short)w;
            Check("ByValue", round, v + (int)(v * 3) * 3L + w * 5 + (int)v * 7L + (v + w) * 11 + (short)w * 13L + 9, Ops.ByValue(r, big, 9));
        }

        for (int m = 0; m < 8; m++)
        {
            byte* p = s_buf + m;
            Refill(m + 900);
            Check("PointerFields", m, L(p + 1) + S(p + 9) * 3 + I(p + 11) * 5 + L(p + 16) * 7, Ops.PointerFields((Packed*)p));
            Same("PointerFields", m);
        }
    }

    static int Main()
    {
        byte* mem = stackalloc byte[4 * Half + 64];
        s_buf = mem + Half + 8;
        s_exp = mem + 3 * Half + 24;
        for (int i = 0; i < s_table.Length; i++)
            s_table[i] = i * 0x100000007L + 13;

        Type<short>("short"); Type<ushort>("ushort"); Type<int>("int"); Type<uint>("uint"); Type<long>("long");
        Type<ulong>("ulong"); Type<nint>("nint"); Type<float>("float"); Type<double>("double");
        EndGroup("every scalar type at every distance from an 8-byte boundary");

        Type<T3>("T3"); Type<T6>("T6"); Type<T8>("T8"); Type<T12>("T12"); Type<T16>("T16"); Type<T24>("T24");
        EndGroup("small structs loaded, stored, passed and returned");

        Scalars();
        EndGroup("extension, arithmetic on the loaded value, stores of constants");

        Arithmetic();
        EndGroup("address arithmetic and locals that are or are not provably aligned");

        Spans();
        EndGroup("stackalloc space and spans over arrays");

        Regions();
        EndGroup("runs, regions and loops that share one alignment test");

        OddOffsets();
        EndGroup("fields at odd offsets in objects, arrays, statics, locals and parameters");

        Console.WriteLine(s_fails == 0 ? "ALL PASS" : "FAILED " + s_fails);
        return s_fails == 0 ? 0 : 1;
    }
}
