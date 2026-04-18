// Comparer<T>.Default / TypeLoader crash: reference-type key in a generic class.
// BROKEN crash: Comparer_1<__Canon>__get_Default → SortedList_2<__Canon,int>___ctor
// Build BROKEN: bflat build comparer_default.cs --os linux --libc zisk_sim
// Build FIXED:  bflat build comparer_default.cs --os linux --libc zisk_sim -d:FIXED

using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
#if FIXED
        var store = new Store<Key>(KeyComparer.Instance);
#else
        var store = new Store<Key>();
#endif
        store.Add(new Key(3), 3);
        store.Add(new Key(1), 1);
        store.Add(new Key(2), 2);

        int prev = int.MinValue, count = 0;
        foreach (var kv in store.Items)
        {
            if (kv.Key.Value <= prev) Environment.FailFast("out of order");
            prev = kv.Key.Value;
            count++;
        }
        if (count != 3) Environment.FailFast("wrong count");
        return 0;
    }
}

#if FIXED
class Store<T>
{
    public readonly SortedList<T, int> Items;
    public Store(IComparer<T> cmp) => Items = new(cmp);
    public void Add(T key, int val) => Items.Add(key, val);
}
#else
class Store<T> where T : IComparable<T>
{
    public readonly SortedList<T, int> Items = new();   // ← crash: Comparer<__Canon>.Default → TypeLoader
    public void Add(T key, int val) => Items.Add(key, val);
}
#endif

class Key : IComparable<Key>
{
    public readonly int Value;
    public Key(int value) => Value = value;
    public int CompareTo(Key? other) => Value.CompareTo(other?.Value ?? 0);
}

sealed class KeyComparer : IComparer<Key>
{
    public static readonly KeyComparer Instance = new();
    public int Compare(Key? x, Key? y) => (x?.Value ?? 0).CompareTo(y?.Value ?? 0);
}