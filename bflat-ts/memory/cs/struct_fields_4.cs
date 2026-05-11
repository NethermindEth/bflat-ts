// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Text;
using System.Collections.Generic;

struct S
{
    public int F0;
    public int F1;
    public int F2;
    public int F3;
}
class Program
{
    static int Main()
    {
        S s = new S();
        s.F0 = 5;
        s.F3 = 7;
        if (s.F0 + s.F3 != 12) return 1;
        Console.WriteLine("memory: struct_fields_4 ok");
        return 0;
    }
}
