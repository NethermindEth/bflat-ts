        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                H.V = 0;
                for (int i = 0; i < 10; i++) H.V++;
                if (H.V != 10) return 1;
                Console.WriteLine("tls: inc_loop_10 ok");
                return 0;
            }
        }
