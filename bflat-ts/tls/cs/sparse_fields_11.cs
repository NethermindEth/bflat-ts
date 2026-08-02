        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H {
            [ThreadStatic] public static int A0;
    [ThreadStatic] public static int A1;
    [ThreadStatic] public static int A2;
    [ThreadStatic] public static int A3;
    [ThreadStatic] public static int A4;
    [ThreadStatic] public static int A5;
    [ThreadStatic] public static int A6;
    [ThreadStatic] public static int A7;
    [ThreadStatic] public static int A8;
    [ThreadStatic] public static int A9;
    [ThreadStatic] public static int A10;
        }

        class Program
        {
            static int Main()
            {
                H.A0 = 1;
                H.A10 = 2;
                if (H.A0 + H.A10 != 3) return 1;
                Console.WriteLine("tls: sparse_fields_11 ok");
                return 0;
            }
        }
