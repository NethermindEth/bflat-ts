// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Text;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < 100; i++) sb.Append(i);
        if (sb.Length == 0) return 1;
        Console.WriteLine("memory: sb_append_int_100 ok");
        return 0;
    }
}
