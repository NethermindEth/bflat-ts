// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Loads and stores the program itself marks as unaligned.
//
// Unsafe.ReadUnaligned and WriteUnaligned, BinaryPrimitives,
// MemoryMarshal.Read and Write and BitConverter carry the `unaligned.`
// prefix. A target that tolerates
// misaligned accesses gets one wide instruction for each; with
// JitNoUnalignedAccess the load is put together from single bytes and the
// store taken apart into single bytes, and with JitRiscV64StrictAlign the
// address is tested first and the wide instruction kept for the aligned
// case. What can go wrong: bytes combined in the wrong order or from the
// wrong offset, a value of a signed type that is not sign-extended (or of an
// unsigned type that is), the scratch register of the expansion being the
// address, the value or another live register, an offset that no longer fits
// the immediate once the last byte is addressed, the two paths of the tested
// form giving different values, a store that writes a byte too many, and a
// wide access left on a path where the address can be misaligned.
//
// Every access runs with its address at all eight distances from an 8-byte
// boundary: every integer type of two, four and eight bytes, float and
// double, small structs, at constant offsets up to the limits of an
// immediate (2047 and -2048) and at a variable index, with the result used
// in arithmetic, several accesses in one expression, a value read, changed
// and written back, stores of zero, of a constant and of the address itself,
// loops, and big-endian accesses. Values have their top bit set and clear.
// The last group reads and writes fields of packed structs through pointers,
// in arrays and in locals. Those accesses carry no prefix: the JIT marks a
// field whose offset is not a multiple of its size, and only strict
// alignment covers a field at an even offset of a struct at an odd address.
//
// The expected values are put together from single bytes by methods
// compiled without optimization. A run under an emulator that accepts
// misaligned accesses checks the values only; that no wide access is
// misaligned needs a target that rejects them or a trace of the accesses.
// Each group prints one line; the run ends with ALL PASS or a FAIL count.
using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct Packed
{
    public byte B0;
    public long L;
    public short S;
    public int I;
    public ushort U;
    public byte B1;
    public uint UI;
    public ulong UL;
    public char C;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct Outer
{
    public byte B;
    public Packed Inner;
    public long Tail;
}

struct T3 { public byte A, B, C; }
struct T6 { public short A, B, C; }
struct T12 { public int A, B, C; }
struct T16 { public long A, B; }
struct T24 { public long A, B, C; }

static unsafe class Ref
{
    const MethodImplOptions Plain = MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization;

    // The little-endian value of `size` bytes, zero-extended.
    [MethodImpl(Plain)]
    public static ulong Get(byte* p, int size)
    {
        ulong v = 0;
        for (int i = size - 1; i >= 0; i--)
            v = (v << 8) | p[i];
        return v;
    }

    // Sign-extended.
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
    public static ulong Reverse(ulong v, int size)
    {
        ulong r = 0;
        for (int i = 0; i < size; i++)
        {
            r = (r << 8) | (v & 0xFF);
            v >>= 8;
        }
        return r;
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
}

// One instantiation per type.
static unsafe class Acc<T> where T : unmanaged
{
    [MethodImpl(MethodImplOptions.NoInlining)] public static T Read(byte* p) => Unsafe.ReadUnaligned<T>(p);
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Write(byte* p, T v) => Unsafe.WriteUnaligned(p, v);
    [MethodImpl(MethodImplOptions.NoInlining)] public static T ReadRef(ref byte p) => Unsafe.ReadUnaligned<T>(ref p);
    [MethodImpl(MethodImplOptions.NoInlining)] public static void WriteRef(ref byte p, T v) => Unsafe.WriteUnaligned(ref p, v);
    [MethodImpl(MethodImplOptions.NoInlining)] public static T ReadSpan(ReadOnlySpan<byte> s) => MemoryMarshal.Read<T>(s);
    [MethodImpl(MethodImplOptions.NoInlining)] public static void WriteSpan(Span<byte> s, T v) => MemoryMarshal.Write(s, in v);
    [MethodImpl(MethodImplOptions.NoInlining)] public static T ReadAt(byte* p, nint i) => Unsafe.ReadUnaligned<T>(p + i);
    [MethodImpl(MethodImplOptions.NoInlining)] public static void WriteAt(byte* p, nint i, T v) => Unsafe.WriteUnaligned(p + i, v);
    [MethodImpl(MethodImplOptions.NoInlining)] public static T ReadIndexed(byte* p, int i) => Unsafe.ReadUnaligned<T>(p + i * sizeof(T));
    [MethodImpl(MethodImplOptions.NoInlining)] public static T ReadRefIndexed(ref byte p, int i) => Unsafe.ReadUnaligned<T>(ref Unsafe.Add(ref p, i));

    // Constant offsets: zero, the size, values that are not a multiple of it, the limits of an immediate.
    [MethodImpl(MethodImplOptions.NoInlining)] public static T Read8(byte* p) => Unsafe.ReadUnaligned<T>(p + 8);
    [MethodImpl(MethodImplOptions.NoInlining)] public static T Read3(byte* p) => Unsafe.ReadUnaligned<T>(p + 3);
    [MethodImpl(MethodImplOptions.NoInlining)] public static T ReadBack16(byte* p) => Unsafe.ReadUnaligned<T>(p - 16);
    [MethodImpl(MethodImplOptions.NoInlining)] public static T ReadBack5(byte* p) => Unsafe.ReadUnaligned<T>(p - 5);
    [MethodImpl(MethodImplOptions.NoInlining)] public static T Read2032(byte* p) => Unsafe.ReadUnaligned<T>(p + 2032);
    [MethodImpl(MethodImplOptions.NoInlining)] public static T Read2040(byte* p) => Unsafe.ReadUnaligned<T>(p + 2040);
    [MethodImpl(MethodImplOptions.NoInlining)] public static T Read2044(byte* p) => Unsafe.ReadUnaligned<T>(p + 2044);
    [MethodImpl(MethodImplOptions.NoInlining)] public static T Read2046(byte* p) => Unsafe.ReadUnaligned<T>(p + 2046);
    [MethodImpl(MethodImplOptions.NoInlining)] public static T Read2048(byte* p) => Unsafe.ReadUnaligned<T>(p + 2048);
    [MethodImpl(MethodImplOptions.NoInlining)] public static T ReadBack2048(byte* p) => Unsafe.ReadUnaligned<T>(p - 2048);
    [MethodImpl(MethodImplOptions.NoInlining)] public static T ReadBack2056(byte* p) => Unsafe.ReadUnaligned<T>(p - 2056);
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Write8(byte* p, T v) => Unsafe.WriteUnaligned(p + 8, v);
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Write3(byte* p, T v) => Unsafe.WriteUnaligned(p + 3, v);
    [MethodImpl(MethodImplOptions.NoInlining)] public static void WriteBack16(byte* p, T v) => Unsafe.WriteUnaligned(p - 16, v);
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Write2040(byte* p, T v) => Unsafe.WriteUnaligned(p + 2040, v);
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Write2044(byte* p, T v) => Unsafe.WriteUnaligned(p + 2044, v);
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Write2046(byte* p, T v) => Unsafe.WriteUnaligned(p + 2046, v);
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Write2048(byte* p, T v) => Unsafe.WriteUnaligned(p + 2048, v);
    [MethodImpl(MethodImplOptions.NoInlining)] public static void WriteBack2048(byte* p, T v) => Unsafe.WriteUnaligned(p - 2048, v);

    [MethodImpl(MethodImplOptions.NoInlining)] public static void WriteDefault(byte* p) => Unsafe.WriteUnaligned(p, default(T));
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Move(byte* d, byte* s) => Unsafe.WriteUnaligned(d, Unsafe.ReadUnaligned<T>(s));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Swap(byte* a, byte* b)
    {
        T x = Unsafe.ReadUnaligned<T>(a);
        T y = Unsafe.ReadUnaligned<T>(b);
        Unsafe.WriteUnaligned(a, y);
        Unsafe.WriteUnaligned(b, x);
    }

    // Other values live across the access.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long Live(byte* p, byte* q, long a, long b, long c, long d)
    {
        long e = a * 3 + b, f = b * 5 + c, g = c * 7 + d, h = d * 11 + a;
        T v = Unsafe.ReadUnaligned<T>(p);
        long i = e ^ f, j = g ^ h;
        Unsafe.WriteUnaligned(q, v);
        return a + b * 3 + c * 5 + d * 7 + e * 11 + f * 13 + g * 17 + h * 19 + i * 23 + j * 29;
    }
}

static unsafe class Ops
{
    // ---- widening: the register after the load ----

    [MethodImpl(MethodImplOptions.NoInlining)] public static long I16(byte* p) => Unsafe.ReadUnaligned<short>(p);
    [MethodImpl(MethodImplOptions.NoInlining)] public static long U16(byte* p) => Unsafe.ReadUnaligned<ushort>(p);
    [MethodImpl(MethodImplOptions.NoInlining)] public static long C16(byte* p) => Unsafe.ReadUnaligned<char>(p);
    [MethodImpl(MethodImplOptions.NoInlining)] public static long I32(byte* p) => Unsafe.ReadUnaligned<int>(p);
    [MethodImpl(MethodImplOptions.NoInlining)] public static long U32(byte* p) => Unsafe.ReadUnaligned<uint>(p);
    [MethodImpl(MethodImplOptions.NoInlining)] public static long I64(byte* p) => Unsafe.ReadUnaligned<long>(p);
    [MethodImpl(MethodImplOptions.NoInlining)] public static ulong U64(byte* p) => Unsafe.ReadUnaligned<ulong>(p);
    [MethodImpl(MethodImplOptions.NoInlining)] public static long I32Shifted(byte* p) => (long)Unsafe.ReadUnaligned<int>(p) << 8;
    [MethodImpl(MethodImplOptions.NoInlining)] public static int I32Negative(byte* p) => (Unsafe.ReadUnaligned<int>(p) < 0) ? 1 : 2;
    [MethodImpl(MethodImplOptions.NoInlining)] public static int I16Negative(byte* p) => (Unsafe.ReadUnaligned<short>(p) < 0) ? 1 : 2;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long U32Times(byte* p, long k) => Unsafe.ReadUnaligned<uint>(p) * k;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long I32Div(byte* p) => Unsafe.ReadUnaligned<int>(p) / 3;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long I32Compare(byte* p, long x) => (Unsafe.ReadUnaligned<int>(p) == x) ? 1 : 2;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long I32Add(byte* p, int x) => Unsafe.ReadUnaligned<int>(p) + x;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long Sum4(byte* p) =>
        Unsafe.ReadUnaligned<long>(p) + Unsafe.ReadUnaligned<long>(p + 8) * 3 + Unsafe.ReadUnaligned<int>(p + 16) * 5L +
        Unsafe.ReadUnaligned<short>(p + 20) * 7L + Unsafe.ReadUnaligned<ushort>(p + 22) * 11L;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Increment(byte* p) => Unsafe.WriteUnaligned(p, Unsafe.ReadUnaligned<long>(p) + 1);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void IncrementInt(byte* p, int k) => Unsafe.WriteUnaligned(p, Unsafe.ReadUnaligned<int>(p) + k);

    [MethodImpl(MethodImplOptions.NoInlining)] public static void StoreSelf(byte* p) => Unsafe.WriteUnaligned(p, (long)p);
    [MethodImpl(MethodImplOptions.NoInlining)] public static void StoreZero(byte* p) => Unsafe.WriteUnaligned(p, 0L);
    [MethodImpl(MethodImplOptions.NoInlining)] public static void StoreZeroInt(byte* p) => Unsafe.WriteUnaligned(p, 0);
    [MethodImpl(MethodImplOptions.NoInlining)] public static void StoreZeroShort(byte* p) => Unsafe.WriteUnaligned(p, (short)0);
    [MethodImpl(MethodImplOptions.NoInlining)] public static void StoreConst(byte* p) => Unsafe.WriteUnaligned(p, 0x1122334455667788L);
    [MethodImpl(MethodImplOptions.NoInlining)] public static void StoreConstInt(byte* p) => Unsafe.WriteUnaligned(p, unchecked((int)0x89ABCDEF));
    [MethodImpl(MethodImplOptions.NoInlining)] public static void StoreNarrowed(byte* p, long v) => Unsafe.WriteUnaligned(p, (short)v);
    [MethodImpl(MethodImplOptions.NoInlining)] public static void StoreNarrowedInt(byte* p, long v) => Unsafe.WriteUnaligned(p, (int)v);

    // The pointer is loaded through itself: the result register is the address register.
    [MethodImpl(MethodImplOptions.NoInlining)] public static byte* Follow(byte* p) => Unsafe.ReadUnaligned<nint>(p) + (byte*)0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long Loop(byte* p, int count)
    {
        long sum = 0;
        for (int i = 0; i < count; i++)
            sum = sum * 3 + Unsafe.ReadUnaligned<long>(p + i * 8);
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long LoopOdd(byte* p, int count)
    {
        long sum = 0;
        for (int i = 0; i < count; i++)
            sum = sum * 3 + Unsafe.ReadUnaligned<int>(p + i * 5);
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LoopStore(byte* p, int count, long v)
    {
        for (int i = 0; i < count; i++)
            Unsafe.WriteUnaligned(p + i * 8, v + i);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long LoopSpan(ReadOnlySpan<byte> s)
    {
        long sum = 0;
        while (s.Length >= 4)
        {
            sum = sum * 5 + BinaryPrimitives.ReadInt32LittleEndian(s);
            s = s.Slice(3);
        }
        return sum;
    }

    // ---- BinaryPrimitives, BitConverter ----

    [MethodImpl(MethodImplOptions.NoInlining)] public static long LE64(ReadOnlySpan<byte> s) => BinaryPrimitives.ReadInt64LittleEndian(s);
    [MethodImpl(MethodImplOptions.NoInlining)] public static long BE64(ReadOnlySpan<byte> s) => BinaryPrimitives.ReadInt64BigEndian(s);
    [MethodImpl(MethodImplOptions.NoInlining)] public static long LE32(ReadOnlySpan<byte> s) => BinaryPrimitives.ReadInt32LittleEndian(s);
    [MethodImpl(MethodImplOptions.NoInlining)] public static long BE32(ReadOnlySpan<byte> s) => BinaryPrimitives.ReadInt32BigEndian(s);
    [MethodImpl(MethodImplOptions.NoInlining)] public static long BEU32(ReadOnlySpan<byte> s) => BinaryPrimitives.ReadUInt32BigEndian(s);
    [MethodImpl(MethodImplOptions.NoInlining)] public static long LE16(ReadOnlySpan<byte> s) => BinaryPrimitives.ReadInt16LittleEndian(s);
    [MethodImpl(MethodImplOptions.NoInlining)] public static long BE16(ReadOnlySpan<byte> s) => BinaryPrimitives.ReadInt16BigEndian(s);
    [MethodImpl(MethodImplOptions.NoInlining)] public static long BEU16(ReadOnlySpan<byte> s) => BinaryPrimitives.ReadUInt16BigEndian(s);
    [MethodImpl(MethodImplOptions.NoInlining)] public static void WBE64(Span<byte> s, long v) => BinaryPrimitives.WriteInt64BigEndian(s, v);
    [MethodImpl(MethodImplOptions.NoInlining)] public static void WLE64(Span<byte> s, long v) => BinaryPrimitives.WriteInt64LittleEndian(s, v);
    [MethodImpl(MethodImplOptions.NoInlining)] public static void WBE32(Span<byte> s, uint v) => BinaryPrimitives.WriteUInt32BigEndian(s, v);
    [MethodImpl(MethodImplOptions.NoInlining)] public static void WBE16(Span<byte> s, short v) => BinaryPrimitives.WriteInt16BigEndian(s, v);
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Bits64(ReadOnlySpan<byte> s) => BitConverter.ToInt64(s);
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Bits32(byte[] a, int i) => BitConverter.ToInt32(a, i);
    [MethodImpl(MethodImplOptions.NoInlining)] public static bool TryWrite(Span<byte> s, long v) => BitConverter.TryWriteBytes(s, v);

    // ---- fields of a packed struct ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long PackedSum(Packed* p) =>
        p->B0 + p->L * 3 + p->S * 5L + p->I * 7L + p->U * 11L + p->B1 * 13L + p->UI * 17L + (long)p->UL * 19 + p->C * 23L;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void PackedSet(Packed* p, long v)
    {
        p->B0 = (byte)v;
        p->L = v * 3;
        p->S = (short)(v * 5);
        p->I = (int)(v * 7);
        p->U = (ushort)(v * 11);
        p->B1 = (byte)(v * 13);
        p->UI = (uint)(v * 17);
        p->UL = (ulong)(v * 19);
        p->C = (char)(v * 23);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long PackedRefSum(ref Packed p) => p.L + p.S * 3L + p.I * 5L + p.UI * 7L + (long)p.UL * 11;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long OuterSum(Outer* p) => p->Inner.L + p->Inner.I * 3L + p->Tail * 5 + p->Inner.S * 7L;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void OuterSet(Outer* p, long v)
    {
        p->Inner.L = v;
        p->Inner.I = (int)(v >> 3);
        p->Tail = ~v;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long PackedElement(Packed[] a, int i) => a[i].L + a[i].I * 3L + a[i].S * 5L + (long)a[i].UL * 7;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void PackedElementSet(Packed[] a, int i, long v)
    {
        a[i].L = v;
        a[i].I = (int)(v * 3);
        a[i].UL = (ulong)(v * 5);
        a[i].S = (short)(v * 7);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long PackedLocal(long v)
    {
        Packed p = default;
        p.L = v;
        p.I = (int)(v * 3);
        p.S = (short)(v * 5);
        p.UL = (ulong)(v * 7);
        Touch(&p);
        return p.L + p.I * 3L + p.S * 5L + (long)p.UL * 7 + p.B0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long PackedCopy(Packed* d, Packed* s)
    {
        Packed t = *s;
        t.I++;
        *d = t;
        return t.L;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)] static void Touch(Packed* p) => p->B0 = 9;
}

static unsafe class Program
{
    const int Half = 2400;

    static byte* s_buf;
    static byte* s_exp;
    static int s_fails;
    static int s_groupFails;

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
            Check(type + " ReadRef", m, (long)Ref.Get(p, first), Bytes(Acc<T>.ReadRef(ref *p)));
            Check(type + " ReadSpan", m, (long)Ref.Get(p, first), Bytes(Acc<T>.ReadSpan(new ReadOnlySpan<byte>(p, n))));
            Check(type + " ReadAt", m, (long)Ref.Get(p + 13, first), Bytes(Acc<T>.ReadAt(p, 13)));
            Check(type + " ReadAt back", m, (long)Ref.Get(p - 21, first), Bytes(Acc<T>.ReadAt(p, -21)));
            Check(type + " ReadIndexed", m, (long)Ref.Get(p + 3 * n, first), Bytes(Acc<T>.ReadIndexed(p, 3)));
            Check(type + " ReadRefIndexed", m, (long)Ref.Get(p + 7, first), Bytes(Acc<T>.ReadRefIndexed(ref *p, 7)));
            Check(type + " Read8", m, (long)Ref.Get(p + 8, first), Bytes(Acc<T>.Read8(p)));
            Check(type + " Read3", m, (long)Ref.Get(p + 3, first), Bytes(Acc<T>.Read3(p)));
            Check(type + " ReadBack16", m, (long)Ref.Get(p - 16, first), Bytes(Acc<T>.ReadBack16(p)));
            Check(type + " ReadBack5", m, (long)Ref.Get(p - 5, first), Bytes(Acc<T>.ReadBack5(p)));
            Check(type + " Read2032", m, (long)Ref.Get(p + 2032, first), Bytes(Acc<T>.Read2032(p)));
            Check(type + " Read2040", m, (long)Ref.Get(p + 2040, first), Bytes(Acc<T>.Read2040(p)));
            Check(type + " Read2044", m, (long)Ref.Get(p + 2044, first), Bytes(Acc<T>.Read2044(p)));
            Check(type + " Read2046", m, (long)Ref.Get(p + 2046, first), Bytes(Acc<T>.Read2046(p)));
            Check(type + " Read2048", m, (long)Ref.Get(p + 2048, first), Bytes(Acc<T>.Read2048(p)));
            Check(type + " ReadBack2048", m, (long)Ref.Get(p - 2048, first), Bytes(Acc<T>.ReadBack2048(p)));
            Check(type + " ReadBack2056", m, (long)Ref.Get(p - 2056, first), Bytes(Acc<T>.ReadBack2056(p)));
            Same(type + " reads", m);

            T v = Make<T>(m * 7 + n);
            Ref.Copy(e, (byte*)&v, n); Acc<T>.Write(p, v); Same(type + " Write", m);
            v = Make<T>(m * 7 + n + 1);
            Ref.Copy(e + 32, (byte*)&v, n); Acc<T>.WriteRef(ref p[32], v); Same(type + " WriteRef", m);
            Ref.Copy(e + 64, (byte*)&v, n); Acc<T>.WriteSpan(new Span<byte>(p + 64, n + 3), v); Same(type + " WriteSpan", m);
            Ref.Copy(e + 109, (byte*)&v, n); Acc<T>.WriteAt(p, 109, v); Same(type + " WriteAt", m);
            Ref.Copy(e - 77, (byte*)&v, n); Acc<T>.WriteAt(p, -77, v); Same(type + " WriteAt back", m);
            v = Make<T>(m * 7 + n + 2);
            Ref.Copy(e + 8, (byte*)&v, n); Acc<T>.Write8(p, v); Same(type + " Write8", m);
            Ref.Copy(e + 3, (byte*)&v, n); Acc<T>.Write3(p, v); Same(type + " Write3", m);
            Ref.Copy(e - 16, (byte*)&v, n); Acc<T>.WriteBack16(p, v); Same(type + " WriteBack16", m);
            Ref.Copy(e + 2040, (byte*)&v, n); Acc<T>.Write2040(p, v); Same(type + " Write2040", m);
            Ref.Copy(e + 2044, (byte*)&v, n); Acc<T>.Write2044(p, v); Same(type + " Write2044", m);
            Ref.Copy(e + 2046, (byte*)&v, n); Acc<T>.Write2046(p, v); Same(type + " Write2046", m);
            Ref.Copy(e + 2048, (byte*)&v, n); Acc<T>.Write2048(p, v); Same(type + " Write2048", m);
            Ref.Copy(e - 2048, (byte*)&v, n); Acc<T>.WriteBack2048(p, v); Same(type + " WriteBack2048", m);
            for (int i = 0; i < n; i++) e[200 + i] = 0;
            Acc<T>.WriteDefault(p + 200); Same(type + " WriteDefault", m);

            for (int m2 = 0; m2 < 8; m2 += 3)
            {
                Ref.Copy(e + 300 + m2, e + 400, n); Acc<T>.Move(p + 300 + m2, p + 400); Same(type + " Move", m);
                Ref.Copy((byte*)&v, e + 500 + m2, n); Ref.Copy(e + 500 + m2, e + 600, n); Ref.Copy(e + 600, (byte*)&v, n);
                Acc<T>.Swap(p + 500 + m2, p + 600); Same(type + " Swap", m);
                long a = m + 1, b = m2 + 2, c = n, d = 77;
                long f1 = a * 3 + b, f2 = b * 5 + c, f3 = c * 7 + d, f4 = d * 11 + a;
                long want = a + b * 3 + c * 5 + d * 7 + f1 * 11 + f2 * 13 + f3 * 17 + f4 * 19 + (f1 ^ f2) * 23 + (f3 ^ f4) * 29;
                Ref.Copy(e + 700 + m2, e + 800, n);
                Check(type + " Live", m, want, Acc<T>.Live(p + 800, p + 700 + m2, a, b, c, d));
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
            for (int round = 0; round < 6; round++)
            {
                Refill(m * 9 + round);
                // Top bits set and clear in turn.
                if ((round & 1) == 0) { p[1] &= 0x7F; p[3] &= 0x7F; p[7] &= 0x7F; }
                else { p[1] |= 0x80; p[3] |= 0x80; p[7] |= 0x80; }
                Ref.Copy(s_exp - Half, s_buf - Half, 2 * Half);

                Check("I16", m, Ref.GetSigned(p, 2), Ops.I16(p));
                Check("U16", m, (long)Ref.Get(p, 2), Ops.U16(p));
                Check("C16", m, (long)Ref.Get(p, 2), Ops.C16(p));
                Check("I32", m, Ref.GetSigned(p, 4), Ops.I32(p));
                Check("U32", m, (long)Ref.Get(p, 4), Ops.U32(p));
                Check("I64", m, (long)Ref.Get(p, 8), Ops.I64(p));
                Check("U64", m, (long)Ref.Get(p, 8), (long)Ops.U64(p));
                Check("I32Shifted", m, Ref.GetSigned(p, 4) << 8, Ops.I32Shifted(p));
                Check("I32Negative", m, (Ref.GetSigned(p, 4) < 0) ? 1 : 2, Ops.I32Negative(p));
                Check("I16Negative", m, (Ref.GetSigned(p, 2) < 0) ? 1 : 2, Ops.I16Negative(p));
                Check("U32Times", m, (long)Ref.Get(p, 4) * 0x100000001L, Ops.U32Times(p, 0x100000001L));
                Check("I32Div", m, Ref.GetSigned(p, 4) / 3, Ops.I32Div(p));
                Check("I32Compare", m, 1, Ops.I32Compare(p, Ref.GetSigned(p, 4)));
                Check("I32Compare unsigned", m, (Ref.GetSigned(p, 4) < 0) ? 2 : 1, Ops.I32Compare(p, (long)Ref.Get(p, 4)));
                Check("I32Add", m, (int)(Ref.GetSigned(p, 4) + 1000), Ops.I32Add(p, 1000));
                Check("Sum4", m, (long)Ref.Get(p, 8) + (long)Ref.Get(p + 8, 8) * 3 + Ref.GetSigned(p + 16, 4) * 5 +
                      Ref.GetSigned(p + 20, 2) * 7 + (long)Ref.Get(p + 22, 2) * 11, Ops.Sum4(p));
                long loop = 0, loopOdd = 0, loopSpan = 0;
                for (int i = 0; i < 9; i++) loop = loop * 3 + (long)Ref.Get(p + 40 + i * 8, 8);
                for (int i = 0; i < 9; i++) loopOdd = loopOdd * 3 + Ref.GetSigned(p + 40 + i * 5, 4);
                for (int i = 0; i + 4 <= 31; i += 3) loopSpan = loopSpan * 5 + Ref.GetSigned(p + 40 + i, 4);
                Check("Loop", m, loop, Ops.Loop(p + 40, 9));
                Check("LoopOdd", m, loopOdd, Ops.LoopOdd(p + 40, 9));
                Check("LoopSpan", m, loopSpan, Ops.LoopSpan(new ReadOnlySpan<byte>(p + 40, 31)));

                Check("LE64", m, (long)Ref.Get(p, 8), Ops.LE64(new ReadOnlySpan<byte>(p, 8)));
                Check("BE64", m, (long)Ref.Reverse(Ref.Get(p, 8), 8), Ops.BE64(new ReadOnlySpan<byte>(p, 9)));
                Check("LE32", m, Ref.GetSigned(p, 4), Ops.LE32(new ReadOnlySpan<byte>(p, 4)));
                Check("BE32", m, (int)Ref.Reverse(Ref.Get(p, 4), 4), Ops.BE32(new ReadOnlySpan<byte>(p, 4)));
                Check("BEU32", m, (long)Ref.Reverse(Ref.Get(p, 4), 4), Ops.BEU32(new ReadOnlySpan<byte>(p, 4)));
                Check("LE16", m, Ref.GetSigned(p, 2), Ops.LE16(new ReadOnlySpan<byte>(p, 2)));
                Check("BE16", m, (short)Ref.Reverse(Ref.Get(p, 2), 2), Ops.BE16(new ReadOnlySpan<byte>(p, 2)));
                Check("BEU16", m, (long)Ref.Reverse(Ref.Get(p, 2), 2), Ops.BEU16(new ReadOnlySpan<byte>(p, 2)));
                Check("Bits64", m, (long)Ref.Get(p + 1, 8), Ops.Bits64(new ReadOnlySpan<byte>(p + 1, 8)));
                Same("scalar reads", m);

                long v = (long)Ref.Get(p + 100, 8);
                Ref.Put(e, 8, Ref.Get(p, 8) + 1); Ops.Increment(p); Same("Increment", m);
                Ref.Put(e + 16, 4, Ref.Get(p + 16, 4) + 0x7FFFFFFF); Ops.IncrementInt(p + 16, 0x7FFFFFFF); Same("IncrementInt", m);
                Ref.Put(e + 24, 8, (ulong)(p + 24)); Ops.StoreSelf(p + 24); Same("StoreSelf", m);
                Check("Follow", m, 24, Ops.Follow(p + 24) - p);
                Ref.Put(e + 32, 8, 0); Ops.StoreZero(p + 32); Same("StoreZero", m);
                Ref.Put(e + 41, 4, 0); Ops.StoreZeroInt(p + 41); Same("StoreZeroInt", m);
                Ref.Put(e + 46, 2, 0); Ops.StoreZeroShort(p + 46); Same("StoreZeroShort", m);
                Ref.Put(e + 48, 8, 0x1122334455667788); Ops.StoreConst(p + 48); Same("StoreConst", m);
                Ref.Put(e + 57, 4, 0x89ABCDEF); Ops.StoreConstInt(p + 57); Same("StoreConstInt", m);
                Ref.Put(e + 62, 2, (ulong)v); Ops.StoreNarrowed(p + 62, v); Same("StoreNarrowed", m);
                Ref.Put(e + 64, 4, (ulong)v); Ops.StoreNarrowedInt(p + 64, v); Same("StoreNarrowedInt", m);
                for (int i = 0; i < 5; i++) Ref.Put(e + 120 + i * 8, 8, (ulong)(v + i));
                Ops.LoopStore(p + 120, 5, v); Same("LoopStore", m);
                Ref.Put(e + 200, 8, Ref.Reverse((ulong)v, 8)); Ops.WBE64(new Span<byte>(p + 200, 8), v); Same("WBE64", m);
                Ref.Put(e + 209, 8, (ulong)v); Ops.WLE64(new Span<byte>(p + 209, 8), v); Same("WLE64", m);
                Ref.Put(e + 218, 4, Ref.Reverse((ulong)(uint)v, 4)); Ops.WBE32(new Span<byte>(p + 218, 4), (uint)v); Same("WBE32", m);
                Ref.Put(e + 223, 2, Ref.Reverse((ulong)(ushort)v, 2)); Ops.WBE16(new Span<byte>(p + 223, 2), (short)v); Same("WBE16", m);
                Ref.Put(e + 226, 8, (ulong)~v); Check("TryWrite", m, 1, Ops.TryWrite(new Span<byte>(p + 226, 9), ~v) ? 1 : 0);
                Same("TryWrite", m);
            }
        }

        byte[] arr = new byte[40];
        for (int i = 0; i < arr.Length; i++) arr[i] = (byte)(i * 29 + 0x85);
        fixed (byte* a = arr)
        {
            for (int i = 0; i < 12; i++)
                Check("Bits32", i, Ref.GetSigned(a + i, 4), Ops.Bits32(arr, i));
        }
    }

    static void PackedStructs()
    {
        for (int m = 0; m < 8; m++)
        {
            byte* p = s_buf + m;
            byte* e = s_exp + m;
            for (int round = 0; round < 4; round++)
            {
                Refill(m * 5 + round + 40);
                Packed* q = (Packed*)p;
                long sum = p[0] + (long)Ref.Get(p + 1, 8) * 3 + Ref.GetSigned(p + 9, 2) * 5 + Ref.GetSigned(p + 11, 4) * 7 +
                           (long)Ref.Get(p + 15, 2) * 11 + p[17] * 13L + (long)Ref.Get(p + 18, 4) * 17 +
                           (long)Ref.Get(p + 22, 8) * 19 + (long)Ref.Get(p + 30, 2) * 23;
                Check("PackedSum", m, sum, Ops.PackedSum(q));
                Check("PackedRefSum", m, (long)Ref.Get(p + 1, 8) + Ref.GetSigned(p + 9, 2) * 3 + Ref.GetSigned(p + 11, 4) * 5 +
                      (long)Ref.Get(p + 18, 4) * 7 + (long)Ref.Get(p + 22, 8) * 11, Ops.PackedRefSum(ref *q));
                Check("OuterSum", m, (long)Ref.Get(p + 2, 8) + Ref.GetSigned(p + 12, 4) * 3 + (long)Ref.Get(p + 33, 8) * 5 +
                      Ref.GetSigned(p + 10, 2) * 7, Ops.OuterSum((Outer*)p));
                Same("packed reads", m);

                long v = (long)Ref.Get(p + 300, 8) | 1;
                byte* d = e + 64;
                d[0] = (byte)v; Ref.Put(d + 1, 8, (ulong)(v * 3)); Ref.Put(d + 9, 2, (ulong)(v * 5)); Ref.Put(d + 11, 4, (ulong)(v * 7));
                Ref.Put(d + 15, 2, (ulong)(v * 11)); d[17] = (byte)(v * 13); Ref.Put(d + 18, 4, (ulong)(v * 17));
                Ref.Put(d + 22, 8, (ulong)(v * 19)); Ref.Put(d + 30, 2, (ulong)(v * 23));
                Ops.PackedSet((Packed*)(p + 64), v);
                Same("PackedSet", m);
                d = e + 128;
                Ref.Put(d + 2, 8, (ulong)v); Ref.Put(d + 12, 4, (ulong)(v >> 3)); Ref.Put(d + 33, 8, (ulong)~v);
                Ops.OuterSet((Outer*)(p + 128), v);
                Same("OuterSet", m);

                Ref.Copy(e + 200, e + 400, 32);
                Ref.Put(e + 200 + 11, 4, Ref.Get(e + 400 + 11, 4) + 1);
                Check("PackedCopy", m, (long)Ref.Get(p + 400 + 1, 8), Ops.PackedCopy((Packed*)(p + 200), (Packed*)(p + 400)));
                Same("PackedCopy", m);

                Check("PackedLocal", m, v + (int)(v * 3) * 3L + (short)(v * 5) * 5L + v * 7 * 7 + 9, Ops.PackedLocal(v));
            }
        }

        Packed[] arr = new Packed[5];
        for (int i = 0; i < 5; i++)
        {
            long v = 0x0123456789ABCDEF * (i + 1);
            Ops.PackedElementSet(arr, i, v);
            Check("PackedElement", i, v + (int)(v * 3) * 3L + (short)(v * 7) * 5L + v * 5 * 7, Ops.PackedElement(arr, i));
        }
        for (int i = 0; i < 5; i++)
        {
            long v = 0x0123456789ABCDEF * (i + 1);
            Check("PackedElement again", i, v + (int)(v * 3) * 3L + (short)(v * 7) * 5L + v * 5 * 7, Ops.PackedElement(arr, i));
        }
    }

    static int Main()
    {
        byte* mem = stackalloc byte[4 * Half + 64];
        s_buf = mem + Half + 8;
        s_exp = mem + 3 * Half + 24;

        Type<short>("short"); Type<ushort>("ushort"); Type<char>("char"); Type<int>("int"); Type<uint>("uint");
        Type<long>("long"); Type<ulong>("ulong"); Type<nint>("nint"); Type<float>("float"); Type<double>("double");
        EndGroup("every scalar type at every distance from an 8-byte boundary");

        Type<T3>("T3"); Type<T6>("T6"); Type<T12>("T12"); Type<T16>("T16"); Type<T24>("T24");
        EndGroup("small structs");

        Scalars();
        EndGroup("sign and zero extension, arithmetic, loops, big-endian accesses");

        PackedStructs();
        EndGroup("fields of packed structs");

        Console.WriteLine(s_fails == 0 ? "ALL PASS" : "FAILED " + s_fails);
        return s_fails == 0 ? 0 : 1;
    }
}
