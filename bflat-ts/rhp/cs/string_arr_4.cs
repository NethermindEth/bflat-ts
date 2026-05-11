// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        string[] arr = new string[4];
        for (int i = 0; i < 4; i++) arr[i] = i.ToString();
        if (arr[3] != "3") return 1;
        Console.WriteLine("rhp: string_arr_4 ok");
        return 0;
    }
}
