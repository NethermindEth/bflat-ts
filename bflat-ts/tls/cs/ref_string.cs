        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Holder { [ThreadStatic] public static string V; }

        class Program
        {
            static int Main()
            {
                if (Holder.V != null) return 1;
                Holder.V = "hi";
                if (Holder.V == null) return 1;
                Console.WriteLine("tls: ref_string ok");
                return 0;
            }
        }
