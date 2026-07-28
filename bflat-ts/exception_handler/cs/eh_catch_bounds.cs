// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Out-of-range array access must raise a catchable IndexOutOfRangeException.
using System;

class Program
{
    static int Index = 5; // static so the bounds check cannot be folded

    static int Main()
    {
        int[] arr = new int[3];
        try
        {
            arr[Index] = 1;
            return 1;
        }
        catch (IndexOutOfRangeException)
        {
            Console.WriteLine("eh: eh_catch_bounds ok");
            return 0;
        }
    }
}
