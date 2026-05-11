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
    [ThreadStatic] public static int F8;
    [ThreadStatic] public static int F9;
    [ThreadStatic] public static int F10;
    [ThreadStatic] public static int F11;
    [ThreadStatic] public static int F12;
    [ThreadStatic] public static int F13;
    [ThreadStatic] public static int F14;
    [ThreadStatic] public static int F15;
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
        Holder.F8 = 8;
        Holder.F9 = 9;
        Holder.F10 = 10;
        Holder.F11 = 11;
        Holder.F12 = 12;
        Holder.F13 = 13;
        Holder.F14 = 14;
        Holder.F15 = 15;
                if (Holder.F0 != 0) return 1;
        if (Holder.F1 != 1) return 1;
        if (Holder.F2 != 2) return 1;
        if (Holder.F3 != 3) return 1;
        if (Holder.F4 != 4) return 1;
        if (Holder.F5 != 5) return 1;
        if (Holder.F6 != 6) return 1;
        if (Holder.F7 != 7) return 1;
        if (Holder.F8 != 8) return 1;
        if (Holder.F9 != 9) return 1;
        if (Holder.F10 != 10) return 1;
        if (Holder.F11 != 11) return 1;
        if (Holder.F12 != 12) return 1;
        if (Holder.F13 != 13) return 1;
        if (Holder.F14 != 14) return 1;
        if (Holder.F15 != 15) return 1;
                Console.WriteLine("tls: multi_16 ok");
                return 0;
            }
        }
