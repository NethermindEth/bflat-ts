// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

using System.Text;
class Program
{
    static int Main()
    {
        var sb = new StringBuilder();
        sb.Append("hello");
        if (sb.ToString() != "hello") return 1;
        Console.WriteLine("format: sb_append_string ok");
        return 0;
    }
}
