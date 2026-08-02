// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Synchronously-completing Task/async machinery must work without ever
// starting a thread-pool worker (GateThreadStart is stubbed on zisk).
using System;
using System.Threading.Tasks;

class Program
{
    static async Task<int> DoubleAsync(int v) => await Task.FromResult(v) * 2;

    static int Main()
    {
        Task<int> t = DoubleAsync(21);
        if (!t.IsCompleted) return 1;
        if (t.Result != 42) return 2;
        Task all = Task.WhenAll(Task.FromResult(1), Task.CompletedTask);
        if (!all.IsCompleted) return 3;
        ValueTask<int> vt = new ValueTask<int>(7);
        if (!vt.IsCompleted) return 4;
        if (vt.Result != 7) return 5;
        Console.WriteLine("bcl_subst: task_sync_completion ok");
        return 0;
    }
}
