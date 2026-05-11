        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Holder { [ThreadStatic] public static bool V; }

        class Program
        {
            static int Main()
            {
                if (!Holder.V.Equals((bool)false)) return 1;
                Holder.V = true;
                if (!Holder.V.Equals(true)) return 1;
                Console.WriteLine("tls: prim_bool ok");
                return 0;
            }
        }
