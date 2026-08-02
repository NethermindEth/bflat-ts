        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H {
            [ThreadStatic] public static int T0;
    public static int N0;
    [ThreadStatic] public static int T1;
    public static int N1;
    [ThreadStatic] public static int T2;
    public static int N2;
    [ThreadStatic] public static int T3;
    public static int N3;
        }

        class Program
        {
            static int Main()
            {
                H.T0 = 0; H.N0 = -0;
        H.T1 = 1; H.N1 = -1;
        H.T2 = 2; H.N2 = -2;
        H.T3 = 3; H.N3 = -3;
                if (H.T0 != 0 || H.N0 != -0) return 1;
        if (H.T1 != 1 || H.N1 != -1) return 1;
        if (H.T2 != 2 || H.N2 != -2) return 1;
        if (H.T3 != 3 || H.N3 != -3) return 1;
                Console.WriteLine("tls_stress: mixed_static_4 ok");
                return 0;
            }
        }
