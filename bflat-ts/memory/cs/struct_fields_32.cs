// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Text;
using System.Collections.Generic;

struct S
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
    public int F16;
    public int F17;
    public int F18;
    public int F19;
    public int F20;
    public int F21;
    public int F22;
    public int F23;
    public int F24;
    public int F25;
    public int F26;
    public int F27;
    public int F28;
    public int F29;
    public int F30;
    public int F31;
}
class Program
{
    static int Main()
    {
        S s = new S();
        s.F0 = 5;
        s.F31 = 7;
        if (s.F0 + s.F31 != 12) return 1;
        Console.WriteLine("memory: struct_fields_32 ok");
        return 0;
    }
}
