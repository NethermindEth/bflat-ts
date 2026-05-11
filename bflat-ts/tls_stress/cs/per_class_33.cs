        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_33 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_33.V = 33;
                Slot_33.L = 33L * 100L;
                if (Slot_33.V != 33) return 1;
                if (Slot_33.L != 3300L) return 1;
                Console.WriteLine("tls_stress: per_class_33 ok");
                return 0;
            }
        }
