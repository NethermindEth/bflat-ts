// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        object o = "hi"; if (!(o is string)) return 1;
        Console.WriteLine("rhp: check_is_string ok");
        return 0;
    }
}
