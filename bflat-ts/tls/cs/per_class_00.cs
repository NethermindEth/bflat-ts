        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H_0 { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                H_0.V = 0;
                if (H_0.V != 0) return 1;
                Console.WriteLine("tls: per_class_00 ok");
                return 0;
            }
        }
