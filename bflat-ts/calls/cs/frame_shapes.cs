// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Frames of many sizes, alone and in recursion.
//
// A frame up to 2040 bytes is allocated by one instruction. A larger one
// is allocated in two steps, and from 2048 bytes on the size of a step no
// longer fits an instruction: it is loaded into a register first, in two
// instructions whose halves carry into each other at every multiple of
// 2048. A method without a frame pointer addresses its locals, its saved
// registers and the arguments it got on the stack from SP, so beyond 2047
// bytes each of those accesses builds its address in a register too. The
// epilog releases the frame the same way before a return and before a
// call in tail position, where the registers of the arguments and of the
// target are live. On a 32-bit target a long is two words that are
// addressed one by one, and the slots are four bytes: a frame can end four
// bytes off a multiple of eight.
//
// The frame of a method here is a struct of a given size that is a local.
// Its first, middle and last words are written as a long and read back as
// its two halves, so the accesses at both ends of the frame are in the code
// whatever else is optimized. The sizes: small ones, every 8 bytes around
// 2040, and around 4096, 6144 and 8192, where the two halves of a size
// carry, then up to 32 KB; and sizes four bytes off for the 32-bit target.
//
// The cases:
//   - a method that makes no call, for every size;
//   - a method that calls itself, for every size, to a depth that the size
//     allows;
//   - one frame of each kind below over a method that returns or throws,
//     for every size;
//   - chains of calls, four and two deep, where the next method and its
//     frame size depend on the value passed down: a method that keeps
//     values across calls, one that ends in a call in tail position, one
//     that takes twelve arguments, most of them on the stack, and reads
//     them over its frame, one with a handler, one with a stackalloc of a
//     variable size and one with a finally block. The last method of a
//     chain returns or throws; the exception is caught by the nearest
//     method with a handler, or by the caller of the chain;
//   - the same chains two hundred frames deep, of the smallest frames.
//
// The stack of the smallest target is 64 KB: the chains take the frame
// sizes that fit it at their depth.
//
// Every result is compared with that of the same computation made by
// methods that have no such struct. Every group of cases runs under a
// caller that keeps twelve values across the call, one for every
// callee-saved register, and checks them after it.
//
// Each group prints one line, and one more for each case that failed; the
// run ends with ALL PASS or a FAIL count, and the guest returns the number
// of failures.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

class E : Exception { public long Code; public E(long c) : base("e") { Code = c; } }

interface IBuf
{
    long First { get; set; }
    long Mid { get; set; }
    long Last { get; set; }
    int FirstLo { get; }
    int MidHi { get; }
    int LastLo { get; }
    int LastHi { get; }
}

[StructLayout(LayoutKind.Explicit, Size = 24)] struct B24 : IBuf { [FieldOffset(0)] long f; [FieldOffset(0)] int f0; [FieldOffset(8)] long m; [FieldOffset(12)] int m1; [FieldOffset(16)] long l; [FieldOffset(16)] int l0; [FieldOffset(20)] int l1; public long First { get => f; set => f = value; } public long Mid { get => m; set => m = value; } public long Last { get => l; set => l = value; } public int FirstLo => f0; public int MidHi => m1; public int LastLo => l0; public int LastHi => l1; }
[StructLayout(LayoutKind.Explicit, Size = 64)] struct B64 : IBuf { [FieldOffset(0)] long f; [FieldOffset(0)] int f0; [FieldOffset(32)] long m; [FieldOffset(36)] int m1; [FieldOffset(56)] long l; [FieldOffset(56)] int l0; [FieldOffset(60)] int l1; public long First { get => f; set => f = value; } public long Mid { get => m; set => m = value; } public long Last { get => l; set => l = value; } public int FirstLo => f0; public int MidHi => m1; public int LastLo => l0; public int LastHi => l1; }
[StructLayout(LayoutKind.Explicit, Size = 512)] struct B512 : IBuf { [FieldOffset(0)] long f; [FieldOffset(0)] int f0; [FieldOffset(256)] long m; [FieldOffset(260)] int m1; [FieldOffset(504)] long l; [FieldOffset(504)] int l0; [FieldOffset(508)] int l1; public long First { get => f; set => f = value; } public long Mid { get => m; set => m = value; } public long Last { get => l; set => l = value; } public int FirstLo => f0; public int MidHi => m1; public int LastLo => l0; public int LastHi => l1; }
[StructLayout(LayoutKind.Explicit, Size = 1024)] struct B1024 : IBuf { [FieldOffset(0)] long f; [FieldOffset(0)] int f0; [FieldOffset(512)] long m; [FieldOffset(516)] int m1; [FieldOffset(1016)] long l; [FieldOffset(1016)] int l0; [FieldOffset(1020)] int l1; public long First { get => f; set => f = value; } public long Mid { get => m; set => m = value; } public long Last { get => l; set => l = value; } public int FirstLo => f0; public int MidHi => m1; public int LastLo => l0; public int LastHi => l1; }
[StructLayout(LayoutKind.Explicit, Size = 2000)] struct B2000 : IBuf { [FieldOffset(0)] long f; [FieldOffset(0)] int f0; [FieldOffset(1000)] long m; [FieldOffset(1004)] int m1; [FieldOffset(1992)] long l; [FieldOffset(1992)] int l0; [FieldOffset(1996)] int l1; public long First { get => f; set => f = value; } public long Mid { get => m; set => m = value; } public long Last { get => l; set => l = value; } public int FirstLo => f0; public int MidHi => m1; public int LastLo => l0; public int LastHi => l1; }
[StructLayout(LayoutKind.Explicit, Size = 2016)] struct B2016 : IBuf { [FieldOffset(0)] long f; [FieldOffset(0)] int f0; [FieldOffset(1008)] long m; [FieldOffset(1012)] int m1; [FieldOffset(2008)] long l; [FieldOffset(2008)] int l0; [FieldOffset(2012)] int l1; public long First { get => f; set => f = value; } public long Mid { get => m; set => m = value; } public long Last { get => l; set => l = value; } public int FirstLo => f0; public int MidHi => m1; public int LastLo => l0; public int LastHi => l1; }
[StructLayout(LayoutKind.Explicit, Size = 2024)] struct B2024 : IBuf { [FieldOffset(0)] long f; [FieldOffset(0)] int f0; [FieldOffset(1008)] long m; [FieldOffset(1012)] int m1; [FieldOffset(2016)] long l; [FieldOffset(2016)] int l0; [FieldOffset(2020)] int l1; public long First { get => f; set => f = value; } public long Mid { get => m; set => m = value; } public long Last { get => l; set => l = value; } public int FirstLo => f0; public int MidHi => m1; public int LastLo => l0; public int LastHi => l1; }
[StructLayout(LayoutKind.Explicit, Size = 2028)] struct B2028 : IBuf { [FieldOffset(0)] int f0; [FieldOffset(4)] int f1; [FieldOffset(1012)] int m0; [FieldOffset(1016)] int m1; [FieldOffset(2020)] int l0; [FieldOffset(2024)] int l1; public long First { get => (uint)f0 | ((long)f1 << 32); set { f0 = (int)value; f1 = (int)(value >> 32); } } public long Mid { get => (uint)m0 | ((long)m1 << 32); set { m0 = (int)value; m1 = (int)(value >> 32); } } public long Last { get => (uint)l0 | ((long)l1 << 32); set { l0 = (int)value; l1 = (int)(value >> 32); } } public int FirstLo => f0; public int MidHi => m1; public int LastLo => l0; public int LastHi => l1; }
[StructLayout(LayoutKind.Explicit, Size = 2032)] struct B2032 : IBuf { [FieldOffset(0)] long f; [FieldOffset(0)] int f0; [FieldOffset(1016)] long m; [FieldOffset(1020)] int m1; [FieldOffset(2024)] long l; [FieldOffset(2024)] int l0; [FieldOffset(2028)] int l1; public long First { get => f; set => f = value; } public long Mid { get => m; set => m = value; } public long Last { get => l; set => l = value; } public int FirstLo => f0; public int MidHi => m1; public int LastLo => l0; public int LastHi => l1; }
[StructLayout(LayoutKind.Explicit, Size = 2036)] struct B2036 : IBuf { [FieldOffset(0)] int f0; [FieldOffset(4)] int f1; [FieldOffset(1016)] int m0; [FieldOffset(1020)] int m1; [FieldOffset(2028)] int l0; [FieldOffset(2032)] int l1; public long First { get => (uint)f0 | ((long)f1 << 32); set { f0 = (int)value; f1 = (int)(value >> 32); } } public long Mid { get => (uint)m0 | ((long)m1 << 32); set { m0 = (int)value; m1 = (int)(value >> 32); } } public long Last { get => (uint)l0 | ((long)l1 << 32); set { l0 = (int)value; l1 = (int)(value >> 32); } } public int FirstLo => f0; public int MidHi => m1; public int LastLo => l0; public int LastHi => l1; }
[StructLayout(LayoutKind.Explicit, Size = 2040)] struct B2040 : IBuf { [FieldOffset(0)] long f; [FieldOffset(0)] int f0; [FieldOffset(1016)] long m; [FieldOffset(1020)] int m1; [FieldOffset(2032)] long l; [FieldOffset(2032)] int l0; [FieldOffset(2036)] int l1; public long First { get => f; set => f = value; } public long Mid { get => m; set => m = value; } public long Last { get => l; set => l = value; } public int FirstLo => f0; public int MidHi => m1; public int LastLo => l0; public int LastHi => l1; }
[StructLayout(LayoutKind.Explicit, Size = 2044)] struct B2044 : IBuf { [FieldOffset(0)] int f0; [FieldOffset(4)] int f1; [FieldOffset(1020)] int m0; [FieldOffset(1024)] int m1; [FieldOffset(2036)] int l0; [FieldOffset(2040)] int l1; public long First { get => (uint)f0 | ((long)f1 << 32); set { f0 = (int)value; f1 = (int)(value >> 32); } } public long Mid { get => (uint)m0 | ((long)m1 << 32); set { m0 = (int)value; m1 = (int)(value >> 32); } } public long Last { get => (uint)l0 | ((long)l1 << 32); set { l0 = (int)value; l1 = (int)(value >> 32); } } public int FirstLo => f0; public int MidHi => m1; public int LastLo => l0; public int LastHi => l1; }
[StructLayout(LayoutKind.Explicit, Size = 2048)] struct B2048 : IBuf { [FieldOffset(0)] long f; [FieldOffset(0)] int f0; [FieldOffset(1024)] long m; [FieldOffset(1028)] int m1; [FieldOffset(2040)] long l; [FieldOffset(2040)] int l0; [FieldOffset(2044)] int l1; public long First { get => f; set => f = value; } public long Mid { get => m; set => m = value; } public long Last { get => l; set => l = value; } public int FirstLo => f0; public int MidHi => m1; public int LastLo => l0; public int LastHi => l1; }
[StructLayout(LayoutKind.Explicit, Size = 2052)] struct B2052 : IBuf { [FieldOffset(0)] int f0; [FieldOffset(4)] int f1; [FieldOffset(1024)] int m0; [FieldOffset(1028)] int m1; [FieldOffset(2044)] int l0; [FieldOffset(2048)] int l1; public long First { get => (uint)f0 | ((long)f1 << 32); set { f0 = (int)value; f1 = (int)(value >> 32); } } public long Mid { get => (uint)m0 | ((long)m1 << 32); set { m0 = (int)value; m1 = (int)(value >> 32); } } public long Last { get => (uint)l0 | ((long)l1 << 32); set { l0 = (int)value; l1 = (int)(value >> 32); } } public int FirstLo => f0; public int MidHi => m1; public int LastLo => l0; public int LastHi => l1; }
[StructLayout(LayoutKind.Explicit, Size = 2056)] struct B2056 : IBuf { [FieldOffset(0)] long f; [FieldOffset(0)] int f0; [FieldOffset(1024)] long m; [FieldOffset(1028)] int m1; [FieldOffset(2048)] long l; [FieldOffset(2048)] int l0; [FieldOffset(2052)] int l1; public long First { get => f; set => f = value; } public long Mid { get => m; set => m = value; } public long Last { get => l; set => l = value; } public int FirstLo => f0; public int MidHi => m1; public int LastLo => l0; public int LastHi => l1; }
[StructLayout(LayoutKind.Explicit, Size = 2064)] struct B2064 : IBuf { [FieldOffset(0)] long f; [FieldOffset(0)] int f0; [FieldOffset(1032)] long m; [FieldOffset(1036)] int m1; [FieldOffset(2056)] long l; [FieldOffset(2056)] int l0; [FieldOffset(2060)] int l1; public long First { get => f; set => f = value; } public long Mid { get => m; set => m = value; } public long Last { get => l; set => l = value; } public int FirstLo => f0; public int MidHi => m1; public int LastLo => l0; public int LastHi => l1; }
[StructLayout(LayoutKind.Explicit, Size = 2072)] struct B2072 : IBuf { [FieldOffset(0)] long f; [FieldOffset(0)] int f0; [FieldOffset(1032)] long m; [FieldOffset(1036)] int m1; [FieldOffset(2064)] long l; [FieldOffset(2064)] int l0; [FieldOffset(2068)] int l1; public long First { get => f; set => f = value; } public long Mid { get => m; set => m = value; } public long Last { get => l; set => l = value; } public int FirstLo => f0; public int MidHi => m1; public int LastLo => l0; public int LastHi => l1; }
[StructLayout(LayoutKind.Explicit, Size = 2080)] struct B2080 : IBuf { [FieldOffset(0)] long f; [FieldOffset(0)] int f0; [FieldOffset(1040)] long m; [FieldOffset(1044)] int m1; [FieldOffset(2072)] long l; [FieldOffset(2072)] int l0; [FieldOffset(2076)] int l1; public long First { get => f; set => f = value; } public long Mid { get => m; set => m = value; } public long Last { get => l; set => l = value; } public int FirstLo => f0; public int MidHi => m1; public int LastLo => l0; public int LastHi => l1; }
[StructLayout(LayoutKind.Explicit, Size = 4072)] struct B4072 : IBuf { [FieldOffset(0)] long f; [FieldOffset(0)] int f0; [FieldOffset(2032)] long m; [FieldOffset(2036)] int m1; [FieldOffset(4064)] long l; [FieldOffset(4064)] int l0; [FieldOffset(4068)] int l1; public long First { get => f; set => f = value; } public long Mid { get => m; set => m = value; } public long Last { get => l; set => l = value; } public int FirstLo => f0; public int MidHi => m1; public int LastLo => l0; public int LastHi => l1; }
[StructLayout(LayoutKind.Explicit, Size = 4080)] struct B4080 : IBuf { [FieldOffset(0)] long f; [FieldOffset(0)] int f0; [FieldOffset(2040)] long m; [FieldOffset(2044)] int m1; [FieldOffset(4072)] long l; [FieldOffset(4072)] int l0; [FieldOffset(4076)] int l1; public long First { get => f; set => f = value; } public long Mid { get => m; set => m = value; } public long Last { get => l; set => l = value; } public int FirstLo => f0; public int MidHi => m1; public int LastLo => l0; public int LastHi => l1; }
[StructLayout(LayoutKind.Explicit, Size = 4088)] struct B4088 : IBuf { [FieldOffset(0)] long f; [FieldOffset(0)] int f0; [FieldOffset(2040)] long m; [FieldOffset(2044)] int m1; [FieldOffset(4080)] long l; [FieldOffset(4080)] int l0; [FieldOffset(4084)] int l1; public long First { get => f; set => f = value; } public long Mid { get => m; set => m = value; } public long Last { get => l; set => l = value; } public int FirstLo => f0; public int MidHi => m1; public int LastLo => l0; public int LastHi => l1; }
[StructLayout(LayoutKind.Explicit, Size = 4092)] struct B4092 : IBuf { [FieldOffset(0)] int f0; [FieldOffset(4)] int f1; [FieldOffset(2044)] int m0; [FieldOffset(2048)] int m1; [FieldOffset(4084)] int l0; [FieldOffset(4088)] int l1; public long First { get => (uint)f0 | ((long)f1 << 32); set { f0 = (int)value; f1 = (int)(value >> 32); } } public long Mid { get => (uint)m0 | ((long)m1 << 32); set { m0 = (int)value; m1 = (int)(value >> 32); } } public long Last { get => (uint)l0 | ((long)l1 << 32); set { l0 = (int)value; l1 = (int)(value >> 32); } } public int FirstLo => f0; public int MidHi => m1; public int LastLo => l0; public int LastHi => l1; }
[StructLayout(LayoutKind.Explicit, Size = 4096)] struct B4096 : IBuf { [FieldOffset(0)] long f; [FieldOffset(0)] int f0; [FieldOffset(2048)] long m; [FieldOffset(2052)] int m1; [FieldOffset(4088)] long l; [FieldOffset(4088)] int l0; [FieldOffset(4092)] int l1; public long First { get => f; set => f = value; } public long Mid { get => m; set => m = value; } public long Last { get => l; set => l = value; } public int FirstLo => f0; public int MidHi => m1; public int LastLo => l0; public int LastHi => l1; }
[StructLayout(LayoutKind.Explicit, Size = 4100)] struct B4100 : IBuf { [FieldOffset(0)] int f0; [FieldOffset(4)] int f1; [FieldOffset(2048)] int m0; [FieldOffset(2052)] int m1; [FieldOffset(4092)] int l0; [FieldOffset(4096)] int l1; public long First { get => (uint)f0 | ((long)f1 << 32); set { f0 = (int)value; f1 = (int)(value >> 32); } } public long Mid { get => (uint)m0 | ((long)m1 << 32); set { m0 = (int)value; m1 = (int)(value >> 32); } } public long Last { get => (uint)l0 | ((long)l1 << 32); set { l0 = (int)value; l1 = (int)(value >> 32); } } public int FirstLo => f0; public int MidHi => m1; public int LastLo => l0; public int LastHi => l1; }
[StructLayout(LayoutKind.Explicit, Size = 4104)] struct B4104 : IBuf { [FieldOffset(0)] long f; [FieldOffset(0)] int f0; [FieldOffset(2048)] long m; [FieldOffset(2052)] int m1; [FieldOffset(4096)] long l; [FieldOffset(4096)] int l0; [FieldOffset(4100)] int l1; public long First { get => f; set => f = value; } public long Mid { get => m; set => m = value; } public long Last { get => l; set => l = value; } public int FirstLo => f0; public int MidHi => m1; public int LastLo => l0; public int LastHi => l1; }
[StructLayout(LayoutKind.Explicit, Size = 6128)] struct B6128 : IBuf { [FieldOffset(0)] long f; [FieldOffset(0)] int f0; [FieldOffset(3064)] long m; [FieldOffset(3068)] int m1; [FieldOffset(6120)] long l; [FieldOffset(6120)] int l0; [FieldOffset(6124)] int l1; public long First { get => f; set => f = value; } public long Mid { get => m; set => m = value; } public long Last { get => l; set => l = value; } public int FirstLo => f0; public int MidHi => m1; public int LastLo => l0; public int LastHi => l1; }
[StructLayout(LayoutKind.Explicit, Size = 6136)] struct B6136 : IBuf { [FieldOffset(0)] long f; [FieldOffset(0)] int f0; [FieldOffset(3064)] long m; [FieldOffset(3068)] int m1; [FieldOffset(6128)] long l; [FieldOffset(6128)] int l0; [FieldOffset(6132)] int l1; public long First { get => f; set => f = value; } public long Mid { get => m; set => m = value; } public long Last { get => l; set => l = value; } public int FirstLo => f0; public int MidHi => m1; public int LastLo => l0; public int LastHi => l1; }
[StructLayout(LayoutKind.Explicit, Size = 6144)] struct B6144 : IBuf { [FieldOffset(0)] long f; [FieldOffset(0)] int f0; [FieldOffset(3072)] long m; [FieldOffset(3076)] int m1; [FieldOffset(6136)] long l; [FieldOffset(6136)] int l0; [FieldOffset(6140)] int l1; public long First { get => f; set => f = value; } public long Mid { get => m; set => m = value; } public long Last { get => l; set => l = value; } public int FirstLo => f0; public int MidHi => m1; public int LastLo => l0; public int LastHi => l1; }
[StructLayout(LayoutKind.Explicit, Size = 6152)] struct B6152 : IBuf { [FieldOffset(0)] long f; [FieldOffset(0)] int f0; [FieldOffset(3072)] long m; [FieldOffset(3076)] int m1; [FieldOffset(6144)] long l; [FieldOffset(6144)] int l0; [FieldOffset(6148)] int l1; public long First { get => f; set => f = value; } public long Mid { get => m; set => m = value; } public long Last { get => l; set => l = value; } public int FirstLo => f0; public int MidHi => m1; public int LastLo => l0; public int LastHi => l1; }
[StructLayout(LayoutKind.Explicit, Size = 8184)] struct B8184 : IBuf { [FieldOffset(0)] long f; [FieldOffset(0)] int f0; [FieldOffset(4088)] long m; [FieldOffset(4092)] int m1; [FieldOffset(8176)] long l; [FieldOffset(8176)] int l0; [FieldOffset(8180)] int l1; public long First { get => f; set => f = value; } public long Mid { get => m; set => m = value; } public long Last { get => l; set => l = value; } public int FirstLo => f0; public int MidHi => m1; public int LastLo => l0; public int LastHi => l1; }
[StructLayout(LayoutKind.Explicit, Size = 8192)] struct B8192 : IBuf { [FieldOffset(0)] long f; [FieldOffset(0)] int f0; [FieldOffset(4096)] long m; [FieldOffset(4100)] int m1; [FieldOffset(8184)] long l; [FieldOffset(8184)] int l0; [FieldOffset(8188)] int l1; public long First { get => f; set => f = value; } public long Mid { get => m; set => m = value; } public long Last { get => l; set => l = value; } public int FirstLo => f0; public int MidHi => m1; public int LastLo => l0; public int LastHi => l1; }
[StructLayout(LayoutKind.Explicit, Size = 8200)] struct B8200 : IBuf { [FieldOffset(0)] long f; [FieldOffset(0)] int f0; [FieldOffset(4096)] long m; [FieldOffset(4100)] int m1; [FieldOffset(8192)] long l; [FieldOffset(8192)] int l0; [FieldOffset(8196)] int l1; public long First { get => f; set => f = value; } public long Mid { get => m; set => m = value; } public long Last { get => l; set => l = value; } public int FirstLo => f0; public int MidHi => m1; public int LastLo => l0; public int LastHi => l1; }
[StructLayout(LayoutKind.Explicit, Size = 16384)] struct B16384 : IBuf { [FieldOffset(0)] long f; [FieldOffset(0)] int f0; [FieldOffset(8192)] long m; [FieldOffset(8196)] int m1; [FieldOffset(16376)] long l; [FieldOffset(16376)] int l0; [FieldOffset(16380)] int l1; public long First { get => f; set => f = value; } public long Mid { get => m; set => m = value; } public long Last { get => l; set => l = value; } public int FirstLo => f0; public int MidHi => m1; public int LastLo => l0; public int LastHi => l1; }
[StructLayout(LayoutKind.Explicit, Size = 32760)] struct B32760 : IBuf { [FieldOffset(0)] long f; [FieldOffset(0)] int f0; [FieldOffset(16376)] long m; [FieldOffset(16380)] int m1; [FieldOffset(32752)] long l; [FieldOffset(32752)] int l0; [FieldOffset(32756)] int l1; public long First { get => f; set => f = value; } public long Mid { get => m; set => m = value; } public long Last { get => l; set => l = value; } public int FirstLo => f0; public int MidHi => m1; public int LastLo => l0; public int LastHi => l1; }
[StructLayout(LayoutKind.Explicit, Size = 32768)] struct B32768 : IBuf { [FieldOffset(0)] long f; [FieldOffset(0)] int f0; [FieldOffset(16384)] long m; [FieldOffset(16388)] int m1; [FieldOffset(32760)] long l; [FieldOffset(32760)] int l0; [FieldOffset(32764)] int l1; public long First { get => f; set => f = value; } public long Mid { get => m; set => m = value; } public long Last { get => l; set => l = value; } public int FirstLo => f0; public int MidHi => m1; public int LastLo => l0; public int LastHi => l1; }

unsafe class Program
{
    const MethodImplOptions NI = MethodImplOptions.NoInlining;
    const int Sizes = 35;
    // The stack of the smallest target is 64 KB, so the chains use the first sizes only: up to 8200
    // bytes four frames deep, up to 16384 bytes two frames deep, and up to 64 bytes two hundred deep.
    const int ChainSizes = 32;
    const int PairSizes = 33;
    const int DeepSizes = 2;
    static readonly int[] SizeOf = { 24, 64, 512, 1024, 2000, 2016, 2024, 2028, 2032, 2036, 2040, 2044, 2048, 2052, 2056, 2064, 2072, 2080, 4072, 4080, 4088, 4092, 4096, 4100, 4104, 6128, 6136, 6144, 6152, 8184, 8192, 8200, 16384, 32760, 32768 };

    static int fails;
    static int groupFails;
    static int sizeLimit;
    static long finallyCount;

    static void Eq(string name, long got, long want)
    {
        if (got != want)
        {
            Console.WriteLine("FAIL " + name + ": got 0x" + got.ToString("x") + ", want 0x" + want.ToString("x"));
            groupFails++;
        }
    }

    [MethodImpl(NI)] static long Op(long v) => v;

    // Twelve values live across the group: one for every callee-saved register.
    [MethodImpl(NI)]
    static void Group(string name, Action body)
    {
        long k1 = Op(1), k2 = Op(2), k3 = Op(3), k4 = Op(4), k5 = Op(5), k6 = Op(6);
        long k7 = Op(7), k8 = Op(8), k9 = Op(9), k10 = Op(10), k11 = Op(11), k12 = Op(12);
        groupFails = 0;
        body();
        if (k1 != 1 || k2 != 2 || k3 != 3 || k4 != 4 || k5 != 5 || k6 != 6 ||
            k7 != 7 || k8 != 8 || k9 != 9 || k10 != 10 || k11 != 11 || k12 != 12)
        {
            Console.WriteLine("FAIL " + name + ": a value kept across the group changed");
            groupFails++;
        }
        Console.WriteLine((groupFails == 0 ? "ok " : "FAILED ") + name);
        fails += groupFails;
    }

    // The halves of a long, as the frames read them back.
    static int Lo(long v) => (int)v;
    static int Hi(long v) => (int)(v >> 32);

    // ---- a method that makes no call

    [MethodImpl(NI)]
    static long Leaf<T>(long a, long b) where T : struct, IBuf
    {
        T buf;
        Unsafe.SkipInit(out buf);
        buf.First = a;
        buf.Mid = b;
        buf.Last = a ^ (b << 1);
        return buf.FirstLo + 3L * buf.LastLo + 5L * buf.LastHi + 7L * buf.MidHi;
    }

    static long RefLeaf(long a, long b) => Lo(a) + 3L * Lo(a ^ (b << 1)) + 5L * Hi(a ^ (b << 1)) + 7L * Hi(b);

    // ---- a method that calls itself

    [MethodImpl(NI)]
    static long Rec<T>(int n, long a) where T : struct, IBuf
    {
        if (n == 0)
            return a;
        T buf;
        Unsafe.SkipInit(out buf);
        buf.First = a * 3;
        buf.Last = a ^ n;
        buf.Mid = a << 33;
        long r = Rec<T>(n - 1, a + buf.FirstLo);
        return r * 5 + buf.LastLo + buf.LastHi + buf.MidHi + buf.FirstLo;
    }

    static long RefRec(int n, long a)
    {
        if (n == 0)
            return a;
        long r = RefRec(n - 1, a + Lo(a * 3));
        return r * 5 + Lo(a ^ n) + Hi(a ^ n) + Hi(a << 33) + Lo(a * 3);
    }

    // ---- the methods of a chain

    [MethodImpl(NI)]
    static long Bottom(long a)
    {
        if ((a & 3) == 1)
            throw new E(a);
        return a;
    }

    [MethodImpl(NI)]
    static long Call<T>(int depth, long a) where T : struct, IBuf
    {
        long k1 = Op(a + 1), k2 = Op(a * 3), k3 = Op(a ^ 0x55), k4 = Op(a - 7);
        T buf;
        Unsafe.SkipInit(out buf);
        buf.First = k1;
        buf.Last = k2;
        buf.Mid = k3 << 32;
        long r = Next(depth - 1, a + 1);
        return r * 3 + buf.LastLo + 5L * buf.FirstLo + buf.MidHi + buf.LastHi + k4 + (k1 ^ k2) + k3;
    }

    static long RefCall(int depth, long a)
    {
        long k1 = a + 1, k2 = a * 3, k3 = a ^ 0x55, k4 = a - 7;
        long r = RefNext(depth - 1, a + 1);
        return r * 3 + Lo(k2) + 5L * Lo(k1) + Hi(k3 << 32) + Hi(k2) + k4 + (k1 ^ k2) + k3;
    }

    // Ends in a call in tail position: the frame is released before the jump.
    [MethodImpl(NI)]
    static long Tail<T>(int depth, long a) where T : struct, IBuf
    {
        T buf;
        Unsafe.SkipInit(out buf);
        buf.Last = a * 7;
        buf.First = a;
        buf.Mid = a << 32;
        long x = buf.LastLo + (long)buf.FirstLo + buf.MidHi;
        return Next(depth - 1, a ^ x);
    }

    static long RefTail(int depth, long a) => RefNext(depth - 1, a ^ (Lo(a * 7) + (long)Lo(a) + Hi(a << 32)));

    // Twelve arguments, read over the frame.
    [MethodImpl(NI)]
    static long Many<T>(int depth, long a1, long a2, long a3, long a4, long a5, long a6, long a7, long a8,
                        long a9, long a10, long a11, long a12) where T : struct, IBuf
    {
        T buf;
        Unsafe.SkipInit(out buf);
        buf.First = a12;
        buf.Last = a1;
        buf.Mid = a7 << 32;
        long r = Next(depth - 1, a1 + a12);
        return r + a1 + 2 * a2 + 3 * a3 + 4 * a4 + 5 * a5 + 6 * a6 + 7 * a7 + 8 * a8 + 9 * a9 + 10 * a10 +
               11 * a11 + 12 * a12 + buf.FirstLo + buf.LastLo + buf.MidHi;
    }

    [MethodImpl(NI)]
    static long Args<T>(int depth, long a) where T : struct, IBuf =>
        Many<T>(depth, a, a + 1, a + 2, a + 3, a + 4, a + 5, a + 6, a + 7, a + 8, a + 9, a + 10, a + 11);

    static long RefArgs(int depth, long a)
    {
        long r = RefNext(depth - 1, a + a + 11);
        long s = 0;
        for (int i = 0; i < 12; i++) s += (i + 1) * (a + i);
        return r + s + Lo(a + 11) + Lo(a) + Hi((a + 6) << 32);
    }

    // A handler: a frame with a frame pointer among the others.
    [MethodImpl(NI)]
    static long Catch(int depth, long a)
    {
        long k = Op(a * 11);
        long r;
        try { r = Next(depth - 1, a + 2); }
        catch (E e) { r = e.Code ^ k; }
        return r + k;
    }

    static long RefCatch(int depth, long a)
    {
        long k = a * 11;
        long r;
        try { r = RefNext(depth - 1, a + 2); }
        catch (E e) { r = e.Code ^ k; }
        return r + k;
    }

    [MethodImpl(NI)]
    static long Alloca(int depth, long a)
    {
        int n = (int)(a & 63) + 1;
        long* p = stackalloc long[n];
        p[n - 1] = a * 13;
        p[0] = a;
        long r = Next(depth - 1, a + 3);
        return r + p[n - 1] + (n > 1 ? p[0] : 0);
    }

    static long RefAlloca(int depth, long a)
    {
        int n = (int)(a & 63) + 1;
        long r = RefNext(depth - 1, a + 3);
        return r + (n > 1 ? a * 13 + a : a);
    }

    [MethodImpl(NI)]
    static long Finally(int depth, long a)
    {
        long k = Op(a + 17);
        try { return Next(depth - 1, a + 5) + k; }
        finally { finallyCount += k; }
    }

    static long RefFinally(int depth, long a)
    {
        long k = a + 17;
        try { return RefNext(depth - 1, a + 5) + k; }
        finally { finallyCount += k; }
    }

    // What the next method of a chain is: its kind and its frame size.
    static uint Pick(int depth, long a) => (uint)((ulong)(a * 2654435761L + depth * 40503L) >> 7);

    [MethodImpl(NI)]
    static long Next(int depth, long a)
    {
        if (depth <= 0)
            return Bottom(a);
        uint h = Pick(depth, a);
        int size = (int)((h / 8) % (uint)sizeLimit);
        switch (h % 8)
        {
            case 0: case 1: return CallOf(size, depth, a);
            case 2: case 3: return TailOf(size, depth, a);
            case 4: return ArgsOf(size, depth, a);
            case 5: return Catch(depth, a);
            case 6: return Alloca(depth, a);
            default: return Finally(depth, a);
        }
    }

    static long RefNext(int depth, long a)
    {
        if (depth <= 0)
            return Bottom(a);
        uint h = Pick(depth, a);
        switch (h % 8)
        {
            case 0: case 1: return RefCall(depth, a);
            case 2: case 3: return RefTail(depth, a);
            case 4: return RefArgs(depth, a);
            case 5: return RefCatch(depth, a);
            case 6: return RefAlloca(depth, a);
            default: return RefFinally(depth, a);
        }
    }

    static long LeafOf(int size, long a, long b)
    {
        switch (size)
        {
            case 0: return Leaf<B24>(a, b);
            case 1: return Leaf<B64>(a, b);
            case 2: return Leaf<B512>(a, b);
            case 3: return Leaf<B1024>(a, b);
            case 4: return Leaf<B2000>(a, b);
            case 5: return Leaf<B2016>(a, b);
            case 6: return Leaf<B2024>(a, b);
            case 7: return Leaf<B2028>(a, b);
            case 8: return Leaf<B2032>(a, b);
            case 9: return Leaf<B2036>(a, b);
            case 10: return Leaf<B2040>(a, b);
            case 11: return Leaf<B2044>(a, b);
            case 12: return Leaf<B2048>(a, b);
            case 13: return Leaf<B2052>(a, b);
            case 14: return Leaf<B2056>(a, b);
            case 15: return Leaf<B2064>(a, b);
            case 16: return Leaf<B2072>(a, b);
            case 17: return Leaf<B2080>(a, b);
            case 18: return Leaf<B4072>(a, b);
            case 19: return Leaf<B4080>(a, b);
            case 20: return Leaf<B4088>(a, b);
            case 21: return Leaf<B4092>(a, b);
            case 22: return Leaf<B4096>(a, b);
            case 23: return Leaf<B4100>(a, b);
            case 24: return Leaf<B4104>(a, b);
            case 25: return Leaf<B6128>(a, b);
            case 26: return Leaf<B6136>(a, b);
            case 27: return Leaf<B6144>(a, b);
            case 28: return Leaf<B6152>(a, b);
            case 29: return Leaf<B8184>(a, b);
            case 30: return Leaf<B8192>(a, b);
            case 31: return Leaf<B8200>(a, b);
            case 32: return Leaf<B16384>(a, b);
            case 33: return Leaf<B32760>(a, b);
            default: return Leaf<B32768>(a, b);
        }
    }

    static long RecOf(int size, int n, long a)
    {
        switch (size)
        {
            case 0: return Rec<B24>(n, a);
            case 1: return Rec<B64>(n, a);
            case 2: return Rec<B512>(n, a);
            case 3: return Rec<B1024>(n, a);
            case 4: return Rec<B2000>(n, a);
            case 5: return Rec<B2016>(n, a);
            case 6: return Rec<B2024>(n, a);
            case 7: return Rec<B2028>(n, a);
            case 8: return Rec<B2032>(n, a);
            case 9: return Rec<B2036>(n, a);
            case 10: return Rec<B2040>(n, a);
            case 11: return Rec<B2044>(n, a);
            case 12: return Rec<B2048>(n, a);
            case 13: return Rec<B2052>(n, a);
            case 14: return Rec<B2056>(n, a);
            case 15: return Rec<B2064>(n, a);
            case 16: return Rec<B2072>(n, a);
            case 17: return Rec<B2080>(n, a);
            case 18: return Rec<B4072>(n, a);
            case 19: return Rec<B4080>(n, a);
            case 20: return Rec<B4088>(n, a);
            case 21: return Rec<B4092>(n, a);
            case 22: return Rec<B4096>(n, a);
            case 23: return Rec<B4100>(n, a);
            case 24: return Rec<B4104>(n, a);
            case 25: return Rec<B6128>(n, a);
            case 26: return Rec<B6136>(n, a);
            case 27: return Rec<B6144>(n, a);
            case 28: return Rec<B6152>(n, a);
            case 29: return Rec<B8184>(n, a);
            case 30: return Rec<B8192>(n, a);
            case 31: return Rec<B8200>(n, a);
            case 32: return Rec<B16384>(n, a);
            case 33: return Rec<B32760>(n, a);
            default: return Rec<B32768>(n, a);
        }
    }

    static long CallOf(int size, int depth, long a)
    {
        switch (size)
        {
            case 0: return Call<B24>(depth, a);
            case 1: return Call<B64>(depth, a);
            case 2: return Call<B512>(depth, a);
            case 3: return Call<B1024>(depth, a);
            case 4: return Call<B2000>(depth, a);
            case 5: return Call<B2016>(depth, a);
            case 6: return Call<B2024>(depth, a);
            case 7: return Call<B2028>(depth, a);
            case 8: return Call<B2032>(depth, a);
            case 9: return Call<B2036>(depth, a);
            case 10: return Call<B2040>(depth, a);
            case 11: return Call<B2044>(depth, a);
            case 12: return Call<B2048>(depth, a);
            case 13: return Call<B2052>(depth, a);
            case 14: return Call<B2056>(depth, a);
            case 15: return Call<B2064>(depth, a);
            case 16: return Call<B2072>(depth, a);
            case 17: return Call<B2080>(depth, a);
            case 18: return Call<B4072>(depth, a);
            case 19: return Call<B4080>(depth, a);
            case 20: return Call<B4088>(depth, a);
            case 21: return Call<B4092>(depth, a);
            case 22: return Call<B4096>(depth, a);
            case 23: return Call<B4100>(depth, a);
            case 24: return Call<B4104>(depth, a);
            case 25: return Call<B6128>(depth, a);
            case 26: return Call<B6136>(depth, a);
            case 27: return Call<B6144>(depth, a);
            case 28: return Call<B6152>(depth, a);
            case 29: return Call<B8184>(depth, a);
            case 30: return Call<B8192>(depth, a);
            case 31: return Call<B8200>(depth, a);
            case 32: return Call<B16384>(depth, a);
            case 33: return Call<B32760>(depth, a);
            default: return Call<B32768>(depth, a);
        }
    }

    static long TailOf(int size, int depth, long a)
    {
        switch (size)
        {
            case 0: return Tail<B24>(depth, a);
            case 1: return Tail<B64>(depth, a);
            case 2: return Tail<B512>(depth, a);
            case 3: return Tail<B1024>(depth, a);
            case 4: return Tail<B2000>(depth, a);
            case 5: return Tail<B2016>(depth, a);
            case 6: return Tail<B2024>(depth, a);
            case 7: return Tail<B2028>(depth, a);
            case 8: return Tail<B2032>(depth, a);
            case 9: return Tail<B2036>(depth, a);
            case 10: return Tail<B2040>(depth, a);
            case 11: return Tail<B2044>(depth, a);
            case 12: return Tail<B2048>(depth, a);
            case 13: return Tail<B2052>(depth, a);
            case 14: return Tail<B2056>(depth, a);
            case 15: return Tail<B2064>(depth, a);
            case 16: return Tail<B2072>(depth, a);
            case 17: return Tail<B2080>(depth, a);
            case 18: return Tail<B4072>(depth, a);
            case 19: return Tail<B4080>(depth, a);
            case 20: return Tail<B4088>(depth, a);
            case 21: return Tail<B4092>(depth, a);
            case 22: return Tail<B4096>(depth, a);
            case 23: return Tail<B4100>(depth, a);
            case 24: return Tail<B4104>(depth, a);
            case 25: return Tail<B6128>(depth, a);
            case 26: return Tail<B6136>(depth, a);
            case 27: return Tail<B6144>(depth, a);
            case 28: return Tail<B6152>(depth, a);
            case 29: return Tail<B8184>(depth, a);
            case 30: return Tail<B8192>(depth, a);
            case 31: return Tail<B8200>(depth, a);
            case 32: return Tail<B16384>(depth, a);
            case 33: return Tail<B32760>(depth, a);
            default: return Tail<B32768>(depth, a);
        }
    }

    static long ArgsOf(int size, int depth, long a)
    {
        switch (size)
        {
            case 0: return Args<B24>(depth, a);
            case 1: return Args<B64>(depth, a);
            case 2: return Args<B512>(depth, a);
            case 3: return Args<B1024>(depth, a);
            case 4: return Args<B2000>(depth, a);
            case 5: return Args<B2016>(depth, a);
            case 6: return Args<B2024>(depth, a);
            case 7: return Args<B2028>(depth, a);
            case 8: return Args<B2032>(depth, a);
            case 9: return Args<B2036>(depth, a);
            case 10: return Args<B2040>(depth, a);
            case 11: return Args<B2044>(depth, a);
            case 12: return Args<B2048>(depth, a);
            case 13: return Args<B2052>(depth, a);
            case 14: return Args<B2056>(depth, a);
            case 15: return Args<B2064>(depth, a);
            case 16: return Args<B2072>(depth, a);
            case 17: return Args<B2080>(depth, a);
            case 18: return Args<B4072>(depth, a);
            case 19: return Args<B4080>(depth, a);
            case 20: return Args<B4088>(depth, a);
            case 21: return Args<B4092>(depth, a);
            case 22: return Args<B4096>(depth, a);
            case 23: return Args<B4100>(depth, a);
            case 24: return Args<B4104>(depth, a);
            case 25: return Args<B6128>(depth, a);
            case 26: return Args<B6136>(depth, a);
            case 27: return Args<B6144>(depth, a);
            case 28: return Args<B6152>(depth, a);
            case 29: return Args<B8184>(depth, a);
            case 30: return Args<B8192>(depth, a);
            case 31: return Args<B8200>(depth, a);
            case 32: return Args<B16384>(depth, a);
            case 33: return Args<B32760>(depth, a);
            default: return Args<B32768>(depth, a);
        }
    }

    // The outcome of a chain: its value, or the code of the exception that left it, and what the finally blocks counted.
    static long Outcome(Func<int, long, long> next, int depth, long a, out long counted)
    {
        finallyCount = 0;
        long r;
        try { r = next(depth, a); }
        catch (E e) { r = e.Code ^ 0x4000_0000_0000_0000; }
        counted = finallyCount;
        return r;
    }

    static void Chains(string name, int limit, int depth, int count)
    {
        sizeLimit = limit;
        for (int i = 0; i < count; i++)
        {
            long a = i * 0x9E3779B97F4A7C15UL.GetHashCode() + i * 1000003L + 17;
            long want = Outcome(RefNext, depth, a, out long wantCounted);
            long got = Outcome(Next, depth, a, out long gotCounted);
            Eq(name + " " + i, got, want);
            Eq(name + " " + i + ", finally blocks", gotCounted, wantCounted);
        }
    }

    static void Leaves()
    {
        for (int s = 0; s < Sizes; s++)
        {
            Eq("no call, " + SizeOf[s] + " bytes", LeafOf(s, 0x1122334455667788, 0x0102030405060708), RefLeaf(0x1122334455667788, 0x0102030405060708));
            Eq("no call, " + SizeOf[s] + " bytes, negative", LeafOf(s, -2, -0x7fffffff00000001), RefLeaf(-2, -0x7fffffff00000001));
        }
    }

    static void Recursion()
    {
        for (int s = 0; s < Sizes; s++)
        {
            // Up to 40 KB of stack for one size.
            int n = Math.Min(24, Math.Max(1, 40000 / SizeOf[s]));
            Eq("recursion, " + SizeOf[s] + " bytes, depth " + n, RecOf(s, n, 0x0123456789abcdef + s), RefRec(n, 0x0123456789abcdef + s));
        }
    }

    // One frame of each kind over the last method of a chain, which returns for the first value and throws for the second.
    static void Singles()
    {
        sizeLimit = Sizes;
        foreach (long a in new[] { 0x0123456789abcdecL, 0x0123456789abcdedL, -0x1234567800000003L })
        {
            for (int s = 0; s < Sizes; s++)
            {
                int size = s;
                Eq("keeps values, " + SizeOf[s] + " bytes", Outcome((d, x) => CallOf(size, d, x), 1, a, out _), Outcome(RefCall, 1, a, out _));
                Eq("tail call, " + SizeOf[s] + " bytes", Outcome((d, x) => TailOf(size, d, x), 1, a, out _), Outcome(RefTail, 1, a, out _));
                Eq("twelve arguments, " + SizeOf[s] + " bytes", Outcome((d, x) => ArgsOf(size, d, x), 1, a, out _), Outcome(RefArgs, 1, a, out _));
            }
        }
    }

    static int Main()
    {
        Group("methods that make no call", Leaves);
        Group("methods that call themselves", Recursion);
        Group("one frame over the last method", Singles);
        Group("chains of every frame size", () => Chains("chain", ChainSizes, 4, 400));
        Group("chains of two frames", () => Chains("pair", PairSizes, 2, 400));
        Group("deep chains", () => Chains("deep chain", DeepSizes, 200, 12));
        Console.WriteLine(fails == 0 ? "ALL PASS" : "FAIL count " + fails);
        return fails;
    }
}
