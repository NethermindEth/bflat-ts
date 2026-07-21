// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Tests __wrap_RhGetThreadStaticStorage and
// __wrap_S_P_CoreLib_Internal_Runtime_ThreadStatics__GetUninlinedThreadStaticBaseForType
// via [ThreadStatic] fields - verifies that thread-local storage
// initialisation (tls module: __init_tls, __copy_tls, __tls_get_addr)
// and the rhp thread-statics slot table work correctly in single-threaded
// execution.
using System;

class Counter
{
    [ThreadStatic]
    public static int Value;
}

class Tag
{
    [ThreadStatic]
    public static string Name;
}

class Program
{
    [ThreadStatic]
    private static int s_local;

    static int Main()
    {
        // Write and read back a [ThreadStatic] int
        Counter.Value = 42;
        if (Counter.Value != 42)
        {
            Console.WriteLine($"tls: counter expected 42, got {Counter.Value}");
            return 1;
        }

        Counter.Value += 8;
        if (Counter.Value != 50)
        {
            Console.WriteLine($"tls: counter expected 50, got {Counter.Value}");
            return 1;
        }

        // Write and read back a [ThreadStatic] string
        Tag.Name = "hello";
        if (Tag.Name != "hello")
        {
            Console.WriteLine($"tls: tag expected 'hello', got '{Tag.Name}'");
            return 1;
        }

        // [ThreadStatic] field declared in the entry-point class itself
        s_local = 7;
        if (s_local != 7)
        {
            Console.WriteLine($"tls: s_local expected 7, got {s_local}");
            return 1;
        }

        Console.WriteLine(
            $"tls: thread_static ok counter={Counter.Value} tag={Tag.Name} local={s_local}");
        return 0;
    }
}