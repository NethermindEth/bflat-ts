// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        object o = 7;
        int v = (int)o;
        if (v != 7) return 1;
        Console.WriteLine("rhp: type_cast ok");
        return 0;
    }
}
