        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Holder { [ThreadStatic] public static ushort V; }

        class Program
        {
            static int Main()
            {
                if (!Holder.V.Equals((ushort)0)) return 1;
                Holder.V = (ushort)42;
                if (!Holder.V.Equals((ushort)42)) return 1;
                Console.WriteLine("tls: prim_ushort ok");
                return 0;
            }
        }
