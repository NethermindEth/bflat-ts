// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Buffer.BlockCopy on primitive arrays: byte-level copy with offsets.
using System;

class Program
{
    static int Main()
    {
        int[] src = { 0x11223344, 0x55667788 };
        byte[] dst = new byte[8];
        Buffer.BlockCopy(src, 0, dst, 0, 8);
        int lo = dst[0] | (dst[1] << 8) | (dst[2] << 16) | (dst[3] << 24);
        if (lo != 0x11223344) return 1;
        byte[] tail = new byte[4];
        Buffer.BlockCopy(dst, 4, tail, 0, 4);
        int hi = tail[0] | (tail[1] << 8) | (tail[2] << 16) | (tail[3] << 24);
        if (hi != 0x55667788) return 2;
        Console.WriteLine("bcl_subst: buffer_blockcopy_bytes ok");
        return 0;
    }
}
