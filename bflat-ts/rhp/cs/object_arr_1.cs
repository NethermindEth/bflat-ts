// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        object[] arr = new object[1];
        for (int i = 0; i < 1; i++) arr[i] = new object();
        if (arr[0] == null) return 1;
        Console.WriteLine("rhp: object_arr_1 ok");
        return 0;
    }
}
