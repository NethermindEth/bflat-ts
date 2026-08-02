        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H {
            [ThreadStatic] public static int F0;
    [ThreadStatic] public static int F1;
    [ThreadStatic] public static int F2;
    [ThreadStatic] public static int F3;
    [ThreadStatic] public static int F4;
    [ThreadStatic] public static int F5;
    [ThreadStatic] public static int F6;
    [ThreadStatic] public static int F7;
    [ThreadStatic] public static int F8;
    [ThreadStatic] public static int F9;
        }

        class Program
        {
            static int Main()
            {
                H.F0 = 0;
        H.F1 = 1;
        H.F2 = 2;
        H.F3 = 3;
        H.F4 = 4;
        H.F5 = 5;
        H.F6 = 6;
        H.F7 = 7;
        H.F8 = 8;
        H.F9 = 9;
                if (H.F0 != 0) return 1;
        if (H.F1 != 1) return 1;
        if (H.F2 != 2) return 1;
        if (H.F3 != 3) return 1;
        if (H.F4 != 4) return 1;
        if (H.F5 != 5) return 1;
        if (H.F6 != 6) return 1;
        if (H.F7 != 7) return 1;
        if (H.F8 != 8) return 1;
        if (H.F9 != 9) return 1;
                Console.WriteLine("tls_stress: wide_class_10 ok");
                return 0;
            }
        }
