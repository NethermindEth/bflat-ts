// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Throwing from a generic method must unwind like any other frame.
using System;

class Program
{
    static T Fail<T>(string msg) => throw new InvalidOperationException(msg);

    static int Main()
    {
        try
        {
            int v = Fail<int>("generic");
            GC.KeepAlive(v);
            return 1;
        }
        catch (InvalidOperationException e)
        {
            if (e.Message != "generic") return 2;
        }
        try
        {
            string s = Fail<string>("generic-ref");
            GC.KeepAlive(s);
            return 3;
        }
        catch (InvalidOperationException e)
        {
            if (e.Message != "generic-ref") return 4;
            Console.WriteLine("eh: eh_generic_method_throw ok");
            return 0;
        }
    }
}
