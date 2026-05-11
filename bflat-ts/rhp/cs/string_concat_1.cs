// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        string s = "";
        for (int i = 0; i < 1; i++) s += "a";
        if (s.Length != 1) return 1;
        Console.WriteLine("rhp: string_concat_1 ok");
        return 0;
    }
}
