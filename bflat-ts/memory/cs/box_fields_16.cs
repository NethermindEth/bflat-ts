// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Text;
using System.Collections.Generic;

class Box
{
    public int F0;
    public int F1;
    public int F2;
    public int F3;
    public int F4;
    public int F5;
    public int F6;
    public int F7;
    public int F8;
    public int F9;
    public int F10;
    public int F11;
    public int F12;
    public int F13;
    public int F14;
    public int F15;
}
class Program
{
    static int Main()
    {
        var b = new Box();
        b.F0 = 1;
        b.F15 = 2;
        if (b.F0 + b.F15 != 3) return 1;
        Console.WriteLine("memory: box_fields_16 ok");
        return 0;
    }
}
