// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ((123).ToString("D6") != "000123") return 1;
        Console.WriteLine("format: int_pad_d6_123 ok");
        return 0;
    }
}
