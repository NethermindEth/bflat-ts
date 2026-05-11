// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        object[] arr = new object[2];
        for (int i = 0; i < 2; i++) arr[i] = new object();
        if (arr[1] == null) return 1;
        Console.WriteLine("rhp: object_arr_2 ok");
        return 0;
    }
}
