        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Holder { [ThreadStatic] public static long V; }

        class Program
        {
            static int Main()
            {
                if (!Holder.V.Equals((long)0)) return 1;
                Holder.V = 42L;
                if (!Holder.V.Equals(42L)) return 1;
                Console.WriteLine("tls: prim_long ok");
                return 0;
            }
        }
