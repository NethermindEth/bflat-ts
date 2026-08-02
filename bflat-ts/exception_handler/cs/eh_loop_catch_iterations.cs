// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Catching inside a loop must work repeatedly without corrupting state.
using System;

class Program
{
    static int Main()
    {
        int caught = 0;
        for (int i = 0; i < 10; i++)
        {
            try
            {
                if ((i & 1) == 0) throw new Exception("even");
            }
            catch (Exception)
            {
                caught++;
            }
        }
        if (caught != 5) return 1;
        Console.WriteLine("eh: eh_loop_catch_iterations ok");
        return 0;
    }
}
