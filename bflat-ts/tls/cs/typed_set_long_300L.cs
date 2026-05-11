        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H { [ThreadStatic] public static long V; }

        class Program
        {
            static int Main()
            {
                H.V = 300L;
                if (!H.V.Equals((long)(300L))) return 1;
                Console.WriteLine("tls: typed_set_long_300L ok");
                return 0;
            }
        }
