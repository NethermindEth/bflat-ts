// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Base { public virtual int Count<T>(T[] xs) { return xs.Length; } }
class D : Base { public override int Count<T>(T[] xs) { return xs.Length; } }


class Program
{
    static int Main()
    {
        Base b = new D();
        if (b.Count<int>(new int[] { 1, 2, 3 }) != 3) return 1;
        Console.WriteLine("gvm: gvm_array_count_int ok");
        return 0;
    }
}
