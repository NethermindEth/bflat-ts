        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class C1 { [ThreadStatic] public static long V; }
        class C2 { [ThreadStatic] public static long V; }

        class Program
        {
            static int Main()
            {
                for (long i = 0; i < 1000; i++)
                {
                    C1.V += 1;
                    C2.V += 2;
                }
                if (C1.V != 1000) return 1;
                if (C2.V != 2000) return 1;
                Console.WriteLine("tls_stress: long_inc_1000 ok");
                return 0;
            }
        }
