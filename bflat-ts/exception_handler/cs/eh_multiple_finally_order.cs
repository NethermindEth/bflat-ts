// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Nested finally blocks must run inner-first while unwinding.
using System;

class Program
{
    static int Order;
    static int InnerAt;
    static int OuterAt;

    static void Thrower()
    {
        try
        {
            try
            {
                throw new Exception("unwind");
            }
            finally
            {
                InnerAt = ++Order;
            }
        }
        finally
        {
            OuterAt = ++Order;
        }
    }

    static int Main()
    {
        try
        {
            Thrower();
            return 1;
        }
        catch (Exception)
        {
            if (InnerAt != 1) return 2;
            if (OuterAt != 2) return 3;
            Console.WriteLine("eh: eh_multiple_finally_order ok");
            return 0;
        }
    }
}
