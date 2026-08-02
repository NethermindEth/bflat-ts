// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if (string.Format("{0}", 1) != "1") return 1;
        Console.WriteLine("format: format_string_format_plain1 ok");
        return 0;
    }
}
