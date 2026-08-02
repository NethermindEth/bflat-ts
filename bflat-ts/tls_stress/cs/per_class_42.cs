        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_42 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_42.V = 42;
                Slot_42.L = 42L * 100L;
                if (Slot_42.V != 42) return 1;
                if (Slot_42.L != 4200L) return 1;
                Console.WriteLine("tls_stress: per_class_42 ok");
                return 0;
            }
        }
