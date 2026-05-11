        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H_15 { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                H_15.V = 15;
                if (H_15.V != 15) return 1;
                Console.WriteLine("tls: per_class_15 ok");
                return 0;
            }
        }
