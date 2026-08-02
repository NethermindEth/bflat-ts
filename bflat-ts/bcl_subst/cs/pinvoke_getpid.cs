// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// A direct P/Invoke crosses the RhpPInvoke/RhpPInvokeReturn transitions
// (no-ops on zisk) into pal's wrapped getpid; the value must be stable.
using System;
using System.Runtime.InteropServices;

class Program
{
    [DllImport("*", EntryPoint = "getpid")]
    static extern int GetPid();

    static int Main()
    {
        int a = GetPid();
        int b = GetPid();
        if (a != b) return 1;
        if (a < 0) return 2;
        Console.WriteLine("bcl_subst: pinvoke_getpid ok");
        return 0;
    }
}
