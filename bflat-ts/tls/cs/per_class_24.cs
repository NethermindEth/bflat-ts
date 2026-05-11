        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H_24 { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                H_24.V = 24;
                if (H_24.V != 24) return 1;
                Console.WriteLine("tls: per_class_24 ok");
                return 0;
            }
        }
