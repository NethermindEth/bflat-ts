// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var s = new Stack<int>();
        s.Push(1); s.Push(2);
        if (s.Pop() != 2) return 1;
        if (s.Pop() != 1) return 1;
        Console.WriteLine("generics: stack_basic ok");
        return 0;
    }
}
