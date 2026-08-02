// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        string s = "hi";
        switch (s) {
            case "hi": Console.WriteLine("greet"); break;
            default: Console.WriteLine("other"); break;
        }
        return 0;
    }
}
