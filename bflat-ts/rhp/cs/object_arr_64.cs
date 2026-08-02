// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        object[] arr = new object[64];
        for (int i = 0; i < 64; i++) arr[i] = new object();
        if (arr[63] == null) return 1;
        Console.WriteLine("rhp: object_arr_64 ok");
        return 0;
    }
}
