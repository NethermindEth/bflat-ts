// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        object o = (long)7L;
        long v = (long)o;
        if (!v.Equals(7L)) return 1;
        Console.WriteLine("rhp: box_unbox_long ok");
        return 0;
    }
}
