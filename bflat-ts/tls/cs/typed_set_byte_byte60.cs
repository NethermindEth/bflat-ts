        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H { [ThreadStatic] public static byte V; }

        class Program
        {
            static int Main()
            {
                H.V = (byte)60;
                if (!H.V.Equals((byte)((byte)60))) return 1;
                Console.WriteLine("tls: typed_set_byte_byte60 ok");
                return 0;
            }
        }
