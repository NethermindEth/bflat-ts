// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        if (default(string) != null) return 1;
        Console.WriteLine("generics: default_string ok");
        return 0;
    }
}
