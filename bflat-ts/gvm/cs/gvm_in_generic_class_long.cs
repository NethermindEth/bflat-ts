// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Box<TVal> {
    public virtual T Echo<T>(T x) => x;
}
class D<TVal> : Box<TVal> {
    public override T Echo<T>(T x) => x;
}


class Program
{
    static int Main()
    {
        Box<int> b = new D<int>();
        if (!b.Echo<long>(1L).Equals(1L)) return 1;
        Console.WriteLine("gvm: gvm_in_generic_class_long ok");
        return 0;
    }
}
