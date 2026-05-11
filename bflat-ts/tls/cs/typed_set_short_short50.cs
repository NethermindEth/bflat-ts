        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H { [ThreadStatic] public static short V; }

        class Program
        {
            static int Main()
            {
                H.V = (short)50;
                if (!H.V.Equals((short)((short)50))) return 1;
                Console.WriteLine("tls: typed_set_short_short50 ok");
                return 0;
            }
        }
