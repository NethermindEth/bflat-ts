// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        string[] arr = new string[64];
        for (int i = 0; i < 64; i++) arr[i] = i.ToString();
        if (arr[63] != "63") return 1;
        Console.WriteLine("rhp: string_arr_64 ok");
        return 0;
    }
}
