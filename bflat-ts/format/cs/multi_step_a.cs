// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        string s1 = 1.ToString();
        string s2 = 2.ToString();
        string s = s1 + "+" + s2 + "=" + (1 + 2).ToString();
        if (s != "1+2=3") return 1;
        Console.WriteLine("format: multi_step_a ok");
        return 0;
    }
}
