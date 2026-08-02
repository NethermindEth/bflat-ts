// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Reference-array copy (string[] -> object[]) uses the GC-ref path, not the
// removed primitive-widening path.
using System;

class Program
{
    static int Main()
    {
        string[] src = { "a", "b", "c", "d" };
        object[] dst = new object[4];
        Array.Copy(src, dst, 4);
        for (int i = 0; i < 4; i++)
            if (!ReferenceEquals(src[i], dst[i])) return 1;
        Console.WriteLine("bcl_subst: array_copy_string_to_object ok");
        return 0;
    }
}
