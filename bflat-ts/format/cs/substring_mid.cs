// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ("hello".Substring(1, 3) != "ell") return 1;
        Console.WriteLine("format: substring_mid ok");
        return 0;
    }
}
