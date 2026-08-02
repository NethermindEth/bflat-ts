// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        object[] objs = new object[2];
        objs[0] = new object(); objs[1] = new object();
        if (object.ReferenceEquals(objs[0], objs[1])) return 1;
        Console.WriteLine("rhp: objects ok");
        return 0;
    }
}
