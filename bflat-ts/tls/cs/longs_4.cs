        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H {
            [ThreadStatic] public static long L0;
    [ThreadStatic] public static long L1;
    [ThreadStatic] public static long L2;
    [ThreadStatic] public static long L3;
        }

        class Program
        {
            static int Main()
            {
                H.L0 = 0;
        H.L1 = 1;
        H.L2 = 2;
        H.L3 = 3;
                if (H.L0 != 0) return 1;
        if (H.L1 != 1) return 1;
        if (H.L2 != 2) return 1;
        if (H.L3 != 3) return 1;
                Console.WriteLine("tls: longs_4 ok");
                return 0;
            }
        }
