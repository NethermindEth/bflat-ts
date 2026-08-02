        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H_14 { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                H_14.V = 14;
                if (H_14.V != 14) return 1;
                Console.WriteLine("tls: per_class_14 ok");
                return 0;
            }
        }
