// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        int a = 3;
        string s = a switch { 1 => "one", 2 => "two", _ => "many" };
        Console.WriteLine(s);
        return 0;
    }
}
