// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ("v=" + 42L != "v=42") return 1;
        Console.WriteLine("format: string_concat_with_long ok");
        return 0;
    }
}
