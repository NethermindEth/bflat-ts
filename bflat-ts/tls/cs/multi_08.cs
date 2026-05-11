        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Holder {
            [ThreadStatic] public static int F0;
    [ThreadStatic] public static int F1;
    [ThreadStatic] public static int F2;
    [ThreadStatic] public static int F3;
    [ThreadStatic] public static int F4;
    [ThreadStatic] public static int F5;
    [ThreadStatic] public static int F6;
    [ThreadStatic] public static int F7;
        }

        class Program
        {
            static int Main()
            {
                Holder.F0 = 0;
        Holder.F1 = 1;
        Holder.F2 = 2;
        Holder.F3 = 3;
        Holder.F4 = 4;
        Holder.F5 = 5;
        Holder.F6 = 6;
        Holder.F7 = 7;
                if (Holder.F0 != 0) return 1;
        if (Holder.F1 != 1) return 1;
        if (Holder.F2 != 2) return 1;
        if (Holder.F3 != 3) return 1;
        if (Holder.F4 != 4) return 1;
        if (Holder.F5 != 5) return 1;
        if (Holder.F6 != 6) return 1;
        if (Holder.F7 != 7) return 1;
                Console.WriteLine("tls: multi_8 ok");
                return 0;
            }
        }
