// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        sbyte[] arr = new sbyte[64];
        for (int i = 0; i < arr.Length; i++) arr[i] = (sbyte)i;
        if (arr.Length != 64) return 1;
        Console.WriteLine("rhp: arr_sbyte_64 ok");
        return 0;
    }
}
