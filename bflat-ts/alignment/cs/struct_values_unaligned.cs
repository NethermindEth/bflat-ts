// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Struct values taken from memory that is aligned for their fields only.
//
// A struct of three shorts is aligned to two bytes and a struct of three
// bytes to one, so the elements of an array of them sit at every distance
// from a word boundary. Passing such an element by value loads the struct
// into registers, or copies it to the stack of the callee, in pieces as
// wide as a register. On a target that rejects misaligned accesses each
// piece has to be read as the alignment of the struct allows, or the
// address has to be tested first. The same holds for a read that the
// program marks as unaligned when its address is that of a field of a
// local struct: the field is at an offset that is not a multiple of the
// width of the read.
//
// The cases:
//   - structs of 3, 5, 6, 7, 9, 10, 11, 12 and 14 bytes made of bytes and
//     shorts, and of 12 and 20 bytes made of ints, passed by value from
//     every element of an array, as the only argument, after seven other
//     arguments, where a struct of more than a register is split between
//     the last register and the stack, and after eight, where it is wholly
//     on the stack;
//   - the same structs returned by value from an element, and copied from
//     one element to another;
//   - Unsafe.ReadUnaligned and WriteUnaligned of two, four and eight bytes
//     at the address of every field of a local struct of shorts, of bytes
//     and of ints.
//
// A run under an emulator that accepts misaligned accesses checks the
// values only. Each group prints one line, and one more for each case that
// failed; the run ends with ALL PASS or a FAIL count.
using System;
using System.Runtime.CompilerServices;

struct B3 { public byte A, B, C; }
struct B5 { public byte A, B, C, D, E; }
struct H6 { public short A, B, C; }
struct B7 { public byte A, B, C, D, E, F, G; }
struct B9 { public byte A, B, C, D, E, F, G, H, I; }
struct H10 { public short A, B, C, D, E; }
struct B11 { public byte A, B, C, D, E, F, G, H, I, J, K; }
struct I12 { public int A, B, C; }
struct H14 { public short A, B, C, D, E, F, G; }
struct I20 { public int A, B, C, D, E; }

struct LocalH { public short A, B, C, D, E, F, G, H; }
struct LocalB { public byte A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P; }
struct LocalI { public int A, B, C, D; }

class Program
{
    const MethodImplOptions NI = MethodImplOptions.NoInlining;
    static int fails;
    static int groupFails;

    static void Eq(string name, long got, long want)
    {
        if (got != want)
        {
            Console.WriteLine("FAIL " + name + ": got 0x" + got.ToString("x") + ", want 0x" + want.ToString("x"));
            groupFails++;
        }
    }

    static void Group(string name, Action body)
    {
        groupFails = 0;
        body();
        Console.WriteLine((groupFails == 0 ? "ok " : "FAILED ") + name);
        fails += groupFails;
    }

    // The value of field number f of element number i: distinct for every byte of the array.
    static int V(int i, int f) => (i * 31 + f * 7 + 5) & 0x7f;

    [MethodImpl(NI)] static long S(B3 s) => s.A + 2 * s.B + 3 * s.C;
    [MethodImpl(NI)] static long S(B5 s) => s.A + 2 * s.B + 3 * s.C + 4 * s.D + 5 * s.E;
    [MethodImpl(NI)] static long S(H6 s) => s.A + 2 * s.B + 3 * s.C;
    [MethodImpl(NI)] static long S(B7 s) => s.A + 2 * s.B + 3 * s.C + 4 * s.D + 5 * s.E + 6 * s.F + 7 * s.G;
    [MethodImpl(NI)] static long S(B9 s) => s.A + 2 * s.B + 3 * s.C + 4 * s.D + 5 * s.E + 6 * s.F + 7 * s.G + 8 * s.H + 9 * s.I;
    [MethodImpl(NI)] static long S(H10 s) => s.A + 2 * s.B + 3 * s.C + 4 * s.D + 5 * s.E;
    [MethodImpl(NI)] static long S(B11 s) => s.A + 2 * s.B + 3 * s.C + 4 * s.D + 5 * s.E + 6 * s.F + 7 * s.G + 8 * s.H + 9 * s.I + 10 * s.J + 11 * s.K;
    [MethodImpl(NI)] static long S(I12 s) => s.A + 2 * s.B + 3 * s.C;
    [MethodImpl(NI)] static long S(H14 s) => s.A + 2 * s.B + 3 * s.C + 4 * s.D + 5 * s.E + 6 * s.F + 7 * s.G;
    [MethodImpl(NI)] static long S(I20 s) => s.A + 2 * s.B + 3 * s.C + 4 * s.D + 5 * s.E;

    static long Want(int i, int fields)
    {
        long r = 0;
        for (int f = 0; f < fields; f++) r += (f + 1) * V(i, f);
        return r;
    }

    // One struct type: its array, and the ways of taking an element by value. The methods are written
    // out for every type: the element is the argument itself, not a copy of it in a local.
    sealed class Shape
    {
        public string Name;
        public int Fields;
        public Func<int, long> Only, AfterSeven, AfterEight, Returned;
        public Func<int, int, long> Copied;
    }

    static B3[] sB3;
    [MethodImpl(NI)] static long Seven(int a, int b, int c, int d, int e, int f, int g, B3 s) => S(s) + a + g;
    [MethodImpl(NI)] static long Eight(int a, int b, int c, int d, int e, int f, int g, int h, B3 s) => S(s) + a + h;
    [MethodImpl(NI)] static long Only(B3[] x, int i) => S(x[i]);
    [MethodImpl(NI)] static long AfterSeven(B3[] x, int i) => Seven(100, 2, 3, 4, 5, 6, 700, x[i]) - 800;
    [MethodImpl(NI)] static long AfterEight(B3[] x, int i) => Eight(100, 2, 3, 4, 5, 6, 7, 800, x[i]) - 900;
    [MethodImpl(NI)] static B3 Get(B3[] x, int i) => x[i];
    [MethodImpl(NI)] static void Copy(B3[] x, int from, int to) => x[to] = x[from];
    [MethodImpl(NI)] static long Copied(B3[] x, int from, int to) { B3 keep = x[to]; Copy(x, from, to); long r = S(x[to]); x[to] = keep; return r; }

    static B5[] sB5;
    [MethodImpl(NI)] static long Seven(int a, int b, int c, int d, int e, int f, int g, B5 s) => S(s) + a + g;
    [MethodImpl(NI)] static long Eight(int a, int b, int c, int d, int e, int f, int g, int h, B5 s) => S(s) + a + h;
    [MethodImpl(NI)] static long Only(B5[] x, int i) => S(x[i]);
    [MethodImpl(NI)] static long AfterSeven(B5[] x, int i) => Seven(100, 2, 3, 4, 5, 6, 700, x[i]) - 800;
    [MethodImpl(NI)] static long AfterEight(B5[] x, int i) => Eight(100, 2, 3, 4, 5, 6, 7, 800, x[i]) - 900;
    [MethodImpl(NI)] static B5 Get(B5[] x, int i) => x[i];
    [MethodImpl(NI)] static void Copy(B5[] x, int from, int to) => x[to] = x[from];
    [MethodImpl(NI)] static long Copied(B5[] x, int from, int to) { B5 keep = x[to]; Copy(x, from, to); long r = S(x[to]); x[to] = keep; return r; }

    static H6[] sH6;
    [MethodImpl(NI)] static long Seven(int a, int b, int c, int d, int e, int f, int g, H6 s) => S(s) + a + g;
    [MethodImpl(NI)] static long Eight(int a, int b, int c, int d, int e, int f, int g, int h, H6 s) => S(s) + a + h;
    [MethodImpl(NI)] static long Only(H6[] x, int i) => S(x[i]);
    [MethodImpl(NI)] static long AfterSeven(H6[] x, int i) => Seven(100, 2, 3, 4, 5, 6, 700, x[i]) - 800;
    [MethodImpl(NI)] static long AfterEight(H6[] x, int i) => Eight(100, 2, 3, 4, 5, 6, 7, 800, x[i]) - 900;
    [MethodImpl(NI)] static H6 Get(H6[] x, int i) => x[i];
    [MethodImpl(NI)] static void Copy(H6[] x, int from, int to) => x[to] = x[from];
    [MethodImpl(NI)] static long Copied(H6[] x, int from, int to) { H6 keep = x[to]; Copy(x, from, to); long r = S(x[to]); x[to] = keep; return r; }

    static B7[] sB7;
    [MethodImpl(NI)] static long Seven(int a, int b, int c, int d, int e, int f, int g, B7 s) => S(s) + a + g;
    [MethodImpl(NI)] static long Eight(int a, int b, int c, int d, int e, int f, int g, int h, B7 s) => S(s) + a + h;
    [MethodImpl(NI)] static long Only(B7[] x, int i) => S(x[i]);
    [MethodImpl(NI)] static long AfterSeven(B7[] x, int i) => Seven(100, 2, 3, 4, 5, 6, 700, x[i]) - 800;
    [MethodImpl(NI)] static long AfterEight(B7[] x, int i) => Eight(100, 2, 3, 4, 5, 6, 7, 800, x[i]) - 900;
    [MethodImpl(NI)] static B7 Get(B7[] x, int i) => x[i];
    [MethodImpl(NI)] static void Copy(B7[] x, int from, int to) => x[to] = x[from];
    [MethodImpl(NI)] static long Copied(B7[] x, int from, int to) { B7 keep = x[to]; Copy(x, from, to); long r = S(x[to]); x[to] = keep; return r; }

    static B9[] sB9;
    [MethodImpl(NI)] static long Seven(int a, int b, int c, int d, int e, int f, int g, B9 s) => S(s) + a + g;
    [MethodImpl(NI)] static long Eight(int a, int b, int c, int d, int e, int f, int g, int h, B9 s) => S(s) + a + h;
    [MethodImpl(NI)] static long Only(B9[] x, int i) => S(x[i]);
    [MethodImpl(NI)] static long AfterSeven(B9[] x, int i) => Seven(100, 2, 3, 4, 5, 6, 700, x[i]) - 800;
    [MethodImpl(NI)] static long AfterEight(B9[] x, int i) => Eight(100, 2, 3, 4, 5, 6, 7, 800, x[i]) - 900;
    [MethodImpl(NI)] static B9 Get(B9[] x, int i) => x[i];
    [MethodImpl(NI)] static void Copy(B9[] x, int from, int to) => x[to] = x[from];
    [MethodImpl(NI)] static long Copied(B9[] x, int from, int to) { B9 keep = x[to]; Copy(x, from, to); long r = S(x[to]); x[to] = keep; return r; }

    static H10[] sH10;
    [MethodImpl(NI)] static long Seven(int a, int b, int c, int d, int e, int f, int g, H10 s) => S(s) + a + g;
    [MethodImpl(NI)] static long Eight(int a, int b, int c, int d, int e, int f, int g, int h, H10 s) => S(s) + a + h;
    [MethodImpl(NI)] static long Only(H10[] x, int i) => S(x[i]);
    [MethodImpl(NI)] static long AfterSeven(H10[] x, int i) => Seven(100, 2, 3, 4, 5, 6, 700, x[i]) - 800;
    [MethodImpl(NI)] static long AfterEight(H10[] x, int i) => Eight(100, 2, 3, 4, 5, 6, 7, 800, x[i]) - 900;
    [MethodImpl(NI)] static H10 Get(H10[] x, int i) => x[i];
    [MethodImpl(NI)] static void Copy(H10[] x, int from, int to) => x[to] = x[from];
    [MethodImpl(NI)] static long Copied(H10[] x, int from, int to) { H10 keep = x[to]; Copy(x, from, to); long r = S(x[to]); x[to] = keep; return r; }

    static B11[] sB11;
    [MethodImpl(NI)] static long Seven(int a, int b, int c, int d, int e, int f, int g, B11 s) => S(s) + a + g;
    [MethodImpl(NI)] static long Eight(int a, int b, int c, int d, int e, int f, int g, int h, B11 s) => S(s) + a + h;
    [MethodImpl(NI)] static long Only(B11[] x, int i) => S(x[i]);
    [MethodImpl(NI)] static long AfterSeven(B11[] x, int i) => Seven(100, 2, 3, 4, 5, 6, 700, x[i]) - 800;
    [MethodImpl(NI)] static long AfterEight(B11[] x, int i) => Eight(100, 2, 3, 4, 5, 6, 7, 800, x[i]) - 900;
    [MethodImpl(NI)] static B11 Get(B11[] x, int i) => x[i];
    [MethodImpl(NI)] static void Copy(B11[] x, int from, int to) => x[to] = x[from];
    [MethodImpl(NI)] static long Copied(B11[] x, int from, int to) { B11 keep = x[to]; Copy(x, from, to); long r = S(x[to]); x[to] = keep; return r; }

    static I12[] sI12;
    [MethodImpl(NI)] static long Seven(int a, int b, int c, int d, int e, int f, int g, I12 s) => S(s) + a + g;
    [MethodImpl(NI)] static long Eight(int a, int b, int c, int d, int e, int f, int g, int h, I12 s) => S(s) + a + h;
    [MethodImpl(NI)] static long Only(I12[] x, int i) => S(x[i]);
    [MethodImpl(NI)] static long AfterSeven(I12[] x, int i) => Seven(100, 2, 3, 4, 5, 6, 700, x[i]) - 800;
    [MethodImpl(NI)] static long AfterEight(I12[] x, int i) => Eight(100, 2, 3, 4, 5, 6, 7, 800, x[i]) - 900;
    [MethodImpl(NI)] static I12 Get(I12[] x, int i) => x[i];
    [MethodImpl(NI)] static void Copy(I12[] x, int from, int to) => x[to] = x[from];
    [MethodImpl(NI)] static long Copied(I12[] x, int from, int to) { I12 keep = x[to]; Copy(x, from, to); long r = S(x[to]); x[to] = keep; return r; }

    static H14[] sH14;
    [MethodImpl(NI)] static long Seven(int a, int b, int c, int d, int e, int f, int g, H14 s) => S(s) + a + g;
    [MethodImpl(NI)] static long Eight(int a, int b, int c, int d, int e, int f, int g, int h, H14 s) => S(s) + a + h;
    [MethodImpl(NI)] static long Only(H14[] x, int i) => S(x[i]);
    [MethodImpl(NI)] static long AfterSeven(H14[] x, int i) => Seven(100, 2, 3, 4, 5, 6, 700, x[i]) - 800;
    [MethodImpl(NI)] static long AfterEight(H14[] x, int i) => Eight(100, 2, 3, 4, 5, 6, 7, 800, x[i]) - 900;
    [MethodImpl(NI)] static H14 Get(H14[] x, int i) => x[i];
    [MethodImpl(NI)] static void Copy(H14[] x, int from, int to) => x[to] = x[from];
    [MethodImpl(NI)] static long Copied(H14[] x, int from, int to) { H14 keep = x[to]; Copy(x, from, to); long r = S(x[to]); x[to] = keep; return r; }

    static I20[] sI20;
    [MethodImpl(NI)] static long Seven(int a, int b, int c, int d, int e, int f, int g, I20 s) => S(s) + a + g;
    [MethodImpl(NI)] static long Eight(int a, int b, int c, int d, int e, int f, int g, int h, I20 s) => S(s) + a + h;
    [MethodImpl(NI)] static long Only(I20[] x, int i) => S(x[i]);
    [MethodImpl(NI)] static long AfterSeven(I20[] x, int i) => Seven(100, 2, 3, 4, 5, 6, 700, x[i]) - 800;
    [MethodImpl(NI)] static long AfterEight(I20[] x, int i) => Eight(100, 2, 3, 4, 5, 6, 7, 800, x[i]) - 900;
    [MethodImpl(NI)] static I20 Get(I20[] x, int i) => x[i];
    [MethodImpl(NI)] static void Copy(I20[] x, int from, int to) => x[to] = x[from];
    [MethodImpl(NI)] static long Copied(I20[] x, int from, int to) { I20 keep = x[to]; Copy(x, from, to); long r = S(x[to]); x[to] = keep; return r; }

    const int Count = 9;

    static Shape[] MakeShapes()
    {
        sB3 = new B3[Count];
        sB5 = new B5[Count];
        sH6 = new H6[Count];
        sB7 = new B7[Count];
        sB9 = new B9[Count];
        sH10 = new H10[Count];
        sB11 = new B11[Count];
        sI12 = new I12[Count];
        sH14 = new H14[Count];
        sI20 = new I20[Count];
        for (int i = 0; i < Count; i++)
        {
            sB3[i] = new B3 { A = (byte)V(i, 0), B = (byte)V(i, 1), C = (byte)V(i, 2) };
            sB5[i] = new B5 { A = (byte)V(i, 0), B = (byte)V(i, 1), C = (byte)V(i, 2), D = (byte)V(i, 3), E = (byte)V(i, 4) };
            sH6[i] = new H6 { A = (short)V(i, 0), B = (short)V(i, 1), C = (short)V(i, 2) };
            sB7[i] = new B7 { A = (byte)V(i, 0), B = (byte)V(i, 1), C = (byte)V(i, 2), D = (byte)V(i, 3), E = (byte)V(i, 4), F = (byte)V(i, 5), G = (byte)V(i, 6) };
            sB9[i] = new B9 { A = (byte)V(i, 0), B = (byte)V(i, 1), C = (byte)V(i, 2), D = (byte)V(i, 3), E = (byte)V(i, 4), F = (byte)V(i, 5), G = (byte)V(i, 6), H = (byte)V(i, 7), I = (byte)V(i, 8) };
            sH10[i] = new H10 { A = (short)V(i, 0), B = (short)V(i, 1), C = (short)V(i, 2), D = (short)V(i, 3), E = (short)V(i, 4) };
            sB11[i] = new B11 { A = (byte)V(i, 0), B = (byte)V(i, 1), C = (byte)V(i, 2), D = (byte)V(i, 3), E = (byte)V(i, 4), F = (byte)V(i, 5), G = (byte)V(i, 6), H = (byte)V(i, 7), I = (byte)V(i, 8), J = (byte)V(i, 9), K = (byte)V(i, 10) };
            sI12[i] = new I12 { A = (int)V(i, 0), B = (int)V(i, 1), C = (int)V(i, 2) };
            sH14[i] = new H14 { A = (short)V(i, 0), B = (short)V(i, 1), C = (short)V(i, 2), D = (short)V(i, 3), E = (short)V(i, 4), F = (short)V(i, 5), G = (short)V(i, 6) };
            sI20[i] = new I20 { A = (int)V(i, 0), B = (int)V(i, 1), C = (int)V(i, 2), D = (int)V(i, 3), E = (int)V(i, 4) };
        }
        return new Shape[]
        {
            new Shape { Name = "3 bytes", Fields = 3, Only = i => Only(sB3, i), AfterSeven = i => AfterSeven(sB3, i), AfterEight = i => AfterEight(sB3, i), Returned = i => S(Get(sB3, i)), Copied = (f, t) => Copied(sB3, f, t) },
            new Shape { Name = "5 bytes", Fields = 5, Only = i => Only(sB5, i), AfterSeven = i => AfterSeven(sB5, i), AfterEight = i => AfterEight(sB5, i), Returned = i => S(Get(sB5, i)), Copied = (f, t) => Copied(sB5, f, t) },
            new Shape { Name = "3 shorts", Fields = 3, Only = i => Only(sH6, i), AfterSeven = i => AfterSeven(sH6, i), AfterEight = i => AfterEight(sH6, i), Returned = i => S(Get(sH6, i)), Copied = (f, t) => Copied(sH6, f, t) },
            new Shape { Name = "7 bytes", Fields = 7, Only = i => Only(sB7, i), AfterSeven = i => AfterSeven(sB7, i), AfterEight = i => AfterEight(sB7, i), Returned = i => S(Get(sB7, i)), Copied = (f, t) => Copied(sB7, f, t) },
            new Shape { Name = "9 bytes", Fields = 9, Only = i => Only(sB9, i), AfterSeven = i => AfterSeven(sB9, i), AfterEight = i => AfterEight(sB9, i), Returned = i => S(Get(sB9, i)), Copied = (f, t) => Copied(sB9, f, t) },
            new Shape { Name = "5 shorts", Fields = 5, Only = i => Only(sH10, i), AfterSeven = i => AfterSeven(sH10, i), AfterEight = i => AfterEight(sH10, i), Returned = i => S(Get(sH10, i)), Copied = (f, t) => Copied(sH10, f, t) },
            new Shape { Name = "11 bytes", Fields = 11, Only = i => Only(sB11, i), AfterSeven = i => AfterSeven(sB11, i), AfterEight = i => AfterEight(sB11, i), Returned = i => S(Get(sB11, i)), Copied = (f, t) => Copied(sB11, f, t) },
            new Shape { Name = "3 ints", Fields = 3, Only = i => Only(sI12, i), AfterSeven = i => AfterSeven(sI12, i), AfterEight = i => AfterEight(sI12, i), Returned = i => S(Get(sI12, i)), Copied = (f, t) => Copied(sI12, f, t) },
            new Shape { Name = "7 shorts", Fields = 7, Only = i => Only(sH14, i), AfterSeven = i => AfterSeven(sH14, i), AfterEight = i => AfterEight(sH14, i), Returned = i => S(Get(sH14, i)), Copied = (f, t) => Copied(sH14, f, t) },
            new Shape { Name = "5 ints", Fields = 5, Only = i => Only(sI20, i), AfterSeven = i => AfterSeven(sI20, i), AfterEight = i => AfterEight(sI20, i), Returned = i => S(Get(sI20, i)), Copied = (f, t) => Copied(sI20, f, t) },
        };
    }

    static void Elements(Shape[] shapes, string how, Func<Shape, int, long> call)
    {
        foreach (Shape s in shapes)
            for (int i = 0; i < Count; i++)
                Eq(s.Name + " " + how + ", element " + i, call(s, i), Want(i, s.Fields));
    }

    [MethodImpl(NI)]
    static long ShortFields(int x, int which)
    {
        LocalH q;
        q.A = (short)x; q.B = (short)(x + 1); q.C = (short)(x + 2); q.D = (short)(x + 3);
        q.E = (short)(x + 4); q.F = (short)(x + 5); q.G = (short)(x + 6); q.H = (short)(x + 7);
        switch (which)
        {
            case 0: return Unsafe.ReadUnaligned<int>(ref Unsafe.As<short, byte>(ref q.A));
            case 1: return Unsafe.ReadUnaligned<int>(ref Unsafe.As<short, byte>(ref q.B));
            case 2: return Unsafe.ReadUnaligned<int>(ref Unsafe.As<short, byte>(ref q.C));
            case 3: return Unsafe.ReadUnaligned<int>(ref Unsafe.As<short, byte>(ref q.D));
            case 4: return Unsafe.ReadUnaligned<long>(ref Unsafe.As<short, byte>(ref q.A));
            case 5: return Unsafe.ReadUnaligned<long>(ref Unsafe.As<short, byte>(ref q.B));
            case 6: return Unsafe.ReadUnaligned<long>(ref Unsafe.As<short, byte>(ref q.C));
            case 7: return Unsafe.ReadUnaligned<long>(ref Unsafe.As<short, byte>(ref q.D));
            default: return Unsafe.ReadUnaligned<long>(ref Unsafe.As<short, byte>(ref q.E));
        }
    }

    [MethodImpl(NI)]
    static long ByteFields(int x, int which)
    {
        LocalB q;
        q.A = (byte)x; q.B = (byte)(x + 1); q.C = (byte)(x + 2); q.D = (byte)(x + 3);
        q.E = (byte)(x + 4); q.F = (byte)(x + 5); q.G = (byte)(x + 6); q.H = (byte)(x + 7);
        q.I = (byte)(x + 8); q.J = (byte)(x + 9); q.K = (byte)(x + 10); q.L = (byte)(x + 11);
        q.M = (byte)(x + 12); q.N = (byte)(x + 13); q.O = (byte)(x + 14); q.P = (byte)(x + 15);
        switch (which)
        {
            case 0: return Unsafe.ReadUnaligned<ushort>(ref q.B);
            case 1: return Unsafe.ReadUnaligned<int>(ref q.B);
            case 2: return Unsafe.ReadUnaligned<int>(ref q.C);
            case 3: return Unsafe.ReadUnaligned<int>(ref q.D);
            case 4: return Unsafe.ReadUnaligned<long>(ref q.B);
            case 5: return Unsafe.ReadUnaligned<long>(ref q.E);
            case 6: return Unsafe.ReadUnaligned<long>(ref q.G);
            default: return Unsafe.ReadUnaligned<long>(ref q.H);
        }
    }

    [MethodImpl(NI)]
    static long IntFields(int x, int which)
    {
        LocalI q;
        q.A = x; q.B = x + 1; q.C = x + 2; q.D = x + 3;
        switch (which)
        {
            case 0: return Unsafe.ReadUnaligned<long>(ref Unsafe.As<int, byte>(ref q.A));
            case 1: return Unsafe.ReadUnaligned<long>(ref Unsafe.As<int, byte>(ref q.B));
            default: return Unsafe.ReadUnaligned<long>(ref Unsafe.As<int, byte>(ref q.C));
        }
    }

    [MethodImpl(NI)]
    static long WriteShortFields(int which, long v)
    {
        LocalH q = default;
        switch (which)
        {
            case 0: Unsafe.WriteUnaligned(ref Unsafe.As<short, byte>(ref q.B), (int)v); break;
            case 1: Unsafe.WriteUnaligned(ref Unsafe.As<short, byte>(ref q.B), v); break;
            case 2: Unsafe.WriteUnaligned(ref Unsafe.As<short, byte>(ref q.C), v); break;
            default: Unsafe.WriteUnaligned(ref Unsafe.As<short, byte>(ref q.D), (int)v); break;
        }
        return (ushort)q.A + ((long)(ushort)q.B << 8) + ((long)(ushort)q.C << 16) + ((long)(ushort)q.D << 24) +
               ((long)(ushort)q.E << 32) + ((long)(ushort)q.F << 40) + ((long)(ushort)q.G << 44) + ((long)(ushort)q.H << 48);
    }

    static long Bytes(int x, int first, int count)
    {
        long r = 0;
        for (int i = 0; i < count; i++) r |= (long)((x + first + i) & 0xff) << (8 * i);
        return count == 4 ? (int)r : r;
    }

    static long Shorts(int x, int first, int count)
    {
        long r = 0;
        for (int i = 0; i < count; i++) r |= (long)((x + first + i) & 0xffff) << (16 * i);
        return count == 2 ? (int)r : r;
    }

    static void FieldAddresses()
    {
        const int x = 0x1110;
        for (int w = 0; w < 4; w++) Eq("int at short field " + w, ShortFields(x, w), Shorts(x, w, 2));
        for (int w = 4; w < 9; w++) Eq("long at short field " + (w - 4), ShortFields(x, w), Shorts(x, w - 4, 4));
        const int y = 0x21;
        Eq("ushort at byte field 1", ByteFields(y, 0), Bytes(y, 1, 2));
        Eq("int at byte field 1", ByteFields(y, 1), Bytes(y, 1, 4));
        Eq("int at byte field 2", ByteFields(y, 2), Bytes(y, 2, 4));
        Eq("int at byte field 3", ByteFields(y, 3), Bytes(y, 3, 4));
        Eq("long at byte field 1", ByteFields(y, 4), Bytes(y, 1, 8));
        Eq("long at byte field 4", ByteFields(y, 5), Bytes(y, 4, 8));
        Eq("long at byte field 6", ByteFields(y, 6), Bytes(y, 6, 8));
        Eq("long at byte field 7", ByteFields(y, 7), Bytes(y, 7, 8));
        const int z = 0x11111110;
        for (int w = 0; w < 3; w++)
            Eq("long at int field " + w, IntFields(z, w), ((long)(z + w + 1) << 32) | (uint)(z + w));

        const long v = 0x0807060504030201;
        Eq("write int at short field 1", WriteShortFields(0, v), (0x0201L << 8) + (0x0403L << 16));
        Eq("write long at short field 1", WriteShortFields(1, v), (0x0201L << 8) + (0x0403L << 16) + (0x0605L << 24) + (0x0807L << 32));
        Eq("write long at short field 2", WriteShortFields(2, v), (0x0201L << 16) + (0x0403L << 24) + (0x0605L << 32) + (0x0807L << 40));
        Eq("write int at short field 3", WriteShortFields(3, v), (0x0201L << 24) + (0x0403L << 32));
    }

    static int Main()
    {
        Shape[] shapes = MakeShapes();
        Group("an element as the only argument", () => Elements(shapes, "alone", (s, i) => s.Only(i)));
        Group("an element after seven arguments", () => Elements(shapes, "after seven", (s, i) => s.AfterSeven(i)));
        Group("an element after eight arguments", () => Elements(shapes, "after eight", (s, i) => s.AfterEight(i)));
        Group("an element returned", () => Elements(shapes, "returned", (s, i) => s.Returned(i)));
        Group("an element copied", () => Elements(shapes, "copied", (s, i) => s.Copied(i, (i + 4) % Count)));
        Group("unaligned accesses at field addresses", FieldAddresses);
        Console.WriteLine(fails == 0 ? "ALL PASS" : "FAIL count " + fails);
        return fails;
    }
}
