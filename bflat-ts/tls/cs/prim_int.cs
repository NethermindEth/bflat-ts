        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Holder { [ThreadStatic] public static int V; }

        class Program
        {
            static int Main()
            {
                if (!Holder.V.Equals((int)0)) return 1;
                Holder.V = 42;
                if (!Holder.V.Equals(42)) return 1;
                Console.WriteLine("tls: prim_int ok");
                return 0;
            }
        }
