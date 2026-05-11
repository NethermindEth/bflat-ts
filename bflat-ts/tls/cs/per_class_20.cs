        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H_20 { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                H_20.V = 20;
                if (H_20.V != 20) return 1;
                Console.WriteLine("tls: per_class_20 ok");
                return 0;
            }
        }
