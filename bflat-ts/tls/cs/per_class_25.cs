        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H_25 { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                H_25.V = 25;
                if (H_25.V != 25) return 1;
                Console.WriteLine("tls: per_class_25 ok");
                return 0;
            }
        }
