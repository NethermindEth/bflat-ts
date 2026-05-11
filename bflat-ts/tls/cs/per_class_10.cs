        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H_10 { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                H_10.V = 10;
                if (H_10.V != 10) return 1;
                Console.WriteLine("tls: per_class_10 ok");
                return 0;
            }
        }
