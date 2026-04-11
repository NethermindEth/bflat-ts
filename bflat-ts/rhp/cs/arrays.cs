// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Tests __wrap_RhpNewArrayFast / __wrap_RhpNewPtrArrayFast by
// allocating arrays of value types and reference types of various
// sizes, and verifying element read-back.
using System;

class Program
{
    static int Main()
    {
        // int[] - fixed component size (4 bytes) -> RhpNewArrayFast
        int[] nums = new int[8];
        for (int i = 0; i < nums.Length; i++)
            nums[i] = i * i;
        if (nums[7] != 49) return 1;

        // string[] - pointer-sized elements -> RhpNewPtrArrayFast
        string[] words = new string[3];
        words[0] = "alpha";
        words[1] = "beta";
        words[2] = "gamma";
        if (words[1] != "beta") return 1;

        // byte[] - component size = 1
        byte[] buf = new byte[16];
        for (int i = 0; i < buf.Length; i++)
            buf[i] = (byte)i;
        if (buf[15] != 15) return 1;

        // long[] - component size = 8
        long[] longs = new long[4];
        longs[0] = 100L;
        longs[3] = 400L;
        if (longs[0] != 100L) return 1;
        if (longs[3] != 400L) return 1;

        // object[] - pointer array
        object[] objs = new object[2];
        objs[0] = new object();
        objs[1] = new object();
        if (objs[0] == null || objs[1] == null) return 1;
        if (object.ReferenceEquals(objs[0], objs[1])) return 1;

        Console.WriteLine(
            $"rhp: arrays ok nums[7]={nums[7]} words[1]={words[1]} buf[15]={buf[15]}");
        return 0;
    }
}