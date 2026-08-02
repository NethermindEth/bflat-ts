        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class SlotInt { [ThreadStatic] public static int V; }
        class SlotLong { [ThreadStatic] public static long V; }
        class SlotString { [ThreadStatic] public static string V; }
        class SlotBool { [ThreadStatic] public static bool V; }
        class SlotByte { [ThreadStatic] public static byte V; }

        class Program
        {
            static int Main()
            {
                SlotInt.V = 42; SlotLong.V = 1234567890123L; SlotString.V = "hello"; SlotBool.V = true; SlotByte.V = 255;
                if (SlotInt.V != 42) return 1;
                if (SlotLong.V != 1234567890123L) return 1;
                if (SlotString.V != "hello") return 1;
                if (!SlotBool.V) return 1;
                if (SlotByte.V != 255) return 1;
                Console.WriteLine("tls_stress: tss_independent ok");
                return 0;
            }
        }
