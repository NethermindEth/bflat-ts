// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if (ulong.MaxValue.ToString() != "18446744073709551615") return 1;
        Console.WriteLine("format: ulong_to_string_max ok");
        return 0;
    }
}
