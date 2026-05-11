// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        char[] arr = new char[16];
        for (int i = 0; i < arr.Length; i++) arr[i] = (char)('a' + i % 26);
        if (arr.Length != 16) return 1;
        Console.WriteLine("rhp: arr_char_16 ok");
        return 0;
    }
}
