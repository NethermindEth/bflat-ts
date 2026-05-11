        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                H.V = -1000000;
                if (H.V != -1000000) return 1;
                Console.WriteLine("tls: set_get_neg1000000 ok");
                return 0;
            }
        }
