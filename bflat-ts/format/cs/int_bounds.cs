// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Exercises: int.ToString(), uint.ToString() for boundary values
// Bug targeted: UInt32ToDecStr, Int32ToDecStr for min/max/negative values

using System;

class Program
{
    static int Main()
    {
        // Zero
        if (0.ToString() != "0") return 1;

        // Negative one
        if ((-1).ToString() != "-1") return 1;

        // Small positive
        if (42.ToString() != "42") return 1;

        // int.MaxValue
        if (int.MaxValue.ToString() != "2147483647") return 1;

        // int.MinValue
        if (int.MinValue.ToString() != "-2147483648") return 1;

        // uint.MaxValue
        if (uint.MaxValue.ToString() != "4294967295") return 1;

        // Negative hundred
        if ((-100).ToString() != "-100") return 1;

        // One million
        if (1000000.ToString() != "1000000") return 1;

        Console.WriteLine("format: int_bounds ok");
        return 0;
    }
}