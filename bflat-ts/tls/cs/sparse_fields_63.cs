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
    [ThreadStatic] public static int A11;
    [ThreadStatic] public static int A12;
    [ThreadStatic] public static int A13;
    [ThreadStatic] public static int A14;
    [ThreadStatic] public static int A15;
    [ThreadStatic] public static int A16;
    [ThreadStatic] public static int A17;
    [ThreadStatic] public static int A18;
    [ThreadStatic] public static int A19;
    [ThreadStatic] public static int A20;
    [ThreadStatic] public static int A21;
    [ThreadStatic] public static int A22;
    [ThreadStatic] public static int A23;
    [ThreadStatic] public static int A24;
    [ThreadStatic] public static int A25;
    [ThreadStatic] public static int A26;
    [ThreadStatic] public static int A27;
    [ThreadStatic] public static int A28;
    [ThreadStatic] public static int A29;
    [ThreadStatic] public static int A30;
    [ThreadStatic] public static int A31;
    [ThreadStatic] public static int A32;
    [ThreadStatic] public static int A33;
    [ThreadStatic] public static int A34;
    [ThreadStatic] public static int A35;
    [ThreadStatic] public static int A36;
    [ThreadStatic] public static int A37;
    [ThreadStatic] public static int A38;
    [ThreadStatic] public static int A39;
    [ThreadStatic] public static int A40;
    [ThreadStatic] public static int A41;
    [ThreadStatic] public static int A42;
    [ThreadStatic] public static int A43;
    [ThreadStatic] public static int A44;
    [ThreadStatic] public static int A45;
    [ThreadStatic] public static int A46;
    [ThreadStatic] public static int A47;
    [ThreadStatic] public static int A48;
    [ThreadStatic] public static int A49;
    [ThreadStatic] public static int A50;
    [ThreadStatic] public static int A51;
    [ThreadStatic] public static int A52;
    [ThreadStatic] public static int A53;
    [ThreadStatic] public static int A54;
    [ThreadStatic] public static int A55;
    [ThreadStatic] public static int A56;
    [ThreadStatic] public static int A57;
    [ThreadStatic] public static int A58;
    [ThreadStatic] public static int A59;
    [ThreadStatic] public static int A60;
    [ThreadStatic] public static int A61;
    [ThreadStatic] public static int A62;
        }

        class Program
        {
            static int Main()
            {
                H.A0 = 1;
                H.A62 = 2;
                if (H.A0 + H.A62 != 3) return 1;
                Console.WriteLine("tls: sparse_fields_63 ok");
                return 0;
            }
        }
