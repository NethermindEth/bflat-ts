// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if (string.Format("{0}", 42) != "42") return 1;
        Console.WriteLine("format: format_string_format_plain42 ok");
        return 0;
    }
}
