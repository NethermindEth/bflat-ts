// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main(string[] args)
    {
        uint v;
        bool ok = args.Length > 0 && uint.TryParse(args[0], out v);
        Console.WriteLine(ok);
        return 0;
    }
}
