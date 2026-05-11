// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

enum E { A = 1 }
class Program
{
    static int Main()
    {
        E e = (E)99;
        if (e.ToString() != "99") return 1;
        Console.WriteLine("format: enum_to_string_unknown ok");
        return 0;
    }
}
