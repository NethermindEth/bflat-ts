// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        string n = "x";
        if ($"[{n}]" != "[x]") return 1;
        Console.WriteLine("format: interp_with_string_var ok");
        return 0;
    }
}
