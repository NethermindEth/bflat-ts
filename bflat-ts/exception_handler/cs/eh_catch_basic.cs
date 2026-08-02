// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Basic throw/catch of a specific exception type with message intact.
using System;

class Program
{
    static int Main()
    {
        try
        {
            throw new InvalidOperationException("boom");
        }
        catch (InvalidOperationException e)
        {
            if (e.Message != "boom") return 2;
            Console.WriteLine("eh: eh_catch_basic ok");
            return 0;
        }
    }
}
