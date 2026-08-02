        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H_16 { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                H_16.V = 16;
                if (H_16.V != 16) return 1;
                Console.WriteLine("tls: per_class_16 ok");
                return 0;
            }
        }
