// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Text;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        Span<int> s = stackalloc int[64];
        for (int i = 0; i < 64; i++) s[i] = i;
        int sum = 0; foreach (var x in s) sum += x;
        if (sum != 2016) return 1;
        Console.WriteLine("memory: stackalloc_int_64 ok");
        return 0;
    }
}
