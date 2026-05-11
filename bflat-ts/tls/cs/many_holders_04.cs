        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class T0 { [ThreadStatic] public static int V; }
class T1 { [ThreadStatic] public static int V; }
class T2 { [ThreadStatic] public static int V; }
class T3 { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                T0.V = 0;
        T1.V = 1;
        T2.V = 2;
        T3.V = 3;
                if (T0.V != 0) return 1;
        if (T1.V != 1) return 1;
        if (T2.V != 2) return 1;
        if (T3.V != 3) return 1;
                Console.WriteLine("tls: many_holders_4 ok");
                return 0;
            }
        }
