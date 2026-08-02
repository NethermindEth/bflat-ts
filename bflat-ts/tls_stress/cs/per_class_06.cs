        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_6 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_6.V = 6;
                Slot_6.L = 6L * 100L;
                if (Slot_6.V != 6) return 1;
                if (Slot_6.L != 600L) return 1;
                Console.WriteLine("tls_stress: per_class_06 ok");
                return 0;
            }
        }
