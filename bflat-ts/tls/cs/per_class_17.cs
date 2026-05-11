        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H_17 { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                H_17.V = 17;
                if (H_17.V != 17) return 1;
                Console.WriteLine("tls: per_class_17 ok");
                return 0;
            }
        }
