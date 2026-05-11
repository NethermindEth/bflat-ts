// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        object o = "hi"; var s = o as int?; if (s != null) return 1;
        Console.WriteLine("rhp: check_as_null ok");
        return 0;
    }
}
