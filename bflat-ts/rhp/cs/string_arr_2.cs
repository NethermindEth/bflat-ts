// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        string[] arr = new string[2];
        for (int i = 0; i < 2; i++) arr[i] = i.ToString();
        if (arr[1] != "1") return 1;
        Console.WriteLine("rhp: string_arr_2 ok");
        return 0;
    }
}
