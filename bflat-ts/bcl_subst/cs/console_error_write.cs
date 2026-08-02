// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Console.Error goes out through the wrapped FP-free vfprintf/stdio path;
// writing and flushing must not disturb execution.
using System;

class Program
{
    static int Main()
    {
        Console.Error.WriteLine("stderr line one");
        Console.Error.Write("stderr ");
        Console.Error.WriteLine(12345);
        Console.Error.Flush();
        Console.WriteLine("bcl_subst: console_error_write ok");
        return 0;
    }
}
