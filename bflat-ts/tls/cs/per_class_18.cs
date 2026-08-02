        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H_18 { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                H_18.V = 18;
                if (H_18.V != 18) return 1;
                Console.WriteLine("tls: per_class_18 ok");
                return 0;
            }
        }
