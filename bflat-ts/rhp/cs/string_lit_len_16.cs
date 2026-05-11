// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        string s = "xxxxxxxxxxxxxxxx";
        if (s.Length != 16) return 1;
        Console.WriteLine("rhp: string_lit_len_16 ok");
        return 0;
    }
}
