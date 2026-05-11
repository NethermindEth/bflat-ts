// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        string s = "";
        for (int i = 0; i < 5; i++) s += "a";
        if (s.Length != 5) return 1;
        Console.WriteLine("rhp: string_concat_5 ok");
        return 0;
    }
}
