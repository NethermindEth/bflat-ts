// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// finally must run on the normal (no-throw) path.
using System;

class Program
{
    static int Main()
    {
        int order = 0;
        try
        {
            if (order != 0) return 1;
            order = 1;
        }
        finally
        {
            if (order != 1) order = -1;
            else order = 2;
        }
        if (order != 2) return 2;
        Console.WriteLine("eh: eh_try_finally_normal ok");
        return 0;
    }
}
