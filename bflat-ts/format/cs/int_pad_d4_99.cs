// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ((99).ToString("D4") != "0099") return 1;
        Console.WriteLine("format: int_pad_d4_99 ok");
        return 0;
    }
}
