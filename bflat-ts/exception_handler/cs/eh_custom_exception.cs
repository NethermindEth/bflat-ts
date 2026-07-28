// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// User-defined exception types must round-trip through throw/catch with
// their extra state intact.
using System;

class PayloadException : Exception
{
    public int Code;
    public PayloadException(string msg, int code) : base(msg) => Code = code;
}

class Program
{
    static int Main()
    {
        try
        {
            throw new PayloadException("custom", 77);
        }
        catch (PayloadException e)
        {
            if (e.Message != "custom") return 2;
            if (e.Code != 77) return 3;
            Console.WriteLine("eh: eh_custom_exception ok");
            return 0;
        }
    }
}
