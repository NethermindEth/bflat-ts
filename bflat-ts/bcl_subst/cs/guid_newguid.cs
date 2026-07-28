// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Guid.NewGuid draws entropy through the rng_stupid module's getrandom
// path. Version/variant bits must be correct and values must differ.
using System;

class Program
{
    static int Main()
    {
        Guid a = Guid.NewGuid();
        Guid b = Guid.NewGuid();
        if (a == b) return 1;
        if (a == Guid.Empty) return 2;
        byte[] bytes = a.ToByteArray();
        if ((bytes[7] & 0xF0) != 0x40) return 3;  // RFC 4122 version 4
        if ((bytes[8] & 0xC0) != 0x80) return 4;  // RFC 4122 variant
        Guid c = new Guid(bytes);
        if (c != a) return 5;
        Console.WriteLine("bcl_subst: guid_newguid ok");
        return 0;
    }
}
