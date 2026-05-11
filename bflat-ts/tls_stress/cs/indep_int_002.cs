        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class S0 { [ThreadStatic] public static int V; }
class S1 { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                S0.V = 0;
        S1.V = 1;
                if (S0.V != 0) return 1;
        if (S1.V != 1) return 1;
                Console.WriteLine("tls_stress: indep_int_2 ok");
                return 0;
            }
        }
