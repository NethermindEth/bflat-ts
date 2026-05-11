        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_55 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_55.V = 55;
                Slot_55.L = 55L * 100L;
                if (Slot_55.V != 55) return 1;
                if (Slot_55.L != 5500L) return 1;
                Console.WriteLine("tls_stress: per_class_55 ok");
                return 0;
            }
        }
