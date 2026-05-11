// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        string s = (-10).ToString();
        if (s != "-10") return 1;
        Console.WriteLine("rhp: int_format_neg10 ok");
        return 0;
    }
}
