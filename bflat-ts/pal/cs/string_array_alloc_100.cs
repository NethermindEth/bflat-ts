// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        string[] arr = new string[100];
        for (int i = 0; i < 100; i++) arr[i] = i.ToString();
        if (arr[99] != "99") return 1;
        Console.WriteLine("pal: string_array_alloc_100 ok");
        return 0;
    }
}
