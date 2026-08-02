// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class C { public static int F() => 1; public static int G() => F() + 2; }

class Program
{
    static int Main()
    {
        if (C.G() != 3) return 1;
        Console.WriteLine("dispatch: static_method_chain ok");
        return 0;
    }
}
