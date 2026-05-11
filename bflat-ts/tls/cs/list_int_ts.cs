// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

using System.Collections.Generic;
class Holder { [ThreadStatic] public static List<int> V; }
class Program
{
    static int Main()
    {
        Holder.V = new List<int> { 1, 2, 3 };
        if (Holder.V.Count != 3) return 1;
        Console.WriteLine("tls: list_int_ts ok");
        return 0;
    }
}
