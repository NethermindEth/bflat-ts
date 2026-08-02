// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Materializing many distinct constructed generic types via MakeGenericType
// fills the runtime type unifier (ConcurrentUnifierW) past its initial
// capacity, driving the Container.Resize body that zisk rewrites to integer
// load-factor math (live*4 vs len*3).
using System;

class Wrap<T> { }

class Program
{
    static int Main()
    {
        Type t = typeof(Wrap<object>);
        for (int i = 0; i < 80; i++)
        {
            t = typeof(Wrap<>).MakeGenericType(t);
            if (t == null) return 1;
            if (!t.IsConstructedGenericType) return 2;
        }
        if (t.GetGenericTypeDefinition() != typeof(Wrap<>)) return 3;
        Console.WriteLine("bcl_subst: unifier_resize_makegeneric ok");
        return 0;
    }
}
