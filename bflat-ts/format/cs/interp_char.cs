// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        char c = 'A';
        if ($"{c}" != "A") return 1;
        Console.WriteLine("format: interp_char ok");
        return 0;
    }
}
