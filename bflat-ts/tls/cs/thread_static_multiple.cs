// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Tests multiple [ThreadStatic] slots across different types to
// stress __wrap_GetUninlinedThreadStaticBaseForType slot table.
using System;

class SlotA { [ThreadStatic] public static int X; }
class SlotB { [ThreadStatic] public static int Y; }
class SlotC { [ThreadStatic] public static string Label; }

class Program
{
    static int Main()
    {
        SlotA.X = 10;
        SlotB.Y = 20;
        SlotC.Label = "abc";

        if (SlotA.X != 10) return 1;
        if (SlotB.Y != 20) return 1;
        if (SlotC.Label != "abc") return 1;

        // Slots must be independent of each other
        SlotA.X = 99;
        if (SlotB.Y != 20) return 1;
        if (SlotC.Label != "abc") return 1;

        Console.WriteLine(
            $"tls: multi_static ok A={SlotA.X} B={SlotB.Y} C={SlotC.Label}");
        return 0;
    }
}