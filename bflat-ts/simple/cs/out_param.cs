// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static void Get(out int x) { x = 7; }

    static int Main()
    {
        int v; Get(out v); Console.WriteLine(v);
        return 0;
    }
}
