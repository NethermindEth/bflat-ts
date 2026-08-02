        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Holder { [ThreadStatic] public static short V; }

        class Program
        {
            static int Main()
            {
                if (!Holder.V.Equals((short)0)) return 1;
                Holder.V = (short)42;
                if (!Holder.V.Equals((short)42)) return 1;
                Console.WriteLine("tls: prim_short ok");
                return 0;
            }
        }
