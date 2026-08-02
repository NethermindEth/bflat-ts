// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        char[] arr = new char[1];
        for (int i = 0; i < arr.Length; i++) arr[i] = (char)('a' + i % 26);
        if (arr.Length != 1) return 1;
        Console.WriteLine("rhp: arr_char_1 ok");
        return 0;
    }
}
