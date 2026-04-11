// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Minimal C# program used as the target for run_perf startup-steps
// measurement.  The test counts instruction steps from _start until
// __managed__Main is first entered, so the body of Main() is kept
// intentionally trivial to maximise signal-to-noise ratio for the
// NativeAOT runtime initialisation measurement.

using System;

class Program
{
    static int Main()
    {
        Console.WriteLine("started");
        return 0;
    }
}