// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Dereferencing a null reference must raise a catchable
// NullReferenceException.
using System;

class Program
{
    static string Nothing; // stays null

    static int Main()
    {
        try
        {
            int len = Nothing.Length;
            GC.KeepAlive(len);
            return 1;
        }
        catch (NullReferenceException)
        {
            Console.WriteLine("eh: eh_catch_nre ok");
            return 0;
        }
    }
}
