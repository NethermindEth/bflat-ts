// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Same-element-type Array.Copy takes the memmove fast path, which stays
// intact while CopyImplPrimitiveWiden is removed. Includes overlapping copy.
using System;

class Program
{
    static int Main()
    {
        int[] src = new int[16];
        for (int i = 0; i < 16; i++) src[i] = i * i;
        int[] dst = new int[16];
        Array.Copy(src, dst, 16);
        for (int i = 0; i < 16; i++)
            if (dst[i] != i * i) return 1;
        Array.Copy(src, 0, src, 4, 8);
        if (src[4] != 0) return 2;
        if (src[11] != 49) return 3;
        Console.WriteLine("bcl_subst: array_copy_int ok");
        return 0;
    }
}
