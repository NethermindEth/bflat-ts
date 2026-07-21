// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Tests __wrap_RhpCidResolve (interface dispatch) and
// __wrap_RhpAssignRefRiscV64 (reference assignment without write barrier).

using System;

interface IShape
{
    int Area();
    string Describe();
}

class Rectangle : IShape
{
    public int W, H;
    public Rectangle(int w, int h) { W = w; H = h; }
    public int Area() => W * H;
    public string Describe() => $"rect({W}x{H})";
}

class Circle : IShape
{
    public int R;
    public Circle(int r) { R = r; }
    // Approximate area: pi ~ 3, testing dispatch correctness not math
    public int Area() => 3 * R * R;
    public string Describe() => $"circle(r={R})";
}

class Program
{
    static int Compute(IShape s) => s.Area();

    static int Main()
    {
        IShape r = new Rectangle(3, 4);
        IShape c = new Circle(5);

        if (Compute(r) != 12) return 1;
        if (Compute(c) != 75) return 1;

        if (r.Describe() != "rect(3x4)") return 1;
        if (c.Describe() != "circle(r=5)") return 1;

        // Array of interfaces - exercises dispatch for each element
        IShape[] shapes = new IShape[] { r, c, new Rectangle(2, 2) };
        int total = 0;
        foreach (IShape s in shapes)
            total += s.Area();
        // 12 + 75 + 4 = 91
        if (total != 91) return 1;

        Console.WriteLine($"dispatch: interface ok total={total}");
        return 0;
    }
}