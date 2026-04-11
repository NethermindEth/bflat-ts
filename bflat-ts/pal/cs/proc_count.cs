// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Tests that __wrap_sysconf returns 1 for SC_NPROCESSORS_ONLN (84),
// which the .NET runtime surfaces as Environment.ProcessorCount.
using System;

class Program
{
    static int Main()
    {
        int n = Environment.ProcessorCount;
        if (n != 1)
        {
            Console.WriteLine($"pal: unexpected ProcessorCount={n}");
            return 1;
        }
        Console.WriteLine($"pal: ProcessorCount={n} ok");
        return 0;
    }
}