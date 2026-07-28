// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Copying arrays of ref-carrying structs lowers to
// RhBulkMoveWithWriteBarrier, which zisk wraps to a plain memmove (no write
// barrier under uGC). References must survive copies and List shifts.
using System;
using System.Collections.Generic;

struct Entry
{
    public string Name;
    public int Value;
    public Entry(string n, int v) { Name = n; Value = v; }
}

class Program
{
    static int Main()
    {
        var src = new Entry[8];
        for (int i = 0; i < 8; i++)
            src[i] = new Entry("n" + i, i);
        var dst = new Entry[8];
        Array.Copy(src, dst, 8);
        for (int i = 0; i < 8; i++)
        {
            if (!ReferenceEquals(src[i].Name, dst[i].Name)) return 1;
            if (dst[i].Value != i) return 2;
        }
        var list = new List<Entry>(src);
        list.Insert(0, new Entry("head", -1));
        list.RemoveAt(4);
        if (list.Count != 8) return 3;
        if (list[0].Name != "head") return 4;
        if (list[1].Name != "n0") return 5;
        if (list[4].Name != "n4") return 6;
        Console.WriteLine("bcl_subst: bulkmove_struct_refs ok");
        return 0;
    }
}
