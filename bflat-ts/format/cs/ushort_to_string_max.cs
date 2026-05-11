// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if (ushort.MaxValue.ToString() != "65535") return 1;
        Console.WriteLine("format: ushort_to_string_max ok");
        return 0;
    }
}
