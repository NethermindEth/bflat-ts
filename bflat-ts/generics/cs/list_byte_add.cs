// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var l = new List<byte>();
        l.Add((byte)1);
        l.Add((byte)2);
        if (l.Count != 2) return 1;
        Console.WriteLine("generics: list_byte_add ok");
        return 0;
    }
}
