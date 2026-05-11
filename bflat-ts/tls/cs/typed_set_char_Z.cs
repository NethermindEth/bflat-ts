        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H { [ThreadStatic] public static char V; }

        class Program
        {
            static int Main()
            {
                H.V = 'Z';
                if (!H.V.Equals((char)('Z'))) return 1;
                Console.WriteLine("tls: typed_set_char_Z ok");
                return 0;
            }
        }
