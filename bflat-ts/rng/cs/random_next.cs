// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Tests __wrap_minipal_get_cryptographically_secure_random_bytes
// via System.Random - verifies no crash and values are within requested range.
using System;

class Program
{
    static int Main()
    {
        var rng = new Random();

        // rng_stupid produces deterministic LCG values in 0..32767 range
        int a = rng.Next(0, 1000);
        int b = rng.Next(0, 1000);
        int c = rng.Next(0, 1000);

        if (a < 0 || a >= 1000) return 1;
        if (b < 0 || b >= 1000) return 1;
        if (c < 0 || c >= 1000) return 1;

        Console.WriteLine($"rng: random_next ok a={a} b={b} c={c}");
        return 0;
    }
}