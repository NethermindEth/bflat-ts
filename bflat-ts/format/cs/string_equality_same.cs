// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ("a".Equals("a").ToString() != "True") return 1;
        Console.WriteLine("format: string_equality_same ok");
        return 0;
    }
}
