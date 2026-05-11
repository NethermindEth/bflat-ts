// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Box {
    public int Value;
    public Box(int v) { Value = v; }
}
class Program
{
    static int Main()
    {
        var b = new Box(42);
        Console.WriteLine(b.Value);
        return 0;
    }
}
