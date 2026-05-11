// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        object o = (int)7;
        int v = (int)o;
        if (!v.Equals(7)) return 1;
        Console.WriteLine("rhp: box_unbox_int ok");
        return 0;
    }
}
