        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H {
            [ThreadStatic] public static long L0;
        }

        class Program
        {
            static int Main()
            {
                H.L0 = 0;
                if (H.L0 != 0) return 1;
                Console.WriteLine("tls: longs_1 ok");
                return 0;
            }
        }
