// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        var r = new Random();
        byte[] buf = new byte[16];
        r.NextBytes(buf);
        if (buf.Length != 16) return 1;
        Console.WriteLine("rng: nextbytes_16 ok");
        return 0;
    }
}
