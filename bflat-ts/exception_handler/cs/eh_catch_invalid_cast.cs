// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// A failing castclass must raise a catchable InvalidCastException.
using System;

class Program
{
    static object Boxed = 42;

    static int Main()
    {
        try
        {
            string s = (string)Boxed;
            GC.KeepAlive(s);
            return 1;
        }
        catch (InvalidCastException)
        {
            Console.WriteLine("eh: eh_catch_invalid_cast ok");
            return 0;
        }
    }
}
