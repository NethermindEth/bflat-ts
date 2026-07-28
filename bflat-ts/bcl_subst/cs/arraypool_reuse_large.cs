// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Larger pooled buffers across repeated rent/return cycles under the
// bump allocator: no trimming, no corruption.
using System;
using System.Buffers;

class Program
{
    static int Main()
    {
        var pool = ArrayPool<int>.Shared;
        for (int round = 0; round < 10; round++)
        {
            int[] buf = pool.Rent(1 << 14);
            if (buf.Length < (1 << 14)) return 1;
            buf[0] = round;
            buf[(1 << 14) - 1] = -round;
            if (buf[0] != round) return 2;
            if (buf[(1 << 14) - 1] != -round) return 3;
            pool.Return(buf);
        }
        Console.WriteLine("bcl_subst: arraypool_reuse_large ok");
        return 0;
    }
}
