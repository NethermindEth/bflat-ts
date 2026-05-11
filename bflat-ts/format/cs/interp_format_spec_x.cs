// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        int x = 255;
        if ($"{x:X}" != "FF") return 1;
        Console.WriteLine("format: interp_format_spec_x ok");
        return 0;
    }
}
