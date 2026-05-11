        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H { [ThreadStatic] public static string V; }

        class Program
        {
            static int Main()
            {
                if (H.V != null) return 1;
                H.V = "x";
                if (H.V == null) return 1;
                H.V = null;
                if (H.V != null) return 1;
                Console.WriteLine("tls: ref_round_string ok");
                return 0;
            }
        }
