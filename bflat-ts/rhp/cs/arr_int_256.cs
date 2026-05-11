// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        int[] arr = new int[256];
        for (int i = 0; i < arr.Length; i++) arr[i] = i * 2;
        if (arr.Length != 256) return 1;
        Console.WriteLine("rhp: arr_int_256 ok");
        return 0;
    }
}
