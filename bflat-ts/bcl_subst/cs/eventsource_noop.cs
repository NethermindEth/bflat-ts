// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// EventPipe provider registration is wrapped to a no-op on zisk; creating
// an EventSource and writing events must be inert but safe.
using System;
using System.Diagnostics.Tracing;

[EventSource(Name = "BflatTs-Test")]
sealed class TestSource : EventSource
{
    public static readonly TestSource Log = new TestSource();

    [Event(1)]
    public void Ping(int value) { WriteEvent(1, value); }
}

class Program
{
    static int Main()
    {
        if (TestSource.Log.IsEnabled()) return 1;
        TestSource.Log.Ping(42);
        TestSource.Log.Dispose();
        Console.WriteLine("bcl_subst: eventsource_noop ok");
        return 0;
    }
}
