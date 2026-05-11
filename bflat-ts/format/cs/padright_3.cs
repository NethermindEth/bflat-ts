// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ("7".PadRight(3) != "7  ") return 1;
        Console.WriteLine("format: padright_3 ok");
        return 0;
    }
}
