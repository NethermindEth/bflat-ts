// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Throw several frames deep, catch at the top: multi-frame unwinding.
using System;

class Program
{
    static int Depth;

    static void A() { Depth++; B(); }
    static void B() { Depth++; C(); }
    static void C() { Depth++; throw new InvalidOperationException("deep"); }

    static int Main()
    {
        try
        {
            A();
            return 1;
        }
        catch (InvalidOperationException e)
        {
            if (Depth != 3) return 2;
            if (e.Message != "deep") return 3;
            Console.WriteLine("eh: eh_deep_throw_shallow_catch ok");
            return 0;
        }
    }
}
