        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class S0 { [ThreadStatic] public static int V; }
class S1 { [ThreadStatic] public static long V; }

        class Program
        {
            static int Main()
            {
                S0.V = 1;
        S1.V = 2L;
                Console.WriteLine("tls_stress: mixed_types_2 ok");
                return 0;
            }
        }
