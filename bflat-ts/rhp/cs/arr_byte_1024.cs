// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        byte[] arr = new byte[1024];
        for (int i = 0; i < arr.Length; i++) arr[i] = (byte)i;
        if (arr.Length != 1024) return 1;
        Console.WriteLine("rhp: arr_byte_1024 ok");
        return 0;
    }
}
