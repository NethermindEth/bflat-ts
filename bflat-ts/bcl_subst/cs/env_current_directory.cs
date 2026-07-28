// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Environment.CurrentDirectory reads through the wrapped getcwd.
using System;

class Program
{
    static int Main()
    {
        string cwd = Environment.CurrentDirectory;
        if (cwd == null) return 1;
        if (cwd.Length == 0) return 2;
        if (cwd[0] != '/') return 3;
        Console.WriteLine("bcl_subst: env_current_directory ok");
        return 0;
    }
}
