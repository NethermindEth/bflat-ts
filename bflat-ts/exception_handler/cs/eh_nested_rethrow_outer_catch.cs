// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Inner catch rethrows; the outer catch must receive the same exception.
using System;

class Program
{
    static int Main()
    {
        try
        {
            try
            {
                throw new ArgumentException("pass-through");
            }
            catch (ArgumentException)
            {
                throw;
            }
        }
        catch (ArgumentException e)
        {
            if (e.Message != "pass-through") return 2;
            Console.WriteLine("eh: eh_nested_rethrow_outer_catch ok");
            return 0;
        }
    }
}
