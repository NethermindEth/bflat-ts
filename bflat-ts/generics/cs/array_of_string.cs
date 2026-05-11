// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var a = new string[] { "a", "a" };
        if (a.Length != 2) return 1;
        Console.WriteLine("generics: array_of_string ok");
        return 0;
    }
}
