// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        int a = 10; string s = "v"; bool b = true; double d = 1.5;
        string r = $"{a}|{s}|{b}|{d}";
        if (r != "10|v|True|1.5") return 1;
        Console.WriteLine("format: interp_mix_a ok");
        return 0;
    }
}
