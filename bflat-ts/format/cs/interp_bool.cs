// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        bool b = true;
        if ($"{b}" != "True") return 1;
        if ($"{!b}" != "False") return 1;
        Console.WriteLine("format: interp_bool ok");
        return 0;
    }
}
