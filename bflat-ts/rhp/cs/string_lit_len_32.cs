// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        string s = "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";
        if (s.Length != 32) return 1;
        Console.WriteLine("rhp: string_lit_len_32 ok");
        return 0;
    }
}
