// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Text;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var arr = new byte[256];
        Span<byte> s = arr;
        s[0] = 1; s[255] = 2;
        if (arr[0] + arr[255] != 3) return 1;
        Console.WriteLine("memory: span_byte_256 ok");
        return 0;
    }
}
