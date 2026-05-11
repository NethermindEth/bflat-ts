        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Holder {
            [ThreadStatic] public static int F0;
        }

        class Program
        {
            static int Main()
            {
                Holder.F0 = 0;
                if (Holder.F0 != 0) return 1;
                Console.WriteLine("tls: multi_1 ok");
                return 0;
            }
        }
