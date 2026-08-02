// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        ulong[] arr = new ulong[16];
        for (int i = 0; i < arr.Length; i++) arr[i] = (ulong)i;
        if (arr.Length != 16) return 1;
        Console.WriteLine("rhp: arr_ulong_16 ok");
        return 0;
    }
}
