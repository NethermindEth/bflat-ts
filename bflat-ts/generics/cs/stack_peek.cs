// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var s = new Stack<int>();
        s.Push(7);
        if (s.Peek() != 7) return 1;
        if (s.Count != 1) return 1;
        Console.WriteLine("generics: stack_peek ok");
        return 0;
    }
}
