        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H { [ThreadStatic] public static long V; }

        class Program
        {
            static int Main()
            {
                H.V = 400L;
                if (!H.V.Equals((long)(400L))) return 1;
                Console.WriteLine("tls: typed_set_long_400L ok");
                return 0;
            }
        }
