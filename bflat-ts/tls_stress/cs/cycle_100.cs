        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                for (int c = 0; c < 100; c++)
                {
                    H.V = c;
                    if (H.V != c) return 1;
                }
                Console.WriteLine("tls_stress: cycle_100 ok");
                return 0;
            }
        }
