// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Box { public int Value; }
class Program
{
    static int Main()
    {
        var b = new Box { Value = 42 };
        Console.WriteLine(b.Value);
        return 0;
    }
}
