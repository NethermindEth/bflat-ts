// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

enum E { A = 1, B = 2, C = 3 }
class Program
{
    static int Main()
    {
        E e = E.B;
        if (e.ToString() != "B") return 1;
        Console.WriteLine("format: enum_to_string_value ok");
        return 0;
    }
}
