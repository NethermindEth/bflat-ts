// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        string s = 12345.ToString();
        if (s != "12345") return 1;
        Console.WriteLine("rhp: number_fmt ok");
        return 0;
    }
}
