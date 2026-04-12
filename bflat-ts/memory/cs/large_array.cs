// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Exercises: large array allocation that may hit mmap→malloc path
// Bug targeted: mmap→malloc without page alignment causing GC metadata corruption
using System;

class Program
{
    static int Main()
    {
        const int Size = 65536;
        byte[] buf = new byte[Size];

        for (int i = 0; i < Size; i++)
            buf[i] = (byte)(i * 37 & 0xFF);

        for (int i = 0; i < Size; i++)
        {
            byte expected = (byte)(i * 37 & 0xFF);
            if (buf[i] != expected)
            {
                Console.WriteLine(
                    $"large_array: FAIL at [{i}]: got {buf[i]} expected {expected}");
                return 1;
            }
        }

        Console.WriteLine($"large_array: ok size={Size}");
        return 0;
    }
}