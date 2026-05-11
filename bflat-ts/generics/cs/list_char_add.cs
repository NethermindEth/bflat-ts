// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var l = new List<char>();
        l.Add('a');
        l.Add('b');
        if (l.Count != 2) return 1;
        Console.WriteLine("generics: list_char_add ok");
        return 0;
    }
}
