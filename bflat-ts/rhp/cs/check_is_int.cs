// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        object o = 7; if (!(o is int)) return 1;
        Console.WriteLine("rhp: check_is_int ok");
        return 0;
    }
}
