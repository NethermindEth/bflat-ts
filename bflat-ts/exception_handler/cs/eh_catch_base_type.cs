// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// catch (Exception) must catch a derived exception type.
using System;

class Program
{
    static int Main()
    {
        try
        {
            throw new ArgumentException("derived");
        }
        catch (Exception e)
        {
            if (!(e is ArgumentException)) return 2;
            if (e.Message != "derived") return 3;
            Console.WriteLine("eh: eh_catch_base_type ok");
            return 0;
        }
    }
}
