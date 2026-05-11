        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H { [ThreadStatic] public static bool V; }

        class Program
        {
            static int Main()
            {
                H.V = true;
                if (!H.V.Equals((bool)(true))) return 1;
                Console.WriteLine("tls: typed_set_bool_true ok");
                return 0;
            }
        }
