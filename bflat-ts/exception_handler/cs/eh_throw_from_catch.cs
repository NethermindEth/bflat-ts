// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// A new exception thrown from a catch block must propagate to the outer
// handler.
using System;

class Program
{
    static int Main()
    {
        try
        {
            try
            {
                throw new ArgumentException("first");
            }
            catch (ArgumentException)
            {
                throw new InvalidOperationException("from-catch");
            }
        }
        catch (InvalidOperationException e)
        {
            if (e.Message != "from-catch") return 2;
            Console.WriteLine("eh: eh_throw_from_catch ok");
            return 0;
        }
    }
}
