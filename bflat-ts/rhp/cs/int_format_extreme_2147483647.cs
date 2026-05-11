// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        string s = (2147483647).ToString();
        if (s != "2147483647") return 1;
        Console.WriteLine("rhp: int_format_extreme_2147483647 ok");
        return 0;
    }
}
