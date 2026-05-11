// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        long[] arr = new long[16];
        for (int i = 0; i < arr.Length; i++) arr[i] = (long)i * 10L;
        if (arr.Length != 16) return 1;
        Console.WriteLine("rhp: arr_long_16 ok");
        return 0;
    }
}
