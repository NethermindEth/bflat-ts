// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        long[] arr = new long[1];
        for (int i = 0; i < arr.Length; i++) arr[i] = (long)i * 10L;
        if (arr.Length != 1) return 1;
        Console.WriteLine("rhp: arr_long_1 ok");
        return 0;
    }
}
