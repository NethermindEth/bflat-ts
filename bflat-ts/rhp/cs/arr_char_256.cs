// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        char[] arr = new char[256];
        for (int i = 0; i < arr.Length; i++) arr[i] = (char)('a' + i % 26);
        if (arr.Length != 256) return 1;
        Console.WriteLine("rhp: arr_char_256 ok");
        return 0;
    }
}
