// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// `throw;` must re-raise the original exception object.
using System;

class Program
{
    static void Inner()
    {
        try
        {
            throw new InvalidOperationException("original");
        }
        catch (InvalidOperationException)
        {
            throw;
        }
    }

    static int Main()
    {
        try
        {
            Inner();
            return 1;
        }
        catch (InvalidOperationException e)
        {
            if (e.Message != "original") return 2;
            Console.WriteLine("eh: eh_catch_rethrow ok");
            return 0;
        }
    }
}
