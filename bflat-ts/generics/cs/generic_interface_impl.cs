// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

interface IBox<T> { T Get(); }
class IntBox : IBox<int> { public int Get() => 7; }
class Program
{
    static int Main()
    {
        IBox<int> b = new IntBox();
        if (b.Get() != 7) return 1;
        Console.WriteLine("generics: generic_interface_impl ok");
        return 0;
    }
}
