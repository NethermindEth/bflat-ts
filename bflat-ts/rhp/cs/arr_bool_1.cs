// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        bool[] arr = new bool[1];
        for (int i = 0; i < arr.Length; i++) arr[i] = (i % 2 == 0);
        if (arr.Length != 1) return 1;
        Console.WriteLine("rhp: arr_bool_1 ok");
        return 0;
    }
}
