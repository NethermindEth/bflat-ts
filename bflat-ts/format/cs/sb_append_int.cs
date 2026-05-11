// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

using System.Text;
class Program
{
    static int Main()
    {
        var sb = new StringBuilder();
        sb.Append(42);
        if (sb.ToString() != "42") return 1;
        Console.WriteLine("format: sb_append_int ok");
        return 0;
    }
}
