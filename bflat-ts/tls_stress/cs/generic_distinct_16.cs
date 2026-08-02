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
        H<C8>.V = 8;
        H<C9>.V = 9;
        H<C10>.V = 10;
        H<C11>.V = 11;
        H<C12>.V = 12;
        H<C13>.V = 13;
        H<C14>.V = 14;
        H<C15>.V = 15;
        if (H<C0>.V != 0) return 1;
        if (H<C1>.V != 1) return 1;
        if (H<C2>.V != 2) return 1;
        if (H<C3>.V != 3) return 1;
        if (H<C4>.V != 4) return 1;
        if (H<C5>.V != 5) return 1;
        if (H<C6>.V != 6) return 1;
        if (H<C7>.V != 7) return 1;
        if (H<C8>.V != 8) return 1;
        if (H<C9>.V != 9) return 1;
        if (H<C10>.V != 10) return 1;
        if (H<C11>.V != 11) return 1;
        if (H<C12>.V != 12) return 1;
        if (H<C13>.V != 13) return 1;
        if (H<C14>.V != 14) return 1;
        if (H<C15>.V != 15) return 1;

        Console.WriteLine("tls_stress: generic_distinct_16 ok");
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
class C8 {}
class C9 {}
class C10 {}
class C11 {}
class C12 {}
class C13 {}
class C14 {}
class C15 {}
