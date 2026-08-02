// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        ushort[] arr = new ushort[4];
        for (int i = 0; i < arr.Length; i++) arr[i] = (ushort)i;
        if (arr.Length != 4) return 1;
        Console.WriteLine("rhp: arr_ushort_4 ok");
        return 0;
    }
}
