// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Whole copies of structs whose alignment is smaller than the pieces they
// are copied in, on a target that traps on a misaligned access (riscv32,
// Airbender).
//
// A struct of bytes has alignment 1 and a struct of shorts alignment 2, so
// an array element, a field or a pointer of such a type can sit at any
// address of that alignment: element i of an array of 3-byte structs is at
// data + 3 * i. A copy of the whole struct is unrolled into register-sized
// pieces and a 4-, 2- and 1-byte tail. With JitNoUnalignedAccess and
// without JitRiscV64StrictAlign the JIT trusts a base address to be aligned
// for the piece it loads or stores, and only tells an access apart by a
// constant offset that is not a multiple of the piece.
//
// What can go wrong: that trust is not justified for these types - a 2-byte
// piece at an odd address for a 3-byte struct, a 4-byte piece at an address
// that is 1, 2 or 3 modulo 4 for a struct of 5, 6 or 7 bytes. On Airbender
// the guest traps ("Unaligned memory access"), so a failure here ends the
// run before ALL PASS rather than printing a FAIL line; where unaligned
// accesses work (riscv64 under qemu) only the values are checked.
//
// Structs of 3, 5 and 7 bytes and of 3 shorts are loaded from, stored to and
// copied between array elements at every index, through pointers at every
// offset, and as fields at odd offsets of other structs.
//
// Each step is announced before it runs, so the last line printed names the
// access that trapped. The run ends with ALL PASS or a FAIL count, and the
// guest returns 1 on failure.
using System;
using System.Runtime.CompilerServices;

struct S3 { public byte A, B, C; }
struct B5 { public byte A, B, C, D, E; }
struct B7 { public byte A, B, C, D, E, F, G; }
struct S6 { public short A, B, C; }
struct Holder { public byte Tag; public S3 Inner; public byte Tag2; public B5 Five; }

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

    static void Group(string name)
    {
        Console.WriteLine("ok   " + name + " " + checks.ToString());
    }

    [MethodImpl(NI)] static T[] New<T>(int n) => new T[n];

    [MethodImpl(NI)] static S3 Get3(S3[] a, int i) => a[i];
    [MethodImpl(NI)] static void Set3(S3[] a, int i, S3 v) { a[i] = v; }
    [MethodImpl(NI)] static void Copy3(S3[] a, int i, int j) { a[i] = a[j]; }
    [MethodImpl(NI)] static B5 Get5(B5[] a, int i) => a[i];
    [MethodImpl(NI)] static void Copy5(B5[] a, int i, int j) { a[i] = a[j]; }
    [MethodImpl(NI)] static void Copy7(B7[] a, int i, int j) { a[i] = a[j]; }
    [MethodImpl(NI)] static S6 Get6(S6[] a, int i) => a[i];
    [MethodImpl(NI)] static void Copy6(S6[] a, int i, int j) { a[i] = a[j]; }
    [MethodImpl(NI)] static S3 Deref3(S3* p) => *p;
    [MethodImpl(NI)] static void Store3(S3* p, S3 v) { *p = v; }
    [MethodImpl(NI)] static void CopyPtr5(B5* d, B5* s) { *d = *s; }
    [MethodImpl(NI)] static S3 Field3(Holder* h) => h->Inner;
    [MethodImpl(NI)] static B5 Field5(Holder[] a, int i) => a[i].Five;
    [MethodImpl(NI)] static void SetField5(Holder[] a, int i, B5 v) { a[i].Five = v; }
    [MethodImpl(NI)] static void CopySpan3(Span<S3> d, ReadOnlySpan<S3> s, int i) { d[i] = s[i + 1]; }

    static int Main()
    {
        S3[] a3 = New<S3>(9);
        B5[] a5 = New<B5>(9);
        B7[] a7 = New<B7>(9);
        S6[] a6 = New<S6>(9);
        Holder[] ah = New<Holder>(5);
        for (int i = 0; i < 9; i++)
        {
            a3[i].A = (byte)(i + 1); a3[i].B = (byte)(i + 101); a3[i].C = (byte)(i + 201);
            a5[i].A = (byte)(i + 1); a5[i].E = (byte)(i + 51);
            a7[i].A = (byte)(i + 1); a7[i].D = (byte)(i + 31); a7[i].G = (byte)(i + 61);
            a6[i].A = (short)(i + 1); a6[i].B = (short)(-i - 1000); a6[i].C = (short)(i + 2000);
        }

        for (int i = 0; i < 8; i++)
        {
            Console.WriteLine("load element " + i.ToString() + " of S3[]");
            S3 e = Get3(a3, i);
            Check("S3[i]", e.A + e.B * 256 + e.C * 65536, (i + 1) + (i + 101) * 256 + (i + 201) * 65536);
        }
        for (int i = 0; i < 8; i++)
        {
            Console.WriteLine("load element " + i.ToString() + " of B5[]");
            B5 f = Get5(a5, i);
            Check("B5[i]", f.A + f.B + f.C + f.D + f.E * 256, (i + 1) + (i + 51) * 256);
        }
        for (int i = 0; i < 8; i++)
        {
            Console.WriteLine("load element " + i.ToString() + " of S6[]");
            S6 g = Get6(a6, i);
            Check("S6[i]", g.A + g.B * 3 + g.C * 5, (i + 1) + (-i - 1000) * 3 + (i + 2000) * 5);
        }
        Group("loads");

        for (int i = 0; i < 8; i++)
        {
            Console.WriteLine("copy element " + (i + 1).ToString() + " to " + i.ToString() + " of S3[], B5[], B7[], S6[]");
            Copy3(a3, i, i + 1);
            Check("S3 copy", a3[i].A + a3[i].B * 256 + a3[i].C * 65536, (i + 2) + (i + 102) * 256 + (i + 202) * 65536);
            Copy5(a5, i, i + 1);
            Check("B5 copy", a5[i].A + a5[i].E * 256, (i + 2) + (i + 52) * 256);
            Copy7(a7, i, i + 1);
            Check("B7 copy", a7[i].A + a7[i].D * 256 + a7[i].G * 65536, (i + 2) + (i + 32) * 256 + (i + 62) * 65536);
            Copy6(a6, i, i + 1);
            Check("S6 copy", a6[i].A + a6[i].B * 3 + a6[i].C * 5, (i + 2) + (-i - 1001) * 3 + (i + 2001) * 5);
        }
        S3 v = new S3 { A = 0x11, B = 0x22, C = 0x33 };
        for (int i = 0; i < 9; i++)
        {
            Console.WriteLine("store element " + i.ToString() + " of S3[]");
            Set3(a3, i, v);
        }
        int sum = 0;
        for (int i = 0; i < 9; i++) sum += a3[i].A + a3[i].B + a3[i].C;
        Check("S3 store", sum, 9 * 0x66);
        a3[5].B = 0x77;
        for (int i = 0; i < 7; i++)
        {
            Console.WriteLine("copy span element " + (i + 1).ToString() + " to " + i.ToString());
            CopySpan3(a3, a3, i);
        }
        Check("S3 span copy", a3[4].B * 256 + a3[3].B, 0x7722);
        Group("copies and stores");

        byte* raw = stackalloc byte[32];
        byte* buf = (byte*)(((nuint)raw + 3) & ~(nuint)3);
        for (int off = 0; off < 4; off++)
        {
            Console.WriteLine("pointer at offset " + off.ToString());
            for (int i = 0; i < 24; i++) buf[i] = (byte)(0xE0 + i);
            Store3((S3*)(buf + off), v);
            Check("S3 through a pointer: store", buf[off] + buf[off + 1] * 256 + buf[off + 2] * 65536, 0x332211);
            Check("S3 through a pointer: after", buf[off + 3], 0xE0 + off + 3);
            S3 r = Deref3((S3*)(buf + off));
            Check("S3 through a pointer: load", r.A + r.B * 256 + r.C * 65536, 0x332211);
            CopyPtr5((B5*)(buf + 12 + off), (B5*)(buf + (3 - off)));
            Check("B5 through pointers", buf[12 + off] * 256 + buf[16 + off], buf[3 - off] * 256 + buf[7 - off]);
            Check("B5 through pointers: after", buf[17 + off], 0xE0 + 17 + off);
        }
        Group("pointers");

        for (int i = 0; i < 5; i++)
        {
            Console.WriteLine("fields of element " + i.ToString());
            ah[i].Tag = 0x7A; ah[i].Tag2 = 0x7B;
            ah[i].Inner = v;
            SetField5(ah, i, new B5 { A = (byte)i, B = 2, C = 3, D = 4, E = (byte)(i + 5) });
            B5 f = Field5(ah, i);
            Check("B5 field of an element", f.A + f.B + f.C + f.D + f.E * 256, i + 9 + (i + 5) * 256);
            Check("tags kept", ah[i].Tag * 256 + ah[i].Tag2, 0x7A7B);
            fixed (Holder* h = &ah[i])
            {
                S3 r = Field3(h);
                Check("S3 field at offset 1", r.A + r.B * 256 + r.C * 65536, 0x332211);
            }
        }
        Group("fields");

        if (fails == 0)
        {
            Console.WriteLine("ALL PASS");
            return 0;
        }
        Console.WriteLine("FAIL " + fails.ToString());
        return 1;
    }
}
