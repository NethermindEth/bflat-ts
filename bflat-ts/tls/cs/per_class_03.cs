        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H_3 { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                H_3.V = 3;
                if (H_3.V != 3) return 1;
                Console.WriteLine("tls: per_class_03 ok");
                return 0;
            }
        }
