// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// GC.Collect deep in a call chain scans the stack within the synthetic
// bounds reported by the wrapped PalGetMaximumStackBounds.
using System;

class Program
{
    static object[] Keep = new object[512];

    static int Recurse(int depth)
    {
        Keep[depth] = new int[] { depth, depth + 1 };
        if (depth == 511)
        {
            GC.Collect();
            return depth;
        }
        int r = Recurse(depth + 1);
        int[] mine = (int[])Keep[depth];
        if (mine[0] != depth) return -1;
        return r;
    }

    static int Main()
    {
        if (Recurse(0) != 511) return 1;
        for (int i = 0; i < 512; i++)
            if (((int[])Keep[i])[1] != i + 1) return 2;
        Console.WriteLine("bcl_subst: gc_collect_deep_stack ok");
        return 0;
    }
}
