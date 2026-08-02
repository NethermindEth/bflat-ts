        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_3 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_3.V = 3;
                Slot_3.L = 3L * 100L;
                if (Slot_3.V != 3) return 1;
                if (Slot_3.L != 300L) return 1;
                Console.WriteLine("tls_stress: per_class_03 ok");
                return 0;
            }
        }
