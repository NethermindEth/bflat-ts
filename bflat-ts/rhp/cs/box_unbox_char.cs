// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        object o = (char)'A';
        char v = (char)o;
        if (!v.Equals('A')) return 1;
        Console.WriteLine("rhp: box_unbox_char ok");
        return 0;
    }
}
