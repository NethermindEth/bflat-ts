// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Array allocation and element access, block copies, unaligned loads and
// stores, and enums, on riscv32 (Airbender).
//
// Arrays. On a 32-bit target an array is a 4-byte type pointer, a 4-byte
// length and the elements from offset 8; its size is the 12-byte base size
// plus length times element size, rounded up to the object alignment, and
// arrays of 8-byte elements are aligned to 8. The allocation helpers and
// the JIT's inline allocation compute that size, and the element address
// is base + 8 + index * size with the scale and the offset folded into the
// load or store where they fit. What can go wrong: a size that is rounded
// before the elements are added or not rounded at all, so that neighbouring
// objects overlap or the next allocation is misaligned; the 64-bit element
// offset or scale used on 32 bits; an element address folded with the wrong
// offset; a length or bounds check reading the wrong word; a negative
// length accepted.
//
// Unaligned access. Airbender traps on a misaligned load or store, so with
// JitNoUnalignedAccess every access the front end marks unaligned
// (Unsafe.ReadUnaligned, WriteUnaligned, CopyBlockUnaligned, fields of
// packed structs at odd offsets) is done byte by byte. A struct of 2 or 4
// bytes stored through such an access is a block store that is turned into
// a scalar store, and patch 03 keeps the unaligned mark across that. What
// can go wrong: the mark lost on the way (the guest traps), bytes assembled
// in the wrong order or sign-extended into their neighbours, a byte too
// many or too few written, a 64-bit value whose halves go to the wrong
// offsets. (Structs that travel in registers - a parameter that wraps one
// primitive or holds two ints, a struct of up to 8 bytes returned from the
// read - are in rv32_unaligned_wrappers.cs and whole copies of byte-aligned
// structs in rv32_byte_aligned_structs.cs: a trap ends the guest, so each
// has its own file.)
//
// Block copies go to memcpy and memmove, which the Airbender library
// replaces: every length from 0 to 130 at every mutual alignment, forward
// and overlapping, is checked against a byte loop, with guard bytes on both
// sides.
//
// Enums. Patch 03 gives an enum a description without names when the build
// has no reflection data, so that formatting falls back to the number. What
// can go wrong: the wrong underlying width or signedness (0xFF as -1, a
// 64-bit value truncated to its low half), a failure instead of the number.
// The test adapts to the build: it expects names when the first enum prints
// its name and numbers otherwise.
//
// Each group prints the number of checks; a failed check prints one line
// with the expected and the actual value. The run ends with ALL PASS or a
// FAIL count, and the guest returns 1 on failure.
using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

struct S3 { public byte A, B, C; }
struct S2 { public byte A, B; }
struct S4 { public byte A, B, C, D; }
struct SInt { public int V; }
struct SPair { public short A, B; }
struct S6 { public short A, B, C; }
struct S8 { public int A, B; }
struct S12 { public int A, B, C; }
struct S16 { public long A, B; }
struct S24 { public long A; public int B; public long C; }
struct SRef { public object O; public int N; }

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct Packed
{
    public byte Tag;
    public int I;
    public long L;
    public short S;
    public SInt Inner;
    public byte Tail;
}

enum Color { Red, Green, Blue }
enum Small : byte { None = 0, Max = 0xFF }
enum Neg : sbyte { Minus = -1, Zero = 0 }
enum Wide : long { Low = 1, High = 0x100000000L, Top = long.MinValue }
enum WideU : ulong { Low = 1, High = 0x100000000UL, Top = 0x8000000000000000UL, All = ulong.MaxValue }
[Flags] enum Bits { A = 1, B = 2, C = 4, D = 0x40000000 }
[Flags] enum WideBits : ulong { Lo = 1, Hi = 0x8000000000000000UL }

unsafe class Program
{
    const MethodImplOptions NI = MethodImplOptions.NoInlining;

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

    static void CheckS(string name, string got, string exp)
    {
        checks++;
        if (got != exp)
        {
            fails++;
            if (fails <= 60) Console.WriteLine("FAIL " + name + " expected " + exp + " got " + got);
        }
    }

    static void Group(string name)
    {
        Console.WriteLine("ok   " + name + " " + checks.ToString());
    }

    [MethodImpl(NI)] static int Id(int x) => x;

    // ================= arrays =================

    [MethodImpl(NI)] static T[] New<T>(int n) => new T[n];
    [MethodImpl(NI)] static byte[] NewBytes(int n) => new byte[n];
    [MethodImpl(NI)] static long[] NewLongs(int n) => new long[n];
    [MethodImpl(NI)] static object[] NewObjects(int n) => new object[n];
    [MethodImpl(NI)] static byte[] NewBytes5() => new byte[5];
    [MethodImpl(NI)] static short[] NewShorts3() => new short[3];
    [MethodImpl(NI)] static long[] NewLongs3() => new long[3];
    [MethodImpl(NI)] static S3[] NewS3x5() => new S3[5];
    [MethodImpl(NI)] static S12[] NewS12x3() => new S12[3];
    [MethodImpl(NI)] static int[] NewInts0() => new int[0];
    [MethodImpl(NI)] static int[,] New2D(int a, int b) => new int[a, b];
    [MethodImpl(NI)] static long[] NewLongsL(long n) => new long[n];

    // The address of element 0, and the address one past the object's last element.
    static nuint First<T>(T[] a) => (nuint)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(a));
    static nuint End<T>(T[] a) => First(a) + (nuint)(a.Length * Unsafe.SizeOf<T>());

    static readonly int[] Lengths = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 15, 16, 17, 31, 32, 33, 100, 257 };

    // Allocates arrays of T of every length in a row with other objects between them, fills each
    // with its own pattern, and only then verifies all of them: an overlap shows as a damaged pattern.
    static void AllocRow<T>(string name, int size, int align) where T : unmanaged
    {
        T[][] rows = new T[Lengths.Length][];
        object[] between = new object[Lengths.Length];
        byte[][] odd = new byte[Lengths.Length][];
        for (int k = 0; k < Lengths.Length; k++)
        {
            int n = Lengths[k];
            rows[k] = New<T>(n);
            between[k] = new object();
            odd[k] = NewBytes(k % 7);
            Check(name + " length", rows[k].Length, n);
            Check(name + " element size", Unsafe.SizeOf<T>(), size);
            if (n > 0) Check(name + " alignment", (long)(First(rows[k]) % (nuint)align), 0);
            // A fresh array is zeroed.
            byte* z = (byte*)First(rows[k]);
            int nz = 0;
            for (int i = 0; i < n * size; i++) nz |= z[i];
            Check(name + " zeroed", nz, 0);
        }
        for (int k = 0; k < Lengths.Length; k++)
        {
            byte* p = (byte*)First(rows[k]);
            for (int i = 0; i < Lengths[k] * size; i++) p[i] = (byte)(k * 31 + i * 7 + 1);
            for (int i = 0; i < odd[k].Length; i++) odd[k][i] = (byte)(0xC0 + k);
        }
        for (int k = 0; k < Lengths.Length; k++)
        {
            byte* p = (byte*)First(rows[k]);
            int bad = 0;
            for (int i = 0; i < Lengths[k] * size; i++) if (p[i] != (byte)(k * 31 + i * 7 + 1)) bad++;
            for (int i = 0; i < odd[k].Length; i++) if (odd[k][i] != (byte)(0xC0 + k)) bad++;
            Check(name + " pattern kept", bad, 0);
            Check(name + " length kept", rows[k].Length, Lengths[k]);
            Check(name + " neighbour length kept", odd[k].Length, k % 7);
            if (between[k] == null) Check(name + " neighbour kept", 0, 1);
        }
        // No two arrays share a byte.
        for (int k = 0; k < Lengths.Length; k++)
        {
            for (int j = k + 1; j < Lengths.Length; j++)
            {
                bool apart = End(rows[k]) <= First(rows[j]) - 8 || End(rows[j]) <= First(rows[k]) - 8 ||
                             (Lengths[k] == 0 && Lengths[j] == 0);
                if (!apart) Check(name + " overlap", k * 100 + j, -1);
            }
        }
        checks++;
    }

    [MethodImpl(NI)] static long Get(long[] a, int i) => a[i];
    [MethodImpl(NI)] static void Set(long[] a, int i, long v) { a[i] = v; }
    [MethodImpl(NI)] static int GetB(byte[] a, int i) => a[i];
    [MethodImpl(NI)] static int GetS(short[] a, int i) => a[i];
    [MethodImpl(NI)] static int GetC(char[] a, int i) => a[i];
    [MethodImpl(NI)] static int GetI(int[] a, int i) => a[i];
    [MethodImpl(NI)] static int Get12B(S12[] a, int i) => a[i].B;
    [MethodImpl(NI)] static long Get16B(S16[] a, int i) => a[i].B;
    [MethodImpl(NI)] static long Get24C(S24[] a, int i) => a[i].C;
    [MethodImpl(NI)] static void Set24(S24[] a, int i, long v) { a[i].A = v; a[i].B = (int)v; a[i].C = ~v; }
    [MethodImpl(NI)] static object GetO(object[] a, int i) => a[i];
    [MethodImpl(NI)] static long Const(long[] a) => a[0] + a[1] * 3 + a[7] * 5;
    [MethodImpl(NI)] static int ConstB(byte[] a) => a[0] + a[1] * 3 + a[13] * 5;
    [MethodImpl(NI)] static int ConstS(short[] a) => a[0] + a[1] * 3 + a[13] * 5;
    [MethodImpl(NI)] static int Len(long[] a) => a.Length;
    [MethodImpl(NI)] static long SumRev(long[] a) { long s = 0; for (int i = a.Length - 1; i >= 0; i--) s = s * 3 + a[i]; return s; }
    [MethodImpl(NI)] static int Sum2D(int[,] a) { int s = 0; for (int i = 0; i < a.GetLength(0); i++) for (int j = 0; j < a.GetLength(1); j++) s = s * 3 + a[i, j]; return s; }
    [MethodImpl(NI)] static long LongIndex(long[] a, long i) => a[i];
    [MethodImpl(NI)] static long UIntIndex(long[] a, uint i) => a[i];

    delegate void Act();

    // 0 = no exception, 1 = IndexOutOfRange, 2 = Overflow, 3 = OutOfMemory, 4 = any other.
    static int Throws(Act a)
    {
        try { a(); return 0; }
        catch (IndexOutOfRangeException) { return 1; }
        catch (OverflowException) { return 2; }
        catch (OutOfMemoryException) { return 3; }
        catch (Exception) { return 4; }
    }

    static void TestArrays()
    {
        AllocRow<byte>("byte[]", 1, 1);
        AllocRow<short>("short[]", 2, 2);
        AllocRow<int>("int[]", 4, 4);
        AllocRow<long>("long[]", 8, sizeof(nint));
        AllocRow<S3>("S3[]", 3, 1);
        AllocRow<S6>("S6[]", 6, 2);
        AllocRow<S12>("S12[]", 12, 4);
        AllocRow<S16>("S16[]", 16, sizeof(nint));
        AllocRow<S24>("S24[]", 24, sizeof(nint));
        AllocRow<double>("double[]", 8, sizeof(nint));

        Check("new byte[5]", NewBytes5().Length, 5);
        Check("new short[3]", NewShorts3().Length, 3);
        Check("new long[3]", NewLongs3().Length, 3);
        Check("new S3[5]", NewS3x5().Length, 5);
        Check("new S12[3]", NewS12x3().Length, 3);
        Check("new int[0]", NewInts0().Length, 0);
        Check("new long[(long)9]", NewLongsL(9).Length, 9);

        // Constant-length arrays in a row.
        byte[] b5 = NewBytes5(); short[] s3 = NewShorts3(); long[] l3 = NewLongs3(); S3[] t5 = NewS3x5(); byte[] b5b = NewBytes5();
        for (int i = 0; i < 5; i++) { b5[i] = 0x11; b5b[i] = 0x55; t5[i].A = 0x44; t5[i].B = 0x44; t5[i].C = 0x44; }
        for (int i = 0; i < 3; i++) { s3[i] = 0x2222; l3[i] = 0x3333333333333333L; }
        int sum = 0;
        for (int i = 0; i < 5; i++) sum += b5[i] + b5b[i] + t5[i].A + t5[i].B + t5[i].C;
        Check("constant lengths: bytes", sum, 5 * (0x11 + 0x55 + 3 * 0x44));
        Check("constant lengths: shorts", s3[0] + s3[1] + s3[2], 3 * 0x2222);
        Check("constant lengths: longs", (l3[0] ^ l3[1] ^ l3[2]), 0x3333333333333333L);
        Check("constant lengths: lengths", b5.Length * 10000 + s3.Length * 1000 + l3.Length * 100 + t5.Length * 10 + b5b.Length, 53355);

        // On 32 bits only: the riscv64 ZisK runtime allocates arrays inline and exits on a negative length
        // where .NET throws.
        if (sizeof(nint) == 4)
        {
            Check("negative length", Throws(() => NewBytes(Id(-1))), 2);
            Check("negative long length", Throws(() => NewLongs(Id(-5))), 2);
            Check("negative object length", Throws(() => NewObjects(Id(-1))), 2);
            Check("negative generic length", Throws(() => New<S12>(Id(-1))), 2);
            Check("long length too large", Throws(() => NewLongsL(0x100000000L)), 2);
        }

        // Element access.
        long[] a = NewLongs(16);
        byte[] ab = NewBytes(16);
        short[] ash = New<short>(16);
        char[] ac = New<char>(16);
        int[] ai = New<int>(16);
        S3[] a3 = New<S3>(16);
        S12[] a12 = New<S12>(16);
        S16[] a16 = New<S16>(16);
        S24[] a24 = New<S24>(16);
        object[] ao = NewObjects(16);
        for (int i = 0; i < 16; i++)
        {
            Set(a, i, 0x0101010101010101L * i + 0x100000000L);
            ab[i] = (byte)(200 + i); ash[i] = (short)(-30000 + i); ac[i] = (char)(0xFF00 + i); ai[i] = -1000000 * i;
            a3[i].A = (byte)i; a3[i].B = (byte)(i + 100); a3[i].C = (byte)(i + 200);
            a12[i].A = i; a12[i].B = i * 1000; a12[i].C = -i;
            a16[i].A = i; a16[i].B = 0x100000000L * i + 7;
            Set24(a24, i, 0x100000001L * (i + 1));
            ao[i] = i.ToString();
        }
        for (int i = 0; i < 16; i++)
        {
            Check("long[i]", Get(a, i), 0x0101010101010101L * i + 0x100000000L);
            Check("byte[i]", GetB(ab, i), 200 + i);
            Check("short[i]", GetS(ash, i), -30000 + i);
            Check("char[i]", GetC(ac, i), 0xFF00 + i);
            Check("int[i]", GetI(ai, i), -1000000 * i);
            Check("S3[i] fields", a3[i].A + a3[i].B * 256 + a3[i].C * 65536, i + ((i + 100) & 0xFF) * 256 + ((i + 200) & 0xFF) * 65536);
            Check("S12[i].B", Get12B(a12, i), i * 1000);
            Check("S16[i].B", Get16B(a16, i), 0x100000000L * i + 7);
            Check("S24[i].C", Get24C(a24, i), ~(0x100000001L * (i + 1)));
            Check("S24[i].B", a24[i].B, i + 1);
            CheckS("object[i]", (string)GetO(ao, i), i.ToString());
            Check("long[(long)i]", LongIndex(a, i), 0x0101010101010101L * i + 0x100000000L);
            Check("long[(uint)i]", UIntIndex(a, (uint)i), 0x0101010101010101L * i + 0x100000000L);
        }
        Check("constant indices long", Const(a), a[0] + a[1] * 3 + a[7] * 5);
        Check("constant indices byte", ConstB(ab), 200 + 201 * 3 + 213 * 5);
        Check("constant indices short", ConstS(ash), -30000 + -29999 * 3 + -29987 * 5);
        Check("length", Len(a), 16);
        long rev = 0;
        for (int i = 15; i >= 0; i--) rev = rev * 3 + (0x0101010101010101L * i + 0x100000000L);
        Check("sum reversed", SumRev(a), rev);

        Check("index -1", Throws(() => Get(a, Id(-1))), 1);
        Check("index length", Throws(() => Get(a, Id(16))), 1);
        Check("index min", Throws(() => GetB(ab, Id(int.MinValue))), 1);
        Check("store index length", Throws(() => Set(a, Id(16), 1)), 1);
        // A long index is converted to a native int with an overflow check: on 32 bits one that does
        // not fit throws OverflowException before the bounds check.
        int tooWide = sizeof(nint) == 4 ? 2 : 1;
        Check("long index 2^32", Throws(() => LongIndex(a, 0x100000000L)), tooWide);
        Check("long index 2^32 + 1", Throws(() => LongIndex(a, 0x100000001L)), tooWide);
        Check("long index negative", Throws(() => LongIndex(a, -0x100000000L)), tooWide);
        Check("long index -1", Throws(() => LongIndex(a, -1L)), 1);
        Check("long index length", Throws(() => LongIndex(a, 16L)), 1);
        Check("uint index", Throws(() => UIntIndex(a, 0xFFFFFFFFu)), 1);
        Check("empty index 0", Throws(() => GetI(NewInts0(), Id(0))), 1);

        int[,] m = New2D(3, 5);
        int e2 = 0;
        for (int i = 0; i < 3; i++) for (int j = 0; j < 5; j++) { m[i, j] = i * 10 + j; e2 = e2 * 3 + i * 10 + j; }
        Check("2D sum", Sum2D(m), e2);
        Check("2D length", m.Length, 15);
        Check("2D index", Throws(() => { m[Id(3), 0] = 1; }), 1);

        // Clear, Fill, Copy, Resize.
        Array.Fill(a, -1L, 3, 5);
        Check("fill before", a[2], 0x0101010101010101L * 2 + 0x100000000L);
        Check("fill first", a[3], -1); Check("fill last", a[7], -1);
        Check("fill after", a[8], 0x0101010101010101L * 8 + 0x100000000L);
        Array.Clear(a, 4, 2);
        Check("clear", a[3] * 100 + a[4] * 10 + a[5] + a[6], -100 - 1);
        long[] c = NewLongs(20);
        Array.Copy(a, 0, c, 2, 16);
        Check("copy first", c[2], a[0]); Check("copy last", c[17], a[15]); Check("copy before", c[1], 0); Check("copy after", c[18], 0);
        Array.Resize(ref c, 40);
        Check("resize", c.Length * 1000 + (c[17] == a[15] ? 1 : 0) + (int)c[39], 40001);
        Group("arrays");
    }

    // ================= unaligned access =================

    [MethodImpl(NI)] static void W<T>(byte* p, T v) where T : unmanaged { Unsafe.WriteUnaligned(p, v); }
    [MethodImpl(NI)] static T Rd<T>(byte* p) where T : unmanaged => Unsafe.ReadUnaligned<T>(p);
    [MethodImpl(NI)] static void WRef<T>(ref byte p, T v) where T : unmanaged { Unsafe.WriteUnaligned(ref p, v); }
    [MethodImpl(NI)] static T RdRef<T>(ref byte p) where T : unmanaged => Unsafe.ReadUnaligned<T>(ref p);

    // Fixed offsets: the offset is a constant in the address.
    [MethodImpl(NI)] static void WInt1(byte* p, int v) { Unsafe.WriteUnaligned(p + 1, v); }
    [MethodImpl(NI)] static void WInt2(byte* p, int v) { Unsafe.WriteUnaligned(p + 2, v); }
    [MethodImpl(NI)] static void WInt3(byte* p, int v) { Unsafe.WriteUnaligned(p + 3, v); }
    [MethodImpl(NI)] static void WLong1(byte* p, long v) { Unsafe.WriteUnaligned(p + 1, v); }
    [MethodImpl(NI)] static void WLong4(byte* p, long v) { Unsafe.WriteUnaligned(p + 4, v); }
    [MethodImpl(NI)] static void WLong7(byte* p, long v) { Unsafe.WriteUnaligned(p + 7, v); }
    [MethodImpl(NI)] static void WShort1(byte* p, short v) { Unsafe.WriteUnaligned(p + 1, v); }
    [MethodImpl(NI)] static void WS4At1(byte* p, S4 v) { Unsafe.WriteUnaligned(p + 1, v); }
    [MethodImpl(NI)] static void WSPairAt1(byte* p, SPair v) { Unsafe.WriteUnaligned(p + 1, v); }
    [MethodImpl(NI)] static void WS2At1(byte* p, S2 v) { Unsafe.WriteUnaligned(p + 1, v); }
    [MethodImpl(NI)] static int RInt1(byte* p) => Unsafe.ReadUnaligned<int>(p + 1);
    [MethodImpl(NI)] static long RLong3(byte* p) => Unsafe.ReadUnaligned<long>(p + 3);
    [MethodImpl(NI)] static short RShort1(byte* p) => Unsafe.ReadUnaligned<short>(p + 1);
    [MethodImpl(NI)] static ushort RUShort3(byte* p) => Unsafe.ReadUnaligned<ushort>(p + 3);
    [MethodImpl(NI)] static void CopyS4(byte* d, byte* s) { Unsafe.WriteUnaligned(d + 1, Unsafe.ReadUnaligned<S4>(s + 3)); }
    [MethodImpl(NI)] static void CopyBlock11(byte* d, byte* s) { Unsafe.CopyBlockUnaligned(d, s, 11); }
    [MethodImpl(NI)] static void CopyBlock4(byte* d, byte* s) { Unsafe.CopyBlockUnaligned(d, s, 4); }
    [MethodImpl(NI)] static void InitBlock7(byte* d, byte v) { Unsafe.InitBlockUnaligned(d, v, 7); }
    [MethodImpl(NI)] static void InitBlock4(byte* d, byte v) { Unsafe.InitBlockUnaligned(d, v, 4); }

    [MethodImpl(NI)] static void PackedWrite(Packed* p, int i, long l, short s) { p->I = i; p->L = l; p->S = s; p->Inner.V = ~i; p->Tag = 0x7E; p->Tail = 0x7F; }
    [MethodImpl(NI)] static long PackedRead(Packed* p) => p->I + p->L + p->S + p->Inner.V + p->Tag + p->Tail;
    [MethodImpl(NI)] static void PackedCopyInner(Packed* p, Packed* q) { p->Inner = q->Inner; p->I = q->I; }
    [MethodImpl(NI)] static Packed PackedMake(int i, long l) { Packed p = default; p.I = i; p.L = l; p.S = -2; p.Inner.V = i * 3; p.Tag = 1; p.Tail = 2; return p; }
    [MethodImpl(NI)] static long PackedLocal(int i, long l) { Packed p = PackedMake(i, l); Packed q = p; q.L += q.I; return q.L + q.S + q.Inner.V + q.Tag + q.Tail; }

    static void Fill(byte* p, int n) { for (int i = 0; i < n; i++) p[i] = (byte)(0xE0 + i); }

    // The bytes at p[off..off+size) are the little-endian bytes of v, and nothing else changed.
    static void Bytes(string name, byte* p, int n, int off, int size, ulong v)
    {
        int bad = 0;
        for (int i = 0; i < n; i++)
        {
            byte exp = (i >= off && i < off + size) ? (byte)(v >> (8 * (i - off))) : (byte)(0xE0 + i);
            if (p[i] != exp) bad++;
        }
        Check(name, bad, 0);
    }

    static void TestUnaligned()
    {
        byte* raw = stackalloc byte[48];
        byte* buf = (byte*)(((nuint)raw + 7) & ~(nuint)7);
        byte* src = buf + 24;
        ulong V = 0x8877665544332291UL;

        for (int off = 0; off < 8; off++)
        {
            Fill(buf, 24); W<short>(buf + off, unchecked((short)V)); Bytes("write short", buf, 24, off, 2, V);
            Check("read short", Rd<short>(buf + off), unchecked((short)V));
            Check("read ushort", Rd<ushort>(buf + off), (ushort)V);
            Fill(buf, 24); W<int>(buf + off, unchecked((int)V)); Bytes("write int", buf, 24, off, 4, V);
            Check("read int", Rd<int>(buf + off), unchecked((int)V));
            Check("read uint", Rd<uint>(buf + off), (uint)V);
            Fill(buf, 24); W<long>(buf + off, unchecked((long)V)); Bytes("write long", buf, 24, off, 8, V);
            Check("read long", Rd<long>(buf + off), unchecked((long)V));
            Fill(buf, 24); WRef<long>(ref buf[off], unchecked((long)V)); Bytes("write long ref", buf, 24, off, 8, V);
            Check("read long ref", RdRef<long>(ref buf[off]), unchecked((long)V));
            Fill(buf, 24); WRef<int>(ref buf[off], unchecked((int)V)); Bytes("write int ref", buf, 24, off, 4, V);
            Check("read char", Rd<char>(buf + off), (char)V);

            // Structs of every size up to 8 and of 12 and 16 bytes.
            Fill(buf, 24); W(buf + off, new S2 { A = 0x91, B = 0x22 }); Bytes("write S2", buf, 24, off, 2, V);
            Fill(buf, 24); W(buf + off, new S3 { A = 0x91, B = 0x22, C = 0x33 }); Bytes("write S3", buf, 24, off, 3, V);
            Fill(buf, 24); W(buf + off, new S4 { A = 0x91, B = 0x22, C = 0x33, D = 0x44 }); Bytes("write S4", buf, 24, off, 4, V);
            Fill(buf, 24); W(buf + off, new SPair { A = 0x2291, B = 0x4433 }); Bytes("write SPair", buf, 24, off, 4, V);
            Fill(buf, 24); W(buf + off, new S6 { A = 0x2291, B = 0x4433, C = 0x6655 }); Bytes("write S6", buf, 24, off, 6, V);
            S3 r3 = Rd<S3>(buf + off);
            Check("read S3", r3.A + (r3.B << 8) + (r3.C << 16), (int)(V & 0xFFFFFF));

            Fill(buf, 24);
            W(buf + off, new S12 { A = 0x44332291, B = unchecked((int)0x88776655), C = 0x0C0B0A09 });
            Bytes("write S12 low", buf, 12 + off - 4, off, 8, V);
            Check("read S12", Rd<S12>(buf + off).C, 0x0C0B0A09);
            Check("write S12 after", buf[off + 12], 0xE0 + off + 12);
            Fill(buf, 24);
            W(buf + off, new S16 { A = unchecked((long)V), B = 0x100F0E0D0C0B0A09L });
            Check("read S16 A", Rd<S16>(buf + off).A, unchecked((long)V));
            Check("read S16 B", Rd<S16>(buf + off).B, 0x100F0E0D0C0B0A09L);
            Check("write S16 before", off == 0 ? 0 : buf[off - 1] - (0xE0 + off - 1), 0);
            Check("write S16 after", buf[off + 16], 0xE0 + off + 16);

            // BinaryPrimitives and BitConverter go through the same reads.
            Fill(buf, 24); W<long>(buf + off, unchecked((long)V));
            Span<byte> span = new Span<byte>(buf + off, 8);
            Check("ReadInt64LittleEndian", BinaryPrimitives.ReadInt64LittleEndian(span), unchecked((long)V));
            Check("ReadUInt32BigEndian", BinaryPrimitives.ReadUInt32BigEndian(span), 0x91223344u);
            Check("ReadInt16LittleEndian", BinaryPrimitives.ReadInt16LittleEndian(span), unchecked((short)V));
            Check("BitConverter.ToInt64", BitConverter.ToInt64(span), unchecked((long)V));
            Check("MemoryMarshal.Read", MemoryMarshal.Read<int>(span), unchecked((int)V));
            BinaryPrimitives.WriteUInt64BigEndian(span, V);
            Check("WriteUInt64BigEndian", buf[off] * 256 + buf[off + 7], 0x88 * 256 + 0x91);
        }

        Fill(buf, 24); WInt1(buf, unchecked((int)V)); Bytes("int at +1", buf, 24, 1, 4, V);
        Check("int from +1", RInt1(buf), unchecked((int)V));
        Fill(buf, 24); WInt2(buf, unchecked((int)V)); Bytes("int at +2", buf, 24, 2, 4, V);
        Fill(buf, 24); WInt3(buf, unchecked((int)V)); Bytes("int at +3", buf, 24, 3, 4, V);
        Fill(buf, 24); WLong1(buf, unchecked((long)V)); Bytes("long at +1", buf, 24, 1, 8, V);
        Fill(buf, 24); WLong4(buf, unchecked((long)V)); Bytes("long at +4", buf, 24, 4, 8, V);
        Fill(buf, 24); WLong7(buf, unchecked((long)V)); Bytes("long at +7", buf, 24, 7, 8, V);
        Fill(buf, 24); WShort1(buf, unchecked((short)V)); Bytes("short at +1", buf, 24, 1, 2, V);
        Check("short from +1", RShort1(buf), unchecked((short)V));
        Fill(buf, 24); WS4At1(buf, new S4 { A = 0x91, B = 0x22, C = 0x33, D = 0x44 }); Bytes("S4 at +1", buf, 24, 1, 4, V);
        Fill(buf, 24); WInt3(buf, unchecked((int)V)); Check("ushort from +3", RUShort3(buf), (ushort)V);
        Fill(buf, 24); WSPairAt1(buf, new SPair { A = 0x2291, B = 0x4433 }); Bytes("SPair at +1", buf, 24, 1, 4, V);
        Fill(buf, 24); WS2At1(buf, new S2 { A = 0x91, B = 0x22 }); Bytes("S2 at +1", buf, 24, 1, 2, V);
        Fill(buf, 24); WLong1(buf, unchecked((long)V) << 16); Check("long from +3", RLong3(buf), (unchecked((long)V) & 0xFFFFFFFFFFFFL) | (0xE9L << 48) | unchecked((long)(0xEAUL << 56)));

        Fill(buf, 24); for (int i = 0; i < 16; i++) src[i] = (byte)(0x91 + i * 0x11);
        CopyS4(buf, src); Bytes("copy S4 +3 -> +1", buf, 24, 1, 4, (ulong)src[3] | ((ulong)src[4] << 8) | ((ulong)src[5] << 16) | ((ulong)src[6] << 24));
        Fill(buf, 24); CopyBlock4(buf + 1, src + 2); Bytes("copy block 4", buf, 24, 1, 4, (ulong)src[2] | ((ulong)src[3] << 8) | ((ulong)src[4] << 16) | ((ulong)src[5] << 24));
        Fill(buf, 24); CopyBlock11(buf + 3, src + 1);
        int bad = 0;
        for (int i = 0; i < 24; i++) if (buf[i] != ((i >= 3 && i < 14) ? src[i - 2] : (byte)(0xE0 + i))) bad++;
        Check("copy block 11", bad, 0);
        Fill(buf, 24); InitBlock7(buf + 1, 0x5A); Bytes("init block 7", buf, 24, 1, 7, 0x5A5A5A5A5A5A5AUL);
        Fill(buf, 24); InitBlock4(buf + 3, 0x5A); Bytes("init block 4", buf, 24, 3, 4, 0x5A5A5A5AUL);

        // A packed struct: I at 1, L at 5, S at 13, Inner at 15, Tail at 19.
        Check("sizeof(Packed)", sizeof(Packed), 20);
        Packed* pk = (Packed*)buf;
        Fill(buf, 24);
        PackedWrite(pk, 0x44332291, unchecked((long)V), 0x1234);
        Check("packed tag", buf[0], 0x7E);
        Bytes("packed I", buf + 1, 4, 0, 4, V);
        Bytes("packed L", buf + 5, 8, 0, 8, V);
        Check("packed S", buf[13] + buf[14] * 256, 0x1234);
        Bytes("packed Inner", buf + 15, 4, 0, 4, ~V);
        Check("packed tail", buf[19], 0x7F);
        Check("packed after", buf[20], 0xE0 + 20);
        Check("packed read", PackedRead(pk), 0x44332291L + unchecked((long)V) + 0x1234 + ~0x44332291 + 0x7E + 0x7F);
        Packed other = PackedMake(77, 0x100000000L);
        PackedCopyInner(pk, &other);
        Check("packed copy inner", pk->Inner.V * 1000 + pk->I, 231 * 1000 + 77);
        Check("packed copy kept L", pk->L, unchecked((long)V));
        Check("packed local", PackedLocal(5, 0x1FFFFFFFFL), 0x1FFFFFFFFL + 5 - 2 + 15 + 1 + 2);
        Group("unaligned");
    }

    // ================= block copies =================

    [MethodImpl(NI)] static void Copy(byte* d, byte* s, int n) { Buffer.MemoryCopy(s, d, n, n); }
    [MethodImpl(NI)] static void SpanCopy(byte* d, byte* s, int n) { new Span<byte>(s, n).CopyTo(new Span<byte>(d, n)); }
    [MethodImpl(NI)] static void Clear(byte* d, int n) { new Span<byte>(d, n).Clear(); }
    [MethodImpl(NI)] static void FillSpan(byte* d, int n, byte v) { new Span<byte>(d, n).Fill(v); }
    [MethodImpl(NI)] static S24 CopyStruct(S24* p) => *p;
    [MethodImpl(NI)] static void StoreStruct(S24* p, S24 v) { *p = v; }
    [MethodImpl(NI)] static void CopyBig(Big64* d, Big64* s) { *d = *s; }

    struct Big64 { public long A, B, C, D, E, F, G, H; }

    static void TestCopies()
    {
        byte[] srcArr = new byte[320];
        byte[] dstArr = new byte[320];
        fixed (byte* s0 = srcArr, d0 = dstArr)
        {
            for (int i = 0; i < 320; i++) s0[i] = (byte)(i * 13 + 5);
            int bad = 0, badSpan = 0, badClear = 0, badFill = 0, badMove = 0;
            for (int n = 0; n <= 130; n += (n < 40 ? 1 : 9))
            {
                for (int sa = 0; sa < 8; sa += (n < 20 ? 1 : 3))
                {
                    for (int da = 0; da < 8; da += (n < 20 ? 1 : 3))
                    {
                        byte* s = s0 + 16 + sa, d = d0 + 16 + da;
                        for (int i = 0; i < 180; i++) d0[i] = 0xEE;
                        Copy(d, s, n);
                        for (int i = 0; i < 180; i++)
                        {
                            byte exp = (i >= 16 + da && i < 16 + da + n) ? s[i - 16 - da] : (byte)0xEE;
                            if (d0[i] != exp) bad++;
                        }
                        for (int i = 0; i < 180; i++) d0[i] = 0xEE;
                        SpanCopy(d, s, n);
                        for (int i = 0; i < 180; i++)
                        {
                            byte exp = (i >= 16 + da && i < 16 + da + n) ? s[i - 16 - da] : (byte)0xEE;
                            if (d0[i] != exp) badSpan++;
                        }
                        Clear(d, n);
                        for (int i = 0; i < 180; i++)
                        {
                            byte exp = (i >= 16 + da && i < 16 + da + n) ? (byte)0 : (byte)0xEE;
                            if (d0[i] != exp) badClear++;
                        }
                        FillSpan(d, n, 0x3C);
                        for (int i = 0; i < 180; i++)
                        {
                            byte exp = (i >= 16 + da && i < 16 + da + n) ? (byte)0x3C : (byte)0xEE;
                            if (d0[i] != exp) badFill++;
                        }

                        // Overlapping, both directions: shift by sa - 4 bytes within one buffer.
                        int shift = sa - 4;
                        for (int i = 0; i < 180; i++) d0[i] = (byte)(i * 7 + 1);
                        Copy(d0 + 20 + shift, d0 + 20, n);
                        for (int i = 0; i < 180; i++)
                        {
                            byte exp = (i >= 20 + shift && i < 20 + shift + n) ? (byte)((i - shift) * 7 + 1) : (byte)(i * 7 + 1);
                            if (d0[i] != exp) badMove++;
                        }
                    }
                }
            }
            Check("MemoryCopy", bad, 0);
            Check("Span.CopyTo", badSpan, 0);
            Check("Span.Clear", badClear, 0);
            Check("Span.Fill", badFill, 0);
            Check("overlapping MemoryCopy", badMove, 0);
        }

        S24 v = new S24 { A = 0x1111111122222222L, B = 0x33333333, C = 0x4444444455555555L };
        S24* two = stackalloc S24[2];
        StoreStruct(two + 1, v);
        S24 back = CopyStruct(two + 1);
        Check("struct copy A", back.A, v.A); Check("struct copy B", back.B, v.B); Check("struct copy C", back.C, v.C);
        Big64 big = new Big64 { A = 1, B = 0x200000000L, C = 3, D = 0x400000000L, E = 5, F = 0x600000000L, G = 7, H = long.MinValue };
        Big64 copy = default;
        CopyBig(&copy, &big);
        Check("big struct copy", copy.A + copy.B + copy.C + copy.D + copy.E + copy.F + copy.G, 1 + 3 + 5 + 7 + 0xC00000000L);
        Check("big struct copy last", copy.H, long.MinValue);
        Group("block copies");
    }

    // ================= enums =================

    [MethodImpl(NI)] static string Str<T>(T e) where T : struct, Enum => e.ToString();
    [MethodImpl(NI)] static bool Has(Bits a, Bits b) => a.HasFlag(b);
    [MethodImpl(NI)] static bool HasWide(WideBits a, WideBits b) => (a & b) == b;
    [MethodImpl(NI)] static bool WideLess(Wide a, Wide b) => a < b;
    [MethodImpl(NI)] static bool WideULess(WideU a, WideU b) => a < b;
    [MethodImpl(NI)] static long WideToLong(Wide a) => (long)a;
    [MethodImpl(NI)] static Wide LongToWide(long a) => (Wide)a;
    [MethodImpl(NI)] static int SwitchWide(Wide a) { switch (a) { case Wide.Low: return 1; case Wide.High: return 2; case Wide.Top: return 3; default: return 0; } }
    [MethodImpl(NI)] static bool EqualsBoxed(object a, object b) => a.Equals(b);
    [MethodImpl(NI)] static int Hash<T>(T e) where T : struct, Enum => e.GetHashCode();

    static void TestEnums()
    {
        bool named = Str(Color.Green) == "Green";
        Console.WriteLine(named ? "enum names: present" : "enum names: absent, numbers expected");

        CheckS("Color.Green", Str(Color.Green), named ? "Green" : "1");
        CheckS("Color.Blue", Str(Color.Blue), named ? "Blue" : "2");
        CheckS("(Color)7", Str((Color)7), "7");
        CheckS("(Color)-1", Str((Color)(-1)), "-1");
        CheckS("Small.Max", Str(Small.Max), named ? "Max" : "255");
        CheckS("(Small)200", Str((Small)200), "200");
        CheckS("Neg.Minus", Str(Neg.Minus), named ? "Minus" : "-1");
        CheckS("(Neg)-128", Str((Neg)(-128)), "-128");
        CheckS("Wide.High", Str(Wide.High), named ? "High" : "4294967296");
        CheckS("Wide.Top", Str(Wide.Top), named ? "Top" : "-9223372036854775808");
        CheckS("(Wide)0x100000001", Str((Wide)0x100000001L), "4294967297");
        CheckS("(Wide)-2", Str((Wide)(-2)), "-2");
        CheckS("WideU.Top", Str(WideU.Top), named ? "Top" : "9223372036854775808");
        CheckS("WideU.All", Str(WideU.All), named ? "All" : "18446744073709551615");
        CheckS("(WideU)0x1FFFFFFFF", Str((WideU)0x1FFFFFFFFUL), "8589934591");
        CheckS("Bits.A | Bits.C", Str(Bits.A | Bits.C), named ? "A, C" : "5");
        CheckS("Bits.D | Bits.B", Str(Bits.D | Bits.B), named ? "B, D" : "1073741826");
        CheckS("(Bits)8", Str((Bits)8), "8");
        CheckS("WideBits both", Str(WideBits.Lo | WideBits.Hi), named ? "Lo, Hi" : "9223372036854775809");
        CheckS("Color format D", Color.Blue.ToString("D"), "2");
        CheckS("Wide format X", Wide.High.ToString("X"), "0000000100000000");
        CheckS("Small format X", Small.Max.ToString("X"), "FF");

        Check("HasFlag yes", Has(Bits.A | Bits.D, Bits.D) ? 1 : 0, 1);
        Check("HasFlag no", Has(Bits.A | Bits.D, Bits.B) ? 1 : 0, 0);
        Check("wide flag high", HasWide(WideBits.Hi, WideBits.Hi) ? 1 : 0, 1);
        Check("wide flag low only", HasWide(WideBits.Lo, WideBits.Hi) ? 1 : 0, 0);
        Check("wide flag both", HasWide(WideBits.Lo | WideBits.Hi, WideBits.Lo | WideBits.Hi) ? 1 : 0, 1);
        Check("wide less", WideLess(Wide.Top, Wide.Low) ? 1 : 0, 1);
        Check("wide less 2", WideLess(Wide.High, Wide.Low) ? 1 : 0, 0);
        Check("wide unsigned less", WideULess(WideU.Top, WideU.Low) ? 1 : 0, 0);
        Check("wide unsigned less 2", WideULess(WideU.High, WideU.All) ? 1 : 0, 1);
        Check("wide to long", WideToLong(Wide.High), 0x100000000L);
        Check("long to wide", (long)LongToWide(long.MinValue), long.MinValue);
        Check("switch wide", SwitchWide(Wide.Low) * 1000 + SwitchWide(Wide.High) * 100 + SwitchWide(Wide.Top) * 10 + SwitchWide((Wide)2), 1230);
        Check("boxed equals", EqualsBoxed(Wide.High, Wide.High) ? 1 : 0, 1);
        Check("boxed differs in high half", EqualsBoxed((Wide)0x100000001L, (Wide)0x200000001L) ? 1 : 0, 0);
        Check("boxed differs in type", EqualsBoxed(Color.Green, Bits.A) ? 1 : 0, 0);
        Check("hash equal", Hash(Wide.High) == Hash((Wide)0x100000000L) ? 1 : 0, 1);
        Check("hash of int enum", Hash(Color.Blue), 2);
        Group("enums");
    }

    static int Main()
    {
        TestArrays();
        TestUnaligned();
        TestCopies();
        TestEnums();

        if (fails == 0)
        {
            Console.WriteLine("ALL PASS");
            return 0;
        }
        Console.WriteLine("FAIL " + fails.ToString());
        return 1;
    }
}
