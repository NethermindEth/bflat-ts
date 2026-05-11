// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Text;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        Span<int> s = stackalloc int[4];
        for (int i = 0; i < 4; i++) s[i] = i;
        int sum = 0; foreach (var x in s) sum += x;
        if (sum != 6) return 1;
        Console.WriteLine("memory: stackalloc_int_4 ok");
        return 0;
    }
}
