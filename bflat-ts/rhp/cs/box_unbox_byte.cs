// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        object o = (byte)(byte)7;
        byte v = (byte)o;
        if (!v.Equals((byte)7)) return 1;
        Console.WriteLine("rhp: box_unbox_byte ok");
        return 0;
    }
}
