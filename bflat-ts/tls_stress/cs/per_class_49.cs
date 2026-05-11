        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_49 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_49.V = 49;
                Slot_49.L = 49L * 100L;
                if (Slot_49.V != 49) return 1;
                if (Slot_49.L != 4900L) return 1;
                Console.WriteLine("tls_stress: per_class_49 ok");
                return 0;
            }
        }
