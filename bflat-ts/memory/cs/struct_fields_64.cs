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
    public int F32;
    public int F33;
    public int F34;
    public int F35;
    public int F36;
    public int F37;
    public int F38;
    public int F39;
    public int F40;
    public int F41;
    public int F42;
    public int F43;
    public int F44;
    public int F45;
    public int F46;
    public int F47;
    public int F48;
    public int F49;
    public int F50;
    public int F51;
    public int F52;
    public int F53;
    public int F54;
    public int F55;
    public int F56;
    public int F57;
    public int F58;
    public int F59;
    public int F60;
    public int F61;
    public int F62;
    public int F63;
}
class Program
{
    static int Main()
    {
        S s = new S();
        s.F0 = 5;
        s.F63 = 7;
        if (s.F0 + s.F63 != 12) return 1;
        Console.WriteLine("memory: struct_fields_64 ok");
        return 0;
    }
}
