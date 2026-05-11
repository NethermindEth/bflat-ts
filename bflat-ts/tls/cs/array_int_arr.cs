// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Holder { [ThreadStatic] public static int[] V; }
class Program
{
    static int Main()
    {
        Holder.V = new int[] { 1, 2, 3 };
        if (Holder.V == null || Holder.V.Length == 0) return 1;
        Console.WriteLine("tls: array_int_arr ok");
        return 0;
    }
}
