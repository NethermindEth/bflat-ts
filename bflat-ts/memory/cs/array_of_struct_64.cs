// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Text;
using System.Collections.Generic;

struct S { public int X, Y; }
class Program
{
    static int Main()
    {
        S[] arr = new S[64];
        for (int i = 0; i < 64; i++) { arr[i].X = i; arr[i].Y = -i; }
        if (arr[63].X != 63) return 1;
        Console.WriteLine("memory: array_of_struct_64 ok");
        return 0;
    }
}
