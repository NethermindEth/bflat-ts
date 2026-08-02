        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H<T> { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
        H<C0>.V = 0;
        H<C1>.V = 1;
        H<C2>.V = 2;
        H<C3>.V = 3;
        if (H<C0>.V != 0) return 1;
        if (H<C1>.V != 1) return 1;
        if (H<C2>.V != 2) return 1;
        if (H<C3>.V != 3) return 1;

        Console.WriteLine("tls_stress: generic_distinct_4 ok");
        return 0;
    }
}

class C0 {}
class C1 {}
class C2 {}
class C3 {}
