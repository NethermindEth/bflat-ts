// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Text;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        char[] c = new char[256];
        for (int i = 0; i < 256; i++) c[i] = (char)('a' + i % 26);
        if (c[0] != 'a') return 1;
        Console.WriteLine("memory: char_array_256 ok");
        return 0;
    }
}
