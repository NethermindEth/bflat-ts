// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        byte[] arr = new byte[16];
        for (int i = 0; i < arr.Length; i++) arr[i] = (byte)i;
        if (arr.Length != 16) return 1;
        Console.WriteLine("rhp: arr_byte_16 ok");
        return 0;
    }
}
