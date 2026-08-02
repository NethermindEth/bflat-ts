// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// PortableThreadPool is sized by the stubbed ProcessorCount=1 and its
// HillClimbing tuner is disabled; the config surface must stay coherent.
using System;
using System.Threading;

class Program
{
    static int Main()
    {
        ThreadPool.GetMaxThreads(out int maxW, out int maxIo);
        if (maxW < 1) return 1;
        if (maxIo < 1) return 2;
        ThreadPool.GetMinThreads(out int minW, out int minIo);
        if (minW < 0 || minW > maxW) return 3;
        if (!ThreadPool.SetMinThreads(1, 1)) return 4;
        ThreadPool.GetAvailableThreads(out int availW, out _);
        if (availW < 0 || availW > maxW) return 5;
        Console.WriteLine("bcl_subst: threadpool_config_single_proc ok");
        return 0;
    }
}
