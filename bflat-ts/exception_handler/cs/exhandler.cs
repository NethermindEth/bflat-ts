// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Verifies that a managed `throw` is routed into a C# handler exported as
// [UnmanagedCallersOnly(EntryPoint = "ZkvmThrow")], that the handler receives
// the live Exception object, and that it can terminate the guest cleanly.
//
//   throw -> RhpThrowEx (object in a0) -> __wrap_RhpThrowEx -> ZkvmThrow
//
// Notes:
//  * Exit via the native exit() (Zisk a7=93 ecall), NOT Environment.Exit: the
//    latter runs managed shutdown which, mid-throw, recurses through the
//    handler. Exit code 0 so the TE run step passes.
//  * Print via Console.WriteLine for portability across libc=zisk / zisk_sim.
//    Under libc=zisk the console is a no-op (the zkVM has no stdout), so the
//    test does not assert stdout; it asserts the guest reaches the handler and
//    exits cleanly (without the runtime wiring this run never completes).

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

class Program
{
    [DllImport("*", EntryPoint = "exit")]
    static extern void NativeExit(int code);

    [UnmanagedCallersOnly(EntryPoint = "ZkvmThrow")]
    static void ZkvmThrow(IntPtr exceptionObj)
    {
        Exception ex = Unsafe.As<IntPtr, Exception>(ref exceptionObj);
        Console.WriteLine("[ZkvmThrow] " + ex.GetType().ToString() + ": " + ex.Message);
        NativeExit(0);
    }

    static int Main()
    {
        Console.WriteLine("before throw");
        throw new InvalidOperationException("boom from managed code");
    }
}
