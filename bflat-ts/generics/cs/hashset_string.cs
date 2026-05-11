// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var s = new HashSet<string>();
        s.Add("a"); s.Add("b"); s.Add("a");
        if (s.Count != 2) return 1;
        Console.WriteLine("generics: hashset_string ok");
        return 0;
    }
}
