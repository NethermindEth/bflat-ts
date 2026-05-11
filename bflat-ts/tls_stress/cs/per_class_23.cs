        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_23 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_23.V = 23;
                Slot_23.L = 23L * 100L;
                if (Slot_23.V != 23) return 1;
                if (Slot_23.L != 2300L) return 1;
                Console.WriteLine("tls_stress: per_class_23 ok");
                return 0;
            }
        }
