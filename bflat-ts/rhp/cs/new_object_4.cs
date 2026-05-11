// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        object o = null;
        for (int i = 0; i < 4; i++) o = new object();
        if (o == null) return 1;
        Console.WriteLine("rhp: new_object_4 ok");
        return 0;
    }
}
