// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ("7".PadLeft(3, '0') != "007") return 1;
        Console.WriteLine("format: padleft_3_char ok");
        return 0;
    }
}
