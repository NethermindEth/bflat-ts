// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Tests PAL bump allocator (__wrap___libc_malloc_impl) by allocating
// many small objects and verifying their values are intact.

using System;
using System.Collections.Generic;

class Item
{
    public int Value { get; set; }
    public Item(int v) { Value = v; }
}

class Program
{
    static int Main()
    {
        const int count = 200;
        var list = new List<Item>(count);

        for (int i = 0; i < count; i++)
            list.Add(new Item(i));

        if (list.Count != count)
        {
            Console.WriteLine($"pal: alloc_many count mismatch: {list.Count} != {count}");
            return 1;
        }

        for (int i = 0; i < count; i++)
        {
            if (list[i].Value != i)
            {
                Console.WriteLine($"pal: alloc_many value mismatch at {i}: {list[i].Value}");
                return 1;
            }
        }

        Console.WriteLine($"pal: alloc_many ok ({count} objects)");
        return 0;
    }
}