// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// The first (more specific) matching catch clause must win.
using System;

class Program
{
    static int Main()
    {
        try
        {
            throw new ArgumentNullException("p");
        }
        catch (ArgumentNullException)
        {
            Console.WriteLine("eh: eh_two_catch_first_match ok");
            return 0;
        }
        catch (ArgumentException)
        {
            return 2;
        }
    }
}
