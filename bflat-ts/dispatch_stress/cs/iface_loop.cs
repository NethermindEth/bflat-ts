// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Exercises: interface dispatch (CID resolve) in tight loop with 4 implementations
// Bug targeted: t5 register clobber in __rhp_cid_resolve_nocache
// If t5 is clobbered before dispatch cell is read, wrong method pointer is called

using System;

interface ICalc
{
    int Compute(int x);
}

class Adder : ICalc
{
    private int _k;
    public Adder(int k) { _k = k; }
    public int Compute(int x) => x + _k;
}

class Multiplier : ICalc
{
    private int _k;
    public Multiplier(int k) { _k = k; }
    public int Compute(int x) => x * _k;
}

class Subtractor : ICalc
{
    private int _k;
    public Subtractor(int k) { _k = k; }
    public int Compute(int x) => x - _k;
}

class Negator : ICalc
{
    public int Compute(int x) => -x;
}

class Program
{
    static int Main()
    {
        ICalc[] ops = new ICalc[]
        {
            new Adder(3),
            new Multiplier(4),
            new Subtractor(2),
            new Negator()
        };

        int[] expected = new int[] { 13, 40, 8, -10 };

        for (int iter = 0; iter < 200; iter++)
        {
            for (int i = 0; i < ops.Length; i++)
            {
                int result = ops[i].Compute(10);
                if (result != expected[i]) return 1;
            }
        }

        Console.WriteLine("dispatch_stress: iface_loop ok iterations=200");
        return 0;
    }
}