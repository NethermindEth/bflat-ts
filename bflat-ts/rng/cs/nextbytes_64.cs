// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        var r = new Random();
        byte[] buf = new byte[64];
        r.NextBytes(buf);
        if (buf.Length != 64) return 1;
        Console.WriteLine("rng: nextbytes_64 ok");
        return 0;
    }
}
