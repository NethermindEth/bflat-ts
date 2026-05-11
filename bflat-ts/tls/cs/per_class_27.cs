        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H_27 { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                H_27.V = 27;
                if (H_27.V != 27) return 1;
                Console.WriteLine("tls: per_class_27 ok");
                return 0;
            }
        }
