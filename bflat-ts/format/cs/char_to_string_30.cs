// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        char c = '0';
        if (c.ToString() != "0") return 1;
        Console.WriteLine("format: char_to_string_30 ok");
        return 0;
    }
}
