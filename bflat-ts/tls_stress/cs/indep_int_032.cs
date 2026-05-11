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
class S16 { [ThreadStatic] public static int V; }
class S17 { [ThreadStatic] public static int V; }
class S18 { [ThreadStatic] public static int V; }
class S19 { [ThreadStatic] public static int V; }
class S20 { [ThreadStatic] public static int V; }
class S21 { [ThreadStatic] public static int V; }
class S22 { [ThreadStatic] public static int V; }
class S23 { [ThreadStatic] public static int V; }
class S24 { [ThreadStatic] public static int V; }
class S25 { [ThreadStatic] public static int V; }
class S26 { [ThreadStatic] public static int V; }
class S27 { [ThreadStatic] public static int V; }
class S28 { [ThreadStatic] public static int V; }
class S29 { [ThreadStatic] public static int V; }
class S30 { [ThreadStatic] public static int V; }
class S31 { [ThreadStatic] public static int V; }

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
        S16.V = 16;
        S17.V = 17;
        S18.V = 18;
        S19.V = 19;
        S20.V = 20;
        S21.V = 21;
        S22.V = 22;
        S23.V = 23;
        S24.V = 24;
        S25.V = 25;
        S26.V = 26;
        S27.V = 27;
        S28.V = 28;
        S29.V = 29;
        S30.V = 30;
        S31.V = 31;
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
        if (S16.V != 16) return 1;
        if (S17.V != 17) return 1;
        if (S18.V != 18) return 1;
        if (S19.V != 19) return 1;
        if (S20.V != 20) return 1;
        if (S21.V != 21) return 1;
        if (S22.V != 22) return 1;
        if (S23.V != 23) return 1;
        if (S24.V != 24) return 1;
        if (S25.V != 25) return 1;
        if (S26.V != 26) return 1;
        if (S27.V != 27) return 1;
        if (S28.V != 28) return 1;
        if (S29.V != 29) return 1;
        if (S30.V != 30) return 1;
        if (S31.V != 31) return 1;
                Console.WriteLine("tls_stress: indep_int_32 ok");
                return 0;
            }
        }
