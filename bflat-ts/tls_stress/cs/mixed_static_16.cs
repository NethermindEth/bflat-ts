        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class H {
            [ThreadStatic] public static int T0;
    public static int N0;
    [ThreadStatic] public static int T1;
    public static int N1;
    [ThreadStatic] public static int T2;
    public static int N2;
    [ThreadStatic] public static int T3;
    public static int N3;
    [ThreadStatic] public static int T4;
    public static int N4;
    [ThreadStatic] public static int T5;
    public static int N5;
    [ThreadStatic] public static int T6;
    public static int N6;
    [ThreadStatic] public static int T7;
    public static int N7;
    [ThreadStatic] public static int T8;
    public static int N8;
    [ThreadStatic] public static int T9;
    public static int N9;
    [ThreadStatic] public static int T10;
    public static int N10;
    [ThreadStatic] public static int T11;
    public static int N11;
    [ThreadStatic] public static int T12;
    public static int N12;
    [ThreadStatic] public static int T13;
    public static int N13;
    [ThreadStatic] public static int T14;
    public static int N14;
    [ThreadStatic] public static int T15;
    public static int N15;
        }

        class Program
        {
            static int Main()
            {
                H.T0 = 0; H.N0 = -0;
        H.T1 = 1; H.N1 = -1;
        H.T2 = 2; H.N2 = -2;
        H.T3 = 3; H.N3 = -3;
        H.T4 = 4; H.N4 = -4;
        H.T5 = 5; H.N5 = -5;
        H.T6 = 6; H.N6 = -6;
        H.T7 = 7; H.N7 = -7;
        H.T8 = 8; H.N8 = -8;
        H.T9 = 9; H.N9 = -9;
        H.T10 = 10; H.N10 = -10;
        H.T11 = 11; H.N11 = -11;
        H.T12 = 12; H.N12 = -12;
        H.T13 = 13; H.N13 = -13;
        H.T14 = 14; H.N14 = -14;
        H.T15 = 15; H.N15 = -15;
                if (H.T0 != 0 || H.N0 != -0) return 1;
        if (H.T1 != 1 || H.N1 != -1) return 1;
        if (H.T2 != 2 || H.N2 != -2) return 1;
        if (H.T3 != 3 || H.N3 != -3) return 1;
        if (H.T4 != 4 || H.N4 != -4) return 1;
        if (H.T5 != 5 || H.N5 != -5) return 1;
        if (H.T6 != 6 || H.N6 != -6) return 1;
        if (H.T7 != 7 || H.N7 != -7) return 1;
        if (H.T8 != 8 || H.N8 != -8) return 1;
        if (H.T9 != 9 || H.N9 != -9) return 1;
        if (H.T10 != 10 || H.N10 != -10) return 1;
        if (H.T11 != 11 || H.N11 != -11) return 1;
        if (H.T12 != 12 || H.N12 != -12) return 1;
        if (H.T13 != 13 || H.N13 != -13) return 1;
        if (H.T14 != 14 || H.N14 != -14) return 1;
        if (H.T15 != 15 || H.N15 != -15) return 1;
                Console.WriteLine("tls_stress: mixed_static_16 ok");
                return 0;
            }
        }
