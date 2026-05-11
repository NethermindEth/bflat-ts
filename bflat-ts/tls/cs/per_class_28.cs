        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H_28 { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                H_28.V = 28;
                if (H_28.V != 28) return 1;
                Console.WriteLine("tls: per_class_28 ok");
                return 0;
            }
        }
