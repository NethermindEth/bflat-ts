// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        string v = Environment.GetEnvironmentVariable("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT");
        if (v != "1") return 1;
        Console.WriteLine("pal: env_dotnet_system_globalization_invariant ok");
        return 0;
    }
}
