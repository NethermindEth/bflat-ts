// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        var parts = "a/b/c/d".Split('/');
        if (parts.Length != 4) return 1;
        Console.WriteLine("format: split_multi ok");
        return 0;
    }
}
