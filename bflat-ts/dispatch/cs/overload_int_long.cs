// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class C {
    public static int F(int x) => 1;
    public static int F(long x) => 2;
}


class Program
{
    static int Main()
    {
        if (C.F(1) != 1) return 1;
        if (C.F(1L) != 2) return 1;
        Console.WriteLine("dispatch: overload_int_long ok");
        return 0;
    }
}
