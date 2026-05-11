        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H_23 { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                H_23.V = 23;
                if (H_23.V != 23) return 1;
                Console.WriteLine("tls: per_class_23 ok");
                return 0;
            }
        }
