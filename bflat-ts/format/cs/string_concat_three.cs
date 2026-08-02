// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ("a" + "b" + "c" != "abc") return 1;
        Console.WriteLine("format: string_concat_three ok");
        return 0;
    }
}
