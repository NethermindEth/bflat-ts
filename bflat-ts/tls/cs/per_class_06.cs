        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H_6 { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                H_6.V = 6;
                if (H_6.V != 6) return 1;
                Console.WriteLine("tls: per_class_06 ok");
                return 0;
            }
        }
