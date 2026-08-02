// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// An exception filter evaluating to false must fall through to the next
// matching catch clause.
using System;

class Program
{
    static int Main()
    {
        try
        {
            throw new InvalidOperationException("no");
        }
        catch (InvalidOperationException e) when (e.Message == "yes")
        {
            GC.KeepAlive(e);
            return 2;
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine("eh: eh_filter_when_false ok");
            return 0;
        }
    }
}
