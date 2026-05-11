// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if (string.Format("{0:D5}", 0) != "00000") return 1;
        Console.WriteLine("format: format_string_format_d5_zero ok");
        return 0;
    }
}
