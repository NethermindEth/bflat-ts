// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Tests that 5 independent [ThreadStatic] slots of different types do not
// corrupt each other when written and re-written.
// Bug targeted: TSS slot table index collision between adjacent type registrations.
using System;

class SlotInt    { [ThreadStatic] public static int    Val; }
class SlotLong   { [ThreadStatic] public static long   Val; }
class SlotString { [ThreadStatic] public static string Val; }
class SlotBool   { [ThreadStatic] public static bool   Val; }
class SlotByte   { [ThreadStatic] public static byte   Val; }

class Program
{
    static int Main()
    {
        // Step 1: initialise all slots
        SlotInt.Val    = 42;
        SlotLong.Val   = 1234567890123L;
        SlotString.Val = "hello";
        SlotBool.Val   = true;
        SlotByte.Val   = 255;

        // Step 2: verify all slots hold their initial values
        if (SlotInt.Val != 42)
        {
            Console.WriteLine($"tss_independent: FAIL SlotInt.Val={SlotInt.Val} expected=42");
            return 1;
        }
        if (SlotLong.Val != 1234567890123L)
        {
            Console.WriteLine($"tss_independent: FAIL SlotLong.Val={SlotLong.Val} expected=1234567890123");
            return 1;
        }
        if (SlotString.Val != "hello")
        {
            Console.WriteLine($"tss_independent: FAIL SlotString.Val={SlotString.Val} expected=hello");
            return 1;
        }
        if (!SlotBool.Val)
        {
            Console.WriteLine($"tss_independent: FAIL SlotBool.Val={SlotBool.Val} expected=true");
            return 1;
        }
        if (SlotByte.Val != 255)
        {
            Console.WriteLine($"tss_independent: FAIL SlotByte.Val={SlotByte.Val} expected=255");
            return 1;
        }

        // Step 3: overwrite only SlotInt
        SlotInt.Val = 99;

        // Step 4: SlotLong / SlotString / SlotBool / SlotByte must be unchanged
        if (SlotLong.Val != 1234567890123L)
        {
            Console.WriteLine(
                $"tss_independent: FAIL after SlotInt overwrite: SlotLong.Val={SlotLong.Val} expected=1234567890123");
            return 1;
        }
        if (SlotString.Val != "hello")
        {
            Console.WriteLine(
                $"tss_independent: FAIL after SlotInt overwrite: SlotString.Val={SlotString.Val} expected=hello");
            return 1;
        }
        if (!SlotBool.Val)
        {
            Console.WriteLine(
                $"tss_independent: FAIL after SlotInt overwrite: SlotBool.Val={SlotBool.Val} expected=true");
            return 1;
        }
        if (SlotByte.Val != 255)
        {
            Console.WriteLine(
                $"tss_independent: FAIL after SlotInt overwrite: SlotByte.Val={SlotByte.Val} expected=255");
            return 1;
        }

        Console.WriteLine(
            $"tss_independent: ok int={SlotInt.Val} long={SlotLong.Val} " +
            $"string={SlotString.Val} bool={SlotBool.Val} byte={SlotByte.Val}");
        return 0;
    }
}