// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        var r1 = new Random(1);
        var r2 = new Random(1);
        for (int i = 0; i < 16; i++) {
            int a = r1.Next(); int b = r2.Next();
            if (a != b) return 1;
        }
        Console.WriteLine("rng: seeded_reproducible_1 ok");
        return 0;
    }
}
