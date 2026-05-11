// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        object o = "hi"; var s = o as string; if (s != "hi") return 1;
        Console.WriteLine("rhp: check_as_string ok");
        return 0;
    }
}
