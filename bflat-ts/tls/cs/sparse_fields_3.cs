        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H {
            [ThreadStatic] public static int A0;
    [ThreadStatic] public static int A1;
    [ThreadStatic] public static int A2;
        }

        class Program
        {
            static int Main()
            {
                H.A0 = 1;
                H.A2 = 2;
                if (H.A0 + H.A2 != 3) return 1;
                Console.WriteLine("tls: sparse_fields_3 ok");
                return 0;
            }
        }
