// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// An inner catch must handle the exception; the outer one stays untouched.
using System;

class Program
{
    static int Main()
    {
        int mark = 0;
        try
        {
            try
            {
                throw new Exception("inner");
            }
            catch (Exception)
            {
                mark = 1;
            }
            if (mark != 1) return 1;
            mark = 2;
        }
        catch (Exception)
        {
            return 2;
        }
        if (mark != 2) return 3;
        Console.WriteLine("eh: eh_nested_try_inner_catch ok");
        return 0;
    }
}
