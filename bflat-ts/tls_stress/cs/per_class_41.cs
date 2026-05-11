        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_41 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_41.V = 41;
                Slot_41.L = 41L * 100L;
                if (Slot_41.V != 41) return 1;
                if (Slot_41.L != 4100L) return 1;
                Console.WriteLine("tls_stress: per_class_41 ok");
                return 0;
            }
        }
