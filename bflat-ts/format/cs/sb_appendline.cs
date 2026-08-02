// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

using System.Text;
class Program
{
    static int Main()
    {
        var sb = new StringBuilder();
        sb.AppendLine("hi");
        string s = sb.ToString();
        if (!s.StartsWith("hi")) return 1;
        Console.WriteLine("format: sb_appendline ok");
        return 0;
    }
}
