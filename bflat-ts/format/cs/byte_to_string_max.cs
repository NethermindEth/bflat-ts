// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if (byte.MaxValue.ToString() != "255") return 1;
        Console.WriteLine("format: byte_to_string_max ok");
        return 0;
    }
}
