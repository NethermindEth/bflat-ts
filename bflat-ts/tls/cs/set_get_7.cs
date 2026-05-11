        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                H.V = 7;
                if (H.V != 7) return 1;
                Console.WriteLine("tls: set_get_7 ok");
                return 0;
            }
        }
