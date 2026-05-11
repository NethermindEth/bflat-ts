        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H_1 { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                H_1.V = 1;
                if (H_1.V != 1) return 1;
                Console.WriteLine("tls: per_class_01 ok");
                return 0;
            }
        }
