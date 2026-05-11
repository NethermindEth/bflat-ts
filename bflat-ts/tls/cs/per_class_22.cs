        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H_22 { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                H_22.V = 22;
                if (H_22.V != 22) return 1;
                Console.WriteLine("tls: per_class_22 ok");
                return 0;
            }
        }
