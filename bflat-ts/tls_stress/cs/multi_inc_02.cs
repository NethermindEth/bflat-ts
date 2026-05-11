        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class C0 { [ThreadStatic] public static long V; }
class C1 { [ThreadStatic] public static long V; }

        class Program
        {
            static int Main()
            {
                const long iters = 100;
                for (long it = 0; it < iters; it++)
                {
                    C0.V += 1;
            C1.V += 1;
                }
                if (C0.V != iters) return 1;
        if (C1.V != iters) return 1;
                Console.WriteLine("tls_stress: multi_inc_2 ok");
                return 0;
            }
        }
