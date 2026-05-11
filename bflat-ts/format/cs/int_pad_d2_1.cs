// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ((1).ToString("D2") != "01") return 1;
        Console.WriteLine("format: int_pad_d2_1 ok");
        return 0;
    }
}
