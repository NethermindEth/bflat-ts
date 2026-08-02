// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        ulong[] arr = new ulong[4];
        for (int i = 0; i < arr.Length; i++) arr[i] = (ulong)i;
        if (arr.Length != 4) return 1;
        Console.WriteLine("rhp: arr_ulong_4 ok");
        return 0;
    }
}
