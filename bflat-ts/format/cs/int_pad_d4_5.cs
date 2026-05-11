// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ((5).ToString("D4") != "0005") return 1;
        Console.WriteLine("format: int_pad_d4_5 ok");
        return 0;
    }
}
