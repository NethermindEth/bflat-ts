        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Holder { [ThreadStatic] public static ulong V; }

        class Program
        {
            static int Main()
            {
                if (!Holder.V.Equals((ulong)0)) return 1;
                Holder.V = 42UL;
                if (!Holder.V.Equals(42UL)) return 1;
                Console.WriteLine("tls: prim_ulong ok");
                return 0;
            }
        }
