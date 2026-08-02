// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        int[] a = { 1, 2, 3, 4 };
        int s = 0; foreach (var x in a) s += x;
        Console.WriteLine(s);
        return 0;
    }
}
