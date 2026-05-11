// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        string u = Environment.UserName;
        GC.KeepAlive(u);
        Console.WriteLine("pal: user_name ok");
        return 0;
    }
}
