// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if (string.Format("{0:X}", 255) != "FF") return 1;
        Console.WriteLine("format: format_string_format_hex_upper_255 ok");
        return 0;
    }
}
