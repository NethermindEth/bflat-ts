// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        sbyte[] arr = new sbyte[8];
        for (int i = 0; i < arr.Length; i++) arr[i] = (sbyte)i;
        if (arr.Length != 8) return 1;
        Console.WriteLine("rhp: arr_sbyte_8 ok");
        return 0;
    }
}
