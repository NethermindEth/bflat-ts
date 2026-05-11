// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ((1).ToString("D4") != "0001") return 1;
        Console.WriteLine("format: int_pad_d4_1 ok");
        return 0;
    }
}
