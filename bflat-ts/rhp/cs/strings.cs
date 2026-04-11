// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Tests __wrap_RhNewString by exercising string allocation,
// concatenation, and formatting.
using System;

class Program
{
    static int Main()
    {
        string s1 = "Hello";
        string s2 = ", World";
        string s3 = s1 + s2;
        if (s3 != "Hello, World") return 1;

        string fmt = string.Format("x={0} y={1}", 10, 20);
        if (fmt != "x=10 y=20") return 1;

        string sub = "abcdef".Substring(2, 3);
        if (sub != "cde") return 1;

        Console.WriteLine($"rhp: strings ok s3={s3}");
        return 0;
    }
}