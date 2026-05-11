// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Holder { [ThreadStatic] public static string[] V; }
class Program
{
    static int Main()
    {
        Holder.V = new string[] { "a", "b" };
        if (Holder.V == null || Holder.V.Length == 0) return 1;
        Console.WriteLine("tls: array_string_arr ok");
        return 0;
    }
}
