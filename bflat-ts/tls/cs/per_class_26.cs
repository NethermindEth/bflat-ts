        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H_26 { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                H_26.V = 26;
                if (H_26.V != 26) return 1;
                Console.WriteLine("tls: per_class_26 ok");
                return 0;
            }
        }
