// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ((12).ToString("D4") != "0012") return 1;
        Console.WriteLine("format: int_pad_d4_12 ok");
        return 0;
    }
}
