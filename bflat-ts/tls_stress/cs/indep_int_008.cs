        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class S0 { [ThreadStatic] public static int V; }
class S1 { [ThreadStatic] public static int V; }
class S2 { [ThreadStatic] public static int V; }
class S3 { [ThreadStatic] public static int V; }
class S4 { [ThreadStatic] public static int V; }
class S5 { [ThreadStatic] public static int V; }
class S6 { [ThreadStatic] public static int V; }
class S7 { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                S0.V = 0;
        S1.V = 1;
        S2.V = 2;
        S3.V = 3;
        S4.V = 4;
        S5.V = 5;
        S6.V = 6;
        S7.V = 7;
                if (S0.V != 0) return 1;
        if (S1.V != 1) return 1;
        if (S2.V != 2) return 1;
        if (S3.V != 3) return 1;
        if (S4.V != 4) return 1;
        if (S5.V != 5) return 1;
        if (S6.V != 6) return 1;
        if (S7.V != 7) return 1;
                Console.WriteLine("tls_stress: indep_int_8 ok");
                return 0;
            }
        }
