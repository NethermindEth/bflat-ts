        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_45 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_45.V = 45;
                Slot_45.L = 45L * 100L;
                if (Slot_45.V != 45) return 1;
                if (Slot_45.L != 4500L) return 1;
                Console.WriteLine("tls_stress: per_class_45 ok");
                return 0;
            }
        }
