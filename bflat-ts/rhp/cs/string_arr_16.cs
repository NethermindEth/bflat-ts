// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        string[] arr = new string[16];
        for (int i = 0; i < 16; i++) arr[i] = i.ToString();
        if (arr[15] != "15") return 1;
        Console.WriteLine("rhp: string_arr_16 ok");
        return 0;
    }
}
