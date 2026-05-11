// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        char c = ' ';
        if (c.ToString() != " ") return 1;
        Console.WriteLine("format: char_to_string_20 ok");
        return 0;
    }
}
