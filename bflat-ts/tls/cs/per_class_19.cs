        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H_19 { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                H_19.V = 19;
                if (H_19.V != 19) return 1;
                Console.WriteLine("tls: per_class_19 ok");
                return 0;
            }
        }
