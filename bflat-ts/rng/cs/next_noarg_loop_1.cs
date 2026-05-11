// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        var r = new Random();
        for (int i = 0; i < 1; i++) { int v = r.Next(); if (v < 0) return 1; }
        Console.WriteLine("rng: next_noarg_loop_1 ok");
        return 0;
    }
}
