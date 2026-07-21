// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Exercises: List<int> internal realloc on capacity doublings
// Bug targeted: realloc leaks old allocation (heap bloat / eventual exhaustion)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        const int Count = 5000;
        var list = new List<int>();

        for (int i = 0; i < Count; i++)
            list.Add(i * 7);

        if (list.Count != Count)
        {
            Console.WriteLine($"list_growth: FAIL count={list.Count} expected={Count}");
            return 1;
        }

        for (int i = 0; i < Count; i++)
        {
            if (list[i] != i * 7)
            {
                Console.WriteLine($"list_growth: FAIL at i={i} got={list[i]} expected={i * 7}");
                return 1;
            }
        }

        Console.WriteLine($"list_growth: ok count={list.Count} last={list[Count - 1]}");
        return 0;
    }
}