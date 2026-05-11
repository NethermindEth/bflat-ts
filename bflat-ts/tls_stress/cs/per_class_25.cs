        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_25 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_25.V = 25;
                Slot_25.L = 25L * 100L;
                if (Slot_25.V != 25) return 1;
                if (Slot_25.L != 2500L) return 1;
                Console.WriteLine("tls_stress: per_class_25 ok");
                return 0;
            }
        }
