// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Tests __wrap_minipal_get_cryptographically_secure_random_bytes
// via System.Security.Cryptography.RandomNumberGenerator.Fill().
using System;
using System.Security.Cryptography;

class Program
{
    static int Main()
    {
        byte[] buf = new byte[32];
        RandomNumberGenerator.Fill(buf);

        // The LCG in rng_stupid always produces values in 0..0xFF,
        // so the call must succeed without crashing.
        Console.WriteLine($"rng: filled {buf.Length} bytes ok");
        return 0;
    }
}