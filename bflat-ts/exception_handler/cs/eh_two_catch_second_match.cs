// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// When the first catch clause does not match, the second must be selected.
using System;

class Program
{
    static int Main()
    {
        try
        {
            throw new InvalidOperationException("second");
        }
        catch (ArgumentException)
        {
            return 2;
        }
        catch (InvalidOperationException e)
        {
            if (e.Message != "second") return 3;
            Console.WriteLine("eh: eh_two_catch_second_match ok");
            return 0;
        }
    }
}
