// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class C { public static int F() => 42; }

class Program
{
    static int Main()
    {
        if (C.F() != 42) return 1;
        Console.WriteLine("dispatch: static_method_call ok");
        return 0;
    }
}
