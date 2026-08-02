// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// An exception thrown in a finally block replaces the in-flight exception.
using System;

class Program
{
    static int Main()
    {
        try
        {
            try
            {
                throw new ArgumentException("lost");
            }
            finally
            {
                throw new InvalidOperationException("winner");
            }
        }
        catch (InvalidOperationException e)
        {
            if (e.Message != "winner") return 2;
            Console.WriteLine("eh: eh_throw_in_finally ok");
            return 0;
        }
        catch (ArgumentException)
        {
            return 3;
        }
    }
}
