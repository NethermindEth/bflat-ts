// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        string[] arr = new string[256];
        for (int i = 0; i < 256; i++) arr[i] = i.ToString();
        if (arr[255] != "255") return 1;
        Console.WriteLine("rhp: string_arr_256 ok");
        return 0;
    }
}
