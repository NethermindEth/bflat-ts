// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Array lengths that must be refused: negative, larger than the largest
// array, and larger than the heap.
//
// `new T[n]` with n < 0 throws OverflowException, and a length the heap
// cannot hold throws OutOfMemoryException; a program may catch both and go
// on (a decoder that takes a length from its input does). The allocation
// helper does that. The allocation sequence the JIT emits in its place for
// a zkVM guest has two checks of its own and no way to throw:
//
//   - the length is compared, unsigned, with the largest array length;
//   - the new heap pointer is compared, unsigned, with the heap floor kept
//     in the cell under the pointer.
//
// What can go wrong, besides the exception becoming the end of the guest:
//
//   - a length whose low 32 bits are small and whose high bits are not
//     (a long, a uint): cut to 32 bits it would make a short array;
//   - a size larger than the heap pointer itself: pointer - size wraps to a
//     huge unsigned value, passes the floor check and the stores go to a
//     wild address;
//   - a floor cell that was never set: the check compares with zero and
//     never fires;
//   - a constant length: the check is folded at compile time and must fold
//     to the same answer.
//
// The cases run in order of rising danger, and each prints its name before
// it runs: when the guest dies in a case, the last `case` line names it and
// nothing after it has run. A case that returns an array, or throws another
// exception, prints FAIL. The run ends with ALL PASS or a FAIL count, and
// the guest returns the number of failures.
using System;
using System.Runtime.CompilerServices;

static class Program
{
    static int fails;
    static object s_sink;

    delegate object Make();

    static void Expect(string name, string exception, Make make)
    {
        Console.WriteLine("case " + name);
        string got;
        try
        {
            object o = make();
            got = "an array of length " + ((Array)o).Length;
        }
        catch (Exception e) { got = e.GetType().Name; }
        if (got != exception)
        {
            fails++;
            Console.WriteLine("FAIL " + name + " expected " + exception + " got " + got);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)] static int MinusOne() => -1;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] static T[] Inl<T>(int n) => new T[n];

    [MethodImpl(MethodImplOptions.NoInlining)] static int[] NegativeConstant() => new int[MinusOne()];
    [MethodImpl(MethodImplOptions.NoInlining)] static byte[] TooLongConstant() => Inl<byte>(int.MaxValue);
    [MethodImpl(MethodImplOptions.NoInlining)] static byte[] Bytes(int n) => new byte[n];
    [MethodImpl(MethodImplOptions.NoInlining)] static int[] Ints(int n) => new int[n];
    [MethodImpl(MethodImplOptions.NoInlining)] static long[] Longs(int n) => new long[n];
    [MethodImpl(MethodImplOptions.NoInlining)] static object[] Objects(int n) => new object[n];
    [MethodImpl(MethodImplOptions.NoInlining)] static Guid[] Guids(int n) => new Guid[n];
    [MethodImpl(MethodImplOptions.NoInlining)] static int[] IntsLong(long n) => new int[n];
    [MethodImpl(MethodImplOptions.NoInlining)] static int[] IntsUInt(uint n) => new int[n];
    [MethodImpl(MethodImplOptions.NoInlining)] static int[] IntsULong(ulong n) => new int[n];
    [MethodImpl(MethodImplOptions.NoInlining)] static long[] HugeConstant() => Inl<long>(0x7FFFFFC7);
    [MethodImpl(MethodImplOptions.NoInlining)] static byte[] BigConstant() => Inl<byte>(0x7FFFFFC7);
    [MethodImpl(MethodImplOptions.NoInlining)] static int Opaque(int x) => x;

    static int Main()
    {
        // Constants the helper rejects: the JIT has to leave the helper call.
        Expect("constant -1", "OverflowException", () => NegativeConstant());
        Expect("constant int.MaxValue of byte", "OutOfMemoryException", () => TooLongConstant());

        // Negative lengths.
        Expect("int -1", "OverflowException", () => Ints(Opaque(-1)));
        Expect("int int.MinValue", "OverflowException", () => Ints(Opaque(int.MinValue)));
        Expect("object -5", "OverflowException", () => Objects(Opaque(-5)));
        Expect("long -1", "OverflowException", () => IntsLong(Opaque(-1)));
        Expect("long long.MinValue + 3", "OverflowException", () => IntsLong(long.MinValue + Opaque(3)));

        // Too large for any array; the low 32 bits alone are a small length.
        Expect("long 0x100000005", "OverflowException", () => IntsLong(0x100000000L + Opaque(5)));
        Expect("ulong 0x8000000000000002", "OverflowException", () => IntsULong(0x8000000000000000UL + (ulong)Opaque(2)));
        Expect("uint 0x80000003", "OverflowException", () => IntsUInt(0x80000000u + (uint)Opaque(3)));
        Expect("int 0x7FFFFFC8 of byte", "OutOfMemoryException", () => Bytes(Opaque(0x7FFFFFC8)));
        Expect("int int.MaxValue of long", "OutOfMemoryException", () => Longs(Opaque(int.MaxValue)));

        // A valid length, more memory than the guest has.
        Expect("int 0x7FFFFFC7 of byte", "OutOfMemoryException", () => Bytes(Opaque(0x7FFFFFC7)));
        Expect("constant 0x7FFFFFC7 of byte", "OutOfMemoryException", () => BigConstant());
        // The size is larger than the heap pointer: pointer - size wraps.
        Expect("int 0x7FFFFFC7 of int", "OutOfMemoryException", () => Ints(Opaque(0x7FFFFFC7)));
        Expect("int 0x7FFFFFC7 of long", "OutOfMemoryException", () => Longs(Opaque(0x7FFFFFC7)));
        Expect("int 0x7FFFFFC7 of Guid", "OutOfMemoryException", () => Guids(Opaque(0x7FFFFFC7)));
        Expect("constant 0x7FFFFFC7 of long", "OutOfMemoryException", () => HugeConstant());

        Console.WriteLine(fails == 0 ? "ALL PASS" : "FAILED " + fails);
        return fails;
    }
}
