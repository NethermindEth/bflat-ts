// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var d = new Dictionary<int, int>();
        for (int i = 0; i < 5; i++) d[i] = i * 10;
        int sum = 0;
        foreach (var kv in d) sum += kv.Key + kv.Value;
        if (sum != (0+1+2+3+4) + (0+10+20+30+40)) return 1;
        Console.WriteLine("generics: dict_iterate ok");
        return 0;
    }
}
