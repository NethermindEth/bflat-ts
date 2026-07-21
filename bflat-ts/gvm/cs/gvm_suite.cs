using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        int passed = 0, total = 0;

        Run("1. SortedList",          Test1.Run, ref passed, ref total);
        Run("2. SortedDictionary",    Test2.Run, ref passed, ref total);
        Run("3. SortedSet",           Test3.Run, ref passed, ref total);
        Run("4. HashSet",             Test4.Run, ref passed, ref total);
        Run("5. Dictionary",          Test5.Run, ref passed, ref total);
        Run("6. Mixed sorted+hash",   Test6.Run, ref passed, ref total);
        Run("7. typeof(T)",           Test7.Run, ref passed, ref total);
        Run("8. KeyValuePair<T,int>", Test8.Run, ref passed, ref total);

        if (passed == total)
            return 0;
        Environment.FailFast($"FAILED: {passed}/{total} passed");
        return 1;
    }

    static void Run(string name, Func<bool> test, ref int passed, ref int total)
    {
        total++;
        if (test()) passed++;
        else Environment.FailFast($"FAILED: {name}");
    }
}

class Key : IComparable<Key>, IEquatable<Key>
{
    public readonly int Value;
    public Key(int v) => Value = v;
    public int  CompareTo(Key? other) => Value.CompareTo(other?.Value ?? 0);
    public bool Equals(Key? other)    => other is not null && Value == other.Value;
    public override int  GetHashCode()       => Value;
    public override bool Equals(object? obj) => obj is Key k && Equals(k);
}

sealed class KeyComparer : IComparer<Key>
{
    public static readonly KeyComparer Instance = new();
    public int Compare(Key? x, Key? y) => (x?.Value ?? 0).CompareTo(y?.Value ?? 0);
}

sealed class KeyEqualityComparer : IEqualityComparer<Key>
{
    public static readonly KeyEqualityComparer Instance = new();
    public bool Equals(Key? x, Key? y) => (x?.Value ?? 0) == (y?.Value ?? 0);
    public int  GetHashCode(Key? k)    => k?.Value ?? 0;
}

class SortedListStore<T> where T : class, IComparable<T>
{
#if FIXED
    public readonly SortedList<T, int> Items;
    public SortedListStore(IComparer<T> cmp) => Items = new(cmp);
#else
    public readonly SortedList<T, int> Items = new();   // ← crash
    public SortedListStore() { }
#endif
    public void Add(T key, int val) => Items.Add(key, val);
}

static class Test1
{
    public static bool Run()
    {
#if FIXED
        var store = new SortedListStore<Key>(KeyComparer.Instance);
#else
        var store = new SortedListStore<Key>();
#endif
        store.Add(new Key(3), 3); store.Add(new Key(1), 1); store.Add(new Key(2), 2);
        int prev = int.MinValue, count = 0;
        foreach (var kv in store.Items) { if (kv.Key.Value <= prev) return false; prev = kv.Key.Value; count++; }
        return count == 3;
    }
}

class SortedDictStore<T> where T : class, IComparable<T>
{
#if FIXED
    public readonly SortedDictionary<T, int> Items;
    public SortedDictStore(IComparer<T> cmp) => Items = new(cmp);
#else
    public readonly SortedDictionary<T, int> Items = new();  // ← crash
    public SortedDictStore() { }
#endif
    public void Add(T key, int val) => Items.Add(key, val);
}

static class Test2
{
    public static bool Run()
    {
#if FIXED
        var store = new SortedDictStore<Key>(KeyComparer.Instance);
#else
        var store = new SortedDictStore<Key>();
#endif
        store.Add(new Key(5), 5); store.Add(new Key(2), 2); store.Add(new Key(8), 8);
        int prev = int.MinValue, count = 0;
        foreach (var kv in store.Items) { if (kv.Key.Value <= prev) return false; prev = kv.Key.Value; count++; }
        return count == 3;
    }
}

class SortedSetStore<T> where T : class, IComparable<T>
{
#if FIXED
    public readonly SortedSet<T> Items;
    public SortedSetStore(IComparer<T> cmp) => Items = new(cmp);
#else
    public readonly SortedSet<T> Items = new();  // ← crash
    public SortedSetStore() { }
#endif
    public void Add(T item) => Items.Add(item);
}

static class Test3
{
    public static bool Run()
    {
#if FIXED
        var store = new SortedSetStore<Key>(KeyComparer.Instance);
#else
        var store = new SortedSetStore<Key>();
#endif
        store.Add(new Key(4)); store.Add(new Key(1)); store.Add(new Key(7));
        int prev = int.MinValue, count = 0;
        foreach (var k in store.Items) { if (k.Value <= prev) return false; prev = k.Value; count++; }
        return count == 3;
    }
}

class HashSetStore<T> where T : class, IEquatable<T>
{
#if FIXED
    public readonly HashSet<T> Items;
    public HashSetStore(IEqualityComparer<T> cmp) => Items = new(cmp);
#else
    public readonly HashSet<T> Items = new();  // ← crash
    public HashSetStore() { }
#endif
    public bool Add(T item)      => Items.Add(item);
    public bool Contains(T item) => Items.Contains(item);
}

static class Test4
{
    public static bool Run()
    {
#if FIXED
        var store = new HashSetStore<Key>(KeyEqualityComparer.Instance);
#else
        var store = new HashSetStore<Key>();
#endif
        store.Add(new Key(1)); store.Add(new Key(2)); store.Add(new Key(1));
        return store.Items.Count == 2
            && store.Contains(new Key(1))
            && store.Contains(new Key(2))
            && !store.Contains(new Key(3));
    }
}

class DictStore<T> where T : class, IEquatable<T>
{
#if FIXED
    public readonly Dictionary<T, int> Items;
    public DictStore(IEqualityComparer<T> cmp) => Items = new(cmp);
#else
    public readonly Dictionary<T, int> Items = new();  // ← crash
    public DictStore() { }
#endif
    public void Add(T key, int val)        => Items.Add(key, val);
    public bool TryGet(T key, out int val) => Items.TryGetValue(key, out val);
}

static class Test5
{
    public static bool Run()
    {
#if FIXED
        var store = new DictStore<Key>(KeyEqualityComparer.Instance);
#else
        var store = new DictStore<Key>();
#endif
        store.Add(new Key(10), 100); store.Add(new Key(20), 200);
        if (!store.TryGet(new Key(10), out int v10) || v10 != 100) return false;
        if (!store.TryGet(new Key(20), out int v20) || v20 != 200) return false;
        if ( store.TryGet(new Key(99), out _)) return false;
        return true;
    }
}

class MixedStore<T> where T : class, IComparable<T>, IEquatable<T>
{
#if FIXED
    public readonly SortedList<T, int> Sorted;
    public readonly HashSet<T>         Seen;
    public MixedStore(IComparer<T> cmp, IEqualityComparer<T> eqCmp)
    { Sorted = new(cmp); Seen = new(eqCmp); }
#else
    public readonly SortedList<T, int> Sorted = new();  // ← crash
    public readonly HashSet<T>         Seen   = new();  // ← crash
    public MixedStore() { }
#endif
    public void Add(T item, int rank) { Sorted.Add(item, rank); Seen.Add(item); }
}

static class Test6
{
    public static bool Run()
    {
#if FIXED
        var store = new MixedStore<Key>(KeyComparer.Instance, KeyEqualityComparer.Instance);
#else
        var store = new MixedStore<Key>();
#endif
        store.Add(new Key(3), 30); store.Add(new Key(1), 10); store.Add(new Key(2), 20);
        if (store.Sorted.Count != 3 || store.Seen.Count != 3) return false;
        if (!store.Seen.Contains(new Key(2))) return false;
        int prev = int.MinValue;
        foreach (var kv in store.Sorted) { if (kv.Key.Value <= prev) return false; prev = kv.Key.Value; }
        return true;
    }
}

static class Test7
{
    static string GetTypeName<T>() where T : class => typeof(T).Name;
    public static bool Run() => GetTypeName<Key>().Length == 3;
}

class KvpSortedStore<T> where T : class, IComparable<T>
{
#if FIXED
    public readonly SortedList<KeyValuePair<T, int>, int> Items;
    public KvpSortedStore(IComparer<KeyValuePair<T, int>> cmp) => Items = new(cmp);
#else
    public readonly SortedList<KeyValuePair<T, int>, int> Items = new();  // ← crash
    public KvpSortedStore() { }
#endif
    public void Add(T key, int secondary, int val)
        => Items[new KeyValuePair<T, int>(key, secondary)] = val;
}

sealed class KvpKeyIntComparer : IComparer<KeyValuePair<Key, int>>
{
    public static readonly KvpKeyIntComparer Instance = new();
    public int Compare(KeyValuePair<Key, int> x, KeyValuePair<Key, int> y)
    {
        int c = KeyComparer.Instance.Compare(x.Key, y.Key);
        return c != 0 ? c : x.Value.CompareTo(y.Value);
    }
}

static class Test8
{
    public static bool Run()
    {
#if FIXED
        var store = new KvpSortedStore<Key>(KvpKeyIntComparer.Instance);
        store.Add(new Key(3), 0, 30); store.Add(new Key(1), 0, 10); store.Add(new Key(2), 0, 20);
        if (store.Items.Count != 3) return false;
        int prev = int.MinValue;
        foreach (var kv in store.Items) { if (kv.Key.Key.Value <= prev) return false; prev = kv.Key.Key.Value; }
        return true;
#else
        var store = new KvpSortedStore<Key>();
        store.Add(new Key(3), 0, 30);
        store.Add(new Key(1), 0, 10);  // ← crash
        store.Add(new Key(2), 0, 20);
        return false;
#endif
    }
}