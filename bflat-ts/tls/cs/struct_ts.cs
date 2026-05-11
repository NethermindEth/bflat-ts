// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

struct P { public int X, Y; }
class Holder { [ThreadStatic] public static P V; }

class Program
{
    static int Main()
    {
        Holder.V = new P { X = 1, Y = 2 };
        if (Holder.V.X != 1 || Holder.V.Y != 2) return 1;
        Console.WriteLine("tls: struct_ts ok");
        return 0;
    }
}
