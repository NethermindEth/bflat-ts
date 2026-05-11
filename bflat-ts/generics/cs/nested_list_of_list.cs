// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var ll = new List<List<int>>();
        ll.Add(new List<int> { 1, 2, 3 });
        ll.Add(new List<int> { 4, 5 });
        if (ll[0].Count != 3) return 1;
        if (ll[1][1] != 5) return 1;
        Console.WriteLine("generics: nested_list_of_list ok");
        return 0;
    }
}
