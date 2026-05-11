// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        object[] arr = new object[8];
        for (int i = 0; i < 8; i++) arr[i] = new object();
        if (arr[7] == null) return 1;
        Console.WriteLine("rhp: object_arr_8 ok");
        return 0;
    }
}
