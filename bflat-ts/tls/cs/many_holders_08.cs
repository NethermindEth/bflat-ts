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
                if (T0.V != 0) return 1;
        if (T1.V != 1) return 1;
        if (T2.V != 2) return 1;
        if (T3.V != 3) return 1;
        if (T4.V != 4) return 1;
        if (T5.V != 5) return 1;
        if (T6.V != 6) return 1;
        if (T7.V != 7) return 1;
                Console.WriteLine("tls: many_holders_8 ok");
                return 0;
            }
        }
