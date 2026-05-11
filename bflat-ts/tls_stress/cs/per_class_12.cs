        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_12 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_12.V = 12;
                Slot_12.L = 12L * 100L;
                if (Slot_12.V != 12) return 1;
                if (Slot_12.L != 1200L) return 1;
                Console.WriteLine("tls_stress: per_class_12 ok");
                return 0;
            }
        }
