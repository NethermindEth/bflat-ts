        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H0 { [ThreadStatic] public static int V; }
class H1 { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                H0.V = 1;
        H1.V = 2;
                if (H0.V != 1) return 1;
        if (H1.V != 2) return 1;
                Console.WriteLine("tls: classes_2 ok");
                return 0;
            }
        }
