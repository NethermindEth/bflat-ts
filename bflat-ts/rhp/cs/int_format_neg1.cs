// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        string s = (-1).ToString();
        if (s != "-1") return 1;
        Console.WriteLine("rhp: int_format_neg1 ok");
        return 0;
    }
}
