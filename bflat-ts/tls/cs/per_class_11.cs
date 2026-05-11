        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H_11 { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                H_11.V = 11;
                if (H_11.V != 11) return 1;
                Console.WriteLine("tls: per_class_11 ok");
                return 0;
            }
        }
