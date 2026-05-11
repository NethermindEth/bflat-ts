// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ("v=" + 1.5 != "v=1.5") return 1;
        Console.WriteLine("format: string_concat_with_double ok");
        return 0;
    }
}
