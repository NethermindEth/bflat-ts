// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Math.Truncate(double) is removed on zisk; the integer Math surface must
// stay fully functional.
using System;

class Program
{
    static int Main()
    {
        if (Math.Abs(-5) != 5) return 1;
        if (Math.Abs(-9000000000L) != 9000000000L) return 2;
        if (Math.Max(3, 7) != 7) return 3;
        if (Math.Min(-3L, 2L) != -3L) return 4;
        if (Math.Sign(-42) != -1) return 5;
        if (Math.Clamp(15, 0, 10) != 10) return 6;
        int q = Math.DivRem(17, 5, out int r);
        if (q != 3 || r != 2) return 7;
        if (Math.BigMul(1 << 20, 1 << 20) != (1L << 40)) return 8;
        Console.WriteLine("bcl_subst: math_integer_ops ok");
        return 0;
    }
}
