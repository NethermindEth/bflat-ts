// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        string s = (-2147483648).ToString();
        if (s != "-2147483648") return 1;
        Console.WriteLine("rhp: int_format_extreme_neg2147483648 ok");
        return 0;
    }
}
