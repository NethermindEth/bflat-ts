// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Unaligned loads and stores of structs that travel in registers - a struct
// that wraps a single primitive, a struct of two ints - on a target that
// traps on a misaligned access (riscv32, Airbender).
//
// With JitNoUnalignedAccess an access the front end marks unaligned
// (Unsafe.ReadUnaligned, Unsafe.WriteUnaligned, MemoryMarshal.Read and
// Write) is done byte by byte unless the address turns out to be aligned.
// The mark sits on the indirection. A struct with one field of 2, 4 or 8
// bytes is not copied as a block: its store or load is retyped to the
// field's type on the way - when the value is a parameter or a return value
// held in a register, when a block store is turned into a scalar store
// (patch 03 keeps the mark there), when the struct is promoted to its field.
// A struct of two ints that arrives in two registers is promoted too and is
// stored field by field. A struct of up to 8 bytes that a method returns
// straight from the read, whatever its fields, is loaded as the int or the
// pair of ints it is returned as.
//
// What can go wrong: any of these retypings drops the mark, and the access
// becomes one sh, sw, lh or lw at an odd address. On Airbender the guest
// traps ("Unaligned memory access"), so a failure here ends the run before
// ALL PASS rather than printing a FAIL line; where unaligned accesses work
// (riscv64 under qemu) only the values are checked.
//
// Wrappers of short, ushort, char, int, uint, long, ulong, a pointer, a
// nested wrapper, an enum and a struct of two ints are written and read at
// every offset 0..7 as
// parameters and return values, as locals, as fields and through generic
// methods; the bytes around each store are checked too.
//
// Each group prints the number of checks and each step is announced before
// it runs, so the last line printed names the access that trapped. The run
// ends with ALL PASS or a FAIL count, and the guest returns 1 on failure.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

struct WShort { public short V; }
struct WUShort { public ushort V; }
struct WChar { public char V; }
struct WInt { public int V; }
struct WUInt { public uint V; }
struct WLong { public long V; }
struct WULong { public ulong V; }
struct WNested { public WInt Inner; }
struct W2Int { public int A, B; }
struct WBytes4 { public byte A, B, C, D; }
struct WShorts2 { public short A, B; }
struct WBytes2 { public byte A, B; }
unsafe struct WPtr { public void* P; }
enum EInt : int { A = 0x44332291 }
enum EShort : short { A = 0x2291 }

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

    // The value in a register on the way in or out.
    [MethodImpl(NI)] static void Put(byte* p, WShort v) { Unsafe.WriteUnaligned(p, v); }
    [MethodImpl(NI)] static void Put(byte* p, WUShort v) { Unsafe.WriteUnaligned(p, v); }
    [MethodImpl(NI)] static void Put(byte* p, WChar v) { Unsafe.WriteUnaligned(p, v); }
    [MethodImpl(NI)] static void Put(byte* p, WInt v) { Unsafe.WriteUnaligned(p, v); }
    [MethodImpl(NI)] static void Put(byte* p, WUInt v) { Unsafe.WriteUnaligned(p, v); }
    [MethodImpl(NI)] static void Put(byte* p, WLong v) { Unsafe.WriteUnaligned(p, v); }
    [MethodImpl(NI)] static void Put(byte* p, WULong v) { Unsafe.WriteUnaligned(p, v); }
    [MethodImpl(NI)] static void Put(byte* p, WNested v) { Unsafe.WriteUnaligned(p, v); }
    [MethodImpl(NI)] static void Put(byte* p, WPtr v) { Unsafe.WriteUnaligned(p, v); }
    [MethodImpl(NI)] static void Put(byte* p, EInt v) { Unsafe.WriteUnaligned(p, v); }
    [MethodImpl(NI)] static void Put(byte* p, EShort v) { Unsafe.WriteUnaligned(p, v); }
    [MethodImpl(NI)] static void Put(byte* p, W2Int v) { Unsafe.WriteUnaligned(p, v); }
    [MethodImpl(NI)] static void Put(byte* p, WBytes4 v) { Unsafe.WriteUnaligned(p, v); }
    [MethodImpl(NI)] static void Put(byte* p, WShorts2 v) { Unsafe.WriteUnaligned(p, v); }
    [MethodImpl(NI)] static void Put(byte* p, WBytes2 v) { Unsafe.WriteUnaligned(p, v); }

    [MethodImpl(NI)] static WShort GetShort(byte* p) => Unsafe.ReadUnaligned<WShort>(p);
    [MethodImpl(NI)] static WUShort GetUShort(byte* p) => Unsafe.ReadUnaligned<WUShort>(p);
    [MethodImpl(NI)] static WChar GetChar(byte* p) => Unsafe.ReadUnaligned<WChar>(p);
    [MethodImpl(NI)] static WInt GetInt(byte* p) => Unsafe.ReadUnaligned<WInt>(p);
    [MethodImpl(NI)] static WUInt GetUInt(byte* p) => Unsafe.ReadUnaligned<WUInt>(p);
    [MethodImpl(NI)] static WLong GetLong(byte* p) => Unsafe.ReadUnaligned<WLong>(p);
    [MethodImpl(NI)] static WULong GetULong(byte* p) => Unsafe.ReadUnaligned<WULong>(p);
    [MethodImpl(NI)] static WNested GetNested(byte* p) => Unsafe.ReadUnaligned<WNested>(p);
    [MethodImpl(NI)] static WPtr GetPtr(byte* p) => Unsafe.ReadUnaligned<WPtr>(p);
    [MethodImpl(NI)] static EInt GetEInt(byte* p) => Unsafe.ReadUnaligned<EInt>(p);
    [MethodImpl(NI)] static EShort GetEShort(byte* p) => Unsafe.ReadUnaligned<EShort>(p);
    [MethodImpl(NI)] static W2Int Get2Int(byte* p) => Unsafe.ReadUnaligned<W2Int>(p);
    [MethodImpl(NI)] static WBytes4 GetBytes4(byte* p) => Unsafe.ReadUnaligned<WBytes4>(p);
    [MethodImpl(NI)] static WShorts2 GetShorts2(byte* p) => Unsafe.ReadUnaligned<WShorts2>(p);
    [MethodImpl(NI)] static WBytes2 GetBytes2(byte* p) => Unsafe.ReadUnaligned<WBytes2>(p);

    // Through a ref, generic, from a field read, into a local, and with a constant offset.
    [MethodImpl(NI)] static void PutRef<T>(ref byte p, T v) where T : unmanaged { Unsafe.WriteUnaligned(ref p, v); }
    [MethodImpl(NI)] static T GetRef<T>(ref byte p) where T : unmanaged => Unsafe.ReadUnaligned<T>(ref p);
    [MethodImpl(NI)] static void PutField(byte* p, int v) { WInt w; w.V = v; Unsafe.WriteUnaligned(p, w); }
    [MethodImpl(NI)] static int GetField(byte* p) => Unsafe.ReadUnaligned<WInt>(p).V;
    [MethodImpl(NI)] static long GetFieldLong(byte* p) => Unsafe.ReadUnaligned<WLong>(p).V;
    [MethodImpl(NI)] static int GetLocal(byte* p) { WShort w = Unsafe.ReadUnaligned<WShort>(p); WInt x = Unsafe.ReadUnaligned<WInt>(p + 2); return w.V + x.V; }
    [MethodImpl(NI)] static void PutAt1(byte* p, WInt v) { Unsafe.WriteUnaligned(p + 1, v); }
    [MethodImpl(NI)] static void PutAt3(byte* p, WShort v) { Unsafe.WriteUnaligned(p + 3, v); }
    [MethodImpl(NI)] static void PutAt5(byte* p, WLong v) { Unsafe.WriteUnaligned(p + 5, v); }
    [MethodImpl(NI)] static WInt GetAt1(byte* p) => Unsafe.ReadUnaligned<WInt>(p + 1);
    [MethodImpl(NI)] static WShort GetAt3(byte* p) => Unsafe.ReadUnaligned<WShort>(p + 3);
    [MethodImpl(NI)] static WLong GetAt5(byte* p) => Unsafe.ReadUnaligned<WLong>(p + 5);
    [MethodImpl(NI)] static void Move(byte* d, byte* s) { Unsafe.WriteUnaligned(d, Unsafe.ReadUnaligned<WInt>(s)); }
    [MethodImpl(NI)] static void SpanWrite(Span<byte> s, WInt v) { MemoryMarshal.Write(s, in v); }
    [MethodImpl(NI)] static WInt SpanRead(Span<byte> s) => MemoryMarshal.Read<WInt>(s);
    [MethodImpl(NI)] static WLong SpanReadLong(ReadOnlySpan<byte> s) => MemoryMarshal.Read<WLong>(s);

    static void Fill(byte* p) { for (int i = 0; i < 24; i++) p[i] = (byte)(0xE0 + i); }

    // The bytes at p[off..off+size) are the little-endian bytes of v, and nothing else changed.
    static void Bytes(string name, byte* p, int off, int size, ulong v)
    {
        int bad = 0;
        for (int i = 0; i < 24; i++)
        {
            byte exp = (i >= off && i < off + size) ? (byte)(v >> (8 * (i - off))) : (byte)(0xE0 + i);
            if (p[i] != exp) bad++;
        }
        Check(name, bad, 0);
    }

    static int Main()
    {
        byte* raw = stackalloc byte[40];
        byte* buf = (byte*)(((nuint)raw + 7) & ~(nuint)7);
        ulong V = 0x8877665544332291UL;
        long vl = unchecked((long)V);
        int vi = unchecked((int)V);
        short vs = unchecked((short)V);

        for (int off = 0; off < 8; off++)
        {
            Console.WriteLine("offset " + off.ToString() + ": stores");
            Fill(buf); Put(buf + off, new WShort { V = vs }); Bytes("write WShort", buf, off, 2, V);
            Fill(buf); Put(buf + off, new WUShort { V = (ushort)V }); Bytes("write WUShort", buf, off, 2, V);
            Fill(buf); Put(buf + off, new WChar { V = (char)V }); Bytes("write WChar", buf, off, 2, V);
            Fill(buf); Put(buf + off, new WInt { V = vi }); Bytes("write WInt", buf, off, 4, V);
            Fill(buf); Put(buf + off, new WUInt { V = (uint)V }); Bytes("write WUInt", buf, off, 4, V);
            Fill(buf); Put(buf + off, new WNested { Inner = new WInt { V = vi } }); Bytes("write WNested", buf, off, 4, V);
            Fill(buf); Put(buf + off, EInt.A); Bytes("write EInt", buf, off, 4, V);
            Fill(buf); Put(buf + off, EShort.A); Bytes("write EShort", buf, off, 2, V);
            Fill(buf); Put(buf + off, new WLong { V = vl }); Bytes("write WLong", buf, off, 8, V);
            Fill(buf); Put(buf + off, new WULong { V = V }); Bytes("write WULong", buf, off, 8, V);
            Fill(buf); Put(buf + off, new WPtr { P = (void*)(nuint)(uint)V }); Bytes("write WPtr", buf, off, sizeof(nint), sizeof(nint) == 4 ? V : (uint)V);
            Fill(buf); Put(buf + off, new W2Int { A = vi, B = unchecked((int)(V >> 32)) }); Bytes("write W2Int", buf, off, 8, V);
            Fill(buf); Put(buf + off, new WBytes4 { A = 0x91, B = 0x22, C = 0x33, D = 0x44 }); Bytes("write WBytes4", buf, off, 4, V);
            Fill(buf); Put(buf + off, new WShorts2 { A = 0x2291, B = 0x4433 }); Bytes("write WShorts2", buf, off, 4, V);
            Fill(buf); Put(buf + off, new WBytes2 { A = 0x91, B = 0x22 }); Bytes("write WBytes2", buf, off, 2, V);
            Fill(buf); PutRef(ref buf[off], new WInt { V = vi }); Bytes("write WInt ref", buf, off, 4, V);
            Fill(buf); PutRef(ref buf[off], new WShort { V = vs }); Bytes("write WShort ref", buf, off, 2, V);
            Fill(buf); PutRef(ref buf[off], new WLong { V = vl }); Bytes("write WLong ref", buf, off, 8, V);
            Fill(buf); PutField(buf + off, vi); Bytes("write WInt from a field", buf, off, 4, V);
            Fill(buf); SpanWrite(new Span<byte>(buf + off, 4), new WInt { V = vi }); Bytes("MemoryMarshal.Write", buf, off, 4, V);

            Console.WriteLine("offset " + off.ToString() + ": loads");
            Fill(buf);
            for (int i = 0; i < 8; i++) buf[off + i] = (byte)(V >> (8 * i));
            Check("read WShort", GetShort(buf + off).V, vs);
            Check("read WUShort", GetUShort(buf + off).V, (ushort)V);
            Check("read WChar", GetChar(buf + off).V, (char)V);
            Check("read WInt", GetInt(buf + off).V, vi);
            Check("read WUInt", GetUInt(buf + off).V, (uint)V);
            Check("read WNested", GetNested(buf + off).Inner.V, vi);
            Check("read EInt", (long)GetEInt(buf + off), vi);
            Check("read EShort", (long)GetEShort(buf + off), vs);
            Check("read WLong", GetLong(buf + off).V, vl);
            Check("read WULong", (long)GetULong(buf + off).V, vl);
            Check("read WPtr", (long)(nuint)GetPtr(buf + off).P, sizeof(nint) == 4 ? (uint)V : vl);
            Check("read W2Int A", Get2Int(buf + off).A, vi);
            Check("read W2Int B", Get2Int(buf + off).B, unchecked((int)(V >> 32)));
            WBytes4 b4 = GetBytes4(buf + off);
            Check("read WBytes4", b4.A + (b4.B << 8) + (b4.C << 16) + ((long)b4.D << 24), (uint)V);
            Check("read WShorts2", GetShorts2(buf + off).A + GetShorts2(buf + off).B * 65536, 0x2291 + 0x4433 * 65536);
            Check("read WBytes2", GetBytes2(buf + off).A + GetBytes2(buf + off).B * 256, 0x2291);
            Check("read WInt ref", GetRef<WInt>(ref buf[off]).V, vi);
            Check("read WShort ref", GetRef<WShort>(ref buf[off]).V, vs);
            Check("read WLong ref", GetRef<WLong>(ref buf[off]).V, vl);
            Check("read WInt field", GetField(buf + off), vi);
            Check("read WLong field", GetFieldLong(buf + off), vl);
            Check("read into locals", GetLocal(buf + off), vs + unchecked((int)(V >> 16)));
            Check("MemoryMarshal.Read", SpanRead(new Span<byte>(buf + off, 4)).V, vi);
            Check("MemoryMarshal.Read long", SpanReadLong(new ReadOnlySpan<byte>(buf + off, 8)).V, vl);

            Move(buf + 16 + (off ^ 1), buf + off);
            Check("move", buf[16 + (off ^ 1)] + buf[19 + (off ^ 1)] * 256, 0x91 + 0x44 * 256);
        }
        Group("every offset");

        Console.WriteLine("constant offsets");
        Fill(buf); PutAt1(buf, new WInt { V = vi }); Bytes("WInt at +1", buf, 1, 4, V);
        Check("WInt from +1", GetAt1(buf).V, vi);
        Fill(buf); PutAt3(buf, new WShort { V = vs }); Bytes("WShort at +3", buf, 3, 2, V);
        Check("WShort from +3", GetAt3(buf).V, vs);
        Fill(buf); PutAt5(buf, new WLong { V = vl }); Bytes("WLong at +5", buf, 5, 8, V);
        Check("WLong from +5", GetAt5(buf).V, vl);
        Group("constant offsets");

        if (fails == 0)
        {
            Console.WriteLine("ALL PASS");
            return 0;
        }
        Console.WriteLine("FAIL " + fails.ToString());
        return 1;
    }
}
