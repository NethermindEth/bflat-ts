        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H {
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
    [ThreadStatic] public static int F16;
    [ThreadStatic] public static int F17;
    [ThreadStatic] public static int F18;
    [ThreadStatic] public static int F19;
        }

        class Program
        {
            static int Main()
            {
                H.F0 = 0;
        H.F1 = 1;
        H.F2 = 2;
        H.F3 = 3;
        H.F4 = 4;
        H.F5 = 5;
        H.F6 = 6;
        H.F7 = 7;
        H.F8 = 8;
        H.F9 = 9;
        H.F10 = 10;
        H.F11 = 11;
        H.F12 = 12;
        H.F13 = 13;
        H.F14 = 14;
        H.F15 = 15;
        H.F16 = 16;
        H.F17 = 17;
        H.F18 = 18;
        H.F19 = 19;
                if (H.F0 != 0) return 1;
        if (H.F1 != 1) return 1;
        if (H.F2 != 2) return 1;
        if (H.F3 != 3) return 1;
        if (H.F4 != 4) return 1;
        if (H.F5 != 5) return 1;
        if (H.F6 != 6) return 1;
        if (H.F7 != 7) return 1;
        if (H.F8 != 8) return 1;
        if (H.F9 != 9) return 1;
        if (H.F10 != 10) return 1;
        if (H.F11 != 11) return 1;
        if (H.F12 != 12) return 1;
        if (H.F13 != 13) return 1;
        if (H.F14 != 14) return 1;
        if (H.F15 != 15) return 1;
        if (H.F16 != 16) return 1;
        if (H.F17 != 17) return 1;
        if (H.F18 != 18) return 1;
        if (H.F19 != 19) return 1;
                Console.WriteLine("tls_stress: wide_class_20 ok");
                return 0;
            }
        }
