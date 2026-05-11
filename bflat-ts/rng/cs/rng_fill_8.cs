// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        byte[] b = new byte[8];
        System.Security.Cryptography.RandomNumberGenerator.Fill(b);
        if (b.Length != 8) return 1;
        Console.WriteLine("rng: rng_fill_8 ok");
        return 0;
    }
}
