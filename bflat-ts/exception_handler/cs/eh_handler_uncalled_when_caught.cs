// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// A caught exception must NOT be routed into the ZkvmThrow unhandled-exception
// handler: catch handles it and execution continues. The handler exits with a
// failing code, so if the runtime short-circuits the throw into the handler
// (no real unwinding), the test fails.
using System;
using System.Runtime.InteropServices;

class Program
{
    [DllImport("*", EntryPoint = "exit")]
    static extern void NativeExit(int code);

    [UnmanagedCallersOnly(EntryPoint = "ZkvmThrow")]
    static void ZkvmThrow(IntPtr exceptionObj)
    {
        // Reached only if the caught throw was misrouted here.
        NativeExit(3);
    }

    static int Main()
    {
        try
        {
            throw new InvalidOperationException("must be caught");
        }
        catch (InvalidOperationException)
        {
        }
        Console.WriteLine("eh: eh_handler_uncalled_when_caught ok");
        return 0;
    }
}
