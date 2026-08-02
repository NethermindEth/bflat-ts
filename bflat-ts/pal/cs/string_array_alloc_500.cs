// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        string[] arr = new string[500];
        for (int i = 0; i < 500; i++) arr[i] = i.ToString();
        if (arr[499] != "499") return 1;
        Console.WriteLine("pal: string_array_alloc_500 ok");
        return 0;
    }
}
