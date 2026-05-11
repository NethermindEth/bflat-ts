// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Base {}
class D : Base {}


class Program
{
    static int Main()
    {
        Base b = new D();
        if (!(b is D)) return 1;
        if (b is null) return 1;
        Console.WriteLine("dispatch: is_check_base ok");
        return 0;
    }
}
