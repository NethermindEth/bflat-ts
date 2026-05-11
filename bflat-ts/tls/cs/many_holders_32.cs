        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class T0 { [ThreadStatic] public static int V; }
class T1 { [ThreadStatic] public static int V; }
class T2 { [ThreadStatic] public static int V; }
class T3 { [ThreadStatic] public static int V; }
class T4 { [ThreadStatic] public static int V; }
class T5 { [ThreadStatic] public static int V; }
class T6 { [ThreadStatic] public static int V; }
class T7 { [ThreadStatic] public static int V; }
class T8 { [ThreadStatic] public static int V; }
class T9 { [ThreadStatic] public static int V; }
class T10 { [ThreadStatic] public static int V; }
class T11 { [ThreadStatic] public static int V; }
class T12 { [ThreadStatic] public static int V; }
class T13 { [ThreadStatic] public static int V; }
class T14 { [ThreadStatic] public static int V; }
class T15 { [ThreadStatic] public static int V; }
class T16 { [ThreadStatic] public static int V; }
class T17 { [ThreadStatic] public static int V; }
class T18 { [ThreadStatic] public static int V; }
class T19 { [ThreadStatic] public static int V; }
class T20 { [ThreadStatic] public static int V; }
class T21 { [ThreadStatic] public static int V; }
class T22 { [ThreadStatic] public static int V; }
class T23 { [ThreadStatic] public static int V; }
class T24 { [ThreadStatic] public static int V; }
class T25 { [ThreadStatic] public static int V; }
class T26 { [ThreadStatic] public static int V; }
class T27 { [ThreadStatic] public static int V; }
class T28 { [ThreadStatic] public static int V; }
class T29 { [ThreadStatic] public static int V; }
class T30 { [ThreadStatic] public static int V; }
class T31 { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                T0.V = 0;
        T1.V = 1;
        T2.V = 2;
        T3.V = 3;
        T4.V = 4;
        T5.V = 5;
        T6.V = 6;
        T7.V = 7;
        T8.V = 8;
        T9.V = 9;
        T10.V = 10;
        T11.V = 11;
        T12.V = 12;
        T13.V = 13;
        T14.V = 14;
        T15.V = 15;
        T16.V = 16;
        T17.V = 17;
        T18.V = 18;
        T19.V = 19;
        T20.V = 20;
        T21.V = 21;
        T22.V = 22;
        T23.V = 23;
        T24.V = 24;
        T25.V = 25;
        T26.V = 26;
        T27.V = 27;
        T28.V = 28;
        T29.V = 29;
        T30.V = 30;
        T31.V = 31;
                if (T0.V != 0) return 1;
        if (T1.V != 1) return 1;
        if (T2.V != 2) return 1;
        if (T3.V != 3) return 1;
        if (T4.V != 4) return 1;
        if (T5.V != 5) return 1;
        if (T6.V != 6) return 1;
        if (T7.V != 7) return 1;
        if (T8.V != 8) return 1;
        if (T9.V != 9) return 1;
        if (T10.V != 10) return 1;
        if (T11.V != 11) return 1;
        if (T12.V != 12) return 1;
        if (T13.V != 13) return 1;
        if (T14.V != 14) return 1;
        if (T15.V != 15) return 1;
        if (T16.V != 16) return 1;
        if (T17.V != 17) return 1;
        if (T18.V != 18) return 1;
        if (T19.V != 19) return 1;
        if (T20.V != 20) return 1;
        if (T21.V != 21) return 1;
        if (T22.V != 22) return 1;
        if (T23.V != 23) return 1;
        if (T24.V != 24) return 1;
        if (T25.V != 25) return 1;
        if (T26.V != 26) return 1;
        if (T27.V != 27) return 1;
        if (T28.V != 28) return 1;
        if (T29.V != 29) return 1;
        if (T30.V != 30) return 1;
        if (T31.V != 31) return 1;
                Console.WriteLine("tls: many_holders_32 ok");
                return 0;
            }
        }
