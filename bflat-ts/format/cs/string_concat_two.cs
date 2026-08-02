// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ("a" + "b" != "ab") return 1;
        Console.WriteLine("format: string_concat_two ok");
        return 0;
    }
}
