// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Wrapping an exception must preserve the original in InnerException.
using System;

class Program
{
    static int Main()
    {
        try
        {
            try
            {
                throw new ArgumentException("cause");
            }
            catch (ArgumentException e)
            {
                throw new InvalidOperationException("wrapper", e);
            }
        }
        catch (InvalidOperationException e)
        {
            if (e.Message != "wrapper") return 2;
            if (e.InnerException == null) return 3;
            if (e.InnerException.Message != "cause") return 4;
            Console.WriteLine("eh: eh_catch_inner_exception ok");
            return 0;
        }
    }
}
