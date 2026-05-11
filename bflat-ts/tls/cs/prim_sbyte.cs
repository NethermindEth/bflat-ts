        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Holder { [ThreadStatic] public static sbyte V; }

        class Program
        {
            static int Main()
            {
                if (!Holder.V.Equals((sbyte)0)) return 1;
                Holder.V = (sbyte)-42;
                if (!Holder.V.Equals((sbyte)-42)) return 1;
                Console.WriteLine("tls: prim_sbyte ok");
                return 0;
            }
        }
