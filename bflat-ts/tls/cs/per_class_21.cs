        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H_21 { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                H_21.V = 21;
                if (H_21.V != 21) return 1;
                Console.WriteLine("tls: per_class_21 ok");
                return 0;
            }
        }
