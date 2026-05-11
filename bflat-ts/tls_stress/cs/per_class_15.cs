        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_15 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_15.V = 15;
                Slot_15.L = 15L * 100L;
                if (Slot_15.V != 15) return 1;
                if (Slot_15.L != 1500L) return 1;
                Console.WriteLine("tls_stress: per_class_15 ok");
                return 0;
            }
        }
