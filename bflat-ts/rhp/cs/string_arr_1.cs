// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        string[] arr = new string[1];
        for (int i = 0; i < 1; i++) arr[i] = i.ToString();
        if (arr[0] != "0") return 1;
        Console.WriteLine("rhp: string_arr_1 ok");
        return 0;
    }
}
