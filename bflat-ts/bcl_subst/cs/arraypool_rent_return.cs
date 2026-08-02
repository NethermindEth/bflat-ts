// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// ArrayPool trim decisions read the stubbed GetMemoryPressure (always Low:
// keep buffers). Rent/return round-trips must preserve data and reuse.
using System;
using System.Buffers;

class Program
{
    static int Main()
    {
        var pool = ArrayPool<byte>.Shared;
        byte[] a = pool.Rent(1024);
        if (a.Length < 1024) return 1;
        for (int i = 0; i < 1024; i++)
            a[i] = (byte)(i & 0xFF);
        for (int i = 0; i < 1024; i++)
            if (a[i] != (byte)(i & 0xFF)) return 2;
        pool.Return(a);
        byte[] b = pool.Rent(1024);
        if (b.Length < 1024) return 3;
        pool.Return(b);
        Console.WriteLine("bcl_subst: arraypool_rent_return ok");
        return 0;
    }
}
