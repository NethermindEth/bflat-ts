        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H_8 { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                H_8.V = 8;
                if (H_8.V != 8) return 1;
                Console.WriteLine("tls: per_class_08 ok");
                return 0;
            }
        }
