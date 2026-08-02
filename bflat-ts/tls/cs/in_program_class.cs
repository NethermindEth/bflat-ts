// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    [ThreadStatic] static int S_local;
    static int Main()
    {
        S_local = 7;
        if (S_local != 7) return 1;
        Console.WriteLine("tls: in_program_class ok");
        return 0;
    }
}
