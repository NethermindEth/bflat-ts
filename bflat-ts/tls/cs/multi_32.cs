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
    [ThreadStatic] public static int F16;
    [ThreadStatic] public static int F17;
    [ThreadStatic] public static int F18;
    [ThreadStatic] public static int F19;
    [ThreadStatic] public static int F20;
    [ThreadStatic] public static int F21;
    [ThreadStatic] public static int F22;
    [ThreadStatic] public static int F23;
    [ThreadStatic] public static int F24;
    [ThreadStatic] public static int F25;
    [ThreadStatic] public static int F26;
    [ThreadStatic] public static int F27;
    [ThreadStatic] public static int F28;
    [ThreadStatic] public static int F29;
    [ThreadStatic] public static int F30;
    [ThreadStatic] public static int F31;
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
        Holder.F16 = 16;
        Holder.F17 = 17;
        Holder.F18 = 18;
        Holder.F19 = 19;
        Holder.F20 = 20;
        Holder.F21 = 21;
        Holder.F22 = 22;
        Holder.F23 = 23;
        Holder.F24 = 24;
        Holder.F25 = 25;
        Holder.F26 = 26;
        Holder.F27 = 27;
        Holder.F28 = 28;
        Holder.F29 = 29;
        Holder.F30 = 30;
        Holder.F31 = 31;
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
        if (Holder.F16 != 16) return 1;
        if (Holder.F17 != 17) return 1;
        if (Holder.F18 != 18) return 1;
        if (Holder.F19 != 19) return 1;
        if (Holder.F20 != 20) return 1;
        if (Holder.F21 != 21) return 1;
        if (Holder.F22 != 22) return 1;
        if (Holder.F23 != 23) return 1;
        if (Holder.F24 != 24) return 1;
        if (Holder.F25 != 25) return 1;
        if (Holder.F26 != 26) return 1;
        if (Holder.F27 != 27) return 1;
        if (Holder.F28 != 28) return 1;
        if (Holder.F29 != 29) return 1;
        if (Holder.F30 != 30) return 1;
        if (Holder.F31 != 31) return 1;
                Console.WriteLine("tls: multi_32 ok");
                return 0;
            }
        }
