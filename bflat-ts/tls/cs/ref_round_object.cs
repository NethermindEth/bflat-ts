        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H { [ThreadStatic] public static object V; }

        class Program
        {
            static int Main()
            {
                if (H.V != null) return 1;
                H.V = new object();
                if (H.V == null) return 1;
                H.V = null;
                if (H.V != null) return 1;
                Console.WriteLine("tls: ref_round_object ok");
                return 0;
            }
        }
