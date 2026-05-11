        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Holder { [ThreadStatic] public static uint V; }

        class Program
        {
            static int Main()
            {
                if (!Holder.V.Equals((uint)0)) return 1;
                Holder.V = 42u;
                if (!Holder.V.Equals(42u)) return 1;
                Console.WriteLine("tls: prim_uint ok");
                return 0;
            }
        }
