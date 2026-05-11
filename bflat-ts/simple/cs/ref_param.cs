// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static void Inc(ref int x) { x++; }

    static int Main()
    {
        int v = 1; Inc(ref v); Console.WriteLine(v);
        return 0;
    }
}
