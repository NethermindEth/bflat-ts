// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Exercises: string interpolation ($"...{expr}...") with various types
// Bug targeted: string concat path, __wrap_RhNewString, StringBuilder for format

using System;

class Program
{
    static int Main()
    {
        // Basic int interpolation
        int a = 42, b = 7;
        string s1 = $"{a}+{b}={a + b}";
        if (s1 != "42+7=49") return 1;

        // Bool negation interpolation
        bool f = false;
        string s2 = $"{!f}";
        if (s2 != "True") return 1;

        // String variable interpolation
        string s = "hi";
        string s3 = $"[{s}]";
        if (s3 != "[hi]") return 1;

        // Simple int-only interpolation
        string s4 = $"{42}";
        if (s4 != "42") return 1;

        // Multiple types in one interpolation
        int x = 10;
        string label = "val";
        string s5 = $"{label}={x}";
        if (s5 != "val=10") return 1;

        // Negative number interpolation
        int neg = -7;
        string s6 = $"{neg}";
        if (s6 != "-7") return 1;

        // Zero interpolation
        int zero = 0;
        string s7 = $"{zero}";
        if (s7 != "0") return 1;

        // Concatenation of interpolated strings
        string part1 = $"hello";
        string part2 = $"world";
        string joined = part1 + " " + part2;
        if (joined != "hello world") return 1;

        Console.WriteLine("format: string interpolation ok");
        return 0;
    }
}