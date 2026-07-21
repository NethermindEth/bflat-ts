// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Exercises: interface dispatch when called method allocates and returns objects
// The allocation between CID-resolve and return forces compiler to use temp regs
// Bug targeted: t5 clobber risk when complex code surrounds dispatch

using System;
using System.Text;

interface IProducer
{
    string Produce();
}

class ConstProducer : IProducer
{
    private string _val;
    public ConstProducer(string val) { _val = val; }
    public string Produce() => _val;
}

class JoinProducer : IProducer
{
    private string _a, _b;
    public JoinProducer(string a, string b) { _a = a; _b = b; }
    public string Produce() => _a + "_" + _b;
}

class RepeatProducer : IProducer
{
    private string _ch;
    private int _n;
    public RepeatProducer(string ch, int n) { _ch = ch; _n = n; }
    public string Produce()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < _n; i++)
            sb.Append(_ch);
        return sb.ToString();
    }
}

class Program
{
    static int Main()
    {
        IProducer p1 = new ConstProducer("hello");
        IProducer p2 = new JoinProducer("foo", "bar");
        IProducer p3 = new RepeatProducer("x", 4);

        // Call through interface and store result - forces compiler to keep
        // dispatch result in registers across subsequent allocations
        string r1 = p1.Produce();
        if (r1 != "hello") return 1;

        string r2 = p2.Produce();
        if (r2 != "foo_bar") return 1;

        string r3 = p3.Produce();
        if (r3 != "xxxx") return 1;

        // Use results in a subsequent computation to prevent elision
        string combined = r1 + "_" + r2 + "_" + r3;
        if (combined != "hello_foo_bar_xxxx") return 1;

        // Exercise dispatch in a loop, alternating producers - each iteration
        // allocates a new string, stressing the temp-register bookkeeping
        IProducer[] producers = new IProducer[] { p1, p2, p3 };
        int totalLen = 0;
        for (int iter = 0; iter < 50; iter++)
        {
            for (int i = 0; i < producers.Length; i++)
            {
                string result = producers[i].Produce();
                totalLen += result.Length;
            }
        }
        // Each iteration: "hello"(5) + "foo_bar"(7) + "xxxx"(4) = 16; 50 * 16 = 800
        if (totalLen != 800) return 1;

        Console.WriteLine($"dispatch_stress: iface_return_obj ok combined={combined} totalLen={totalLen}");
        return 0;
    }
}