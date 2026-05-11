// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        (int a, int b) t = (1, 2);
        Console.WriteLine(t.a + t.b);
        return 0;
    }
}
