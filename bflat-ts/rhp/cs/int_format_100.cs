// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        string s = (100).ToString();
        if (s != "100") return 1;
        Console.WriteLine("rhp: int_format_100 ok");
        return 0;
    }
}
