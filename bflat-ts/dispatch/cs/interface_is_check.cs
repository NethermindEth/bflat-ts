// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

interface I {}
class C : I {}


class Program
{
    static int Main()
    {
        object o = new C();
        if (!(o is I)) return 1;
        Console.WriteLine("dispatch: interface_is_check ok");
        return 0;
    }
}
