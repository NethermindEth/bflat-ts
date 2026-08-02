        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H_5 { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                H_5.V = 5;
                if (H_5.V != 5) return 1;
                Console.WriteLine("tls: per_class_05 ok");
                return 0;
            }
        }
