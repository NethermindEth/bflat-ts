// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        string[] arr = new string[8];
        for (int i = 0; i < 8; i++) arr[i] = i.ToString();
        if (arr[7] != "7") return 1;
        Console.WriteLine("rhp: string_arr_8 ok");
        return 0;
    }
}
