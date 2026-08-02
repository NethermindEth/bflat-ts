        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                H.V = 100;
                if (!H.V.Equals((int)(100))) return 1;
                Console.WriteLine("tls: typed_set_int_100 ok");
                return 0;
            }
        }
