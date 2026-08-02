// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ("v=" + true != "v=True") return 1;
        Console.WriteLine("format: string_concat_with_bool ok");
        return 0;
    }
}
