// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        string s = (0).ToString();
        if (s != "0") return 1;
        Console.WriteLine("rhp: int_format_0 ok");
        return 0;
    }
}
