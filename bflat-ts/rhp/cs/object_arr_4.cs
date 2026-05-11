// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        object[] arr = new object[4];
        for (int i = 0; i < 4; i++) arr[i] = new object();
        if (arr[3] == null) return 1;
        Console.WriteLine("rhp: object_arr_4 ok");
        return 0;
    }
}
