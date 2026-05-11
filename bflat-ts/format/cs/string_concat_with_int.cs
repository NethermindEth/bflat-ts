// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ("v=" + 42 != "v=42") return 1;
        Console.WriteLine("format: string_concat_with_int ok");
        return 0;
    }
}
