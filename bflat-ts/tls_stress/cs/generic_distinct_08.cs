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
        H<C4>.V = 4;
        H<C5>.V = 5;
        H<C6>.V = 6;
        H<C7>.V = 7;
        if (H<C0>.V != 0) return 1;
        if (H<C1>.V != 1) return 1;
        if (H<C2>.V != 2) return 1;
        if (H<C3>.V != 3) return 1;
        if (H<C4>.V != 4) return 1;
        if (H<C5>.V != 5) return 1;
        if (H<C6>.V != 6) return 1;
        if (H<C7>.V != 7) return 1;

        Console.WriteLine("tls_stress: generic_distinct_8 ok");
        return 0;
    }
}

class C0 {}
class C1 {}
class C2 {}
class C3 {}
class C4 {}
class C5 {}
class C6 {}
class C7 {}
