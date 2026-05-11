// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Text;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        string s = "";
        for (int i = 0; i < 100; i++) s = s + "x";
        if (s.Length != 100) return 1;
        Console.WriteLine("memory: string_alloc_100 ok");
        return 0;
    }
}
