// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        var r = new Random();
        byte[] buf = new byte[256];
        r.NextBytes(buf);
        if (buf.Length != 256) return 1;
        Console.WriteLine("rng: nextbytes_256 ok");
        return 0;
    }
}
