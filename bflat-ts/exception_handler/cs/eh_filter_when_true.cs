// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// An exception filter evaluating to true must select its catch clause.
using System;

class Program
{
    static int Main()
    {
        try
        {
            throw new InvalidOperationException("yes");
        }
        catch (InvalidOperationException e) when (e.Message == "yes")
        {
            Console.WriteLine("eh: eh_filter_when_true ok");
            return 0;
        }
        catch (InvalidOperationException)
        {
            return 2;
        }
    }
}
