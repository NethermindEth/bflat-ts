// Two-level TypeLoader crash with nested generic types.
// BROKEN: default sorted-collection ctors → TypeLoader → crash
// FIXED:  explicit IComparer<T> instances

using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        string typeName = TypeHelper.GetTypeName<EntityState>();
        if (typeName == null) Environment.FailFast("null type name");

#if FIXED
        var map = new StateMapFixed();
        var entity = CreateEntityFixed<RefKey>(new RefKey(0x01));
#else
        var map = new StateMap();
        var entity = CreateEntity<RefKey>(new RefKey(0x01));
#endif
        map.AddEntity(entity);

        if (map.EntityCount != 1)         Environment.FailFast("wrong entity count");
        if (entity.ValueChangeCount != 1) Environment.FailFast("wrong value-change count");

        return 0;
    }

    static EntityState CreateEntity<T>(T key) where T : class
    {
        var es = new EntityState();                 // ← crash (BROKEN)
        es.RecordValueChange(1, new BigUintKey(100));
        return es;
    }

    static EntityStateFixed CreateEntityFixed<T>(T key) where T : class
    {
        var es = new EntityStateFixed();
        es.RecordValueChange(1, new BigUintKey(100));
        return es;
    }
}

static class TypeHelper
{
    public static string GetTypeName<T>() where T : class => typeof(T).Name;
}

readonly struct BigUintKey : IComparable<BigUintKey>
{
    public readonly ulong Lo;
    public BigUintKey(ulong lo) => Lo = lo;
    public int CompareTo(BigUintKey other) => Lo.CompareTo(other.Lo);
}

class RefKey : IComparable<RefKey>
{
    public readonly ulong Value;
    public RefKey(ulong value) => Value = value;
    public int CompareTo(RefKey? other) => Value.CompareTo(other?.Value ?? 0);
}

readonly record struct StorageEntry(BigUintKey Key) : IComparable<StorageEntry>
{
    public int CompareTo(StorageEntry other) => Key.CompareTo(other.Key);
}

readonly record struct ValueChange(int Index, BigUintKey PostValue);
readonly record struct SlotEntry(int Index, BigUintKey NewValue);
readonly record struct SlotUpdate(BigUintKey Slot, IReadOnlyList<SlotEntry> Entries);

// ── BROKEN ────────────────────────────────────────────────────────────────────

class EntityState
{
    private readonly SortedList<BigUintKey, SlotUpdate> _slotChanges = [];  // ← crash
    private readonly SortedSet<StorageEntry>            _storageReads = []; // ← crash
    private readonly SortedList<int, ValueChange>       _valueChanges = []; // ← crash

    public int ValueChangeCount => _valueChanges.Count;

    public void RecordValueChange(int index, BigUintKey val)
        => _valueChanges[index] = new ValueChange(index, val);

    public void RecordStorageRead(BigUintKey slot)
        => _storageReads.Add(new StorageEntry(slot));

    public void RecordSlotChange(BigUintKey slot, int index, BigUintKey newVal)
    {
        if (!_slotChanges.ContainsKey(slot))
            _slotChanges[slot] = new SlotUpdate(slot, []);
    }
}

class StateMap
{
    private readonly SortedDictionary<RefKey, EntityState> _entities = []; // ← crash

    public int EntityCount => _entities.Count;

    public void AddEntity(EntityState es)
        => _entities[new RefKey((ulong)_entities.Count + 1)] = es;
}

// ── FIXED ─────────────────────────────────────────────────────────────────────

class EntityStateFixed
{
    private readonly SortedList<BigUintKey, SlotUpdate> _slotChanges = new(BigUintKeyComparer.Instance);
    private readonly SortedSet<StorageEntry>            _storageReads = new(StorageEntryComparer.Instance);
    private readonly SortedList<int, ValueChange>       _valueChanges = new(IntComparer.Instance);

    public int ValueChangeCount => _valueChanges.Count;

    public void RecordValueChange(int index, BigUintKey val)
        => _valueChanges[index] = new ValueChange(index, val);

    public void RecordStorageRead(BigUintKey slot)
        => _storageReads.Add(new StorageEntry(slot));

    public void RecordSlotChange(BigUintKey slot, int index, BigUintKey newVal)
    {
        if (!_slotChanges.ContainsKey(slot))
            _slotChanges[slot] = new SlotUpdate(slot, []);
    }
}

class StateMapFixed
{
    private readonly SortedDictionary<RefKey, EntityStateFixed> _entities = new(RefKeyComparer.Instance);

    public int EntityCount => _entities.Count;

    public void AddEntity(EntityStateFixed es)
        => _entities[new RefKey((ulong)_entities.Count + 1)] = es;
}

// ── Comparers ─────────────────────────────────────────────────────────────────

sealed class IntComparer : IComparer<int>
{
    public static readonly IntComparer Instance = new();
    public int Compare(int x, int y) => x.CompareTo(y);
}

sealed class BigUintKeyComparer : IComparer<BigUintKey>
{
    public static readonly BigUintKeyComparer Instance = new();
    public int Compare(BigUintKey x, BigUintKey y) => x.CompareTo(y);
}

sealed class StorageEntryComparer : IComparer<StorageEntry>
{
    public static readonly StorageEntryComparer Instance = new();
    public int Compare(StorageEntry x, StorageEntry y) => x.CompareTo(y);
}

sealed class RefKeyComparer : IComparer<RefKey>
{
    public static readonly RefKeyComparer Instance = new();
    public int Compare(RefKey? x, RefKey? y)
    {
        if (x is null) return y is null ? 0 : -1;
        if (y is null) return 1;
        return x.Value.CompareTo(y.Value);
    }
}