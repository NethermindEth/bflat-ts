// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Text;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var arr = new byte[1024];
        Span<byte> s = arr;
        s[0] = 1; s[1023] = 2;
        if (arr[0] + arr[1023] != 3) return 1;
        Console.WriteLine("memory: span_byte_1024 ok");
        return 0;
    }
}
