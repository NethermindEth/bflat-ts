// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// A load whose address is formed by an addi right before it.
//
// An array element is read as "address of the array plus the scaled index,
// plus the offset of the first element, then load at 0". When the load
// writes the register that holds the address, the address is dead after it,
// and with JitRiscV64FoldIndexOffset the emitter removes the addi and puts
// its constant into the load. The same happens for any in-place addi that
// is followed by a load at 0 through the same register into the same
// register. What can go wrong: an addi that is removed although the load
// does not use it (another register, another instruction between them, a
// label between them), a constant that is read wrongly from the removed
// instruction (negative, 2047, -2048, the low part of a larger constant), a
// load of another width or signedness, a removed addi whose register is
// read again, and an object reference loaded through an address the
// collector no longer sees.
//
// The methods read arrays of every element type, fields at small and large
// offsets, pointers at positive and negative displacements and nested
// objects, return the value in the register the address was in, and are
// checked against literals and against values written by code compiled
// without optimization.
//
// A failed check prints one line; the run ends with ALL PASS or a FAIL
// count, and the guest returns 1 when anything failed.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

class Node
{
    public long A;
    public Node Next;
    public int[] Ints;
    public string Text;
    public int I;
    public short S;
    public byte B;
}

[StructLayout(LayoutKind.Sequential)]
unsafe struct Large
{
    public fixed long Head[254];        // 2032 bytes
    public long At2032;
    public long At2040;                 // the last offset an addi reaches
    public long At2048;
    public long At2056;
    public int At2064;
}

struct Elem { public long X; public int Y; public short Z; public byte W; }

unsafe class Program
{
    const MethodImplOptions NI = MethodImplOptions.NoInlining;
    const MethodImplOptions NoOpt = MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization;

    static int fails;
    static int checks;
    static int shown;

    // ---- arrays ----

    [MethodImpl(NI)] static long ALong(long[] a, int i) => a[i];
    [MethodImpl(NI)] static ulong AULong(ulong[] a, int i) => a[i];
    [MethodImpl(NI)] static int AInt(int[] a, int i) => a[i];
    [MethodImpl(NI)] static uint AUInt(uint[] a, int i) => a[i];
    [MethodImpl(NI)] static short AShort(short[] a, int i) => a[i];
    [MethodImpl(NI)] static ushort AUShort(ushort[] a, int i) => a[i];
    [MethodImpl(NI)] static sbyte ASByte(sbyte[] a, int i) => a[i];
    [MethodImpl(NI)] static byte AByte(byte[] a, int i) => a[i];
    [MethodImpl(NI)] static char AChar(char[] a, int i) => a[i];
    [MethodImpl(NI)] static bool ABool(bool[] a, int i) => a[i];
    [MethodImpl(NI)] static nint ANint(nint[] a, int i) => a[i];
    [MethodImpl(NI)] static string AString(string[] a, int i) => a[i];
    [MethodImpl(NI)] static Node ANode(Node[] a, int i) => a[i];
    [MethodImpl(NI)] static long AElemX(Elem[] a, int i) => a[i].X;
    [MethodImpl(NI)] static int AElemY(Elem[] a, int i) => a[i].Y;
    [MethodImpl(NI)] static short AElemZ(Elem[] a, int i) => a[i].Z;
    [MethodImpl(NI)] static byte AElemW(Elem[] a, int i) => a[i].W;
    [MethodImpl(NI)] static long ALongU(long[] a, uint i) => a[i];
    [MethodImpl(NI)] static long ALongL(long[] a, long i) => a[i];
    [MethodImpl(NI)] static long AConst(long[] a) => a[0] + a[1] * 3 + a[5] * 5;
    [MethodImpl(NI)] static long AWiden(int[] a, int i) => (long)a[i] + (long)(uint)a[i + 1] * 3;
    [MethodImpl(NI)] static char SChar(string s, int i) => s[i];
    [MethodImpl(NI)] static long SpanElem(ReadOnlySpan<long> s, int i) => s[i];
    [MethodImpl(NI)] static int SpanByte(ReadOnlySpan<byte> s, int i) => s[i];
    [MethodImpl(NI)] static long Md(long[,] a, int i, int j) => a[i, j];
    [MethodImpl(NI)] static int Jagged(int[][] a, int i, int j) => a[i][j];
    [MethodImpl(NI)] static int Length(int[] a) => a.Length;
    [MethodImpl(NI)] static int TextLength(Node n) => n.Text.Length;
    [MethodImpl(NI)] static int IntsLength(Node n) => n.Ints.Length;

    // The index is used again after the load, and so is the array.
    [MethodImpl(NI)] static long AReuse(long[] a, int i) => a[i] + i + a.Length;
    [MethodImpl(NI)] static long ATwo(long[] a, int i, int j) => a[i] * 3 + a[j];
    [MethodImpl(NI)] static long ASame(long[] a, int i) => a[i] + a[i] * a[i];
    [MethodImpl(NI)] static long AChain(long[] a, int i) => a[(int)a[(int)a[i]]];
    [MethodImpl(NI)] static long AStore(long[] a, int i) { long v = a[i]; a[i] = v + 1; return v * 3 + a[i]; }

    [MethodImpl(NI)]
    static long ASum(long[] a)
    {
        long s = 0;
        for (int i = 0; i < a.Length; i++) s = s * 3 + a[i];
        return s;
    }

    [MethodImpl(NI)]
    static long ASumBack(int[] a, short[] b, byte[] c)
    {
        long s = 0;
        for (int i = a.Length - 1; i >= 0; i--) s = s * 5 + a[i] + b[i] + c[i];
        return s;
    }

    // The same loads compiled without optimization, where the element offset is always a separate addi.
    [MethodImpl(NoOpt)] static long NLong(long[] a, int i) => a[i];
    [MethodImpl(NoOpt)] static ulong NULong(ulong[] a, int i) => a[i];
    [MethodImpl(NoOpt)] static int NInt(int[] a, int i) => a[i];
    [MethodImpl(NoOpt)] static uint NUInt(uint[] a, int i) => a[i];
    [MethodImpl(NoOpt)] static short NShort(short[] a, int i) => a[i];
    [MethodImpl(NoOpt)] static ushort NUShort(ushort[] a, int i) => a[i];
    [MethodImpl(NoOpt)] static sbyte NSByte(sbyte[] a, int i) => a[i];
    [MethodImpl(NoOpt)] static byte NByte(byte[] a, int i) => a[i];
    [MethodImpl(NoOpt)] static char NChar(char[] a, int i) => a[i];
    [MethodImpl(NoOpt)] static bool NBool(bool[] a, int i) => a[i];
    [MethodImpl(NoOpt)] static string NString(string[] a, int i) => a[i];
    [MethodImpl(NoOpt)] static Node NNode(Node[] a, int i) => a[i];
    [MethodImpl(NoOpt)] static long NElemX(Elem[] a, int i) => a[i].X;
    [MethodImpl(NoOpt)] static int NElemY(Elem[] a, int i) => a[i].Y;
    [MethodImpl(NoOpt)] static short NElemZ(Elem[] a, int i) => a[i].Z;
    [MethodImpl(NoOpt)] static byte NElemW(Elem[] a, int i) => a[i].W;
    [MethodImpl(NoOpt)] static long NReuse(long[] a, int i) => a[i] + i + a.Length;
    [MethodImpl(NoOpt)] static long NSame(long[] a, int i) => a[i] + a[i] * a[i];
    [MethodImpl(NoOpt)] static long NChain(long[] a, int i) => a[(int)a[(int)a[i]]];
    [MethodImpl(NoOpt)] static long NStore(long[] a, int i) { long v = a[i]; a[i] = v + 1; return v * 3 + a[i]; }
    [MethodImpl(NoOpt)] static long NWiden(int[] a, int i) => (long)a[i] + (long)(uint)a[i + 1] * 3;
    [MethodImpl(NoOpt)] static int NJagged(int[][] a, int i, int j) => a[i][j];
    [MethodImpl(NoOpt)] static long NPointer(long* p, int i) => p[i] + *(p + 1) * 3 + p[-2] * 5;
    [MethodImpl(NoOpt)] static long NFields(Node n) => n.Next.A * 3 + n.Next.Ints[1] + n.Text[1];

    [MethodImpl(NoOpt)]
    static long NSum(long[] a, int[] b, short[] c, byte[] d)
    {
        long s = 0;
        for (int i = 0; i < a.Length; i++) s = s * 3 + a[i] + b[i] + c[i] + d[i];
        return s;
    }

    // The address is a local of its own, changed in place and dead after the load.
    [MethodImpl(NI)] static long QStep(long* p, long k) { long* q = p + 1; long x = k; return *q + x; }
    [MethodImpl(NI)] static long QStepNeg(long* p, long k) { long* q = p - 3; long x = k; return *q ^ x; }
    [MethodImpl(NI)] static int QStepInt(int* p, long k) { int* q = p + 3; long x = k; return *q + (int)x; }
    [MethodImpl(NI)] static long QRef(ref long r, long k) { ref long q = ref Unsafe.Add(ref r, 2); long x = k; return q + x; }
    [MethodImpl(NI)] static long QTwo(long* p, long* o) { long* q = p + 1; long* t = o + 2; return *q * 3 + *t; }
    [MethodImpl(NI)] static long QFixed(long[] a) { fixed (long* p = &a[1]) { long* q = p + 1; return *q; } }
    [MethodImpl(NI)] static long QCond(long* p, bool f) { long* q = p; if (f) q += 2; q += 1; return *q; }

    // ---- fields and nested objects ----

    [MethodImpl(NI)] static long FA(Node n) => n.A;
    [MethodImpl(NI)] static Node FNext(Node n) => n.Next;
    [MethodImpl(NI)] static long FNextA(Node n) => n.Next.A;
    [MethodImpl(NI)] static long FNextNextA(Node n) => n.Next.Next.A;
    [MethodImpl(NI)] static int FInts(Node n, int i) => n.Ints[i];
    [MethodImpl(NI)] static int FNextInts(Node n, int i) => n.Next.Ints[i];
    [MethodImpl(NI)] static char FText(Node n, int i) => n.Text[i];
    [MethodImpl(NI)] static long FSmall(Node n) => (long)n.I * 65536 + n.S * 256L + n.B;
    [MethodImpl(NI)] static long FNextSmall(Node n) => (long)n.Next.I * 65536 + n.Next.S * 256L + n.Next.B;

    [MethodImpl(NI)]
    static long Walk(Node n)
    {
        long s = 0;
        while (n != null)
        {
            s = s * 7 + n.A + n.Ints[1];
            n = n.Next;
        }
        return s;
    }

    // ---- pointers: the displacement is added to the pointer in place ----

    [MethodImpl(NI)] static long P8(long* p) => *(p + 1);
    [MethodImpl(NI)] static long P16(long* p) => p[2];
    [MethodImpl(NI)] static long PNeg8(long* p) => *(p - 1);
    [MethodImpl(NI)] static long PNeg16(long* p) => p[-2];
    [MethodImpl(NI)] static long P2040(long* p) => p[255];
    [MethodImpl(NI)] static long P2048(long* p) => p[256];
    [MethodImpl(NI)] static long PNeg2048(long* p) => p[-256];
    [MethodImpl(NI)] static long PNeg2056(long* p) => p[-257];
    [MethodImpl(NI)] static long P4096(long* p) => p[512];
    [MethodImpl(NI)] static long PVar(long* p, int i) => p[i];
    [MethodImpl(NI)] static long PVarOff(long* p, int i) => *(long*)((byte*)p + i * 8 + 16);
    [MethodImpl(NI)] static long PStep(long* p) { p += 3; return *p; }
    [MethodImpl(NI)] static long PStepTwice(long* p) { p += 3; p -= 1; return *p; }
    [MethodImpl(NI)] static long PStepKept(long* p, long** q) { p += 3; *q = p; return *p; }
    [MethodImpl(NI)] static long PStepUsed(long* p) { p += 3; long v = *p; return v + p[1]; }
    [MethodImpl(NI)] static int PInt(int* p) => p[3];
    [MethodImpl(NI)] static uint PUInt(uint* p) => p[3];
    [MethodImpl(NI)] static short PShort(short* p) => p[3];
    [MethodImpl(NI)] static ushort PUShort(ushort* p) => p[3];
    [MethodImpl(NI)] static sbyte PSByte(sbyte* p) => p[3];
    [MethodImpl(NI)] static byte PByte(byte* p) => p[3];
    [MethodImpl(NI)] static long PIntWide(int* p) => (long)p[1] + (long)(uint)p[2] * 3;
    [MethodImpl(NI)] static long PPtr(long** p) => *p[2];
    [MethodImpl(NI)] static long PPtrOff(long** p) => p[2][1];
    [MethodImpl(NI)] static long PPtrNeg(long** p) => p[-1][-1];
    [MethodImpl(NI)] static long PByteOff(byte* p) => *(long*)(p + 2047);
    [MethodImpl(NI)] static long PByteOffNeg(byte* p) => *(long*)(p - 2048);
    [MethodImpl(NI)] static long PByteOffOdd(byte* p) => *(p + 1) + *(sbyte*)(p + 2) * 256L;
    [MethodImpl(NI)] static long PAddThenLoad(long* p, long k) { long* q = (long*)((long)p + k); q++; return *q; }
    [MethodImpl(NI)] static long PRef(ref long r) => Unsafe.Add(ref r, 2);
    [MethodImpl(NI)] static int PRefInt(ref int r) => Unsafe.Add(ref r, -1);

    [MethodImpl(NI)] static long L2032(Large* p) => p->At2032;
    [MethodImpl(NI)] static long L2040(Large* p) => p->At2040;
    [MethodImpl(NI)] static long L2048(Large* p) => p->At2048;
    [MethodImpl(NI)] static long L2056(Large* p) => p->At2056;
    [MethodImpl(NI)] static int L2064(Large* p) => p->At2064;
    [MethodImpl(NI)] static long LAll(Large* p) => p->At2032 + p->At2040 * 3 + p->At2048 * 5 + p->At2056 * 7 + p->At2064 * 11L;
    [MethodImpl(NI)] static long LRef(ref Large r) => r.At2040 + r.At2048 * 3;

    // An increment that is not an address: the value itself is loaded from memory right after.
    [MethodImpl(NI)] static long Counter(long* p, long x) { x += 8; long v = *p; return x * 3 + v; }
    [MethodImpl(NI)] static long CounterSame(long* p) { long x = (long)p; x += 8; return x ^ *p; }
    [MethodImpl(NI)] static long TwoAdds(long* p) { long* q = p + 1; long* r = q + 1; return *r * 3 + *q; }
    [MethodImpl(NI)] static long AddAfterBranch(long* p, bool f) { if (f) p += 2; else p += 4; return *p; }

    [MethodImpl(NI)]
    static long LoopPointer(long* p, int n)
    {
        long s = 0;
        for (int i = 0; i < n; i++)
        {
            p += 1;
            s = s * 3 + *p;
        }
        return s;
    }

    // ---- driver ----

    static void Eq(string name, long want, long got)
    {
        checks++;
        if (want == got) return;
        fails++;
        if (++shown <= 60)
            Console.WriteLine("FAIL " + name + " expected 0x" + want.ToString("x") + " got 0x" + got.ToString("x"));
    }

    [MethodImpl(NoOpt)] static long V(long i) => (i + 1) * 0x0101010101010101L + (i << 56) - (i << 31);
    [MethodImpl(NoOpt)] static long S32(long v) { v &= 0xFFFFFFFFL; if (v >= 0x80000000L) v -= 0x100000000L; return v; }
    [MethodImpl(NoOpt)] static long S16(long v) { v &= 0xFFFF; if (v >= 0x8000) v -= 0x10000; return v; }
    [MethodImpl(NoOpt)] static long S8(long v) { v &= 0xFF; if (v >= 0x80) v -= 0x100; return v; }

    [MethodImpl(NoOpt)]
    static void Arrays()
    {
        const int n = 9;
        long[] l = new long[n]; ulong[] ul = new ulong[n]; int[] i4 = new int[n]; uint[] u4 = new uint[n];
        short[] i2 = new short[n]; ushort[] u2 = new ushort[n]; sbyte[] i1 = new sbyte[n]; byte[] u1 = new byte[n];
        char[] ch = new char[n]; bool[] bo = new bool[n]; nint[] ni = new nint[n]; string[] st = new string[n];
        Node[] no = new Node[n]; Elem[] el = new Elem[n];
        long[,] md = new long[3, 4];
        int[][] jag = new int[3][];
        string text = "";
        for (int i = 0; i < n; i++)
        {
            long v = V(i) ^ -(i & 1);
            l[i] = v; ul[i] = (ulong)v; i4[i] = (int)v; u4[i] = (uint)v; i2[i] = (short)v; u2[i] = (ushort)v;
            i1[i] = (sbyte)v; u1[i] = (byte)v; ch[i] = (char)(v & 0xFFFF); bo[i] = (i & 1) != 0; ni[i] = (nint)v;
            st[i] = "s" + i; no[i] = new Node(); no[i].A = v;
            el[i].X = v; el[i].Y = (int)~v; el[i].Z = (short)(v >> 3); el[i].W = (byte)(v >> 5);
            text += (char)('a' + i);
        }
        for (int i = 0; i < 3; i++)
        {
            jag[i] = new int[4];
            for (int j = 0; j < 4; j++) { md[i, j] = V(i * 4 + j); jag[i][j] = (int)V(i * 7 + j); }
        }

        long sum = 0, sumBack = 0;
        for (int i = 0; i < n; i++)
        {
            long v = V(i) ^ -(i & 1);
            Eq("long[]", v, ALong(l, i));
            Eq("ulong[]", v, (long)AULong(ul, i));
            Eq("int[]", S32(v), AInt(i4, i));
            Eq("uint[]", v & 0xFFFFFFFFL, AUInt(u4, i));
            Eq("short[]", S16(v), AShort(i2, i));
            Eq("ushort[]", v & 0xFFFF, AUShort(u2, i));
            Eq("sbyte[]", S8(v), ASByte(i1, i));
            Eq("byte[]", v & 0xFF, AByte(u1, i));
            Eq("char[]", v & 0xFFFF, AChar(ch, i));
            Eq("bool[]", i & 1, ABool(bo, i) ? 1 : 0);
            Eq("nint[]", v, ANint(ni, i));
            Eq("string[]", '0' + i, AString(st, i)[1]);
            Eq("Node[]", v, ANode(no, i).A);
            Eq("struct[].X", v, AElemX(el, i));
            Eq("struct[].Y", S32(~v), AElemY(el, i));
            Eq("struct[].Z", S16(v >> 3), AElemZ(el, i));
            Eq("struct[].W", (v >> 5) & 0xFF, AElemW(el, i));
            Eq("long[uint]", v, ALongU(l, (uint)i));
            Eq("long[long]", v, ALongL(l, i));
            Eq("string[i]", 'a' + i, SChar(text, i));
            Eq("span[i]", v, SpanElem(l, i));
            Eq("byte span[i]", v & 0xFF, SpanByte(u1, i));
            Eq("unoptimized long[]", v, NLong(l, i));
            Eq("unoptimized ulong[]", v, (long)NULong(ul, i));
            Eq("unoptimized int[]", S32(v), NInt(i4, i));
            Eq("unoptimized uint[]", v & 0xFFFFFFFFL, NUInt(u4, i));
            Eq("unoptimized short[]", S16(v), NShort(i2, i));
            Eq("unoptimized ushort[]", v & 0xFFFF, NUShort(u2, i));
            Eq("unoptimized sbyte[]", S8(v), NSByte(i1, i));
            Eq("unoptimized byte[]", v & 0xFF, NByte(u1, i));
            Eq("unoptimized char[]", v & 0xFFFF, NChar(ch, i));
            Eq("unoptimized bool[]", i & 1, NBool(bo, i) ? 1 : 0);
            Eq("unoptimized string[]", '0' + i, NString(st, i)[1]);
            Eq("unoptimized Node[]", v, NNode(no, i).A);
            Eq("unoptimized struct[].X", v, NElemX(el, i));
            Eq("unoptimized struct[].Y", S32(~v), NElemY(el, i));
            Eq("unoptimized struct[].Z", S16(v >> 3), NElemZ(el, i));
            Eq("unoptimized struct[].W", (v >> 5) & 0xFF, NElemW(el, i));
            Eq("unoptimized element, index and length", v + i + n, NReuse(l, i));
            Eq("unoptimized element three times", v + v * v, NSame(l, i));
            if (i + 1 < n) Eq("unoptimized int[] widened", S32(v) + ((V(i + 1) ^ -((i + 1) & 1)) & 0xFFFFFFFFL) * 3, NWiden(i4, i));
            Eq("element, index and length", v + i + n, AReuse(l, i));
            Eq("element three times", v + v * v, ASame(l, i));
            Eq("two elements", v * 3 + (V(n - 1 - i) ^ -((n - 1 - i) & 1)), ATwo(l, i, n - 1 - i));
            if (i + 1 < n) Eq("int[] widened", S32(v) + ((V(i + 1) ^ -((i + 1) & 1)) & 0xFFFFFFFFL) * 3, AWiden(i4, i));
            sum = sum * 3 + v;
        }
        for (int i = n - 1; i >= 0; i--)
        {
            long v = V(i) ^ -(i & 1);
            sumBack = sumBack * 5 + S32(v) + S16(v) + (v & 0xFF);
        }
        long sumAll = 0;
        for (int i = 0; i < n; i++)
        {
            long v = V(i) ^ -(i & 1);
            sumAll = sumAll * 3 + v + S32(v) + S16(v) + (v & 0xFF);
        }
        Eq("unoptimized sum", sumAll, NSum(l, i4, i2, u1));
        Eq("constant indices", l[0] + l[1] * 3 + l[5] * 5, AConst(l));
        Eq("sum", sum, ASum(l));
        Eq("sum backwards", sumBack, ASumBack(i4, i2, u1));
        Eq("length", n, Length(i4));
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 4; j++)
            {
                Eq("long[,]", V(i * 4 + j), Md(md, i, j));
                Eq("int[][]", S32(V(i * 7 + j)), Jagged(jag, i, j));
                Eq("unoptimized int[][]", S32(V(i * 7 + j)), NJagged(jag, i, j));
            }

        long[] chain = { 3, 0, 1, 2, 77 };
        Eq("chained elements", 0, AChain(chain, 3));       // a[3] = 2, a[2] = 1, a[1] = 0
        Eq("chained elements (2)", 1, AChain(chain, 0));    // a[0] = 3, a[3] = 2, a[2] = 1
        Eq("unoptimized chained elements", 0, NChain(chain, 3));
        long[] two = { 41 };
        Eq("unoptimized load and store", 41 * 3 + 42, NStore(two, 0));
        long[] four = { 5, 6, 7, 8 };
        Eq("fixed pointer stepped", 7, QFixed(four));
        long[] one = { 41 };
        Eq("load and store", 41 * 3 + 42, AStore(one, 0));

        Node a = new Node(), b = new Node(), c = new Node();
        a.A = 0x1111111111111111L; b.A = 0x2222222222222222L; c.A = -0x3333333333333334L;
        a.Next = b; b.Next = c;
        a.Ints = new int[] { 1, -2, 3 }; b.Ints = new int[] { -40, 50, -60 }; c.Ints = new int[] { 700, -800, 900 };
        a.Text = "abc"; b.Text = "defg"; c.Text = "h";
        a.I = -5; a.S = -6; a.B = 250; b.I = int.MinValue; b.S = short.MinValue; b.B = 255;
        Eq("field", a.A, FA(a));
        Eq("reference field", b.A, FNext(a).A);
        Eq("field of a field", b.A, FNextA(a));
        Eq("field of a field of a field", c.A, FNextNextA(a));
        Eq("element of a field", 3, FInts(a, 2));
        Eq("element of a field of a field", -60, FNextInts(a, 2));
        Eq("char of a field", 'c', FText(a, 2));
        Eq("small fields", -5 * 65536L + -6 * 256L + 250, FSmall(a));
        Eq("small fields of a field", int.MinValue * 65536L + short.MinValue * 256L + 255, FNextSmall(a));
        Eq("length of a string field", 4, TextLength(b));
        Eq("length of an array field", 3, IntsLength(c));
        Eq("list walk", ((a.A - 2) * 7 + b.A + 50) * 7 + c.A - 800, Walk(a));
        Eq("unoptimized fields", b.A * 3 + 50 + 'b', NFields(a));
    }

    [MethodImpl(NoOpt)]
    static void Pointers()
    {
        const int half = 600;
        long* buf = stackalloc long[2 * half + 1];
        long* mid = buf + half;
        for (int i = -half; i <= half; i++) mid[i] = V(i + half) ^ i;

        Eq("p[1]", mid[1], P8(mid));
        Eq("p[2]", mid[2], P16(mid));
        Eq("p[-1]", mid[-1], PNeg8(mid));
        Eq("p[-2]", mid[-2], PNeg16(mid));
        Eq("p[255]", mid[255], P2040(mid));
        Eq("p[256]", mid[256], P2048(mid));
        Eq("p[-256]", mid[-256], PNeg2048(mid));
        Eq("p[-257]", mid[-257], PNeg2056(mid));
        Eq("p[512]", mid[512], P4096(mid));
        for (int i = -5; i <= 5; i++)
        {
            Eq("p[i]", mid[i], PVar(mid, i));
            Eq("p + i * 8 + 16", mid[i + 2], PVarOff(mid, i));
        }
        Eq("p += 3", mid[3], PStep(mid));
        Eq("p += 3, p -= 1", mid[2], PStepTwice(mid));
        long* kept = null;
        Eq("p += 3, kept", mid[3], PStepKept(mid, &kept));
        Eq("p += 3, kept: the pointer", (long)(mid + 3), (long)kept);
        Eq("p += 3, used again", mid[3] + mid[4], PStepUsed(mid));
        Eq("int*", S32(mid[1] >> 32), PInt((int*)mid));
        Eq("uint*", (mid[1] >> 32) & 0xFFFFFFFFL, PUInt((uint*)mid));
        Eq("short*", S16(mid[0] >> 48), PShort((short*)mid));
        Eq("ushort*", (mid[0] >> 48) & 0xFFFF, PUShort((ushort*)mid));
        Eq("sbyte*", S8(mid[0] >> 24), PSByte((sbyte*)mid));
        Eq("byte*", (mid[0] >> 24) & 0xFF, PByte((byte*)mid));
        Eq("int* widened", S32(mid[0] >> 32) + (mid[1] & 0xFFFFFFFFL) * 3, PIntWide((int*)mid));

        for (int i = -5; i <= 5; i++)
            Eq("unoptimized pointer", mid[i] + mid[1] * 3 + mid[-2] * 5, NPointer(mid, i));
        Eq("address local + 1", mid[1] + 9, QStep(mid, 9));
        Eq("address local - 3", mid[-3] ^ 9, QStepNeg(mid, 9));
        Eq("address local, int", S32(S32(mid[1] >> 32) + 9), QStepInt((int*)mid, 9));
        Eq("address ref + 2", mid[2] + 9, QRef(ref mid[0], 9));
        Eq("two address locals", mid[1] * 3 + mid[12], QTwo(mid, mid + 10));
        Eq("address local, conditional (true)", mid[3], QCond(mid, true));
        Eq("address local, conditional (false)", mid[1], QCond(mid, false));
        long** table = stackalloc long*[5];
        for (int i = 0; i < 5; i++) table[i] = mid + i * 10;
        Eq("*p[2]", mid[20], PPtr(table));
        Eq("p[2][1]", mid[21], PPtrOff(table));
        Eq("p[-1][-1]", mid[9], PPtrNeg(table + 2));

        byte* bytes = (byte*)mid;
        Eq("byte offset 2047", Unaligned(bytes + 2047), PByteOff(bytes));
        Eq("byte offset -2048", mid[-256], PByteOffNeg(bytes));
        Eq("byte offsets 1 and 2", ((mid[0] >> 8) & 0xFF) + S8(mid[0] >> 16) * 256, PByteOffOdd(bytes));
        Eq("(p + k) + 1", mid[5], PAddThenLoad(mid, 32));
        Eq("ref + 2", mid[2], PRef(ref mid[0]));
        Eq("ref int - 1", S32(mid[-1] >> 32), PRefInt(ref *(int*)mid));

        Large* large = (Large*)mid;
        Eq("field at 2032", mid[254], L2032(large));
        Eq("field at 2040", mid[255], L2040(large));
        Eq("field at 2048", mid[256], L2048(large));
        Eq("field at 2056", mid[257], L2056(large));
        Eq("field at 2064", S32(mid[258]), L2064(large));
        Eq("fields at 2032 to 2064", mid[254] + mid[255] * 3 + mid[256] * 5 + mid[257] * 7 + S32(mid[258]) * 11, LAll(large));
        Eq("ref fields at 2040 and 2048", mid[255] + mid[256] * 3, LRef(ref *large));

        Eq("counter and load", (100 + 8) * 3 + mid[0], Counter(mid, 100));
        Eq("counter from the pointer", ((long)mid + 8) ^ mid[0], CounterSame(mid));
        Eq("two adds", mid[2] * 3 + mid[1], TwoAdds(mid));
        Eq("add in both arms (true)", mid[2], AddAfterBranch(mid, true));
        Eq("add in both arms (false)", mid[4], AddAfterBranch(mid, false));
        long want = 0;
        for (int i = 1; i <= 7; i++) want = want * 3 + mid[i];
        Eq("pointer stepped in a loop", want, LoopPointer(mid, 7));
    }

    [MethodImpl(NoOpt)]
    static long Unaligned(byte* p)
    {
        long v = 0;
        for (int i = 7; i >= 0; i--) v = (v << 8) | p[i];
        return v;
    }

    static int Main()
    {
        Arrays();
        Pointers();
        Console.WriteLine(fails == 0 ? "ALL PASS (" + checks + " checks)" : "FAILED " + fails + " of " + checks);
        return fails == 0 ? 0 : 1;
    }
}
