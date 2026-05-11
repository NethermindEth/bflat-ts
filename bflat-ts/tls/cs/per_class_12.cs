        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H_12 { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                H_12.V = 12;
                if (H_12.V != 12) return 1;
                Console.WriteLine("tls: per_class_12 ok");
                return 0;
            }
        }
