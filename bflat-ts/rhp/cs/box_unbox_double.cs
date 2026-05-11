// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        object o = (double)1.5;
        double v = (double)o;
        if (!v.Equals(1.5)) return 1;
        Console.WriteLine("rhp: box_unbox_double ok");
        return 0;
    }
}
