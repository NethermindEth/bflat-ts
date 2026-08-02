// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        byte[] arr = new byte[64];
        for (int i = 0; i < arr.Length; i++) arr[i] = (byte)i;
        if (arr.Length != 64) return 1;
        Console.WriteLine("rhp: arr_byte_64 ok");
        return 0;
    }
}
