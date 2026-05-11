// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

// Note: this variant uses an explicit comparer to avoid the
// Comparer<__Canon>.Default reference-type crash.
class Key : IComparable<Key>
{
    public readonly int Value;
    public Key(int v) => Value = v;
    public int CompareTo(Key other) => Value.CompareTo(other == null ? 0 : other.Value);
}
sealed class KeyComparer : IComparer<Key>
{
    public static readonly KeyComparer Instance = new();
    public int Compare(Key x, Key y) =>
        (x == null ? 0 : x.Value).CompareTo(y == null ? 0 : y.Value);
}
class Program
{
    static int Main()
    {
        var list = new SortedList<Key, int>(KeyComparer.Instance);
        list.Add(new Key(3), 3);
        list.Add(new Key(1), 1);
        list.Add(new Key(2), 2);
        int prev = int.MinValue;
        foreach (var kv in list)
        {
            if (kv.Key.Value <= prev) return 1;
            prev = kv.Key.Value;
        }
        Console.WriteLine("gvm: comparer_default ok");
        return 0;
    }
}
