// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Exercises: List<int> Add, Count, Sort, indexer, Remove
// Bug targeted: generic realloc + element layout in managed heap

using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var list = new List<int>();
        list.Add(5);
        list.Add(3);
        list.Add(1);
        list.Add(4);
        list.Add(2);

        if (list.Count != 5) return 1;

        list.Sort();

        for (int i = 0; i < 5; i++)
        {
            if (list[i] != i + 1) return 1;
        }

        list.Remove(3);
        if (list.Count != 4) return 1;

        // After removing 3: { 1, 2, 4, 5 }
        if (list[0] != 1) return 1;
        if (list[1] != 2) return 1;
        if (list[2] != 4) return 1;
        if (list[3] != 5) return 1;

        for (int i = 0; i < 100; i++)
            list.Add(i);

        if (list.Count != 104) return 1;
        if (list[103] != 99) return 1;

        Console.WriteLine("generics/list_int: ok");
        return 0;
    }
}