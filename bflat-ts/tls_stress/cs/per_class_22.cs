        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_22 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_22.V = 22;
                Slot_22.L = 22L * 100L;
                if (Slot_22.V != 22) return 1;
                if (Slot_22.L != 2200L) return 1;
                Console.WriteLine("tls_stress: per_class_22 ok");
                return 0;
            }
        }
