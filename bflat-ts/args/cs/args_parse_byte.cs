// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main(string[] args)
    {
        byte value;
        if (args.Length > 0 && byte.Parse(args[0]) is var v) value = v;
        else value = 0;
        Console.WriteLine(value);
        return 0;
    }
}
