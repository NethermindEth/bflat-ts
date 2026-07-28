// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// The BCL ArgumentNullException.ThrowIfNull throw helper must produce a
// catchable exception with the parameter name.
using System;

class Program
{
    static object Missing; // stays null

    static int Main()
    {
        try
        {
            ArgumentNullException.ThrowIfNull(Missing, "missing");
            return 1;
        }
        catch (ArgumentNullException e)
        {
            if (e.ParamName != "missing") return 2;
            Console.WriteLine("eh: eh_argnull_throwifnull ok");
            return 0;
        }
    }
}
