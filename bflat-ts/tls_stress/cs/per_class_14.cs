        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_14 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_14.V = 14;
                Slot_14.L = 14L * 100L;
                if (Slot_14.V != 14) return 1;
                if (Slot_14.L != 1400L) return 1;
                Console.WriteLine("tls_stress: per_class_14 ok");
                return 0;
            }
        }
