        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Holder { [ThreadStatic] public static byte V; }

        class Program
        {
            static int Main()
            {
                if (!Holder.V.Equals((byte)0)) return 1;
                Holder.V = (byte)42;
                if (!Holder.V.Equals((byte)42)) return 1;
                Console.WriteLine("tls: prim_byte ok");
                return 0;
            }
        }
