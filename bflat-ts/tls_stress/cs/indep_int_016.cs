        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class S0 { [ThreadStatic] public static int V; }
class S1 { [ThreadStatic] public static int V; }
class S2 { [ThreadStatic] public static int V; }
class S3 { [ThreadStatic] public static int V; }
class S4 { [ThreadStatic] public static int V; }
class S5 { [ThreadStatic] public static int V; }
class S6 { [ThreadStatic] public static int V; }
class S7 { [ThreadStatic] public static int V; }
class S8 { [ThreadStatic] public static int V; }
class S9 { [ThreadStatic] public static int V; }
class S10 { [ThreadStatic] public static int V; }
class S11 { [ThreadStatic] public static int V; }
class S12 { [ThreadStatic] public static int V; }
class S13 { [ThreadStatic] public static int V; }
class S14 { [ThreadStatic] public static int V; }
class S15 { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                S0.V = 0;
        S1.V = 1;
        S2.V = 2;
        S3.V = 3;
        S4.V = 4;
        S5.V = 5;
        S6.V = 6;
        S7.V = 7;
        S8.V = 8;
        S9.V = 9;
        S10.V = 10;
        S11.V = 11;
        S12.V = 12;
        S13.V = 13;
        S14.V = 14;
        S15.V = 15;
                if (S0.V != 0) return 1;
        if (S1.V != 1) return 1;
        if (S2.V != 2) return 1;
        if (S3.V != 3) return 1;
        if (S4.V != 4) return 1;
        if (S5.V != 5) return 1;
        if (S6.V != 6) return 1;
        if (S7.V != 7) return 1;
        if (S8.V != 8) return 1;
        if (S9.V != 9) return 1;
        if (S10.V != 10) return 1;
        if (S11.V != 11) return 1;
        if (S12.V != 12) return 1;
        if (S13.V != 13) return 1;
        if (S14.V != 14) return 1;
        if (S15.V != 15) return 1;
                Console.WriteLine("tls_stress: indep_int_16 ok");
                return 0;
            }
        }
