        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class S0 { [ThreadStatic] public static int V; }
class S1 { [ThreadStatic] public static long V; }
class S2 { [ThreadStatic] public static uint V; }
class S3 { [ThreadStatic] public static ulong V; }
class S4 { [ThreadStatic] public static short V; }
class S5 { [ThreadStatic] public static ushort V; }
class S6 { [ThreadStatic] public static byte V; }
class S7 { [ThreadStatic] public static sbyte V; }
class S8 { [ThreadStatic] public static bool V; }
class S9 { [ThreadStatic] public static char V; }

        class Program
        {
            static int Main()
            {
                S0.V = 1;
        S1.V = 2L;
        S2.V = 3u;
        S3.V = 4UL;
        S4.V = (short)5;
        S5.V = (ushort)6;
        S6.V = (byte)7;
        S7.V = (sbyte)8;
        S8.V = true;
        S9.V = 'x';
                Console.WriteLine("tls_stress: mixed_types_10 ok");
                return 0;
            }
        }
