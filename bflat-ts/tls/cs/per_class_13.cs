        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H_13 { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                H_13.V = 13;
                if (H_13.V != 13) return 1;
                Console.WriteLine("tls: per_class_13 ok");
                return 0;
            }
        }
