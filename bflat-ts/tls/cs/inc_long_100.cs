        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H { [ThreadStatic] public static long V; }

        class Program
        {
            static int Main()
            {
                H.V = 0;
                for (int i = 0; i < 100; i++) H.V++;
                if (H.V != 100) return 1;
                Console.WriteLine("tls: inc_long_100 ok");
                return 0;
            }
        }
