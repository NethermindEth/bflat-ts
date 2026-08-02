        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_1 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_1.V = 1;
                Slot_1.L = 1L * 100L;
                if (Slot_1.V != 1) return 1;
                if (Slot_1.L != 100L) return 1;
                Console.WriteLine("tls_stress: per_class_01 ok");
                return 0;
            }
        }
