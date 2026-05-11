        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H_2 { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                H_2.V = 2;
                if (H_2.V != 2) return 1;
                Console.WriteLine("tls: per_class_02 ok");
                return 0;
            }
        }
