// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

using System.Text;
class Program
{
    static int Main()
    {
        var sb = new StringBuilder();
        sb.Append("hi");
        sb.Clear();
        if (sb.Length != 0) return 1;
        Console.WriteLine("format: sb_clear ok");
        return 0;
    }
}
