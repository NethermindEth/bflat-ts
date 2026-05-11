// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Text;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        string s = "";
        for (int i = 0; i < 10; i++) s = s + "x";
        if (s.Length != 10) return 1;
        Console.WriteLine("memory: string_alloc_10 ok");
        return 0;
    }
}
