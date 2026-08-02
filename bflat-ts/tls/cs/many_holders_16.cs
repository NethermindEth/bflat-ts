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
                Console.WriteLine("tls: many_holders_16 ok");
                return 0;
            }
        }
