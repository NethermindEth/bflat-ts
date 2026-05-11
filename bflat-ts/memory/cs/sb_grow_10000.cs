// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Text;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < 10000; i++) sb.Append('a');
        if (sb.Length != 10000) return 1;
        Console.WriteLine("memory: sb_grow_10000 ok");
        return 0;
    }
}
