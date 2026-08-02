        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H_7 { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                H_7.V = 7;
                if (H_7.V != 7) return 1;
                Console.WriteLine("tls: per_class_07 ok");
                return 0;
            }
        }
