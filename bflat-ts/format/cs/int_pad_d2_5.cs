// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ((5).ToString("D2") != "05") return 1;
        Console.WriteLine("format: int_pad_d2_5 ok");
        return 0;
    }
}
