        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class S0 { [ThreadStatic] public static int V; }
class S1 { [ThreadStatic] public static int V; }
class S2 { [ThreadStatic] public static int V; }
class S3 { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                S0.V = 0;
        S1.V = 1;
        S2.V = 2;
        S3.V = 3;
                if (S0.V != 0) return 1;
        if (S1.V != 1) return 1;
        if (S2.V != 2) return 1;
        if (S3.V != 3) return 1;
                Console.WriteLine("tls_stress: indep_int_4 ok");
                return 0;
            }
        }
