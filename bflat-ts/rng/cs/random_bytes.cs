// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        var r = new Random();
        byte[] b = new byte[32];
        r.NextBytes(b);
        if (b.Length != 32) return 1;
        Console.WriteLine("rng: random_bytes ok");
        return 0;
    }
}
