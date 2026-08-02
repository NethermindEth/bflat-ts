// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// ProcessorIdCache.ProcessorNumberSpeedCheck is stubbed to false (cached,
// non-striped path). GetCurrentProcessorId must be stable on one hart.
using System;
using System.Threading;

class Program
{
    static int Main()
    {
        int id = Thread.GetCurrentProcessorId();
        if (id < 0) return 1;
        for (int i = 0; i < 100; i++)
            if (Thread.GetCurrentProcessorId() != id) return 2;
        Console.WriteLine("bcl_subst: thread_processor_id ok");
        return 0;
    }
}
