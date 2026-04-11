// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Tests that programs containing float operations compile and run
// under nofp stubs (all FP helpers are no-ops returning void).
// Results of FP arithmetic are undefined/zero; we only verify
// that the program starts and exits without crashing.

using System;

class Program
{
    // Non-const statics force the compiler to emit a real __addsf3 call
    // instead of folding the arithmetic at compile time.
    static float s_x = 1.5f;
    static float s_y = 2.5f;

    static int Main()
    {
        // Triggers __addsf3 stub (no-op) – result is garbage, not checked
        float z = s_x + s_y;

        // Triggers __mulsf3 stub – result is garbage, not checked
        float w = s_x * s_y;

        // Do NOT use z/w in control-flow: nofp stubs make them 0 / undefined
        Console.WriteLine("nofp: float ops ok");
        return 0;
    }
}