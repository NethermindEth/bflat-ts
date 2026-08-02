        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H_9 { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                H_9.V = 9;
                if (H_9.V != 9) return 1;
                Console.WriteLine("tls: per_class_09 ok");
                return 0;
            }
        }
