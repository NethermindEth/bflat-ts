// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        object o = (bool)true;
        bool v = (bool)o;
        if (!v.Equals(true)) return 1;
        Console.WriteLine("rhp: box_unbox_bool ok");
        return 0;
    }
}
