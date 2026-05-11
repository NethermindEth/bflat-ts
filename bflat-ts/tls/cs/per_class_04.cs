        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H_4 { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                H_4.V = 4;
                if (H_4.V != 4) return 1;
                Console.WriteLine("tls: per_class_04 ok");
                return 0;
            }
        }
