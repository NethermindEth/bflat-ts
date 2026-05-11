// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        string[] arr = new string[10];
        for (int i = 0; i < 10; i++) arr[i] = i.ToString();
        if (arr[9] != "9") return 1;
        Console.WriteLine("pal: string_array_alloc_10 ok");
        return 0;
    }
}
