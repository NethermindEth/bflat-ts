// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Text;
using System.Collections.Generic;

struct S { public int X, Y; }
class Program
{
    static int Main()
    {
        S[] arr = new S[1024];
        for (int i = 0; i < 1024; i++) { arr[i].X = i; arr[i].Y = -i; }
        if (arr[1023].X != 1023) return 1;
        Console.WriteLine("memory: array_of_struct_1024 ok");
        return 0;
    }
}
