// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if (string.Format("{0:x}", 255) != "ff") return 1;
        Console.WriteLine("format: format_string_format_hex_lower_255 ok");
        return 0;
    }
}
