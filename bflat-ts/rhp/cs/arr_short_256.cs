// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        short[] arr = new short[256];
        for (int i = 0; i < arr.Length; i++) arr[i] = (short)i;
        if (arr.Length != 256) return 1;
        Console.WriteLine("rhp: arr_short_256 ok");
        return 0;
    }
}
