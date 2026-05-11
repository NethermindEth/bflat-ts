// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

using System.Threading.Tasks;
class Program
{
    static async Task<int> Main(string[] args)
    {
        await Task.CompletedTask;
        Console.WriteLine(args.Length);
        return 0;
    }
}
