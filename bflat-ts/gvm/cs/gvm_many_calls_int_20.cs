// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Base { public virtual T Echo<T>(T x) => x; }
class D : Base { public override T Echo<T>(T x) => x; }


class Program
{
    static int Main()
    {
        Base b = new D();
        if (b.Echo<int>(0) != 0) return 1;
        if (b.Echo<int>(1) != 1) return 1;
        if (b.Echo<int>(2) != 2) return 1;
        if (b.Echo<int>(3) != 3) return 1;
        if (b.Echo<int>(4) != 4) return 1;
        if (b.Echo<int>(5) != 5) return 1;
        if (b.Echo<int>(6) != 6) return 1;
        if (b.Echo<int>(7) != 7) return 1;
        if (b.Echo<int>(8) != 8) return 1;
        if (b.Echo<int>(9) != 9) return 1;
        if (b.Echo<int>(10) != 10) return 1;
        if (b.Echo<int>(11) != 11) return 1;
        if (b.Echo<int>(12) != 12) return 1;
        if (b.Echo<int>(13) != 13) return 1;
        if (b.Echo<int>(14) != 14) return 1;
        if (b.Echo<int>(15) != 15) return 1;
        if (b.Echo<int>(16) != 16) return 1;
        if (b.Echo<int>(17) != 17) return 1;
        if (b.Echo<int>(18) != 18) return 1;
        if (b.Echo<int>(19) != 19) return 1;
        Console.WriteLine("gvm: gvm_many_calls_int_20 ok");
        return 0;
    }
}
