// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Box {
    public int F0;
}
class Program
{
    static int Main()
    {
        var b = new Box();
        b.F0 = 7;
        if (b.F0 != 7) return 1;
        Console.WriteLine("rhp: new_class_fields_1 ok");
        return 0;
    }
}
