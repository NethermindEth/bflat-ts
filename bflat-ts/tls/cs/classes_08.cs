        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H0 { [ThreadStatic] public static int V; }
class H1 { [ThreadStatic] public static int V; }
class H2 { [ThreadStatic] public static int V; }
class H3 { [ThreadStatic] public static int V; }
class H4 { [ThreadStatic] public static int V; }
class H5 { [ThreadStatic] public static int V; }
class H6 { [ThreadStatic] public static int V; }
class H7 { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                H0.V = 1;
        H1.V = 2;
        H2.V = 3;
        H3.V = 4;
        H4.V = 5;
        H5.V = 6;
        H6.V = 7;
        H7.V = 8;
                if (H0.V != 1) return 1;
        if (H1.V != 2) return 1;
        if (H2.V != 3) return 1;
        if (H3.V != 4) return 1;
        if (H4.V != 5) return 1;
        if (H5.V != 6) return 1;
        if (H6.V != 7) return 1;
        if (H7.V != 8) return 1;
                Console.WriteLine("tls: classes_8 ok");
                return 0;
            }
        }
