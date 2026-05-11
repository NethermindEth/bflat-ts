// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ("v=" + 'A' != "v=A") return 1;
        Console.WriteLine("format: string_concat_with_char ok");
        return 0;
    }
}
